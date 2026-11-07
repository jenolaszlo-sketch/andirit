using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Frozen = AdrCi1Exp.Shared;

// S23-PUB-Q1 reviewer-owned process harness. Independent of implementation test
// sources and of the R1 evidence: every phase reconstructs live authority from
// the fixture sources in this file and asserts against accepted identity
// helpers, not against producer/R1 outputs. Each verb is invoked as its own
// process, so fresh-process boundaries are real OS process boundaries.
//
// Usage: dotnet run --project ... -c Release -- <verb> <database> <outputJson> [detail]
var action = args[0];
var database = args[1];
var output = args[2];
var detail = args.Length > 3 ? args[3] : "";
var result = await S23PubQ1Probe.RunAsync(action, database, detail);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action +
    " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class S23PubQ1Probe
{
    internal static readonly CodeRepositoryId Repo = new("repo:s23-pub-q1");
    internal static readonly CodePluginId Plugin = new("probe.s23-pub-q1");
    internal static readonly DateTimeOffset Started = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    // Cross-project fixture: defining (Lib) <- referencing (App). The Lib body
    // can be varied for stale-dependency controls; the project names can be
    // varied to control the persisted unit enumeration order (unit ids are
    // sha256(project path) per the plugin's public identity convention).
    internal static (IReadOnlyDictionary<string, string> Files, string DefiningPath, string ReferencingPath) CrossProject(
        string libBody = "public static int Get() => 1;", string naming = "default")
    {
        var (defining, referencing) = naming == "defining-first" ? ("OLib", "MApp") : ("QLib", "QApp");
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"{defining}/{defining}.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            [$"{defining}/Value.cs"] = $"namespace QLib; public static class Value {{ {libBody} }}",
            [$"{referencing}/{referencing}.csproj"] =
                $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../{defining}/{defining}.csproj\" /></ItemGroup></Project>",
            [$"{referencing}/Application.cs"] =
                "namespace QApp; public static class Application { public static int Run() => QLib.Value.Get(); }",
        };
        return (files, $"{defining}/{defining}.csproj", $"{referencing}/{referencing}.csproj");
    }

    internal static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(Repo, null, new CodeIndexRunId(run),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key,
                    "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

    // Independent expected-target recomputation: the frozen R2 authority and the
    // accepted identity primitive must agree with each other before the probe
    // asserts anything against the publication.
    internal static CodeOccurrenceTargetId ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        var identifier = CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]);
        var declaration = CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]);
        var shape = CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
            string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
        var viaIdentity = CodeOccurrenceIdentity.ComputeTargetId(
            mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName, identifier, declaration, shape);
        var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var viaFrozen = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, identifier, declaration, shape]);
        if (viaIdentity.Value != viaFrozen)
            throw new InvalidDataException("Independent target computations disagree: " + viaIdentity.Value + " vs " + viaFrozen);
        return viaIdentity;
    }

    internal static IMethodSymbol Method(VerifiedCSharpCompilation unit, string type, string name) =>
        (IMethodSymbol)unit.Compilation.GetTypeByMetadataName(type)!.GetMembers(name).Single();

    internal static async Task<(VerifiedCSharpAnalysis Analysis, VerifiedCSharpCompilation App, VerifiedCSharpCompilation Lib)>
        LiveAsync(CodeGraphPluginContext context, string naming = "default")
    {
        var (_, definingPath, referencingPath) = CrossProject(naming: naming);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        return (analysis,
            analysis.Projects.Single(p => p.Project.Path == referencingPath).Verified!,
            analysis.Projects.Single(p => p.Project.Path == definingPath).Verified!);
    }

    // Real publication through the accepted chain: plugin session -> sink ->
    // StageIndexUnitAsync -> CompleteIndexRunAsync.
    internal static async Task<LatticeCodeGraphStore> PublishRealAsync(string database, string run,
        string libBody = "public static int Get() => 1;", string naming = "default")
    {
        var context = Context(run, CrossProject(libBody, naming).Files);
        var plugin = new CSharpCodeGraphPlugin();
        var store = new LatticeCodeGraphStore(database);
        await store.UpsertRepositoryAsync(new(Repo));
        await store.StoreIndexRunAsync(new(Repo, new(run), Started, plugins: [plugin.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }
        await store.CompleteIndexRunAsync(
            new(Repo, new(run), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
            new(Repo, new(run), []));
        return store;
    }

    internal static async Task<JsonObject> RunAsync(string action, string database, string detail)
    {
        var notes = new List<string>();
        var ok = true;
        try
        {
            switch (action)
            {
                case "publish": await PublishAsync(database, notes); break;
                case "verify": await VerifyAsync(database, notes); break;
                case "graphonly-publish": await GraphOnlyPublishAsync(database, detail, notes); break;
                case "graphonly-verify": await GraphOnlyVerifyAsync(database, notes); break;
                case "corrupt": Corrupt(database, detail, notes); break;
                case "corrupt-verify": CorruptVerify(database, notes); break;
                case "segment-stage": await SegmentStageAsync(database, detail, notes); break;
                case "segment-resume-complete": await SegmentResumeCompleteAsync(database, detail, notes); break;
                case "segment-resume-restage": await SegmentResumeRestageAsync(database, detail, notes); break;
                case "negative-authority": await NegativeAuthorityAsync(database, notes); break;
                case "stale-after-reopen": await StaleAfterReopenAsync(database, notes); break;
                case "replay-probe": await ReplayProbeAsync(notes); break;
                case "dump": Dump(database, notes); break;
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

    // ---- §6 positive control: real cross-project publication + receipts ----
    private static async Task PublishAsync(string database, List<string> notes)
    {
        var store = await PublishRealAsync(database, "run:q1:a");
        var plugin = new CSharpCodeGraphPlugin();
        notes.Add("plugin=" + plugin.Id.Value + " version=" + plugin.Version);
        var units = await store.GetPublishedUnitsAsync(Repo);
        notes.Add("publishedUnits=" + units.Count + " [" + string.Join(", ",
            units.Select(u => u.Origin.IndexUnitId.Value)) + "]");
        foreach (var unit in units)
            notes.Add("unit " + unit.Origin.IndexUnitId.Value + " nodes=" + unit.Nodes.Count +
                " edges=" + unit.Edges.Count + " occurrences=" + unit.Occurrences.Count +
                " requiresAuthority=" + unit.RequiresOccurrenceAuthority +
                " coverageAnalyzed=" + (unit.OccurrenceCoverage?.AnalysisPerformed).ToString() +
                " coverageComplete=" + (unit.OccurrenceCoverage?.IsComplete).ToString());
        store.Dispose();

        foreach (var (key, row) in Rows(database, "AndiritOccurrenceUnit"))
        {
            notes.Add("receipt " + key.Replace("\n", "\\n"));
            notes.Add("  requiresAuthority=" + row["RequiresAuthority"]!.GetValue<bool>() +
                " hasAuthority=" + (row["Authority"] is not null) +
                " authorityPublicationRun=" + (row["AuthorityPublicationIndexRunId"]?.GetValue<string>() ?? "null"));
            if (row["Authority"] is { } authority)
            {
                notes.Add("  authorityIndexUnitId=" + authority["IndexUnitId"]!.GetValue<string>());
                notes.Add("  authoritySourceUnitId=" + authority["SourceUnitId"]!.GetValue<string>());
                notes.Add("  authorityVersion=" + authority["Version"]!.GetValue<int>() +
                    " members=" + ((JsonArray)authority["Members"]!).Count +
                    " facts=" + ((JsonArray)authority["Facts"]!).Count);
                foreach (var fact in (JsonArray)authority["Facts"]!)
                    notes.Add("  fact definingUnit=" + fact!["DefiningProjectUnitId"]!.GetValue<string>() +
                        " target=" + fact["TargetId"]!.GetValue<string>()[..28] + "…");
            }
        }
        notes.Add("metaRows=" + Rows(database, "AndiritOccurrenceMeta").Count +
            " pageRows=" + Rows(database, "AndiritOccurrencePage").Count);
    }

    // ---- §6 positive control verify phase (fresh process, no publish) ----
    private static async Task VerifyAsync(string database, List<string> notes)
    {
        var context = Context("run:q1:a", CrossProject().Files);
        var (analysis, app, lib) = await LiveAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "QLib.Value", "Get"));
        notes.Add("expectedTarget=" + target.Value);

        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var units = await reopened.GetPublishedUnitsAsync(Repo);
            if (units.Count != 2) throw new InvalidDataException("Expected 2 published units, got " + units.Count);

            // Cross-project graph edge resolves after reconstruction.
            var appNodes = await reopened.FindNodesByQualifiedNameAsync(Repo, "QApp/QApp.csproj");
            var appProject = appNodes.Single(n => n.Kind == CodeNodeKinds.Project);
            var traversal = await reopened.TraverseAsync(Repo,
                new(appProject.Id, CodeGraphDirection.Outgoing, maxDepth: 1));
            if (!traversal.Nodes.Any(n => n.Kind == CodeNodeKinds.Project && n.QualifiedName == "QLib/QLib.csproj"))
                throw new InvalidDataException("Cross-project DependsOn edge did not resolve to the Lib project node.");
            notes.Add("crossProjectEdge=resolved");

            // Bound, current-authority query.
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);
            var hit = await reopened.FindOccurrencesAsync(query);
            if (hit.Availability != CodeOccurrenceAvailability.Available)
                throw new InvalidDataException("Availability=" + hit.Availability + " reason=" + hit.RefusalReason);
            if (hit.Occurrences.Count != 1) throw new InvalidDataException("Hits=" + hit.Occurrences.Count);
            if (!hit.IsComplete) throw new InvalidDataException("Coverage incomplete: " + hit.Coverage);
            if (hit.Occurrences[0].TargetId != target) throw new InvalidDataException("Target mismatch");
            if (hit.Occurrences[0].SourceUnitId != app.Descriptor.ProjectUnitId)
                throw new InvalidDataException("SourceUnitId=" + hit.Occurrences[0].SourceUnitId);
            notes.Add("availability=Available hits=1 isComplete=true");
            notes.Add("sourceUnitId=" + hit.Occurrences[0].SourceUnitId);
            notes.Add("examined=" + hit.Examined + " totalMatches=" + hit.TotalMatches);
            notes.Add("coverageVerifiedUnits=" + hit.Coverage!.VerifiedUnitCount + "/" + hit.Coverage.DeclaredUnitCount +
                " state=" + hit.Coverage.State);
        }

        // Durable defining-unit receipt (fresh native read after close).
        var receipt = OccurrenceRowBySourceUnit(database, app.Descriptor.ProjectUnitId);
        var defining = receipt["Authority"]!["Facts"]![0]!["DefiningProjectUnitId"]!.GetValue<string>();
        if (defining != lib.Descriptor.ProjectUnitId)
            throw new InvalidDataException("DefiningProjectUnitId=" + defining);
        notes.Add("definingProjectUnitId=" + defining);
    }

    // ---- §6 graph-only control: cross-unit edges, zero occurrence rows ----
    private static async Task GraphOnlyPublishAsync(string database, string variant, List<string> notes)
    {
        // "ref-first": the referencing unit id sorts before the referenced one
        // (the historical failing order); "refd-first": the mirror order.
        var (appUnit, libUnit) = variant == "ref-first" ? ("a-app", "z-lib") : ("a-lib", "z-app");
        var app = Node("AppType");
        var lib = Node("LibType");
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(Repo));
            await store.StoreIndexRunAsync(new(Repo, new("run:graphonly"), Started, plugins: [Plugin]));
            // Staging follows dependency order (endpoint validation at staging time).
            await store.StageIndexUnitAsync(new(Origin(libUnit, "run:graphonly"), [lib]));
            await store.StageIndexUnitAsync(new(Origin(appUnit, "run:graphonly"), [app],
                edges: [DependsOn(app, lib)]));
            await store.CompleteIndexRunAsync(
                new(Repo, new("run:graphonly"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
                new(Repo, new("run:graphonly"), []));
        }

        var order = Rows(database, "HetuUnit").Keys.Select(k => k.Split('\n')[2]).ToArray();
        notes.Add("variant=" + variant + " persistedOrder=[" + string.Join(", ", order) + "]");
        notes.Add("occurrenceMetaRows=" + Rows(database, "AndiritOccurrenceMeta").Count +
            " occurrenceUnitRows=" + Rows(database, "AndiritOccurrenceUnit").Count);
    }

    private static async Task GraphOnlyVerifyAsync(string database, List<string> notes)
    {
        using var reopened = new LatticeCodeGraphStore(database);
        var units = await reopened.GetPublishedUnitsAsync(Repo);
        if (units.Count != 2) throw new InvalidDataException("Expected 2 units, got " + units.Count);
        var traversal = await reopened.TraverseAsync(Repo,
            new(new("node:AppType"), CodeGraphDirection.Outgoing, maxDepth: 1));
        var edge = traversal.Edges.Single(e => e.Kind == CodeEdgeKinds.DependsOn);
        if (edge.TargetId.Value != "node:LibType") throw new InvalidDataException("Edge target=" + edge.TargetId.Value);
        notes.Add("graphOnlyReopen=ok crossUnitEdge=resolved units=2 unitOccurrences=" +
            string.Join(",", units.Select(u => u.Occurrences.Count)));
    }

    // ---- §6 corruption controls on real durable rows ----
    private static void Corrupt(string database, string mode, List<string> notes)
    {
        var snapshotBefore = Snapshot(database);
        switch (mode)
        {
            case "missing-endpoint":
            {
                // Remove the defining project's graph row entirely.
                var libUnitId = CSharpProjectDiscovery.IndexUnitId("QLib/QLib.csproj");
                var key = HetuUnitKeyByUnitId(database, libUnitId);
                RemoveRow(database, "HetuUnit", key);
                notes.Add("removedGraphRow=" + key.Replace("\n", "\\n"));
                break;
            }
            case "inconsistent-edge":
            {
                var appUnitId = CSharpProjectDiscovery.IndexUnitId("QApp/QApp.csproj");
                var key = HetuUnitKeyByUnitId(database, appUnitId);
                var row = Rows(database, "HetuUnit")["HetuUnit|" + key];
                var edges = (JsonArray)row["Edges"]!;
                var crossProject = (JsonObject)edges.Single(e =>
                    e!["Kind"]!["Value"]!.GetValue<string>() == "depends-on")!;
                var target = (JsonObject)crossProject["TargetId"]!;
                target["Value"] =
                    "csharp:project:0000000000000000000000000000000000000000000000000000000000000000";
                WriteRow(database, "HetuUnit", key, row);
                notes.Add("retargetedCrossProjectEdge=true");
                break;
            }
            case "malformed-row":
            {
                var libUnitId = CSharpProjectDiscovery.IndexUnitId("QLib/QLib.csproj");
                var key = HetuUnitKeyByUnitId(database, libUnitId);
                var row = Rows(database, "HetuUnit")["HetuUnit|" + key];
                row["Nodes"] = "not-an-array";
                WriteRow(database, "HetuUnit", key, row);
                notes.Add("malformedPayload=true");
                break;
            }
            default: throw new ArgumentException("Unknown corrupt mode " + mode);
        }
        notes.Add("snapshotChanged=" + (Snapshot(database) != snapshotBefore));
    }

    private static void CorruptVerify(string database, List<string> notes)
    {
        var expected = Snapshot(database);
        try
        {
            using var reopened = new LatticeCodeGraphStore(database);
            throw new InvalidDataException("Reopen unexpectedly SUCCEEDED for the corrupted fixture.");
        }
        catch (CodeGraphBatchRejectedException exception)
        {
            notes.Add("refused=true exception=CodeGraphBatchRejectedException codes=[" +
                string.Join(", ", exception.Errors.Select(e => e.Code)) + "]");
        }
        catch (Exception exception) when (exception is JsonException or FormatException or InvalidDataException)
        {
            notes.Add("refused=true exception=" + exception.GetType().Name);
        }
        if (Snapshot(database) != expected)
            throw new InvalidDataException("The failed reopen modified durable state.");
        notes.Add("durableSnapshotUnchanged=true (no partial published state, no write-back)");
    }

    // ---- §7 Q1-Q1 segment resumption through the real plugin/sink chain ----
    private static async Task SegmentStageAsync(string database, string naming, List<string> notes)
    {
        // Extract through the real plugin + sink (segments stage with the marker;
        // the authority envelope is transient) and never complete the run.
        var context = Context("run:segment", CrossProject(naming: naming).Files);
        var plugin = new CSharpCodeGraphPlugin();
        var store = new LatticeCodeGraphStore(database);
        await store.UpsertRepositoryAsync(new(Repo));
        await store.StoreIndexRunAsync(new(Repo, new("run:segment"), Started, plugins: [plugin.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }
        store.Dispose();

        var (_, definingPath, referencingPath) = CrossProject(naming: naming);
        var definingUnitId = CSharpProjectDiscovery.IndexUnitId(definingPath);
        var referencingUnitId = CSharpProjectDiscovery.IndexUnitId(referencingPath);
        var stageOrder = Rows(database, "HetuStage").Keys
            .Select(k => k.Split('\n')[3]).ToArray();
        notes.Add("naming=" + naming +
            " definingUnitFirst=" + (string.CompareOrdinal(definingUnitId, referencingUnitId) < 0) +
            " stageKeyOrder=[" + string.Join(", ", stageOrder.Select(id => id[..Math.Min(14, id.Length)])) + "]");
        if (stageOrder.Length != 2) throw new InvalidDataException("Expected 2 staged segments.");

        var segmentRows = Rows(database, "HetuStage");
        foreach (var (key, row) in segmentRows)
        {
            var replacement = row["Replacement"]!.AsObject();
            notes.Add("segment " + key.Split('\n')[^1][..Math.Min(14, key.Split('\n')[^1].Length)] +
                " requiresAuthority=" + replacement["RequiresOccurrenceAuthority"]!.GetValue<bool>() +
                " hasOccurrencesProperty=" + replacement.ContainsKey("Occurrences") +
                " hasAuthorityProperty=" + replacement.ContainsKey("OccurrenceAuthority"));
            if (replacement.ContainsKey("OccurrenceAuthority"))
                throw new InvalidDataException("Transient authority envelope persisted in a segment record.");
        }
        notes.Add("segmentsStaged=true runNotCompleted=true");
    }

    private static async Task SegmentResumeCompleteAsync(string database, string naming, List<string> notes)
    {
        // Fresh process: segments replay; completing the run WITHOUT re-staging
        // publishes marker + coverage but no authority; the verified gate must
        // fail closed.
        var context = Context("run:segment", CrossProject(naming: naming).Files);
        var (analysis, app, lib) = await LiveAsync(context, naming);
        var target = ExpectedTarget(lib, Method(lib, "QLib.Value", "Get"));
        using (var resumed = new LatticeCodeGraphStore(database))
        {
            var staged = await resumed.GetPublishedUnitsAsync(Repo);
            if (staged.Count != 0) throw new InvalidDataException("Staged segments must not be published before completion.");
            await resumed.CompleteIndexRunAsync(
                new(Repo, new("run:segment"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1),
                    [new CodePluginId("penghou.hetu.csharp")]),
                new(Repo, new("run:segment"), []));
        }

        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var published = await reopened.GetPublishedUnitsAsync(Repo);
            notes.Add("publishedUnitsAfterResumeComplete=" + published.Count +
                " markers=" + string.Join(",", published.Select(u => u.RequiresOccurrenceAuthority)));
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);
            var result = await reopened.FindOccurrencesAsync(query);
            if (result.Availability is CodeOccurrenceAvailability.Available)
                throw new InvalidDataException("Graph-only resumption was served as Available.");
            if (result.Availability is not (CodeOccurrenceAvailability.AnalysisUnavailable or CodeOccurrenceAvailability.CorruptedIndex))
                throw new InvalidDataException("Unexpected availability " + result.Availability);
            notes.Add("failClosed=true availability=" + result.Availability +
                " hits=" + result.Occurrences.Count + " examined=" + result.Examined +
                " isComplete=" + result.IsComplete + " reason=" + result.RefusalReason);
        }
    }

    private static async Task SegmentResumeRestageAsync(string database, string naming, List<string> notes)
    {
        // Fresh process: segments replay; explicit re-staging with fresh
        // verified authority into the still-running run, then completion.
        var context = Context("run:segment", CrossProject(naming: naming).Files);
        var plugin = new CSharpCodeGraphPlugin();
        using (var resumed = new LatticeCodeGraphStore(database))
        {
            var run = await resumed.GetIndexRunAsync(Repo, new("run:segment"));
            if (run!.Status != CodeIndexRunStatus.Running) throw new InvalidDataException("Run must still be running.");
            await using (var sink = new CodeGraphIngestionSink(resumed))
            await using (var session = await plugin.CreateSessionAsync(context))
            {
                await session.ExtractAsync(sink);
                await sink.CompleteAsync();
            }
            await resumed.CompleteIndexRunAsync(
                new(Repo, new("run:segment"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
                new(Repo, new("run:segment"), []));
        }

        var (analysis, app, lib) = await LiveAsync(context, naming);
        var target = ExpectedTarget(lib, Method(lib, "QLib.Value", "Get"));
        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);
            var result = await reopened.FindOccurrencesAsync(query);
            if (result.Availability != CodeOccurrenceAvailability.Available || result.Occurrences.Count != 1 || !result.IsComplete)
                throw new InvalidDataException("Re-staged resumption failed: " + result.Availability + " " + result.RefusalReason);
            notes.Add("restagedResumption=Available hits=1 isComplete=true sourceUnitId=" +
                result.Occurrences[0].SourceUnitId);
        }
    }

    // ---- §4 negative authority controls at the real sink / gate level ----
    private static async Task NegativeAuthorityAsync(string database, List<string> notes)
    {
        var store = await PublishRealAsync(database, "run:neg");
        var context = Context("run:neg", CrossProject().Files);
        var (analysis, app, lib) = await LiveAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "QLib.Value", "Get"));

        // 1./2. The sink handoff refuses authority that does not belong to the
        // batch origin (wrong plugin, or envelope/origin mismatch).
        await using (var sink = new CodeGraphIngestionSink(store))
        {
            var extraction = VerifiedCSharpOccurrenceExtractor.Extract(analysis.Universe,
                new VerifiedOccurrenceExtractionRequest(Repo, new CodePluginId("penghou.hetu.csharp"), lib,
                    m => CodeOccurrenceIdentity.ComputeIdentifierDigest([m.Name]),
                    m => CodeOccurrenceIdentity.ComputeDeclarationDigest([m.GetDocumentationCommentId() ?? m.Name]),
                    m => CodeOccurrenceIdentity.ComputeShapeDigest("method|" + m.Arity),
                    m => m.GetDocumentationCommentId() ?? "doc", "1.0.0"));
            if (extraction.Authority is null) throw new InvalidDataException("Fixture extraction must carry authority.");
            var wrongPluginOrigin = new CodeFactOrigin(Repo, new CodePluginId("probe.other"), "1.0.0",
                new("run:neg"), new CodeIndexUnitId("unit:other"));
            var wrongPlugin = await ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                await ((ICodeOccurrenceAuthoritySink)sink).SetOccurrenceAuthorityAsync(wrongPluginOrigin, extraction.Authority));
            notes.Add("wrongPluginAuthorityRefused=" +
                wrongPlugin.Errors.Any(e => e.Code == "ingestion.occurrence-authority.ownership-mismatch"));

            var mismatchedOrigin = new CodeFactOrigin(Repo, new CodePluginId("penghou.hetu.csharp"), "1.0.0",
                new("run:neg"), new CodeIndexUnitId("unit:x"));
            var mismatched = await ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                await ((ICodeOccurrenceAuthoritySink)sink).SetOccurrenceAuthorityAsync(mismatchedOrigin,
                    extraction.Authority with { PluginId = "probe.other" }));
            notes.Add("mismatchedEnvelopeRefused=" +
                mismatched.Errors.Any(e => e.Code == "ingestion.occurrence-authority.ownership-mismatch"));

            // 3. Authority registered but the unit never completes: CompleteAsync
            // refuses (an authority cannot be silently dropped or published
            // without its completing batch).
            var unconsumedOrigin = new CodeFactOrigin(Repo, new CodePluginId("penghou.hetu.csharp"), "1.0.0",
                new("run:neg"), new CodeIndexUnitId("unit:unconsumed"));
            await ((ICodeOccurrenceAuthoritySink)sink).SetOccurrenceAuthorityAsync(
                unconsumedOrigin, extraction.Authority with { IndexUnitId = "unit:unconsumed" });
            var unconsumed = await ThrowsAsync<CodeGraphBatchRejectedException>(async () => await sink.CompleteAsync());
            notes.Add("unconsumedAuthorityRefused=" +
                unconsumed.Errors.Any(e => e.Code == "ingestion.index-unit.incomplete"));
        }
        store.Dispose();

        // 4. Missing current authority is never a successful empty.
        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var result = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target));
            if (result.Availability != CodeOccurrenceAvailability.AnalysisUnavailable ||
                result.Occurrences.Count != 0 || result.IsComplete)
                throw new InvalidDataException("Missing current authority must refuse AnalysisUnavailable.");
            notes.Add("missingCurrentAuthority=AnalysisUnavailable hits=0 examined=" + result.Examined + " isComplete=false");
        }

        // 5. A persisted occurrence unit whose ownership was tampered (moved to
        // another unit id) must refuse CorruptedIndex, never serve.
        var appRowKey = OccurrenceRowKeyBySourceUnit(database, app.Descriptor.ProjectUnitId);
        var row = Rows(database, "AndiritOccurrenceUnit")[appRowKey];
        var originalUnitId = row["IndexUnitId"]!.GetValue<string>();
        row["IndexUnitId"] = "unit:moved";
        var movedKey = appRowKey[..(appRowKey.Length - originalUnitId.Length)] + "unit:moved";
        WriteRow(database, "AndiritOccurrenceUnit", movedKey, row);
        RemoveRow(database, "AndiritOccurrenceUnit", appRowKey["AndiritOccurrenceUnit|".Length..]);
        using (var tampered = new LatticeCodeGraphStore(database))
        {
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);
            var result = await tampered.FindOccurrencesAsync(query);
            if (result.Availability != CodeOccurrenceAvailability.CorruptedIndex)
                throw new InvalidDataException("Tampered ownership must refuse CorruptedIndex, got " + result.Availability);
            notes.Add("wrongUnitAssociation=CorruptedIndex reason=" + result.RefusalReason);
        }
    }

    // ---- §4 stale controls after fresh-process reopen ----
    private static async Task StaleAfterReopenAsync(string database, List<string> notes)
    {
        var a = Context("run:a", CrossProject().Files);
        var (_, appA, libA) = await LiveAsync(a);
        var target = ExpectedTarget(libA, Method(libA, "QLib.Value", "Get"));
        (await PublishRealAsync(database, "run:a")).Dispose();

        // Direct defining-source change (stable target identity is impossible
        // here, but the query must still refuse, never serve).
        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var direct = Context("run:b", CrossProject("public static int Get() => 2;").Files);
            var (analysisDirect, appDirect, _) = await LiveAsync(direct);
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysisDirect.Universe, appDirect.Compilation);
            var result = await reopened.FindOccurrencesAsync(query);
            notes.Add("directChange=" + result.Availability + " hits=" + result.Occurrences.Count +
                " examined=" + result.Examined + " isComplete=" + result.IsComplete);
            if (result.Availability != CodeOccurrenceAvailability.StalePublication)
                throw new InvalidDataException("Direct change must refuse StalePublication.");
        }

        // Unchanged-input control: the same verified membership stays Available
        // after reopen (proves the refusals above come from staleness, not from
        // reopen itself).
        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var control = Context("run:a", CrossProject().Files);
            var (analysisControl, appControl, _) = await LiveAsync(control);
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysisControl.Universe, appControl.Compilation);
            var result = await reopened.FindOccurrencesAsync(query);
            if (result.Availability != CodeOccurrenceAvailability.Available || !result.IsComplete)
                throw new InvalidDataException("Unchanged-input control must stay Available.");
            notes.Add("unchangedControl=Available isComplete=true");
        }
    }

    // ---- §5 replay-level ordering / mixed-repository / repeated-replacement ----
    private static async Task ReplayProbeAsync(List<string> notes)
    {
        var app = Node("AppType");
        var lib = Node("LibType");
        var appUnit = new CodeIndexUnitReplacement(Origin("app", "run:p"), [app], edges: [DependsOn(app, lib)]);
        var libUnit = new CodeIndexUnitReplacement(Origin("lib", "run:p"), [lib]);
        var completed = new CodeIndexRunManifest(Repo, new("run:p"), Started, CodeIndexRunStatus.Completed,
            Started.AddSeconds(1), [Plugin]);
        var state = new CodeRepositoryIndexState(Repo, new("run:p"), []);

        DurableCommandLog.PersistedCommand[] Base() =>
        [
            new("repository", Repository: new CodeRepositoryManifest(Repo)),
            new("run", Run: new CodeIndexRunManifest(Repo, new("run:p"), Started, plugins: [Plugin])),
            new("replace", Replacement: appUnit),
            new("replace", Replacement: libUnit),
            new("complete", Run: completed, State: state),
        ];

        // Forward (referencing first — the historical failing order) vs exact reverse.
        var forward = await DurableCommandLog.ReplayAsync(Base(), CancellationToken.None);
        var reversedCommands = Base();
        (reversedCommands[2], reversedCommands[3]) = (reversedCommands[3], reversedCommands[2]);
        var reversed = await DurableCommandLog.ReplayAsync(reversedCommands, CancellationToken.None);
        var forwardUnits = await forward.GetPublishedUnitsAsync(Repo);
        var reversedUnits = await reversed.GetPublishedUnitsAsync(Repo);
        if (forwardUnits.Count != 2 || reversedUnits.Count != 2)
            throw new InvalidDataException("Permutation changed the publication: " + forwardUnits.Count + "/" + reversedUnits.Count);
        var forwardTraversal = await forward.TraverseAsync(Repo, new(new("node:AppType"), CodeGraphDirection.Outgoing, maxDepth: 1));
        var reversedTraversal = await reversed.TraverseAsync(Repo, new(new("node:AppType"), CodeGraphDirection.Outgoing, maxDepth: 1));
        if (!forwardTraversal.Edges.Select(e => e.Id.Value).Order().SequenceEqual(
                reversedTraversal.Edges.Select(e => e.Id.Value).Order()))
            throw new InvalidDataException("Permutation changed the traversed edges.");
        notes.Add("forwardAndReverseIdentical=true units=2 edges=" + forwardTraversal.Edges.Count);

        // Mixed repositories: an invalid unit in another repository must refuse
        // the whole reconstruction.
        var repoB = new CodeRepositoryId("repo:s23-pub-q1-b");
        var badEdge = new CodeGraphEdge(new("edge:bad"), new("node:orphan-a"), new("node:orphan-b"),
            CodeEdgeKinds.References, new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"));
        var badUnit = new CodeIndexUnitReplacement(
            new CodeFactOrigin(repoB, Plugin, "1.0.0", new("run:p"), new("unit:bad")), [Node("OrphanA")], edges: [badEdge]);
        var mixedCommands = Base().Concat(
        [
            new("repository", Repository: new CodeRepositoryManifest(repoB)),
            new("replace", Replacement: badUnit),
        ]).ToArray();
        try
        {
            await DurableCommandLog.ReplayAsync(mixedCommands, CancellationToken.None);
            throw new InvalidDataException("Mixed-repository invalid unit was admitted.");
        }
        catch (CodeGraphBatchRejectedException exception)
        {
            notes.Add("mixedInvalidRefused=true codes=[" + string.Join(", ", exception.Errors.Select(e => e.Code)) + "]");
        }

        // Repeated replacement of one owner: later row wins, every row is
        // intrinsically validated, final set is consistent.
        var superseded = new CodeIndexUnitReplacement(Origin("lib", "run:p"), [Node("LibTypeV1")]);
        var repeatedCommands = Base();
        repeatedCommands[3] = new("replace", Replacement: superseded);
        repeatedCommands = repeatedCommands.Append(new("replace", Replacement: libUnit)).ToArray();
        var replayedRepeated = await DurableCommandLog.ReplayAsync(repeatedCommands, CancellationToken.None);
        var repeatedUnits = await replayedRepeated.GetPublishedUnitsAsync(Repo);
        if (repeatedUnits.Count != 2 ||
            !repeatedUnits.Any(u => u.Nodes.Any(n => n.Id.Value == "node:LibType")))
            throw new InvalidDataException("Repeated replacement semantics changed: " +
                string.Join(",", repeatedUnits.Select(u => u.Origin.IndexUnitId.Value)));
        notes.Add("repeatedReplacementLastWins=true units=2");

        // Intrinsic unit-level duplicate refusal survives the bulk restore.
        var duplicateNode = Node("AppType");
        var duplicateUnit = new CodeIndexUnitReplacement(Origin("dup", "run:p"), [duplicateNode, duplicateNode]);
        var duplicateCommands = Base().Append(new("replace", Replacement: duplicateUnit)).ToArray();
        try
        {
            await DurableCommandLog.ReplayAsync(duplicateCommands, CancellationToken.None);
            throw new InvalidDataException("Unit-level duplicate was admitted.");
        }
        catch (CodeGraphBatchRejectedException exception)
        {
            notes.Add("unitDuplicateRefused=true codes=[" + string.Join(", ", exception.Errors.Select(e => e.Code)) + "]");
        }
    }

    // ---- helpers ----
    private static void Dump(string database, List<string> notes)
    {
        foreach (var (key, row) in Rows(database, "HetuUnit"))
        {
            var edges = (JsonArray?)row["Edges"];
            if (edges is { Count: > 0 })
            {
                notes.Add("key " + key.Replace("\n", "\\n"));
                notes.Add("edge0 " + edges[0]!.ToJsonString());
                return;
            }
        }
        notes.Add("no edges found");
    }

    private static CodeFactOrigin Origin(string unit, string run) =>
        new(Repo, Plugin, "1.0.0", new(run), new(unit));

    private static CodeGraphNode Node(string name) => new(
        new CodeNodeId("node:" + name), CodeNodeKinds.Type, name, "Probe." + name, new CodeSymbolId("symbol:" + name));

    private static CodeGraphEdge DependsOn(CodeGraphNode from, CodeGraphNode to) => new(
        new CodeEdgeId("edge:" + from.Name + "->" + to.Name),
        from.Id, to.Id, CodeEdgeKinds.DependsOn,
        new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"));

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

    private static string HetuUnitKeyByUnitId(string database, string unitId)
    {
        var key = "HetuUnit|" + Repo.Value + "\n" + new CodePluginId("penghou.hetu.csharp").Value + "\n" + unitId;
        return Rows(database, "HetuUnit").ContainsKey(key)
            ? key["HetuUnit|".Length..]
            : throw new InvalidDataException("Graph row not found for " + unitId);
    }

    private static JsonObject OccurrenceRowBySourceUnit(string database, string projectUnitId) =>
        Rows(database, "AndiritOccurrenceUnit").Single(pair =>
            pair.Value["Authority"] is not null &&
            pair.Value["Authority"]!["SourceUnitId"]!.GetValue<string>() == projectUnitId).Value;

    private static string OccurrenceRowKeyBySourceUnit(string database, string projectUnitId) =>
        Rows(database, "AndiritOccurrenceUnit").Single(pair =>
            pair.Value["Authority"] is not null &&
            pair.Value["Authority"]!["SourceUnitId"]!.GetValue<string>() == projectUnitId).Key;

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
        JsonSerializer.Serialize(Rows(database,
            "HetuUnit", "HetuStage", "HetuIndexState", "HetuRun",
            "AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage"));

    private static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
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
