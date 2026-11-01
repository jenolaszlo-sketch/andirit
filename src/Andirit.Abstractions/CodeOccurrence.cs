using System.Security.Cryptography;
using System.Text;

namespace Andirit;

/// <summary>
/// The supported occurrence category for CI-1A Stage 1. The set is intentionally
/// closed: unsupported semantic categories are reported as coverage, never as
/// facts. This is not the full Find Usages feature.
/// </summary>
public enum CodeOccurrenceCategory
{
    /// <summary>An ordinary invocation of a bound method target.</summary>
    Invocation = 0
}

/// <summary>Identifies one usage-occurrence fact (the accepted `usage4` shape).</summary>
public sealed record CodeOccurrenceId
{
    public CodeOccurrenceId(string value) =>
        Value = ContractValue.Identifier(value, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>Identifies the exact bound declaration target (the accepted `asmctx3` shape).</summary>
public sealed record CodeOccurrenceTargetId
{
    public CodeOccurrenceTargetId(string value) =>
        Value = ContractValue.Identifier(value, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>One `(plugin, unit)` that contributed an identical occurrence fact.</summary>
public sealed record CodeOccurrenceContributor
{
    public CodeOccurrenceContributor(CodePluginId pluginId, string indexUnitId)
    {
        PluginId = pluginId ?? throw new ArgumentNullException(nameof(pluginId));
        IndexUnitId = ContractValue.Identifier(indexUnitId, nameof(indexUnitId));
    }

    public CodePluginId PluginId { get; }
    public string IndexUnitId { get; }
}

/// <summary>
/// Domain-separated, deterministic, length-prefixed identity construction for
/// occurrence facts. No wall clock, process id, object address, run id or
/// enumeration order participates. This preserves the accepted `usage4` /
/// `asmctx3` semantic shape; it is not a documentation-id, path or descriptor
/// shortcut.
/// </summary>
public static class CodeOccurrenceIdentity
{
    private const string TargetDomain = "andirit-asmctx3";
    private const string UsageDomain = "andirit-usage4";
    private const string PayloadDomain = "andirit-occurrence-payload4";

    /// <summary>Computes the exact target identity from the accepted target inputs.</summary>
    public static CodeOccurrenceTargetId ComputeTargetId(
        CodeRepositoryId repositoryId,
        string compilationContextId,
        string moduleName,
        string declarationDigest)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        return new CodeOccurrenceTargetId(Hash(
            TargetDomain,
            repositoryId.Value,
            ContractValue.Identifier(compilationContextId, nameof(compilationContextId)),
            ContractValue.Identifier(moduleName, nameof(moduleName)),
            ContractValue.Identifier(declarationDigest, nameof(declarationDigest))));
    }

    /// <summary>Computes the occurrence identity from the accepted usage inputs.</summary>
    public static CodeOccurrenceId ComputeOccurrenceId(
        CodeRepositoryId repositoryId,
        CodeOccurrenceTargetId targetId,
        CodeOccurrenceCategory category,
        CodeLocation span,
        string contentHash)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentNullException.ThrowIfNull(targetId);
        ArgumentNullException.ThrowIfNull(span);
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category));
        // The contributing unit is deliberately excluded: identical physical
        // occurrences contributed by several units must share one identity so
        // contributors can be unioned. The unit is recorded as contributor/owner.
        return new CodeOccurrenceId(Hash(
            UsageDomain,
            repositoryId.Value,
            targetId.Value,
            ((int)category).ToString(System.Globalization.CultureInfo.InvariantCulture),
            span.Path,
            span.StartLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
            span.StartColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
            span.EndLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
            span.EndColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ContractValue.Identifier(contentHash, nameof(contentHash))));
    }

