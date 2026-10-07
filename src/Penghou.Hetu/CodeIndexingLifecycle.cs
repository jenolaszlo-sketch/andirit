using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;

namespace Penghou.Hetu;

/// <summary>Bounds and configures one repository indexing lifecycle.</summary>
public sealed record CodeIndexingOptions
{
    public CodeIndexingOptions(
        CodeIndexPlanningOptions? planning = null,
        CodeGraphBatchLimits? batchLimits = null,
        int maxConcurrentPlugins = 1,
        long maxSourceBytes = 16 * 1024 * 1024,
        long maxTotalSourceBytes = 512 * 1024 * 1024,
        ProviderFallbackPolicy fallbackPolicy = ProviderFallbackPolicy.OnUnavailable)
    {
        if (maxConcurrentPlugins < 1)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrentPlugins));
        if (maxSourceBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(maxSourceBytes));
        if (maxTotalSourceBytes < maxSourceBytes)
            throw new ArgumentOutOfRangeException(nameof(maxTotalSourceBytes));
        if (!Enum.IsDefined(fallbackPolicy))
            throw new ArgumentOutOfRangeException(nameof(fallbackPolicy));
        Planning = planning ?? new CodeIndexPlanningOptions();
        BatchLimits = batchLimits ?? new CodeGraphBatchLimits();
        MaxConcurrentPlugins = maxConcurrentPlugins;
        MaxSourceBytes = maxSourceBytes;
        MaxTotalSourceBytes = maxTotalSourceBytes;
        FallbackPolicy = fallbackPolicy;
    }

    public CodeIndexPlanningOptions Planning { get; }
    public CodeGraphBatchLimits BatchLimits { get; }
    public int MaxConcurrentPlugins { get; }
    public long MaxSourceBytes { get; }
    public long MaxTotalSourceBytes { get; }
    /// <summary>
    /// Controls whether a failed preferred provider may fall back to the
    /// next configured language provider. Conservative by default: only
    /// unavailable capability falls back, never suspicious analysis.
    /// </summary>
    public ProviderFallbackPolicy FallbackPolicy { get; }
}

/// <summary>
/// Decides which provider failures may fall back to the next configured
/// language provider. Fallback handles unavailable capability, not
/// suspicious analysis: a project-load or parse problem is evidence about
/// the analysis environment and must stay visible by default.
/// </summary>
public enum ProviderFallbackPolicy
{
    /// <summary>Never fall back; preferred-provider failures fail the run.</summary>
    None = 0,
    /// <summary>Fall back only when the capability is unavailable.</summary>
    OnUnavailable = 1,
    /// <summary>Fall back on unavailable capability and on analysis failures.</summary>
    OnFailure = 2
}

/// <summary>Fallback eligibility for classified provider failures.</summary>
public static class ProviderFallbackPolicyExtensions
{
    /// <summary>
    /// Reports whether a failure kind may fall back under a policy.
    /// Cancellation and live-source conflicts never permit fallback.
    /// </summary>
    public static bool IsFallbackEligible(
        this ProviderFallbackPolicy policy,
        CodePluginFailureKind kind)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy));
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        return policy switch
        {
            ProviderFallbackPolicy.None => false,
            ProviderFallbackPolicy.OnUnavailable => kind is
                CodePluginFailureKind.UnsupportedLanguage or
                CodePluginFailureKind.ProviderUnavailable or
                CodePluginFailureKind.VersionMismatch,
            ProviderFallbackPolicy.OnFailure => kind is not
                CodePluginFailureKind.Cancelled and not
                CodePluginFailureKind.SourceChanged,
            _ => false
        };
    }

    /// <summary>Maps a failure kind to its stable privacy-safe warning code.</summary>
    public static string ToWarningCode(this CodePluginFailureKind kind) =>
        kind switch
        {
            CodePluginFailureKind.UnsupportedLanguage => "plugin.unsupported-language",
            CodePluginFailureKind.ProviderUnavailable => "plugin.unavailable",
            CodePluginFailureKind.VersionMismatch => "plugin.version-mismatch",
            CodePluginFailureKind.ProjectLoadFailure => "plugin.project-load-failure",
            CodePluginFailureKind.ParseFailure => "plugin.parse-failure",
            CodePluginFailureKind.SemanticModelUnavailable => "plugin.semantic-unavailable",
            CodePluginFailureKind.ProviderCrash => "plugin.failed",
            CodePluginFailureKind.PartialResult => "plugin.partial-result",
            CodePluginFailureKind.Cancelled => "plugin.cancelled",
            CodePluginFailureKind.SourceChanged => "plugin.source-changed",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}

/// <summary>
/// Records one language-provider selection event: every attempted provider,
/// the selected provider if any, the classified reason, and the capability
/// cost. Attempted is an ordered history so three-candidate orders stay
/// representable; it is never reduced to a single requested/actual pair.
/// </summary>
public sealed record CodeProviderSelection
{
    public CodeProviderSelection(
        string language,
        CodePluginId preferred,
        IReadOnlyList<CodePluginId> attempted,
        CodePluginId? selected,
        CodePluginFailureKind? failureKind,
        string reason,
        CodeGraphCapabilities lostCapabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language, nameof(language));
        ArgumentNullException.ThrowIfNull(preferred);
        ArgumentNullException.ThrowIfNull(attempted);
        if (attempted.Count == 0)
            throw new ArgumentException(
                "A selection event must record at least one attempt.",
                nameof(attempted));
        if (attempted.Any(id => id is null))
            throw new ArgumentException(
                "Selection attempts cannot contain null identities.",
                nameof(attempted));
        if (failureKind is not null && !Enum.IsDefined(failureKind.Value))
            throw new ArgumentOutOfRangeException(nameof(failureKind));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        Language = language;
        Preferred = preferred;
        Attempted = attempted.ToArray();
        Selected = selected;
        FailureKind = failureKind;
        Reason = reason;
        LostCapabilities = lostCapabilities;
    }

    public string Language { get; }
    public CodePluginId Preferred { get; }
    public IReadOnlyList<CodePluginId> Attempted { get; }
    public CodePluginId? Selected { get; }
    public CodePluginFailureKind? FailureKind { get; }
    public string Reason { get; }
    /// <summary>
    /// Capabilities the preferred provider had that the selected provider
    /// lacks. Evidence quality is deliberately not reduced to a transition:
    /// fallback can also remove entire capabilities.
    /// </summary>
    public CodeGraphCapabilities LostCapabilities { get; }
}

