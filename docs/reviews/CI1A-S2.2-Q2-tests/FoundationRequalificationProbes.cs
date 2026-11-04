using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection;
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

namespace IndependentFoundationRequalification;

// Q2-only probes. Structural fixture acceptance never asserts semantic usability.
public sealed class FoundationRequalificationProbes(ITestOutputHelper output)
{
    private const string Repo = "review:s2.2-q2";
    private static readonly CSharpCompilationOptions Options = new(OutputKind.DynamicallyLinkedLibrary,
        deterministic: true, concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform = new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location));
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));

    [Theory]
    [InlineData("first-type-name", "14ede20fe8ae1585df478f8d3f6a0ebbf1c1501a0c501ebde2bf4828dc5799c7")]
    [InlineData("later-type-name", "584974319d734cfb6c452095975335898981ee6261d6bf809e44d2e135116097")]
    [InlineData("method-signature", "506edf234f78729295ce75c22e619211f3c7c745a9f1786d5f056700d9ccc6c7")]
    public void ExactHistoricalBytesNowRefuse(string name, string expected)
    {
        var bytes = File.ReadAllBytes(Path.Combine(Root, "docs/reviews/CI1A-S2.2-R1-evidence/inputs", name + ".bin"));
        Assert.Equal(expected, Hex(bytes));
        Refuses(name, bytes);
    }

    [Theory]
    [InlineData("typedef-name-empty", TableIndex.TypeDef, 4, 0)]
    [InlineData("typedef-namespace-oob", TableIndex.TypeDef, 6, 65535)]
    [InlineData("typeref-name-empty", TableIndex.TypeRef, 2, 0)]
    [InlineData("typeref-name-oob", TableIndex.TypeRef, 2, 65535)]
    [InlineData("typeref-namespace-oob", TableIndex.TypeRef, 4, 65535)]
    [InlineData("method-name-empty", TableIndex.MethodDef, 8, 0)]
    [InlineData("method-name-oob", TableIndex.MethodDef, 8, 65535)]
    [InlineData("method-blob-oob", TableIndex.MethodDef, 10, 65535)]
    [InlineData("generic-index-gap", TableIndex.GenericParam, 0, 1)]
    [InlineData("parameter-outside-signature", TableIndex.Param, 2, 2)]
    public void AdditionalHeapAndRelationshipRefusals(string name, TableIndex table, int field, int value)
    {
        var bytes = Fixture([0x10, 1, 1, 0x1e, 0, 8], generic: true, parameter: true);
        Patch(bytes, table, field, value);
        Refuses(name, bytes);
    }

    public static IEnumerable<object[]> Signatures()
    {
        yield return ["reserved-count-prefix", new byte[] {0, 0xe0, 0, 0, 0, 1}];
        yield return ["truncated-four-byte-count", new byte[] {0, 0xc0, 0, 0}];
        yield return ["modifier-without-token", new byte[] {0, 0, 0x1f}];
        yield return ["modifier-without-type", new byte[] {0, 0, 0x1f, 5}];
        yield return ["modifier-reserved-token", new byte[] {0, 0, 0x20, 7, 8}];
        yield return ["local-generic-arity-mismatch", new byte[] {0, 0, 0x15, 0x12, 8, 1, 8}];
        yield return ["array-truncated-lower-bound", new byte[] {0, 0, 0x14, 8, 1, 0, 1, 0x80}];
        yield return ["array-too-many-bounds", new byte[] {0, 0, 0x14, 8, 1, 0, 2, 0, 0}];
        yield return ["generic-arg-void", new byte[] {0, 0, 0x15, 0x12, 5, 1, 1}];
        yield return ["generic-arg-byref", new byte[] {0, 0, 0x15, 0x12, 5, 1, 0x10, 8}];
        yield return ["fnptr-reserved-convention", new byte[] {0, 0, 0x1b, 0x0b, 0, 1}];
        yield return ["fnptr-truncated-parameter", new byte[] {0, 0, 0x1b, 0, 1, 1}];
        yield return ["fnptr-double-sentinel", new byte[] {0, 0, 0x1b, 5, 2, 1, 0x41, 8, 0x41, 8}];
        yield return ["fnptr-default-sentinel", new byte[] {0, 0, 0x1b, 0, 1, 1, 0x41, 8}];
        yield return ["nested-array-unbound-var", new byte[] {0, 0, 0x1d, 0x1d, 0x13, 0}];
    }
    [Theory, MemberData(nameof(Signatures))]
    public void CompoundSignatureAdversaries(string name, byte[] signature) => Refuses(name, Fixture(signature));

    [Theory]
    [InlineData("名 前,|.+<>😀")]
    [InlineData("é")]
    [InlineData("e\u0301")]
    [InlineData("$generated:λ")]
    public void NonLanguageMetadataNamesRemainAccepted(string name) => Captures("legal-name:" + name, Fixture([0, 0, 1], name: name));

    [Fact]
    public void MinimalAssemblyAndOmittedParameterRowsRemainAccepted()
    {
        Captures("minimal-module-only", Fixture(null));
        Captures("omitted-param-rows", Fixture([0, 2, 1, 8, 0x1d, 0x0e]));
        Captures("return-param-row-zero", Fixture([0, 0, 8], parameter: true, sequence: 0));
        Captures("generic-return-and-array-param", Fixture([0x10, 1, 1, 0x1e, 0, 0x1d, 0x1e, 0], generic: true));
        Captures("two-modifiers-and-pointer", Fixture([0, 0, 0x20, 5, 0x1f, 5, 0x0f, 1]));
        Captures("fnptr-vararg", Fixture([0, 0, 0x1b, 5, 2, 1, 8, 0x41, 0x0e]));
    }

    [Fact]
    public void ExplicitlyExcludedStructuresCanStillCapture()
    {
        var field = Fixture([0, 0, 1], fieldBlob: new byte[] {0xff});
        Captures("excluded-malformed-field-signature", field);
        var scope = Fixture([0, 0, 1]);
        Patch(scope, TableIndex.TypeRef, 0, 0xfffc);
        Captures("excluded-invalid-typeref-resolution-scope", scope);
        var modifier = Fixture([0, 0, 0x1f, 6, 8], typeSpec: new byte[] {0xff});
        Captures("excluded-malformed-modifier-typespec-syntax", modifier);
        Log(new { kind = "qualification", conclusion = "Capture does not certify all tables, type resolution or complete metadata usability" });
    }

    [Fact]
    public void TruncationAndNonAssemblyBoundaries()
    {
        var image = Emit("public class A { public int M(int x) => x; }");
        using var pe = new PEReader(new MemoryStream(image));
        int end = pe.PEHeaders.MetadataStartOffset + pe.PEHeaders.MetadataSize;
        foreach (int length in new[] {0, 2, 128, pe.PEHeaders.MetadataStartOffset, end - 1})
            Refuses("truncated:" + length, image[..length]);
        Captures("truncated-after-complete-metadata:" + end, image[..end]);
        Refuses("netmodule", Emit("public class A {}", OutputKind.NetModule));
        Refuses("raw-metadata-without-pe", pe.GetMetadata().GetContent().ToArray());
    }

    [Fact]
    public void RuntimeInventoryAndComplexEmittedAssemblyRemainAccepted()
    {
        // Exercise the actual production inventory rather than infer it from the three-file corpus.
        var method = typeof(CSharpCodeGraphPlugin).GetMethod("BuildPlatformReferenceEvidence", BindingFlags.NonPublic | BindingFlags.Static)!;
        var captures = (IReadOnlyList<VerifiedMetadataReference>)method.Invoke(null, null)!;
        var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.Equal(tpa.Length, captures.Count);
        for (int i = 0; i < tpa.Length; i++) Assert.Equal("file|" + Path.GetFullPath(tpa[i]).Replace('\\', '/') + "|" + Hex(File.ReadAllBytes(tpa[i])), captures[i].Identity);
        Log(new { kind = "actual-production-inventory", count = captures.Count, identities = captures.Select(c => c.Identity) });
        Captures("complex-emitted", Emit("""
            public unsafe class Outer<T> where T : class {
                public class Inner<U> {
                    public ref V M<V>(ref V x, T[][] a, U[,,] b, delegate* unmanaged[Cdecl]<int, void> p) where V : unmanaged => ref x;
                    public delegate*<ref T, U*, void> Pointer;
                }
            }
            public class Other { public static void Varargs(__arglist) {} }
            """));
    }

    [Fact]
    public async Task IndependentMappingOrderingEquivalenceAndAuthorityChallenges()
    {
        async Task<VerifiedCSharpCompilation> Leaf(bool reverse = false, string symbols = "ONE")
        {
            var a = await Source("d\\A.cs", "public class A {}");
            var b = await Source("d/B.cs", "public class B {}");
            var sources = reverse ? new[] {b,a} : new[] {a,b};
            return Verify("Leaf.csproj", "Leaf", sources, [Platform.Value], [], symbols);
        }
        var first = await Leaf(); var second = await Leaf(true); var changed = await Leaf(symbols: "TWO");
        var s = await Source("Parent.cs", "public class Parent {}");
        var parent = Verify("Parent.csproj", "Parent", [s], [Platform.Value], [first]);
        var repeated = Verify("Parent.csproj", "Parent", [s], [Platform.Value], [second]);
        var transitive = Verify("Parent.csproj", "Parent", [s], [Platform.Value], [changed]);
        foreach (var v in new[] {first, second, changed, parent, repeated, transitive})
            Assert.Equal(Frozen.CompilationProvenance.Describe(Repo, v.Descriptor.ProjectUnitId, v.Compilation,
                v.Sources.Select(x => (x.Path, Bytes(x.Text.ToString()))).ToArray()).DescriptorDigest, Mapped(v));
        Assert.NotSame(first.Compilation, second.Compilation);
        Assert.Equal(first.Descriptor.DescriptorDigest, second.Descriptor.DescriptorDigest);
        Assert.Equal(Mapped(parent), Mapped(repeated)); Assert.Equal(Target(parent), Target(repeated));
        Assert.Equal(Mapped(parent), Mapped(transitive)); Assert.Equal(Target(parent), Target(transitive));
        Assert.NotEqual(parent.Descriptor.DescriptorDigest, transitive.Descriptor.DescriptorDigest);
        Assert.NotEqual(Mapped(first), Mapped(changed));
        // CodeGraphSource normalizes separators before verified construction.
        Assert.All(first.Compilation.SyntaxTrees, t => Assert.DoesNotContain('\\', t.FilePath));
        Assert.Equal(Mapped(parent), Mapped(parent, normalizeBeforeSort: true));
        var reordered = VerifiedCSharpCompilation.Create(Repo, "Parent.csproj", parent.Compilation.WithReferences(parent.Compilation.References.Reverse()), [s], [Platform.Value], [first]);
        Assert.Equal(Mapped(parent), Mapped(reordered));
        var foreign = VerifiedCSharpCompilation.Create("other-repository", "Leaf.csproj", first.Compilation, first.Sources, [Platform.Value], []);
        Assert.Equal("MissingDefiningCompilation", Assert.Throws<InvalidDataException>(() =>
            VerifiedCSharpCompilation.Create(Repo, "Parent.csproj", parent.Compilation, [s], [Platform.Value], [foreign])).Message);
        Assert.Equal("MissingDefiningCompilation", Assert.Throws<InvalidDataException>(() =>
            VerifiedCSharpCompilation.Create(Repo, "Parent.csproj", parent.Compilation, [s], [Platform.Value], [second])).Message);
        Assert.Equal("UnverifiedMetadataReference", Assert.Throws<InvalidDataException>(() =>
            VerifiedCSharpCompilation.Create(Repo, "Leaf.csproj", first.Compilation.WithReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)), first.Sources, [Platform.Value], [])).Message);
        var wrong = await Source("Parent.cs", "public class Different {}");
        Assert.StartsWith("RoslynTextMismatch:", Assert.Throws<InvalidDataException>(() =>
            VerifiedCSharpCompilation.Create(Repo, "Parent.csproj", parent.Compilation, [wrong], [Platform.Value], [first])).Message);
        var scope = VerifiedCSharpScope.Declare(Repo, new[] {first, parent}.Select(v => new CSharpScopeUnit(v.Descriptor.ProjectUnitId, v.Descriptor.ProjectUnitId, v.Sources.Select(x => x.Path).ToImmutableArray())));
        var complete = VerifiedCSharpUniverse.Create(Repo, scope, [first, parent]);
        Assert.True(complete.IsComplete);
        Assert.Equal("MissingDefiningCompilation", Assert.Throws<InvalidDataException>(() => complete.RequireCompilation(second.Compilation)).Message);
        var incomplete = VerifiedCSharpUniverse.Create(Repo, scope, [first]);
        Assert.False(incomplete.IsComplete);
        Assert.Equal("IncompleteCompilationUniverse", Assert.Throws<InvalidDataException>(() => incomplete.RequireCompilation(first.Compilation)).Message);
        var refusal = VerifiedCSharpUniverse.Create(Repo, scope, [first,parent], ["UnsupportedProfile"]);
        Assert.False(refusal.IsComplete);
        Assert.Equal("IncompleteCompilationUniverse", Assert.Throws<InvalidDataException>(() => refusal.RequireCompilation(parent.Compilation)).Message);
        string fabricated = "compctx1:sha256:" + new string('0', 64);
        var shape = CodeOccurrenceIdentity.ComputeTargetId(fabricated, "m", "n", "i", "d", "s");
        Assert.NotNull(new CodeOccurrenceTargetId(shape.Value));
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(parent.Descriptor.DescriptorDigest, "m", "n", "i", "d", "s"));
        Log(new {kind="mapping-authority", mapped=Mapped(parent), target=Target(parent), equivalent=Target(repeated), transitive=Target(transitive), provenance=parent.Descriptor.DescriptorDigest,
            changedProvenance=transitive.Descriptor.DescriptorDigest, wrongTreeOrder=Mapped(parent, true), fabricatedShape=shape.Value});
    }

    [Fact]
    public void FrozenLegacyRawPathOrderAndDuplicateProjectionArePreserved()
    {
        // Raw R2 inputs can contain paths the current verified source layer normalizes.
        var trees = new[] { CSharpSyntaxTree.ParseText("class A {}", path: "d\\A.cs"),
            CSharpSyntaxTree.ParseText("class B {}", path: "d/B.cs") };
        var dependency = CSharpCompilation.Create("Raw", trees, [Platform.Value.Reference], Options);
        var reference = dependency.ToMetadataReference();
        var parent = CSharpCompilation.Create("Parent", references: [reference, dependency.ToMetadataReference()], options: Options);
        var descriptor = Frozen.CompilationProvenance.Describe(Repo, "Parent.csproj", parent);
        string Item(bool normalizedSort) => "compilation|Raw|" + Metadata(dependency.Assembly.Identity) + "|" +
            string.Join(",", trees.OrderBy(t => normalizedSort ? t.FilePath.Replace('\\','/') : t.FilePath, StringComparer.Ordinal)
                .Select(t => t.FilePath.Replace('\\','/') + "=" + Hex(Encoding.UTF8.GetBytes(t.GetText().ToString()))));
        Assert.NotEqual(Item(false), Item(true));
        Assert.Equal(new[] { Item(false), Item(false) }, descriptor.References);
        Log(new { kind="frozen-raw-order", correct=Item(false), incorrect=Item(true), references=descriptor.References });
    }

    [Fact]
    public async Task EquivalentCapturedInstancesAndDistinctPathPreimages()
    {
        var source=await Source("A.cs","public class A {}");
        string dir=Path.Combine(Path.GetTempPath(),"Andirit-Q2-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string a=Path.Combine(dir,"a.dll"), b=Path.Combine(dir,"b.dll");
            var bytes=Fixture([0,0,1]); File.WriteAllBytes(a,bytes);File.WriteAllBytes(b,bytes);
            var x=VerifiedMetadataReference.Capture(a);var y=VerifiedMetadataReference.Capture(a);var z=VerifiedMetadataReference.Capture(b);
            Assert.NotSame(x.Reference,y.Reference);Assert.Equal(x.Identity,y.Identity);Assert.NotEqual(x.Identity,z.Identity);
            var first=Verify("A.csproj","A",[source],[x],[]);
            var repeat=Verify("A.csproj","A",[source],[y],[]);
            var different=Verify("A.csproj","A",[source],[z],[]);
            Assert.Equal(Mapped(first),Mapped(repeat));Assert.NotEqual(Mapped(first),Mapped(different));
            Assert.Equal(Target(first),Target(repeat));Assert.NotEqual(Target(first),Target(different));
            // Capture and verified provenance are not successful semantic compilation.
            Assert.Contains(first.Compilation.GetDiagnostics(),d=>d.Severity==DiagnosticSeverity.Error);
            Log(new {kind="reference-equivalence",sameBytes=Hex(bytes),samePathEqual=true,differentPathsDistinct=true,compilationErrors=true});
        }
        finally {Directory.Delete(dir,true);}
    }

    [Fact]
    public async Task FrozenProfilesReconstructFromAuthoritativeArtifacts()
    {
        var corpus = SemanticCorpusOracle.CorpusBuild.Build(Path.Combine(Root, "docs/planning/adr-ci1-01-ci1a-s2-0/corpus"), SemanticCorpusOracle.CorpusVariant.Baseline);
        var frozen = SemanticCorpusOracle.CorpusIdentity.Compute(corpus);
        var live = new List<VerifiedCSharpCompilation>();
        foreach (string unit in new[] {"Leaf/Leaf.csproj", "Core/Core.csproj"})
        {
            var original = corpus.Get(unit);
            var sources = new List<VerifiedCSharpSource>();
            foreach (var p in original.SourceBytes) sources.Add(await Read(p.Key, p.Value));
            var metadata = original.Compilation.References.OfType<PortableExecutableReference>().Select(r => VerifiedMetadataReference.Capture(r.FilePath!)).ToArray();
            var dependencies = original.Compilation.References.OfType<CompilationReference>().Select(r => live.Single(v => v.Compilation.AssemblyName == r.Compilation.AssemblyName)).ToArray();
            var compilation = original.Compilation.WithReferences(metadata.Select(m => (MetadataReference)m.Reference).Concat(dependencies.Select(d => d.Compilation.ToMetadataReference())));
            var v = VerifiedCSharpCompilation.Create(SemanticCorpusOracle.CorpusBuild.RepoId, unit, compilation, sources, metadata, dependencies);
            live.Add(v);
            Assert.Equal(Frozen.CompilationProvenance.Describe(SemanticCorpusOracle.CorpusBuild.RepoId, unit, compilation, original.SourceBytes.Select(p => (p.Key,p.Value)).ToArray()).DescriptorDigest, Mapped(v));
            Assert.NotEqual(frozen.Compctx1[unit], Mapped(v));
            Assert.Equal(frozen.Compctx1[unit], Mapped(v, overridePE: corpus.FrameworkIdentities));
            Log(new {kind="frozen-profile", unit, actual=Mapped(v), aligned=Mapped(v, overridePE: corpus.FrameworkIdentities), expected=frozen.Compctx1[unit], actualReferences=metadata.Select(m=>m.Identity), fixture=corpus.FrameworkIdentities});
        }
    }

    private static byte[] Fixture(byte[]? signature, string name = "Synthetic type|😀", bool generic = false, bool parameter = false,
        int sequence = 1, byte[]? fieldBlob = null, byte[]? typeSpec = null)
    {
        var m = new MetadataBuilder();
        m.AddModule(0, m.GetOrAddString("Q2.dll"), m.GetOrAddGuid(Guid.Empty), default, default);
        m.AddAssembly(m.GetOrAddString("Q2"), new Version(1,0), default, default, 0, AssemblyHashAlgorithm.None);
        m.AddTypeDefinition(TypeAttributes.NotPublic, default, m.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        if (signature is not null)
        {
            var a = m.AddAssemblyReference(m.GetOrAddString("External"), new Version(1,0), default, default, 0, default);
            m.AddTypeReference(a, m.GetOrAddString("N"), m.GetOrAddString("R"));
            m.AddTypeDefinition(TypeAttributes.Public, default, m.GetOrAddString(name), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
            var method = m.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.Runtime, m.GetOrAddString(name), m.GetOrAddBlob(signature), 0, MetadataTokens.ParameterHandle(1));
            if (generic) m.AddGenericParameter(method, 0, m.GetOrAddString("T"), 0);
            if (parameter) m.AddParameter(0, default, sequence);
            if (fieldBlob is not null) m.AddFieldDefinition(FieldAttributes.Public, m.GetOrAddString("F"), m.GetOrAddBlob(fieldBlob));
            if (typeSpec is not null) m.AddTypeSpecification(m.GetOrAddBlob(typeSpec));
        }
        var pe = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(m), new BlobBuilder());
        var result = new BlobBuilder(); pe.Serialize(result); return result.ToArray();
    }
    private static void Patch(byte[] bytes, TableIndex table, int field, int value)
    {
        using var pe = new PEReader(new MemoryStream(bytes)); var reader = pe.GetMetadataReader();
        int offset = pe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(table) + field;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset,2), (ushort)value);
    }
    private void Refuses(string name, byte[] bytes) => WithFile(bytes, path =>
    {
        for (int i=0; i<2; i++)
        {
            VerifiedMetadataReference? returned = null;
            var ex = Assert.Throws<InvalidDataException>(() => returned = VerifiedMetadataReference.Capture(path));
            Assert.Equal("UnsupportedMetadataReference", ex.Message); Assert.Null(returned);
            Log(new {kind="refused",name,sha256=Hex(bytes),bytes=bytes.Length,attempt=i,exception=ex.Message,inner=ex.InnerException?.GetType().Name});
        }
    });
    private void Captures(string name, byte[] bytes) => WithFile(bytes, path =>
    {
        var c = VerifiedMetadataReference.Capture(path);
        Assert.Equal("file|" + path.Replace('\\','/') + "|" + Hex(bytes), c.Identity);
        Assert.Equal(c.Identity, VerifiedMetadataReference.Capture(path).Identity);
        Log(new {kind="captured",name,sha256=Hex(bytes),bytes=bytes.Length});
    });
    private static void WithFile(byte[] bytes, Action<string> action)
    {
        // Optional reviewer-owned output; never writes into frozen/producer inputs.
        if (Environment.GetEnvironmentVariable("ANDIRIT_Q2_INPUTS") is {Length: > 0} evidence)
        {
            Directory.CreateDirectory(evidence);
            File.WriteAllBytes(Path.Combine(evidence, Hex(bytes)+".bin"),bytes);
        }
        string dir = Path.Combine(Path.GetTempPath(), "Andirit-Q2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {string p=Path.Combine(dir,"image.dll"); File.WriteAllBytes(p,bytes); action(p);}
        finally {Directory.Delete(dir,true);}
    }
    private static byte[] Emit(string text, OutputKind kind=OutputKind.DynamicallyLinkedLibrary)
    {
        var c=CSharpCompilation.Create("Q2Control", [CSharpSyntaxTree.ParseText(text)], [Platform.Value.Reference], Options.WithOutputKind(kind).WithAllowUnsafe(true));
        using var s=new MemoryStream(); var r=c.Emit(s); Assert.True(r.Success,string.Join("\n",r.Diagnostics)); return s.ToArray();
    }
    private static byte[] Bytes(string text) => new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
    private static Task<VerifiedCSharpSource> Source(string path,string text) => Read(path,Bytes(text));
    private static async Task<VerifiedCSharpSource> Read(string path,byte[] bytes) => await VerifiedCSharpSource.ReadAsync(new CodeGraphSource(path,"sha256:"+Hex(bytes),_=>ValueTask.FromResult<Stream>(new MemoryStream(bytes))));
    private static VerifiedCSharpCompilation Verify(string unit,string name,IReadOnlyCollection<VerifiedCSharpSource> sources,IReadOnlyCollection<VerifiedMetadataReference> refs,IReadOnlyCollection<VerifiedCSharpCompilation> deps,string symbol="ONE") =>
        VerifiedCSharpCompilation.Create(Repo,unit,CSharpCompilation.Create(name,sources.Select(s=>CSharpSyntaxTree.ParseText(s.Text,new CSharpParseOptions(LanguageVersion.Latest,DocumentationMode.Diagnose,preprocessorSymbols:[symbol]),s.Path)),refs.Select(r=>(MetadataReference)r.Reference).Concat(deps.Select(d=>d.Compilation.ToMetadataReference())),Options),sources,refs,deps);
    // A fresh reviewer encoder, compared with frozen Describe over the exact same inputs.
    private static string Mapped(VerifiedCSharpCompilation v,bool normalizeBeforeSort=false,IEnumerable<string>? overridePE=null)
    {
        var d=v.Descriptor;
        var projects=v.Compilation.References.OfType<CompilationReference>().Select(r=>"compilation|"+r.Compilation.AssemblyName+"|"+Metadata(r.Compilation.Assembly.Identity)+"|"+string.Join(",",r.Compilation.SyntaxTrees.OrderBy(t=>normalizeBeforeSort?t.FilePath.Replace('\\','/'):t.FilePath,StringComparer.Ordinal).Select(t=>t.FilePath.Replace('\\','/')+"="+Hex(Encoding.UTF8.GetBytes(t.GetText().ToString())))));
        var refs=(overridePE??d.References.Where(r=>!r.StartsWith("compilation-context|",StringComparison.Ordinal))).Concat(projects).Order(StringComparer.Ordinal);
        return "compctx1:sha256:"+Hash("compctx1-context",new[]{"compctx1",d.RepositoryId,d.ProjectUnitId,d.AssemblyName,d.MetadataIdentity,d.ModuleName,d.OptionsDigest}.Concat(d.Sources.Select(s=>s.CanonicalForm)).Concat(refs));
    }
    private static string Target(VerifiedCSharpCompilation v)=>"asmctx3:sha256:"+Hash("asmctx3-context",["compctx1",Mapped(v),v.Descriptor.MetadataIdentity,v.Descriptor.ModuleName,"i","d","s"]);
    private static string Metadata(AssemblyIdentity a)=>string.Join("|",a.Name,a.Version.ToString(),a.CultureName??"",Convert.ToHexStringLower(a.PublicKeyToken.AsSpan()));
    private static string Hash(string domain,IEnumerable<string> parts)
    {
        using var stream=new MemoryStream();
        foreach(var p in new[]{domain}.Concat(parts)) {var bytes=Encoding.UTF8.GetBytes(p); var n=new byte[4]; BinaryPrimitives.WriteInt32BigEndian(n,bytes.Length);stream.Write(n);stream.Write(bytes);}
        return Hex(stream.ToArray());
    }
    private static string Hex(byte[] b)=>Convert.ToHexStringLower(SHA256.HashData(b));
    private void Log(object v)=>output.WriteLine(JsonSerializer.Serialize(v));
}
