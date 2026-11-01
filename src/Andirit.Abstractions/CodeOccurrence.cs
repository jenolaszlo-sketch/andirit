using System.Security.Cryptography;
using System.Text;

namespace Andirit;

/// <summary>
/// The supported occurrence category for CI-1A. The set is intentionally
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
    public CodeOccurrenceId(string value)
    {
        if (!CodeOccurrenceIdentity.IsCanonicalOccurrence(value))
        {
            throw new ArgumentException(
                "An occurrence identity must be 64 lowercase hexadecimal characters.",
                nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>Identifies the exact bound declaration target (the accepted `asmctx3` shape).</summary>
public sealed record CodeOccurrenceTargetId
{
    public CodeOccurrenceTargetId(string value)
    {
        if (!CodeOccurrenceIdentity.IsCanonicalTarget(value))
        {
            throw new ArgumentException(
                "A target identity must be 'asmctx3:sha256:' followed by 64 lowercase hexadecimal characters.",
                nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>One `(plugin, storage unit)` that contributed an identical occurrence fact.</summary>
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
/// occurrence facts. This implements the accepted R2 `usage4` / `asmctx3`
/// contracts: big-endian part lengths, the accepted domain strings and the exact
/// tuple fields. It does not use documentation IDs, paths or descriptors as
/// shortcuts.
/// </summary>
public static class CodeOccurrenceIdentity
{
    /// <summary>The accepted R2 occurrence semantics string, hashed into `usage4`.</summary>
    public const string SemanticsVersion = "usage4-b2c-exp-r2-20261101";

    /// <summary>The accepted R2 target domain and canonical prefix.</summary>
    public const string TargetDomain = "asmctx3";
    public const string TargetPrefix = TargetDomain + ":sha256:";

    /// <summary>The accepted R2 occurrence domain.</summary>
    public const string UsageDomain = "usage4";

    /// <summary>The accepted R2 category token for an ordinary method invocation.</summary>
    public const string InvocationCategory = "method-invocation";

    private const string CompilationContractVersion = "compctx1";
    private const string TargetUnavailable = TargetDomain + ":unavailable";

    public static bool IsCanonicalOccurrence(string? value) =>
        value is { Length: 64 } && value.All(char.IsAsciiHexDigitLower);

    public static bool IsCanonicalTarget(string? value) =>
        value is not null &&
        value.StartsWith(TargetPrefix, StringComparison.Ordinal) &&
        value.Length == TargetPrefix.Length + 64 &&
        value.AsSpan(TargetPrefix.Length).ToString().All(char.IsAsciiHexDigitLower);

    /// <summary>
    /// Computes the exact R2 `asmctx3` target identity from verified defining
    /// compilation inputs. The compilation context must be a canonical
    /// `compctx1:sha256:&lt;64 hex&gt;` value; a caller-supplied descriptor is not
    /// sufficient.
    /// </summary>
    public static CodeOccurrenceTargetId ComputeTargetId(
        string compilationContextId,
        string metadataIdentity,
        string moduleName,
        string identifierDigest,
        string declarationDigest,
        string shapeDigest)
    {
        var context = ContractValue.Identifier(
            compilationContextId,
            nameof(compilationContextId));
        if (!IsCanonicalCompilationContext(context))
        {
            throw new ArgumentException(
                "The compilation context must be 'compctx1:sha256:' followed by 64 lowercase hexadecimal characters.",
                nameof(compilationContextId));
        }

        return new CodeOccurrenceTargetId(TargetPrefix + Hash(
            TargetDomain + "-context",
            CompilationContractVersion,
            context,
            ContractValue.Identifier(metadataIdentity, nameof(metadataIdentity)),
            ContractValue.Identifier(moduleName, nameof(moduleName)),
            ContractValue.Identifier(identifierDigest, nameof(identifierDigest)),
            ContractValue.Identifier(declarationDigest, nameof(declarationDigest)),
            ContractValue.Identifier(shapeDigest, nameof(shapeDigest))));
    }

    /// <summary>Computes the R2 identifier-token digest over canonical site forms.</summary>
    public static string ComputeIdentifierDigest(IEnumerable<string> identifierSiteForms) =>
        Hash(TargetDomain + "-identifier", ToParts(identifierSiteForms));

    /// <summary>Computes the R2 full-declaration digest over canonical site forms.</summary>
    public static string ComputeDeclarationDigest(IEnumerable<string> declarationSiteForms) =>
        Hash(TargetDomain + "-declarations", ToParts(declarationSiteForms));

    /// <summary>Computes the R2 semantic declaration-shape digest.</summary>
    public static string ComputeShapeDigest(string shape) =>
        Hash(TargetDomain + "-target", ContractValue.Required(shape, nameof(shape)));

    /// <summary>
    /// Computes the exact R2 `usage4` occurrence identity. The contributing
    /// storage unit is intentionally excluded; <paramref name="sourceUnitId"/>
    /// is the semantic source/calling unit and does participate. Identical
    /// physical occurrences of the same semantic unit can therefore union
    /// contributors across storage units.
    /// </summary>
    public static CodeOccurrenceId ComputeOccurrenceId(
        CodeRepositoryId repositoryId,
        CodePluginId pluginId,
        string sourceUnitId,
        string sourcePath,
        string sourceHash,
        CodeLocation span,
        string targetDocumentId,
        CodeOccurrenceTargetId targetId,
        CodeOccurrenceCategory category)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentNullException.ThrowIfNull(pluginId);
        ArgumentNullException.ThrowIfNull(span);
        ArgumentNullException.ThrowIfNull(targetId);
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category));
        if (!IsCanonicalTarget(targetId.Value))
            throw new ArgumentException("The target identity is not canonical.", nameof(targetId));

        return new CodeOccurrenceId(Hash(
            UsageDomain,
            repositoryId.Value,
            pluginId.Value,
            SemanticsVersion,
            ContractValue.Identifier(sourceUnitId, nameof(sourceUnitId)),
            ContractValue.Identifier(sourcePath, nameof(sourcePath)),
            ContractValue.Identifier(sourceHash, nameof(sourceHash)),
            span.StartLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
            span.StartColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
            span.EndLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
            span.EndColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ContractValue.Identifier(targetDocumentId, nameof(targetDocumentId)),
            targetId.Value,
            CategoryToken(category)));
    }

    public static string ComputePayloadHash(CodeOccurrenceFact fact) => Hash(
        "occurrence-payload4",
        fact.RepositoryId.Value,
        fact.TargetId.Value,
        fact.TargetDocumentId,
        fact.Contributor.PluginId.Value,
        fact.PluginVersion,
        fact.SourceUnitId,
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

    internal static string CategoryToken(CodeOccurrenceCategory category) => category switch
    {
        CodeOccurrenceCategory.Invocation => InvocationCategory,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private static bool IsCanonicalCompilationContext(string value) =>
        value.StartsWith("compctx1:sha256:", StringComparison.Ordinal) &&
        value.Length == "compctx1:sha256:".Length + 64 &&
        value.AsSpan("compctx1:sha256:".Length).ToString().All(char.IsAsciiHexDigitLower);

    private static string[] ToParts(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var parts = values
            .Select(value => ContractValue.Required(value, nameof(values)))
            .ToArray();
        if (parts.Length == 0)
            throw new ArgumentException("At least one canonical site form is required.", nameof(values));
        return parts;
    }

    private static string Hash(string domain, params string[] fields) => Hash(domain, (IEnumerable<string>)fields);

    private static string Hash(string domain, IEnumerable<string> fields)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        static void Append(IncrementalHash hash, Span<byte> length, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        Append(hash, length, domain);
        foreach (var field in fields)
            Append(hash, length, field);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}

/// <summary>
/// One validated, immutable occurrence fact owned by a source unit. The
/// contributor is not part of the immutable payload: identical facts contributed
/// by several storage units share one payload hash and are unioned, while
/// conflicting payloads for one identity are rejected.
/// </summary>
public sealed record CodeOccurrenceFact
{
    /// <summary>Creates a fact and verifies that the supplied identity is the canonical R2 value.</summary>
    public CodeOccurrenceFact(
        CodeOccurrenceId id,
        CodeOccurrenceTargetId targetId,
        string targetDocumentId,
        CodeRepositoryId repositoryId,
        CodeOccurrenceCategory category,
        string sourceUnitId,
        CodeOccurrenceContributor contributor,
        CodeLocation span,
        string sourceHash,
        string contentHash,
        CodeLocation? declarationSpan = null,
        int semanticsVersion = 4,
        string? pluginVersion = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
        TargetDocumentId = ContractValue.Identifier(targetDocumentId, nameof(targetDocumentId));
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
        PluginVersion = string.IsNullOrWhiteSpace(pluginVersion)
            ? "unknown"
            : ContractValue.Identifier(pluginVersion, nameof(pluginVersion));

        var expected = CodeOccurrenceIdentity.ComputeOccurrenceId(
            RepositoryId,
            Contributor.PluginId,
            SourceUnitId,
            Span.Path,
            SourceHash,
            Span,
            TargetDocumentId,
            TargetId,
            Category);
        if (!string.Equals(expected.Value, Id.Value, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The occurrence identity does not match the canonical R2 tuple.",
                nameof(id));
        }

        PayloadHash = CodeOccurrenceIdentity.ComputePayloadHash(this);
    }

    public CodeOccurrenceId Id { get; }
    public CodeOccurrenceTargetId TargetId { get; }
    public string TargetDocumentId { get; }
    public CodeRepositoryId RepositoryId { get; }
    public CodeOccurrenceCategory Category { get; }
    public string SourceUnitId { get; }
    public CodeOccurrenceContributor Contributor { get; }
    public CodeLocation Span { get; }
    public CodeLocation? DeclarationSpan { get; }
    public string SourceHash { get; }
    public string ContentHash { get; }
    public string PluginVersion { get; }
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
        string targetDocumentId,
        string sourceUnitId,
        CodeOccurrenceContributor contributor,
        CodeOccurrenceCategory category,
        CodeLocation span,
        string sourceHash,
        string contentHash,
        CodeLocation? declarationSpan = null,
        int semanticsVersion = 4,
        string? pluginVersion = null)
    {
        ArgumentNullException.ThrowIfNull(contributor);
        ArgumentNullException.ThrowIfNull(span);
        var id = CodeOccurrenceIdentity.ComputeOccurrenceId(
            repositoryId,
            contributor.PluginId,
            sourceUnitId,
            span.Path,
            sourceHash,
            span,
            targetDocumentId,
            targetId,
            category);
        return new CodeOccurrenceFact(
            id,
            targetId,
            targetDocumentId,
            repositoryId,
            category,
            sourceUnitId,
            contributor,
            span,
            sourceHash,
            contentHash,
            declarationSpan,
            semanticsVersion,
            pluginVersion);
    }
}

/// <summary>
/// Disjoint resolution/coverage counters plus the analysis state for one
/// occurrence contribution. <see cref="State"/> uses the existing coverage-state
/// vocabulary: <see cref="CodeRelationshipCoverageState.Produced"/> means the
/// declared scope was completely analyzed, <see cref="CodeRelationshipCoverageState.Partial"/>
/// means analysis ran but completeness is unproven, and
/// <see cref="CodeRelationshipCoverageState.NotProduced"/> means no occurrence
/// analysis ran. A zero-fact contribution with <c>Produced</c> is a valid
/// complete-empty analysis, not a missing analysis.
/// </summary>
public sealed record CodeOccurrenceCoverage
{
    public CodeOccurrenceCoverage(
        int invocationFacts = 0,
        int unsupportedCategories = 0,
        CodeRelationshipCoverageState state = CodeRelationshipCoverageState.NotProduced,
        string? scopeId = null,
        string? universeId = null,
        int declaredUnitCount = 0,
        int verifiedUnitCount = 0,
        int external = 0,
        int ambiguous = 0,
        int unresolved = 0,
        int unsupported = 0,
        int conditionalInactive = 0,
        IReadOnlyDictionary<string, int>? unavailableReasons = null)
    {
        if (invocationFacts < 0 || unsupportedCategories < 0 ||
            declaredUnitCount < 0 || verifiedUnitCount < 0 ||
            external < 0 || ambiguous < 0 || unresolved < 0 ||
            unsupported < 0 || conditionalInactive < 0)
            throw new ArgumentOutOfRangeException(nameof(invocationFacts));
        if (!Enum.IsDefined(state))
            throw new ArgumentOutOfRangeException(nameof(state));
        if (verifiedUnitCount > declaredUnitCount && declaredUnitCount != 0)
            throw new ArgumentException(
                "Verified unit count cannot exceed the declared unit count.",
                nameof(verifiedUnitCount));

        InvocationFacts = invocationFacts;
        UnsupportedCategories = unsupportedCategories;
        State = state;
        ScopeId = string.IsNullOrWhiteSpace(scopeId)
            ? null
            : ContractValue.Identifier(scopeId, nameof(scopeId));
        UniverseId = string.IsNullOrWhiteSpace(universeId)
            ? null
            : ContractValue.Identifier(universeId, nameof(universeId));
        DeclaredUnitCount = declaredUnitCount;
        VerifiedUnitCount = verifiedUnitCount;
        External = external;
        Ambiguous = ambiguous;
        Unresolved = unresolved;
        Unsupported = unsupported;
        ConditionalInactive = conditionalInactive;
        UnavailableReasons = unavailableReasons is null
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : new Dictionary<string, int>(unavailableReasons, StringComparer.Ordinal);
    }

    public int InvocationFacts { get; }
    public int UnsupportedCategories { get; }
    public CodeRelationshipCoverageState State { get; }
    public string? ScopeId { get; }
    public string? UniverseId { get; }
    public int DeclaredUnitCount { get; }
    public int VerifiedUnitCount { get; }
    public int External { get; }
    public int Ambiguous { get; }
    public int Unresolved { get; }
    public int Unsupported { get; }
    public int ConditionalInactive { get; }
    public IReadOnlyDictionary<string, int> UnavailableReasons { get; }
    public int TotalFacts => InvocationFacts;

    /// <summary>True when an occurrence pass ran, even if it produced no facts.</summary>
    public bool AnalysisPerformed => State is CodeRelationshipCoverageState.Produced or
        CodeRelationshipCoverageState.Partial;

    /// <summary>True only when the declared scope is completely analyzed.</summary>
    public bool IsComplete => State == CodeRelationshipCoverageState.Produced;

    /// <summary>
    /// Combines partial/complete evidence from several batches or units. The
    /// worst analysis state wins; counters are summed; the first non-null scope
    /// and universe identities are retained.
    /// </summary>
    public CodeOccurrenceCoverage Merge(CodeOccurrenceCoverage other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var state = Worse(State, other.State);
        var reasons = new Dictionary<string, int>(UnavailableReasons, StringComparer.Ordinal);
        foreach (var (key, value) in other.UnavailableReasons)
            reasons[key] = reasons.GetValueOrDefault(key) + value;
        return new CodeOccurrenceCoverage(
            InvocationFacts + other.InvocationFacts,
            UnsupportedCategories + other.UnsupportedCategories,
            state,
            ScopeId ?? other.ScopeId,
            UniverseId ?? other.UniverseId,
            Math.Max(DeclaredUnitCount, other.DeclaredUnitCount),
            Math.Max(VerifiedUnitCount, other.VerifiedUnitCount),
            External + other.External,
            Ambiguous + other.Ambiguous,
            Unresolved + other.Unresolved,
            Unsupported + other.Unsupported,
            ConditionalInactive + other.ConditionalInactive,
            reasons);
    }

    /// <summary>Returns a copy with a different analysis state.</summary>
    public CodeOccurrenceCoverage WithState(CodeRelationshipCoverageState state) =>
        new(
            InvocationFacts,
            UnsupportedCategories,
            state,
            ScopeId,
            UniverseId,
            DeclaredUnitCount,
            VerifiedUnitCount,
            External,
            Ambiguous,
            Unresolved,
            Unsupported,
            ConditionalInactive,
            UnavailableReasons);

    private static CodeRelationshipCoverageState Worse(
        CodeRelationshipCoverageState first,
        CodeRelationshipCoverageState second)
    {
        // Completeness is a conjunction. If any contribution is unavailable,
        // the combined result is unavailable. If one contribution ran and the
        // other did not (or ran partially), the combined result is partial.
        if (first == CodeRelationshipCoverageState.Unavailable ||
            second == CodeRelationshipCoverageState.Unavailable)
            return CodeRelationshipCoverageState.Unavailable;
        if (first == CodeRelationshipCoverageState.Partial ||
            second == CodeRelationshipCoverageState.Partial)
            return CodeRelationshipCoverageState.Partial;
        if (first == CodeRelationshipCoverageState.Produced &&
            second == CodeRelationshipCoverageState.Produced)
            return CodeRelationshipCoverageState.Produced;
        if (first == CodeRelationshipCoverageState.Produced ||
            second == CodeRelationshipCoverageState.Produced)
            return CodeRelationshipCoverageState.Partial;
        return CodeRelationshipCoverageState.NotProduced;
    }
}
