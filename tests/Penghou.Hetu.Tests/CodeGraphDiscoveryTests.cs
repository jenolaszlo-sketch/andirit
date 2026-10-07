namespace Penghou.Hetu.Tests;

public sealed class CodeGraphDiscoveryTests
{
    [Fact]
    public async Task Discovery_FiltersByKindAndLanguage()
    {
        var queries = await QueriesAsync();
        var repositoryId = new CodeRepositoryId("repo:discovery");

        var callables = await queries.FindSymbolsAsync(
            repositoryId,
            new CodeSymbolDiscoveryQuery("example", nodeKinds: [CodeNodeKinds.Callable]));

        Assert.Equal(
            ["Example.Widget.Run"],
            callables.Symbols.Select(node => node.QualifiedName));
        Assert.False(callables.Truncated);

        var elixir = await queries.FindSymbolsAsync(
            repositoryId,
            new CodeSymbolDiscoveryQuery("example", languages: ["elixir"]));

        Assert.Equal(
            ["Example.Accounts"],
            elixir.Symbols.Select(node => node.QualifiedName));

        var csharp = await queries.FindSymbolsAsync(
            repositoryId,
            new CodeSymbolDiscoveryQuery("example", languages: ["csharp"]));

        Assert.Equal(
            ["Example.Widget", "Example.Widget.Run"],
            csharp.Symbols.Select(node => node.QualifiedName));
    }

    [Fact]
    public async Task Discovery_ScopesToContainersAndProjects()
    {
        var queries = await QueriesAsync();
        var repositoryId = new CodeRepositoryId("repo:discovery");

        var inWidget = await queries.FindSymbolsAsync(
            repositoryId,
            new CodeSymbolDiscoveryQuery(containerId: new CodeNodeId("node:widget")));

        Assert.Equal(
            ["Example.Widget.Run"],
            inWidget.Symbols.Select(node => node.QualifiedName));

        var inFile = await queries.FindSymbolsAsync(
            repositoryId,
            new CodeSymbolDiscoveryQuery(containerId: new CodeNodeId("node:file-a")));

        Assert.Equal(
            ["Example.Widget", "Example.Widget.Run"],
            inFile.Symbols.Select(node => node.QualifiedName));

        var inProject = await queries.FindSymbolsAsync(
            repositoryId,
            new CodeSymbolDiscoveryQuery(projectPath: "src/App"));

        Assert.Equal(
            ["Example.Accounts", "Example.Widget", "Example.Widget.Run"],
            inProject.Symbols.Select(node => node.QualifiedName));
        Assert.False(inProject.Truncated);

        var missing = await queries.FindSymbolsAsync(
            repositoryId,
            new CodeSymbolDiscoveryQuery(projectPath: "src/Missing"));

        Assert.Empty(missing.Symbols);
        Assert.False(missing.Truncated);
    }

    [Fact]
    public async Task Discovery_TruncatesDeterministically()
    {
        var queries = await QueriesAsync();
        var repositoryId = new CodeRepositoryId("repo:discovery");

        var bounded = await queries.FindSymbolsAsync(
            repositoryId,
            new CodeSymbolDiscoveryQuery("example", maxResults: 2));

        Assert.Equal(2, bounded.Symbols.Count);
        Assert.True(bounded.Truncated);
        Assert.Equal(
            ["Example.Accounts", "Example.Widget"],
            bounded.Symbols.Select(node => node.QualifiedName));
    }

