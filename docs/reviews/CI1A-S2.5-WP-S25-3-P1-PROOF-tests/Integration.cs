using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Frozen = AdrCi1Exp.Shared;

namespace P1Proof;

// S1-S8 integrated proof over the accepted real source-to-native-store path.
//
// Structure (all in one native database, system temp):
//   - repository "p1:integration": the frozen R2 corpus publishable subset
//     (App, Core, Leaf, Solo) published as run:corpus through the real
//     extraction -> batch/sink -> stage/complete path.
//   - repository "p1:fixture": the bounded adversarial canonicalization fixture
//     published as run:fixture through the same path (separate repository so
//     per-project index-unit owners never replace corpus units of the same key).
//   - recovery verb: managed before-commit and after-commit faults around
//     CompleteIndexRunAsync on the corpus repository, mirroring the accepted
//     NEW-01/FINAL-Q1 fault mechanics, plus a tampered occurrence-page refusal.
internal static class Integration
{
    internal static readonly CodeRepositoryId CorpusRepo = new("p1:integration");
    internal static readonly CodeRepositoryId FixtureRepo = new("p1:fixture");
    private const string CorpusRun = "run:corpus";
    private const string FixtureRun = "run:fixture";

    private const string FixtureCoreProject = "P1Fixture/P1Core/P1Core.csproj";
    private const string FixtureAppProject = "P1Fixture/P1App/P1App.csproj";

    private sealed record Scenario
    {
        public required string Name { get; init; }
        public required CodeRepositoryId Repo { get; init; }
        public required string RunId { get; init; }
        public required Dictionary<string, string> Files { get; init; }
        public required VerifiedCSharpAnalysis Analysis { get; init; }
        public required VerifiedCSharpCompilation App { get; init; }
        public required CodeOccurrenceTargetId Target { get; init; }
        public required JsonObject Expected { get; init; }
        public (string Path, int Line)? Span { get; init; }
        public CodeOccurrenceTargetId? ExtraTarget { get; init; }
        public JsonObject? ExtraExpected { get; init; }
    }

    // ------------------------------------------------------------------
    // Expectations: literal canonical forms -> independent reference digests
    // (RefEnc) -> target id via the adopted asmctx3 composition, cross-checked
    // against the frozen R2 authority primitives (both must agree).
    // ------------------------------------------------------------------
    private static JsonObject Digests(string identifierForm, string declarationForm, string shapeForm) => new()
    {
        ["identifierForm"] = identifierForm,
        ["declarationForm"] = declarationForm,
        ["shapeForm"] = shapeForm,
        ["identifierDigest"] = RefEnc.ExpectedIdentifier(identifierForm),
        ["declarationDigest"] = RefEnc.ExpectedDeclaration(declarationForm),
        ["shapeDigest"] = RefEnc.ExpectedShape(shapeForm),
    };

