using System.Text;
using System.Text.Json;
using Andirit;

internal static partial class Probe
{
    private static readonly CodeRepositoryId UnionRepo = new("review:s254:contributors");
    private static readonly CodePluginId PluginA = new("review:plugin:a");
    private static readonly CodePluginId PluginB = new("review:plugin:b");
    private static readonly CodeOccurrenceTargetId UnionTarget =
        new("asmctx3:sha256:" + new string('a', 64));
    private static readonly CodeLocation SharedSite = new("Controls/Call.cs", 1, 24, 1, 28);
    private static readonly string SharedSourceHash =
        FileDigest(Encoding.UTF8.GetBytes("static int Invoke() => Mark();\n"));
    private const string SharedDocument = "M:QualifierDependency.Api.Mark";
    private const string SemanticOwner = "semantic:caller";
    private const string OtherSemanticOwner = "semantic:other";
    private const string StorageA = "storage:01";
    private const string StorageB = "storage:02";
    private const string StorageC = "storage:03";
    private const string StorageD = "storage:04";
    private const string StorageConflict = "storage:conflicting";

    private static CodeOccurrenceFact Fact(CodePluginId plugin, string storage, string owner, string payload)
    {
        var id = Usage(UnionRepo.Value, plugin.Value, owner, SharedSite, SharedSourceHash,
            SharedDocument, UnionTarget.Value);
        return new(new(id), UnionTarget, SharedDocument, UnionRepo, CodeOccurrenceCategory.Invocation,
            owner, new(plugin, storage), SharedSite, SharedSourceHash, payload, pluginVersion: "1.0.0");
    }

    private static Task Stage(LatticeCodeGraphStore store, string run, CodeOccurrenceFact fact)
    {
        var origin = new CodeFactOrigin(UnionRepo, fact.Contributor.PluginId, "1.0.0",
            new(run), new(fact.Contributor.IndexUnitId));
        var node = new CodeGraphNode(new("node:" + fact.Contributor.IndexUnitId), CodeNodeKinds.Callable,
            fact.Contributor.IndexUnitId, fact.Contributor.IndexUnitId, new("symbol:" + fact.Contributor.IndexUnitId));
        return store.StageIndexUnitAsync(new(origin, [node], occurrences: [fact],
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new(1, 0, CodeRelationshipCoverageState.Produced))).AsTask();
    }

