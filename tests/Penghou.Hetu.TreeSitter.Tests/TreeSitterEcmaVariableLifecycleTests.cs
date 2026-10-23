namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>EC-5 publication tests for file-level variable facts.</summary>
public sealed class TreeSitterEcmaVariableLifecycleTests
{
    [Fact]
    public async Task IndexAsync_PublishesAndReplacesVariableFactsAcrossIncrementalChanges()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ec5-variables-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            var path = Path.Combine(src.FullName, "data.ts");
            await File.WriteAllTextAsync(path, "export const projects = [];\nconst first = 1, second = 2;\n");

            var (repositoryId, store, indexing) = CreateTypeScriptService();
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var queries = new CodeGraphQueryService(store);

            var firstRun = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:ec5-first"));
            Assert.Equal(CodeIndexRunStatus.Completed, firstRun.Diagnostics.Status);
            var projects = Variable(await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "projects"));
            var originalProjectsId = projects.Id;
            Assert.Equal(CodeNodeKinds.Field, projects.Kind);
            Assert.Equal("const export", Modifiers(projects));
            var projectsDeclarations = await queries.FindDeclarationsAsync(repositoryId, projects.SymbolId!);
            var projectsDeclaration = Assert.Single(projectsDeclarations);
            Assert.Equal("src/data.ts", projectsDeclaration.Location.Path);
            Assert.Equal(1, projectsDeclaration.Location.StartLine);
            Assert.Equal(14, projectsDeclaration.Location.StartColumn);
            var enclosing = await queries.FindEnclosingSymbolAsync(repositoryId, "src/data.ts", 1, 14);
            Assert.NotNull(enclosing);
            Assert.Equal(projects.Id, enclosing.Symbol.Id);

            var originalFirst = Variable(await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "first"));
            var originalSecond = Variable(await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "second"));
            Assert.Single(await queries.FindDeclarationsAsync(repositoryId, originalFirst.SymbolId!));
            Assert.Single(await queries.FindDeclarationsAsync(repositoryId, originalSecond.SymbolId!));
            Assert.Equal(3, (await queries.GetDeclarationsInFileAsync(repositoryId, "src/data.ts")).Count);

            var unchanged = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:ec5-unchanged"));
            Assert.Equal(CodeIndexRunStatus.Completed, unchanged.Diagnostics.Status);
            Assert.Equal(originalProjectsId, Variable(
                await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "projects")).Id);
            Assert.Single(await queries.FindDeclarationsAsync(repositoryId, projects.SymbolId!));

            await File.WriteAllTextAsync(
                path,
                "export const projects = [];\nconst first = 1, second = 2;\nlet currentProject = null;\n");
            var added = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:ec5-add"));
            Assert.Equal(CodeIndexRunStatus.Completed, added.Diagnostics.Status);
            Assert.Single((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "currentProject")).Candidates);

            await File.WriteAllTextAsync(path, "const projectCatalog = [];\nconst first = 1;\nlet currentProject = null;\n");
            var renamed = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:ec5-rename"));
            Assert.Equal(CodeIndexRunStatus.Completed, renamed.Diagnostics.Status);
            Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "projects")).Candidates);
            Assert.Empty(await queries.FindDeclarationsAsync(repositoryId, projects.SymbolId!));
            var projectCatalog = Variable(await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "projectCatalog"));
            Assert.NotEqual(originalProjectsId, projectCatalog.Id);
            Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "second")).Candidates);
            Assert.Single(await queries.FindDeclarationsAsync(repositoryId, originalFirst.SymbolId!));

            // Written export changes node metadata while preserving the
            // declaration identity; it does not create an export edge.
            await File.WriteAllTextAsync(path, "export const projectCatalog = [];\nexport const first = 1;\nlet currentProject = null;\n");
            var exported = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:ec5-export-add"));
            Assert.Equal(CodeIndexRunStatus.Completed, exported.Diagnostics.Status);
            var exportedFirst = Variable(await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "first"));
            Assert.Equal(originalFirst.Id, exportedFirst.Id);
            Assert.Equal("const export", Modifiers(exportedFirst));

            await File.WriteAllTextAsync(path, "export const projectCatalog = [];\nconst first = 1;\nlet currentProject = null;\n");
            var unexported = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:ec5-export-remove"));
            Assert.Equal(CodeIndexRunStatus.Completed, unexported.Diagnostics.Status);
            var currentFirst = Variable(await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "first"));
            Assert.Equal(originalFirst.Id, currentFirst.Id);
            Assert.Equal("const", Modifiers(currentFirst));

            File.Delete(path);
            var deleted = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:ec5-delete"));
            Assert.Equal(CodeIndexRunStatus.Completed, deleted.Diagnostics.Status);
            Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "projectCatalog")).Candidates);
            Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "first")).Candidates);
            Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "currentProject")).Candidates);
            Assert.Empty(await queries.GetDeclarationsInFileAsync(repositoryId, "src/data.ts"));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task IndexAsync_IndexesMjsVariablesThroughTheJavaScriptPipeline()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ec5-mjs-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            await File.WriteAllTextAsync(Path.Combine(src.FullName, "data.mjs"), "export const projects = [];\n");
            var repositoryId = new CodeRepositoryId("repo:ec5-mjs");
            var store = new InMemoryCodeGraphStore();
            var indexing = new CodeIndexingService(
                new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
                new CodeGraphPluginRegistry([new TreeSitterJavaScriptPlugin()]),
                store);
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var queries = new CodeGraphQueryService(store);

            var result = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:ec5-mjs"));

            Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
            var projects = Variable(await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "projects"));
            Assert.Equal(CodeNodeKinds.Field, projects.Kind);
            Assert.Equal("const export", Modifiers(projects));
            Assert.Single(await queries.FindDeclarationsAsync(repositoryId, projects.SymbolId!));
            Assert.False(new TreeSitterTypeScriptPlugin().CanHandle("src/data.mjs"));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static (CodeRepositoryId RepositoryId, InMemoryCodeGraphStore Store, CodeIndexingService Indexing)
        CreateTypeScriptService()
    {
        var repositoryId = new CodeRepositoryId("repo:ec5-variables");
        var store = new InMemoryCodeGraphStore();
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
            store);
        return (repositoryId, store, indexing);
    }

    private static CodeGraphNode Variable(CodeSymbolLookupResult query) =>
        Assert.Single(query.Candidates);

    private static string? Modifiers(CodeGraphNode node) =>
        node.Properties.TryGetValue(CodePropertyKeys.Modifiers, out var value)
            ? ((CodeTextProperty)value).Value
            : null;
}
