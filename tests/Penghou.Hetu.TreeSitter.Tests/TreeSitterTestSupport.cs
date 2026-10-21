using System.Security.Cryptography;
using System.Text;

namespace Penghou.Hetu.TreeSitter.Tests;

internal sealed record Extraction(
    IReadOnlyList<CodeGraphNode> Nodes,
    IReadOnlyList<CodeGraphDeclaration> Declarations,
    IReadOnlyList<CodeGraphEdge> Edges,
    CodeGraphExtractionResult Result,
    IReadOnlyList<CodeGraphBatch> Batches);

internal sealed class RecordingSink(CodeGraphBatchLimits limits) : ICodeGraphSink
{
    public CodeGraphBatchLimits Limits { get; } = limits;
    public List<CodeGraphBatch> Batches { get; } = [];

    public ValueTask WriteBatchAsync(
        CodeGraphBatch batch,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Batches.Add(batch);
        return ValueTask.CompletedTask;
    }
}

internal static class TreeSitterTestSupport
{
    internal static Task<Extraction> ExtractAsync(
        ICodeGraphPlugin plugin,
        params (string Path, string Content)[] values) =>
        ExtractAsync(plugin, new CodeGraphBatchLimits(), values);

    internal static async Task<Extraction> ExtractAsync(
        ICodeGraphPlugin plugin,
        CodeGraphBatchLimits limits,
        params (string Path, string Content)[] values)
    {
        var sources = values.Select(value => new CodeGraphSource(
            value.Path,
            $"sha256:{Hash(value.Content)}",
            _ => new ValueTask<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(value.Content))))).ToArray();
        var context = new CodeGraphPluginContext(
            new CodeRepositoryId("repo:test"),
            "memory://test",
            new CodeIndexRunId("run:test"),
            sources);
        var sink = new RecordingSink(limits);

        await using var session = await plugin.CreateSessionAsync(context);
        var result = await session.ExtractAsync(sink);

        if (sources.Length > 0)
        {
            Assert.True(sink.Batches.Count > 0);
            Assert.True(sink.Batches[^1].CompletesIndexUnit);
        }

        return new(
            sink.Batches.SelectMany(batch => batch.Nodes).OrderBy(node => node.Id.Value, StringComparer.Ordinal).ToArray(),
            sink.Batches.SelectMany(batch => batch.Declarations).OrderBy(value => value.Id.Value, StringComparer.Ordinal).ToArray(),
            sink.Batches.SelectMany(batch => batch.Edges).OrderBy(edge => edge.Id.Value, StringComparer.Ordinal).ToArray(),
            result,
            sink.Batches.ToArray());
    }

    internal static string Hash(string content) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
