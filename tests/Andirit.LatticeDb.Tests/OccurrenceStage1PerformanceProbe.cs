using System.Diagnostics;
using Andirit;
using Andirit.LatticeDb;
using Xunit.Abstractions;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// Stage 1 reproducible performance baseline. Performance is secondary to
/// correctness; these values are measurements, not acceptance thresholds.
/// </summary>
public sealed class OccurrenceStage1PerformanceProbe
{
    private static readonly CodePluginId Plugin = new("plugin:perf");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ITestOutputHelper _output;

    public OccurrenceStage1PerformanceProbe(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Stage1WorkloadMeasurements()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"andirit-occ-perf-{Guid.NewGuid():N}.ltdb");
        var repo = new CodeRepositoryId("repo:perf");
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await store.UpsertRepositoryAsync(new(repo));

                var publication = Stopwatch.StartNew();
                await PublishAsync(store, repo, "run:perf:1", Facts(repo, "unit:perf", 0));
                publication.Stop();
                _output.WriteLine($"perf publication_ms={publication.Elapsed.TotalMilliseconds:F1} facts=200 targets=20 units=1");

                var replace = Stopwatch.StartNew();
                await PublishAsync(store, repo, "run:perf:2", Facts(repo, "unit:perf", 1000));
                replace.Stop();
                _output.WriteLine($"perf replace_ms={replace.Elapsed.TotalMilliseconds:F1} facts=200");

                var target = Target(repo, "T0");
                var query = Stopwatch.StartNew();
                var page = await store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repo, target, maxResults: 50));
                query.Stop();
                _output.WriteLine($"perf query_ms={query.Elapsed.TotalMilliseconds:F1} examined={page.Examined} total={page.TotalMatches}");
            }

            var reopen = Stopwatch.StartNew();
            using (var reopened = new LatticeCodeGraphStore(path))
            {
                reopen.Stop();
                _output.WriteLine($"perf reopen_ms={reopen.Elapsed.TotalMilliseconds:F1}");

                var deletion = Stopwatch.StartNew();
                await PublishDeletionAsync(reopened, repo, "run:perf:3", "unit:perf");
                deletion.Stop();
                _output.WriteLine($"perf delete_ms={deletion.Elapsed.TotalMilliseconds:F1}");
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name) =>
        CodeOccurrenceIdentity.ComputeTargetId(repo, $"compctx:{name}", $"module:{name}", $"decl:{name}");

    private static CodeOccurrenceFact[] Facts(
        CodeRepositoryId repo,
        string unit,
        int offset)
    {
        var facts = new List<CodeOccurrenceFact>(200);
        for (var target = 0; target < 20; target++)
        {
            var id = Target(repo, $"T{target}");
            for (var index = 0; index < 10; index++)
            {
                var line = offset + target * 100 + index + 1;
                facts.Add(CodeOccurrenceFact.Create(
                    repo,
                    id,
                    unit,
                    new CodeOccurrenceContributor(Plugin, unit),
                    CodeOccurrenceCategory.Invocation,
                    new CodeLocation($"src/P{target}.cs", line, 1, line, 5),
                    "sha256:perf",
                    "content:perf"));
            }
        }
        return facts.ToArray();
    }

    private static async Task PublishAsync(
        LatticeCodeGraphStore store,
        CodeRepositoryId repo,
        string run,
        IReadOnlyList<CodeOccurrenceFact> facts)
    {
        var runId = new CodeIndexRunId(run);
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", runId, new CodeIndexUnitId("unit:perf")),
            [new CodeGraphNode(new CodeNodeId("node:perf"), CodeNodeKinds.Callable, "perf", "perf")],
            occurrences: facts,
            occurrenceAnalysisIncluded: true));
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
        await store.StoreIndexRunAsync(new(repo, runId, Started.AddSeconds(10), plugins: [Plugin]));
        await store.StageIndexUnitDeletionAsync(repo, runId, Plugin, new CodeIndexUnitId(unit));
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started.AddSeconds(10), CodeIndexRunStatus.Completed, Started.AddSeconds(11), [Plugin]),
            new(repo, runId, []));
    }

    private static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-wal", path + "-journal", path + ".wal" })
        {
            try
            {
                if (File.Exists(candidate))
                    File.Delete(candidate);
            }
            catch
            {
            }
        }

        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}
