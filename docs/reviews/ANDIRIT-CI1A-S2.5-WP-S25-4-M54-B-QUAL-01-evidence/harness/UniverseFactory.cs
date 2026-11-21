// M54-B-QUAL-01 reviewer harness — construction helpers (reviewer-owned).
// Mirrors the established reviewer-harness pattern (S2.2 production tests):
// compilations are constructed in-process; VerifiedCSharpCompilation.Create /
// VerifiedCSharpScope.Declare / VerifiedCSharpUniverse.Create are the real
// production entry points under qualification. Nothing here modifies
// production or test projects.
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class Receipt
{
    public static void Emit(string name, object payload)
    {
        var dir = Environment.GetEnvironmentVariable("M54B_RECEIPTS");
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, name + ".json"),
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }
}

internal static class UniverseFactory
{
    internal const string RepositoryId = "fixture:m54b";

    internal static readonly CSharpCompilationOptions LibraryOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);

    internal static VerifiedMetadataReference CapturePlatform() =>
        VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None);

    // Captured PE evidence for the BCL surface used by the constructed source
    // (so a Console call is classified as PE-external evidence, not missing).
    internal static VerifiedMetadataReference CaptureConsole() =>
        VerifiedMetadataReference.Capture(typeof(Console).Assembly.Location, CancellationToken.None);

    internal static VerifiedCSharpSource ReadTextSource(string path, string text) =>
        ReadSource(path, Encoding.UTF8.GetBytes(text));

    internal static VerifiedCSharpSource ReadSource(string path, byte[] bytes) =>
        VerifiedCSharpSource.ReadAsync(
            new CodeGraphSource(path, "sha256:" + Sha256(bytes),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))),
            CancellationToken.None).AsTask().GetAwaiter().GetResult();

    internal static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static CSharpParseOptions ParseOptions() =>
        new(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Regular);

    internal static SyntaxTree Tree(VerifiedCSharpSource source) =>
        CSharpSyntaxTree.ParseText(source.Text, ParseOptions(), source.Path);

    internal static SyntaxTree Tree(string path, string text) =>
        CSharpSyntaxTree.ParseText(text, ParseOptions(), path);

    internal static CSharpCompilation Compile(
        string assembly,
        IEnumerable<SyntaxTree> trees,
        IEnumerable<MetadataReference> references,
        CSharpCompilationOptions? options = null) =>
        CSharpCompilation.Create(assembly, trees, references, options ?? LibraryOptions);

    internal static VerifiedCSharpCompilation CreateVerified(
        string projectUnit,
        CSharpCompilation compilation,
        IReadOnlyCollection<VerifiedCSharpSource> sources,
        IReadOnlyCollection<VerifiedMetadataReference> metadataReferences,
        IReadOnlyCollection<VerifiedCSharpCompilation> dependencies) =>
        VerifiedCSharpCompilation.Create(
            RepositoryId, projectUnit, compilation, sources, metadataReferences, dependencies,
            CancellationToken.None);

    internal static VerifiedCSharpScope DeclareScope(params (string Unit, string Path, string[] Sources)[] units) =>
        VerifiedCSharpScope.Declare(RepositoryId, units.Select(u => new CSharpScopeUnit(
            u.Unit,
            u.Path,
            u.Sources.Select(s => s).ToImmutableArray())));

    internal static VerifiedCSharpUniverse CreateUniverse(
        VerifiedCSharpScope scope,
        IReadOnlyCollection<VerifiedCSharpCompilation> entries,
        IEnumerable<string>? refusals = null) =>
        VerifiedCSharpUniverse.Create(RepositoryId, scope, entries, refusals);

    internal static IMethodSymbol Method(this CSharpCompilation compilation, string type, string name)
    {
        var symbol = compilation.GetTypeByMetadataName(type)
            ?? throw new InvalidOperationException($"type {type} not found in {compilation.AssemblyName}");
        return symbol.GetMembers(name).OfType<IMethodSymbol>().Single();
    }

    // Caller-supplied component digests (S-01 accepted obligation): deterministic
    // placeholders derived from the symbol name; never read from the code path.
    internal static string Digest(string label) => "sha256:" + Sha256(Encoding.UTF8.GetBytes(label));

    internal static string TargetDocument(IMethodSymbol symbol) =>
        "doc:" + Convert.ToHexStringLower(SHA256.HashData(
            Encoding.UTF8.GetBytes(symbol.ToDisplayString())))[..16];
}