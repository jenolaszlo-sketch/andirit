namespace Penghou.Hetu.TreeSitter.Tests;

// Independent EC6B-GLM-Q2 review probes for version invalidation and
// historical preservation. Inert unless the env inputs are present.
public sealed class ReviewEc6bGlmQ2HistoricalProbe(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task ProbeQ2E_P7FalseFactsAreInvalidatedAndRemovedByP8()
    {
        var fixture = Environment.GetEnvironmentVariable("EC6B_Q2_FIXTURE");
        var databasePath = Environment.GetEnvironmentVariable("EC6B_Q2_P7_DB");
        var evidencePath = Environment.GetEnvironmentVariable("EC6B_Q2_EVIDENCE");
        if (string.IsNullOrWhiteSpace(fixture) || string.IsNullOrWhiteSpace(databasePath) ||
            !File.Exists(databasePath))
            return;

        var repositoryId = new CodeRepositoryId("repo:q2-stale");
        using (var store = new LatticeCodeGraphStore(databasePath))
        {
            var latest = (await store.GetLatestIndexStateAsync(repositoryId))!;
            Assert.Equal("run:q2-p7-seed", latest.IndexRunId.Value);
            var p7Units = await store.GetPublishedUnitsAsync(repositoryId);
            Assert.All(p7Units, unit => Assert.EndsWith(".p7", unit.Origin.PluginVersion, StringComparison.Ordinal));
            var p7Paths = FilePaths(p7Units);
            var p7ModuleEdges = p7Units.SelectMany(unit => unit.Edges)
                .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)
                .ToArray();
            output.WriteLine("p7 module edges:");
            foreach (var edge in p7ModuleEdges.OrderBy(edge => edge.Id.Value, StringComparer.Ordinal))
                output.WriteLine($"  {Describe(edge, p7Paths)}");

            // Authentic p7 defects are present before remediation.
            var falseEdge = Assert.Single(p7ModuleEdges, edge =>
                Text(edge, CodePropertyKeys.Specifier) == "./a" &&
                p7Paths[edge.TargetId.Value] == "src/a.ts");
            Assert.Equal("0.2.0-preview.6+resolver1.relative-v1", falseEdge.Evidence.ProviderVersion);
            var falseTypeOnly = Assert.Single(p7ModuleEdges, edge =>
                Text(edge, CodePropertyKeys.Specifier) == "./m");
            Assert.True(falseTypeOnly.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var typeOnly) &&
                typeOnly is CodeBooleanProperty { Value: true });
            var plainEdge = Assert.Single(p7ModuleEdges, edge =>
                Text(edge, CodePropertyKeys.Specifier) == "./plain");

            var p7Snapshot = Snapshot(p7Units);
            if (!string.IsNullOrWhiteSpace(evidencePath))
            {
                var written = (await File.ReadAllLinesAsync(Path.Combine(evidencePath, "p7-snapshot.txt")))
                    .OrderBy(line => line, StringComparer.Ordinal).ToArray();
                Assert.Equal(written, p7Snapshot);
            }

            // Reindex unchanged sources with the remediated p8 implementation.
            var indexing = new CodeIndexingService(
                new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
                new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
                store);
            var result = await indexing.IndexAsync(
                new CodeRepositoryDescriptor(repositoryId, fixture),
                new CodeIndexRunId("run:q2-p8"));
            Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
            Assert.Equal(1, result.Diagnostics.PluginsExecuted);
            output.WriteLine($"migration status={result.Diagnostics.Status} pluginsExecuted={result.Diagnostics.PluginsExecuted}");

            var p8Units = await store.GetPublishedUnitsAsync(repositoryId);
            Assert.All(p8Units, unit => Assert.EndsWith(".p8", unit.Origin.PluginVersion, StringComparison.Ordinal));
            var p8Paths = FilePaths(p8Units);
            var p8ModuleEdges = p8Units.SelectMany(unit => unit.Edges)
                .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)
                .ToArray();
            output.WriteLine("p8 module edges:");
            foreach (var edge in p8ModuleEdges.OrderBy(edge => edge.Id.Value, StringComparer.Ordinal))
                output.WriteLine($"  {Describe(edge, p8Paths)}");

            // The false edge is gone; unescaped resolution survives unchanged.
            Assert.DoesNotContain(p8ModuleEdges, edge =>
                Text(edge, CodePropertyKeys.Specifier) == "./a");
            Assert.DoesNotContain(p8ModuleEdges, edge => p8Paths[edge.TargetId.Value] == "src/a.ts");
            var survivingPlain = Assert.Single(p8ModuleEdges, edge =>
                Text(edge, CodePropertyKeys.Specifier) == "./plain");
            Assert.Equal(plainEdge.Id, survivingPlain.Id);
            Assert.Equal("src/plain.ts", p8Paths[survivingPlain.TargetId.Value]);

            // The type-only property is corrected under the same occurrence identity.
            var corrected = Assert.Single(p8ModuleEdges, edge =>
                Text(edge, CodePropertyKeys.Specifier) == "./m");
            Assert.Equal(falseTypeOnly.Id, corrected.Id);
            Assert.False(corrected.Properties.ContainsKey(CodePropertyKeys.TypeOnly));
            Assert.EndsWith("+resolver2.relative-v1", corrected.Evidence.ProviderVersion, StringComparison.Ordinal);

            // Structural facts and non-resolver identities are preserved.
            var missing = p7Snapshot
                .Where(line => !line.Contains(" resolves-to ", StringComparison.Ordinal))
                .Except(Snapshot(p8Units), StringComparer.Ordinal)
                .ToArray();
            output.WriteLine($"missing non-resolver p7 lines: {missing.Length}");
            Assert.Empty(missing);
            Assert.Equal(3, p8Units.SelectMany(unit => unit.Edges).Count(edge => edge.Kind == CodeEdgeKinds.Imports));
        }

        // Corrected facts survive a durable close/reopen.
        using (var reopened = new LatticeCodeGraphStore(databasePath))
        {
            var units = await reopened.GetPublishedUnitsAsync(repositoryId);
            Assert.All(units, unit => Assert.EndsWith(".p8", unit.Origin.PluginVersion, StringComparison.Ordinal));
            var edges = units.SelectMany(unit => unit.Edges)
                .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
            Assert.DoesNotContain(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./a");
            var reopenedPlain = Assert.Single(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./plain");
            Assert.Equal("0.2.0-preview.6+resolver2.relative-v1", reopenedPlain.Evidence.ProviderVersion);
            output.WriteLine($"reopen module edges={edges.Length}");
        }
    }

    [Fact]
    public async Task ProbeQ2F_P6ToP8WebsiteMigrationPreservesIdentities()
    {
        var snapshotPath = Environment.GetEnvironmentVariable("EC6B_WEBSITE_SNAPSHOT");
        var databasePath = Environment.GetEnvironmentVariable("EC6B_Q2_P6_DB");
        var evidencePath = Environment.GetEnvironmentVariable("EC6B_Q2_EVIDENCE");
        if (string.IsNullOrWhiteSpace(snapshotPath) || string.IsNullOrWhiteSpace(databasePath) ||
            string.IsNullOrWhiteSpace(evidencePath) || !File.Exists(databasePath))
            return;

        var repositoryId = new CodeRepositoryId("repo:q2-migration");
        using var store = new LatticeCodeGraphStore(databasePath);
        var latest = (await store.GetLatestIndexStateAsync(repositoryId))!;
        Assert.Equal("run:q2-p6-seed", latest.IndexRunId.Value);
        var p6Units = await store.GetPublishedUnitsAsync(repositoryId);
        Assert.All(p6Units, unit => Assert.EndsWith(".p6", unit.Origin.PluginVersion, StringComparison.Ordinal));
        var p6 = Snapshot(p6Units);
        var p6Written = (await File.ReadAllLinesAsync(Path.Combine(evidencePath, "website-p6-snapshot.txt")))
            .OrderBy(line => line, StringComparer.Ordinal).ToArray();
        Assert.Equal(p6Written, p6);
        output.WriteLine($"p6: units={p6Units.Count} nodeLines={Count(p6, "NODE ")} declarations={Count(p6, "DECL ")} edges={Count(p6, "EDGE ")}");

        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]),
            store);
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, snapshotPath),
            new CodeIndexRunId("run:q2-p8-migration"));
        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
        Assert.Equal(2, result.Diagnostics.PluginsExecuted);
        output.WriteLine($"migration status={result.Diagnostics.Status} pluginsExecuted={result.Diagnostics.PluginsExecuted}");

        var p8Units = await store.GetPublishedUnitsAsync(repositoryId);
        Assert.All(p8Units, unit => Assert.EndsWith(".p8", unit.Origin.PluginVersion, StringComparison.Ordinal));
        var p8 = Snapshot(p8Units);
        output.WriteLine($"p8: units={p8Units.Count} nodeLines={Count(p8, "NODE ")} declarations={Count(p8, "DECL ")} edges={Count(p8, "EDGE ")}");

        var missing = p6.Except(p8, StringComparer.Ordinal).ToArray();
        output.WriteLine($"missing p6 lines: {missing.Length}");
        Assert.Empty(missing);
        var added = p8.Except(p6, StringComparer.Ordinal).ToArray();
        Assert.All(added, line => Assert.Contains(" resolves-to ", line, StringComparison.Ordinal));
        Assert.Equal(3, added.Length);

        var paths = FilePaths(p8Units);
        var pairs = p8Units.SelectMany(unit => unit.Edges)
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

        // Resolver identities match the previously reviewed p7 publication.
        var edgeIds = p8Units.SelectMany(unit => unit.Edges)
            .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)
            .Select(edge => edge.Id.Value)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "typescript:3ddbda1c4e15868e5b182b30df5a0b6045f6da2461c8419efecc5e65540d7016",
                "typescript:6e6cb95801bbcd7d421eb6b6e89ddb226bb6519f8bcd2b38d6cc69eb53fec0ea",
                "typescript:a9764b44dbd07bd010a437e37001deb17df88f7dd9d441490562b8b7aaa00eab"
            ],
            edgeIds);
        foreach (var edge in p8Units.SelectMany(unit => unit.Edges).Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo))
            Assert.EndsWith("+resolver2.relative-v1", edge.Evidence.ProviderVersion, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProbeQ2H_WebsiteGroundTruthStillMatches()
    {
        var snapshotPath = Environment.GetEnvironmentVariable("EC6B_WEBSITE_SNAPSHOT");
        var evidencePath = Environment.GetEnvironmentVariable("EC6B_Q2_EVIDENCE");
        if (string.IsNullOrWhiteSpace(snapshotPath) || string.IsNullOrWhiteSpace(evidencePath))
            return;

        var databasePath = Path.Combine(evidencePath, "website-q2-graph.db");
        if (File.Exists(databasePath))
            File.Delete(databasePath);
        var repositoryId = new CodeRepositoryId("repo:q2-website");
        using var store = new LatticeCodeGraphStore(databasePath);
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]),
            store);
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, snapshotPath),
            new CodeIndexRunId("run:q2-website"));
        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);

        var units = await store.GetPublishedUnitsAsync(repositoryId);
        var nodes = units.SelectMany(unit => unit.Nodes).ToArray();
        var edges = units.SelectMany(unit => unit.Edges).ToArray();
        var declarations = units.SelectMany(unit => unit.Declarations).ToArray();
        var distinctNodes = nodes.Select(node => node.Id.Value).Distinct(StringComparer.Ordinal).Count();
        output.WriteLine($"nodes={distinctNodes} declarations={declarations.Length} edges={edges.Length}");
        foreach (var group in edges.GroupBy(edge => edge.Kind.Value).OrderBy(group => group.Key, StringComparer.Ordinal))
            output.WriteLine($"kind {group.Key}={group.Count()}");

        Assert.Equal(128, distinctNodes);
        Assert.Equal(83, declarations.Length);
        Assert.Equal(280, edges.Length);
        Assert.Equal(9, edges.Count(edge => edge.Kind == CodeEdgeKinds.Imports));
        Assert.Equal(1, edges.Count(edge => edge.Kind == CodeEdgeKinds.Inherits));
        Assert.Equal(101, edges.Count(edge => edge.Kind == CodeEdgeKinds.InvokesName));
        Assert.Equal(3, edges.Count(edge => edge.Kind == CodeEdgeKinds.ResolvesTo));
        Assert.DoesNotContain(edges, edge => edge.Kind.Value is "exports" or "binds");

        var paths = FilePaths(units);
        var pairs = edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)
            .Select(edge => $"{paths[edge.SourceId.Value]}->{paths[edge.TargetId.Value]}")
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            [
                "src/lib/schema.ts->src/lib/paths.ts",
                "src/lib/schema.ts->src/lib/site.ts",
                "src/pages/robots.txt.ts->src/lib/paths.ts"
            ],
            pairs);
    }

    private static Dictionary<string, string> FilePaths(IReadOnlyList<CodeIndexUnitReplacement> units) =>
        units.SelectMany(unit => unit.Nodes)
            .Where(node => node.Kind == CodeNodeKinds.File)
            .ToDictionary(node => node.Id.Value, node => node.QualifiedName ?? "", StringComparer.Ordinal);

    private static string Describe(CodeGraphEdge edge, Dictionary<string, string> paths) =>
        $"{edge.Id.Value} specifier='{Text(edge, CodePropertyKeys.Specifier)}' " +
        $"{paths[edge.SourceId.Value]}->{paths[edge.TargetId.Value]} " +
        $"typeOnly={edge.Properties.ContainsKey(CodePropertyKeys.TypeOnly)}";

    private static string Text(CodeGraphEdge edge, string key, bool required = true) =>
        edge.Properties.TryGetValue(key, out var value) && value is CodeTextProperty text
            ? text.Value
            : required
                ? throw new InvalidOperationException($"Missing text property '{key}'.")
                : "";

    private static int Count(IReadOnlyList<string> lines, string prefix) =>
        lines.Count(line => line.StartsWith(prefix, StringComparison.Ordinal));

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
