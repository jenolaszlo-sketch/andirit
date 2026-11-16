using System.Diagnostics;
using Andirit;

internal static partial class Probe
{
    private static async Task Schedule(string path, string fixtures)
    {
        var repo = new CodeRepositoryId("review:s254:schedule");
        var files = Directory.EnumerateFiles(fixtures, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(fixtures, file).Replace('\\', '/'), File.ReadAllBytes);
        var revised = new Dictionary<string, byte[]>(files);
        const string callerFile = "Caller/Fixture1.cs";
        const string method = "QualifierDependency.Api.Mark();";
        revised[callerFile] = System.Text.Encoding.UTF8.GetBytes(
            System.Text.Encoding.UTF8.GetString(files[callerFile]).Replace(method, method + "\n        " + method));
        const string before = "schedule:old";
        const string after = "schedule:new";
        var oldAnalysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, before, files));
        var newAnalysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, after, revised));

        static VerifiedCSharpCompilation Caller(VerifiedCSharpAnalysis analysis) =>
            analysis.Projects.Single(project => project.Project.Path == "Caller/Caller.csproj").Verified!;
        var defining = oldAnalysis.Projects.Single(project => project.Project.Path == "Dependency/Dependency.csproj").Verified!;
        var frozen = AdrCi1Exp.Shared.CompilationProvenance.Describe(repo.Value,
            ProjectUnit("Dependency/Dependency.csproj"), defining.Compilation, [("Dependency/Api.cs", files["Dependency/Api.cs"])]);
        var target = new CodeOccurrenceTargetId("asmctx3:sha256:" + Digest("asmctx3-context", "compctx1",
            frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, Digest("asmctx3-identifier", "Mark"),
            Digest("asmctx3-declarations", "M:QualifierDependency.Api.Mark"), Digest("asmctx3-target", "method|0|")));
        CodeOccurrenceQuery Query(VerifiedCSharpAnalysis analysis, CodeGraphPublication? expected = null) =>
            VerifiedCSharpOccurrenceAuthority.Bind(new(repo, target, expected), analysis.Universe,
                Caller(analysis).Compilation);

        using var atCommit = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var publishing = false;
        using var store = new LatticeCodeGraphStore(path, null, point =>
        {
            if (point != "before-commit" || !publishing)
                return;
            atCommit.Set();
            if (!release.Wait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("Reader barrier was not released in time");
        });

        await PublishCSharp(store, repo, before, files);
        var old = await store.FindOccurrencesAsync(Query(oldAnalysis));
        Receipt["expectedBeforeCount"] = 50;
        Receipt["expectedAfterCount"] = 55;
        Receipt["oldBeforeWriter"] = old;
        Receipt["target"] = target.Value;
        Check(old.Occurrences.Count == 50 && old.IsComplete && old.Publication?.IndexRunId.Value == before,
            "Reader before writer observes complete old generation");

        await store.StoreIndexRunAsync(new(repo, new(after), Started, plugins: [CSharp.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        {
            await using var session = await CSharp.CreateSessionAsync(Context(repo, after, revised));
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }

        publishing = true;
        var write = Task.Run(async () => await store.CompleteIndexRunAsync(
            new(repo, new(after), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [CSharp.Id]),
            new(repo, new(after), [])));
        Task<CodeOccurrenceQueryResult>? queued = null;
        try
        {
            Check(atCommit.Wait(TimeSpan.FromSeconds(15)), "Writer reached native before-commit barrier");
            Receipt["writerAtCommit"] = atCommit.IsSet;
            if (atCommit.IsSet)
            {
                queued = store.FindOccurrencesAsync(Query(newAnalysis)).AsTask();
                Receipt["readerCompletedWhileWriterHeld"] = queued.IsCompleted;
                Check(!queued.IsCompleted, "Reader serializes behind the native writer");
            }
        }
        finally
        {
            release.Set();
        }

        await write.WaitAsync(TimeSpan.FromSeconds(15));
        publishing = false;
        if (queued is not null)
        {
            var current = await queued.WaitAsync(TimeSpan.FromSeconds(15));
            Receipt["queuedReaderAfterPublish"] = current;
            Check(current.Occurrences.Count == 55 && current.IsComplete && current.Publication?.IndexRunId.Value == after,
                "Queued reader observes complete new generation and truthful attribution");
        }
        var stale = await store.FindOccurrencesAsync(Query(newAnalysis, old.Publication));
        Receipt["staleOldHandle"] = stale;
        Check(stale.Availability == CodeOccurrenceAvailability.StalePublication && !stale.IsComplete &&
            stale.Occurrences.Count == 0, "Old publication is refused after the writer completes");
    }
}