    internal static string ComputePayloadHash(CodeOccurrenceFact fact) => Hash(
        PayloadDomain,
        fact.RepositoryId.Value,
        fact.TargetId.Value,
        ((int)fact.Category).ToString(System.Globalization.CultureInfo.InvariantCulture),
        fact.Span.Path,
        fact.Span.StartLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
        fact.Span.StartColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
        fact.Span.EndLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
        fact.Span.EndColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
        fact.DeclarationSpan?.Path ?? string.Empty,
        fact.DeclarationSpan?.StartLine.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        fact.DeclarationSpan?.StartColumn.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        fact.DeclarationSpan?.EndLine.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        fact.DeclarationSpan?.EndColumn.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        fact.SourceHash,
        fact.ContentHash,
        fact.SemanticsVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static string Hash(params string[] fields)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        foreach (var field in fields)
        {
            var bytes = Encoding.UTF8.GetBytes(field ?? string.Empty);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}

/// <summary>
/// One validated, immutable occurrence fact owned by a source unit. The
/// contributor is not part of the immutable payload: identical facts contributed
/// by several units share one payload hash and are unioned, while conflicting
/// payloads for one identity are rejected.
/// </summary>
public sealed record CodeOccurrenceFact
{
    public CodeOccurrenceFact(
        CodeOccurrenceId id,
        CodeOccurrenceTargetId targetId,
        CodeRepositoryId repositoryId,
        CodeOccurrenceCategory category,
        string sourceUnitId,
        CodeOccurrenceContributor contributor,
        CodeLocation span,
        string sourceHash,
        string contentHash,
        CodeLocation? declarationSpan = null,
        int semanticsVersion = 4,
        string? payloadHash = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
        RepositoryId = repositoryId ?? throw new ArgumentNullException(nameof(repositoryId));
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category));
        Category = category;
        SourceUnitId = ContractValue.Identifier(sourceUnitId, nameof(sourceUnitId));
        Contributor = contributor ?? throw new ArgumentNullException(nameof(contributor));
        Span = span ?? throw new ArgumentNullException(nameof(span));
        SourceHash = ContractValue.Identifier(sourceHash, nameof(sourceHash));
        ContentHash = ContractValue.Identifier(contentHash, nameof(contentHash));
        DeclarationSpan = declarationSpan;
        if (semanticsVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(semanticsVersion));
        SemanticsVersion = semanticsVersion;
        PayloadHash = payloadHash ?? CodeOccurrenceIdentity.ComputePayloadHash(this);
    }

    public CodeOccurrenceId Id { get; }
    public CodeOccurrenceTargetId TargetId { get; }
    public CodeRepositoryId RepositoryId { get; }
    public CodeOccurrenceCategory Category { get; }
    public string SourceUnitId { get; }
    public CodeOccurrenceContributor Contributor { get; }
    public CodeLocation Span { get; }
    public CodeLocation? DeclarationSpan { get; }
    public string SourceHash { get; }
    public string ContentHash { get; }
    public int SemanticsVersion { get; }
    public string PayloadHash { get; }

    /// <summary>
    /// Builds a fact with production-computed identity and payload hash. Callers
    /// supply only the accepted semantic inputs; identity construction cannot be
    /// bypassed.
    /// </summary>
    public static CodeOccurrenceFact Create(
        CodeRepositoryId repositoryId,
        CodeOccurrenceTargetId targetId,
        string sourceUnitId,
        CodeOccurrenceContributor contributor,
        CodeOccurrenceCategory category,
        CodeLocation span,
        string sourceHash,
        string contentHash,
        CodeLocation? declarationSpan = null,
        int semanticsVersion = 4)
    {
        var id = CodeOccurrenceIdentity.ComputeOccurrenceId(
            repositoryId,
            targetId,
            category,
            span,
            contentHash);
        return new CodeOccurrenceFact(
            id,
            targetId,
            repositoryId,
            category,
            sourceUnitId,
            contributor,
            span,
            sourceHash,
            contentHash,
            declarationSpan,
            semanticsVersion);
    }
}

/// <summary>Disjoint coverage counters for one occurrence contribution.</summary>
public sealed record CodeOccurrenceCoverage(
    int InvocationFacts,
    int UnsupportedCategories)
{
    public int TotalFacts => InvocationFacts;
}