    [Fact]
    public void Discovery_RequiresScopeAndRejectsAmbiguousScope()
    {
        Assert.Throws<ArgumentException>(() => new CodeSymbolDiscoveryQuery());
        Assert.Throws<ArgumentException>(() => new CodeSymbolDiscoveryQuery(
            "example",
            containerId: new CodeNodeId("node:widget"),
            projectPath: "src/App"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CodeSymbolDiscoveryQuery("example", maxResults: 0));
    }

    [Fact]
    public async Task EnclosingSymbol_SelectsTightestDeclaration()
    {
        var queries = await QueriesAsync();
        var repositoryId = new CodeRepositoryId("repo:discovery");

        var inner = await queries.FindEnclosingSymbolAsync(repositoryId, "src/A.cs", 5, 5);

        Assert.NotNull(inner);
        Assert.Equal("Example.Widget.Run", inner.Symbol.QualifiedName);
        Assert.Equal("decl:run", inner.Declaration.Id.Value);

        var outer = await queries.FindEnclosingSymbolAsync(repositoryId, "src/A.cs", 3, 1);

        Assert.NotNull(outer);
        Assert.Equal("Example.Widget", outer.Symbol.QualifiedName);

        // Exclusive end boundary: line 10 column 5 sits past the (10, 1) end.
        Assert.Null(await queries.FindEnclosingSymbolAsync(repositoryId, "src/A.cs", 10, 5));
        Assert.Null(await queries.FindEnclosingSymbolAsync(repositoryId, "src/A.cs", 50, 1));
        Assert.Null(await queries.FindEnclosingSymbolAsync(repositoryId, "src/Missing.cs", 1, 1));
    }

    [Fact]
    public async Task SymbolsInFile_ReturnsDeclaredSymbols()
    {
        var queries = await QueriesAsync();
        var repositoryId = new CodeRepositoryId("repo:discovery");

        Assert.Equal(
            ["Example.Widget", "Example.Widget.Run"],
            (await queries.GetSymbolsInFileAsync(repositoryId, "src/A.cs"))
                .Select(node => node.QualifiedName));
        Assert.Equal(
            ["Example.Accounts"],
            (await queries.GetSymbolsInFileAsync(repositoryId, "lib/b.ex"))
                .Select(node => node.QualifiedName));
        Assert.Empty(await queries.GetSymbolsInFileAsync(repositoryId, "src/Missing.cs"));
    }

    [Fact]
    public async Task ProjectQueries_ReturnSymbolsAndDeclarations()
    {
        var queries = await QueriesAsync();
        var repositoryId = new CodeRepositoryId("repo:discovery");

        Assert.Equal(
            ["Example.Accounts", "Example.Widget", "Example.Widget.Run"],
            (await queries.GetSymbolsInProjectAsync(repositoryId, "src/App"))
                .Select(node => node.QualifiedName));
        Assert.Empty(await queries.GetSymbolsInProjectAsync(repositoryId, "src/Missing"));

        var declarations = await queries.GetDeclarationsInProjectAsync(repositoryId, "src/App");

        Assert.Equal(3, declarations.Count);
        Assert.Equal("lib/b.ex", declarations[0].Location.Path);
        Assert.Equal("src/A.cs", declarations[1].Location.Path);
        Assert.Equal(2, declarations[1].Location.StartLine);
        Assert.Equal("src/A.cs", declarations[2].Location.Path);
        Assert.Equal(4, declarations[2].Location.StartLine);
        Assert.Empty(await queries.GetDeclarationsInProjectAsync(repositoryId, "src/Missing"));
    }

    private static async Task<CodeGraphQueryService> QueriesAsync()
    {
        var store = new InMemoryCodeGraphStore();
        var repositoryId = new CodeRepositoryId("repo:discovery");
        var runId = new CodeIndexRunId("run:discovery");
        var pluginId = new CodePluginId("plugin:discovery");
        var started = DateTimeOffset.UtcNow;
        await store.UpsertRepositoryAsync(new(repositoryId));
        await store.StoreIndexRunAsync(new(repositoryId, runId, started, plugins: [pluginId]));

        var project = new CodeGraphNode(
            new CodeNodeId("node:project"), CodeNodeKinds.Project, "App", "src/App");
        var fileA = new CodeGraphNode(
            new CodeNodeId("node:file-a"), CodeNodeKinds.File, "A.cs", "src/A.cs");
        var fileB = new CodeGraphNode(
            new CodeNodeId("node:file-b"), CodeNodeKinds.File, "b.ex", "lib/b.ex");
        var widget = SymbolNode("widget", CodeNodeKinds.Type, "Widget", "Example.Widget", "csharp");
        var run = SymbolNode("run", CodeNodeKinds.Callable, "Run", "Example.Widget.Run", "csharp");
        var accounts = SymbolNode("accounts", CodeNodeKinds.Type, "Accounts", "Example.Accounts", "elixir");

        CodeGraphEdge Edge(string id, CodeNodeId source, CodeNodeId target, CodeEdgeKind kind) =>
            new(
                new CodeEdgeId($"edge:{id}"),
                source,
                target,
                kind,
                new CodeEvidence(EvidenceQuality.Structural, "tests"));
        await store.StageIndexUnitAsync(new(
            new CodeFactOrigin(repositoryId, pluginId, "1.0.0", runId, new("unit:discovery")),
            [project, fileA, fileB, widget, run, accounts],
            [
                new CodeGraphDeclaration(
                    new CodeDeclarationId("decl:widget"),
                    widget.SymbolId!,
                    widget.Id,
                    new CodeLocation("src/A.cs", 2, 1, 10, 1)),
                new CodeGraphDeclaration(
                    new CodeDeclarationId("decl:run"),
                    run.SymbolId!,
                    run.Id,
                    new CodeLocation("src/A.cs", 4, 3, 6, 20)),
                new CodeGraphDeclaration(
                    new CodeDeclarationId("decl:accounts"),
                    accounts.SymbolId!,
                    accounts.Id,
                    new CodeLocation("lib/b.ex", 1, 1, 20, 1))
            ],
            edges:
            [
                Edge("project-a", project.Id, fileA.Id, CodeEdgeKinds.Contains),
                Edge("project-b", project.Id, fileB.Id, CodeEdgeKinds.Contains),
                Edge("declares-widget", fileA.Id, widget.Id, CodeEdgeKinds.Declares),
                Edge("declares-run", fileA.Id, run.Id, CodeEdgeKinds.Declares),
                Edge("declares-accounts", fileB.Id, accounts.Id, CodeEdgeKinds.Declares),
                Edge("contains-run", widget.Id, run.Id, CodeEdgeKinds.Contains)
            ]));
        await store.CompleteIndexRunAsync(
            new(repositoryId, runId, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [pluginId]),
            new(repositoryId, runId, []));
        return new CodeGraphQueryService(store);
    }

    private static CodeGraphNode SymbolNode(
        string id,
        CodeNodeKind kind,
        string name,
        string qualifiedName,
        string language) =>
        new(
            new CodeNodeId($"node:{id}"),
            kind,
            name,
            qualifiedName,
            new CodeSymbolId($"symbol:{id}"),
            new Dictionary<string, CodePropertyValue>
            {
                [CodePropertyKeys.Language] = new CodeTextProperty(language)
            });
}
