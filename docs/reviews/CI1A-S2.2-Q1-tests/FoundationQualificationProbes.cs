using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;
using Frozen = AdrCi1Exp.Shared;

namespace IndependentFoundationReview;

// Reviewer-owned probes. Observation tests deliberately preserve counterexamples;
// their passing status does not mean the challenged production contract passes.
public sealed class FoundationQualificationProbes(ITestOutputHelper output)
{
    private const string Repo = "review:s2.2-q1";
    private static readonly CSharpCompilationOptions Options = new(
        OutputKind.DynamicallyLinkedLibrary, deterministic: true, concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform = new(() =>
        VerifiedMetadataReference.Capture(typeof(object).Assembly.Location));

    public static IEnumerable<object[]> EncodingCases()
    {
        const string text = "// café 中文 😀\r\nclass A {}\n";
        foreach (var encoding in new Encoding[] { new UTF8Encoding(true, true),
            new UnicodeEncoding(false, true, true), new UnicodeEncoding(true, true, true),
            new UTF32Encoding(false, true, true), new UTF32Encoding(true, true, true) })
            yield return [encoding.WebName + "-bom", encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray(), null!, text];
        yield return ["utf8", Encoding.UTF8.GetBytes(text), null!, text];
        yield return ["empty", Array.Empty<byte>(), null!, ""];
        yield return ["short", new byte[] { 0x61 }, null!, "a"];
        foreach (var encoding in new Encoding[] { Encoding.Unicode, Encoding.BigEndianUnicode,
            new UTF32Encoding(false, false), new UTF32Encoding(true, false) })
            yield return [encoding.WebName + "-bomless", encoding.GetBytes("class A {}"), "AmbiguousBomlessEncoding", null!];
        foreach (var bytes in new byte[][] { [0xef], [0xef, 0xbb], [0xff], [0xfe],
            [0xff, 0xfe, 0], [0, 0, 0xfe], [0xff, 0xfe, 0, 0, 0x41],
            [0xfe, 0xff, 0xd8, 0], [0, 0, 0xfe, 0xff, 0, 0x11, 0, 0], [0xc0, 0xaf], [0x80] })
            yield return ["malformed-" + Convert.ToHexString(bytes), bytes, "InvalidSourceEncoding", null!];
        yield return ["nul", new byte[] { 0 }, "AmbiguousBomlessEncoding", null!];
        yield return ["utf8-bom-nul", new byte[] { 0xef, 0xbb, 0xbf, 0 }, null!, "\0"];
        yield return ["utf16-bom-only", new byte[] { 0xff, 0xfe }, null!, ""];
        yield return ["utf32-bom-only", new byte[] { 0xff, 0xfe, 0, 0 }, null!, ""];
        // BOM-less UTF-16 U+4141 has indistinguishable valid UTF-8 bytes: known limit.
        yield return ["indistinguishable-utf16", new byte[] { 0x41, 0x41 }, null!, "AA"];
    }

    [Theory]
    [MemberData(nameof(EncodingCases))]
    public async Task EncodingBoundary(string name, byte[] bytes, string? reason, string? text)
    {
        var source = await Read("probe.cs", bytes);
        var repeat = await Read("probe.cs", bytes);
        Assert.Equal(reason is null ? null : reason + ":probe.cs", source.Refusal);
        Assert.Equal(source.Refusal, repeat.Refusal);
        Assert.Equal("sha256:" + Hex(bytes), source.ContentHash);
        if (text is not null) Assert.Equal(text, source.Text.ToString());
        var compilation = Compile("A", [Tree(source)], [Platform.Value.Reference]);
        if (reason is not null)
            Assert.StartsWith("UnverifiedSource:", Assert.Throws<InvalidDataException>(() =>
                Verify("A.csproj", compilation, [source], [Platform.Value], [])).Message);
        else
            Assert.NotNull(Verify("A.csproj", compilation, [source], [Platform.Value], []));
        Log(new { kind = "encoding", name, bytes = Convert.ToHexString(bytes), source.ContentHash,
            source.EncodingName, source.Refusal, decoded = source.Text.ToString() });
    }

    [Fact]
    public async Task EncodingRefusalPrecedenceAndMaterializedHandoff()
    {
        byte[] bytes = [0, 0x61];
        var wrong = new CodeGraphSource("probe.cs", "sha256:" + new string('0', 64),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes)));
        Assert.Equal("SourceHashMismatch:probe.cs", (await VerifiedCSharpSource.ReadAsync(wrong)).Refusal);
        // Exercise the lifecycle-owned buffer branch as well as the stream branch.
        var factory = typeof(CodeGraphSource).GetMethod("FromMaterialized",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var materialized = (CodeGraphSource)factory.Invoke(null,
            ["probe.cs", "sha256:" + Hex(bytes), bytes, bytes.Length, "sha256:" + Hex(bytes)])!;
        var captured = await VerifiedCSharpSource.ReadAsync(materialized);
        Assert.Equal("AmbiguousBomlessEncoding:probe.cs", captured.Refusal);
        Assert.Equal("sha256:" + Hex(bytes), captured.ContentHash);
    }

