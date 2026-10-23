// EC-4D: incremental publication lifecycle on a disposable copy of the target.
using System.Text.Json;
using Penghou.Hetu;
using Penghou.Hetu.TreeSitter;

/// <summary>Incremental indexing scenarios: add, rename, remove, import change, delete file.</summary>
internal static class Ec4Lifecycle
{
    public static async Task<int> RunAsync(string repoPath, string outDir)
    {
        if (!Directory.Exists(repoPath))
        {
            Console.Error.WriteLine($"repository not found: {repoPath}");
            return 1;
        }

        var disposable = Path.Combine(
            Path.GetTempPath(),
            "ec4-lifecycle-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(repoPath, disposable);
        Console.WriteLine($"disposable copy: {disposable}");

        var evidence = new Dictionary<string, object>(StringComparer.Ordinal);
        try
        {
            await using var host = new HetuHostBuilder()
                .AddTreeSitterTypeScriptPlugin()
                .AddTreeSitterJavaScriptPlugin()
                .Build();
            var repositoryId = new CodeRepositoryId("repo:website-ec4-lifecycle");
            var descriptor = new CodeRepositoryDescriptor(repositoryId, disposable);
            var queries = host.Queries;

            async Task<StepResult> IndexAsync(string runId)
            {
                var started = System.Diagnostics.Stopwatch.StartNew();
                CodeIndexingResult? indexed = null;
                Exception? failure = null;
                try
                {
                    indexed = await host.IndexRepositoryAsync(
                        descriptor,
                        new CodeIndexRunId(runId),
                        diagnostics: d => Console.WriteLine(
                            $"  [{runId}] status={d.Status} new={d.FilesNew} changed={d.FilesChanged} " +
                            $"unchanged={d.FilesUnchanged} deleted={d.FilesDeleted} " +
                            $"nodes={d.NodesProduced} decls={d.DeclarationsProduced} edges={d.EdgesProduced}"));
                }
                catch (Exception exception)
                {
                    failure = exception;
                    Console.WriteLine($"  [{runId}] THREW {exception.GetType().Name}: {exception.Message}");
                }

                started.Stop();
                return new StepResult(indexed, failure, started.Elapsed);
            }

            var formatPath = Path.Combine(disposable, "src", "lib", "format.ts");
            var sitePath = Path.Combine(disposable, "src", "lib", "site.ts");
            var envPath = Path.Combine(disposable, "src", "env.d.ts");

            // 1. Initial indexing and publication.
            var initial = await IndexAsync("run:ec4-life-1-initial");
            var initialWithBase = await SingleWithBaseAsync(queries, repositoryId);
            evidence["step1-initial"] = Step(initial, initialWithBase);

            // 2. Reindex unchanged sources.
            var reindex = await IndexAsync("run:ec4-life-2-unchanged");
            evidence["step2-reindex-unchanged"] = Step(reindex, initialWithBase);

            // 3. Add a supported declaration.
            await File.WriteAllTextAsync(
                formatPath,
                await File.ReadAllTextAsync(formatPath) +
                "\nexport function ec4Probe(): number {\n  return 42;\n}\n");
            var added = await IndexAsync("run:ec4-life-3-add");
            var afterAdd = await ProbeStateAsync(queries, repositoryId, "ec4Probe");
            evidence["step3-add-declaration"] = Step(added, afterAdd);

            // 4. Rename the declaration.
            var content = await File.ReadAllTextAsync(formatPath);
            await File.WriteAllTextAsync(
                formatPath, content.Replace("ec4Probe", "ec4ProbeRenamed"));
            var renamed = await IndexAsync("run:ec4-life-4-rename");
            var afterRename = await ProbeStateAsync(queries, repositoryId, "ec4ProbeRenamed");
            var oldGone = await ProbeStateAsync(queries, repositoryId, "ec4Probe");
            evidence["step4-rename-declaration"] = Step(renamed, afterRename, oldProbe: oldGone);

            // 5. Remove the declaration again.
            content = await File.ReadAllTextAsync(formatPath);
            var probeStart = content.IndexOf("export function ec4ProbeRenamed", StringComparison.Ordinal);
            content = content[..probeStart].TrimEnd() + "\n";
            await File.WriteAllTextAsync(formatPath, content);
            var removed = await IndexAsync("run:ec4-life-5-remove");
            var afterRemove = await ProbeStateAsync(queries, repositoryId, "ec4ProbeRenamed");
            evidence["step5-remove-declaration"] = Step(removed, afterRemove);

            // 6. Change an import: site.ts gains a named import from ../lib/format.
            var siteContent = await File.ReadAllTextAsync(sitePath);
            await File.WriteAllTextAsync(
                sitePath,
                "import { formatDate } from '../lib/format';\n" + siteContent);
            var importChanged = await IndexAsync("run:ec4-life-6-import");
            var siteImports = await ImportsOfAsync(queries, repositoryId, "src/lib/site.ts");
            evidence["step6-change-import"] = Step(importChanged, siteImports);

            // 7. Remove a source file.
            File.Delete(envPath);
            var fileDeleted = await IndexAsync("run:ec4-life-7-delete-file");
            var envLookup = await queries.FindSymbolsByQualifiedNameAsync(
                repositoryId, "src/env.d.ts");
            var envFileGone = envLookup.Candidates.Count == 0;
            evidence["step7-delete-source-file"] = Step(fileDeleted, new
            {
                fileNodeStillPublished = !envFileGone
            });

            // 8. Failed indexing attempt must leave the prior publication intact.
            var badDescriptor = new CodeRepositoryDescriptor(
                repositoryId,
                Path.Combine(disposable, "does-not-exist"));
            Exception? failure = null;
            try
            {
                await host.IndexRepositoryAsync(badDescriptor, new CodeIndexRunId("run:ec4-life-8-failed"));
            }
            catch (Exception exception)
            {
                failure = exception;
                Console.WriteLine($"  expected failure: {exception.GetType().Name}");
            }

            var survived = await SingleWithBaseAsync(queries, repositoryId);
            evidence["step8-failed-attempt"] = new
            {
                threw = failure?.GetType().Name,
                message = failure?.Message,
                priorPublicationStillQueryable = survived is not null,
                withBaseStillPublished = survived is not null ? survived.Id.Value : null
            };
        }
        finally
        {
            try
            {
                Directory.Delete(disposable, recursive: true);
            }
            catch
            {
                // best effort cleanup of the disposable copy
            }
        }

        Json.Write(Path.Combine(outDir, "lifecycle.json"), evidence);
        return 0;
    }

    private static async Task<CodeGraphNode?> SingleWithBaseAsync(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId)
    {
        var lookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "withBase");
        var node = Ec4Support.Single(lookup.Candidates);
        return node;
    }

