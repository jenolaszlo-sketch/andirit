using System.Reflection;
using System.Text;

// Dumps the public API surface of the given assemblies (types, members, signatures)
// so that the baseline and the implementation revision can be diffed byte-for-byte.
if (args.Length == 0)
{
    Console.Error.WriteLine("usage: apidump <assembly> [<assembly> ...]");
    return 2;
}

var lines = new List<string>();
foreach (var path in args)
{
    var assembly = Assembly.LoadFrom(Path.GetFullPath(path));
    lines.Add($"ASSEMBLY {assembly.GetName().Name} {assembly.GetName().Version}");

    foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
    {
        lines.Add($"TYPE {type.FullName} : {type.BaseType?.FullName} [{string.Join(",", type.GetInterfaces().Select(i => i.FullName).Order(StringComparer.Ordinal))}]");

        var members = type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(Describe)
            .Order(StringComparer.Ordinal);
        lines.AddRange(members.Select(member => $"  MEMBER {type.FullName}.{member}"));
    }
}

var text = string.Join("\n", lines) + "\n";
var output = Environment.GetEnvironmentVariable("APIDUMP_OUT");
if (output is null)
{
    Console.Write(text);
}
else
{
    File.WriteAllText(output, text, new UTF8Encoding(false));
    Console.WriteLine($"wrote {output} ({lines.Count} lines)");
}

return 0;

static string Describe(MemberInfo member)
{
    switch (member)
    {
        case MethodInfo method:
            return $"{method.Name}({string.Join(",", method.GetParameters().Select(DescribeParameter))}) : {method.ReturnType.FullName} [{method.Attributes}]";
        case ConstructorInfo constructor:
            return $".ctor({string.Join(",", constructor.GetParameters().Select(DescribeParameter))}) [{constructor.Attributes}]";
        case PropertyInfo property:
            return $"{property.Name} : {property.PropertyType.FullName} get={property.GetMethod is not null} set={property.SetMethod is not null}";
        case FieldInfo field:
            return $"{field.Name} : {field.FieldType.FullName} [{field.Attributes}]";
        case EventInfo @event:
            return $"{@event.Name} : {@event.EventHandlerType?.FullName}";
        case Type nested:
            return $"NESTED {nested.FullName}";
        default:
            return member.Name;
    }
}

static string DescribeParameter(ParameterInfo parameter)
{
    return $"{parameter.ParameterType.FullName} {parameter.Name} default={parameter.HasDefaultValue}";
}
