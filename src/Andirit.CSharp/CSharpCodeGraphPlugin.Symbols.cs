using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Andirit;

public sealed partial class CSharpCodeGraphPlugin
{
    private static ISymbol Normalize(ISymbol symbol) =>
        symbol is IMethodSymbol method
            ? method.ReducedFrom ?? method.OriginalDefinition
            : symbol.OriginalDefinition;

    private static ISymbol? GetSupportedSymbol(
    SyntaxNode syntax,
    SemanticModel model,
    CancellationToken cancellationToken) => syntax switch
    {
        BaseNamespaceDeclarationSyntax value => model.GetDeclaredSymbol(value, cancellationToken),
        BaseTypeDeclarationSyntax value => model.GetDeclaredSymbol(value, cancellationToken),
        DelegateDeclarationSyntax value => model.GetDeclaredSymbol(value, cancellationToken),
        BaseMethodDeclarationSyntax value => model.GetDeclaredSymbol(value, cancellationToken),
        LocalFunctionStatementSyntax value => model.GetDeclaredSymbol(value, cancellationToken),
        BasePropertyDeclarationSyntax value => model.GetDeclaredSymbol(value, cancellationToken),
        VariableDeclaratorSyntax value when value.Parent?.Parent is FieldDeclarationSyntax =>
            model.GetDeclaredSymbol(value, cancellationToken),
        ParameterSyntax value when HasSupportedParameterOwner(value) =>
            model.GetDeclaredSymbol(value, cancellationToken),
        _ => null
    };

    private static bool HasSupportedParameterOwner(ParameterSyntax parameter) =>
        parameter.Parent?.Parent is BaseMethodDeclarationSyntax or
            LocalFunctionStatementSyntax or DelegateDeclarationSyntax or
            BasePropertyDeclarationSyntax;

    private static CodeNodeKind? GetNodeKind(ISymbol symbol) => symbol switch
    {
        INamespaceSymbol => CodeNodeKinds.Namespace,
        INamedTypeSymbol named when named.TypeKind == TypeKind.Interface => CodeNodeKinds.Interface,
        INamedTypeSymbol => CodeNodeKinds.Type,
        IMethodSymbol => CodeNodeKinds.Callable,
        IPropertySymbol => CodeNodeKinds.Property,
        IFieldSymbol => CodeNodeKinds.Field,
        IParameterSymbol => CodeNodeKinds.Parameter,
        _ => null
    };

    private static string DisplayName(ISymbol symbol) => symbol is IMethodSymbol
    {
        MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor
    } method
        ? method.ContainingType.Name
        : symbol.Name;

    private static string QualifiedName(ISymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    private static string CanonicalSymbolKey(ISymbol symbol)
    {
        var documentationId = symbol.GetDocumentationCommentId();
        if (documentationId is not null)
            return documentationId;
        if (symbol is IParameterSymbol parameter)
        {
            return $"P:{CanonicalSymbolKey(parameter.ContainingSymbol)}:{parameter.Ordinal}:{parameter.Name}";
        }

        return $"{symbol.Kind}:{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";
    }

    /// <summary>
    /// Builds a definition-level fingerprint independent of the compilation that
    /// produced the symbol. Documentation IDs are lookup keys only; this
    /// fingerprint proves that a bound target and an indexed declaration describe
    /// the same definition after generics/reduced extension methods normalize.
    /// </summary>
    private static string DefinitionFingerprint(ISymbol symbol)
    {
        var normalized = Normalize(symbol);
        var builder = new StringBuilder();
        builder.Append(normalized.Kind).Append('\n');
        switch (normalized)
        {
            case IMethodSymbol method:
                builder.Append(method.MethodKind).Append('\n')
                    .Append(method.MetadataName).Append('\n')
                    .Append(method.Arity).Append('\n');
                foreach (var parameter in method.Parameters)
                    builder.Append(TypeIdentity(parameter.Type)).Append(',');
                builder.Append('\n').Append(TypeIdentity(method.ReturnType));
                break;
            case INamedTypeSymbol type:
                builder.Append(type.TypeKind).Append('\n')
                    .Append(type.MetadataName).Append('\n')
                    .Append(type.Arity);
                break;
            case IPropertySymbol property:
                builder.Append(property.MetadataName).Append('\n')
                    .Append(TypeIdentity(property.Type));
                break;
            case IFieldSymbol field:
                builder.Append(field.MetadataName).Append('\n')
                    .Append(TypeIdentity(field.Type));
                break;
            case IParameterSymbol parameter:
                builder.Append(parameter.Ordinal).Append('\n')
                    .Append(TypeIdentity(parameter.Type));
                break;
            case INamespaceSymbol @namespace:
                builder.Append(@namespace.ToDisplayString());
                break;
        }

        return builder.ToString();
    }

    private static string TypeIdentity(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static CodeNodeId NodeId(string category, string canonical) =>
        new($"csharp:{category}:{Hash(canonical)}");

    private static CodeNodeId ProjectNodeId(string projectPath) =>
        NodeId("project", projectPath);

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
