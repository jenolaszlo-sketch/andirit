using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Andirit;

/// <summary>
/// Semantic inputs for one bounded extraction pass. The component digests and the
/// target document are caller-supplied under the accepted S-01 qualification; the
/// extractor does not canonicalize or independently verify them.
/// </summary>
internal sealed record VerifiedOccurrenceExtractionRequest(
    CodeRepositoryId RepositoryId,
    CodePluginId PluginId,
    VerifiedCSharpCompilation SourceUnit,
    Func<IMethodSymbol, string> IdentifierDigest,
    Func<IMethodSymbol, string> DeclarationDigest,
    Func<IMethodSymbol, string> ShapeDigest,
    Func<IMethodSymbol, string> TargetDocumentId,
    string? PluginVersion = null);

/// <summary>
/// S2.2 / WP5 bounded occurrence extraction, coverage and F09 scanner. Supported
/// `Invocation` sites are bound to the exact live defining compilation in the
/// accepted universe and routed through the WP4 `asmctx3` mint before any `usage4`
/// fact is created; every other outcome is reported as coverage, never as a trusted
/// fact. Coverage is constructed directly (the deferred NEW-04 <c>Merge</c> is not
/// used). No clause here invents a universal extraction guarantee.
/// </summary>
internal static class VerifiedCSharpOccurrenceExtractor
{
    private static readonly Regex InvocationLike =
        new(@"(?<![A-Za-z0-9_])(?!(?:if|for|foreach|while|switch|catch|using|lock|fixed|return|new|typeof|sizeof|nameof|default|checked|unchecked|when|do|else|try|finally|throw|yield|await)\b)[A-Za-z_][A-Za-z0-9_]*\s*\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static VerifiedOccurrenceExtraction Extract(
        VerifiedCSharpUniverse universe, VerifiedOccurrenceExtractionRequest request)
    {
        ArgumentNullException.ThrowIfNull(universe);
        ArgumentNullException.ThrowIfNull(request);
        var sourceUnit = request.SourceUnit;
        var facts = ImmutableArray.CreateBuilder<CodeOccurrenceFact>();
        var authorities = ImmutableArray.CreateBuilder<CodeOccurrenceDefiningAuthority>();
        int unsupportedCategories = 0, external = 0, ambiguous = 0, unresolved = 0,
            unsupported = 0, conditionalInactive = 0;

        foreach (var tree in sourceUnit.Compilation.SyntaxTrees)
        {
            var source = sourceUnit.Sources.SingleOrDefault(s => s.Path == tree.FilePath);
            if (source is null) { unresolved++; continue; }
            var sourceHash = source.ContentHash;
            var model = sourceUnit.Compilation.GetSemanticModel(tree);

            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var info = model.GetSymbolInfo(invocation);
                var symbol = info.Symbol as IMethodSymbol;
                if (symbol is null)
                {
                    // WP5-Q1 F1: only a uniquely resolved symbol may establish the
                    // trusted method target. Multiple equally-applicable candidates
                    // are an `Ambiguous` refusal; any other unresolved binding stays
                    // `Unresolved`. Candidate selection never invents a binding.
                    if (info.CandidateSymbols.Length > 1) ambiguous++;
                    else unresolved++;
                    continue;
                }
                if (!IsSupportedInvocation(symbol)) { unsupported++; unsupportedCategories++; continue; }

                var resolved = universe.ResolveDefining(symbol, null, sourceUnit.Compilation);
                if (resolved.Status != VerifiedDefiningStatus.Complete)
                {
                    switch (resolved.Status)
                    {
                        case VerifiedDefiningStatus.Ambiguous: ambiguous++; break;
                        case VerifiedDefiningStatus.UnsupportedExternalEvidence: external++; break;
                        default: unresolved++; break;
                    }
                    continue;
                }

                // Fresh extraction records the defining compilation's current verified
                // context; the WP4 mint re-validates it and remains the sole authority.
                var mint = universe.MintUsage(new VerifiedUsageInput(
                    request.RepositoryId, request.PluginId, symbol,
                    resolved.Defining!.Descriptor.DescriptorDigest,
                    sourceUnit.Descriptor.ProjectUnitId,
                    SiteSpan(tree, invocation),
                    sourceHash, sourceHash,
                    request.TargetDocumentId(symbol),
                    request.IdentifierDigest(symbol),
                    request.DeclarationDigest(symbol),
                    request.ShapeDigest(symbol),
                    sourceUnit.Compilation,
                    DeclarationSpan(symbol),
                    request.PluginVersion));

                if (mint.IsMinted)
                {
                    facts.Add(mint.Fact!);
                    authorities.Add(mint.Authority!);
                }
                else unresolved++;
            }

            conditionalInactive += CountConditionalInactiveSites(tree);
        }

