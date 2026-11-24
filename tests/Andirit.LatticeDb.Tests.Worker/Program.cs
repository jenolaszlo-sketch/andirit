// Worker process for Stage 6 native durability and publication-correctness tests.
//
// Two roles, selected by --mode:
//
//   hold            (D-07 / S6.2) Opens the store, seeds a repository/run, stages a
//                   graph unit and then holds the native database open. The harness
//                   may terminate the process mid-window. If --signal <file> is
//                   supplied the worker writes "READY" to it once the store is open
//                   and then blocks until a sibling "GO" file appears, so a test can
//                   deterministically observe that a second process cannot open the
//                   same file (native file locking).
//
//   publish-complete (S6.3 / D-05 fix, P-15) Opens the store, seeds a
//                   repository/run, stages an index unit that carries occurrence
//                   facts, and then COMPLETES the run (CompleteIndexRunAsync) so the
//                   publication actually commits. This is the correction recorded
//                   against S6.2 finding F-QU-01: the previous worker only ever
//                   staged and never completed, so no publication was ever visible.
//                   On success it prints "PUBLISHED|<indexRunId>|<snapshotIdentity>"
//                   and exits 0.
//
// Usage:
//   Andirit.LatticeDb.Tests.Worker.exe --db <path> --unit <unitId> --node <nodeId> [--mode hold|publish-complete] [--signal <file>] [--target <name>] [--occurrences <n>]

using System.Security.Cryptography;
using System.Text;
using Andirit;
using Andirit.LatticeDb;

namespace Andirit.LatticeDb.Tests.Worker;

internal static class Program
{
    private static readonly CodePluginId Plugin = new("plugin:publication-worker");
    private static readonly DateTimeOffset Started = new(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);

    private static async Task<int> Main(string[] args)
    {
        var options = Parse(args);
        if (options.DatabasePath is null)
        {
            Console.Error.WriteLine("Usage: --db <path> --unit <unitId> --node <nodeId> [--mode hold|publish-complete] [--signal <file>] [--target <name>] [--occurrences <n>]");
            return 1;
        }

        try
        {
            return options.Mode switch
            {
                "publish-complete" => await PublishCompleteAsync(options),
                _ => await HoldAsync(options),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Worker {options.Mode} failed: {ex}");
            return 2;
        }
    }

    private static async Task<int> PublishCompleteAsync(WorkerOptions options)
    {
        var repositoryId = new CodeRepositoryId(options.RepositoryId);
        var runId = new CodeIndexRunId(options.RunId);
        var target = ComputeTarget(options.RepositoryId, options.Target);
        using var store = new LatticeCodeGraphStore(options.DatabasePath!);

        await store.UpsertRepositoryAsync(
            new CodeRepositoryManifest(repositoryId, "Stage 6 publication worker", options.RepositoryId))
            .ConfigureAwait(false);
        await store.StoreIndexRunAsync(
            new CodeIndexRunManifest(repositoryId, runId, Started, plugins: new[] { Plugin }))
            .ConfigureAwait(false);

        var occurrences = Enumerable.Range(0, Math.Max(1, options.OccurrenceCount))
            .Select(index => ComputeFact(
                options.RepositoryId,
                options.NodeId,
                target,
                $"src/Worker{index}.cs",
                1 + index,
                1))
            .ToList();

        await store.StageIndexUnitAsync(
            new CodeIndexUnitReplacement(
                new CodeFactOrigin(repositoryId, Plugin, "1.0.0", runId, new CodeIndexUnitId(options.UnitId)),
                new[]
                {
                    new CodeGraphNode(
                        new CodeNodeId(options.NodeId),
                        CodeNodeKinds.Type,
                        "Worker",
                        "Example.Worker",
                        new CodeSymbolId($"symbol:{options.NodeId}"))
                },
                occurrences: occurrences,
                occurrenceAnalysisIncluded: true,
                occurrenceCoverage: new CodeOccurrenceCoverage(
                    occurrences.Count,
                    0,
                    CodeRelationshipCoverageState.Produced,
                    declaredUnitCount: 1,
                    verifiedUnitCount: 1)))
            .ConfigureAwait(false);

        // F-QU-01 fix: complete the publication so the staged facts commit durably.
        await store.CompleteIndexRunAsync(
            new CodeIndexRunManifest(repositoryId, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), new[] { Plugin }),
            new CodeRepositoryIndexState(
                repositoryId,
                runId,
                new[]
                {
                    new CodeSourceManifest(Plugin, "1.0.0", $"src/{options.NodeId}.cs", $"sha256:{options.NodeId}")
                }))
            .ConfigureAwait(false);

        var publication = await store.GetLatestPublicationAsync(repositoryId).ConfigureAwait(false);
        if (publication is null)
        {
            Console.Error.WriteLine("Worker publish-complete: no publication after completion.");
            return 3;
        }

        Console.WriteLine($"PUBLISHED|{publication.IndexRunId.Value}|{publication.SnapshotIdentity ?? "<null>"}");
        return 0;
    }

