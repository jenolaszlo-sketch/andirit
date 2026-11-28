using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit.CSharp.Tests;

/// <summary>
/// Independent implementation of the successor occurrence identity contract
/// (<c>refctx1</c> reference identity, <c>compctx4</c> defining context,
/// <c>asmctx4</c> target and <c>usage5</c> occurrence). It reads the live
/// compilation inputs directly and never calls the production identity helpers
/// or the historical frozen path-bound authority. It is the expected-value
/// oracle for the transition tests and the C4 ledger.
/// </summary>
internal static class SuccessorIdentityOracle
{
    internal const string ReferenceDomain = "refctx1";
    internal const string ProductionContext = "compctx3";
    internal const string DefiningContext = "compctx4";
    internal const string TargetDomain = "asmctx4";
    internal const string UsageDomain = "usage5";
    internal const string SemanticsVersion = "usage5-b2c-exp-r2-20261128";

    internal static string DescribeDefiningContext(string repositoryId, VerifiedCSharpCompilation unit)
    {
        var parseByPath = unit.Compilation.SyntaxTrees.ToDictionary(
            tree => tree.FilePath.Replace('\\', '/'), tree => tree, StringComparer.Ordinal);
        var sources = unit.Sources
            .Select(source =>
            {
                var path = source.Path.Replace('\\', '/');
                var tree = parseByPath[path];
                var parse = (CSharpParseOptions)tree.Options;
                var symbols = string.Join(",", parse.PreprocessorSymbolNames.Order(StringComparer.Ordinal));
                return string.Join("|", path, source.ContentHash, parse.LanguageVersion.ToString(), symbols);
            })
            .Order(StringComparer.Ordinal)
            .ToArray();
        var references = unit.Compilation.References
            .Select(ReferenceIdentity)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var parts = new List<string>
        {
            DefiningContext,
            repositoryId,
            unit.Descriptor.ProjectUnitId,
            unit.Compilation.AssemblyName!,
            Metadata(unit.Compilation.Assembly.Identity),
            unit.Compilation.Assembly.Modules.First().Name,
            OptionsDigest(unit.Compilation.Options),
        };
        parts.AddRange(sources);
        parts.AddRange(references);
        return DefiningContext + ":sha256:" + Hash(DefiningContext + "-context", parts);
    }

    internal static string TargetId(
        string repositoryId, VerifiedCSharpCompilation defining, IMethodSymbol method,
        Func<IMethodSymbol, string> identifier, Func<IMethodSymbol, string> declaration,
        Func<IMethodSymbol, string> shape)
    {
        var context = DescribeDefiningContext(repositoryId, defining);
        return TargetIdFromContext(context, defining, method, identifier, declaration, shape);
    }

    internal static string TargetIdFromContext(
        string context, VerifiedCSharpCompilation defining, IMethodSymbol method,
        Func<IMethodSymbol, string> identifier, Func<IMethodSymbol, string> declaration,
        Func<IMethodSymbol, string> shape)
    {
        return TargetDomain + ":sha256:" + Hash(TargetDomain + "-context",
        [
            DefiningContext,
            context,
            Metadata(defining.Compilation.Assembly.Identity),
            defining.Compilation.Assembly.Modules.First().Name,
            identifier(method),
            declaration(method),
            shape(method),
        ]);
    }

    internal static string IdentifierDigest(IMethodSymbol method) =>
        Hash(TargetDomain + "-identifier", [method.Name]);

    internal static string DeclarationDigest(IMethodSymbol method) =>
        Hash(TargetDomain + "-declarations", [CanonicalSymbolKey(Normalize(method))]);

    internal static string ShapeDigest(IMethodSymbol method)
    {
        var normalized = (IMethodSymbol)Normalize(method);
        return Hash(TargetDomain + "-target",
            ["method|" + normalized.Arity + "|" +
             string.Join(",", normalized.Parameters.Select(parameter =>
                 parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))]);
    }

    internal static string TargetDocumentId(IMethodSymbol method) =>
        method.GetDocumentationCommentId() ??
        method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    internal static ISymbol Normalize(ISymbol symbol) =>
        symbol is IMethodSymbol method
            ? method.ReducedFrom ?? method.OriginalDefinition
            : symbol.OriginalDefinition;

