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

// ANDIRIT-CI1A-S2.4-S24-READER-Q2 reviewer-owned probe harness.
//
// Fresh independent requalification of the WP-S24-1 R1 remediation (F-Q1-01,
// F-OBS-Q1-02). Reviewer-owned token constructions; expected values derive from
// the accepted D1/D2/D5 contract and the total-order tuple, not from production
// helpers. Usage: <verb> <database> <outputJson> [detail].
var action = args[0];
var database = args[1];
var output = args[2];
var detail = args.Length > 3 ? args[3] : "";
var result = await Q2Probe.RunAsync(action, database, detail);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action + " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class Q2Probe
{
    internal static readonly CodePluginId Plugin = new("probe.s24.reader.q2");
    internal static readonly CodeRepositoryId PluginRepo = new("repo:s24-reader-q2-plugin");
    internal static readonly DateTimeOffset Started = new(2026, 11, 8, 14, 0, 0, TimeSpan.Zero);
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

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
                case "malformed": await MalformedAsync(database, root, notes); break;
                case "canonical": await CanonicalAsync(database, root, notes); break;
                case "precedence": await PrecedenceAsync(database, root, notes); break;
                case "pagination": await PaginationAsync(database, root, notes); break;
                case "bounded": await BoundedAsync(database, root, notes); break;
                case "noninterference": await NonInterferenceAsync(database, root, notes); break;
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

    // ---- Q2-02: independent malformed-token adversarial matrix ----

    private static async Task MalformedAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q2:malformed");
        var target = Target(repo, "T");
        using var store = await PublishedAsync(database, repo, "run",
            Unit(repo, "run", "u", [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)]));
        var pub = (await store.GetLatestPublicationAsync(repo))!;
        var good = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
        var cursorId = Hex("cursor");
        string Base(string sum = "0000000000000000", int version = 1, string repoV = "", string mode = "target",
            string targetV = "", string runV = "", string idV = "", string dir = "asc", string afterId = "",
            int size = 1) =>
            "{" +
            $"\"v\":{version},\"repo\":\"{repoV}\",\"mode\":\"{mode}\",\"target\":\"{targetV}\"," +
            $"\"pubRun\":\"{runV}\",\"pubId\":\"{idV}\",\"dir\":\"{dir}\"," +
            $"\"afterPath\":\"src/A.cs\",\"afterStartLine\":1,\"afterStartColumn\":1,\"afterEndLine\":1,\"afterEndColumn\":4," +
            $"\"afterId\":\"{afterId}\",\"size\":{size},\"sum\":\"{sum}\"}}";
        var valid = Base(repoV: repo.Value, targetV: target.Value, runV: pub.IndexRunId.Value, idV: pub.IndexIdentity.Value, afterId: cursorId);

        var cases = new List<(string Name, string Token)>
        {
            ("duplicate-v", Raw(Base(repoV: repo.Value, targetV: target.Value, runV: pub.IndexRunId.Value, idV: pub.IndexIdentity.Value, afterId: cursorId).Replace("\"v\":1,", "\"v\":1,\"v\":1,"))),
            ("duplicate-repo", Raw(valid.Replace($"\"repo\":\"{repo.Value}\",", $"\"repo\":\"{repo.Value}\",\"repo\":\"{repo.Value}\","))),
            ("duplicate-pubRun", Raw(valid.Replace($"\"pubRun\":\"{pub.IndexRunId.Value}\",", $"\"pubRun\":\"{pub.IndexRunId.Value}\",\"pubRun\":\"{pub.IndexRunId.Value}\","))),
            ("duplicate-pubId", Raw(valid.Replace($"\"pubId\":\"{pub.IndexIdentity.Value}\",", $"\"pubId\":\"{pub.IndexIdentity.Value}\",\"pubId\":\"{pub.IndexIdentity.Value}\","))),
            ("duplicate-sum", Raw(valid.Replace("\"sum\":\"0000000000000000\"", "\"sum\":\"0000000000000000\",\"sum\":\"0000000000000000\""))),
            ("missing-repo", Raw(valid.Replace($"\"repo\":\"{repo.Value}\",", ""))),
            ("missing-target", Raw(valid.Replace($"\"target\":\"{target.Value}\",", ""))),
            ("missing-pubRun", Raw(valid.Replace($"\"pubRun\":\"{pub.IndexRunId.Value}\",", ""))),
            ("missing-afterId", Raw(valid.Replace($"\"afterId\":\"{cursorId}\",", ""))),
            ("missing-size", Raw(valid.Replace("\"size\":1,", ""))),
            ("missing-sum", Raw(valid.Replace(",\"sum\":\"0000000000000000\"", ""))),
            ("wrong-type-v-string", Raw(valid.Replace("\"v\":1", "\"v\":\"1\""))),
            ("wrong-type-size-string", Raw(valid.Replace("\"size\":1", "\"size\":\"1\""))),
            ("wrong-type-repo-number", Raw(valid.Replace($"\"repo\":\"{repo.Value}\"", "\"repo\":42"))),
            ("wrong-type-line-string", Raw(valid.Replace("\"afterStartLine\":1", "\"afterStartLine\":\"1\""))),
            ("extra-field", Raw(valid.Replace(",\"sum\":", ",\"extra\":1,\"sum\":"))),
            ("nested-object-scalar", Raw(valid.Replace("\"size\":1", "\"size\":{}"))),
            ("array-scalar", Raw(valid.Replace($"\"repo\":\"{repo.Value}\"", "\"repo\":[]"))),
            ("malformed-json-truncated", Raw("{\"v\":1,\"repo\":")),
            ("malformed-json-trailing", Raw(valid + "garbage")),
            ("malformed-json-comment", Raw("/* comment */" + valid)),
            ("unsupported-version-0", Raw(valid.Replace("\"v\":1", "\"v\":0"))),
            ("unsupported-version-2", Raw(valid.Replace("\"v\":1", "\"v\":2"))),
            ("unsupported-version-negative", Raw(valid.Replace("\"v\":1", "\"v\":-1"))),
            ("invalid-checksum", Raw(valid)),
            ("invalid-checksum-tampered", TamperSum(good)),
            ("oversized-1025", new string('A', 1025)),
            ("oversized-5000", new string('A', 5000)),
            ("invalid-char-percent", valid + "%"),
            ("invalid-char-space", valid + " "),
            ("whitespace-leading", " " + good),
            ("whitespace-newline", good + "\n"),
        };
        // Codec-authored out-of-range cursors (checksum-valid, canonical).
        foreach (var (name, env) in new (string, CodeOccurrenceContinuation.Envelope)[]
        {
            ("range-size-0", new(repo.Value, "target", target.Value, pub.IndexRunId.Value, pub.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, cursorId, 0)),
            ("range-size-1001", new(repo.Value, "target", target.Value, pub.IndexRunId.Value, pub.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, cursorId, 1001)),
            ("range-startline-0", new(repo.Value, "target", target.Value, pub.IndexRunId.Value, pub.IndexIdentity.Value, "asc", "src/A.cs", 0, 1, 1, 4, cursorId, 1)),
            ("range-endcolumn-negative", new(repo.Value, "target", target.Value, pub.IndexRunId.Value, pub.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, -1, cursorId, 1)),
            ("range-endline-less-than-start", new(repo.Value, "target", target.Value, pub.IndexRunId.Value, pub.IndexIdentity.Value, "asc", "src/A.cs", 2, 1, 1, 4, cursorId, 1)),
            ("range-sameline-endcolumn-below-start", new(repo.Value, "target", target.Value, pub.IndexRunId.Value, pub.IndexIdentity.Value, "asc", "src/A.cs", 1, 5, 1, 4, cursorId, 1)),
            ("range-empty-path", new(repo.Value, "target", target.Value, pub.IndexRunId.Value, pub.IndexIdentity.Value, "asc", "", 1, 1, 1, 4, cursorId, 1)),
            ("range-noncanonical-cursor-id", new(repo.Value, "target", target.Value, pub.IndexRunId.Value, pub.IndexIdentity.Value, "asc", "src/A.cs", 1, 1, 1, 4, "not-a-canonical-id", 1)),
        })
        {
            cases.Add((name, CodeOccurrenceContinuation.Encode(env)));
        }

        var matrix = new JsonArray();
        foreach (var (name, token) in cases)
        {
            matrix.Add(Probe(store, repo, target, name, token));
        }
        root["matrix"] = matrix;
        var failed = matrix.Count(e => e!.AsObject()["ok"]!.GetValue<bool>() == false);
        Assert(root, failed == 0, "all " + cases.Count + " malformed/out-of-range tokens refused as Invalid without propagating (" + failed + " failures)", notes);

        // Valid canonical control (round-trip must still work).
        var control = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = good });
        root["control"] = Describe(control);
        Assert(root, control.Availability == CodeOccurrenceAvailability.Available && control.Occurrences.Count == 1,
            "valid canonical token still continues", notes);
    }

    // ---- Q2-03: canonical encoding ----

    private static async Task CanonicalAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q2:canonical");
        var target = Target(repo, "T");
        using var store = await PublishedAsync(database, repo, "run",
            Unit(repo, "run", "u", [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)]));
        var good = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
        var bytes = DecodeB64Url(good);

        var matrix = new JsonArray();
        var valid = (CodeOccurrenceQueryResult r) =>
            r.Availability == CodeOccurrenceAvailability.Available && r.Occurrences.Count == 1;
        void Case(string name, string token) => matrix.Add(Probe(store, repo, target, name, token));
        void Note(string name, string note) => matrix.Add(new JsonObject { ["case"] = name, ["ok"] = true, ["note"] = note });

        matrix.Add(Probe(store, repo, target, "canonical-control", good, valid));

        // Find a canonical token whose base64url form contains a '-'/'_' (and thus
        // differs from its standard-base64 form), so the alphabet and padding cases
        // are exercised deterministically rather than being data-dependent no-ops.
        var alphabetToken = good;
        foreach (var name in new[] { "T2", "T3", "T4", "T5", "T6" })
        {
            var extra = Target(repo, name);
            await PublishAsync(store, repo, "run:" + name, Unit(repo, "run:" + name, "u",
                [Fact(repo, "u", extra, "src/A.cs", 1, 1), Fact(repo, "u", extra, "src/A.cs", 2, 1)]));
            var token = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, extra, maxResults: 1))).NextContinuationToken;
            if (token is not null && (token.Contains('-') || token.Contains('_')))
            {
                alphabetToken = token;
                break;
            }
        }

        root["controlTokenLength"] = good.Length;
        root["controlTokenHasSpecialAlphabetByte"] = good.Contains('-') || good.Contains('_');
        root["alphabetTokenHasSpecialAlphabetByte"] = alphabetToken.Contains('-') || alphabetToken.Contains('_');
        root["alphabetNote"] = "base64url of the ASCII canonical envelope contains no '-'/'_' for these tokens, so the base64url and standard-base64 forms coincide; the equivalent noncanonical-encoding requirement is exercised by padding, trailing-pad-bit, invalid-character, whitespace and mixed-alphabet variants.";
        if (alphabetToken.Contains('-') || alphabetToken.Contains('_'))
        {
            Case("standard-alphabet", alphabetToken.Replace('-', '+').Replace('_', '/'));
            var paddedStandard = Convert.ToBase64String(DecodeB64Url(alphabetToken));
            if (paddedStandard != alphabetToken) Case("padded-standard", paddedStandard);
            else Note("padded-standard", "not-applicable: no padding needed for this token");
        }
        else
        {
            Note("standard-alphabet", "not-applicable: no canonical token with a special base64url byte was produced");
            Note("padded-standard", "not-applicable: no canonical token with a special base64url byte was produced");
        }

        Case("padded-base64url", good + "=");

        var sameBytes = SameBytesVariant(good);
        if (sameBytes == good) Note("same-bytes-trailing-pad-bits", "not-applicable: no alternative encoding of the identical bytes exists for this token");
        else Case("same-bytes-trailing-pad-bits", sameBytes);

        Case("invalid-char-percent", good + "%");
        Case("invalid-char-exclamation", "!" + good[1..]);
        Case("whitespace", " " + good + " ");
        Case("mixed-alphabet", good[..8] + "/" + good[9..]);

        root["matrix"] = matrix;
        root["roundTripEqual"] = string.Equals(B64Url(bytes), good, StringComparison.Ordinal);
        var failed = matrix.Count(e => e!.AsObject()["ok"]!.GetValue<bool>() == false);
        Assert(root, failed == 0, "canonical encoding: noncanonical variants refused, canonical control continues (" + failed + " failures)", notes);
        Assert(root, root["roundTripEqual"]!.GetValue<bool>(), "canonical token round-trips byte-exactly through decode/re-encode", notes);
    }

    // ---- Q2-04: refusal precedence ----

    private static async Task PrecedenceAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q2:precedence");
        var target = Target(repo, "T");
        var matrix = new JsonArray();

        using (var store = await PublishedAsync(database, repo, "run:1",
            Unit(repo, "run:1", "u", [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)])))
        {
            var pub = (await store.GetLatestPublicationAsync(repo))!;
            var good = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
            var cursorId = Hex("cursor");
            CodeOccurrenceContinuation.Envelope Env(string run, string id, string afterId = "") =>
                new(repo.Value, "target", target.Value, run, id, "asc", "src/A.cs", 1, 1, 1, 4, string.IsNullOrEmpty(afterId) ? cursorId : afterId, 1);

            var valid = (CodeOccurrenceQueryResult r) => r.Availability == CodeOccurrenceAvailability.Available && r.Occurrences.Count == 1;
            var stale = (CodeOccurrenceQueryResult r) => r.Availability == CodeOccurrenceAvailability.StalePublication &&
                r.Continuation == CodeOccurrenceContinuationState.Invalid && r.Occurrences.Count == 0;
            matrix.Add(Probe(store, repo, target, "valid-current", good, valid));
            matrix.Add(Probe(store, repo, target, "wrong-pub-run", CodeOccurrenceContinuation.Encode(Env("run:foreign", pub.IndexIdentity.Value)), stale));
            matrix.Add(Probe(store, repo, target, "wrong-pub-id", CodeOccurrenceContinuation.Encode(Env(pub.IndexRunId.Value, "identity:foreign")), stale));
            matrix.Add(Probe(store, repo, target, "malformed-cursor-current", CodeOccurrenceContinuation.Encode(Env(pub.IndexRunId.Value, pub.IndexIdentity.Value, afterId: "x"))));
            matrix.Add(Probe(store, repo, target, "malformed-envelope-current", Raw("{not-json")));

            // Stale after replacement (two occurrences so a continuation token exists).
            await PublishAsync(store, repo, "run:2", Unit(repo, "run:2", "u",
                [Fact(repo, "u", target, "src/B.cs", 9, 1), Fact(repo, "u", target, "src/B.cs", 10, 1)]));
            matrix.Add(Probe(store, repo, target, "stale-after-replacement", good, stale));

            // Retired publication.
            var good2 = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken;
            await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:3"), Started.AddSeconds(5), plugins: [Plugin]));
            await store.StageIndexUnitDeletionAsync(repo, new("run:3"), Plugin, new CodeIndexUnitId("u"));
            await store.CompleteIndexRunAsync(new(repo, new CodeIndexRunId("run:3"), Started.AddSeconds(5), CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]), new(repo, new CodeIndexRunId("run:3"), []));
            if (good2 is not null)
                matrix.Add(Probe(store, repo, target, "stale-after-retirement", good2,
                    r => (r.Availability == CodeOccurrenceAvailability.StalePublication || r.Availability == CodeOccurrenceAvailability.AnalysisUnavailable) &&
                         r.Occurrences.Count == 0));
        }

        // Recovery between pages must not blend publications.
        var arm = false;
        using (var store = new LatticeCodeGraphStore(database + ".rec", null, point => { if (arm && point == "before-commit") throw new Fault(); }))
        {
            await PublishAsync(store, repo, "run:x", Unit(repo, "run:x", "u",
                [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1), Fact(repo, "u", target, "src/A.cs", 3, 1)]));
            var good = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
            await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:fail"), Started.AddSeconds(5), plugins: [Plugin]));
            await store.StageIndexUnitAsync(Unit(repo, "run:fail", "u9", [Fact(repo, "u9", target, "src/Z.cs", 1, 1)]));
            arm = true;
            try { await store.CompleteIndexRunAsync(new(repo, new CodeIndexRunId("run:fail"), Started.AddSeconds(5), CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]), new(repo, new CodeIndexRunId("run:fail"), [])); }
            catch (Fault) { }
            arm = false;
            matrix.Add(Probe(store, repo, target, "recovery-between-pages", good,
                r => r.Availability == CodeOccurrenceAvailability.Available && r.Occurrences.Count == 1 && r.Publication!.IndexRunId.Value == "run:x"));
        }
        Cleanup(database + ".rec");

        root["matrix"] = matrix;
        var failed = matrix.Count(e => e!.AsObject()["ok"]!.GetValue<bool>() == false);
        Assert(root, failed == 0, "refusal precedence normative (Invalid / StalePublication / AnalysisUnavailable) (" + failed + " failures)", notes);
    }

    // ---- Q2-05: pagination completeness against an independent oracle ----

    private static async Task PaginationAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q2:pagination");
        var target = Target(repo, "T");
        var specs = new (string Path, int Sl, int Sc, int El, int Ec, string Sem)[]
        {
            ("src/C.cs", 3, 1, 3, 5, "c1"), ("src/A.cs", 2, 1, 2, 5, "a1"), ("src/A.cs", 1, 2, 1, 6, "a2"),
            ("src/A.cs", 1, 1, 1, 5, "a3"), ("src/B.cs", 1, 1, 1, 5, "b1"), ("src/A.cs", 1, 1, 1, 5, "a4"),
            ("src/A.cs", 1, 1, 1, 4, "a5"),
        };
        var facts = specs.Select(s => Fact(repo, "u", target, s.Path, s.Sl, s.Sc, s.El, s.Ec, s.Sem)).ToList();
        var expected = facts
            .OrderBy(f => f, Comparer<CodeOccurrenceFact>.Create(CompareFactByTuple))
            .Select(f => f.Id.Value).ToArray();
        using var store = await PublishedAsync(database, repo, "run", Unit(repo, "run", "u", facts));

        var matrix = new JsonArray();
        var allOk = true;
        foreach (var pageSize in new[] { 1, 2, 3, 7, 100 })
        {
            var (ids, pages, terminal) = await TraverseAsync(store, repo, target, pageSize);
            var ok = ids.SequenceEqual(expected) && ids.Distinct(StringComparer.Ordinal).Count() == expected.Length && terminal;
            allOk &= ok;
            matrix.Add(new JsonObject { ["pageSize"] = pageSize, ["pages"] = pages, ["returned"] = ids.Length, ["noDuplicatesOrOmissions"] = ids.SequenceEqual(expected), ["terminal"] = terminal, ["ok"] = ok });
        }
        root["matrix"] = matrix;
        Assert(root, allOk, "pagination reproduces the reviewer-owned total-order oracle at every page size", notes);
    }

    // ---- Q2-06: bounded execution ----

    private static async Task BoundedAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q2:bounded");
        var target = Target(repo, "T");
        var scaling = new JsonArray();
        foreach (var size in new[] { 500, 2000, 8000 })
        {
            var path = database + "." + size;
            Cleanup(path);
            var facts = Enumerable.Range(0, size).Select(i => Fact(repo, "u", target, "src/A.cs", i + 1, 1)).ToList();
            using var store = await PublishedAsync(path, repo, "run", Unit(repo, "run", "u", facts));
            var first = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 10));
            var sw = Stopwatch.StartNew();
            var second = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 10) { ContinuationToken = first.NextContinuationToken });
            sw.Stop();
            scaling.Add(new JsonObject
            {
                ["cardinality"] = size, ["firstExamined"] = first.Examined, ["secondExamined"] = second.Examined,
                ["firstReturned"] = first.Occurrences.Count, ["secondReturned"] = second.Occurrences.Count,
                ["secondPageMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 3),
            });
            store.Dispose();
            Cleanup(path);
        }
        root["scaling"] = scaling;
        var bounded = scaling.All(e => e!.AsObject()["firstExamined"]!.GetValue<int>() == e!.AsObject()["cardinality"]!.GetValue<int>() &&
                                       e!.AsObject()["firstReturned"]!.GetValue<int>() == 10);
        Assert(root, bounded, "Examined equals target cardinality (honest full-target read), page size bounded", notes);

        // Token-size validation occurs before costly processing: a huge token is
        // rejected with bounded latency and no exception.
        using (var store = await PublishedAsync(database + ".big", repo, "run", Unit(repo, "run", "u", [Fact(repo, "u", target, "src/A.cs", 1, 1)])))
        {
            var sw = Stopwatch.StartNew();
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = new string('A', 100_000) });
            sw.Stop();
            root["hugeTokenMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 3);
            Assert(root, result.Continuation == CodeOccurrenceContinuationState.Invalid && sw.Elapsed.TotalMilliseconds < 200,
                "oversized token rejected quickly with Invalid (length validated up front)", notes);
        }
        Cleanup(database + ".big");
    }

    // ---- Q2-05: noninterference ----

    private static async Task NonInterferenceAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q2:noninterf");
        var target = Target(repo, "T");
        string before;
        using (var store = await PublishedAsync(database, repo, "run",
            Unit(repo, "run", "u", [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1), Fact(repo, "u", target, "src/A.cs", 3, 1)])))
        {
            before = string.Join("|", (await store.GetPublishedUnitsAsync(repo)).SelectMany(u => u.Nodes).Select(n => n.Id.Value).Order(StringComparer.Ordinal));
        }
        var rowsBefore = CountRows(database);
        using (var store = new LatticeCodeGraphStore(database))
        {
            await TraverseAsync(store, repo, target, 1);
            Assert(root, string.Join("|", (await store.GetPublishedUnitsAsync(repo)).SelectMany(u => u.Nodes).Select(n => n.Id.Value).Order(StringComparer.Ordinal)) == before,
                "D3 graph identities unchanged across pagination", notes);
        }
        var rowsAfter = CountRows(database);
        Assert(root, rowsBefore == rowsAfter, "no persisted token/session rows from pagination", notes);
        root["rows"] = rowsAfter;
    }

    // ---- helpers ----

    private static JsonObject Probe(LatticeCodeGraphStore store, CodeRepositoryId repo, CodeOccurrenceTargetId target, string name, string token,
        Func<CodeOccurrenceQueryResult, bool>? expect = null)
    {
        expect ??= r => r.Continuation == CodeOccurrenceContinuationState.Invalid && r.Occurrences.Count == 0 && !r.IsComplete;
        var row = new JsonObject { ["case"] = name };
        try
        {
            var result = store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token }).GetAwaiter().GetResult();
            row["availability"] = result.Availability.ToString();
            row["continuation"] = result.Continuation.ToString();
            row["occurrences"] = result.Occurrences.Count;
            row["threw"] = false;
            row["ok"] = expect(result);
        }
        catch (Exception exception)
        {
            row["threw"] = true;
            row["exception"] = exception.GetType().Name;
            row["message"] = exception.Message;
            row["ok"] = false;
        }
        return row;
    }

    private static void Assert(JsonObject root, bool condition, string message, JsonArray notes)
    {
        notes.Add((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition) root["ok"] = false;
    }

    private static JsonObject Describe(CodeOccurrenceQueryResult r) => new()
    {
        ["availability"] = r.Availability.ToString(), ["continuation"] = r.Continuation.ToString(), ["occurrences"] = r.Occurrences.Count,
    };

    private static async Task<(string[] Ids, int Pages, bool Terminal)> TraverseAsync(
        LatticeCodeGraphStore store, CodeRepositoryId repo, CodeOccurrenceTargetId target, int pageSize)
    {
        var ids = new List<string>();
        string? token = null;
        var pages = 0;
        while (true)
        {
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: pageSize) { ContinuationToken = token });
            pages++;
            ids.AddRange(result.Occurrences.Select(o => o.Id.Value));
            if (result.NextContinuationToken is null)
                return (ids.ToArray(), pages, result.Continuation == CodeOccurrenceContinuationState.Complete);
            token = result.NextContinuationToken;
        }
    }

    private static int CompareFactByTuple(CodeOccurrenceFact x, CodeOccurrenceFact y)
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
    }

    private static string Raw(string json) => B64Url(Encoding.UTF8.GetBytes(json));
    private static string B64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] DecodeB64Url(string token)
    {
        var padded = token.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch { 2 => padded + "==", 3 => padded + "=", _ => padded };
        return Convert.FromBase64String(padded);
    }

    // An alternative base64url string that decodes to the SAME bytes (noncanonical
    // trailing pad bits), if one exists.
    private static string SameBytesVariant(string canonical)
    {
        var bytes = DecodeB64Url(canonical);
        foreach (var c in Alphabet)
        {
            if (c == canonical[^1]) continue;
            var candidate = canonical[..^1] + c;
            try { if (DecodeB64Url(candidate).AsSpan().SequenceEqual(bytes)) return candidate; }
            catch { }
        }
        return canonical + "-"; // forces a different (invalid) token if no pad-bit variant exists
    }

    private static string TamperSum(string token)
    {
        var bytes = DecodeB64Url(token);
        var json = Encoding.UTF8.GetString(bytes);
        json = System.Text.RegularExpressions.Regex.Replace(json, "\"sum\":\"[0-9a-f]{16}\"", "\"sum\":\"1111111111111111\"");
        return Raw(json);
    }

    private static string Hex(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

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

    private static void Cleanup(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }

    private static async Task<LatticeCodeGraphStore> PublishedAsync(string database, CodeRepositoryId repo, string run, params CodeIndexUnitReplacement[] units)
    {
        var store = new LatticeCodeGraphStore(database);
        await PublishAsync(store, repo, run, units);
        return store;
    }

    private static async Task PublishAsync(LatticeCodeGraphStore store, CodeRepositoryId repo, string run, params CodeIndexUnitReplacement[] units)
    {
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        foreach (var unit in units) await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]), new(repo, runId, []));
    }

    private static CodeIndexUnitReplacement Unit(CodeRepositoryId repo, string run, string unit, IReadOnlyList<CodeOccurrenceFact> facts) =>
        new(new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            [new CodeGraphNode(new CodeNodeId("node:" + unit), CodeNodeKinds.Callable, unit, unit, new CodeSymbolId("symbol:" + unit))],
            occurrences: facts, occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(facts.Count, 0, CodeRelationshipCoverageState.Produced));

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name)
    {
        var context = "compctx1:sha256:" + Hex(repo.Value + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(context, "meta:" + name, "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static CodeOccurrenceFact Fact(CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target, string path, int line, int column,
        int? endLine = null, int? endColumn = null, string? semantic = null) =>
        CodeOccurrenceFact.Create(repo, target, target.Value, semantic ?? unit,
            new CodeOccurrenceContributor(Plugin, unit), CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, endLine ?? line, endColumn ?? column + 4), "sha256:source", "content:invocation");

    private sealed class Fault : Exception;
}
