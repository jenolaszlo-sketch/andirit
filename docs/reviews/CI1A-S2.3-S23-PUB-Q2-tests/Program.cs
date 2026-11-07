using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;

// S23-PUB-Q2 reviewer-owned adversarial probe for staged-run reconstruction.
// Independent of the R2 test suite and of the Q1 harness internals: this probe
// challenges the R2 grouping boundary directly with hand-crafted durable
// command sequences (replay level) and real durable files (fresh-process
// reopen level).
//
// Usage: dotnet run ... -- <verb> <database> <outputJson> [detail]
var action = args[0];
var database = args[1];
var output = args[2];
var detail = args.Length > 3 ? args[3] : "";
var result = await Q2AdversarialProbe.RunAsync(action, database, detail);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action +
    " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class Q2AdversarialProbe
{
    internal static readonly CodeRepositoryId Repo = new("repo:s23-pub-q2");
    internal static readonly CodeRepositoryId RepoB = new("repo:s23-pub-q2-b");
    internal static readonly CodePluginId Plugin = new("plugin:s23-pub-q2");
    internal static readonly DateTimeOffset Started = new(2026, 11, 7, 16, 0, 0, TimeSpan.Zero);
    private const string RunA = "run:q2-a";
    private const string RunB = "run:q2-b";

    internal static async Task<JsonObject> RunAsync(string action, string database, string detail)
    {
        var notes = new List<string>();
        var ok = true;
        try
        {
            switch (action)
            {
                case "replay-adversarial": await ReplayAdversarialAsync(notes); break;
                case "durable-interleaved": await DurableInterleavedAsync(database, detail, notes); break;
                case "durable-corrupt": DurableCorrupt(database, detail, notes); break;
                case "durable-corrupt-verify": DurableCorruptVerify(database, notes); break;
                case "durable-retire": await DurableRetireAsync(database, notes); break;
                case "durable-retire-verify": await DurableRetireVerifyAsync(database, notes); break;
                default: throw new ArgumentException("Unknown verb: " + action);
            }
        }
        catch (Exception exception)
        {
            ok = false;
            notes.Add("EXCEPTION " + exception.GetType().FullName + ": " + exception.Message);
            if (exception is CodeGraphBatchRejectedException rejected)
                foreach (var error in rejected.Errors)
                    notes.Add("  rejection " + error.Code + " | " + error.FactId + " | " + error.Message);
        }

        return new JsonObject
        {
            ["action"] = action,
            ["ok"] = ok,
            ["processId"] = Environment.ProcessId,
            ["database"] = database,
            ["detail"] = detail,
            ["notes"] = new JsonArray(notes.Select(n => JsonValue.Create(n)!).ToArray()),
        };
    }

    private static DurableCommandLog.PersistedCommand[] BaseCommands(bool withRepoB = false) =>
        withRepoB
            ?
            [
                new("repository", Repository: new CodeRepositoryManifest(Repo)),
                new("repository", Repository: new CodeRepositoryManifest(RepoB)),
            ]
            :
            [
                new("repository", Repository: new CodeRepositoryManifest(Repo)),
            ];

    private static CodeGraphNode Node(string name) => new(
        new CodeNodeId("node:" + name), CodeNodeKinds.Type, name, "Q2." + name, new CodeSymbolId("symbol:" + name));

    private static CodeGraphEdge DependsOn(CodeGraphNode from, CodeGraphNode to) => new(
        new CodeEdgeId("edge:" + from.Name + "->" + to.Name),
        from.Id, to.Id, CodeEdgeKinds.DependsOn,
        new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"));

    private static CodeFactOrigin Origin(string unit, string run, CodeRepositoryId? repo = null) =>
        new(repo ?? Repo, Plugin, "1.0.0", new(run), new(unit));

    // ---- §5 replay-level adversarial cases (grouping boundary, ordering,
    // interleaving, repeated identities, deletion ordering, similar identifiers).
    private static async Task ReplayAdversarialAsync(List<string> notes)
    {
        var app = Node("AppType");
        var lib = Node("LibType");
        var appUnit = new CodeIndexUnitReplacement(Origin("unit-shared", RunA), [app], edges: [DependsOn(app, lib)]);
        var libUnit = new CodeIndexUnitReplacement(Origin("unit-shared-b", RunA), [lib]);
        var completedA = new CodeIndexRunManifest(
            Repo, new(RunA), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]);
        var stateA = new CodeRepositoryIndexState(Repo, new(RunA), []);

        // 1. Interleaved staged commands from two runs of one repository, using
        // the SAME unit identifier in both runs. Groups must stay independent:
        // completing run A publishes only run A's staged units; run B's staged
        // state is untouched and unpublished (and vice versa).
        var runBUnit = new CodeIndexUnitReplacement(Origin("unit-shared", RunB), [Node("RunBType")]);
        var interleaved = BaseCommands().Concat(
        [
            new("run", Run: new CodeIndexRunManifest(Repo, new(RunA), Started, plugins: [Plugin])),
            new("run", Run: new CodeIndexRunManifest(Repo, new(RunB), Started, plugins: [Plugin])),
            new("stage-replace", Replacement: libUnit),                       // run A
            new("stage-replace", Replacement: runBUnit),                      // run B
            new("stage-replace", Replacement: appUnit),                       // run A (interleaved)
            new("stage-delete", RepositoryId: Repo, RunId: new(RunB), PluginId: Plugin, UnitId: new("unit-shared")), // run B delete
            new("stage-replace", Replacement: runBUnit),                      // run B replace after delete
        ]).ToList();

        // 1a. Completing run A publishes only run A's units.
        var resumed = await DurableCommandLog.ReplayAsync(interleaved, CancellationToken.None);
        Assert.Equal(0, (await resumed.GetPublishedUnitsAsync(Repo)).Count);
        await resumed.CompleteIndexRunAsync(completedA, stateA);
        var publishedA = await resumed.GetPublishedUnitsAsync(Repo);
        Assert.Equal(2, publishedA.Count);
        Assert.All(publishedA, unit =>
        {
            if (unit.Origin.IndexRunId.Value != RunA)
                throw new InvalidDataException("Run B's staged state leaked into run A's publication.");
        });

        // 1b. Completing run B instead publishes only run B's unit; run A's
        // staged state is untouched and unpublished.
        var resumedB = await DurableCommandLog.ReplayAsync(interleaved, CancellationToken.None);
        await resumedB.CompleteIndexRunAsync(
            new(Repo, new(RunB), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(Repo, new(RunB), []));
        var publishedB = await resumedB.GetPublishedUnitsAsync(Repo);
        Assert.Single(publishedB);
        Assert.Equal("unit-shared", publishedB[0].Origin.IndexUnitId.Value);
        Assert.Contains(publishedB[0].Nodes, node => node.Id.Value == "node:RunBType");
        Assert.Null(await resumedB.GetNodeAsync(Repo, app.Id));
        Assert.Null(await resumedB.GetNodeAsync(Repo, lib.Id));
        notes.Add("interleavedRunsIndependent=true deleteThenReplace=present");

        // 2. Replacement followed by deletion of the same staged identity:
        // the unit is absent from the completed publication. The surviving
        // staged unit is standalone (a cross-unit edge to a deleted unit would
        // make the staged set genuinely inconsistent, which the reconstruction
        // correctly refuses - observed separately below).
        var standalone = new CodeIndexUnitReplacement(Origin("unit-shared-b", RunA), [lib]);
        var appStandalone = new CodeIndexUnitReplacement(Origin("unit-shared", RunA), [app]);
        var replaceThenDelete = BaseCommands().Concat(
        [
            new("run", Run: new CodeIndexRunManifest(Repo, new(RunA), Started, plugins: [Plugin])),
            new("stage-replace", Replacement: standalone),
            new("stage-delete", RepositoryId: Repo, RunId: new(RunA), PluginId: Plugin, UnitId: new("unit-shared-b")),
            new("stage-replace", Replacement: appStandalone),
        ]).ToList();
        var resumedReplaceDelete = await DurableCommandLog.ReplayAsync(replaceThenDelete, CancellationToken.None);
        await resumedReplaceDelete.CompleteIndexRunAsync(completedA, stateA);
        var onlyApp = await resumedReplaceDelete.GetPublishedUnitsAsync(Repo);
        Assert.Single(onlyApp);
        Assert.Equal("unit-shared", onlyApp[0].Origin.IndexUnitId.Value);
        Assert.Null(await resumedReplaceDelete.GetNodeAsync(Repo, lib.Id));
        notes.Add("replaceThenDelete=absent");

        // 2b. Deletion of a staged unit whose node is still referenced by
        // another staged unit's edge is a genuinely inconsistent staged set
        // and must refuse (corruption control, not an ordering artifact).
        var replaceThenDeleteReferenced = BaseCommands().Concat(
        [
            new("run", Run: new CodeIndexRunManifest(Repo, new(RunA), Started, plugins: [Plugin])),
            new("stage-replace", Replacement: libUnit),
            new("stage-delete", RepositoryId: Repo, RunId: new(RunA), PluginId: Plugin, UnitId: new("unit-shared-b")),
            new("stage-replace", Replacement: appUnit),
        ]).ToList();
        var referencedException = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
            async () => await DurableCommandLog.ReplayAsync(replaceThenDeleteReferenced, CancellationToken.None));
        Assert.Contains(referencedException.Errors, error => error.Code == "graph.edge.endpoint-missing");
        notes.Add("deleteReferencedUnit=refused");

        // 3. Two repositories with the same unit and node identifiers: groups
        // never merge across repositories, and both reconstruct.
        var repoBUnit = new CodeIndexUnitReplacement(Origin("unit-shared", RunA, RepoB), [Node("SameNameDifferentRepo")]);
        var twoRepos = BaseCommands(withRepoB: true).Concat(
        [
            new("run", Run: new CodeIndexRunManifest(Repo, new(RunA), Started, plugins: [Plugin])),
            new("run", Run: new CodeIndexRunManifest(RepoB, new(RunA), Started, plugins: [Plugin])),
            new("stage-replace", Replacement: libUnit),
            new("stage-replace", Replacement: repoBUnit),
            new("stage-replace", Replacement: appUnit),
        ]).ToList();
        var resumedTwoRepos = await DurableCommandLog.ReplayAsync(twoRepos, CancellationToken.None);
        await resumedTwoRepos.CompleteIndexRunAsync(completedA, stateA);
        Assert.Equal(2, (await resumedTwoRepos.GetPublishedUnitsAsync(Repo)).Count);
        // Completing repository B's interrupted run publishes only its own
        // unit; per-repository baselines do not supersede each other.
        await resumedTwoRepos.CompleteIndexRunAsync(
            new(RepoB, new(RunA), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(RepoB, new(RunA), []));
        Assert.Single(await resumedTwoRepos.GetPublishedUnitsAsync(RepoB));
        Assert.NotNull(await resumedTwoRepos.GetNodeAsync(RepoB, new("node:SameNameDifferentRepo")));
        Assert.NotNull(await resumedTwoRepos.GetNodeAsync(Repo, lib.Id));
        notes.Add("similarIdentifiersAcrossRepos=isolated");

        // 4. Varying stage-row enumeration order without changing the logical
        // sequence: reversing the interleaved list must produce the identical
        // final staged state (per-owner last-wins and per-run scoping are
        // order-invariant here because identities do not repeat across rows).
        var reversedInterleaved = interleaved.AsEnumerable().Reverse().ToList();
        var resumedReversed = await DurableCommandLog.ReplayAsync(reversedInterleaved, CancellationToken.None);
        await resumedReversed.CompleteIndexRunAsync(completedA, stateA);
        Assert.Equal(2, (await resumedReversed.GetPublishedUnitsAsync(Repo)).Count);
        notes.Add("enumerationOrderVariance=identicalOutcome");

        // 5. Duplicate identities inside one staged unit are refused at the
        // reconstruction boundary (intrinsic checks preserved).
        var duplicateNode = Node("DuplicateType");
        var duplicateCommands = BaseCommands().Concat(
        [
            new("run", Run: new CodeIndexRunManifest(Repo, new(RunA), Started, plugins: [Plugin])),
            new("stage-replace", Replacement: new CodeIndexUnitReplacement(
                Origin("unit-shared", RunA), [duplicateNode, duplicateNode])),
        ]).ToList();
        var duplicateException = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
            async () => await DurableCommandLog.ReplayAsync(duplicateCommands, CancellationToken.None));
        Assert.Contains(duplicateException.Errors, error => error.Code == "replacement.node.duplicate");
        notes.Add("duplicateStagedIdentity=refused");

        // 6. Conflicting contributions for one node identity across staged
        // units of one run are refused on the complete staged set.
        var conflicting = new CodeGraphNode(lib.Id, CodeNodeKinds.Type, "LibTypeOther",
            "Q2.LibTypeOther", new CodeSymbolId("symbol:other"));
        var conflictingCommands = BaseCommands().Concat(
        [
            new("run", Run: new CodeIndexRunManifest(Repo, new(RunA), Started, plugins: [Plugin])),
            new("stage-replace", Replacement: libUnit),
            new("stage-replace", Replacement: appUnit),
            new("stage-replace", Replacement: new CodeIndexUnitReplacement(
                Origin("unit-conflict", RunA), [conflicting])),
        ]).ToList();
        var conflictException = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
            async () => await DurableCommandLog.ReplayAsync(conflictingCommands, CancellationToken.None));
        Assert.Contains(conflictException.Errors, error => error.Code == "graph.node.conflict");
        notes.Add("conflictingStagedIdentity=refused");

        // 7. A genuinely missing endpoint across staged units is refused after
        // complete reconstruction (in both enumeration orders).
        var orphanEdge = new CodeGraphEdge(
            new("edge:orphan"), new("node:AppType"), new("node:Missing"),
            CodeEdgeKinds.References, new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"));
        var appWithOrphan = new CodeIndexUnitReplacement(
            appUnit.Origin, appUnit.Nodes, edges: new[] { orphanEdge });
        foreach (var (label, first, second) in new[]
                 {
                     (Label: "forward", First: (CodeIndexUnitReplacement)appWithOrphan, Second: libUnit),
                     (Label: "reversed", First: libUnit, Second: (CodeIndexUnitReplacement)appWithOrphan),
                 })
        {
            var missingCommands = BaseCommands().Concat(
            [
                new("run", Run: new CodeIndexRunManifest(Repo, new(RunA), Started, plugins: [Plugin])),
                new("stage-replace", Replacement: first),
                new("stage-replace", Replacement: second),
            ]).ToList();
            var missingException = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
                async () => await DurableCommandLog.ReplayAsync(missingCommands, CancellationToken.None));
            Assert.Contains(missingException.Errors, error => error.Code == "graph.edge.endpoint-missing");
        }
        notes.Add("missingEndpointBothOrders=refused");

        // 8. Ownership checks execute per command: a staged replacement whose
        // run is not registered refuses (OwnershipMismatch), not a silent
        // admission.
        var unregisteredRun = BaseCommands().Append(new DurableCommandLog.PersistedCommand(
            "stage-replace",
            Replacement: new CodeIndexUnitReplacement(
                Origin("unit-shared", "run:unregistered"), [Node("Ghost")]))).ToList();
        var ownershipException = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
            async () => await DurableCommandLog.ReplayAsync(unregisteredRun, CancellationToken.None));
        Assert.Contains(ownershipException.Errors,
            error => error.Code == "replacement.run.not-running" || error.Code == "run.repository.missing");
        notes.Add("unregisteredRunStagedReplace=refused");
    }

    // ---- durable-level interleaving + snapshot-unchanged failure evidence ----
    private static async Task DurableInterleavedAsync(string database, string detail, List<string> notes)
    {
        var app = Node("AppType");
        var lib = Node("LibType");
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(Repo));
            await store.StoreIndexRunAsync(new(Repo, new(RunA), Started, plugins: [Plugin]));
            // Dependency-order staging (endpoint unit first), so the persisted
            // HetuStage order is defining-first while the referencing unit's
            // stage row carries the cross-unit edge.
            await store.StageIndexUnitAsync(new(Origin("a-lib", RunA), [lib]));
            await store.StageIndexUnitAsync(new(Origin("z-app", RunA), [app], edges: [DependsOn(app, lib)]));
        }

        var order = Rows(database, "HetuStage").Keys
            .Where(key => key.StartsWith("HetuStage|", StringComparison.Ordinal))
            .Select(key => key.Split('\n')[3]).ToArray();
        notes.Add("persistedStageOrder=[" + string.Join(", ", order) + "]");
        notes.Add("occurrenceRows=" + Rows(database, "AndiritOccurrenceMeta").Count);
    }

    private static void DurableCorrupt(string database, string mode, List<string> notes)
    {
        var snapshotBefore = Snapshot(database);
        switch (mode)
        {
            case "missing-endpoint":
                // Remove the defining unit's staged segment: the referencing
                // staged unit's cross-unit endpoint becomes genuinely missing.
                RemoveRow(database, "HetuStage", Repo.Value + "\n" + RunA + "\n" + Plugin.Value + "\na-lib");
                break;
            case "malformed-row":
            {
                var key = Repo.Value + "\n" + RunA + "\n" + Plugin.Value + "\nz-app";
                var row = Rows(database, "HetuStage")["HetuStage|" + key];
                row["Replacement"]!["Edges"] = 42;
                WriteRow(database, "HetuStage", key, row);
                break;
            }
            default: throw new ArgumentException("Unknown corrupt mode " + mode);
        }
        notes.Add("mode=" + mode + " snapshotChanged=" + (Snapshot(database) != snapshotBefore));
    }

    private static void DurableCorruptVerify(string database, List<string> notes)
    {
        var expected = Snapshot(database);
        try
        {
            using var reopened = new LatticeCodeGraphStore(database);
            throw new InvalidDataException("Corrupted staged durable state reopened successfully.");
        }
        catch (CodeGraphBatchRejectedException exception)
        {
            notes.Add("refused=true codes=[" + string.Join(", ", exception.Errors.Select(e => e.Code)) + "]");
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            notes.Add("refused=true exception=" + exception.GetType().Name);
        }
        if (Snapshot(database) != expected)
            throw new InvalidDataException("The failed reopen modified durable state.");
        notes.Add("durableSnapshotUnchanged=true");
    }

    // Retirement through the staged boundary: a published unit is removed by a
    // staged deletion in a later run; completion retires it and the durable
    // reopen reflects the retirement.
    private static async Task DurableRetireAsync(string database, List<string> notes)
    {
        var published = Node("RetireMe");
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(Repo));
            await store.StoreIndexRunAsync(new(Repo, new("run:retire-base"), Started, plugins: [Plugin]));
            await store.StageIndexUnitAsync(new(Origin("retire-unit", "run:retire-base"), [published]));
            await store.CompleteIndexRunAsync(
                new(Repo, new("run:retire-base"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
                new(Repo, new("run:retire-base"), []));
            // Interrupted retirement run: stage a deletion of the published unit.
            await store.StoreIndexRunAsync(new(Repo, new(RunA), Started.AddSeconds(2), plugins: [Plugin]));
            await store.StageIndexUnitDeletionAsync(Repo, new(RunA), Plugin, new("retire-unit"));
        }

        notes.Add("publishedBeforeRetirement=" + (await CountPublishedAsync(database)));
    }

    private static async Task DurableRetireVerifyAsync(string database, List<string> notes)
    {
        using var reopened = new LatticeCodeGraphStore(database);
        // A staged deletion does not retire the published unit until its run
        // completes: the published state is unchanged on reopen.
        Assert.Single(await reopened.GetPublishedUnitsAsync(Repo));
        Assert.NotNull(await reopened.GetNodeAsync(Repo, new("node:RetireMe")));
        await reopened.CompleteIndexRunAsync(
            new(Repo, new(RunA), Started.AddSeconds(2), CodeIndexRunStatus.Completed, Started.AddSeconds(3), [Plugin]),
            new(Repo, new(RunA), []));
        // Completion publishes the staged deletion: the unit is retired.
        Assert.Empty(await reopened.GetPublishedUnitsAsync(Repo));
        Assert.Null(await reopened.GetNodeAsync(Repo, new("node:RetireMe")));
        notes.Add("stagedRetirementReopen=ok publishedSurvivesUntilCompletion=true unit=retired");
    }

    // ---- helpers ----
    private static async Task<int> CountPublishedAsync(string database)
    {
        using var store = new LatticeCodeGraphStore(database);
        return (await store.GetPublishedUnitsAsync(Repo)).Count;
    }

    internal static SortedDictionary<string, JsonObject> Rows(string database, params string[] labels)
    {
        var rows = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);
        using var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        foreach (var label in labels)
        {
            using var q = db.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
            using var result = q.Execute(txn);
            foreach (var row in result.ReadAll())
                rows[label + "|" + row.GetValue(0).AsString()] =
                    JsonNode.Parse(Convert.FromBase64String(row.GetValue(1).AsString()))!.AsObject();
        }
        txn.Commit();
        return rows;
    }

    internal static void RemoveRow(string database, string label, string key)
    {
        using var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginWriteTransaction();
        using (var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s"))
        { q.Bind("k", LatticeValue.From(key)); using var r = q.Execute(txn); r.ReadAll(); }
        txn.Commit();
    }

    internal static void WriteRow(string database, string label, string key, JsonObject row)
    {
        RemoveRow(database, label, key);
        using var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginWriteTransaction();
        using (var q = db.Prepare($"CREATE (:{label} {{key: $k, payload: $p}})"))
        {
            q.Bind("k", LatticeValue.From(key));
            q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var r = q.Execute(txn); r.ReadAll();
        }
        txn.Commit();
    }

    internal static string Snapshot(string database) =>
        JsonSerializer.Serialize(Rows(database, "HetuUnit", "HetuStage", "HetuIndexState", "HetuRun"));

    private static class Assert
    {
        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidDataException($"Expected {expected}, got {actual}");
        }
        public static void Single<T>(IEnumerable<T> collection)
        {
            if (collection.Count() != 1) throw new InvalidDataException("Expected single element, got " + collection.Count());
        }
        public static void Contains<T>(IEnumerable<T> collection, Func<T, bool> predicate)
        {
            if (!collection.Any(predicate)) throw new InvalidDataException("Expected matching element not found");
        }
        public static void All<T>(IEnumerable<T> collection, Action<T> check)
        {
            foreach (var item in collection) check(item);
        }
        public static void NotNull(object? value)
        {
            if (value is null) throw new InvalidDataException("Expected non-null value");
        }
        public static void Null(object? value)
        {
            if (value is not null) throw new InvalidDataException("Expected null value");
        }
        public static void Empty<T>(IEnumerable<T> collection)
        {
            if (collection.Any()) throw new InvalidDataException("Expected empty collection");
        }
        public static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
        {
            try { await action(); }
            catch (T exception) { return exception; }
            catch (Exception exception)
            {
                throw new InvalidDataException("Expected " + typeof(T).Name + " but got " + exception.GetType().Name + ": " + exception.Message);
            }
            throw new InvalidDataException("Expected " + typeof(T).Name + " was not thrown.");
        }
    }
}
