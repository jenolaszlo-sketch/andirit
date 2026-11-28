using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

internal static class Program
{
    private const string NodeLabel = "HetuGraphNode";
    private const string MirrorLabel = "HetuMirror";
    private const string JsonProperty = "hetu_json";
    private const string UnrelatedLabel = "QualUnrelated";
    private const string IncidentType = "QualIncident";

    private static readonly CodePluginId Plugin = new("plugin:qual");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 28, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Output =
        new() { WriteIndented = true };

    private static readonly CodeGraphNode A = Node("node:a");
    private static readonly CodeGraphNode B = Node("node:b");
    private static readonly CodeGraphEdge E = Edge("edge:ab", A, B);

    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: <command> <database> [out]");
            return 2;
        }

        var command = args[0];
        var database = Path.GetFullPath(args[1]);
        var output = args.Length > 2 ? args[2] : null;
        object result = command switch
        {
            "repro-shared" => await ReproShared(database),
            "repro-orphan" => await ReproOrphan(database),
            "repro-unrelated-node" => await ReproUnrelatedNode(database),
            "repro-authoritative-node" => await ReproAuthoritativeNode(database),
            "repro-incident-edge" => await ReproIncidentEdge(database),
            "repro-cross-repository" => await ReproCrossRepository(database),
            "repro-inconsistent-manifest" => await ReproInconsistentManifest(database),
            "repro-protocol-orphan" => await ReproProtocolOrphan(database),
            "repro-foreign-outgoing" => await ReproForeignOutgoing(database),
            "repro-missing-manifest" => await ReproMissingManifest(database),
            "repro-malformed-manifest" => await ReproMalformedManifest(database),
            "repro-stale-manifest" => await ReproStaleManifest(database),
            "cleanup-failure" => await CleanupFailure(database),
            "cleanup-cancel" => await CleanupCancel(database),
            "probe-delete" => ProbeDelete(database),
            "make-affected" => await MakeAffected(database),
            "recover" => await Recover(database),
            "recover-sealed" => await RecoverSealed(database),
            "census" => ReadCensus(database),
            _ => throw new ArgumentException("unknown command " + command)
        };
        var json = JsonSerializer.Serialize(result, Output);
        Console.WriteLine(json);
        if (output is not null)
            await File.WriteAllTextAsync(output, json);
        return 0;
    }

    // ------------------------------------------------------------- scenarios

    private static async Task<object> ReproShared(string database)
    {
        var repository = new CodeRepositoryId("repo:shared");
        var units = SharedUnits(repository, "run:one");
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var initial = ReadCensus(database);
        string[] traversal;
        using (var store = Open(database, true))
            traversal = await Traverse(store, repository, A.Id);
        var reopened = ReadCensus(database);
        var errors = ValidateRepository(
            reopened, repository.Value,
            ExpectedNodes(units), ExpectedEdges(units));
        return new
        {
            command = "repro-shared",
            pass = errors.Count == 0,
            errors,
            initial = initial.Summary,
            after = reopened,
            traversal
        };
    }

    private static async Task<object> ReproOrphan(string database)
    {
        var repository = new CodeRepositoryId("repo:orphan");
        var units = SharedUnits(repository, "run:one");
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        using (var disabled = Open(database, false))
            await Publish(disabled, repository, "run:two",
                SharedUnits(repository, "run:two"));
        var disabledCensus = ReadCensus(database);
        using (var store = Open(database, true))
            _ = await Traverse(store, repository, A.Id);
        var reopened = ReadCensus(database);
        var errors = ValidateRepository(
            reopened, repository.Value,
            ExpectedNodes(units), ExpectedEdges(units));
        if (disabledCensus.Nodes.Count != 0 ||
            disabledCensus.Edges.Count != 0 ||
            disabledCensus.Manifests.Count != 0)
        {
            errors.Add("disabled publication left mirror objects or manifests");
        }

        return new
        {
            command = "repro-orphan",
            pass = errors.Count == 0,
            errors,
            disabled = disabledCensus.Summary,
            after = reopened
        };
    }

    private static async Task<object> ReproUnrelatedNode(string database)
    {
        var repository = new CodeRepositoryId("repo:ownership-node");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var unrelated = CreateUnrelatedNode(database, "sentinel-node", 17);
        var map = ReadCensus(database);
        var nativeA = map.Nodes.Single(node => node.LogicalId == A.Id.Value).NativeId;
        WriteManifest(database, repository.Value,
            new() { [A.Id.Value] = unrelated.NativeId },
            []);
        var refused = false;
        try
        {
            using var store = Open(database, true);
            _ = await Traverse(store, repository, A.Id);
        }
        catch (Exception exception)
        {
            refused = exception is InvalidDataException;
        }

        var after = ReadCensus(database);
        var unrelatedAfter = after.Related.SingleOrDefault(
            node => node.NativeId == unrelated.NativeId);
        var errors = ValidateRepository(
            after, repository.Value,
            ExpectedNodes(units), ExpectedEdges(units));
        if (unrelatedAfter is null ||
            unrelatedAfter.Note != "sentinel-node" ||
            unrelatedAfter.Number != 17)
        {
            errors.Add("unrelated node was not preserved with its properties");
        }

        return new
        {
            command = "repro-unrelated-node",
            pass = errors.Count == 0 && !refused,
            refused,
            errors,
            unrelated = unrelatedAfter,
            after
        };
    }

    private static async Task<object> ReproAuthoritativeNode(string database)
    {
        var repository = new CodeRepositoryId("repo:ownership-auth");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var before = ReadAuthoritative(database);
        var unit = before.Single(row => row.Label == "HetuUnit");
        WriteManifest(database, repository.Value,
            new() { [A.Id.Value] = unit.NativeId },
            []);
        var refused = false;
        try
        {
            using var store = Open(database, true);
            _ = await Traverse(store, repository, A.Id);
        }
        catch (Exception exception)
        {
            refused = exception is InvalidDataException;
        }

        var after = ReadAuthoritative(database);
        var errors = ValidateRepository(
            ReadCensus(database), repository.Value,
            ExpectedNodes(units), ExpectedEdges(units));
        if (!before.SequenceEqual(after))
            errors.Add("authoritative rows changed");
        return new
        {
            command = "repro-authoritative-node",
            pass = errors.Count == 0 && !refused,
            refused,
            errors,
            authoritativeBefore = before,
            authoritativeAfter = after
        };
    }

    private static async Task<object> ReproIncidentEdge(string database)
    {
        var repository = new CodeRepositoryId("repo:incident");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var map = ReadCensus(database);
        var mirrorA = map.Nodes.Single(node => node.LogicalId == A.Id.Value).NativeId;
        var authoritativeBefore = ReadAuthoritative(database);
        var incident = CreateIncidentEdge(database, "incident", mirrorA);
        var refused = false;
        try
        {
            using var store = Open(database, true);
            _ = await Traverse(store, repository, A.Id);
        }
        catch (Exception exception)
        {
            refused = exception is InvalidDataException;
        }

        var afterRefusal = ReadCensus(database);
        var mirrorUnchanged = map.Nodes.Count == afterRefusal.Nodes.Count &&
            map.Edges.Count == afterRefusal.Edges.Count &&
            map.Manifests.Single().Nodes.Count ==
                afterRefusal.Manifests.Single().Nodes.Count;
        var authoritativeUnchanged =
            authoritativeBefore.SequenceEqual(ReadAuthoritative(database));
        var unrelatedPreserved = afterRefusal.Related.Any(
            node => node.NativeId == incident.NodeId && node.Note == "incident");
        var edgePreserved = afterRefusal.Related.Any(
            node => node.NativeId == incident.NodeId &&
                node.Outgoing.Any(edge => edge.NativeId == incident.EdgeId &&
                    edge.Type == IncidentType));
        try
        {
            DeleteIncidentEdge(database, incident);
        }
        catch
        {
            // The baseline path may already have deleted the edge.
        }
        var repaired = false;
        try
        {
            using var store = Open(database, true);
            _ = await Traverse(store, repository, A.Id);
            repaired = true;
        }
        catch
        {
            repaired = false;
        }

        var errors = ValidateRepository(
            ReadCensus(database), repository.Value,
            ExpectedNodes(units), ExpectedEdges(units));
        if (!refused) errors.Add("ambiguous incident edge was not refused");
        if (!mirrorUnchanged) errors.Add("refusal partially changed mirror state");
        if (!authoritativeUnchanged) errors.Add("authoritative rows changed");
        if (!unrelatedPreserved) errors.Add("unrelated node was not preserved");
        if (!edgePreserved) errors.Add("unrelated edge was not preserved after refusal");
        if (!repaired) errors.Add("repaired store did not reopen");
        return new
        {
            command = "repro-incident-edge",
            pass = errors.Count == 0,
            refused,
            mirrorUnchanged,
            authoritativeUnchanged,
            unrelatedPreserved,
            edgePreserved,
            repaired,
            errors
        };
    }

    private static async Task<object> ReproCrossRepository(string database)
    {
        var first = new CodeRepositoryId("repo:cross-first");
        var second = new CodeRepositoryId("repo:cross-second");
        var secondUnits = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
        {
            await Publish(store, first, "run:one",
                new[] { Unit("unit:one", [A], []) });
            await Publish(store, second, "run:one", secondUnits);
        }

        var census = ReadCensus(database);
        var secondB = census.Manifests
            .Single(manifest => manifest.Repository == second.Value)
            .Nodes[B.Id.Value];
        WriteManifest(database, first.Value,
            new() { [A.Id.Value] = secondB },
            []);
        using (var store = Open(database, true))
        {
            _ = await Traverse(store, first, A.Id);
            _ = await Traverse(store, second, A.Id);
        }

        var after = ReadCensus(database);
        var errors = new List<string>();
        ValidateManifest(after, first.Value, [A.Id.Value], [], errors);
        ValidateManifest(after, second.Value,
            [A.Id.Value, B.Id.Value], [E.Id.Value], errors);
        ValidateGlobal(after, 3, 1, errors);
        return new
        {
            command = "repro-cross-repository",
            pass = errors.Count == 0,
            errors,
            after
        };
    }

    private static async Task<object> ReproInconsistentManifest(string database)
    {
        var repository = new CodeRepositoryId("repo:inconsistent");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var census = ReadCensus(database);
        var nativeE = census.Edges.Single(edge => edge.LogicalId == E.Id.Value).NativeId;
        WriteManifest(database, repository.Value, new(), new() { [E.Id.Value] = nativeE });
        using (var store = Open(database, true))
            _ = await Traverse(store, repository, A.Id);
        var after = ReadCensus(database);
        var errors = ValidateRepository(
            after, repository.Value, ExpectedNodes(units), ExpectedEdges(units));
        if (HasDanglingMirrorEdge(database))
            errors.Add("dangling mirror edge remains");
        return new
        {
            command = "repro-inconsistent-manifest",
            pass = errors.Count == 0,
            errors,
            after
        };
    }

    private static async Task<object> ReproProtocolOrphan(string database)
    {
        var repository = new CodeRepositoryId("repo:protocol-orphan");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var orphan = CreateProtocolOrphan(database, "node:qual-orphan");
        var unmarked = CreateUnrelatedNode(database, "unmarked", 31);
        using (var store = Open(database, true))
            _ = await Traverse(store, repository, A.Id);
        var after = ReadCensus(database);
        var errors = ValidateRepository(
            after, repository.Value, ExpectedNodes(units), ExpectedEdges(units));
        if (after.Nodes.Any(node => node.LogicalId == "node:qual-orphan"))
            errors.Add("unreferenced protocol-marked orphan was not swept");
        if (!after.Related.Any(node => node.NativeId == unmarked.NativeId &&
                node.Note == "unmarked" && node.Number == 31))
            errors.Add("unmarked unrelated node was not preserved");
        return new
        {
            command = "repro-protocol-orphan",
            pass = errors.Count == 0,
            errors,
            orphan,
            after
        };
    }

    private static async Task<object> ReproForeignOutgoing(string database)
    {
        var repository = new CodeRepositoryId("repo:foreign-outgoing");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var before = ReadCensus(database);
        // Attach the foreign edge to the second manifest node so a defective
        // cleanup would have already deleted the first node before failing.
        var mirrorB = before.Nodes.Single(node => node.LogicalId == B.Id.Value).NativeId;
        var foreign = CreateForeignOutgoing(database, "foreign-out", mirrorB);
        var authoritativeBefore = ReadAuthoritative(database);
        var refused = false;
        try
        {
            using var store = Open(database, true);
            _ = await Traverse(store, repository, A.Id);
        }
        catch (Exception exception)
        {
            refused = exception is InvalidDataException;
        }

        var afterRefusal = ReadCensus(database);
        var mirrorUnchanged = before.Nodes.Count == afterRefusal.Nodes.Count &&
            before.Edges.Count == afterRefusal.Edges.Count &&
            before.Manifests.Single().Nodes.Count ==
                afterRefusal.Manifests.Single().Nodes.Count;
        var unrelatedPreserved = afterRefusal.Related.Any(
            node => node.NativeId == foreign.NodeId && node.Note == "foreign-out");
        var edgePreserved = EdgeExists(database, foreign.EdgeId);
        var authoritativeUnchanged =
            authoritativeBefore.SequenceEqual(ReadAuthoritative(database));
        try
        {
            DeleteEdge(database, foreign);
        }
        catch
        {
            // Baseline may already have deleted the relationship.
        }

        using (var store = Open(database, true))
            _ = await Traverse(store, repository, A.Id);
        using (var store = Open(database, true))
            _ = await Traverse(store, repository, A.Id);
        var errors = ValidateRepository(
            ReadCensus(database), repository.Value,
            ExpectedNodes(units), ExpectedEdges(units));
        if (!refused) errors.Add("foreign outgoing edge was not refused");
        if (!mirrorUnchanged) errors.Add("refusal partially changed mirror state");
        if (!unrelatedPreserved) errors.Add("unrelated outgoing target lost");
        if (!edgePreserved) errors.Add("unrelated outgoing edge lost");
        if (!authoritativeUnchanged) errors.Add("authoritative rows changed");
        return new
        {
            command = "repro-foreign-outgoing",
            pass = errors.Count == 0,
            refused,
            mirrorUnchanged,
            unrelatedPreserved,
            edgePreserved,
            authoritativeUnchanged,
            errors
        };
    }

    private static async Task<object> ReproMissingManifest(string database)
    {
        var repository = new CodeRepositoryId("repo:missing-manifest");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var authoritativeBefore = ReadAuthoritative(database);
        DeleteManifest(database, repository.Value);
        using (var store = Open(database, true))
            _ = await Traverse(store, repository, A.Id);
        var errors = ValidateRepository(
            ReadCensus(database), repository.Value,
            ExpectedNodes(units), ExpectedEdges(units));
        if (!authoritativeBefore.SequenceEqual(ReadAuthoritative(database)))
            errors.Add("authoritative rows changed");
        return new
        {
            command = "repro-missing-manifest",
            pass = errors.Count == 0,
            errors
        };
    }

    private static async Task<object> ReproMalformedManifest(string database)
    {
        var repository = new CodeRepositoryId("repo:malformed-manifest");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var authoritativeBefore = ReadAuthoritative(database);
        CorruptManifest(database, repository.Value, "not-a-manifest");
        using (var store = Open(database, true))
            _ = await Traverse(store, repository, A.Id);
        var errors = ValidateRepository(
            ReadCensus(database), repository.Value,
            ExpectedNodes(units), ExpectedEdges(units));
        if (!authoritativeBefore.SequenceEqual(ReadAuthoritative(database)))
            errors.Add("authoritative rows changed");
        return new
        {
            command = "repro-malformed-manifest",
            pass = errors.Count == 0,
            errors
        };
    }

    private static async Task<object> ReproStaleManifest(string database)
    {
        var repository = new CodeRepositoryId("repo:stale-manifest");
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one",
                new[] { Unit("unit:one", [A, B], [E]) });
        var first = ReadCensus(database);
        var staleA = first.Nodes.Single(node => node.LogicalId == A.Id.Value).NativeId;
        using (var store = Open(database, true))
            await Publish(store, repository, "run:two",
                new[] { Unit("unit:one", [A, Node("node:c")], []) });
        WriteManifest(database, repository.Value,
            new() { [A.Id.Value] = staleA }, new());
        using (var store = Open(database, true))
            _ = await Traverse(store, repository, A.Id);
        var after = ReadCensus(database);
        var errors = new List<string>();
        if (after.Nodes.Select(node => node.LogicalId)
                .OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(new[] { "node:a", "node:c" }) is false)
            errors.Add("stale manifest did not rebuild canonical nodes");
        if (after.Nodes.Count != 2 || after.Edges.Count != 0)
            errors.Add("stale manifest left residue");
        if (after.Manifests.Sum(manifest => manifest.Nodes.Count) != after.Nodes.Count)
            errors.Add("manifest does not cover physical nodes");
        return new
        {
            command = "repro-stale-manifest",
            pass = errors.Count == 0,
            errors,
            staleA,
            after
        };
    }

    private static async Task<object> CleanupFailure(string database)
    {
        var repository = new CodeRepositoryId("repo:cleanup-failure");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var before = ReadCensus(database);
        var failed = false;
        var fail = true;
        using (var store = CreateStoreWithFault(database, point =>
               {
                   if (fail && point == "before-commit")
                       throw new QualFault();
               }))
        {
            try
            {
                await Publish(store, repository, "run:disable", units);
            }
            catch (QualFault)
            {
                failed = true;
            }
            finally
            {
                fail = false;
            }
        }

        var after = ReadCensus(database);
        var errors = ValidateRepository(
            after, repository.Value, ExpectedNodes(units), ExpectedEdges(units));
        if (!failed) errors.Add("cleanup failure was not injected");
        if (!SameMirror(before, after))
            errors.Add("failed cleanup changed mirror state");
        using (var reopened = Open(database, true))
        {
            var publication = await reopened.GetLatestPublicationAsync(repository);
            if (publication?.IndexRunId.Value != "run:one")
                errors.Add("failed cleanup changed the accepted publication");
        }

        return new
        {
            command = "cleanup-failure",
            pass = errors.Count == 0,
            failed,
            errors
        };
    }

    private static async Task<object> CleanupCancel(string database)
    {
        var repository = new CodeRepositoryId("repo:cleanup-cancel");
        var units = new[] { Unit("unit:one", [A, B], [E]) };
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        var before = ReadCensus(database);
        var cancelled = false;
        using (var disabled = Open(database, false))
        {
            var run = new CodeIndexRunId("run:cancel");
            await disabled.StoreIndexRunAsync(
                new(repository, run, Started.AddMinutes(2), plugins: [Plugin]));
            await disabled.StageIndexUnitAsync(
                MakeUnit(repository, run.Value, "unit:one", [A, B], [E]));
            using var token = new CancellationTokenSource();
            token.Cancel();
            try
            {
                await disabled.CompleteIndexRunAsync(
                    new(repository, run, Started.AddMinutes(2),
                        CodeIndexRunStatus.Completed, Started.AddMinutes(3), [Plugin]),
                    new(repository, run, []),
                    token.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
        }

        var after = ReadCensus(database);
        var errors = ValidateRepository(
            after, repository.Value, ExpectedNodes(units), ExpectedEdges(units));
        if (!cancelled) errors.Add("cancelled completion did not throw");
        if (!SameMirror(before, after))
            errors.Add("cancelled completion changed mirror state");
        using (var reopened = Open(database, true))
        {
            var publication = await reopened.GetLatestPublicationAsync(repository);
            if (publication?.IndexRunId.Value != "run:one")
                errors.Add("cancelled completion changed the accepted publication");
        }

        return new
        {
            command = "cleanup-cancel",
            pass = errors.Count == 0,
            cancelled,
            errors
        };
    }

    private static CodeIndexUnitReplacement MakeUnit(
        CodeRepositoryId repository, string run, string unit,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeGraphEdge> edges) =>
        new(new CodeFactOrigin(
            repository, Plugin, "1.0.0", new CodeIndexRunId(run),
            new CodeIndexUnitId(unit)), nodes, edges: edges);

    private static LatticeCodeGraphStore CreateStoreWithFault(
        string path, Action<string> injector)
    {
        var ctor = typeof(LatticeCodeGraphStore).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(string), typeof(LatticeDbStoreOptions), typeof(Action<string>)],
            modifiers: null) ?? throw new InvalidOperationException("fault ctor");
        return (LatticeCodeGraphStore)ctor.Invoke([path, null, injector]);
    }

    private static bool SameMirror(Census left, Census right) =>
        left.Nodes.Count == right.Nodes.Count &&
        left.Edges.Count == right.Edges.Count &&
        left.Manifests.Count == right.Manifests.Count &&
        left.Nodes.Select(node => node.LogicalId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .SequenceEqual(right.Nodes.Select(node => node.LogicalId)
                .OrderBy(id => id, StringComparer.Ordinal)) &&
        left.Edges.Select(edge => edge.LogicalId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .SequenceEqual(right.Edges.Select(edge => edge.LogicalId)
                .OrderBy(id => id, StringComparer.Ordinal));

    private sealed class QualFault : Exception;

    private static object ProbeDelete(string database)
    {
        ulong mirror;
        ulong target;
        ulong mirrorEdge;
        ulong unrelated;
        ulong unrelatedEdge;
        using (var db = LatticeDatabase.Open(
                   database, new LatticeDatabaseOptions { Create = true }))
        using (var txn = db.BeginWriteTransaction())
        {
            mirror = txn.CreateNode(NodeLabel).Value;
            txn.SetProperty(new LatticeNodeId(mirror), JsonProperty,
                LatticeValue.From("{\"Id\":{\"Value\":\"probe:mirror\"}}"));
            target = txn.CreateNode(NodeLabel).Value;
            txn.SetProperty(new LatticeNodeId(target), JsonProperty,
                LatticeValue.From("{\"Id\":{\"Value\":\"probe:target\"}}"));
            mirrorEdge = txn.CreateEdge(
                new LatticeNodeId(mirror), new LatticeNodeId(target), "calls").Value;
            txn.SetEdgeProperty(new LatticeEdgeId(mirrorEdge), JsonProperty,
                LatticeValue.From("{}"));
            unrelated = txn.CreateNode(UnrelatedLabel).Value;
            txn.SetProperty(new LatticeNodeId(unrelated), "note",
                LatticeValue.From("keep"));
            unrelatedEdge = txn.CreateEdge(
                new LatticeNodeId(unrelated), new LatticeNodeId(mirror),
                "QualUnrelated").Value;
            txn.SetEdgeProperty(new LatticeEdgeId(unrelatedEdge), "note",
                LatticeValue.From("keep-edge"));
            txn.Commit();
        }

        using (var db = LatticeDatabase.Open(
                   database, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginWriteTransaction())
        {
            txn.DeleteNode(new LatticeNodeId(mirror));
            txn.Commit();
        }

        using (var db = LatticeDatabase.Open(
                   database, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginReadTransaction())
        {
            var mirrorExists = txn.NodeExists(new LatticeNodeId(mirror));
            var unrelatedExists = txn.NodeExists(new LatticeNodeId(unrelated));
            var edgeExists = txn.GetOutgoingEdges(new LatticeNodeId(unrelated))
                .Any(edge => edge.Id.Value == unrelatedEdge);
            txn.Commit();
            return new
            {
                command = "probe-delete",
                mirrorExists,
                unrelatedExists,
                danglingEdgeExists = edgeExists,
                deleteNodeCascades = !mirrorExists && !edgeExists
            };
        }
    }

    private static async Task<object> MakeAffected(string database)
    {
        var repository = new CodeRepositoryId("repo:affected");
        var units = WithOccurrence(repository, SharedUnits(repository, "run:one"));
        using (var store = Open(database, true))
            await Publish(store, repository, "run:one", units);
        using (var store = Open(database, true))
        {
            // Reopen to force the defective rebuild without traversing it.
        }
        using (var disabled = Open(database, false))
            await Publish(disabled, repository, "run:two",
                WithOccurrence(repository, SharedUnits(repository, "run:two")));
        using (var store = Open(database, true))
        {
            // Re-enable to accumulate the defective generation.
        }
        var sentinel = CreateSentinel(database);
        var census = ReadCensus(database);
        return new
        {
            command = "make-affected",
            census,
            sentinel,
            authoritative = ReadAuthoritative(database)
        };
    }

    private static async Task<object> Recover(string database)
    {
        var repository = new CodeRepositoryId("repo:affected");
        var units = WithOccurrence(repository, SharedUnits(repository, "run:one"));
        var before = ReadCensus(database);
        var authoritativeBefore = ReadAuthoritative(database);
        string[] projection;
        using (var disabled = Open(database, false))
            projection = await Traverse(disabled, repository, A.Id);
        string[] native;
        using (var enabled = Open(database, true))
            native = await Traverse(enabled, repository, A.Id);
        var after = ReadCensus(database);
        string[] second;
        using (var enabled = Open(database, true))
            second = await Traverse(enabled, repository, A.Id);
        var afterSecond = ReadCensus(database);
        var authoritativeAfter = ReadAuthoritative(database);
        var errors = ValidateRepository(
            after, repository.Value, ExpectedNodes(units), ExpectedEdges(units));
        if (!projection.SequenceEqual(native))
            errors.Add("projection and native traversal differ after recovery");
        if (!native.SequenceEqual(second))
            errors.Add("repeated recovery changed traversal");
        if (afterSecond.Nodes.Count != after.Nodes.Count ||
            afterSecond.Edges.Count != after.Edges.Count)
            errors.Add("repeated recovery changed physical cardinality");
        if (!authoritativeBefore.SequenceEqual(authoritativeAfter))
            errors.Add("authoritative rows changed during recovery");
        if (after.OccurrenceFacts != 1)
            errors.Add("occurrence facts not preserved");
        if (!SameRelated(before.Related, after.Related))
            errors.Add("unrelated sentinel objects or relationships changed");
        return new
        {
            command = "recover",
            pass = errors.Count == 0,
            errors,
            before,
            after,
            afterSecond,
            projection,
            native,
            second
        };
    }

    private static async Task<object> RecoverSealed(string database)
    {
        var repository = new CodeRepositoryId("review:s64:mirror");
        var sentinel = CreateSentinel(database);
        var before = ReadCensus(database);
        var authoritativeBefore = ReadAuthoritative(database);
        string[] projection;
        using (var disabled = Open(database, false))
            projection = await TraverseQualified(disabled, repository, "Shared/Linked.cs");
        string[] native;
        using (var enabled = Open(database, true))
            native = await TraverseQualified(enabled, repository, "Shared/Linked.cs");
        var after = ReadCensus(database);
        string[] second;
        using (var enabled = Open(database, true))
            second = await TraverseQualified(enabled, repository, "Shared/Linked.cs");
        var afterSecond = ReadCensus(database);
        var authoritativeAfter = ReadAuthoritative(database);
        var errors = new List<string>();
        if (after.Nodes.Count != after.Nodes.Select(node => node.LogicalId)
                .Distinct(StringComparer.Ordinal).Count())
            errors.Add("duplicate physical nodes after recovery");
        if (after.Edges.Count != after.Edges.Select(edge => edge.LogicalId)
                .Distinct(StringComparer.Ordinal).Count())
            errors.Add("duplicate physical edges after recovery");
        if (UnmappedNodes(after) != 0) errors.Add("unmapped physical nodes");
        if (UnmappedEdges(after) != 0) errors.Add("unmapped physical edges");
        if (after.Manifests.Sum(manifest => manifest.Nodes.Count) != after.Nodes.Count)
            errors.Add("manifest nodes do not cover physical nodes");
        if (after.Manifests.Sum(manifest => manifest.Edges.Count) != after.Edges.Count)
            errors.Add("manifest edges do not cover physical edges");
        if (!projection.SequenceEqual(native))
            errors.Add("projection and native traversal differ after recovery");
        if (!native.SequenceEqual(second))
            errors.Add("repeated recovery changed traversal");
        if (afterSecond.Nodes.Count != after.Nodes.Count ||
            afterSecond.Edges.Count != after.Edges.Count)
            errors.Add("repeated recovery changed physical cardinality");
        if (!authoritativeBefore.SequenceEqual(authoritativeAfter))
            errors.Add("authoritative rows changed during recovery");
        if (before.OccurrenceFacts != after.OccurrenceFacts)
            errors.Add("occurrence facts not preserved");
        if (!SameRelated(before.Related, after.Related))
            errors.Add("unrelated sentinel changed");
        return new
        {
            command = "recover-sealed",
            pass = errors.Count == 0,
            errors,
            sentinel,
            before,
            after,
            afterSecond,
            projection,
            native,
            second
        };
    }

    private static async Task<string[]> TraverseQualified(
        LatticeCodeGraphStore store,
        CodeRepositoryId repository,
        string qualifiedName)
    {
        var nodes = await store.FindNodesByQualifiedNameAsync(repository, qualifiedName);
        var edges = new List<string>();
        foreach (var node in nodes)
        {
            edges.AddRange((await store.TraverseAsync(
                repository,
                new CodeGraphTraversalQuery(
                    node.Id, CodeGraphDirection.Both,
                    maxDepth: 1, maxNodes: 100, maxEdges: 100)))
                .Edges.Select(edge => edge.Id.Value));
        }
        return edges.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static int UnmappedNodes(Census census) =>
        census.Nodes.Count(node => !census.Manifests.Any(
            manifest => manifest.Nodes.ContainsValue(node.NativeId)));

    private static int UnmappedEdges(Census census) =>
        census.Edges.Count(edge => !census.Manifests.Any(
            manifest => manifest.Edges.ContainsValue(edge.NativeId)));

    // --------------------------------------------------------------- fixtures

    private static CodeIndexUnitReplacement[] SharedUnits(
        CodeRepositoryId repository, string run) =>
    [
        Unit("unit:one", [A, B], [E]),
        Unit("unit:two", [A, B], [E])
    ];

    private static CodeIndexUnitReplacement[] WithOccurrence(
        CodeRepositoryId repository, CodeIndexUnitReplacement[] units)
    {
        var fact = Fact(repository, "unit:one");
        return units.Select(unit => unit.Origin.IndexUnitId.Value == "unit:one"
            ? new CodeIndexUnitReplacement(
                unit.Origin,
                unit.Nodes,
                declarations: unit.Declarations,
                edges: unit.Edges,
                occurrences: [fact],
                occurrenceAnalysisIncluded: true,
                occurrenceCoverage: new CodeOccurrenceCoverage(
                    1, 0, CodeRelationshipCoverageState.Produced))
            : unit).ToArray();
    }

    private static CodeIndexUnitReplacement Unit(
        string unit,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeGraphEdge> edges) =>
        new(new CodeFactOrigin(
            new CodeRepositoryId("repo:shared"), Plugin, "1.0.0",
            new CodeIndexRunId("run:one"), new CodeIndexUnitId(unit)),
            nodes, edges: edges);

    private static CodeOccurrenceFact Fact(CodeRepositoryId repository, string unit)
    {
        const string name = "qual-target";
        var context = "compctx1:sha256:" +
            Convert.ToHexStringLower(SHA256.HashData(
                Encoding.UTF8.GetBytes(repository.Value + "|" + name)));
        var target = CodeOccurrenceIdentity.ComputeTargetId(
            context, "meta:" + name, "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest(
                [$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
        return CodeOccurrenceFact.Create(
            repository, target, target.Value, unit,
            new CodeOccurrenceContributor(Plugin, unit),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation("src/Qual.cs", 1, 1, 1, 6),
            "sha256:qual-source", "content:qual");
    }

    private static CodeGraphNode Node(string id)
    {
        var name = id[(id.LastIndexOf(':') + 1)..];
        return new CodeGraphNode(
            new CodeNodeId(id), CodeNodeKinds.Type, name, "Example." + name,
            new CodeSymbolId("symbol:" + id));
    }

    private static CodeGraphEdge Edge(
        string id, CodeGraphNode source, CodeGraphNode target) =>
        new(new CodeEdgeId(id), source.Id, target.Id, CodeEdgeKinds.Calls,
            new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"));

    // --------------------------------------------------------------- oracle

    private static string[] ExpectedNodes(IEnumerable<CodeIndexUnitReplacement> units) =>
        units.SelectMany(unit => unit.Nodes)
            .Select(node => node.Id.Value).Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();

    private static string[] ExpectedEdges(IEnumerable<CodeIndexUnitReplacement> units)
    {
        var unitsArray = units.ToArray();
        var nodes = ExpectedNodes(unitsArray).ToHashSet(StringComparer.Ordinal);
        return unitsArray.SelectMany(unit => unit.Edges)
            .Where(edge => nodes.Contains(edge.SourceId.Value) &&
                nodes.Contains(edge.TargetId.Value))
            .Select(edge => edge.Id.Value).Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    private static List<string> ValidateRepository(
        Census census, string repository, string[] nodes, string[] edges)
    {
        var errors = new List<string>();
        ValidateManifest(census, repository, nodes, edges, errors);
        ValidateGlobal(census, nodes.Length, edges.Length, errors);
        return errors;
    }

    private static void ValidateManifest(
        Census census, string repository, string[] nodes, string[] edges,
        List<string> errors)
    {
        var manifest = census.Manifests.SingleOrDefault(
            candidate => candidate.Repository == repository);
        if (manifest is null)
        {
            errors.Add("missing manifest for " + repository);
            return;
        }

        if (!manifest.Nodes.Keys.OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(nodes.OrderBy(id => id, StringComparer.Ordinal)))
            errors.Add("manifest nodes differ for " + repository);
        if (!manifest.Edges.Keys.OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(edges.OrderBy(id => id, StringComparer.Ordinal)))
            errors.Add("manifest edges differ for " + repository);
        var byNative = census.Nodes.ToDictionary(node => node.NativeId);
        foreach (var (logical, native) in manifest.Nodes)
        {
            if (!byNative.TryGetValue(native, out var row) || row.LogicalId != logical)
                errors.Add("manifest node does not resolve: " + logical);
        }
        var edgeByNative = census.Edges.ToDictionary(edge => edge.NativeId);
        foreach (var (logical, native) in manifest.Edges)
        {
            if (!edgeByNative.TryGetValue(native, out var row) || row.LogicalId != logical)
                errors.Add("manifest edge does not resolve: " + logical);
        }
    }

    private static void ValidateGlobal(
        Census census, int nodes, int edges, List<string> errors)
    {
        if (census.Nodes.Count != nodes)
            errors.Add($"physical node count {census.Nodes.Count} != {nodes}");
        if (census.Edges.Count != edges)
            errors.Add($"physical edge count {census.Edges.Count} != {edges}");
        var mappedNodes = census.Manifests.Sum(manifest => manifest.Nodes.Count);
        var mappedEdges = census.Manifests.Sum(manifest => manifest.Edges.Count);
        if (mappedNodes != census.Nodes.Count)
            errors.Add("manifest node entries do not cover physical nodes");
        if (mappedEdges != census.Edges.Count)
            errors.Add("manifest edge entries do not cover physical edges");
        if (census.Nodes.Select(node => node.NativeId).Distinct().Count() != census.Nodes.Count)
            errors.Add("duplicate native mirror node id");
        if (census.Edges.Select(edge => edge.NativeId).Distinct().Count() != census.Edges.Count)
            errors.Add("duplicate native mirror edge id");
    }

    // ------------------------------------------------------------- raw access

    private sealed record CensusNode(string LogicalId, ulong NativeId);
    private sealed record CensusEdge(
        string LogicalId, ulong NativeId, ulong Source, ulong Target);
    private sealed record ManifestView(
        string Repository,
        Dictionary<string, ulong> Nodes,
        Dictionary<string, ulong> Edges);
    private sealed record RelatedEdge(
        ulong NativeId, ulong Source, ulong Target, string Type, string? Note);
    private sealed record RelatedNode(
        ulong NativeId, string Note, long Number, string[] Tags,
        List<RelatedEdge> Outgoing);
    private sealed record AuthoritativeRow(
        string Label, ulong NativeId, string Key, string Payload);
    private sealed record Census(
        List<CensusNode> Nodes,
        List<CensusEdge> Edges,
        List<ManifestView> Manifests,
        List<RelatedNode> Related,
        List<AuthoritativeRow> Authoritative,
        int OccurrenceFacts)
    {
        public object Summary => new
        {
            nodes = Nodes.Count,
            edges = Edges.Count,
            manifests = Manifests.Count,
            nodeLogical = Nodes.Select(node => node.LogicalId)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            edgeLogical = Edges.Select(edge => edge.LogicalId)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            occurrenceFacts = OccurrenceFacts
        };
    }

    private static Census ReadCensus(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var nodes = new List<CensusNode>();
        var edgeMap = new Dictionary<ulong, CensusEdge>();
        var mirrorNodes = transaction.GetNodesByLabel(NodeLabel);
        foreach (var node in mirrorNodes)
        {
            if (!transaction.TryGetProperty(node, JsonProperty, out var payload))
                continue;
            using var document = JsonDocument.Parse(payload.AsString());
            nodes.Add(new CensusNode(
                document.RootElement.GetProperty("Id").GetProperty("Value").GetString()!,
                node.Value));
        }
        foreach (var node in mirrorNodes)
        {
            foreach (var edge in transaction.GetOutgoingEdges(node))
            {
                if (!transaction.TryGetEdgeProperty(edge.Id, JsonProperty, out var payload))
                    continue;
                using var document = JsonDocument.Parse(payload.AsString());
                edgeMap[edge.Id.Value] = new CensusEdge(
                    document.RootElement.GetProperty("Id").GetProperty("Value").GetString()!,
                    edge.Id.Value, edge.Source.Value, edge.Target.Value);
            }
        }

        var manifests = new List<ManifestView>();
        foreach (var node in transaction.GetNodesByLabel(MirrorLabel))
        {
            if (!transaction.TryGetProperty(node, "key", out var key) ||
                !transaction.TryGetProperty(node, "payload", out var payload))
                continue;
            var json = JsonSerializer.Deserialize<string>(
                Convert.FromBase64String(payload.AsString()))!;
            using var document = JsonDocument.Parse(json);
            manifests.Add(new ManifestView(
                key.AsString(),
                ReadMap(document.RootElement.GetProperty("Nodes")),
                ReadMap(document.RootElement.GetProperty("Edges"))));
        }

        var related = new List<RelatedNode>();
        foreach (var node in transaction.GetNodesByLabel(UnrelatedLabel))
        {
            var outgoing = new List<RelatedEdge>();
            foreach (var edge in transaction.GetOutgoingEdges(node))
            {
                string? note = null;
                if (transaction.TryGetEdgeProperty(edge.Id, "note", out var noteValue))
                    note = noteValue.AsString();
                outgoing.Add(new RelatedEdge(
                    edge.Id.Value, edge.Source.Value, edge.Target.Value,
                    edge.Type, note));
            }
            var tags = Array.Empty<string>();
            if (transaction.TryGetProperty(node, "tags", out var tagValue))
                tags = tagValue.AsList().Select(value => value.AsString()).ToArray();
            related.Add(new RelatedNode(
                node.Value,
                transaction.TryGetProperty(node, "note", out var noteProperty)
                    ? noteProperty.AsString() : string.Empty,
                transaction.TryGetProperty(node, "number", out var numberProperty)
                    ? numberProperty.AsInt64() : 0,
                tags,
                outgoing.OrderBy(edge => edge.NativeId).ToList()));
        }

        var occurrenceFacts = 0;
        using (var query = database.Prepare(
                   "MATCH (n:AndiritOccurrenceUnit) RETURN n.payload"))
        using (var result = query.Execute(transaction))
        {
            foreach (var row in result.ReadAll())
            {
                using var document = JsonDocument.Parse(
                    Convert.FromBase64String(row.GetValue(0).AsString()));
                occurrenceFacts += document.RootElement.GetProperty("Facts").GetArrayLength();
            }
        }

        var census = new Census(
            nodes.OrderBy(node => node.NativeId).ToList(),
            edgeMap.Values.OrderBy(edge => edge.NativeId).ToList(),
            manifests.OrderBy(manifest => manifest.Repository, StringComparer.Ordinal).ToList(),
            related.OrderBy(node => node.NativeId).ToList(),
            ReadAuthoritative(transaction),
            occurrenceFacts);
        transaction.Commit();
        return census;
    }

    private static Dictionary<string, ulong> ReadMap(JsonElement element)
    {
        var map = new Dictionary<string, ulong>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            map[property.Name] = property.Value.GetUInt64();
        return map;
    }

    private static List<AuthoritativeRow> ReadAuthoritative(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var rows = ReadAuthoritative(transaction);
        transaction.Commit();
        return rows;
    }

    private static List<AuthoritativeRow> ReadAuthoritative(
        LatticeTransaction transaction)
    {
        var rows = new List<AuthoritativeRow>();
        foreach (var label in new[]
                 {
                     "HetuRepository", "HetuRun", "HetuUnit", "HetuIndexState",
                     "AndiritOccurrenceUnit", "AndiritOccurrenceMeta"
                 })
        {
            foreach (var node in transaction.GetNodesByLabel(label))
            {
                if (!transaction.TryGetProperty(node, "key", out var key) ||
                    !transaction.TryGetProperty(node, "payload", out var payload))
                    continue;
                rows.Add(new AuthoritativeRow(
                    label, node.Value, key.AsString(), payload.AsString()));
            }
        }
        return rows.OrderBy(row => row.Label, StringComparer.Ordinal)
            .ThenBy(row => row.Key, StringComparer.Ordinal)
            .ThenBy(row => row.NativeId)
            .ToList();
    }

    private static bool HasDanglingMirrorEdge(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var ids = transaction.GetNodesByLabel(NodeLabel)
            .Select(node => node.Value).ToHashSet();
        var dangling = false;
        foreach (var node in transaction.GetNodesByLabel(NodeLabel))
        {
            foreach (var edge in transaction.GetOutgoingEdges(node))
            {
                if (!transaction.TryGetEdgeProperty(edge.Id, JsonProperty, out _))
                    continue;
                if (!ids.Contains(edge.Source.Value) ||
                    !ids.Contains(edge.Target.Value))
                    dangling = true;
            }
        }
        transaction.Commit();
        return dangling;
    }

    // ------------------------------------------------------------ raw writes

    private sealed record ManifestDto(
        Dictionary<string, ulong> Nodes, Dictionary<string, ulong> Edges);

    private static void WriteManifest(
        string path, string repository,
        Dictionary<string, ulong> nodes, Dictionary<string, ulong> edges)
    {
        var json = JsonSerializer.Serialize(new ManifestDto(nodes, edges));
        var payload = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(json)));
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var manifest = transaction.GetNodesByLabel(MirrorLabel)
            .Single(node => transaction.TryGetProperty(node, "key", out var key) &&
                key.AsString() == repository);
        transaction.SetProperty(manifest, "payload", LatticeValue.From(payload));
        transaction.Commit();
    }

    private static (ulong NativeId, string Note, long Number) CreateUnrelatedNode(
        string path, string note, long number)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var node = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(node, "note", LatticeValue.From(note));
        transaction.SetProperty(node, "number", LatticeValue.From(number));
        transaction.Commit();
        return (node.Value, note, number);
    }

    private sealed record Incident(
        ulong NodeId, ulong MirrorId, ulong EdgeId);

    private static Incident CreateIncidentEdge(
        string path, string note, ulong mirrorId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var node = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(node, "note", LatticeValue.From(note));
        var edge = transaction.CreateEdge(
            node, new LatticeNodeId(mirrorId), IncidentType);
        transaction.SetEdgeProperty(edge, "note", LatticeValue.From(note));
        transaction.Commit();
        return new Incident(node.Value, mirrorId, edge.Value);
    }

    private static void DeleteIncidentEdge(string path, Incident incident)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        transaction.DeleteEdge(
            new LatticeNodeId(incident.NodeId),
            new LatticeNodeId(incident.MirrorId),
            IncidentType);
        transaction.Commit();
    }

    private sealed record ForeignEdge(
        ulong NodeId, ulong MirrorId, ulong EdgeId);

    private static ulong CreateProtocolOrphan(string path, string logicalId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var node = transaction.CreateNode(NodeLabel);
        transaction.SetProperty(node, JsonProperty, LatticeValue.From(
            JsonSerializer.Serialize(new { Id = new { Value = logicalId } })));
        transaction.Commit();
        return node.Value;
    }

    private static ForeignEdge CreateForeignOutgoing(
        string path, string note, ulong mirrorId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var node = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(node, "note", LatticeValue.From(note));
        var edge = transaction.CreateEdge(
            new LatticeNodeId(mirrorId), node, "QualForeignOut");
        transaction.SetEdgeProperty(edge, "note", LatticeValue.From(note));
        transaction.Commit();
        return new ForeignEdge(node.Value, mirrorId, edge.Value);
    }

    private static void DeleteEdge(string path, ForeignEdge edge)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        transaction.DeleteEdge(
            new LatticeNodeId(edge.MirrorId),
            new LatticeNodeId(edge.NodeId),
            "QualForeignOut");
        transaction.Commit();
    }

    private static bool EdgeExists(string path, ulong edgeId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var exists = transaction.GetNodesByLabel(NodeLabel)
            .SelectMany(node => transaction.GetOutgoingEdges(node))
            .Any(edge => edge.Id.Value == edgeId);
        transaction.Commit();
        return exists;
    }

    private static void DeleteManifest(string path, string repository)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var manifest = transaction.GetNodesByLabel(MirrorLabel)
            .Single(node => transaction.TryGetProperty(node, "key", out var key) &&
                key.AsString() == repository);
        transaction.DeleteNode(manifest);
        transaction.Commit();
    }

    private static void CorruptManifest(
        string path, string repository, string value)
    {
        var payload = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var manifest = transaction.GetNodesByLabel(MirrorLabel)
            .Single(node => transaction.TryGetProperty(node, "key", out var key) &&
                key.AsString() == repository);
        transaction.SetProperty(manifest, "payload", LatticeValue.From(payload));
        transaction.Commit();
    }

    private sealed record Sentinel(
        ulong First, ulong Second, ulong Edge);

    private static Sentinel CreateSentinel(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var first = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(first, "note", LatticeValue.From("sentinel-one"));
        transaction.SetProperty(first, "number", LatticeValue.From(7L));
        transaction.SetProperty(first, "tags", LatticeValue.From(
            new LatticeValue[] { LatticeValue.From("x"), LatticeValue.From("y") }));
        var second = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(second, "note", LatticeValue.From("sentinel-two"));
        transaction.SetProperty(second, "number", LatticeValue.From(9L));
        var edge = transaction.CreateEdge(first, second, "QualSentinel");
        transaction.SetEdgeProperty(edge, "note", LatticeValue.From("sentinel-edge"));
        transaction.Commit();
        return new Sentinel(first.Value, second.Value, edge.Value);
    }

    // ------------------------------------------------------------ store use

    private static LatticeCodeGraphStore Open(string path, bool enabled) =>
        new(path, new LatticeDbStoreOptions { UseNativeTraversal = enabled });

    private static async Task Publish(
        LatticeCodeGraphStore store,
        CodeRepositoryId repository,
        string run,
        IReadOnlyList<CodeIndexUnitReplacement> units)
    {
        var runId = new CodeIndexRunId(run);
        var started = Started.AddMinutes(run.GetHashCode(StringComparison.Ordinal) & 0x3ff);
        await store.UpsertRepositoryAsync(new(repository));
        await store.StoreIndexRunAsync(
            new(repository, runId, started, plugins: [Plugin]));
        foreach (var unit in units)
        {
            await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                new CodeFactOrigin(
                    repository, Plugin, "1.0.0", runId,
                    unit.Origin.IndexUnitId),
                unit.Nodes,
                declarations: unit.Declarations,
                edges: unit.Edges,
                occurrences: unit.Occurrences,
                occurrenceAnalysisIncluded: unit.OccurrenceAnalysisIncluded,
                occurrenceCoverage: unit.OccurrenceCoverage));
        }
        await store.CompleteIndexRunAsync(
            new(repository, runId, started, CodeIndexRunStatus.Completed,
                started.AddSeconds(1), [Plugin]),
            new(repository, runId, []));
    }

    private static async Task<string[]> Traverse(
        LatticeCodeGraphStore store,
        CodeRepositoryId repository,
        CodeNodeId start) =>
        (await store.TraverseAsync(
            repository,
            new CodeGraphTraversalQuery(
                start, CodeGraphDirection.Both,
                maxDepth: 1, maxNodes: 100, maxEdges: 100)))
        .Edges.Select(edge => edge.Id.Value).ToArray();

    private static bool SameRelated(
        IReadOnlyList<RelatedNode> left, IReadOnlyList<RelatedNode> right)
    {
        if (left.Count != right.Count)
            return false;
        for (var index = 0; index < left.Count; index++)
        {
            var first = left[index];
            var second = right[index];
            if (first.NativeId != second.NativeId ||
                first.Note != second.Note ||
                first.Number != second.Number ||
                !first.Tags.SequenceEqual(second.Tags) ||
                first.Outgoing.Count != second.Outgoing.Count)
                return false;
            for (var edge = 0; edge < first.Outgoing.Count; edge++)
            {
                var a = first.Outgoing[edge];
                var b = second.Outgoing[edge];
                if (a.NativeId != b.NativeId || a.Source != b.Source ||
                    a.Target != b.Target || a.Type != b.Type || a.Note != b.Note)
                    return false;
            }
        }
        return true;
    }
}
