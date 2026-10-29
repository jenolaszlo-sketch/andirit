using Andirit;
using System.Security.Cryptography;
using System.Text.Json;

// Reads an authentic p9 seed and copies its durable database before migration.
// Usage: Qualification <source-root> <p9-seed-directory> <new-output-directory>
if (args.Length != 3) throw new ArgumentException("Expected source-root, p9-seed-directory, new-output-directory.");
var originalSource = Path.GetFullPath(args[0]);
var seed = Path.GetFullPath(args[1]);
var output = Path.GetFullPath(args[2]);
Directory.CreateDirectory(output);
var source = Path.Combine(output, "source-copy");
foreach (var file in Directory.GetFiles(originalSource, "*", SearchOption.AllDirectories))
{
    var relative = Path.GetRelativePath(originalSource, file);
    if (relative.Split(Path.DirectorySeparatorChar).Any(p => p is ".git" or "bin" or "obj" or "node_modules")) continue;
    var destination = Path.Combine(source, relative);
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    File.Copy(file, destination, overwrite: false);
}
var old = CodePublicationSnapshot.FromJson(await File.ReadAllTextAsync(Path.Combine(seed, "p9.json")));
if (!old.Units.Any(u => u.Origin.PluginVersion.EndsWith(".p9", StringComparison.Ordinal))) throw new InvalidOperationException("Seed is not authentic p9.");
var database = Path.Combine(output, "rehearsal.db");
File.Copy(Path.Combine(seed, "p9.db"), database, overwrite: false);
var repository = old.Repository.Id;
string[] results;
CodePublicationSnapshot migrated;
using (var store = new LatticeCodeGraphStore(database))
{
    var before = await CodePublicationSnapshot.ExportAsync(store, repository);
    if (before.IntegrityHash != old.IntegrityHash) throw new InvalidOperationException("Durable p9 seed differs from its snapshot.");
    var indexer = new CodeIndexingService(new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
        new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]), store);
    await indexer.IndexAsync(new CodeRepositoryDescriptor(repository, source), new CodeIndexRunId("run:p10-migration"));
    migrated = await CodePublicationSnapshot.ExportAsync(store, repository);
    Preserve(old.Units.SelectMany(u => u.Nodes), migrated.Units.SelectMany(u => u.Nodes), n => n.Id.Value, "nodes");
    Preserve(old.Units.SelectMany(u => u.Declarations), migrated.Units.SelectMany(u => u.Declarations), d => d.Id.Value, "declarations");
    Preserve(old.Units.SelectMany(u => u.Edges), migrated.Units.SelectMany(u => u.Edges), e => e.Id.Value, "edges");
    if (migrated.Units.Any(u => u.Origin.PluginVersion.EndsWith(".p9", StringComparison.Ordinal))) throw new InvalidOperationException("Stale p9 unit survived.");
    results = await Query(store);
    await File.WriteAllTextAsync(Path.Combine(output, "p10.json"), migrated.ToJson());
    await File.WriteAllTextAsync(Path.Combine(output, "queries.json"), JsonSerializer.Serialize(results));
    await indexer.IndexAsync(new CodeRepositoryDescriptor(repository, source), new CodeIndexRunId("run:p10-repeat"));
    if (!Enumerable.SequenceEqual(results, await Query(store), StringComparer.Ordinal)) throw new InvalidOperationException("Unchanged indexing changed queries.");
    var memory = new InMemoryCodeGraphStore();
    await migrated.ImportAsync(memory);
    if (!Enumerable.SequenceEqual(results, await Query(memory), StringComparer.Ordinal)) throw new InvalidOperationException("Stores disagree.");
}
using (var reopened = new LatticeCodeGraphStore(database))
{
    if (!Enumerable.SequenceEqual(results, await Query(reopened), StringComparer.Ordinal)) throw new InvalidOperationException("Reopen changed queries.");
    await File.WriteAllTextAsync(Path.Combine(source, "failure-probe.ts"), "export const triggerReindex = 1;\n");
    foreach (var cancelled in new[] { false, true })
    {
        var indexer = new CodeIndexingService(new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new RejectingPlugin(cancelled), new TreeSitterJavaScriptPlugin()]), reopened);
        var refused = false;
        try { await indexer.IndexAsync(new CodeRepositoryDescriptor(repository, source), new CodeIndexRunId(cancelled ? "run:cancelled" : "run:failed")); }
        catch (Exception e) when (e is IOException or OperationCanceledException) { refused = true; }
        if (!refused || !Enumerable.SequenceEqual(results, await Query(reopened), StringComparer.Ordinal)) throw new InvalidOperationException("Failed/cancelled migration follow-up changed the last publication.");
    }
    var removed = migrated.Units.First(u => u.Origin.SourcePath is not null && u.Edges.Any(e => e.Kind.Value == "imports-name")).Origin.SourcePath!;
    File.Delete(Path.Combine(source, removed));
    await new CodeIndexingService(new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
        new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]), reopened)
        .IndexAsync(new CodeRepositoryDescriptor(repository, source), new CodeIndexRunId("run:post-migration-delete"));
    if ((await reopened.GetPublishedUnitsAsync(repository)).Any(u => u.Origin.SourcePath == removed)) throw new InvalidOperationException("Deleted source unit survived.");
    results = await Query(reopened);
    Console.WriteLine($"Post-migration failed/cancelled indexing retained publication; deletion removed owned unit {removed}.");
}
using (var reopened = new LatticeCodeGraphStore(database))
    if (!Enumerable.SequenceEqual(results, await Query(reopened), StringComparer.Ordinal)) throw new InvalidOperationException("Lifecycle reopen changed queries.");
