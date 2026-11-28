using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.CSharp;
using Andirit.LatticeDb;
var root = Path.GetFullPath(args[0]);
var db = Path.GetFullPath(args[1]);
var repo = new CodeRepositoryId("review:s64:d3-c4");
var sources = Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories)
    .Where(p=>p.EndsWith(".cs")||p.EndsWith(".csproj"))
    .OrderBy(p=>Path.GetRelativePath(root,p),StringComparer.Ordinal)
    .Select(path=>{var b=File.ReadAllBytes(path);return new CodeGraphSource(Path.GetRelativePath(root,path).Replace('\\','/'),"sha256:"+Convert.ToHexStringLower(SHA256.HashData(b)),_=>ValueTask.FromResult<Stream>(new MemoryStream(b,false)));})
    .ToArray();
var context = new CodeGraphPluginContext(repo,null,new CodeIndexRunId("run:query"),sources);
var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
var caller = analysis.Projects.FirstOrDefault(p=>p.Project.Path=="Caller/Caller.csproj")?.Verified
    ?? analysis.Projects.Single(p=>p.Project.Path=="Caller2/Caller2.csproj").Verified!;
var target = new CodeOccurrenceTargetId(args[2]);
using var store = new LatticeCodeGraphStore(db);
var result = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
    new CodeOccurrenceQuery(repo,target,maxResults:1000), analysis.Universe, caller.Compilation));
Console.WriteLine(JsonSerializer.Serialize(new {
    availability=result.Availability.ToString(),
    isComplete=result.IsComplete,
    total=result.TotalMatches,
    count=result.Occurrences.Count,
    units=result.Occurrences.Select(o=>o.SourceUnitId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
    target=target.Value,
    ids=result.Occurrences.Select(o=>o.Id.Value).Order(StringComparer.Ordinal).ToArray(),
    coverage=result.Coverage is null ? null : new { result.Coverage.State, result.Coverage.InvocationFacts, result.Coverage.DeclaredUnitCount, result.Coverage.VerifiedUnitCount },
}, new JsonSerializerOptions{WriteIndented=true}));