/// <summary>Privacy-safe measurements for one indexing attempt.</summary>
public sealed record CodeIndexingDiagnostics(
    CodeRepositoryId RepositoryId,
    CodeIndexRunId IndexRunId,
    CodeIndexRunStatus Status,
    int FilesDiscovered,
    int FilesNew,
    int FilesChanged,
    int FilesUnchanged,
    int FilesDeleted,
    int FilesUnsupported,
    int DirectoriesExcluded,
    int DirectoriesDepthLimited,
    int ReparsePointsSkipped,
    long SourceBytesRead,
    int PluginsExecuted,
    int IndexUnitsCompleted,
    int IndexUnitsDeleted,
    int NodesProduced,
    int DeclarationsProduced,
    int EdgesProduced,
    int RejectedFacts,
    int UnresolvedRelationships,
    TimeSpan PlanningDuration,
    TimeSpan ExtractionDuration,
    TimeSpan PersistenceDuration,
    IReadOnlyList<CodePluginIndexingDiagnostics> Plugins,
    IReadOnlyList<string> WarningCodes,
    IReadOnlyList<CodeProviderSelection> ProviderSelections);

public enum CodePluginIndexingStatus
{
    Completed = 0,
    Failed = 1,
    Cancelled = 2
}

/// <summary>Privacy-safe measurements for one plugin execution.</summary>
public sealed record CodePluginIndexingDiagnostics(
    CodePluginId PluginId,
    string PluginVersion,
    CodePluginIndexingStatus Status,
    TimeSpan Duration,
    int SourcesSupplied,
    int SourcesExamined,
    int SourcesContributingFacts,
    int UnresolvedRelationships,
    int ObsoleteIndexUnits,
    IReadOnlyList<string> WarningCodes);

/// <summary>Result and exact publication receipt of one completed indexing lifecycle.</summary>
public sealed record CodeIndexingResult(
    CodeIndexPlan Plan,
    CodeIndexingDiagnostics Diagnostics,
    CodeGraphPublication Publication,
    CodeRepositoryIndexState PublishedState);

/// <summary>Coordinates repository discovery, plugins, ingestion, and durable run state.</summary>
public sealed class CodeIndexingService
{
    private readonly CodeRepositoryProviderRegistry _repositories;
    private readonly CodeGraphPluginRegistry _plugins;
    private readonly ICodeGraphStore _store;
    private readonly TimeProvider _timeProvider;

