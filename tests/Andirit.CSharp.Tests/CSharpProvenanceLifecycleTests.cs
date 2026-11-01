using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Andirit.CSharp.Tests;

public sealed class CSharpProvenanceLifecycleTests
{
    [Fact]
    public async Task RealLifecycleHandsOffVerifiedSnapshotAndSourceEditCreatesNewContext()
    {
        var directory = Directory.CreateTempSubdirectory("andirit-s2-1-");
        try
        {
            var path = Path.Combine(directory.FullName, "Source.cs");
            const string first = "public class Source { public int Value() => 1; }";
            const string second = "public class Source { public int Value() => 2; }";
            await File.WriteAllTextAsync(path, first, new UTF8Encoding(false));
            var observer = new ObservingPlugin();
            var service = new CodeIndexingService(
                new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
                new CodeGraphPluginRegistry([observer]), new InMemoryCodeGraphStore());
            var repository = new CodeRepositoryDescriptor(new("repo:real-snapshot"), directory.FullName);
            await service.IndexAsync(repository, new("run:first"));
            Assert.True(observer.SawMaterializedInputs);
            var original = Assert.Single(observer.Analysis!.Universe.Entries);
            Assert.True(observer.Analysis.Universe.IsComplete);
            Assert.Equal("sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(first))),
                Assert.Single(original.Sources).ContentHash);
            await File.WriteAllTextAsync(path, second, new UTF8Encoding(false));
            Assert.Equal(first, Assert.Single(original.Sources).Text.ToString());
            await service.IndexAsync(repository, new("run:second"));
            var updated = Assert.Single(observer.Analysis!.Universe.Entries);
            Assert.NotEqual(original.Descriptor.DescriptorDigest, updated.Descriptor.DescriptorDigest);
            Assert.Equal(second, Assert.Single(updated.Sources).Text.ToString());
            Assert.Throws<InvalidDataException>(() => updated.ValidateExpectedDescriptor(original.Descriptor));
        }
        finally { directory.Delete(recursive: true); }
    }

    [Fact]
    public void VersionedContextsCannotEnterLegacyOccurrenceMinting()
    {
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            "compctx2:sha256:" + new string('a', 64), "Assembly|0.0.0.0||", "Assembly.dll",
            new string('b', 64), new string('c', 64), new string('d', 64)));
    }

    [Fact]
    public async Task SolutionWithoutAnyAcquiredProjectsRefusesCompleteness()
    {
        const string text = "<Solution><Project Path=\"Missing/Missing.csproj\"/></Solution>";
        var context = new CodeGraphPluginContext(new("repo:missing"), null, new("run:missing"),
            [new("Scope.slnx", "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(text))))]);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.False(analysis.Universe.IsComplete);
        Assert.Contains(analysis.Universe.Refusals, r => r.StartsWith("MissingDeclaredProject:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvalidSolutionXmlRefusesCompleteness()
    {
        const string text = "<Solution><Project";
        var context = new CodeGraphPluginContext(new("repo:invalid"), null, new("run:invalid"),
            [new("Scope.slnx", "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(text))))]);
        Assert.Contains((await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context)).Universe.Refusals,
            r => r.StartsWith("InvalidSolutionXml:", StringComparison.Ordinal));
    }

    private sealed class ObservingPlugin : ICodeGraphPlugin
    {
        private readonly CSharpCodeGraphPlugin inner = new();
        internal VerifiedCSharpAnalysis? Analysis { get; set; }
        internal bool SawMaterializedInputs { get; private set; }
        public CodePluginId Id => inner.Id;
        public string Version => inner.Version;
        public string Language => inner.Language;
        public IReadOnlyCollection<string> FileExtensions => inner.FileExtensions;
        public CodeGraphCapabilities Capabilities => inner.Capabilities;
        public bool CanHandle(string path) => inner.CanHandle(path);
        public async ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(CodeGraphPluginContext context,
            CancellationToken cancellationToken = default)
        {
            // Observe the existing private handoff without adding a public test/provenance API.
            var property = typeof(CodeGraphSource).GetProperty("MaterializedByteHash", BindingFlags.NonPublic | BindingFlags.Instance)!;
            SawMaterializedInputs = context.Sources.Count > 0 && context.Sources.All(s => property.GetValue(s) is string hash && hash == s.ContentHash);
            return new ObservingSession(await inner.CreateSessionAsync(context, cancellationToken), this);
        }
    }
    private sealed class ObservingSession(ICodeGraphExtractionSession inner, ObservingPlugin owner) : ICodeGraphExtractionSession
    {
        public async ValueTask<CodeGraphExtractionResult> ExtractAsync(ICodeGraphSink sink, CancellationToken cancellationToken = default)
        {
            var result = await inner.ExtractAsync(sink, cancellationToken);
            owner.Analysis = ((IVerifiedCSharpSession)inner).Provenance;
            return result;
        }
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
