using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Penghou.Hetu.TreeSitter.Tests;

public sealed class TreeSitterElixirPluginTests
{
    private const string AccountsSource = """
        defmodule MyApp.Accounts do
          @moduledoc "Manages user accounts."
          alias MyApp.Repo
          import Ecto.Query, only: [from: 2]

          @spec get_user(integer()) :: map() | nil
          def get_user(id) do
            Repo.get(MyApp.User, id)
          end

          defp hash_password(password) do
            password
            |> String.trim()
            |> Bcrypt.hash_pwd_salt()
          end

          def create(attrs \\ %{}) do
            if attrs == %{} do
              {:error, :empty}
            else
              {:ok, attrs}
            end
          end
        end
        """;

    private const string BrokenSource = """
        defmodule Broken do
          def oops( do
            :never
        end
        """;

    [Fact]
    public async Task ExtractAsync_ModelsModulesFunctionsAndContainment()
    {
        var extracted = await ExtractAsync(("lib/accounts.ex", AccountsSource));

        var module = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "MyApp.Accounts");
        Assert.Equal("Accounts", module.Name);
        Assert.NotNull(module.SymbolId);

        var getUser = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "MyApp.Accounts.get_user/1");
        var hashPassword = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "MyApp.Accounts.hash_password/1");
        var create = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "MyApp.Accounts.create/1");

        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == module.Id &&
                edge.TargetId == getUser.Id);
        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Declares &&
                edge.TargetId == hashPassword.Id);
        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Declares &&
                edge.TargetId == create.Id);
        Assert.Equal(1, extracted.Result.SourcesExamined);
        Assert.Equal(0, extracted.Result.UnresolvedRelationships);
        Assert.DoesNotContain(extracted.Result.WarningCodes, code => code == "elixir.parse-error");
    }

    [Fact]
    public async Task ExtractAsync_EmitsImportsAndStructuralInvocationsOnly()
    {
        var extracted = await ExtractAsync(("lib/accounts.ex", AccountsSource));

        var imports = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Imports).ToArray();
        Assert.Contains(imports, edge => edge.TargetId == TargetNode(extracted, "MyApp.Repo").Id);
        Assert.Contains(imports, edge => edge.TargetId == TargetNode(extracted, "Ecto.Query").Id);

        var invocations = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.InvokesName).ToArray();
        Assert.NotEmpty(invocations);
        Assert.Contains(invocations, edge => edge.TargetId == InvokedNode(extracted, "Repo.get").Id);
        Assert.Contains(invocations, edge => edge.TargetId == InvokedNode(extracted, "String.trim").Id);
        Assert.Contains(invocations, edge => edge.TargetId == InvokedNode(extracted, "Bcrypt.hash_pwd_salt").Id);

        // Control forms are not invocations, and structural parsing never
        // claims resolved calls or references.
        Assert.DoesNotContain(invocations, edge => InvokedName(extracted, edge) == "if");
        Assert.DoesNotContain(invocations, edge => InvokedName(extracted, edge) == "moduledoc");
        Assert.DoesNotContain(invocations, edge => InvokedName(extracted, edge) == "spec");
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Calls);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.References);
        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    [Fact]
    public async Task ExtractAsync_PreservesValidFactsFromPartialFiles()
    {
        var extracted = await ExtractAsync(("lib/broken.ex", BrokenSource));

        // The module and its function survive recovery with honest locations.
        var module = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Broken");
        var declaration = Assert.Single(
            extracted.Declarations,
            declaration => declaration.SymbolId == module.SymbolId);
        Assert.Equal("lib/broken.ex", declaration.Location.Path);

        Assert.Contains("elixir.parse-error", extracted.Result.WarningCodes);
        var declares = Assert.Single(
            extracted.Result.RelationshipCoverage,
            coverage => coverage.RelationshipKind == CodeEdgeKinds.Declares.Value);
        Assert.Equal(CodeRelationshipCoverageState.Partial, declares.State);
        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    [Fact]
    public async Task ExtractAsync_RepeatsNormalizedFactsDeterministically()
    {
        var first = await ExtractAsync(("lib/accounts.ex", AccountsSource), ("lib/broken.ex", BrokenSource));
        var second = await ExtractAsync(("lib/broken.ex", BrokenSource), ("lib/accounts.ex", AccountsSource));

        Assert.Equal(JsonSerializer.Serialize(first.Nodes), JsonSerializer.Serialize(second.Nodes));
        Assert.Equal(JsonSerializer.Serialize(first.Declarations), JsonSerializer.Serialize(second.Declarations));
        Assert.Equal(JsonSerializer.Serialize(first.Edges), JsonSerializer.Serialize(second.Edges));
    }

    [Fact]
    public async Task ExtractAsync_EmittedBatchesPassHetuValidation()
    {
        var extracted = await ExtractAsync(("lib/accounts.ex", AccountsSource), ("lib/broken.ex", BrokenSource));
        var validator = new CodeGraphBatchValidator();

        Assert.All(extracted.Batches, batch =>
            Assert.Empty(validator.Validate(batch, new CodeGraphBatchLimits())));
    }

    [Fact]
    public async Task ExtractAsync_ReportsCapabilitiesHonestly()
    {
        var plugin = new TreeSitterElixirPlugin();

        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Syntax));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Declarations));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Imports));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Diagnostics));
        Assert.False(plugin.Capabilities.HasFlag(CodeGraphCapabilities.References));
        Assert.False(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Calls));

        Assert.True(plugin.CanHandle("lib/accounts.ex"));
        Assert.True(plugin.CanHandle("priv/script.EXS"));
        Assert.False(plugin.CanHandle("lib/accounts.cs"));
        Assert.False(plugin.CanHandle("README.md"));

        var extracted = await ExtractAsync(("lib/accounts.ex", AccountsSource));
        Assert.Contains(
            extracted.Result.RelationshipCoverage,
            coverage => coverage.RelationshipKind == CodeEdgeKinds.InvokesName.Value &&
                coverage.State == CodeRelationshipCoverageState.Produced &&
                coverage.EdgesEmitted > 0);
        Assert.Contains(
            extracted.Result.RelationshipCoverage,
            coverage => coverage.RelationshipKind == CodeEdgeKinds.Calls.Value &&
                coverage.State == CodeRelationshipCoverageState.NotProduced);
    }

    [Fact]
    public async Task ExtractAsync_ScopesScriptInvocationsToTheirFile()
    {
        var extracted = await ExtractAsync(
            ("priv/notify.exs", """
                Logger.info("starting")
                MyApp.Notify.send(:hello)
                """));

        var file = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.File);
        var invoked = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.InvokesName).ToArray();
        Assert.Equal(2, invoked.Length);
        Assert.All(invoked, edge => Assert.Equal(file.Id, edge.SourceId));
        Assert.Contains(invoked, edge => InvokedName(extracted, edge) == "Logger.info");
        Assert.Contains(invoked, edge => InvokedName(extracted, edge) == "MyApp.Notify.send");
    }

    [Fact]
    public async Task ExtractAsync_SkipsUndecodableSourcesButClearsTheirUnits()
    {
        var extracted = await ExtractBytesAsync(
            ("lib/latin1.ex", new byte[] { 0x64, 0x65, 0x66, 0x20, 0xE9, 0x0A }));

        Assert.Contains("elixir.undecodable-source", extracted.Result.WarningCodes);
        Assert.Equal(1, extracted.Result.SourcesExamined);
        Assert.Equal(0, extracted.Result.SourcesContributingFacts);
        var completing = Assert.Single(
            extracted.Batches,
            batch => batch.CompletesIndexUnit);
        Assert.Empty(completing.Nodes);
        Assert.Empty(completing.Edges);
    }

    [Fact]
    public async Task ExtractAsync_EmptySessionProducesEmptyResult()
    {
        var extracted = await ExtractAsync();

        Assert.Empty(extracted.Batches);
        Assert.Equal(0, extracted.Result.SourcesExamined);
        Assert.Empty(extracted.Result.WarningCodes);
    }

    [Fact]
    public void Plugin_ExposesPinnedGrammarIdentity()
    {
        var plugin = new TreeSitterElixirPlugin();

        Assert.Equal("elixir", plugin.Language);
        Assert.Equal("tree-sitter-elixir", plugin.Grammar.GrammarId);
        Assert.Equal("v0.3.5", plugin.Grammar.GrammarVersion);
        Assert.Equal("e2d9e6e0e76b0c436fa48a0b8c32a031d0cbdf49", plugin.Grammar.GrammarCommit);
        Assert.Equal(14, plugin.Grammar.AbiVersion);

        // Effective provider identity folds implementation, grammar, ABI, and
        // profile revision into one freshness-relevant string.
        var packageVersion = typeof(TreeSitterElixirPlugin).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+', 2)[0];
        Assert.Equal($"{packageVersion}+v0.3.5.abi14.p1", plugin.Version);
    }

    [Fact]
    public async Task IndexAsync_SelectsElixirPluginThroughProviderMapAndAnswersQueries()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-elixir-");
        try
        {
            var lib = Directory.CreateDirectory(Path.Combine(directory.FullName, "lib"));
            await File.WriteAllTextAsync(Path.Combine(lib.FullName, "accounts.ex"), AccountsSource);
            await File.WriteAllTextAsync(Path.Combine(lib.FullName, "broken.ex"), BrokenSource);

            var registry = new CodeGraphPluginRegistry(
                [
                    new TreeSitterElixirPlugin(),
                    new FakeDotNetPlugin()
                ],
                new CodeLanguageProviderMap(new Dictionary<string, IReadOnlyList<CodePluginId>>
                {
                    ["elixir"] = [new CodePluginId("penghou.hetu.tree-sitter.elixir"), new CodePluginId("plugin:dotnet")]
                }));
            var repositoryId = new CodeRepositoryId("repo:elixir");
            var store = new InMemoryCodeGraphStore();
            var indexing = new CodeIndexingService(
                new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
                registry,
                store);

            var result = await indexing.IndexAsync(
                new CodeRepositoryDescriptor(repositoryId, directory.FullName),
                new CodeIndexRunId("run:elixir"));

            Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
            var queries = new CodeGraphQueryService(store);
            var lookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "MyApp.Accounts");
            var symbol = Assert.Single(lookup.Candidates);
            Assert.Equal("Accounts", symbol.Name);

            var getUserId = (await queries.FindSymbolsByQualifiedNameAsync(
                repositoryId, "MyApp.Accounts.get_user/1"))
                .Candidates.Single().Id;

            // Resolved callers: none — structural evidence never poses as
            // resolved. Structural invocations stay explicitly traversable.
            var callers = await queries.FindCallersAsync(repositoryId, getUserId);
            Assert.Empty(callers.Edges);
            var structural = await store.TraverseAsync(
                repositoryId,
                new CodeGraphTraversalQuery(
                    getUserId,
                    CodeGraphDirection.Outgoing,
                    [CodeEdgeKinds.InvokesName],
                    maxDepth: 1,
                    maxNodes: 100,
                    maxEdges: 100));
            var invoked = Assert.Single(structural.Edges);
            Assert.Equal(
                "Repo.get",
                structural.Nodes.Single(node => node.Id == invoked.TargetId).Name);

            var view = (await queries.OpenLatestPublicationAsync(repositoryId))!;
            var neighborhood = await view.GetNeighborhoodAsync(
                symbol.Id,
                options: new CodeGraphQueryOptions(maxDepth: 2, maxNodes: 100, maxEdges: 200));
            Assert.Contains(
                neighborhood.Result.Nodes,
                node => node.Kind == CodeNodeKinds.Callable &&
                    node.QualifiedName == "MyApp.Accounts.get_user/1");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static CodeGraphNode TargetNode(Extraction extracted, string qualifiedName) =>
        Assert.Single(
            extracted.Nodes,
            node => node.QualifiedName == qualifiedName);

    private static CodeGraphNode InvokedNode(Extraction extracted, string name) =>
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.Name == name && node.QualifiedName is null);

    private static string InvokedName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.TargetId).Name;

    private static Task<Extraction> ExtractAsync(params (string Path, string Content)[] values) =>
        ExtractAsync(new CodeGraphBatchLimits(), values);

    private static Task<Extraction> ExtractBytesAsync(params (string Path, byte[] Content)[] values)
    {
        var sources = values.Select(value => new CodeGraphSource(
            value.Path,
            $"sha256:{Hash(Convert.ToHexString(value.Content))}",
            _ => new ValueTask<Stream>(new MemoryStream(value.Content)))).ToArray();
        return ExtractFromSourcesAsync(new CodeGraphBatchLimits(), sources);
    }

    private static async Task<Extraction> ExtractAsync(
        CodeGraphBatchLimits limits,
        params (string Path, string Content)[] values)
    {
        var sources = values.Select(value => new CodeGraphSource(
            value.Path,
            $"sha256:{Hash(value.Content)}",
            _ => new ValueTask<Stream>(
                new MemoryStream(Encoding.UTF8.GetBytes(value.Content))))).ToArray();
        return await ExtractFromSourcesAsync(limits, sources).ConfigureAwait(false);
    }

    private static async Task<Extraction> ExtractFromSourcesAsync(
        CodeGraphBatchLimits limits,
        CodeGraphSource[] sources)
    {
        var context = new CodeGraphPluginContext(
            new CodeRepositoryId("repo:test"),
            "memory://test",
            new CodeIndexRunId("run:test"),
            sources);
        var plugin = new TreeSitterElixirPlugin();
        var sink = new RecordingSink(limits);

        await using var session = await plugin.CreateSessionAsync(context);
        var result = await session.ExtractAsync(sink);

        if (sources.Length > 0)
        {
            Assert.True(sink.Batches.Count > 0);
            Assert.True(sink.Batches[^1].CompletesIndexUnit);
        }
        return new(
            sink.Batches.SelectMany(batch => batch.Nodes).OrderBy(node => node.Id.Value, StringComparer.Ordinal).ToArray(),
            sink.Batches.SelectMany(batch => batch.Declarations).OrderBy(value => value.Id.Value, StringComparer.Ordinal).ToArray(),
            sink.Batches.SelectMany(batch => batch.Edges).OrderBy(edge => edge.Id.Value, StringComparer.Ordinal).ToArray(),
            result,
            sink.Batches.ToArray());
    }

    private static string Hash(string content) =>
        Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    private sealed class RecordingSink(CodeGraphBatchLimits limits) : ICodeGraphSink
    {
        public CodeGraphBatchLimits Limits { get; } = limits;
        public List<CodeGraphBatch> Batches { get; } = [];

        public ValueTask WriteBatchAsync(
            CodeGraphBatch batch,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Batches.Add(batch);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeDotNetPlugin : ICodeGraphPlugin
    {
        public CodePluginId Id { get; } = new("plugin:dotnet");
        public string Version => "1.0.0";
        public string Language => "elixir";
        public IReadOnlyCollection<string> FileExtensions => [".ex", ".exs"];
        public CodeGraphCapabilities Capabilities => CodeGraphCapabilities.Syntax;

        public bool CanHandle(string path) =>
            path.EndsWith(".ex", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".exs", StringComparison.OrdinalIgnoreCase);

        public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
            CodeGraphPluginContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The provider map must prefer the Tree-sitter plugin.");
    }

    private sealed record Extraction(
        IReadOnlyList<CodeGraphNode> Nodes,
        IReadOnlyList<CodeGraphDeclaration> Declarations,
        IReadOnlyList<CodeGraphEdge> Edges,
        CodeGraphExtractionResult Result,
        IReadOnlyList<CodeGraphBatch> Batches);
}
