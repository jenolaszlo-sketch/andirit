namespace Andirit;

/// <summary>
/// Language-specific structural extraction shared by every Tree-sitter
/// provider. A profile maps one grammar's syntax onto Andirit graph facts and
/// declares only the language key and revision identity the generic session
/// needs; it never sees plugin, session, or native-runtime concerns.
/// </summary>
internal interface ITreeSitterExtractionProfile
{
    /// <summary>Gets the Andirit language key used for unit and file identities.</summary>
    string LanguageKey { get; }

    /// <summary>
    /// Gets the extraction-logic revision. Changes to emitted facts must bump
    /// this so provider freshness invalidates previous indexes.
    /// </summary>
    int Revision { get; }

    /// <summary>Extracts the facts of one parsed source file.</summary>
    TreeSitterExtractionResult Extract(
        TreeSitterSyntaxNode root,
        CodeNodeId fileNodeId,
        string path);
}

/// <summary>Accumulated, collision-safe facts produced for one source file.</summary>
internal sealed class TreeSitterExtractionResult
{
    internal Dictionary<string, CodeGraphNode> Nodes { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, CodeGraphDeclaration> Declarations { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, CodeGraphEdge> Edges { get; } = new(StringComparer.Ordinal);
    internal Dictionary<CodeEdgeKind, int> EdgeCounts { get; } = new();
    internal List<TreeSitterModuleReference> ModuleReferences { get; } = [];
    internal List<EcmaScriptExportObservation> ExportObservations { get; } = [];
}
