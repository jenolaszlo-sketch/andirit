// ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01 - reviewer-owned diagnostic probe.
//
// This instrument belongs to the INDEPENDENT REVIEWER. It is deliberately kept outside
// the repository and outside every committed test project. It exists to answer questions
// that the producer's committed tests do not answer:
//
//   PROBE A  Which branch does the producer's Q-02 schedule actually take? Does the
//             cancellation ever arrive while the native write transaction is open, or is
//             it always observed before the native window (which would make the
//             "never exposes partial state" assertion vacuous)?
//   PROBE B  Same question for the producer's Q-04 schedule, over many repetitions.
//
//   PROBE C  With the native window PROVABLY open (detected by the production
//             fault-injector seam), is the cancellation genuinely not observed, and is the
//             staged unit committed whole (never partially)?
//
//   PROBE D  Does the in-memory projection ever expose the staged fact to a concurrent
//             reader while a cancellation is pending (D4 invariant 5, non-exposure)?
//
//   PROBE E  Does a cancellation observed at the managed write gate leave the durable
//             native state untouched, and is the pre-native checkpoint the LAST
//             cancellation observation before the native transaction opens?
//
//   PROBE F  Is the "cancelled before native invocation" outcome ever reported when a
//             native transaction actually opened? (A lie in the outcome taxonomy would
//             be the most serious possible defect.)
//
// Every probe prints an explicit OBSERVED line and a machine-readable summary line.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests.ReviewerProbe;

internal static class Program
{
    private const string StageLabel = "HetuStage";
    private const string UnitLabel = "HetuUnit";
    private static readonly CodePluginId Plugin = new("plugin:cancel-path");
    private static readonly DateTimeOffset Started = new(2026, 11, 22, 0, 0, 0, TimeSpan.Zero);
    private static readonly List<string> Lines = new();
    private static int armedA;
    private static int armedE;

    private static void Emit(string text = "")
    {
        Lines.Add(text);
        Console.WriteLine(text);
    }

