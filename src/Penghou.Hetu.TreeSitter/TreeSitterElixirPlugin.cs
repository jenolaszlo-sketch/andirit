namespace Penghou.Hetu;

/// <summary>
/// Tree-sitter structural extraction for Elixir, backed by the pinned
/// <c>tree-sitter-elixir</c> grammar. Every fact carries structural evidence:
/// modules, functions, imports, containment, and invocation names are
/// observed from syntax, never resolved. Malformed regions yield partial
/// results with diagnostics rather than fabricated relationships.
/// </summary>
public sealed class TreeSitterElixirPlugin : ICodeGraphPlugin
{
    internal static readonly CodePluginId ElixirPluginId = new("penghou.hetu.tree-sitter.elixir");

    private static readonly string[] Extensions = [".ex", ".exs"];

    /// <summary>
    /// Effective provider identity: package implementation plus pinned
    /// grammar, ABI, and extraction-profile revisions. Any component change
    /// alters this string, so planner freshness can never survive a grammar
    /// or profile change behind an unchanged package version.
    /// </summary>
    private static readonly string ElixirPluginVersion =
        $"{TreeSitterPluginSupport.PackageVersion(typeof(TreeSitterElixirPlugin))}" +
        $"+{TreeSitterGrammarRegistry.Elixir.GrammarVersion}" +
        $".abi{TreeSitterGrammarRegistry.Elixir.AbiVersion}" +
        $".p{ElixirExtractionProfile.ProfileRevision}";

    public CodePluginId Id => ElixirPluginId;

    /// <summary>Gets the plugin package version driving index invalidation.</summary>
    public string Version => ElixirPluginVersion;

    internal static string PluginVersion => ElixirPluginVersion;

    internal static CodePluginId PluginId => ElixirPluginId;

    public string Language => TreeSitterGrammarRegistry.Elixir.Language;

    public IReadOnlyCollection<string> FileExtensions => Extensions;

    public CodeGraphCapabilities Capabilities => TreeSitterGrammarRegistry.Elixir.Capabilities;

    /// <summary>Gets the pinned grammar descriptor backing this plugin.</summary>
    public TreeSitterGrammarDescriptor Grammar => TreeSitterGrammarRegistry.Elixir;

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
            ElixirPluginId,
            ElixirPluginVersion,
            static _ => TreeSitterGrammarRegistry.Elixir,
            static path => new ElixirExtractionProfile(path)));
    }
}
