using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

// Semantic comparison of two managed assemblies: every type, field, method, property
// and event is dumped with its attributes, signature and (for methods) its IL bytes.
// The module MVID and the PE debug directory are deliberately excluded, because a
// deterministic build derives them from the source hash and they therefore change for
// any source edit - including a documentation-only edit.
//
// A byte-identical dump proves that the change is documentation-only at the level of
// the compiled program: identical types, identical signatures, identical IL.

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: dllcompare <baseline.dll> <implementation.dll>");
    return 2;
}

static List<string> Dump(string path)
{
    var lines = new List<string>();
    using var stream = File.OpenRead(path);
    using var pe = new PEReader(stream);
    var reader = pe.GetMetadataReader();

    var assembly = reader.GetAssemblyDefinition();
    lines.Add($"ASSEMBLY {reader.GetString(assembly.Name)} {assembly.Version} flags={assembly.Flags}");

    foreach (var handle in reader.TypeDefinitions)
    {
        var type = reader.GetTypeDefinition(handle);
        var ns = reader.GetString(type.Namespace);
        var name = reader.GetString(type.Name);
        var declaring = type.GetDeclaringType();
        var scope = declaring.IsNil
            ? reader.GetString(type.Namespace)
            : reader.GetString(reader.GetTypeDefinition(declaring).Namespace);
        lines.Add($"TYPE {scope}.{ns}.{name} attrs={type.Attributes} base={TypeName(reader, type.BaseType)}");
        lines.Add($"  TYPE-INTERFACES {string.Join(",", type.GetInterfaceImplementations().Select(i => TypeName(reader, reader.GetInterfaceImplementation(i).Interface)))}");

        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            lines.Add($"  FIELD {reader.GetString(field.Name)} attrs={field.Attributes} sig={Convert.ToHexString(reader.GetBlobBytes(field.Signature))}");
        }

        foreach (var methodHandle in type.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            var il = Array.Empty<byte>();
            if (method.RelativeVirtualAddress != 0)
            {
                var body = pe.GetMethodBody(method.RelativeVirtualAddress);
                il = body.GetILBytes() ?? Array.Empty<byte>();
            }
            var locals = string.Join(",", bodyLocals(reader, pe, method));
            lines.Add($"  METHOD {reader.GetString(method.Name)} attrs={method.Attributes} impl={method.ImplAttributes} sig={Convert.ToHexString(reader.GetBlobBytes(method.Signature))} locals=[{locals}] il={Convert.ToHexString(il)}");
        }

        foreach (var propertyHandle in type.GetProperties())
        {
            var property = reader.GetPropertyDefinition(propertyHandle);
            lines.Add($"  PROPERTY {reader.GetString(property.Name)} attrs={property.Attributes} sig={Convert.ToHexString(reader.GetBlobBytes(property.Signature))}");
        }

        foreach (var eventHandle in type.GetEvents())
        {
            var @event = reader.GetEventDefinition(eventHandle);
            lines.Add($"  EVENT {reader.GetString(@event.Name)} attrs={@event.Attributes} type={TypeName(reader, @event.Type)}");
        }
    }

    return lines;
}

static IEnumerable<string> bodyLocals(MetadataReader reader, PEReader pe, MethodDefinition method)
{
    if (method.RelativeVirtualAddress == 0)
    {
        yield break;
    }

    var body = pe.GetMethodBody(method.RelativeVirtualAddress);
    if (!body.LocalSignature.IsNil)
    {
        yield return Convert.ToHexString(reader.GetBlobBytes(reader.GetStandaloneSignature(body.LocalSignature).Signature));
    }
}

static string TypeName(MetadataReader reader, EntityHandle handle)
{
    if (handle.IsNil)
    {
        return "(none)";
    }

    switch (handle.Kind)
    {
        case HandleKind.TypeDefinition:
        {
            var type = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            return $"{reader.GetString(type.Namespace)}.{reader.GetString(type.Name)}";
        }
        case HandleKind.TypeReference:
        {
            var type = reader.GetTypeReference((TypeReferenceHandle)handle);
            return $"{reader.GetString(type.Namespace)}.{reader.GetString(type.Name)}";
        }
        case HandleKind.TypeSpecification:
            return "(typespec)";
        default:
            return handle.Kind.ToString();
    }
}

var baseline = Dump(args[0]);
var implementation = Dump(args[1]);

var baselineText = string.Join("\n", baseline);
var implementationText = string.Join("\n", implementation);

Console.WriteLine("ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 - managed assembly semantic comparison");
Console.WriteLine($"baseline:       {args[0]}");
Console.WriteLine($"implementation: {args[1]}");
Console.WriteLine();
Console.WriteLine("method: System.Reflection.Metadata dump of every type, field, method, property");
Console.WriteLine("        and event with its attributes, signature blob and IL bytes. The module");
Console.WriteLine("        MVID and the PE debug directory are excluded: a deterministic build");
Console.WriteLine("        derives them from the source hash, so they change for any source edit.");
Console.WriteLine();
Console.WriteLine($"baseline lines:       {baseline.Count}");
Console.WriteLine($"implementation lines: {implementation.Count}");
Console.WriteLine($"baseline sha256:      {Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(baselineText)))}");
Console.WriteLine($"implementation sha256: {Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(implementationText)))}");
Console.WriteLine();

var equal = baseline.SequenceEqual(implementation);
Console.WriteLine($"semantic-dump-identical: {equal}");
if (!equal)
{
    Console.WriteLine();
    var count = Math.Max(baseline.Count, implementation.Count);
    var shown = 0;
    for (var index = 0; index < count && shown < 40; index++)
    {
        var b = index < baseline.Count ? baseline[index] : "(missing)";
        var i = index < implementation.Count ? implementation[index] : "(missing)";
        if (b == i)
        {
            continue;
        }

        Console.WriteLine($"DIFF[{index}] baseline:       {b}");
        Console.WriteLine($"DIFF[{index}] implementation: {i}");
        shown++;
    }
}

Console.WriteLine();
Console.WriteLine(equal
    ? "RESULT: the compiled program is IDENTICAL - same types, same signatures, same IL."
    + " The change is documentation-only."
    : "RESULT: the compiled program DIFFERS - the change is not documentation-only.");

return equal ? 0 : 1;
