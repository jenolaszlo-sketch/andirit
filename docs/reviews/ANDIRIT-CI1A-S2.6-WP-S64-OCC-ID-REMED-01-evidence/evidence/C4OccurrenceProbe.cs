using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.LatticeDb;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S6.4 D3 C4 contributor check: the two-unit occurrence ledger is frozen from
/// the accepted authority and compared with production publication output.
/// </summary>
public sealed class S64D3C4Tests
{
    private static readonly CodeRepositoryId Repository = new("review:s64:d3-c4");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task C4OccurrenceLedger_MatchesFrozenOracle()
    {
        var root = RepositoryRoot();
        var corpus = Path.Combine(root,
            "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence/corpus/c4-contributors");
        var expectedPath = Path.Combine(root,
            "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence/expected/c4-occurrences.json");
        var expected = JsonSerializer.Deserialize<Expected>(
            await File.ReadAllTextAsync(expectedPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var database = Path.Combine(Path.GetTempPath(),
            $"andirit-s64-c4-{Guid.NewGuid():N}.ltdb");
        try
        {
            var sources = Directory.EnumerateFiles(corpus, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) ||
                    path.EndsWith(".csproj", StringComparison.Ordinal))
                .OrderBy(path => Path.GetRelativePath(corpus, path), StringComparer.Ordinal)
                .Select(path =>
                {
                    var bytes = File.ReadAllBytes(path);
                    return new CodeGraphSource(
                        Path.GetRelativePath(corpus, path).Replace('\\', '/'),
                        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                        _ => ValueTask.FromResult<Stream>(
                            new MemoryStream(bytes, writable: false)));
                }).ToArray();
            var plugin = new CSharpCodeGraphPlugin();
            var run = new CodeIndexRunId("run:c4-oracle");
            var context = new CodeGraphPluginContext(Repository, null, run, sources);
            using var store = new LatticeCodeGraphStore(database);
            await store.UpsertRepositoryAsync(new(Repository));
            await store.StoreIndexRunAsync(new(Repository, run, Started, plugins: [plugin.Id]));
            await using (var sink = new CodeGraphIngestionSink(store))
            await using (var session = await plugin.CreateSessionAsync(context))
            {
                await session.ExtractAsync(sink);
                await sink.CompleteAsync();
            }
            await store.CompleteIndexRunAsync(
                new(Repository, run, Started, CodeIndexRunStatus.Completed,
                    Started.AddSeconds(1), [plugin.Id]),
                new(Repository, run, []));

            var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
            var caller = analysis.Projects.Single(project =>
                project.Project.Path == "Caller/Caller.csproj").Verified!;
            var publishedFacts = (await store.GetPublishedUnitsAsync(Repository)).SelectMany(unit => unit.Occurrences).ToArray();
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "c4-diag.txt"), "facts=" + publishedFacts.Length + " targets=" + string.Join(",", publishedFacts.GroupBy(fact => fact.TargetId.Value).Select(group => group.Key + ":" + group.Count())) + " expected=" + expected.Target.Value);
            var dep = analysis.Projects.Single(project => project.Project.Path == "Dependency/Dependency.csproj").Verified!;
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "c4-diag.txt"),
                "\nprod=" + dep.Descriptor.DescriptorDigest + "|" + dep.Descriptor.MetadataIdentity +
                "|" + dep.Descriptor.ModuleName + "|" + dep.Descriptor.OptionsDigest +
                "|sources=" + string.Join(";", dep.Descriptor.Sources.Select(s => s.Path + ":" + s.ContentHash)) +
                "|refs=" + string.Join(";", dep.Descriptor.References));
            var query = VerifiedCSharpOccurrenceAuthority.Bind(
                new CodeOccurrenceQuery(
                    Repository, new CodeOccurrenceTargetId(expected.Target.Value),
                    maxResults: 1000),
                analysis.Universe,
                caller.Compilation);
            var result = await store.FindOccurrencesAsync(query);
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Equal(expected.Cardinality, result.Occurrences.Count);
            Assert.Equal(
                expected.Occurrences.Select(item => item.Id)
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                result.Occurrences.Select(item => item.Id.Value)
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray());
            Assert.Equal(
                expected.Target.Contributors.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                result.Occurrences.Select(item => item.SourceUnitId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray());
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal" })
                if (File.Exists(database + suffix)) File.Delete(database + suffix);
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Andirit.slnx")))
            directory = directory.Parent;
        return directory?.FullName ??
            throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record Expected(
        string Variant, string Repository, string Plugin, string PluginVersion,
        Target Target, Occurrence[] Occurrences, int Cardinality,
        int TargetContributorCount);

    private sealed record Target(
        string Value, string Document, string Context, string Metadata, string Module,
        string[] Contributors);

    private sealed record Occurrence(
        string Id, string Unit, string Path, string SourceHash,
        int StartLine, int StartColumn, int EndLine, int EndColumn,
        string TargetDocument, string TargetId, string Category);
}