    private static async Task<string> ContributorSnapshot(
        LatticeCodeGraphStore store, string field, Dictionary<string, string[]> expected)
    {
        var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(UnionRepo, UnionTarget));
        var units = await store.GetPublishedUnitsAsync(UnionRepo);
        var actual = result.Occurrences.OrderBy(fact => fact.Id.Value, StringComparer.Ordinal)
            .ToDictionary(fact => fact.Id.Value, fact => fact.Contributors
                .Select(c => c.PluginId.Value + "/" + c.IndexUnitId).ToArray());
        var snapshot = new
        {
            result,
            nodes = units.SelectMany(unit => unit.Nodes).Select(node => node.Id.Value)
                .Order(StringComparer.Ordinal).ToArray(),
            edges = units.SelectMany(unit => unit.Edges).Select(edge => edge.Id.Value)
                .Order(StringComparer.Ordinal).ToArray(),
            contributors = actual
        };
        Receipt[field] = snapshot;
        Check(actual.Count == expected.Count && expected.All(pair =>
            actual.TryGetValue(pair.Key, out var contributors) && pair.Value.SequenceEqual(contributors)),
            field + " contains the exact expected fact IDs and ordered contributor sets");
        return JsonSerializer.Serialize(snapshot);
    }

    private static async Task Contributors(string phase, string path, string evidence)
    {
        const string payload = "content:original";
        const string conflictPayload = "content:changed";
        var a = Fact(PluginA, StorageA, SemanticOwner, payload);
        var b = Fact(PluginA, StorageB, SemanticOwner, payload);
        var otherUnit = Fact(PluginA, StorageC, OtherSemanticOwner, payload);
        var otherPlugin = Fact(PluginB, StorageD, SemanticOwner, payload);
        var conflict = Fact(PluginA, StorageConflict, SemanticOwner, conflictPayload);
        var expected = new Dictionary<string, string[]>
        {
            [otherUnit.Id.Value] = [PluginA.Value + "/" + StorageC],
            [otherPlugin.Id.Value] = [PluginB.Value + "/" + StorageD]
        };
        if (phase is not "b6-delete-b" and not "b6-reopen-b")
            expected[a.Id.Value] = phase is "b6-delete-a" or "b6-reopen-a"
                ? [PluginA.Value + "/" + StorageB]
                : [PluginA.Value + "/" + StorageA, PluginA.Value + "/" + StorageB];

        Receipt["sourcePrimitive"] = "static int Invoke() => Mark();\n";
        Receipt["inputs"] = new[] { a, b, otherUnit, otherPlugin, conflict };
        Receipt["expectedContributors"] = expected;
        Check(a.Id == b.Id && a.PayloadHash == b.PayloadHash, "Storage contributor exclusion");
        Check(a.Id == conflict.Id && a.PayloadHash != conflict.PayloadHash, "Payload-only conflict");
        Check(new[] { a.Id, otherUnit.Id, otherPlugin.Id }.Distinct().Count() == 3,
            "Semantic source unit and plugin each discriminate identity");

        using (var store = new LatticeCodeGraphStore(path))
        {
            if (phase == "b6-publish")
            {
                await Run(store, UnionRepo, "original", [PluginA, PluginB], async () =>
                {
                    await Stage(store, "original", b);
                    await Stage(store, "original", a);
                    await Stage(store, "original", a);
                    await Stage(store, "original", otherUnit);
                    await Stage(store, "original", otherPlugin);
                });
            }

            if (phase == "b7-conflict")
            {
                var before = await ContributorSnapshot(store, "before", expected);
                await store.StoreIndexRunAsync(new(UnionRepo, new("conflict"), Started, plugins: [PluginA]));
                try
                {
                    await Stage(store, "conflict", conflict);
                    Failures.Add("Same identity with incompatible immutable payload was accepted");
                }
                catch (CodeGraphBatchRejectedException ex)
                {
                    Receipt["refusalType"] = ex.GetType().FullName;
                    Receipt["refusalMessage"] = ex.Message;
                }

                var after = await ContributorSnapshot(store, "immediateAfter", expected);
                Check(before == after, "Graph, publication, availability, coverage and contributors are unchanged immediately");
            }
            else if (phase == "b6-delete-a" || phase == "b6-delete-b")
            {
                var storage = phase == "b6-delete-a" ? StorageA : StorageB;
                await Run(store, UnionRepo, phase, [PluginA], async () =>
                    await store.StageIndexUnitDeletionAsync(UnionRepo, new(phase), PluginA, new(storage)));
                await ContributorSnapshot(store, "snapshot", expected);
            }
            else
            {
                await ContributorSnapshot(store, "snapshot", expected);
            }
        }

        var native = ReadNative(path);
        Receipt["native"] = native;
        if (phase == "b7-reopen")
        {
            using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(evidence, "b6-publish.json")));
            var originalNative = baseline.RootElement.GetProperty("native");
            Check(JsonSerializer.Serialize(originalNative) == JsonSerializer.Serialize(native),
                "All accepted raw unit, page and meta rows agree with the pre-conflict receipt");
            var snapshot = baseline.RootElement.GetProperty("snapshot");
            Check(JsonSerializer.Serialize(snapshot) == JsonSerializer.Serialize(Receipt["snapshot"]),
                "Accepted graph, publication, coverage and occurrence snapshot survives fresh-process reopen");
        }
    }
}

