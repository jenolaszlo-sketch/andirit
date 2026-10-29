namespace Andirit.TreeSitter.Tests;

public sealed class HistoricalProviderIdentityTests
{
    [Fact]
    public void ProductRename_PreservesPersistedPluginsResolversExportsAndProfile()
    {
        // These strings participate in stored unit/fact identity and provenance.
        Assert.Equal("penghou.hetu.tree-sitter.typescript", new TreeSitterTypeScriptPlugin().Id.Value);
        Assert.Equal("penghou.hetu.tree-sitter.javascript", new TreeSitterJavaScriptPlugin().Id.Value);
        Assert.Equal("penghou.hetu.tree-sitter.elixir", new TreeSitterElixirPlugin().Id.Value);
        Assert.Equal("penghou.hetu.tree-sitter.ecmascript-module-resolver", EcmaScriptModuleResolver.ResolverProvider);
        Assert.Equal("penghou.hetu.tree-sitter.ecmascript-export-surface", EcmaScriptExportGraphPublisher.Provider);
        Assert.Equal(9, EcmaScriptExtractionProfile.ProfileRevision);
        Assert.Equal(2, EcmaScriptModuleResolver.ResolverRevision);
        Assert.Equal("relative-v1", EcmaScriptModuleResolver.Policy);
        Assert.Equal("exports1.local-v1", EcmaScriptExportGraphPublisher.ProviderRevision);
    }
}
