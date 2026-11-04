using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Exact independently demonstrated Q1 source, options, heap-index mutations.
var destination = Path.GetFullPath(args[0]);
Directory.CreateDirectory(destination);
var compilation = CSharpCompilation.Create("Adversary", [CSharpSyntaxTree.ParseText(
    "public class First { public int M() => 1; } public class Later { public int N() => 2; }", path: "Adversary.cs")],
    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true, concurrentBuild: false));
using var stream = new MemoryStream();
var result = compilation.Emit(stream);
if (!result.Success) throw new Exception(string.Join("\n", result.Diagnostics));
var valid = stream.ToArray();
var receipts = new List<object>();
foreach (var damage in new[] { "first-type-name", "later-type-name", "method-signature" })
{
    var image = valid.ToArray();
    int offset;
    using (var pe = new PEReader(new MemoryStream(image)))
    {
        var reader = pe.GetMetadataReader();
        if (reader.GetHeapSize(HeapIndex.String) >= ushort.MaxValue) throw new Exception("Unexpected heap width");
        offset = pe.PEHeaders.MetadataStartOffset + (damage switch
        {
            "first-type-name" => reader.GetTableMetadataOffset(TableIndex.TypeDef) + 4,
            "later-type-name" => reader.GetTableMetadataOffset(TableIndex.TypeDef) + 2 * reader.GetTableRowSize(TableIndex.TypeDef) + 4,
            _ => reader.GetTableMetadataOffset(TableIndex.MethodDef) + 10
        });
    }
    BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(offset, 2), ushort.MaxValue);
    File.WriteAllBytes(Path.Combine(destination, damage + ".bin"), image);
    receipts.Add(new { damage, offset, patch = "FFFF", bytes = image.Length, sha256 = Convert.ToHexStringLower(SHA256.HashData(image)) });
}
File.WriteAllText(Path.Combine(destination, "inputs.json"), JsonSerializer.Serialize(receipts, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine(File.ReadAllText(Path.Combine(destination, "inputs.json")));
