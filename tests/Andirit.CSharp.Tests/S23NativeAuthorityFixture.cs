using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Frozen = AdrCi1Exp.Shared;

namespace Andirit.CSharp.Tests;

// Controlled native qualification fixture, not plugin/sink publication wiring.
internal sealed record S23NativeAuthorityFixture(
    VerifiedCSharpUniverse Universe, VerifiedCSharpCompilation[] Entries)
{
    internal static readonly CodeRepositoryId Repo = new("fixture:s23-durability");
    internal static readonly CodePluginId Plugin = new("fixture.s23");
    internal static readonly DateTimeOffset Started = new(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);
    internal static readonly CSharpCompilationOptions Options = new(
        OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable, deterministic: true, concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location));

    internal VerifiedCSharpCompilation Leaf => Entries[0];
    internal VerifiedCSharpCompilation Mid => Entries[1];
    internal VerifiedCSharpCompilation Lib => Entries[2];
    internal VerifiedCSharpCompilation App => Entries[3];
    internal VerifiedCSharpCompilation App2 => Entries[4];
    internal VerifiedCSharpCompilation Empty => Entries[5];

    internal static async Task<S23NativeAuthorityFixture> Build(string variant = "a", bool retire = false)
    {
        var leaf = await Unit("Leaf", "namespace L; public static class Leaf { public static int V() => " +
            (variant == "source" ? "2" : "1") + "; }", [], variant == "options" ? Options.WithOverflowChecks(true) : Options);
        var mid = await Unit("Mid", "namespace M; public static class Mid { public static int V() => L.Leaf.V(); }", [leaf]);
        var lib = await Unit("Lib", "namespace B; public static class Lib { public static int V() => M.Mid.V(); }", [mid]);
        var app = await Unit("App", retire ? "namespace A; public class App { }" :
            "namespace A; public static class App { public static int Run() => B.Lib.V() + M.Mid.V(); }", [lib, mid]);
        var app2 = await Unit("App2", "namespace A2; public static class App2 { public static int Run() => B.Lib.V(); }", [lib]);
        var empty = await Unit("Empty", "namespace E; public class Empty { }", []);
        VerifiedCSharpCompilation[] entries = [leaf, mid, lib, app, app2, empty];
        return new(UniverseFor(entries), entries);
    }

    internal static VerifiedCSharpUniverse UniverseFor(params VerifiedCSharpCompilation[] entries) =>
        VerifiedCSharpUniverse.Create(Repo.Value, VerifiedCSharpScope.Declare(Repo.Value,
            entries.Select(e => new CSharpScopeUnit(e.Descriptor.ProjectUnitId, e.Descriptor.ProjectUnitId,
                e.Descriptor.Sources.Select(s => s.Path).ToImmutableArray()))), entries);

    private static async Task<VerifiedCSharpCompilation> Unit(string name, string text,
        VerifiedCSharpCompilation[] dependencies, CSharpCompilationOptions? options = null)
    {
        var path = name + "/" + name + ".cs";
        var bytes = Encoding.UTF8.GetBytes(text);
        var source = await VerifiedCSharpSource.ReadAsync(new CodeGraphSource(path,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, false))));
        var tree = CSharpSyntaxTree.ParseText(source.Text,
            new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose), path);
        var compilation = CSharpCompilation.Create(name, [tree],
            new MetadataReference[] { Platform.Value.Reference }.Concat(dependencies.Select(d => d.Compilation.ToMetadataReference())), options ?? Options);
        if (compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            throw new InvalidDataException("Invalid qualification compilation: " + name);
        return VerifiedCSharpCompilation.Create(Repo.Value, name + "/" + name + ".csproj", compilation,
            [source], [Platform.Value], dependencies);
    }

    internal VerifiedOccurrenceExtraction Extract(VerifiedCSharpCompilation source) =>
        VerifiedCSharpOccurrenceExtractor.Extract(Universe, new(Repo, Plugin, source,
            Id, Decl, Shape, m => m.GetDocumentationCommentId() ?? m.Name, "1.0.0"));
    private static string Id(IMethodSymbol m) => "id:" + m.Name;
    private static string Decl(IMethodSymbol m) => "decl:" + m.ContainingType.Name + "." + m.Name;
    private static string Shape(IMethodSymbol m) => "method|" + m.Parameters.Length;

    internal static CodeFactOrigin Origin(string run, string unit) =>
        new(Repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit));
    internal CodeIndexUnitReplacement Replacement(string run, VerifiedCSharpCompilation source) =>
        Extract(source).PrepareNativeUnit(Origin(run, source.Descriptor.ProjectUnitId));
    internal CodeOccurrenceQuery Query(CodeOccurrenceTargetId target, CodeGraphPublication? expected = null) =>
        VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, expected), Universe, App.Compilation);
    internal CodeOccurrenceTargetId LibTarget => Extract(App2).Facts.Single().TargetId;

    internal static async Task Start(LatticeCodeGraphStore store, string run)
    {
        await store.UpsertRepositoryAsync(new(Repo));
        await store.StoreIndexRunAsync(new(Repo, new CodeIndexRunId(run), Started, plugins: [Plugin]));
    }
    internal static ValueTask Complete(LatticeCodeGraphStore store, string run) => store.CompleteIndexRunAsync(
        new(Repo, new CodeIndexRunId(run), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
        new(Repo, new CodeIndexRunId(run), []));
    internal async Task Publish(LatticeCodeGraphStore store, string run, params VerifiedCSharpCompilation[] sources)
    {
        await Start(store, run);
        foreach (var source in sources) await store.StageIndexUnitAsync(Replacement(run, source));
        await Complete(store, run);
    }

    // Independent frozen R2 Describe and tuple hashing, not the new carrier or
    // production mapper, establish expected target and usage identities.
    internal static string ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method)
    {
        var mapped = Frozen.CompilationProvenance.Describe(Repo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        return "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName, Id(method), Decl(method), Shape(method)]);
    }
    internal static string ExpectedUsage(CodeOccurrenceFact fact) => Frozen.ProvenanceHashing.Hash("usage4",
        [Repo.Value, Plugin.Value, "usage4-b2c-exp-r2-20261101", fact.SourceUnitId, fact.Span.Path, fact.SourceHash,
            fact.Span.StartLine.ToString(), fact.Span.StartColumn.ToString(), fact.Span.EndLine.ToString(),
            fact.Span.EndColumn.ToString(), fact.TargetDocumentId, fact.TargetId.Value, "method-invocation"]);

    internal static SortedDictionary<string, JsonObject> Rows(string path, params string[] labels)
    {
        var rows = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        foreach (var label in labels.Length == 0 ? new[] { "AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage", "HetuUnit", "HetuStage", "HetuIndexState" } : labels)
        {
            using var q = db.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
            using var result = q.Execute(txn);
            foreach (var row in result.ReadAll())
                rows[label + "|" + row.GetValue(0).AsString()] = JsonNode.Parse(Convert.FromBase64String(row.GetValue(1).AsString()))!.AsObject();
        }
        txn.Commit();
        return rows;
    }
    internal static void MutateUnit(string path, Action<JsonObject> mutation, string unit = "App/App.csproj")
    {
        var key = Repo.Value + "\n" + Plugin.Value + "\n" + unit;
        var row = Rows(path, "AndiritOccurrenceUnit")["AndiritOccurrenceUnit|" + key];
        mutation(row);
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginWriteTransaction();
        using (var q = db.Prepare("MATCH (s:AndiritOccurrenceUnit) WHERE s.key = $k DELETE s"))
        { q.Bind("k", LatticeValue.From(key)); using var result = q.Execute(txn); result.ReadAll(); }
        using (var q = db.Prepare("CREATE (:AndiritOccurrenceUnit {key: $k, payload: $p})"))
        {
            q.Bind("k", LatticeValue.From(key));
            q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var result = q.Execute(txn); result.ReadAll();
        }
        txn.Commit();
    }
    internal static string Snapshot(string path) => JsonSerializer.Serialize(Rows(path));
}
