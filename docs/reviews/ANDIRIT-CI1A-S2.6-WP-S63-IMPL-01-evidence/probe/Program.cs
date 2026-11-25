// ============================================================================
// PRODUCER-PREPARED REVIEWER PROBE HARNESS - ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01
// ============================================================================
//
// HONESTY NOTICE (read before trusting any output of this program):
// This harness was written by the PRODUCER of the S6.3 implementation. It is
// provided so the independent qualification commission (S63-QUAL-01) has a
// concrete, runnable starting point for its own diagnostic probes (A-H, from
// the entry qualification plan §4.4). It is NOT independent evidence and must
// not be represented as such anywhere. The reviewer is expected to re-derive
// the expectations from the governing contracts and to re-execute - or rewrite
// - these probes in its own session (qualification gate G-5).
//
// Each probe runs 3 times and prints one line per repetition:
//     <probe> <repetition> PASS|FAIL <detail>
// A probe "passes" only when every repetition passes. Exit code is 0 when all
// probes pass, 1 otherwise. Each probe uses its own temporary database and
// cleans up after itself.
//
// Probes (entry qualification plan §4.4):
//   A  stale occurrence index (older meta generation) -> explicit refusal
//   B  missing occurrence meta -> explicit refusal, never a silent empty success
//   C  failed staging (pre-commit) leaves the prior occurrence index intact
//   D  superseded publication fails to replace the newer accepted generation
//   E  ordinary reopen produces identical occurrence query results
//   F  rebuild from durable authority produces an equivalent occurrence index
//   G  the native file lock refuses a second open while a writer holds it
//   H  no interleaving/coordination seam exists on the pinned native surface
// ============================================================================

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.S63.Probe;

