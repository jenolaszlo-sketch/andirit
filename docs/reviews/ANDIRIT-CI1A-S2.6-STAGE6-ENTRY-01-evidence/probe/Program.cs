// Stage 6 entry probe harness — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.
//
// Read-only feasibility probes over the production LatticeCodeGraphStore and
// the pinned LatticeDbSharp 0.2.0 native dependency. The harness never
// modifies repository sources; every scenario runs against a scratch database
// in a caller-supplied work directory. Each scenario is executed in its own
// process so that a native abort cannot contaminate sibling scenarios.
//
// Scenarios:
//   api-surface    managed + native cancellation surface of the pinned dependency
//   pre-invocation cancellation requested before the native invocation
//   managed-wait   cancellation of a managed wait while native work is in flight
//   native-window  cancellation requested during active native execution
//   post-commit    cancellation requested after the irreversible commit
//   commit-unknown indeterminate commit outcome (truthfulness template)
//   dispose-race   concurrent cancellation and disposal
//   cleanup        resource cleanup after disposal
//   control        successful non-cancelled control
//   reopen         reopen and durable-state consistency
//
// Exit code: 0 when every CHECK in the scenario passed, 1 when at least one
// CHECK failed, 2 for a usage error, 3 for an unhandled exception.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

internal static class Program
{
    private const string StageLabel = "HetuStage";
    private const string UnitLabel = "HetuUnit";
    private const string RepositoryLabel = "HetuRepository";
    private const string RunLabel = "HetuRun";

    private static int _failures;
    private static string _work = string.Empty;

    private static void Emit(string line)
    {
        Console.Out.WriteLine(line);
        Console.Out.Flush();
    }

