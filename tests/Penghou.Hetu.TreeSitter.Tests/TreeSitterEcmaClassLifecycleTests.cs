namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>
/// EC-3C lifecycle qualification: class and member identities survive
/// incremental reindexing, stale facts disappear with the source change, and
/// same-named classes from separate modules stay queryable without merging.
/// </summary>
public sealed class TreeSitterEcmaClassLifecycleTests
{
    [Fact]
    public async Task IndexAsync_RemovesRenamedAndRemovedClassMembers()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ts-lifecycle-members-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            var path = Path.Combine(src.FullName, "service.ts");
            await File.WriteAllTextAsync(path, """
                export class Service {
                  process(): number {
                    return 1;
                  }
                  count = 0;
                }
                """);
            var (repositoryId, store, indexing) = CreateService();
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var queries = new CodeGraphQueryService(store);

            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:first"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
            Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Service.process")).Candidates);
            Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Service.count")).Candidates);

            // Rename `process` to `run` and remove the `count` field.
            await File.WriteAllTextAsync(path, """
                export class Service {
                  run(): number {
                    return 1;
                  }
                }
                """);
            var second = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:second"));
            Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

            Assert.Empty(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Service.process")).Candidates);
            Assert.Empty(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Service.count")).Candidates);
            var run = Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Service.run")).Candidates);
            Assert.Single(await queries.FindDeclarationsAsync(repositoryId, run.SymbolId!));

            // Stale identities and their containment relationships are gone
            // from the current neighborhood.
            var service = Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Service")).Candidates);
            var neighborhood = await queries.GetNeighborhoodAsync(
                repositoryId,
                service.Id,
                options: new CodeGraphQueryOptions(maxDepth: 2, maxNodes: 100, maxEdges: 200));
            Assert.Contains(neighborhood.Nodes, node => node.QualifiedName == "Service.run");
            Assert.DoesNotContain(neighborhood.Nodes, node => node.QualifiedName == "Service.process");
            Assert.DoesNotContain(neighborhood.Nodes, node => node.QualifiedName == "Service.count");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task IndexAsync_RemovesFactsForRenamedClasses()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ts-lifecycle-class-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            var path = Path.Combine(src.FullName, "widget.ts");
            await File.WriteAllTextAsync(path, """
                export class OldWidget {
                  spin(): number {
                    return 1;
                  }
                  turns = 0;
                }
                """);
            var (repositoryId, store, indexing) = CreateService();
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var queries = new CodeGraphQueryService(store);

            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:first"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
            var oldWidget = Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "OldWidget")).Candidates);

            await File.WriteAllTextAsync(path, """
                export class NewWidget {
                  spin(): number {
                    return 1;
                  }
                }
                """);
            var second = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:second"));
            Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

            Assert.Empty(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "OldWidget")).Candidates);
            Assert.Empty(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "OldWidget.spin")).Candidates);
            Assert.Empty(await queries.FindDeclarationsAsync(repositoryId, oldWidget.SymbolId!));
            Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "NewWidget")).Candidates);

            var declarations = await queries.GetDeclarationsInFileAsync(repositoryId, "src/widget.ts");
            Assert.Equal(2, declarations.Count);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task IndexAsync_RemovesClassFactsWhenSourceFileIsDeleted()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ts-lifecycle-delete-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            var path = Path.Combine(src.FullName, "solo.ts");
            await File.WriteAllTextAsync(path, """
                export class Solo {
                  run(): number {
                    return 1;
                  }
                  count = 0;
                }
                """);
            var (repositoryId, store, indexing) = CreateService();
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var queries = new CodeGraphQueryService(store);

            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:first"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
            Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Solo")).Candidates);

            File.Delete(path);
            var second = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:second"));
            Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

            Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Solo")).Candidates);
            Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Solo.count")).Candidates);
            Assert.Empty(await queries.GetDeclarationsInFileAsync(repositoryId, "src/solo.ts"));
            Assert.Null(await queries.FindEnclosingSymbolAsync(repositoryId, "src/solo.ts", 1, 1));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task IndexAsync_KeepsSameNameClassesDistinctThroughQueries()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ts-lifecycle-query-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            await File.WriteAllTextAsync(Path.Combine(src.FullName, "inventory.ts"), """
                export class Service {
                  process(): number {
                    return 1;
                  }
                  inventoryOnly = 1;
                }
                """);
            await File.WriteAllTextAsync(Path.Combine(src.FullName, "billing.ts"), """
                export class Service {
                  process(): number {
                    return 1;
                  }
                  billingOnly = 2;
                }
                """);
            var (repositoryId, store, indexing) = CreateService();
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var queries = new CodeGraphQueryService(store);

            var result = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:query"));
            Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);

            // Both classes are indexed under one display name; exact lookup
            // reports both candidates instead of silently selecting one.
            var services = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Service")).Candidates;
            Assert.Equal(2, services.Count);
            Assert.Equal(2, services.Select(node => node.Id).Distinct().Count());

            var processes = (await queries.FindSymbolsByQualifiedNameAsync(
                repositoryId, "Service.process")).Candidates;
            Assert.Equal(2, processes.Count);
            Assert.Equal(2, processes.Select(node => node.Id).Distinct().Count());

            // Each class neighborhood reaches its own field and never the
            // unrelated same-named class's field.
            var fieldsByClass = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var service in services)
            {
                var neighborhood = await queries.GetNeighborhoodAsync(
                    repositoryId,
                    service.Id,
                    options: new CodeGraphQueryOptions(maxDepth: 2, maxNodes: 100, maxEdges: 200));
                Assert.Contains(neighborhood.Nodes, node => node.QualifiedName == "Service.process");
                var field = Assert.Single(neighborhood.Nodes, node =>
                    node.Kind == CodeNodeKinds.Property);
                fieldsByClass[service.Id.Value] = field.QualifiedName!;
            }

            Assert.Equal(
                ["Service.billingOnly", "Service.inventoryOnly"],
                fieldsByClass.Values.OrderBy(value => value, StringComparer.Ordinal));

            // Enclosing-symbol lookup resolves the position to the field in
            // the file that contains it.
            var enclosing = await queries.FindEnclosingSymbolAsync(
                repositoryId,
                "src/inventory.ts",
                5,
                19);
            Assert.NotNull(enclosing);
            Assert.Equal("Service.inventoryOnly", enclosing.Symbol.QualifiedName);

            var classEnclosing = await queries.FindEnclosingSymbolAsync(
                repositoryId,
                "src/billing.ts",
                1,
                8);
            Assert.NotNull(classEnclosing);
            Assert.Equal(CodeNodeKinds.Type, classEnclosing.Symbol.Kind);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static (CodeRepositoryId RepositoryId, InMemoryCodeGraphStore Store, CodeIndexingService Indexing)
        CreateService()
    {
        var repositoryId = new CodeRepositoryId("repo:ts-class-lifecycle");
        var store = new InMemoryCodeGraphStore();
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
            store);
        return (repositoryId, store, indexing);
    }
}
