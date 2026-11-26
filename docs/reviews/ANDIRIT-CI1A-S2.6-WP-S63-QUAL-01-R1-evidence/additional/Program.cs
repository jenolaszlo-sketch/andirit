using System.Reflection;
using System.Runtime.ExceptionServices;
using Andirit;
using Andirit.LatticeDb;
using Andirit.LatticeDb.Tests;
using LatticeDbSharp;

var failures = 0;
using var scratch = new FixtureDirectory();
var repository = new CodeRepositoryId("q2:repository");
var plugin = new CodePluginId("q2:provider");
var start = new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero);
var primary = Target("Primary");
var other = Target("Other");

await Check("deadline helper rejects a real late native handle and releases its file", () =>
{
    var path = Path.Combine(scratch.Path, "deadline.ltdb");
    LatticeException prior;
    using (var held = LatticeDatabase.Open(path, new() { Create = true }))
        prior = Catch<LatticeException>(() => LatticeDatabase.Open(path, new() { Create = false }));
    Require(prior.NativeErrorCode == -16, "Expected native file ownership failure.");
    var clock = new SyntheticClock();
    var opens = 0;
    var timeout = Catch<TimeoutException>(() => InvokeOpen(() =>
    {
        if (++opens == 1) throw prior;
        var opened = LatticeDatabase.Open(path, new() { Create = false });
        clock.Now += 160;
        return opened;
    }, 250, clock, delay => clock.Now += (long)delay.TotalMilliseconds));
    Require(opens == 2 && ReferenceEquals(timeout.InnerException, prior), "Late native handle accepted or diagnostic lost.");
    using var reopened = LatticeDatabase.Open(path, new() { Create = false });
    Console.WriteLine($"late_native_disposed=true attempts={opens} ms={clock.Now} lock_error={prior.NativeErrorCode}");
    return Task.CompletedTask;
});

await Check("delayed scheduling past budget prevents a second open", () =>
{
    var path = Path.Combine(scratch.Path, "overshoot.ltdb");
    using var held = LatticeDatabase.Open(path, new() { Create = true });
    var clock = new SyntheticClock();
    var opens = 0;
    var delays = new List<long>();
    var timeout = Catch<TimeoutException>(() => InvokeOpen(() =>
    {
        opens++;
        return LatticeDatabase.Open(path, new() { Create = false });
    }, 25, clock, delay =>
    {
        delays.Add((long)delay.TotalMilliseconds);
        clock.Now += 40;
    }));
    Require(opens == 1 && delays.SequenceEqual([25L]) && timeout.InnerException is LatticeException { NativeErrorCode: -16 },
        "The helper issued a retry after scheduled time passed the budget.");
    Console.WriteLine($"requested_delay_ms={delays.Single()} observed_monotonic_ms={clock.Now} attempts={opens}");
    return Task.CompletedTask;
});

await Check("a late handle disposal exception remains observable", () =>
{
    var clock = new SyntheticClock();
    var cleanup = new DeliberateException();
    var resource = new ThrowingDisposal(cleanup);
    var thrown = Catch<DeliberateException>(() => InvokeOpen(() =>
    {
        clock.Now = 25;
        return resource;
    }, 25, clock, _ => throw new InvalidOperationException("Unexpected delay.")));
    Require(ReferenceEquals(cleanup, thrown) && resource.Calls == 1, "Cleanup failure was hidden or disposal repeated.");
    Console.WriteLine($"exact_edge_ms={clock.Now} cleanup_calls={resource.Calls} cleanup_failure_propagates=true");
    return Task.CompletedTask;
});

