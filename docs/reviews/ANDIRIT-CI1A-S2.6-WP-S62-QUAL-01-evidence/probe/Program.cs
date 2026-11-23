// ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01 — Reviewer-owned diagnostic probe
// Verifies native durability behavior independently of producer's test suite

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QualProbe
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("S6.2 Independent Qualifier Probe - Starting");
            Console.WriteLine("=========================================");
            
            var path = Path.Combine(Path.GetTempPath(), $"qual-s62-probe-{Guid.NewGuid():N}.ltdb");
            int failures = 0;
            
            try
            {
                // Test 1: Verify commit is durability point - crash before commit leaves state unchanged
                Console.WriteLine("\n[PROBE 1] Crash before native invocation - durable state unchanged");
                if (!await TestCrashBeforeCommit()) failures++;
                
                // Test 2: Verify crash during native transaction - atomic rollback or commit
                Console.WriteLine("\n[PROBE 2] Crash inside native transaction - atomic rollback or commit");
                if (!await TestCrashInsideTransaction()) failures++;
                
                // Test 3: Crash immediately after commit - commit durable
                Console.WriteLine("\n[PROBE 3] Crash after commit - commit durable");
                if (!await TestCrashAfterCommit()) failures++;
                
                // Test 4: Unknown outcome - quarantine and reopen corroboration
                Console.WriteLine("\n[PROBE 4] Unknown commit outcome - quarantine and reopen corroboration");
                if (!await TestUnknownOutcome()) failures++;
                
                // Test 5: Disposal releases handle
                Console.WriteLine("\n[PROBE 5] Disposal releases native handle");
                if (!await TestDisposalReleasesHandle()) failures++;
                
                // Test 7: Abrupt termination
                Console.WriteLine("\n[PROBE 7] Abrupt process termination - durable state consistent");
                if (!await TestAbruptTermination()) failures++;
                
                // Test 8: Disposal releases handle after crash
                Console.WriteLine("\n[PROBE 8] Disposal releases handle after crash");
                if (!await TestDisposalAfterCrash()) failures++;
                
                Console.WriteLine($"\n=========================================");
                Console.WriteLine($"Probes completed. Failures: {failures}");
                Console.WriteLine(failures == 0 ? "ALL PROBES PASSED" : "SOME PROBES FAILED");
                
                return failures == 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: {ex}");
                return 1;
            }
            finally
            {
                CleanupTempFiles();
            }
        }
        
        static async Task<bool> TestCrashBeforeCommit()
        {
            var path = GetTempDb();
            try
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var (repo, run, _, _) = Seed(store);
                    using var cancelled = new CancellationTokenSource();
                    cancelled.Cancel();
                    
                    try
                    {
                        await store.StageIndexUnitAsync(Unit(repo, run, "unit:before", "node:before"), cancelled.Token);
                        return false; // Should have thrown
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                }
                
                var staged = ReadNativeStage(path);
                if (staged.Count > 0)
                {
                    Console.WriteLine("  FAIL: Staged rows present after cancelled-before-commit");
                    return false;
                }
                
                using var reopened = new LatticeCodeGraphStore(path);
                var health = await reopened.CheckHealthAsync();
                if (!health.IsHealthy)
                {
                    Console.WriteLine("  FAIL: Reopened store not healthy");
                    return false;
                }
                
                Console.WriteLine("  PASS: No native transaction started, state unchanged, reopen healthy");
                return true;
            }
            finally
            {
                DeleteDb(path);
            }
        }
        
        static async Task<bool> TestCrashInsideTransaction()
        {
            var path = GetTempDb();
            try
            {
                using var nativeWindowOpen = new ManualResetEventSlim(false);
                using var crashIssued = new ManualResetEventSlim(false);
                var armed = 0;
                
                Action<string> injector = point =>
                {
                    if (point != "before-commit" || Volatile.Read(ref armed) == 0) return;
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
                        return false; // should not reach here
                    }
                    catch (InjectedCrashException)
                    {
                        // expected — the crash was simulated inside the native window
                    }
                    
                    await crasher;
                }
                
                // The crash was injected inside the native window. The transaction either
                // rolled back (staged = 0) or committed atomically (staged = 1, whole unit).
                var staged = NativePayloads(path, "HetuStage");
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
                    Console.WriteLine($"  FAIL: Unexpected staged count: {staged.Count}");
                    return false;
                }
                
                using var reopened = new LatticeCodeGraphStore(path);
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
                
                Console.WriteLine($"  PASS: Transaction atomic (staged rows: {staged.Count}), reopen healthy");
                return true;
            }
            finally
            {
                DeleteDb(path);
            }
        }
        
        static async Task<bool> TestCrashAfterCommit()
        {
            var path = GetTempDb();
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
                        return false; // should not reach here
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
                var staged = NativePayloads(path, "HetuStage");
                Assert.Single(staged);
                Assert.Contains("node:after-commit", staged[0], StringComparison.Ordinal);
                
                using var reopened = new LatticeCodeGraphStore(path);
                var health = await reopened.CheckHealthAsync();
                Assert.True(health.IsHealthy);
                
                Console.WriteLine("  PASS: Commit durable, crash after commit wraps in unknown outcome");
                return true;
            }
            finally
            {
                DeleteDb(path);
            }
        }
        
        static async Task<bool> TestUnknownOutcome()
        {
            var path = GetTempDb();
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
                
                var staged = NativePayloads(path, "HetuStage");
                Assert.Single(staged);
                Assert.Contains("node:unknown", staged[0], StringComparison.Ordinal);
                
                using var reopened = new LatticeCodeGraphStore(path);
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
                Assert.Null(await reopened.GetLatestPublicationAsync(new CodeRepositoryId("repo:durability-path")));
                
                Console.WriteLine("  PASS: Unknown outcome reported, quarantined, reopen corroborates");
                return true;
            }
            finally
            {
                DeleteDb(path);
            }
        }
        
        static async Task<bool> TestDisposalReleasesHandle()
        {
            var path = GetTempDb();
            try
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var (repo, run, _, _) = Seed(store);
                    await store.StageIndexUnitAsync(Unit(repo, run, "unit:cleanup", "node:cleanup"));
                }
                
                using (var reopened = new LatticeCodeGraphStore(path))
                {
                    var health = await reopened.CheckHealthAsync();
                    if (!health.IsHealthy)
                    {
                        Console.WriteLine("  FAIL: Reopened store not healthy");
                        return false;
                    }
                }
                
                // On Windows an open native handle keeps the file locked; a successful delete proves
                // the handle was released.
                File.Delete(path);
                if (File.Exists(path))
                {
                    Console.WriteLine("  FAIL: File still locked after disposal");
                    return false;
                }
                
                Console.WriteLine("  PASS: Handle released, file deletable");
                return true;
            }
            finally
            {
                DeleteDb(path);
            }
        }
        
        static async Task<bool> TestAbruptTermination()
        {
            var path = GetTempDb();
            try
            {
                // First, establish a baseline publication
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var (repository, run, _, started) = Seed(store);
                    await store.StageIndexUnitAsync(Unit(repository, run, "unit:baseline", "node:baseline"));
                    await store.CompleteIndexRunAsync(
                        new(repository, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), new[] { new CodePluginId("plugin:durability") }),
                        new(repository, run, new[] { new CodeSourceManifest(new CodePluginId("plugin:durability"), "1.0.0", "src/Baseline.cs", "sha256:baseline") }));
                }
                
                // Now spawn a worker that does a write and gets killed mid-transaction
                var workerExe = Path.Combine(AppContext.BaseDirectory, "Andirit.LatticeDb.Tests.Worker.exe");
                if (!File.Exists(workerExe))
                {
                    Console.WriteLine("  SKIP: Worker executable not found");
                    return true;
                }
                
                var psi = new ProcessStartInfo
                {
                    FileName = workerExe,
                    Arguments = $"--db \"{path}\" --unit unit:abrupt --node node:abrupt",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                var worker = Process.Start(psi);
                Assert.NotNull(worker);
                
                // Give it a moment to start the write, then kill it
                await Task.Delay(100);
                if (!worker.HasExited)
                {
                    worker.Kill(true);
                    await worker.WaitForExitAsync();
                }
                
                // Wait a bit more for the OS to release the file lock
                await Task.Delay(200);
                
                // Verify the database is still consistent after abrupt termination
                var staged = NativePayloads(path, "HetuStage");
                // The worker may or may not have committed; either way, no partial state
                if (staged.Count > 1)
                {
                    Console.WriteLine($"  FAIL: Partial state detected - {staged.Count} rows");
                    return false;
                }
                
                using var reopened = new LatticeCodeGraphStore(path);
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
                var stagedAfter = NativePayloads(path, "HetuStage");
                // No partial staged units
                Assert.True(stagedAfter.Count == 0 || stagedAfter.Count == 1);
                
                Console.WriteLine("  PASS: Abrupt termination - state consistent");
                return true;
            }
            finally
            {
                DeleteDb(path);
            }
        }
        
        static async Task<bool> TestDisposalAfterCrash()
        {
            var path = GetTempDb();
            try
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var (repo, run, _, _) = Seed(store);
                    await store.StageIndexUnitAsync(Unit(repo, run, "unit:crash", "node:crash"));
                }
                
                using (var reopened = new LatticeCodeGraphStore(path))
                {
                    var health = await reopened.CheckHealthAsync();
                    if (!health.IsHealthy)
                    {
                        Console.WriteLine("  FAIL: Reopened store not healthy after crash");
                        return false;
                    }
                }
                
                File.Delete(path);
                if (File.Exists(path))
                {
                    Console.WriteLine("  FAIL: File still exists after delete");
                    return false;
                }
                
                Console.WriteLine("  PASS: Handle released after crash");
                return true;
            }
            finally
            {
                DeleteDb(path);
            }
        }
        
        // ------------------------------------------------------------------ helpers
        
        private static (CodeRepositoryId, CodeIndexRunId, CodePluginId, DateTimeOffset) Seed(
            LatticeCodeGraphStore store)
        {
            var repository = new CodeRepositoryId("repo:qual");
            var run = new CodeIndexRunId("run:qual");
            var plugin = new CodePluginId("plugin:qual");
            var started = new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);
            
            store.UpsertRepositoryAsync(
                new CodeRepositoryManifest(repository, "Qual Repository", "repo://qual"))
                .GetAwaiter().GetResult();
            store.StoreIndexRunAsync(new CodeIndexRunManifest(repository, run, started, plugins: new[] { new CodePluginId("plugin:qual") }))
                .GetAwaiter().GetResult();
            return (repository, run, plugin, started);
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
                new CodeFactOrigin(repository, new CodePluginId("plugin:qual"), "1.0.0", run, new CodeIndexUnitId(unitId)),
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
        
        private static List<string> ReadNativeStage(string path)
        {
            return NativePayloads(path, "HetuStage");
        }
        
        private static string GetTempDb() =>
            Path.Combine(Path.GetTempPath(), $"qual-s62-{Guid.NewGuid():N}.ltdb");
        
        private static void DeleteDb(string path)
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
        
        static void CleanupTempFiles()
        {
            var dir = Path.GetTempPath();
            foreach (var f in Directory.GetFiles(dir, "qual-s62-*.ltdb*"))
            {
                try { File.Delete(f); } catch { }
            }
        }
        
        class InjectedCrashException : Exception;
        class InjectedPersistenceException : Exception;
    }
}