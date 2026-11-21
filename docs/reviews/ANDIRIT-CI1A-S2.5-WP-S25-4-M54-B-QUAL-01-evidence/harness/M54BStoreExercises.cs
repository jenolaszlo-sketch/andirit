// M54-B-QUAL-01 harness — integrated accounting leg (native store chain).
// Publication uses the REAL production bridge (VerifiedOccurrenceExtraction.
// PrepareNativeUnit); the reopen leg runs in a DISTINCT process (built from the
// same deterministic fixture, authority-bound through the live boundary) and the
// query result is read back as JSON.
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Xunit;

namespace M54B;

public sealed class M54BStoreExercises
{
    private static readonly DateTimeOffset Started = SharedFixture.Started;
    private static readonly CodePluginId Plugin = new("plugin:m54b");
    private static readonly CodeRepositoryId Repo = new(SharedFixture.RepositoryId);
    private static readonly CodeIndexRunId Run = new("run:m54b");

    private static string TempDatabase() =>
        Path.Combine(Path.GetTempPath(), $"m54b-{Guid.NewGuid():N}.ltdb");

    private static void Publish(string path, CodeIndexUnitReplacement replacement)
    {
        using var store = new LatticeCodeGraphStore(path);
        store.UpsertRepositoryAsync(new(Repo)).GetAwaiter().GetResult();
        store.StoreIndexRunAsync(new(Repo, Run, Started, plugins: [Plugin])).GetAwaiter().GetResult();
        store.StageIndexUnitAsync(replacement).GetAwaiter().GetResult();
        store.CompleteIndexRunAsync(
            new(Repo, Run, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(Repo, Run, [])).GetAwaiter().GetResult();
    }

    // Distinct-process reopen through the production store and the live authority
    // boundary (the reopen host rebuilds the same deterministic universe). The
    // bind flag selects the plain query (no current authority) or the
    // authority-bound query through the live verified-compilation boundary.
    private static JsonNode Reopen(string path, string targetId, bool bind)
    {
        var reopenDll = Environment.GetEnvironmentVariable("M54B_REOPEN_DLL");
        if (string.IsNullOrEmpty(reopenDll))
            throw new InvalidOperationException("M54B_REOPEN_DLL is not set; run via run-qualification.ps1");
        var start = new System.Diagnostics.ProcessStartInfo(
            "dotnet", $"exec \"{reopenDll}\" \"{path}\" \"{Repo.Value}\" \"{targetId}\" {(bind ? 1 : 0)}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException("reopen process did not start");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(180_000);
        Assert.True(process.ExitCode == 0, "reopen exit " + process.ExitCode + ": " + stderr);
        return JsonNode.Parse(stdout)!;
    }

    private static void Delete(string path)
    {
        foreach (var candidate in new[] { path, path + "-wal", path + "-journal", path + ".wal" })
            if (File.Exists(candidate))
                File.Delete(candidate);
    }

    [Fact]
    public void E3b_PublishedCoverageAndFact_SurviveDistinctProcessReopen()
    {
        var (universe, app, _, _) = SharedFixture.Build();
        var extraction = SharedFixture.Extract(universe, app);
        var path = TempDatabase();
        try
        {
            // Real production bridge: refuses without a verified authority envelope.
            var replacement = extraction.PrepareNativeUnit(
                new CodeFactOrigin(Repo, Plugin, "1.0.0", Run, new CodeIndexUnitId("App/App.csproj")));
            Publish(path, replacement);

            var target = extraction.Facts[0].TargetId;
            var reopened = Reopen(path, target.Value, bind: true);
            var availability = reopened["availability"]!.GetValue<string>();

            Receipt.Emit("e3b", new
            {
                classification = "natural production bridge + distinct-process reopen (authority-bound)",
                target = target.Value,
                factId = extraction.Facts[0].Id.Value,
                reopenedRaw = reopened.ToJsonString(),
                availability,
                occurrenceCount = reopened["occurrenceCount"]!.GetValue<int>(),
            });

            Assert.Equal(CodeOccurrenceAvailability.Available.ToString(), availability);
            Assert.Equal(1, reopened["occurrenceCount"]!.GetValue<int>());
            Assert.Equal(extraction.Facts[0].Id.Value, reopened["occurrences"]![0]!["id"]!.GetValue<string>());
            Assert.Empty(reopened["refusalReason"]?.GetValue<string>() ?? string.Empty);
            // The unresolved/external classification survives publication and reopen.
            Assert.Equal(extraction.Coverage.Unresolved, reopened["coverage"]!["unresolved"]!.GetValue<int>());
            Assert.Equal(extraction.Coverage.External, reopened["coverage"]!["external"]!.GetValue<int>());
            Assert.Equal("Produced", reopened["coverage"]!["state"]!.GetValue<string>());
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void E3b2_PartialCoverage_ReopensAsIncompleteAnalysis_NeverFalseAvailable()
    {
        var (universe, app, _, _) = SharedFixture.Build();
        var extraction = SharedFixture.Extract(universe, app);
        var path = TempDatabase();
        try
        {
            // Constructed control: the same fact published with a PARTIAL coverage
            // record (an incomplete declared universe's accounting) and without an
            // authority marker, through the real publication path.
            var partial = new CodeOccurrenceCoverage(
                invocationFacts: extraction.Facts.Length,
                unsupportedCategories: extraction.Coverage.UnsupportedCategories,
                state: CodeRelationshipCoverageState.Partial,
                scopeId: universe.Scope.Identity,
                universeId: universe.Identity,
                declaredUnitCount: 2,
                verifiedUnitCount: 1,
                external: extraction.Coverage.External,
                unresolved: extraction.Coverage.Unresolved,
                unsupported: extraction.Coverage.Unsupported);
            Publish(path, new CodeIndexUnitReplacement(
                new CodeFactOrigin(Repo, Plugin, "1.0.0", Run, new CodeIndexUnitId("App/App.csproj")),
                occurrences: extraction.Facts, occurrenceAnalysisIncluded: true, occurrenceCoverage: partial));

            var reopened = Reopen(path, extraction.Facts[0].TargetId.Value, bind: false);
            var availability = reopened["availability"]!.GetValue<string>();

            Receipt.Emit("e3b2", new
            {
                classification = "constructed control (partial coverage through the real publication path)",
                reopenedRaw = reopened.ToJsonString(),
                availability,
            });

            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis.ToString(), availability);
            Assert.False(string.IsNullOrEmpty(reopened["refusalReason"]?.GetValue<string>()));
            Assert.Equal(1, reopened["occurrenceCount"]!.GetValue<int>());
            Assert.Equal("Partial", reopened["coverage"]!["state"]!.GetValue<string>());
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void E3c_AuthorityMasking_IsRefusedAfterReopen()
    {
        var (universe, app, _, _) = SharedFixture.Build();
        var extraction = SharedFixture.Extract(universe, app);
        var path = TempDatabase();
        try
        {
            Publish(path, extraction.PrepareNativeUnit(
                new CodeFactOrigin(Repo, Plugin, "1.0.0", Run, new CodeIndexUnitId("App/App.csproj"))));

            // Constructed tamper: strip the durable authority envelope while the durable
            // unit still claims required authority (the masking attempt: authority
            // required, no envelope to validate).
            StripAuthority(path, Repo.Value, Plugin.Value, "App/App.csproj");

            var reopened = Reopen(path, extraction.Facts[0].TargetId.Value, bind: false);
            var availability = reopened["availability"]!.GetValue<string>();

            Receipt.Emit("e3c", new
            {
                classification = "constructed tamper through the natural native authority gate",
                reopenedRaw = reopened.ToJsonString(),
                availability,
                refusalReason = reopened["refusalReason"]!.GetValue<string>(),
            });

            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable.ToString(), availability);
            Assert.Contains("authority", reopened["refusalReason"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, reopened["occurrenceCount"]!.GetValue<int>());
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void A4b_PrepareNativeUnit_RefusesWithoutVerifiedAuthority()
    {
        var app = SharedFixture.StandaloneApp();
        var scope = UniverseFactory.DeclareScope(
            ("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
            ("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]));
        var incomplete = UniverseFactory.CreateUniverse(scope, [app]);
        var extraction = SharedFixture.Extract(incomplete, app);

        var refusal = Assert.Throws<InvalidDataException>(() =>
            extraction.PrepareNativeUnit(
                new CodeFactOrigin(Repo, Plugin, "1.0.0", Run, new CodeIndexUnitId("App/App.csproj"))));
        Assert.Contains("MissingVerifiedOccurrenceAuthority", refusal.Message, StringComparison.Ordinal);

        Receipt.Emit("a4b", new
        {
            classification = "natural production guard (incomplete universe)",
            refusal = refusal.Message,
            note = "the unresolved classification never reaches publication without a verified authority envelope",
        });
    }

    // Native durable-row tamper (reviewer harness; identical technique to the
    // established durable-state regression pattern).
    private static void StripAuthority(string path, string repositoryId, string pluginId, string unitId)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var key = repositoryId + "\n" + pluginId + "\n" + unitId;
        JsonObject row;
        using (var query = database.Prepare("MATCH (s:AndiritOccurrenceUnit) WHERE s.key = $k RETURN s.payload"))
        {
            query.Bind("k", LatticeValue.From(key));
            using var result = query.Execute(transaction);
            row = JsonNode.Parse(Convert.FromBase64String(
                result.ReadAll().Single().GetValue(0).AsString()))!.AsObject();
        }

        row["RequiresAuthority"] = true;
        row["Authority"] = null;
        using (var query = database.Prepare("MATCH (s:AndiritOccurrenceUnit) WHERE s.key = $k SET s.payload = $p"))
        {
            query.Bind("k", LatticeValue.From(key));
            query.Bind("p", LatticeValue.From(Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var result = query.Execute(transaction);
            result.ReadAll();
        }

        transaction.Commit();
    }
}