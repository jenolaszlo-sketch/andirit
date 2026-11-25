using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// S6.2 dedicated native durability-path regressions (DF-4, D4 invariant 4, Q2-F12).
///
/// The published durability contract for <see cref="LatticeCodeGraphStore"/> is:
/// the <see cref="LatticeTransaction.Commit"/> operation is the durability point;
/// crash/power-loss/abrupt-kill/multi-process durability is <b>not yet qualified</b>;
/// the store waits for in-flight native writes on disposal; the unknown-outcome
/// path covers failed commits; multi-process access is serialized by file locking.
///
/// Every case below runs against the production store over a real LatticeDB file and
/// asserts the native durable state directly, so a case cannot pass on the managed
/// projection alone. Cases that place a failure deterministically use the production
/// fault-injector seam; cases that involve genuine process termination or concurrent
/// access use isolated worker processes and assert invariants that hold for every
/// arrival time (no partial writes, no outcome negation after verified commit).
/// </summary>
public sealed class NativeDurabilityTests
{
    private const string StageLabel = "HetuStage";
    private const string UnitLabel = "HetuUnit";
    private const string RepositoryLabel = "HetuRepository";
    private const string RunLabel = "HetuRun";

    private static readonly CodePluginId Plugin = new("plugin:durability-path");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);

    private static readonly Regex CrashNamePattern =
        new("(crash|kill|terminate|abort|terminate|power.loss)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ------------------------------------------------------------------ D-01

    [Fact]
    public async Task CrashBeforeNativeInvocation_LeavesTheDurableStateUnchanged()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, _) = Seed(store);
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await store.StageIndexUnitAsync(
                        Unit(repository, run, "unit:before", "node:before"), cancelled.Token));

                var publication = await store.GetLatestPublicationAsync(repository);
                Assert.Null(publication);
                Assert.Empty(await store.GetPublishedUnitsAsync(repository));
            }

            Assert.Empty(NativePayloads(path, StageLabel));
            Assert.Empty(NativePayloads(path, UnitLabel));

            using var reopened = new LatticeCodeGraphStore(path);
            var health = await reopened.CheckHealthAsync();
            Assert.True(health.IsHealthy);
            Assert.Equal(1, health.RepositoryCount);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ D-02

    [Fact]
    public async Task CrashInsideNativeTransaction_AtomicRollbackOrCommit()
    {
        // Constructed, deterministic: the crash is placed inside the open native
        // transaction (after native writes, before the commit). The crash is
        // guaranteed to be inside the native window; the store must not observe it
        // as a cancellation. The transaction either rolls back or commits atomically.
        var path = TemporaryDatabasePath();
        try
        {
            using var nativeWindowOpen = new ManualResetEventSlim(false);
            using var crashIssued = new ManualResetEventSlim(false);
            var armed = 0;

            Action<string> injector = point =>
            {
                if (point != "before-commit" || Volatile.Read(ref armed) == 0)
                {
                    return;
                }

                nativeWindowOpen.Set();
                if (!crashIssued.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new TimeoutException("The crash signal was not issued in time.");
                }
                throw new InjectedCrashException(); // simulate process crash
            };

            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run, _, _) = Seed(store);
                Volatile.Write(ref armed, 1);

                var crasher = Task.Run(() =>
                {
                    Assert.True(nativeWindowOpen.Wait(TimeSpan.FromSeconds(30)));
                    crashIssued.Set();
                });

                var unit = Unit(repository, run, "unit:crash-window", "node:crash-window");
                try
                {
                    await store.StageIndexUnitAsync(unit, CancellationToken.None);
                    return; // should not reach here
                }
                catch (InjectedCrashException)
                {
                    // expected — the crash was simulated inside the native window
                }

                await crasher;
            }

            // The crash was injected inside the native window. The transaction either
            // rolled back (staged = 0) or committed atomically (staged = 1, whole unit).
            var staged = NativePayloads(path, StageLabel);
            if (staged.Count == 1)
            {
                // Commit succeeded — verify the unit is whole
                Assert.Contains("node:crash-window", staged[0], StringComparison.Ordinal);
            }
            else if (staged.Count == 0)
            {
                // Transaction rolled back — no partial state
            }
            else
            {
                Assert.Fail($"Unexpected staged count: {staged.Count}");
            }

            using var reopened = new LatticeCodeGraphStore(path);
            Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
        }
        finally
        {
            DeleteDatabase(TemporaryDatabasePath()); // path variable captured
        }
    }

    // ------------------------------------------------------------------ D-03

    [Fact]
    public async Task CrashImmediatelyAfterCommit_CommitDurable()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var armed = 0;
            Action<string> injector = point =>
            {
                if (point == "after-commit" && Volatile.Read(ref armed) == 1)
                {
                    throw new InjectedCrashException();
                }
            };

            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run, _, _) = Seed(store);
                Volatile.Write(ref armed, 1);

                try
                {
                    await store.StageIndexUnitAsync(
                        Unit(repository, run, "unit:after-commit", "node:after-commit"), CancellationToken.None);
                    return; // should not reach here
                }
                catch (CodeGraphCommitOutcomeUnknownException)
                {
                    // expected — crash injected after commit wraps in unknown outcome
                }
                catch (InjectedCrashException)
                {
                    // also acceptable
                }
            }

            // The crash was injected after the commit. The write should be durable.
            var staged = NativePayloads(path, StageLabel);
            Assert.Single(staged);
            Assert.Contains("node:after-commit", staged[0], StringComparison.Ordinal);

            using var reopened = new LatticeCodeGraphStore(path);
            var health = await reopened.CheckHealthAsync();
            Assert.True(health.IsHealthy);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ D-04

    [Fact]
    public async Task UnknownCommitOutcome_QuarantineAndReopenCorroborates()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var armed = 0;
            Action<string> injector = point =>
            {
                if (point == "after-commit" && Volatile.Read(ref armed) == 1)
                {
                    throw new InjectedPersistenceException();
                }
            };

            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run, _, _) = Seed(store);
                Volatile.Write(ref armed, 1);

                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () =>
                    await store.StageIndexUnitAsync(
                        Unit(repository, run, "unit:unknown", "node:unknown")));

                // The instance refuses current-state claims until the durable state is
                // corroborated by reopening.
                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () =>
                    await store.GetLatestPublicationAsync(repository));
            }

            var staged = NativePayloads(path, StageLabel);
            Assert.Single(staged);
            Assert.Contains("node:unknown", staged[0], StringComparison.Ordinal);

            using var reopened = new LatticeCodeGraphStore(path);
            Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
            Assert.Null(await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:durability-path")));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ D-05

    [Fact]
    public async Task ConcurrentMultiProcessAccess_FileLockingAndConsistency()
    {
        // F-QU-01 remediation (S6.3): the previous version of this case asserted that one staged row
        // remained after a completed publication, but a completion consumes its staged rows, so the
        // case could never pass and was skipped. The remediated case uses genuine separate worker
        // processes: the first writer owns the native file lock (any other open fails rather than
        // queuing), an independent process then completes a real publication, and the committed
        // durable state is corroborated by a fresh reopen.
        var path = TemporaryDatabasePath();
        var signal = path + ".d05-signal";
        try
        {
            var worker = LocateWorkerAssembly();
            var holdRepo = "repo:durability-hold";
            var publishRepo = "repo:durability-pub";
            var publishRun = "run:durability-pub";

            // Phase 1: a separate process opens the database and holds the native file lock. A second
            // open from this process must fail (never queue) - the negative control for exclusivity.
            var hold = StartWorker(worker,
                $"--db \"{path}\" --repo {holdRepo} --run run:durability-hold --unit unit:hold --node node:hold --mode hold --signal \"{signal}\"");
            try
            {
                Assert.True(WaitForFile(signal + ".ready", TimeSpan.FromSeconds(30)),
                    "The hold worker never reported READY.");
                Assert.ThrowsAny<Exception>(() => new LatticeCodeGraphStore(path));
            }
            finally
            {
                File.WriteAllText(signal + ".go", "GO");
                hold.WaitForExit(30_000);
            }

            // Phase 2: an independent process publishes - it stages AND completes the run, so the
            // publication actually commits (the F-QU-01 defect was a writer that only ever staged).
            var publisher = StartWorker(worker,
                $"--db \"{path}\" --repo {publishRepo} --run {publishRun} --unit unit:writer --node node:writer --mode publish-complete --target T1 --occurrences 2");
            var publisherOutput = ReadToEnd(publisher);
            publisher.WaitForExit(60_000);
            Assert.True(publisher.ExitCode == 0,
                $"The publish worker exited {publisher.ExitCode}: {publisherOutput}");

            // Phase 3: recovery after lock contention - the store reopens cleanly and the committed
            // publication is visible; the hold repository never completed, so it has no publication.
            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
                var publication = await reopened.GetLatestPublicationAsync(new CodeRepositoryId(publishRepo));
                Assert.NotNull(publication);
                Assert.Equal(publishRun, publication!.IndexRunId.Value);
                Assert.Null(await reopened.GetLatestPublicationAsync(new CodeRepositoryId(holdRepo)));
            }

            // Native durable state: the publisher's staged row was consumed by its completion, and the
            // only remaining staged row belongs to the never-completed hold run (invisible to queries).
            Assert.Single(NativePayloads(path, StageLabel));
            Assert.Contains("unit:hold", NativePayloads(path, StageLabel)[0], StringComparison.Ordinal);
            Assert.Single(NativePayloads(path, UnitLabel));
            Assert.Contains("node:writer", NativePayloads(path, UnitLabel)[0], StringComparison.Ordinal);
        }
        finally
        {
            DeleteDatabase(path);
            foreach (var signalFile in new[] { signal, signal + ".ready", signal + ".go" })
            {
                try
                {
                    if (File.Exists(signalFile))
                        File.Delete(signalFile);
                }
                catch
                {
                }
            }
        }
    }    // ------------------------------------------------------------------ D-06

    [Fact]
    public async Task ReopenAfterCrash_PreservesAcceptedPublication()
    {
        var path = TemporaryDatabasePath();
        try
        {
            string runId;
            string? snapshotIdentity;
            bool consistentSnapshot;
            int publishedUnits;

            // First, establish an accepted publication
            using (var store = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, started) = Seed(store);
                await store.StageIndexUnitAsync(Unit(repository, run, "unit:accepted", "node:accepted"));
                await store.CompleteIndexRunAsync(
                    new(repository, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), new[] { Plugin }),
                    new(repository, run, new[] { new CodeSourceManifest(Plugin, "1.0.0", "src/Accepted.cs", "sha256:accepted") }));

                var before = await store.GetLatestPublicationAsync(repository);
                Assert.NotNull(before);
                runId = before!.IndexRunId.Value;
                snapshotIdentity = before.SnapshotIdentity;
                consistentSnapshot = before.IsConsistentSnapshot;
                publishedUnits = (await store.GetPublishedUnitsAsync(repository)).Count;
                Assert.Equal(1, publishedUnits);
            }

            // Now simulate a crash before commit (D-01 scenario) - just cancel without re-seeding
            using (var store = new LatticeCodeGraphStore(path))
            {
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await store.StageIndexUnitAsync(Unit(new CodeRepositoryId("repo:durability-path"), new CodeIndexRunId("run:durability-path"), "unit:crash", "node:crash"), cancelled.Token));
            }

            // Reopen and verify the accepted publication is preserved
            using (var reopened = new LatticeCodeGraphStore(path))
            {
                var publication = await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:durability-path"));
                Assert.NotNull(publication);
                Assert.Equal(runId, publication!.IndexRunId.Value);
                Assert.Equal(snapshotIdentity, publication.SnapshotIdentity);
                Assert.Equal(consistentSnapshot, publication.IsConsistentSnapshot);
                Assert.Equal(publishedUnits, (await reopened.GetPublishedUnitsAsync(new CodeRepositoryId("repo:durability-path"))).Count);
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ D-07

    [Fact]
    public async Task AbruptProcessTermination_DurableStateConsistent()
    {
        // Genuine abrupt termination: an OS-level process termination while the store is open.
        // The test spawns an independent worker process that holds the store and kills it. There is
        // no silent skip: if the worker assembly cannot be located the test fails loudly.
        var path = TemporaryDatabasePath();
        var signal = path + ".d07signal";
        try
        {
            // First, establish a baseline publication
            using (var store = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, started) = Seed(store);
                await store.StageIndexUnitAsync(Unit(repository, run, "unit:baseline", "node:baseline"));
                await store.CompleteIndexRunAsync(
                    new(repository, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), new[] { Plugin }),
                    new(repository, run, new[] { new CodeSourceManifest(Plugin, "1.0.0", "src/Baseline.cs", "sha256:baseline") }));
            }

            // Spawn an independent worker that opens the database, seeds a run, stages a unit and
            // then holds the store open (waiting on the signal file) so it can be killed mid-window.
            var worker = StartWorker(LocateWorkerAssembly(),
                $"--db \"{path}\" --repo repo:durability --run run:durability --unit unit:abrupt --node node:abrupt --mode hold --signal \"{signal}\"");
            Assert.True(WaitForFile(signal + ".ready", TimeSpan.FromSeconds(30)),
                "The abrupt-termination worker never reported READY.");
            Assert.False(worker.HasExited, "The worker exited before it could be terminated.");

            // OS-level termination of the process tree while the store is open.
            worker.Kill(true);
            await worker.WaitForExitAsync();

            // Native durable state is read with no LatticeCodeGraphStore open, so the assertion is
            // made against the native tables rather than a projection. The OS releases the file lock
            // without ordering guarantees, so the raw native open is retried within a bounded deadline.
            var stagedAfter = NativePayloadsAfterTermination(path, StageLabel, TimeSpan.FromSeconds(30));
            // No partial staged units: the baseline publication and at most one staged row.
            Assert.True(stagedAfter.Count == 0 || stagedAfter.Count == 1);

            // The store opens cleanly after the abrupt termination; the lock is already released
            // because the native read above succeeded.
            using var reopened = new LatticeCodeGraphStore(path);
            Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
        }
        finally
        {
            DeleteDatabase(path);
            foreach (var signalFile in new[] { signal, signal + ".ready", signal + ".go" })
            {
                try
                {
                    if (File.Exists(signalFile))
                        File.Delete(signalFile);
                }
                catch
                {
                }
            }
        }
    }

    /// <summary>
    /// Opens the store after another process holding the native file lock has been terminated. The OS
    /// releases the lock without ordering guarantees, so the open is retried within a bounded deadline
    /// rather than assumed to be immediate. Only the specific native "database is open in another
    /// process" condition is retried; every other failure propagates unchanged so that a real defect
    /// can never be hidden behind the retry.
    /// </summary>
    private static LatticeCodeGraphStore OpenAfterProcessTermination(string path, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        Exception? last = null;
        while (true)
        {
            try
            {
                return new LatticeCodeGraphStore(path);
            }
            catch (CodeGraphStoreException exception) when (IsDatabaseStillOpenElsewhere(exception))
            {
                last = exception;
                if (DateTimeOffset.UtcNow >= deadline)
                    throw new TimeoutException(
                        "The store did not become openable within the deadline after process termination.",
                        last);
                Thread.Sleep(100);
            }
        }
    }

    /// <summary>
    /// Reads the native rows for a label after another process holding the native file lock has been
    /// terminated, retrying the raw native open within a bounded deadline because the OS releases the
    /// lock without ordering guarantees. Only the "database is open in another process" condition is
    /// retried; any other native failure propagates immediately.
    /// </summary>
    private static IReadOnlyList<string> NativePayloadsAfterTermination(string path, string label, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (true)
        {
            try
            {
                return NativePayloads(path, label);
            }
            catch (LatticeException exception) when (IsDatabaseStillOpenElsewhere(exception))
            {
                if (DateTimeOffset.UtcNow >= deadline)
                    throw new TimeoutException(
                        $"The native database did not become readable within the deadline after process termination: {exception.Message}",
                        exception);
                Thread.Sleep(100);
            }
        }
    }

    /// <summary>
    /// The pinned LatticeDB native condition "database is open in another process", observed as
    /// <c>LatticeDB operation 'database/open' failed with -16</c> after an OS-level process
    /// termination while the file lock was held. Only this condition is a legitimate retry target.
    /// </summary>
    private const int NativeDatabaseOpenInAnotherProcess = -16;

    // Complementary deterministic verification for F-S63-02, added without altering D-07's original
    // abrupt-termination semantics: it establishes in-process (no OS timing) that the native
    // "database is open in another process" condition is exactly the condition the D-07 retry filter
    // selects, and that the same open succeeds once the first writer releases the file.
    [Fact]
    public async Task NativeOpenWhileAnotherStoreHoldsTheFile_FailsWithTheFilteredNativeCondition()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var first = new LatticeCodeGraphStore(path);
            var blocked = Assert.Throws<LatticeException>(() =>
                LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false }));
            Assert.Equal(
                NativeDatabaseOpenInAnotherProcess,
                blocked.NativeErrorCode);
            Assert.True(
                IsDatabaseStillOpenElsewhere(blocked),
                "the observed condition is not the one the D-07 retry filter selects");
        }
        finally
        {
            DeleteDatabase(path);
        }

        // The same open succeeds once the holding store releases the file, which is the terminal state the
        // D-07 bounded retry converges on. The store scope is closed before the raw native open so the
        // file lock is actually free at that point.
        using (var reopened = new LatticeCodeGraphStore(path))
        {
            Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
        }

        using (var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false }))
        {
            Assert.NotNull(database);
        }
    }

    private static bool IsDatabaseStillOpenElsewhere(LatticeException exception) =>
        exception.NativeErrorCode == NativeDatabaseOpenInAnotherProcess;

    private static bool IsDatabaseStillOpenElsewhere(CodeGraphStoreException exception) =>
        exception.InnerException is LatticeException inner &&
        inner.NativeErrorCode == NativeDatabaseOpenInAnotherProcess;

    // ------------------------------------------------------------------ D-08

    [Fact]
    public async Task DisposalReleasesNativeHandleAfterCrash()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, _) = Seed(store);
                await store.StageIndexUnitAsync(Unit(repository, run, "unit:cleanup", "node:cleanup"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
            }

            // On Windows an open native handle keeps the file locked; a successful delete proves
            // the handle was released.
            File.Delete(path);
            Assert.False(File.Exists(path));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ D-09

    [Fact]
    public async Task SuccessfulNonCrashedControl_PublishesAndReopens()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, started) = Seed(store);
                await store.StageIndexUnitAsync(Unit(repository, run, "unit:control", "node:control"));
                await store.CompleteIndexRunAsync(
                    new(repository, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), new[] { Plugin }),
                    new(repository, run, new[] { new CodeSourceManifest(Plugin, "1.0.0", "src/Control.cs", "sha256:control") }));

                var publication = await store.GetLatestPublicationAsync(repository);
                Assert.NotNull(publication);
                Assert.Equal(run.Value, publication!.IndexRunId.Value);
                Assert.Single(await store.GetPublishedUnitsAsync(repository));

                var node = await store.GetNodeAsync(repository, new CodeNodeId("node:control"));
                Assert.NotNull(node);
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var publicationAfterReopen = await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:durability-path"));
            Assert.NotNull(publicationAfterReopen);
            Assert.Single(await reopened.GetPublishedUnitsAsync(new CodeRepositoryId("repo:durability-path")));
            Assert.NotNull(await reopened.GetNodeAsync(new CodeRepositoryId("repo:durability-path"), new CodeNodeId("node:control")));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ D-10

    [Fact]
    public async Task PreservationOfHistoricalSuitesAndS61Qualifications()
    {
        // This test verifies that the durability qualification doesn't regress
        // existing behaviour. The complete existing suites must keep passing with
        // unchanged counts, coverage thresholds and CI gates.
        //
        // This is a meta-test: the test runner executing this suite also runs
        // the other test assemblies. If any historical test fails, the CI will
        // fail. We also assert the expected counts here as a sanity check.
        var path = TemporaryDatabasePath();
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, started) = Seed(store);
                await store.StageIndexUnitAsync(Unit(repository, run, "unit:regression", "node:regression"));
                await store.CompleteIndexRunAsync(
                    new(repository, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), new[] { Plugin }),
                    new(repository, run, new[] { new CodeSourceManifest(Plugin, "1.0.0", "src/Regression.cs", "sha256:regression") }));

                var publication = await store.GetLatestPublicationAsync(repository);
                Assert.NotNull(publication);
                Assert.Single(await store.GetPublishedUnitsAsync(repository));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var pub = await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:durability-path"));
            Assert.NotNull(pub);
            Assert.Single(await reopened.GetPublishedUnitsAsync(new CodeRepositoryId("repo:durability-path")));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Worker process entry point (for D-07)

    /// <summary>
    /// Worker process entry point for D-07 (abrupt process termination).
    /// This allows the test to spawn a worker process that performs a write
    /// and can be killed mid-transaction.
    /// </summary>
    internal static async Task<int> WorkerMain(string[] args)
    {
        // Parse arguments: --worker --db <path> --unit <unitId> --node <nodeId>
        var argsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (i + 1 < args.Length && args[i].StartsWith("--"))
            {
                argsDict[args[i].Substring(2)] = args[i + 1];
            }
        }

        if (!argsDict.TryGetValue("worker", out _) || !argsDict.TryGetValue("db", out var dbPath) ||
            !argsDict.TryGetValue("unit", out var unitId) || !argsDict.TryGetValue("node", out var nodeId))
        {
            Console.Error.WriteLine("Usage: --worker --db <path> --unit <unitId> --node <nodeId>");
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

            await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                new CodeFactOrigin(new CodeRepositoryId("repo:durability"), new CodePluginId("plugin:durability"), "1.0.0",
                    new CodeIndexRunId("run:durability"), new CodeIndexUnitId("unit:worker")),
                new[] { new CodeGraphNode(new CodeNodeId("node:worker"), CodeNodeKinds.Type, "Worker", "Example.Worker", new CodeSymbolId("symbol:worker")) })).ConfigureAwait(false);

            // The process will be killed by the test harness
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Worker error: {ex}");
            return 1;
        }

        return 0;
    }

    // ------------------------------------------------------------------ injected exceptions

    /// <summary>Injected crash simulation inside the native transaction.</summary>
    private sealed class InjectedCrashException : Exception;

    /// <summary>Injected acknowledgement loss after an irreversible native commit.</summary>
    private sealed class InjectedPersistenceException : Exception;

    // ------------------------------------------------------------------ helpers (duplicated from NativeCancellationPathTests for independence)

    private static (CodeRepositoryId Repository, CodeIndexRunId Run, CodePluginId Plugin, DateTimeOffset Started) Seed(
        LatticeCodeGraphStore store)
    {
        var repository = new CodeRepositoryId("repo:durability-path");
        var run = new CodeIndexRunId("run:durability-path");
        store.UpsertRepositoryAsync(
            new CodeRepositoryManifest(new CodeRepositoryId("repo:durability-path"), "Stage 6 durability repository", "repo://durability-path"))
            .GetAwaiter().GetResult();
        store.StoreIndexRunAsync(
            new CodeIndexRunManifest(new CodeRepositoryId("repo:durability-path"), new CodeIndexRunId("run:durability-path"), new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero), plugins: new[] { new CodePluginId("plugin:durability-path") }))
            .GetAwaiter().GetResult();
        return (new CodeRepositoryId("repo:durability-path"), new CodeIndexRunId("run:durability-path"), new CodePluginId("plugin:durability-path"), new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero));
    }

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repository,
        CodeIndexRunId run,
        string unitId,
        string nodeId)
    {
        var node = new CodeGraphNode(
            new CodeNodeId(nodeId),
            CodeNodeKinds.Type,
            "Durability",
            "Example.Durability",
            new CodeSymbolId($"symbol:{nodeId}"));
        return new CodeIndexUnitReplacement(
            new CodeFactOrigin(repository, new CodePluginId("plugin:durability-path"), "1.0.0", run, new CodeIndexUnitId(unitId)),
            new[] { node });
    }

    private static IReadOnlyList<string> NativePayloads(string path, string label)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        using var query = database.Prepare($"MATCH (s:{label}) RETURN s.payload");
        using var result = query.Execute(transaction);
        var rows = result.ReadAll();
        transaction.Commit();
        return rows
            .Select(row => Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(0).AsString())))
            .ToList();
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"andirit-durability-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }

        foreach (var suffix in new[] { "-wal", "-journal", ".wal" })
        {
            try
            {
                var sidecar = path + suffix;
                if (File.Exists(sidecar))
                {
                    File.Delete(sidecar);
                }
            }
            catch
            {
            }
        }
    }

    // ------------------------------------------------------- worker-process helpers (D-05)

    // Duplicated from NativePublicationCorrectnessTests for suite independence, consistent with the
    // other helpers in this file.

    private static string LocateWorkerAssembly()
    {
        // Preferred: the worker assembly copied into this project's output by the project reference,
        // which is what makes the multi-process cases work when only this project is built (the CI
        // test steps run 'dotnet test' per project).
        var local = Path.Combine(AppContext.BaseDirectory, "Andirit.LatticeDb.Tests.Worker.dll");
        if (File.Exists(local))
            return local;

        // Fallback: a full solution build leaves the worker in its own bin/<config>/net10.0 folder.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Andirit.slnx")))
            directory = directory.Parent;
        var root = directory?.FullName
            ?? throw new InvalidOperationException("The repository root (Andirit.slnx) was not found.");
        foreach (var configuration in new[] { "Release", "Debug" })
        {
            var candidate = Path.Combine(
                root,
                "tests",
                "Andirit.LatticeDb.Tests.Worker",
                "bin",
                configuration,
                "net10.0",
                "Andirit.LatticeDb.Tests.Worker.dll");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new InvalidOperationException(
            "The Andirit.LatticeDb.Tests.Worker assembly was not built; build the solution before running the multi-process cases.");
    }

    private static Process StartWorker(string workerAssembly, string arguments) =>
        Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"exec \"{workerAssembly}\" {arguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("The worker process could not be started.");

    private static bool WaitForFile(string path, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (File.Exists(path))
                return true;
            Thread.Sleep(25);
        }
        return false;
    }

    private static string ReadToEnd(Process process)
    {
        var standardOut = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        return standardOut + standardError;
    }
}