    private static void Check(bool condition, string label, string detail)
    {
        Emit($"CHECK|{label}|{(condition ? "PASS" : "FAIL")}|{detail}");
        if (!condition)
        {
            _failures++;
        }
    }

    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Emit("usage: Stage6Probe <scenario> <work-dir>");
            return 2;
        }

        string scenario = args[0];
        _work = args[1];
        Directory.CreateDirectory(_work);

        Emit($"SCENARIO|{scenario}|start|{DateTimeOffset.UtcNow:O}");
        Emit($"ENVIRONMENT|os|{Environment.OSVersion}|description|{System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        Emit($"ENVIRONMENT|architecture|{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        Emit($"ENVIRONMENT|runtime|{Environment.Version}|base-directory|{AppContext.BaseDirectory}");

        try
        {
            switch (scenario)
            {
                case "api-surface":
                    ApiSurface();
                    break;
                case "pre-invocation":
                    PreInvocationAsync().GetAwaiter().GetResult();
                    break;
                case "managed-wait":
                    ManagedWaitAsync().GetAwaiter().GetResult();
                    break;
                case "native-window":
                    NativeWindowAsync().GetAwaiter().GetResult();
                    break;
                case "post-commit":
                    PostCommitAsync().GetAwaiter().GetResult();
                    break;
                case "commit-unknown":
                    CommitUnknownAsync().GetAwaiter().GetResult();
                    break;
                case "dispose-race":
                    DisposeRaceAsync().GetAwaiter().GetResult();
                    break;
                case "cleanup":
                    CleanupAsync().GetAwaiter().GetResult();
                    break;
                case "control":
                    ControlAsync().GetAwaiter().GetResult();
                    break;
                case "reopen":
                    ReopenAsync().GetAwaiter().GetResult();
                    break;
                default:
                    Emit($"ERROR|unknown-scenario|{scenario}");
                    return 2;
            }
        }
        catch (Exception exception)
        {
            Emit($"UNHANDLED|{exception.GetType().FullName}|{exception.Message}");
            _failures++;
        }

        Emit($"SCENARIO|{scenario}|failures|{_failures}");
        return _failures == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------- helpers

    private static LatticeCodeGraphStore CreateStore(string path, Action<string>? faultInjector)
    {
        ConstructorInfo? constructor = typeof(LatticeCodeGraphStore).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string), typeof(LatticeDbStoreOptions), typeof(Action<string>) },
            modifiers: null);
        if (constructor is null)
        {
            Emit("FATAL|internal-constructor-with-fault-injector|not-found");
            throw new InvalidOperationException(
                "The internal LatticeCodeGraphStore fault-injector constructor was not found; " +
                "the production surface changed and the probe must be re-baselined.");
        }

        return (LatticeCodeGraphStore)constructor.Invoke(new object?[] { path, null, faultInjector });
    }

    private static (CodeRepositoryId Repository, CodeIndexRunId Run, CodePluginId Plugin, DateTimeOffset Started) Seed(
        LatticeCodeGraphStore store)
    {
        var repository = new CodeRepositoryId("repo:s6-entry");
        var run = new CodeIndexRunId("run:s6-entry");
        var plugin = new CodePluginId("plugin:s6-entry");
        DateTimeOffset started = DateTimeOffset.UtcNow;

        store.UpsertRepositoryAsync(
            new CodeRepositoryManifest(repository, "Stage 6 entry repository", "repo://s6-entry"))
            .GetAwaiter().GetResult();
        store.StoreIndexRunAsync(
            new CodeIndexRunManifest(repository, run, started, plugins: new[] { plugin }))
            .GetAwaiter().GetResult();

        return (repository, run, plugin, started);
    }

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repository,
        CodePluginId plugin,
        CodeIndexRunId run,
        string unitId,
        string nodeId,
        string symbolId)
    {
        var node = new CodeGraphNode(
            new CodeNodeId(nodeId),
            CodeNodeKinds.Type,
            "Durable",
            "Example.Durable",
            new CodeSymbolId(symbolId));

        return new CodeIndexUnitReplacement(
            new CodeFactOrigin(repository, plugin, "1.0.0", run, new CodeIndexUnitId(unitId)),
            new[] { node });
    }

    private static long NativeCount(string path, string label)
    {
        try
        {
            return NativeCountCore(path, label, new LatticeDatabaseOptions { Create = false });
        }
        catch (Exception exception)
        {
            Emit($"NOTE|native-count|fallback|{exception.GetType().Name}|{exception.Message}");
            return NativeCountCore(path, label, new LatticeDatabaseOptions { Create = true });
        }
    }

    private static long NativeCountCore(string path, string label, LatticeDatabaseOptions options)
    {
        using LatticeDatabase database = LatticeDatabase.Open(path, options);
        using LatticeTransaction transaction = database.BeginReadTransaction();
        using LatticeQuery query = database.Prepare($"MATCH (s:{label}) RETURN count(s)");
        using LatticeQueryResult result = query.Execute(transaction);
        IReadOnlyList<LatticeRow> rows = result.ReadAll();
        transaction.Commit();
        return rows.Count == 0 ? 0L : rows[0].GetValue(0).AsInt64();
    }

    private static string HashFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static IEnumerable<string> NativeAssets()
    {
        string root = AppContext.BaseDirectory;
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            if (name.Equals("lattice.dll", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("liblattice.so", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("liblattice.dylib", StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }

    // ------------------------------------------------------------- scenarios

    private static void ApiSurface()
    {
        Assembly assembly = typeof(LatticeDatabase).Assembly;
        Emit($"PROBE|assembly|{assembly.FullName}");
        Emit($"PROBE|assembly-version|{assembly.GetName().Version}");
        Emit($"PROBE|assembly-informational-version|" +
             $"{assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
        Emit($"PROBE|assembly-path|{assembly.Location}");
        Emit($"PROBE|assembly-sha256|{HashFile(assembly.Location)}");
        Emit($"PROBE|assembly-bytes|{new FileInfo(assembly.Location).Length}");

        Type[] types;
        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }

        Emit($"PROBE|public-types|{types.Length}");

        var members = new List<string>();
        foreach (Type type in types)
        {
            foreach (MemberInfo member in type.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                members.Add($"{type.FullName}.{member.Name}");
            }
        }

        Emit($"PROBE|public-members|{members.Count}");

        List<string> cancellationLike = members
            .Where(member => Regex.IsMatch(member, "(?i)cancel|interrupt|abort|terminate|shutdown"))
            .OrderBy(member => member, StringComparer.Ordinal)
            .ToList();
        Emit($"PROBE|cancellation-like-member-names|{cancellationLike.Count}");
        foreach (string member in cancellationLike)
        {
            Emit($"PROBE|cancellation-like-member|{member}");
        }

        List<string> tokenMembers = members
            .Where(member => member.Contains("CancellationToken", StringComparison.Ordinal))
            .OrderBy(member => member, StringComparer.Ordinal)
            .ToList();
        Emit($"PROBE|members-with-cancellationtoken|{tokenMembers.Count}");
        foreach (string member in tokenMembers)
        {
            Emit($"PROBE|member-with-cancellationtoken|{member}");
        }

        Emit("PROBE|transaction-surface-start");
        foreach (MemberInfo member in typeof(LatticeTransaction)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OrderBy(member => member.Name, StringComparer.Ordinal))
        {
            Emit($"PROBE|transaction-member|{member.MemberType}|{member.Name}");
        }

        Emit("PROBE|transaction-surface-end");
        Emit("PROBE|database-surface-start");
        foreach (MemberInfo member in typeof(LatticeDatabase)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(member => member.Name.Contains("Transaction", StringComparison.Ordinal) ||
                             member.Name.Contains("Execute", StringComparison.Ordinal) ||
                             member.Name.Contains("Close", StringComparison.Ordinal) ||
                             member.Name.Contains("Dispose", StringComparison.Ordinal))
            .OrderBy(member => member.Name, StringComparer.Ordinal))
        {
            Emit($"PROBE|database-member|{member.MemberType}|{member.Name}");
        }

        Emit("PROBE|database-surface-end");
        foreach (string asset in NativeAssets())
        {
            Emit($"PROBE|native-asset|{asset}|bytes={new FileInfo(asset).Length}|sha256={HashFile(asset)}");
        }

        Check(cancellationLike.Count == 0,
            "api-surface/no-cancellation-member",
            $"public members whose name suggests cancellation/interruption: {cancellationLike.Count}");
        Check(tokenMembers.Count == 0,
            "api-surface/no-cancellationtoken-member",
            $"public members carrying a CancellationToken: {tokenMembers.Count}");
    }

    private static async Task PreInvocationAsync()
    {
        string path = Path.Combine(_work, "pre-invocation.db");
        LatticeCodeGraphStore store = CreateStore(path, null);
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            CodeIndexUnitReplacement unit = Unit(repository, plugin, run, "unit:cancelled", "node:cancelled", "symbol:cancelled");

            using var source = new CancellationTokenSource();
            source.Cancel();

            bool observed = false;
            string detail = "no exception was raised";
            try
            {
                await store.StageIndexUnitAsync(unit, source.Token);
            }
            catch (OperationCanceledException exception)
            {
                observed = true;
                detail = $"{exception.GetType().Name}|{exception.Message}";
            }

            Check(observed, "pre-invocation/cancellation-observed", detail);

            CodeGraphPublication? publication = await store.GetLatestPublicationAsync(repository);
            Check(publication is null,
                "pre-invocation/no-publication",
                $"publication={(publication is null ? "none" : "present")}");

            IReadOnlyList<CodeIndexUnitReplacement> published = await store.GetPublishedUnitsAsync(repository);
            Check(published.Count == 0,
                "pre-invocation/no-published-units",
                $"published unit count={published.Count}");
        }
        finally
        {
            store.Dispose();
        }

        long staged = NativeCount(path, StageLabel);
        long units = NativeCount(path, UnitLabel);
        Check(staged == 0, "pre-invocation/native-stage-table-empty", $"HetuStage rows={staged}");
        Check(units == 0, "pre-invocation/native-unit-table-empty", $"HetuUnit rows={units}");

        using var reopened = new LatticeCodeGraphStore(path);
        CodeGraphStoreHealth health = await reopened.CheckHealthAsync();
        Check(health.IsHealthy, "pre-invocation/reopen-healthy", $"status={health.Status}");
    }

    private static async Task ManagedWaitAsync()
    {
        string path = Path.Combine(_work, "managed-wait.db");
        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        int armed = 0;

        Action<string> injector = phase =>
        {
            if (phase == "before-commit" && Volatile.Read(ref armed) == 1)
            {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(30));
            }
        };

        LatticeCodeGraphStore store = CreateStore(path, injector);
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            Volatile.Write(ref armed, 1);

            Task blocking = Task.Run(async () => await store.StageIndexUnitAsync(
                Unit(repository, plugin, run, "unit:blocked", "node:blocked", "symbol:blocked")));
            Check(entered.Wait(TimeSpan.FromSeconds(30)),
                "managed-wait/first-write-entered-native-window",
                "the fault injector reached the before-commit checkpoint inside the native transaction");

            using var source = new CancellationTokenSource();
            Task waiting = Task.Run(async () =>
            {
                await Task.Delay(250);
                source.Cancel();
                await store.StageIndexUnitAsync(
                    Unit(repository, plugin, run, "unit:waiting", "node:waiting", "symbol:waiting"),
                    source.Token);
            });

            bool observed = false;
            string detail = "no exception was raised";
            try
            {
                await waiting;
            }
            catch (OperationCanceledException exception)
            {
                observed = true;
                detail = $"{exception.GetType().Name}|{exception.Message}";
            }

            Check(observed, "managed-wait/cancellation-observed-on-gate-wait", detail);

            release.Set();
            await blocking;
            Check(true,
                "managed-wait/in-flight-native-write-unaffected",
                "the native write that held the write gate completed normally after the managed wait was cancelled");
        }
        finally
        {
            release.Set();
            store.Dispose();
        }

        long staged = NativeCount(path, StageLabel);
        Check(staged == 1,
            "managed-wait/only-the-non-cancelled-write-is-durable",
            $"HetuStage rows={staged} (expected exactly the one non-cancelled staging write)");
    }

    private static async Task NativeWindowAsync()
    {
        string path = Path.Combine(_work, "native-window.db");
        using var source = new CancellationTokenSource();
        int armed = 0;
        int cancelledInsideNativeWindow = 0;

        Action<string> injector = phase =>
        {
            if (phase == "before-commit" && Volatile.Read(ref armed) == 1)
            {
                Interlocked.Exchange(ref cancelledInsideNativeWindow, 1);
                source.Cancel();
            }
        };

        LatticeCodeGraphStore store = CreateStore(path, injector);
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            Volatile.Write(ref armed, 1);

            bool observed = false;
            string detail = "the operation completed without a cancellation exception";
            try
            {
                await store.StageIndexUnitAsync(
                    Unit(repository, plugin, run, "unit:native-window", "node:native-window", "symbol:native-window"),
                    source.Token);
            }
            catch (OperationCanceledException exception)
            {
                observed = true;
                detail = $"{exception.GetType().Name}|{exception.Message}";
            }

            Check(Volatile.Read(ref cancelledInsideNativeWindow) == 1,
                "native-window/cancellation-requested-inside-native-window",
                "the fault injector cancelled the token after the native writes and before the commit");
            Check(!observed,
                "native-window/cancellation-not-honoured-inside-native-window",
                detail);

            CodeGraphPublication? publication = await store.GetLatestPublicationAsync(repository);
            Check(publication is null,
                "native-window/no-publication-from-cancelled-request",
                $"publication={(publication is null ? "none" : "present")}");
        }
        finally
        {
            store.Dispose();
        }

        long staged = NativeCount(path, StageLabel);
        Check(staged == 1,
            "native-window/native-write-committed-despite-cancellation",
            $"HetuStage rows={staged} (the native transaction was not interrupted and the commit proceeded)");

        using var reopened = new LatticeCodeGraphStore(path);
        CodeGraphStoreHealth health = await reopened.CheckHealthAsync();
        Check(health.IsHealthy, "native-window/reopen-healthy", $"status={health.Status}");
    }

    private static async Task PostCommitAsync()
    {
        string path = Path.Combine(_work, "post-commit.db");
        using var source = new CancellationTokenSource();
        int armed = 0;
        int cancelledAfterCommit = 0;

        Action<string> injector = phase =>
        {
            if (phase == "after-commit" && Volatile.Read(ref armed) == 1)
            {
                Interlocked.Exchange(ref cancelledAfterCommit, 1);
                source.Cancel();
            }
        };

        LatticeCodeGraphStore store = CreateStore(path, injector);
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            Volatile.Write(ref armed, 1);

            bool observed = false;
            string detail = "the operation completed without a cancellation exception";
            try
            {
                await store.StageIndexUnitAsync(
                    Unit(repository, plugin, run, "unit:post-commit", "node:post-commit", "symbol:post-commit"),
                    source.Token);
            }
            catch (OperationCanceledException exception)
            {
                observed = true;
                detail = $"{exception.GetType().Name}|{exception.Message}";
            }

            Check(Volatile.Read(ref cancelledAfterCommit) == 1,
                "post-commit/cancellation-requested-after-commit",
                "the fault injector cancelled the token after the irreversible commit");
            Check(!observed,
                "post-commit/cancellation-not-reported-after-commit",
                detail);
        }
        finally
        {
            store.Dispose();
        }

        long staged = NativeCount(path, StageLabel);
        Check(staged == 1,
            "post-commit/write-is-durable",
            $"HetuStage rows={staged}");
    }

    private static async Task CommitUnknownAsync()
    {
        string path = Path.Combine(_work, "commit-unknown.db");
        int armed = 0;

        Action<string> injector = phase =>
        {
            if (phase == "after-commit" && Volatile.Read(ref armed) == 1)
            {
                throw new InvalidOperationException("probe: injected post-commit fault");
            }
        };

        LatticeCodeGraphStore store = CreateStore(path, injector);
        bool quarantined = false;
        string quarantineDetail = "not observed";
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            Volatile.Write(ref armed, 1);

            bool observed = false;
            string detail = "no exception was raised";
            try
            {
                await store.StageIndexUnitAsync(
                    Unit(repository, plugin, run, "unit:unknown", "node:unknown", "symbol:unknown"));
            }
            catch (CodeGraphCommitOutcomeUnknownException exception)
            {
                observed = true;
                detail = exception.Message;
            }

            Check(observed, "commit-unknown/outcome-reported-as-unknown", detail);

            try
            {
                await store.GetLatestPublicationAsync(repository);
            }
            catch (CodeGraphCommitOutcomeUnknownException exception)
            {
                quarantined = true;
                quarantineDetail = exception.Message;
            }

            Check(quarantined,
                "commit-unknown/instance-quarantined-after-unknown-outcome",
                quarantineDetail);
        }
        finally
        {
            store.Dispose();
        }

        long staged = NativeCount(path, StageLabel);
        Check(staged == 1,
            "commit-unknown/commit-had-actually-landed",
            $"HetuStage rows={staged} (the commit succeeded but the API refuses to claim it)");
    }

    private static async Task DisposeRaceAsync()
    {
        string path = Path.Combine(_work, "dispose-race.db");
        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        int armed = 0;

        Action<string> injector = phase =>
        {
            if (phase == "before-commit" && Volatile.Read(ref armed) == 1)
            {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(30));
            }
        };

        LatticeCodeGraphStore store = CreateStore(path, injector);
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            Volatile.Write(ref armed, 1);

            Task write = Task.Run(async () => await store.StageIndexUnitAsync(
                Unit(repository, plugin, run, "unit:dispose-race", "node:dispose-race", "symbol:dispose-race")));
            Check(entered.Wait(TimeSpan.FromSeconds(30)),
                "dispose-race/write-entered-native-window",
                "the native write is in flight inside the native transaction");

            using var source = new CancellationTokenSource();
            Task dispose = Task.Run(() =>
            {
                source.Cancel();
                store.Dispose();
            });

            await Task.Delay(750);
            Check(!dispose.IsCompleted,
                "dispose-race/dispose-waits-for-the-in-flight-native-write",
                $"dispose completed while the native write was in flight: {dispose.IsCompleted}");

            release.Set();
            await write;
            await dispose;
            Check(true,
                "dispose-race/write-completed-before-disposal",
                "the in-flight native write completed and was not aborted by disposal");
        }
        finally
        {
            release.Set();
            store.Dispose();
        }

        long staged = NativeCount(path, StageLabel);
        Check(staged == 1,
            "dispose-race/durable-state-intact",
            $"HetuStage rows={staged}");

        using var reopened = new LatticeCodeGraphStore(path);
        CodeGraphStoreHealth health = await reopened.CheckHealthAsync();
        Check(health.IsHealthy, "dispose-race/reopen-healthy", $"status={health.Status}");
    }

    private static async Task CleanupAsync()
    {
        string path = Path.Combine(_work, "cleanup.db");
        LatticeCodeGraphStore store = new LatticeCodeGraphStore(path);
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            await store.StageIndexUnitAsync(
                Unit(repository, plugin, run, "unit:cleanup", "node:cleanup", "symbol:cleanup"));
        }
        finally
        {
            store.Dispose();
        }

        using (var reopened = new LatticeCodeGraphStore(path))
        {
            CodeGraphStoreHealth health = await reopened.CheckHealthAsync();
            Check(health.IsHealthy, "cleanup/reopen-healthy", $"status={health.Status}");
        }

        bool deleted;
        try
        {
            File.Delete(path);
            deleted = !File.Exists(path);
        }
        catch (IOException exception)
        {
            Emit($"NOTE|cleanup|delete|{exception.Message}");
            deleted = false;
        }

        Check(deleted,
            "cleanup/database-file-released-after-dispose",
            "the native database file could be deleted, so no native handle was retained");
    }

    private static async Task ControlAsync()
    {
        string path = Path.Combine(_work, "control.db");
        LatticeCodeGraphStore store = new LatticeCodeGraphStore(path);
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            await store.StageIndexUnitAsync(
                Unit(repository, plugin, run, "unit:control", "node:control", "symbol:control"));

            await store.CompleteIndexRunAsync(
                new CodeIndexRunManifest(
                    repository,
                    run,
                    started,
                    CodeIndexRunStatus.Completed,
                    started.AddSeconds(1),
                    new[] { plugin }),
                new CodeRepositoryIndexState(
                    repository,
                    run,
                    new[] { new CodeSourceManifest(plugin, "1.0.0", "src/Control.cs", "sha256:control") }));

            CodeGraphPublication? publication = await store.GetLatestPublicationAsync(repository);
            Check(publication is not null, "control/publication-present", "publication is null");
            if (publication is not null)
            {
                Emit($"PROBE|control|publication|run={publication.IndexRunId}|snapshot={publication.SnapshotIdentity}|consistent={publication.IsConsistentSnapshot}");
                Check(publication.IndexRunId == run, "control/publication-binds-the-run", $"run={publication.IndexRunId}");
            }

            IReadOnlyList<CodeIndexUnitReplacement> published = await store.GetPublishedUnitsAsync(repository);
            Check(published.Count == 1, "control/published-unit-count", $"count={published.Count}");

            CodeGraphNode? node = await store.GetNodeAsync(repository, new CodeNodeId("node:control"));
            Check(node is not null, "control/node-resolvable", node is null ? "node not found" : node.Name);
        }
        finally
        {
            store.Dispose();
        }
    }

    private static async Task ReopenAsync()
    {
        string path = Path.Combine(_work, "reopen.db");
        string beforeRun = string.Empty;
        string beforeSnapshot = string.Empty;
        bool beforeConsistent = false;
        int beforeUnits = 0;

        LatticeCodeGraphStore store = new LatticeCodeGraphStore(path);
        try
        {
            (CodeRepositoryId repository, CodeIndexRunId run, CodePluginId plugin, DateTimeOffset started) = Seed(store);
            await store.StageIndexUnitAsync(
                Unit(repository, plugin, run, "unit:reopen", "node:reopen", "symbol:reopen"));
            await store.CompleteIndexRunAsync(
                new CodeIndexRunManifest(
                    repository,
                    run,
                    started,
                    CodeIndexRunStatus.Completed,
                    started.AddSeconds(1),
                    new[] { plugin }),
                new CodeRepositoryIndexState(
                    repository,
                    run,
                    new[] { new CodeSourceManifest(plugin, "1.0.0", "src/Reopen.cs", "sha256:reopen") }));

            CodeGraphPublication? before = await store.GetLatestPublicationAsync(repository);
            if (before is not null)
            {
                beforeRun = before.IndexRunId.Value;
                beforeSnapshot = before.SnapshotIdentity ?? string.Empty;
                beforeConsistent = before.IsConsistentSnapshot;
            }

            beforeUnits = (await store.GetPublishedUnitsAsync(repository)).Count;
        }
        finally
        {
            store.Dispose();
        }

        using var reopened = new LatticeCodeGraphStore(path);
        CodeGraphStoreHealth health = await reopened.CheckHealthAsync();
        Check(health.IsHealthy, "reopen/healthy-after-reopen", $"status={health.Status}");

        CodeGraphPublication? after = await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:s6-entry"));
        Check(after is not null, "reopen/publication-present", "publication is null");
        if (after is not null)
        {
            Check(after.IndexRunId.Value == beforeRun,
                "reopen/publication-identity-preserved",
                $"before={beforeRun}|after={after.IndexRunId.Value}");
            Check((after.SnapshotIdentity ?? string.Empty) == beforeSnapshot,
                "reopen/snapshot-identity-preserved",
                $"before={beforeSnapshot}|after={after.SnapshotIdentity}");
            Check(after.IsConsistentSnapshot == beforeConsistent,
                "reopen/snapshot-consistency-preserved",
                $"before={beforeConsistent}|after={after.IsConsistentSnapshot}");
        }

        int afterUnits = (await reopened.GetPublishedUnitsAsync(new CodeRepositoryId("repo:s6-entry"))).Count;
        Check(afterUnits == beforeUnits,
            "reopen/published-unit-count-preserved",
            $"before={beforeUnits}|after={afterUnits}");

        CodeGraphNode? node = await reopened.GetNodeAsync(
            new CodeRepositoryId("repo:s6-entry"), new CodeNodeId("node:reopen"));
        Check(node is not null, "reopen/node-resolvable", node is null ? "node not found" : node.Name);
    }
}
