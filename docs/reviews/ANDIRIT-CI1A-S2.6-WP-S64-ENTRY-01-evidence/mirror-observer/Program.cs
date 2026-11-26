using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

// Read-only on the repository. All graph writes go to a disposable database.
// The fixture is the two-project linked-file shape from CSharpCorrectnessRegressionTests.
if (args is not [var destination, "graph-only" or "integrated"])
    throw new ArgumentException("Supply a database path and graph-only or integrated.");
var databasePath = Path.GetFullPath(destination);
var graphOnly = args[1] == "graph-only";
if (File.Exists(databasePath))
    throw new InvalidOperationException("Supply a fresh database path.");

var repository = new CodeRepositoryId("review:s64:mirror");
var plugin = new CSharpCodeGraphPlugin();
var start = DateTimeOffset.UnixEpoch.AddDays(1);
const string linkedPath = "Shared/Linked.cs";
var files = new SortedDictionary<string, string>(StringComparer.Ordinal)
{
    [linkedPath] = "namespace Example; public static class Helper { public static void Go() {} }",
    ["src/ALib/ALib.csproj"] = Project("ALib"),
    ["src/BLib/BLib.csproj"] = Project("BLib"),
    ["src/ALib/Anchor.cs"] = "namespace ALib; public class Anchor {}",
    ["src/BLib/Anchor.cs"] = "namespace BLib; public class Anchor {}",
    ["src/App/App.csproj"] = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
          <ItemGroup><ProjectReference Include="../BLib/BLib.csproj" /></ItemGroup>
        </Project>
        """,
    ["src/App/Program.cs"] = "namespace App; public class Program { public void Run() => Example.Helper.Go(); }"
};
var mirror = new LatticeDbStoreOptions { UseNativeTraversal = true };
string nodeId;
string[] unitIds;
int logicalNodeCount;
string[] nativeTraversal;
string[] projectionTraversal;
string? nativeRoute;
using (var store = new LatticeCodeGraphStore(databasePath, mirror))
{
    await Publish(store, "run:enabled", start);
    var units = await store.GetPublishedUnitsAsync(repository);
    logicalNodeCount = units.SelectMany(unit => unit.Nodes).Select(node => node.Id).Distinct().Count();
    var linked = units
        .SelectMany(unit => unit.Nodes.Where(node => node.QualifiedName == linkedPath)
            .Select(node => (unit.Origin.IndexUnitId.Value, Node: node)))
        .ToArray();
    nodeId = linked.Select(pair => pair.Node.Id.Value).Distinct(StringComparer.Ordinal).Single();
    unitIds = linked.Select(pair => pair.Value).Order(StringComparer.Ordinal).ToArray();
    nativeRoute = (await store.CheckHealthAsync()).Detail;
    nativeTraversal = await Traverse(store, nodeId);
}
var initialRows = ReadPhysicalRows(databasePath, nodeId);
using (var store = new LatticeCodeGraphStore(databasePath, mirror))
    await Traverse(store, nodeId);
var enabledReopenRows = ReadPhysicalRows(databasePath, nodeId);
using (var store = new LatticeCodeGraphStore(databasePath))
{
    projectionTraversal = await Traverse(store, nodeId);
    await Publish(store, "run:disabled", start.AddMinutes(1));
}
var disabledRows = ReadPhysicalRows(databasePath, nodeId);
string[] reopenedTraversal;
using (var store = new LatticeCodeGraphStore(databasePath, mirror))
    reopenedTraversal = await Traverse(store, nodeId);
var reopenedRows = ReadPhysicalRows(databasePath, nodeId);

Console.WriteLine(JsonSerializer.Serialize(new
{
    os = RuntimeInformation.OSDescription,
    processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    framework = RuntimeInformation.FrameworkDescription,
    mode = args[1],
    fixtureFiles = files.ToDictionary(pair => pair.Key, pair => Digest(Encoding.UTF8.GetBytes(pair.Value))),
    logicalNode = nodeId,
    logicalNodeCount,
    unitIds,
    nativeRoute,
    projectionTraversal,
    nativeTraversal,
    reopenedTraversal,
    initialRows,
    enabledReopenRows,
    disabledRows,
    reopenedRows
}, new JsonSerializerOptions { WriteIndented = true }));

async Task Publish(LatticeCodeGraphStore store, string id, DateTimeOffset started)
{
    var run = new CodeIndexRunId(id);
    await store.UpsertRepositoryAsync(new(repository));
    await store.StoreIndexRunAsync(new(repository, run, started, plugins: [plugin.Id]));
    var context = new CodeGraphPluginContext(repository, null, run, files.Select(pair =>
    {
        var bytes = Encoding.UTF8.GetBytes(pair.Value);
        return new CodeGraphSource(pair.Key, $"sha256:{Digest(bytes)}",
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
    }).ToArray());
    await using var sink = new CodeGraphIngestionSink(store);
    await using var session = await plugin.CreateSessionAsync(context);
    await session.ExtractAsync(graphOnly ? new GraphOnlySink(sink) : sink);
    await sink.CompleteAsync();
    await store.CompleteIndexRunAsync(new(repository, run, started, CodeIndexRunStatus.Completed,
        started.AddSeconds(1), [plugin.Id]), new(repository, run, []));
}

async Task<string[]> Traverse(LatticeCodeGraphStore store, string root) =>
    (await store.TraverseAsync(repository, new(new(root), CodeGraphDirection.Incoming,
        maxDepth: 1, maxNodes: 100, maxEdges: 100))).Edges
    .Select(edge => edge.Id.Value).ToArray();

static object ReadPhysicalRows(string path, string id)
{
    using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
    using var txn = database.BeginReadTransaction();
    using var nodeQuery = database.Prepare("MATCH (n:HetuGraphNode) RETURN n.hetu_json");
    using var nodeResult = nodeQuery.Execute(txn);
    var nodeCount = 0;
    var linkedCount = 0;
    foreach (var row in nodeResult.ReadAll())
    {
        using var payload = JsonDocument.Parse(row.GetValue(0).AsString());
        nodeCount++;
        if (payload.RootElement.GetProperty("Id").GetProperty("Value").GetString() == id)
            linkedCount++;
    }
    using var manifestQuery = database.Prepare("MATCH (n:HetuMirror) RETURN n.key");
    using var manifestResult = manifestQuery.Execute(txn);
    var manifestCount = manifestResult.ReadAll().Count();
    using var occurrenceQuery = database.Prepare("MATCH (n:AndiritOccurrenceUnit) RETURN n.payload");
    using var occurrenceResult = occurrenceQuery.Execute(txn);
    var occurrenceFacts = 0;
    foreach (var row in occurrenceResult.ReadAll())
    {
        using var payload = JsonDocument.Parse(Convert.FromBase64String(row.GetValue(0).AsString()));
        occurrenceFacts += payload.RootElement.GetProperty("Facts").GetArrayLength();
    }
    txn.Commit();
    return new { nodeCount, linkedCount, manifestCount, occurrenceFacts };
}

static string Digest(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

static string Project(string name) => $"""
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <AssemblyName>{name}</AssemblyName>
      </PropertyGroup>
      <ItemGroup>
        <Compile Include="../../Shared/Linked.cs" />
      </ItemGroup>
    </Project>
    """;

sealed class GraphOnlySink(CodeGraphIngestionSink sink) : ICodeGraphSink
{
    public CodeGraphBatchLimits Limits => sink.Limits;

    public ValueTask WriteBatchAsync(CodeGraphBatch batch, CancellationToken cancellationToken = default) =>
        sink.WriteBatchAsync(new(batch.Origin, batch.Nodes, batch.Declarations, batch.Edges,
            batch.CompletesIndexUnit), cancellationToken);
}
