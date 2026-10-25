namespace Penghou.Hetu;

/// <summary>
/// Tree-sitter structural extraction for JavaScript, backed by the pinned
/// <c>tree-sitter-javascript</c> grammar (<c>.js</c>/<c>.mjs</c>/<c>.cjs</c> and
/// <c>.jsx</c>). Every fact carries structural evidence: classes, functions,
/// imports, containment, and invocation names are observed from syntax, never
/// resolved.
/// </summary>
public sealed class TreeSitterJavaScriptPlugin : ICodeGraphPlugin
{
    internal static readonly CodePluginId JavaScriptPluginId = new("penghou.hetu.tree-sitter.javascript");
    private readonly ITreeSitterModuleResolver _moduleResolver;

    private static readonly string[] Extensions = [".js", ".mjs", ".cjs", ".jsx"];

    /// <summary>
    /// Effective provider identity: package implementation plus pinned
    /// grammar, ABI, and extraction-profile revisions.
    /// </summary>
    private static readonly string JavaScriptPluginVersion =
        $"{TreeSitterPluginSupport.PackageVersion(typeof(TreeSitterJavaScriptPlugin))}" +
        $"+{TreeSitterGrammarRegistry.JavaScript.GrammarVersion}" +
        $".abi{TreeSitterGrammarRegistry.JavaScript.AbiVersion}" +
        $".p{EcmaScriptExtractionProfile.ProfileRevision}";

    public TreeSitterJavaScriptPlugin()
        : this(new EcmaScriptModuleResolver(
            "javascript",
            TreeSitterPluginSupport.PackageVersion(typeof(TreeSitterJavaScriptPlugin))))
    {
    }

    internal TreeSitterJavaScriptPlugin(ITreeSitterModuleResolver moduleResolver) =>
        _moduleResolver = moduleResolver ?? throw new ArgumentNullException(nameof(moduleResolver));

    public CodePluginId Id => JavaScriptPluginId;

    /// <summary>Gets the plugin package version driving index invalidation.</summary>
    public string Version => JavaScriptPluginVersion;

    internal static string PluginVersion => JavaScriptPluginVersion;

    internal static CodePluginId PluginId => JavaScriptPluginId;

    public string Language => "javascript";

    public IReadOnlyCollection<string> FileExtensions => Extensions;

    public CodeGraphCapabilities Capabilities => TreeSitterGrammarRegistry.JavaScript.Capabilities;

    /// <summary>Gets the pinned grammar descriptor backing this plugin.</summary>
    public TreeSitterGrammarDescriptor Grammar => TreeSitterGrammarRegistry.JavaScript;

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
            JavaScriptPluginId,
            JavaScriptPluginVersion,
            EcmaScriptExtractionProfile.JavaScriptRelationshipKinds,
            static _ => TreeSitterGrammarRegistry.JavaScript,
            static path => new EcmaScriptExtractionProfile(
                path,
                "javascript",
                JavaScriptPluginId,
                JavaScriptPluginVersion),
            _moduleResolver));
    }
}