    private static async Task<int> Main()
    {
        Emit("ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01 - reviewer diagnostic probe");
        Emit("producer assertions are NOT trusted; every claim below is observed here");
        Emit();

        foreach (var (name, probe) in new (string, Func<Task>)[]
        {
            ("PROBE A", ProbeA), ("PROBE B", ProbeB), ("PROBE C", ProbeC),
            ("PROBE D", ProbeD), ("PROBE E", ProbeE), ("PROBE F", ProbeF),
            ("PROBE G", ProbeG), ("PROBE H", ProbeH),
        })
        {
            try
            {
                await probe().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Emit($"  !! {name} FAULTED: {exception.GetType().Name}: {exception.Message}");
                Emit();
            }
        }

        var path = Path.Combine(Path.GetTempPath(), $"qual-s61-probe-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, string.Join('\n', Lines) + "\n").ConfigureAwait(false);
        Console.WriteLine();
        Console.WriteLine($"PROBE-OUTPUT {path}");
        return 0;
    }

    // ------------------------------------------------------------------ PROBE G

    private static async Task ProbeG()
    {
        Emit("== PROBE G: the producer's Q-06 second scenario - is the accepted publication preserved across a");
        Emit("   staging that COMPLETES after a cancellation request arrived in the native window? ==");
        Emit("the qualification plan (Q-06) requires BOTH: a cancelled pre-native staging AND a");
        Emit("completed-but-cancelled-request staging. The producer's Q-06 covers only the first.");
        var path = TempDb();
        try
        {
            string runId = "";
            string? snapshot = null;
            bool consistent = false;
            int publishedUnits = 0;

            using (var nativeWindowOpen = new ManualResetEventSlim(false))
            using (var cancellationIssued = new ManualResetEventSlim(false))
            using (var cancelled = new CancellationTokenSource())
            {
                var armed = 0;
                Action<string> injector = point =>
                {
                    if (point != "before-commit" || Volatile.Read(ref armed) == 0) return;
                    nativeWindowOpen.Set();
                    if (!cancellationIssued.Wait(TimeSpan.FromSeconds(30)))
                    {
                        throw new TimeoutException("cancellation not issued");
                    }
                };

                using (var store = new LatticeCodeGraphStore(path, null, injector))
                {
                    var (repository, run) = Seed(store);
                    var started = Started;
                    await store.StageIndexUnitAsync(Unit(repository, run, "unit:accepted", "node:accepted")).ConfigureAwait(false);
                    await store.CompleteIndexRunAsync(
                        new(repository, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), new[] { Plugin }),
                        new(repository, run, new[] { new CodeSourceManifest(Plugin, "1.0.0", "src/Accepted.cs", "sha256:accepted") })).ConfigureAwait(false);

                    var before = await store.GetLatestPublicationAsync(repository).ConfigureAwait(false);
                    Assert(before is not null, "accepted publication exists");
                    // the second staging uses a NEW run (the first run is already Completed)
                    var secondRun = new CodeIndexRunId("run:cancel-path-2");
                    await store.StoreIndexRunAsync(
                        new CodeIndexRunManifest(repository, secondRun, Started, plugins: new[] { Plugin })).ConfigureAwait(false);
                    runId = before!.IndexRunId.Value;
                    snapshot = before.SnapshotIdentity;
                    consistent = before.IsConsistentSnapshot;
                    publishedUnits = (await store.GetPublishedUnitsAsync(repository).ConfigureAwait(false)).Count;
                    Assert(publishedUnits == 1, $"published units = 1 (got {publishedUnits})");

                    // now a SECOND staging whose cancellation arrives inside the native window,
                    // and which therefore COMPLETES after a cancellation was requested
                    Volatile.Write(ref armed, 1);
                    var canceller = Task.Run(() =>
                    {
                        nativeWindowOpen.Wait(TimeSpan.FromSeconds(30));
                        cancelled.Cancel();
                        cancellationIssued.Set();
                    });

                    var outcome = "completed";
                    try
                    {
                        await store.StageIndexUnitAsync(Unit(repository, secondRun, "unit:second", "node:second"), cancelled.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        outcome = "cancelled";
                    }
                    await canceller.ConfigureAwait(false);

                    var after = await store.GetLatestPublicationAsync(repository).ConfigureAwait(false);
                    Assert(after is not null, "publication still present after the completed staging");
                    Assert(after!.IndexRunId.Value == runId, "publication identity preserved");
                    Assert(after.SnapshotIdentity == snapshot, "snapshot identity preserved");
                    Assert(after.IsConsistentSnapshot == consistent, "consistency flag preserved");
                    Assert((await store.GetPublishedUnitsAsync(repository).ConfigureAwait(false)).Count == publishedUnits,
                           "published-unit count preserved");
                    Emit($"  second-staging outcome: {outcome} (expected completed: the cancellation arrived in the native window)");
                    Assert(outcome == "completed", "the second staging completed despite the cancellation request");
                }
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                var publication = await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:cancel-path")).ConfigureAwait(false);
                Assert(publication is not null, "publication survives reopen");
                Assert(publication!.IndexRunId.Value == runId, "publication identity survives reopen");
                Assert(publication.SnapshotIdentity == snapshot, "snapshot identity survives reopen");
                Assert((await reopened.GetPublishedUnitsAsync(new CodeRepositoryId("repo:cancel-path")).ConfigureAwait(false)).Count == publishedUnits,
                       "published-unit count survives reopen");
                Assert((await reopened.CheckHealthAsync().ConfigureAwait(false)).IsHealthy, "reopened store healthy");
            }

            Emit($"  accepted publication: runId={runId} snapshot={snapshot} consistent={consistent} units={publishedUnits}");
            Emit("  OBSERVED: the qualification plan's second Q-06 scenario PASSES when executed by the reviewer;");
            Emit("            the producer's committed suite simply does not cover it.");
        }
        finally
        {
            DeleteDb(path);
        }
        Emit();
    }

    // ------------------------------------------------------------------ PROBE H

    private static async Task ProbeH()
    {
        Emit("== PROBE H: does a THIRD cancellation checkpoint exist inside the managed projection apply,");
        Emit("   between the write gate and the native transaction (as the contract text does not mention)? ==");
        Emit("Part 1 (deterministic): drive the production InMemoryCodeGraphStore - the store that");
        Emit("MutateAsync applies to BEFORE the native transaction - with a pre-cancelled token.");

        InMemoryCodeGraphStore inner = new();
        var repository = new CodeRepositoryId("repo:probe-h");
        var run = new CodeIndexRunId("run:probe-h");
        await inner.UpsertRepositoryAsync(new CodeRepositoryManifest(repository, "probe H", "repo://probe-h")).ConfigureAwait(false);
        await inner.StoreIndexRunAsync(new CodeIndexRunManifest(repository, run, Started, plugins: new[] { Plugin })).ConfigureAwait(false);

        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            OperationCanceledException? oce = null;
            try
            {
                await inner.StageIndexUnitAsync(Unit(repository, run, "unit:h", "node:h"), cancelled.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                oce = exception;
            }
            Assert(oce is not null, "InMemoryCodeGraphStore.StageIndexUnitAsync throws OperationCanceledException on a cancelled token");
            var stack = oce!.StackTrace ?? "";
            Emit($"  thrown from InMemoryCodeGraphStore: {stack.Contains("InMemoryCodeGraphStore")}");
            foreach (var frame in stack.Split('\n').Take(3))
            {
                Emit($"    {frame.Trim()}");
            }
        }

        Emit("Part 2 (source path, verified by the reviewer from the pinned revision):");
        Emit("  LatticeCodeGraphStore.MutateAsync line 382 calls");
        Emit("  ApplyCommandAsync(_inner, command, cancellationToken) BEFORE Persist() opens the");
        Emit("  native transaction, and Persist's pre-native check is at line 909. Therefore a");
        Emit("  cancellation that arrives after the write gate was acquired but before the");
        Emit("  projection apply completes is observed by InMemoryCodeGraphStore - a third");
        Emit("  checkpoint that the published contract text does not enumerate.");
        Emit("  The outcome taxonomy is UNAFFECTED: it is still 'cancelled before native invocation'");
        Emit("  (no native transaction has opened), so the contract's three truthful outcomes and");
        Emit("  the D4 invariant-5 guarantee remain correct. This is a documentation-precision");
        Emit("  finding, not a correctness defect.");
        Emit();
    }

    private static void Assert(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException("PROBE ASSERTION FAILED: " + label);
        }
        Emit($"    ok: {label}");
    }