Console.WriteLine($"Migration, repeat, snapshot replay, both-store equivalence and durable reopen passed for {results.Length} interfaces.");
Console.WriteLine($"p9 snapshot {old.IntegrityHash}; p10 snapshot {migrated.IntegrityHash}");
Console.WriteLine($"p9 database SHA256: {Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(seed, "p9.db"))))}");

static void Preserve<T>(IEnumerable<T> oldFacts, IEnumerable<T> newFacts, Func<T, string> id, string kind)
{
    var before = oldFacts.DistinctBy(id).ToArray();
    var after = newFacts.GroupBy(id).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    foreach (var fact in before)
        if (!after.TryGetValue(id(fact), out var value) || JsonSerializer.Serialize(fact) != JsonSerializer.Serialize(value))
            throw new InvalidOperationException($"Pre-existing {kind} changed: {id(fact)}");
    Console.WriteLine($"Preserved {before.Length} distinct {kind} byte-for-byte as serialized facts; new total {after.Count}.");
}

async Task<string[]> Query(ICodeGraphStore store)
{
    var units = await store.GetPublishedUnitsAsync(repository);
    var targets = units.SelectMany(u => u.Nodes).DistinctBy(n => n.Id).Where(n => n.Kind == CodeNodeKinds.Interface).OrderBy(n => n.Id.Value, StringComparer.Ordinal);
    var query = (await new CodeGraphQueryService(store).OpenLatestPublicationAsync(repository))!;
    var responses = new List<string>();
    foreach (var target in targets)
    {
        var result = await query.GetInterfaceImpactV1Async(target.Id);
        responses.Add(result.ToJson());
        Console.WriteLine($"{target.QualifiedName}: {result.ConfirmedImplementations.Count} confirmed; {result.PossibleConsumers.Count} possible modules; {string.Join(',', result.Unknowns.Select(u => u.Reason))}; {result.Qualification}; {result.Truncation.State}");
    }
    return responses.ToArray();
}

sealed class RejectingPlugin(bool cancel) : ICodeGraphPlugin
{
    private readonly TreeSitterTypeScriptPlugin _inner = new();
    public CodePluginId Id => _inner.Id;
    public string Version => _inner.Version;
    public string Language => _inner.Language;
    public IReadOnlyCollection<string> FileExtensions => _inner.FileExtensions;
    public CodeGraphCapabilities Capabilities => _inner.Capabilities;
    public bool CanHandle(string path) => _inner.CanHandle(path);
    public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(CodeGraphPluginContext context, CancellationToken cancellationToken = default) =>
        cancel ? throw new OperationCanceledException("Injected cancellation during migration follow-up.") : throw new IOException("Injected failed migration follow-up.");
}
