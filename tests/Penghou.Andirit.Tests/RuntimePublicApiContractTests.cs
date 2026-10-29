namespace Penghou.Andirit.Tests;

public sealed class RuntimePublicApiContractTests
{
    [Fact]
    public void Runtime_PublicTypeSnapshot_IsIntentional()
    {
        var actual = typeof(ICodeGraphStore).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "Penghou.Andirit.CodeAffectedTestsResult",
            "Penghou.Andirit.CodeEnclosingSymbolResult",
            "Penghou.Andirit.CodeGraphBatchValidator",
            "Penghou.Andirit.CodeGraphDirection",
            "Penghou.Andirit.CodeGraphIngestionDiagnostics",
            "Penghou.Andirit.CodeGraphIngestionSink",
            "Penghou.Andirit.CodeGraphFactKind",
            "Penghou.Andirit.CodeGraphFactProvenance",
            "Penghou.Andirit.CodeGraphPluginRegistry",
            "Penghou.Andirit.CodeGraphPluginSelectionException",
            "Penghou.Andirit.CodeGraphQueryOptions",
            "Penghou.Andirit.CodeGraphQueryDescriptor",
            "Penghou.Andirit.CodeGraphQueryEnvelope`1",
            "Penghou.Andirit.CodeGraphQueryService",
            "Penghou.Andirit.CodeGraphPublication",
            "Penghou.Andirit.CodeGraphPublicationChangedException",
            "Penghou.Andirit.CodeGraphPublicationQuery",
            "Penghou.Andirit.CodeFreshnessResult",
            "Penghou.Andirit.CodeFreshnessStatus",
            "Penghou.Andirit.CodeGraphMultiTraversalResult",
            "Penghou.Andirit.CodeGraphStoreException",
            "Penghou.Andirit.CodeGraphStoreHealth",
            "Penghou.Andirit.CodeGraphStoreHealthStatus",
            "Penghou.Andirit.CodeGraphTraversalQuery",
            "Penghou.Andirit.CodeGraphTraversalResult",
            "Penghou.Andirit.CodeGraphTruncationReason",
            "Penghou.Andirit.CodeIndexRunManifest",
            "Penghou.Andirit.CodeIndexRunStatus",
            "Penghou.Andirit.CodeIndexPlan",
            "Penghou.Andirit.CodeIndexPlanItem",
            "Penghou.Andirit.CodeIndexPlanner",
            "Penghou.Andirit.CodeIndexPlanningOptions",
            "Penghou.Andirit.CodeIndexPlanStatus",
            "Penghou.Andirit.CodeIndexingDiagnostics",
            "Penghou.Andirit.CodeIndexingOptions",
            "Penghou.Andirit.CodeIndexingResult",
            "Penghou.Andirit.CodeIndexingService",
            "Penghou.Andirit.CodeLanguageProviderMap",
            "Penghou.Andirit.CodeIndexIdentity",
            "Penghou.Andirit.CodePluginIndexingDiagnostics",
            "Penghou.Andirit.CodePluginIndexingStatus",
            "Penghou.Andirit.CodeProviderSelection",
            "Penghou.Andirit.CodePublicationSnapshot",
            "Penghou.Andirit.CodePublicationSnapshotException",
            "Penghou.Andirit.CodeSymbolDiscoveryQuery",
            "Penghou.Andirit.CodeSymbolDiscoveryResult",
            "Penghou.Andirit.ProviderFallbackPolicy",
            "Penghou.Andirit.ProviderFallbackPolicyExtensions",
            "Penghou.Andirit.CodeQueryOperations",
            "Penghou.Andirit.CodeIndexUnitReplacement",
            "Penghou.Andirit.CodeNamePatternResult",
            "Penghou.Andirit.CodeRepositoryManifest",
            "Penghou.Andirit.CodeRepositoryDescriptor",
            "Penghou.Andirit.CodeRepositoryDiscoveryEventKind",
            "Penghou.Andirit.CodeRepositoryEntry",
            "Penghou.Andirit.CodeRepositoryEnumerationLimitException",
            "Penghou.Andirit.CodeRepositoryEnumerationOptions",
            "Penghou.Andirit.CodeRepositoryProviderNotFoundException",
            "Penghou.Andirit.CodeRepositoryProviderRegistry",
            "Penghou.Andirit.CodeRepositoryProviderSelectionException",
            "Penghou.Andirit.CodeRepositoryIndexState",
            "Penghou.Andirit.CodeSnapshotOptions",
            "Penghou.Andirit.CodeSourceManifest",
            "Penghou.Andirit.CodeSourceChangedDuringIndexingException",
            "Penghou.Andirit.CodeSourceSizeLimitException",
            "Penghou.Andirit.CodeSymbolLookupResult",
            "Penghou.Andirit.FileSystemCodeRepositoryProvider",
            "Penghou.Andirit.AndiritBuilder",
            "Penghou.Andirit.AndiritHost",
            "Penghou.Andirit.AndiritHostBuilder",
            "Penghou.Andirit.AndiritHostHealth",
            "Penghou.Andirit.ICodeGraphIndexStore",
            "Penghou.Andirit.ICodeGraphReader",
            "Penghou.Andirit.ICodeGraphStore",
            "Penghou.Andirit.ICodeGraphStoreHealthCheck",
            "Penghou.Andirit.ICodeRepositoryProvider",
            "Penghou.Andirit.ICodeRepositorySource",
            "Penghou.Andirit.InMemoryCodeGraphStore"
        }.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
    }
}
