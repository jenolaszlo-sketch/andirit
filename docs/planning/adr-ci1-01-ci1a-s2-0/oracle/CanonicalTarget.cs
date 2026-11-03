using System.Globalization;
using Microsoft.CodeAnalysis;

namespace SemanticCorpusOracle;

/// <summary>The canonical description of one semantic target.</summary>
public readonly record struct TargetDescription(
    string DefiningUnit,
    string DocumentId,
    string DeclarationForm,
    string Shape);

/// <summary>
/// The single canonical target-description pathway used by both the main identity
/// view and the source lifecycle projection.
///
/// It implements the accepted R2 separation:
///   * the identifier digest input is the defining declaration's documentation id;
///   * the declaration digest input is the defining unit plus that document id;
///   * the shape digest input is the fixture-declared <c>method|parameterCount</c>
///     form (the named R2 target vectors use parameter counts, not Roslyn generic
///     arity);
///   * reduced extension methods are normalized to the unreduced defining method
///     using <see cref="IMethodSymbol.ReducedFrom"/>, falling back to
///     <see cref="ISymbol.OriginalDefinition"/> only when the method is not reduced.
///
/// The component-digest forms remain fixture-declared (qualification plan §6.1);
/// this type only removes the possibility of two oracle views inventing different
/// forms for the same occurrence.
/// </summary>
public static class CanonicalTarget
{
    public const string ShapePrefix = "method|";

    /// <summary>The accepted defining-declaration normalization.</summary>
    public static IMethodSymbol CanonicalSymbol(IMethodSymbol method) =>
        method.ReducedFrom ?? method.OriginalDefinition;

    /// <summary>Canonical shape input: <c>method|</c> plus the canonical parameter count.</summary>
    public static string ShapeFor(int parameterCount) =>
        ShapePrefix + parameterCount.ToString(CultureInfo.InvariantCulture);

    /// <summary>An explicitly declared fixture target tuple (named R2 vectors).</summary>
    public static TargetDescription Fixture(string definingUnit, string documentId, int parameterCount) =>
        new(definingUnit, documentId, definingUnit + "::" + documentId, ShapeFor(parameterCount));

    /// <summary>
    /// Derives the canonical target tuple from a resolved Roslyn method symbol.
    /// Returns null when the symbol has no documentation id or no verified unit.
    /// </summary>
    public static TargetDescription? Describe(IMethodSymbol method, Func<string?, string?> unitForAssembly)
    {
        var canonical = CanonicalSymbol(method);
        var definingUnit = unitForAssembly(canonical.ContainingAssembly?.Name);
        if (definingUnit is null)
            return null;
        var documentId = canonical.GetDocumentationCommentId();
        if (documentId is null)
            return null;
        return new TargetDescription(definingUnit, documentId,
            definingUnit + "::" + documentId, ShapeFor(canonical.Parameters.Length));
    }

    public static string TargetId(CorpusIdentity identity, TargetDescription target) =>
        identity.TargetId(target.DefiningUnit, target.DocumentId, target.DeclarationForm, target.Shape);

    /// <summary>The canonical <c>usage4</c> tuple parts for an occurrence of a target.</summary>
    public static string[] Usage4Parts(CorpusIdentity identity, TargetDescription target,
        string semanticUnit, string path, string sourceHash, int startLine, int startColumn,
        int endLine, int endColumn) =>
        R2Identity.Usage4Parts(CorpusBuild.RepoId, CorpusBuild.PluginId, semanticUnit, path,
            sourceHash, startLine, startColumn, endLine, endColumn, target.DocumentId,
            TargetId(identity, target));

    public static string OccurrenceId(CorpusIdentity identity, TargetDescription target,
        string semanticUnit, string path, string sourceHash, int startLine, int startColumn,
        int endLine, int endColumn) =>
        R2Identity.Hash("usage4", Usage4Parts(identity, target, semanticUnit, path, sourceHash,
            startLine, startColumn, endLine, endColumn));
}
