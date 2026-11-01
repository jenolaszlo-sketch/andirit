using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit.CSharp.Tests;

public sealed class CSharpProvenanceIntegrationTests
{
    private static readonly (string, string)[] Fixture =
    [
        ("Lib/Lib.csproj", "<Project><PropertyGroup><LangVersion>14</LangVersion><Nullable>enable</Nullable></PropertyGroup></Project>"),
        ("Lib/One.cs", "namespace Fixture; public partial class C { public int A() => 1; }"),
        ("Lib/Two.cs", "namespace Fixture; public partial class C { public int B() => A(); }")
    ];
    private static CodeGraphSource Source(string path, string content) => new(path,
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
        _ => new ValueTask<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(content))));
    private static CodeGraphPluginContext Context(IEnumerable<(string Path, string Text)> sources) =>
        new(new("repo:provenance"), "memory://fixture", new("run:provenance"),
            sources.Select(s => Source(s.Path, s.Text)).ToArray());
    private static ValueTask<VerifiedCSharpAnalysis> Prepare(params (string Path, string Text)[] sources) =>
        CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(sources));

    [Fact]
    public async Task ProductionPreparationRetainsExactTreesOptionsAndMembers()
    {
        var analysis = await Prepare(Fixture);
        Assert.True(analysis.Universe.IsComplete, string.Join(";", analysis.Universe.Refusals));
        var entry = Assert.Single(analysis.Universe.Entries);
        Assert.Equal(2, entry.Compilation.SyntaxTrees.Count());
        Assert.Equal(NullableContextOptions.Enable, entry.Compilation.Options.NullableContextOptions);
        Assert.All(entry.Compilation.SyntaxTrees, tree =>
        {
            var input = analysis.Sources.Single(s => s.Path == tree.FilePath);
            Assert.True(tree.GetText().ContentEquals(input.Text));
            Assert.Contains(entry.Descriptor.Sources, s => s.Path == input.Path && s.ContentHash == input.ContentHash);
        });
        Assert.Same(entry, analysis.Universe.RequireCompilation(entry.Compilation));
        Assert.NotNull(entry.LegacyDescriptor);
        Assert.StartsWith("compctx1:sha256:", entry.LegacyDescriptor!.DescriptorDigest);
        Assert.StartsWith("compctx2:sha256:", entry.Descriptor.DescriptorDigest);
    }

    [Fact]
    public async Task EnumerationOrderAndRunDoNotChangeIdentities()
    {
        var first = await Prepare(Fixture);
        var second = await Prepare(Fixture.Reverse().ToArray());
        Assert.Equal(first.Universe.Identity, second.Universe.Identity);
        Assert.Equal(first.Universe.Scope.Identity, second.Universe.Scope.Identity);
        Assert.Equal(first.Universe.Entries[0].Descriptor.DescriptorDigest, second.Universe.Entries[0].Descriptor.DescriptorDigest);
    }

    [Fact]
    public async Task ConditionalPublicApiChangesPropagateThroughActualLiveProjectReference()
    {
        const string source = """
public static class Lib {
#if FEATURE
public static string Value() => "value";
#else
public static int Value() => 1;
#endif
}
""";
        (string, string)[] Sources(bool feature) =>
        [
            ("Lib/Lib.csproj", $"<Project><PropertyGroup><DefineConstants>{(feature ? "FEATURE" : "")}</DefineConstants></PropertyGroup></Project>"),
            ("Lib/Lib.cs", source),
            ("App/App.csproj", "<Project><ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /></ItemGroup></Project>"),
            ("App/App.cs", "public class App { public object Read() => Lib.Value(); }")
        ];
        var a = await Prepare(Sources(false));
        var b = await Prepare(Sources(true));
        Assert.True(a.Universe.IsComplete);
        Assert.True(b.Universe.IsComplete);
        foreach (var analysis in new[] { a, b })
        {
            var caller = analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
            var dependency = analysis.Projects.Single(p => p.Project.Path == "Lib/Lib.csproj").Verified!;
            Assert.Same(dependency.Compilation, Assert.Single(caller.Compilation.References.OfType<CompilationReference>()).Compilation);
            Assert.Contains("compilation-context|" + dependency.Descriptor.DescriptorDigest, caller.Descriptor.References);
            Assert.Null(caller.LegacyDescriptor);
            Assert.Empty(caller.Compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        }
        var before = a.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        var after = b.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        string Type(VerifiedCSharpCompilation entry) => ((IMethodSymbol)entry.Compilation
            .GetTypeByMetadataName("Lib")!.GetMembers("Value").Single()).ReturnType.ToDisplayString();
        Assert.Equal("int", Type(before));
        Assert.Equal("string", Type(after));
        Assert.NotEqual(before.Descriptor.DescriptorDigest, after.Descriptor.DescriptorDigest);
        Assert.Throws<InvalidDataException>(() => after.ValidateExpectedDescriptor(before.Descriptor));
    }

    [Theory]
    [InlineData("<Project><ItemGroup><ProjectReference Include=\"../Missing/Missing.csproj\"/></ItemGroup></Project>", "MissingOrCyclicProjectDependency")]
    [InlineData("<Project><ItemGroup><Compile Include=\"Absent.cs\"/></ItemGroup></Project>", "MissingDeclaredSource")]
    [InlineData("<Project><PropertyGroup><LangVersion>invalid</LangVersion></PropertyGroup></Project>", "UnsupportedLanguageVersion")]
    [InlineData("<Project><ItemGroup><PackageReference Include=\"Unresolved\" Version=\"1\"/></ItemGroup></Project>", "UnsupportedPackageResolution")]
    [InlineData("<Project><Import Project=\"unknown.props\"/></Project>", "UnsupportedProjectEvaluation")]
    public async Task UnsupportedProjectInputsRefuseStrongProvenance(string project, string reason)
    {
        var analysis = await Prepare(("App/App.csproj", project), ("App/App.cs", "public class App {}"));
        Assert.False(analysis.Universe.IsComplete);
        Assert.Contains(analysis.Universe.Refusals, r => r.Contains(reason, StringComparison.Ordinal));
        Assert.Null(Assert.Single(analysis.Projects).Verified);
    }

    [Fact]
    public async Task CyclicDependenciesRefuseButIndependentCompilationSurvives()
    {
        var analysis = await Prepare(
            ("A/A.csproj", "<Project><ItemGroup><ProjectReference Include=\"../B/B.csproj\"/></ItemGroup></Project>"),
            ("B/B.csproj", "<Project><ItemGroup><ProjectReference Include=\"../A/A.csproj\"/></ItemGroup></Project>"),
            ("Independent/I.csproj", "<Project/>"), ("Independent/I.cs", "class Independent {}"));
        Assert.False(analysis.Universe.IsComplete);
        Assert.Single(analysis.Universe.Entries);
        Assert.Null(analysis.Projects.Single(p => p.Project.Path == "A/A.csproj").Verified);
        Assert.Null(analysis.Projects.Single(p => p.Project.Path == "B/B.csproj").Verified);
        Assert.NotNull(analysis.Projects.Single(p => p.Project.Path == "Independent/I.csproj").Verified);
    }

    [Fact]
    public async Task MissingSolutionProjectCannotBecomeCompleteEmpty()
    {
        var analysis = await Prepare(("Scope.slnx", "<Solution><Project Path=\"Missing/Missing.csproj\"/></Solution>"),
            ("Existing/E.csproj", "<Project/>"));
        Assert.False(analysis.Universe.IsComplete);
        Assert.Contains("MissingDeclaredProject", analysis.Universe.Refusals);
    }

    [Fact]
    public async Task ScopeRejectsWrongMembersDuplicatesAndRepositoryMismatch()
    {
        var analysis = await Prepare(Fixture);
        var entry = Assert.Single(analysis.Universe.Entries);
        var unit = entry.Descriptor.ProjectUnitId;
        var wrong = VerifiedCSharpScope.Declare("repo:provenance", [new(unit, "Lib/Lib.csproj", ["Lib/Wrong.cs", "Lib/Two.cs"])]);
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create("repo:provenance", wrong, [entry]));
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create("repo:other", analysis.Universe.Scope, [entry]));
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpScope.Declare("repo:provenance",
            [new(unit, "A.csproj", []), new(unit, "B.csproj", [])]));
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create("repo:provenance", analysis.Universe.Scope, [entry, entry]));
    }

    [Fact]
    public async Task IncompleteUniverseAndAbsentLiveCompilationRefuse()
    {
        var analysis = await Prepare(Fixture);
        var entry = Assert.Single(analysis.Universe.Entries);
        var incomplete = VerifiedCSharpUniverse.Create("repo:provenance", analysis.Universe.Scope, []);
        Assert.False(incomplete.IsComplete);
        Assert.Single(incomplete.MissingScopeUnits);
        Assert.Throws<InvalidDataException>(() => incomplete.RequireCompilation(entry.Compilation));
        Assert.Throws<InvalidDataException>(() => analysis.Universe.RequireCompilation(entry.Compilation.WithAssemblyName("Unrelated")));
    }

    [Fact]
    public async Task FailedAcquisitionDoesNotYieldAnalysis()
    {
        var context = new CodeGraphPluginContext(new("repo:failure"), null, new("run:failure"),
            [new("Source.cs", "sha256:" + new string('0', 64), _ => throw new IOException("fixture acquisition failure"))]);
        await Assert.ThrowsAsync<IOException>(async () => await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context));
    }

    [Fact]
    public async Task SessionExposesOnlyCompletedSnapshotAndDoesNotPublishOccurrences()
    {
        var plugin = new CSharpCodeGraphPlugin();
        await using var session = await plugin.CreateSessionAsync(Context(Fixture));
        var proof = Assert.IsAssignableFrom<IVerifiedCSharpSession>(session);
        var sink = new RecordingSink();
        await session.ExtractAsync(sink);
        Assert.True(proof.Provenance!.Universe.IsComplete);
        Assert.NotEmpty(sink.Batches);
        Assert.All(sink.Batches, b => { Assert.Empty(b.Occurrences); Assert.Null(b.OccurrenceCoverage); });
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await session.ExtractAsync(sink, cancelled.Token));
        Assert.Null(proof.Provenance);
    }

    private sealed class RecordingSink : ICodeGraphSink
    {
        public CodeGraphBatchLimits Limits { get; } = new();
        public List<CodeGraphBatch> Batches { get; } = [];
        public ValueTask WriteBatchAsync(CodeGraphBatch batch, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); Batches.Add(batch); return ValueTask.CompletedTask; }
    }
}
