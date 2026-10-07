using System.Runtime.CompilerServices;
using System.Text;

namespace Penghou.Hetu.Tests;

public sealed class CodeIndexingServiceTests
{
    [Fact]
    public async Task IndexAsync_CompletesRunPublishesStateAndDeletesObsoletePluginUnit()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var provider = new MemoryProvider(files);
        var plugin = new LifecyclePlugin();
        var store = new InMemoryCodeGraphStore();
        var service = Service(provider, plugin, store);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");

        var first = await service.IndexAsync(descriptor, new("run:first"));

        Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
        Assert.Equal(descriptor.Id, first.Publication.RepositoryId);
        Assert.Equal(new CodeIndexRunId("run:first"), first.Publication.IndexRunId);
        Assert.Equal("snapshot:test", first.Publication.SnapshotIdentity);
        Assert.True(first.Publication.IsConsistentSnapshot);
        Assert.Equal(first.PublishedState.IndexIdentity, first.Publication.IndexIdentity);
        Assert.Equal(64, first.Publication.IndexIdentity.Value.Length);
        Assert.Equal(1, first.Diagnostics.FilesNew);
        Assert.Equal(1, first.Diagnostics.IndexUnitsCompleted);
        Assert.Equal(2, first.Diagnostics.UnresolvedRelationships);
        var pluginDiagnostics = Assert.Single(first.Diagnostics.Plugins);
        Assert.Equal(plugin.Id, pluginDiagnostics.PluginId);
        Assert.Equal(1, pluginDiagnostics.SourcesExamined);
        Assert.Equal(1, pluginDiagnostics.SourcesContributingFacts);
        Assert.Equal(2, pluginDiagnostics.UnresolvedRelationships);
        Assert.Contains("test.unresolved", pluginDiagnostics.WarningCodes);
        Assert.True(pluginDiagnostics.Duration >= TimeSpan.Zero);
        Assert.NotNull(await store.GetNodeAsync(descriptor.Id, new("node:example")));
        Assert.Equal("run:first", (await store.GetLatestIndexStateAsync(descriptor.Id))!.IndexRunId.Value);

        files.Clear();
        var second = await service.IndexAsync(descriptor, new("run:second"));

