using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.CSharp;
using Andirit.LatticeDb;

if (args is not ["c4", var corpusRoot, var oraclePath, var databasePath, var outputPath])
{
    Console.Error.WriteLine("usage: D3Campaign c4 <corpus-root> <oracle.json> <fresh.ltdb> <output.json>");
    return 2;
}

var root = Path.GetFullPath(corpusRoot);
var database = Path.GetFullPath(databasePath);
var oracle = JsonSerializer.Deserialize<Oracle>(
    await File.ReadAllTextAsync(oraclePath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
var repository = new CodeRepositoryId(oracle.Repository);
var plugin = new CSharpCodeGraphPlugin();
var run = new CodeIndexRunId("run:c4-baseline");
var started = new DateTimeOffset(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);
var sources = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
    .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) ||
        path.EndsWith(".csproj", StringComparison.Ordinal))
    .OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal)
    .Select(path =>
    {
        var bytes = File.ReadAllBytes(path);
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        return new CodeGraphSource(
            relative,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
    }).ToArray();

await DumpAnalysis();

CodeOccurrenceQueryResult result;
using (var store = new LatticeCodeGraphStore(database))
{
    await store.UpsertRepositoryAsync(new(repository));
    await store.StoreIndexRunAsync(
        new(repository, run, started, plugins: [plugin.Id]));
    var context = new CodeGraphPluginContext(repository, null, run, sources);
    await using (var sink = new CodeGraphIngestionSink(store))
    await using (var session = await plugin.CreateSessionAsync(context))
    {
        await session.ExtractAsync(sink);
        await sink.CompleteAsync();
    }
    await store.CompleteIndexRunAsync(
        new(repository, run, started, CodeIndexRunStatus.Completed,
            started.AddSeconds(1), [plugin.Id]),
        new(repository, run, []));
    var units = await store.GetPublishedUnitsAsync(repository);
    Console.Error.WriteLine("units=" + units.Count + " facts=" + string.Join(",", units.Select(unit => unit.Occurrences.Count)) + " coverage=" + string.Join(",", units.Select(unit => unit.OccurrenceCoverage?.State.ToString() ?? "null")) + " requires=" + string.Join(",", units.Select(unit => Prop(unit, "RequiresOccurrenceAuthority"))) + " auth=" + string.Join(",", units.Select(unit => Prop(unit, "OccurrenceAuthority") is not null)));
    result = await store.FindOccurrencesAsync(
        new(repository, new CodeOccurrenceTargetId(oracle.Target.Value), maxResults: 1000));
}

var actual = new
{
    target = oracle.Target.Value,
    availability = result.Availability.ToString(),
    occurrences = result.Occurrences.Select(hit => new
    {
        id = hit.Id.Value,
        unit = hit.SourceUnitId,
        path = hit.Span.Path,
        startLine = hit.Span.StartLine,
        startColumn = hit.Span.StartColumn,
        endLine = hit.Span.EndLine,
        endColumn = hit.Span.EndColumn,
        contributors = hit.Contributors.Select(c =>
            new { plugin = c.PluginId.Value, unit = c.IndexUnitId }).ToArray()
    }).OrderBy(item => item.unit, StringComparer.Ordinal)
        .ThenBy(item => item.path, StringComparer.Ordinal)
        .ThenBy(item => item.startLine)
        .ThenBy(item => item.startColumn).ToArray(),
    cardinality = result.Occurrences.Count,
    contributors = result.Occurrences.Select(hit => hit.SourceUnitId)
        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
};
var json = JsonSerializer.Serialize(actual, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
await File.WriteAllTextAsync(outputPath, json);

async Task DumpAnalysis()
{
    var context = new CodeGraphPluginContext(repository, null, run, sources);
    var method = typeof(CSharpCodeGraphPlugin).GetMethod(
        "PrepareAnalysisAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
    var valueTask = method.Invoke(null, [context, CancellationToken.None])!;
    var task = (Task)valueTask.GetType().GetMethod("AsTask")!.Invoke(valueTask, null)!;
    await task;
    var analysis = Prop(task, "Result")!;
    var universe = Prop(analysis, "Universe")!;
    Console.Error.WriteLine("analysis complete=" + Prop(universe, "IsComplete"));
    Console.Error.WriteLine("refusals=" + string.Join(";",
        ((System.Collections.IEnumerable)Prop(universe, "Refusals")!).Cast<object>()));
    Console.Error.WriteLine("missing=" + string.Join(";",
        ((System.Collections.IEnumerable)Prop(universe, "MissingScopeUnits")!).Cast<object>()));
    foreach (var project in (System.Collections.IEnumerable)Prop(analysis, "Projects")!)
    {
        var model = Prop(project, "Project")!;
        var path = Prop(model, "Path");
        var verified = Prop(project, "Verified") is not null;
        var refusals = string.Join(";",
            ((System.Collections.IEnumerable)Prop(project, "Refusals")!).Cast<object>());
        Console.Error.WriteLine($"project={path} verified={verified} refusals={refusals}");
    }
}
object? Prop(object target, string name) => target.GetType()
    .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
    .GetValue(target);

return 0;

internal sealed record Oracle(
    string Variant,
    string Repository,
    string Plugin,
    string PluginVersion,
    Target Target,
    Occurrence[] Occurrences,
    int Cardinality,
    int TargetContributorCount);

internal sealed record Target(
    string Value, string Document, string Context, string Metadata, string Module,
    string[] Contributors);

internal sealed record Occurrence(
    string Id, string Unit, string Path, string SourceHash,
    int StartLine, int StartColumn, int EndLine, int EndColumn,
    string TargetDocument, string TargetId, string Category);