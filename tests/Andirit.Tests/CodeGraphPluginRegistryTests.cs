namespace Andirit.Tests;

public sealed class CodeGraphPluginRegistryTests
{
    [Fact]
    public void Registry_OrdersPluginsDeterministically()
    {
        var registry = new CodeGraphPluginRegistry(
        [
            new FakePlugin("plugin:z", "typescript", ".ts"),
            new FakePlugin("plugin:b", "csharp", ".csx"),
            new FakePlugin("plugin:a", "csharp", ".cs")
        ]);

        Assert.Equal(
            ["plugin:a", "plugin:b", "plugin:z"],
            registry.Plugins.Select(plugin => plugin.Id.Value));
    }

    [Fact]
    public void Resolve_ReturnsNullWhenNoPluginClaimsPath()
    {
        var registry = new CodeGraphPluginRegistry(
            [new FakePlugin("plugin:csharp", "csharp", ".cs")]);

        Assert.Null(registry.Resolve("README.md"));
    }

    [Fact]
    public void Resolve_ReportsAmbiguousClaimsRatherThanGuessing()
    {
        var registry = new CodeGraphPluginRegistry(
        [
            new FakePlugin("plugin:first", "csharp", ".cs"),
            new FakePlugin("plugin:second", "csharp", ".cs")
        ]);

        var exception = Assert.Throws<CodeGraphPluginSelectionException>(() =>
            registry.Resolve("Example.cs"));

        Assert.Equal(
            ["plugin:first", "plugin:second"],
            exception.CandidateIds.Select(id => id.Value));
    }

    [Fact]
    public void Registry_RejectsDuplicatePluginIdentity()
    {
        Assert.Throws<ArgumentException>(() => new CodeGraphPluginRegistry(
        [
            new FakePlugin("plugin:same", "csharp", ".cs"),
            new FakePlugin("plugin:same", "typescript", ".ts")
        ]));
    }

    [Fact]
    public void Resolve_WithoutMap_PreservesStrictAmbiguity()
    {
        var registry = new CodeGraphPluginRegistry(
        [
            new FakePlugin("plugin:first", "csharp", ".cs"),
            new FakePlugin("plugin:second", "csharp", ".cs")
        ]);

        Assert.Throws<CodeGraphPluginSelectionException>(() =>
            registry.Resolve("Example.cs"));
    }

    [Fact]
    public void Resolve_WithMap_SelectsPreferredCandidate()
    {
        var registry = new CodeGraphPluginRegistry(
        [
            new FakePlugin("plugin:fallback", "csharp", ".cs"),
            new FakePlugin("plugin:preferred", "csharp", ".cs")
        ],
        Map("csharp", "plugin:preferred", "plugin:fallback"));

        Assert.Equal(
            "plugin:preferred",
            registry.Resolve("Example.cs")!.Id.Value);
    }

    [Fact]
    public void Resolve_WithMap_SkipsPreferredWhenItDeclinesThePath()
    {
        var registry = new CodeGraphPluginRegistry(
        [
            new FakePlugin(
                "plugin:preferred",
                "csharp",
                ".cs",
                canHandle: path => !path.EndsWith(".g.cs", StringComparison.Ordinal)),
            new FakePlugin("plugin:fallback", "csharp", ".cs")
        ],
        Map("csharp", "plugin:preferred", "plugin:fallback"));

        Assert.Equal(
            "plugin:preferred",
            registry.Resolve("Example.cs")!.Id.Value);
        Assert.Equal(
            "plugin:fallback",
            registry.Resolve("Generated.g.cs")!.Id.Value);
    }

    [Fact]
    public void Resolve_WithMap_RejectsUnlistedCandidateForMappedLanguage()
    {
        var registry = new CodeGraphPluginRegistry(
        [
            new FakePlugin("plugin:preferred", "csharp", ".cs"),
            new FakePlugin("plugin:unlisted", "csharp", ".cs")
        ],
        Map("csharp", "plugin:preferred"));

        Assert.Throws<CodeGraphPluginSelectionException>(() =>
            registry.Resolve("Example.cs"));
    }

    [Fact]
    public void Resolve_WithMap_KeepsLegacyBehaviorForUnmappedLanguages()
    {
        var registry = new CodeGraphPluginRegistry(
        [
            new FakePlugin("plugin:preferred", "csharp", ".cs"),
            new FakePlugin("plugin:first", "typescript", ".ts"),
            new FakePlugin("plugin:second", "typescript", ".ts")
        ],
        Map("csharp", "plugin:preferred"));

        Assert.Equal(
            "plugin:preferred",
            registry.Resolve("Example.cs")!.Id.Value);
        Assert.Throws<CodeGraphPluginSelectionException>(() =>
            registry.Resolve("Example.ts"));
    }

    [Fact]
    public void Registry_RejectsMapEntriesForUnregisteredPlugins()
    {
        Assert.Throws<ArgumentException>(() => new CodeGraphPluginRegistry(
            [new FakePlugin("plugin:only", "csharp", ".cs")],
            Map("csharp", "plugin:only", "plugin:missing")));
    }

    [Fact]
    public void TryGetFallback_ReturnsNextProviderInOrder()
    {
        var preferred = new FakePlugin("plugin:preferred", "csharp", ".cs");
        var registry = new CodeGraphPluginRegistry(
            [preferred, new FakePlugin("plugin:fallback", "csharp", ".cs")],
            Map("csharp", "plugin:preferred", "plugin:fallback"));

        Assert.True(registry.TryGetFallback(preferred, out var fallback));
        Assert.Equal("plugin:fallback", fallback!.Id.Value);
        Assert.False(registry.TryGetFallback(fallback, out _));
    }

    [Fact]
    public void TryGetFallback_ReturnsFalseWithoutMapEntry()
    {
        var registry = new CodeGraphPluginRegistry(
            [new FakePlugin("plugin:only", "csharp", ".cs")]);

        Assert.False(registry.TryGetFallback(registry.Plugins[0], out _));
    }

    private static CodeLanguageProviderMap Map(string language, params string[] providerIds) =>
        new(new Dictionary<string, IReadOnlyList<CodePluginId>>
        {
            [language] = providerIds.Select(id => new CodePluginId(id)).ToArray()
        });

    private sealed class FakePlugin(
        string id,
        string language,
        string extension,
        Func<string, bool>? canHandle = null) : ICodeGraphPlugin
    {
        public CodePluginId Id { get; } = new(id);
        public string Version => "1.0.0";
        public string Language => language;
        public IReadOnlyCollection<string> FileExtensions => [extension];
        public CodeGraphCapabilities Capabilities => CodeGraphCapabilities.Syntax;

        public bool CanHandle(string path) =>
            canHandle?.Invoke(path) ??
            path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);

        public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(
            CodeGraphPluginContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
