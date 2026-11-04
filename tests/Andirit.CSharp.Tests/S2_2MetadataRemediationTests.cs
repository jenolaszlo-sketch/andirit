using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;

namespace Andirit.CSharp.Tests;

public sealed class S2_2MetadataRemediationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("first-type-name")]
    [InlineData("later-type-name")]
    [InlineData("method-signature")]
    public void Q1F01_OriginalCounterexamplesRefuseBeforeIdentity(string damage)
    {
        // Exact Q1 emit options, source and FFFF mutation; independent probes stay unchanged.
        var compilation = CSharpCompilation.Create("Adversary", [CSharpSyntaxTree.ParseText(
            "public class First { public int M() => 1; } public class Later { public int N() => 2; }", path: "Adversary.cs")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true, concurrentBuild: false));
        using var stream = new MemoryStream();
        Assert.True(compilation.Emit(stream).Success);
        var image = stream.ToArray();
        int offset;
        using (var pe = new PEReader(new MemoryStream(image)))
        {
            var reader = pe.GetMetadataReader();
            Assert.True(reader.GetHeapSize(HeapIndex.String) < ushort.MaxValue);
            offset = pe.PEHeaders.MetadataStartOffset + (damage switch
            {
                "first-type-name" => reader.GetTableMetadataOffset(TableIndex.TypeDef) + 4,
                "later-type-name" => reader.GetTableMetadataOffset(TableIndex.TypeDef) + 2 * reader.GetTableRowSize(TableIndex.TypeDef) + 4,
                _ => reader.GetTableMetadataOffset(TableIndex.MethodDef) + 10
            });
        }
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(offset, 2), ushort.MaxValue);
        output.WriteLine($"{damage}: offset={offset}, patch=FFFF, sha256={Convert.ToHexStringLower(SHA256.HashData(image))}");
        Refuses(image);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReferencedNamesDereferenceBothStringFields(bool nameSpace)
    {
        var image = Image([0, 0, 1]);
        using (var pe = new PEReader(new MemoryStream(image)))
        {
            var reader = pe.GetMetadataReader();
            int offset = pe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.TypeRef) + (nameSpace ? 4 : 2);
            BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(offset, 2), ushort.MaxValue);
        }
        Refuses(image);
    }

    public static IEnumerable<object[]> InvalidSignatures()
    {
        yield return ["empty", Array.Empty<byte>()];
        yield return ["truncated-header", new byte[] { 0 }];
        yield return ["field-header", new byte[] { 6, 0, 1 }];
        yield return ["property-header", new byte[] { 8, 0, 1 }];
        yield return ["reserved-header", new byte[] { 0x80, 0, 1 }];
        yield return ["unmanaged-methoddef", new byte[] { 1, 0, 1 }];
        yield return ["explicit-this-methoddef", new byte[] { 0x40, 0, 1 }];
        yield return ["instance-static-mismatch", new byte[] { 0x20, 0, 1 }];
        yield return ["compressed-count-truncated", new byte[] { 0, 0x80 }];
        yield return ["missing-return", new byte[] { 0, 0 }];
        yield return ["missing-parameter", new byte[] { 0, 1, 1 }];
        yield return ["huge-count", new byte[] { 0, 0xdf, 0xff, 0xff, 0xff, 1 }];
        yield return ["bad-type", new byte[] { 0, 0, 0xff }];
        yield return ["trailing", new byte[] { 0, 0, 1, 8 }];
        yield return ["void-parameter", new byte[] { 0, 1, 1, 1 }];
        yield return ["void-array", new byte[] { 0, 0, 0x1d, 1 }];
        yield return ["nested-byref", new byte[] { 0, 0, 0x10, 0x10, 8 }];
        yield return ["pinned-return", new byte[] { 0, 0, 0x45, 8 }];
        yield return ["methoddef-sentinel", new byte[] { 5, 1, 1, 0x41, 8 }];
        yield return ["generic-zero", new byte[] { 0x10, 0, 0, 1 }];
        yield return ["generic-count-mismatch", new byte[] { 0x10, 1, 0, 1 }];
        yield return ["unbound-mvar", new byte[] { 0, 0, 0x1e, 0 }];
        yield return ["unbound-var", new byte[] { 0, 0, 0x13, 0 }];
        yield return ["nil-type-handle", new byte[] { 0, 0, 0x12, 0 }];
        yield return ["out-of-range-type-handle", new byte[] { 0, 0, 0x12, 0x7c }];
        yield return ["typespec-as-class", new byte[] { 0, 0, 0x12, 6 }];
        yield return ["array-rank-zero", new byte[] { 0, 0, 0x14, 8, 0, 0, 0 }];
        yield return ["array-size-count", new byte[] { 0, 0, 0x14, 8, 1, 2, 1, 1, 0 }];
        yield return ["genericinst-nonclass", new byte[] { 0, 0, 0x15, 8, 1, 8 }];
    }

    [Theory]
    [MemberData(nameof(InvalidSignatures))]
    public void MethodSignatureStructureRefuses(string name, byte[] signature)
    {
        output.WriteLine($"{name}: signature={Convert.ToHexString(signature)}");
        Refuses(Image(signature));
    }

    [Fact]
    public void MethodGenericAndParameterRelationshipsRefuse()
    {
        Refuses(Image([0, 0, 1], methodGeneric: true));
        Refuses(Image([0x10, 1, 0, 0x1e, 1], methodGeneric: true));
        Refuses(Image([0, 0, 0x13, 1], typeGeneric: true));
        Refuses(Image([0, 0, 1], parameterSequence: 1));
    }

    [Theory]
    [InlineData("a,b|c.d+<> 😀")]
    [InlineData("é")]
    [InlineData("e\u0301")]
    public void MetadataNamesUseMetadataRulesRatherThanCSharpIdentifierRules(string name) =>
        Captures(Image([0, 0, 1], typeName: name));

    [Fact]
    public void ValidGenericAndComplexSignaturesCapture()
    {
        Captures(Image([0x10, 1, 1, 0x1e, 0, 0x13, 0], methodGeneric: true, typeGeneric: true));
        Captures(Image([0, 0, 0x0f, 1])); // void*
        Captures(Image([0, 0, 0x14, 8, 2, 1, 3, 1, 0x7f])); // int32[ -1.., ]
        Captures(Image([0, 0, 0x1b, 0, 1, 1, 8])); // function pointer
        Captures(Image([0, 0, 0x1f, 5, 8])); // modreq(TypeRef row 1) int32
        Captures(Image([0, 0, 0x15, 0x12, 5, 1, 8])); // generic external class<int>
        // Iterative traversal must not impose an arbitrary legal nesting limit.
        Captures(Image(new byte[] { 0, 0 }.Concat(Enumerable.Repeat((byte)0x1d, 10000)).Append((byte)8).ToArray()));
    }

    [Fact]
    public void PlatformAndEmittedComplexCSharpStillCapture()
    {
        var captured = VerifiedMetadataReference.Capture(typeof(object).Assembly.Location);
        Assert.StartsWith("file|", captured.Identity);
        var compilation = CSharpCompilation.Create("Complex", [CSharpSyntaxTree.ParseText("""
            public unsafe class Outer<T> {
                public class Inner<U> {
                    public ref T M<V>(ref T t, U[,] array, V[] values, delegate* unmanaged<int, void> callback, int* p) => ref t;
                }
            }
            public class Other { public static int Varargs(__arglist) => 1; }
            """)], [captured.Reference], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Captures(stream.ToArray());
    }

    private static byte[] Image(byte[] signature, bool methodGeneric = false, bool typeGeneric = false,
        string typeName = "Type", int? parameterSequence = null)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("Fixture.dll"), metadata.GetOrAddGuid(Guid.Empty), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("Fixture"), new Version(1, 0), default, default, 0, AssemblyHashAlgorithm.None);
        var assembly = metadata.AddAssemblyReference(metadata.GetOrAddString("External"), new Version(1, 0), default, default, 0, default);
        metadata.AddTypeReference(assembly, metadata.GetOrAddString("Namespace"), metadata.GetOrAddString("Reference"));
        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        var type = metadata.AddTypeDefinition(TypeAttributes.Public, default, metadata.GetOrAddString(typeName), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        var method = metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static,
            MethodImplAttributes.Runtime, metadata.GetOrAddString("Method"), metadata.GetOrAddBlob(signature), 0,
            MetadataTokens.ParameterHandle(1));
        if (parameterSequence is int sequence) metadata.AddParameter(0, metadata.GetOrAddString("p"), sequence);
        if (methodGeneric) metadata.AddGenericParameter(method, 0, metadata.GetOrAddString("M"), 0);
        if (typeGeneric) metadata.AddGenericParameter(type, 0, metadata.GetOrAddString("T"), 0);
        var pe = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
        var image = new BlobBuilder();
        pe.Serialize(image);
        return image.ToArray();
    }

    private static void Refuses(byte[] image) => WithFile(image, path =>
    {
        VerifiedMetadataReference? capture = null;
        var error = Assert.Throws<InvalidDataException>(() => capture = VerifiedMetadataReference.Capture(path));
        Assert.Equal("UnsupportedMetadataReference", error.Message);
        Assert.Null(capture);
        Assert.Equal(error.Message, Assert.Throws<InvalidDataException>(() => VerifiedMetadataReference.Capture(path)).Message);
    });

    private static void Captures(byte[] image) => WithFile(image, path =>
        Assert.EndsWith("|" + Convert.ToHexStringLower(SHA256.HashData(image)), VerifiedMetadataReference.Capture(path).Identity));

    private static void WithFile(byte[] image, Action<string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Andirit-R1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { var path = Path.Combine(directory, "image.dll"); File.WriteAllBytes(path, image); action(path); }
        finally { Directory.Delete(directory, true); }
    }
}
