using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// Durable-state regressions for the LatticeDB provider. These cases use only
/// public store behavior and genuine native rows; no production visibility or
/// private implementation seam is introduced.
/// </summary>
public sealed class LatticeDbDurableStateRegressionTests
{
    private static readonly CodePluginId Plugin = new("plugin:durable-regression");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void StoreRejectsABlankDatabasePath()
    {
        Assert.Throws<ArgumentException>(() => new LatticeCodeGraphStore(""));
        Assert.Throws<ArgumentException>(() => new LatticeCodeGraphStore("   "));
    }

    [Fact]
    public async Task OccurrenceFactsWithoutDeclaredCoverageAreRestoredAsPartial()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:no-coverage", "unit:no-coverage");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:no-coverage", coverage: null));

            using var reopened = new LatticeCodeGraphStore(path);
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId));

            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.NotNull(result.Coverage);
            Assert.Equal(CodeRelationshipCoverageState.Partial, result.Coverage.State);
            Assert.False(result.IsComplete);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task NotProducedCoverageWithFactsIsCorrectedToPartial()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:not-produced", "unit:not-produced");
            var coverage = new CodeOccurrenceCoverage(
                invocationFacts: 1,
                state: CodeRelationshipCoverageState.NotProduced,
                scopeId: "analysisscope1:sha256:" + new string('a', 64),
                universeId: "compilation-universe1:sha256:" + new string('b', 64),
                declaredUnitCount: 1,
                verifiedUnitCount: 1);
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:not-produced", coverage));

            using var reopened = new LatticeCodeGraphStore(path);
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId));

            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.NotNull(result.Coverage);
            Assert.Equal(CodeRelationshipCoverageState.Partial, result.Coverage.State);
            Assert.False(result.IsComplete);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task LegacyPersistedUnitWithoutCoverageRestoresAsPartial()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:legacy-no-coverage", "unit:legacy-no-coverage");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:legacy-no-coverage", CompleteCoverage(fixture)));

            MutatePayload(path, "AndiritOccurrenceMeta", fixture.RepositoryId.Value, row =>
                row["CompleteCoverage"] = false);
            MutatePayload(path, "AndiritOccurrenceUnit", OccurrenceUnitKey(fixture), row =>
                row.Remove("Coverage"));

            using var reopened = new LatticeCodeGraphStore(path);
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId));

            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.NotNull(result.Coverage);
            Assert.Equal(CodeRelationshipCoverageState.Partial, result.Coverage.State);
            Assert.False(result.IsComplete);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task AuthorityRequiredUnitWithoutCurrentAuthorityIsUnavailable()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:missing-authority", "unit:missing-authority");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:missing-authority", CompleteCoverage(fixture)));
            RequireAuthority(path, fixture);

            using var reopened = new LatticeCodeGraphStore(path);
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId));

            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Contains("authority", result.RefusalReason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task AuthorityRequiredUnitRejectsAStaleExpectedPublication()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:stale-authority", "unit:stale-authority");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:stale-authority", CompleteCoverage(fixture)));
            RequireAuthority(path, fixture);

            using var reopened = new LatticeCodeGraphStore(path);
            var stale = new CodeGraphPublication(
                fixture.RepositoryId,
                new CodeIndexRunId("run:stale"),
                snapshotIdentity: null,
                isConsistentSnapshot: true,
                new CodeIndexIdentity("identity:stale"));
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId, stale));

            Assert.Equal(CodeOccurrenceAvailability.StalePublication, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Contains("publication", result.RefusalReason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task AuthorityRequiredUnitRejectsAMismatchedPublicationIdentity()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:mismatched-identity", "unit:mismatched-identity");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:mismatched-identity", CompleteCoverage(fixture)));
            RequireAuthority(path, fixture);
            MutatePayload(path, "AndiritOccurrenceMeta", fixture.RepositoryId.Value, row =>
                row["IndexIdentity"] = "identity:mismatched");

            using var reopened = new LatticeCodeGraphStore(path);
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId));

            Assert.Equal(CodeOccurrenceAvailability.StalePublication, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Contains("publication", result.RefusalReason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Theory]
    [InlineData("UnitCount")]
    [InlineData("FactCount")]
    public async Task AuthorityPublicationAccountingMismatchIsRefused(string field)
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:accounting-" + field, "unit:accounting");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:accounting", CompleteCoverage(fixture)));
            RequireAuthority(path, fixture);
            MutatePayload(path, "AndiritOccurrenceMeta", fixture.RepositoryId.Value, row =>
                row[field] = row[field]!.GetValue<int>() + 1);

            using var reopened = new LatticeCodeGraphStore(path);
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId));

            Assert.Equal(CodeOccurrenceAvailability.CorruptedIndex, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Contains("accounting", result.RefusalReason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task OrphanedAuthorityUnitIsRefused()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:orphan-authority", "unit:orphan-authority");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:orphan-authority", CompleteCoverage(fixture)));
            MutatePayload(path, "AndiritOccurrenceUnit", OccurrenceUnitKey(fixture), row =>
            {
                row["RequiresAuthority"] = true;
                row["IndexUnitId"] = "unit:orphaned";
            });

            using var reopened = new LatticeCodeGraphStore(path);
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId));

            Assert.Equal(CodeOccurrenceAvailability.CorruptedIndex, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Contains("orphaned", result.RefusalReason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task PublishedGraphUnitRequiringAuthorityWithoutAnOccurrenceOwnerIsRefused()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:missing-owner", "unit:owner");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:owner", CompleteCoverage(fixture)),
                GraphUnit(fixture, "unit:graph-only"));
            RequireAuthority(path, fixture);
            MutatePayloadContaining(
                path,
                "HetuUnit",
                "\nunit:graph-only",
                row => row["RequiresOccurrenceAuthority"] = true);

            using var reopened = new LatticeCodeGraphStore(path);
            var result = await reopened.FindOccurrencesAsync(
                new CodeOccurrenceQuery(fixture.RepositoryId, fixture.TargetId));

            Assert.Equal(CodeOccurrenceAvailability.CorruptedIndex, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Contains("no occurrence association", result.RefusalReason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Theory]
    [InlineData("RepositoryId", "repo:unknown-page")]
    [InlineData("PublicationIndexRunId", "run:unknown-page")]
    public async Task OccurrencePageMustRemainBoundToItsAcceptedPublication(string field, string value)
    {
        var path = TemporaryDatabasePath();
        try
        {
            var fixture = new Fixture("repo:page-binding", "unit:page-binding");
            await PublishAsync(path, fixture,
                OccurrenceUnit(fixture, "unit:page-binding", CompleteCoverage(fixture)));
            MutatePayloadContaining(path, "AndiritOccurrencePage", fixture.RepositoryId.Value, row =>
                row[field] = value);

            var exception = Assert.Throws<CodeOccurrenceIndexException>(
                () => new LatticeCodeGraphStore(path));
            Assert.Contains("bound", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static async Task PublishAsync(
        string path,
        Fixture fixture,
        params CodeIndexUnitReplacement[] units)
    {
        using var store = new LatticeCodeGraphStore(path);
        await store.UpsertRepositoryAsync(new(fixture.RepositoryId));
        await store.StoreIndexRunAsync(new(
            fixture.RepositoryId,
            fixture.RunId,
            Started,
            plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(
                fixture.RepositoryId,
                fixture.RunId,
                Started,
                CodeIndexRunStatus.Completed,
                Started.AddSeconds(1),
                [Plugin]),
            new(fixture.RepositoryId, fixture.RunId, []));
    }

    private static CodeIndexUnitReplacement OccurrenceUnit(
        Fixture fixture,
        string unit,
        CodeOccurrenceCoverage? coverage)
    {
        var fact = Fact(fixture, unit, "src/A.cs", 1, 1);
        return new CodeIndexUnitReplacement(
            Origin(fixture, unit),
            [Node("node:" + unit)],
            occurrences: [fact],
            occurrenceAnalysisIncluded: coverage is not null,
            occurrenceCoverage: coverage);
    }

    private static CodeIndexUnitReplacement GraphUnit(Fixture fixture, string unit) =>
        new(Origin(fixture, unit), [Node("node:" + unit)]);

    private static CodeOccurrenceCoverage CompleteCoverage(Fixture fixture) =>
        new(
            invocationFacts: 1,
            state: CodeRelationshipCoverageState.Produced,
            scopeId: "analysisscope1:sha256:" + new string('a', 64),
            universeId: "compilation-universe1:sha256:" + new string('b', 64),
            declaredUnitCount: 1,
            verifiedUnitCount: 1);

    private static CodeFactOrigin Origin(Fixture fixture, string unit) =>
        new(
            fixture.RepositoryId,
            Plugin,
            "1.0.0",
            fixture.RunId,
            new CodeIndexUnitId(unit));

    private static CodeOccurrenceFact Fact(
        Fixture fixture,
        string unit,
        string path,
        int line,
        int column) =>
        CodeOccurrenceFact.Create(
            fixture.RepositoryId,
            fixture.TargetId,
            fixture.TargetId.Value,
            unit,
            new CodeOccurrenceContributor(Plugin, unit),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            "sha256:source",
            "content:invocation");

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId("symbol:" + id));

    private static void RequireAuthority(string path, Fixture fixture) =>
        MutatePayload(path, "AndiritOccurrenceUnit", OccurrenceUnitKey(fixture), row =>
            row["RequiresAuthority"] = true);

    private static string OccurrenceUnitKey(Fixture fixture) =>
        fixture.RepositoryId.Value + "\n" + Plugin.Value + "\n" + fixture.Unit;

    private static void MutatePayload(
        string path,
        string label,
        string key,
        Action<JsonObject> mutation)
    {
        using var database = LatticeDatabase.Open(
            path,
            new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        JsonObject row;
        using (var query = database.Prepare($"MATCH (s:{label}) WHERE s.key = $k RETURN s.payload"))
        {
            query.Bind("k", LatticeValue.From(key));
            using var result = query.Execute(transaction);
            row = JsonNode.Parse(Convert.FromBase64String(
                result.ReadAll().Single().GetValue(0).AsString()))!.AsObject();
        }

        mutation(row);
        using (var query = database.Prepare($"MATCH (s:{label}) WHERE s.key = $k SET s.payload = $p"))
        {
            query.Bind("k", LatticeValue.From(key));
            query.Bind("p", LatticeValue.From(Convert.ToBase64String(
                Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var result = query.Execute(transaction);
            result.ReadAll();
        }
        transaction.Commit();
    }

    private static void MutatePayloadContaining(
        string path,
        string label,
        string keyFragment,
        Action<JsonObject> mutation)
    {
        using var database = LatticeDatabase.Open(
            path,
            new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        string key;
        JsonObject row;
        using (var query = database.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload"))
        {
            using var result = query.Execute(transaction);
            var match = result.ReadAll().Single(candidate =>
                candidate.GetValue(0).AsString().Contains(keyFragment, StringComparison.Ordinal));
            key = match.GetValue(0).AsString();
            row = JsonNode.Parse(Convert.FromBase64String(match.GetValue(1).AsString()))!.AsObject();
        }

        mutation(row);
        using (var query = database.Prepare($"MATCH (s:{label}) WHERE s.key = $k SET s.payload = $p"))
        {
            query.Bind("k", LatticeValue.From(key));
            query.Bind("p", LatticeValue.From(Convert.ToBase64String(
                Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var result = query.Execute(transaction);
            result.ReadAll();
        }
        transaction.Commit();
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"andirit-latticedb-regression-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-wal", path + "-journal", path + ".wal" })
        {
            if (File.Exists(candidate))
                File.Delete(candidate);
        }
    }

    private sealed record Fixture(string Name, string Unit)
    {
        internal CodeRepositoryId RepositoryId { get; } = new(Name);

        internal CodeIndexRunId RunId { get; } = new(
            "run:" + (Name.StartsWith("repo:", StringComparison.Ordinal) ? Name[5..] : Name));

        internal CodeOccurrenceTargetId TargetId { get; } = Target(
            new CodeRepositoryId(Name),
            "T1");
    }

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repository, string name)
    {
        var context = "compctx1:sha256:" + Hex(repository.Value + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(
            context,
            "meta:" + name,
            "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static string Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
