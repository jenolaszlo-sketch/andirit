using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;

// ANDIRIT-CI1A-S2.5-WP-S25-1-BIND-01 historical fixture binding probe (producer side).
// Opens ONLY staged disposable copies of the committed authentic masters.
// Usage: <case> <stagedPath> <outJson> [originalPath] [originalExpectedSha256]
//   case "pre-rem":     expect CodeOccurrenceSchemaException at open (P13b).
//   case "graph-only":  expect legacy graph queries usable (H-01) and the occurrence
//                       query reporting AnalysisUnavailable, never successful empty (H-02).
var action = args[0];
var staged = args[1];
var output = args[2];
var original = args.Length > 3 ? args[3] : null;
var originalExpected = args.Length > 4 ? args[4] : null;

static string Sha(string path) =>
    Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

var root = new JsonObject
{
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
                // Unexpected path: capture what the store would serve so the
                // divergence is fully evidenced (should not occur).
                var target = new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('0', 64));
                var occ = await store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repository, target, maxResults: 100));
                root["unexpectedOccurrenceAvailability"] = occ.Availability.ToString();
                root["unexpectedOccurrenceCount"] = occ.Occurrences.Count;
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
                "pre-REM store refused at open with CodeOccurrenceSchemaException");
            break;
        }
        case "graph-only":
        {
            using var store = new LatticeCodeGraphStore(staged);
            root["openSucceeded"] = true;

            var units = await store.GetPublishedUnitsAsync(repository);
            root["publishedUnitCount"] = units.Count;
            Assert(units.Count >= 1, "legacy graph publication exposes published units");

            var nodes = await store.FindNodesByQualifiedNameAsync(repository, "Logger.Write(string)");
            root["loggerNodeCount"] = nodes.Count;
            Assert(nodes.Count >= 1, "legacy graph node lookup finds Logger.Write(string) (frozen corpus)");

            var service = new CodeGraphQueryService(store);
            var publication = await service.OpenLatestPublicationAsync(repository);
            Assert(publication is not null, "latest graph publication opens");
            if (publication is not null)
            {
                root["publicationRun"] = publication.Publication.IndexRunId.Value;
                root["publicationIndexIdentity"] = publication.Publication.IndexIdentity.Value;
                var callers = await publication.FindCallersAsync(
                    nodes[0].Id, new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 500));
                root["callerEdgeCount"] = callers.Result.Edges.Count;
                root["callerTruncated"] = callers.Result.Truncated;
                Assert(callers.Result.Edges.Count >= 1,
                    "legacy callers traversal returns at least one edge (H-01)");
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
            root["occurrenceHasCoverage"] = occurrence.Coverage is not null;
            Assert(occurrence.Availability == CodeOccurrenceAvailability.AnalysisUnavailable,
                "occurrence query on the graph-only store reports AnalysisUnavailable (H-02, current contract)");
            Assert(occurrence.Availability != CodeOccurrenceAvailability.Available &&
                   occurrence.Occurrences.Count == 0 && !occurrence.IsComplete,
                "no fabricated occurrences and no complete-empty claim");
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
if (original is not null && originalExpected is not null &&
    root["originalSha256AfterRun"] is JsonValue after)
{
    Assert(after.GetValue<string>() == originalExpected,
        "committed original master bytes unchanged by this run");
}

root["notes"] = notes;
File.WriteAllText(output, JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (root["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action +
    " pid=" + Environment.ProcessId);
return root["ok"]!.GetValue<bool>() ? 0 : 1;