    private static async Task<int> HoldAsync(WorkerOptions options)
    {
        var repositoryId = new CodeRepositoryId(options.RepositoryId);
        var runId = new CodeIndexRunId(options.RunId);
        using var store = new LatticeCodeGraphStore(options.DatabasePath!);

        await store.UpsertRepositoryAsync(
            new CodeRepositoryManifest(repositoryId, "Stage 6 hold worker", options.RepositoryId))
            .ConfigureAwait(false);
        await store.StoreIndexRunAsync(
            new CodeIndexRunManifest(repositoryId, runId, Started, plugins: new[] { Plugin }))
            .ConfigureAwait(false);
        await store.StageIndexUnitAsync(
            new CodeIndexUnitReplacement(
                new CodeFactOrigin(repositoryId, Plugin, "1.0.0", runId, new CodeIndexUnitId(options.UnitId)),
                new[]
                {
                    new CodeGraphNode(
                        new CodeNodeId(options.NodeId),
                        CodeNodeKinds.Type,
                        "Worker",
                        "Example.Worker",
                        new CodeSymbolId($"symbol:{options.NodeId}"))
                }))
            .ConfigureAwait(false);

        if (options.SignalPath is not null)
            Signal(options.SignalPath);
        // The store is disposed (and the file lock released) only when this method
        // returns; the harness decides when to signal release or to kill the process.
        return 0;
    }

    private static void Signal(string signalBase)
    {
        var ready = signalBase + ".ready";
        File.WriteAllText(ready, "READY");
        var go = signalBase + ".go";
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        while (!File.Exists(go))
        {
            if (DateTimeOffset.UtcNow > deadline)
                return;
            Thread.Sleep(25);
        }
    }

    private static CodeOccurrenceTargetId ComputeTarget(string repositoryId, string name)
    {
        var context = "compctx1:sha256:" + Hex(repositoryId + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(
            context,
            "meta:" + name,
            "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static CodeOccurrenceFact ComputeFact(
        string repositoryId,
        string unitId,
        CodeOccurrenceTargetId target,
        string path,
        int line,
        int column) =>
        CodeOccurrenceFact.Create(
            new CodeRepositoryId(repositoryId),
            target,
            target.Value,
            unitId,
            new CodeOccurrenceContributor(Plugin, unitId),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            sourceHash: "sha256:source:" + path,
            contentHash: "content:invocation");

    private static string Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static WorkerOptions Parse(string[] args)
    {
        var unitId = "unit:publication-worker";
        var nodeId = "node:publication-worker";
        var repositoryId = "repo:publication-worker";
        var runId = "run:publication-worker";
        string? databasePath = null;
        string mode = "hold";
        string? signalPath = null;
        var target = "T1";
        var occurrences = 2;

        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                continue;
            var value = args[index + 1];
            switch (args[index][2..].ToLowerInvariant())
            {
                case "db": databasePath = value; break;
                case "unit": unitId = value; break;
                case "node": nodeId = value; break;
                case "mode": mode = value; break;
                case "signal": signalPath = value; break;
                case "target": target = value; break;
                case "occurrences": int.TryParse(value, out occurrences); break;
                case "repo": repositoryId = value; break;
                case "run": runId = value; break;
            }
        }

        return new WorkerOptions(
            repositoryId,
            runId,
            unitId,
            nodeId)
        {
            DatabasePath = databasePath,
            Mode = mode,
            SignalPath = signalPath,
            Target = target,
            OccurrenceCount = occurrences,
            RepositoryId = repositoryId,
            RunId = runId,
        };
    }

    private sealed record WorkerOptions(
        string RepositoryId,
        string RunId,
        string UnitId,
        string NodeId)
    {
        public string? DatabasePath { get; init; }
        public string Mode { get; init; } = "hold";
        public string? SignalPath { get; init; }
        public string Target { get; init; } = "T1";
        public int OccurrenceCount { get; init; } = 2;
    }
}