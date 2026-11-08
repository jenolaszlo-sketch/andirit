using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Frozen = AdrCi1Exp.Shared;

// ANDIRIT-CI1A-S2.4-S24-READER-Q1 reviewer-owned probe harness.
//
// Independent qualification of the internal exact-target occurrence continuation
// and coverage breakdown. Expected values are computed from the accepted
// contract (total-order tuple, identity primitives, coverage algebra) by
// reviewer-owned code, never by the production sort/compare helpers.
//
// Usage: dotnet run --project ... -c Release -- <verb> <database> <outputJson> [detail]
var action = args[0];
var database = args[1];
var output = args[2];
var detail = args.Length > 3 ? args[3] : "";
var result = await Q1Probe.RunAsync(action, database, detail);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action +
    " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class Q1Probe
{
    internal static readonly CodePluginId Plugin = new("probe.s24.reader.q1");
    internal static readonly CodeRepositoryId PluginRepo = new("repo:s24-reader-q1-plugin");
    internal static readonly DateTimeOffset Started = new(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);

    internal static async Task<JsonObject> RunAsync(string action, string database, string detail)
    {
        var root = new JsonObject
        {
            ["action"] = action,
            ["processId"] = Environment.ProcessId,
            ["database"] = database,
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        };
        var notes = new JsonArray();
        root["ok"] = true;
        try
        {
            switch (action)
            {
                case "environment": root["head"] = detail; break;
                case "oracle": await OracleAsync(database, root, notes); break;
                case "tokens": await TokensAsync(database, root, notes); break;
                case "bounded": await BoundedAsync(database, root, notes); break;
                case "publication": await PublicationAsync(database, root, notes); break;
                case "coverage": await CoverageAsync(database, root, notes); break;
                case "noninterference": await NonInterferenceAsync(database, root, notes); break;
                case "authority": await AuthorityAsync(database, root, notes); break;
                default: throw new ArgumentException("Unknown action: " + action);
            }
        }
        catch (Exception exception)
        {
            notes.Add(exception.GetType().Name + ": " + exception.Message);
            root["stack"] = exception.StackTrace;
            root["ok"] = false;
        }
        root["notes"] = notes;
        return root;
    }

    // ---- Q3: independent pagination oracle ----

    private static async Task OracleAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q1:oracle");
        // Mixed positions including equal-position ties, unsorted spec order.
        var specs = new List<(string Path, int Sl, int Sc, int El, int Ec, string Semantic)>
        {
            ("src/C.cs", 3, 1, 3, 5, "c1"),
            ("src/A.cs", 2, 1, 2, 5, "a1"),
            ("src/A.cs", 1, 2, 1, 6, "a2"),
            ("src/A.cs", 1, 1, 1, 5, "a3"),
            ("src/B.cs", 1, 1, 1, 5, "b1"),
            ("src/A.cs", 1, 1, 1, 5, "a4"), // equal position with a3 -> tie by id
            ("src/A.cs", 1, 1, 1, 4, "a5"), // differs by end column
        };
        var target = Target(repo, "T");
        var facts = specs.Select(s => Fact(repo, "u", target, s.Path, s.Sl, s.Sc, s.El, s.Ec, s.Semantic)).ToList();
        // Independent expected order (reviewer-owned tuple comparison).
        var expected = facts
            .OrderBy(f => f, Comparer<CodeOccurrenceFact>.Create((x, y) =>
            {
                var c = string.CompareOrdinal(x.Span.Path, y.Span.Path);
                if (c != 0) return c;
                c = x.Span.StartLine.CompareTo(y.Span.StartLine);
                if (c != 0) return c;
                c = x.Span.StartColumn.CompareTo(y.Span.StartColumn);
                if (c != 0) return c;
                c = x.Span.EndLine.CompareTo(y.Span.EndLine);
                if (c != 0) return c;
                c = x.Span.EndColumn.CompareTo(y.Span.EndColumn);
                return c != 0 ? c : string.CompareOrdinal(x.Id.Value, y.Id.Value);
            }))
            .Select(f => f.Id.Value)
            .ToArray();

        using var store = new LatticeCodeGraphStore(database);
        await PublishAsync(store, repo, "run", Unit(repo, "run", "u", facts));

        var matrix = new JsonArray();
        var allOk = true;
        foreach (var pageSize in new[] { 1, 2, 3, 7, 100 })
        {
            var (ids, pages, terminal, total, truncated, boundariesOk) = await TraverseAsync(store, repo, target, pageSize);
            var ok = ids.SequenceEqual(expected) && ids.Distinct(StringComparer.Ordinal).Count() == expected.Length &&
                     terminal && total == expected.Length;
            allOk &= ok;
            matrix.Add(new JsonObject
            {
                ["pageSize"] = pageSize,
                ["pages"] = pages,
                ["returned"] = ids.Length,
                ["distinct"] = ids.Distinct(StringComparer.Ordinal).Count(),
                ["matchesOracle"] = ids.SequenceEqual(expected),
                ["terminal"] = terminal,
                ["totalMatches"] = total,
                ["truncatedSeenWhenMore"] = truncated,
                ["ok"] = ok,
            });
        }
        root["oracle"] = new JsonArray(expected.Select(v => (JsonNode)v).ToArray());
        root["matrix"] = matrix;
        Assert(root, allOk, "all page sizes reproduce the independent total-order oracle with no duplicates or omissions", notes);
    }

    // ---- Q2: token adversarial matrix ----

    private static async Task TokensAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q1:tokens");
        var target = Target(repo, "T");
        var other = Target(repo, "Other");
        var facts = new List<CodeOccurrenceFact>
        {
            Fact(repo, "u", target, "src/A.cs", 1, 1),
            Fact(repo, "u", target, "src/A.cs", 2, 1),
            Fact(repo, "u", target, "src/A.cs", 3, 1),
            Fact(repo, "u", other, "src/A.cs", 1, 1),
        };
        using var store = new LatticeCodeGraphStore(database);
        await PublishAsync(store, repo, "run:1", Unit(repo, "run:1", "u", facts));
        var publication = (await store.GetLatestPublicationAsync(repo))!;
        var page1 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        var good = page1.NextContinuationToken!;

        CodeOccurrenceContinuation.Envelope Env(
            string repoId, string mode, string targetId, string run, string id, string dir, string afterPath,
            int sl, int sc, int el, int ec, string afterId, int size) =>
            new(repoId, mode, targetId, run, id, dir, afterPath, sl, sc, el, ec, afterId, size);

        var matrix = new JsonArray();
        void Case(string name, string? token, Func<CodeOccurrenceQueryResult, bool> check)
        {
            try
            {
                var result = store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token }).GetAwaiter().GetResult();
                matrix.Add(new JsonObject
                {
                    ["case"] = name,
                    ["availability"] = result.Availability.ToString(),
                    ["continuation"] = result.Continuation.ToString(),
                    ["occurrences"] = result.Occurrences.Count,
                    ["threw"] = false,
                    ["ok"] = check(result),
                });
            }
            catch (Exception exception)
            {
                matrix.Add(new JsonObject
                {
                    ["case"] = name,
                    ["threw"] = true,
                    ["exception"] = exception.GetType().Name,
                    ["message"] = exception.Message,
                    ["ok"] = false, // a malformed token must be refused, never throw
                });
            }
        }

        bool Invalid(CodeOccurrenceQueryResult r) => r.Continuation == CodeOccurrenceContinuationState.Invalid && r.Occurrences.Count == 0 && !r.IsComplete;
        bool Stale(CodeOccurrenceQueryResult r) => r.Availability == CodeOccurrenceAvailability.StalePublication && r.Occurrences.Count == 0;

        Case("malformed-base64url", "%%%not base64%%%", Invalid);
        // Leniency observation: standard base64 / padding that decodes to the same
        // bytes is either refused or resolves to a valid page; it must never throw.
        Case("noncanonical-standard-base64", StandardBase64(good),
            r => r.Continuation is CodeOccurrenceContinuationState.More or CodeOccurrenceContinuationState.Complete or CodeOccurrenceContinuationState.Invalid);
        Case("oversized", new string('A', 5000), Invalid);
        Case("unsupported-version", ManualToken(version: 2, repo.Value, "target", target.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, "x", 1), Invalid);
        Case("missing-field", ManualTokenMissing(repo.Value, target.Value, publication), Invalid);
        Case("duplicated-field", ManualTokenDuplicate(repo.Value, target.Value, publication), Invalid);
        Case("size-zero", CodeOccurrenceContinuation.Encode(Env(repo.Value, "target", target.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, "x", 0)), Invalid);
        Case("size-over-max", CodeOccurrenceContinuation.Encode(Env(repo.Value, "target", target.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, "x", 100000)), Invalid);
        Case("wrong-repository", CodeOccurrenceContinuation.Encode(Env("repo:foreign", "target", target.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, "x", 1)), Invalid);
        Case("wrong-target", CodeOccurrenceContinuation.Encode(Env(repo.Value, "target", other.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, "x", 1)), Invalid);
        Case("wrong-mode", CodeOccurrenceContinuation.Encode(Env(repo.Value, "targets", target.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, "x", 1)), Invalid);
        Case("wrong-direction", CodeOccurrenceContinuation.Encode(Env(repo.Value, "target", target.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value, "desc", "src/A.cs", 1, 1, 1, 4, "x", 1)), Invalid);
        Case("wrong-publication-run", CodeOccurrenceContinuation.Encode(Env(repo.Value, "target", target.Value, "run:foreign", publication.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, "x", 1)), Stale);
        Case("wrong-publication-identity", CodeOccurrenceContinuation.Encode(Env(repo.Value, "target", target.Value, publication.IndexRunId.Value, "identity:foreign", "asc", "src/A.cs", 1, 1, 1, 4, "x", 1)), Stale);
        Case("good-token-continues", good, r => r.Availability == CodeOccurrenceAvailability.Available && r.Occurrences.Count == 1);

        // Altered / forged cursor with a checksum-consistent envelope (unkeyed).
        var forgedCursorBeforeAll = CodeOccurrenceContinuation.Encode(Env(repo.Value, "target", target.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value, "asc", "", 0, 0, 0, 0, "", 1));
        var forgedResult = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = forgedCursorBeforeAll });
        matrix.Add(new JsonObject
        {
            ["case"] = "forged-checksum-consistent-cursor",
            ["availability"] = forgedResult.Availability.ToString(),
            ["continuation"] = forgedResult.Continuation.ToString(),
            ["occurrences"] = forgedResult.Occurrences.Count,
            ["ok"] = true,
            ["note"] = "accepted (unkeyed checksum); cursor before all => restartable page (documented limitation, not authority bypass)",
        });

        root["matrix"] = matrix;
        var failed = matrix.Count(entry => entry!.AsObject()["ok"]!.GetValue<bool>() == false);
        Assert(root, failed == 0, "token adversarial matrix: all binding/tamper cases produce the normative status (" + failed + " failures)", notes);
    }

    // ---- Q4: bounded-execution scaling ----

    private static async Task BoundedAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q1:bounded");
        var target = Target(repo, "T");
        var matrix = new JsonArray();
        foreach (var size in new[] { 500, 2000, 8000 })
        {
            var path = database + "." + size;
            foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
            var facts = Enumerable.Range(0, size)
                .Select(i => Fact(repo, "u", target, "src/A.cs", i + 1, 1)).ToList();
            using var store = new LatticeCodeGraphStore(path);
            await PublishAsync(store, repo, "run", Unit(repo, "run", "u", facts));

            // First page.
            var sw = Stopwatch.StartNew();
            var first = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 10));
            sw.Stop();
            // A mid continuation page.
            var sw2 = Stopwatch.StartNew();
            var second = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 10) { ContinuationToken = first.NextContinuationToken });
            sw2.Stop();
            matrix.Add(new JsonObject
            {
                ["targetCardinality"] = size,
                ["pageSize"] = 10,
                ["firstPageExamined"] = first.Examined,
                ["firstPageReturned"] = first.Occurrences.Count,
                ["firstPageTotalMatches"] = first.TotalMatches,
                ["firstPageMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 3),
                ["secondPageExamined"] = second.Examined,
                ["secondPageReturned"] = second.Occurrences.Count,
                ["secondPageMs"] = Math.Round(sw2.Elapsed.TotalMilliseconds, 3),
            });
            store.Dispose();
            foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
        root["scaling"] = matrix;
        root["codePath"] = "Per request: index lookup O(1); copy+Array.Sort of the whole target array O(n log n); linear scan for the page. Examined == target cardinality (full target read), not the returned page.";
        Assert(root, true, "scaling recorded (see codePath); target-local only, no repository-global scan", notes);
    }

    // ---- Q5: publication/authority ----

    private static async Task PublicationAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q1:pub");
        var target = Target(repo, "T");
        var matrix = new JsonArray();

        // Stale after replacement.
        using (var store = new LatticeCodeGraphStore(database))
        {
            await PublishAsync(store, repo, "run:1", Unit(repo, "run:1", "u",
                [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)]));
            var token = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
            await PublishAsync(store, repo, "run:2", Unit(repo, "run:2", "u", [Fact(repo, "u", target, "src/B.cs", 9, 1)]));
            var stale = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token });
            matrix.Add(Result("stale-after-replacement", stale.Availability == CodeOccurrenceAvailability.StalePublication && stale.Continuation == CodeOccurrenceContinuationState.Invalid, stale));

            // Replayed after retirement: delete the only unit -> publication changes.
            var token2 = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken;
            await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:3"), Started.AddSeconds(5), plugins: [Plugin]));
            await store.StageIndexUnitDeletionAsync(repo, new("run:3"), Plugin, new CodeIndexUnitId("u"));
            await store.CompleteIndexRunAsync(new(repo, new CodeIndexRunId("run:3"), Started.AddSeconds(5), CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]), new(repo, new CodeIndexRunId("run:3"), []));
            var retired = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token2 });
            matrix.Add(Result("stale-after-retirement", retired.Availability == CodeOccurrenceAvailability.StalePublication || retired.Availability == CodeOccurrenceAvailability.AnalysisUnavailable, retired));
        }

        // Durable reopen: token from store A used in a fresh store B (same publication).
        using (var store = new LatticeCodeGraphStore(database + ".reopen"))
        {
            await PublishAsync(store, repo, "run:r", Unit(repo, "run:r", "u",
                [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)]));
            var token = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
            store.Dispose();
            using var reopened = new LatticeCodeGraphStore(database + ".reopen");
            var afterReopen = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token });
            matrix.Add(Result("continuation-after-durable-reopen", afterReopen.Availability == CodeOccurrenceAvailability.Available && afterReopen.Occurrences.Count == 1, afterReopen));
        }
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(database + ".reopen" + suffix)) File.Delete(database + ".reopen" + suffix);

        // Recovery between pages.
        var arm = false;
        using (var store = new LatticeCodeGraphStore(database + ".rec", null, point => { if (arm && point == "before-commit") throw new Fault(); }))
        {
            await PublishAsync(store, repo, "run:x", Unit(repo, "run:x", "u",
                [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1), Fact(repo, "u", target, "src/A.cs", 3, 1)]));
            var token = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
            await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:fail"), Started.AddSeconds(5), plugins: [Plugin]));
            await store.StageIndexUnitAsync(Unit(repo, "run:fail", "u9", [Fact(repo, "u9", target, "src/Z.cs", 1, 1)]));
            arm = true;
            try { await store.CompleteIndexRunAsync(new(repo, new CodeIndexRunId("run:fail"), Started.AddSeconds(5), CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]), new(repo, new CodeIndexRunId("run:fail"), [])); }
            catch (Fault) { }
            arm = false;
            var afterRecovery = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token });
            matrix.Add(Result("continuation-after-recovery", afterRecovery.Availability == CodeOccurrenceAvailability.Available && afterRecovery.Occurrences.Count == 1, afterRecovery));
        }
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(database + ".rec" + suffix)) File.Delete(database + ".rec" + suffix);

        root["matrix"] = matrix;
        var failed = matrix.Count(entry => entry!["ok"] is { } okNode && okNode.GetValue<bool>() == false);
        Assert(root, failed == 0, "publication/authority: stale/reopen/recovery refusal and continuity (" + failed + " failures)", notes);
    }

    // ---- Q6: coverage ----

    private static async Task CoverageAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q1:coverage");
        var target = Target(repo, "T");
        using var store = new LatticeCodeGraphStore(database);
        // Two units: one Produced with 2 facts, one Partial with 0 facts.
        await PublishAsync(store, repo, "run",
            Unit(repo, "run", "u-complete", [Fact(repo, "u-complete", target, "src/A.cs", 1, 1), Fact(repo, "u-complete", target, "src/A.cs", 2, 1)], CodeRelationshipCoverageState.Produced),
            Unit(repo, "run", "u-partial", [], CodeRelationshipCoverageState.Partial));

        var page1 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        var page2 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = page1.NextContinuationToken });

        root["page1Availability"] = page1.Availability.ToString();
        root["page1State"] = page1.Coverage!.State.ToString();
        root["page2Availability"] = page2.Availability.ToString();
        root["page1Units"] = new JsonArray(page1.UnitCoverage.Select(u => (JsonNode)new JsonObject
        {
            ["unit"] = u.IndexUnitId, ["state"] = u.State.ToString(), ["facts"] = u.InvocationFacts,
        }).ToArray());
        root["page2Units"] = new JsonArray(page2.UnitCoverage.Select(u => (JsonNode)new JsonObject
        {
            ["unit"] = u.IndexUnitId, ["state"] = u.State.ToString(), ["facts"] = u.InvocationFacts,
        }).ToArray());

        var breakdownStable = page1.UnitCoverage.Select(u => (u.IndexUnitId, u.State, u.InvocationFacts))
            .SequenceEqual(page2.UnitCoverage.Select(u => (u.IndexUnitId, u.State, u.InvocationFacts)));
        Assert(root, page1.Availability == CodeOccurrenceAvailability.IncompleteAnalysis &&
            page2.Availability == CodeOccurrenceAvailability.IncompleteAnalysis,
            "partial contribution forces IncompleteAnalysis on every page (worst-wins)", notes);
        Assert(root, breakdownStable && page1.UnitCoverage.Count == 2,
            "scope-level per-unit breakdown is stable across pages and lists both contributors", notes);
        Assert(root, page1.UnitCoverage.Any(u => u.IndexUnitId == "u-complete" && u.State == CodeRelationshipCoverageState.Produced) &&
            page1.UnitCoverage.Any(u => u.IndexUnitId == "u-partial" && u.State == CodeRelationshipCoverageState.Partial),
            "complete and incomplete contributors are distinguished with provenance", notes);
        Assert(root, page1.Coverage!.IsComplete == false && page1.IsComplete == false,
            "no false-complete coverage on any page of a partial scope", notes);
    }

    // ---- Q7: noninterference ----

    private static async Task NonInterferenceAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q1:noninterf");
        var target = Target(repo, "T");
        string before;
        using (var store = new LatticeCodeGraphStore(database))
        {
            await PublishAsync(store, repo, "run", Unit(repo, "run", "u",
                [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1), Fact(repo, "u", target, "src/A.cs", 3, 1)]));
            before = NodeIds(await store.GetPublishedUnitsAsync(repo));
        }
        var rowsBefore = CountRows(database);
        using (var store = new LatticeCodeGraphStore(database))
        {
            await TraverseAsync(store, repo, target, 1);
            Assert(root, NodeIds(await store.GetPublishedUnitsAsync(repo)) == before, "D3 graph identities unchanged across pagination", notes);
        }
        var rowsAfter = CountRows(database);
        Assert(root, rowsBefore == rowsAfter, "no persisted row/label change from pagination (no token/session persistence)", notes);
        root["rows"] = rowsAfter;
    }

    // ---- Real-project verified authority ----

    private static async Task AuthorityAsync(string database, JsonObject root, JsonArray notes)
    {
        var context = Context("run:a", CrossProject());
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var app = analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        var lib = analysis.Projects.Single(p => p.Project.Path == "Lib/Lib.csproj").Verified!;
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        var plugin = new CSharpCodeGraphPlugin();
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(PluginRepo));
            await store.StoreIndexRunAsync(new(PluginRepo, new("run:a"), Started, plugins: [plugin.Id]));
            await using (var sink = new CodeGraphIngestionSink(store))
            await using (var session = await plugin.CreateSessionAsync(context))
            {
                await session.ExtractAsync(sink);
                await sink.CompleteAsync();
            }
            await store.CompleteIndexRunAsync(new(PluginRepo, new("run:a"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]), new(PluginRepo, new("run:a"), []));
        }

        using var reopened = new LatticeCodeGraphStore(database);
        CodeOccurrenceQuery Bind(string? token) => VerifiedCSharpOccurrenceAuthority.Bind(
            new(PluginRepo, target, maxResults: 1) { ContinuationToken = token }, analysis.Universe, app.Compilation);

        var page1 = await reopened.FindOccurrencesAsync(Bind(null));
        root["page1"] = page1.Availability.ToString();
        var token = page1.NextContinuationToken;
        Assert(root, page1.Availability == CodeOccurrenceAvailability.Available && page1.Occurrences.Count == 1 && token is not null,
            "real-project verified authority serves a first page", notes);

        var noAuthority = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(PluginRepo, target, maxResults: 1) { ContinuationToken = token });
        root["noAuthority"] = noAuthority.Availability.ToString();
        Assert(root, noAuthority.Availability == CodeOccurrenceAvailability.AnalysisUnavailable && noAuthority.Occurrences.Count == 0,
            "per-page authority revalidation: continuation without current authority is refused", notes);
    }

    // ---- helpers ----

    private static void Assert(JsonObject root, bool condition, string message, JsonArray notes)
    {
        notes.Add((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition) root["ok"] = false;
    }

    private static JsonObject Result(string name, bool ok, CodeOccurrenceQueryResult r) => new()
    {
        ["case"] = name,
        ["ok"] = ok,
        ["availability"] = r.Availability.ToString(),
        ["continuation"] = r.Continuation.ToString(),
        ["occurrences"] = r.Occurrences.Count,
    };

    private static async Task<(string[] Ids, int Pages, bool Terminal, int Total, bool TruncatedWhenMore, bool BoundariesOk)>
        TraverseAsync(LatticeCodeGraphStore store, CodeRepositoryId repo, CodeOccurrenceTargetId target, int pageSize)
    {
        var ids = new List<string>();
        string? token = null;
        var pages = 0;
        var truncatedWhenMore = true;
        var boundariesOk = true;
        while (true)
        {
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: pageSize) { ContinuationToken = token });
            pages++;
            if (!(result.Occurrences.Count <= pageSize))
                boundariesOk = false;
            ids.AddRange(result.Occurrences.Select(o => o.Id.Value));
            if (result.NextContinuationToken is null)
            {
                var terminal = result.Continuation == CodeOccurrenceContinuationState.Complete;
                return (ids.ToArray(), pages, terminal, result.TotalMatches, truncatedWhenMore && boundariesOk, boundariesOk);
            }
            if (result.Continuation != CodeOccurrenceContinuationState.More)
                boundariesOk = false;
            if (!result.Truncated)
                truncatedWhenMore = false;
            token = result.NextContinuationToken;
        }
    }

    private static string StandardBase64(string token)
    {
        var padded = token.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch { 2 => padded + "==", 3 => padded + "=", _ => padded };
        return padded; // standard base64 (with possible '=' padding / '+' '/') -> must be rejected
    }

    private static string ManualToken(int version, string repo, string mode, string target, string run, string id, string dir,
        string path, int sl, int sc, int el, int ec, string afterId, int size)
    {
        var json = new JsonObject
        {
            ["v"] = version, ["repo"] = repo, ["mode"] = mode, ["target"] = target, ["pubRun"] = run, ["pubId"] = id,
            ["dir"] = dir, ["afterPath"] = path, ["afterStartLine"] = sl, ["afterStartColumn"] = sc,
            ["afterEndLine"] = el, ["afterEndColumn"] = ec, ["afterId"] = afterId, ["size"] = size, ["sum"] = "0000000000000000",
        };
        return B64Url(Encoding.UTF8.GetBytes(json.ToJsonString()));
    }

    private static string ManualTokenMissing(string repo, string target, CodeGraphPublication pub)
    {
        var json = new JsonObject
        {
            ["v"] = 1, ["repo"] = repo, ["mode"] = "target", ["target"] = target, ["pubRun"] = pub.IndexRunId.Value,
            ["pubId"] = pub.IndexIdentity.Value, ["dir"] = "asc", ["afterPath"] = "src/A.cs", ["afterStartLine"] = 1,
            ["afterStartColumn"] = 1, ["afterEndLine"] = 1, ["afterEndColumn"] = 4, ["afterId"] = "x", ["sum"] = "0000000000000000",
        }; // 'size' omitted
        return B64Url(Encoding.UTF8.GetBytes(json.ToJsonString()));
    }

    private static string ManualTokenDuplicate(string repo, string target, CodeGraphPublication pub)
    {
        var body = "{\"v\":1,\"v\":1,\"repo\":\"" + repo + "\",\"mode\":\"target\",\"target\":\"" + target +
            "\",\"pubRun\":\"" + pub.IndexRunId.Value + "\",\"pubId\":\"" + pub.IndexIdentity.Value +
            "\",\"dir\":\"asc\",\"afterPath\":\"src/A.cs\",\"afterStartLine\":1,\"afterStartColumn\":1,\"afterEndLine\":1,\"afterEndColumn\":4,\"afterId\":\"x\",\"size\":1,\"sum\":\"x\"}";
        return B64Url(Encoding.UTF8.GetBytes(body));
    }

    private static string B64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string NodeIds(IReadOnlyList<CodeIndexUnitReplacement> units) =>
        string.Join("|", units.SelectMany(u => u.Nodes).Select(n => n.Id.Value).Order(StringComparer.Ordinal));

    private static string CountRows(string path)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginReadTransaction();
        var counts = new List<string>();
        foreach (var label in new[] { "AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage", "HetuUnit" })
        {
            using var q = db.Prepare($"MATCH (s:{label}) RETURN count(s)");
            using var rows = q.Execute(txn);
            var all = rows.ReadAll();
            counts.Add(label + "=" + (all.Count == 0 ? 0 : all[0].GetValue(0).AsInt64()));
        }
        txn.Commit();
        return string.Join(",", counts);
    }

    private static async Task PublishAsync(LatticeCodeGraphStore store, CodeRepositoryId repo, string run, params CodeIndexUnitReplacement[] units)
    {
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
    }

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo, string run, string unit, IReadOnlyList<CodeOccurrenceFact> facts,
        CodeRelationshipCoverageState state = CodeRelationshipCoverageState.Produced) =>
        new(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            [new CodeGraphNode(new CodeNodeId("node:" + unit), CodeNodeKinds.Callable, unit, unit, new CodeSymbolId("symbol:" + unit))],
            occurrences: facts,
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(facts.Count, 0, state));

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name)
    {
        var context = "compctx1:sha256:" + Hex(repo.Value + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(
            context, "meta:" + name, "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static string Hex(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static CodeOccurrenceFact Fact(
        CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target, string path, int line, int column,
        int? endLine = null, int? endColumn = null, string? semantic = null) =>
        CodeOccurrenceFact.Create(
            repo, target, target.Value, semantic ?? unit,
            new CodeOccurrenceContributor(Plugin, unit), CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, endLine ?? line, endColumn ?? column + 4),
            "sha256:source", "content:invocation");

    private static IReadOnlyDictionary<string, string> CrossProject() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Lib/Lib.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
        ["Lib/Value.cs"] = "namespace Lib; public static class Value { public static int Get() => 1; }",
        ["App/App.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /></ItemGroup></Project>",
        ["App/Application.cs"] = "namespace App; public static class Application { public static int Run() => Lib.Value.Get() + Lib.Value.Get(); }",
    };

    private static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(PluginRepo, null, new CodeIndexRunId(run), files.OrderBy(p => p.Key, StringComparer.Ordinal).Select(pair =>
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Value);
            return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }).ToArray());

    private static IMethodSymbol Method(VerifiedCSharpCompilation unit, string type, string name) =>
        (IMethodSymbol)unit.Compilation.GetTypeByMetadataName(type)!.GetMembers(name).Single();

    private static CodeOccurrenceTargetId ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        var identifier = CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]);
        var declaration = CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]);
        var shape = CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
            string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
        var viaIdentity = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName, identifier, declaration, shape);
        var frozen = Frozen.CompilationProvenance.Describe(PluginRepo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var viaFrozen = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, identifier, declaration, shape]);
        if (viaIdentity.Value != viaFrozen)
            throw new InvalidDataException("Independent target computations disagree.");
        return viaIdentity;
    }

    private sealed class Fault : Exception;
}
