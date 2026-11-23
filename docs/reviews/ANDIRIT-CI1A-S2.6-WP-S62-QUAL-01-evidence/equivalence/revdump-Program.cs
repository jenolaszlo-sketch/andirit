// Reviewer-owned equivalence dumper for ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01.
//
// Dumps a managed assembly as a canonical text form of its COMPILED PROGRAM:
//   * every type, field, method, property, event, with attributes, signature blob
//     and (for methods) the raw IL bytes and the local-signature blob;
//   * the public API surface (exported types + every public/protected member signature).
//
// The module MVID and the PE debug directory are deliberately EXCLUDED: a deterministic
// build derives both from the source hash, so they change for any source edit — including
// a documentation-only edit. A byte-identical dump therefore proves that the compiled
// program is identical: same types, same signatures, same IL.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

if (args.Length < 2 || args.Length > 3)
{
    Console.Error.WriteLine("usage: revdump <assembly.dll> <out-dump.txt> [--api]");
    return 2;
}

var path = args[0];
var output = args[1];
var apiOnly = args.Length == 3 && args[2] == "--api";

var lines = new List<string>();

using (var stream = File.OpenRead(path))
using (var pe = new PEReader(stream))
{
    var reader = pe.GetMetadataReader();

    var assembly = reader.GetAssemblyDefinition();
    lines.Add($"ASSEMBLY {reader.GetString(assembly.Name)} {assembly.Version} flags={assembly.Flags}");

    foreach (var handle in reader.TypeDefinitions)
    {
        var type = reader.GetTypeDefinition(handle);
        var ns = reader.GetString(type.Namespace);
        var name = reader.GetString(type.Name);
        var declaring = type.GetDeclaringType();
        var scope = declaring.IsNil ? ns : reader.GetString(reader.GetTypeDefinition(declaring).Namespace);
        lines.Add($"TYPE {scope}.{ns}.{name} attrs={type.Attributes} base={TypeName(reader, type.BaseType)}");
        lines.Add($"  INTERFACES {string.Join(",", type.GetInterfaceImplementations().Select(i => TypeName(reader, reader.GetInterfaceImplementation(i).Interface)))}");

        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            lines.Add($"  FIELD {reader.GetString(field.Name)} attrs={field.Attributes} sig={Hex(reader.GetBlobBytes(field.Signature))}");
        }

        foreach (var methodHandle in type.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            var il = Array.Empty<byte>();
            var locals = "";
            if (method.RelativeVirtualAddress != 0)
            {
                var body = pe.GetMethodBody(method.RelativeVirtualAddress);
                il = body.GetILBytes() ?? Array.Empty<byte>();
                if (!body.LocalSignature.IsNil)
                {
                    locals = Hex(reader.GetBlobBytes(reader.GetStandaloneSignature(body.LocalSignature).Signature));
                }
            }
            lines.Add($"  METHOD {reader.GetString(method.Name)} attrs={method.Attributes} impl={method.ImplAttributes} sig={Hex(reader.GetBlobBytes(method.Signature))} locals={locals} il={Hex(il)}");
        }

        foreach (var propertyHandle in type.GetProperties())
        {
            var property = reader.GetPropertyDefinition(propertyHandle);
            lines.Add($"  PROPERTY {reader.GetString(property.Name)} attrs={property.Attributes} sig={Hex(reader.GetBlobBytes(property.Signature))}");
        }

        foreach (var eventHandle in type.GetEvents())
        {
            var @event = reader.GetEventDefinition(eventHandle);
            lines.Add($"  EVENT {reader.GetString(@event.Name)} attrs={@event.Attributes} type={TypeName(reader, @event.Type)}");
        }
    }
}

if (apiOnly)
{
    var assembly = Assembly.LoadFrom(path);
    var api = new List<string>();
    foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
    {
        api.Add($"TYPE {type.FullName} attrs={type.Attributes}");
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var member in type.GetMembers(all).OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            if (member is MethodBase method && (method.IsPrivate || method.IsAssembly || method.IsFamilyAndAssembly))
                continue;
            if (member is FieldInfo field && !field.IsPublic && !field.IsFamily)
                continue;
            api.Add($"  {member.MemberType} {member}");
        }
    }
    lines.Add("### PUBLIC API SURFACE ###");
    lines.AddRange(api);
}

File.WriteAllLines(output, lines, Encoding.UTF8);
var bytes = File.ReadAllBytes(output);
Console.WriteLine($"{Path.GetFileName(path)}: {lines.Count} lines, sha256={Convert.ToHexString(SHA256.HashData(bytes))}");
return 0;

static string Hex(byte[] data) => data.Length == 0 ? "-" : Convert.ToHexString(data);
static string TypeName(MetadataReader reader, EntityHandle handle)
{
    if (handle.IsNil) return "(none)";
    switch (handle.Kind)
    {
        case HandleKind.TypeDefinition:
            var t = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            return $"{reader.GetString(t.Namespace)}.{reader.GetString(t.Name)}";
        case HandleKind.TypeReference:
            var r = reader.GetTypeReference((TypeReferenceHandle)handle);
            return $"{reader.GetString(r.Namespace)}.{reader.GetString(r.Name)}";
        case HandleKind.TypeSpecification:
            return "(typespec)";
        default:
            return handle.Kind.ToString();
    }
}