internal static class Program
{
    private static readonly CodePluginId Plugin = new("plugin:s63-probe");
    private static readonly DateTimeOffset Started = new(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    private static int Main()
    {
        Console.WriteLine("PRODUCER-PREPARED probes - not independent evidence (see header).");
        Console.WriteLine($"probe-run started {DateTimeOffset.UtcNow:O}");
        var failures = 0;
        foreach (var (name, probe) in new (string, Func<int, string>)[] {
            ("A", ProbeA_StaleIndexRefuses),
            ("B", ProbeB_MissingMetaRefuses),
            ("C", ProbeC_FailedStagingPreservesPrior),
            ("D", ProbeD_SupersessionRetainsNewest),
            ("E", ProbeE_ReopenEquivalent),
            ("F", ProbeF_RebuildEquivalent),
            ("G", ProbeG_FileLockRefusesSecondOpen),
            ("H", ProbeH_NoInterleavingSeam),
        })
        {
            for (var repetition = 1; repetition <= 3; repetition++)
            {
                string detail;
                try
                {
                    detail = probe(repetition);
                    Console.WriteLine($"{name} {repetition} PASS {detail}");
                }
                catch (Exception exception)
                {
                    failures++;
                    detail = exception.GetType().Name + ": " + exception.Message;
                    Console.WriteLine($"{name} {repetition} FAIL {detail}");
                }
            }
        }

        Console.WriteLine(failures == 0
            ? "probe-run complete: all repetitions passed"
            : $"probe-run complete: {failures} failing repetition(s)");
        return failures == 0 ? 0 : 1;
    }

    // A - a query pinned to a superseded generation refuses explicitly.
    private static string ProbeA_StaleIndexRefuses(int repetition)
    {
        var path = Db(repetition, "A");
        try
        {
            var repo = new CodeRepositoryId($"repo:probeA{repetition}");
            var target = Target(repo.Value, "T1");
            using var store = new LatticeCodeGraphStore(path);
            Publish(store, repo, "run:a1",
                Unit(repo, "run:a1", "unit:a", [Node("n:a")], [Fact(repo, "unit:a", target, "src/1.cs", 1, 1)]));
            var first = store.GetLatestPublicationAsync(repo).GetAwaiter().GetResult();
            Publish(store, repo, "run:a2",
                Unit(repo, "run:a2", "unit:a", [Node("n:a")], [Fact(repo, "unit:a", target, "src/2.cs", 1, 1)]));
            var stale = store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, first)).GetAwaiter().GetResult();
            if (stale.Availability != CodeOccurrenceAvailability.StalePublication || stale.Occurrences.Count != 0)
                throw new InvalidOperationException("stale generation was served");
            return $"availability={stale.Availability} occurrences=0";
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // B - an occurrence index without its manifest refuses at open (never a silent empty success).
    private static string ProbeB_MissingMetaRefuses(int repetition)
    {
        var path = Db(repetition, "B");
        try
        {
            var repo = new CodeRepositoryId($"repo:probeB{repetition}");
            var target = Target(repo.Value, "T1");
            using (var store = new LatticeCodeGraphStore(path))
            {
                Publish(store, repo, "run:b1",
                    Unit(repo, "run:b1", "unit:b", [Node("n:b")], [Fact(repo, "unit:b", target, "src/1.cs", 1, 1)]));
            }

            DeleteLabelRow(path, "AndiritOccurrenceMeta", repo.Value);
            try
            {
                new LatticeCodeGraphStore(path).Dispose();
                throw new InvalidOperationException("open succeeded without an occurrence manifest");
            }
            catch (Exception exception) when (exception.GetType().Name == "CodeOccurrenceSchemaException")
            {
                // The refusal type is internal to Andirit.LatticeDb, so it is identified by name at
                // runtime; this probe deliberately uses public API surface only.
                return $"explicit refusal: {exception.GetType().Name}: {exception.Message}";
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // C - a failed staging (rejected through the public contract) leaves the prior occurrence
    // publication and index intact.
    private static string ProbeC_FailedStagingPreservesPrior(int repetition)
    {
        var path = Db(repetition, "C");
        try
        {
            var repo = new CodeRepositoryId($"repo:probeC{repetition}");
            var target = Target(repo.Value, "T1");
            using var store = new LatticeCodeGraphStore(path);
            Publish(store, repo, "run:c1",
                Unit(repo, "run:c1", "unit:c", [Node("n:c")], [Fact(repo, "unit:c", target, "src/Prior.cs", 1, 1)]));
            store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:c2"), Later, plugins: [Plugin])).GetAwaiter().GetResult();

            // A unit carrying an immutable payload that conflicts with the accepted occurrence is
            // rejected through the public contract; the accepted state must remain intact.
            var conflicting = Fact(repo, "unit:c", target, "src/Prior.cs", 1, 1, "unit:c");
            var conflicted = new CodeOccurrenceFact(
                conflicting.Id, conflicting.TargetId, conflicting.TargetDocumentId, conflicting.RepositoryId,
                conflicting.Category, conflicting.SourceUnitId,
                new CodeOccurrenceContributor(Plugin, "unit:conflict"),
                conflicting.Span, conflicting.SourceHash, contentHash: "content:conflict",
                conflicting.DeclarationSpan, conflicting.SemanticsVersion);
            try
            {
                store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                    new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run:c2"), new CodeIndexUnitId("unit:conflict")),
                    [Node("n:conflict")],
                    occurrences: [conflicted],
                    occurrenceAnalysisIncluded: true)).GetAwaiter().GetResult();
                throw new InvalidOperationException("the conflicting staging was accepted");
            }
            catch (CodeGraphBatchRejectedException)
            {
            }

            var after = store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)).GetAwaiter().GetResult();
            if (after.Occurrences.Count != 1 || after.Occurrences[0].Span.Path != "src/Prior.cs")
                throw new InvalidOperationException("prior occurrence index was not preserved");
            return $"preserved run=run:c1 facts={after.Occurrences.Count}";
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // D - an older generation cannot replace the newer accepted generation.
    private static string ProbeD_SupersessionRetainsNewest(int repetition)
    {
        var path = Db(repetition, "D");
        try
        {
            var repo = new CodeRepositoryId($"repo:probeD{repetition}");
            var target = Target(repo.Value, "T1");
            using (var store = new LatticeCodeGraphStore(path))
            {
                Publish(store, repo, "run:d1",
                    Unit(repo, "run:d1", "unit:d", [Node("n:d")], [Fact(repo, "unit:d", target, "src/G1.cs", 1, 1)]));
                var first = store.GetLatestPublicationAsync(repo).GetAwaiter().GetResult();
                Publish(store, repo, "run:d2",
                    Unit(repo, "run:d2", "unit:d", [Node("n:d")], [Fact(repo, "unit:d", target, "src/G2.cs", 1, 1)]));
                var stale = store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, first)).GetAwaiter().GetResult();
                var current = store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)).GetAwaiter().GetResult();
                if (stale.Availability != CodeOccurrenceAvailability.StalePublication)
                    throw new InvalidOperationException("older generation was served");
                if (current.Publication!.IndexRunId.Value != "run:d2")
                    throw new InvalidOperationException("newest generation was not retained");
            }

