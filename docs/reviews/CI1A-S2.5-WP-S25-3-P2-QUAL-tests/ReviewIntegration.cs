using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Frozen = AdrCi1Exp.Shared;

namespace P2Qual;

// ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01 — reviewer-owned integrated source-to-store
// reproduction (Part B). Fresh disposable store; frozen R2 corpus publishable subset;
// real extraction/batch/sink/stage/complete path; native row inspection; fresh-process
// durable reopen; authority/refusal; managed recovery; tamper refusal.
internal static class ReviewIntegration
{
    internal static readonly CodeRepositoryId Repo = new("p2:integrated");
    private const string RunId = "run:review";
    private static readonly DateTimeOffset Started = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Andirit.slnx"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }

    private static Dictionary<string, string> CorpusFiles()
    {
        var root = Path.Combine(RepoRoot(), "docs", "planning", "adr-ci1-01-ci1a-s2-0", "corpus", "repo");
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var project in new[] { "App", "Core", "Leaf", "Solo" })
            files[$"{project}/{project}.csproj"] = File.ReadAllText(Path.Combine(root, project, $"{project}.csproj"));
        foreach (var file in new[] { "App/Bulk.cs", "App/Sites.cs", "Core/Extensions.cs", "Core/LeafUser.cs",
                     "Core/Methods.cs", "Core/Types.cs", "Leaf/Helper.cs", "Solo/Declarations.cs" })
            files[file] = File.ReadAllText(Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar)));
        return files;
    }

    private static CodeGraphPluginContext Context(string runId, IReadOnlyDictionary<string, string> files) =>
        new(Repo, null, new CodeIndexRunId(runId), files.OrderBy(p => p.Key, StringComparer.Ordinal).Select(pair =>
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Value);
            return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }).ToArray());

    private sealed class ReviewCaptureSink : ICodeGraphSink, ICodeOccurrenceAuthoritySink
    {
        private readonly CodeGraphIngestionSink _inner;
        internal List<CodeGraphBatch> Batches { get; } = [];
        internal List<CodeOccurrenceAuthority> Authorities { get; } = [];
        internal ReviewCaptureSink(CodeGraphIngestionSink inner) => _inner = inner;
        public CodeGraphBatchLimits Limits => _inner.Limits;
        public ValueTask WriteBatchAsync(CodeGraphBatch batch, CancellationToken ct = default)
        {
            Batches.Add(batch);
            return _inner.WriteBatchAsync(batch, ct);
        }
        ValueTask ICodeOccurrenceAuthoritySink.SetOccurrenceAuthorityAsync(
            CodeFactOrigin origin, CodeOccurrenceAuthority authority, CancellationToken ct)
        {
            Authorities.Add(authority);
            return ((ICodeOccurrenceAuthoritySink)_inner).SetOccurrenceAuthorityAsync(origin, authority, ct);
        }
    }

    // Reviewer-owned composed target: production composition cross-checked against the
    // reviewer encoder over the frozen recomputation. Throws if the two disagree.
    private static (string Production, string Independent) ComposedTarget(
        VerifiedCSharpCompilation defining, string idForm, string declForm, string shapeForm)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        var idDigest = Review.Enc.Hash("asmctx3-identifier", idForm);
        var declDigest = Review.Enc.Hash("asmctx3-declarations", declForm);
        var shapeDigest = Review.Enc.Hash("asmctx3-target", shapeForm);
        var production = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity,
            mapped.ModuleName, idDigest, declDigest, shapeDigest).Value;
        var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var independent = "asmctx3:sha256:" + Review.Enc.Hash("asmctx3-context",
        [
            "compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, idDigest, declDigest, shapeDigest,
        ]);
        if (production != independent)
            throw new InvalidDataException("reviewer composition disagrees with production: " + production + " vs " + independent);
        return (production, independent);
    }

    private static async Task<(VerifiedCSharpAnalysis Analysis, VerifiedCSharpCompilation App, CodeOccurrenceTargetId AddTarget,
        CodeOccurrenceTargetId GreetTarget, JsonObject Receipt)> ExpectationsAsync()
    {
        var files = CorpusFiles();
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(RunId, files));
        var app = analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var (addProduction, addIndependent) = ComposedTarget(core, "Add",
            "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)", "method|0|int,int");
        var (greetProduction, greetIndependent) = ComposedTarget(core, "Greet",
            "M:Corpus.Core.IGreeter.Greet(System.String)", "method|0|string");
        var receipt = new JsonObject
        {
            ["add"] = new JsonObject
            {
                ["identifierForm"] = "Add",
                ["declarationForm"] = "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)",
                ["shapeForm"] = "method|0|int,int",
                ["identifierDigest"] = Review.Enc.Hash("asmctx3-identifier", "Add"),
                ["declarationDigest"] = Review.Enc.Hash("asmctx3-declarations", "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)"),
                ["shapeDigest"] = Review.Enc.Hash("asmctx3-target", "method|0|int,int"),
                ["productionTarget"] = addProduction,
                ["independentTarget"] = addIndependent,
                ["agree"] = addProduction == addIndependent,
            },
            ["greet"] = new JsonObject
            {
                ["declarationForm"] = "M:Corpus.Core.IGreeter.Greet(System.String)",
                ["shapeForm"] = "method|0|string",
                ["productionTarget"] = greetProduction,
                ["independentTarget"] = greetIndependent,
                ["agree"] = greetProduction == greetIndependent,
            },
        };
        return (analysis, app, new CodeOccurrenceTargetId(addProduction), new CodeOccurrenceTargetId(greetProduction), receipt);
    }

    private static JsonObject QueryReceipt(CodeOccurrenceQueryResult result, CodeOccurrenceTargetId target)
    {
        var receipt = new JsonObject
        {
            ["availability"] = result.Availability.ToString(),
            ["isComplete"] = result.IsComplete,
            ["factCount"] = result.Occurrences.Count,
            ["totalMatches"] = result.TotalMatches,
            ["examined"] = result.Examined,
            ["publicationRun"] = result.Publication?.IndexRunId.Value,
            ["allTargetsMatch"] = result.Occurrences.All(o => o.TargetId.Value == target.Value),
            ["refusalReason"] = result.RefusalReason,
        };
        var spans = new JsonArray();
        foreach (var occurrence in result.Occurrences.Take(5))
            spans.Add($"{occurrence.Span.Path}:{occurrence.Span.StartLine}:{occurrence.Span.StartColumn}");
        receipt["firstSpans"] = spans;
        return receipt;
    }

    private static void Cleanup(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }

    // ------------------------------------------------------------------
    // Integrate (S1–S5, reviewer-owned).
    // ------------------------------------------------------------------
    public static async Task<JsonObject> IntegrateAsync(string database)
    {
        var receipt = new JsonObject { ["action"] = "integrate", ["database"] = database, ["processId"] = Environment.ProcessId };
        Cleanup(database);
        var files = CorpusFiles();
        var plugin = new CSharpCodeGraphPlugin();
        var batches = new JsonArray();
        var authorities = 0;
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(Repo));
            await store.StoreIndexRunAsync(new(Repo, new CodeIndexRunId(RunId), Started, plugins: [plugin.Id]));
            await using (var ingestion = new CodeGraphIngestionSink(store))
            {
                var capture = new ReviewCaptureSink(ingestion);
                await using (var session = await plugin.CreateSessionAsync(Context(RunId, files)))
                    await session.ExtractAsync(capture);
                await ingestion.CompleteAsync();
                authorities = capture.Authorities.Count;
                foreach (var batch in capture.Batches)
                {
                    batches.Add(new JsonObject
                    {
                        ["unit"] = batch.Origin.IndexUnitId.Value,
                        ["completesIndexUnit"] = batch.CompletesIndexUnit,
                        ["nodes"] = batch.Nodes.Count,
                        ["declarations"] = batch.Declarations.Count,
                        ["edges"] = batch.Edges.Count,
                        ["occurrences"] = batch.Occurrences.Count,
                    });
                }
            }
            await store.CompleteIndexRunAsync(
                new(Repo, new CodeIndexRunId(RunId), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
                new(Repo, new CodeIndexRunId(RunId), []));
        }
        var expectations = await ExpectationsAsync();
        receipt["expectations"] = expectations.Receipt;
        receipt["publication"] = new JsonObject
        {
            ["run"] = RunId, ["status"] = "Completed", ["authorityEnvelopes"] = authorities, ["batches"] = batches,
        };
        using (var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginReadTransaction())
        {
            long Count(string label)
            {
                using var q = db.Prepare($"MATCH (n:{label}) RETURN count(n)");
                using var r = q.Execute(txn);
                var rows = r.ReadAll();
                return rows.Count == 0 ? 0 : rows[0].GetValue(0).AsInt64();
            }
            var unitPayloads = new List<string>();
            using (var q = db.Prepare("MATCH (s:AndiritOccurrenceUnit) RETURN s.payload"))
            using (var r = q.Execute(txn))
                foreach (var row in r.ReadAll())
                    unitPayloads.Add(Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(0).AsString())));
            receipt["nativeRows"] = new JsonObject
            {
                ["occurrenceMetaRows"] = Count("AndiritOccurrenceMeta"),
                ["occurrencePages"] = Count("AndiritOccurrencePage"),
                ["occurrenceUnits"] = Count("AndiritOccurrenceUnit"),
                ["nativeGraphUnitRows"] = Count("HetuUnit"),
                ["expectedTargetPresentInUnits"] = new JsonObject
                {
                    ["add"] = unitPayloads.Any(p => p.Contains(expectations.AddTarget.Value, StringComparison.Ordinal)),
                    ["greet"] = unitPayloads.Any(p => p.Contains(expectations.GreetTarget.Value, StringComparison.Ordinal)),
                },
            };
            txn.Commit();
        }
        return receipt;
    }

    // ------------------------------------------------------------------
    // Reopen (S6–S7, fresh process).
    // ------------------------------------------------------------------
    public static async Task<JsonObject> ReopenAsync(string database)
    {
        var receipt = new JsonObject { ["action"] = "reopen", ["database"] = database, ["processId"] = Environment.ProcessId };
        var (analysis, app, addTarget, greetTarget, expectations) = await ExpectationsAsync();
        receipt["expectations"] = expectations;
        using var store = new LatticeCodeGraphStore(database);
        var add = await store.FindOccurrencesAsync(
            VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, addTarget, maxResults: 100), analysis.Universe, app.Compilation));
        var greet = await store.FindOccurrencesAsync(
            VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, greetTarget, maxResults: 100), analysis.Universe, app.Compilation));
        receipt["addQuery"] = QueryReceipt(add, addTarget);
        receipt["greetQuery"] = QueryReceipt(greet, greetTarget);
        if (add.Publication is { } current)
        {
            var stale = new CodeGraphPublication(Repo, new CodeIndexRunId("run:stale-probe"),
                current.SnapshotIdentity, current.IsConsistentSnapshot, current.IndexIdentity);
            var staleResult = await store.FindOccurrencesAsync(
                VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, addTarget, stale, maxResults: 10), analysis.Universe, app.Compilation));
            receipt["stalePublication"] = new JsonObject
            {
                ["availability"] = staleResult.Availability.ToString(),
                ["factCount"] = staleResult.Occurrences.Count,
            };
        }
        var noAuthority = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, addTarget, maxResults: 10));
        receipt["missingAuthority"] = new JsonObject
        {
            ["availability"] = noAuthority.Availability.ToString(),
            ["factCount"] = noAuthority.Occurrences.Count,
            ["refusalReason"] = noAuthority.RefusalReason,
        };
        try
        {
            _ = new CodeOccurrenceTargetId("not-a-canonical-target");
            receipt["invalidTargetConstruction"] = new JsonObject { ["outcome"] = "UNEXPECTED: accepted" };
        }
        catch (ArgumentException exception)
        {
            receipt["invalidTargetConstruction"] = new JsonObject
            {
                ["outcome"] = "refused", ["exceptionType"] = exception.GetType().Name,
            };
        }
        return receipt;
    }

    // ------------------------------------------------------------------
    // Recovery (S8, fresh process) + tamper refusal.
    // ------------------------------------------------------------------
    public static async Task<JsonObject> RecoverAsync(string database)
    {
        var receipt = new JsonObject
        {
            ["action"] = "recover", ["database"] = database, ["processId"] = Environment.ProcessId,
            ["f02Note"] = "Managed fault injection exercises the accepted recovery contract; it is not crash or power-loss durability evidence.",
        };
        var (analysis, app, addTarget, _, _) = await ExpectationsAsync();
        var plugin = new CSharpCodeGraphPlugin();
        var files = CorpusFiles();

        using (var store = new LatticeCodeGraphStore(database))
            receipt["baseline"] = QueryReceipt(await store.FindOccurrencesAsync(
                VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, addTarget, maxResults: 10), analysis.Universe, app.Compilation)), addTarget);

        // Known failure before commit.
        var arm = false;
        using (var store = new LatticeCodeGraphStore(database, null, phase => { if (arm && phase == "before-commit") throw new ReviewFault(); }))
        {
            var known = new JsonObject();
            await store.StoreIndexRunAsync(new(Repo, new CodeIndexRunId("run:review2"), Started, plugins: [plugin.Id]));
            await using (var ingestion = new CodeGraphIngestionSink(store))
            {
                var session = await plugin.CreateSessionAsync(Context("run:review2", files));
                await session.ExtractAsync(new ReviewCaptureSink(ingestion));
                await ingestion.CompleteAsync();
                arm = true;
                try
                {
                    await store.CompleteIndexRunAsync(
                        new(Repo, new CodeIndexRunId("run:review2"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
                        new(Repo, new CodeIndexRunId("run:review2"), []));
                    known["faultObserved"] = false;
                }
                catch (Exception exception)
                {
                    known["faultObserved"] = true;
                    known["exceptionType"] = exception.GetType().Name;
                }
                arm = false;
            }
            known["afterFailure"] = QueryReceipt(await store.FindOccurrencesAsync(
                VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, addTarget, maxResults: 10), analysis.Universe, app.Compilation)), addTarget);
            receipt["knownFailure"] = known;
        }

        // Unknown outcome after commit: quarantine + fresh-open reconcile.
        var armed = false;
        using (var store = new LatticeCodeGraphStore(database, null, phase => { if (armed && phase == "after-commit") throw new ReviewFault(); }))
        {
            var unknown = new JsonObject();
            await store.StoreIndexRunAsync(new(Repo, new CodeIndexRunId("run:review3"), Started.AddSeconds(10), plugins: [plugin.Id]));
            await using (var ingestion = new CodeGraphIngestionSink(store))
            {
                var session = await plugin.CreateSessionAsync(Context("run:review3", files));
                await session.ExtractAsync(new ReviewCaptureSink(ingestion));
                await ingestion.CompleteAsync();
                armed = true;
                try
                {
                    await store.CompleteIndexRunAsync(
                        new(Repo, new CodeIndexRunId("run:review3"), Started.AddSeconds(10), CodeIndexRunStatus.Completed, Started.AddSeconds(11), [plugin.Id]),
                        new(Repo, new CodeIndexRunId("run:review3"), []));
                    unknown["faultObserved"] = false;
                }
                catch (Exception exception)
                {
                    unknown["faultObserved"] = true;
                    unknown["exceptionType"] = exception.GetType().Name;
                }
                armed = false;
                unknown["quarantinedRead"] = await CaptureThrownAsync(async () => await store.FindOccurrencesAsync(
                    VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, addTarget, maxResults: 5), analysis.Universe, app.Compilation)));
                unknown["quarantinedMutation"] = await CaptureThrownAsync(async () => await store.StoreIndexRunAsync(
                    new(Repo, new CodeIndexRunId("run:quarantine-probe"), Started, plugins: [plugin.Id])));
            }
            receipt["unknownOutcome"] = unknown;
        }
        using (var reconciled = new LatticeCodeGraphStore(database))
            receipt["reconciled"] = QueryReceipt(await reconciled.FindOccurrencesAsync(
                VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, addTarget, maxResults: 10), analysis.Universe, app.Compilation)), addTarget);

        // Incompatible persisted occurrence payload: tamper the page publication binding.
        using (var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginWriteTransaction())
        {
            string key;
            JsonObject payload;
            using (var q = db.Prepare("MATCH (s:AndiritOccurrencePage) RETURN s.key, s.payload"))
            using (var r = q.Execute(txn))
            {
                var row = r.ReadAll().First();
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
            receipt["tamperedPayloadRefusal"] = new JsonObject { ["outcome"] = "UNEXPECTED: open succeeded" };
        }
        catch (Exception exception)
        {
            receipt["tamperedPayloadRefusal"] = new JsonObject
            {
                ["outcome"] = "refused", ["openExceptionType"] = exception.GetType().Name,
            };
        }
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

    private sealed class ReviewFault : Exception;
}
