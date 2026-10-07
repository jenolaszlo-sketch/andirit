namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>
/// Milestone 8A dogfood: the new discovery queries exercised against one
/// repository indexed from both C# (resolved) and Elixir (structural)
/// sources. This is the consumer evidence for the traversal-policy
/// question, not another unit test of each query in isolation.
/// </summary>
public sealed class QueryDogfoodTests
{
    private const string WidgetSource = """
        namespace Example;

        public class Widget
        {
            public int Add(int a, int b) => a + b;
            public int Run() => Add(1, 2);
        }
        """;

    private const string AccountsSource = """
        defmodule Example.Accounts do
          def get_user(id) do
            Repo.get(id)
          end
        end
        """;

    [Fact]
    public async Task Dogfood_DiscoverySpansResolvedAndStructuralSymbols()
    {
        using var workspace = await IndexWorkspaceAsync();

        // Partial names find both providers; language filters split them.
        var partial = await workspace.Queries.FindSymbolsAsync(
            workspace.RepositoryId,
            new CodeSymbolDiscoveryQuery("example"));

        Assert.Contains(partial.Symbols, node => node.QualifiedName == "Example.Widget");
        Assert.Contains(partial.Symbols, node => node.QualifiedName == "Example.Accounts");

        var csharp = await workspace.Queries.FindSymbolsAsync(
            workspace.RepositoryId,
            new CodeSymbolDiscoveryQuery("example", languages: ["csharp"]));

        Assert.All(csharp.Symbols, node => Assert.Equal(
            "csharp",
            ((CodeTextProperty)node.Properties[CodePropertyKeys.Language]).Value));

        var elixir = await workspace.Queries.FindSymbolsAsync(
            workspace.RepositoryId,
            new CodeSymbolDiscoveryQuery("example", languages: ["elixir"]));

        Assert.Contains(elixir.Symbols, node => node.QualifiedName == "Example.Accounts");
        Assert.DoesNotContain(elixir.Symbols, node => node.QualifiedName == "Example.Widget");

        // Name-pattern search is provider-neutral.
        var pattern = await workspace.Queries.FindNodesByNamePatternAsync(
            workspace.RepositoryId, "get_user");

        Assert.Contains(pattern.Candidates, node => node.QualifiedName == "Example.Accounts.get_user/1");
    }

    [Fact]
    public async Task Dogfood_EnclosingSymbolBridgesDiffsIntoBothGraphs()
    {
        using var workspace = await IndexWorkspaceAsync();

        var csharp = await workspace.Queries.FindEnclosingSymbolAsync(
            workspace.RepositoryId, "src/Widget.cs", 6, 30);

        Assert.NotNull(csharp);
        Assert.Equal("Example.Widget.Run()", csharp.Symbol.QualifiedName);

        var elixir = await workspace.Queries.FindEnclosingSymbolAsync(
            workspace.RepositoryId, "lib/accounts.ex", 3, 10);

        Assert.NotNull(elixir);
        Assert.Equal("Example.Accounts.get_user/1", elixir.Symbol.QualifiedName);
    }

    [Fact]
    public async Task Dogfood_StructuralContainmentStopsResolvedOnlyTraversal()
    {
        using var workspace = await IndexWorkspaceAsync();
        var lookup = await workspace.Queries.FindSymbolsByQualifiedNameAsync(
            workspace.RepositoryId, "Example.Accounts");
        var moduleId = lookup.Candidates.Single().Id;

        // A resolved-only traversal cannot cross structural containment to
        // reach anything: the evidence filter governs navigation as well as
        // matching today. This pins the open traversal-policy question on a
        // real mixed graph rather than deciding it theoretically.
        var resolvedOnly = await workspace.Queries.GetNeighborhoodAsync(
            workspace.RepositoryId,
            moduleId,
            options: new CodeGraphQueryOptions(
                evidenceQualities: [EvidenceQuality.Resolved],
                maxDepth: 5,
                maxNodes: 100,
                maxEdges: 250));

        Assert.DoesNotContain(
            resolvedOnly.Nodes,
            node => node.QualifiedName == "Example.Accounts.get_user/1");

        // Unfiltered traversal reaches the structural neighborhood, and the
        // resolved C# side keeps working through the same API.
        var neighborhood = await workspace.Queries.GetNeighborhoodAsync(
            workspace.RepositoryId,
            moduleId,
            options: new CodeGraphQueryOptions(maxDepth: 3, maxNodes: 100, maxEdges: 250));

        Assert.Contains(
            neighborhood.Nodes,
            node => node.QualifiedName == "Example.Accounts.get_user/1");

        var widget = await workspace.Queries.FindSymbolsByQualifiedNameAsync(
            workspace.RepositoryId, "Example.Widget.Run()");
        var callers = await workspace.Queries.FindCallersAsync(
            workspace.RepositoryId, widget.Candidates.Single().Id);

        Assert.Empty(callers.Edges);
        var callees = await workspace.Queries.FindCalleesAsync(
            workspace.RepositoryId, widget.Candidates.Single().Id);

        Assert.Contains(callees.Nodes, node => node.Name == "Add");
    }

    private static async Task<Workspace> IndexWorkspaceAsync()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-dogfood-");
        var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var lib = Directory.CreateDirectory(Path.Combine(directory.FullName, "lib"));
        await File.WriteAllTextAsync(Path.Combine(src.FullName, "Widget.cs"), WidgetSource);
        await File.WriteAllTextAsync(Path.Combine(lib.FullName, "accounts.ex"), AccountsSource);

        var repositoryId = new CodeRepositoryId("repo:dogfood");
        var store = new InMemoryCodeGraphStore();
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new CSharpCodeGraphPlugin(), new TreeSitterElixirPlugin()]),
            store);
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, directory.FullName),
            new CodeIndexRunId("run:dogfood"));

        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
        return new Workspace(directory, repositoryId, new CodeGraphQueryService(store));
    }

    private sealed class Workspace(DirectoryInfo directory, CodeRepositoryId repositoryId, CodeGraphQueryService queries)
        : IDisposable
    {
        public CodeRepositoryId RepositoryId { get; } = repositoryId;
        public CodeGraphQueryService Queries { get; } = queries;

        public void Dispose() => directory.Delete(recursive: true);
    }
}
