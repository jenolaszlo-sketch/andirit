using System.Security.Cryptography;
using System.Text;

namespace Penghou.Hetu;

/// <summary>
/// Generic repository-aware extraction session for Tree-sitter providers. It
/// owns file iteration, decoding, per-grammar parser lifetimes, bounded batch
/// emission, unit identity, and relationship coverage. Language differences
/// enter only through the profile and the per-path grammar resolver, so a new
/// language adds no session logic.
/// </summary>
internal sealed class TreeSitterExtractionSession : ICodeGraphExtractionSession
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>
    /// Every normalized relationship kind a Tree-sitter provider may report.
    /// Kinds a provider does not produce are reported as
    /// <see cref="CodeRelationshipCoverageState.NotProduced"/> rather than
    /// omitted, so consumers can tell "no such relationship exists" from
    /// "this provider does not emit that kind".
    /// </summary>
    private static readonly CodeEdgeKind[] CoverageKinds =
    [
        CodeEdgeKinds.Contains,
        CodeEdgeKinds.Declares,
        CodeEdgeKinds.Imports,
        CodeEdgeKinds.InvokesName,
        CodeEdgeKinds.Inherits,
        CodeEdgeKinds.Implements,
        CodeEdgeKinds.Calls,
        CodeEdgeKinds.References
    ];

    private readonly CodeGraphPluginContext _context;
    private readonly CodePluginId _pluginId;
    private readonly string _pluginVersion;
    private readonly Func<string, TreeSitterGrammarDescriptor> _grammarForPath;
    private readonly Func<string, ITreeSitterExtractionProfile> _profileForPath;
    private readonly HashSet<CodeEdgeKind> _producedRelationshipKinds;
    private readonly Dictionary<string, TreeSitterSessionParser> _parsers = new(StringComparer.Ordinal);
    private bool _disposed;

    internal TreeSitterExtractionSession(
        CodeGraphPluginContext context,
        CodePluginId pluginId,
        string pluginVersion,
        IReadOnlyList<CodeEdgeKind> producedRelationshipKinds,
        Func<string, TreeSitterGrammarDescriptor> grammarForPath,
        Func<string, ITreeSitterExtractionProfile> profileForPath)
    {
        _context = context;
        _pluginId = pluginId;
        _pluginVersion = pluginVersion;
        _producedRelationshipKinds = [.. producedRelationshipKinds];
        _grammarForPath = grammarForPath;
        _profileForPath = profileForPath;
    }

    public async ValueTask<CodeGraphExtractionResult> ExtractAsync(
        ICodeGraphSink sink,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sink);

        var examined = 0;
        var contributing = 0;
        var partial = false;
        var warnings = new HashSet<string>(StringComparer.Ordinal);
        var coverage = new Dictionary<CodeEdgeKind, (int Candidates, int Emitted)>();
        var obsolete = new List<CodeIndexUnitId>();

        foreach (var source in _context.Sources.OrderBy(s => s.Path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            examined++;

            var profile = _profileForPath(source.Path);
            var language = profile.LanguageKey;
            var parser = GetParser(_grammarForPath(source.Path));

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
                warnings.Add($"{language}.undecodable-source");
                await sink.WriteBatchAsync(
                    new CodeGraphBatch(
                        UnitOrigin(source.Path, language, source.ContentHash),
                        completesIndexUnit: true),
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            using var document = parser.Parse(text);
            var fileNodeId = new CodeNodeId($"{language}:file:{Hash(source.Path)}");
            var fileNode = new CodeGraphNode(
                fileNodeId,
                CodeNodeKinds.File,
                Path.GetFileName(source.Path),
                source.Path,
                properties: new Dictionary<string, CodePropertyValue>
                {
                    [CodePropertyKeys.Language] = new CodeTextProperty(language),
                    [CodePropertyKeys.ContentHash] = new CodeTextProperty(source.ContentHash)
                });
            var extraction = profile.Extract(document.Root, fileNodeId, source.Path);
            if (document.Root.HasError)
            {
                partial = true;
                warnings.Add($"{language}.parse-error");
            }

            var origin = UnitOrigin(source.Path, language, source.ContentHash);
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

        foreach (var change in _context.Changes
                     .Where(c => c.Kind == CodeGraphSourceChangeKind.Deleted)
                     .OrderBy(c => c.Path, StringComparer.Ordinal))
        {
            obsolete.Add(new CodeIndexUnitId(
                $"{_profileForPath(change.Path).LanguageKey}:file:{Hash(change.Path)}"));
        }

        var state = partial
            ? CodeRelationshipCoverageState.Partial
            : CodeRelationshipCoverageState.Produced;
        var relationshipCoverage = new List<CodeRelationshipCoverage>(CoverageKinds.Length);
        foreach (var kind in CoverageKinds)
        {
            relationshipCoverage.Add(_producedRelationshipKinds.Contains(kind)
                ? Coverage(kind, coverage, state)
                : new(kind.Value, CodeRelationshipCoverageState.NotProduced, 0, 0));
        }

        return new CodeGraphExtractionResult(
            obsolete,
            sourcesExamined: examined,
            sourcesContributingFacts: contributing,
            warningCodes: warnings.Order(StringComparer.Ordinal).ToArray(),
            relationshipCoverage: relationshipCoverage);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;
        _disposed = true;
        foreach (var parser in _parsers.Values)
            parser.Dispose();
        _parsers.Clear();
        return ValueTask.CompletedTask;
    }

    private TreeSitterSessionParser GetParser(TreeSitterGrammarDescriptor descriptor)
    {
        if (!_parsers.TryGetValue(descriptor.NativeLibrary, out var parser))
        {
            parser = TreeSitterSessionParser.Create(descriptor);
            _parsers[descriptor.NativeLibrary] = parser;
        }

        return parser;
    }

    private CodeFactOrigin UnitOrigin(string path, string language, string contentHash) =>
        new(
            _context.RepositoryId,
            _pluginId,
            _pluginVersion,
            _context.IndexRunId,
            new CodeIndexUnitId($"{language}:file:{Hash(path)}"),
            path,
            contentHash);

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