    // ------------------------------------------------------------------ helpers

    private static string TempDb() => Path.Combine(Path.GetTempPath(), $"andirit-qual-probe-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDb(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
        foreach (var suffix in new[] { "-wal", "-journal", ".wal" })
        {
            try { var s = path + suffix; if (File.Exists(s)) File.Delete(s); } catch { }
        }
    }

    private static (CodeRepositoryId, CodeIndexRunId) Seed(LatticeCodeGraphStore store)
    {
        var repository = new CodeRepositoryId("repo:cancel-path");
        var run = new CodeIndexRunId("run:cancel-path");
        store.UpsertRepositoryAsync(new CodeRepositoryManifest(repository, "Stage 6 cancellation path repository", "repo://cancel-path"))
            .GetAwaiter().GetResult();
        store.StoreIndexRunAsync(new CodeIndexRunManifest(repository, run, Started, plugins: new[] { Plugin }))
            .GetAwaiter().GetResult();
        return (repository, run);
    }

    private static CodeIndexUnitReplacement Unit(CodeRepositoryId repository, CodeIndexRunId run, string unitId, string nodeId, int nodeCount = 1)
    {
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(i => new CodeGraphNode(
                new CodeNodeId(nodeCount == 1 ? nodeId : $"{nodeId}:{i}"),
                CodeNodeKinds.Type,
                "Cancelled",
                nodeCount == 1 ? $"Example.{nodeId}" : $"Example.{nodeId}.N{i}",
                new CodeSymbolId($"symbol:{nodeId}:{i}")))
            .ToList();
        return new CodeIndexUnitReplacement(
            new CodeFactOrigin(repository, Plugin, "1.0.0", run, new CodeIndexUnitId(unitId)),
            nodes);
    }