    public CodeIndexingService(
        CodeRepositoryProviderRegistry repositories,
        CodeGraphPluginRegistry plugins,
        ICodeGraphStore store,
        TimeProvider? timeProvider = null)
    {
        _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        _plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Compares live repository sources with the latest published source state
    /// without staging or publishing anything. <see cref="CodeFreshnessStatus.Unknown"/>
    /// means the repository was never indexed; <see cref="CodeFreshnessStatus.SourceConflict"/>
    /// means live sources changed underneath the check itself.
    /// </summary>
    public async ValueTask<CodeFreshnessResult> CheckFreshnessAsync(
        CodeRepositoryDescriptor descriptor,
        CodeIndexingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        options ??= new CodeIndexingOptions();
        await using var repository = await _repositories.OpenAsync(descriptor, cancellationToken)
            .ConfigureAwait(false);
        var previousState = await _store.GetLatestIndexStateAsync(descriptor.Id, cancellationToken)
            .ConfigureAwait(false);
        if (previousState is null)
            return new(CodeFreshnessStatus.Unknown, null, 0, 0, 0, 0);
        var publication = new CodeGraphPublication(
            previousState.RepositoryId,
            previousState.IndexRunId,
            previousState.SnapshotIdentity,
            previousState.IsConsistentSnapshot,
            previousState.IndexIdentity);
        var planner = new CodeIndexPlanner(_plugins);
        CodeIndexPlan plan;
        try
        {
            plan = await planner.CreatePlanAsync(
                repository,
                previousState.Sources,
                new CodeIndexPlanningOptions(
                    options.Planning.Enumeration,
                    options.Planning.PluginIds,
                    options.MaxSourceBytes,
                    options.MaxTotalSourceBytes),
                cancellationToken).ConfigureAwait(false);
        }
        catch (CodeSourceChangedDuringIndexingException)
        {
            return new(CodeFreshnessStatus.SourceConflict, publication, 0, 0, 0, 0);
        }
        var changed = 0;
        var unchanged = 0;
        var added = 0;
        var deleted = 0;
        foreach (var item in plan.Items)
        {
            switch (item.Status)
            {
                case CodeIndexPlanStatus.New: added++; break;
                case CodeIndexPlanStatus.Changed: changed++; break;
                case CodeIndexPlanStatus.Unchanged: unchanged++; break;
                case CodeIndexPlanStatus.Deleted: deleted++; break;
            }
        }
        return new(
            added + changed + deleted > 0 ? CodeFreshnessStatus.Stale : CodeFreshnessStatus.Fresh,
            publication,
            added,
            changed,
            unchanged,
            deleted);
    }

    public async ValueTask<CodeIndexingResult> IndexAsync(
        CodeRepositoryDescriptor descriptor,
        CodeIndexRunId runId,
        CodeIndexingOptions? options = null,
        Action<CodeIndexingDiagnostics>? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(runId);
        options ??= new CodeIndexingOptions();
        var startedAt = _timeProvider.GetUtcNow();
        CodeIndexPlan plan;
        var ingestion = new ConcurrentBag<CodeGraphIngestionDiagnostics>();
        var pluginDiagnostics = new ConcurrentBag<CodePluginIndexingDiagnostics>();
        var selections = new List<CodeProviderSelection>();
        var extractionDuration = TimeSpan.Zero;
        var persistenceDuration = TimeSpan.Zero;
        var pluginsExecuted = 0;
        var unitsDeleted = 0;
        long sourceBytesRead = 0;

        await using var repository = await _repositories.OpenAsync(descriptor, cancellationToken)
            .ConfigureAwait(false);
        var existingRepository = await _store.GetRepositoryAsync(descriptor.Id, cancellationToken)
            .ConfigureAwait(false);
        await _store.UpsertRepositoryAsync(
            new CodeRepositoryManifest(
                descriptor.Id,
                existingRepository?.DisplayName,
                descriptor.Location,
                existingRepository?.RegisteredAt),
            cancellationToken).ConfigureAwait(false);

        var previousState = await _store.GetLatestIndexStateAsync(descriptor.Id, cancellationToken)
            .ConfigureAwait(false);
        var planner = new CodeIndexPlanner(_plugins);
        var planningStarted = Stopwatch.GetTimestamp();
        var planningOptions = new CodeIndexPlanningOptions(
            options.Planning.Enumeration,
            options.Planning.PluginIds,
            options.MaxSourceBytes,
            options.MaxTotalSourceBytes);
        plan = await planner.CreatePlanAsync(
            repository,
            previousState?.Sources,
            planningOptions,
            cancellationToken).ConfigureAwait(false);
        var planningDuration = Stopwatch.GetElapsedTime(planningStarted);
        var selectedPlugins = SelectPlugins(plan, options.Planning);
        var executingPlugins = selectedPlugins
            .Where(plugin => plan.Items.Any(item =>
                item.Manifest.PluginId == plugin.Id &&
                item.Status != CodeIndexPlanStatus.Unchanged))
            .ToArray();
        if (executingPlugins.Length == 0 && previousState is not null)
        {
            // Nothing changed: reuse the existing publication instead of
            // minting a run that would supersede concurrent runs for no
            // content change.
            var unchanged = CreateDiagnostics(
                descriptor.Id, runId, CodeIndexRunStatus.Completed, plan, ingestion,
                0, 0, plan.HashBytesRead,
                planningDuration, TimeSpan.Zero, TimeSpan.Zero,
                pluginDiagnostics);
            Report(diagnostics, unchanged);
            return new(
                plan,
                unchanged,
                new CodeGraphPublication(
                    previousState.RepositoryId,
                    previousState.IndexRunId,
                    previousState.SnapshotIdentity,
                    previousState.IsConsistentSnapshot,
                    previousState.IndexIdentity),
                previousState);
        }
        var publishedUnits = await _store.GetPublishedUnitsAsync(descriptor.Id, cancellationToken)
            .ConfigureAwait(false);
        // The run manifest names every plugin that may contribute staged
        // facts: executing plugins plus their transitive fallback candidates.
        // Stores validate staged ownership against this set and require the
        // completed manifest to name the same set, so fallback eligibility
        // must be declared here rather than amended mid-run. Diagnostics and
        // selection events record who actually ran.
        var runPlugins = executingPlugins
            .SelectMany(FallbackClosureIds)
            .Distinct()
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .ToArray();
        var running = new CodeIndexRunManifest(
            descriptor.Id,
            runId,
            startedAt,
            plugins: runPlugins);
        await _store.StoreIndexRunAsync(running, cancellationToken).ConfigureAwait(false);

        try
        {
            var extractionStarted = Stopwatch.GetTimestamp();
            var materialized = await MaterializeSourcesAsync(
                repository,
                plan,
                executingPlugins,
                options,
                cancellationToken).ConfigureAwait(false);
            sourceBytesRead = plan.HashBytesRead +
                materialized.Values.Distinct().Sum(source => (long)source.Length);
            var successes = new ConcurrentBag<(ICodeGraphPlugin Plugin, CodeGraphExtractionResult Result)>();
            var completedRunnerIds = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
            var runnerUnits = new ConcurrentDictionary<string, ConcurrentBag<CodeIndexUnitId>>(StringComparer.Ordinal);
            var absorbedBy = new Dictionary<string, string>(StringComparer.Ordinal);
            var failedEver = new HashSet<string>(StringComparer.Ordinal);
            var unrecovered = new List<(string PluginId, Exception Failure)>();

            IReadOnlyList<CodeIndexPlanItem> ItemsFor(ICodeGraphPlugin plugin) =>
                plan.Items.Where(item => item.Manifest.PluginId == plugin.Id).ToArray();

            void RecordUnits(ICodeGraphPlugin plugin, IEnumerable<CodeGraphIngestionDiagnostics> diags)
            {
                var bag = runnerUnits.GetOrAdd(plugin.Id.Value, _ => new ConcurrentBag<CodeIndexUnitId>());
                foreach (var diag in diags)
                    bag.Add(diag.IndexUnitId);
            }

            async ValueTask<PluginExecution> ExecuteOnceAsync(
                ICodeGraphPlugin plugin,
                IReadOnlyList<CodeIndexPlanItem> items)
            {
                var started = Stopwatch.GetTimestamp();
                var ownIngestion = new List<CodeGraphIngestionDiagnostics>();
                await using var sink = new CodeGraphIngestionSink(
                    _store,
                    options.BatchLimits,
                    onCompleted: ownIngestion.Add);
                CodeGraphPluginContext? context = null;
                try
                {
                    context = CreateContextForItems(
                        descriptor, runId, plugin, items, materialized, previousState, publishedUnits);
                    await using var session = await plugin.CreateSessionAsync(context, cancellationToken)
                        .ConfigureAwait(false);
                    var scopedSink = new PluginScopedSink(
                        sink, descriptor.Id, runId, plugin.Id, plugin.Version);
                    var result = await session.ExtractAsync(scopedSink, cancellationToken)
                        .ConfigureAwait(false)
                        ?? throw new InvalidOperationException(
                            $"Plugin '{plugin.Id}' returned a null extraction result.");
                    await sink.CompleteAsync(cancellationToken).ConfigureAwait(false);
                    foreach (var diag in ownIngestion)
                        ingestion.Add(diag);
                    RecordUnits(plugin, ownIngestion);
                    pluginDiagnostics.Add(CreatePluginDiagnostics(
                        plugin,
                        CodePluginIndexingStatus.Completed,
                        Stopwatch.GetElapsedTime(started),
                        context.Sources.Count,
                        result));
                    Interlocked.Increment(ref pluginsExecuted);
                    completedRunnerIds.TryAdd(plugin.Id.Value, 0);
                    return new PluginExecution(plugin, items, result, null);
                }
                catch (Exception exception)
                {
                    foreach (var diag in ownIngestion)
                        ingestion.Add(diag);
                    RecordUnits(plugin, ownIngestion);
                    var kind = ClassifyPluginFailure(exception);
                    pluginDiagnostics.Add(new(
                        plugin.Id,
                        plugin.Version,
                        exception is OperationCanceledException
                            ? CodePluginIndexingStatus.Cancelled
                            : CodePluginIndexingStatus.Failed,
                        Stopwatch.GetElapsedTime(started),
                        context?.Sources.Count ?? 0,
                        0,
                        0,
                        0,
                        0,
                        [kind.ToWarningCode()]));
                    failedEver.Add(plugin.Id.Value);
                    return new PluginExecution(plugin, items, null, exception);
                }
            }

            // Phase 1 runs every executing plugin that is not itself a fallback
            // target of another executing plugin. Fallback targets are deferred
            // to phase 2 so a takeover executes once over the union of sources:
            // index units are plugin-defined, and restaging the same units from
            // a second execution could clobber already-staged facts.
            var deferred = FindDeferredPluginIds(executingPlugins);
            using var concurrency = new SemaphoreSlim(options.MaxConcurrentPlugins);
            var phaseOne = executingPlugins.Where(plugin => !deferred.Contains(plugin.Id.Value)).ToArray();
            var phaseOneExecutions = new ConcurrentBag<PluginExecution>();
            var phaseOneTasks = phaseOne.Select(async plugin =>
            {
                await concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    phaseOneExecutions.Add(await ExecuteOnceAsync(plugin, ItemsFor(plugin)).ConfigureAwait(false));
                }
                finally
                {
                    concurrency.Release();
                }
            }).ToArray();
            await Task.WhenAll(phaseOneTasks).ConfigureAwait(false);
            var phaseOneFailures = new List<PluginExecution>();
            foreach (var execution in phaseOneExecutions.OrderBy(
                         execution => execution.Plugin.Id.Value,
                         StringComparer.Ordinal))
            {
                if (execution.Result is null)
                    phaseOneFailures.Add(execution);
                else
                    successes.Add((execution.Plugin, execution.Result));
            }

            // Phase 2 recovers failures through configured fallback providers and
            // runs deferred plugins (with any absorbed takeovers). Sequential and
            // deterministic: plugin-identity order decides every sequence.
            var pendingIdle = new SortedDictionary<string, (ICodeGraphPlugin Plugin, IReadOnlyList<CodeIndexPlanItem> Items)>(
                StringComparer.Ordinal);
            foreach (var plugin in executingPlugins.Where(plugin => deferred.Contains(plugin.Id.Value)))
                pendingIdle[plugin.Id.Value] = (plugin, ItemsFor(plugin));

            async ValueTask DeletePluginFootprintAsync(ICodeGraphPlugin plugin)
            {
                var doomed = new HashSet<CodeIndexUnitId>();
                if (runnerUnits.TryGetValue(plugin.Id.Value, out var staged))
                {
                    foreach (var unitId in staged)
                        doomed.Add(unitId);
                }
                foreach (var unit in publishedUnits.Where(unit => unit.Origin.PluginId == plugin.Id))
                    doomed.Add(unit.Origin.IndexUnitId);
                foreach (var unitId in doomed.OrderBy(id => id.Value, StringComparer.Ordinal))
                {
                    await _store.StageIndexUnitDeletionAsync(
                        descriptor.Id,
                        runId,
                        plugin.Id,
                        unitId,
                        cancellationToken).ConfigureAwait(false);
                    unitsDeleted++;
                }
            }

            // A takeover chain: every link runs at most once over the union of
            // its own and absorbed sources. Takeover invalidates the previous
            // owner's units (deleted before the attempt), so the runner must
            // emit complete facts for every supplied source — even ones flagged
            // Unchanged against the previous owner's history. Change flags stay
            // honest transitions; completeness is the runner's duty.
            async ValueTask RunTakeoverChainAsync(
                ICodeGraphPlugin failed,
                IReadOnlyList<CodeIndexPlanItem> takenItems,
                Exception failure)
            {
                var attempts = new List<CodePluginId> { failed.Id };
                var origins = new List<string> { failed.Id.Value };
                var current = failed;
                var currentItems = takenItems;
                var currentFailure = failure;
                while (true)
                {
                    var kind = ClassifyPluginFailure(currentFailure);
                    // Scan forward past runners that already failed or completed
                    // in this run: every runner executes at most once, so an
                    // exhausted runner is skipped rather than retried, and the
                    // chain keeps a later eligible provider reachable. The walk
                    // is bounded (visited guard) and deterministic (map order).
                    ICodeGraphPlugin? next = null;
                    {
                        var seen = new HashSet<string>(StringComparer.Ordinal) { current.Id.Value };
                        var probe = current;
                        while (next is null &&
                            _plugins.TryGetFallback(probe, out var candidate) &&
                            candidate is not null &&
                            seen.Add(candidate.Id.Value))
                        {
                            if (!failedEver.Contains(candidate.Id.Value) &&
                                !completedRunnerIds.ContainsKey(candidate.Id.Value))
                                next = candidate;
                            else
                                probe = candidate;
                        }
                    }
                    var hasNext = next is not null &&
                        options.FallbackPolicy.IsFallbackEligible(kind);
                    IReadOnlyList<string> takePaths = hasNext
                        ? currentItems
                            .Where(item => item.Source is not null)
                            .Select(item => item.Manifest.SourcePath)
                            .ToArray()
                        : [];
                    if (hasNext && !takePaths.All(next!.CanHandle))
                        hasNext = false;
                    if (!hasNext)
                    {
                        if (attempts.Count > 1)
                        {
                            selections.Add(new CodeProviderSelection(
                                failed.Language,
                                failed.Id,
                                attempts,
                                null,
                                kind,
                                kind.ToWarningCode(),
                                CodeGraphCapabilities.None));
                        }
                        unrecovered.Add((failed.Id.Value, currentFailure));
                        return;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var runner = next!;
                    if (pendingIdle.Remove(runner.Id.Value, out var idle))
                        currentItems = UnionItems(currentItems, idle.Items);
                    await DeletePluginFootprintAsync(current).ConfigureAwait(false);
                    var execution = await ExecuteOnceAsync(runner, currentItems).ConfigureAwait(false);
                    if (execution.Result is not null)
                    {
                        foreach (var origin in origins)
                            absorbedBy[origin] = runner.Id.Value;
                        attempts.Add(runner.Id);
                        selections.Add(new CodeProviderSelection(
                            failed.Language,
                            failed.Id,
                            attempts,
                            runner.Id,
                            kind,
                            kind.ToWarningCode(),
                            failed.Capabilities & ~runner.Capabilities));
                        successes.Add((runner, execution.Result));
                        return;
                    }

                    attempts.Add(runner.Id);
                    origins.Add(runner.Id.Value);
                    current = runner;
                    currentFailure = execution.Failure!;
                }
            }

            foreach (var failure in phaseOneFailures)
                await RunTakeoverChainAsync(failure.Plugin, failure.Items, failure.Failure!).ConfigureAwait(false);
            foreach (var idleId in pendingIdle.Keys.ToArray())
            {
                if (!pendingIdle.TryGetValue(idleId, out var idle))
                    continue;
                pendingIdle.Remove(idleId);
                var execution = await ExecuteOnceAsync(idle.Plugin, idle.Items).ConfigureAwait(false);
                if (execution.Result is null)
                    await RunTakeoverChainAsync(idle.Plugin, idle.Items, execution.Failure!).ConfigureAwait(false);
                else
                    successes.Add((idle.Plugin, execution.Result));
            }
            if (unrecovered.Count > 0)
            {
                var fatal = unrecovered
                    .OrderBy(entry => entry.PluginId, StringComparer.Ordinal)
                    .First();
                ExceptionDispatchInfo.Capture(fatal.Failure).Throw();
            }

            var finalPlan = ApplyAbsorbedOwners(plan, absorbedBy, _plugins);
            extractionDuration = Stopwatch.GetElapsedTime(extractionStarted);

            var persistenceStarted = Stopwatch.GetTimestamp();
            var stagedDeletions = new HashSet<(string PluginId, string UnitId)>();
            foreach (var (plugin, result) in successes.OrderBy(result => result.Plugin.Id.Value, StringComparer.Ordinal))
            {
                foreach (var unitId in result.ObsoleteIndexUnits)
                {
                    if (stagedDeletions.Add((plugin.Id.Value, unitId.Value)))
                    {
                        await _store.StageIndexUnitDeletionAsync(
                            descriptor.Id,
                            runId,
                            plugin.Id,
                            unitId,
                            cancellationToken).ConfigureAwait(false);
                        unitsDeleted++;
                    }
                }
            }

            var completedAt = _timeProvider.GetUtcNow();
            var completed = new CodeIndexRunManifest(
                descriptor.Id,
                runId,
                startedAt,
                CodeIndexRunStatus.Completed,
                completedAt,
                runPlugins);
            var finalSelectedPlugins = SelectPlugins(finalPlan, options.Planning);
            var state = CreateState(descriptor.Id, runId, repository, previousState, finalPlan, finalSelectedPlugins);
            // Cross-run ownership hygiene: a published unit whose owner holds
            // no path in the new state is displaced. The takeover path is one
            // producer (a recovered preferred provider displacing fallback
            // units); obsolete-unit reporting remains each plugin's own
            // responsibility and wins the dedupe above. Project-scoped units of
            // a plugin that retains other paths are not touched here: partial
            // displacement stays the previous owner's reporting duty.
            var stateOwners = state.Sources.Select(source => source.PluginId).ToHashSet();
            foreach (var unit in publishedUnits
                         .Where(unit => !stateOwners.Contains(unit.Origin.PluginId))
                         .OrderBy(unit => unit.Origin.PluginId.Value, StringComparer.Ordinal)
                         .ThenBy(unit => unit.Origin.IndexUnitId.Value, StringComparer.Ordinal))
            {
                if (stagedDeletions.Add((unit.Origin.PluginId.Value, unit.Origin.IndexUnitId.Value)))
                {
                    await _store.StageIndexUnitDeletionAsync(
                        descriptor.Id,
                        runId,
                        unit.Origin.PluginId,
                        unit.Origin.IndexUnitId,
                        cancellationToken).ConfigureAwait(false);
                    unitsDeleted++;
                }
            }
            await _store.CompleteIndexRunAsync(completed, state, cancellationToken).ConfigureAwait(false);
            persistenceDuration = Stopwatch.GetElapsedTime(persistenceStarted);
            var final = CreateDiagnostics(
                descriptor.Id, runId, CodeIndexRunStatus.Completed, finalPlan, ingestion,
                pluginsExecuted, unitsDeleted, sourceBytesRead,
                planningDuration, extractionDuration, persistenceDuration,
                pluginDiagnostics,
                selections);
            Report(diagnostics, final);
            return new(
                finalPlan,
                final,
                new CodeGraphPublication(
                    state.RepositoryId,
                    state.IndexRunId,
                    state.SnapshotIdentity,
                    state.IsConsistentSnapshot,
                    state.IndexIdentity),
                state);
        }
        catch (Exception exception)
        {
            var status = exception is OperationCanceledException
                ? CodeIndexRunStatus.Cancelled
                : CodeIndexRunStatus.Failed;
            await TryFinishFailedRunAsync(running, status).ConfigureAwait(false);
            var final = CreateDiagnostics(
                descriptor.Id, runId, status, plan, ingestion,
                pluginsExecuted, unitsDeleted, sourceBytesRead,
                planningDuration, extractionDuration, persistenceDuration,
                pluginDiagnostics,
                selections);
            Report(diagnostics, final);
            throw;
        }
    }

    private IReadOnlyList<ICodeGraphPlugin> SelectPlugins(
        CodeIndexPlan plan,
        CodeIndexPlanningOptions options)
    {
        var ids = options.PluginIds.Count > 0
            ? options.PluginIds.ToHashSet()
            : plan.Items.Select(item => item.Manifest.PluginId).ToHashSet();
        return _plugins.Plugins.Where(plugin => ids.Contains(plugin.Id)).ToArray();
    }

    private static CodeGraphPluginContext CreateContext(
        CodeRepositoryDescriptor descriptor,
        CodeIndexRunId runId,
        ICodeGraphPlugin plugin,
        CodeIndexPlan plan,
        IReadOnlyDictionary<PluginSourceKey, MaterializedSource> materialized,
        CodeRepositoryIndexState? previousState,
        IReadOnlyList<CodeIndexUnitReplacement> publishedUnits) =>
        CreateContextForItems(
            descriptor,
            runId,
            plugin,
            plan.Items.Where(item => item.Manifest.PluginId == plugin.Id).ToArray(),
            materialized,
            previousState,
            publishedUnits);

    /// <summary>
    /// Builds a plugin context for an explicit item set. Takeover executions
    /// pass the union of their own items and absorbed items; each item keeps
    /// its original manifest identity so history lookups and materialized
    /// buffers resolve against the plugin that planned the item, while unit
    /// ownership follows the executing (runner) plugin.
    /// </summary>
    private static CodeGraphPluginContext CreateContextForItems(
        CodeRepositoryDescriptor descriptor,
        CodeIndexRunId runId,
        ICodeGraphPlugin plugin,
        IReadOnlyList<CodeIndexPlanItem> items,
        IReadOnlyDictionary<PluginSourceKey, MaterializedSource> materialized,
        CodeRepositoryIndexState? previousState,
        IReadOnlyList<CodeIndexUnitReplacement> publishedUnits)
    {
        var ordered = items
            .OrderBy(item => item.Manifest.PluginId.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Manifest.SourcePath, StringComparer.Ordinal)
            .ToArray();
        var sources = ordered
            .Where(item => item.Source is not null)
            .Select(item =>
            {
                var source = materialized[new PluginSourceKey(item.Manifest.PluginId, item.Manifest.SourcePath)];
                return new CodeGraphSource(
                    item.Manifest.SourcePath,
                    item.Manifest.SourceHash,
                    cancellationToken =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Stream stream = new MemoryStream(
                            source.Buffer,
                            0,
                            source.Length,
                            writable: false,
                            publiclyVisible: false);
                        return new ValueTask<Stream>(stream);
                    });
            })
            .ToArray();
        var changes = ordered.Select(item => new CodeGraphSourceChange(
            item.Manifest.SourcePath,
            (CodeGraphSourceChangeKind)item.Status,
            previousState?.Sources.FirstOrDefault(source =>
                source.PluginId == item.Manifest.PluginId &&
                source.SourcePath == item.Manifest.SourcePath)?.SourceHash,
            item.Status == CodeIndexPlanStatus.Deleted ? null : item.Manifest.SourceHash)).ToArray();
        var previousUnits = publishedUnits
            .Where(unit => unit.Origin.PluginId == plugin.Id)
            .Select(unit => unit.Origin.IndexUnitId)
            .ToArray();
        return new(
            descriptor.Id,
            descriptor.Location,
            runId,
            sources,
            descriptor.Settings,
            changes,
            previousUnits);
    }

