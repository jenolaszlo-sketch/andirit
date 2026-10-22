// REVIEW-ONLY: lifecycle and publication counterexamples for the independent
// EC-3 review (EC3-GLM-Q1). Not part of the implementation's qualification
// suite. These tests must be deleted with the review worktree or preserved
// only as review evidence.

using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>
/// Lifecycle probes exercising real indexing and query operations against the
/// in-memory store and the durable LatticeDb store: rename/removal cleanup,
/// file deletion, failed-run publication isolation, and plugin-version
/// invalidation.
/// </summary>
public class EcmaGlmReviewLifecycleTests : IDisposable
{
    private readonly string root;

    public EcmaGlmReviewLifecycleTests()
    {
        root = Path.Combine(
            Path.GetTempPath(),
            "hetu-ec3-glm-review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private void WriteFile(string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private void DeleteFile(string relativePath) =>
        File.Delete(Path.Combine(root, relativePath));

    private async Task<CodeIndexingResult> IndexAsync(
        ICodeGraphStore store,
        string runId,
        params ICodeGraphPlugin[] plugins)
    {
        var repositories = new CodeRepositoryProviderRegistry(
            [new FileSystemCodeRepositoryProvider()]);
        var registry = new CodeGraphPluginRegistry(plugins);
        var service = new CodeIndexingService(repositories, registry, store);
        return await service.IndexAsync(
            new CodeRepositoryDescriptor(
                new CodeRepositoryId("repo:review"),
                root),
            new CodeIndexRunId(runId));
    }

    private static IReadOnlyList<CodeIndexUnitReplacement> PublishedUnits(
        ICodeGraphStore store) =>
        store.GetPublishedUnitsAsync(new CodeRepositoryId("repo:review"))
            .GetAwaiter().GetResult();

    private static CodeGraphNode? FindNode(
        ICodeGraphStore store,
        string qualifiedName)
    {
        var nodes = store.FindNodesByQualifiedNameAsync(
                new CodeRepositoryId("repo:review"), qualifiedName)
            .GetAwaiter().GetResult();
        return nodes.Count == 0 ? null : nodes[0];
    }

    private static IReadOnlyList<CodeGraphNode> FindAllNodes(
        ICodeGraphStore store,
        string qualifiedName) =>
        store.FindNodesByQualifiedNameAsync(
                new CodeRepositoryId("repo:review"), qualifiedName)
            .GetAwaiter().GetResult();

    [Fact]
    public async Task CE13_InMemory_Rename_And_Member_Removal_Remove_Stale_Facts()
    {
        WriteFile("widget.ts", "export class Widget { render() {} stale = gone(); }\n");
        var store = new InMemoryCodeGraphStore();
        var plugin = new TreeSitterTypeScriptPlugin();

        var first = await IndexAsync(store, "run:1", plugin);
        Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);

        var widget = FindNode(store, "Widget");
        var render = FindNode(store, "Widget.render");
        var stale = FindNode(store, "Widget.stale");
        Assert.NotNull(widget);
        Assert.NotNull(render);
        Assert.NotNull(stale);
        var staleSymbol = stale!.SymbolId!;
        var widgetSymbol = widget!.SymbolId!;
        var staleNodeId = stale.Id;
        Assert.NotEmpty(await store.GetDeclarationsAsync(
            new CodeRepositoryId("repo:review"), staleSymbol));

        // Mutate: rename the class, drop the stale field, keep render.
        WriteFile("widget.ts", "export class Gadget { render() {} }\n");
        var second = await IndexAsync(store, "run:2", plugin);
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);
        Assert.Equal(1, second.Diagnostics.FilesChanged);

        // Stale nodes are gone from the latest publication.
        Assert.Null(FindNode(store, "Widget"));
        Assert.Null(FindNode(store, "Widget.stale"));
        Assert.NotNull(FindNode(store, "Gadget"));
        Assert.NotNull(FindNode(store, "Gadget.render"));

        // Stale symbol identity carries no declarations anymore.
        Assert.Empty(await store.GetDeclarationsAsync(
            new CodeRepositoryId("repo:review"), staleSymbol));
        Assert.Null(await store.GetNodeAsync(new CodeRepositoryId("repo:review"), staleNodeId));

        // No published fact anywhere still references the stale identities.
        var units = PublishedUnits(store);
        Assert.DoesNotContain(units.SelectMany(unit => unit.Nodes),
            node => node.Id == staleNodeId || node.SymbolId == staleSymbol || node.SymbolId == widgetSymbol);
        Assert.DoesNotContain(units.SelectMany(unit => unit.Declarations),
            declaration => declaration.SymbolId == staleSymbol || declaration.SymbolId == widgetSymbol);
        Assert.DoesNotContain(units.SelectMany(unit => unit.Edges),
            edge => edge.SourceId == staleNodeId || edge.TargetId == staleNodeId);

        // The inheritance/invocation edges of the surviving class are intact:
        // Gadget.render still exists with its declaration.
        var gadgetRender = FindNode(store, "Gadget.render")!;
        Assert.NotEmpty(await store.GetDeclarationsAsync(
            new CodeRepositoryId("repo:review"), gadgetRender.SymbolId!));
    }

