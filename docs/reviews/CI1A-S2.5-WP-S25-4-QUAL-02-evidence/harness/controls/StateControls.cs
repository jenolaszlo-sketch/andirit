using System.Text;
using System.Text.Json;
using Andirit;

internal static partial class Probe
{
    private static async Task StageReal(LatticeCodeGraphStore store, CodeRepositoryId repo, string run,
         Dictionary<string, byte[]> files)
    {
        await store.StoreIndexRunAsync(new(repo, new(run), Started, plugins: [CSharp.Id]));
        await using var sink = new CodeGraphIngestionSink(store);
        await using var session = await CSharp.CreateSessionAsync(Context(repo, run, files));
        await session.ExtractAsync(sink);
        await sink.CompleteAsync();
    }

    private static async Task CompleteReal(LatticeCodeGraphStore store, CodeRepositoryId repo, string run) =>
        await store.CompleteIndexRunAsync(new(repo, new(run), Started, CodeIndexRunStatus.Completed,
            Started.AddSeconds(1), [CSharp.Id]), new(repo, new(run), []));

    private static Dictionary<string, string[]> NativeFacts(string path)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ReadNative(path)));
        return doc.RootElement.GetProperty("AndiritOccurrenceUnit").EnumerateArray()
            .ToDictionary(row => row.GetProperty("key").GetString()!.Split('\n')[^1], row =>
            {
                using var raw = JsonDocument.Parse(Convert.FromBase64String(row.GetProperty("payload").GetString()!));
                return raw.RootElement.GetProperty("Facts").EnumerateArray()
                    .Select(fact => fact.GetProperty("Id").GetProperty("Value").GetString()!).Order(StringComparer.Ordinal).ToArray();
            });
    }

    private static async Task StateControls(string phase, string path, string fixtures)
    {
        var parts = phase.Split('-');
        var scenario = parts[1];
        var operation = parts[2];
        var repo = new CodeRepositoryId("review:s254:state:" + scenario);
        var files = LoadControlFiles(fixtures);
        var otherOwner = ProjectUnit("Other/Other.csproj");
        var callerOwner = ProjectUnit(CallerProject);
        const string otherPath = "Other/Entry.cs";
        const string otherDoc = "M:Independent.Entry.Answer";
        if (scenario == "lifecycle")
        {
            files["Other/Other.csproj"] = Encoding.UTF8.GetBytes(
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            files[otherPath] = Encoding.UTF8.GetBytes(
                "namespace Independent;\npublic static class Entry {\n    public static int Answer() => 7;\n" +
                "    public static int Call() => Answer();\n}\n");
        }
        var revised = new Dictionary<string, byte[]>(files);
        const string callerFile = "Caller/Fixture1.cs";
        revised[callerFile] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files[callerFile])
            .Replace("QualifierDependency.Api.Mark();", "   QualifierDependency.Api.Mark();", StringComparison.Ordinal));
        Receipt["indexedSource"] = files.ToDictionary(pair => pair.Key, pair => Convert.ToBase64String(pair.Value));
        Receipt["replacementInput"] = Convert.ToBase64String(revised[callerFile]);

        var oldAnalysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "old", files));
        var newAnalysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "new", revised));
        var defining = oldAnalysis.Projects.Single(project => project.Project.Path == DependencyProject).Verified!;
        var target = new CodeOccurrenceTargetId(MarkTarget(repo.Value, defining, files[DependencyPath]));
        CodeOccurrenceQuery Query(VerifiedCSharpAnalysis analysis, int pageSize = 100, CodeOccurrenceTargetId? desired = null) =>
            VerifiedCSharpOccurrenceAuthority.Bind(new(repo, desired ?? target, maxResults: pageSize), analysis.Universe,
                analysis.Projects.Single(project => project.Project.Path == CallerProject).Compilation);

        string[] ExpectedCaller(Dictionary<string, byte[]> sources)
        {
            var ids = new List<string>();
            foreach (var pair in sources.Where(pair => pair.Key.StartsWith("Caller/Fixture", StringComparison.Ordinal)))
            {
                var lines = Encoding.UTF8.GetString(pair.Value).Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    var found = lines[i].IndexOf("Api.Mark();", StringComparison.Ordinal);
                    if (found < 0) continue;
                    ids.Add(Usage(repo.Value, CSharp.Id.Value, callerOwner,
                        new CodeLocation(pair.Key, i + 1, found + 5, i + 1, found + 9),
                        FileDigest(pair.Value), MarkDocument, target.Value));
                }
            }
            return ids.Order(StringComparer.Ordinal).ToArray();
        }

        var oldIds = ExpectedCaller(files);
        var newIds = ExpectedCaller(revised);
        string[] Observed(CodeOccurrenceQueryResult result) => result.Occurrences
            .Select(fact => fact.Id.Value).Order(StringComparer.Ordinal).ToArray();
        Receipt["expected"] = new { oldIds, newIds, callerOwner, target = target.Value };
        Check(oldIds.Length == 50 && newIds.Length == 50 && oldIds.Except(newIds).Count() == 5,
            "Five moved sites determine the exact old and new generation");
        string? fault = null;
        using (var store = new LatticeCodeGraphStore(path, null, point =>
        {
            if (point == fault)
                throw new ReviewInjectedFault(point);
        }))
        {
            if (operation == "publish")
                await PublishCSharp(store, repo, "old", files);
            if (scenario == "lifecycle")
            {
                var other = oldAnalysis.Projects.Single(project => project.Project.Path == "Other/Other.csproj").Verified!;
                var frozen = AdrCi1Exp.Shared.CompilationProvenance.Describe(repo.Value, otherOwner,
                    other.Compilation, [(otherPath, files[otherPath])]);
                var otherTarget = "asmctx3:sha256:" + Digest("asmctx3-context", "compctx1", frozen.DescriptorDigest,
                    frozen.MetadataIdentity, frozen.ModuleName, Digest("asmctx3-identifier", "Answer"),
                    Digest("asmctx3-declarations", otherDoc), Digest("asmctx3-target", "method|0|"));
                var otherId = Usage(repo.Value, CSharp.Id.Value, otherOwner, new(otherPath, 4, 33, 4, 39),
                    FileDigest(files[otherPath]), otherDoc, otherTarget);
                Receipt["unrelatedExpected"] = new { otherOwner, otherTarget, otherId, frozen };
                if (operation is "publish" or "replace" or "reopen")
                {
                    if (operation == "replace")
                        await PublishCSharp(store, repo, "new", revised);
                    var observed = await store.FindOccurrencesAsync(Query(operation == "publish" ? oldAnalysis : newAnalysis));
                    Receipt["query"] = observed;
                    Check(observed.IsComplete && Observed(observed).SequenceEqual(operation == "publish" ? oldIds : newIds),
                        "Current live caller occurrence set matches the independently derived publication");
                    var authority = operation == "publish" ? oldAnalysis : newAnalysis;
                    var otherQuery = VerifiedCSharpOccurrenceAuthority.Bind(new(repo, new(otherTarget)),
                        authority.Universe, authority.Projects.Single(project => project.Project.Path == "Other/Other.csproj").Compilation);
                    var unrelated = await store.FindOccurrencesAsync(otherQuery);
                    Receipt["unrelatedQuery"] = unrelated;
                    Check(unrelated.IsComplete && Observed(unrelated).SequenceEqual([otherId]),
                        "Unrelated project occurrence remains queryable with the exact identity");
                }
                if (operation == "delete")
                    await Run(store, repo, "delete", [CSharp.Id], async () =>
                        await store.StageIndexUnitDeletionAsync(repo, new("delete"), CSharp.Id, new(callerOwner)));
                store.Dispose();
                var facts = NativeFacts(path);
                Receipt["nativeIdsByUnit"] = facts;
                Check(facts[otherOwner].SequenceEqual([otherId]), "Native unrelated unit retains its verified occurrence");
                var deleted = operation is "delete" or "deletedreopen";
                Check(deleted ? !facts.ContainsKey(callerOwner) :
                    facts[callerOwner].SequenceEqual(operation == "publish" ? oldIds : newIds),
                    "Native caller state matches publication, replacement, or deletion");
                return;
            }

            if (operation == "publish" && scenario == "supersession")
            {
                var oldPage = await store.FindOccurrencesAsync(Query(oldAnalysis, 7));
                Receipt["continuationSeed"] = oldPage;
                Check(oldPage.NextContinuationToken is not null && oldPage.Occurrences.Count == 7,
                    "Independently expected seven results and a continuation on the old publication");
                var otherTarget = new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('9', 64));
                var wrongTarget = await store.FindOccurrencesAsync(Query(oldAnalysis, 7, otherTarget) with
                {

                    ContinuationToken = oldPage.NextContinuationToken
                });
                Receipt["wrongTargetContinuation"] = wrongTarget;
                Check(wrongTarget.Continuation == CodeOccurrenceContinuationState.Invalid &&
                    wrongTarget.Occurrences.Count == 0 && !wrongTarget.IsComplete,
                    "A continuation token for a different target is rejected");
                await StageReal(store, repo, "stale", files);
                await PublishCSharp(store, repo, "new", revised);
                var oldToken = await store.FindOccurrencesAsync(Query(newAnalysis, 7) with
                {
                    ContinuationToken = oldPage.NextContinuationToken
                });
                Receipt["stalePublicationContinuation"] = oldToken;
                Check(oldToken.Continuation == CodeOccurrenceContinuationState.Invalid &&
                    oldToken.Occurrences.Count == 0 && !oldToken.IsComplete,
                    "A continuation token from a superseded publication is rejected");
                try
                {
                    await CompleteReal(store, repo, "stale");
                    Check(false, "A superseded real writer was accepted");
                }
                catch (InvalidOperationException error)
                {
                    Receipt["supersededWriterRefusal"] = error.Message;
                }
            }

            if (operation == "publish" && scenario is "known" or "unknown")
            {
                var before = await store.FindOccurrencesAsync(Query(oldAnalysis));
                var graphBefore = await GraphSnapshot(store, repo);
                Receipt["beforeFault"] = new { result = before, graph = graphBefore };
                await StageReal(store, repo, "new", revised);
                fault = scenario == "known" ? "before-commit" : "after-commit";
                try
                {
                    await CompleteReal(store, repo, "new");
                    Check(false, "Expected fault was not reached");
                }
                catch (Exception error) when (error is ReviewInjectedFault or CodeGraphCommitOutcomeUnknownException)
                {
                    Receipt["fault"] = new { point = fault, type = error.GetType().Name, error.Message };
                    Check(scenario == "known" ? error is ReviewInjectedFault : error is CodeGraphCommitOutcomeUnknownException,
                        "Commit outcome classification agrees with injection point");
                }
                finally { fault = null; }

                if (scenario == "unknown")
                {
                    await Quarantined("occurrences", async () => { await store.FindOccurrencesAsync(Query(newAnalysis)); });
                    await Quarantined("mutation", async () =>
                        await store.StoreIndexRunAsync(new(repo, new("quarantined"), Started, plugins: [CSharp.Id])));
                    return;
                }
                var restored = await store.FindOccurrencesAsync(Query(oldAnalysis));
                var graphRestored = await GraphSnapshot(store, repo);
                Receipt["afterKnownFault"] = new { result = restored, graph = graphRestored };
                Check(JsonSerializer.Serialize(before) == JsonSerializer.Serialize(restored) &&
                    JsonSerializer.Serialize(graphBefore) == JsonSerializer.Serialize(graphRestored),
                    "Known fault preserves old graph, authority, coverage, and occurrences immediately");
                var prior = (await store.GetPublishedUnitsAsync(repo))
                    .SelectMany(unit => unit.Occurrences).Single(fact => fact.Id == restored.Occurrences[0].Id);
                const string conflictRun = "conflict";
                const string conflictOwner = "review:conflicting-storage";
                var conflicting = new CodeOccurrenceFact(prior.Id, prior.TargetId, prior.TargetDocumentId,
                    prior.RepositoryId, prior.Category, prior.SourceUnitId, new(CSharp.Id, conflictOwner),
                    prior.Span, prior.SourceHash, FileDigest(Encoding.UTF8.GetBytes("conflicting payload")),
                    prior.DeclarationSpan, prior.SemanticsVersion, prior.PluginVersion);
                Receipt["conflictInputs"] = new { prior, conflicting };
                await store.StoreIndexRunAsync(new(repo, new(conflictRun), Started, plugins: [CSharp.Id]));
                var origin = new CodeFactOrigin(repo, CSharp.Id, prior.PluginVersion, new(conflictRun), new(conflictOwner));
                try
                {
                    await store.StageIndexUnitAsync(new(origin, occurrences: [conflicting],
                        occurrenceAnalysisIncluded: true, occurrenceCoverage: new(1, 0, CodeRelationshipCoverageState.Produced)));
                    Check(false, "Conflicting immutable payload was accepted after recovery");
                }
                catch (CodeGraphBatchRejectedException error)
                {
                    Receipt["postRecoveryConflict"] = new { type = error.GetType().Name, error.Message, error.Errors };
                    Check(error.Errors.Any(item => item.Code == "occurrence.payload.conflict" && item.FactId == prior.Id.Value),
                        "Same-ID payload conflict is reported explicitly");
                }
                var afterConflict = await store.FindOccurrencesAsync(Query(oldAnalysis));
                Receipt["afterConflict"] = afterConflict;
                Check(JsonSerializer.Serialize(before) == JsonSerializer.Serialize(afterConflict) &&
                    JsonSerializer.Serialize(graphBefore) == JsonSerializer.Serialize(await GraphSnapshot(store, repo)),
                    "Post-recovery rejection preserves accepted graph and occurrences");
            }

            var expectNew = scenario is "supersession" or "unknown";
            var result = await store.FindOccurrencesAsync(Query(expectNew ? newAnalysis : oldAnalysis));
            Receipt["query"] = result;
            Check(result.IsComplete && result.Publication?.IndexRunId.Value == (expectNew ? "new" : "old") &&
                Observed(result).SequenceEqual(expectNew ? newIds : oldIds),
                "Bound native query exposes exactly the accepted graph generation");
            Receipt["graph"] = await GraphSnapshot(store, repo);
        }
        Receipt["native"] = ReadNative(path);

        async Task Quarantined(string kind, Func<Task> action)
        {
            try
            {
                await action();
                Check(false, "Unknown outcome must quarantine " + kind);
            }
            catch (CodeGraphCommitOutcomeUnknownException error)
            {
                Receipt["quarantined:" + kind] = error.Message;
            }
        }
    }

    private sealed class ReviewInjectedFault(string point) : Exception(point);
}




