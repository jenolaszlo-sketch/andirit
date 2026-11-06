using System.Text;
using System.Text.Json;
using LatticeDbSharp;
using Andirit.LatticeDb;
using static Andirit.DurableCommandLog;

namespace Andirit;

/// <summary>Durable embedded Andirit store backed by LatticeDB.</summary>
/// <remarks>
/// <para>
/// LatticeDB is the durable command log; all graph semantics and query behavior
/// are served from a materialized <see cref="InMemoryCodeGraphStore"/> rebuilt by
/// replaying that log on open. When <see cref="LatticeDb.LatticeDbStoreOptions.UseNativeTraversal"/>
/// is enabled, an experimental native mirror additionally serves
/// <c>TraverseAsync</c> without evidence filters from real graph structure;
/// everything else keeps delegating to the inner store.
/// </para>
/// <para>
/// Ownership and errors: a database file has a single owner process while open.
/// Opening a second store on the same path (in- or out-of-process) fails rather
/// than queuing. Native failures surface as <see cref="Andirit.CodeGraphStoreException"/>
/// with the engine error preserved as the inner exception; Andirit validation
/// failures keep their <see cref="Andirit.CodeGraphBatchRejectedException"/> contract.
/// </para>
/// <para>
/// Schema evolution: <see cref="CurrentSchemaVersion"/> is validated on open and
/// mismatches are rejected; see the repository ROADMAP for the migration policy
/// (export/import; no in-place upgrades in preview).
/// </para>
/// </remarks>
public sealed partial class LatticeCodeGraphStore :
    ICodeGraphStore,
    ICodeGraphStoreHealthCheck,
    ICodeOccurrenceReader,
    IDisposable
{
    public const int CurrentSchemaVersion = 2;

    /// <summary>Schema version of the additive production occurrence tables.</summary>
    public const int CurrentOccurrenceSchemaVersion = 1;

    internal const string OccurrenceNamespace = "andirit-occurrence-facts/1";

    // Schema-2 protocol identities survive the Andirit product rename.
    private const string MetadataLabel = "HetuMetadata";
    private const string RepositoryLabel = "HetuRepository";
    private const string RunLabel = "HetuRun";
    private const string IndexStateLabel = "HetuIndexState";
    private const string UnitLabel = "HetuUnit";
    private const string StageLabel = "HetuStage";
    private const string BaselineLabel = "HetuBaseline";
    // Additive occurrence labels are deliberately distinct from the Hetu* graph
    // labels: occurrence facts are never legacy graph nodes or edges.
    private const string OccurrenceUnitLabel = "AndiritOccurrenceUnit";
    private const string OccurrencePageLabel = "AndiritOccurrencePage";
    private const string OccurrenceMetaLabel = "AndiritOccurrenceMeta";
    private const string SchemaKey = "schema";

    private readonly LatticeDatabase _database;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly bool _nativeTraversal;
    private List<PersistedCommand> _commands;
    private readonly Dictionary<string, string?> _baselines = new(StringComparer.Ordinal);
    private volatile InMemoryCodeGraphStore _inner;
    private volatile Dictionary<string, NativeGraphMirror> _mirrors =
        new(StringComparer.Ordinal);
    private bool _disposed;
    private readonly Action<string>? _faultInjector;
    private bool _outcomeUnknown;

    public LatticeCodeGraphStore(string databasePath)
        : this(databasePath, null, null)
    {
    }

    public LatticeCodeGraphStore(string databasePath, LatticeDbStoreOptions? options)
        : this(databasePath, options, null)
    {
    }

    internal LatticeCodeGraphStore(
        string databasePath,
        LatticeDbStoreOptions? options,
        Action<string>? faultInjector)
        : this(databasePath, options, faultInjector, CancellationToken.None)
    {
    }

    private LatticeCodeGraphStore(
        string databasePath,
        LatticeDbStoreOptions? options,
        Action<string>? faultInjector,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("A LatticeDB database file path is required.", nameof(databasePath));
        _database = TranslateNative(
            () => LatticeDatabase.Open(databasePath, ToNativeOptions(options)),
            "open database");
        _nativeTraversal = options?.UseNativeTraversal is true;
        _faultInjector = faultInjector;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            InitializeSchema();
            cancellationToken.ThrowIfCancellationRequested();
            var repositories = ReadTable<CodeRepositoryManifest>(RepositoryLabel);
            var runs = ReadTable<CodeIndexRunManifest>(RunLabel);
            var units = ReadTable<CodeIndexUnitReplacement>(UnitLabel);
            var stages = ReadTable<PersistedCommand>(StageLabel);
            var states = ReadTable<CodeRepositoryIndexState>(IndexStateLabel);
            _commands = Reconstruct(repositories, runs, units, stages, states);
            _inner = ReplayAsync(_commands, cancellationToken).GetAwaiter().GetResult();
            foreach (var (key, baseline) in ReadPairs(BaselineLabel))
            {
                var separator = key.IndexOf('\n');
                if (separator <= 0 || separator == key.Length - 1)
                    throw new InvalidDataException($"Lattice {BaselineLabel} key is invalid.");
                _baselines.Add(key, string.IsNullOrEmpty(baseline) ? null : baseline);
            }
            RestoreBaselines(_inner);
            RestoreOccurrences(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (_nativeTraversal)
            {
                RestoreMirrors(repositories.Select(repository => repository.Id.Value).ToArray());
            }
        }
        catch
        {
            _database.Dispose();
            throw;
        }
    }

    /// <summary>Opens a store without blocking the calling thread on native I/O.</summary>
    /// <remarks>Construction performs native I/O and full log replay, so this factory
    /// offloads that work to the thread pool; the returned store is identical to the
    /// constructor overloads.</remarks>
    public static Task<LatticeCodeGraphStore> OpenAsync(
        string databasePath,
        CancellationToken cancellationToken = default) =>
        OpenAsync(databasePath, null, cancellationToken);

    /// <summary>Opens a tuned store without blocking the calling thread on native I/O.</summary>
    public static Task<LatticeCodeGraphStore> OpenAsync(
        string databasePath,
        LatticeDbStoreOptions? options,
        CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new LatticeCodeGraphStore(databasePath, options, null, cancellationToken);
        }, cancellationToken);

    public ValueTask UpsertRepositoryAsync(CodeRepositoryManifest repository, CancellationToken cancellationToken = default) =>
        MutateAsync(new("repository", Repository: repository), cancellationToken);

    public ValueTask<CodeRepositoryManifest?> GetRepositoryAsync(CodeRepositoryId repositoryId, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.GetRepositoryAsync(repositoryId, cancellationToken), cancellationToken);

    public ValueTask StoreIndexRunAsync(CodeIndexRunManifest run, CancellationToken cancellationToken = default) =>
        MutateAsync(new("run", Run: run), cancellationToken);

    public ValueTask<CodeIndexRunManifest?> GetIndexRunAsync(CodeRepositoryId repositoryId, CodeIndexRunId runId, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.GetIndexRunAsync(repositoryId, runId, cancellationToken), cancellationToken);

    public ValueTask CompleteIndexRunAsync(CodeIndexRunManifest completedRun, CodeRepositoryIndexState state, CancellationToken cancellationToken = default) =>
        MutateAsync(new("complete", Run: completedRun, State: state), cancellationToken);

    public ValueTask<CodeRepositoryIndexState?> GetLatestIndexStateAsync(CodeRepositoryId repositoryId, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.GetLatestIndexStateAsync(repositoryId, cancellationToken), cancellationToken);

    public ValueTask<CodeGraphPublication?> GetLatestPublicationAsync(CodeRepositoryId repositoryId, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.GetLatestPublicationAsync(repositoryId, cancellationToken), cancellationToken);

    public ValueTask<IReadOnlyList<CodeIndexUnitReplacement>> GetPublishedUnitsAsync(CodeRepositoryId repositoryId, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.GetPublishedUnitsAsync(repositoryId, cancellationToken), cancellationToken);

    public ValueTask StageIndexUnitAsync(CodeIndexUnitReplacement replacement, CancellationToken cancellationToken = default) =>
        MutateAsync(new("stage-replace", Replacement: replacement), cancellationToken);

    public ValueTask StageIndexUnitDeletionAsync(CodeRepositoryId repositoryId, CodeIndexRunId indexRunId, CodePluginId pluginId, CodeIndexUnitId indexUnitId, CancellationToken cancellationToken = default) =>
        MutateAsync(new("stage-delete", RepositoryId: repositoryId, RunId: indexRunId, PluginId: pluginId, UnitId: indexUnitId), cancellationToken);

    public ValueTask<CodeGraphNode?> GetNodeAsync(CodeRepositoryId repositoryId, CodeNodeId nodeId, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.GetNodeAsync(repositoryId, nodeId, cancellationToken), cancellationToken);

    public ValueTask<CodeGraphNode?> FindSymbolAsync(CodeRepositoryId repositoryId, CodeSymbolId symbolId, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.FindSymbolAsync(repositoryId, symbolId, cancellationToken), cancellationToken);

    public ValueTask<IReadOnlyList<CodeGraphNode>> FindNodesByQualifiedNameAsync(CodeRepositoryId repositoryId, string qualifiedName, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.FindNodesByQualifiedNameAsync(repositoryId, qualifiedName, cancellationToken), cancellationToken);

    public ValueTask<CodeNamePatternResult> FindNodesByNamePatternAsync(
        CodeRepositoryId repositoryId,
        string pattern,
        int maxResults = CodeNamePatternResult.DefaultMaxResults,
        CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.FindNodesByNamePatternAsync(repositoryId, pattern, maxResults, cancellationToken), cancellationToken);

    public ValueTask<IReadOnlyList<CodeGraphDeclaration>> GetDeclarationsAsync(CodeRepositoryId repositoryId, CodeSymbolId symbolId, CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.GetDeclarationsAsync(repositoryId, symbolId, cancellationToken), cancellationToken);

    public async ValueTask<CodeGraphTraversalResult> TraverseAsync(CodeRepositoryId repositoryId, CodeGraphTraversalQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentNullException.ThrowIfNull(query);
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            ThrowIfUncertain();
            if (_nativeTraversal &&
                query.EvidenceQualities.Count == 0 &&
                _mirrors.TryGetValue(repositoryId.Value, out var mirror))
            {
                return TranslateNative(() =>
                {
                    using var txn = _database.BeginReadTransaction();
                    var result = mirror.Traverse(txn, query, cancellationToken);
                    txn.Commit();
                    return result;
                }, "traverse graph");
            }

            return await _inner.TraverseAsync(repositoryId, query, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public ValueTask<CodeGraphQueryEnvelope<IReadOnlyList<CodeGraphNode>>?> FindNodesByQualifiedNameWithProvenanceAsync(
        CodeRepositoryId repositoryId,
        string qualifiedName,
        CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.FindNodesByQualifiedNameWithProvenanceAsync(repositoryId, qualifiedName, cancellationToken), cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?> TraverseWithProvenanceAsync(
        CodeRepositoryId repositoryId,
        CodeGraphTraversalQuery query,
        CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.TraverseWithProvenanceAsync(repositoryId, query, cancellationToken), cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<IReadOnlyList<CodeGraphDeclaration>>?> GetDeclarationsWithProvenanceAsync(
        CodeRepositoryId repositoryId,
        CodeSymbolId symbolId,
        CancellationToken cancellationToken = default) =>
        ReadAsync(store => store.GetDeclarationsWithProvenanceAsync(repositoryId, symbolId, cancellationToken), cancellationToken);

    public async ValueTask<CodeOccurrenceQueryResult> FindOccurrencesAsync(
        CodeOccurrenceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            ThrowIfUncertain();
            var refusal = await CheckOccurrenceAuthorityAsync(query, cancellationToken).ConfigureAwait(false);
            if (refusal is not null)
                return refusal;
            return await _inner.FindOccurrencesAsync(query, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private CodeGraphStoreHealth CheckHealth()
    {
        ThrowIfDisposed();
        var version = ReadSchemaVersion();
        var healthy = version == CurrentSchemaVersion;
        return new CodeGraphStoreHealth(
            healthy ? CodeGraphStoreHealthStatus.Healthy : CodeGraphStoreHealthStatus.Unhealthy,
            "lattice",
            healthy
                ? _nativeTraversal
                    ? "Native traversal mirrors are enabled and verified against the current publication on open."
                    : "Queries use the authoritative materialized projection; native traversal mirrors are disabled."
                : $"Schema version {version} is incompatible with expected version {CurrentSchemaVersion}.",
            version,
            Count(RepositoryLabel),
            Count(RunLabel),
            Count(UnitLabel));
    }

    public async ValueTask<CodeGraphStoreHealth> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return CheckHealth();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new CodeGraphStoreHealth(
                CodeGraphStoreHealthStatus.Unhealthy,
                "lattice",
                exception.Message);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public void Dispose()
    {
        _writeGate.Wait();
        try
        {
            if (_disposed)
                return;
            _database.Dispose();
            _disposed = true;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async ValueTask MutateAsync(PersistedCommand command, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            ThrowIfUncertain();
            var runKey = command.Run is null ? null : RunKey(command.Run);
            string? baseline = null;
            if (command is { Kind: "run", Run.Status: CodeIndexRunStatus.Running })
            {
                if (!_baselines.TryGetValue(runKey!, out baseline))
                {
                    baseline = (await _inner.GetLatestPublicationAsync(command.Run.RepositoryId, cancellationToken)
                        .ConfigureAwait(false))?.IndexRunId.Value;
                }
            }
            var next = Apply(_commands, command);

            // Apply to the in-memory projection first so ordinary validation
            // failures happen before any native write. The command log is not
            // advanced until the native write returns.
            try
            {
                await ApplyCommandAsync(_inner, command, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await RebuildInnerFromDurableStateAsync().ConfigureAwait(false);
                throw;
            }

            NativeGraphMirror? mirror;
            OccurrenceAuthorityPublication? authority;
            try
            {
                (mirror, authority) = TranslateNative(
                    () => Persist(command, baseline, cancellationToken),
                    "persist mutation");
            }
            catch (CodeGraphCommitOutcomeUnknownException)
            {
                // The write may have committed. Do not claim a rollback or
                // advance the command log; quarantine the instance instead.
                _outcomeUnknown = true;
                throw;
            }
            catch
            {
                // Known failure before commit: restore the projection from the
                // unchanged command log.
                await RebuildInnerFromDurableStateAsync().ConfigureAwait(false);
                throw;
            }

            _commands = next;
            if (command is { Kind: "run", Run.Status: CodeIndexRunStatus.Running })
                _baselines.TryAdd(runKey!, baseline);
            else if (command.Kind == "complete" ||
                     command is { Kind: "run", Run.Status: not CodeIndexRunStatus.Running })
                _baselines.Remove(runKey!);

            if (command.Kind == "complete" && command.Run is not null)
            {
                var repositoryId = command.Run.RepositoryId.Value;
                if (authority is null)
                    _authorityPublications.Remove(repositoryId);
                else
                    _authorityPublications[repositoryId] = authority;
                var mirrors = new Dictionary<string, NativeGraphMirror>(_mirrors, StringComparer.Ordinal);
                if (mirror is null)
                    mirrors.Remove(command.Run.RepositoryId.Value);
                else
                    mirrors[command.Run.RepositoryId.Value] = mirror;
                _mirrors = mirrors;
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async ValueTask<T> ReadAsync<T>(
        Func<InMemoryCodeGraphStore, ValueTask<T>> read,
        CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            ThrowIfUncertain();
            return await read(_inner).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private void RestoreBaselines(InMemoryCodeGraphStore store)
    {
        foreach (var (key, baseline) in _baselines)
        {
            var separator = key.IndexOf('\n');
            store.RestoreBaseline(key[..separator], key[(separator + 1)..], baseline);
        }
    }

    /// <summary>
    /// Rebuilds the in-memory projection from durable authority after a
    /// definitively resolved known-failure recovery.
    /// </summary>
    /// <remarks>
    /// NEW-01: replaying the graph command log alone leaves the occurrence
    /// projection (facts, coverage, availability and publication binding) empty,
    /// so the recovered instance disagreed with the durable occurrence tables.
    /// This performs the same sequence as initial open — command-log replay, then
    /// baseline and occurrence restoration from the authoritative native rows —
    /// so recovered in-memory state corresponds to the accepted durable
    /// publication. It reads only; it never mutates durable authority. The
    /// unknown-outcome/quarantine path is unaffected (it does not replay).
    /// </remarks>
    private async ValueTask RebuildInnerFromDurableStateAsync()
    {
        _inner = await ReplayAsync(_commands, CancellationToken.None).ConfigureAwait(false);
        RestoreBaselines(_inner);
        RestoreOccurrences(CancellationToken.None);
    }

    private void RestoreMirrors(string[] repositoryIds)
    {
        var mirrors = new Dictionary<string, NativeGraphMirror>(StringComparer.Ordinal);
        var existingMirrors = new Dictionary<string, NativeGraphMirror>(StringComparer.Ordinal);
        foreach (var (key, payload) in ReadPairs(NativeGraphMirror.MirrorLabel))
        {
            try
            {
                existingMirrors[key] = new NativeGraphMirror(NativeGraphMirror.ReadManifest(
                    JsonSerializer.Deserialize<string>(
                        Convert.FromBase64String(payload), SerializerOptions)
                    ?? throw new InvalidDataException()));
            }
            catch (Exception exception) when (exception is FormatException or JsonException or InvalidDataException)
            {
                // Derived mirror metadata is recoverable. Rebuild it from the
                // authoritative published units below.
            }
        }

        foreach (var repositoryId in repositoryIds)
        {
            var repository = new CodeRepositoryId(repositoryId);
            var publication = _inner.GetLatestPublicationAsync(repository, CancellationToken.None)
                .GetAwaiter().GetResult();
            if (publication is null)
                continue;
            var units = _inner.GetPublishedUnitsAsync(repository, CancellationToken.None)
                .GetAwaiter().GetResult();
            using var txn = _database.BeginWriteTransaction();
            try
            {
                existingMirrors.TryGetValue(repositoryId, out var existing);
                var mirror = TranslateNative(
                    () => NativeGraphMirror.Rebuild(txn, units, existing?.Manifest),
                    "rebuild graph mirror");
                Upsert(txn, NativeGraphMirror.MirrorLabel, repositoryId, Serialize(NativeGraphMirror.WriteManifest(mirror.Manifest)));
                txn.Commit();
                mirrors[repositoryId] = mirror;
            }
            catch
            {
                if (!txn.IsCompleted)
                    txn.Rollback();
                throw;
            }
        }

        _mirrors = mirrors;
    }

    private void RestoreOccurrences(CancellationToken cancellationToken)
    {
        var metaPairs = ReadPairs(OccurrenceMetaLabel);
        var unitPairs = ReadPairs(OccurrenceUnitLabel);
        var pagePairs = ReadPairs(OccurrencePageLabel);
        if (metaPairs.Count == 0)
        {
            _authorityPublications.Clear();
            if (unitPairs.Count != 0 || pagePairs.Count != 0)
            {
                throw new CodeOccurrenceSchemaException(
                    0,
                    CurrentOccurrenceSchemaVersion,
                    "Occurrence records exist without a schema manifest.");
            }
            // Graph-only history: occurrence analysis is unavailable, never empty-success.
            return;
        }

        var metas = new Dictionary<string, PersistedOccurrenceMeta>(StringComparer.Ordinal);
        foreach (var pair in metaPairs)
        {
            var meta = DeserializeOccurrence<PersistedOccurrenceMeta>(pair.Payload);
            if (meta.SchemaVersion != CurrentOccurrenceSchemaVersion ||
                !string.Equals(meta.Namespace, OccurrenceNamespace, StringComparison.Ordinal))
            {
                throw new CodeOccurrenceSchemaException(
                    meta.SchemaVersion,
                    CurrentOccurrenceSchemaVersion,
                    "The occurrence schema version or namespace is unsupported.");
            }
            metas[meta.RepositoryId] = meta;
        }

        var unitsByRepository =
            new Dictionary<string, List<PersistedOccurrenceUnit>>(StringComparer.Ordinal);
        foreach (var pair in unitPairs)
        {
            var unit = DeserializeOccurrence<PersistedOccurrenceUnit>(pair.Payload);
            unit = unit with { DurableKey = pair.Key };
            if (!metas.ContainsKey(unit.RepositoryId))
            {
                throw new CodeOccurrenceSchemaException(
                    0,
                    CurrentOccurrenceSchemaVersion,
                    "Occurrence unit records exist without a repository schema manifest.");
            }
            if (!unitsByRepository.TryGetValue(unit.RepositoryId, out var units))
            {
                units = [];
                unitsByRepository[unit.RepositoryId] = units;
            }
            units.Add(unit);
        }

        foreach (var pair in pagePairs)
        {
            var page = DeserializeOccurrence<PersistedOccurrencePage>(pair.Payload);
            if (!metas.TryGetValue(page.RepositoryId, out var meta) ||
                !string.Equals(
                    page.PublicationIndexRunId,
                    meta.PublicationIndexRunId,
                    StringComparison.Ordinal))
            {
                throw new CodeOccurrenceIndexException(
                    "An occurrence page is not bound to the accepted occurrence publication.");
            }
        }

        foreach (var meta in metas.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CodeOccurrenceCoverage? repositoryCoverage = null;
            if (unitsByRepository.TryGetValue(meta.RepositoryId, out var units))
            {
                foreach (var unit in units)
                {
                    var coverage = CoverageForRestore(unit);
                    if (coverage?.AnalysisPerformed == true)
                    {
                        repositoryCoverage = repositoryCoverage is null
                            ? coverage
                            : repositoryCoverage.Merge(coverage);
                    }
                    _inner.RestorePublishedOccurrencesAsync(
                            new CodeRepositoryId(unit.RepositoryId),
                            new CodePluginId(unit.PluginId),
                            unit.IndexUnitId,
                            unit.Facts,
                            unit.AnalysisIncluded,
                            coverage,
                            cancellationToken)
                        .GetAwaiter().GetResult();
                }
            }

            // Older schema-version-1 records have no completeness evidence. A
            // non-empty analysis is restored as Partial, never as complete.
            repositoryCoverage ??= meta.AnalysisIncluded
                ? new CodeOccurrenceCoverage(
                    meta.FactCount,
                    0,
                    CodeRelationshipCoverageState.Partial,
                    meta.ScopeId,
                    meta.UniverseId,
                    meta.UnitCount,
                    meta.UnitCount)
                : null;
            if (meta.CompleteCoverage && repositoryCoverage is not null)
                repositoryCoverage = repositoryCoverage.WithState(CodeRelationshipCoverageState.Produced);

            _inner.RestoreOccurrencePublication(
                new CodeRepositoryId(meta.RepositoryId),
                new CodeGraphPublication(
                    new CodeRepositoryId(meta.RepositoryId),
                    new CodeIndexRunId(meta.PublicationIndexRunId),
                    meta.SnapshotIdentity,
                    meta.IsConsistentSnapshot,
                    new CodeIndexIdentity(meta.IndexIdentity)),
                meta.AnalysisIncluded || repositoryCoverage?.AnalysisPerformed == true,
                repositoryCoverage);
        }
        // The additive carrier remains opaque during reconstruction. Malformed
        // carrier data is interpreted/refused at query time, not thrown here.
        _authorityPublications = metas.Values.ToDictionary(meta => meta.RepositoryId,
            meta => new OccurrenceAuthorityPublication(meta,
                unitsByRepository.GetValueOrDefault(meta.RepositoryId) ?? []), StringComparer.Ordinal);
    }

    private OccurrenceAuthorityPublication? PersistOccurrences(
        LatticeTransaction txn,
        CodeIndexRunManifest run,
        CodeRepositoryIndexState state)
    {
        var repositoryId = run.RepositoryId.Value;
        var unitPairs = ReadPairsInTxn(txn, OccurrenceUnitLabel);
        var units = unitPairs
            .Select(pair => DeserializeOccurrence<PersistedOccurrenceUnit>(pair.Payload))
            .Where(unit => string.Equals(unit.RepositoryId, repositoryId, StringComparison.Ordinal))
            .ToDictionary(OccurrenceUnitKey, StringComparer.Ordinal);
        foreach (var command in _commands.Where(command => MatchesRun(command, run)))
        {
            if (command.Kind == "stage-delete")
            {
                units.Remove(UnitKey(
                    command.RepositoryId!,
                    command.PluginId!,
                    command.UnitId!));
                continue;
            }

            if (command.Kind != "stage-replace")
                continue;
            var replacement = command.Replacement!;
            var key = UnitKey(replacement.Origin);
            if (TryGetCoverage(replacement, out var coverage))
            {
                units[key] = new PersistedOccurrenceUnit(
                    repositoryId,
                    replacement.Origin.PluginId.Value,
                    replacement.Origin.IndexUnitId.Value,
                    true,
                    replacement.Occurrences,
                    coverage,
                    replacement.OccurrenceAuthority is { } recorded
                        ? JsonSerializer.SerializeToElement(recorded, SerializerOptions) : null,
                    replacement.RequiresOccurrenceAuthority || replacement.OccurrenceAuthority is not null);
            }
            else
            {
                units.Remove(key);
            }
        }

        foreach (var key in units.Keys.ToArray())
        {
            var unit = units[key] with { DurableKey = key };
            units[key] = unit;
            if (unit.RequiresAuthority || unit.Authority is not null)
                units[key] = unit with { AuthorityPublicationIndexRunId = state.IndexRunId.Value };
        }
        var analysisIncluded = units.Values.Any(unit =>
            unit.AnalysisIncluded || unit.Facts.Count > 0 || unit.Coverage?.AnalysisPerformed == true);
        var repositoryCoverage = AggregateCoverage(units.Values);

        var unitKeys = unitPairs
            .Select(pair => pair.Key)
            .Where(key => key.StartsWith(repositoryId + "\n", StringComparison.Ordinal))
            .ToArray();
        if (unitKeys.Length > 0)
            DeleteMany(txn, OccurrenceUnitLabel, unitKeys);
        UpsertMany(
            txn,
            OccurrenceUnitLabel,
            units.Select(pair => (pair.Key, Serialize(pair.Value))).ToArray());

        var pagePairs = ReadPairsInTxn(txn, OccurrencePageLabel);
        var pageKeys = pagePairs
            .Select(pair => pair.Key)
            .Where(key => key.StartsWith(repositoryId + "\n", StringComparison.Ordinal))
            .ToArray();
        if (units.Count == 0 && !analysisIncluded)
        {
            if (pageKeys.Length > 0)
                DeleteMany(txn, OccurrencePageLabel, pageKeys);
            if (ReadPairsInTxn(txn, OccurrenceMetaLabel)
                .Any(pair => string.Equals(pair.Key, repositoryId, StringComparison.Ordinal)))
            {
                DeleteMany(txn, OccurrenceMetaLabel, [repositoryId]);
            }
            return null;
        }

        var pages = units.Values
            .SelectMany(unit => unit.Facts)
            .GroupBy(fact => fact.TargetId.Value, StringComparer.Ordinal)
            .Select(group => new PersistedOccurrencePage(
                repositoryId,
                group.Key,
                state.IndexRunId.Value,
                group
                    .OrderBy(fact => fact.Span.Path, StringComparer.Ordinal)
                    .ThenBy(fact => fact.Span.StartLine)
                    .ThenBy(fact => fact.Span.StartColumn)
                    .ThenBy(fact => fact.Span.EndLine)
                    .ThenBy(fact => fact.Span.EndColumn)
                    .ThenBy(fact => fact.Id.Value, StringComparer.Ordinal)
                    .Select(fact => fact.Id.Value)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();
        if (pageKeys.Length > 0)
            DeleteMany(txn, OccurrencePageLabel, pageKeys);
        UpsertMany(
            txn,
            OccurrencePageLabel,
            pages.Select(page => (
                repositoryId + "\n" + page.TargetId,
                Serialize(page))).ToArray());

        var meta = new PersistedOccurrenceMeta(
            CurrentOccurrenceSchemaVersion,
            OccurrenceNamespace,
            repositoryId,
            state.IndexRunId.Value,
            state.IndexIdentity.Value,
            state.SnapshotIdentity,
            state.IsConsistentSnapshot,
            units.Values.Sum(unit => unit.Facts.Count),
            units.Count,
            pages.Length,
            analysisIncluded,
            repositoryCoverage?.State ?? CodeRelationshipCoverageState.NotProduced,
            repositoryCoverage?.ScopeId,
            repositoryCoverage?.UniverseId,
            repositoryCoverage?.IsComplete == true,
            units.Values.Any(unit => unit.RequiresAuthority || unit.Authority is not null));
        Upsert(txn, OccurrenceMetaLabel, repositoryId, Serialize(meta));
        return new OccurrenceAuthorityPublication(meta, units.Values.ToArray());
    }

    private static bool TryGetCoverage(
        CodeIndexUnitReplacement replacement,
        out CodeOccurrenceCoverage coverage)
    {
        if (replacement.OccurrenceCoverage is { } declared)
        {
            coverage = declared.State == CodeRelationshipCoverageState.NotProduced &&
                (replacement.OccurrenceAnalysisIncluded || replacement.Occurrences.Count > 0)
                ? declared.WithState(CodeRelationshipCoverageState.Partial)
                : declared;
            return coverage.AnalysisPerformed;
        }

        if (replacement.OccurrenceAnalysisIncluded || replacement.Occurrences.Count > 0)
        {
            // Legacy contributions prove analysis ran, not that it was complete.
            coverage = new CodeOccurrenceCoverage(
                replacement.Occurrences.Count,
                0,
                CodeRelationshipCoverageState.Partial);
            return true;
        }

        coverage = new CodeOccurrenceCoverage();
        return false;
    }

    private static CodeOccurrenceCoverage? CoverageForRestore(PersistedOccurrenceUnit unit)
    {
        if (unit.Coverage is not null)
            return unit.Coverage;
        if (unit.AnalysisIncluded || unit.Facts.Count > 0)
        {
            return new CodeOccurrenceCoverage(
                unit.Facts.Count,
                0,
                CodeRelationshipCoverageState.Partial);
        }
        return null;
    }

    private static CodeOccurrenceCoverage? AggregateCoverage(
        IEnumerable<PersistedOccurrenceUnit> units)
    {
        CodeOccurrenceCoverage? result = null;
        foreach (var unit in units)
        {
            var coverage = CoverageForRestore(unit);
            if (coverage?.AnalysisPerformed != true)
                continue;
            result = result is null ? coverage : result.Merge(coverage);
        }
        return result;
    }

    private List<(string Key, string Payload)> ReadPairsInTxn(
        LatticeTransaction txn,
        string label)
    {
        using var query = _database.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
        using var result = query.Execute(txn);
        return result.ReadAll()
            .Select(row => (row.GetValue(0).AsString(), row.GetValue(1).AsString()))
            .ToList();
    }

    private static T DeserializeOccurrence<T>(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                Convert.FromBase64String(payload),
                SerializerOptions)
                ?? throw new InvalidDataException("Occurrence record payload is invalid.");
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or FormatException)
        {
            // A pre-REM record may contain a non-canonical occurrence/target
            // identity or an incompatible payload shape. Refuse it explicitly
            // rather than reinterpreting it as the corrected contract.
            throw new CodeOccurrenceSchemaException(
                0,
                CurrentOccurrenceSchemaVersion,
                "The occurrence payload is not canonical or is incompatible with the accepted occurrence contract.");
        }
    }

    private static string OccurrenceUnitKey(PersistedOccurrenceUnit unit) =>
        UnitKey(
            new CodeRepositoryId(unit.RepositoryId),
            new CodePluginId(unit.PluginId),
            new CodeIndexUnitId(unit.IndexUnitId));

    private (NativeGraphMirror? Mirror, OccurrenceAuthorityPublication? Authority) Persist(
        PersistedCommand command, string? baseline, CancellationToken cancellationToken)
    {
        NativeGraphMirror? mirror = null;
        OccurrenceAuthorityPublication? authority = null;
        cancellationToken.ThrowIfCancellationRequested();
        using var txn = _database.BeginWriteTransaction();
        try
        {
            switch (command.Kind)
            {
                case "repository":
                    Upsert(txn, RepositoryLabel, command.Repository!.Id.Value, Serialize(command.Repository));
                    break;
                case "run":
                    Upsert(txn, RunLabel, RunKey(command.Run!), Serialize(command.Run));
                    if (command.Run!.Status != CodeIndexRunStatus.Running)
                    {
                        DeleteStagedRun(txn, command.Run);
                        DeleteMany(txn, BaselineLabel, [RunKey(command.Run)]);
                    }
                    else
                    {
                        Upsert(txn, BaselineLabel, RunKey(command.Run), baseline ?? string.Empty);
                    }
                    break;
                case "complete":
                    PublishStagedRun(txn, command.Run!);
                    Upsert(txn, RunLabel, RunKey(command.Run!), Serialize(command.Run));
                    Upsert(txn, IndexStateLabel, command.State!.RepositoryId.Value, Serialize(command.State));
                    DeleteMany(txn, BaselineLabel, [RunKey(command.Run!)]);
                    authority = PersistOccurrences(txn, command.Run!, command.State!);
                    if (_nativeTraversal)
                    {
                        var completed = command.Run!;
                        var repositoryId = completed.RepositoryId.Value;
                        var published = Apply(_commands, command)
                            .Where(candidate => candidate.Kind == "replace" &&
                                candidate.Replacement!.Origin.RepositoryId == completed.RepositoryId)
                            .Select(candidate => candidate.Replacement!)
                            .ToList();
                        _mirrors.TryGetValue(repositoryId, out var existing);
                        mirror = NativeGraphMirror.Rebuild(txn, published, existing?.Manifest);
                        Upsert(
                            txn,
                            NativeGraphMirror.MirrorLabel,
                            repositoryId,
                            Serialize(NativeGraphMirror.WriteManifest(mirror.Manifest)));
                    }
                    else
                    {
                        DeleteMany(txn, NativeGraphMirror.MirrorLabel, [command.Run!.RepositoryId.Value]);
                    }

                    break;
                case "stage-replace":
                case "stage-delete":
                    Upsert(txn, StageLabel, StageKey(command), Serialize(command));
                    break;
                case "replace":
                    Upsert(txn, UnitLabel, UnitKey(command.Replacement!.Origin), Serialize(command.Replacement));
                    break;
                case "delete":
                    DeleteMany(txn, UnitLabel, [UnitKey(command.RepositoryId!, command.PluginId!, command.UnitId!)]);
                    break;
                default: throw new InvalidDataException($"Unknown persisted Hetu command '{command.Kind}'.");
            }
            _faultInjector?.Invoke("before-commit");
            var commitAttempted = true;
            try
            {
                txn.Commit();
                _faultInjector?.Invoke("after-commit");
            }
            catch (Exception exception)
            {
                if (!txn.IsCompleted)
                    txn.Rollback();
                if (commitAttempted)
                {
                    throw new CodeGraphCommitOutcomeUnknownException(
                        "The native publication commit outcome could not be established. " +
                        "The durable state must be corroborated before any retry or current-state claim.",
                        exception);
                }
                throw;
            }
            return (mirror, authority);
        }
        catch (CodeGraphCommitOutcomeUnknownException)
        {
            throw;
        }
        catch
        {
            if (!txn.IsCompleted)
                txn.Rollback();
            throw;
        }
    }

    private void PublishStagedRun(LatticeTransaction txn, CodeIndexRunManifest run)
    {
        var staged = _commands.Where(command => MatchesRun(command, run)).ToArray();
        var unitUpserts = staged
            .Where(command => command.Kind == "stage-replace")
            .Select(command => (UnitKey(command.Replacement!.Origin), Serialize(command.Replacement)))
            .ToArray();
        var unitDeletions = staged
            .Where(command => command.Kind == "stage-delete")
            .Select(command => UnitKey(command.RepositoryId!, command.PluginId!, command.UnitId!))
            .ToArray();
        UpsertMany(txn, UnitLabel, unitUpserts);
        DeleteMany(txn, UnitLabel, unitDeletions);
        DeleteMany(txn, StageLabel, staged.Select(StageKey).ToArray());
    }

    private void DeleteStagedRun(LatticeTransaction txn, CodeIndexRunManifest run)
    {
        DeleteMany(
            txn,
            StageLabel,
            _commands.Where(command => MatchesRun(command, run)).Select(StageKey).ToArray());
    }

    private void Upsert(LatticeTransaction txn, string label, string key, string payload) =>
        UpsertMany(txn, label, [(key, payload)]);

    private void UpsertMany(
        LatticeTransaction txn,
        string label,
        IReadOnlyList<(string Key, string Payload)> records)
    {
        if (records.Count == 0)
            return;
        // Native statement/parameter bounds are implementation-defined; write in
        // bounded chunks so large occurrence pages cannot overflow the planner.
        foreach (var chunk in records.Chunk(8))
        {
            DeleteMany(txn, label, chunk.Select(record => record.Key).ToArray());
            var statement = new StringBuilder();
            for (var index = 0; index < chunk.Length; index++)
                statement.Append($"CREATE (:{label} {{key: $k{index}, payload: $p{index}}}) ");
            using var query = _database.Prepare(statement.ToString());
            for (var index = 0; index < chunk.Length; index++)
            {
                query.Bind($"k{index}", LatticeValue.From(chunk[index].Key));
                query.Bind($"p{index}", LatticeValue.From(chunk[index].Payload));
            }
            using var result = query.Execute(txn);
            result.ReadAll();
        }
    }

    private void DeleteMany(LatticeTransaction txn, string label, IReadOnlyList<string> keys)
    {
        if (keys.Count == 0)
            return;
        using var query = _database.Prepare($"MATCH (s:{label}) WHERE s.key IN $keys DELETE s");
        query.Bind("keys", LatticeValue.From(keys.Select(LatticeValue.From).ToArray()));
        using var result = query.Execute(txn);
        result.ReadAll();
    }

    private void InitializeSchema() =>
        TranslateNative(() => InitializeSchemaCore(), "initialize schema");

    private void InitializeSchemaCore()
    {
        var existing = ReadMetadata();
        if (existing is null)
        {
            using var txn = _database.BeginWriteTransaction();
            try
            {
                var node = txn.CreateNode(MetadataLabel);
                txn.SetProperty(node, "key", LatticeValue.From(SchemaKey));
                txn.SetProperty(node, "schemaVersion", LatticeValue.From((long)CurrentSchemaVersion));
                txn.Commit();
            }
            catch
            {
                if (!txn.IsCompleted)
                    txn.Rollback();
                throw;
            }
            return;
        }

        if (existing.Value != CurrentSchemaVersion)
            throw new LatticeCodeGraphSchemaException(existing.Value, CurrentSchemaVersion);
    }

    private int? ReadMetadata() =>
        TranslateNative(() => ReadMetadataCore(), "read metadata");

    private int? ReadMetadataCore()
    {
        using var txn = _database.BeginReadTransaction();
        using var query = _database.Prepare($"MATCH (s:{MetadataLabel}) WHERE s.key = $key RETURN s.schemaVersion");
        query.Bind("key", LatticeValue.From(SchemaKey));
        using var result = query.Execute(txn);
        var found = result.MoveNext();
        var version = found ? (int)result.Current.GetValue(0).AsInt64() : (int?)null;
        txn.Commit();
        return version;
    }

    private int ReadSchemaVersion() => ReadMetadata() ?? CurrentSchemaVersion;

    private static T TranslateNative<T>(Func<T> action, string operation)
    {
        try
        {
            return action();
        }
        catch (LatticeException exception)
        {
            throw new CodeGraphStoreException(
                $"LatticeDB {operation} failed.",
                "lattice",
                exception.NativeErrorCode.ToString(),
                exception);
        }
    }

    private static void TranslateNative(Action action, string operation)
    {
        try
        {
            action();
        }
        catch (LatticeException exception)
        {
            throw new CodeGraphStoreException(
                $"LatticeDB {operation} failed.",
                "lattice",
                exception.NativeErrorCode.ToString(),
                exception);
        }
    }

    private List<(string Key, string Payload)> ReadPairs(string label) =>
        TranslateNative(() => ReadPairsCore(label), "read records");

    private List<(string Key, string Payload)> ReadPairsCore(string label)
    {
        IReadOnlyList<LatticeRow> rows;
        using (var txn = _database.BeginReadTransaction())
        {
            using var query = _database.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
            using var result = query.Execute(txn);
            rows = result.ReadAll();
            txn.Commit();
        }
        return rows
            .Select(row => (row.GetValue(0).AsString(), row.GetValue(1).AsString()))
            .OrderBy(pair => pair.Item1, StringComparer.Ordinal)
            .ToList();
    }

    private List<T> ReadTable<T>(string label) =>
        TranslateNative(() => ReadTableCore<T>(label), "read records");

    private List<T> ReadTableCore<T>(string label)
    {
        IReadOnlyList<LatticeRow> rows;
        using (var txn = _database.BeginReadTransaction())
        {
            using var query = _database.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
            using var result = query.Execute(txn);
            rows = result.ReadAll();
            txn.Commit();
        }
        return rows
            .Select(row => (
                Key: row.GetValue(0).AsString(),
                Value: JsonSerializer.Deserialize<T>(
                    Convert.FromBase64String(row.GetValue(1).AsString()), SerializerOptions)
                    ?? throw new InvalidDataException($"Lattice {label} payload is invalid.")))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToList();
    }

    private int Count(string label) =>
        TranslateNative(() => CountCore(label), "count records");

    private int CountCore(string label)
    {
        using var txn = _database.BeginReadTransaction();
        using var query = _database.Prepare($"MATCH (s:{label}) RETURN count(s)");
        using var result = query.Execute(txn);
        var rows = result.ReadAll();
        txn.Commit();
        return rows.Count == 0 ? 0 : (int)rows[0].GetValue(0).AsInt64();
    }

    private static LatticeDbSharp.LatticeDatabaseOptions ToNativeOptions(LatticeDbStoreOptions? options)
    {
        if (options is null)
            return new LatticeDbSharp.LatticeDatabaseOptions { Create = true };
        var defaults = new LatticeDbSharp.LatticeDatabaseOptions();
        return new LatticeDbSharp.LatticeDatabaseOptions
        {
            Create = true,
            CacheSizeMb = options.CacheSizeMb ?? defaults.CacheSizeMb,
            PageSize = options.PageSize ?? defaults.PageSize,
            EnableWal = options.EnableWal ?? defaults.EnableWal,
            EnableAdjacencyCache = options.EnableAdjacencyCache ?? defaults.EnableAdjacencyCache
        };
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Refuses current-state claims after a native write whose commit outcome
    /// could not be established. Reopening the store reconstructs the durable
    /// accepted state and clears the uncertainty; blind retry on this instance
    /// is not permitted.
    /// </summary>
    private void ThrowIfUncertain()
    {
        if (_outcomeUnknown)
        {
            throw new CodeGraphCommitOutcomeUnknownException(
                "A native publication commit outcome is unknown. " +
                "Refusing current-state claims and mutations until the durable " +
                "publication is corroborated by reopening the store.");
        }
    }
}

public sealed class LatticeCodeGraphSchemaException : Exception
{
    public LatticeCodeGraphSchemaException(int actualVersion, int expectedVersion)
        : base($"Lattice Hetu schema version {actualVersion} is incompatible with expected version {expectedVersion}.")
    {
        ActualVersion = actualVersion;
        ExpectedVersion = expectedVersion;
    }

    public int ActualVersion { get; }
    public int ExpectedVersion { get; }
}

/// <summary>Internal durable record: one unit's published occurrence contribution.</summary>
internal sealed record PersistedOccurrenceUnit(
    string RepositoryId,
    string PluginId,
    string IndexUnitId,
    bool AnalysisIncluded,
    IReadOnlyList<CodeOccurrenceFact> Facts,
    CodeOccurrenceCoverage? Coverage = null,
    JsonElement? Authority = null,
    bool RequiresAuthority = false,
    string? AuthorityPublicationIndexRunId = null)
{
    [System.Text.Json.Serialization.JsonIgnore]
    internal string? DurableKey { get; init; }
}

/// <summary>Internal durable record: the accepted occurrence schema/publication manifest.</summary>
internal sealed record PersistedOccurrenceMeta(
    int SchemaVersion,
    string Namespace,
    string RepositoryId,
    string PublicationIndexRunId,
    string IndexIdentity,
    string? SnapshotIdentity,
    bool IsConsistentSnapshot,
    int FactCount,
    int UnitCount,
    int PageCount,
    bool AnalysisIncluded,
    CodeRelationshipCoverageState CoverageState = CodeRelationshipCoverageState.NotProduced,
    string? ScopeId = null,
    string? UniverseId = null,
    bool CompleteCoverage = false,
    bool RequiresAuthority = false);

/// <summary>Internal durable record: one target-index page.</summary>
internal sealed record PersistedOccurrencePage(
    string RepositoryId,
    string TargetId,
    string PublicationIndexRunId,
    IReadOnlyList<string> OccurrenceIds);

/// <summary>
/// A native mutation's commit outcome is unknown. The store refuses
/// current-state claims until durable state is corroborated by reopening.
/// </summary>
public sealed class CodeGraphCommitOutcomeUnknownException : Exception
{
    public CodeGraphCommitOutcomeUnknownException(string message)
        : base(message) { }

    public CodeGraphCommitOutcomeUnknownException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>An occurrence format is unsupported or inconsistent with its schema.</summary>
internal sealed class CodeOccurrenceSchemaException : Exception
{
    public CodeOccurrenceSchemaException(
        int actualVersion,
        int expectedVersion,
        string message)
        : base(message)
    {
        ActualVersion = actualVersion;
        ExpectedVersion = expectedVersion;
    }

    public int ActualVersion { get; }
    public int ExpectedVersion { get; }
}

/// <summary>The durable occurrence index is internally inconsistent.</summary>
internal sealed class CodeOccurrenceIndexException : Exception
{
    public CodeOccurrenceIndexException(string message)
        : base(message) { }
}