    private static async Task<object> ProbeStateAsync(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        string name)
    {
        var lookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, name);
        var node = Ec4Support.Single(lookup.Candidates);
        if (node is null)
            return new { published = false };
        var declarations = await queries.FindDeclarationsAsync(repositoryId, node.SymbolId!);
        return new
        {
            published = true,
            nodeId = node.Id.Value,
            declarations = declarations.Select(declaration =>
                $"{declaration.Location.Path}:{declaration.Location.StartLine}.{declaration.Location.StartColumn}")
        };
    }

    private static async Task<object> ImportsOfAsync(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        string path)
    {
        var lookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, path);
        var fileNode = lookup.Candidates.FirstOrDefault(node => node.Kind == CodeNodeKinds.File);
        if (fileNode is null)
            return new { error = "file node not found" };
        var neighborhood = await queries.GetNeighborhoodAsync(
            repositoryId,
            fileNode.Id,
            [CodeEdgeKinds.Imports],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 200));
        return new { importTargets = neighborhood.Nodes.Where(node => node.Id != fileNode.Id).Select(node => node.QualifiedName ?? node.Name) };
    }

    private static object Step(StepResult step, object? observation, object? oldProbe = null) => new
    {
        status = step.Indexed?.Diagnostics.Status.ToString(),
        failure = step.Failure?.GetType().Name,
        filesNew = step.Indexed?.Diagnostics.FilesNew,
        filesChanged = step.Indexed?.Diagnostics.FilesChanged,
        filesUnchanged = step.Indexed?.Diagnostics.FilesUnchanged,
        filesDeleted = step.Indexed?.Diagnostics.FilesDeleted,
        nodesProduced = step.Indexed?.Diagnostics.NodesProduced,
        declarationsProduced = step.Indexed?.Diagnostics.DeclarationsProduced,
        edgesProduced = step.Indexed?.Diagnostics.EdgesProduced,
        durationMs = step.Elapsed.TotalMilliseconds,
        publicationRunId = step.Indexed?.Publication.IndexRunId.Value,
        observation,
        oldProbe = oldProbe ?? null
    };

    private readonly record struct StepResult(
        CodeIndexingResult? Indexed,
        Exception? Failure,
        TimeSpan Elapsed);

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(directory.Replace(source, target, StringComparison.Ordinal));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, target, StringComparison.Ordinal));
    }
}



