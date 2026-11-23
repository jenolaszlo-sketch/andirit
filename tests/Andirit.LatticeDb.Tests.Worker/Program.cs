// Worker process for S6.2 native durability tests (D-07: abrupt process termination).
//
// This is a standalone console application that can be spawned as a worker process
// to perform a write operation and be killed mid-transaction by the test harness.
//
// Usage: Andirit.LatticeDb.Tests.Worker.exe --db <path> --unit <unitId> --node <nodeId>

using System;
using System.Threading.Tasks;
using Andirit;
using Andirit.LatticeDb;

namespace Andirit.LatticeDb.Tests.Worker;

internal static class Program
{
    private static readonly CodePluginId Plugin = new("plugin:durability-worker");
    private static readonly DateTimeOffset Started = new(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);

    private static async Task<int> Main(string[] args)
    {
        // Parse arguments: --db <path> --unit <unitId> --node <nodeId>
        var argsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (i + 1 < args.Length && args[i].StartsWith("--", StringComparison.Ordinal))
            {
                argsDict[args[i].Substring(2)] = args[i + 1];
            }
        }

        if (!argsDict.TryGetValue("db", out var dbPath) ||
            !argsDict.TryGetValue("unit", out var unitId) ||
            !argsDict.TryGetValue("node", out var nodeId))
        {
            Console.Error.WriteLine("Usage: --db <path> --unit <unitId> --node <nodeId>");
            return 1;
        }

        try
        {
            using var store = new LatticeCodeGraphStore(dbPath);
            var repository = new CodeRepositoryId("repo:durability");
            var run = new CodeIndexRunId("run:durability");

            await store.UpsertRepositoryAsync(
                new CodeRepositoryManifest(new CodeRepositoryId("repo:durability"), "Stage 6 durability", "repo://durability")).ConfigureAwait(false);
            await store.StoreIndexRunAsync(
                new CodeIndexRunManifest(new CodeRepositoryId("repo:durability"), new CodeIndexRunId("run:durability"),
                    new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero), plugins: new[] { new CodePluginId("plugin:durability") })).ConfigureAwait(false);

            var unit = new CodeIndexUnitReplacement(
                new CodeFactOrigin(new CodeRepositoryId("repo:durability"), new CodePluginId("plugin:durability"), "1.0.0",
                    new CodeIndexRunId("run:durability"), new CodeIndexUnitId(unitId)),
                new[] { new CodeGraphNode(
                    new CodeNodeId(nodeId),
                    CodeNodeKinds.Type,
                    "Worker",
                    "Example.Worker",
                    new CodeSymbolId($"symbol:{nodeId}")) });

            await store.StageIndexUnitAsync(unit).ConfigureAwait(false);

            // The process will be killed by the test harness mid-transaction or after
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Worker error: {ex}");
            return 1;
        }

        return 0;
    }
}