            // Native read after the store released the file.
            var bound = NativeMetaRunId(path, repo.Value);
            if (bound != "run:d2")
                throw new InvalidOperationException($"native meta bound to {bound}");
            return "stale=StalePublication current=run:d2 nativeMeta=run:d2";
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // E - an ordinary reopen reproduces identical occurrence query results.
    private static string ProbeE_ReopenEquivalent(int repetition)
    {
        var path = Db(repetition, "E");
        try
        {
            var repo = new CodeRepositoryId($"repo:probeE{repetition}");
            var target = Target(repo.Value, "T1");
            string before;
            using (var store = new LatticeCodeGraphStore(path))
            {
                Publish(store, repo, "run:e1",
                    Unit(repo, "run:e1", "unit:e", [Node("n:e")],
                    [
                        Fact(repo, "unit:e", target, "src/A.cs", 3, 1),
                        Fact(repo, "unit:e", target, "src/A.cs", 1, 1),
                        Fact(repo, "unit:e", target, "src/B.cs", 2, 1),
                    ]));
                before = Describe(store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)).GetAwaiter().GetResult());
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var after = Describe(reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)).GetAwaiter().GetResult());
            if (before != after)
                throw new InvalidOperationException("reopen changed the occurrence index");
            return "identical after reopen";
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // F - the durable-authority rebuild (triggered by a rejected mutation) reproduces the index.
    private static string ProbeF_RebuildEquivalent(int repetition)
    {
        var path = Db(repetition, "F");
        try
        {
            var repo = new CodeRepositoryId($"repo:probeF{repetition}");
            var target = Target(repo.Value, "T1");
            using var store = new LatticeCodeGraphStore(path);
            Publish(store, repo, "run:f1",
                Unit(repo, "run:f1", "unit:f", [Node("n:f")],
                [
                    Fact(repo, "unit:f", target, "src/A.cs", 2, 1),
                    Fact(repo, "unit:f", target, "src/B.cs", 1, 1),
                ]));
            var before = Describe(store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)).GetAwaiter().GetResult());

            // A rejected staging routes through the store's recovery path, which rebuilds the
            // projection from durable authority; the accepted index must be reproduced exactly.
            store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:f2"), Later, plugins: [Plugin])).GetAwaiter().GetResult();
            var conflicting = Fact(repo, "unit:f", target, "src/A.cs", 2, 1, "unit:f");
            var conflicted = new CodeOccurrenceFact(
                conflicting.Id, conflicting.TargetId, conflicting.TargetDocumentId, conflicting.RepositoryId,
                conflicting.Category, conflicting.SourceUnitId,
                new CodeOccurrenceContributor(Plugin, "unit:conflict"),
                conflicting.Span, conflicting.SourceHash, contentHash: "content:conflict",
                conflicting.DeclarationSpan, conflicting.SemanticsVersion);
            try
            {
                store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                    new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run:f2"), new CodeIndexUnitId("unit:conflict")),
                    [Node("n:conflict")],
                    occurrences: [conflicted],
                    occurrenceAnalysisIncluded: true)).GetAwaiter().GetResult();
                throw new InvalidOperationException("the conflicting staging was accepted");
            }
            catch (CodeGraphBatchRejectedException)
            {
            }

            var after = Describe(store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)).GetAwaiter().GetResult());
            if (before != after)
                throw new InvalidOperationException("the rebuild changed the occurrence index");
            return "rebuild reproduced the accepted index";
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // G - the native file lock refuses a second open while a writer holds it.
    private static string ProbeG_FileLockRefusesSecondOpen(int repetition)
    {
        var path = Db(repetition, "G");
        try
        {
            var repo = new CodeRepositoryId($"repo:probeG{repetition}");
            var first = new LatticeCodeGraphStore(path);
            try
            {
                try
                {
                    var second = new LatticeCodeGraphStore(path);
                    second.Dispose();
                    throw new InvalidOperationException("a second open succeeded while the first writer held the lock");
                }
                catch (Exception exception) when (exception is not InvalidOperationException)
                {
                    return $"second open refused: {exception.GetType().Name}";
                }
            }
            finally
            {
                first.Dispose();
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // H - the pinned native surface exposes no interleaving/coordination seam.
    private static string ProbeH_NoInterleavingSeam(int repetition)
    {
        var assembly = typeof(LatticeDatabase).Assembly;
        if (assembly.GetName().Version! != new Version(0, 2, 0, 0))
            throw new InvalidOperationException("pinned dependency version changed");
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
        var offenders = members.Where(name =>
            name.Contains("Interleav", StringComparison.Ordinal) ||
            name.Contains("Coordinator", StringComparison.Ordinal) ||
            name.Contains("Schedule", StringComparison.Ordinal) ||
            name.Contains("Hook", StringComparison.Ordinal)).ToList();
        if (offenders.Count > 0)
            throw new InvalidOperationException("interleaving-like surface: " + string.Join(",", offenders));
        return $"members scanned={members.Count} offenders=0";
    }

    // ------------------------------------------------------------------ helpers

    private static string Describe(CodeOccurrenceQueryResult result) =>
        string.Join("|",
            $"availability={result.Availability}",
            $"publication={result.Publication?.IndexRunId.Value}",
            $"examined={result.Examined}",
            string.Join(",", result.Occurrences.Select(hit =>
                $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Id.Value}")));

    private static CodeOccurrenceTargetId Target(string repositoryId, string name)
    {
        var context = "compctx1:sha256:" + Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(repositoryId + "|" + name)));
        return CodeOccurrenceIdentity.ComputeTargetId(
            context,
            "meta:" + name,
            "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static CodeOccurrenceFact Fact(
        CodeRepositoryId repo, string unitId, CodeOccurrenceTargetId target, string path, int line, int column,
        string? semanticUnitId = null) =>
        CodeOccurrenceFact.Create(
            repo, target, target.Value, semanticUnitId ?? unitId,
            new CodeOccurrenceContributor(Plugin, unitId),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            sourceHash: "sha256:source:" + path,
            contentHash: "content:invocation");

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId($"symbol:{id}"));

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo, string run, string unit,
        IReadOnlyList<CodeGraphNode> nodes, IReadOnlyList<CodeOccurrenceFact> occurrences) =>
        new(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            nodes,
            occurrences: occurrences,
            occurrenceAnalysisIncluded: occurrences.Count > 0,
            occurrenceCoverage: occurrences.Count > 0
                ? new CodeOccurrenceCoverage(occurrences.Count, 0, CodeRelationshipCoverageState.Produced,
                    declaredUnitCount: 1, verifiedUnitCount: 1)
                : null);

    private static void Publish(
        LatticeCodeGraphStore store, CodeRepositoryId repo, string run, params CodeIndexUnitReplacement[] units)
    {
        var runId = new CodeIndexRunId(run);
        store.UpsertRepositoryAsync(new(repo)).GetAwaiter().GetResult();
        store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin])).GetAwaiter().GetResult();
        foreach (var unit in units)
            store.StageIndexUnitAsync(unit).GetAwaiter().GetResult();
        store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, runId, [])).GetAwaiter().GetResult();
    }

    private static string NativeMetaRunId(string path, string repositoryId)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginWriteTransaction();
        string? runId = null;
        using (var query = database.Prepare("MATCH (s:AndiritOccurrenceMeta) RETURN s.payload"))
        using (var result = query.Execute(txn))
        {
            foreach (var payload in result.ReadAll()
                         .Select(row => Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(0).AsString()))))
            {
                var meta = System.Text.Json.JsonSerializer.Deserialize<ProbeMetaShim>(payload);
                if (meta is not null && meta.RepositoryId == repositoryId)
                    runId = meta.PublicationIndexRunId;
            }
        }

        txn.Commit();
        if (runId is null)
            throw new InvalidOperationException("no occurrence meta for " + repositoryId);
        return runId;
    }

    /// <summary>Local projection of the durable occurrence meta row (the store's record is internal).</summary>
    private sealed record ProbeMetaShim(string RepositoryId, string PublicationIndexRunId);

    private static void DeleteLabelRow(string path, string label, string key)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginWriteTransaction();
        using (var query = database.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s"))
        {
            query.Bind("k", LatticeValue.From(key));
            using var result = query.Execute(txn);
            result.ReadAll();
        }

        txn.Commit();
    }

    private static string Db(int repetition, string probe) =>
        Path.Combine(Path.GetTempPath(), $"andirit-s63-probe-{probe}{repetition}-{Guid.NewGuid():N}.ltdb");

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
    }

    // The pinned fault-injector seam is internal to Andirit.LatticeDb (InternalsVisibleTo is
    // restricted to the test assembly), so these probes deliberately exercise failure and recovery
    // through the public contract only. No production change was made to widen visibility.
}
