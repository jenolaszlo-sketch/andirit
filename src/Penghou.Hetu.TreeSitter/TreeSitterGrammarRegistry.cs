namespace Penghou.Hetu;

/// <summary>Grammar pins bundled with <c>Penghou.Hetu.TreeSitter</c>.</summary>
internal static class TreeSitterGrammarRegistry
{
    private const CodeGraphCapabilities StructuralCapabilities =
        CodeGraphCapabilities.Syntax |
        CodeGraphCapabilities.Declarations |
        CodeGraphCapabilities.Imports |
        CodeGraphCapabilities.Diagnostics;

    /// <summary>
    /// Capabilities of the ECMAScript profiles (TypeScript, TSX, JavaScript).
    /// They emit structural <c>inherits</c> (class/interface heritage and
    /// JavaScript <c>extends</c>) and TypeScript additionally <c>implements</c>;
    /// the recognized JavaScript prototype idioms are <c>inherits</c> with
    /// <c>Derived</c> evidence. This is heritage syntax, not resolved type
    /// binding, but it is real emitted relationship coverage, so the
    /// Inheritance capability is advertised. Elixir emits no inheritance facts
    /// and deliberately keeps <see cref="StructuralCapabilities"/>.
    /// </summary>
    private const CodeGraphCapabilities EcmaScriptCapabilities =
        StructuralCapabilities | CodeGraphCapabilities.Inheritance;

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
        capabilities: StructuralCapabilities);

    /// <summary>
    /// The official TypeScript grammar for <c>.ts</c>/<c>.mts</c>/<c>.cts</c>.
    /// The native library is supplied by the pinned <c>TreeSitter.DotNet</c>
    /// package (which vendors the grammar submodule) rather than checked into
    /// this repository; see <c>native/grammar-pins.md</c>.
    /// </summary>
    internal static readonly TreeSitterGrammarDescriptor TypeScript = new(
        language: "typescript",
        grammarId: "tree-sitter-typescript",
        grammarSource: "https://github.com/tree-sitter/tree-sitter-typescript",
        grammarVersion: "v0.23.2",
        grammarCommit: "75b3874edb2dc714fb1fd77a32013d0f8699989f",
        abiVersion: 14,
        nativeLibrary: "tree-sitter-typescript",
        languageFunction: "tree_sitter_typescript",
        capabilities: EcmaScriptCapabilities);

    /// <summary>
    /// The TSX grammar for <c>.tsx</c>, from the same pinned
    /// <c>tree-sitter-typescript</c> repository and commit as
    /// <see cref="TypeScript"/>. It is a distinct grammar because JSX changes
    /// how type assertions parse.
    /// </summary>
    internal static readonly TreeSitterGrammarDescriptor Tsx = new(
        language: "typescript",
        grammarId: "tree-sitter-tsx",
        grammarSource: "https://github.com/tree-sitter/tree-sitter-typescript",
        grammarVersion: "v0.23.2",
        grammarCommit: "75b3874edb2dc714fb1fd77a32013d0f8699989f",
        abiVersion: 14,
        nativeLibrary: "tree-sitter-tsx",
        languageFunction: "tree_sitter_tsx",
        capabilities: EcmaScriptCapabilities);

    /// <summary>
    /// The official JavaScript grammar for <c>.js</c>/<c>.mjs</c>/<c>.cjs</c>
    /// and <c>.jsx</c>, supplied by the pinned <c>TreeSitter.DotNet</c>
    /// package.
    /// </summary>
    internal static readonly TreeSitterGrammarDescriptor JavaScript = new(
        language: "javascript",
        grammarId: "tree-sitter-javascript",
        grammarSource: "https://github.com/tree-sitter/tree-sitter-javascript",
        grammarVersion: "v0.25.0",
        grammarCommit: "58404d8cf191d69f2674a8fd507bd5776f46cb11",
        abiVersion: 15,
        nativeLibrary: "tree-sitter-javascript",
        languageFunction: "tree_sitter_javascript",
        capabilities: EcmaScriptCapabilities);

    internal static TreeSitterGrammarDescriptor ForLanguage(string language) =>
        language switch
        {
            "elixir" => Elixir,
            "typescript" => TypeScript,
            "javascript" => JavaScript,
            _ => throw new CodePluginFailureException(
                CodePluginFailureKind.UnsupportedLanguage,
                $"The Tree-sitter integration has no grammar for language '{language}'.")
        };
}
