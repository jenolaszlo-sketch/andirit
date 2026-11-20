// QUAL-04 reviewer-owned legacy membership-compatibility controls (commission §9).
// Distinguishes: a matching-authority historically shaped store (X2), a valid
// legacy shape mixed with fresh rows (X4a), and hand-tampered stores that no
// admissible historical production path could have written (X3, X4b). The
// genuine cross-revision cases (X1b/X1c) are driven by the runner from separate
// harness builds against the pinned R2 and R3 production revisions.
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static partial class Probe
{
    // Reviewer-owned 3-unit universe staged exactly the way the pinned R2
    // production path staged rows (full-universe coverage in every row and its
    // envelope, one row per verified unit): this is the historical row layout.
    private static async Task<(VerifiedCSharpUniverse Universe, VerifiedCSharpCompilation Caller, CodeOccurrenceTargetId Target, Dictionary<string, byte[]> Files)>
        BuildThreeUnitUniverse()
    {
        var repoValue = "qual4:legacy:x2";
        var platform = VerifiedMetadataReference.Capture(typeof(object).Assembly.Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release, nullableContextOptions: NullableContextOptions.Disable,
            deterministic: true, concurrentBuild: false);

        static async Task<VerifiedCSharpCompilation> Unit(string repo, string name, string text,
            VerifiedCSharpCompilation[] dependencies, VerifiedMetadataReference platform,
            CSharpCompilationOptions options)
        {
            var path = name + "/" + name + ".cs";
            var bytes = Encoding.UTF8.GetBytes(text);
            var source = await VerifiedCSharpSource.ReadAsync(new CodeGraphSource(path,
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, false))));
            var tree = CSharpSyntaxTree.ParseText(source.Text,
                new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose), path);
            var references = new List<MetadataReference> { platform.Reference };
            references.AddRange(dependencies.Select(d => d.Compilation.ToMetadataReference()));
            var compilation = CSharpCompilation.Create(name, [tree], references, options);
            if (compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                throw new InvalidDataException("Invalid qualification compilation: " + name);
            return VerifiedCSharpCompilation.Create(repo, name + "/" + name + ".csproj", compilation,
                [source], [platform], dependencies);
        }

        var baseUnit = await Unit(repoValue, "Base", "namespace B; public static class Base { public static int Get() => 1; }", [], platform, options);
        var mid = await Unit(repoValue, "Mid", "namespace M; public static class Mid { public static int Run() => B.Base.Get(); }", [baseUnit], platform, options);
        var top = await Unit(repoValue, "Top", "namespace T; public static class Top { public static int Run() => B.Base.Get(); }", [baseUnit], platform, options);
        VerifiedCSharpCompilation[] entries = [baseUnit, mid, top];
        var scope = VerifiedCSharpScope.Declare(repoValue,
            entries.Select(e => new CSharpScopeUnit(e.Descriptor.ProjectUnitId, e.Descriptor.ProjectUnitId,
                e.Descriptor.Sources.Select(s => s.Path).ToImmutableArray())));
        var universe = VerifiedCSharpUniverse.Create(repoValue, scope, entries);

        var mapped = baseUnit.MapR2FaithfulCompctx1();
        var target = CodeOccurrenceIdentity.ComputeTargetId(
            mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName,
            CodeOccurrenceIdentity.ComputeIdentifierDigest(["Get"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest(["M:B.Base.Get"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("method|0|"));
        return (universe, mid, target, new Dictionary<string, byte[]>(StringComparer.Ordinal));
    }

    private static async Task Legacy(string name, string path, string receiptPath)
    {
        var repo = new CodeRepositoryId("qual4:legacy:" + name.ToLowerInvariant());
        switch (name)
        {
            case "X2":
            {
                var (universe, caller, target, _) = await BuildThreeUnitUniverse();
                Receipt["target"] = target.Value;
                var plugin = new CodePluginId("penghou.hetu.csharp");
                var run = new CodeIndexRunId("run:legacy-shaped");
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                    {
                        await Run(store, repo, run.Value, [plugin], async () =>
                        {
                            foreach (var entry in universe.Entries)
                            {
                                var extraction = VerifiedCSharpOccurrenceExtractor.Extract(universe,
                                    new VerifiedOccurrenceExtractionRequest(repo, plugin, entry,
                                        _ => "id:" + _.Name, m => "decl:" + m.ContainingType.Name + "." + m.Name,
                                        m => "method|" + m.Parameters.Length,
                                        m => m.GetDocumentationCommentId() ?? m.Name, "1.0.0"));
                                // Historical R2 row layout: PrepareNativeUnit stages the
                                // full-universe coverage in the row and the envelope.
                                await store.StageIndexUnitAsync(extraction.PrepareNativeUnit(
                                    new CodeFactOrigin(repo, plugin, "1.0.0", run,
                                        new CodeIndexUnitId(entry.Descriptor.ProjectUnitId))));
                            }
                        });
                    }
                }

                using (var store = new LatticeCodeGraphStore(path))
                {
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                        universe, caller.Compilation);
                    Receipt["query"] = DescribeResult(await store.FindOccurrencesAsync(query));
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            case "X4a":
            case "X4b":
            case "X3":
            {
                // Plugin-path publication of a 3-project scope with an empty middle
                // project (calls 1/0/1), then bounded native tampering.
                var run = "run:" + name.ToLowerInvariant();
                var files = MembershipFixture(3, 0, new[] { 1, 0, 1 });
                var target = await FixtureTargetAsync(repo, run, files, "P0/P0.csproj", "P0.Value", "Get");
                // Only the publishing phase writes the durable file; the verify
                // phase reopens exactly what that process left behind.
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                        await PublishCSharp(store, repo, run, files);
                }
                Receipt["target"] = target.Value;

                var unitP1 = ProjectUnit("P1/P1.csproj");
                var unitP0 = ProjectUnit("P0/P0.csproj");
                var unitP2 = ProjectUnit("P2/P2.csproj");
                var keyP1 = repo.Value + "\n" + CSharp.Id.Value + "\n" + unitP1;
                var keyP2 = repo.Value + "\n" + CSharp.Id.Value + "\n" + unitP2;
                var graphKeyP2 = repo.Value + "\n" + CSharp.Id.Value + "\n" + unitP2;
                if (Phase != "verify")
                {
                    if (name == "X3")
                    {
                        RemoveRow(path, "AndiritOccurrenceUnit", keyP2);
                        RemoveRow(path, "HetuUnit", graphKeyP2);
                        MutateMetaRow(path, meta =>
                        {
                            meta["UnitCount"] = 2;
                            meta["FactCount"] = 1;
                        });
                    }
                    else
                    {
                        MutateOccurrenceUnit(path, keyP1, row =>
                        {
                            // Row carries the full-universe counts (historical layout).
                            row["Coverage"]!["DeclaredUnitCount"] = 3;
                            row["Coverage"]!["VerifiedUnitCount"] = 3;
                            if (name == "X4b")
                                // Not constructible historically: R2/R3 always record a
                                // self-owned envelope for the row's own unit.
                                row["Authority"]!["SourceUnitId"] = unitP0;
                        });
                    }
                }

                using (var store = new LatticeCodeGraphStore(path))
                {
                    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                        analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
                    var result = await store.FindOccurrencesAsync(query);
                    Receipt["query"] = DescribeResult(result);
                    Receipt["hitsBySourceUnit"] = result.Occurrences
                        .GroupBy(fact => fact.SourceUnitId, StringComparer.Ordinal)
                        .OrderBy(group => group.Key, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Count());
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            default:
                throw new ArgumentException(name);
        }
    }

    // Genuine cross-revision legacy case: the publication is written by the harness
    // build for one pinned revision and reopened by the harness build for another
    // (driven by the runner). The publication path is the real plugin/sink path.
    private static async Task GenuineLegacy(string name, string path, string receiptPath)
    {
        var repo = new CodeRepositoryId("qual4:genuine");
        var run = "run:genuine";
        var files = MembershipFixture(3, 0, new[] { 1, 1, 1 });
        var target = await FixtureTargetAsync(repo, run, files, "P0/P0.csproj", "P0.Value", "Get");
        Receipt["target"] = target.Value;
        Receipt["productionAssembly"] = typeof(CSharpCodeGraphPlugin).Assembly.FullName;

        if (Phase != "verify")
        {
            using (var store = new LatticeCodeGraphStore(path))
                await PublishCSharp(store, repo, run, files);
        }

        using (var store = new LatticeCodeGraphStore(path))
        {
            var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "P0/P0.csproj").Compilation);
            var result = await store.FindOccurrencesAsync(query);
            Receipt["query"] = DescribeResult(result);
            Receipt["referenceIdentityCount"] = analysis.Universe.Entries
                .Select(e => e.Descriptor.References.Length).ToArray();
        }

        Receipt["native"] = NativeRowSummary(path);
    }

    // Bounded native row helpers (disposable stores only; canonical repositories
    // and historical artifacts are never modified).
    private static void MutateOccurrenceUnit(string path, string key, Action<JsonObject> mutation)
    {
        var row = ReadRow(path, "AndiritOccurrenceUnit", key);
        mutation(row);
        WriteRow(path, "AndiritOccurrenceUnit", key, row);
    }

    private static void MutateMetaRow(string path, Action<JsonObject> mutation)
    {
        string repoKey;
        JsonObject row;
        // Read and write in separate database sessions: LatticeDB takes an
        // exclusive file lock, so the reader must be closed before writing.
        {
            using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
            using var txn = db.BeginReadTransaction();
            using var query = db.Prepare("MATCH (s:AndiritOccurrenceMeta) RETURN s.key, s.payload");
            using var result = query.Execute(txn);
            var entry = result.ReadAll().Single();
            repoKey = entry.GetValue(0).AsString();
            row = JsonNode.Parse(Convert.FromBase64String(entry.GetValue(1).AsString()))!.AsObject();
            txn.Commit();
        }
        mutation(row);
        WriteRow(path, "AndiritOccurrenceMeta", repoKey, row);
    }

    private static JsonObject ReadRow(string path, string label, string key)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        using var query = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k RETURN s.key, s.payload");
        query.Bind("k", LatticeValue.From(key));
        using var result = query.Execute(txn);
        var row = result.ReadAll().Single();
        var payload = JsonNode.Parse(Convert.FromBase64String(row.GetValue(1).AsString()))!.AsObject();
        txn.Commit();
        return payload;
    }

    private static void RemoveRow(string path, string label, string key)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginWriteTransaction();
        using var query = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s");
        query.Bind("k", LatticeValue.From(key));
        using var result = query.Execute(txn);
        result.ReadAll();
        txn.Commit();
    }

    private static void WriteRow(string path, string label, string key, JsonObject row)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginWriteTransaction();
        using (var query = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s"))
        {
            query.Bind("k", LatticeValue.From(key));
            using var deleted = query.Execute(txn);
            deleted.ReadAll();
        }

        using (var create = db.Prepare($"CREATE (:{label} {{key: $k, payload: $p}})"))
        {
            create.Bind("k", LatticeValue.From(key));
            create.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var created = create.Execute(txn);
            created.ReadAll();
        }

        txn.Commit();
    }
}