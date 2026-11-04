using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S2.2 foundation boundary tests: WP1 (Q-F2 encoding), WP2 (Q-F3 metadata
/// validation) and WP3 (A-01 preprocessor-symbol canonicalization). No
/// occurrence extraction, association minting or native publication.
/// </summary>
public sealed class S2_2FoundationBoundaryTests
{
    private const string RepositoryId = "fixture:s2.2-foundation";
    private static readonly CSharpCompilationOptions LibraryOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);

    // ---------------------------------------------------------------- WP1 / Q-F2

    [Fact]
    public async Task SourceRead_RefusesBomlessUtf16AndUtf32NulInterleavedDecode()
    {
        const string text = "class A { }";
        // No BOM preamble: these are the BOM-less inputs Q-F2 bounds.
        var cases = new (string Path, byte[] Bytes)[]
        {
            ("utf16le.cs", Encoding.Unicode.GetBytes(text)),
            ("utf16be.cs", new UnicodeEncoding(bigEndian: true, byteOrderMark: false).GetBytes(text)),
            ("utf32le.cs", new UTF32Encoding(bigEndian: false, byteOrderMark: false).GetBytes(text)),
            ("utf32be.cs", new UTF32Encoding(bigEndian: true, byteOrderMark: false).GetBytes(text))
        };

        foreach (var (path, bytes) in cases)
        {
            var source = await ReadSourceAsync(path, bytes);

            Assert.NotNull(source.Refusal);
            Assert.StartsWith("AmbiguousBomlessEncoding:", source.Refusal);
            // The raw-byte identity stays honest; only verification is refused.
            Assert.Equal("sha256:" + Sha256(bytes), source.ContentHash);
        }
    }

    [Fact]
    public async Task SourceRead_RefusesAmbiguousBomlessDecodeWithNonAsciiBytes()
    {
        // A BOM-less UTF-16LE non-ASCII sequence is not valid strict UTF-8, so
        // the strict decoder already refuses it (a different bounded path).
        var bytes = new byte[] { 0xE9, 0x00, 0x41, 0x00 };
        var source = await ReadSourceAsync("nonascii.cs", bytes);

        Assert.NotNull(source.Refusal);
        Assert.DoesNotContain("AmbiguousBomlessEncoding", source.Refusal);
    }

    [Fact]
    public async Task SourceRead_BomlessUtf8ContainingNulIsRefusedUnderBoundedContract()
    {
        // Deliberate limitation of the bounded rule: a legitimate BOM-less UTF-8
        // file containing a NUL character is refused, because the rule cannot
        // distinguish it from a BOM-less UTF-16/32 misread.
        var bytes = Encoding.UTF8.GetBytes("class A {}\0");
        var source = await ReadSourceAsync("nul.cs", bytes);

        Assert.NotNull(source.Refusal);
        Assert.StartsWith("AmbiguousBomlessEncoding:", source.Refusal);
    }

    [Fact]
    public async Task SourceRead_BomPresenceExcludesTheBoundedBomlessNulRule()
    {
        // The bounded rule applies only when no supported BOM was recognized.
        var bytes = new UTF8Encoding(true, true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes("class A {}\0")).ToArray();
        var source = await ReadSourceAsync("bom-nul.cs", bytes);

        Assert.Null(source.Refusal);
        Assert.EndsWith(":bom", source.EncodingName);
    }

    [Fact]
    public async Task SourceRead_AmbiguousRefusalPreventsVerifiedCompilation()
    {
        var bytes = Encoding.Unicode.GetBytes("class A { }");
        var source = await ReadSourceAsync("utf16le.cs", bytes);
        var platform = CapturePlatform();
        var compilation = Compile("A", [Tree(source)], [platform.Reference], LibraryOptions);

        Assert.NotNull(source.Refusal);
        Assert.Throws<InvalidDataException>(() =>
            CreateVerified("src/A.csproj", compilation, [source], [platform], []));
    }

    // ---------------------------------------------------------------- WP2 / Q-F3

    [Fact]
    public void MetadataCapture_RefusesTruncatedGarbageAndNonAssemblyImages()
    {
        var directory = NewTemporaryDirectory();
        try
        {
            // Positive control: a valid managed assembly is still captured.
            var valid = Path.Combine(directory, "Valid.dll");
            File.WriteAllBytes(valid, EmitLibrary());
            var captured = VerifiedMetadataReference.Capture(valid, CancellationToken.None);
            Assert.StartsWith("file|", captured.Identity);

            // Truncated PE image.
            var full = EmitLibrary();
            var truncated = Path.Combine(directory, "Truncated.dll");
            File.WriteAllBytes(truncated, full.Take(full.Length / 3).ToArray());
            Assert.Throws<InvalidDataException>(() =>
                VerifiedMetadataReference.Capture(truncated, CancellationToken.None));

            // Garbage bytes with no PE structure.
            var garbage = Path.Combine(directory, "Garbage.dll");
            var noise = new byte[256];
            for (var i = 0; i < noise.Length; i++) noise[i] = (byte)(i * 7 + 13);
            File.WriteAllBytes(garbage, noise);
            Assert.Throws<InvalidDataException>(() =>
                VerifiedMetadataReference.Capture(garbage, CancellationToken.None));

            // Non-assembly CLI metadata (netmodule): parses as a module, has no
            // assembly manifest, and must never back a captured assembly identity.
            var netmodule = Path.Combine(directory, "Mod.netmodule");
            File.WriteAllBytes(netmodule, NonAssemblyMetadataFixture.NetModuleBytes);
            Assert.Throws<InvalidDataException>(() =>
                VerifiedMetadataReference.Capture(netmodule, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    // ---------------------------------------------------------------- WP3 / A-01

    [Fact]
    public async Task VerifiedCompilation_RejectsPreprocessorSymbolsWithCommaOrPipe()
    {
        var source = await ReadTextSourceAsync(
            "src/A.cs", "#if FEATURE\npublic sealed class A { }\n#endif");
        var platform = CapturePlatform();

        foreach (var symbol in new[] { "A,B", "A|B" })
        {
            var compilation = Compile(
                "A", [Tree(source, ParseOptions(symbol))], [platform.Reference], LibraryOptions);
            Assert.Throws<InvalidDataException>(() =>
                CreateVerified("src/A.csproj", compilation, [source], [platform], []));
        }

        // Valid identifier symbols remain accepted and canonically ordered.
        var accepted = Compile(
            "A", [Tree(source, ParseOptions("TRACE", "FEATURE"))], [platform.Reference], LibraryOptions);
        var verified = CreateVerified("src/A.csproj", accepted, [source], [platform], []);
        Assert.Equal("FEATURE,TRACE", verified.Descriptor.Sources.Single().PreprocessorSymbols);
    }

    // ------------------------------------------------------------------- helpers

    private static async Task<VerifiedCSharpSource> ReadTextSourceAsync(string path, string text) =>
        await ReadSourceAsync(path, Encoding.UTF8.GetBytes(text));

    private static async Task<VerifiedCSharpSource> ReadSourceAsync(string path, byte[] bytes) =>
        await VerifiedCSharpSource.ReadAsync(
            new CodeGraphSource(
                path,
                "sha256:" + Sha256(bytes),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))),
            CancellationToken.None);

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static CSharpParseOptions ParseOptions(params string[] symbols) =>
        new(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Regular, symbols);

    private static SyntaxTree Tree(VerifiedCSharpSource source, CSharpParseOptions? options = null) =>
        CSharpSyntaxTree.ParseText(source.Text, options ?? ParseOptions(), source.Path);

    private static CSharpCompilation Compile(
        string assembly,
        IEnumerable<SyntaxTree> trees,
        IEnumerable<MetadataReference> references,
        CSharpCompilationOptions options) =>
        CSharpCompilation.Create(assembly, trees, references, options);

    private static VerifiedCSharpCompilation CreateVerified(
        string projectUnit,
        CSharpCompilation compilation,
        IReadOnlyCollection<VerifiedCSharpSource> sources,
        IReadOnlyCollection<VerifiedMetadataReference> metadataReferences,
        IReadOnlyCollection<VerifiedCSharpCompilation> dependencies) =>
        VerifiedCSharpCompilation.Create(
            RepositoryId, projectUnit, compilation, sources, metadataReferences, dependencies,
            CancellationToken.None);

    private static VerifiedMetadataReference CapturePlatform() =>
        VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None);

    private static byte[] EmitLibrary()
    {
        var tree = CSharpSyntaxTree.ParseText(
            "namespace RefLib; public static class Api { public static int Value() => 1; }",
            ParseOptions(),
            "RefLib.cs");
        var compilation = Compile(
            "RefLib",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            LibraryOptions);
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return output.ToArray();
    }

    private static string NewTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Andirit-S2.2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