        Assert.Equal(1, second.Diagnostics.FilesDeleted);
        Assert.Equal(1, second.Diagnostics.IndexUnitsDeleted);
        Assert.Null(await store.GetNodeAsync(descriptor.Id, new("node:example")));
        Assert.Empty((await store.GetLatestIndexStateAsync(descriptor.Id))!.Sources);
        Assert.Equal(CodeGraphSourceChangeKind.Deleted, plugin.LastChanges.Single().Kind);
        Assert.Equal(second.PublishedState.IndexIdentity, second.Publication.IndexIdentity);
    }

    [Fact]
    public async Task IndexAsync_FailedRunRetainsLastSuccessfulSourceState()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "one" };
        var provider = new MemoryProvider(files);
        var plugin = new LifecyclePlugin();
        var store = new InMemoryCodeGraphStore();
        var service = Service(provider, plugin, store);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        await service.IndexAsync(descriptor, new("run:first"));
        var firstState = await store.GetLatestIndexStateAsync(descriptor.Id);
        files["src/Example.cs"] = "two";
        plugin.Fail = true;
        CodeIndexingDiagnostics? diagnostics = null;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.IndexAsync(
                descriptor,
                new("run:failed"),
                diagnostics: value => diagnostics = value));

        var failed = await store.GetIndexRunAsync(descriptor.Id, new("run:failed"));
        Assert.Equal(CodeIndexRunStatus.Failed, failed!.Status);
        Assert.Equal(firstState!.IndexRunId, (await store.GetLatestIndexStateAsync(descriptor.Id))!.IndexRunId);
        Assert.NotNull(diagnostics);
        Assert.Equal(CodePluginIndexingStatus.Failed, Assert.Single(diagnostics.Plugins).Status);
        Assert.Contains("plugin.failed", diagnostics.WarningCodes);
    }

    [Fact]
    public async Task IndexAsync_DiagnosticsCallbackCannotBreakSuccessfulIndexing()
    {
        var provider = new MemoryProvider(
            new Dictionary<string, string> { ["src/Example.cs"] = "content" });
        var store = new InMemoryCodeGraphStore();

        var result = await Service(provider, new LifecyclePlugin(), store).IndexAsync(
            new CodeRepositoryDescriptor(new("repo:test"), "memory://test"),
            new CodeIndexRunId("run:one"),
            diagnostics: _ => throw new InvalidOperationException("observer failure"));

        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
    }

    [Fact]
    public async Task IndexAsync_CancelledExtractionRecordsCancelledRun()
    {
        var provider = new MemoryProvider(
            new Dictionary<string, string> { ["src/Example.cs"] = "content" });
        var plugin = new LifecyclePlugin { Cancel = true };
        var store = new InMemoryCodeGraphStore();
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Service(provider, plugin, store).IndexAsync(descriptor, new("run:cancelled")));

        var run = await store.GetIndexRunAsync(descriptor.Id, new("run:cancelled"));
        Assert.Equal(CodeIndexRunStatus.Cancelled, run!.Status);
        Assert.Null(await store.GetLatestIndexStateAsync(descriptor.Id));
    }

    [Fact]
    public async Task IndexAsync_SkipsPluginWhenEverySourceIsUnchanged()
    {
        var provider = new MemoryProvider(
            new Dictionary<string, string> { ["src/Example.cs"] = "content" });
        var plugin = new LifecyclePlugin();
        var store = new InMemoryCodeGraphStore();
        var service = Service(provider, plugin, store);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        var first = await service.IndexAsync(descriptor, new("run:first"));

        var second = await service.IndexAsync(descriptor, new("run:second"));

        Assert.Equal(1, plugin.ExecutionCount);
        Assert.Equal(0, second.Diagnostics.PluginsExecuted);
        Assert.Equal(Encoding.UTF8.GetByteCount("content"), second.Diagnostics.SourceBytesRead);
        Assert.Equal(3, provider.OpenCount);
        Assert.Equal(1, second.Diagnostics.FilesUnchanged);
        Assert.Equal("run:first", second.Publication.IndexRunId.Value);
        Assert.Equal(first.Publication, second.Publication);
        Assert.Equal(
            (await store.GetLatestIndexStateAsync(descriptor.Id))!.IndexIdentity,
            second.Publication.IndexIdentity);
    }

    [Fact]
    public async Task IndexAsync_RejectsBatchOutsideExecutingPluginScope()
    {
        var provider = new MemoryProvider(
            new Dictionary<string, string> { ["src/Example.cs"] = "content" });
        var plugin = new LifecyclePlugin { EmitForeignOrigin = true };
        var store = new InMemoryCodeGraphStore();
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");

        var exception = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
            await Service(provider, plugin, store).IndexAsync(descriptor, new("run:foreign")));

        Assert.Contains(exception.Errors, error => error.Code == "lifecycle.batch.ownership-mismatch");
        Assert.Equal(
            CodeIndexRunStatus.Failed,
            (await store.GetIndexRunAsync(descriptor.Id, new("run:foreign")))!.Status);
    }

    [Fact]
    public async Task IndexAsync_DetectsSourceChangedAfterPlanning()
    {
        var provider = new MemoryProvider(
            new Dictionary<string, string> { ["src/Example.cs"] = "first" })
        {
            ChangeContentOnSecondOpen = true
        };
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");

        await Assert.ThrowsAsync<CodeSourceChangedDuringIndexingException>(async () =>
            await Service(provider, new LifecyclePlugin(), new InMemoryCodeGraphStore())
                .IndexAsync(descriptor, new("run:changed")));
    }

    [Fact]
    public async Task IndexAsync_EnforcesPerSourceByteLimit()
    {
        var provider = new MemoryProvider(
            new Dictionary<string, string> { ["src/Example.cs"] = "too-large" });
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        var options = new CodeIndexingOptions(maxSourceBytes: 4, maxTotalSourceBytes: 8);

        var exception = await Assert.ThrowsAsync<CodeSourceSizeLimitException>(async () =>
            await Service(provider, new LifecyclePlugin(), new InMemoryCodeGraphStore())
                .IndexAsync(descriptor, new("run:large"), options));

        Assert.False(exception.IsTotalLimit);
        Assert.Equal(4, exception.MaximumBytes);
    }

    [Fact]
    public async Task IndexAsync_EnforcesTotalHashByteLimit()
    {
        var provider = new MemoryProvider(new Dictionary<string, string>
        {
            ["src/One.cs"] = "123456",
            ["src/Two.cs"] = "abcdef"
        });
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        var options = new CodeIndexingOptions(maxSourceBytes: 8, maxTotalSourceBytes: 10);

        var exception = await Assert.ThrowsAsync<CodeSourceSizeLimitException>(async () =>
            await Service(provider, new LifecyclePlugin(), new InMemoryCodeGraphStore())
                .IndexAsync(descriptor, new("run:large-total"), options));

        Assert.True(exception.IsTotalLimit);
    }

    [Fact]
    public async Task IndexAsync_ExecutesPluginsInParallelWithinConfiguredBound()
    {
        var provider = new MemoryProvider(new Dictionary<string, string>
        {
            ["src/Example.cs"] = "csharp",
            ["src/Example.ts"] = "typescript"
        });
        var probe = new ConcurrencyProbe(expectedConcurrency: 2);
        ICodeGraphPlugin[] plugins =
        [
            new ConcurrentPlugin("plugin:csharp", ".cs", probe),
            new ConcurrentPlugin("plugin:typescript", ".ts", probe)
        ];
        var service = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([provider]),
            new CodeGraphPluginRegistry(plugins),
            new InMemoryCodeGraphStore());

        var result = await service.IndexAsync(
            new CodeRepositoryDescriptor(new("repo:test"), "memory://test"),
            new CodeIndexRunId("run:parallel"),
            new CodeIndexingOptions(maxConcurrentPlugins: 2));

        Assert.Equal(2, probe.MaximumObserved);
        Assert.Equal(2, result.Diagnostics.PluginsExecuted);
    }

    [Fact]
    public async Task CheckFreshnessAsync_ReportsUnknownBeforeFirstIndex()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var store = new InMemoryCodeGraphStore();
        var service = Service(new MemoryProvider(files), new LifecyclePlugin(), store);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");

        var freshness = await service.CheckFreshnessAsync(descriptor);

        Assert.Equal(CodeFreshnessStatus.Unknown, freshness.Status);
        Assert.Null(freshness.Publication);
        Assert.Null(await store.GetLatestPublicationAsync(descriptor.Id));
        Assert.Null(await store.GetLatestIndexStateAsync(descriptor.Id));
    }

    [Fact]
    public async Task CheckFreshnessAsync_ReportsFreshWithoutTouchingTheGraph()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var plugin = new LifecyclePlugin();
        var store = new InMemoryCodeGraphStore();
        var service = Service(new MemoryProvider(files), plugin, store);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        await service.IndexAsync(descriptor, new("run:first"));
        var executions = plugin.ExecutionCount;

        var freshness = await service.CheckFreshnessAsync(descriptor);

        Assert.Equal(CodeFreshnessStatus.Fresh, freshness.Status);
        Assert.Equal(new CodeIndexRunId("run:first"), freshness.Publication!.IndexRunId);
        Assert.Equal(0, freshness.NewCount + freshness.ChangedCount + freshness.DeletedCount);
        Assert.Equal(1, freshness.UnchangedCount);
        Assert.Equal(executions, plugin.ExecutionCount);
        Assert.Equal(new CodeIndexRunId("run:first"), (await store.GetLatestPublicationAsync(descriptor.Id))!.IndexRunId);
    }

    [Fact]
    public async Task CheckFreshnessAsync_ReportsStaleOnChangeAddAndDelete()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var store = new InMemoryCodeGraphStore();
        var service = Service(new MemoryProvider(files), new LifecyclePlugin(), store);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        await service.IndexAsync(descriptor, new("run:first"));

        files["src/Example.cs"] = "class Example { /* changed */ }";
        var changed = await service.CheckFreshnessAsync(descriptor);
        Assert.Equal(CodeFreshnessStatus.Stale, changed.Status);
        Assert.Equal(1, changed.ChangedCount);

        files["src/Added.cs"] = "class Added {}";
        var added = await service.CheckFreshnessAsync(descriptor);
        Assert.Equal(CodeFreshnessStatus.Stale, added.Status);
        Assert.Equal(1, added.NewCount);

        files.Clear();
        var deleted = await service.CheckFreshnessAsync(descriptor);
        Assert.Equal(CodeFreshnessStatus.Stale, deleted.Status);
        Assert.Equal(1, deleted.DeletedCount);

        // The published graph still serves the original publication.
        Assert.Equal(new CodeIndexRunId("run:first"), (await store.GetLatestPublicationAsync(descriptor.Id))!.IndexRunId);
        Assert.NotNull(await store.GetNodeAsync(descriptor.Id, new("node:example")));
    }

    [Fact]
    public async Task IndexAsync_ReusesPublicationWhenNothingChanged()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var plugin = new LifecyclePlugin();
        var store = new InMemoryCodeGraphStore();
        var service = Service(new MemoryProvider(files), plugin, store);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");

        var first = await service.IndexAsync(descriptor, new("run:first"));
        var second = await service.IndexAsync(descriptor, new("run:second"));

        Assert.Equal(first.Publication, second.Publication);
        Assert.Equal(first.PublishedState, second.PublishedState);
        Assert.Equal(1, plugin.ExecutionCount);
    }

    private static CodeIndexingService Service(
        MemoryProvider provider,
        LifecyclePlugin plugin,
        ICodeGraphStore store) =>
        new(new CodeRepositoryProviderRegistry([provider]), new([plugin]), store);

    private static CodeIndexingService ServiceWithMap(
        MemoryProvider provider,
        ICodeGraphStore store,
        CodeLanguageProviderMap map,
        params ICodeGraphPlugin[] plugins) =>
        new(new CodeRepositoryProviderRegistry([provider]), new CodeGraphPluginRegistry(plugins, map), store);

    private static CodeLanguageProviderMap ProviderMap(
        string language,
        params string[] providerIds) =>
        new(new Dictionary<string, IReadOnlyList<CodePluginId>>
        {
            [language] = providerIds.Select(id => new CodePluginId(id)).ToArray()
        });

    [Fact]
    public async Task IndexAsync_FallbackOnUnavailableTakesOverUnitsManifestsAndSelection()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var provider = new MemoryProvider(files);
        var preferred = new ConfigurablePlugin(
            "plugin:preferred",
            "csharp",
            CodeGraphCapabilities.References | CodeGraphCapabilities.Calls,
            nodeName: "Preferred");
        var fallback = new ConfigurablePlugin(
            "plugin:fallback",
            "csharp",
            CodeGraphCapabilities.Syntax,
            nodeName: "Fallback");
        var map = ProviderMap("csharp", "plugin:preferred", "plugin:fallback");
        var store = new InMemoryCodeGraphStore();
        var service = ServiceWithMap(provider, store, map, preferred, fallback);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        var options = new CodeIndexingOptions();

        var first = await service.IndexAsync(descriptor, new("run:first"), options);
        Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
        Assert.Equal(1, preferred.ExecutionCount);
        Assert.Equal(0, fallback.ExecutionCount);
        Assert.NotNull(await store.GetNodeAsync(descriptor.Id, new("node:plugin:preferred")));

        // The preferred provider stages facts and then reports itself
        // unavailable: takeover must discard its footprint and reindex.
        files["src/Example.cs"] = "class Example {} // changed";
        preferred.EmitBeforeFailure = true;
        preferred.FailWith = _ => new CodePluginFailureException(CodePluginFailureKind.ProviderUnavailable);

        CodeIndexingDiagnostics? reported = null;
        var second = await service.IndexAsync(
            descriptor,
            new("run:second"),
            options,
            diagnostics: value => reported = value);

        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);
        Assert.Equal(2, preferred.ExecutionCount);
        Assert.Equal(1, fallback.ExecutionCount);
        Assert.NotNull(await store.GetNodeAsync(descriptor.Id, new("node:plugin:fallback")));
        Assert.Null(await store.GetNodeAsync(descriptor.Id, new("node:plugin:preferred")));
        Assert.All(
            second.Plan.Items,
            item => Assert.Equal(new CodePluginId("plugin:fallback"), item.Manifest.PluginId));
        Assert.All(
            (await store.GetLatestIndexStateAsync(descriptor.Id))!.Sources,
            source => Assert.Equal(new CodePluginId("plugin:fallback"), source.PluginId));
        var selection = Assert.Single(second.Diagnostics.ProviderSelections);
        Assert.Equal("csharp", selection.Language);
        Assert.Equal(new CodePluginId("plugin:preferred"), selection.Preferred);
        Assert.Equal(
            ["plugin:preferred", "plugin:fallback"],
            selection.Attempted.Select(id => id.Value));
        Assert.Equal(new CodePluginId("plugin:fallback"), selection.Selected);
        Assert.Equal(CodePluginFailureKind.ProviderUnavailable, selection.FailureKind);
        Assert.Equal("plugin.unavailable", selection.Reason);
        Assert.Equal(
            CodeGraphCapabilities.References | CodeGraphCapabilities.Calls,
            selection.LostCapabilities);
        Assert.Contains("plugin.fallback", second.Diagnostics.WarningCodes);
        Assert.Contains("plugin.unavailable", second.Diagnostics.WarningCodes);
        Assert.Contains(
            reported!.Plugins,
            plugin => plugin.PluginId == preferred.Id &&
                plugin.Status == CodePluginIndexingStatus.Failed &&
                plugin.WarningCodes.Contains("plugin.unavailable"));
        Assert.Contains(
            reported.Plugins,
            plugin => plugin.PluginId == fallback.Id &&
                plugin.Status == CodePluginIndexingStatus.Completed);

        // The preferred provider recovers: it takes ownership back and the
        // fallback's displaced units are collected without manual reporting.
        preferred.EmitBeforeFailure = false;
        preferred.FailWith = null;
        files["src/Example.cs"] = "class Example {} // changed again";

        var third = await service.IndexAsync(descriptor, new("run:third"), options);

        Assert.Equal(CodeIndexRunStatus.Completed, third.Diagnostics.Status);
        Assert.Equal(3, preferred.ExecutionCount);
        // The fallback executes once more for its Deleted item: ownership moved
        // back, so it sees the deletion and can report obsolete units. Its
        // displaced facts are collected even though this fake reports none.
        Assert.Equal(2, fallback.ExecutionCount);
        Assert.NotNull(await store.GetNodeAsync(descriptor.Id, new("node:plugin:preferred")));
        Assert.Null(await store.GetNodeAsync(descriptor.Id, new("node:plugin:fallback")));
        Assert.All(
            (await store.GetLatestIndexStateAsync(descriptor.Id))!.Sources,
            source => Assert.Equal(new CodePluginId("plugin:preferred"), source.PluginId));
        Assert.Empty(third.Diagnostics.ProviderSelections);
    }

    [Fact]
    public async Task IndexAsync_RepeatedFallbackThenRecoveryLeavesSingleOwner()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {} // v1" };
        var provider = new MemoryProvider(files);
        var preferred = new ConfigurablePlugin(
            "plugin:preferred",
            "csharp",
            CodeGraphCapabilities.References | CodeGraphCapabilities.Calls,
            nodeName: "Preferred");
        var fallback = new ConfigurablePlugin(
            "plugin:fallback",
            "csharp",
            CodeGraphCapabilities.Syntax,
            nodeName: "Fallback");
        var map = ProviderMap("csharp", "plugin:preferred", "plugin:fallback");
        var store = new InMemoryCodeGraphStore();
        var service = ServiceWithMap(provider, store, map, preferred, fallback);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        var options = new CodeIndexingOptions();
        var unavailable = () => new CodePluginFailureException(CodePluginFailureKind.ProviderUnavailable);

        var first = await service.IndexAsync(descriptor, new("run:first"), options);
        Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
        Assert.Empty(first.Diagnostics.ProviderSelections);

        // Two consecutive fallback generations with changed sources: the
        // second must replace the first without leaving either behind.
        preferred.FailWith = _ => unavailable();
        files["src/Example.cs"] = "class Example {} // v2";
        var second = await service.IndexAsync(descriptor, new("run:second"), options);
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);
        Assert.Single(second.Diagnostics.ProviderSelections);

        files["src/Example.cs"] = "class Example {} // v3";
        var third = await service.IndexAsync(descriptor, new("run:third"), options);
        Assert.Equal(CodeIndexRunStatus.Completed, third.Diagnostics.Status);
        var thirdSelection = Assert.Single(third.Diagnostics.ProviderSelections);
        Assert.Equal(new CodePluginId("plugin:fallback"), thirdSelection.Selected);
        Assert.All(
            (await store.GetLatestIndexStateAsync(descriptor.Id))!.Sources,
            source => Assert.Equal(new CodePluginId("plugin:fallback"), source.PluginId));

        // Recovery: the preferred provider takes ownership back and every
        // fallback footprint from both generations disappears completely.
        preferred.FailWith = null;
        files["src/Example.cs"] = "class Example {} // v4";
        var fourth = await service.IndexAsync(descriptor, new("run:fourth"), options);

        Assert.Equal(CodeIndexRunStatus.Completed, fourth.Diagnostics.Status);
        Assert.Empty(fourth.Diagnostics.ProviderSelections);
        Assert.NotNull(await store.GetNodeAsync(descriptor.Id, new("node:plugin:preferred")));
        Assert.Null(await store.GetNodeAsync(descriptor.Id, new("node:plugin:fallback")));
        var state = (await store.GetLatestIndexStateAsync(descriptor.Id))!;
        var source = Assert.Single(state.Sources);
        Assert.Equal(new CodePluginId("plugin:preferred"), source.PluginId);
        Assert.Equal(new CodeIndexRunId("run:fourth"), state.IndexRunId);
        var units = await store.GetPublishedUnitsAsync(descriptor.Id);
        Assert.All(units, unit => Assert.Equal(new CodePluginId("plugin:preferred"), unit.Origin.PluginId));
        Assert.All(
            fourth.Plan.Items.Where(item => item.Status != CodeIndexPlanStatus.Deleted),
            item => Assert.Equal(new CodePluginId("plugin:preferred"), item.Manifest.PluginId));
        Assert.Equal(source.SourceHash, fourth.Plan.Items.Single(item =>
            item.Status != CodeIndexPlanStatus.Deleted).Manifest.SourceHash);
    }

    [Fact]
    public async Task IndexAsync_ProjectLoadFailureDoesNotFallBackByDefault()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var preferred = new ConfigurablePlugin("plugin:preferred", "csharp", CodeGraphCapabilities.Symbols);
        preferred.FailWith = _ => new CodePluginFailureException(CodePluginFailureKind.ProjectLoadFailure);
        var fallback = new ConfigurablePlugin("plugin:fallback", "csharp", CodeGraphCapabilities.Syntax);
        var service = ServiceWithMap(
            new MemoryProvider(files),
            new InMemoryCodeGraphStore(),
            ProviderMap("csharp", "plugin:preferred", "plugin:fallback"),
            preferred,
            fallback);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");
        CodeIndexingDiagnostics? reported = null;

        var failure = await Assert.ThrowsAsync<CodePluginFailureException>(async () =>
            await service.IndexAsync(
                descriptor,
                new("run:failed"),
                new CodeIndexingOptions(),
                diagnostics: value => reported = value));

        Assert.Equal(CodePluginFailureKind.ProjectLoadFailure, failure.FailureKind);
        Assert.Equal(0, fallback.ExecutionCount);
        Assert.NotNull(reported);
        Assert.Empty(reported.ProviderSelections);
        Assert.DoesNotContain("plugin.fallback", reported.WarningCodes);
        Assert.Contains("plugin.project-load-failure", reported.WarningCodes);
        Assert.Equal(CodePluginIndexingStatus.Failed, Assert.Single(reported.Plugins).Status);
    }

    [Fact]
    public async Task IndexAsync_ProjectLoadFailureFallsBackUnderOnFailure()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var preferred = new ConfigurablePlugin("plugin:preferred", "csharp", CodeGraphCapabilities.Symbols);
        preferred.FailWith = _ => new CodePluginFailureException(CodePluginFailureKind.ProjectLoadFailure);
        var fallback = new ConfigurablePlugin(
            "plugin:fallback", "csharp", CodeGraphCapabilities.Syntax, nodeName: "Fallback");
        var store = new InMemoryCodeGraphStore();
        var service = ServiceWithMap(
            new MemoryProvider(files),
            store,
            ProviderMap("csharp", "plugin:preferred", "plugin:fallback"),
            preferred,
            fallback);
        var descriptor = new CodeRepositoryDescriptor(new("repo:test"), "memory://test");

        var result = await service.IndexAsync(
            descriptor,
            new("run:one"),
            new CodeIndexingOptions(fallbackPolicy: ProviderFallbackPolicy.OnFailure));

        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
        Assert.Equal(1, fallback.ExecutionCount);
        Assert.NotNull(await store.GetNodeAsync(descriptor.Id, new("node:plugin:fallback")));
        var selection = Assert.Single(result.Diagnostics.ProviderSelections);
        Assert.Equal(CodePluginFailureKind.ProjectLoadFailure, selection.FailureKind);
        Assert.Equal(new CodePluginId("plugin:fallback"), selection.Selected);
    }

    [Fact]
    public async Task IndexAsync_FallbackPolicyNoneDisablesFallback()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var preferred = new ConfigurablePlugin("plugin:preferred", "csharp", CodeGraphCapabilities.Symbols);
        preferred.FailWith = _ => new CodePluginFailureException(CodePluginFailureKind.ProviderUnavailable);
        var fallback = new ConfigurablePlugin("plugin:fallback", "csharp", CodeGraphCapabilities.Syntax);
        var service = ServiceWithMap(
            new MemoryProvider(files),
            new InMemoryCodeGraphStore(),
            ProviderMap("csharp", "plugin:preferred", "plugin:fallback"),
            preferred,
            fallback);

        await Assert.ThrowsAsync<CodePluginFailureException>(async () =>
            await service.IndexAsync(
                new CodeRepositoryDescriptor(new("repo:test"), "memory://test"),
                new("run:failed"),
                new CodeIndexingOptions(fallbackPolicy: ProviderFallbackPolicy.None)));

        Assert.Equal(0, fallback.ExecutionCount);
    }

    [Fact]
    public async Task IndexAsync_CancelledPreferredNeverFallsBack()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var preferred = new ConfigurablePlugin("plugin:preferred", "csharp", CodeGraphCapabilities.Symbols);
        preferred.FailWith = _ => new OperationCanceledException("stop");
        var fallback = new ConfigurablePlugin("plugin:fallback", "csharp", CodeGraphCapabilities.Syntax);
        var service = ServiceWithMap(
            new MemoryProvider(files),
            new InMemoryCodeGraphStore(),
            ProviderMap("csharp", "plugin:preferred", "plugin:fallback"),
            preferred,
            fallback);
        CodeIndexingDiagnostics? reported = null;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await service.IndexAsync(
                new CodeRepositoryDescriptor(new("repo:test"), "memory://test"),
                new("run:cancelled"),
                new CodeIndexingOptions(fallbackPolicy: ProviderFallbackPolicy.OnFailure),
                diagnostics: value => reported = value));

        Assert.Equal(0, fallback.ExecutionCount);
        Assert.NotNull(reported);
        Assert.Empty(reported.ProviderSelections);
        Assert.Equal(CodeIndexRunStatus.Cancelled, reported.Status);
    }

    [Fact]
    public async Task IndexAsync_ChainedFallbackRecordsFullAttemptHistory()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var first = new ConfigurablePlugin("plugin:first", "csharp", CodeGraphCapabilities.Symbols);
        first.FailWith = _ => new CodePluginFailureException(CodePluginFailureKind.ProviderUnavailable);
        var second = new ConfigurablePlugin("plugin:second", "csharp", CodeGraphCapabilities.Symbols);
        second.FailWith = _ => new CodePluginFailureException(CodePluginFailureKind.ParseFailure);
        var third = new ConfigurablePlugin(
            "plugin:third", "csharp", CodeGraphCapabilities.Syntax, nodeName: "Third");
        var store = new InMemoryCodeGraphStore();
        var service = ServiceWithMap(
            new MemoryProvider(files),
            store,
            ProviderMap("csharp", "plugin:first", "plugin:second", "plugin:third"),
            first,
            second,
            third);

        var result = await service.IndexAsync(
            new CodeRepositoryDescriptor(new("repo:test"), "memory://test"),
            new("run:one"),
            new CodeIndexingOptions(fallbackPolicy: ProviderFallbackPolicy.OnFailure));

        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
        Assert.NotNull(await store.GetNodeAsync(
            new CodeRepositoryId("repo:test"), new("node:plugin:third")));
        var selection = Assert.Single(result.Diagnostics.ProviderSelections);
        Assert.Equal(
            ["plugin:first", "plugin:second", "plugin:third"],
            selection.Attempted.Select(id => id.Value));
        Assert.Equal(new CodePluginId("plugin:third"), selection.Selected);
        Assert.Equal(CodePluginFailureKind.ParseFailure, selection.FailureKind);
    }

    [Fact]
    public async Task IndexAsync_FallbackDecliningThePathDoesNotFallBack()
    {
        var files = new Dictionary<string, string> { ["src/Example.cs"] = "class Example {}" };
        var preferred = new ConfigurablePlugin("plugin:preferred", "csharp", CodeGraphCapabilities.Symbols);
        preferred.FailWith = _ => new CodePluginFailureException(CodePluginFailureKind.ProviderUnavailable);
        var fallback = new ConfigurablePlugin(
            "plugin:fallback",
            "csharp",
            CodeGraphCapabilities.Syntax,
            canHandle: _ => false);
        var service = ServiceWithMap(
            new MemoryProvider(files),
            new InMemoryCodeGraphStore(),
            ProviderMap("csharp", "plugin:preferred", "plugin:fallback"),
            preferred,
            fallback);

        await Assert.ThrowsAsync<CodePluginFailureException>(async () =>
            await service.IndexAsync(
                new CodeRepositoryDescriptor(new("repo:test"), "memory://test"),
                new("run:failed"),
                new CodeIndexingOptions()));

        Assert.Equal(0, fallback.ExecutionCount);
    }

    private sealed class ConfigurablePlugin(
        string id,
        string language,
        CodeGraphCapabilities capabilities,
        Func<string, bool>? canHandle = null,
        string? nodeName = null) : ICodeGraphPlugin
    {
        private readonly Func<string, bool> _canHandle =
            canHandle ?? (path => path.EndsWith(".cs", StringComparison.Ordinal));

        public CodePluginId Id { get; } = new(id);
        public string Version => "1.0.0";
        public string Language => language;
        public IReadOnlyCollection<string> FileExtensions => [".cs"];
        public CodeGraphCapabilities Capabilities => capabilities;
        public string? NodeName { get; } = nodeName;
        public Func<CodeGraphPluginContext, Exception?>? FailWith { get; set; }
        public bool EmitBeforeFailure { get; set; }
        public int ExecutionCount { get; private set; }

        public bool CanHandle(string path) => _canHandle(path);

        public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
            CodeGraphPluginContext context,
            CancellationToken cancellationToken = default) =>
            new(new Session(context, this));

        private sealed class Session(
            CodeGraphPluginContext context,
            ConfigurablePlugin plugin) : ICodeGraphExtractionSession
        {
            public async ValueTask<CodeGraphExtractionResult> ExtractAsync(
                ICodeGraphSink sink,
                CancellationToken cancellationToken = default)
            {
                plugin.ExecutionCount++;
                if (plugin.EmitBeforeFailure)
                    await EmitAsync(sink, cancellationToken).ConfigureAwait(false);
                var failure = plugin.FailWith?.Invoke(context);
                if (failure is not null)
                    throw failure;
                if (plugin.NodeName is null || context.Sources.Count == 0)
                    return new CodeGraphExtractionResult();
                await EmitAsync(sink, cancellationToken).ConfigureAwait(false);
                return new(
                    sourcesExamined: context.Sources.Count,
                    sourcesContributingFacts: context.Sources.Count);
            }

            private async ValueTask EmitAsync(ICodeGraphSink sink, CancellationToken cancellationToken)
            {
                var origin = new CodeFactOrigin(
                    context.RepositoryId,
                    plugin.Id,
                    plugin.Version,
                    context.IndexRunId,
                    new CodeIndexUnitId($"unit:{plugin.Id.Value}"));
                await sink.WriteBatchAsync(
                    new CodeGraphBatch(
                        origin,
                        nodes:
                        [
                            new CodeGraphNode(
                                new CodeNodeId($"node:{plugin.Id.Value}"),
                                CodeNodeKinds.Type,
                                plugin.NodeName!)
                        ],
                        completesIndexUnit: true),
                    cancellationToken);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class LifecyclePlugin : ICodeGraphPlugin
    {
        public CodePluginId Id { get; } = new("plugin:csharp");
        public string Version => "1.0.0";
        public string Language => "csharp";
        public IReadOnlyCollection<string> FileExtensions => [".cs"];
        public CodeGraphCapabilities Capabilities => CodeGraphCapabilities.Syntax;
        public bool Fail { get; set; }
        public bool Cancel { get; set; }
        public bool EmitForeignOrigin { get; set; }
        public int ExecutionCount { get; private set; }
        public IReadOnlyList<CodeGraphSourceChange> LastChanges { get; private set; } = [];

        public bool CanHandle(string path) => path.EndsWith(".cs", StringComparison.Ordinal);

        public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
            CodeGraphPluginContext context,
            CancellationToken cancellationToken = default)
        {
            LastChanges = context.Changes;
            return new(new Session(context, this));
        }

        private sealed class Session(
            CodeGraphPluginContext context,
            LifecyclePlugin plugin) : ICodeGraphExtractionSession
        {
            public async ValueTask<CodeGraphExtractionResult> ExtractAsync(
                ICodeGraphSink sink,
                CancellationToken cancellationToken = default)
            {
                plugin.ExecutionCount++;
                if (plugin.Fail)
                    throw new InvalidOperationException("plugin failure");
                if (plugin.Cancel)
                    throw new OperationCanceledException("plugin cancellation");
                if (context.Sources.Count == 0)
                    return new([new CodeIndexUnitId("unit:example")]);

                var origin = new CodeFactOrigin(
                    context.RepositoryId,
                    plugin.EmitForeignOrigin ? new CodePluginId("plugin:foreign") : plugin.Id,
                    plugin.Version,
                    context.IndexRunId,
                    new CodeIndexUnitId("unit:example"));
                await sink.WriteBatchAsync(
                    new CodeGraphBatch(
                        origin,
                        nodes:
                        [
                            new CodeGraphNode(
                                new CodeNodeId("node:example"),
                                CodeNodeKinds.Type,
                                "Example")
                        ],
                        completesIndexUnit: true),
                    cancellationToken);
                return new(
                    sourcesExamined: context.Sources.Count,
                    sourcesContributingFacts: context.Sources.Count,
                    unresolvedRelationships: 2,
                    warningCodes: ["test.unresolved"]);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class MemoryProvider(Dictionary<string, string> files) : ICodeRepositoryProvider
    {
        private int _openCount;

        public string Name => "memory";
        public bool ChangeContentOnSecondOpen { get; init; }
        public int OpenCount => _openCount;
        public bool CanOpen(CodeRepositoryDescriptor repository) =>
            repository.Location.StartsWith("memory://", StringComparison.Ordinal);
        public ValueTask<ICodeRepositorySource> OpenAsync(
            CodeRepositoryDescriptor repository,
            CancellationToken cancellationToken = default) =>
            new(new Source(repository.Id, this, files));

        private Stream Open(CodeRepositoryEntry entry)
        {
            var content = files[entry.Path];
            var openCount = Interlocked.Increment(ref _openCount);
            if (ChangeContentOnSecondOpen && openCount == 2)
                content += "-changed";
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private sealed class Source(
            CodeRepositoryId repositoryId,
            MemoryProvider provider,
            Dictionary<string, string> files) : ICodeRepositorySource
        {
            public CodeRepositoryId RepositoryId { get; } = repositoryId;
            public string ProviderName => "memory";
            public string? SnapshotIdentity => "snapshot:test";
            public bool IsConsistentSnapshot => true;

            public async IAsyncEnumerable<CodeRepositoryEntry> EnumerateAsync(
                CodeRepositoryEnumerationOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                foreach (var (path, content) in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new(path, Encoding.UTF8.GetByteCount(content));
                    await Task.Yield();
                }
            }

            public ValueTask<Stream> OpenReadAsync(
                CodeRepositoryEntry entry,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new(provider.Open(entry));
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class ConcurrentPlugin(
        string id,
        string extension,
        ConcurrencyProbe probe) : ICodeGraphPlugin
    {
        public CodePluginId Id { get; } = new(id);
        public string Version => "1.0.0";
        public string Language => id;
        public IReadOnlyCollection<string> FileExtensions => [extension];
        public CodeGraphCapabilities Capabilities => CodeGraphCapabilities.Syntax;
        public bool CanHandle(string path) => path.EndsWith(extension, StringComparison.Ordinal);

        public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
            CodeGraphPluginContext context,
            CancellationToken cancellationToken = default) =>
            new(new Session(context, this, probe));

        private sealed class Session(
            CodeGraphPluginContext context,
            ConcurrentPlugin plugin,
            ConcurrencyProbe probe) : ICodeGraphExtractionSession
        {
            public async ValueTask<CodeGraphExtractionResult> ExtractAsync(
                ICodeGraphSink sink,
                CancellationToken cancellationToken = default)
            {
                await probe.EnterAsync(cancellationToken);
                try
                {
                    await sink.WriteBatchAsync(
                        new CodeGraphBatch(
                            new CodeFactOrigin(
                                context.RepositoryId,
                                plugin.Id,
                                plugin.Version,
                                context.IndexRunId,
                                new CodeIndexUnitId($"unit:{plugin.Id.Value}")),
                            completesIndexUnit: true),
                        cancellationToken);
                    return new();
                }
                finally
                {
                    probe.Exit();
                }
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class ConcurrencyProbe(int expectedConcurrency)
    {
        private readonly TaskCompletionSource _reached = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _active;
        private int _maximumObserved;

        public int MaximumObserved => _maximumObserved;

        public async ValueTask EnterAsync(CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _active);
            int observed;
            do
            {
                observed = _maximumObserved;
                if (active <= observed)
                    break;
            }
            while (Interlocked.CompareExchange(ref _maximumObserved, active, observed) != observed);

            if (active >= expectedConcurrency)
                _reached.TrySetResult();
            await _reached.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }

        public void Exit() => Interlocked.Decrement(ref _active);
    }
}
