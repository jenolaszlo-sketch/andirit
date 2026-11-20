using System.Text.Json;
using Andirit;
using LatticeDbSharp;

var phase = args[0];
var database = args[1];
var repo = new CodeRepositoryId("review:r3:disjoint");
var plugin = new CodePluginId("r3.disjoint");
var run = new CodeIndexRunId("accounting");
var started = new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);
const string scope = "analysisscope1:sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
const string universe = "compilation-universe1:sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

CodeOccurrenceQueryResult result;
using (var store = new LatticeCodeGraphStore(database))
{
    if (phase == "publish")
    {
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, run, started, plugins: [plugin]));
        await Stage("a", new(state: CodeRelationshipCoverageState.Partial, scopeId: scope, universeId: universe,
            declaredUnitCount: 2, verifiedUnitCount: 1, external: 1, unresolved: 2));
        await Stage("b", new(state: CodeRelationshipCoverageState.Produced, scopeId: scope, universeId: universe,
            declaredUnitCount: 2, verifiedUnitCount: 2, external: 2, ambiguous: 1,
            unsupported: 1, unsupportedCategories: 1, conditionalInactive: 1));
        await store.CompleteIndexRunAsync(new(repo, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [plugin]),
            new(repo, run, []));
    }
    result = await store.FindOccurrencesAsync(new(repo, new("asmctx3:sha256:" + new string('0', 64))));

    ValueTask Stage(string unit, CodeOccurrenceCoverage coverage) => store.StageIndexUnitAsync(
        new(new(repo, plugin, "1.0.0", run, new(unit)), occurrenceAnalysisIncluded: true, occurrenceCoverage: coverage));
}

using var raw = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false });
using var transaction = raw.BeginReadTransaction();
using var query = raw.Prepare("MATCH (s:AndiritOccurrenceUnit) RETURN s.key, s.payload");
using var rows = query.Execute(transaction);
var units = rows.ReadAll().Select(row => new
{
    key = row.GetValue(0).AsString(),
    unit = JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(row.GetValue(1).AsString())),
}).OrderBy(row => row.key, StringComparer.Ordinal).ToArray();
Console.WriteLine(JsonSerializer.Serialize(new { phase, processId = Environment.ProcessId, result, units },
    new JsonSerializerOptions { WriteIndented = true }));

var coverage = result.Coverage;
return result.Availability == CodeOccurrenceAvailability.IncompleteAnalysis && !result.IsComplete &&
    result.Occurrences.Count == 0 && coverage is
    { DeclaredUnitCount: 4, VerifiedUnitCount: 3, InvocationFacts: 0, External: 3, Ambiguous: 1,
      Unresolved: 2, Unsupported: 1, UnsupportedCategories: 1, ConditionalInactive: 1 } && units.Length == 2 ? 0 : 1;
