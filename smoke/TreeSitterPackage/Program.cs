// Pack-consume-load smoke test for the Penghou.Hetu.TreeSitter NuGet package.
// This project is intentionally outside the solution: it restores the PACKED
// package (plus its dependencies) from a package feed, proving the native
// RID assets resolve and parse through the real NuGet layout rather than a
// repository build output. Run with:
//   dotnet run -p:RestoreSources="<feed>;<nuget.org>" [-p:SmokerPackageVersion=<ver>]
using System.Text;
using Penghou.Hetu;

var plugin = new TreeSitterElixirPlugin();
Check(plugin.Id.Value == "penghou.hetu.tree-sitter.elixir", "plugin identity");
Check(plugin.Language == "elixir", "language");
Check(plugin.Grammar.GrammarId == "tree-sitter-elixir", "grammar id");
Check(plugin.Grammar.AbiVersion == 14, "grammar ABI");
Console.WriteLine($"provider version: {plugin.Version}");

const string Source = """
    defmodule Smoke.Test do
      def run(x), do: x
    end
    """;
var context = new CodeGraphPluginContext(
    new CodeRepositoryId("repo:smoke"),
    "memory://smoke",
    new CodeIndexRunId("run:smoke"),
    [
        new CodeGraphSource(
            "lib/smoke.ex",
            $"sha256:{Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(Source)))}",
            _ => new ValueTask<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(Source))))
    ]);
var sink = new CollectingSink();
await using (var session = await plugin.CreateSessionAsync(context))
{
    var result = await session.ExtractAsync(sink);
    Check(!result.WarningCodes.Contains("elixir.parse-error"), "no parse diagnostics");
}

var nodes = sink.Batches.SelectMany(batch => batch.Nodes).ToArray();
var edges = sink.Batches.SelectMany(batch => batch.Edges).ToArray();
Check(nodes.Any(node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Smoke.Test"), "module node");
Check(nodes.Any(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Smoke.Test.run/1"), "function node");
Check(edges.All(edge => edge.Evidence.Quality == EvidenceQuality.Structural), "structural evidence only");
Check(edges.All(edge => edge.Kind != CodeEdgeKinds.Calls), "no resolved calls");
Console.WriteLine($"PASS: {nodes.Length} nodes, {edges.Length} edges from the packed Tree-sitter provider.");
return 0;

static void Check(bool condition, string name)
{
    Console.WriteLine($"{(condition ? "ok" : "FAIL")}: {name}");
    if (!condition)
        Environment.Exit(1);
}

sealed class CollectingSink : ICodeGraphSink
{
    public CodeGraphBatchLimits Limits { get; } = new();
    public List<CodeGraphBatch> Batches { get; } = [];
    public ValueTask WriteBatchAsync(CodeGraphBatch batch, CancellationToken cancellationToken = default)
    {
        Batches.Add(batch);
        return ValueTask.CompletedTask;
    }
}
