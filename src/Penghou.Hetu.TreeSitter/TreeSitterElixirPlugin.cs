using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Penghou.Hetu;

/// <summary>
/// Tree-sitter structural extraction for Elixir, backed by the pinned
/// <c>tree-sitter-elixir</c> grammar. Every fact carries structural evidence:
/// modules, functions, imports, containment, and invocation names are
/// observed from syntax, never resolved. Malformed regions yield partial
/// results with diagnostics rather than fabricated relationships.
/// </summary>
public sealed class TreeSitterElixirPlugin : ICodeGraphPlugin
{
    internal static readonly CodePluginId ElixirPluginId = new("penghou.hetu.tree-sitter.elixir");

    /// <summary>
    /// Effective provider identity: package implementation plus pinned
    /// grammar, ABI, and extraction-profile revisions. Any component change
    /// alters this string, so planner freshness can never survive a grammar
    /// or profile change behind an unchanged package version.
    /// </summary>
    private static readonly string ElixirPluginVersion =
        $"{GetPackageVersion()}+{TreeSitterGrammarRegistry.Elixir.GrammarVersion}" +
        $".abi{TreeSitterGrammarRegistry.Elixir.AbiVersion}" +
        $".p{ElixirExtractionProfile.ProfileRevision}";

    public CodePluginId Id => ElixirPluginId;

    /// <summary>Gets the plugin package version driving index invalidation.</summary>
    public string Version => ElixirPluginVersion;

    internal static string PluginVersion => ElixirPluginVersion;

    internal static CodePluginId PluginId => ElixirPluginId;

    public string Language => TreeSitterGrammarRegistry.Elixir.Language;

    public IReadOnlyCollection<string> FileExtensions => [".ex", ".exs"];

    public CodeGraphCapabilities Capabilities => TreeSitterGrammarRegistry.Elixir.Capabilities;

    /// <summary>Gets the pinned grammar descriptor backing this plugin.</summary>
    public TreeSitterGrammarDescriptor Grammar => TreeSitterGrammarRegistry.Elixir;

    public bool CanHandle(string path) =>
        path.EndsWith(".ex", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".exs", StringComparison.OrdinalIgnoreCase);