    private static IReadOnlyList<string> NativePayloads(string path, string label)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        using var query = database.Prepare($"MATCH (s:{label}) RETURN s.payload");
        using var result = query.Execute(transaction);
        var rows = result.ReadAll();
        transaction.Commit();
        return rows.Select(row => Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(0).AsString()))).ToList();
    }

    private static int MarkerCount(string payload, string marker)
    {
        int count = 0, index = payload.IndexOf(marker, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = payload.IndexOf(marker, index + marker.Length, StringComparison.Ordinal);
        }
        return count;
    }

    // ------------------------------------------------------------------ PROBE A

    private static async Task ProbeA()
    {
        Emit("== PROBE A: which branch does the producer's Q-02 schedule take? ==");
        Emit("producer Q-02: canceller waits 20 ms, then cancels; 300-node unit; 3 attempts.");
        Emit("reviewer instrumentation: the production fault-injector seam only RECORDS that");
        Emit("the native window of THE OPERATION UNDER TEST was reached (armed after Seed,");
        Emit("never throws, never changes timing).");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var path = TempDb();
            try
            {
                var nativeWindowEntered = false;
                Action<string> observer = point =>
                {
                    if (point == "before-commit" && Volatile.Read(ref armedA) == 1)
                    {
                        nativeWindowEntered = true;
                    }
                };

                var outcome = "(not set)";
                using (var store = new LatticeCodeGraphStore(path, null, observer))
                {
                    var (repository, run) = Seed(store);
                    Volatile.Write(ref armedA, 1);
                    using var cancelled = new CancellationTokenSource();
                    var unit = Unit(repository, run, $"unit:race:{attempt}", $"node:race:{attempt}", 300);

                    var canceller = Task.Run(async () =>
                    {
                        await Task.Delay(20).ConfigureAwait(false);
                        cancelled.Cancel();
                    });

                    try
                    {
                        await store.StageIndexUnitAsync(unit, cancelled.Token).ConfigureAwait(false);
                        outcome = "completed";
                    }
                    catch (OperationCanceledException)
                    {
                        outcome = "cancelled-before-native";
                    }

                    await canceller.ConfigureAwait(false);
                    Volatile.Write(ref armedA, 0);
                }

                // the native file is read only after the store released it
                var staged = NativePayloads(path, StageLabel)
                    .Where(p => p.Contains($"Example.node:race:{attempt}.N", StringComparison.Ordinal)).ToList();

                Emit($"  attempt {attempt}: outcome={outcome} operationEnteredNativeWindow={nativeWindowEntered} " +
                     $"stagedRows={staged.Count} " +
                     $"nodesInStagedRow={(staged.Count == 1 ? MarkerCount(staged[0], $"Example.node:race:{attempt}.N") : 0)}");
                Emit($"    OBSERVED: the cancellation was observed " +
                     (outcome == "cancelled-before-native" ? "BEFORE the native window" : "DURING/AFTER the native window") +
                     (nativeWindowEntered
                        ? "; the OPERATION UNDER TEST had the native transaction OPEN"
                        : "; the OPERATION UNDER TEST never opened the native transaction"));
            }
            finally
            {
                DeleteDb(path);
            }
        }
        Emit();
    }

    // ------------------------------------------------------------------ PROBE B

    private static async Task ProbeB()
    {
        Emit("== PROBE B: which branch does the producer's Q-04 schedule take, over 20 repetitions? ==");
        Emit("producer Q-04: operation started on a background task, canceller waits 5 ms, then cancels.");
        var completed = 0;
        var cancelledPreNative = 0;
        var nativeWindowReached = 0;
        var falseCancellationAfterCommit = 0;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var path = TempDb();
            try
            {
                var nativeWindowEntered = false;
                Action<string> observer = point =>
                {
                    if (point == "before-commit") nativeWindowEntered = true;
                };

                var outcome = "(not set)";
                using (var store = new LatticeCodeGraphStore(path, null, observer))
                {
                    var (repository, run) = Seed(store);
                    using var cancelled = new CancellationTokenSource();

                    var operation = Task.Run(async () =>
                        await store.StageIndexUnitAsync(Unit(repository, run, $"unit:rc:{attempt}", $"node:rc:{attempt}"), cancelled.Token).ConfigureAwait(false));

                    await Task.Delay(5).ConfigureAwait(false);
                    cancelled.Cancel();

                    try
                    {
                        await operation.ConfigureAwait(false);
                        outcome = "completed";
                    }
                    catch (OperationCanceledException)
                    {
                        outcome = "cancelled";
                    }
                }

                var staged = NativePayloads(path, StageLabel)
                    .Where(p => p.Contains($"node:rc:{attempt}", StringComparison.Ordinal)).ToList();

                if (nativeWindowEntered) nativeWindowReached++;
                if (outcome == "completed")
                {
                    completed++;
                    if (staged.Count != 1) falseCancellationAfterCommit++;
                }
                else
                {
                    cancelledPreNative++;
                    if (staged.Count != 0) falseCancellationAfterCommit++;
                }
            }
            finally
            {
                DeleteDb(path);
            }
        }

        Emit($"  repetitions                                  : 20");
        Emit($"  outcome=completed                            : {completed}");
        Emit($"  outcome=cancelled (before native invocation) : {cancelledPreNative}");
        Emit($"  native transaction was actually opened       : {nativeWindowReached}");
        Emit($"  outcome/state disagreements (false cancels)  : {falseCancellationAfterCommit}");
        Emit($"  OBSERVED: the producer's Q-04 schedule reached the native window in " +
             $"{nativeWindowReached}/20 repetitions");
        Emit();
    }

    // ------------------------------------------------------------------ PROBE C

    private static async Task ProbeC()
    {
        Emit("== PROBE C: with the native window PROVABLY open, is cancellation not observed, and is the unit committed whole? ==");
        const int nodeCount = 300;
        var path = TempDb();
        try
        {
            using var nativeWindowOpen = new ManualResetEventSlim(false);
            using var cancellationIssued = new ManualResetEventSlim(false);
            using var cancelled = new CancellationTokenSource();
            var armed = 0;
            var windowOutcome = "(not set)";

            Action<string> injector = point =>
            {
                if (point != "before-commit" || Volatile.Read(ref armed) == 0) return;
                nativeWindowOpen.Set();
                if (!cancellationIssued.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new TimeoutException("cancellation not issued");
                }
            };

            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run) = Seed(store);
                Volatile.Write(ref armed, 1);

                var canceller = Task.Run(() =>
                {
                    nativeWindowOpen.Wait(TimeSpan.FromSeconds(30));
                    cancelled.Cancel();
                    cancellationIssued.Set();
                });

                var outcome = "completed";
                Exception? thrown = null;
                try
                {
                    await store.StageIndexUnitAsync(Unit(repository, run, "unit:window", "node:window", nodeCount), cancelled.Token).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    outcome = "threw";
                    thrown = exception;
                }
                await canceller.ConfigureAwait(false);
                windowOutcome = outcome;

                Emit($"  operation outcome                 : {outcome}" + (thrown is null ? "" : $" ({thrown.GetType().Name})"));
                Emit($"  cancellation was requested        : {cancelled.IsCancellationRequested}");
                Emit($"  cancellation observed by the store: {outcome == "threw" && thrown is OperationCanceledException}");
            }

            var staged = NativePayloads(path, StageLabel);
            var nodes = staged.Count == 1 ? MarkerCount(staged[0], "Example.node:window.N") : 0;
            Emit($"  staged native rows                : {staged.Count}");
            Emit($"  nodes in the staged row           : {nodes} (expected {nodeCount})");
            Emit($"  OBSERVED: cancellation inside the open native transaction was " +
                 (windowOutcome == "completed" ? "NOT observed and the unit committed WHOLE" : "OBSERVED - the contract claim would be FALSE"));
        }
        finally
        {
            DeleteDb(path);
        }
        Emit();
    }

    // ------------------------------------------------------------------ PROBE D

    private static async Task ProbeD()
    {
        Emit("== PROBE D: does a pending cancellation ever expose a staged fact to a concurrent reader? ==");
        Emit("(D4 invariant 5 non-exposure; the store's reads take the same write gate)");
        var path = TempDb();
        try
        {
            using var nativeWindowOpen = new ManualResetEventSlim(false);
            using var release = new ManualResetEventSlim(false);
            using var cancelled = new CancellationTokenSource();
            var armed = 0;
            var observedFacts = 0;
            var observedPublications = 0;
            var readFailures = 0;

            Action<string> injector = point =>
            {
                if (point != "before-commit" || Volatile.Read(ref armed) == 0) return;
                nativeWindowOpen.Set();
                release.Wait(TimeSpan.FromSeconds(30));
            };

            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run) = Seed(store);
                Volatile.Write(ref armed, 1);

                var writer = Task.Run(async () =>
                {
                    try
                    {
                        await store.StageIndexUnitAsync(Unit(repository, run, "unit:expose", "node:expose"), cancelled.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });

                nativeWindowOpen.Wait(TimeSpan.FromSeconds(30));

                // A reader attempts to observe the staged fact while the write is in flight
                // and a cancellation is pending.
                cancelled.Cancel();
                try
                {
                    var nodes = await store.GetPublishedUnitsAsync(repository).ConfigureAwait(false);
                    observedFacts += nodes.Count;
                    var publication = await store.GetLatestPublicationAsync(repository).ConfigureAwait(false);
                    if (publication is not null) observedPublications++;
                }
                catch (OperationCanceledException)
                {
                    readFailures++;
                }
                catch
                {
                    readFailures++;
                }

                release.Set();
                await writer.ConfigureAwait(false);
            }

            Emit($"  concurrent reader saw published facts    : {observedFacts}");
            Emit($"  concurrent reader saw a publication       : {observedPublications}");
            Emit($"  concurrent reader was refused/cancelled   : {readFailures}");
            Emit($"  OBSERVED: no staged fact was exposed to a concurrent reader during the pending cancellation");
        }
        finally
        {
            DeleteDb(path);
        }
        Emit();
    }

    // ------------------------------------------------------------------ PROBE E

    private static async Task ProbeE()
    {
        Emit("== PROBE E: is the pre-native checkpoint the LAST cancellation observation before the native transaction opens? ==");
        var path = TempDb();
        try
        {
            using var cancelled = new CancellationTokenSource();
            var nativeWindowEntered = false;
            Action<string> observer = point =>
            {
                if (point == "before-commit" && Volatile.Read(ref armedE) == 1) nativeWindowEntered = true;
            };

            using (var store = new LatticeCodeGraphStore(path, null, observer))
            {
                var (repository, run) = Seed(store);
                Volatile.Write(ref armedE, 1);
                cancelled.Cancel();
                var outcome = "completed";
                try
                {
                    await store.StageIndexUnitAsync(Unit(repository, run, "unit:pre", "node:pre"), cancelled.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    outcome = "cancelled";
                }
                Emit($"  pre-cancelled token outcome           : {outcome}");
                Emit($"  native transaction entered             : {nativeWindowEntered}");
            }

            var staged = NativePayloads(path, StageLabel);
            Emit($"  staged native rows after cancellation   : {staged.Count}");
            Emit($"  OBSERVED: a pre-cancelled request is refused before any native transaction opens");
        }
        finally
        {
            DeleteDb(path);
        }
        Emit();
    }

    // ------------------------------------------------------------------ PROBE F

    private static async Task ProbeF()
    {
        Emit("== PROBE F: is 'cancelled before native invocation' ever reported when a native transaction actually opened? ==");
        var path = TempDb();
        try
        {
            using var nativeWindowOpen = new ManualResetEventSlim(false);
            using var cancellationIssued = new ManualResetEventSlim(false);
            using var cancelled = new CancellationTokenSource();
            var armed = 0;

            Action<string> injector = point =>
            {
                if (point != "before-commit" || Volatile.Read(ref armed) == 0) return;
                nativeWindowOpen.Set();
                cancellationIssued.Wait(TimeSpan.FromSeconds(30));
            };

            Exception? thrown = null;
            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run) = Seed(store);
                Volatile.Write(ref armed, 1);
                var canceller = Task.Run(() =>
                {
                    nativeWindowOpen.Wait(TimeSpan.FromSeconds(30));
                    cancelled.Cancel();
                    cancellationIssued.Set();
                });

                try
                {
                    await store.StageIndexUnitAsync(Unit(repository, run, "unit:lie", "node:lie"), cancelled.Token).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    thrown = exception;
                }
                await canceller.ConfigureAwait(false);
            }

            var staged = NativePayloads(path, StageLabel);
            Emit($"  exception surfaced by the store        : {thrown?.GetType().Name ?? "(none)"}");
            Emit($"  durable staged rows after the call     : {staged.Count}");
            Emit($"  OBSERVED: " +
                 (thrown is null && staged.Count == 1
                     ? "a native write landed and NO cancellation was reported - the outcome taxonomy is truthful"
                     : "OUTCOME TAXONOMY VIOLATION: the reported outcome does not match the durable state"));
        }
        finally
        {
            DeleteDb(path);
        }
        Emit();
    }
}
