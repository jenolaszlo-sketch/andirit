using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// S64-MIRROR-REMED-01 regression campaign. Every case reads the real native
/// inventory (nodes, edges and manifests) from the database after all store
/// handles are closed, and compares it with an expected set derived from the
/// fixture inputs rather than from the store's projection.
/// </summary>
public sealed class NativeGraphMirrorRemediationTests
{
    private static readonly DateTimeOffset Started =
        new(2026, 11, 27, 0, 0, 0, TimeSpan.Zero);

    private static readonly CodePluginId Plugin = new("plugin:s64-remediation");

    // ---------------------------------------------------------------- F-S64E-01

    [Fact]
    public async Task SharedLogicalNodeIdentity_AcrossContributors_HasOnePhysicalNode()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:shared-node");
        var first = Node("node:first");
        var second = Node("node:second");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [first, second]),
                    Unit(repository, "run:one", "unit:b", [first, second]));
                Assert.NotNull(await store.GetNodeAsync(repository, second.Id));
            }

            var inventory = ReadInventory(path);
            AssertMirrorIsCanonical(
                inventory,
                repository,
                ["node:first", "node:second"],
                []);
            Assert.Single(inventory.LogicalNodeIds, id => id == "node:first");
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task SharedLogicalEdgeIdentity_AcrossContributors_HasOnePhysicalEdge()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:shared-edge");
        var source = Node("node:source");
        var target = Node("node:target");
        var edge = Edge("edge:calls", source, target);
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [source, target], [edge]),
                    Unit(repository, "run:one", "unit:b", [source, target], [edge]));
            }

            var inventory = ReadInventory(path);
            AssertMirrorIsCanonical(
                inventory,
                repository,
                ["node:source", "node:target"],
                ["edge:calls"]);
            Assert.Single(inventory.Edges);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task ConflictingContributors_AreRejected_AndNeverReachTheMirror()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:conflict");
        var accepted = Node("node:accepted");
        var conflicting = new CodeGraphNode(
            accepted.Id,
            CodeNodeKinds.Type,
            "DifferentName",
            "Example.DifferentName",
            accepted.SymbolId);
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:accepted", [accepted]));

                var rejected = new CodeIndexRunId("run:conflict");
                await store.StoreIndexRunAsync(
                    new(repository, rejected, Started.AddMinutes(1), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repository, "run:conflict", "unit:a", [accepted]));
                await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                    await store.StageIndexUnitAsync(
                        Unit(repository, "run:conflict", "unit:b", [conflicting])));
                Assert.Equal("Example.accepted",
                    (await store.GetNodeAsync(repository, accepted.Id))!.QualifiedName);
            }

            using var reopened = Open(path);
            Assert.Equal("Example.accepted",
                (await reopened.GetNodeAsync(repository, accepted.Id))!.QualifiedName);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------- lifecycle / cardinality

    [Fact]
    public async Task RepeatedEnabledReopen_KeepsPhysicalCardinalityStable()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:reopen-stable");
        var a = Node("node:a");
        var b = Node("node:b");
        var c = Node("node:c");
        var edges = new[]
        {
            Edge("edge:ab", a, b),
            Edge("edge:bc", b, c)
        };
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [a, b], [edges[0]]),
                    Unit(repository, "run:one", "unit:b", [b, c], [edges[1]]));
            }

            for (var iteration = 0; iteration < 4; iteration++)
            {
                using (var store = Open(path))
                {
                    await Traverse(store, repository, a.Id);
                }

                var inventory = ReadInventory(path);
                AssertMirrorIsCanonical(
                    inventory,
                    repository,
                    ["node:a", "node:b", "node:c"],
                    ["edge:ab", "edge:bc"]);
                Assert.Equal(3, inventory.LogicalNodeIds.Count);
                Assert.Equal(2, inventory.Edges.Count);
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task SourceUnitReplacementAndDeletion_RemoveOldPhysicalFacts()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:replacement");
        var a = Node("node:a");
        var b = Node("node:b");
        var c = Node("node:c");
        var replacement = Node("node:a", "Replacement");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [a, b], [Edge("edge:ab", a, b)]),
                    Unit(repository, "run:one", "unit:b", [c]));

                var run = new CodeIndexRunId("run:two");
                await store.StoreIndexRunAsync(
                    new(repository, run, Started.AddMinutes(3), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repository, "run:two", "unit:a", [replacement, b]));
                await store.StageIndexUnitDeletionAsync(
                    repository, run, Plugin, new CodeIndexUnitId("unit:b"));
                await CompleteAsync(store, repository, "run:two", Started.AddMinutes(3));
            }

            var inventory = ReadInventory(path);
            AssertMirrorIsCanonical(inventory, repository, ["node:a", "node:b"], []);
            Assert.DoesNotContain("node:c", inventory.LogicalNodeIds);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task DisabledPublication_RemovesPhysicalMirrorObjects_AndReEnableRebuilds()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:transition");
        var a = Node("node:a");
        var b = Node("node:b");
        var c = Node("node:c");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [a, b], [Edge("edge:ab", a, b)]));
            }

            for (var cycle = 0; cycle < 2; cycle++)
            {
                using (var disabled = new LatticeCodeGraphStore(path))
                {
                    await disabled.StoreIndexRunAsync(
                        new(repository, new CodeIndexRunId($"run:disabled:{cycle}"),
                            Started.AddMinutes(10 + cycle), plugins: [Plugin]));
                    await disabled.StageIndexUnitAsync(
                        Unit(repository, $"run:disabled:{cycle}", "unit:a", [a, b],
                            [Edge("edge:ab", a, b)]));
                    await CompleteAsync(
                        disabled, repository, $"run:disabled:{cycle}",
                        Started.AddMinutes(10 + cycle));
                }

                var whileDisabled = ReadInventory(path);
                Assert.Empty(whileDisabled.LogicalNodeIds);
                Assert.Empty(whileDisabled.Edges);
                Assert.Empty(whileDisabled.Manifests);
                Assert.Empty(NativeNodes(path));

                using (var reenabled = Open(path))
                {
                    await Traverse(reenabled, repository, a.Id);
                }

                var afterReenable = ReadInventory(path);
                AssertMirrorIsCanonical(
                    afterReenable,
                    repository,
                    ["node:a", "node:b"],
                    ["edge:ab"]);
            }

            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:final",
                    Unit(repository, "run:final", "unit:a", [a, c], [Edge("edge:ac", a, c)]));
            }

            var final = ReadInventory(path);
            AssertMirrorIsCanonical(final, repository, ["node:a", "node:c"], ["edge:ac"]);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task RepeatedDisabledPublications_LeaveNoMirrorState()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:disabled-repeat");
        var node = Node("node:only");
        try
        {
            for (var run = 0; run < 3; run++)
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    await store.UpsertRepositoryAsync(new(repository));
                    await store.StoreIndexRunAsync(
                        new(repository, new CodeIndexRunId($"run:{run}"),
                            Started.AddMinutes(run), plugins: [Plugin]));
                    await store.StageIndexUnitAsync(
                        Unit(repository, $"run:{run}", "unit:a", [node]));
                    await CompleteAsync(
                        store, repository, $"run:{run}", Started.AddMinutes(run));
                }

                var inventory = ReadInventory(path);
                Assert.Empty(inventory.LogicalNodeIds);
                Assert.Empty(inventory.Edges);
                Assert.Empty(inventory.Manifests);
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------ transactional edges

    [Fact]
    public async Task FailedEnabledPublication_RollsBackWithoutMirrorDrift()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:rollback");
        var accepted = Node("node:accepted");
        var proposed = Node("node:proposed");
        var fail = false;
        try
        {
            using (var store = new LatticeCodeGraphStore(
                       path,
                       new LatticeDbStoreOptions { UseNativeTraversal = true },
                       point =>
                       {
                           if (fail && point == "before-commit")
                               throw new InjectedMirrorFault();
                       }))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [accepted]));

                var run = new CodeIndexRunId("run:two");
                await store.StoreIndexRunAsync(
                    new(repository, run, Started.AddMinutes(5), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repository, "run:two", "unit:a", [proposed]));
                fail = true;
                await Assert.ThrowsAsync<InjectedMirrorFault>(async () =>
                    await CompleteAsync(store, repository, "run:two", Started.AddMinutes(5)));
                fail = false;
                Assert.NotNull(await store.GetNodeAsync(repository, accepted.Id));
                Assert.Null(await store.GetNodeAsync(repository, proposed.Id));
            }

            var inventory = ReadInventory(path);
            AssertMirrorIsCanonical(inventory, repository, ["node:accepted"], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task CancelledCompletion_LeavesAcceptedMirrorIntact()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:cancelled");
        var accepted = Node("node:accepted");
        var proposed = Node("node:proposed");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [accepted]));

                var run = new CodeIndexRunId("run:cancelled");
                await store.StoreIndexRunAsync(
                    new(repository, run, Started.AddMinutes(6), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repository, "run:cancelled", "unit:a", [proposed]));

                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repository, run, Started.AddMinutes(6),
                            CodeIndexRunStatus.Completed, Started.AddMinutes(7), [Plugin]),
                        new(repository, run, []),
                        cancelled.Token));
            }

            var inventory = ReadInventory(path);
            AssertMirrorIsCanonical(inventory, repository, ["node:accepted"], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ----------------------------------------------------- repository isolation

    [Fact]
    public async Task MultipleRepositories_WithSharedLogicalIdentity_StayIsolated()
    {
        var path = TemporaryDatabasePath();
        var first = new CodeRepositoryId("repo:first");
        var second = new CodeRepositoryId("repo:second");
        var shared = Node("node:shared");
        var onlyFirst = Node("node:only-first");
        var onlySecond = Node("node:only-second");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, first, "run:one",
                    Unit(first, "run:one", "unit:a", [shared, onlyFirst]));
                await PublishAsync(store, second, "run:one",
                    Unit(second, "run:one", "unit:a", [shared, onlySecond]));
            }

            using (var store = Open(path))
            {
                await Traverse(store, first, shared.Id);
                await Traverse(store, second, shared.Id);
            }

            var both = ReadInventory(path);
            AssertRepositoryMirror(both, first, ["node:shared", "node:only-first"], []);
            AssertRepositoryMirror(both, second, ["node:shared", "node:only-second"], []);
            AssertGlobalCensus(
                both,
                ["node:shared", "node:only-first", "node:shared", "node:only-second"],
                []);
            Assert.Equal(2, both.LogicalNodeIds.Count(id => id == "node:shared"));
            Assert.Equal(4, both.LogicalNodeIds.Count);

            using (var disabled = new LatticeCodeGraphStore(path))
            {
                await disabled.StoreIndexRunAsync(
                    new(first, new CodeIndexRunId("run:disable"),
                        Started.AddMinutes(20), plugins: [Plugin]));
                await disabled.StageIndexUnitAsync(
                    Unit(first, "run:disable", "unit:a", [shared, onlyFirst]));
                await CompleteAsync(disabled, first, "run:disable", Started.AddMinutes(20));
            }

            var after = ReadInventory(path);
            Assert.Single(after.Manifests, manifest => manifest.RepositoryId == second.Value);
            AssertMirrorIsCanonical(after, second, ["node:shared", "node:only-second"], []);
            Assert.Equal(2, after.LogicalNodeIds.Count);

            using var reopened = Open(path);
            await Traverse(reopened, second, shared.Id);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------- recovery of stores

    [Fact]
    public async Task Recovery_RemovesUntrackedDuplicateNodesAndEdges()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:recover-duplicates");
        var a = Node("node:a");
        var b = Node("node:b");
        var edge = Edge("edge:ab", a, b);
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [a, b], [edge]));
            }

            // Emulate the physical residue the defective per-contribution
            // rebuild left behind: an extra node per logical identity and an
            // extra edge, all marked with the mirror protocol but absent from
            // the manifest.
            InjectDuplicate(path, a, b, edge);
            var defective = ReadInventory(path);
            Assert.Equal(2, defective.LogicalNodeIds.Count(id => id == "node:a"));
            Assert.Equal(2, defective.Edges.Count);
            Assert.Contains(defective.Edges, candidate =>
                !defective.Manifests.Any(manifest =>
                    manifest.Edges.Values.Contains(candidate.NativeId)));

            using (var store = Open(path))
            {
                await Traverse(store, repository, a.Id);
            }

            var recovered = ReadInventory(path);
            AssertMirrorIsCanonical(recovered, repository, ["node:a", "node:b"], ["edge:ab"]);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task Recovery_RemovesOrphansLeftByManifestOnlyDeletion()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:recover-orphans");
        var a = Node("node:a");
        var b = Node("node:b");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [a, b], [Edge("edge:ab", a, b)]));
            }

            // Emulate the defective disabled publication: the manifest is gone
            // while every physical row remains.
            DeleteManifestRows(path);
            var defective = ReadInventory(path);
            Assert.Empty(defective.Manifests);
            Assert.Equal(2, defective.LogicalNodeIds.Count);
            Assert.Single(defective.Edges);

            using (var store = Open(path))
            {
                await Traverse(store, repository, a.Id);
            }

            var recovered = ReadInventory(path);
            AssertMirrorIsCanonical(
                recovered, repository, ["node:a", "node:b"], ["edge:ab"]);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task DisabledPublication_CleansLegacyOrphansWithoutManifest()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:disabled-legacy");
        var node = Node("node:a");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [node]));
            }

            // Defective disabled publication: manifest gone, rows remain.
            DeleteManifestRows(path);
            Assert.Single(ReadInventory(path).LogicalNodeIds);

            using (var disabled = new LatticeCodeGraphStore(path))
            {
                await disabled.StoreIndexRunAsync(
                    new(repository, new CodeIndexRunId("run:disabled"),
                        Started.AddMinutes(40), plugins: [Plugin]));
                await disabled.StageIndexUnitAsync(
                    Unit(repository, "run:disabled", "unit:a", [node]));
                await CompleteAsync(
                    disabled, repository, "run:disabled", Started.AddMinutes(40));
            }

            var cleaned = ReadInventory(path);
            Assert.Empty(cleaned.LogicalNodeIds);
            Assert.Empty(cleaned.Edges);
            Assert.Empty(cleaned.Manifests);

            using (var reenabled = Open(path))
            {
                await Traverse(reenabled, repository, node.Id);
            }

            AssertMirrorIsCanonical(
                ReadInventory(path), repository, ["node:a"], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task Recovery_RebuildsMalformedManifest_FromAuthoritativeFacts()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:recover-malformed");
        var node = Node("node:a");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [node]));
            }

            CorruptManifest(path, "not-base64-and-not-json");
            using (var store = Open(path))
            {
                await Traverse(store, repository, node.Id);
            }

            var recovered = ReadInventory(path);
            AssertMirrorIsCanonical(recovered, repository, ["node:a"], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task Recovery_RebuildsManifestWithDuplicateNativeIdentities()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:recover-duplicate-manifest");
        var node = Node("node:a");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [node]));
            }

            WriteManifest(
                path,
                """{"Nodes":{"node:a":1,"node:b":1},"Edges":{}}""");
            using (var store = Open(path))
            {
                await Traverse(store, repository, node.Id);
            }

            AssertMirrorIsCanonical(
                ReadInventory(path), repository, ["node:a"], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task RecoveryFailure_RollsBackCleanup_AndSubsequentReopenSucceeds()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:recovery-failure");
        var node = Node("node:a");
        var fail = false;
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [node]));
            }

            using (var disabled = new LatticeCodeGraphStore(path, null, point =>
                   {
                       if (fail && point == "before-commit")
                           throw new InjectedMirrorFault();
                   }))
            {
                await disabled.StoreIndexRunAsync(
                    new(repository, new CodeIndexRunId("run:disable"),
                        Started.AddMinutes(30), plugins: [Plugin]));
                await disabled.StageIndexUnitAsync(
                    Unit(repository, "run:disable", "unit:a", [node]));
                fail = true;
                await Assert.ThrowsAsync<InjectedMirrorFault>(async () =>
                    await CompleteAsync(disabled, repository, "run:disable", Started.AddMinutes(30)));
                fail = false;
            }

            // The failed disable transaction must not have removed anything.
            var afterFailure = ReadInventory(path);
            AssertMirrorIsCanonical(afterFailure, repository, ["node:a"], []);

            using (var reopened = Open(path))
            {
                await Traverse(reopened, repository, node.Id);
            }

            AssertMirrorIsCanonical(ReadInventory(path), repository, ["node:a"], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task MalformedManifest_DoesNotAffectDisabledAuthoritativeOpen()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:malformed-disabled");
        var node = Node("node:a");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [node]));
            }

            CorruptManifest(path, "{invalid json");
            using (var disabled = new LatticeCodeGraphStore(path))
            {
                Assert.NotNull(await disabled.GetNodeAsync(repository, node.Id));
                await Traverse(disabled, repository, node.Id);
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // --------------------------------------------------------- occurrence lane

    [Fact]
    public async Task OccurrencePublication_DoesNotChangeGraphMirrorCardinality()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:occurrence-mirror");
        var source = Node("node:source");
        var target = Node("node:target");
        var edge = Edge("edge:calls", source, target);
        var fact = Fact(repository, "unit:a", target);
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [source, target], [edge], [fact]));
            }

            var inventory = ReadInventory(path);
            AssertMirrorIsCanonical(
                inventory, repository, ["node:source", "node:target"], ["edge:calls"]);
            Assert.Equal(1, inventory.OccurrenceFacts);
            Assert.DoesNotContain(inventory.LogicalNodeIds, id =>
                id.StartsWith("AndiritOccurrence", StringComparison.Ordinal));

            using (var reopened = Open(path))
            {
                var result = await reopened.TraverseAsync(
                    repository,
                    new CodeGraphTraversalQuery(source.Id, maxDepth: 1));
                Assert.Contains(result.Edges, candidate => candidate.Id == edge.Id);
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ------------------------------------------------------- negative controls

    [Fact]
    public async Task InventoryOracle_DetectsInjectedDuplicateAndOrphan()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:negative-control");
        var a = Node("node:a");
        var b = Node("node:b");
        var edge = Edge("edge:ab", a, b);
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [a, b], [edge]));
            }

            AssertMirrorIsCanonical(
                ReadInventory(path), repository, ["node:a", "node:b"], ["edge:ab"]);

            InjectDuplicate(path, a, b, edge);

            // The same oracle that passes above must fail on the residue the
            // original per-contribution rebuild produced.
            Assert.ThrowsAny<Exception>(() =>
                AssertMirrorIsCanonical(
                    ReadInventory(path), repository, ["node:a", "node:b"], ["edge:ab"]));

            using (var store = Open(path))
            {
                await Traverse(store, repository, a.Id);
            }

            AssertMirrorIsCanonical(
                ReadInventory(path), repository, ["node:a", "node:b"], ["edge:ab"]);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task InventoryOracle_DetectsManifestOnlyDeletionResidue()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:negative-orphans");
        var node = Node("node:a");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    Unit(repository, "run:one", "unit:a", [node]));
            }

            DeleteManifestRows(path);
            var defective = ReadInventory(path);
            Assert.Empty(defective.Manifests);
            Assert.NotEmpty(defective.LogicalNodeIds);
            Assert.ThrowsAny<Exception>(() =>
                AssertMirrorIsCanonical(defective, repository, ["node:a"], []));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ----------------------------------------------------------- oracle helpers

    /// <summary>Single-repository convenience: repository view plus global census.</summary>
    private static void AssertMirrorIsCanonical(
        Inventory inventory,
        CodeRepositoryId repository,
        string[] expectedNodes,
        string[] expectedEdges)
    {
        AssertRepositoryMirror(inventory, repository, expectedNodes, expectedEdges);
        AssertGlobalCensus(inventory, expectedNodes, expectedEdges);
    }

    private static void AssertRepositoryMirror(
        Inventory inventory,
        CodeRepositoryId repository,
        string[] expectedNodes,
        string[] expectedEdges)
    {
        var manifest = Assert.Single(
            inventory.Manifests, candidate => candidate.RepositoryId == repository.Value);
        Assert.Equal(
            expectedNodes.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            manifest.Nodes.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            expectedEdges.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            manifest.Edges.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());

        var physicalNodes = inventory.NativeNodeIds
            .Zip(inventory.LogicalNodeIds)
            .ToDictionary(pair => pair.First, pair => pair.Second);
        foreach (var (logicalId, nativeId) in manifest.Nodes)
        {
            Assert.True(physicalNodes.TryGetValue(nativeId, out var actual));
            Assert.Equal(logicalId, actual);
        }

        var physicalEdges = inventory.Edges.ToDictionary(edge => edge.NativeId);
        foreach (var (logicalId, nativeId) in manifest.Edges)
        {
            Assert.True(physicalEdges.TryGetValue(nativeId, out var actual));
            Assert.Equal(logicalId, actual.LogicalId);
        }
    }

    /// <summary>
    /// Asserts the whole-database census: the expected physical multiset, a
    /// complete bidirectional manifest-to-row correspondence and unique native
    /// identities. Aggregate counts alone are insufficient.
    /// </summary>
    private static void AssertGlobalCensus(
        Inventory inventory,
        string[] expectedNodes,
        string[] expectedEdges)
    {
        Assert.Equal(
            expectedNodes.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            inventory.LogicalNodeIds.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            expectedEdges.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            inventory.Edges.Select(edge => edge.LogicalId)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray());

        Assert.Equal(inventory.NativeNodeIds.Count, inventory.Manifests.Sum(m => m.Nodes.Count));
        Assert.Equal(
            inventory.NativeNodeIds.OrderBy(id => id).ToArray(),
            inventory.Manifests.SelectMany(m => m.Nodes.Values).OrderBy(id => id).ToArray());
        Assert.Equal(inventory.Edges.Count, inventory.Manifests.Sum(m => m.Edges.Count));
        Assert.Equal(
            inventory.Edges.Select(edge => edge.NativeId).OrderBy(id => id).ToArray(),
            inventory.Manifests.SelectMany(m => m.Edges.Values).OrderBy(id => id).ToArray());
        Assert.Equal(
            inventory.NativeNodeIds.Count,
            inventory.NativeNodeIds.Distinct().Count());
        Assert.Equal(
            inventory.Edges.Count,
            inventory.Edges.Select(edge => edge.NativeId).Distinct().Count());

        foreach (var edge in inventory.Edges)
        {
            Assert.Contains(edge.SourceNative, inventory.NativeNodeIds);
            Assert.Contains(edge.TargetNative, inventory.NativeNodeIds);
        }
    }

    // ------------------------------------------------------ physical inventory

    private sealed record PhysicalEdge(
        ulong NativeId,
        string LogicalId,
        ulong SourceNative,
        ulong TargetNative,
        string Type);

    private sealed record ManifestView(
        string RepositoryId,
        IReadOnlyDictionary<string, ulong> Nodes,
        IReadOnlyDictionary<string, ulong> Edges);

    private sealed record Inventory(
        IReadOnlyList<string> LogicalNodeIds,
        IReadOnlyList<ulong> NativeNodeIds,
        IReadOnlyList<PhysicalEdge> Edges,
        IReadOnlyList<ManifestView> Manifests,
        int OccurrenceFacts);

    private static Inventory ReadInventory(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var logicalNodeIds = new List<string>();
        var nativeNodeIds = new List<ulong>();
        var mirrorNodes = transaction.GetNodesByLabel(NativeGraphMirror.NodeLabel);
        foreach (var node in mirrorNodes)
        {
            if (!transaction.TryGetProperty(node, "hetu_json", out var payload))
                continue;
            using var document = JsonDocument.Parse(payload.AsString());
            logicalNodeIds.Add(document.RootElement
                .GetProperty("Id").GetProperty("Value").GetString()!);
            nativeNodeIds.Add(node.Value);
        }

        var edges = new Dictionary<ulong, PhysicalEdge>();
        foreach (var node in mirrorNodes)
        {
            foreach (var edge in transaction.GetOutgoingEdges(node))
            {
                if (!transaction.TryGetEdgeProperty(edge.Id, "hetu_json", out var payload))
                    continue;
                using var document = JsonDocument.Parse(payload.AsString());
                edges[edge.Id.Value] = new PhysicalEdge(
                    edge.Id.Value,
                    document.RootElement
                        .GetProperty("Id").GetProperty("Value").GetString()!,
                    edge.Source.Value,
                    edge.Target.Value,
                    edge.Type);
            }
        }

        var manifests = new List<ManifestView>();
        foreach (var node in transaction.GetNodesByLabel(NativeGraphMirror.MirrorLabel))
        {
            if (!transaction.TryGetProperty(node, "key", out var key) ||
                !transaction.TryGetProperty(node, "payload", out var payload))
                continue;
            var manifest = NativeGraphMirror.ReadManifest(
                JsonSerializer.Deserialize<string>(
                    Convert.FromBase64String(payload.AsString()))!);
            manifests.Add(new ManifestView(
                key.AsString(),
                new Dictionary<string, ulong>(manifest.Nodes, StringComparer.Ordinal),
                new Dictionary<string, ulong>(manifest.Edges, StringComparer.Ordinal)));
        }

        using var occurrenceQuery = database.Prepare(
            "MATCH (n:AndiritOccurrenceUnit) RETURN n.payload");
        using var occurrenceResult = occurrenceQuery.Execute(transaction);
        var occurrenceFacts = 0;
        foreach (var row in occurrenceResult.ReadAll())
        {
            using var document = JsonDocument.Parse(
                Convert.FromBase64String(row.GetValue(0).AsString()));
            occurrenceFacts += document.RootElement.GetProperty("Facts").GetArrayLength();
        }

        transaction.Commit();
        return new Inventory(
            logicalNodeIds,
            nativeNodeIds,
            edges.Values.OrderBy(edge => edge.NativeId).ToArray(),
            manifests,
            occurrenceFacts);
    }

    private static IReadOnlyList<ulong> NativeNodes(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var result = transaction.GetNodesByLabel(NativeGraphMirror.NodeLabel)
            .Select(node => node.Value)
            .ToArray();
        transaction.Commit();
        return result;
    }

    // ----------------------------------------------------- residue injection

    private static void InjectDuplicate(
        string path,
        CodeGraphNode first,
        CodeGraphNode second,
        CodeGraphEdge edge)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var firstId = transaction.CreateNode(NativeGraphMirror.NodeLabel);
        transaction.SetProperty(
            firstId,
            "hetu_json",
            LatticeValue.From(JsonSerializer.Serialize(
                first)));
        var secondId = transaction.CreateNode(NativeGraphMirror.NodeLabel);
        transaction.SetProperty(
            secondId,
            "hetu_json",
            LatticeValue.From(JsonSerializer.Serialize(
                second)));
        var edgeId = transaction.CreateEdge(firstId, secondId, edge.Kind.Value);
        transaction.SetEdgeProperty(
            edgeId,
            "hetu_json",
            LatticeValue.From(JsonSerializer.Serialize(
                edge)));
        transaction.Commit();
    }

    private static void DeleteManifestRows(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        using var query = database.Prepare(
            $"MATCH (s:{NativeGraphMirror.MirrorLabel}) DELETE s");
        using var result = query.Execute(transaction);
        result.ReadAll();
        transaction.Commit();
    }

    private static void CorruptManifest(string path, string payload) =>
        WriteManifestRaw(path, JsonSerializer.Serialize(payload));

    private static void WriteManifest(string path, string manifestJson) =>
        WriteManifestRaw(path, manifestJson);

    private static void WriteManifestRaw(string path, string encodedValue)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var manifest = transaction.GetNodesByLabel(NativeGraphMirror.MirrorLabel)
            .Single();
        transaction.SetProperty(
            manifest,
            "payload",
            LatticeValue.From(Convert.ToBase64String(
                Encoding.UTF8.GetBytes(encodedValue))));
        transaction.Commit();
    }

    // ------------------------------------------------------------ store helpers

    private static LatticeCodeGraphStore Open(string path) =>
        new(path, new LatticeDbStoreOptions { UseNativeTraversal = true });

    private static async Task PublishAsync(
        LatticeCodeGraphStore store,
        CodeRepositoryId repository,
        string run,
        params CodeIndexUnitReplacement[] units)
    {
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repository));
        await store.StoreIndexRunAsync(
            new(repository, runId, Started, plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await CompleteAsync(store, repository, run, Started);
    }

    private static async Task CompleteAsync(
        LatticeCodeGraphStore store,
        CodeRepositoryId repository,
        string run,
        DateTimeOffset started)
    {
        var runId = new CodeIndexRunId(run);
        await store.CompleteIndexRunAsync(
            new(repository, runId, started, CodeIndexRunStatus.Completed,
                started.AddSeconds(1), [Plugin]),
            new(repository, runId, []));
    }

    private static async Task<CodeGraphTraversalResult> Traverse(
        LatticeCodeGraphStore store,
        CodeRepositoryId repository,
        CodeNodeId start) =>
        await store.TraverseAsync(
            repository,
            new CodeGraphTraversalQuery(start, maxDepth: 5, maxNodes: 100, maxEdges: 100));

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repository,
        string run,
        string unit,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeGraphEdge>? edges = null,
        IReadOnlyList<CodeOccurrenceFact>? occurrences = null) =>
        new(
            new CodeFactOrigin(
                repository, Plugin, "1.0.0", new CodeIndexRunId(run),
                new CodeIndexUnitId(unit)),
            nodes,
            edges: edges,
            occurrences: occurrences,
            occurrenceAnalysisIncluded: occurrences is { Count: > 0 },
            occurrenceCoverage: occurrences is { Count: > 0 }
                ? new CodeOccurrenceCoverage(
                    occurrences.Count, 0, CodeRelationshipCoverageState.Produced)
                : null);

    private static CodeGraphNode Node(string id, string? name = null)
    {
        var actual = name ?? id[(id.LastIndexOf(':') + 1)..];
        return new CodeGraphNode(
            new CodeNodeId(id),
            CodeNodeKinds.Type,
            actual,
            "Example." + actual,
            new CodeSymbolId("symbol:" + id));
    }

    private static CodeGraphEdge Edge(
        string id,
        CodeGraphNode source,
        CodeGraphNode target) =>
        new(
            new CodeEdgeId(id),
            source.Id,
            target.Id,
            CodeEdgeKinds.Calls,
            new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"));

    private static CodeOccurrenceFact Fact(
        CodeRepositoryId repository,
        string unit,
        CodeGraphNode target)
    {
        var name = "target:" + target.Id.Value;
        var context = "compctx1:sha256:" +
            Convert.ToHexStringLower(SHA256.HashData(
                Encoding.UTF8.GetBytes(repository.Value + "|" + name)));
        var targetId = CodeOccurrenceIdentity.ComputeTargetId(
            context,
            "meta:" + name,
            "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest(
                [$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
        return CodeOccurrenceFact.Create(
            repository,
            targetId,
            targetId.Value,
            unit,
            new CodeOccurrenceContributor(Plugin, unit),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation("src/Test.cs", 1, 1, 1, 5),
            sourceHash: "sha256:source",
            contentHash: "content:invocation");
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(
            Path.GetTempPath(), $"andirit-s64-mirror-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
        {
            if (File.Exists(path + suffix))
                File.Delete(path + suffix);
        }
    }

    private sealed class InjectedMirrorFault : Exception;
}
