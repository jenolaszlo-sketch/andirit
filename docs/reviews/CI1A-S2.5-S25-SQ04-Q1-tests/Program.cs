using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;

// ANDIRIT-CI1A-S2.5-S25-SQ04-Q1 — independent reviewer probe (reviewer-owned; NOT BindProbe).
// Opens ONLY reviewer-staged disposable copies. Expectations derive from accepted contracts:
//   P13b / REM-01 owner acceptance: authentic pre-REM store refused store-wide at open
//     with CodeOccurrenceSchemaException; no reinterpretation; no available fallback.
//   H-01 / R2 historical matrix: the authentic baseline graph-only store opens and the
//     legacy callers query works (>= 1 caller). Independent fixture-source derivation:
//     docs/planning/ci1-scope-01/fixtures/Repeated.cs declares Logger.Write(string) with
//     exactly one distinct caller (Worker.Execute) — hence exactly one caller edge.
//   H-02 / matrix §10 row 2: occurrence analysis unavailable — never a successful empty.
// Producer receipts are cross-checks only, never expectation sources.
// Usage: <case> <stagedPath> <outJson> [originalPath] [originalExpectedSha256]
var action = args[0];
var staged = args[1];
var output = args[2];
var original = args.Length > 3 ? args[3] : null;
var originalExpected = args.Length > 4 ? args[4] : null;

static string Sha(string path) =>
    Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

var root = new JsonObject
{
    ["reviewer"] = "ANDIRIT-CI1A-S2.5-S25-SQ04-Q1",
    ["case"] = action,
    ["processId"] = Environment.ProcessId,
    ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
    ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
    ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    ["stagedPath"] = staged,
    ["stagedSha256BeforeOpen"] = File.Exists(staged) ? Sha(staged) : null,
    ["stagedWalPath"] = File.Exists(staged + "-wal") ? staged + "-wal" : null,
    ["stagedWalSha256BeforeOpen"] = File.Exists(staged + "-wal") ? Sha(staged + "-wal") : null,
    ["originalPath"] = original,
    ["originalExpectedSha256"] = originalExpected,
    ["originalSha256BeforeRun"] = original is not null && File.Exists(original) ? Sha(original) : null,
};
var notes = new JsonArray();
root["ok"] = true;
void Assert(bool condition, string message)
{
    notes.Add((condition ? "PASS: " : "FAIL: ") + message);
    if (!condition) root["ok"] = false;
}

// Repository identity inside the R2 baseline master (recorded in the R2 historical matrix
// procedure: OccurrenceExtractor.RepositoryId = "experiment:andirit-31c33a9").
var repository = new CodeRepositoryId("experiment:andirit-31c33a9");

try
{
    switch (action)
    {
        case "pre-rem":
        {
            string? thrown = null;
            string? message = null;
            try
            {
                using var store = new LatticeCodeGraphStore(staged);
                root["openSucceeded"] = true;
                // Unexpected path only: capture whatever the store would serve.
                var probeTarget = new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('0', 64));
                var unexpected = await store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repository, probeTarget, maxResults: 100));
                root["unexpectedAvailability"] = unexpected.Availability.ToString();
                root["unexpectedFactCount"] = unexpected.Occurrences.Count;
            }
            catch (Exception exception)
            {
                thrown = exception.GetType().Name;
                message = exception.Message;
            }
            root["openSucceeded"] = root["openSucceeded"] ?? false;
            root["exceptionType"] = thrown;
            root["exceptionMessage"] = message;
            Assert(thrown == "CodeOccurrenceSchemaException",
                "open-time refusal carries the accepted CodeOccurrenceSchemaException type (P13b)");
            Assert(root["openSucceeded"]!.GetValue<bool>() == false,
                "the store never reaches a usable state (store-wide refusal, including graph-only access)");
            Assert(!string.IsNullOrEmpty(message),
                "a concrete refusal message is preserved for the record");
            break;
        }
        case "graph-only":
        {
            using var store = new LatticeCodeGraphStore(staged);
            root["openSucceeded"] = true;

            var units = await store.GetPublishedUnitsAsync(repository);
            root["publishedUnitCount"] = units.Count;
            Assert(units.Count == 1, "exactly one expected published unit (H-01 record; producer cross-check)");

            var nodes = await store.FindNodesByQualifiedNameAsync(repository, "Logger.Write(string)");
            root["loggerNodeCount"] = nodes.Count;
            Assert(nodes.Count == 1, "Logger.Write(string) resolves to exactly one node (independently derived from the committed fixture source)");

            var service = new CodeGraphQueryService(store);
            var publication = await service.OpenLatestPublicationAsync(repository);
            Assert(publication is not null, "the historical publication opens");
            if (publication is not null)
            {
                root["publicationRun"] = publication.Publication.IndexRunId.Value;
                root["publicationIndexIdentity"] = publication.Publication.IndexIdentity.Value;
                Assert(publication.Publication.IndexRunId.Value == "historical-master-1",
                    "publication identity is historical-master-1 (R2 historical master record)");
                var callers = await publication.FindCallersAsync(
                    nodes[0].Id, new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 500));
                root["callerEdgeCount"] = callers.Result.Edges.Count;
                root["callerTruncated"] = callers.Result.Truncated;
                Assert(callers.Result.Edges.Count == 1,
                    "exactly one expected caller edge (independently derived: Worker.Execute is the sole caller)");
                Assert(!callers.Result.Truncated, "callers result is not truncated");
            }

            var targetId = new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('0', 64));
            var occurrence = await store.FindOccurrencesAsync(
                new CodeOccurrenceQuery(repository, targetId, maxResults: 100));
            root["occurrenceAvailability"] = occurrence.Availability.ToString();
            root["occurrenceCount"] = occurrence.Occurrences.Count;
            root["occurrenceExamined"] = occurrence.Examined;
            root["occurrenceTotalMatches"] = occurrence.TotalMatches;
            root["occurrenceIsComplete"] = occurrence.IsComplete;
            root["occurrenceRefusalReason"] = occurrence.RefusalReason;
            Assert(occurrence.Availability == CodeOccurrenceAvailability.AnalysisUnavailable,
                "occurrence query reports AnalysisUnavailable (H-02; matrix §10 row 2 status contract)");
            Assert(occurrence.Occurrences.Count == 0 && occurrence.TotalMatches == 0 && occurrence.Examined == 0,
                "no accepted occurrence facts are available and nothing is fabricated");
            Assert(!occurrence.IsComplete, "IsComplete is false (never a successful empty)");
            break;
        }
        default:
            throw new ArgumentException("Unknown case: " + action);
    }
}
catch (Exception exception)
{
    notes.Add(exception.GetType().Name + ": " + exception.Message);
    root["stack"] = exception.StackTrace;
    root["ok"] = false;
}

root["stagedSha256AfterRun"] = File.Exists(staged) ? Sha(staged) : null;
root["stagedWalSha256AfterRun"] = File.Exists(staged + "-wal") ? Sha(staged + "-wal") : null;
root["originalSha256AfterRun"] = original is not null && File.Exists(original) ? Sha(original) : null;
if (original is not null && originalExpected is not null && root["originalSha256AfterRun"] is JsonValue after)
{
    Assert(after.GetValue<string>() == originalExpected,
        "committed original master bytes unchanged by this run");
}

root["notes"] = notes;
File.WriteAllText(output, JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (root["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action +
    " pid=" + Environment.ProcessId);
return root["ok"]!.GetValue<bool>() ? 0 : 1;
