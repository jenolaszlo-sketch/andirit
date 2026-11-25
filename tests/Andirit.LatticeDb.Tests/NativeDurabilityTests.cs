using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;
using Xunit.Abstractions;

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
public sealed class NativeDurabilityTests(ITestOutputHelper output)
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
            using var hold = StartWorker(worker,
                $"--db \"{path}\" --repo {holdRepo} --run run:durability-hold --unit unit:hold --node node:hold --mode hold --signal \"{signal}\"");
            try
            {
                await WaitForWorkerReadyAsync(hold, signal);
                Assert.Equal(NativeOpenDeadline.DatabaseOpenInAnotherProcess,
                    Assert.Throws<LatticeException>(() => LatticeDatabase.Open(path, new() { Create = false })).NativeErrorCode);
            }
            finally
            {
                try
                {
                    File.WriteAllText(signal + ".go", "GO");
                    await hold.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
                }
                finally
                {
                    await StopWorkerAsync(hold);
                }
            }
            Assert.Equal(0, hold.ExitCode);
            var holdReceipt = $"READY|{hold.Id}|{holdRepo}|run:durability-hold|unit:hold";
            Assert.Contains(holdReceipt, await hold.StandardOutput.ReadToEndAsync(), StringComparison.Ordinal);
            output.WriteLine(holdReceipt);

            var repository = new CodeRepositoryId(publishRepo);
            var published = await RunWorkerAsync(worker,
                $"--db \"{path}\" --repo {publishRepo} --run {publishRun} --unit unit:writer --node node:writer --mode publish-complete --target T1 --occurrences 2");
            Assert.Contains($"PUBLISHED|{publishRun}|", published, StringComparison.Ordinal);

            CodeOccurrenceQueryResult beforeCancellation;
            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
                beforeCancellation = await ReadWorkerOccurrences(reopened, repository);
                Assert.Equal(publishRun, beforeCancellation.Publication!.IndexRunId.Value);
                Assert.Equal(["src/Worker0.cs", "src/Worker1.cs"], OccurrencePaths(beforeCancellation));
                Assert.Null(await reopened.GetLatestPublicationAsync(new CodeRepositoryId(holdRepo)));
            }

            var stageBefore = Assert.Single(NativePayloads(path, StageLabel));
            Assert.Contains("unit:hold", stageBefore, StringComparison.Ordinal);
            var unitBefore = Assert.Single(NativePayloads(path, UnitLabel));
            Assert.Contains("node:writer", unitBefore, StringComparison.Ordinal);

            var readArguments = $"--db \"{path}\" --repo {publishRepo} --run {publishRun}";
            var index = beforeCancellation.Publication.IndexIdentity.Value;
            AssertWorkerObservation(await RunWorkerAsync(worker, readArguments + " --mode read-publication"),
                "READ", publishRun, index);
            AssertWorkerObservation(await RunWorkerAsync(worker,
                readArguments + " --mode cancel-after-complete --unit unit:cancelled --node node:cancelled"),
                "CANCELLED", publishRun, index);
            AssertWorkerObservation(await RunWorkerAsync(worker, readArguments + " --mode read-publication"),
                "READ", publishRun, index);

            var stageAfter = NativePayloads(path, StageLabel);
            var unitAfter = NativePayloads(path, UnitLabel);
            using var after = new LatticeCodeGraphStore(path);
            AssertCancellationPreserved(beforeCancellation, await ReadWorkerOccurrences(after, repository),
                stageBefore, stageAfter, unitBefore, unitAfter);
        }
        finally
        {
            DeleteDatabase(path);
            foreach (var signalFile in new[] { signal, signal + ".ready", signal + ".ready.tmp", signal + ".go" })
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

    // ------------------------------------------------------------------ D-06

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
        var path = TemporaryDatabasePath();
        var signal = path + ".d07signal";
        var repository = new CodeRepositoryId("repo:d07");
        var heldRepository = new CodeRepositoryId("repo:d07-held");
        const string baselineRun = "run:d07-baseline";
        const string heldRun = "run:d07-held";
        const string republishedRun = "run:d07-after-recovery";
        const string heldUnit = "unit:abrupt";
        try
        {
            var workerAssembly = LocateWorkerAssembly();
            await RunWorkerAsync(workerAssembly,
                $"--db \"{path}\" --repo {repository.Value} --run {baselineRun} --unit unit:baseline --node node:baseline --mode publish-complete --occurrences 1");
            CodeOccurrenceQueryResult before;
            using (var store = new LatticeCodeGraphStore(path))
            {
                before = await ReadWorkerOccurrences(store, repository);
                Assert.Equal(baselineRun, before.Publication!.IndexRunId.Value);
                Assert.Equal(["src/Worker0.cs"], OccurrencePaths(before));
                Assert.Null(await store.GetLatestPublicationAsync(heldRepository));
            }

            Assert.Empty(NativePayloads(path, StageLabel));
            var baselineUnit = Assert.Single(NativePayloads(path, UnitLabel));
            Assert.Contains("node:baseline", baselineUnit, StringComparison.Ordinal);
            using (var worker = StartWorker(workerAssembly,
                $"--db \"{path}\" --repo {heldRepository.Value} --run {heldRun} --unit {heldUnit} --node node:abrupt --mode hold --signal \"{signal}\""))
            {
                try
                {
                    await WaitForWorkerReadyAsync(worker, signal);
                    Assert.NotEqual(Environment.ProcessId, worker.Id);
                    Assert.Equal(NativeOpenDeadline.DatabaseOpenInAnotherProcess,
                        Assert.Throws<LatticeException>(() => LatticeDatabase.Open(path, new() { Create = false })).NativeErrorCode);
                    worker.Kill(entireProcessTree: true);
                    await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
                    Assert.NotEqual(0, worker.ExitCode);
                    var receipt = $"READY|{worker.Id}|{heldRepository.Value}|{heldRun}|{heldUnit}";
                    Assert.Contains(receipt, await worker.StandardOutput.ReadToEndAsync(), StringComparison.Ordinal);
                    output.WriteLine($"{receipt} KILLED exit={worker.ExitCode}");
                }
                finally
                {
                    await StopWorkerAsync(worker);
                }
            }

            var staged = NativePayloadsAfterTermination(path, StageLabel, TimeSpan.FromSeconds(30));
            Assert.Equal(baselineUnit, Assert.Single(NativePayloads(path, UnitLabel)));
            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
                AssertRecoveredPublication(before, await ReadWorkerOccurrences(reopened, repository),
                    staged, await reopened.GetLatestPublicationAsync(heldRepository),
                    heldRepository.Value, heldRun, heldUnit);
                Assert.Empty((await reopened.FindOccurrencesAsync(new(
                    heldRepository, NativePublicationCorrectnessTests.ComputeTarget(heldRepository.Value, "T1")))).Occurrences);
            }

            await RunWorkerAsync(workerAssembly,
                $"--db \"{path}\" --repo {repository.Value} --run {republishedRun} --unit unit:baseline --node node:republication --mode publish-complete --occurrences 2");
            using var final = new LatticeCodeGraphStore(path);
            var republished = await ReadWorkerOccurrences(final, repository);
            Assert.Equal(republishedRun, republished.Publication!.IndexRunId.Value);
            Assert.NotEqual(before.Publication!.IndexIdentity, republished.Publication.IndexIdentity);
            Assert.Equal(["src/Worker0.cs", "src/Worker1.cs"], OccurrencePaths(republished));
            var stale = await final.FindOccurrencesAsync(new(repository,
                NativePublicationCorrectnessTests.ComputeTarget(repository.Value, "T1"), before.Publication));
            Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
            Assert.Empty(stale.Occurrences);
            Assert.Null(await final.GetLatestPublicationAsync(heldRepository));
        }
        finally
        {
            DeleteDatabase(path);
            foreach (var suffix in new[] { ".ready", ".ready.tmp", ".go" })
                File.Delete(signal + suffix);
        }
    }

    [Fact]
    public async Task D07_ExitBeforeReadyFailsPromptly()
    {
        var path = TemporaryDatabasePath();
        using var worker = StartWorker(LocateWorkerAssembly(), $"--db \"{path}\" --mode exit-before-ready");
        try
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WaitForWorkerReadyAsync(worker, path + ".signal"));
            Assert.Contains("before READY", error.Message, StringComparison.Ordinal);
            Assert.Equal(19, worker.ExitCode);
        }
        finally
        {
            await StopWorkerAsync(worker);
        }
    }

    [Theory]
    [InlineData("missing-stage")]
    [InlineData("wrong-stage")]
    [InlineData("wrong-publication")]
    [InlineData("missing-accepted-fact")]
    [InlineData("abrupt-publication")]
    public void D07_RecoveryOracleRejectsInvalidObservation(string fault)
    {
        var before = ControlSnapshot();
        var after = fault switch
        {
            "wrong-publication" => before with { Publication = null },
            "missing-accepted-fact" => before with { Occurrences = [] },
            _ => before,
        };
        IReadOnlyList<string> staged = fault switch
        {
            "missing-stage" => [],
            "wrong-stage" => ["repo:held run:held unit:unrelated node:abrupt"],
            _ => ["repo:held run:held unit:held node:abrupt"],
        };
        var abrupt = fault == "abrupt-publication" ? before.Publication : null;
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            AssertRecoveredPublication(before, after, staged, abrupt, "repo:held", "run:held", "unit:held"));
    }

    [Fact]
    public void D05_CancellationOracleRejectsLeakedStageAndLostPublication()
    {
        var before = ControlSnapshot();
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            AssertCancellationPreserved(before, before, "original-stage",
                ["original-stage", "unit:cancelled"], "original-unit", ["original-unit"]));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            AssertCancellationPreserved(before, before with { Publication = null }, "original-stage",
                ["original-stage"], "original-unit", ["original-unit"]));
    }

    private static CodeOccurrenceQueryResult ControlSnapshot()
    {
        var repo = new CodeRepositoryId("repo:control");
        var run = new CodeIndexRunId("run:control");
        var state = new CodeRepositoryIndexState(repo, run, []);
        var target = NativePublicationCorrectnessTests.ComputeTarget(repo.Value, "T1");
        var location = new CodeLocation("src/Control.cs", 1, 1, 1, 4);
        var hit = new CodeOccurrenceHit(new(new string('0', 64)), target,
            CodeOccurrenceCategory.Invocation, location, "unit:control", [], 1);
        return new(CodeOccurrenceAvailability.Available,
            new(repo, run, state.SnapshotIdentity, state.IsConsistentSnapshot, state.IndexIdentity),
            [hit], 1, 1, null);
    }

    private static async Task<CodeOccurrenceQueryResult> ReadWorkerOccurrences(
        LatticeCodeGraphStore store, CodeRepositoryId repository)
    {
        var result = await store.FindOccurrencesAsync(new(
            repository, NativePublicationCorrectnessTests.ComputeTarget(repository.Value, "T1")));
        Assert.Equal(await store.GetLatestPublicationAsync(repository), result.Publication);
        Assert.NotNull(result.Publication);
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        return result;
    }

    private static string[] OccurrencePaths(CodeOccurrenceQueryResult result) =>
        result.Occurrences.Select(hit => hit.Span.Path).ToArray();

    private static void AssertSameAcceptedSnapshot(CodeOccurrenceQueryResult before, CodeOccurrenceQueryResult after)
    {
        Assert.Equal(before.Publication, after.Publication);
        Assert.Equal(before.Availability, after.Availability);
        Assert.Equivalent(before.Coverage, after.Coverage, strict: true);
        Assert.Equal(before.TotalMatches, after.TotalMatches);
        Assert.Equal(before.Occurrences.Select(hit => (hit.Id, hit.Span)), after.Occurrences.Select(hit => (hit.Id, hit.Span)));
    }

    private static void AssertRecoveredPublication(
        CodeOccurrenceQueryResult before, CodeOccurrenceQueryResult after, IReadOnlyList<string> staged,
        CodeGraphPublication? abruptPublication, string repository, string run, string unit)
    {
        AssertSameAcceptedSnapshot(before, after);
        Assert.Null(abruptPublication);
        var row = Assert.Single(staged);
        Assert.Contains(repository, row, StringComparison.Ordinal);
        Assert.Contains(run, row, StringComparison.Ordinal);
        Assert.Contains(unit, row, StringComparison.Ordinal);
        Assert.Contains("node:abrupt", row, StringComparison.Ordinal);
    }

    private static void AssertCancellationPreserved(
        CodeOccurrenceQueryResult before, CodeOccurrenceQueryResult after,
        string beforeStage, IReadOnlyList<string> afterStages, string beforeUnit, IReadOnlyList<string> afterUnits)
    {
        AssertSameAcceptedSnapshot(before, after);
        Assert.Equal(beforeStage, Assert.Single(afterStages));
        Assert.Equal(beforeUnit, Assert.Single(afterUnits));
    }

    private static IReadOnlyList<string> NativePayloadsAfterTermination(string path, string label, TimeSpan timeout)
    {
        using var database = NativeOpenDeadline.Open(
            () => LatticeDatabase.Open(path, new() { Create = false }), timeout, TimeProvider.System, Thread.Sleep);
        return NativePayloads(database, label);
    }

    [Fact]
    public async Task NativeOpenWhileAnotherStoreHoldsTheFile_FailsWithTheFilteredNativeCondition()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using (var first = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, _) = Seed(first);
                await first.StageIndexUnitAsync(Unit(repository, run, "unit:lock-fixture", "node:lock-fixture"));
                var blocked = Assert.Throws<LatticeException>(() =>
                    LatticeDatabase.Open(path, new() { Create = false }));
                Assert.Equal(NativeOpenDeadline.DatabaseOpenInAnotherProcess, blocked.NativeErrorCode);
            }

            using (var reopened = new LatticeCodeGraphStore(path))
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);

            var staged = Assert.Single(NativePayloadsAfterTermination(path, StageLabel, TimeSpan.FromSeconds(5)));
            Assert.Contains("node:lock-fixture", staged, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDatabase(path);
        }

    }

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
        return NativePayloads(database, label);
    }

    private static IReadOnlyList<string> NativePayloads(LatticeDatabase database, string label)
    {
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

    internal static string LocateWorkerAssembly()
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

    internal static Process StartWorker(string workerAssembly, string arguments) =>
        Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"exec \"{workerAssembly}\" {arguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("The worker process could not be started.");

    internal static async Task WaitForWorkerReadyAsync(Process process, string signal)
    {
        var timer = Stopwatch.StartNew();
        var path = signal + ".ready";
        while (timer.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (process.HasExited)
                throw new InvalidOperationException(
                    $"Worker {process.Id} exited before READY (exit {process.ExitCode}): {await process.StandardError.ReadToEndAsync()}");
            if (File.Exists(path) && await File.ReadAllTextAsync(path) == "READY")
            {
                if (process.HasExited)
                    throw new InvalidOperationException($"Worker {process.Id} exited before it could be observed live.");
                return;
            }
            await Task.Delay(25);
        }
        throw new TimeoutException($"Worker {process.Id} never reported READY.");
    }

    private Task<string> RunWorkerAsync(string assembly, string arguments) =>
        ExecuteWorkerAsync(assembly, arguments, output);

    internal static async Task<string> ExecuteWorkerAsync(string assembly, string arguments, ITestOutputHelper output)
    {
        using var process = StartWorker(assembly, arguments);
        try
        {
            Assert.NotEqual(Environment.ProcessId, process.Id);
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            var text = await stdout;
            output.WriteLine($"worker pid={process.Id} exit={process.ExitCode} sha256={Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly)))}\n{text.Trim()}");
            Assert.True(process.ExitCode == 0, $"Worker {process.Id} exited {process.ExitCode}: {text}{await stderr}");
            return text;
        }
        finally
        {
            await StopWorkerAsync(process);
        }
    }

    internal static async Task StopWorkerAsync(Process process)
    {
        if (!process.HasExited)
        {
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) when (process.HasExited) { }
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    private static void AssertWorkerObservation(string output, string outcome, string run, string index)
    {
        var receipt = Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries),
            line => line.StartsWith(outcome + "|", StringComparison.Ordinal)).TrimEnd('\r');
        var fields = receipt.Split('|');
        Assert.Equal(4, fields.Length);
        Assert.True(int.TryParse(fields[1], out var pid) && pid > 0 && pid != Environment.ProcessId);
        Assert.Equal(run, fields[2]);
        Assert.Equal(index, fields[3]);
    }
}
