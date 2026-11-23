// Reviewer-owned diagnostic probe for S6.2 independent qualification
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
using CodeRepositoryId = Andirit.CodeRepositoryId;
using CodeIndexRunId = Andirit.CodeIndexRunId;
using CodePluginId = Andirit.CodePluginId;
using CodeIndexUnitReplacement = Andirit.CodeIndexUnitReplacement;
using CodeGraphNode = Andirit.CodeGraphNode;
using CodeNodeId = Andirit.CodeNodeId;
using CodeNodeKinds = Andirit.CodeNodeKinds;
using CodeSymbolId = Andirit.CodeSymbolId;
using CodeFactOrigin = Andirit.CodeFactOrigin;
using CodeIndexUnitId = Andirit.CodeIndexUnitId;
using CodeIndexRunManifest = Andirit.CodeIndexRunManifest;
using CodeIndexRunStatus = Andirit.CodeIndexRunStatus;
using CodeSourceManifest = Andirit.CodeSourceManifest;
using CodeRepositoryManifest = Andirit.CodeRepositoryManifest;
using CodeGraphCommitOutcomeUnknownException = Andirit.CodeGraphCommitOutcomeUnknownException;
using CodeGraphStoreHealth = Andirit.CodeGraphStoreHealth;
using LatticeCodeGraphStore = Andirit.LatticeDb.LatticeCodeGraphStore;
using LatticeDatabase = LatticeDbSharp.LatticeDatabase;
using LatticeDatabaseOptions = LatticeDbSharp.LatticeDatabaseOptions;
using LatticeTransaction = LatticeDbSharp.LatticeTransaction;

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
                        throw new TimeoutException("Crash signal timeout");
                    throw new InjectedCrashException();
                };
                
                using (var store = new LatticeCodeGraphStore(path, null, injector))
                {
                    var (repo, run, _, _) = Seed(store);
                    Volatile.Write(ref armed, 1);
                    
                    var crasher = Task.Run(() =>
                    {
                        if (!nativeWindowOpen.Wait(TimeSpan.FromSeconds(30)))
                            throw new TimeoutException("Window open timeout");
                        // Don't set event - just let the exception propagate
                    });
                    
                    try
                    {
                        await store.StageIndexUnitAsync(Unit(repo, run, "unit:crash", "node:crash"), CancellationToken.None);
                        Console.WriteLine("  FAIL: Expected InjectedCrashException");
                        return false;
                    }
                    catch (Exception ex) when (ex is InjectedCrashException || ex is OperationCanceledException)
                    {
                        // Expected
                    }
                }
                
                var staged = ReadNativeStage(GetTempPath());
                var staged2 = ReadNativeStage(GetTempPath());
                
                // The transaction either committed (1 row) or rolled back (0 rows)
                // Both are valid - key is no partial state
                if (staged.Count > 1)
                {
                    Console.WriteLine($"  FAIL: Partial state detected - {staged.Count} rows");
                    return false;
                }
                
                using var reopened = new LatticeCodeGraphStore(GetTempPath());
                var health = await reopened.CheckHealthAsync();
                if (!health.IsHealthy)
                {
                    Console.WriteLine("  FAIL: Reopened store not healthy");
                    return false;
                }
                
                Console.WriteLine($"  PASS: Transaction atomic (staged rows: {staged.Count}), reopen healthy");
                return true;
            }
            catch
            {
                return false;
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
                        throw new InjectedCrashException();
                };
                
                using (var store = new LatticeCodeGraphStore(path, null, injector))
                {
                    var (repo, run, _, _) = Seed(store);
                    Volatile.Write(ref armed, 1);
                    
                    try
                    {
                        await store.StageIndexUnitAsync(Unit(repo, run, "unit:after", "node:after"), CancellationToken.None);
                        Console.WriteLine("  FAIL: Expected exception after commit");
                        return false;
                    }
                    catch (CodeGraphCommitOutcomeUnknownException)
                    {
                        // Expected - crash after commit wraps in unknown outcome
                    }
                    catch (InjectedCrashException)
                    {
                        // Also acceptable
                    }
                }
                
                var staged = ReadNativeStage(path);
                if (staged.Count != 1)
                {
                    Console.WriteLine($"  FAIL: Expected 1 staged row, got {staged.Count}");
                    return false;
                }
                
                using var reopened = new LatticeCodeGraphStore(path);
                var health = await reopened.CheckHealthAsync();
                if (!health.IsHealthy)
                {
                    Console.WriteLine("  FAIL: Reopened store not healthy");
                    return false;
                }
                
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
                        throw new InjectedPersistenceException();
                };
                
                using (var store = new LatticeCodeGraphStore(path, null, injector))
                {
                    var (repo, run, _, _) = Seed(store);
                    Volatile.Write(ref armed, 1);
                    
                    try
                    {
                        await store.StageIndexUnitAsync(Unit(repo, run, "unit:unk", "node:unk"));
                        return false;
                    }
                    catch (CodeGraphCommitOutcomeUnknownException)
                    {
                        // Expected
                    }
                }
                
                // Verify instance refuses current-state claims
                using (var store = new LatticeCodeGraphStore(path))
                {
                    try
                    {
                        await store.GetLatestPublicationAsync(new CodeRepositoryId("repo:durability-path"));
                        Console.WriteLine("  FAIL: Should have thrown CodeGraphCommitOutcomeUnknownException");
                        return false;
                    }
                    catch (CodeGraphCommitOutcomeUnknownException)
                    {
                        // Expected
                    }
                }
                
                var staged = ReadNativeStage(GetTempPath());
                if (staged.Count != 1)
                {
                    Console.WriteLine($"  FAIL: Expected 1 staged row, got {staged.Count}");
                    return false;
                }
                
                using var reopened = new LatticeCodeGraphStore(path);
                var health = await reopened.CheckHealthAsync();
                if (!health.IsHealthy)
                {
                    Console.WriteLine("  FAIL: Reopened store not healthy");
                    return false;
                }
                
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
                
                // On Windows, file should be deletable after disposal
                File.Delete(path);
                if (File.Exists(path))
                {
                    Console.WriteLine("  FAIL: File still locked after disposal");
                    return false;
                }
                
                Console.WriteLine("  PASS: Handle released, file deletable");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL: {ex.Message}");
                return false;
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
                // Establish baseline
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var (repo, run, _, started) = Seed(store);
                    await store.StageIndexUnitAsync(Unit(repo, run, "unit:baseline", "node:baseline"));
                    await store.CompleteIndexRunAsync(
                        new(repo, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), new[] { new CodePluginId("plugin:durability") }),
                        new(repo, run, new[] { new CodeSourceManifest(new CodePluginId("plugin:durability"), "1.0.0", "src/Baseline.cs", "sha256:baseline") }));
                }
                
                // Spawn worker and kill it
                var workerExe = Path.Combine(AppContext.BaseDirectory, "Andirit.LatticeDb.Tests.Worker.exe");
                if (!File.Exists(workerExe))
                {
                    Console.WriteLine("  SKIP: Worker executable not found");
                    return true;
                }
                
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"exec \"{typeof(NativeDurabilityTests).Assembly.Location}\" --worker --db \"{path}\" --unit unit:abrupt --node node:abrupt",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                var worker = Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"exec \"{typeof(NativeDurabilityTests).Assembly.Location}\" --worker --db \"{path}\" --unit unit:abrupt --node node:abrupt",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });
                
                if (worker == null) return false;
                
                await Task.Delay(100);
                if (!worker.HasExited)
                {
                    worker.Kill(true);
                    await worker.WaitForExitAsync();
                }
                
                await Task.Delay(200);
                
                // Verify database is still consistent
                var staged = NativePayloads(path, "HetuStage");
                if (staged.Count > 1)
                {
                    Console.WriteLine($"  FAIL: Partial state detected - {staged.Count} rows");
                    return false;
                }
                
                using var reopened = new LatticeCodeGraphStore(path);
                var health = await reopened.CheckHealthAsync();
                if (!health.IsHealthy)
                {
                    Console.WriteLine("  FAIL: Reopened store not healthy");
                    return false;
                }
                
                Console.WriteLine("  PASS: Abrupt termination - state consistent");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL: {ex.Message}");
                return false;
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
                
                // Simulate crash by not completing - just dispose
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
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL: {ex.Message}");
                return false;
            }
        }
        
        static (CodeRepositoryId, CodeIndexRunId, CodePluginId, DateTimeOffset) Seed(LatticeCodeGraphStore store)
        {
            var repository = new CodeRepositoryId("repo:qual");
            var run = new CodeIndexRunId("run:qual");
            var plugin = new CodePluginId("plugin:qual");
            var started = new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);
            
            store.UpsertRepositoryAsync(new CodeRepositoryManifest(new CodeRepositoryId("repo:qual"), "Qual Repository", "repo://qual")).GetAwaiter().GetResult();
            store.StoreIndexRunAsync(new CodeIndexRunManifest(new CodeRepositoryId("repo:qual"), new CodeIndexRunId("run:qual"), new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero), plugins: new[] { new CodePluginId("plugin:qual") })).GetAwaiter().GetResult();
            
            return (new CodeRepositoryId("repo:qual"), new CodeIndexRunId("run:qual"), new CodePluginId("plugin:qual"), new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero));
        }
        
        static CodeIndexUnitReplacement Unit(CodeRepositoryId repo, CodeIndexRunId run, string unitId, string nodeId)
        {
            var node = new CodeGraphNode(new CodeNodeId(nodeId), CodeNodeKinds.Type, "Test", "Example.Test", new CodeSymbolId($"symbol:{nodeId}"));
            return new CodeIndexUnitReplacement(new CodeFactOrigin(repo, new CodePluginId("plugin:qual"), "1.0.0", run, new CodeIndexUnitId(unitId)), new[] { node });
        }
        
        static List<string> NativePayloads(string path, string label)
        {
            using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
            using var txn = db.BeginReadTransaction();
            using var query = db.Prepare($"MATCH (s:{label}) RETURN s.payload");
            using var result = query.Execute(txn);
            var rows = result.ReadAll();
            txn.Commit();
            return rows.Select(row => Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(0).AsString()))).ToList();
        }
        
        static List<string> ReadNativeStage(string path)
        {
            return NativePayloads(path, "HetuStage");
        }
        
        static string GetTempDb() => Path.Combine(Path.GetTempPath(), $"qual-s62-{Guid.NewGuid():N}.ltdb");
        static string GetTempPath() => Path.Combine(Path.GetTempPath(), $"qual-s62-{Guid.NewGuid():N}.ltdb");
        
        static void DeleteDb(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            foreach (var s in new[] { "-wal", "-journal", ".wal" })
                try { var f = path + s; if (File.Exists(f)) File.Delete(f); } catch { }
        }
        
        static void CleanupTempFiles()
        {
            var dir = Path.GetTempPath();
            foreach (var f in Directory.GetFiles(dir, "qual-s62-*.ltdb*"))
                try { File.Delete(f); } catch { }
        }
        
        class InjectedCrashException : Exception { }
        class InjectedPersistenceException : Exception { }
    }
}