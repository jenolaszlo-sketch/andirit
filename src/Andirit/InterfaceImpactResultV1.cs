using System.Text.Json;

namespace Andirit;

/// <summary>Work budgets are charged before deduplication; output caps are independent.</summary>
public sealed record InterfaceImpactOptionsV1
{
    public InterfaceImpactOptionsV1(int maxCandidateScans = 100000, int maxHeritageScans = 10000,
        int maxResolverJoins = 10000, int maxAttributionLookups = 10000, int maxWitnessSteps = 100000,
        int maxConfirmed = 1000, int maxOccurrences = 10000, int maxUnknowns = 10000)
    {
        var values = new[] { maxCandidateScans, maxHeritageScans, maxResolverJoins, maxAttributionLookups, maxWitnessSteps, maxConfirmed, maxOccurrences, maxUnknowns };
        if (values.Any(v => v < 1)) throw new ArgumentOutOfRangeException(nameof(maxCandidateScans), "All limits must be positive.");
        MaxCandidateScans = maxCandidateScans; MaxHeritageScans = maxHeritageScans;
        MaxResolverJoins = maxResolverJoins; MaxAttributionLookups = maxAttributionLookups; MaxWitnessSteps = maxWitnessSteps;
        MaxConfirmed = maxConfirmed; MaxOccurrences = maxOccurrences; MaxUnknowns = maxUnknowns;
    }
    public int MaxCandidateScans { get; }
    public int MaxHeritageScans { get; }
    public int MaxResolverJoins { get; }
    public int MaxAttributionLookups { get; }
    public int MaxWitnessSteps { get; }
    public int MaxConfirmed { get; }
    public int MaxOccurrences { get; }
    public int MaxUnknowns { get; }
}

