using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;

namespace Andirit.CSharp.Tests;

// ANDIRIT-CI1A-S2.4-WP-S24-1-IMPL-01 focused implementation tests.
//
// Exercise the internal exact-target occurrence continuation and scope-level
// coverage breakdown against the real native LatticeCodeGraphStore on Windows
// x64, using the accepted staging publication path. These are producer-focused
// implementation tests; they are NOT independent qualification.
public sealed class S24ReaderContinuationTests : IDisposable
{
    private static readonly CodePluginId Plugin = new("plugin:s24");
    private static readonly DateTimeOffset Started = new(2026, 11, 8, 0, 0, 0, TimeSpan.Zero);
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "andirit-s24-" + Guid.NewGuid().ToString("N") + ".ltdb");

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
    }

    // 1. Single-page completion.
    [Fact]
    public async Task SinglePage_CompletesWithNoNextToken()
    {
        var repo = new CodeRepositoryId("repo:s24:single");
        var target = Target(repo, "T");
        using var store = await PublishedAsync(repo, "run", Unit(repo, "run", "u",
            [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1), Fact(repo, "u", target, "src/A.cs", 3, 1)]));
        var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        Assert.Equal(3, result.Occurrences.Count);
        Assert.Equal(3, result.TotalMatches);
        Assert.Equal(3, result.Examined);
        Assert.False(result.Truncated);
        Assert.Equal(CodeOccurrenceContinuationState.Complete, result.Continuation);
        Assert.Null(result.NextContinuationToken);
    }

    // 2/6/7. Multi-page traversal, exact boundary, completed set.
    [Fact]
    public async Task MultiPage_TraversesEveryOccurrenceExactlyOnce()
    {
        var repo = new CodeRepositoryId("repo:s24:multi");
        var target = Target(repo, "T");
        var facts = Enumerable.Range(1, 5).Select(i => Fact(repo, "u", target, "src/A.cs", i, 1)).ToList();
        using var store = await PublishedAsync(repo, "run", Unit(repo, "run", "u", facts));

        var seen = new List<string>();
        string? token = null;
        var pages = 0;
        while (true)
        {
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 2) { ContinuationToken = token });
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.True(result.Occurrences.Count <= 2);
            Assert.Equal(5, result.TotalMatches);
            seen.AddRange(result.Occurrences.Select(o => o.Id.Value));
            pages++;
            if (result.NextContinuationToken is null)
            {
                Assert.Equal(CodeOccurrenceContinuationState.Complete, result.Continuation);
                break;
            }
            Assert.Equal(CodeOccurrenceContinuationState.More, result.Continuation);
            token = result.NextContinuationToken;
        }

        Assert.Equal(3, pages); // 2 + 2 + 1
        Assert.Equal(5, seen.Count);
        Assert.Equal(5, seen.Distinct(StringComparer.Ordinal).Count()); // no duplicates/omissions
    }

    // 3. Equal-position deterministic ties (same span, distinct identity by id).
    [Fact]
    public async Task EqualPosition_TiesAreOrderedByIdWithoutDuplicates()
    {
        var repo = new CodeRepositoryId("repo:s24:ties");
        var target = Target(repo, "T");
        var facts = new[]
        {
            Fact(repo, "u", target, "src/A.cs", 1, 1, semanticUnitId: "s1"),
            Fact(repo, "u", target, "src/A.cs", 1, 1, semanticUnitId: "s2"),
            Fact(repo, "u", target, "src/A.cs", 1, 1, semanticUnitId: "s3"),
        };
        using var store = await PublishedAsync(repo, "run", Unit(repo, "run", "u", facts));

        var ordered = await ReadAllAsync(store, repo, target, pageSize: 1);
        Assert.Equal(3, ordered.Count);
        Assert.Equal(ordered.Select(o => o.Id.Value).Order(StringComparer.Ordinal), ordered.Select(o => o.Id.Value));
        Assert.All(ordered, o => Assert.Equal(1, o.Span.StartLine));
    }

    // 4. Overload identity: distinct targets are independent pages.
    [Fact]
    public async Task Overloads_HaveIndependentContinuationPages()
    {
        var repo = new CodeRepositoryId("repo:s24:overload");
        var t0 = Target(repo, "Ping0");
        var t1 = Target(repo, "Ping1");
        using var store = await PublishedAsync(repo, "run",
            Unit(repo, "run", "u",
                [Fact(repo, "u", t0, "src/A.cs", 1, 1), Fact(repo, "u", t0, "src/A.cs", 2, 1),
                 Fact(repo, "u", t1, "src/A.cs", 1, 1)]));

        var zero = await ReadAllAsync(store, repo, t0, pageSize: 1);
        var one = await ReadAllAsync(store, repo, t1, pageSize: 1);
        Assert.Equal(2, zero.Count);
        Assert.Single(one);
        Assert.All(zero, o => Assert.Equal(t0, o.TargetId));
        Assert.All(one, o => Assert.Equal(t1, o.TargetId));
    }

    // 5. Cross-unit contributor deduplication: one logical occurrence, one hit.
    [Fact]
    public async Task CrossUnitContributors_AreDeduplicatedAcrossPages()
    {
        var repo = new CodeRepositoryId("repo:s24:union");
        var target = Target(repo, "T");
        using var store = await PublishedAsync(repo, "run",
            FactUnit(repo, "run", "x", target, "shared"),
            FactUnit(repo, "run", "y", target, "shared"));

        var page1 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        var hit = Assert.Single(page1.Occurrences);
        Assert.Equal(2, hit.Contributors.Count);
        Assert.Equal(CodeOccurrenceContinuationState.Complete, page1.Continuation);
        Assert.Null(page1.NextContinuationToken);
    }

    // 8/12 (publication binding). Stale publication between pages.
    [Fact]
    public async Task StalePublicationBetweenPages_IsRefusedNotBlended()
    {
        var repo = new CodeRepositoryId("repo:s24:stale");
        var target = Target(repo, "T");
        using var store = new LatticeCodeGraphStore(_path);
        await PublishAsync(store, repo, "run:1", Unit(repo, "run:1", "u",
            [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)]));
        var page1 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        Assert.NotNull(page1.NextContinuationToken);

        await PublishAsync(store, repo, "run:2", Unit(repo, "run:2", "u",
            [Fact(repo, "u", target, "src/B.cs", 9, 1)]));

        var stale = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = page1.NextContinuationToken });
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
        Assert.Empty(stale.Occurrences);
        Assert.Equal(CodeOccurrenceContinuationState.Invalid, stale.Continuation);
        Assert.False(stale.IsComplete);
    }

    // 9. Recovery between pages preserves the same publication.
    [Fact]
    public async Task RecoveryBetweenPages_PreservesPublicationAndContinuation()
    {
        var repo = new CodeRepositoryId("repo:s24:recovery");
        var target = Target(repo, "T");
        using var store = new LatticeCodeGraphStore(_path, null, point =>
            { if (arm && point == "before-commit") throw new InjectedFault(); });
        await PublishAsync(store, repo, "run", Unit(repo, "run", "u",
            [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1), Fact(repo, "u", target, "src/A.cs", 3, 1)]));
        var page1 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        Assert.NotNull(page1.NextContinuationToken);

        // Trigger the accepted known-failure recovery rebuild.
        await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:fail"), Started.AddSeconds(5), plugins: [Plugin]));
        await store.StageIndexUnitAsync(Unit(repo, "run:fail", "u2",
            [Fact(repo, "u2", target, "src/Z.cs", 1, 1)]));
        arm = true;
        await Assert.ThrowsAsync<InjectedFault>(async () => await store.CompleteIndexRunAsync(
            new(repo, new CodeIndexRunId("run:fail"), Started.AddSeconds(5), CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
            new(repo, new CodeIndexRunId("run:fail"), [])));
        arm = false;

        var page2 = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = page1.NextContinuationToken });
        Assert.Equal(CodeOccurrenceAvailability.Available, page2.Availability);
        Assert.Single(page2.Occurrences);
        Assert.NotEqual(page1.Occurrences[0].Id, page2.Occurrences[0].Id);
    }

    // 10/11/12. Malformed, altered, oversized, wrong-binding tokens.
    [Fact]
    public async Task MalformedAndMismatchedTokens_AreRefusedAsInvalid()
    {
        var repo = new CodeRepositoryId("repo:s24:tokens");
        var target = Target(repo, "T");
        var other = Target(repo, "Other");
        using var store = await PublishedAsync(repo, "run",
            Unit(repo, "run", "u",
                [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1),
                 Fact(repo, "u", other, "src/A.cs", 1, 1)]));
        var page1 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        var good = page1.NextContinuationToken!;
        var publication = (await store.GetLatestPublicationAsync(repo))!;

        // Malformed / oversized.
        await AssertInvalidAsync(store, repo, target, "not-a-token");
        await AssertInvalidAsync(store, repo, target, new string('A', 5000));
        // Altered: change the leading character (reliably changes the decoded
        // envelope bytes, so the checksum/parse fails).
        var altered = (good[0] == 'A' ? 'B' : 'A') + good[1..];
        await AssertInvalidAsync(store, repo, target, altered);
        // Wrong target binding: build a valid token for `other` and present it for `target`.
        var wrongTarget = CodeOccurrenceContinuation.Encode(new CodeOccurrenceContinuation.Envelope(
            repo.Value, CodeOccurrenceContinuation.TargetMode, other.Value, publication.IndexRunId.Value,
            publication.IndexIdentity.Value, CodeOccurrenceContinuation.Ascending, "src/A.cs", 1, 1, 1, 4, "deadbeef", 1));
        await AssertInvalidAsync(store, repo, target, wrongTarget);
        // Wrong mode binding.
        var wrongMode = CodeOccurrenceContinuation.Encode(new CodeOccurrenceContinuation.Envelope(
            repo.Value, "targets", target.Value, publication.IndexRunId.Value, publication.IndexIdentity.Value,
            CodeOccurrenceContinuation.Ascending, "src/A.cs", 1, 1, 1, 4, "deadbeef", 1));
        await AssertInvalidAsync(store, repo, target, wrongMode);
        // Out-of-range page-size binding.
        var wrongSize = CodeOccurrenceContinuation.Encode(new CodeOccurrenceContinuation.Envelope(
            repo.Value, CodeOccurrenceContinuation.TargetMode, target.Value, publication.IndexRunId.Value,
            publication.IndexIdentity.Value, CodeOccurrenceContinuation.Ascending, "src/A.cs", 1, 1, 1, 4, "deadbeef", 0));
        await AssertInvalidAsync(store, repo, target, wrongSize);
    }

    [Fact]
    public async Task TokenBoundToAnotherRepository_IsRefused()
    {
        var repoA = new CodeRepositoryId("repo:s24:repo-a");
        var repoB = new CodeRepositoryId("repo:s24:repo-b");
        var targetA = Target(repoA, "T");
        var targetB = Target(repoB, "T");
        using var store = new LatticeCodeGraphStore(_path);
        await PublishAsync(store, repoA, "run", Unit(repoA, "run", "u",
            [Fact(repoA, "u", targetA, "src/A.cs", 1, 1), Fact(repoA, "u", targetA, "src/A.cs", 2, 1)]));
        await PublishAsync(store, repoB, "run", Unit(repoB, "run", "u",
            [Fact(repoB, "u", targetB, "src/A.cs", 1, 1), Fact(repoB, "u", targetB, "src/A.cs", 2, 1)]));

        var tokenA = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repoA, targetA, maxResults: 1))).NextContinuationToken!;
        var crossed = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repoB, targetB, maxResults: 1) { ContinuationToken = tokenA });
        Assert.Equal(CodeOccurrenceContinuationState.Invalid, crossed.Continuation);
        Assert.Empty(crossed.Occurrences);
    }

    // 14. Candidate/work bounds: a small page still reports the bounded target page.
    [Fact]
    public async Task SmallPage_DoesNotHideAnUnboundedScan()
    {
        var repo = new CodeRepositoryId("repo:s24:bounds");
        var target = Target(repo, "T");
        var facts = Enumerable.Range(1, 40).Select(i => Fact(repo, "u", target, "src/A.cs", i, 1)).ToList();
        using var store = await PublishedAsync(repo, "run", Unit(repo, "run", "u", facts));
        var page = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        Assert.Single(page.Occurrences);
        Assert.Equal(40, page.TotalMatches);
        Assert.Equal(40, page.Examined); // target page candidates only, bounded by the target
        Assert.True(page.Truncated);
        Assert.NotNull(page.NextContinuationToken);
    }

    // 15/16. Scope-level coverage consistency across pages; partial coverage continues.
    [Fact]
    public async Task PartialCoverage_RemainsIncompleteAcrossPages()
    {
        var repo = new CodeRepositoryId("repo:s24:partial");
        var target = Target(repo, "T");
        using var store = await PublishedAsync(repo, "run",
            Unit(repo, "run", "u",
                [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)],
                CodeRelationshipCoverageState.Partial));

        var page1 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, page1.Availability);
        Assert.False(page1.IsComplete);
        var page2 = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = page1.NextContinuationToken });
        Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, page2.Availability);
        Assert.False(page2.IsComplete);
        Assert.Equal(page1.Coverage!.State, page2.Coverage!.State);
        var unitCoverage = Assert.Single(page1.UnitCoverage);
        Assert.Equal(CodeRelationshipCoverageState.Partial, unitCoverage.State);
        Assert.Equal("u", unitCoverage.IndexUnitId);
    }

    // 17. Unavailable/refused contributor: graph-only repository.
    [Fact]
    public async Task GraphOnlyRepository_IsUnavailableWithEmptyBreakdown()
    {
        var repo = new CodeRepositoryId("repo:s24:graphonly");
        var target = Target(repo, "T");
        using var store = new LatticeCodeGraphStore(_path);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run"), Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run"), new CodeIndexUnitId("g")),
            [Node("node:g")]));
        await store.CompleteIndexRunAsync(
            new(repo, new CodeIndexRunId("run"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, new CodeIndexRunId("run"), []));

        var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, result.Availability);
        Assert.Empty(result.UnitCoverage);
    }

    // 18. Legacy reader compatibility: no-token query returns all results.
    [Fact]
    public async Task LegacyQuery_WithoutToken_ReturnsAll()
    {
        var repo = new CodeRepositoryId("repo:s24:legacy");
        var target = Target(repo, "T");
        using var store = await PublishedAsync(repo, "run", Unit(repo, "run", "u",
            [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)]));
        var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
        Assert.Equal(2, result.Occurrences.Count);
        Assert.False(result.Truncated);
        Assert.Null(result.NextContinuationToken);
    }

    // 19. D3 graph noninterference: pagination does not change graph or row counts.
    [Fact]
    public async Task Pagination_DoesNotChangeGraphOrPersistedRows()
    {
        var repo = new CodeRepositoryId("repo:s24:d3");
        var target = Target(repo, "T");
        using (var store = await PublishedAsync(repo, "run", Unit(repo, "run", "u",
            [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1), Fact(repo, "u", target, "src/A.cs", 3, 1)])))
        {
            var unitsBefore = (await store.GetPublishedUnitsAsync(repo)).SelectMany(u => u.Nodes).Select(n => n.Id.Value).Order(StringComparer.Ordinal).ToArray();
            await ReadAllAsync(store, repo, target, pageSize: 1);
            var unitsAfter = (await store.GetPublishedUnitsAsync(repo)).SelectMany(u => u.Nodes).Select(n => n.Id.Value).Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(unitsBefore, unitsAfter);
            Assert.DoesNotContain(target.Value, unitsAfter);
        }

        // 20. No persisted token/session: the occurrence rows are unchanged by reads.
        var rows = CountRows(_path);
        using (var store = new LatticeCodeGraphStore(_path))
            await ReadAllAsync(store, repo, target, pageSize: 1);
        Assert.Equal(rows, CountRows(_path));
    }

    // 13. Per-page authority revalidation (real verified authority).
    [Fact]
    public async Task PerPageAuthority_IsRevalidatedOnEveryContinuation()
    {
        var a = await S23NativeAuthorityFixture.Build();
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);

        using var reopened = new LatticeCodeGraphStore(_path);
        CodeOccurrenceQuery Bind(string? token) => VerifiedCSharpOccurrenceAuthority.Bind(
            new(S23NativeAuthorityFixture.Repo, a.LibTarget, maxResults: 1) { ContinuationToken = token },
            a.Universe, a.App.Compilation);

        var page1 = await reopened.FindOccurrencesAsync(Bind(null));
        Assert.Equal(CodeOccurrenceAvailability.Available, page1.Availability);
        Assert.Single(page1.Occurrences);
        var token = page1.NextContinuationToken;
        Assert.NotNull(token);

        // Same token without current authority: refused, never a partial page.
        var noAuthority = await reopened.FindOccurrencesAsync(
            new CodeOccurrenceQuery(S23NativeAuthorityFixture.Repo, a.LibTarget, maxResults: 1) { ContinuationToken = token });
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, noAuthority.Availability);
        Assert.Empty(noAuthority.Occurrences);

        // Same token with current authority: continues.
        var page2 = await reopened.FindOccurrencesAsync(Bind(token));
        Assert.Equal(CodeOccurrenceAvailability.Available, page2.Availability);
        Assert.Single(page2.Occurrences);
        Assert.NotEqual(page1.Occurrences[0].Id, page2.Occurrences[0].Id);
    }

    // R1/R2 (F-Q1-01, F-OBS-Q1-02): every malformed caller-controlled token is
    // refused as Invalid continuation and never propagates an exception.
    [Fact]
    public async Task MalformedTokens_FailClosedWithoutPropagating()
    {
        var repo = new CodeRepositoryId("repo:s24:malformed");
        var target = Target(repo, "T");
        using var store = await PublishedAsync(repo, "run",
            Unit(repo, "run", "u", [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)]));
        var publication = (await store.GetLatestPublicationAsync(repo))!;
        var good = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
        var afterId = Hex("cursor"); // canonical 64-hex occurrence id
        var repoV = repo.Value;
        var targetV = target.Value;
        var runV = publication.IndexRunId.Value;
        var idV = publication.IndexIdentity.Value;
        var fakeSum = "0000000000000000";
        var valid = $"{{\"v\":1,\"repo\":\"{repoV}\",\"mode\":\"target\",\"target\":\"{targetV}\",\"pubRun\":\"{runV}\",\"pubId\":\"{idV}\",\"dir\":\"asc\",\"afterPath\":\"src/A.cs\",\"afterStartLine\":1,\"afterStartColumn\":1,\"afterEndLine\":1,\"afterEndColumn\":4,\"afterId\":\"{afterId}\",\"size\":1,\"sum\":\"{fakeSum}\"}}";

        var cases = new (string Name, string Token)[]
        {
            ("duplicate-v", RawToken(valid.Replace("\"v\":1,", "\"v\":1,\"v\":1,"))),
            ("duplicate-repo", RawToken(valid.Replace($"\"repo\":\"{repoV}\",", $"\"repo\":\"{repoV}\",\"repo\":\"{repoV}\","))),
            ("duplicate-pubRun", RawToken(valid.Replace($"\"pubRun\":\"{runV}\",", $"\"pubRun\":\"{runV}\",\"pubRun\":\"{runV}\","))),
            ("duplicate-pubId", RawToken(valid.Replace($"\"pubId\":\"{idV}\",", $"\"pubId\":\"{idV}\",\"pubId\":\"{idV}\","))),
            ("missing-size", RawToken(valid.Replace("\"size\":1,", ""))),
            ("wrong-type-v", RawToken(valid.Replace("\"v\":1", "\"v\":\"1\""))),
            ("malformed-json", RawToken("{not-json")),
            ("unsupported-version", RawToken(valid.Replace("\"v\":1", "\"v\":2"))),
            ("invalid-checksum", RawToken(valid)),
            ("oversized", new string('A', 5000)),
            ("out-of-range-size", CodeOccurrenceContinuation.Encode(new CodeOccurrenceContinuation.Envelope(
                repoV, "target", targetV, runV, idV, "asc", "src/A.cs", 1, 1, 1, 4, afterId, 0))),
            ("out-of-range-cursor", CodeOccurrenceContinuation.Encode(new CodeOccurrenceContinuation.Envelope(
                repoV, "target", targetV, runV, idV, "asc", "src/A.cs", -1, 1, 1, 4, afterId, 1))),
            ("altered", (good[0] == 'A' ? "B" : "A") + good[1..]),
            ("noncanonical-padded-standard", NonCanonical(good)),
        };

        foreach (var (name, token) in cases)
        {
            var result = await store.FindOccurrencesAsync(
                new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token });
            Assert.True(
                result.Continuation == CodeOccurrenceContinuationState.Invalid &&
                result.Occurrences.Count == 0 &&
                result.NextContinuationToken is null &&
                !result.IsComplete,
                name + ": expected Invalid refusal");
        }

        // Valid-token controls: pagination and publication binding remain unchanged.
        var page2 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = good });
        Assert.Equal(CodeOccurrenceAvailability.Available, page2.Availability);
        Assert.Single(page2.Occurrences);
        Assert.NotEqual((await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).Occurrences[0].Id, page2.Occurrences[0].Id);
        var foreign = new CodeGraphPublication(repo, new CodeIndexRunId("run:foreign"), null, true, new CodeIndexIdentity("identity:foreign"));
        var stale = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, foreign, maxResults: 1) { ContinuationToken = good });
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
    }

    // ---- helpers ----

    private static async Task<List<CodeOccurrenceHit>> ReadAllAsync(
        LatticeCodeGraphStore store, CodeRepositoryId repo, CodeOccurrenceTargetId target, int pageSize)
    {
        var seen = new List<CodeOccurrenceHit>();
        string? token = null;
        do
        {
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: pageSize) { ContinuationToken = token });
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            seen.AddRange(result.Occurrences);
            token = result.NextContinuationToken;
        }
        while (token is not null);
        return seen;
    }

    private static string RawToken(string json) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string NonCanonical(string token)
    {
        var padded = token.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch { 2 => padded + "==", 3 => padded + "=", _ => padded };
        var standard = Convert.ToBase64String(Convert.FromBase64String(padded));
        return standard != token ? standard : token + "=";
    }

    private static async Task AssertInvalidAsync(
        LatticeCodeGraphStore store, CodeRepositoryId repo, CodeOccurrenceTargetId target, string token)
    {
        var result = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token });
        Assert.Equal(CodeOccurrenceContinuationState.Invalid, result.Continuation);
        Assert.Empty(result.Occurrences);
        Assert.Null(result.NextContinuationToken);
        Assert.False(result.IsComplete);
    }

    private bool arm;

    private async Task<LatticeCodeGraphStore> PublishedAsync(
        CodeRepositoryId repo, string run, params CodeIndexUnitReplacement[] units)
    {
        var store = new LatticeCodeGraphStore(_path);
        await PublishAsync(store, repo, run, units);
        return store;
    }

    private static async Task PublishAsync(
        LatticeCodeGraphStore store, CodeRepositoryId repo, string run, params CodeIndexUnitReplacement[] units)
    {
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
    }

    private static CodeIndexUnitReplacement FactUnit(
        CodeRepositoryId repo, string run, string unit, CodeOccurrenceTargetId target, string semantic)
    {
        var fact = CodeOccurrenceFact.Create(
            repo, target, target.Value, semantic,
            new CodeOccurrenceContributor(Plugin, unit), CodeOccurrenceCategory.Invocation,
            new CodeLocation("src/Shared.cs", 1, 1, 1, 5), "sha256:source", "content:invocation");
        return Unit(repo, run, unit, [fact], CodeRelationshipCoverageState.Produced);
    }

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo, string run, string unit,
        IReadOnlyList<CodeOccurrenceFact> facts, CodeRelationshipCoverageState state = CodeRelationshipCoverageState.Produced) =>
        new(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            [Node("node:" + unit)],
            occurrences: facts,
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(facts.Count, 0, state));

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name)
    {
        var context = "compctx1:sha256:" + Hex(repo.Value + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(
            context, "meta:" + name, "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static string Hex(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static CodeOccurrenceFact Fact(
        CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target, string path, int line, int column,
        string? semanticUnitId = null) =>
        CodeOccurrenceFact.Create(
            repo, target, target.Value, semanticUnitId ?? unit,
            new CodeOccurrenceContributor(Plugin, unit), CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4), "sha256:source", "content:invocation");

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId("symbol:" + id));

    private static string CountRows(string path)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        var counts = new List<string>();
        foreach (var label in new[] { "AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage", "HetuUnit" })
        {
            using var q = db.Prepare($"MATCH (s:{label}) RETURN count(s)");
            using var rows = q.Execute(txn);
            var all = rows.ReadAll();
            counts.Add(label + "=" + (all.Count == 0 ? 0 : all[0].GetValue(0).AsInt64()));
        }
        txn.Commit();
        return string.Join(",", counts);
    }

    private sealed class InjectedFault : Exception;
}
