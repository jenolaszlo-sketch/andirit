using Andirit;

// Build against authentic fe1a9e37 p9 projects before running the p10 harness.
if (args.Length != 2) throw new ArgumentException("Expected source-root and new-output-directory.");
var plugin = new TreeSitterTypeScriptPlugin();
if (!plugin.Version.EndsWith(".p9", StringComparison.Ordinal))
    throw new InvalidOperationException("An authentic baseline p9 build is required; do not relabel p10 facts.");
var source = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
var database = Path.Combine(output, "p9.db");
if (File.Exists(database)) throw new InvalidOperationException("Use a new seed output directory.");
using var store = new LatticeCodeGraphStore(database);
var repository = new CodeRepositoryId("repo:ec6d-migration");
await new CodeIndexingService(new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
    new CodeGraphPluginRegistry([plugin, new TreeSitterJavaScriptPlugin()]), store)
    .IndexAsync(new CodeRepositoryDescriptor(repository, source), new CodeIndexRunId("run:p9-seed"));
var snapshot = await CodePublicationSnapshot.ExportAsync(store, repository);
await File.WriteAllTextAsync(Path.Combine(output, "p9.json"), snapshot.ToJson());
Console.WriteLine($"Authentic {plugin.Version} seed: {snapshot.Units.Count} units; integrity {snapshot.IntegrityHash}");
