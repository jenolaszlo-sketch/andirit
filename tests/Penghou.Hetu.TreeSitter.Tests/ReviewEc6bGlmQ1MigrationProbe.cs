namespace Penghou.Hetu.TreeSitter.Tests;

// Independent EC6B-GLM-Q1 review probe: authentic p6 -> p7 historical
// migration against a durable LatticeDb publication seeded by the real p6
// extractor (owner-accepted commit 1d11b73). Inert unless EC6B_P6_DB,
// EC6B_WEBSITE_SNAPSHOT, and EC6B_EVIDENCE_PATH are set.
public sealed class ReviewEc6bGlmQ1MigrationProbe(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task Probe13_P6ToP7PreservesEveryExistingIdentityAndAddsOnlyResolvesTo()
    {
        var snapshotPath = Environment.GetEnvironmentVariable("EC6B_WEBSITE_SNAPSHOT");
        var databasePath = Environment.GetEnvironmentVariable("EC6B_P6_DB");
        var evidencePath = Environment.GetEnvironmentVariable("EC6B_EVIDENCE_PATH");
        if (string.IsNullOrWhiteSpace(snapshotPath) || string.IsNullOrWhiteSpace(databasePath) ||
            string.IsNullOrWhiteSpace(evidencePath) || !File.Exists(databasePath))
            return;

        var repositoryId = new CodeRepositoryId("repo:q1-migration");
        using var store = new LatticeCodeGraphStore(databasePath);

        // 1. Read the authentic p6 publication and prove store roundtrip.
        var latest = (await store.GetLatestIndexStateAsync(repositoryId))!;
        output.WriteLine($"pre-migration publication: {latest.IndexRunId.Value} sources={latest.Sources.Count}");
        var p6Units = await store.GetPublishedUnitsAsync(repositoryId);
        var p6 = Snapshot(p6Units);
        var p6Written = (await File.ReadAllLinesAsync(Path.Combine(evidencePath, "website-p6-snapshot.txt")))
            .OrderBy(line => line, StringComparer.Ordinal).ToArray();
        Assert.Equal(p6Written, p6);
        Assert.All(p6Units, unit => Assert.EndsWith(".p6", unit.Origin.PluginVersion, StringComparison.Ordinal));
        Assert.DoesNotContain(p6, line => line.Contains(" resolves-to ", StringComparison.Ordinal));
        output.WriteLine($"p6: units={p6Units.Count} nodes={p6.Count(line => line.StartsWith("NODE ", StringComparison.Ordinal))} " +
            $"declarations={p6.Count(line => line.StartsWith("DECL ", StringComparison.Ordinal))} " +
            $"edges={p6.Count(line => line.StartsWith("EDGE ", StringComparison.Ordinal))}");

        // 2. Reindex the same snapshot with the p7 implementation.
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]),
            store);
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, snapshotPath),
            new CodeIndexRunId("run:q1-p7-migration"));
        output.WriteLine($"migration status={result.Diagnostics.Status} pluginsExecuted={result.Diagnostics.PluginsExecuted}");
        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
        Assert.Equal(2, result.Diagnostics.PluginsExecuted);

        var p7Units = await store.GetPublishedUnitsAsync(repositoryId);
        var p7 = Snapshot(p7Units);
        Assert.All(p7Units, unit => Assert.EndsWith(".p7", unit.Origin.PluginVersion, StringComparison.Ordinal));
        output.WriteLine($"p7: units={p7Units.Count} nodes={p7.Count(line => line.StartsWith("NODE ", StringComparison.Ordinal))} " +
            $"declarations={p7.Count(line => line.StartsWith("DECL ", StringComparison.Ordinal))} " +
            $"edges={p7.Count(line => line.StartsWith("EDGE ", StringComparison.Ordinal))}");

        // 3. Every p6 line must survive byte-identically.
        var missing = p6.Except(p7, StringComparer.Ordinal).ToArray();
        output.WriteLine($"missing p6 lines: {missing.Length}");
        Assert.Empty(missing);

        // 4. The only additions are resolves-to edges; nothing else changed.
        var added = p7.Except(p6, StringComparer.Ordinal).ToArray();
        Assert.All(added, line => Assert.Contains(" resolves-to ", line, StringComparison.Ordinal));
        Assert.Equal(3, added.Length);
        output.WriteLine($"added lines ({added.Length}):");
        foreach (var line in added)
            output.WriteLine($"  {line}");

        // 5. New resolver edges point at the independently established targets.
        var p7Edges = p7Units.SelectMany(unit => unit.Edges).ToArray();
        var nodes = p7Units.SelectMany(unit => unit.Nodes).ToArray();
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var node in nodes)
            paths[node.Id.Value] = node.QualifiedName ?? "";
        var pairs = p7Edges
            .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)
            .Select(edge => $"{paths[edge.SourceId.Value]}->{paths[edge.TargetId.Value]}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "src/lib/schema.ts->src/lib/paths.ts",
                "src/lib/schema.ts->src/lib/site.ts",
                "src/pages/robots.txt.ts->src/lib/paths.ts"
            ],
            pairs);

        await File.WriteAllLinesAsync(
            Path.Combine(evidencePath, "website-p7-migration-snapshot.txt"), p7);
    }

    private static string[] Snapshot(IReadOnlyList<CodeIndexUnitReplacement> units) =>
        units
            .SelectMany(unit => unit.Nodes.Select(node =>
                $"NODE {node.Id.Value} {node.Kind.Value} {node.QualifiedName}"))
            .Concat(units.SelectMany(unit => unit.Declarations.Select(declaration =>
                $"DECL {declaration.Id.Value} {declaration.SymbolNodeId.Value}")))
            .Concat(units.SelectMany(unit => unit.Edges.Select(edge =>
                $"EDGE {edge.Id.Value} {edge.Kind.Value} {edge.SourceId.Value} {edge.TargetId.Value}")))
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();
}
