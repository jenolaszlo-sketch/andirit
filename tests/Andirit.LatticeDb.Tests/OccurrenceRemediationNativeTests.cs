using System.Security.Cryptography;
using System.Text;
using Andirit;
using Andirit.LatticeDb;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// REM-01 native foundation regressions: complete-empty coverage, incomplete
/// coverage, coverage invalidation, and commit-uncertainty refusal.
/// </summary>
public sealed class OccurrenceRemediationNativeTests
{
    private static readonly CodePluginId Plugin = new("plugin:rem-native");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompleteEmptyAnalysisSurvivesReplacementAndReopen()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:rem:zero");
        var target = Target(repo, "T1");
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(
                    store,
                    repo,
                    "run:zero:1",
                    Unit(repo, "run:zero:1", "unit:zero",
                        [Fact(repo, "unit:zero", target, "src/Old.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));

                await PublishAsync(
                    store,
                    repo,
                    "run:zero:2",
                    CompleteEmptyUnit(repo, "run:zero:2", "unit:zero"));

                var current = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
                Assert.Empty(current.Occurrences);
                Assert.NotNull(current.Coverage);
                Assert.True(current.Coverage!.IsComplete);
                Assert.True(current.IsComplete);
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                var after = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
                Assert.Empty(after.Occurrences);
                Assert.True(after.Coverage!.IsComplete);
                Assert.True(after.IsComplete);
            }

            Assert.Equal(1, CountLabel(path, "AndiritOccurrenceUnit"));
            Assert.Equal(0, CountLabel(path, "AndiritOccurrencePage"));
            Assert.Equal(1, CountLabel(path, "AndiritOccurrenceMeta"));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task IncompleteCoverageCannotMasqueradeAsCompleteEmpty()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:rem:incomplete");
        var target = Target(repo, "T1");
        try
        {
            using var store = new LatticeCodeGraphStore(path);
            await PublishAsync(
                store,
                repo,
                "run:inc:1",
                Unit(repo, "run:inc:1", "unit:inc",
                    [Fact(repo, "unit:inc", target, "src/A.cs", 1, 1)],
                    CodeRelationshipCoverageState.Partial),
                Unit(repo, "run:inc:1", "unit:inc:empty",
                    [],
                    CodeRelationshipCoverageState.Partial));

            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.False(result.Coverage!.IsComplete);
            Assert.False(result.IsComplete);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task NoAnalysisWithZeroFactsRemainsUnavailable()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:rem:none");
        var target = Target(repo, "T1");
        try
        {
            using var store = new LatticeCodeGraphStore(path);
            await PublishAsync(
                store,
                repo,
                "run:none:1",
                new CodeIndexUnitReplacement(
                    Origin(repo, "run:none:1", "unit:none"),
                    [Node("node:none")],
                    occurrences: [],
                    occurrenceAnalysisIncluded: false));

            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, result.Availability);
            Assert.Null(result.Coverage);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task DeletingACompleteUnitInvalidatesCoverage()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:rem:delete");
        var target = Target(repo, "T1");
        try
        {
            using var store = new LatticeCodeGraphStore(path);
            await PublishAsync(
                store,
                repo,
                "run:delete:1",
                CompleteEmptyUnit(repo, "run:delete:1", "unit:delete"));
            await PublishDeletionAsync(store, repo, "run:delete:2", "unit:delete");

            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, result.Availability);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task AfterCommitAcknowledgementLossRefusesCurrentStateAndReopenSeesDurableState()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:rem:unknown");
        var target = Target(repo, "T1");
        var failAfterCommit = false;
        try
        {
            using (var store = new LatticeCodeGraphStore(path, null, point =>
                   {
                       if (failAfterCommit && point == "after-commit")
                           throw new InjectedPersistenceException();
                   }))
            {
                await PublishAsync(
                    store,
                    repo,
                    "run:unknown:1",
                    Unit(repo, "run:unknown:1", "unit:unknown",
                        [Fact(repo, "unit:unknown", target, "src/Prior.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));

                var run = new CodeIndexRunId("run:unknown:2");
                await store.StoreIndexRunAsync(new(repo, run, Started.AddSeconds(5), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:unknown:2", "unit:unknown",
                        [Fact(repo, "unit:unknown", target, "src/New.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));

                failAfterCommit = true;
                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, run, Started.AddSeconds(5),
                            CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
                        new(repo, run, [])));

                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () =>
                    await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var after = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
            Assert.Single(after.Occurrences);
            Assert.Equal("src/New.cs", after.Occurrences[0].Span.Path);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static CodeFactOrigin Origin(
        CodeRepositoryId repo,
        string run,
        string unit) =>
        new(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit));

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
        string unit,
        CodeOccurrenceTargetId target,
        string path,
        int line,
        int column) =>
        CodeOccurrenceFact.Create(
            repo,
            target,
            target.Value,
            unit,
            new CodeOccurrenceContributor(Plugin, unit),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            "sha256:source",
            "content:invocation");

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo,
        string run,
        string unit,
        IReadOnlyList<CodeOccurrenceFact> facts,
        CodeRelationshipCoverageState state) =>
        new(
            Origin(repo, run, unit),
            [Node("node:" + unit)],
            occurrences: facts,
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(
                facts.Count,
                0,
                state,
                scopeId: "analysisscope1:sha256:" + new string('a', 64),
                universeId: "compilation-universe1:sha256:" + new string('b', 64),
                declaredUnitCount: 1,
                verifiedUnitCount: 1));

    private static CodeIndexUnitReplacement CompleteEmptyUnit(
        CodeRepositoryId repo,
        string run,
        string unit) =>
        Unit(repo, run, unit, [], CodeRelationshipCoverageState.Produced);

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId("symbol:" + id));

    private static async Task PublishAsync(
        LatticeCodeGraphStore store,
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
        LatticeCodeGraphStore store,
        CodeRepositoryId repo,
        string run,
        string unit)
    {
        var runId = new CodeIndexRunId(run);
        await store.StoreIndexRunAsync(new(repo, runId, Started.AddSeconds(30), plugins: [Plugin]));
        await store.StageIndexUnitDeletionAsync(repo, runId, Plugin, new CodeIndexUnitId(unit));
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started.AddSeconds(30), CodeIndexRunStatus.Completed, Started.AddSeconds(31), [Plugin]),
            new(repo, runId, []));
    }

    private static int CountLabel(string path, string label)
    {
        using var database = LatticeDbSharp.LatticeDatabase.Open(
            path,
            new LatticeDbSharp.LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginReadTransaction();
        using var query = database.Prepare($"MATCH (s:{label}) RETURN count(s)");
        using var result = query.Execute(txn);
        var rows = result.ReadAll();
        txn.Commit();
        return rows.Count == 0 ? 0 : (int)rows[0].GetValue(0).AsInt64();
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"andirit-rem-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-wal", path + "-journal", path + ".wal" })
        {
            if (File.Exists(candidate))
                File.Delete(candidate);
        }
    }

    private sealed class InjectedPersistenceException : Exception
    {
        public InjectedPersistenceException() : base("injected persistence failure") { }
    }
}