    [Theory]
    [InlineData("A,B")]
    [InlineData("A|B")]
    [InlineData("A,B|C")]
    [InlineData(",")]
    [InlineData("|")]
    public async Task SeparatorInjection(string symbol)
    {
        var source = await Read("A.cs", Encoding.UTF8.GetBytes("class A {}"));
        var compilation = Compile("A", [Tree(source, symbol)], [Platform.Value.Reference]);
        Assert.Equal("UnsupportedParseConfiguration", Assert.Throws<InvalidDataException>(() =>
            Verify("A.csproj", compilation, [source], [Platform.Value], [])).Message);
    }

    [Fact]
    public async Task SymbolsOrderOrdinallyAndKeepUnicodeDistinct()
    {
        var source = await Read("A.cs", Encoding.UTF8.GetBytes("class A {}"));
        VerifiedCSharpCompilation Make(params string[] symbols) => Verify("A.csproj",
            Compile("A", [Tree(source, symbols)], [Platform.Value.Reference]), [source], [Platform.Value], []);
        Assert.Equal(Make("TRACE", "FEATURE").Descriptor.DescriptorDigest,
            Make("FEATURE", "TRACE").Descriptor.DescriptorDigest);
        Assert.NotEqual(Make("é").Descriptor.DescriptorDigest, Make("e\u0301").Descriptor.DescriptorDigest);
        Log(new { kind = "symbols", canonical = Make("TRACE", "FEATURE").Descriptor.Sources[0].CanonicalForm });
    }

