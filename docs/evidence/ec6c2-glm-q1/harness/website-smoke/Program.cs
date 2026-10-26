using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Penghou.Hetu;

// GLM-Q1 qualification harness: pinned-website (f9cca177) repository smoke.
// Indexes the nine supported files with the p9 ECMAScript plugins and prints
// every export occurrence with its public name, token span, and identity.
// The harness copies only the git-tracked supported files into a clean root
// first, mirroring a pristine checkout (the local clone carries gitignored
// `.astro/` build residue that a clean clone does not contain).
var websiteRoot = @"C:\Users\Laszlos\source\repos\website";
var reportPath = args.Length > 0 ? args[0] : "smoke-report.json";

var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".ts", ".tsx", ".mts", ".cts", ".mjs", ".cjs", ".js" };
var trackedFiles = GitTrackedFiles(websiteRoot)
    .Where(path => supportedExtensions.Contains(Path.GetExtension(path)))
    .OrderBy(path => path, StringComparer.Ordinal)
    .ToArray();
var sourceRoot = Path.Combine(Path.GetTempPath(), "hetu-q1-smoke-" + Guid.NewGuid().ToString("N")[..8]);
foreach (var relative in trackedFiles)
{
    var destination = Path.Combine(sourceRoot, relative);
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    File.Copy(Path.Combine(websiteRoot, relative), destination);
}

var repositoryId = new CodeRepositoryId("repo:q1-website-smoke");
var store = new InMemoryCodeGraphStore();
var indexing = new CodeIndexingService(
    new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
    new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]),
    store);
var result = await indexing.IndexAsync(
    new CodeRepositoryDescriptor(repositoryId, sourceRoot),
    new CodeIndexRunId("run:q1-website-smoke"));
Console.WriteLine($"index status: {result.Diagnostics.Status}");
Console.WriteLine($"warnings: [{string.Join(", ", result.Diagnostics.WarningCodes)}]");

var units = await store.GetPublishedUnitsAsync(repositoryId);
var fileUnits = units.Where(unit => unit.Origin.SourcePath is not null).ToArray();
Console.WriteLine($"file units: {fileUnits.Length}");
foreach (var unit in fileUnits)
    Console.WriteLine($"  unit: {unit.Origin.SourcePath}");

var exportNodes = units.SelectMany(unit => unit.Nodes).Where(node => node.Kind == CodeNodeKinds.Export).ToArray();
var exportEdges = units.SelectMany(unit => unit.Edges).Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();
Console.WriteLine($"export entries: {exportNodes.Length}");
Console.WriteLine($"export occurrence edges: {exportEdges.Length}");

var entriesByName = exportNodes.ToDictionary(node => node.Id.Value, node => node.Name);
var perFile = new List<FileSmoke>();
foreach (var unit in fileUnits.OrderBy(unit => unit.Origin.SourcePath, StringComparer.Ordinal))
{
    var path = unit.Origin.SourcePath!;
    var edges = unit.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();
    var file = new FileSmoke(path, edges.Length, new List<OccurrenceSmoke>());
    foreach (var edge in edges.OrderBy(edge => edge.Evidence.Location!.StartLine).ThenBy(edge => edge.Evidence.Location!.StartColumn))
    {
        var name = entriesByName[edge.TargetId.Value];
        var location = edge.Evidence.Location!;
        file.Occurrences.Add(new OccurrenceSmoke(
            name,
            $"{location.StartLine}:{location.StartColumn}-{location.EndLine}:{location.EndColumn}",
            edge.TargetId.Value,
            edge.Id.Value,
            Text(edge, CodePropertyKeys.ExportKind),
            Text(edge, CodePropertyKeys.ExportVia),
            edge.Properties.TryGetValue(CodePropertyKeys.LocalName, out var local) ? ((CodeTextProperty)local).Value : null,
            edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var typeOnly) && ((CodeBooleanProperty)typeOnly).Value,
            Text(edge, CodePropertyKeys.NamespaceProjection)));
    }
    perFile.Add(file);
}

var astroNodes = exportNodes.Count(node => node.QualifiedName?.EndsWith(".astro", StringComparison.Ordinal) == true);
var astroEdges = exportEdges.Count(edge => (edge.Evidence.Location?.Path ?? "").EndsWith(".astro", StringComparison.Ordinal));

var report = new SmokeReport(
    result.Diagnostics.WarningCodes.ToArray(),
    fileUnits.Select(unit => unit.Origin.SourcePath!).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
    exportNodes.Length,
    exportEdges.Length,
    perFile,
    astroNodes + astroEdges);
File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

foreach (var file in perFile)
{
    Console.WriteLine($"== {file.Path} ({file.OccurrenceCount})");
    foreach (var occurrence in file.Occurrences)
        Console.WriteLine($"   {occurrence.Name} @ {occurrence.Span} proj={occurrence.Projection} kind={occurrence.ExportKind} via={occurrence.ExportVia} local={occurrence.LocalName ?? "<absent>"}");
}
Console.WriteLine($"astro export facts: {astroNodes + astroEdges}");
Console.WriteLine($"total occurrences: {exportEdges.Length}");
return 0;

static string[] GitTrackedFiles(string repositoryRoot)
{
    var startInfo = new System.Diagnostics.ProcessStartInfo("git", "ls-files")
    {
        WorkingDirectory = repositoryRoot,
        RedirectStandardOutput = true,
        UseShellExecute = false
    };
    using var process = System.Diagnostics.Process.Start(startInfo)!;
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static string Text(CodeGraphEdge edge, string key) =>
    edge.Properties.TryGetValue(key, out var value) && value is CodeTextProperty text ? text.Value : "<absent>";

sealed record SmokeReport(string[] WarningCodes, string[] IndexedFiles, int ExportEntries, int ExportEdges, List<FileSmoke> Files, int AstroExportFacts);

sealed record FileSmoke(string Path, int OccurrenceCount, List<OccurrenceSmoke> Occurrences);

sealed record OccurrenceSmoke(string Name, string Span, string EntryId, string EdgeId, string ExportKind, string ExportVia, string? LocalName, bool TypeOnly, string Projection);
