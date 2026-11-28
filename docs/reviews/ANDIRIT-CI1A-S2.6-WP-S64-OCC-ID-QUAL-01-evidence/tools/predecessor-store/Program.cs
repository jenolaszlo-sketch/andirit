using System.Security.Cryptography;
using System.Text;
using Andirit;
using Andirit.CSharp;
using Andirit.LatticeDb;
var root = Path.GetFullPath(args[0]);
var db = Path.GetFullPath(args[1]);
var repo = new CodeRepositoryId("review:s64:d3-c4");
var plugin = new CSharpCodeGraphPlugin();
var run = new CodeIndexRunId("run:predecessor-store");
var started = new DateTimeOffset(2026,11,28,12,0,0,TimeSpan.Zero);
var sources = Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories)
    .Where(p=>p.EndsWith(".cs")||p.EndsWith(".csproj"))
    .OrderBy(p=>Path.GetRelativePath(root,p),StringComparer.Ordinal)
    .Select(path=>{var b=File.ReadAllBytes(path);return new CodeGraphSource(Path.GetRelativePath(root,path).Replace('\\','/'),"sha256:"+Convert.ToHexStringLower(SHA256.HashData(b)),_=>ValueTask.FromResult<Stream>(new MemoryStream(b,false)));})
    .ToArray();
var context = new CodeGraphPluginContext(repo,null,run,sources);
using (var store = new LatticeCodeGraphStore(db))
{
    await store.UpsertRepositoryAsync(new(repo));
    await store.StoreIndexRunAsync(new(repo,run,started,plugins:[plugin.Id]));
    await using (var sink = new CodeGraphIngestionSink(store))
    await using (var session = await plugin.CreateSessionAsync(context))
    {
        await session.ExtractAsync(sink);
        await sink.CompleteAsync();
    }
    await store.CompleteIndexRunAsync(new(repo,run,started,CodeIndexRunStatus.Completed,started.AddSeconds(1),[plugin.Id]),new(repo,run,[]));
}
Console.WriteLine("store="+db);
Console.WriteLine("sha256="+Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(db))));
