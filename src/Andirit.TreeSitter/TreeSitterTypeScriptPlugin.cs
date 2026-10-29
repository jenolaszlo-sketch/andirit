namespace Andirit;

/// <summary>
/// Tree-sitter structural extraction for TypeScript. Files are parsed with the
/// pinned <c>tree-sitter-typescript</c> grammar, except <c>.tsx</c> which uses
/// the sibling <c>tree-sitter-tsx</c> grammar because JSX changes how type
/// assertions parse. Every fact carries structural evidence: classes,
/// interfaces, functions, imports, containment, and invocation names are
/// observed from syntax, never resolved.
/// </summary>
public sealed class TreeSitterTypeScriptPlugin : ICodeGraphPlugin
{
    internal static readonly CodePluginId TypeScriptPluginId = new("penghou.hetu.tree-sitter.typescript");
    private readonly ITreeSitterModuleResolver _moduleResolver;

    private static readonly string[] Extensions = [".ts", ".mts", ".cts", ".tsx"];

    /// <summary>
    /// Effective provider identity: package implementation plus pinned
    /// grammar, ABI, and extraction-profile revisions. TypeScript and TSX come
    /// from one grammar repository release, so they share the grammar
    /// component.
    /// </summary>
    private static readonly string TypeScriptPluginVersion =
        $"{TreeSitterPluginSupport.PackageVersion(typeof(TreeSitterTypeScriptPlugin))}" +
        $"+{TreeSitterGrammarRegistry.TypeScript.GrammarVersion}" +
        $".abi{TreeSitterGrammarRegistry.TypeScript.AbiVersion}" +
        $".rem1.p{EcmaScriptExtractionProfile.ProfileRevision}";

    public TreeSitterTypeScriptPlugin()
        : this(new EcmaScriptModuleResolver(
            "typescript",
            TreeSitterPluginSupport.PackageVersion(typeof(TreeSitterTypeScriptPlugin))))
    {
    }

    internal TreeSitterTypeScriptPlugin(ITreeSitterModuleResolver moduleResolver) =>
        _moduleResolver = moduleResolver ?? throw new ArgumentNullException(nameof(moduleResolver));

    public CodePluginId Id => TypeScriptPluginId;

    /// <summary>Gets the plugin package version driving index invalidation.</summary>
    public string Version => TypeScriptPluginVersion;

    internal static string PluginVersion => TypeScriptPluginVersion;

    internal static CodePluginId PluginId => TypeScriptPluginId;

    public string Language => "typescript";

    public IReadOnlyCollection<string> FileExtensions => Extensions;

    public CodeGraphCapabilities Capabilities => TreeSitterGrammarRegistry.TypeScript.Capabilities;

    /// <summary>Gets the pinned grammar descriptor for <c>.ts</c>-family files.</summary>
    public TreeSitterGrammarDescriptor Grammar => TreeSitterGrammarRegistry.TypeScript;

    /// <summary>Gets the pinned grammar descriptor used for <c>.tsx</c> files.</summary>
    public TreeSitterGrammarDescriptor TsxGrammar => TreeSitterGrammarRegistry.Tsx;

    public bool CanHandle(string path) =>
        TreeSitterPluginSupport.HasExtension(path, Extensions);

    public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
        CodeGraphPluginContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return new(new TreeSitterExtractionSession(
            context,
            TypeScriptPluginId,
            TypeScriptPluginVersion,
            EcmaScriptExtractionProfile.TypeScriptRelationshipKinds,
            static path => path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
                ? TreeSitterGrammarRegistry.Tsx
                : TreeSitterGrammarRegistry.TypeScript,
            static path => new EcmaScriptExtractionProfile(
                path,
                "typescript",
                TypeScriptPluginId,
                TypeScriptPluginVersion),
            _moduleResolver));
    }
}
