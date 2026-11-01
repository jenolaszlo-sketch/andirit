using System.Security.Cryptography;
using System.Text;
using Andirit;

namespace Andirit.Tests;

/// <summary>
/// Stage 1 in-memory parity for the occurrence vertical slice. Native-only
/// properties (durable reopen, native transactions) are covered separately in
/// Andirit.LatticeDb.Tests; passing here is not provider equivalence.
/// </summary>
public sealed class OccurrenceInMemoryParityTests
{
    private static readonly CodePluginId Plugin = new("plugin:occ-memory");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task InMemory_PublishQueryReplaceAndDelete()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:mem");
        var t1 = Target(repo, "T1");
        var t2 = Target(repo, "T2");

        await PublishAsync(store, repo, "run:1",
            Unit(repo, "run:1", "unit:a", [Node("node:a")],
                [Fact(repo, "unit:a", t1, "src/A.cs", 1, 1),
                 Fact(repo, "unit:a", t2, "src/A.cs", 2, 1)]),
            Unit(repo, "run:1", "unit:b", [Node("node:b")],
                [Fact(repo, "unit:b", t1, "src/B.cs", 1, 1)]));

        var first = await store.FindOccurrencesAsync(new(repo, t1));
        Assert.Equal(CodeOccurrenceAvailability.Available, first.Availability);
        Assert.Equal(2, first.Occurrences.Count);

        await PublishAsync(store, repo, "run:2",
            Unit(repo, "run:2", "unit:a", [Node("node:a")],
                [Fact(repo, "unit:a", t1, "src/A.cs", 5, 1)]));
        var replaced = await store.FindOccurrencesAsync(new(repo, t1));
        Assert.Equal(["src/A.cs:5:1", "src/B.cs:1:1"],
            replaced.Occurrences
                .Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}")
                .ToArray());
        Assert.Empty((await store.FindOccurrencesAsync(new(repo, t2))).Occurrences);

        await PublishDeletionAsync(store, repo, "run:3", "unit:b");
        var deleted = await store.FindOccurrencesAsync(new(repo, t1));
        Assert.Single(deleted.Occurrences);
        Assert.Equal("src/A.cs", deleted.Occurrences[0].Span.Path);
    }

    [Fact]
    public async Task InMemory_ContributorUnionAndConflictRejection()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:mem-union");
        var t1 = Target(repo, "T1");
        await PublishAsync(store, repo, "run:1",
            Unit(repo, "run:1", "unit:a", [],
                [Fact(repo, "unit:a", t1, "src/Shared.cs", 1, 1, "mem:semantic")]),
            Unit(repo, "run:1", "unit:b", [],
                [Fact(repo, "unit:b", t1, "src/Shared.cs", 1, 1, "mem:semantic")]));
        var union = await store.FindOccurrencesAsync(new(repo, t1));
        Assert.Single(union.Occurrences);
        Assert.Equal(2, union.Occurrences[0].Contributors.Count);

        await PublishDeletionAsync(store, repo, "run:2", "unit:a");
        var remaining = await store.FindOccurrencesAsync(new(repo, t1));
        Assert.Single(remaining.Occurrences);
        Assert.Single(remaining.Occurrences[0].Contributors);

        await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:conflict"), Started, plugins: [Plugin]));
        var conflicting = Fact(repo, "unit:c", t1, "src/Shared.cs", 1, 1, "mem:semantic");
        var different = new CodeOccurrenceFact(
            conflicting.Id,
            conflicting.TargetId,
            conflicting.TargetDocumentId,
            conflicting.RepositoryId,
            conflicting.Category,
            conflicting.SourceUnitId,
            new CodeOccurrenceContributor(Plugin, "unit:c"),
            conflicting.Span,
            conflicting.SourceHash,
            contentHash: "content:other",
            conflicting.DeclarationSpan,
            conflicting.SemanticsVersion);
        await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
            await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                new CodeFactOrigin(repo, Plugin, "1.0.0",
                    new CodeIndexRunId("run:conflict"), new CodeIndexUnitId("unit:c")),
                occurrences: [different],
                occurrenceAnalysisIncluded: true)));
    }

    [Fact]
    public async Task InMemory_BoundedDeterministicAndStaleRefusal()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:mem-order");
        var t1 = Target(repo, "T1");
        await PublishAsync(store, repo, "run:1",
            Unit(repo, "run:1", "unit:a", [],
                [
                    Fact(repo, "unit:a", t1, "src/B.cs", 1, 1),
                    Fact(repo, "unit:a", t1, "src/A.cs", 2, 1),
                    Fact(repo, "unit:a", t1, "src/A.cs", 1, 1)
                ]));
        var firstPublication = await store.GetLatestPublicationAsync(repo);

        var bounded = await store.FindOccurrencesAsync(new(repo, t1, maxResults: 2));
        Assert.Equal(3, bounded.TotalMatches);
        Assert.Equal(3, bounded.Examined);
        Assert.Equal(2, bounded.Occurrences.Count);
        Assert.True(bounded.Truncated);
        Assert.Equal("src/A.cs", bounded.Occurrences[0].Span.Path);
        Assert.Equal(1, bounded.Occurrences[0].Span.StartLine);

        await PublishAsync(store, repo, "run:2",
            Unit(repo, "run:2", "unit:a", [],
                [Fact(repo, "unit:a", t1, "src/C.cs", 1, 1)]));
        var stale = await store.FindOccurrencesAsync(new(repo, t1, firstPublication));
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);

        var graphOnly = new CodeRepositoryId("repo:mem-graph-only");
        await PublishAsync(store, graphOnly, "run:g",
            Unit(graphOnly, "run:g", "unit:g", [Node("node:g")], []));
        var unavailable = await store.FindOccurrencesAsync(new(graphOnly, t1));
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, unavailable.Availability);
    }

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name)
    {
        var context = "compctx1:sha256:" + Hex(repo.Value + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(
            context,
            "meta:" + name,
            "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static string Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static CodeOccurrenceFact Fact(
        CodeRepositoryId repo,
        string unitId,
        CodeOccurrenceTargetId target,
        string path,
        int line,
        int column,
        string? semanticUnitId = null) =>
        CodeOccurrenceFact.Create(
            repo, target, target.Value, semanticUnitId ?? unitId,
            new CodeOccurrenceContributor(Plugin, unitId),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            "sha256:source",
            "content:invocation");

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId($"symbol:{id}"));

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo,
        string run,
        string unit,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeOccurrenceFact> occurrences) =>
        new(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            nodes,
            occurrences: occurrences,
            occurrenceAnalysisIncluded: occurrences.Count > 0,
            occurrenceCoverage: occurrences.Count > 0
                ? new CodeOccurrenceCoverage(
                    occurrences.Count,
                    0,
                    CodeRelationshipCoverageState.Produced)
                : null);

    private static async Task PublishAsync(
        InMemoryCodeGraphStore store,
        CodeRepositoryId repo,
        string run,
        params CodeIndexUnitReplacement[] units)
    {
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
    }

    private static async Task PublishDeletionAsync(
        InMemoryCodeGraphStore store,
        CodeRepositoryId repo,
        string run,
        string unit)
    {
        var runId = new CodeIndexRunId(run);
        await store.StoreIndexRunAsync(new(repo, runId, Started.AddSeconds(10), plugins: [Plugin]));
        await store.StageIndexUnitDeletionAsync(repo, runId, Plugin, new CodeIndexUnitId(unit));
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started.AddSeconds(10), CodeIndexRunStatus.Completed, Started.AddSeconds(11), [Plugin]),
            new(repo, runId, []));
    }
}
