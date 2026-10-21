using System.Reflection;
using Penghou.Hetu;

namespace Penghou.Hetu.TreeSitter.Tests;

public sealed class TreeSitterPublicApiContractTests
{
    [Fact]
    public void TreeSitter_PublicTypeSnapshot_IsIntentional()
    {
        var actual = typeof(TreeSitterElixirPlugin).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "Penghou.Hetu.TreeSitter.HetuHostBuilderExtensions",
            "Penghou.Hetu.TreeSitterElixirPlugin",
            "Penghou.Hetu.TreeSitterJavaScriptPlugin",
            "Penghou.Hetu.TreeSitterTypeScriptPlugin",
            "Penghou.Hetu.TreeSitterGrammarDescriptor"
        }.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TreeSitter_PublicSurfaceDoesNotExposeNativeConcepts()
    {
        // The Tree-sitter runtime boundary stays inside this package:
        // TSNode, TSTree, parser handles, and binding types must never appear
        // in public signatures. Plugin names legitimately mention TreeSitter.
        var forbiddenTypeNames = new[]
        {
            "TSNode", "TSTree", "TSParser", "TSQuery", "TSTreeCursor", "TSPoint", "TSRange"
        };
        var bindingAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TreeSitter"
        };

        foreach (var type in typeof(TreeSitterElixirPlugin).Assembly.GetExportedTypes())
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (var memberType in MemberTypes(member))
                {
                    var name = memberType.IsGenericType
                        ? memberType.GetGenericTypeDefinition().Name
                        : memberType.Name;
                    Assert.DoesNotContain(
                        forbiddenTypeNames,
                        forbidden => string.Equals(name, forbidden, StringComparison.Ordinal));
                    if (memberType.Assembly.GetName().Name is { } assemblyName)
                    {
                        Assert.DoesNotContain(
                            bindingAssemblyNames,
                            binding => string.Equals(assemblyName, binding, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
        }
    }

    private static IEnumerable<Type> MemberTypes(MemberInfo member)
    {
        var roots = member switch
        {
            PropertyInfo property => new[] { property.PropertyType }.AsEnumerable(),
            FieldInfo field => new[] { field.FieldType }.AsEnumerable(),
            MethodInfo method => new[] { method.ReturnType }
                .Concat(method.GetParameters().Select(parameter => parameter.ParameterType)),
            ConstructorInfo constructor => constructor
                .GetParameters().Select(parameter => parameter.ParameterType),
            _ => []
        };

        foreach (var root in roots)
        {
            yield return root.IsGenericType ? root.GetGenericTypeDefinition() : root;
            if (root.IsGenericType)
            {
                foreach (var argument in root.GetGenericArguments())
                    yield return argument.IsGenericType ? argument.GetGenericTypeDefinition() : argument;
            }
        }
    }
}