        var complete = universe.IsComplete;
        var state = complete
            ? CodeRelationshipCoverageState.Produced
            : CodeRelationshipCoverageState.Partial;
        var coverage = new CodeOccurrenceCoverage(
            facts.Count, unsupportedCategories, state,
            universe.Scope.Identity, universe.Identity,
            universe.Scope.Units.Length, universe.Entries.Length,
            external, ambiguous, unresolved, unsupported, conditionalInactive);
        // A substituted source instance or mismatched repository never obtains a
        // native authority envelope, even if the legacy extraction yielded facts.
        var canRecord = complete && universe.Scope.RepositoryId == request.RepositoryId.Value &&
            universe.Entries.Any(e => ReferenceEquals(e, sourceUnit));
        var authority = canRecord ? new CodeOccurrenceAuthority(
            1, request.RepositoryId.Value, request.PluginId.Value, sourceUnit.Descriptor.ProjectUnitId,
            sourceUnit.Descriptor.ProjectUnitId, universe.Scope.Identity, universe.Identity, coverage,
            universe.Entries.Select(e => new CodeOccurrenceCompilationAuthority(
                e.Descriptor.ProjectUnitId, e.Descriptor.DescriptorDigest)).ToArray(),
            authorities.ToImmutable()) : null;
        return new(facts.ToImmutable(), coverage, authority);
    }

    private static bool IsSupportedInvocation(IMethodSymbol symbol) =>
        symbol.MethodKind is MethodKind.Ordinary
            or MethodKind.ReducedExtension
            or MethodKind.ExplicitInterfaceImplementation;

    private static CodeLocation SiteSpan(SyntaxTree tree, InvocationExpressionSyntax invocation)
    {
        var token = invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier,
            SimpleNameSyntax simple => simple.Identifier,
            _ => invocation.Expression.GetLastToken(),
        };
        return ToLocation(tree, token.Span);
    }

    private static CodeLocation? DeclarationSpan(IMethodSymbol symbol)
    {
        var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (reference is null) return null;
        try
        {
            var node = reference.GetSyntax();
            return ToLocation(node.SyntaxTree, node.Span);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static CodeLocation ToLocation(SyntaxTree tree, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var line = tree.GetLineSpan(span);
        return new CodeLocation(
            tree.FilePath.Replace('\\', '/'),
            line.StartLinePosition.Line + 1,
            line.StartLinePosition.Character + 1,
            line.EndLinePosition.Line + 1,
            line.EndLinePosition.Character + 1);
    }

    /// <summary>
    /// F09 bounded disabled-text scan. Disabled text is determined by the tree's
    /// declared preprocessor configuration, so any invocation-like site inside it is
    /// `ConditionalInactive` coverage and never a confirmed fact. The site pattern is
    /// a bounded textual heuristic; exact scanner precision is a WP5-Q1 subject.
    /// </summary>
    private static int CountConditionalInactiveSites(SyntaxTree tree)
    {
        var count = 0;
        foreach (var trivia in tree.GetRoot().DescendantTrivia(descendIntoTrivia: true))
        {
            if (!trivia.IsKind(SyntaxKind.DisabledTextTrivia)) continue;
            count += InvocationLike.Matches(trivia.ToString()).Count;
        }
        return count;
    }
}
