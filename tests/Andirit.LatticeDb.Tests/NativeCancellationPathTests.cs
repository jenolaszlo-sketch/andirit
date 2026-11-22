using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// S6.1 dedicated native cancellation-path regressions (M9.5, M12.2, D4 invariant 5).
///
/// The published cancellation contract for <see cref="LatticeCodeGraphStore"/> is:
/// a cancellation is honoured while the operation waits for the managed write gate and
/// immediately before the native write transaction opens; it is <b>not</b> honoured inside the
/// native transaction, so an operation whose cancellation arrives during native execution
/// completes and commits atomically. The only truthful outcomes are: cancelled before native
/// invocation, completed, or commit outcome unknown.
///
/// Every case below runs against the production store over a real LatticeDB file and asserts the
/// native durable state directly, so a case cannot pass on the managed projection alone. Cases
/// that place a cancellation deterministically use the production fault-injector seam; cases that
/// race the native window use a concurrent canceller and assert invariants that hold for every
/// arrival time (never a partially applied write, never a cancellation reported after a
/// successful commit).
/// </summary>
public sealed class NativeCancellationPathTests
{
    private const string StageLabel = "HetuStage";
    private const string UnitLabel = "HetuUnit";
    private const string RepositoryLabel = "HetuRepository";
    private const string RunLabel = "HetuRun";

