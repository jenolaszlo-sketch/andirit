using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.CSharp;
using Andirit.LatticeDb;

if (args.Length != 3)
{
    Console.Error.WriteLine("usage: ConsoleHostProbe <corpus-root> <database> <output.json>");
    return 2;
}

var root = Path.GetFullPath(args[0]);
var database = Path.GetFullPath(args[1]);
var output = Path.GetFullPath(args[2]);
const string repository = "review:s64:d3-c4";
var repositoryId = new CodeRepositoryId(repository);
var plugin = new CSharpCodeGraphPlugin();
var run = new CodeIndexRunId("run:console-host-probe");
var started = new DateTimeOffset(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);
var sources = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
    .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) ||
                   path.EndsWith(".csproj", StringComparison.Ordinal))
    .OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal)
    .Select(path =>
    {
        var bytes = File.ReadAllBytes(path);
        return new CodeGraphSource(
            Path.GetRelativePath(root, path).Replace('\\', '/'),
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
    }).ToArray();
var context = new CodeGraphPluginContext(repositoryId, null, run, sources);
var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
var dependency = analysis.Projects.Single(project =>
    project.Project.Path == "Dependency/Dependency.csproj").Verified!;
var references = dependency.Descriptor.References.ToArray();
var referenceSetHash = Convert.ToHexStringLower(SHA256.HashData(
    Encoding.UTF8.GetBytes(string.Join("\n", references))));

using var store = new LatticeCodeGraphStore(database);
var facts = Array.Empty<CodeOccurrenceFact>();
{
    await store.UpsertRepositoryAsync(new(repositoryId));
    await store.StoreIndexRunAsync(new(repositoryId, run, started, plugins: [plugin.Id]));
    await using (var sink = new CodeGraphIngestionSink(store))
    await using (var session = await plugin.CreateSessionAsync(context))
    {
        await session.ExtractAsync(sink);
        await sink.CompleteAsync();
    }
    await store.CompleteIndexRunAsync(
        new(repositoryId, run, started, CodeIndexRunStatus.Completed,
            started.AddSeconds(1), [plugin.Id]),
        new(repositoryId, run, []));
    facts = (await store.GetPublishedUnitsAsync(repositoryId))
        .SelectMany(unit => unit.Occurrences).ToArray();
}
var result = new
{
    host = Environment.ProcessPath,
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
    architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
    repository,
    target = facts.Select(fact => fact.TargetId.Value).Distinct(StringComparer.Ordinal).Single(),
    cardinality = facts.Length,
    units = facts.GroupBy(fact => fact.SourceUnitId, StringComparer.Ordinal)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal),
    productionDescriptor = dependency.Descriptor.DescriptorDigest,
    definingContext = dependency.MapR2FaithfulDefiningContext().DescriptorDigest,
    referenceCount = references.Length,
    referenceSetHash,
    referenceItems = references.Select(reference => reference.Split('|')[0]).Distinct(StringComparer.Ordinal).ToArray(),
    occurrenceDomain = CodeOccurrenceIdentity.UsageDomain,
    targetDomain = CodeOccurrenceIdentity.TargetDomain,
    identityGeneration = CodeOccurrenceIdentity.Generation,
};
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(output, json + "\n");
Console.WriteLine(json);
return 0;