    /// <summary>
    /// Classifies a plugin execution failure. Classified plugin failures keep
    /// their kind; cancellations and live-source conflicts map to kinds that
    /// never permit fallback; anything else is a provider crash.
    /// </summary>
    private static CodePluginFailureKind ClassifyPluginFailure(Exception exception) =>
        exception switch
        {
            OperationCanceledException => CodePluginFailureKind.Cancelled,
            CodeSourceChangedDuringIndexingException => CodePluginFailureKind.SourceChanged,
            CodePluginFailureException classified => classified.FailureKind,
            _ => CodePluginFailureKind.ProviderCrash
        };

    /// <summary>
    /// Yields a plugin and its transitive fallback candidates in map order.
    /// </summary>
    private IEnumerable<CodePluginId> FallbackClosureIds(ICodeGraphPlugin plugin)
    {
        yield return plugin.Id;
        var visited = new HashSet<string>(StringComparer.Ordinal) { plugin.Id.Value };
        var current = plugin;
        while (_plugins.TryGetFallback(current, out var next) &&
            next is not null &&
            visited.Add(next.Id.Value))
        {
            yield return next.Id;
            current = next;
        }
    }

    /// <summary>
    /// Finds executing plugins that are fallback targets of other executing
    /// plugins. Deferred plugins run in phase 2 so a takeover executes once
    /// over the union of sources.
    /// </summary>
    private HashSet<string> FindDeferredPluginIds(IReadOnlyList<ICodeGraphPlugin> executing)    {
        var executingIds = executing.Select(plugin => plugin.Id).ToHashSet();
        var deferred = new HashSet<string>(StringComparer.Ordinal);
        foreach (var plugin in executing)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal) { plugin.Id.Value };
            var current = plugin;
            while (_plugins.TryGetFallback(current, out var next) &&
                next is not null &&
                visited.Add(next.Id.Value))
            {
                if (executingIds.Contains(next.Id))
                    deferred.Add(next.Id.Value);
                current = next;
            }
        }

        return deferred;
    }

    /// <summary>
    /// Unions takeover sources with the runner's own items, one entry per
    /// path. A path the previous owner lost and the runner must (re)index can
    /// appear twice with different manifests (a live item plus the runner's
    /// own deletion notice); the live source wins so the runner indexes it,
    /// while the runner's previous units are still visible for cleanup
    /// through its published-unit history. Deterministic by (plugin, path).
    /// </summary>
    private static IReadOnlyList<CodeIndexPlanItem> UnionItems(
        IReadOnlyList<CodeIndexPlanItem> first,
        IReadOnlyList<CodeIndexPlanItem> second) =>
        first.Concat(second)
            .GroupBy(item => item.Manifest.SourcePath, StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(item => item.Source is null)
                .ThenBy(item => item.Manifest.PluginId.Value, StringComparer.Ordinal)
                .First())
            .OrderBy(item => item.Manifest.PluginId.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Manifest.SourcePath, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// Rewrites absorbed plan items to their recovering runner so published
    /// source manifests name the plugin that actually produced the facts.
    /// </summary>
    private static CodeIndexPlan ApplyAbsorbedOwners(
        CodeIndexPlan plan,
        IReadOnlyDictionary<string, string> absorbedBy,
        CodeGraphPluginRegistry plugins)
    {
        if (absorbedBy.Count == 0)
            return plan;

        var runners = plugins.Plugins.ToDictionary(plugin => plugin.Id.Value, StringComparer.Ordinal);
        return new CodeIndexPlan(
            plan.Items.Select(item => absorbedBy.TryGetValue(item.Manifest.PluginId.Value, out var runnerId) &&
                    runners.TryGetValue(runnerId, out var runner)
                ? new CodeIndexPlanItem(
                    item.Status,
                    new CodeSourceManifest(
                        runner.Id,
                        runner.Version,
                        item.Manifest.SourcePath,
                        item.Manifest.SourceHash),
                    item.Source)
                : item).ToArray(),
            plan.RepositoryEntries,
            plan.UnsupportedEntries,
            plan.HashBytesRead,
            plan.ExcludedDirectories,
            plan.DepthLimitedDirectories,
            plan.ReparsePointsSkipped);
    }

    /// <summary>One plugin execution: a success carries its result, a failure its exception.</summary>
    private sealed record PluginExecution(
        ICodeGraphPlugin Plugin,
        IReadOnlyList<CodeIndexPlanItem> Items,
        CodeGraphExtractionResult? Result,
        Exception? Failure);

    private static CodeRepositoryIndexState CreateState(
        CodeRepositoryId repositoryId,
        CodeIndexRunId runId,
        ICodeRepositorySource repository,
        CodeRepositoryIndexState? previousState,
        CodeIndexPlan plan,
        IReadOnlyList<ICodeGraphPlugin> selectedPlugins)
    {
        var selectedIds = selectedPlugins.Select(plugin => plugin.Id).ToHashSet();
        // Retention is path-aware: a previous source is retained only when its
        // plugin is out of scope and no current item covers its path. Ownership
        // transfer (fallback rewriting a manifest to another plugin) must not
        // retain the previous owner's superseded entry alongside the new one.
        var currentPaths = plan.Items
            .Where(item => item.Status != CodeIndexPlanStatus.Deleted)
            .Select(item => item.Manifest.SourcePath)
            .ToHashSet(StringComparer.Ordinal);
        var retained = previousState?.Sources.Where(source =>
            !selectedIds.Contains(source.PluginId) &&
            !currentPaths.Contains(source.SourcePath)) ?? [];
        var current = plan.Items
            .Where(item => item.Status != CodeIndexPlanStatus.Deleted)
            .Select(item => item.Manifest);
        return new(
            repositoryId,
            runId,
            retained.Concat(current).ToArray(),
            repository.SnapshotIdentity,
            repository.IsConsistentSnapshot);
    }

    private async ValueTask TryFinishFailedRunAsync(
        CodeIndexRunManifest running,
        CodeIndexRunStatus status)
    {
        try
        {
            await _store.StoreIndexRunAsync(
                new CodeIndexRunManifest(
                    running.RepositoryId,
                    running.Id,
                    running.StartedAt,
                    status,
                    _timeProvider.GetUtcNow(),
                    running.Plugins),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // The original extraction or cancellation exception remains authoritative.
        }
    }

    private static CodeIndexingDiagnostics CreateDiagnostics(
        CodeRepositoryId repositoryId,
        CodeIndexRunId runId,
        CodeIndexRunStatus status,
        CodeIndexPlan plan,
        IEnumerable<CodeGraphIngestionDiagnostics> ingestion,
        int pluginsExecuted,
        int unitsDeleted,
        long sourceBytesRead,
        TimeSpan planningDuration,
        TimeSpan extractionDuration,
        TimeSpan persistenceDuration,
        IEnumerable<CodePluginIndexingDiagnostics> pluginDiagnostics,
        IEnumerable<CodeProviderSelection>? selections = null)
    {
        var units = ingestion.ToArray();
        var plugins = pluginDiagnostics
            .OrderBy(plugin => plugin.PluginId.Value, StringComparer.Ordinal)
            .ToArray();
        var providerSelections = selections?.ToArray() ?? [];
        return new(
            repositoryId,
            runId,
            status,
            plan.RepositoryEntries,
            plan.Items.Count(item => item.Status == CodeIndexPlanStatus.New),
            plan.Items.Count(item => item.Status == CodeIndexPlanStatus.Changed),
            plan.Items.Count(item => item.Status == CodeIndexPlanStatus.Unchanged),
            plan.Items.Count(item => item.Status == CodeIndexPlanStatus.Deleted),
            plan.UnsupportedEntries,
            plan.ExcludedDirectories,
            plan.DepthLimitedDirectories,
            plan.ReparsePointsSkipped,
            sourceBytesRead,
            pluginsExecuted,
            units.Length,
            unitsDeleted,
            units.Sum(unit => unit.NodesReceived),
            units.Sum(unit => unit.DeclarationsReceived),
            units.Sum(unit => unit.EdgesReceived),
            units.Sum(unit => unit.RejectedFacts),
            plugins.Sum(plugin => plugin.UnresolvedRelationships),
            planningDuration,
            extractionDuration,
            persistenceDuration,
            plugins,
            units.SelectMany(unit => unit.WarningCodes)
                .Concat(plugins.SelectMany(plugin => plugin.WarningCodes))
                .Concat(providerSelections.Any(selection => selection.Selected is not null)
                    ? ["plugin.fallback"]
                    : [])
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Take(100)
                .ToArray(),
            providerSelections);
    }

    private static CodePluginIndexingDiagnostics CreatePluginDiagnostics(
        ICodeGraphPlugin plugin,
        CodePluginIndexingStatus status,
        TimeSpan duration,
        int sourcesSupplied,
        CodeGraphExtractionResult result) =>
        new(
            plugin.Id,
            plugin.Version,
            status,
            duration,
            sourcesSupplied,
            result.SourcesExamined,
            result.SourcesContributingFacts,
            result.UnresolvedRelationships,
            result.ObsoleteIndexUnits.Count,
            result.WarningCodes.ToArray());

    private static async ValueTask<IReadOnlyDictionary<PluginSourceKey, MaterializedSource>> MaterializeSourcesAsync(
        ICodeRepositorySource repository,
        CodeIndexPlan plan,
        IReadOnlyList<ICodeGraphPlugin> plugins,
        CodeIndexingOptions options,
        CancellationToken cancellationToken)
    {
        var pluginIds = plugins.Select(plugin => plugin.Id).ToHashSet();
        var result = new Dictionary<PluginSourceKey, MaterializedSource>();
        var sourcesByPath = new Dictionary<string, MaterializedSource>(StringComparer.Ordinal);
        var totalBytes = plan.HashBytesRead;
        var buffer = new byte[81920];
        foreach (var item in plan.Items.Where(item =>
                     item.Source is not null && pluginIds.Contains(item.Manifest.PluginId)))
        {
            var entry = item.Source!;
            if (sourcesByPath.TryGetValue(entry.Path, out var existing))
            {
                result.Add(
                    new PluginSourceKey(item.Manifest.PluginId, item.Manifest.SourcePath),
                    existing);
                continue;
            }
            if (entry.Length > options.MaxSourceBytes)
                throw new CodeSourceSizeLimitException(entry.Path, options.MaxSourceBytes, false);
            await using var input = await repository.OpenReadAsync(entry, cancellationToken)
                .ConfigureAwait(false);
            var initialCapacity = entry.Length is >= 0 and <= int.MaxValue
                ? (int)entry.Length.Value
                : 0;
            await using var output = new MemoryStream(initialCapacity);
            while (true)
            {
                var read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                totalBytes += read;
                if (output.Length + read > options.MaxSourceBytes)
                    throw new CodeSourceSizeLimitException(entry.Path, options.MaxSourceBytes, false);
                if (totalBytes > options.MaxTotalSourceBytes)
                    throw new CodeSourceSizeLimitException(entry.Path, options.MaxTotalSourceBytes, true);
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }

            if (!output.TryGetBuffer(out var content))
                throw new InvalidOperationException("Hetu could not access its materialized source buffer.");
            if (item.Manifest.SourceHash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                var actual = $"sha256:{Convert.ToHexStringLower(SHA256.HashData(content.AsSpan()))}";
                if (!string.Equals(actual, item.Manifest.SourceHash, StringComparison.OrdinalIgnoreCase))
                    throw new CodeSourceChangedDuringIndexingException(entry.Path);
            }

            var materialized = new MaterializedSource(content.Array!, content.Count);
            sourcesByPath.Add(entry.Path, materialized);
            result.Add(
                new PluginSourceKey(item.Manifest.PluginId, item.Manifest.SourcePath),
                materialized);
        }

        return result;
    }

    private static void Report(
        Action<CodeIndexingDiagnostics>? callback,
        CodeIndexingDiagnostics diagnostics)
    {
        try
        {
            callback?.Invoke(diagnostics);
        }
        catch
        {
            // Diagnostics must never change indexing success or failure.
        }
    }

    private sealed record MaterializedSource(byte[] Buffer, int Length);

    private sealed class PluginScopedSink(
        ICodeGraphSink inner,
        CodeRepositoryId repositoryId,
        CodeIndexRunId runId,
        CodePluginId pluginId,
        string pluginVersion) : ICodeGraphSink
    {
        public CodeGraphBatchLimits Limits => inner.Limits;

        public ValueTask WriteBatchAsync(
            CodeGraphBatch batch,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(batch);
            if (batch.Origin.RepositoryId != repositoryId ||
                batch.Origin.IndexRunId != runId ||
                batch.Origin.PluginId != pluginId ||
                !string.Equals(batch.Origin.PluginVersion, pluginVersion, StringComparison.Ordinal))
            {
                throw new CodeGraphBatchRejectedException(
                    "A plugin emitted a batch outside its assigned lifecycle scope.",
                    [
                        new CodeGraphValidationError(
                            CodeGraphValidationErrorKind.OwnershipMismatch,
                            "lifecycle.batch.ownership-mismatch",
                            "Batch repository, run, plugin, and version must match the executing plugin.")
                    ]);
            }

            return inner.WriteBatchAsync(batch, cancellationToken);
        }
    }
}

public sealed class CodeSourceChangedDuringIndexingException(string path)
    : Exception($"Repository source '{path}' changed between planning and extraction.")
{
    public string Path { get; } = path;
}

public sealed class CodeSourceSizeLimitException(
    string path,
    long maximumBytes,
    bool totalLimit) : Exception(totalLimit
        ? $"Repository source materialization exceeded the total limit of {maximumBytes} bytes."
        : $"Repository source '{path}' exceeded the limit of {maximumBytes} bytes.")
{
    public string Path { get; } = path;
    public long MaximumBytes { get; } = maximumBytes;
    public bool IsTotalLimit { get; } = totalLimit;
}