    private static CodeOccurrenceTargetId ExpectedTarget(Scenario scenario, JsonObject digests)
    {
        var defining = scenario.Analysis.Projects
            .First(p => p.Project.Path == (scenario.Name == "corpus" ? "Core/Core.csproj" : FixtureCoreProject)).Verified!;
        var mapped = defining.MapR2FaithfulCompctx1();
        var target = CodeOccurrenceIdentity.ComputeTargetId(
            mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName,
            (string)digests["identifierDigest"]!, (string)digests["declarationDigest"]!, (string)digests["shapeDigest"]!);
        var frozen = Frozen.CompilationProvenance.Describe(scenario.Repo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var viaFrozen = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName,
             (string)digests["identifierDigest"]!, (string)digests["declarationDigest"]!, (string)digests["shapeDigest"]!]);
        if (target.Value != viaFrozen)
            throw new InvalidDataException("independent target computations disagree: " + target.Value + " vs " + viaFrozen);
        // Target composition constituents (compctx1/metadataIdentity/moduleName per
        // contract section 6). Note: the reference identities of the defining
        // compilation come from the host process's trusted-platform-assembly closure,
        // so the composed target is stable within one harness build but binds the
        // host build (see the P1 report observation O-1).
        digests["mappedDescriptorDigest"] = mapped.DescriptorDigest;
        digests["frozenDescriptorDigest"] = frozen.DescriptorDigest;
        digests["metadataIdentity"] = mapped.MetadataIdentity;
        digests["moduleName"] = mapped.ModuleName;
        digests["targetId"] = target.Value;
        return target;
    }

    private static async Task<Scenario> CorpusScenarioAsync()
    {
        var files = Corpus.Load() ?? throw new InvalidDataException("frozen R2 corpus publishable subset not found");
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(CorpusRepo, CorpusRun, files));
        var app = analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        var scenario = new Scenario
        {
            Name = "corpus",
            Repo = CorpusRepo,
            RunId = CorpusRun,
            Files = files,
            Analysis = analysis,
            App = app,
            Target = null!,
            Expected = Digests("Add", "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)", "method|0|int,int"),
            Span = ("App/Sites.cs", Corpus.FindLine(files, "App/Sites.cs", "methods.Add(1, 2)")),
            ExtraExpected = Digests("Greet", "M:Corpus.Core.IGreeter.Greet(System.String)", "method|0|string"),
        };
        return scenario with
        {
            Target = ExpectedTarget(scenario, scenario.Expected),
            ExtraTarget = ExpectedTarget(scenario, scenario.ExtraExpected!),
        };
    }

    private static async Task<Scenario> FixtureScenarioAsync()
    {
        var files = FixtureFiles(out var span);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(FixtureRepo, FixtureRun, files));
        var app = analysis.Projects.Single(p => p.Project.Path == FixtureAppProject).Verified!;
        var scenario = new Scenario
        {
            Name = "fixture",
            Repo = FixtureRepo,
            RunId = FixtureRun,
            Files = files,
            Analysis = analysis,
            App = app,
            Target = null!,
            Expected = Digests("Hit", "M:Fixture.Core.P1Marker.Hit(System.String)", "method|0|string"),
            Span = span,
        };
        return scenario with { Target = ExpectedTarget(scenario, scenario.Expected) };
    }

    private static Dictionary<string, string> FixtureFiles(out (string Path, int Line) span)
    {
        const string coreSource = "namespace Fixture.Core;\npublic static class P1Marker\n{\n    public static void Hit(string value) { }\n}\n";
        var consumer = Cases.BuildConsumer(out var positions, "using Fixture.Core;", "P1Marker.Hit(\"p1\");");
        span = ("P1Fixture/P1App/Consumer.cs", positions[0].Line);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [FixtureCoreProject] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["P1Fixture/P1Core/Targets.cs"] = coreSource,
            [FixtureAppProject] = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../P1Core/P1Core.csproj\" /></ItemGroup></Project>",
            ["P1Fixture/P1App/Consumer.cs"] = consumer,
        };
    }

    internal static async Task<JsonObject> RunAsync(string action, string database)
    {
        return action switch
        {
            "publish" => await PublishAsync(database),
            "reopen" => await ReopenAsync(database),
            "recovery" => await RecoveryAsync(database),
            _ => throw new ArgumentException("unknown verb " + action),
        };
    }

    // ------------------------------------------------------------------
    // S1-S5: publish corpus + fixture, inspect native rows.
    // ------------------------------------------------------------------
    private static async Task<JsonObject> PublishAsync(string database)
    {
        var receipt = new JsonObject
        {
            ["action"] = "publish", ["database"] = database, ["processId"] = Environment.ProcessId,
        };
        Cleanup(database);
        var corpus = await CorpusScenarioAsync();
        var fixture = await FixtureScenarioAsync();
        receipt["corpus"] = DescribeScenario(corpus);
        receipt["fixture"] = DescribeScenario(fixture);

        receipt["corpusPublish"] = await PublishScenarioAsync(database, corpus);
        receipt["fixturePublish"] = await PublishScenarioAsync(database, fixture);

        receipt["nativeRows"] = ReadNativeRows(database, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["corpusAdd"] = corpus.Target.Value,
            ["corpusGreet"] = corpus.ExtraTarget!.Value,
            ["fixtureHit"] = fixture.Target.Value,
        });

        receipt["storageLayerRefusal"] = await StorageLayerRefusalAsync(database);
        return receipt;
    }

    private static JsonObject DescribeScenario(Scenario scenario)
    {
        var hashes = new JsonObject();
        foreach (var (path, content) in scenario.Files.OrderBy(p => p.Key, StringComparer.Ordinal))
            hashes[path] = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        return new JsonObject
        {
            ["name"] = scenario.Name,
            ["repo"] = scenario.Repo.Value,
            ["run"] = scenario.RunId,
            ["files"] = new JsonArray(scenario.Files.Keys.OrderBy(k => k, StringComparer.Ordinal).Select(k => (JsonNode)k).ToArray()),
            ["fileHashes"] = hashes,
            ["compositeHash"] = CompositeHash(scenario.Files),
            ["span"] = scenario.Span is { } span ? new JsonObject { ["path"] = span.Path, ["line"] = span.Line } : null,
            ["expected"] = scenario.Expected.DeepClone(),
            ["extraExpected"] = scenario.ExtraExpected?.DeepClone(),
        };
    }

    // Composite source-fixture identity: SHA-256 over the sorted "path\nsha256:<hex>\n"
    // records of the exact file bytes published for the scenario.
    private static string CompositeHash(IReadOnlyDictionary<string, string> files)
    {
        var builder = new StringBuilder();
        foreach (var (path, content) in files.OrderBy(p => p.Key, StringComparer.Ordinal))
            builder.Append(path).Append('\n')
                .Append("sha256:").Append(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content)))).Append('\n');
        return "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static async Task<JsonObject> PublishScenarioAsync(string database, Scenario scenario)
    {
        var plugin = new CSharpCodeGraphPlugin();
        JsonArray batches;
        int authorities;
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(scenario.Repo));
            await store.StoreIndexRunAsync(new(scenario.Repo, new CodeIndexRunId(scenario.RunId), Extract.Started, plugins: [plugin.Id]));
            await using (var ingestion = new CodeGraphIngestionSink(store))
            {
                var capture = new CaptureSink(ingestion);
                await using (var session = await plugin.CreateSessionAsync(Context(scenario.Repo, scenario.RunId, scenario.Files)))
                {
                    try
                    {
                        await session.ExtractAsync(capture);
                        await ingestion.CompleteAsync();
                    }
                    catch (CodeGraphBatchRejectedException exception)
                    {
                        throw new InvalidOperationException(
                            $"GRAPH-INVARIANT-DETAIL[{scenario.RunId}]: " + string.Join(" | ", exception.Errors), exception);
                    }
                }
                batches = BatchSummaries(capture);
                authorities = capture.Authorities.Count;
            }
            await store.CompleteIndexRunAsync(
                new(scenario.Repo, new CodeIndexRunId(scenario.RunId), Extract.Started, CodeIndexRunStatus.Completed, Extract.Started.AddSeconds(1), [plugin.Id]),
                new(scenario.Repo, new CodeIndexRunId(scenario.RunId), []));
        }
        return new JsonObject
        {
            ["run"] = scenario.RunId,
            ["status"] = "Completed",
            ["authorityEnvelopes"] = authorities,
            ["batches"] = batches,
        };
    }

    private static JsonArray BatchSummaries(CaptureSink sink)
    {
        var array = new JsonArray();
        foreach (var batch in sink.Batches)
        {
            array.Add(new JsonObject
            {
                ["unit"] = batch.Origin.IndexUnitId.Value,
                ["plugin"] = batch.Origin.PluginId.Value,
                ["run"] = batch.Origin.IndexRunId.Value,
                ["completesIndexUnit"] = batch.CompletesIndexUnit,
                ["nodes"] = batch.Nodes.Count,
                ["declarations"] = batch.Declarations.Count,
                ["edges"] = batch.Edges.Count,
                ["occurrences"] = batch.Occurrences.Count,
            });
        }
        return array;
    }

    // ------------------------------------------------------------------
    // S5: real native rows (graph mirror + occurrence tables).
    // ------------------------------------------------------------------
    private static JsonObject ReadNativeRows(string database, IReadOnlyDictionary<string, string> expectedTargets)
    {
        using var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginReadTransaction();
        var meta = new JsonArray();
        using (var q = db.Prepare("MATCH (s:AndiritOccurrenceMeta) RETURN s.payload"))
        using (var r = q.Execute(txn))
        {
            foreach (var row in r.ReadAll())
                meta.Add(JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(0).AsString())))!);
        }
        var pages = new JsonArray();
        using (var q = db.Prepare("MATCH (s:AndiritOccurrencePage) RETURN s.key, s.payload"))
        using (var r = q.Execute(txn))
        {
            foreach (var row in r.ReadAll())
            {
                var payload = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(1).AsString())))!.AsObject();
                pages.Add(new JsonObject
                {
                    ["key"] = row.GetValue(0).AsString(),
                    ["payloadBytes"] = Encoding.UTF8.GetByteCount(row.GetValue(1).AsString()),
                    ["publicationIndexRunId"] = payload["PublicationIndexRunId"]?.GetValue<string>(),
                });
            }
        }
        var unitPayloads = new List<string>();
        using (var q = db.Prepare("MATCH (s:AndiritOccurrenceUnit) RETURN s.payload"))
        using (var r = q.Execute(txn))
        {
            foreach (var row in r.ReadAll())
                unitPayloads.Add(Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(0).AsString())));
        }
        long Count(string label)
        {
            using var q = db.Prepare($"MATCH (n:{label}) RETURN count(n)");
            using var r = q.Execute(txn);
            var rows = r.ReadAll();
            return rows.Count == 0 ? 0 : rows[0].GetValue(0).AsInt64();
        }
        var nativeGraphUnitRows = (int)Count("HetuUnit");
        txn.Commit();
        var present = new JsonObject();
        foreach (var (name, target) in expectedTargets)
            present[name] = unitPayloads.Any(p => p.Contains(target, StringComparison.Ordinal));
        return new JsonObject
        {
            ["occurrenceMetaRows"] = meta,
            ["occurrenceMetaRowCount"] = meta.Count,
            ["occurrencePages"] = pages,
            ["occurrencePageCount"] = pages.Count,
            ["occurrenceUnitCount"] = unitPayloads.Count,
            ["expectedTargetPresentInUnits"] = present,
            ["nativeGraphUnitRows"] = nativeGraphUnitRows,
            ["unitPayloadExcerpts"] = new JsonArray(unitPayloads.Select(p => (JsonNode)(p.Length > 4000 ? p[..4000] : p)).ToArray()),
        };
    }

    // ------------------------------------------------------------------
    // Negative storage-layer case: a staged replacement whose edge endpoint is
    // absent from the materialized graph must be refused at the store boundary.
    // ------------------------------------------------------------------
    private static async Task<JsonObject> StorageLayerRefusalAsync(string database)
    {
        var plugin = new CSharpCodeGraphPlugin();
        var repo = new CodeRepositoryId("p1:negative");
        var run = new CodeIndexRunId("run:negative");
        using var store = new LatticeCodeGraphStore(database);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, run, Extract.Started, plugins: [plugin.Id]));
        try
        {
            await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                new CodeFactOrigin(repo, plugin.Id, plugin.Version, run, new CodeIndexUnitId("negative:unit")),
                nodes: [new CodeGraphNode(new CodeNodeId("node:neg:source"), CodeNodeKinds.Callable, "neg", "neg", new CodeSymbolId("symbol:neg"))],
                edges: [new CodeGraphEdge(new CodeEdgeId("edge:neg:missing"), new CodeNodeId("node:neg:source"), new CodeNodeId("node:neg:missing"),
                    CodeEdgeKinds.DependsOn, new CodeEvidence(EvidenceQuality.Resolved, plugin.Id.Value, plugin.Version))]));
            return new JsonObject { ["scenario"] = "edge with missing endpoint", ["outcome"] = "UNEXPECTED: accepted" };
        }
        catch (CodeGraphBatchRejectedException exception)
        {
            return new JsonObject
            {
                ["scenario"] = "edge with missing endpoint",
                ["outcome"] = "refused",
                ["exceptionType"] = exception.GetType().Name,
                ["message"] = exception.Message,
                ["errorCodes"] = new JsonArray(exception.Errors.Select(e => (JsonNode)e.Code).Distinct().ToArray()),
            };
        }
    }

    // ------------------------------------------------------------------
    // S6-S7: fresh-process reopen, authoritative query, refusals.
    // ------------------------------------------------------------------
    private static async Task<JsonObject> ReopenAsync(string database)
    {
        var receipt = new JsonObject
        {
            ["action"] = "reopen", ["database"] = database, ["processId"] = Environment.ProcessId,
        };
        var corpus = await CorpusScenarioAsync();
        var fixture = await FixtureScenarioAsync();
        receipt["corpusExpected"] = corpus.Expected.DeepClone();
        receipt["corpusExtraExpected"] = corpus.ExtraExpected!.DeepClone();
        receipt["fixtureExpected"] = fixture.Expected.DeepClone();

        using var store = new LatticeCodeGraphStore(database);
        receipt["corpusAdd"] = await QueryAsync(store, corpus, corpus.Target);
        receipt["corpusGreet"] = await QueryAsync(store, corpus, corpus.ExtraTarget!);
        receipt["fixtureHit"] = await QueryAsync(store, fixture, fixture.Target);
        receipt["corpusPublishedUnits"] = await PublishedUnitsAsync(store, corpus.Repo);
        receipt["fixturePublishedUnits"] = await PublishedUnitsAsync(store, fixture.Repo);

        // Stale publication refusal: an expected-publication pointer that is not
        // the current durable publication must refuse.
        var first = await store.FindOccurrencesAsync(
            VerifiedCSharpOccurrenceAuthority.Bind(new(corpus.Repo, corpus.Target, maxResults: 10), corpus.Analysis.Universe, corpus.App.Compilation));
        if (first.Publication is { } current)
        {
            var stale = new CodeGraphPublication(corpus.Repo, new CodeIndexRunId("run:stale-probe"),
                current.SnapshotIdentity, current.IsConsistentSnapshot, current.IndexIdentity);
            var staleResult = await store.FindOccurrencesAsync(
                VerifiedCSharpOccurrenceAuthority.Bind(new(corpus.Repo, corpus.Target, stale, maxResults: 10), corpus.Analysis.Universe, corpus.App.Compilation));
            receipt["stalePublication"] = new JsonObject
            {
                ["availability"] = staleResult.Availability.ToString(),
                ["factCount"] = staleResult.Occurrences.Count,
                ["refusalReason"] = staleResult.RefusalReason,
            };
        }

        // Missing verified-compilation authority: a plain query (no authority
        // verifier) against an authority-bearing publication must refuse.
        var noAuthority = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(corpus.Repo, corpus.Target, maxResults: 10));
        receipt["missingAuthority"] = new JsonObject
        {
            ["availability"] = noAuthority.Availability.ToString(),
            ["factCount"] = noAuthority.Occurrences.Count,
            ["refusalReason"] = noAuthority.RefusalReason,
        };

        // Invalid canonical target: construction outside the contract must be
        // refused at the boundary that owns target identity validation.
        try
        {
            _ = new CodeOccurrenceTargetId("not-a-canonical-target");
            receipt["invalidTargetConstruction"] = new JsonObject { ["outcome"] = "UNEXPECTED: accepted" };
        }
        catch (ArgumentException exception)
        {
            receipt["invalidTargetConstruction"] = new JsonObject
            {
                ["outcome"] = "refused",
                ["exceptionType"] = exception.GetType().Name,
                ["message"] = exception.Message,
            };
        }
        return receipt;
    }

    // Durable graph-side published units, read through the accepted store interface
    // in the fresh reopen process (S5/S6: real persisted graph representation).
    private static async Task<JsonArray> PublishedUnitsAsync(LatticeCodeGraphStore store, CodeRepositoryId repo)
    {
        var units = await store.GetPublishedUnitsAsync(repo);
        var array = new JsonArray();
        foreach (var unit in units.OrderBy(u => u.Origin.IndexUnitId.Value, StringComparer.Ordinal))
        {
            array.Add(new JsonObject
            {
                ["unit"] = unit.Origin.IndexUnitId.Value,
                ["plugin"] = unit.Origin.PluginId.Value,
                ["nodes"] = unit.Nodes.Count,
                ["declarations"] = unit.Declarations.Count,
                ["edges"] = unit.Edges.Count,
                ["requiresOccurrenceAuthority"] = unit.RequiresOccurrenceAuthority,
            });
        }
        return array;
    }

    private static async Task<JsonObject> QueryAsync(LatticeCodeGraphStore store, Scenario scenario, CodeOccurrenceTargetId target)
    {
        var result = await store.FindOccurrencesAsync(
            VerifiedCSharpOccurrenceAuthority.Bind(new(scenario.Repo, target, maxResults: 100), scenario.Analysis.Universe, scenario.App.Compilation));
        return new JsonObject
        {
            ["availability"] = result.Availability.ToString(),
            ["isComplete"] = result.IsComplete,
            ["factCount"] = result.Occurrences.Count,
            ["totalMatches"] = result.TotalMatches,
            ["examined"] = result.Examined,
            ["publicationRun"] = result.Publication?.IndexRunId.Value,
            ["allTargetsMatch"] = result.Occurrences.All(o => o.TargetId.Value == target.Value),
            ["refusalReason"] = result.RefusalReason,
            ["spans"] = new JsonArray(result.Occurrences.Select(o => (JsonNode)$"{o.Span.Path}:{o.Span.StartLine}:{o.Span.StartColumn}").ToArray()),
        };
    }

    // ------------------------------------------------------------------
    // S8: managed recovery (F-02: managed fault injection is not crash or
    // power-loss durability evidence).
    // ------------------------------------------------------------------
    private static async Task<JsonObject> RecoveryAsync(string database)
    {
        var receipt = new JsonObject
        {
            ["action"] = "recovery", ["database"] = database, ["processId"] = Environment.ProcessId,
            ["f02Note"] = "Managed fault injection exercises the accepted recovery contract; it is not evidence of actual process crash or power-loss durability.",
        };
        var corpus = await CorpusScenarioAsync();
        var plugin = new CSharpCodeGraphPlugin();

        using (var store = new LatticeCodeGraphStore(database))
            receipt["baseline"] = await QueryAsync(store, corpus, corpus.Target);

        // Known failure before commit: the last accepted publication must survive
        // both the faulting instance (rebuilt from durable state) and a fresh open.
        var arm = false;
        using (var store = new LatticeCodeGraphStore(database, null, phase => { if (arm && phase == "before-commit") throw new ProbeFault(); }))
        {
            var knownFailure = new JsonObject();
            await store.StoreIndexRunAsync(new(corpus.Repo, new CodeIndexRunId("run:failing"), Extract.Started, plugins: [plugin.Id]));
            await using (var ingestion = new CodeGraphIngestionSink(store))
            {
                var session = await plugin.CreateSessionAsync(Context(corpus.Repo, "run:failing", corpus.Files));
                await session.ExtractAsync(new CaptureSink(ingestion));
                await ingestion.CompleteAsync();
                arm = true;
                try
                {
                    await store.CompleteIndexRunAsync(
                        new(corpus.Repo, new CodeIndexRunId("run:failing"), Extract.Started, CodeIndexRunStatus.Completed, Extract.Started.AddSeconds(1), [plugin.Id]),
                        new(corpus.Repo, new CodeIndexRunId("run:failing"), []));
                    knownFailure["expectedFailureObserved"] = false;
                }
                catch (Exception exception)
                {
                    knownFailure["expectedFailureObserved"] = true;
                    knownFailure["exceptionType"] = exception.GetType().Name;
                }
                arm = false;
            }
            knownFailure["afterFailure"] = await QueryAsync(store, corpus, corpus.Target);
            receipt["knownFailure"] = knownFailure;
        }

        // Unknown outcome after commit: the live instance must quarantine; a fresh
        // open must reconcile against durable state and serve the committed truth.
        var armed2 = false;
        using (var store = new LatticeCodeGraphStore(database, null, phase => { if (armed2 && phase == "after-commit") throw new ProbeFault(); }))
        {
            var unknown = new JsonObject();
            await store.StoreIndexRunAsync(new(corpus.Repo, new CodeIndexRunId("run:unknown"), Extract.Started.AddSeconds(10), plugins: [plugin.Id]));
            await using (var ingestion = new CodeGraphIngestionSink(store))
            {
                var session = await plugin.CreateSessionAsync(Context(corpus.Repo, "run:unknown", corpus.Files));
                await session.ExtractAsync(new CaptureSink(ingestion));
                await ingestion.CompleteAsync();
                armed2 = true;
                try
                {
                    await store.CompleteIndexRunAsync(
                        new(corpus.Repo, new CodeIndexRunId("run:unknown"), Extract.Started.AddSeconds(10), CodeIndexRunStatus.Completed, Extract.Started.AddSeconds(11), [plugin.Id]),
                        new(corpus.Repo, new CodeIndexRunId("run:unknown"), []));
                    unknown["expectedUnknownOutcome"] = false;
                }
                catch (Exception exception)
                {
                    unknown["expectedUnknownOutcome"] = true;
                    unknown["exceptionType"] = exception.GetType().Name;
                }
                armed2 = false;

                unknown["quarantinedRead"] = await CaptureThrownAsync(async () =>
                    await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                        new(corpus.Repo, corpus.Target, maxResults: 5), corpus.Analysis.Universe, corpus.App.Compilation)));
                unknown["quarantinedMutation"] = await CaptureThrownAsync(async () =>
                    await store.StoreIndexRunAsync(new(corpus.Repo, new CodeIndexRunId("run:quarantine-probe"), Extract.Started, plugins: [plugin.Id])));
            }
            receipt["unknownOutcome"] = unknown;
        }
        using (var reconciled = new LatticeCodeGraphStore(database))
            receipt["unknownOutcomeReconciled"] = await QueryAsync(reconciled, corpus, corpus.Target);

        // D5 incompatible persisted occurrence payload: tamper the occurrence page
        // publication binding; a fresh open must refuse fail-closed.
        receipt["tamperedPayloadRefusal"] = TamperOccurrencePage(database);
        return receipt;
    }

    private static async Task<string> CaptureThrownAsync(Func<ValueTask> action)
    {
        try
        {
            await action();
            return "none (unexpected)";
        }
        catch (Exception exception)
        {
            return "throw:" + exception.GetType().Name;
        }
    }

    private static JsonObject TamperOccurrencePage(string database)
    {
        string key;
        JsonObject payload;
        using (var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginWriteTransaction())
        {
            using (var q = db.Prepare("MATCH (s:AndiritOccurrencePage) RETURN s.key, s.payload"))
            using (var r = q.Execute(txn))
            {
                var rows = r.ReadAll();
                var row = rows.First();
                key = row.GetValue(0).AsString();
                payload = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(1).AsString())))!.AsObject();
            }
            payload["PublicationIndexRunId"] = "run:foreign";
            using (var q = db.Prepare("MATCH (s:AndiritOccurrencePage) WHERE s.key = $k SET s.payload = $p"))
            {
                q.Bind("k", LatticeValue.From(key));
                q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload.ToJsonString()))));
                using var r = q.Execute(txn);
                r.ReadAll();
            }
            txn.Commit();
        }
        try
        {
            using var store = new LatticeCodeGraphStore(database);
            return new JsonObject
            {
                ["scenario"] = "occurrence page publication binding altered",
                ["outcome"] = "UNEXPECTED: open succeeded",
            };
        }
        catch (Exception exception)
        {
            return new JsonObject
            {
                ["scenario"] = "occurrence page publication binding altered",
                ["outcome"] = "refused",
                ["openExceptionType"] = exception.GetType().Name,
            };
        }
    }

    // ------------------------------------------------------------------
    // Helpers.
    // ------------------------------------------------------------------
    private static CodeGraphPluginContext Context(CodeRepositoryId repo, string runId, IReadOnlyDictionary<string, string> files) =>
        new(repo, null, new CodeIndexRunId(runId), files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Value);
            return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }).ToArray());

    private static void Cleanup(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }

    private sealed class ProbeFault : Exception;
}