await Check("P-03 measures stale refusal and current target work independently", async () =>
{
    foreach (var (targetCount, unrelatedCount) in new[] { (31, 169), (200, 200), (400, 300) })
    {
        var path = Path.Combine(scratch.Path, $"bounds-{targetCount}.ltdb");
        CodeGraphPublication prior;
        using (var writer = new LatticeCodeGraphStore(path))
        {
            await writer.UpsertRepositoryAsync(new(repository, "P03 independent", "fixture"));
            await Stage(writer, "q2:old", [Fact(primary, "old.cs", 1)]);
            await Complete(writer, "q2:old");
            prior = (await writer.GetLatestPublicationAsync(repository))!;
            var facts = Enumerable.Range(1, targetCount).Select(i => Fact(primary, "new.cs", i))
                .Concat(Enumerable.Range(1, unrelatedCount).Select(i => Fact(other, "unrelated.cs", i))).ToArray();
            await Stage(writer, "q2:current", facts);
            await Complete(writer, "q2:current");
        }
        using var reopened = new LatticeCodeGraphStore(path);
        foreach (var limit in new[] { 1, 100 })
        {
            var stale = await reopened.FindOccurrencesAsync(new(repository, primary, prior, maxResults: limit));
            var fresh = await reopened.FindOccurrencesAsync(new(repository, primary, maxResults: limit));
            Require(stale.Availability == CodeOccurrenceAvailability.StalePublication &&
                stale.Occurrences.Count == 0 && stale.Examined == 0, "A stale handle performed work or served facts.");
            Require(fresh.Availability == CodeOccurrenceAvailability.Available && fresh.Examined == targetCount &&
                fresh.Occurrences.Count == Math.Min(limit, targetCount), "Current work or result limit differs.");
            Require(fresh.Occurrences.Select(hit => hit.Span.StartLine).SequenceEqual(Enumerable.Range(1, Math.Min(limit, targetCount))),
                "Current output differs from the target's site order.");
            Console.WriteLine($"target={targetCount} unrelated={unrelatedCount} limit={limit} stale={stale.Availability} stale_examined={stale.Examined} current_examined={fresh.Examined} returned={fresh.Occurrences.Count}");
        }
    }
});

await Check("P-12 reader queued on a failing write receives the exact prior snapshot", async () =>
{
    var path = Path.Combine(scratch.Path, "failed-overlap.ltdb");
    using var insideWrite = new ManualResetEventSlim();
    using var release = new ManualResetEventSlim();
    var armed = false;
    var previousFacts = new[] { Fact(primary, "prior.cs", 4), Fact(primary, "prior.cs", 9) };
    CodeGraphPublication? prior;
    using (var store = OpenInjected(path, point =>
    {
        if (armed && point == "before-commit")
        {
            insideWrite.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Reviewer did not release checkpoint.");
            throw new DeliberateException();
        }
    }))
    {
        await store.UpsertRepositoryAsync(new(repository, "P12 independent", "fixture"));
        await Stage(store, "q2:prior", previousFacts);
        await Complete(store, "q2:prior");
        prior = await store.GetLatestPublicationAsync(repository);
        await Stage(store, "q2:rejected", [Fact(primary, "rejected.cs", 1)]);
        var source = typeof(LatticeCodeGraphStore).GetField("_inner", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var before = source.GetValue(store);
        armed = true;
        var writer = Task.Run(async () => await Complete(store, "q2:rejected"));
        Task<CodeOccurrenceQueryResult> reader;
        try
        {
            Require(insideWrite.Wait(TimeSpan.FromSeconds(5)), "Native checkpoint not reached.");
            reader = store.FindOccurrencesAsync(new(repository, primary)).AsTask();
            Require(!reader.IsCompleted, "Reader did not queue behind active writer.");
        }
        finally { release.Set(); }
        try { await writer; throw new InvalidOperationException("Injected write unexpectedly committed."); }
        catch (DeliberateException) { }
        Require(!ReferenceEquals(before, source.GetValue(store)), "Prior projection object was not replaced.");
        AssertPrior(await reader);
    }
    using (var reopened = new LatticeCodeGraphStore(path))
        AssertPrior(await reopened.FindOccurrencesAsync(new(repository, primary)));
    Console.WriteLine($"writer_failed=true reader_queued=true reader_run={prior!.IndexRunId.Value} projection_replaced=true reopen_equivalent=true");

    void AssertPrior(CodeOccurrenceQueryResult result)
    {
        Require(result.Availability == CodeOccurrenceAvailability.Available && result.Publication == prior &&
            result.Occurrences.Select(hit => (hit.Id, hit.Span)).SequenceEqual(previousFacts.Select(fact => (fact.Id, fact.Span))),
            "Reader/reopen observed mixed or rejected facts.");
    }
});

return failures == 0 ? 0 : 1;

async Task Check(string name, Func<Task> body)
{
    try { await body(); Console.WriteLine("PASS " + name); }
    catch (Exception error) { failures++; Console.WriteLine($"FAIL {name}\n{error}"); }
}

ValueTask Complete(LatticeCodeGraphStore store, string run) => store.CompleteIndexRunAsync(
    new(repository, new(run), start, CodeIndexRunStatus.Completed, start.AddSeconds(1), [plugin]),
    new(repository, new(run), []));

async Task Stage(LatticeCodeGraphStore store, string run, CodeOccurrenceFact[] facts)
{
    await store.StoreIndexRunAsync(new(repository, new(run), start, plugins: [plugin]));
    await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
        new(repository, plugin, "1.0.0", new(run), new("q2:unit")),
        [new(new("q2:node"), CodeNodeKinds.Type, "Review", "Example.Review", new("symbol:q2"))],
        occurrences: facts, occurrenceAnalysisIncluded: true,
        occurrenceCoverage: new(facts.Length, 0, CodeRelationshipCoverageState.Produced, declaredUnitCount: 1, verifiedUnitCount: 1)));
}

