// REVIEW-ONLY: publication and lifecycle counterexamples for the independent
// EC-3D remediation review (EC3-GLM-Q2). Not part of the implementation's
// qualification suite. Preserved only as review evidence on the review
// branch; must never be merged into production.

using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>
/// Published-graph probes for EC3-GLM-Q2. Every probe drives the real
/// <see cref="CodeIndexingService"/> over real stores and asserts through the
/// public query APIs only, so storage, ingestion, duplicate handling, and
/// traversal participate in the evidence.
/// </summary>
public class EcmaGlmQ2ReviewPublicationTests : IDisposable
{
    private const string RepositoryId = "repo:ec3-glm-q2";

    private readonly string root;

    public EcmaGlmQ2ReviewPublicationTests()
    {
        root = Path.Combine(
            Path.GetTempPath(),
            "hetu-ec3-glm-q2-" + Guid.NewGuid().ToString("N"));
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

    private static string Sha(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string CandidateId(string language, string path, string writtenName) =>
        $"{language}:heritage-target:{Sha($"{path}\n{writtenName}")}";

    private void WriteFile(string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static CodeIndexingService CreateService(
        ICodeGraphStore store,
        params ICodeGraphPlugin[] plugins) =>
        new(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry(plugins),
            store);

    private CodeRepositoryDescriptor Descriptor() =>
        new(new CodeRepositoryId(RepositoryId), root);

    private static CodeGraphQueryService Queries(ICodeGraphStore store) =>
        new(store);

    private static IReadOnlyList<CodeIndexUnitReplacement> PublishedUnits(ICodeGraphStore store) =>
        store.GetPublishedUnitsAsync(new CodeRepositoryId(RepositoryId))
            .GetAwaiter().GetResult();

    private static string[] PublishedFactIdentities(ICodeGraphStore store) =>
        PublishedUnits(store)
            .SelectMany(unit =>
                unit.Nodes.Select(node => node.Id.Value)
                    .Concat(unit.Edges.Select(edge => edge.Id.Value))
                    .Concat(unit.Declarations.Select(declaration => declaration.Id.Value)))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    // Q2-P1: the original F01 counterexample verified against the published
    // graph with public query APIs, including impact-set semantics in both
    // directions.
    [Fact]
    public async Task Q2P1_PublishedGraph_ShadowedHeritage_ImpactQueriesFollowCandidate()
    {
        WriteFile("src/shadow.ts", """
            class Base {}

            function create() {
                class Base {}
                class Derived extends Base {}
            }
            """);
        var store = new InMemoryCodeGraphStore();
        var indexing = CreateService(store, new TreeSitterTypeScriptPlugin());
        var queries = Queries(store);

        var indexed = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:1"));
        Assert.Equal(CodeIndexRunStatus.Completed, indexed.Diagnostics.Status);

        // 1. create.Derived keeps its declared identity.
        var derived = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "create.Derived")).Candidates);
        Assert.NotNull(derived.SymbolId);

        var topLevelBase = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "Base")).Candidates,
            node => node.SymbolId is not null);
        var nestedBase = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "create.Base")).Candidates);

        // 2-6. The heritage candidate is a distinct, declaration-less node in
        // the dedicated namespace; the inherits edge points to it and stays
        // Structural.
        const string path = "src/shadow.ts";
        var candidateId = new CodeNodeId(CandidateId("typescript", path, "Base"));
        var candidate = await store.GetNodeAsync(new(RepositoryId), candidateId);
        Assert.NotNull(candidate);
        Assert.Null(candidate!.SymbolId);
        Assert.Equal("Base", candidate.QualifiedName);
        Assert.NotEqual(topLevelBase.Id, candidateId);
        Assert.NotEqual(nestedBase.Id, candidateId);
        Assert.DoesNotContain(
            PublishedUnits(store).SelectMany(unit => unit.Declarations),
            declaration => declaration.SymbolNodeId == candidateId);

        var neighborhood = await queries.GetNeighborhoodAsync(
            new(RepositoryId),
            derived.Id,
            [CodeEdgeKinds.Inherits],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 20, maxEdges: 20));
        var edge = Assert.Single(neighborhood.Edges);
        Assert.Equal(derived.Id, edge.SourceId);
        Assert.Equal(candidateId, edge.TargetId);
        Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
        Assert.Equal(path, edge.Evidence.Location?.Path);

        // 7. Impact from the unrelated top-level Base excludes create.Derived.
        var topLevelImpact = await queries.GetImpactSetAsync(new(RepositoryId), topLevelBase.Id);
        Assert.DoesNotContain(topLevelImpact.Nodes, node => node.Id == derived.Id);
        Assert.DoesNotContain(topLevelImpact.Nodes, node => node.Id == nestedBase.Id);
        Assert.DoesNotContain(topLevelImpact.Nodes, node => node.Id == candidateId);

        // 8. Impact from the candidate follows the documented traversal
        // semantics: incoming inherits reaches create.Derived; the inner
        // declaration still contributes nothing.
        var candidateImpact = await queries.GetImpactSetAsync(new(RepositoryId), candidateId);
        Assert.Contains(candidateImpact.Nodes, node => node.Id == derived.Id);
        var nestedImpact = await queries.GetImpactSetAsync(new(RepositoryId), nestedBase.Id);
        Assert.DoesNotContain(nestedImpact.Nodes, node => node.Id == derived.Id);

        // Consumer-observation record: exact-name lookup for "Base" now
        // returns the declared symbol AND the declaration-less candidate, and
        // the candidate sorts first because "heritage-target" < "symbol"
        // ordinally. The lookup contract preserves ambiguity; consumers must
        // filter by SymbolId.
        var lookup = await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "Base");
        Assert.Equal(2, lookup.Candidates.Count);
        Assert.True(lookup.IsAmbiguous);
        Assert.Null(lookup.Candidates[0].SymbolId);
        Assert.Equal(candidateId, lookup.Candidates[0].Id);
        Assert.NotNull(lookup.Candidates[1].SymbolId);
    }

    // Q2-P2 (spec Test B, incremental): candidate sharing survives an
    // incremental edit; removing one occurrence keeps the other's
    // relationship, and only the final removal retires the candidate.
    [Fact]
    public async Task Q2P2_CandidateSharing_SurvivesIncrementalOccurrenceRemoval()
    {
        WriteFile("src/shared.ts", """
            class Alpha extends Base {}

            function create() {
                class Base {}
                class Beta extends Base {}
            }
            """);
        var store = new InMemoryCodeGraphStore();
        var indexing = CreateService(store, new TreeSitterTypeScriptPlugin());
        var queries = Queries(store);

        var first = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:1"));
        Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
        var candidateId = new CodeNodeId(CandidateId("typescript", "src/shared.ts", "Base"));
        Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), candidateId));
        var alpha = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "Alpha")).Candidates);
        var beta = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "create.Beta")).Candidates);
        Assert.Equal(
            2,
            (await queries.GetNeighborhoodAsync(
                new(RepositoryId),
                candidateId,
                [CodeEdgeKinds.Inherits],
                new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 20, maxEdges: 20))).Edges.Count);
        var sharedEdges = (await queries.GetNeighborhoodAsync(
            new(RepositoryId),
            candidateId,
            [CodeEdgeKinds.Inherits],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 20, maxEdges: 20))).Edges;
        Assert.Equal(2, sharedEdges.Count);
        Assert.Contains(sharedEdges, edge => edge.SourceId == alpha.Id);
        Assert.Contains(sharedEdges, edge => edge.SourceId == beta.Id);

        // Remove Beta's occurrence; Alpha's relationship must survive intact.
        WriteFile("src/shared.ts", """
            class Alpha extends Base {}

            function create() {
                class Base {}
            }
            """);
        var second = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:2"));
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

        Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), candidateId));
        Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "create.Beta")).Candidates);
        var alphaEdge = Assert.Single((await queries.GetNeighborhoodAsync(
            new(RepositoryId),
            alpha.Id,
            [CodeEdgeKinds.Inherits],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 20, maxEdges: 20))).Edges);
        Assert.Equal(candidateId, alphaEdge.TargetId);
        Assert.Equal(EvidenceQuality.Structural, alphaEdge.Evidence.Quality);

        // Remove the last occurrence; the candidate retires.
        WriteFile("src/shared.ts", """
            class Alpha {}

            function create() {
                class Base {}
            }
            """);
        var third = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:3"));
        Assert.Equal(CodeIndexRunStatus.Completed, third.Diagnostics.Status);
        Assert.Null(await store.GetNodeAsync(new(RepositoryId), candidateId));
        Assert.DoesNotContain(
            PublishedUnits(store).SelectMany(unit => unit.Edges),
            edge => edge.Kind == CodeEdgeKinds.Inherits && edge.TargetId == candidateId);
    }

    // Q2-P3: renaming the written heritage target replaces the candidate
    // relationship and retires the obsolete candidate and edge.
    [Fact]
    public async Task Q2P3_RenamingWrittenTarget_ReplacesCandidateRelationship()
    {
        WriteFile("src/child.ts", "class Child extends OldBase {}\n");
        var store = new InMemoryCodeGraphStore();
        var indexing = CreateService(store, new TreeSitterTypeScriptPlugin());
        var queries = Queries(store);

        await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:1"));
        var oldCandidateId = new CodeNodeId(CandidateId("typescript", "src/child.ts", "OldBase"));
        var child = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "Child")).Candidates);
        var oldEdge = Assert.Single((await queries.GetNeighborhoodAsync(
            new(RepositoryId),
            child.Id,
            [CodeEdgeKinds.Inherits],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 20, maxEdges: 20))).Edges);
        Assert.Equal(oldCandidateId, oldEdge.TargetId);

        WriteFile("src/child.ts", "class Child extends NewBase {}\n");
        var second = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:2"));
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

        var newCandidateId = new CodeNodeId(CandidateId("typescript", "src/child.ts", "NewBase"));
        Assert.Null(await store.GetNodeAsync(new(RepositoryId), oldCandidateId));
        Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), newCandidateId));
        Assert.Empty((await queries.FindNodesByNamePatternAsync(new(RepositoryId), "OldBase")).Candidates);
        var newEdge = Assert.Single((await queries.GetNeighborhoodAsync(
            new(RepositoryId),
            child.Id,
            [CodeEdgeKinds.Inherits],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 20, maxEdges: 20))).Edges);
        Assert.Equal(newCandidateId, newEdge.TargetId);
        Assert.DoesNotContain(
            PublishedUnits(store).SelectMany(unit => unit.Edges),
            edge => edge.TargetId == oldCandidateId);
    }

    // Q2-P4: deleting a source file removes the heritage facts it owns while
    // same-named declared symbols elsewhere survive.
    [Fact]
    public async Task Q2P4_FileDeletion_RemovesOwnedHeritageFacts()
    {
        WriteFile("src/child.ts", "class Child extends Base {}\n");
        WriteFile("src/base.ts", "class Base {}\n");
        var store = new InMemoryCodeGraphStore();
        var indexing = CreateService(store, new TreeSitterTypeScriptPlugin());
        var queries = Queries(store);

        await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:1"));
        var candidateId = new CodeNodeId(CandidateId("typescript", "src/child.ts", "Base"));
        Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), candidateId));
        Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "Base")).Candidates,
            node => node.SymbolId is not null);

        File.Delete(Path.Combine(root, "src", "child.ts"));
        var second = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:2"));
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

        Assert.Null(await store.GetNodeAsync(new(RepositoryId), candidateId));
        Assert.DoesNotContain(
            PublishedUnits(store).SelectMany(unit => unit.Edges),
            edge => edge.TargetId == candidateId);
        Assert.DoesNotContain(
            PublishedUnits(store),
            unit => unit.Origin.SourcePath == "src/child.ts");

        // The declared class in the surviving file keeps its identity and
        // gains no dependency on the removed candidate.
        Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "Base")).Candidates,
            node => node.SymbolId is not null);
    }

    // Q2-P5: reindexing unchanged heritage sources is deterministic; the
    // planner skips them and the published facts keep identical identities.
    [Fact]
    public async Task Q2P5_UnchangedReindex_PreservesHeritagePublicationDeterministically()
    {
        WriteFile("src/child.ts", "class Child extends Base implements Contract {}\n");
        WriteFile("src/contracts.ts", "interface Contract {}\n");
        var store = new InMemoryCodeGraphStore();
        var indexing = CreateService(store, new TreeSitterTypeScriptPlugin());
        var queries = Queries(store);

        await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:1"));
        var before = PublishedFactIdentities(store);

        var second = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:2"));
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);
        Assert.Equal(0, second.Diagnostics.FilesChanged);

        Assert.Equal(before, PublishedFactIdentities(store));

        var candidateId = new CodeNodeId(CandidateId("typescript", "src/child.ts", "Base"));
        Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), candidateId));
    }

    // Q2-P6: a failed indexing run leaves the previous heritage publication
    // authoritative and queryable.
    [Fact]
    public async Task Q2P6_FailedRun_PreservesPreviousHeritagePublication()
    {
        WriteFile("src/child.ts", "class Child extends Base {}\n");
        WriteFile("src/data.fake", "payload v1\n");
        var store = new InMemoryCodeGraphStore();
        var fake = new FakeHeritagePlugin();
        var indexing = CreateService(store, new TreeSitterTypeScriptPlugin(), fake);
        var queries = Queries(store);

        var first = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:1"));
        Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
        var candidateId = new CodeNodeId(CandidateId("typescript", "src/child.ts", "Base"));
        var derived = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "Child")).Candidates);
        Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), candidateId));

        WriteFile("src/data.fake", "payload v2\n");
        fake.ThrowOnExtract = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:2")).AsTask());

        // The prior publication is untouched and fully queryable.
        Assert.Equal(
            "run:1",
            (await store.GetLatestPublicationAsync(new(RepositoryId)))!.IndexRunId.Value);
        Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), candidateId));
        var impact = await queries.GetImpactSetAsync(new(RepositoryId), candidateId);
        Assert.Contains(impact.Nodes, node => node.Id == derived.Id);
        var state = await store.GetLatestIndexStateAsync(new(RepositoryId));
        Assert.Equal("run:1", state!.IndexRunId.Value);
    }

    // Q2-P7: the profile-revision transition (p4 -> p5) invalidates prior
    // plugin-version manifests, removes old converged heritage placeholders
    // and their edges, and publishes the new declaration-less candidates
    // while declared identities stay stable.
    [Fact]
    public async Task Q2P7_ProfileRevisionTransition_p4_to_p5_ReplacesHeritageFacts()
    {
        Assert.EndsWith(
            ".p5",
            new TreeSitterTypeScriptPlugin().Version,
            StringComparison.Ordinal);
        Assert.EndsWith(
            ".p5",
            new TreeSitterJavaScriptPlugin().Version,
            StringComparison.Ordinal);

        WriteFile("src/legacy.fake", "class Child extends Base {}\n");
        var store = new InMemoryCodeGraphStore();
        var fake = new FakeHeritagePlugin();
        var indexing = CreateService(store, fake);
        var queries = Queries(store);

        // p4-shaped publication: converged symbol-namespace placeholder.
        fake.VersionValue = "9.9.9+profile.p4";
        var first = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:1"));
        Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
        var declaredClass = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "FakeChild")).Candidates);
        var placeholderId = new CodeNodeId(
            $"fake:symbol:{Sha($"class\nsrc/legacy.fake\nBase")}");
        Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), placeholderId));
        var oldEdge = Assert.Single((await queries.GetNeighborhoodAsync(
            new(RepositoryId),
            declaredClass.Id,
            [CodeEdgeKinds.Inherits],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 20, maxEdges: 20))).Edges);
        Assert.Equal(placeholderId, oldEdge.TargetId);

        // p5 publication: declared identity unchanged; placeholder retired;
        // candidate + replacement edge published.
        fake.VersionValue = "9.9.9+profile.p5";
        var second = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:2"));
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);
        Assert.True(second.Diagnostics.FilesChanged >= 1, "version change must invalidate");

        Assert.Null(await store.GetNodeAsync(new(RepositoryId), placeholderId));
        Assert.DoesNotContain(
            PublishedUnits(store).SelectMany(unit => unit.Edges),
            edge => edge.TargetId == placeholderId);

        var candidateId = new CodeNodeId(CandidateId("fake", "src/legacy.fake", "Base"));
        var candidate = await store.GetNodeAsync(new(RepositoryId), candidateId);
        Assert.NotNull(candidate);
        Assert.Null(candidate!.SymbolId);
        Assert.Equal("Base", candidate.QualifiedName);

        var declaredAgain = Assert.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "FakeChild")).Candidates);
        Assert.Equal(declaredClass.Id, declaredAgain.Id);
        Assert.Equal(declaredClass.SymbolId, declaredAgain.SymbolId);
        var newEdge = Assert.Single((await queries.GetNeighborhoodAsync(
            new(RepositoryId),
            declaredAgain.Id,
            [CodeEdgeKinds.Inherits],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 20, maxEdges: 20))).Edges);
        Assert.Equal(candidateId, newEdge.TargetId);
    }

    // Q2-P8: the durable LatticeDb store removes stale heritage facts across
    // rename and file deletion, keeps a candidate owned by a surviving file,
    // and preserves the replacements across dispose/reopen.
    [Fact]
    public async Task Q2P8_LatticeDb_HeritageLifecycle_IsDurable()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            "hetu-ec3-glm-q2-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            WriteFile("src/child.ts", "class Child extends OldBase {}\n");
            WriteFile("src/base.ts", "class Base {}\n");
            using (var store = new Penghou.Hetu.LatticeCodeGraphStore(databasePath))
            {
                var indexing = CreateService(store, new TreeSitterTypeScriptPlugin());
                var queries = Queries(store);

                await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:1"));
                var oldCandidateId = new CodeNodeId(CandidateId("typescript", "src/child.ts", "OldBase"));
                Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), oldCandidateId));

                WriteFile("src/child.ts", "class Child extends NewBase {}\n");
                File.Delete(Path.Combine(root, "src", "base.ts"));
                var second = await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:2"));
                Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

                // The renamed relationship is replaced; the declared Base in
                // the deleted file is gone, but the child-owned candidate for
                // NewBase survives because its owning unit survives.
                var newCandidateId = new CodeNodeId(CandidateId("typescript", "src/child.ts", "NewBase"));
                Assert.Null(await store.GetNodeAsync(new(RepositoryId), oldCandidateId));
                Assert.NotNull(await store.GetNodeAsync(new(RepositoryId), newCandidateId));
                Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(new(RepositoryId), "Base")).Candidates);

                // Deleting the owning file retires the candidate durably.
                File.Delete(Path.Combine(root, "src", "child.ts"));
                await indexing.IndexAsync(Descriptor(), new CodeIndexRunId("run:3"));
                Assert.Null(await store.GetNodeAsync(new(RepositoryId), newCandidateId));
                Assert.DoesNotContain(
                    PublishedUnits(store),
                    unit => unit.Origin.SourcePath is "src/child.ts" or "src/base.ts");
            }

            using var reopened = new Penghou.Hetu.LatticeCodeGraphStore(databasePath);
            Assert.Null(await reopened.GetNodeAsync(
                new(RepositoryId),
                new CodeNodeId(CandidateId("typescript", "src/child.ts", "NewBase"))));
        }
        finally
        {
            try
            {
                File.Delete(databasePath);
            }
            catch (FileNotFoundException)
            {
            }
        }
    }

    /// <summary>
    /// A controllable fake plugin used to exercise profile-revision
    /// invalidation and failure isolation without touching production code.
    /// Its p4 version emits the pre-EC-3D converged placeholder shape
    /// (<c>{lang}:symbol:sha256("class\npath\nname")</c>); its p5 version
    /// emits the EC-3D declaration-less candidate shape.
    /// </summary>
    private sealed class FakeHeritagePlugin : ICodeGraphPlugin
    {
        public CodePluginId Id { get; } = new("penghou.hetu.review.ec3d");
        public string VersionValue { get; set; } = "9.9.9+profile.p4";
        public string Version => VersionValue;
        public string Language => "fake";
        public IReadOnlyCollection<string> FileExtensions { get; } = [".fake"];
        public CodeGraphCapabilities Capabilities =>
            CodeGraphCapabilities.Syntax | CodeGraphCapabilities.Inheritance;
        public bool ThrowOnExtract { get; set; }

        public bool CanHandle(string path) =>
            path.EndsWith(".fake", StringComparison.OrdinalIgnoreCase);

        public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
            CodeGraphPluginContext context,
            CancellationToken cancellationToken = default) =>
            new(new FakeSession(context, this));

        private sealed class FakeSession(CodeGraphPluginContext context, FakeHeritagePlugin owner)
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
                    var unitId = new CodeIndexUnitId($"fake:file:{Sha(source.Path)}");
                    var origin = new CodeFactOrigin(
                        context.RepositoryId,
                        owner.Id,
                        owner.Version,
                        context.IndexRunId,
                        unitId,
                        source.Path,
                        source.ContentHash);

                    var classHash = Sha($"class\n{source.Path}\nFakeChild");
                    var classNodeId = new CodeNodeId($"fake:symbol:{classHash}");
                    var classSymbol = new CodeSymbolId($"fake:{classHash}");
                    var declared = new CodeGraphNode(
                        classNodeId,
                        CodeNodeKinds.Type,
                        "FakeChild",
                        "FakeChild",
                        classSymbol);

                    CodeGraphNode target;
                    if (owner.Version.EndsWith(".p4", StringComparison.Ordinal))
                    {
                        // Pre-EC-3D shape: a converged symbol-namespace
                        // placeholder without symbol identity.
                        target = new CodeGraphNode(
                            new CodeNodeId($"fake:symbol:{Sha($"class\n{source.Path}\nBase")}"),
                            CodeNodeKinds.Type,
                            "Base",
                            "Base");
                    }
                    else
                    {
                        // EC-3D shape: declaration-less candidate namespace.
                        target = new CodeGraphNode(
                            new CodeNodeId(CandidateId("fake", source.Path, "Base")),
                            CodeNodeKinds.Type,
                            "Base",
                            "Base");
                    }

                    var edge = new CodeGraphEdge(
                        new CodeEdgeId($"fake:{Sha($"inherits\n{classNodeId.Value}\n{target.Id.Value}\nrun:{owner.Version}")}"),
                        classNodeId,
                        target.Id,
                        CodeEdgeKinds.Inherits,
                        new CodeEvidence(
                            EvidenceQuality.Structural,
                            owner.Id.Value,
                            owner.Version,
                            new CodeLocation(source.Path, 1, 1, 1, 30)));

                    await sink.WriteBatchAsync(
                        new CodeGraphBatch(
                            origin,
                            nodes: [declared, target],
                            edges: [edge],
                            completesIndexUnit: true),
                        cancellationToken);
                }

                return new CodeGraphExtractionResult(
                    sourcesExamined: context.Sources.Count,
                    sourcesContributingFacts: context.Sources.Count);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
