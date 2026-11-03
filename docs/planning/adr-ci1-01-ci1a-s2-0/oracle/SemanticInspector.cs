using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace SemanticCorpusOracle;

public enum Resolution
{
    Resolved,
    Ambiguous,
    Unresolved,
    Dynamic,
    InvalidInvocation,
    Inactive,
}

/// <summary>The actual Roslyn result for one designated source position.</summary>
public sealed record SemanticResult(
    Resolution Resolution,
    string? SymbolKey,
    string? Assembly,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn,
    string Detail);

/// <summary>
/// Direct Roslyn semantic inspection. Locates a source marker, selects the
/// designated syntax node, and records the compiler's actual binding. It is not
/// derived from, and never calls, the production occurrence extractor.
/// </summary>
public static class SemanticInspector
{
    public static SyntaxNode? Locate(SyntaxTree tree, string marker, SyntaxKind kind)
    {
        var root = tree.GetRoot();
        var comment = root.DescendantTrivia(descendIntoTrivia: true)
            .FirstOrDefault(t => t.ToString().Contains(marker, StringComparison.Ordinal));
        if (comment == default)
            return null;
        var after = comment.Span.End;
        return root.DescendantNodes()
            .Where(n => n.IsKind(kind) && n.SpanStart >= after)
            .OrderBy(n => n.SpanStart)
            .FirstOrDefault();
    }

    public static SemanticResult Inspect(CSharpCompilation compilation, SyntaxTree tree, string marker, SyntaxKind kind)
    {
        var model = compilation.GetSemanticModel(tree);
        var node = Locate(tree, marker, kind);
        if (node is null)
            return new SemanticResult(Resolution.Inactive, null, null, 0, 0, 0, 0,
                "no active node of the requested kind follows the marker");

        var span = IdentifierSpan(tree, node);
        var (symbolNode, typeNode) = kind switch
        {
            SyntaxKind.TypeOfExpression => (node, ((TypeOfExpressionSyntax)node).Type as SyntaxNode),
            _ => (node, (SyntaxNode?)null),
        };

        var info = model.GetSymbolInfo(symbolNode!);
        var operation = model.GetOperation(node);
        var diagnostics = model.GetDiagnostics(node.Span).Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (diagnostics.Any(d => d.Id == "CS0176"))
            return new SemanticResult(Resolution.InvalidInvocation, null, null, span.Item1, span.Item2, span.Item3, span.Item4,
                "invalid invocation: " + string.Join(",", diagnostics.Select(d => d.Id)));
        if (operation is IDynamicInvocationOperation or IDynamicMemberReferenceOperation)
            return new SemanticResult(Resolution.Dynamic, null, null, span.Item1, span.Item2, span.Item3, span.Item4,
                "dynamic binding: no statically resolved symbol");

        ISymbol? symbol = info.Symbol ?? (typeNode is not null ? model.GetSymbolInfo(typeNode).Symbol : null);
        if (symbol is not null && symbol.Kind != SymbolKind.ErrorType)
            return new SemanticResult(Resolution.Resolved, SymbolKey(symbol), symbol.ContainingAssembly?.Name,
                span.Item1, span.Item2, span.Item3, span.Item4, "resolved");

        if (info.CandidateSymbols.Length > 0)
        {
            var isAmbiguous = info.CandidateReason == CandidateReason.Ambiguous || diagnostics.Any(d => d.Id == "CS0121");
            var resolution = isAmbiguous ? Resolution.Ambiguous : Resolution.InvalidInvocation;
            return new SemanticResult(resolution, null, null, span.Item1, span.Item2, span.Item3, span.Item4,
                (isAmbiguous ? "ambiguous: " : "invalid invocation: ") + info.CandidateReason +
                " candidates=" + info.CandidateSymbols.Length + " diagnostics=" + string.Join(",", diagnostics.Select(d => d.Id)));
        }

        return new SemanticResult(Resolution.Unresolved, null, null, span.Item1, span.Item2, span.Item3, span.Item4,
            "no unique symbol: " + info.CandidateReason);
    }

    /// <summary>
    /// Canonical symbol key: the defining declaration's documentation id where
    /// one exists. For constructed generic methods the original definition is
    /// used (S04); for reduced extension methods the unreduced static method is
    /// used, so the key names the accepted canonical defining declaration.
    /// </summary>
    public static string SymbolKey(ISymbol symbol)
    {
        var assembly = symbol.ContainingAssembly?.Name ?? "(none)";
        var canonical = symbol is IMethodSymbol method
            ? (ISymbol)CanonicalTarget.CanonicalSymbol(method)
            : symbol.OriginalDefinition;
        var docId = canonical.GetDocumentationCommentId();
        if (docId is not null)
            return assembly + "!" + docId;
        var extra = symbol is IMethodSymbol m ? "/" + m.MethodKind : string.Empty;
        return assembly + "!" + symbol.Kind + ":" + symbol.Name + extra;
    }

    /// <summary>Identifier-token span: 1-based start and 1-based exclusive end.</summary>
    private static (int, int, int, int) IdentifierSpan(SyntaxTree tree, SyntaxNode node)
    {
        var token = IdentifierToken(node);
        var lineSpan = tree.GetLineSpan(token.Span);
        return (lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1,
            lineSpan.EndLinePosition.Line + 1, lineSpan.EndLinePosition.Character + 1);
    }

    private static SyntaxToken IdentifierToken(SyntaxNode node) => node switch
    {
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax ma } => ma.Name.Identifier,
        InvocationExpressionSyntax { Expression: SimpleNameSyntax sn } => sn.Identifier,
        MemberAccessExpressionSyntax ma => ma.Name.Identifier,
        IdentifierNameSyntax id => id.Identifier,
        ObjectCreationExpressionSyntax oc => oc.Type.GetLastToken(),
        TypeOfExpressionSyntax to => to.Type.GetLastToken(),
        _ => node.GetFirstToken(),
    };
}