    [Fact]
    public async Task CE14_InMemory_File_Deletion_Removes_Unit_And_Facts()
    {
        WriteFile("keep.ts", "export class Keeper {}\n");
        WriteFile("drop.ts", "export class Dropper { boom() {} }\n");
        var store = new InMemoryCodeGraphStore();
        var plugin = new TreeSitterTypeScriptPlugin();

        await IndexAsync(store, "run:1", plugin);
        Assert.NotNull(FindNode(store, "Dropper"));
        var dropUnitCount = PublishedUnits(store).Count(
            unit => unit.Origin.SourcePath == "drop.ts");
        Assert.Equal(1, dropUnitCount);

        DeleteFile("drop.ts");
        var second = await IndexAsync(store, "run:2", plugin);
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);
        Assert.Equal(1, second.Diagnostics.FilesDeleted);

        Assert.Null(FindNode(store, "Dropper"));
        Assert.Null(FindNode(store, "Dropper.boom"));
        Assert.NotNull(FindNode(store, "Keeper"));
        Assert.DoesNotContain(PublishedUnits(store),
            unit => unit.Origin.SourcePath == "drop.ts");
    }

    [Fact]
    public async Task CE15_Failed_Run_Does_Not_Corrupt_Published_Graph()
    {
        WriteFile("data.fake", "payload v1\n");
        var store = new InMemoryCodeGraphStore();
        var plugin = new FakePlugin();

        await IndexAsync(store, "run:1", plugin);
        var publishedNode = FindNode(store, "Fake.Symbol");
        Assert.NotNull(publishedNode);

        // Change the source and make extraction fail mid-flight.
        WriteFile("data.fake", "payload v2\n");
        plugin.ThrowOnExtract = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            IndexAsync(store, "run:2", plugin));

        // The failed run must not have replaced the previous publication.
        Assert.Equal("run:1", (await store.GetLatestPublicationAsync(
            new CodeRepositoryId("repo:review")))!.IndexRunId.Value);
        Assert.NotNull(FindNode(store, "Fake.Symbol"));
        var units = PublishedUnits(store);
        var fact = Assert.Single(units.SelectMany(unit => unit.Nodes),
            node => node.Name == "Fake.Symbol");
        Assert.Equal("fake-value-v1",
            Assert.IsType<CodeTextProperty>(fact.Properties["review.value"]).Value);

        // Latest index state still names run:1 sources.
        var state = await store.GetLatestIndexStateAsync(new CodeRepositoryId("repo:review"));
        Assert.Equal("run:1", state!.IndexRunId.Value);

        // A repaired run replaces the facts cleanly: same symbol name, new
        // source content value.
        plugin.ThrowOnExtract = false;
        await IndexAsync(store, "run:3", plugin);
        var repaired = Assert.Single(PublishedUnits(store)
            .SelectMany(unit => unit.Nodes), node => node.Name == "Fake.Symbol");
        Assert.Equal("fake-value-v2",
            Assert.IsType<CodeTextProperty>(repaired.Properties["review.value"]).Value);
    }

    [Fact]
    public async Task CE16_LatticeDb_Rename_And_Delete_Are_Durable_And_Stale_Free()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            "hetu-ec3-glm-review-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            WriteFile("widget.ts", "export class Widget { render() {} stale = gone(); }\n");
            WriteFile("drop.ts", "export class Dropper {}\n");
            using (var store = new Penghou.Hetu.LatticeCodeGraphStore(databasePath))
            {
                var plugin = new TreeSitterTypeScriptPlugin();
                await IndexAsync(store, "run:1", plugin);
                Assert.NotNull(FindNode(store, "Widget"));
                Assert.NotNull(FindNode(store, "Widget.stale"));
                Assert.NotNull(FindNode(store, "Dropper"));

                WriteFile("widget.ts", "export class Gadget { render() {} }\n");
                DeleteFile("drop.ts");
                await IndexAsync(store, "run:2", plugin);

                Assert.Null(FindNode(store, "Widget"));
                Assert.Null(FindNode(store, "Widget.stale"));
                Assert.Null(FindNode(store, "Dropper"));
                Assert.NotNull(FindNode(store, "Gadget"));
                Assert.NotNull(FindNode(store, "Gadget.render"));
                Assert.DoesNotContain(PublishedUnits(store),
                    unit => unit.Origin.SourcePath == "drop.ts");
            }

            // Durability: reopen the store and confirm the replacement stuck.
            using var reopened = new Penghou.Hetu.LatticeCodeGraphStore(databasePath);
            Assert.Null(FindNode(reopened, "Widget"));
            Assert.NotNull(FindNode(reopened, "Gadget"));
        }
        finally
        {
            try { File.Delete(databasePath); } catch (FileNotFoundException) { }
        }
    }

    [Fact]
    public async Task CE17_Plugin_Version_Change_Invalidates_Previous_Facts()
    {
        WriteFile("data.fake", "payload\n");
        var store = new InMemoryCodeGraphStore();
        var plugin = new FakePlugin();

        await IndexAsync(store, "run:1", plugin);
        Assert.NotNull(FindNode(store, "Fake.Symbol"));
        Assert.Empty(FindAllNodes(store, "Fake.Replacement"));

        // Same source content, new plugin version: the planner must mark the
        // unit Changed so the profile-revision invalidation works.
        plugin.VersionValue = "9.9.9+new-profile";
        var second = await IndexAsync(store, "run:2", plugin);
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);
        Assert.True(second.Diagnostics.FilesChanged >= 1, "version change must invalidate");
        Assert.Null(FindNode(store, "Fake.Symbol"));
        Assert.NotNull(FindNode(store, "Fake.Replacement"));
    }

    /// <summary>A controllable fake plugin used to exercise lifecycle failure
    /// and version-invalidation paths without touching production code.</summary>
    private sealed class FakePlugin : ICodeGraphPlugin
    {
        public CodePluginId Id { get; } = new("penghou.hetu.review.fake");
        public string VersionValue { get; set; } = "1.0.0+review";
        public string Version => VersionValue;
        public string Language => "fake";
        public IReadOnlyCollection<string> FileExtensions { get; } = [".fake"];
        public CodeGraphCapabilities Capabilities => CodeGraphCapabilities.Syntax;
        public bool ThrowOnExtract { get; set; }

        public bool CanHandle(string path) =>
            path.EndsWith(".fake", StringComparison.OrdinalIgnoreCase);

        public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
            CodeGraphPluginContext context,
            CancellationToken cancellationToken = default) =>
            new(new FakeSession(context, this));

        private sealed class FakeSession(CodeGraphPluginContext context, FakePlugin owner)
            : ICodeGraphExtractionSession
        {
            public async ValueTask<CodeGraphExtractionResult> ExtractAsync(
                ICodeGraphSink sink,
                CancellationToken cancellationToken = default)
            {
                foreach (var source in context.Sources.OrderBy(s => s.Path, StringComparer.Ordinal))
                {
                    if (owner.ThrowOnExtract)
                        throw new InvalidOperationException("review-simulated plugin crash");

                    var text = await new StreamReader(
                        await source.OpenReadAsync(cancellationToken)).ReadToEndAsync(cancellationToken);
                    var value = text.Contains("v1") ? "fake-value-v1" : "fake-value-v2";
                    var symbolName = owner.Version.StartsWith("1.", StringComparison.Ordinal)
                        ? "Fake.Symbol"
                        : "Fake.Replacement";
                    var unitId = new CodeIndexUnitId($"fake:file:{Hash(source.Path)}");
                    var origin = new CodeFactOrigin(
                        context.RepositoryId,
                        owner.Id,
                        owner.Version,
                        context.IndexRunId,
                        unitId,
                        source.Path,
                        source.ContentHash);
                    var node = new CodeGraphNode(
                        new CodeNodeId($"fake:symbol:{Hash(source.Path + owner.Version)}"),
                        CodeNodeKinds.Type,
                        symbolName,
                        symbolName,
                        new CodeSymbolId($"fake:{Hash(source.Path + owner.Version)}"),
                        new Dictionary<string, CodePropertyValue>
                        {
                            ["review.value"] = new CodeTextProperty(value)
                        });
                    await sink.WriteBatchAsync(
                        new CodeGraphBatch(origin, nodes: [node], completesIndexUnit: true),
                        cancellationToken);
                }

                return new CodeGraphExtractionResult(
                    sourcesExamined: context.Sources.Count,
                    sourcesContributingFacts: context.Sources.Count);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
