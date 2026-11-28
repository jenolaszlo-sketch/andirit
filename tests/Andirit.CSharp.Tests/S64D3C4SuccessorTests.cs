using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.LatticeDb;
using Microsoft.CodeAnalysis;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S6.4 C4 occurrence ledger under the successor identity generation. The
/// expected ledger is produced by the independent C4 successor oracle
/// (<c>tools/c4-successor-oracle</c>), never by the production identity helpers.
/// This test verifies the full production chain and independently recomputes
/// every ledger fact from the live analysis.
/// </summary>
public sealed class S64D3C4SuccessorTests
{
    private static readonly CodeRepositoryId Repository = new("review:s64:d3-c4");
    private static readonly CodePluginId Plugin = new("penghou.hetu.csharp");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("baseline", "c4-occurrences-target-framework.json")]
    [InlineData("without-caller", "c4-oracle-variants/c4-occurrences-without-caller.json")]
    [InlineData("without-caller2", "c4-oracle-variants/c4-occurrences-without-caller2.json")]
    [InlineData("caller2-changed", "c4-oracle-variants/c4-occurrences-caller2-changed.json")]
    public async Task C4SuccessorLedger_MatchesProductionAndIndependentOracle(
        string variant, string ledgerName)
    {
        var root = RepositoryRoot();
        var corpus = Path.Combine(root,
            "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence/corpus/c4-contributors");
        var ledgerPath = Path.Combine(root,
            "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R2-evidence/expected", ledgerName);
        var expected = JsonSerializer.Deserialize<Ledger>(
            await File.ReadAllTextAsync(ledgerPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var files = CorpusFiles(corpus, variant);
        var context = Context(files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.True(analysis.Universe.IsComplete, string.Join(";", analysis.Universe.Refusals));
        var dependency = analysis.Projects.Single(project =>
            project.Project.Path == "Dependency/Dependency.csproj").Verified!;
        var mark = (IMethodSymbol)dependency.Compilation.GetTypeByMetadataName("QualifierDependency.Api")!
            .GetMembers("Mark").Single();

        // The ledger is an independent-oracle artifact, not candidate output.
        // Exact digests are bound to the reference file contents of the producing
        // runtime (S2.0 §5); a different runtime/OS legitimately changes them. The
        // frozen ledger is the Windows x64 10.0.12 anchor. On any other host the
        // independent oracle recomputes this host's expected values and the
        // semantic relations stay frozen.
        var currentContext = SuccessorIdentityOracle.DescribeDefiningContext(
            Repository.Value, dependency);
        var currentTarget = SuccessorIdentityOracle.TargetId(Repository.Value, dependency, mark,
            SuccessorIdentityOracle.IdentifierDigest,
            SuccessorIdentityOracle.DeclarationDigest,
            SuccessorIdentityOracle.ShapeDigest);
        var currentRefHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join("\n", dependency.Descriptor.References))));
        var exactHost = string.Equals(currentContext, expected.Compctx4, StringComparison.Ordinal) &&
            string.Equals(currentRefHash, expected.RefHash, StringComparison.Ordinal);
        if (exactHost)
        {
            Assert.Equal(expected.Target, currentTarget);
            Assert.Equal(expected.Compctx3, dependency.Descriptor.DescriptorDigest);
            Assert.Equal(expected.Metadata,
                SuccessorIdentityOracle.Metadata(dependency.Compilation.Assembly.Identity));
            Assert.Equal(expected.Module,
                dependency.Compilation.Assembly.Modules.First().Name);
        }
        else
        {
            Console.WriteLine("C4 successor ledger host differs from the frozen Windows anchor: " +
                "ledger=" + expected.Target + " current=" + currentTarget +
                " context=" + currentContext);
        }
        Assert.Equal(CodeOccurrenceIdentity.TargetDomain, currentTarget.Split(':')[0]);

        // Independently recompute every expected fact from the live analysis.
        var oracleFacts = OracleFacts(analysis, currentTarget);
        Assert.Equal(expected.Cardinality, oracleFacts.Count);
        Assert.Equal(expected.Facts.Length, oracleFacts.Count);
        Assert.Equal(
            expected.Facts.Select(item => item.Unit).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
            oracleFacts.Select(item => item.Unit).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
        Assert.Equal(expected.Facts.Select(item => item.Unit).Distinct(StringComparer.Ordinal).Count(),
            oracleFacts.Select(item => item.Unit).Distinct(StringComparer.Ordinal).Count());
        if (exactHost)
        {
            Assert.Equal(
                expected.Facts.OrderBy(item => item.Id, StringComparer.Ordinal).Select(item => item.Id),
                oracleFacts.OrderBy(item => item.Id, StringComparer.Ordinal).Select(item => item.Id));
        }

        // The full production chain must publish the frozen ledger (or, on a
        // different runtime, exactly the independent oracle for this host).
        var database = Path.Combine(Path.GetTempPath(),
            $"andirit-c4-successor-{Guid.NewGuid():N}.ltdb");
        try
        {
            using (var store = new LatticeCodeGraphStore(database))
                await PublishAsync(store, context);
            using var reopened = new LatticeCodeGraphStore(database);
            var caller = analysis.Projects.FirstOrDefault(project =>
                project.Project.Path == "Caller/Caller.csproj")?.Verified
                ?? analysis.Projects.Single(project =>
                    project.Project.Path == "Caller2/Caller2.csproj").Verified!;
            var query = VerifiedCSharpOccurrenceAuthority.Bind(
                new CodeOccurrenceQuery(Repository, new CodeOccurrenceTargetId(currentTarget),
                    maxResults: 1000),
                analysis.Universe, caller.Compilation);
            var result = await reopened.FindOccurrencesAsync(query);
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Equal(expected.Cardinality, result.Occurrences.Count);
            Assert.Equal(
                (exactHost ? expected.Facts.Select(item => item.Id) :
                    oracleFacts.Select(item => item.Id))
                    .Order(StringComparer.Ordinal),
                result.Occurrences.OrderBy(item => item.Id.Value, StringComparer.Ordinal)
                    .Select(item => item.Id.Value));
            Assert.Equal(
                expected.Facts.Select(item => item.Unit).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal),
                result.Occurrences.Select(item => item.SourceUnitId).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal));
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
                if (File.Exists(database + suffix)) File.Delete(database + suffix);
        }
    }

    private static IReadOnlyList<OracleFact> OracleFacts(
        VerifiedCSharpAnalysis analysis, string expectedTarget)
    {
        var facts = new List<OracleFact>();
        foreach (var caller in analysis.Universe.Entries)
        {
            foreach (var tree in caller.Compilation.SyntaxTrees)
            {
                var path = tree.FilePath.Replace('\\', '/');
                var source = caller.Sources.Single(item => item.Path == path);
                var model = caller.Compilation.GetSemanticModel(tree);
                foreach (var invocation in tree.GetRoot().DescendantNodes()
                             .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>())
                {
                    if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
                        continue;
                    if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ReducedExtension
                        or MethodKind.ExplicitInterfaceImplementation))
                        continue;
                    var defining = analysis.Universe.Entries.SingleOrDefault(entry =>
                        ReferenceEquals(entry.Compilation.Assembly, method.ContainingAssembly))
                        ?? DefiningViaProjectReference(analysis, caller, method);
                    if (defining is null)
                        continue;
                    var token = invocation.Expression switch
                    {
                        Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax member => member.Name.Identifier,
                        Microsoft.CodeAnalysis.CSharp.Syntax.SimpleNameSyntax simple => simple.Identifier,
                        _ => invocation.Expression.GetLastToken(),
                    };
                    var line = tree.GetLineSpan(token.Span);
                    var span = new CodeLocation(path,
                        line.StartLinePosition.Line + 1, line.StartLinePosition.Character + 1,
                        line.EndLinePosition.Line + 1, line.EndLinePosition.Character + 1);
                    var target = SuccessorIdentityOracle.TargetId(Repository.Value, defining, method,
                        SuccessorIdentityOracle.IdentifierDigest,
                        SuccessorIdentityOracle.DeclarationDigest,
                        SuccessorIdentityOracle.ShapeDigest);
                    Assert.Equal(expectedTarget, target);
                    var id = SuccessorIdentityOracle.UsageId(Repository.Value, Plugin.Value,
                        caller.Descriptor.ProjectUnitId, path, source.ContentHash, span,
                        SuccessorIdentityOracle.TargetDocumentId(method), target);
                    facts.Add(new OracleFact(id, caller.Descriptor.ProjectUnitId));
                }
            }
        }
        return facts;
    }

    private static VerifiedCSharpCompilation? DefiningViaProjectReference(
        VerifiedCSharpAnalysis analysis, VerifiedCSharpCompilation caller, IMethodSymbol method)
    {
        if (method.ContainingAssembly is null)
            return null;
        var metadata = SuccessorIdentityOracle.Metadata(method.ContainingAssembly.Identity);
        foreach (var reference in caller.Compilation.References.OfType<CompilationReference>())
        {
            if (SuccessorIdentityOracle.Metadata(reference.Compilation.Assembly.Identity) != metadata)
                continue;
            return analysis.Universe.Entries.SingleOrDefault(entry =>
                ReferenceEquals(entry.Compilation, reference.Compilation));
        }
        return null;
    }

    private static Dictionary<string, string> CorpusFiles(string corpus, string variant)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(corpus, "*", SearchOption.AllDirectories)
                     .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) ||
                                    path.EndsWith(".csproj", StringComparison.Ordinal))
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var logical = Path.GetRelativePath(corpus, path).Replace('\\', '/');
            var text = File.ReadAllText(path);
            if (variant == "without-caller2" && logical.StartsWith("Caller2/", StringComparison.Ordinal))
                continue;
            if (variant == "without-caller" && logical.StartsWith("Caller/", StringComparison.Ordinal))
                continue;
            if (variant == "caller2-changed" && logical == "Caller2/Work.cs")
                text = "// caller2 variant: same target, changed caller source\n" + text;
            files[logical] = text;
        }
        return files;
    }

    private static CodeGraphPluginContext Context(IReadOnlyDictionary<string, string> files)
    {
        var sources = files.Select(pair =>
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Value);
            return new CodeGraphSource(pair.Key,
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }).ToArray();
        return new(Repository, null, new CodeIndexRunId("run:c4-successor"), sources);
    }

    private static async Task PublishAsync(LatticeCodeGraphStore store, CodeGraphPluginContext context)
    {
        var plugin = new CSharpCodeGraphPlugin();
        await store.UpsertRepositoryAsync(new(Repository));
        await store.StoreIndexRunAsync(new(Repository, context.IndexRunId, Started, plugins: [plugin.Id]));
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

    private sealed record OracleFact(string Id, string Unit);

    private sealed record Ledger(
        string Target, string Compctx3, string Compctx4, string Options, string Unit,
        string Metadata, string Module, string SourceHash, string RefHash, int ReferenceCount,
        int Cardinality, IndependentFact[] Facts);

    private sealed record IndependentFact(
        string Id, string Unit, string Path, string SourceHash,
        string StartLine, string StartColumn, string EndLine, string EndColumn);
}