/// <summary>A publication-bound, evidence-backed answer within the bounded EC-6D profile.</summary>
public sealed record InterfaceImpactResultV1(
    InterfaceImpactResultV1.PublicationInfo Publication,
    InterfaceImpactResultV1.TargetInfo Target,
    IReadOnlyList<InterfaceImpactResultV1.Implementation> ConfirmedImplementations,
    IReadOnlyList<InterfaceImpactResultV1.Consumer> PossibleConsumers,
    IReadOnlyList<InterfaceImpactResultV1.Unknown> Unknowns,
    InterfaceImpactResultV1.CoverageInfo Coverage,
    InterfaceImpactResultV1.TruncationInfo Truncation)
{
    public string Qualification => Publication.Qualification;
    public string CoverageStatement => $"This result covers direct explicit class implements bindings with simple identifiers at module scope, direct named relative imports under the documented EC-6D profile, and one bounded hop of resolved re-export alias association for candidate relevance and barrier reporting. Publication qualification: {Qualification}. It does not evaluate: renamed or multi-hop re-export paths beyond that hop, unresolved or unsupported export sources (beyond their recorded not-evaluated counts), CommonJS/require forms, bare package specifiers, namespace or default binding forms, qualified or expression or extends heritage (reported as unknowns only where their terminal identifier matches the target), nested-scope heritage, ordinary type-use references, call sites, implicit structural implementations, transitive inheritance, other languages (including .astro) or cross-plugin symbols. Absence from the confirmed list is not evidence of absence of impact.";
    public string QualificationStatement => Qualification == "consistent" ? "Provider-declared identified snapshot; coverage limits still apply."
        : "All participating facts were produced in this index run from per-file hash-verified reads; the run's source provider does not guarantee a single snapshot; results may mix per-file states read at different times and may not reflect sources edited after the run.";

    /// <summary>Checked authority path. A consistent source snapshot does not imply complete impact coverage.</summary>
    public InterfaceImpactResultV1 RequireConsistentSnapshot()
    {
        ValidateEnvelope();
        return Qualification == "consistent" ? this
            : throw new InvalidOperationException("Qualified interface impact cannot be used as a consistent-snapshot authority.");
    }
    public string ToJson()
    {
        ValidateEnvelope();
        return JsonSerializer.Serialize(this, CodeJsonDefaults.Options);
    }
    public static InterfaceImpactResultV1 FromJson(string json)
    {
        var result = JsonSerializer.Deserialize<InterfaceImpactResultV1>(json, CodeJsonDefaults.Options)
            ?? throw new ArgumentException("Interface impact JSON is empty.", nameof(json));
        result.ValidateEnvelope();
        return result;
    }

    private void ValidateEnvelope()
    {
        if (Publication is null || Publication.RepositoryId is null || Publication.IndexRunId is null || Publication.IndexIdentity is null ||
            Target is null || Target.DeclarationNodeId is null || string.IsNullOrWhiteSpace(Target.Name) ||
            Coverage is null || Truncation is null || Truncation.ChargedWork is null || Truncation.AppliedLimits is null ||
            ConfirmedImplementations is null || PossibleConsumers is null || Unknowns is null ||
            Coverage.Qualification != Qualification || Coverage.Partial != Truncation.CountsAreLowerBounds)
            throw new ArgumentException("Invalid interface impact envelope: mandatory identity and qualification must be preserved.");
    }

    public sealed record PublicationInfo(CodeRepositoryId RepositoryId, CodeIndexRunId IndexRunId, CodeIndexIdentity IndexIdentity, string? SnapshotIdentity, bool IsConsistentSnapshot)
    {
        public string Qualification => IsConsistentSnapshot && !string.IsNullOrWhiteSpace(SnapshotIdentity) ? "consistent" : "qualified";
    }
    public sealed record TargetInfo(CodeNodeId DeclarationNodeId, CodeDeclarationId? DeclarationRecordId, string Name, string? QualifiedName, string? Path, CodeLocation? Span);
    public sealed record Step(string StepKind, string FactId, CodeNodeId? FromNodeId, CodeNodeId? ToNodeId, CodeEvidence? Evidence, CodeFactOrigin Origin,
        IReadOnlyDictionary<string, CodePropertyValue>? Properties = null, CodeLocation? SourceLocation = null, IReadOnlyList<Step>? SupportingFacts = null);
    public sealed record Witness(string WitnessId, IReadOnlyList<Step> Steps, string Policy = "binding1.local-v1", EvidenceQuality Quality = EvidenceQuality.Resolved);
    public sealed record Implementation(CodeNodeId ClassNodeId, string ClassName, string Path, CodeLocation HeritageSpan, string BindingRoute,
        CodeDeclarationId TargetDeclarationRecordId, CodeNodeId TargetDeclarationNodeId, IReadOnlyList<Witness> Witnesses, IReadOnlyList<string> DiscoveryOrigins);
    public sealed record Occurrence(string OccurrenceId, string? Specifier, string? ImportedName, string? LocalName, bool IsTypeOnly,
        string Form, CodeLocation StatementSpan, CodeLocation? ModuleSpecifierSpan, CodeLocation? BindingSpan, string ResolutionRule, CodeFactOrigin Origin);
    public sealed record Consumer(CodeNodeId FileNodeId, string Path, IReadOnlyList<Occurrence> Occurrences);
    public sealed record Unknown(string SubjectKind, CodeNodeId FileNodeId, CodeNodeId? ClassNodeId, string SubjectId, CodeLocation Span,
        string Reason, IReadOnlyList<Step> Evidence, IReadOnlyList<Step> BarrierWitnesses, IReadOnlyList<string> DiscoveryOrigins);
    public sealed record CoverageInfo(string Profile, string Qualification, bool Partial,
        int EvaluatedImportOccurrences, int UnsupportedImportFormOccurrences, int ExternalOccurrences, int UnresolvedOccurrences,
        int HeritageObserved, int HeritageEvaluated, int HeritageNested, int HeritageQualified, int HeritageExpression, int HeritageExtends,
        int HeritageContextsUnavailable, int ExportReferencesObserved, int ExportReferencesUnresolved, int ExportReferencesUnsupported,
        int ConfirmedImplementationCount, int PossibleConsumerModuleCount, IReadOnlyDictionary<string, int> UnknownCountsByReason, IReadOnlyList<string> NotEvaluated)
    {
        public string Completeness => Partial ? "truncated" : NotEvaluated.Count > 0 ? "incomplete-frontier" : "complete-within-declared-profile";
    }
    public sealed record Work(int CandidateScans, int HeritageScans, int ResolverJoins, int AttributionLookups, int WitnessSteps);
    public sealed record TruncationInfo(string State, string? Stage, Work ChargedWork, InterfaceImpactOptionsV1 AppliedLimits, int OmittedFrontierCount, bool CountsAreLowerBounds);
}
