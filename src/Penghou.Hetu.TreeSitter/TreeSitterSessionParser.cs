namespace Penghou.Hetu;

using TreeSitterApi = global::TreeSitter;

/// <summary>
/// Owns one Tree-sitter language and parser for a single extraction session.
/// Language loading validates the pinned ABI before any source is parsed;
/// every parsed document is disposed after its facts are extracted, so no
/// native tree outlives the file it came from. Parsing is synchronous and
/// cancellation applies between files, not mid-parse.
/// </summary>
internal sealed class TreeSitterSessionParser : IDisposable
{
    private readonly TreeSitterApi.Language _language;
    private readonly TreeSitterApi.Parser _parser;
    private bool _disposed;

    private TreeSitterSessionParser(TreeSitterGrammarDescriptor grammar, TreeSitterApi.Language language)
    {
        Grammar = grammar;
        _language = language;
        _parser = new TreeSitterApi.Parser(language);
    }

    internal TreeSitterGrammarDescriptor Grammar { get; }

    internal static TreeSitterSessionParser Create(TreeSitterGrammarDescriptor grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        var library = TreeSitterNative.ResolveGrammarLibrary(grammar);
        TreeSitterApi.Language language;
        try
        {
            language = new TreeSitterApi.Language(library, grammar.LanguageFunction);
        }
        catch (Exception exception) when (exception is not CodePluginFailureException)
        {
            throw new CodePluginFailureException(
                CodePluginFailureKind.ProviderUnavailable,
                $"The {grammar.GrammarId} native library could not be loaded.",
                exception);
        }

        if (language.AbiVersion != grammar.AbiVersion)
        {
            language.Dispose();
            throw new CodePluginFailureException(
                CodePluginFailureKind.VersionMismatch,
                $"The {grammar.GrammarId} grammar reports ABI {language.AbiVersion} " +
                $"but version {grammar.GrammarVersion} pins ABI {grammar.AbiVersion}.");
        }

        return new TreeSitterSessionParser(grammar, language);
    }

    internal TreeSitterDocument Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            var tree = _parser.Parse(source);
            if (tree is null)
            {
                throw new CodePluginFailureException(
                    CodePluginFailureKind.ProviderCrash,
                    $"The {Grammar.GrammarId} parser returned no syntax tree.");
            }

            return new TreeSitterDocument(tree);
        }
        catch (Exception exception) when (exception is not CodePluginFailureException)
        {
            throw new CodePluginFailureException(
                CodePluginFailureKind.ProviderCrash,
                $"The {Grammar.GrammarId} parser failed.",
                exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _parser.Dispose();
        _language.Dispose();
    }
}

/// <summary>One parsed source file with a disposable native tree.</summary>
internal sealed class TreeSitterDocument : IDisposable
{
    private readonly TreeSitterApi.Tree _tree;
    private bool _disposed;

    internal TreeSitterDocument(TreeSitterApi.Tree tree)
    {
        _tree = tree;
        Root = new TreeSitterSyntaxNode(tree.RootNode);
    }

    internal TreeSitterSyntaxNode Root { get; }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _tree.Dispose();
    }
}