    internal static string CanonicalSymbolKey(ISymbol symbol)
    {
        var documentationId = symbol.GetDocumentationCommentId();
        if (documentationId is not null)
            return documentationId;
        if (symbol is IParameterSymbol parameter)
            return $"P:{CanonicalSymbolKey(parameter.ContainingSymbol)}:{parameter.Ordinal}:{parameter.Name}";
        return $"{symbol.Kind}:{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";
    }

    internal static string UsageId(
        string repositoryId, string pluginId, string sourceUnitId, string sourcePath,
        string sourceHash, CodeLocation span, string targetDocumentId, string targetId)
    {
        return Hash(UsageDomain,
        [
            repositoryId,
            pluginId,
            SemanticsVersion,
            sourceUnitId,
            sourcePath,
            sourceHash,
            span.StartLine.ToString(),
            span.StartColumn.ToString(),
            span.EndLine.ToString(),
            span.EndColumn.ToString(),
            targetDocumentId,
            targetId,
            "method-invocation",
        ]);
    }

    internal static string Hash(string domain, IEnumerable<string> parts)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Append(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Span<byte> length = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        Append(domain);
        foreach (var part in parts) Append(part);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    internal static string DescribeReferenceIdentity(string path) => ContentBoundIdentity(path);

    private static string ReferenceIdentity(MetadataReference reference) => reference switch
    {
        CompilationReference compilation => CompilationIdentity(compilation.Compilation),
        PortableExecutableReference executable when executable.FilePath is { Length: > 0 } path && File.Exists(path) =>
            ContentBoundIdentity(path),
        _ => throw new InvalidOperationException("Unsupported reference shape for the independent oracle: " + reference.Display),
    };

    private static string ContentBoundIdentity(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var stream = new MemoryStream(bytes, writable: false);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var definition = reader.GetAssemblyDefinition();
        var publicKeyToken = definition.PublicKey.IsNil
            ? string.Empty
            : Convert.ToHexStringLower(PublicKeyToken(reader.GetBlobBytes(definition.PublicKey)));
        var metadata = string.Join("|",
            reader.GetString(definition.Name),
            definition.Version.ToString(),
            definition.Culture.IsNil ? string.Empty : reader.GetString(definition.Culture),
            publicKeyToken);
        return ReferenceDomain + "|" + metadata + "|sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private static string CompilationIdentity(Compilation compilation)
    {
        var trees = string.Join(",", compilation.SyntaxTrees
            .OrderBy(tree => tree.FilePath, StringComparer.Ordinal)
            .Select(tree => tree.FilePath.Replace('\\', '/') + "=" +
                Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tree.GetText().ToString())))));
        return "compilation|" + compilation.AssemblyName + "|" +
            Metadata(compilation.Assembly.Identity) + "|" + trees;
    }

    private static byte[] PublicKeyToken(byte[] publicKey)
    {
        var hash = SHA1.HashData(publicKey);
        var token = hash.AsSpan(hash.Length - 8).ToArray();
        Array.Reverse(token);
        return token;
    }

    internal static string Metadata(AssemblyIdentity identity)
    {
        var token = identity.PublicKeyToken.IsDefaultOrEmpty
            ? string.Empty
            : Convert.ToHexStringLower(identity.PublicKeyToken.AsSpan());
        return string.Join("|", identity.Name, identity.Version.ToString(),
            identity.CultureName ?? string.Empty, token);
    }

    private static string OptionsDigest(CompilationOptions options)
    {
        var specific = options.SpecificDiagnosticOptions is { } map
            ? string.Join(",", map.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + "=" + pair.Value))
            : string.Empty;
        return Hash(ProductionContext + "-options",
        [
            options.OutputKind.ToString(),
            options.NullableContextOptions.ToString(),
            (options as CSharpCompilationOptions)?.AllowUnsafe.ToString() ?? string.Empty,
            options.OptimizationLevel.ToString(),
            options.Platform.ToString(),
            options.CheckOverflow.ToString(),
            options.Deterministic.ToString(),
            options.WarningLevel.ToString(System.Globalization.CultureInfo.InvariantCulture),
            options.GeneralDiagnosticOption.ToString(),
            specific,
        ]);
    }
}


