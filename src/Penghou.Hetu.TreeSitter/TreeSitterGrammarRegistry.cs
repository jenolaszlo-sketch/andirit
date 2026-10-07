namespace Penghou.Hetu;

/// <summary>Grammar pins bundled with <c>Penghou.Hetu.TreeSitter</c>.</summary>
internal static class TreeSitterGrammarRegistry
{
    /// <summary>
    /// The official Elixir grammar. Rebuild procedure and upgrade policy live
    /// in <c>native/grammar-pins.md</c>; a pin change must also ship rebuilt
    /// per-RID natives in <c>runtimes/</c>.
    /// </summary>
    internal static readonly TreeSitterGrammarDescriptor Elixir = new(
        language: "elixir",
        grammarId: "tree-sitter-elixir",
        grammarSource: "https://github.com/elixir-lang/tree-sitter-elixir",
        grammarVersion: "v0.3.5",
        grammarCommit: "e2d9e6e0e76b0c436fa48a0b8c32a031d0cbdf49",
        abiVersion: 14,
        nativeLibrary: "tree-sitter-elixir",
        languageFunction: "tree_sitter_elixir",
        capabilities: CodeGraphCapabilities.Syntax |
            CodeGraphCapabilities.Declarations |
            CodeGraphCapabilities.Imports |
            CodeGraphCapabilities.Diagnostics);

    internal static TreeSitterGrammarDescriptor ForLanguage(string language) =>
        language == Elixir.Language
            ? Elixir
            : throw new CodePluginFailureException(
                CodePluginFailureKind.UnsupportedLanguage,
                $"The Tree-sitter integration has no grammar for language '{language}'.");
}