CodeOccurrenceFact Fact(CodeOccurrenceTargetId target, string path, int line) =>
    CodeOccurrenceFact.Create(repository, target, target.Value, "q2:unit", new(plugin, "q2:unit"),
        CodeOccurrenceCategory.Invocation, new(path, line, 1, line, 5), "sha256:q2", "content:q2");

static CodeOccurrenceTargetId Target(string name) => CodeOccurrenceIdentity.ComputeTargetId(
    "compctx1:sha256:" + new string('a', 64), "meta:" + name, "module:q2",
    CodeOccurrenceIdentity.ComputeIdentifierDigest([$"{name}.cs|1|2"]),
    CodeOccurrenceIdentity.ComputeDeclarationDigest([$"{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
    CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));

static T InvokeOpen<T>(Func<T> open, int budget, SyntheticClock clock, Action<TimeSpan> delay) where T : IDisposable
{
    var helper = typeof(NativeDurabilityTests).Assembly.GetType("Andirit.LatticeDb.Tests.NativeOpenDeadline")!
        .GetMethod("Open")!.MakeGenericMethod(typeof(T));
    try { return (T)helper.Invoke(null, [open, TimeSpan.FromMilliseconds(budget), clock, delay])!; }
    catch (TargetInvocationException error) when (error.InnerException is not null)
    {
        ExceptionDispatchInfo.Capture(error.InnerException).Throw();
        throw;
    }
}

static LatticeCodeGraphStore OpenInjected(string path, Action<string> injection) =>
    (LatticeCodeGraphStore)typeof(LatticeCodeGraphStore).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
        null, [typeof(string), typeof(LatticeDbStoreOptions), typeof(Action<string>)], null)!.Invoke([path, null, injection]);

static T Catch<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T error) { return error; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Require(bool condition, string reason)
{
    if (!condition) throw new InvalidOperationException(reason);
}

sealed class SyntheticClock : TimeProvider
{
    public long Now { get; set; }
    public override long TimestampFrequency => 1000;
    public override long GetTimestamp() => Now;
    public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("Wall clock used.");
}

sealed class ThrowingDisposal(Exception error) : IDisposable
{
    public int Calls { get; private set; }
    public void Dispose() { Calls++; throw error; }
}

sealed class FixtureDirectory : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("s63-q2-");
    public string Path => directory.FullName;
    public void Dispose() => directory.Delete(recursive: true);
}

sealed class DeliberateException : Exception;
