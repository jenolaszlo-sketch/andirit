using System.Text.Json;
using Penghou.Hetu;

var evidence = Path.GetFullPath(args[0]);
var cases = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(Path.Combine(evidence, "cases.json")))!;
var results = new Dictionary<string, object>();
foreach (var (name, files) in cases)
{
    var dir = Path.Combine(evidence, "bin", "fixtures", name);
    Directory.CreateDirectory(dir);
    foreach (var (file, source) in files) File.WriteAllText(Path.Combine(dir, file), source);
    var repo = new CodeRepositoryId("repo:q2:" + name);
    var store = new InMemoryCodeGraphStore();
    var indexer = new CodeIndexingService(new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]), new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]), store);
    var indexed = await indexer.IndexAsync(new CodeRepositoryDescriptor(repo,dir),new CodeIndexRunId("run:q2:first"));
    var snapshot = await CodePublicationSnapshot.ExportAsync(store,repo);
    var nodes = snapshot.Units.SelectMany(u=>u.Nodes).DistinctBy(n=>n.Id).ToArray();
    var declarations = snapshot.Units.SelectMany(u=>u.Declarations).DistinctBy(d=>d.Id).ToArray();
    var edges = snapshot.Units.SelectMany(u=>u.Edges).DistinctBy(e=>e.Id).ToArray();
    results[name] = new {
        indexed.Publication.IsConsistentSnapshot,
        indexed.Diagnostics,
        nodes = nodes.Select(n=>new {id=n.Id.Value,kind=n.Kind.Value,n.Name,n.QualifiedName,symbol=n.SymbolId?.Value, physicalDeclarations=declarations.Where(d=>d.SymbolNodeId==n.Id).Select(d=>new {id=d.Id.Value,d.Location})}),
        edges = edges.Select(e=>new {id=e.Id.Value,kind=e.Kind.Value,source=e.SourceId.Value,target=e.TargetId.Value,e.Evidence,properties=e.Properties.ToDictionary(p=>p.Key,p=>Value(p.Value))})
    };
    if (name == "idle")
    {
        File.AppendAllText(Path.Combine(dir,"consumer.ts"),"export const change = 1;\n");
        var next = await indexer.IndexAsync(new CodeRepositoryDescriptor(repo,dir),new CodeIndexRunId("run:q2:second"));
        var snap2 = await CodePublicationSnapshot.ExportAsync(store,repo);
        results["incremental-origins"] = new {publicationRun=next.Publication.IndexRunId.Value,units=snap2.Units.Select(u=>new {unit=u.Origin.IndexUnitId.Value,run=u.Origin.IndexRunId.Value,u.Origin.SourcePath,u.Origin.SourceHash})};
    }
}
File.WriteAllText(Path.Combine(evidence,"p9-adversarial-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine($"Recorded {cases.Count} p9 fixture publications and incremental origins; no EC-6D query exists or was tested.");
static object? Value(CodePropertyValue value) => value switch {
    CodeTextProperty x=>x.Value, CodeBooleanProperty x=>x.Value, CodeIntegerProperty x=>x.Value, CodeTextListProperty x=>x.Values, _=>value.ToString()
};
