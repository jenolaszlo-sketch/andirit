using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit.CSharp.Tests;

/// <summary>
/// Owner-authorized successor identity transition tests: explicit successor
/// domains, predecessor refusal, content-bound reference identity, deterministic
/// framework closure, cross-path determinism and persisted-generation refusal
/// with reconstruction by re-ingestion into a new store.
/// </summary>
public sealed class OccurrenceIdentityTransitionTests
{
    private static readonly CodeRepositoryId Repository = new("repo:occ-transition");
    private static readonly CodePluginId Plugin = new("plugin:occ-transition");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SuccessorDomains_AreExplicitAndPredecessorsAreNotCanonical()
    {
        var context = "compctx4:sha256:" + new string('a', 64);
        var target = CodeOccurrenceIdentity.ComputeTargetId(
            context, "N.C|0.0.0.0||", "N.C.dll", "id", "decl", "shape");
        Assert.StartsWith("asmctx4:sha256:", target.Value, StringComparison.Ordinal);
        Assert.Equal("usage5", CodeOccurrenceIdentity.UsageDomain);
        Assert.Equal(2, CodeOccurrenceIdentity.Generation);
        Assert.Equal(2, CodeOccurrenceIdentity.AuthorityVersion);
        Assert.Equal(5, CodeOccurrenceIdentity.CurrentSemanticsVersion);
        Assert.Contains("usage5", CodeOccurrenceIdentity.SemanticsVersion);

        Assert.False(CodeOccurrenceIdentity.IsCanonicalTarget(
            "asmctx3:sha256:" + new string('a', 64)));
        Assert.True(CodeOccurrenceIdentity.IsPredecessorTarget(
            "asmctx3:sha256:" + new string('a', 64)));
        Assert.Throws<ArgumentException>(() =>
            new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('a', 64)));
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            "compctx1:sha256:" + new string('a', 64), "m", "n", "i", "d", "s"));
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            "compctx3:sha256:" + new string('a', 64), "m", "n", "i", "d", "s"));
    }

    [Fact]
    public void SuccessorContexts_RefusePredecessorReferenceItems()
    {
        static CompilationProvenanceDescriptor Descriptor(string version, string reference) =>
            new(version, "repo:test", "src/App.csproj", "App", "App|1.0.0.0||", "App.dll",
                "sha256:" + new string('a', 64),
                [new CompilationSourceIdentity("src/App.cs",
                    "sha256:" + new string('0', 64), "Latest", string.Empty)],
                [reference], string.Empty);
        Assert.Throws<InvalidDataException>(() => Descriptor("compctx3",
            "file|/ref/System.Runtime.dll|sha256:" + new string('1', 64)).ComputeDigest());
        Assert.Throws<InvalidDataException>(() => Descriptor("compctx4",
            "file|/ref/System.Runtime.dll|sha256:" + new string('1', 64)).ComputeDigest());
        Assert.StartsWith("compctx3:sha256:", Descriptor("compctx3",
            "refctx1|System.Runtime|10.0.0.0||b03f5f7f11d50a3a|sha256:" + new string('1', 64)).ComputeDigest());
        Assert.StartsWith("compctx4:sha256:", Descriptor("compctx4",
            "refctx1|System.Runtime|10.0.0.0||b03f5f7f11d50a3a|sha256:" + new string('1', 64)).ComputeDigest());
    }

    [Fact]
    public async Task FrameworkClosure_ExcludesHostAndTestAssembliesAndAbsolutePaths()
    {
        var context = Context("App/App.csproj", "App/App.cs",
            "namespace App; public static class Entry { public static int Run() => 1; }");
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.True(analysis.Universe.IsComplete, string.Join(";", analysis.Universe.Refusals));
        var app = analysis.Projects.Single().Verified!;
        var references = app.Descriptor.References;

        Assert.NotEmpty(references);
        Assert.All(references, reference =>
        {
            Assert.StartsWith("refctx1|", reference, StringComparison.Ordinal);
            Assert.DoesNotContain("C:", reference, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\\", reference, StringComparison.Ordinal);
            Assert.DoesNotContain("testhost", reference, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("xunit", reference, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Andirit", reference, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.TestPlatform", reference, StringComparison.OrdinalIgnoreCase);
        });

        // The closure is exactly the managed assemblies of the running
        // Microsoft.NETCore.App shared framework directory, never the ambient
        // process TPA composition.
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var managedNames = Directory.EnumerateFiles(runtimeDirectory, "*.dll")
            .Where(IsManagedAssembly)
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var referenceNames = references
            .Select(reference => reference.Split('|')[1])
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(managedNames, referenceNames);

        // Repeated analysis is deterministic and host-composition independent.
        var again = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.Equal(app.Descriptor.DescriptorDigest,
            again.Projects.Single().Verified!.Descriptor.DescriptorDigest);
        var againReferences = again.Projects.Single().Verified!.Descriptor.References;
        Assert.Equal(references.Length, againReferences.Length);
        for (var index = 0; index < references.Length; index++)
            Assert.True(string.Equals(references[index], againReferences[index], StringComparison.Ordinal),
                "reference mismatch at " + index + ": '" + references[index] + "' vs '" + againReferences[index] + "'");
        Assert.Equal(references.ToArray(), againReferences.ToArray());
    }

    [Fact]
    public async Task SameCorpusAtDifferentPhysicalPaths_ProducesIdenticalTargetAndFacts()
    {
        var corpus = Path.Combine(RepositoryRoot(),
            "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence/corpus/c4-contributors");
        var firstRoot = CopyCorpus(corpus);
        var secondRoot = CopyCorpus(corpus);
        try
        {
            var first = await PublishAsync("run:path-a", firstRoot);
            var second = await PublishAsync("run:path-b", secondRoot);

            Assert.Equal(first.Target.Value, second.Target.Value);
            Assert.Equal(first.DependencyDescriptor, second.DependencyDescriptor);
            Assert.Equal(first.DependencyContext, second.DependencyContext);
            Assert.Equal(first.FactIds, second.FactIds);
            Assert.Equal(first.Units, second.Units);
            Assert.Equal(51, first.FactIds.Length);
            Assert.All(first.FactIds, id => Assert.Equal(64, id.Length));
        }
        finally
        {
            Delete(firstRoot);
            Delete(secondRoot);
        }
    }

    [Fact]
    public async Task DifferentSemanticReferenceContent_StillChangesIdentity()
    {
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var first = VerifiedMetadataReference.Capture(
            Path.Combine(runtimeDirectory, "System.Runtime.dll"));
        var second = VerifiedMetadataReference.Capture(
            Path.Combine(runtimeDirectory, "System.Private.CoreLib.dll"));
        Assert.NotEqual(first.Identity, second.Identity);

        var firstUnit = await VerifiedUnitWithReferenceAsync(first);
        var secondUnit = await VerifiedUnitWithReferenceAsync(second);
        Assert.NotEqual(firstUnit.Descriptor.DescriptorDigest,
            secondUnit.Descriptor.DescriptorDigest);
        Assert.NotEqual(firstUnit.MapR2FaithfulDefiningContext().DescriptorDigest,
            secondUnit.MapR2FaithfulDefiningContext().DescriptorDigest);
    }

    [Fact]
    public async Task PredecessorGenerationStore_IsRefusedAndReingestionBuildsSuccessorStore()
    {
        var sourceRoot = CopyCorpus(Path.Combine(RepositoryRoot(),
            "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence/corpus/c4-contributors"));
        var predecessorDatabase = Path.Combine(Path.GetTempPath(),
            $"andirit-occ-predecessor-{Guid.NewGuid():N}.ltdb");
        var successorDatabase = Path.Combine(Path.GetTempPath(),
            $"andirit-occ-successor-{Guid.NewGuid():N}.ltdb");
        try
        {
            var published = await PublishAsync("run:predecessor", sourceRoot, predecessorDatabase);
            var before = Snapshot(predecessorDatabase);

            // Simulate a genuine predecessor-generation durable record (schema 1,
            // namespace /1, no generation marker). The file bytes are preserved.
            TamperPredecessorMeta(predecessorDatabase);
            var tampered = Snapshot(predecessorDatabase);
            var tamperedRows = LogicalSnapshot(predecessorDatabase);
            var error = Assert.Throws<CodeOccurrenceSchemaException>(() =>
                new LatticeCodeGraphStore(predecessorDatabase));
            Assert.Contains("re-ingestion", error.Message, StringComparison.OrdinalIgnoreCase);
            // Refusal preserves the logical predecessor records and never
            // silently migrates them in place.
            Assert.Equal(tamperedRows, LogicalSnapshot(predecessorDatabase));
            Assert.Contains("\"IdentityGeneration\":1", LogicalSnapshot(predecessorDatabase),
                StringComparison.Ordinal);
            Assert.NotEqual(before, tampered);

            // Reconstruction is re-ingestion into a new store, never in-place
            // migration. The new store carries the successor generation.
            var rebuilt = await PublishAsync("run:successor", sourceRoot, successorDatabase);
            Assert.Equal(published.Target.Value, rebuilt.Target.Value);
            Assert.Equal(published.FactIds, rebuilt.FactIds);
            Assert.StartsWith("asmctx4:sha256:", rebuilt.Target.Value, StringComparison.Ordinal);

            using var reopened = new LatticeCodeGraphStore(successorDatabase);
            var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(ContextFromRoot(sourceRoot));
            var caller = analysis.Projects.FirstOrDefault(project =>
                project.Project.Path == "Caller/Caller.csproj")?.Verified
                ?? analysis.Projects.Single(project =>
                    project.Project.Path == "Caller2/Caller2.csproj").Verified!;
            var result = await reopened.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                new CodeOccurrenceQuery(Repository, new CodeOccurrenceTargetId(rebuilt.Target.Value),
                    maxResults: 1000),
                analysis.Universe, caller.Compilation));
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Equal(51, result.Occurrences.Count);
            Assert.All(result.Occurrences,
                hit => Assert.StartsWith("asmctx4:sha256:", hit.TargetId.Value, StringComparison.Ordinal));
        }
        finally
        {
            Delete(sourceRoot);
            Delete(predecessorDatabase);
            Delete(successorDatabase);
        }
    }

    // ---- helpers ----

    private static async Task<Published> PublishAsync(
        string run, string corpusRoot, string? database = null)
    {
        var context = ContextFromRoot(corpusRoot, run);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.True(analysis.Universe.IsComplete, string.Join(";", analysis.Universe.Refusals));
        var dependency = analysis.Projects.Single(project =>
            project.Project.Path == "Dependency/Dependency.csproj").Verified!;
        var plugin = new CSharpCodeGraphPlugin();
        var facts = new List<CodeOccurrenceFact>();
        if (database is null)
        {
            var store = new InMemoryCodeGraphStore();
            await PublishToStoreAsync(store, plugin, context, facts);
        }
        else
        {
            using var store = new LatticeCodeGraphStore(database);
            await PublishToStoreAsync(store, plugin, context, facts);
        }
        var target = facts.Select(fact => fact.TargetId.Value).Distinct(StringComparer.Ordinal).Single();
        return new Published(
            new CodeOccurrenceTargetId(target),
            dependency.Descriptor.DescriptorDigest,
            dependency.MapR2FaithfulDefiningContext().DescriptorDigest,
            facts.Select(fact => fact.Id.Value).Order(StringComparer.Ordinal).ToArray(),
            facts.Select(fact => fact.SourceUnitId).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToArray());
    }

    private static async Task PublishToStoreAsync(
        ICodeGraphStore store, CSharpCodeGraphPlugin plugin,
        CodeGraphPluginContext context, List<CodeOccurrenceFact> facts)
    {
        await store.UpsertRepositoryAsync(new(Repository));
        await store.StoreIndexRunAsync(new(Repository, context.IndexRunId, Started,
            plugins: [plugin.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }
        await store.CompleteIndexRunAsync(
            new(Repository, context.IndexRunId, Started, CodeIndexRunStatus.Completed,
                Started.AddSeconds(1), [plugin.Id]),
            new(Repository, context.IndexRunId, []));
        facts.AddRange((await store.GetPublishedUnitsAsync(Repository))
            .SelectMany(unit => unit.Occurrences));
    }

    private static CodeGraphPluginContext ContextFromRoot(string root, string run = "run:c4")
    {
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) ||
                           path.EndsWith(".csproj", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToDictionary(path => Path.GetRelativePath(root, path).Replace('\\', '/'),
                File.ReadAllText, StringComparer.Ordinal);
        var sources = files.Select(pair =>
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Value);
            return new CodeGraphSource(pair.Key,
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }).ToArray();
        return new(Repository, null, new CodeIndexRunId(run), sources);
    }

    private static CodeGraphPluginContext Context(
        string project, string sourcePath, string source)
    {
        var projectText =
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework>" +
            "<EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>" +
            "<ItemGroup><Compile Include=\"" + Path.GetFileName(sourcePath) + "\" /></ItemGroup></Project>";
        var sources = new[]
        {
            new CodeGraphSource(project,
                "sha256:" + Hash(projectText), _ => ValueTask.FromResult<Stream>(
                    new MemoryStream(Encoding.UTF8.GetBytes(projectText), writable: false))),
            new CodeGraphSource(sourcePath,
                "sha256:" + Hash(source), _ => ValueTask.FromResult<Stream>(
                    new MemoryStream(Encoding.UTF8.GetBytes(source), writable: false))),
        };
        return new(Repository, null, new CodeIndexRunId("run:single"), sources);
    }

    private static async Task<VerifiedCSharpCompilation> VerifiedUnitWithReferenceAsync(
        VerifiedMetadataReference reference)
    {
        const string path = "App/App.cs";
        const string text = "namespace App; public static class Entry { public static System.Type T = typeof(object); }";
        var bytes = Encoding.UTF8.GetBytes(text);
        var source = await VerifiedCSharpSource.ReadAsync(new CodeGraphSource(
            path, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))));
        var tree = CSharpSyntaxTree.ParseText(source.Text,
            new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose),
            path);
        var compilation = CSharpCompilation.Create("App", [tree], [reference.Reference],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                deterministic: true, concurrentBuild: false));
        return VerifiedCSharpCompilation.Create(Repository.Value, "App/App.csproj",
            compilation, [source], [reference], []);
    }

    private static void TamperPredecessorMeta(string database)
    {
        using var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginWriteTransaction();
        var key = Repository.Value;
        JsonObject row;
        using (var query = db.Prepare(
                   "MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k RETURN s.payload"))
        {
            query.Bind("k", LatticeValue.From(key));
            using var result = query.Execute(txn);
            row = JsonNode.Parse(Convert.FromBase64String(
                result.ReadAll()[0].GetValue(0).AsString()))!.AsObject();
        }
        row["SchemaVersion"] = 1;
        row["Namespace"] = "andirit-occurrence-facts/1";
        row["IdentityGeneration"] = 1;
        using (var query = db.Prepare(
                   "MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k SET s.payload = $p"))
        {
            query.Bind("k", LatticeValue.From(key));
            query.Bind("p", LatticeValue.From(Convert.ToBase64String(
                Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var result = query.Execute(txn);
            result.ReadAll();
        }
        txn.Commit();
    }

    private static string Snapshot(string database)
    {
        using var stream = File.OpenRead(database);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private static string LogicalSnapshot(string database)
    {
        using var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginReadTransaction();
        var rows = new List<string>();
        foreach (var label in new[] { "AndiritOccurrenceMeta", "AndiritOccurrenceUnit", "AndiritOccurrencePage" })
        {
            using var query = db.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
            using var result = query.Execute(txn);
            foreach (var row in result.ReadAll())
            {
                var key = row.GetValue(0).AsString();
                var payload = Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(1).AsString()));
                rows.Add(label + "|" + key + "|" + payload);
            }
        }
        txn.Commit();
        return string.Join("\n", rows.Order(StringComparer.Ordinal));
    }

    private static string CopyCorpus(string source)
    {
        var target = Path.Combine(Path.GetTempPath(), "andirit-c4-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(target);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        return target;
    }

    private static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            return;
        }
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
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

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            return pe.HasMetadata && pe.GetMetadataReader().IsAssembly;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private sealed record Published(
        CodeOccurrenceTargetId Target, string DependencyDescriptor,
        string DependencyContext, string[] FactIds, string[] Units);
}
