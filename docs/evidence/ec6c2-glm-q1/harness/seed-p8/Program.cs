using Penghou.Hetu;

// GLM-Q1 qualification harness: authentic p8 seed.
// Runs the pre-publication EC-6C.1 implementation (worktree at 8c1d5e3,
// profile p8) to persist a LatticeDb store over the recorded migration fixture.
var root = args.Length > 0 ? args[0] : throw new ArgumentException("Usage: seed <workspace-root>");
Directory.CreateDirectory(root);

var typescriptMain = "import { type Remote } from \"./dep\";\nexport const value = 1;\nexport function run() { return value; }\nexport { value as current };\nexport { remote as forwarded } from \"./dep\";\n";
var typescriptDep = "export interface Remote {}\nexport const remote = 2;";
var javascriptMain = "import { item } from \"./dep\";\nexport const value = 1;\nexport default function run() { return item; }\nexport { value as current };\nexport { remote as forwarded } from \"./dep\";\n";
var javascriptDep = "export const item = 1;\nexport const remote = 2;";

var tsDirectory = Path.Combine(root, "ts", "src");
var jsDirectory = Path.Combine(root, "js", "src");
Directory.CreateDirectory(tsDirectory);
Directory.CreateDirectory(jsDirectory);
File.WriteAllText(Path.Combine(tsDirectory, "main.ts"), typescriptMain);
File.WriteAllText(Path.Combine(tsDirectory, "dep.ts"), typescriptDep);
File.WriteAllText(Path.Combine(jsDirectory, "main.js"), javascriptMain);
File.WriteAllText(Path.Combine(jsDirectory, "dep.js"), javascriptDep);

var databasePath = Path.Combine(root, "graph.db");
if (File.Exists(databasePath))
    File.Delete(databasePath);

using (var store = new LatticeCodeGraphStore(databasePath))
{
    var typeScriptIndexing = new CodeIndexingService(
        new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
        new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
        store);
    var typeScriptResult = await typeScriptIndexing.IndexAsync(
        new CodeRepositoryDescriptor(new CodeRepositoryId("repo:q1-migration-ts"), Path.Combine(root, "ts")),
        new CodeIndexRunId("run:q1-p8-ts"));
    Console.WriteLine($"ts p8 status: {typeScriptResult.Diagnostics.Status}");
    var javaScriptIndexing = new CodeIndexingService(
        new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
        new CodeGraphPluginRegistry([new TreeSitterJavaScriptPlugin()]),
        store);
    var javaScriptResult = await javaScriptIndexing.IndexAsync(
        new CodeRepositoryDescriptor(new CodeRepositoryId("repo:q1-migration-js"), Path.Combine(root, "js")),
        new CodeIndexRunId("run:q1-p8-js"));
    Console.WriteLine($"js p8 status: {javaScriptResult.Diagnostics.Status}");

    Console.WriteLine($"typescript plugin version (seed): {new TreeSitterTypeScriptPlugin().Version}");
    Console.WriteLine($"javascript plugin version (seed): {new TreeSitterJavaScriptPlugin().Version}");

    var tsUnits = await store.GetPublishedUnitsAsync(new CodeRepositoryId("repo:q1-migration-ts"));
    var jsUnits = await store.GetPublishedUnitsAsync(new CodeRepositoryId("repo:q1-migration-js"));
    Console.WriteLine($"ts p8 units: {tsUnits.Count} nodes={tsUnits.Sum(u => u.Nodes.Count)} decls={tsUnits.Sum(u => u.Declarations.Count)} edges={tsUnits.Sum(u => u.Edges.Count)}");
    Console.WriteLine($"js p8 units: {jsUnits.Count} nodes={jsUnits.Sum(u => u.Nodes.Count)} decls={jsUnits.Sum(u => u.Declarations.Count)} edges={jsUnits.Sum(u => u.Edges.Count)}");
}

Console.WriteLine("p8 seed complete.");
return 0;
