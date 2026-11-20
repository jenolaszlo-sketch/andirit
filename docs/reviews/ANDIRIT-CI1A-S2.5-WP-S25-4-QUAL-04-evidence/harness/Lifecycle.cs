// QUAL-04 reviewer-owned replacement/lifecycle control (commission §7):
// complete -> partial -> restored -> repeated publication against one native
// store, with membership re-verified at publication and after fresh-process
// reopen, stale-authority refusal, and non-accumulation checks.
using System.Text.Json;
using Andirit;

internal static partial class Probe
{
    private static async Task Lifecycle(string name, string path, string receiptPath)
    {
        var repo = new CodeRepositoryId("qual4:lifecycle");
        // Phases: publish-<generation> / verify-<generation>; generation is
        // first|partial|restored|repeat.
        var phase = Phase ?? "publish-first";
        var parts = phase.Split('-', 2);
        var action = parts[0];
        var generation = parts[1];
        var missing = generation == "partial" ? 1 : 0;
        var run = "run:lifecycle-" + generation;
        var files = MembershipFixture(2, missing, new[] { 1, 1 });
        var complete = missing == 0;
        Receipt["generation"] = generation;
        Receipt["missing"] = missing;
        Receipt["expectedSequence"] = new[] { "2/2", "3/2", "2/2", "2/2" };

        var target = await FixtureTargetAsync(repo, "run:lifecycle-first", MembershipFixture(2, 0, new[] { 1, 1 }), "P0/P0.csproj", "P0.Value", "Get");
        Receipt["target"] = target.Value;

        if (action == "publish")
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishCSharp(store, repo, run, files);
                var publication = await store.GetLatestPublicationAsync(repo);
                Receipt["latestPublication"] = publication is null
                    ? null
                    : new
                    {
                        run = publication.IndexRunId.Value,
                        snapshot = publication.SnapshotIdentity,
                        index = publication.IndexIdentity.Value,
                        consistent = publication.IsConsistentSnapshot,
                    };
                if (publication is not null && generation == "first")
                    File.WriteAllText(path + ".publication.json", JsonSerializer.Serialize(new
                    {
                        run = publication.IndexRunId.Value,
                        snapshot = publication.SnapshotIdentity,
                        index = publication.IndexIdentity.Value,
                        consistent = publication.IsConsistentSnapshot,
                    }));
            }
        }

        using (var store = new LatticeCodeGraphStore(path))
        {
            var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
            var query = complete
                ? VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                    analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation)
                : new CodeOccurrenceQuery(repo, target);
            var result = await store.FindOccurrencesAsync(query);
            Receipt["query"] = DescribeResult(result);

            // Stale-publication refusal: the first-generation accepted publication
            // handle must be refused from every later generation, with no facts
            // and no coverage served from it.
            if (generation != "first" && File.Exists(path + ".publication.json"))
            {
                using var saved = JsonDocument.Parse(File.ReadAllText(path + ".publication.json"));
                var root = saved.RootElement;
                var staleQuery = new CodeOccurrenceQuery(repo, target,
                    new CodeGraphPublication(repo,
                        new(root.GetProperty("run").GetString()!),
                        root.GetProperty("snapshot").GetString()!,
                        root.GetProperty("consistent").GetBoolean(),
                        new CodeIndexIdentity(root.GetProperty("index").GetString()!)))
                {
                    CurrentAuthority = query.CurrentAuthority,
                };
                var stale = await store.FindOccurrencesAsync(staleQuery);
                Receipt["staleFirstHandle"] = DescribeResult(stale);
            }
        }

        Receipt["native"] = NativeRowSummary(path);
        Receipt["verifiedUnitIds"] = (await CSharpCodeGraphPlugin.PrepareAnalysisAsync(
            Context(repo, run, files))).Universe.Entries
            .Select(e => e.Descriptor.ProjectUnitId).Order(StringComparer.Ordinal).ToArray();
    }
}