    [Fact]
    public void MetadataCancellationAndIoStaySeparateFromFormatRefusal()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => VerifiedMetadataReference.Capture("absent.dll", cancellation.Token));
        string absent = Path.Combine(Path.GetTempPath(), "Andirit-absent-" + Guid.NewGuid().ToString("N"), "absent.dll");
        Assert.Throws<DirectoryNotFoundException>(() => VerifiedMetadataReference.Capture(absent));
    }

    [Fact]
    public void New02MatrixTupleAndFrozenUsageAnchors()
    {
        var targets = new[] {
            "asmctx3:sha256:632e7953365d1dbadbb55414b7cbcb991c9bc2653ec6cafb6f6d9d95a9c2032d",
            "asmctx3:sha256:fb3e0561272eb342a549036ecf0095c2ec2a6d5d7bc159fc943dee11fe669c26" };
        var expected = new[] {
            "bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af",
            "b874bdc5afa194e0e237d0b1d7d36b3fa3dd767799e0dc62f5f1399f00b90bca" };
        for (int index = 0; index < targets.Length; index++)
        {
            string[] parts = ["experiment:andirit-31c33a9", "experiment.csharp.occurrences",
                "usage4-b2c-exp-r2-20261101", "caller-unit", "Caller.cs",
                "sha256:2f7803daa5910bf2f5fb5927b2e9f999ebd1b7074340096f620df37278df484a",
                "1", "72", "1", "77", "M:N.C.M", targets[index], "method-invocation"];
            Assert.Equal(expected[index], Hash("usage4", parts));
            Assert.Equal(expected[index], CodeOccurrenceIdentity.ComputeOccurrenceId(
                new CodeRepositoryId(parts[0]), new CodePluginId(parts[1]), parts[3], parts[4], parts[5],
                new CodeLocation("Caller.cs", 1, 72, 1, 77), parts[10], new CodeOccurrenceTargetId(parts[11]),
                CodeOccurrenceCategory.Invocation).Value);
            for (int field = 0; field < parts.Length; field++)
            {
                var altered = (string[])parts.Clone();
                altered[field] += "changed";
                Assert.NotEqual(expected[index], Hash("usage4", altered));
            }
            Log(new { kind = "new02-anchor", parts, expected = expected[index] });
        }
        string context = "compctx1:sha256:" + new string('a', 64);
        string[] targetParts = ["compctx1", context, "metadata", "module", "identifier", "declaration", "shape"];
        string target = "asmctx3:sha256:" + Hash("asmctx3-context", targetParts);
        Assert.Equal(target, CodeOccurrenceIdentity.ComputeTargetId(context, "metadata", "module", "identifier", "declaration", "shape").Value);
        for (int field = 0; field < targetParts.Length; field++)
        {
            var altered = (string[])targetParts.Clone(); altered[field] += "changed";
            Assert.NotEqual(target, "asmctx3:sha256:" + Hash("asmctx3-context", altered));
        }
    }

    [Fact]
    public void MetadataControlsAndTruncationSurvey()
    {
        var image = Emit();
        WithFile(image, path =>
        {
            var capture = VerifiedMetadataReference.Capture(path);
            Assert.EndsWith("|" + Hex(image), capture.Identity);
            using var metadata = Assert.IsType<AssemblyMetadata>(capture.Reference.GetMetadata());
            Assert.True(metadata.GetModules()[0].GetMetadataReader().IsAssembly);
        });
        foreach (var imageKind in new[] { OutputKind.NetModule })
            WithFile(Emit(imageKind), path => Assert.Equal("UnsupportedMetadataReference",
                Assert.Throws<InvalidDataException>(() => VerifiedMetadataReference.Capture(path)).Message));
        foreach (var garbage in new byte[][] { [], [0x4d, 0x5a], Enumerable.Range(0, 256).Select(i => (byte)i).ToArray() })
            WithFile(garbage, path => Assert.Equal("UnsupportedMetadataReference",
                Assert.Throws<InvalidDataException>(() => VerifiedMetadataReference.Capture(path)).Message));
        using var pe = new PEReader(new MemoryStream(image));
        int metadataEnd = pe.PEHeaders.MetadataStartOffset + pe.PEHeaders.MetadataSize;
        int[] lengths = [1, 64, image.Length / 3, pe.PEHeaders.MetadataStartOffset,
            pe.PEHeaders.MetadataStartOffset + 20, metadataEnd - 1, metadataEnd, image.Length - 1];
        foreach (int length in lengths.Distinct())
            WithFile(image[..length], path =>
            {
                var error = Record.Exception(() => VerifiedMetadataReference.Capture(path));
                if (length < metadataEnd) Assert.IsType<InvalidDataException>(error);
                Log(new { kind = "truncation", length, fullLength = image.Length, metadataEnd,
                    outcome = error?.GetType().Name ?? "captured", message = error?.Message, hash = Hex(image[..length]) });
            });
    }

    [Theory]
    [InlineData("assembly-name")]
    [InlineData("first-type-name")]
    [InlineData("later-type-name")]
    [InlineData("method-signature")]
    public void ObserveMalformedMetadataStillExposesIdentity(string damage)
    {
        var image = Emit();
        int offset;
        using (var pe = new PEReader(new MemoryStream(image)))
        {
            var reader = pe.GetMetadataReader();
            int start = pe.PEHeaders.MetadataStartOffset;
            Assert.True(reader.GetHeapSize(HeapIndex.String) < ushort.MaxValue);
            offset = damage switch
            {
                "assembly-name" => start + reader.GetTableMetadataOffset(TableIndex.Assembly) + 18,
                "first-type-name" => start + reader.GetTableMetadataOffset(TableIndex.TypeDef) + 4,
                "later-type-name" => start + reader.GetTableMetadataOffset(TableIndex.TypeDef) + 2 * reader.GetTableRowSize(TableIndex.TypeDef) + 4,
                "method-signature" => start + reader.GetTableMetadataOffset(TableIndex.MethodDef) + 10,
                _ => throw new InvalidOperationException()
            };
        }
        // All emitted heaps use two-byte indices. 65535 points beyond each heap.
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(offset, 2), ushort.MaxValue);
        using (var pe = new PEReader(new MemoryStream(image)))
        {
            var reader = pe.GetMetadataReader();
            var deeper = Record.Exception(() =>
            {
                switch (damage)
                {
                    case "assembly-name": _ = reader.GetString(reader.GetAssemblyDefinition().Name); break;
                    case "first-type-name": _ = reader.GetString(reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(1)).Name); break;
                    case "later-type-name": _ = reader.GetString(reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(3)).Name); break;
                    default: _ = reader.GetBlobBytes(reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1)).Signature); break;
                }
            });
            Assert.IsType<BadImageFormatException>(deeper);
        }
        WithFile(image, path =>
        {
            if (damage == "assembly-name")
            {
                // Corrected reviewer assumption: GetModules eagerly reads the
                // assembly identity, so this particular damaged field refuses.
                Assert.Equal("UnsupportedMetadataReference", Assert.Throws<InvalidDataException>(() =>
                    VerifiedMetadataReference.Capture(path)).Message);
                Log(new { kind = "malformed-assembly-refused", damage, offset, hash = Hex(image) });
                return;
            }
            // Counterexample: the type/string/blob indices remain deferred.
            var captured = VerifiedMetadataReference.Capture(path);
            Assert.EndsWith("|" + Hex(image), captured.Identity);
            Log(new { kind = "malformed-metadata-counterexample", damage, offset,
                patch = "FFFF", hash = Hex(image), identity = captured.Identity,
                deeperRead = "BadImageFormatException" });
        });
    }

    [Fact]
    public void MalformedMetadataRootIsRefused()
    {
        var image = Emit();
        using (var pe = new PEReader(new MemoryStream(image)))
        {
            var reader = pe.GetMetadataReader();
            int offset = pe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.TypeDef);
            // Destroy the metadata root signature independently of managed binding.
            image[pe.PEHeaders.MetadataStartOffset] = 0;
            Log(new { kind = "damaged-root", offset, hash = Hex(image) });
        }
        WithFile(image, path => Assert.Equal("UnsupportedMetadataReference",
            Assert.Throws<InvalidDataException>(() => VerifiedMetadataReference.Capture(path)).Message));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DamagedMetadataStreamOffsetAndTableCountAreRefused(bool rowCount)
    {
        var image = Emit();
        using (var pe = new PEReader(new MemoryStream(image)))
        {
            int root = pe.PEHeaders.MetadataStartOffset;
            int versionLength = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(root + 12));
            int header = root + 16 + ((versionLength + 3) & ~3);
            int streamCount = BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(header + 2));
            int streamHeader = header + 4;
            for (int index = 0; index < streamCount; index++)
            {
                int nameEnd = Array.IndexOf(image, (byte)0, streamHeader + 8);
                string name = Encoding.ASCII.GetString(image, streamHeader + 8, nameEnd - streamHeader - 8);
                if (name is "#~" or "#-")
                {
                    int patch = streamHeader;
                    if (rowCount)
                    {
                        int tables = root + BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(streamHeader));
                        ulong valid = BinaryPrimitives.ReadUInt64LittleEndian(image.AsSpan(tables + 8));
                        int preceding = System.Numerics.BitOperations.PopCount(valid & ((1UL << (int)TableIndex.TypeDef) - 1));
                        patch = tables + 24 + 4 * preceding;
                    }
                    BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(patch, 4), 0x7fffffff);
                    Log(new { kind = rowCount ? "table-count" : "stream-offset", patch, hash = Hex(image) });
                    break;
                }
                streamHeader = (nameEnd + 4) & ~3;
            }
        }
        WithFile(image, path => Assert.Equal("UnsupportedMetadataReference",
            Assert.Throws<InvalidDataException>(() => VerifiedMetadataReference.Capture(path)).Message));
    }

    [Fact]
    public async Task MappingReconstructsFrozenAuthorityAndSeparatesTransitiveProvenance()
    {
        async Task<VerifiedCSharpCompilation> Make(string name, string text, string[] symbols,
            params VerifiedCSharpCompilation[] dependencies)
        {
            // BOM-present source proves that own-source byte hashes differ from tree hashes.
            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
            var source = await Read(name + "/A.cs", bytes);
            return Verify(name + "/" + name + ".csproj",
                Compile(name, [Tree(source, symbols)], new MetadataReference[] { Platform.Value.Reference }
                    .Concat(dependencies.Select(d => d.Compilation.ToMetadataReference()))),
                [source], [Platform.Value], dependencies);
        }
        var leaf = await Make("Leaf", "public class Leaf {}", ["ONE"]);
        var leafChanged = await Make("Leaf", "public class Leaf {}", ["TWO"]);
        var lib = await Make("Lib", "public class Lib {}", [], leaf);
        var libChanged = await Make("Lib", "public class Lib {}", [], leafChanged);
        var app = await Make("App", "public class App {}", [], lib);
        var appChanged = await Make("App", "public class App {}", [], libChanged);
        foreach (var value in new[] { leaf, lib, app, leafChanged, libChanged, appChanged })
        {
            Assert.Equal(FrozenDescribe(value).DescriptorDigest, Mapped(value));
        }
        Assert.Equal(leaf.LegacyDescriptor!.DescriptorDigest, Mapped(leaf));
        Assert.Null(lib.LegacyDescriptor);
        Assert.Equal(Mapped(lib), Mapped(libChanged));
        Assert.Equal(Mapped(app), Mapped(appChanged));
        Assert.NotEqual(Mapped(leaf), Mapped(leafChanged));
        foreach (var pair in new[] { (leaf, leafChanged), (lib, libChanged), (app, appChanged) })
            Assert.NotEqual(pair.Item1.Descriptor.DescriptorDigest, pair.Item2.Descriptor.DescriptorDigest);
        Assert.Equal(Target(lib), Target(libChanged));
        Assert.NotEqual(Target(leaf), Target(leafChanged));
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            lib.Descriptor.DescriptorDigest, "metadata", "module", "identifier", "declaration", "shape"));
        var reversed = lib.Compilation.WithReferences(lib.Compilation.References.Reverse());
        var reordered = Verify("Lib/Lib.csproj", reversed, lib.Sources, [Platform.Value], [leaf]);
        Assert.Equal(lib.Descriptor.DescriptorDigest, reordered.Descriptor.DescriptorDigest);
        Assert.Equal(Mapped(lib), Mapped(reordered));
        Assert.Equal("MissingDefiningCompilation", Assert.Throws<InvalidDataException>(() =>
            Verify("Lib/Lib.csproj", lib.Compilation, lib.Sources, [Platform.Value], [leafChanged])).Message);
        Assert.Equal("ReferenceMembershipMismatch", Assert.Throws<InvalidDataException>(() =>
            Verify("Lib/Lib.csproj", lib.Compilation, lib.Sources, [Platform.Value], [])).Message);
        var scope = VerifiedCSharpScope.Declare(Repo, new[] { leaf, lib, app }.Select(v =>
            new CSharpScopeUnit(v.Descriptor.ProjectUnitId, v.Descriptor.ProjectUnitId,
                v.Sources.Select(s => s.Path).ToImmutableArray())));
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [leaf, lib, app]);
        Assert.True(universe.IsComplete);
        Assert.Equal("MissingDefiningCompilation", Assert.Throws<InvalidDataException>(() =>
            universe.RequireCompilation(libChanged.Compilation)).Message);
        var changedSourceScope = VerifiedCSharpScope.Declare(Repo, scope.Units.Select(u =>
            u with { SourcePaths = ["other.cs"] }));
        Assert.Equal(scope.Identity, changedSourceScope.Identity);
        Assert.Equal("ScopeSourceMembershipMismatch", Assert.Throws<InvalidDataException>(() =>
            VerifiedCSharpUniverse.Create(Repo, changedSourceScope, [leaf, lib, app])).Message);
        Log(new { kind = "mapping", leaf = Mapped(leaf), lib = Mapped(lib), app = Mapped(app),
            leaf2 = Mapped(leafChanged), lib2 = Mapped(libChanged), app2 = Mapped(appChanged),
            provenance = new[] { leaf, lib, app, leafChanged, libChanged, appChanged }.Select(v => v.Descriptor.DescriptorDigest),
            libTarget = Target(lib), libTarget2 = Target(libChanged), scope = scope.Identity, universe = universe.Identity });
    }

    [Fact]
    public async Task ObserveTextuallyEquivalentLiveInstancesHaveSameIdentity()
    {
        var source = await Read("A.cs", Encoding.UTF8.GetBytes("class A {}"));
        var first = Verify("A.csproj", Compile("A", [Tree(source)], [Platform.Value.Reference]), [source], [Platform.Value], []);
        var second = Verify("A.csproj", Compile("A", [Tree(source)], [Platform.Value.Reference]), [source], [Platform.Value], []);
        Assert.False(ReferenceEquals(first.Compilation, second.Compilation));
        Assert.Equal(Mapped(first), Mapped(second));
        Assert.Equal(Target(first), Target(second));
        // Current helpers check canonical shape, not construction/live authority.
        var fabricated = "compctx1:sha256:" + new string('0', 64);
        var target = CodeOccurrenceIdentity.ComputeTargetId(fabricated, "m", "n", "i", "d", "s");
        Assert.NotNull(new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('0', 64)));
        Log(new { kind = "mapping-counterexamples", sameTarget = Target(first), fabricated, acceptedTarget = target.Value });
    }

    [Fact]
    public async Task ObserveFrozenCorpusReferenceProfileDiffersFromLiveCapture()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        string corpusRoot = Path.Combine(root, "docs/planning/adr-ci1-01-ci1a-s2-0/corpus");
        var corpus = SemanticCorpusOracle.CorpusBuild.Build(corpusRoot, SemanticCorpusOracle.CorpusVariant.Baseline);
        var frozen = SemanticCorpusOracle.CorpusIdentity.Compute(corpus);
        var live = new Dictionary<string, VerifiedCSharpCompilation>();
        foreach (string unit in new[] { "Leaf/Leaf.csproj", "Core/Core.csproj" })
        {
            var original = corpus.Get(unit);
            var sourceList = new List<VerifiedCSharpSource>();
            foreach (var source in original.SourceBytes) sourceList.Add(await Read(source.Key, source.Value));
            var metadata = original.Compilation.References.OfType<PortableExecutableReference>()
                .Select(r => VerifiedMetadataReference.Capture(r.FilePath!)).ToArray();
            var dependencies = original.Compilation.References.OfType<CompilationReference>()
                .Select(r => live.Values.Single(v => v.Compilation.AssemblyName == r.Compilation.AssemblyName)).ToArray();
            var refs = metadata.Select(m => (MetadataReference)m.Reference)
                .Concat(dependencies.Select(d => d.Compilation.ToMetadataReference()));
            var compilation = original.Compilation.WithReferences(refs);
            var verified = VerifiedCSharpCompilation.Create(SemanticCorpusOracle.CorpusBuild.RepoId,
                unit, compilation, sourceList, metadata, dependencies);
            live.Add(unit, verified);
            Assert.NotEqual(frozen.Compctx1[unit], Mapped(verified));
            // Explicit test-only preimage alignment reproduces the frozen anchor.
            Assert.Equal(frozen.Compctx1[unit], Mapped(verified, corpus.FrameworkIdentities));
            Log(new { kind = "reference-profile-counterexample", unit, frozen = frozen.Compctx1[unit],
                liveMapped = Mapped(verified), aligned = Mapped(verified, corpus.FrameworkIdentities),
                liveReferences = metadata.Select(m => m.Identity), frozenReferences = corpus.FrameworkIdentities });
        }
    }

    private static Frozen.CompilationDescriptor FrozenDescribe(VerifiedCSharpCompilation value) =>
        Frozen.CompilationProvenance.Describe(Repo, value.Descriptor.ProjectUnitId, value.Compilation,
            value.Sources.Select(s => (s.Path, new UTF8Encoding(true).GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(s.Text.ToString())).ToArray())).ToArray());

    // Independent implementation of mapping-contract §2; no production mapper exists.
    private static string Mapped(VerifiedCSharpCompilation value, IEnumerable<string>? peReferenceOverride = null)
    {
        var d = value.Descriptor;
        var projects = value.Compilation.References.OfType<CompilationReference>().Select(cr =>
            "compilation|" + cr.Compilation.AssemblyName + "|" + Metadata(cr.Compilation.Assembly.Identity) + "|" +
                string.Join(",", cr.Compilation.SyntaxTrees.OrderBy(t => t.FilePath, StringComparer.Ordinal)
                    .Select(t => t.FilePath.Replace('\\', '/') + "=" + Hex(Encoding.UTF8.GetBytes(t.GetText().ToString())))));
        var references = (peReferenceOverride ?? d.References.Where(r => !r.StartsWith("compilation-context|", StringComparison.Ordinal)))
            .Concat(projects).Order(StringComparer.Ordinal);
        return "compctx1:sha256:" + Hash("compctx1-context", new[] { "compctx1", d.RepositoryId,
            d.ProjectUnitId, d.AssemblyName, d.MetadataIdentity, d.ModuleName, d.OptionsDigest }
            .Concat(d.Sources.Select(s => s.CanonicalForm)).Concat(references));
    }

    private static string Target(VerifiedCSharpCompilation value) => "asmctx3:sha256:" + Hash("asmctx3-context",
        ["compctx1", Mapped(value), value.Descriptor.MetadataIdentity, value.Descriptor.ModuleName, "identifier", "declaration", "shape"]);
    private static string Metadata(AssemblyIdentity id) => string.Join("|", id.Name, id.Version.ToString(),
        id.CultureName ?? "", Convert.ToHexStringLower(id.PublicKeyToken.AsSpan()));
    private static string Hash(string domain, IEnumerable<string> parts)
    {
        using var stream = new MemoryStream();
        foreach (var part in new[] { domain }.Concat(parts))
        {
            var bytes = Encoding.UTF8.GetBytes(part);
            var length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            stream.Write(length); stream.Write(bytes);
        }
        return Hex(stream.ToArray());
    }
    private static string Hex(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static async Task<VerifiedCSharpSource> Read(string path, byte[] bytes) =>
        await VerifiedCSharpSource.ReadAsync(new CodeGraphSource(path, "sha256:" + Hex(bytes),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, false))));
    private static SyntaxTree Tree(VerifiedCSharpSource source, params string[] symbols) =>
        CSharpSyntaxTree.ParseText(source.Text, new CSharpParseOptions(LanguageVersion.Latest,
            DocumentationMode.Diagnose, SourceCodeKind.Regular, symbols), source.Path);
    private static CSharpCompilation Compile(string name, IEnumerable<SyntaxTree> trees, IEnumerable<MetadataReference> references) =>
        CSharpCompilation.Create(name, trees, references, Options);
    private static VerifiedCSharpCompilation Verify(string unit, CSharpCompilation compilation,
        IReadOnlyCollection<VerifiedCSharpSource> sources, IReadOnlyCollection<VerifiedMetadataReference> metadata,
        IReadOnlyCollection<VerifiedCSharpCompilation> dependencies) =>
        VerifiedCSharpCompilation.Create(Repo, unit, compilation, sources, metadata, dependencies);
    private static byte[] Emit(OutputKind kind = OutputKind.DynamicallyLinkedLibrary)
    {
        var compilation = CSharpCompilation.Create("Adversary", [CSharpSyntaxTree.ParseText(
            "public class First { public int M() => 1; } public class Later { public int N() => 2; }", path: "Adversary.cs")],
            [Platform.Value.Reference], Options.WithOutputKind(kind));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return stream.ToArray();
    }
    private static void WithFile(byte[] bytes, Action<string> action)
    {
        string directory = Path.Combine(Path.GetTempPath(), "Andirit-Q1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { var path = Path.Combine(directory, "image.dll"); File.WriteAllBytes(path, bytes); action(path); }
        finally { Directory.Delete(directory, true); }
    }
    private void Log(object value) => output.WriteLine(JsonSerializer.Serialize(value));
}
