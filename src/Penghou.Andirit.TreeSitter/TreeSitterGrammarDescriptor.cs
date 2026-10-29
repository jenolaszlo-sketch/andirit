namespace Penghou.Andirit;

/// <summary>
/// Pinned identity of one Tree-sitter grammar bundled with
/// <c>Penghou.Andirit.TreeSitter</c>. Grammars are pinned native dependencies:
/// the repository, version, and commit below identify exactly which parser
/// produced a graph fact, so upgrades participate in Andirit's normal
/// plugin-version freshness behavior.
/// </summary>
public sealed record TreeSitterGrammarDescriptor
{
    public TreeSitterGrammarDescriptor(
        string language,
        string grammarId,
        string grammarSource,
        string grammarVersion,
        string grammarCommit,
        int abiVersion,
        string nativeLibrary,
        string languageFunction,
        CodeGraphCapabilities capabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language, nameof(language));
        ArgumentException.ThrowIfNullOrWhiteSpace(grammarId, nameof(grammarId));
        ArgumentException.ThrowIfNullOrWhiteSpace(grammarSource, nameof(grammarSource));
        ArgumentException.ThrowIfNullOrWhiteSpace(grammarVersion, nameof(grammarVersion));
        ArgumentException.ThrowIfNullOrWhiteSpace(grammarCommit, nameof(grammarCommit));
        if (abiVersion < 13 || abiVersion > 15)
            throw new ArgumentOutOfRangeException(
                nameof(abiVersion),
                "Tree-sitter language ABI versions 13 through 15 are supported.");
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeLibrary, nameof(nativeLibrary));
        ArgumentException.ThrowIfNullOrWhiteSpace(languageFunction, nameof(languageFunction));

        Language = language;
        GrammarId = grammarId;
        GrammarSource = grammarSource;
        GrammarVersion = grammarVersion;
        GrammarCommit = grammarCommit;
        AbiVersion = abiVersion;
        NativeLibrary = nativeLibrary;
        LanguageFunction = languageFunction;
        Capabilities = capabilities;
    }

    /// <summary>Gets the Andirit language key (for example <c>elixir</c>).</summary>
    public string Language { get; }
    /// <summary>Gets the grammar project name (for example <c>tree-sitter-elixir</c>).</summary>
    public string GrammarId { get; }
    /// <summary>Gets the grammar source repository.</summary>
    public string GrammarSource { get; }
    /// <summary>Gets the pinned grammar release or tag.</summary>
    public string GrammarVersion { get; }
    /// <summary>Gets the pinned grammar commit hash.</summary>
    public string GrammarCommit { get; }
    /// <summary>Gets the expected Tree-sitter language ABI, validated at load.</summary>
    public int AbiVersion { get; }
    /// <summary>Gets the native library base name without platform prefix or extension.</summary>
    public string NativeLibrary { get; }
    /// <summary>Gets the exported language factory function (for example <c>tree_sitter_elixir</c>).</summary>
    public string LanguageFunction { get; }
    /// <summary>Gets the extraction capabilities this grammar integration provides.</summary>
    public CodeGraphCapabilities Capabilities { get; }
}