    public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
        CodeGraphPluginContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return new(new Session(context, this));
    }

    private static string GetPackageVersion()
    {
        var assembly = typeof(TreeSitterElixirPlugin).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
            return informational.Split('+', 2)[0];

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private sealed class Session(CodeGraphPluginContext context, TreeSitterElixirPlugin plugin)
        : ICodeGraphExtractionSession
    {
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        public async ValueTask<CodeGraphExtractionResult> ExtractAsync(
            ICodeGraphSink sink,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sink);
            using var parser = TreeSitterSessionParser.Create(TreeSitterGrammarRegistry.Elixir);

            var examined = 0;
            var contributing = 0;
            var partial = false;
            var warnings = new HashSet<string>(StringComparer.Ordinal);
            var coverage = new Dictionary<CodeEdgeKind, (int Candidates, int Emitted)>();
            var obsolete = new List<CodeIndexUnitId>();

            foreach (var source in context.Sources.OrderBy(s => s.Path, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                examined++;
                byte[] bytes;
                await using (var stream = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false))
                await using (var buffer = new MemoryStream())
                {
                    await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                    bytes = buffer.ToArray();
                }

                string text;
                try
                {
                    text = StrictUtf8.GetString(bytes);
                }
                catch (DecoderFallbackException)
                {
                    // Undecodable sources contribute no facts, but their previous
                    // unit must not linger: stage an empty completion to clear it.
                    warnings.Add("elixir.undecodable-source");
                    await sink.WriteBatchAsync(
                        new CodeGraphBatch(
                            UnitOrigin(source),
                            completesIndexUnit: true),
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }

                using var document = parser.Parse(text);
                var profile = new ElixirExtractionProfile(source.Path);
                var fileNodeId = new CodeNodeId($"elixir:file:{Hash(source.Path)}");
                var fileNode = new CodeGraphNode(
                    fileNodeId,
                    CodeNodeKinds.File,
                    Path.GetFileName(source.Path),
                    source.Path,
                    properties: new Dictionary<string, CodePropertyValue>
                    {
                        [CodePropertyKeys.Language] = new CodeTextProperty("elixir"),
                        [CodePropertyKeys.ContentHash] = new CodeTextProperty(source.ContentHash)
                    });
                var extraction = profile.Extract(document.Root, fileNodeId);
                var filePartial = document.Root.HasError;
                if (filePartial)
                {
                    partial = true;
                    warnings.Add("elixir.parse-error");
                }

                var origin = UnitOrigin(source);
                var nodes = extraction.Nodes.Values.Prepend(fileNode).ToArray();
                await WriteChunksAsync(
                    nodes,
                    sink.Limits.MaxNodes,
                    values => new CodeGraphBatch(origin, nodes: values),
                    sink,
                    cancellationToken).ConfigureAwait(false);
                await WriteChunksAsync(
                    extraction.Declarations.Values.ToArray(),
                    sink.Limits.MaxDeclarations,
                    values => new CodeGraphBatch(origin, declarations: values),
                    sink,
                    cancellationToken).ConfigureAwait(false);
                await WriteChunksAsync(
                    extraction.Edges.Values.ToArray(),
                    sink.Limits.MaxEdges,
                    values => new CodeGraphBatch(origin, edges: values),
                    sink,
                    cancellationToken).ConfigureAwait(false);
                await sink.WriteBatchAsync(
                    new CodeGraphBatch(origin, completesIndexUnit: true),
                    cancellationToken).ConfigureAwait(false);

                if (nodes.Length > 1 || extraction.Declarations.Count > 0 || extraction.Edges.Count > 0)
                    contributing++;
                foreach (var (kind, count) in extraction.EdgeCounts)
                {
                    var entry = coverage.TryGetValue(kind, out var existing)
                        ? existing
                        : (Candidates: 0, Emitted: 0);
                    coverage[kind] = (entry.Candidates + count, entry.Emitted + count);
                }
            }

            foreach (var change in context.Changes
                         .Where(c => c.Kind == CodeGraphSourceChangeKind.Deleted)
                         .OrderBy(c => c.Path, StringComparer.Ordinal))
                obsolete.Add(new CodeIndexUnitId($"elixir:file:{Hash(change.Path)}"));

            var state = partial
                ? CodeRelationshipCoverageState.Partial
                : CodeRelationshipCoverageState.Produced;
            var relationshipCoverage = new List<CodeRelationshipCoverage>
            {
                Coverage(CodeEdgeKinds.Contains, coverage, state),
                Coverage(CodeEdgeKinds.Declares, coverage, state),
                Coverage(CodeEdgeKinds.Imports, coverage, state),
                Coverage(CodeEdgeKinds.InvokesName, coverage, state),
                new(CodeEdgeKinds.Calls.Value, CodeRelationshipCoverageState.NotProduced, 0, 0),
                new(CodeEdgeKinds.References.Value, CodeRelationshipCoverageState.NotProduced, 0, 0)
            };

            return new CodeGraphExtractionResult(
                obsolete,
                sourcesExamined: examined,
                sourcesContributingFacts: contributing,
                warningCodes: warnings.Order(StringComparer.Ordinal).ToArray(),
                relationshipCoverage: relationshipCoverage);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private CodeFactOrigin UnitOrigin(CodeGraphSource source) =>
            new(
                context.RepositoryId,
                plugin.Id,
                plugin.Version,
                context.IndexRunId,
                new CodeIndexUnitId($"elixir:file:{Hash(source.Path)}"),
                source.Path,
                source.ContentHash);

        private static CodeRelationshipCoverage Coverage(
            CodeEdgeKind kind,
            IReadOnlyDictionary<CodeEdgeKind, (int Candidates, int Emitted)> coverage,
            CodeRelationshipCoverageState state)
        {
            var entry = coverage.TryGetValue(kind, out var counts)
                ? counts
                : (Candidates: 0, Emitted: 0);
            return new(
                kind.Value,
                state,
                edgesEmitted: entry.Emitted,
                unresolvedTargets: 0,
                candidates: entry.Candidates,
                internalTargets: entry.Emitted);
        }

        private static async ValueTask WriteChunksAsync<T>(
            IReadOnlyList<T> values,
            int chunkSize,
            Func<IReadOnlyList<T>, CodeGraphBatch> batch,
            ICodeGraphSink sink,
            CancellationToken cancellationToken)
            where T : class
        {
            for (var index = 0; index < values.Count; index += chunkSize)
            {
                var chunk = values.Skip(index).Take(chunkSize).ToArray();
                await sink.WriteBatchAsync(batch(chunk), cancellationToken).ConfigureAwait(false);
            }
        }

        private static string Hash(string value) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
