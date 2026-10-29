// Pack-consume-load smoke test for all six Andirit NuGet packages.
// This project is intentionally outside the solution: it restores the PACKED
// package (plus its dependencies) from a package feed, proving the native
// RID assets resolve and parse through the real NuGet layout rather than a
// repository build output. Run with:
//   dotnet run -p:RestoreSources="<feed>;<nuget.org>" [-p:SmokerPackageVersion=<ver>]
using System.Security.Cryptography;
using System.Text;
using Andirit;

// Elixir's native is checked into the package; TypeScript and JavaScript
// natives come from the TreeSitter.DotNet dependency. All three must resolve
// and parse in a packed consumer.
var elixirPlugin = new TreeSitterElixirPlugin();
var elixir = await RunAsync(
    elixirPlugin,
    "lib/smoke.ex",
    """
    defmodule Smoke.Test do
      def run(x), do: x
    end
    """);
Check(elixirPlugin.Id.Value == "penghou.hetu.tree-sitter.elixir", "elixir plugin identity");
Check(elixirPlugin.Language == "elixir", "elixir language");
Check(elixirPlugin.Grammar.GrammarId == "tree-sitter-elixir", "elixir grammar id");
Check(elixirPlugin.Grammar.AbiVersion == 14, "elixir grammar ABI");
Check(!elixir.Result.WarningCodes.Contains("elixir.parse-error"), "elixir no parse diagnostics");
Check(elixir.Nodes.Any(node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Smoke.Test"), "elixir module node");
Check(elixir.Nodes.Any(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Smoke.Test.run/1"), "elixir function node");
CheckStructural(elixir, "elixir");
Console.WriteLine($"elixir provider version: {elixirPlugin.Version}");

var typeScriptPlugin = new TreeSitterTypeScriptPlugin();
var typeScript = await RunAsync(
    typeScriptPlugin,
    "src/smoke.ts",
    "export class Smoke { run(): number { return 1; } }");
Check(typeScriptPlugin.Id.Value == "penghou.hetu.tree-sitter.typescript", "typescript plugin identity");
Check(typeScriptPlugin.Language == "typescript", "typescript language");
Check(typeScriptPlugin.Grammar.GrammarId == "tree-sitter-typescript", "typescript grammar id");
Check(typeScriptPlugin.Grammar.AbiVersion == 14, "typescript grammar ABI");
Check(!typeScript.Result.WarningCodes.Contains("typescript.parse-error"), "typescript no parse diagnostics");
Check(typeScript.Nodes.Any(node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Smoke"), "typescript class node");
Check(typeScript.Nodes.Any(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Smoke.run"), "typescript method node");
CheckStructural(typeScript, "typescript");
Console.WriteLine($"typescript provider version: {typeScriptPlugin.Version}");

var javaScriptPlugin = new TreeSitterJavaScriptPlugin();
var javaScript = await RunAsync(
    javaScriptPlugin,
    "src/smoke.js",
    "const fs = require(\"node:fs\");\nexport function main() { return fs.readFileSync(\"x\"); }");
Check(javaScriptPlugin.Id.Value == "penghou.hetu.tree-sitter.javascript", "javascript plugin identity");
Check(javaScriptPlugin.Language == "javascript", "javascript language");
Check(javaScriptPlugin.Grammar.GrammarId == "tree-sitter-javascript", "javascript grammar id");
Check(javaScriptPlugin.Grammar.AbiVersion == 15, "javascript grammar ABI");
Check(!javaScript.Result.WarningCodes.Contains("javascript.parse-error"), "javascript no parse diagnostics");
Check(javaScript.Nodes.Any(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "main"), "javascript function node");
Check(
    javaScript.Edges.Any(edge => edge.Kind == CodeEdgeKinds.Imports && javaScript.Nodes.Single(node => node.Id == edge.TargetId).QualifiedName == "node:fs"),
    "javascript require import");
CheckStructural(javaScript, "javascript");
Console.WriteLine($"javascript provider version: {javaScriptPlugin.Version}");

var csharpPlugin = new CSharpCodeGraphPlugin();
var csharp = await RunAsync(csharpPlugin, "Smoke.cs", "public class Smoke { public void Run() { } }");
Check(csharpPlugin.Id.Value == "penghou.hetu.csharp", "historical C# plugin identity");
Check(csharp.Nodes.Any(node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Smoke"), "packed Roslyn extraction");
Check(typeof(Andirit.Testing.CodeGraphStoreConformanceSuite).Assembly.GetName().Name == "Andirit.Testing", "testing package loads");

var directory = Directory.CreateTempSubdirectory("andirit-packed-consumer-");
try
{
    await File.WriteAllTextAsync(Path.Combine(directory.FullName, "smoke.ts"), "export class Packed { }");
    var database = Path.Combine(directory.FullName, "graph.db");
    var repository = new CodeRepositoryId("repo:packed-andirit");
    using (var store = new LatticeCodeGraphStore(database))
    {
        await using var host = new AndiritHostBuilder().UseStore(store).AddPlugin(new TreeSitterTypeScriptPlugin()).Build();
        var indexed = await host.Indexing.IndexAsync(new CodeRepositoryDescriptor(repository, directory.FullName), new CodeIndexRunId("run:packed-andirit"));
        Check(indexed.Diagnostics.Status == CodeIndexRunStatus.Completed, "packed host indexing");
        Check((await store.CheckHealthAsync()).SchemaVersion == 2, "packed LatticeDB schema 2");
    }
    using var reopened = new LatticeCodeGraphStore(database, new Andirit.LatticeDb.LatticeDbStoreOptions { UseNativeTraversal = true });
    Check((await reopened.FindNodesByQualifiedNameAsync(repository, "Packed")).Count(node => node.Kind == CodeNodeKinds.Type) == 1, "packed durable reopen and native mirror");
    var snapshot = await CodePublicationSnapshot.ExportAsync(reopened, repository);
    Check(CodePublicationSnapshot.FromJson(snapshot.ToJson()).IntegrityHash == snapshot.IntegrityHash, "packed snapshot integrity");
}
finally
{
    directory.Delete(recursive: true);
}

var productAssemblies = new[] { typeof(CodeGraphNode).Assembly, typeof(AndiritHost).Assembly,
    typeof(CSharpCodeGraphPlugin).Assembly, typeof(TreeSitterTypeScriptPlugin).Assembly,
    typeof(LatticeCodeGraphStore).Assembly, typeof(Andirit.Testing.CodeGraphStoreConformanceSuite).Assembly };
var expectedAssemblies = new[] { "Andirit", "Andirit.Abstractions", "Andirit.CSharp", "Andirit.TreeSitter", "Andirit.LatticeDb", "Andirit.Testing" };
Check(productAssemblies.Select(assembly => assembly.GetName().Name!).Order(StringComparer.Ordinal).SequenceEqual(expectedAssemblies.Order(StringComparer.Ordinal)), "exact six standalone assembly identities");
Check(productAssemblies.SelectMany(assembly => assembly.GetReferencedAssemblies()).All(reference => !reference.Name!.StartsWith("Penghou.", StringComparison.Ordinal)), "no old Penghou.Andirit or Hetu assembly dependencies");
var conformance = await Andirit.Testing.CodeGraphStoreConformanceSuite.VerifyAsync(new PackedStoreFixture());
Check(conformance.PassedChecks.Count > 0, "packed Testing package executes store conformance laws");
Console.WriteLine("PASS: all six packed Andirit packages, managed providers, durable store, testing conformance and Tree-sitter natives resolved.");
return 0;

static void CheckStructural(Extraction extraction, string language)
{
    Check(extraction.Edges.All(edge => edge.Evidence.Quality == EvidenceQuality.Structural), $"{language} structural evidence only");
    Check(extraction.Edges.All(edge => edge.Kind != CodeEdgeKinds.Calls), $"{language} no resolved calls");
    Check(extraction.Edges.All(edge => edge.Kind != CodeEdgeKinds.References), $"{language} no references");
}

static async Task<Extraction> RunAsync(ICodeGraphPlugin plugin, string path, string source)
{
    var context = new CodeGraphPluginContext(
        new CodeRepositoryId("repo:smoke"),
        "memory://smoke",
        new CodeIndexRunId("run:smoke"),
        [
            new CodeGraphSource(
                path,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)))}",
                _ => new ValueTask<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(source))))
        ]);
    var sink = new CollectingSink();
    await using var session = await plugin.CreateSessionAsync(context);
    var result = await session.ExtractAsync(sink);
    return new(
        sink.Batches.SelectMany(batch => batch.Nodes).ToArray(),
        sink.Batches.SelectMany(batch => batch.Edges).ToArray(),
        result);
}

static void Check(bool condition, string name)
{
    Console.WriteLine($"{(condition ? "ok" : "FAIL")}: {name}");
    if (!condition)
        Environment.Exit(1);
}

internal sealed record Extraction(
    IReadOnlyList<CodeGraphNode> Nodes,
    IReadOnlyList<CodeGraphEdge> Edges,
    CodeGraphExtractionResult Result);

internal sealed class CollectingSink : ICodeGraphSink
{
    public CodeGraphBatchLimits Limits { get; } = new();
    public List<CodeGraphBatch> Batches { get; } = [];
    public ValueTask WriteBatchAsync(CodeGraphBatch batch, CancellationToken cancellationToken = default)
    {
        Batches.Add(batch);
        return ValueTask.CompletedTask;
    }
}

internal sealed class PackedStoreFixture : Andirit.Testing.ICodeGraphStoreFixture
{
    public ICodeGraphStore CreateStore() => new InMemoryCodeGraphStore();
}