    private static readonly CodePluginId Plugin = new("plugin:cancel-path");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 22, 0, 0, 0, TimeSpan.Zero);

    private static readonly Regex CancellationNamePattern =
        new("(cancel|interrupt|abort|terminate|shutdown)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ------------------------------------------------------------------ Q-01

    [Fact]
    public async Task CancellationBeforeNativeInvocation_LeavesTheDurableStateUnchanged()
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

    // ------------------------------------------------------------------ Q-02

    [Fact]
    public async Task CancellationRacingTheNativeWindow_NeverExposesPartialState()
    {
        // Genuine (uncoordinated) arrival: a concurrent canceller cancels while the native write
        // is in flight. The assertions are invariants, not timing assumptions: the staged unit is
        // either entirely absent or entirely present, never partially applied, and nothing is
        // ever published by a cancelled staging.
        var path = TemporaryDatabasePath();
        const int nodeCount = 300;
        try
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                bool cancelledBeforeNative;
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var (repository, run, _, _) = Seed(store);
                    using var cancelled = new CancellationTokenSource();
                    var unit = LargeUnit(repository, run, $"unit:race:{attempt}", attempt, nodeCount);

                    var canceller = Task.Run(async () =>
                    {
                        await Task.Delay(20);
                        cancelled.Cancel();
                    });

                    cancelledBeforeNative = false;
                    try
                    {
                        await store.StageIndexUnitAsync(unit, cancelled.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelledBeforeNative = true;
                    }

                    await canceller;

                    var publication = await store.GetLatestPublicationAsync(repository);
                    Assert.Null(publication);
                    Assert.Empty(await store.GetPublishedUnitsAsync(repository));
                }

                // The native database file is read directly after the store released it, so the
                // assertion is made against the durable native state and not the projection.
                var staged = NativePayloads(path, StageLabel)
                    .Where(payload => payload.Contains(Marker(attempt), StringComparison.Ordinal))
                    .ToList();

                if (!cancelledBeforeNative)
                {
                    // The operation completed: the native write committed as a whole.
                    Assert.Single(staged);
                    Assert.Equal(nodeCount, MarkerCount(staged[0], attempt));
                }
                else
                {
                    // The operation was cancelled before the native transaction opened.
                    Assert.Empty(staged);
                }
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-03

    [Fact]
    public async Task CancellationInsideTheOpenNativeTransaction_IsNotObservedAndCommitsAtomically()
    {
        // Constructed, deterministic: the cancellation request is issued by a separate thread
        // while the native write transaction is open (after the native writes, before the commit).
        // The request is guaranteed to be inside the native window; the store must not observe it.
        var path = TemporaryDatabasePath();
        try
        {
            using var nativeWindowOpen = new ManualResetEventSlim(false);
            using var cancellationIssued = new ManualResetEventSlim(false);
            using var cancelled = new CancellationTokenSource();
            var armed = 0;

            Action<string> injector = point =>
            {
                if (point != "before-commit" || Volatile.Read(ref armed) == 0)
                {
                    return;
                }

                nativeWindowOpen.Set();
                if (!cancellationIssued.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new TimeoutException("The cancellation request was not issued in time.");
                }
            };

            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run, _, _) = Seed(store);
                Volatile.Write(ref armed, 1);

                var canceller = Task.Run(() =>
                {
                    Assert.True(nativeWindowOpen.Wait(TimeSpan.FromSeconds(30)));
                    cancelled.Cancel();
                    cancellationIssued.Set();
                });

                var unit = Unit(repository, run, "unit:window", "node:window");
                await store.StageIndexUnitAsync(unit, cancelled.Token);
                await canceller;

                // The cancellation was requested inside the native window and was not observed:
                // the operation completed and the native transaction committed.
                var publication = await store.GetLatestPublicationAsync(repository);
                Assert.Null(publication);
            }

            var staged = NativePayloads(path, StageLabel);
            Assert.Single(staged);
            Assert.Contains("node:window", staged[0], StringComparison.Ordinal);

            using var reopened = new LatticeCodeGraphStore(path);
            Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-04

    [Fact]
    public async Task CancellationRacingWithSuccessfulCompletion_NeverReportsCancellationAfterACommit()
    {
        // Genuine race, repeated: whatever moment the cancellation arrives, the operation either
        // reports cancellation before the native window (nothing durable) or completes (fully
        // durable). It must never report cancellation after a successful durable mutation.
        var path = TemporaryDatabasePath();
        try
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                bool cancelledBeforeNative;
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var (repository, run, _, _) = Seed(store);
                    using var cancelled = new CancellationTokenSource();

                    var operation = Task.Run(async () =>
                        await store.StageIndexUnitAsync(
                            Unit(repository, run, $"unit:race-complete:{attempt}", $"node:race-complete:{attempt}"),
                            cancelled.Token));

                    await Task.Delay(5);
                    cancelled.Cancel();

                    cancelledBeforeNative = false;
                    try
                    {
                        await operation;
                    }
                    catch (OperationCanceledException)
                    {
                        cancelledBeforeNative = true;
                    }

                    var publication = await store.GetLatestPublicationAsync(repository);
                    Assert.Null(publication);
                }

                // The durable native state is read after the store released the file.
                var staged = NativePayloads(path, StageLabel)
                    .Where(payload => payload.Contains($"node:race-complete:{attempt}", StringComparison.Ordinal))
                    .ToList();

                if (cancelledBeforeNative)
                {
                    Assert.Empty(staged);
                }
                else
                {
                    Assert.Single(staged);
                }
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-05

    [Fact]
    public async Task CancellationAfterTheIrreversibleCommit_DoesNotReportCancellation()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var cancelled = new CancellationTokenSource();
            var armed = 0;
            Action<string> injector = point =>
            {
                if (point == "after-commit" && Volatile.Read(ref armed) == 1)
                {
                    cancelled.Cancel();
                }
            };

            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run, _, _) = Seed(store);
                Volatile.Write(ref armed, 1);

                await store.StageIndexUnitAsync(
                    Unit(repository, run, "unit:after-commit", "node:after-commit"), cancelled.Token);

                // The commit already happened. The operation reports success: a durable mutation
                // must never be reported as cancelled.
                var publication = await store.GetLatestPublicationAsync(repository);
                Assert.Null(publication);
            }

            var staged = NativePayloads(path, StageLabel);
            Assert.Single(staged);
            Assert.Contains("node:after-commit", staged[0], StringComparison.Ordinal);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    /// <summary>
    /// Regression guard: a future cancellation checkpoint placed after a successful commit would
    /// turn a durable mutation into a reported cancellation. This test fails if such a checkpoint
    /// is ever introduced.
    /// </summary>
    [Fact]
    public async Task Regression_CancellationAfterASuccessfulCommit_MustNotSurfaceAsCancelled()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var cancelled = new CancellationTokenSource();
            var armed = 0;
            Action<string> injector = point =>
            {
                if (point == "after-commit" && Volatile.Read(ref armed) == 1)
                {
                    cancelled.Cancel();
                }
            };

            using (var store = new LatticeCodeGraphStore(path, null, injector))
            {
                var (repository, run, _, _) = Seed(store);
                Volatile.Write(ref armed, 1);

                var exception = await Record.ExceptionAsync(async () =>
                    await store.StageIndexUnitAsync(
                        Unit(repository, run, "unit:regression", "node:regression"), cancelled.Token));

                Assert.Null(exception);
                Assert.True(cancelled.IsCancellationRequested);
            }

            // The mutation is durable: the cancellation was reported to nobody and the write landed.
            var staged = NativePayloads(path, StageLabel);
            Assert.Single(staged);
            Assert.Contains("node:regression", staged[0], StringComparison.Ordinal);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-06

    [Fact]
    public async Task ReopenAfterACancelledStaging_PreservesTheAcceptedPublication()
    {
        var path = TemporaryDatabasePath();
        try
        {
            string runId;
            string? snapshotIdentity;
            bool consistentSnapshot;
            int publishedUnits;

            using (var store = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, started) = Seed(store);
                await store.StageIndexUnitAsync(
                    Unit(repository, run, "unit:accepted", "node:accepted"));
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

                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await store.StageIndexUnitAsync(
                        Unit(repository, run, "unit:cancelled-after-publication", "node:cancelled-after-publication"),
                        cancelled.Token));

                var after = await store.GetLatestPublicationAsync(repository);
                Assert.NotNull(after);
                Assert.Equal(runId, after!.IndexRunId.Value);
                Assert.Equal(snapshotIdentity, after.SnapshotIdentity);
                Assert.Equal(consistentSnapshot, after.IsConsistentSnapshot);
                Assert.Equal(publishedUnits, (await store.GetPublishedUnitsAsync(repository)).Count);
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var publication = await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:cancel-path"));
            Assert.NotNull(publication);
            Assert.Equal(runId, publication!.IndexRunId.Value);
            Assert.Equal(snapshotIdentity, publication.SnapshotIdentity);
            Assert.Equal(consistentSnapshot, publication.IsConsistentSnapshot);
            Assert.Equal(publishedUnits, (await reopened.GetPublishedUnitsAsync(new CodeRepositoryId("repo:cancel-path"))).Count);
            Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-07

    [Fact]
    public async Task ConcurrentCancellationAndDisposal_WaitsForTheInFlightNativeWrite()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var nativeWindowOpen = new ManualResetEventSlim(false);
            using var releaseNativeWindow = new ManualResetEventSlim(false);
            var armed = 0;

            Action<string> injector = point =>
            {
                if (point != "before-commit" || Volatile.Read(ref armed) == 0)
                {
                    return;
                }

                nativeWindowOpen.Set();
                if (!releaseNativeWindow.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new TimeoutException("The in-flight native write was not released in time.");
                }
            };

            var store = new LatticeCodeGraphStore(path, null, injector);
            var (repository, run, _, _) = Seed(store);
            Volatile.Write(ref armed, 1);

            // The in-flight write holds the managed write gate inside the native transaction.
            var inFlight = Task.Run(async () =>
                await store.StageIndexUnitAsync(Unit(repository, run, "unit:in-flight", "node:in-flight")));

            Assert.True(nativeWindowOpen.Wait(TimeSpan.FromSeconds(30)));

            // A second operation waits for the write gate and is cancelled while waiting. It is a
            // different unit from the in-flight write and must not be confused with it.
            using var cancelled = new CancellationTokenSource();
            var waiting = Task.Run(async () =>
            {
                await Task.Delay(50);
                cancelled.Cancel();
                await store.StageIndexUnitAsync(
                    Unit(repository, run, "unit:waiting", "node:waiting"), cancelled.Token);
            });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await waiting);

            // Disposal must not interrupt the in-flight native transaction.
            var disposal = Task.Run(() => store.Dispose());
            await Task.Delay(200);
            Assert.False(disposal.IsCompleted);

            releaseNativeWindow.Set();
            await inFlight;
            await disposal;

            var staged = NativePayloads(path, StageLabel);
            Assert.Single(staged);
            Assert.Contains("node:in-flight", staged[0], StringComparison.Ordinal);
            Assert.DoesNotContain("node:waiting", staged[0], StringComparison.Ordinal);

            using var reopened = new LatticeCodeGraphStore(path);
            Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-08

    [Fact]
    public async Task DisposalReleasesTheNativeHandle()
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

    // ------------------------------------------------------------------ Q-09

    [Fact]
    public async Task SuccessfulNonCancelledControl_PublishesAndReopens()
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
            var publicationAfterReopen = await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:cancel-path"));
            Assert.NotNull(publicationAfterReopen);
            Assert.Single(await reopened.GetPublishedUnitsAsync(new CodeRepositoryId("repo:cancel-path")));
            Assert.NotNull(await reopened.GetNodeAsync(new CodeRepositoryId("repo:cancel-path"), new CodeNodeId("node:control")));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-10

    [Fact]
    public async Task AcceptedStoreSemanticsRemainIntactAcrossACancelledStaging()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                var (repository, run, _, _) = Seed(store);
                var repositoryManifest = await store.GetRepositoryAsync(repository);
                Assert.NotNull(repositoryManifest);
                Assert.Equal(repository.Value, repositoryManifest!.Id.Value);

                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await store.StageIndexUnitAsync(
                        Unit(repository, run, "unit:semantics", "node:semantics"), cancelled.Token));

                // The accepted identity, health and publication semantics are untouched by the
                // cancelled operation.
                var afterRepository = await store.GetRepositoryAsync(repository);
                Assert.NotNull(afterRepository);
                Assert.Equal(repositoryManifest, afterRepository);
                Assert.Equal(1, (await store.CheckHealthAsync()).RepositoryCount);
                Assert.Null(await store.GetLatestPublicationAsync(repository));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var reopenedRepository = await reopened.GetRepositoryAsync(new CodeRepositoryId("repo:cancel-path"));
            Assert.NotNull(reopenedRepository);
            Assert.Equal("Stage 6 cancellation path repository", reopenedRepository!.DisplayName);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-11

    [Fact]
    public async Task UnknownCommitOutcome_IsReportedAndQuarantinedAndReopenCorroborates()
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
            Assert.Null(await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:cancel-path")));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------------------ Q-12

    [Fact]
    public void PinnedNativeDependency_ExposesNoCancellationEntryPoint()
    {
        var assembly = typeof(LatticeDatabase).Assembly;
        Assert.Equal("LatticeDbSharp", assembly.GetName().Name);
        Assert.Equal(new Version(0, 2, 0, 0), assembly.GetName().Version);

        Type[] types;
        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }

        var members = types
            .SelectMany(type => type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(member => $"{type.FullName}.{member.Name}"))
            .ToList();

        Assert.DoesNotContain(members, name => CancellationNamePattern.IsMatch(name));
        Assert.DoesNotContain(members, name => name.Contains("CancellationToken", StringComparison.Ordinal));

        var transactionMembers = typeof(LatticeTransaction)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .ToList();
        Assert.Contains("Commit", transactionMembers);
        Assert.Contains("Rollback", transactionMembers);
        Assert.Contains("Dispose", transactionMembers);
        Assert.DoesNotContain(transactionMembers, name => CancellationNamePattern.IsMatch(name));
    }

    // ------------------------------------------------------------------ helpers

    private static (CodeRepositoryId Repository, CodeIndexRunId Run, CodePluginId Plugin, DateTimeOffset Started) Seed(
        LatticeCodeGraphStore store)
    {
        var repository = new CodeRepositoryId("repo:cancel-path");
        var run = new CodeIndexRunId("run:cancel-path");
        store.UpsertRepositoryAsync(
            new CodeRepositoryManifest(repository, "Stage 6 cancellation path repository", "repo://cancel-path"))
            .GetAwaiter().GetResult();
        store.StoreIndexRunAsync(
            new CodeIndexRunManifest(repository, run, Started, plugins: new[] { Plugin }))
            .GetAwaiter().GetResult();
        return (repository, run, Plugin, Started);
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
            "Cancelled",
            "Example.Cancelled",
            new CodeSymbolId($"symbol:{nodeId}"));
        return new CodeIndexUnitReplacement(
            new CodeFactOrigin(repository, Plugin, "1.0.0", run, new CodeIndexUnitId(unitId)),
            new[] { node });
    }

    private static CodeIndexUnitReplacement LargeUnit(
        CodeRepositoryId repository,
        CodeIndexRunId run,
        string unitId,
        int attempt,
        int nodeCount)
    {
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(index => new CodeGraphNode(
                new CodeNodeId($"node:large:{attempt}:{index}"),
                CodeNodeKinds.Type,
                "Large",
                $"Example.Large.A{attempt}.N{index}",
                new CodeSymbolId($"symbol:large:{attempt}:{index}")))
            .ToList();
        return new CodeIndexUnitReplacement(
            new CodeFactOrigin(repository, Plugin, "1.0.0", run, new CodeIndexUnitId(unitId)),
            nodes);
    }

    private static string Marker(int attempt) => $"Example.Large.A{attempt}.N";

    private static int MarkerCount(string payload, int attempt)
    {
        string marker = Marker(attempt);
        int count = 0;
        int index = payload.IndexOf(marker, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = payload.IndexOf(marker, index + marker.Length, StringComparison.Ordinal);
        }

        return count;
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
        Path.Combine(Path.GetTempPath(), $"andirit-cancel-path-{Guid.NewGuid():N}.ltdb");

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

    /// <summary>Injected acknowledgement loss after an irreversible native commit.</summary>
    private sealed class InjectedPersistenceException : Exception;
}
