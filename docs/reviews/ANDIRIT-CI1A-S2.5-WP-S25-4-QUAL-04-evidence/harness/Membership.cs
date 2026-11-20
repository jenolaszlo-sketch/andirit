// QUAL-04 reviewer-owned mandatory membership controls (commission §6) and
// adversarial controls. Each case publishes through the real plugin/sink path in
// one process (phase=publish) and re-verifies in a fresh process (phase=verify).
using System.Text;
using Andirit;

internal static partial class Probe
{
    private static string? Phase => Environment.GetEnvironmentVariable("QUAL4_PHASE");

    private static async Task Membership(string name, string path, string receiptPath)
    {
        var repo = new CodeRepositoryId("qual4:" + name.ToLowerInvariant());
        var run = "run:" + name.ToLowerInvariant();
        var (projects, missing, calls) = name switch
        {
            "M1" => (1, 0, new[] { 1 }),
            "M2" => (2, 0, new[] { 1, 1 }),
            "M3" => (3, 0, new[] { 1, 1, 1 }),
            "M4" => (2, 0, new[] { 0, 0 }),
            "M5" => (2, 0, new[] { 1, 0 }),
            "M6" => (2, 1, new[] { 1, 1 }),
            "M7" => (2, 2, new[] { 1, 1 }),
            "M8" => (0, 0, Array.Empty<int>()),
            _ => throw new ArgumentException(name),
        };
        Receipt["fixture"] = new { projects, missing, calls };
        var complete = missing == 0;

        if (name == "M8")
        {
            await DisjointContributions(repo, new CodeIndexRunId(run), path);
            return;
        }

        var files = MembershipFixture(projects, missing, calls);
        var target = await FixtureTargetAsync(repo, run, files, "P0/P0.csproj", "P0.Value", "Get");
        Receipt["target"] = target.Value;

        if (Phase != "verify")
        {
            using (var store = new LatticeCodeGraphStore(path))
                await PublishCSharp(store, repo, run, files);
        }

        using (var store = new LatticeCodeGraphStore(path))
        {
            var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
            var query = complete
                ? VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                    analysis.Universe, analysis.Projects.Single(p =>
                        p.Project.Path == "P0/P0.csproj").Compilation)
                : new CodeOccurrenceQuery(repo, target);
            var result = await store.FindOccurrencesAsync(query);
            Receipt["query"] = DescribeResult(result);

            if (complete)
            {
                // Authority mismatch: a modified P0 source changes the live universe;
                // the recorded authority must be refused, never silently served.
                var changed = new Dictionary<string, byte[]>(files, StringComparer.Ordinal)
                {
                    ["P0/Value.cs"] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files["P0/Value.cs"])
                        .Replace("Get() => 1", "Get() => 2", StringComparison.Ordinal)),
                };
                var current = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(
                    Context(repo, run, changed));
                var mismatchQuery = VerifiedCSharpOccurrenceAuthority.Bind(
                    new CodeOccurrenceQuery(repo, target), current.Universe,
                    current.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
                var mismatched = await store.FindOccurrencesAsync(query with
                {
                    CurrentAuthority = mismatchQuery.CurrentAuthority,
                });
                Receipt["authorityMismatch"] = DescribeResult(mismatched);
            }
        }

        Receipt["native"] = NativeRowSummary(path);
        Receipt["verifiedUnitIds"] = (await CSharpCodeGraphPlugin.PrepareAnalysisAsync(
            Context(repo, run, files))).Universe.Entries
            .Select(e => e.Descriptor.ProjectUnitId).Order(StringComparer.Ordinal).ToArray();
        Receipt["declaredUnitIds"] = (await CSharpCodeGraphPlugin.PrepareAnalysisAsync(
            Context(repo, run, files))).Universe.Scope.Units
            .Select(u => u.ProjectUnitId).Order(StringComparer.Ordinal).ToArray();
    }

    // Controlled disjoint native contributions: two independent partial coverage
    // contributions from the same scope/universe must aggregate additively and
    // must never claim a complete analysis (Merge contract: worst state wins).
    private static async Task DisjointContributions(CodeRepositoryId repo, CodeIndexRunId run, string path)
    {
        var scope = "analysisscope1:sha256:" + new string('a', 64);
        var universe = "compilation-universe1:sha256:" + new string('b', 64);
        var plugin = new CodePluginId("qual4.disjoint");
        var target = new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('0', 64));

        if (Phase != "verify")
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await Run(store, repo, run.Value, [plugin], async () =>
                {
                    await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                        new CodeFactOrigin(repo, plugin, "1.0.0", run, new("unit:a")),
                        occurrenceAnalysisIncluded: true,
                        occurrenceCoverage: new CodeOccurrenceCoverage(
                            state: CodeRelationshipCoverageState.Partial, scopeId: scope, universeId: universe,
                            declaredUnitCount: 2, verifiedUnitCount: 1)));
                    await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                        new CodeFactOrigin(repo, plugin, "1.0.0", run, new("unit:b")),
                        occurrenceAnalysisIncluded: true,
                        occurrenceCoverage: new CodeOccurrenceCoverage(
                            state: CodeRelationshipCoverageState.Produced, scopeId: scope, universeId: universe,
                            declaredUnitCount: 2, verifiedUnitCount: 2)));
                });
            }
        }

        using (var store = new LatticeCodeGraphStore(path))
        {
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Receipt["query"] = DescribeResult(result);
        }

        Receipt["native"] = NativeRowSummary(path);
    }

    private static async Task Adversarial(string name, string path, string receiptPath)
    {
        var repo = new CodeRepositoryId("qual4:" + name.ToLowerInvariant());
        switch (name)
        {
            case "A1":
            {
                // Multiple occurrences belonging to a single project, plus a second
                // project with a different occurrence count.
                var run = "run:a1";
                var files = MembershipFixture(2, 0, new[] { 2, 1 });
                var target = await FixtureTargetAsync(repo, run, files, "P0/P0.csproj", "P0.Value", "Get");
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                        await PublishCSharp(store, repo, run, files);
                }
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                        analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
                    Receipt["query"] = DescribeResult(await store.FindOccurrencesAsync(query));
                    Receipt["hitsBySourceUnit"] = (await store.FindOccurrencesAsync(query)).Occurrences
                        .GroupBy(fact => fact.SourceUnitId, StringComparer.Ordinal)
                        .OrderBy(group => group.Key, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Count());
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            case "A2":
            {
                // Repeated publication of identical content: membership and facts
                // must not accumulate.
                var files = MembershipFixture(2, 0, new[] { 1, 1 });
                var target = await FixtureTargetAsync(repo, "run:a2-first", files, "P0/P0.csproj", "P0.Value", "Get");
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                    {
                        await PublishCSharp(store, repo, "run:a2-first", files);
                        await PublishCSharp(store, repo, "run:a2-second", files);
                        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(
                            Context(repo, "run:a2-second", files));
                        var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                            analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
                        Receipt["afterFirst"] = DescribeResult(await store.FindOccurrencesAsync(query));
                    }
                }

                using (var store = new LatticeCodeGraphStore(path))
                {
                    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(
                        Context(repo, "run:a2-second", files));
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                        analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
                    var result = await store.FindOccurrencesAsync(query);
                    Receipt["query"] = DescribeResult(result);
                    Receipt["hitsBySourceUnit"] = result.Occurrences
                        .GroupBy(fact => fact.SourceUnitId, StringComparer.Ordinal)
                        .OrderBy(group => group.Key, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Count());
                    Receipt["distinctFactIds"] = result.Occurrences.Select(f => f.Id.Value)
                        .Distinct(StringComparer.Ordinal).Count();
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            case "A3":
            {
                // Project/file ordering changes must not change membership, facts or
                // identities.
                var plain = MembershipFixture(3, 0, new[] { 1, 1, 1 });
                var reordered = MembershipFixture(3, 0, new[] { 1, 1, 1 }, reordered: true);
                var targetPlain = await FixtureTargetAsync(repo, "run:a3-plain", plain, "P0/P0.csproj", "P0.Value", "Get");
                var targetReordered = await FixtureTargetAsync(repo, "run:a3-reordered", reordered, "P0/P0.csproj", "P0.Value", "Get");
                Receipt["targetStable"] = targetPlain.Value == targetReordered.Value;
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                        await PublishCSharp(store, repo, "run:a3-reordered", reordered);
                }
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "run:a3-reordered", reordered));
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, targetReordered),
                        analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
                    Receipt["query"] = DescribeResult(await store.FindOccurrencesAsync(query));
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            case "A4":
            {
                // Multiple projects with differing occurrence counts.
                var run = "run:a4";
                var files = MembershipFixture(3, 0, new[] { 2, 1, 0 });
                var target = await FixtureTargetAsync(repo, run, files, "P0/P0.csproj", "P0.Value", "Get");
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                        await PublishCSharp(store, repo, run, files);
                }
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                        analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
                    var result = await store.FindOccurrencesAsync(query);
                    Receipt["query"] = DescribeResult(result);
                    Receipt["hitsBySourceUnit"] = result.Occurrences
                        .GroupBy(fact => fact.SourceUnitId, StringComparer.Ordinal)
                        .OrderBy(group => group.Key, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Count());
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            case "A6":
            {
                // Stale publication: after a replacement, the former publication
                // handle must be refused and its facts must not remain visible.
                var files = MembershipFixture(2, 0, new[] { 1, 1 });
                var revised = new Dictionary<string, byte[]>(files, StringComparer.Ordinal);
                revised["P0/Value.cs"] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files["P0/Value.cs"])
                    .Replace("Call0() => Get();", "Call0() => Get(); public static int Call9() => Get();",
                        StringComparison.Ordinal));
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                    {
                        await PublishCSharp(store, repo, "run:a6-old", files);
                        var oldAnalysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "run:a6-old", files));
                        var oldTarget = await FixtureTargetAsync(repo, "run:a6-old", files, "P0/P0.csproj", "P0.Value", "Get");
                        var oldQuery = VerifiedCSharpOccurrenceAuthority.Bind(
                            new CodeOccurrenceQuery(repo, oldTarget), oldAnalysis.Universe,
                            oldAnalysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
                        var before = await store.FindOccurrencesAsync(oldQuery);
                        Receipt["beforeReplacement"] = DescribeResult(before);
                        var oldPublication = before.Publication!;

                        await PublishCSharp(store, repo, "run:a6-new", revised);
                        var newAnalysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "run:a6-new", revised));
                        var stale = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, oldTarget, oldPublication)
                        {
                            CurrentAuthority = oldQuery.CurrentAuthority,
                        });
                        Receipt["stalePublication"] = DescribeResult(stale);
                        var staleHandle = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                            new CodeOccurrenceQuery(repo, oldTarget, oldPublication), newAnalysis.Universe,
                            newAnalysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation));
                        Receipt["staleBoundAuthority"] = DescribeResult(staleHandle);
                        var current = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                            new CodeOccurrenceQuery(repo, oldTarget), newAnalysis.Universe,
                            newAnalysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation));
                        Receipt["currentAfterReplacement"] = DescribeResult(current);
                    }
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            default:
                throw new ArgumentException(name);
        }
    }
}