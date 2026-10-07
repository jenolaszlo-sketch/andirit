namespace Penghou.Hetu.Tests;

public sealed class ProviderFallbackTests
{
    [Theory]
    [InlineData(CodePluginFailureKind.UnsupportedLanguage, true)]
    [InlineData(CodePluginFailureKind.ProviderUnavailable, true)]
    [InlineData(CodePluginFailureKind.VersionMismatch, true)]
    [InlineData(CodePluginFailureKind.ProjectLoadFailure, false)]
    [InlineData(CodePluginFailureKind.ParseFailure, false)]
    [InlineData(CodePluginFailureKind.SemanticModelUnavailable, false)]
    [InlineData(CodePluginFailureKind.ProviderCrash, false)]
    [InlineData(CodePluginFailureKind.PartialResult, false)]
    [InlineData(CodePluginFailureKind.Cancelled, false)]
    [InlineData(CodePluginFailureKind.SourceChanged, false)]
    public void OnUnavailable_FallsBackOnlyForUnavailableCapability(
        CodePluginFailureKind kind,
        bool expected)
    {
        Assert.Equal(expected, ProviderFallbackPolicy.OnUnavailable.IsFallbackEligible(kind));
    }

    [Theory]
    [InlineData(CodePluginFailureKind.UnsupportedLanguage, true)]
    [InlineData(CodePluginFailureKind.ProviderUnavailable, true)]
    [InlineData(CodePluginFailureKind.VersionMismatch, true)]
    [InlineData(CodePluginFailureKind.ProjectLoadFailure, true)]
    [InlineData(CodePluginFailureKind.ParseFailure, true)]
    [InlineData(CodePluginFailureKind.SemanticModelUnavailable, true)]
    [InlineData(CodePluginFailureKind.ProviderCrash, true)]
    [InlineData(CodePluginFailureKind.PartialResult, true)]
    [InlineData(CodePluginFailureKind.Cancelled, false)]
    [InlineData(CodePluginFailureKind.SourceChanged, false)]
    public void OnFailure_FallsBackForAnalysisFailuresButNeverCancellationOrConflict(
        CodePluginFailureKind kind,
        bool expected)
    {
        Assert.Equal(expected, ProviderFallbackPolicy.OnFailure.IsFallbackEligible(kind));
    }

    [Fact]
    public void None_NeverFallsBack()
    {
        foreach (var kind in Enum.GetValues<CodePluginFailureKind>())
            Assert.False(ProviderFallbackPolicy.None.IsFallbackEligible(kind));
    }

    [Fact]
    public void FailureKinds_HaveStableWarningCodes()
    {
        Assert.Equal("plugin.unavailable", CodePluginFailureKind.ProviderUnavailable.ToWarningCode());
        Assert.Equal("plugin.project-load-failure", CodePluginFailureKind.ProjectLoadFailure.ToWarningCode());
        Assert.Equal("plugin.failed", CodePluginFailureKind.ProviderCrash.ToWarningCode());
        Assert.Equal("plugin.cancelled", CodePluginFailureKind.Cancelled.ToWarningCode());
    }

    [Fact]
    public void Selection_RequiresNonemptyAttempts()
    {
        Assert.Throws<ArgumentException>(() => new CodeProviderSelection(
            "csharp",
            new CodePluginId("plugin:preferred"),
            [],
            new CodePluginId("plugin:fallback"),
            CodePluginFailureKind.ProviderUnavailable,
            "plugin.unavailable",
            CodeGraphCapabilities.References));
    }

    [Fact]
    public void Selection_RecordsAttemptHistoryBeyondRequestedActual()
    {
        var selection = new CodeProviderSelection(
            "csharp",
            new CodePluginId("plugin:preferred"),
            [new CodePluginId("plugin:preferred"), new CodePluginId("plugin:middle"), new CodePluginId("plugin:last")],
            new CodePluginId("plugin:last"),
            CodePluginFailureKind.ProviderUnavailable,
            "plugin.unavailable",
            CodeGraphCapabilities.References | CodeGraphCapabilities.Calls);

        Assert.Equal(
            ["plugin:preferred", "plugin:middle", "plugin:last"],
            selection.Attempted.Select(id => id.Value));
        Assert.Equal("plugin:last", selection.Selected!.Value);
        Assert.True(selection.LostCapabilities.HasFlag(CodeGraphCapabilities.Calls));
    }

    [Fact]
    public void FailureException_CarriesKind()
    {
        var exception = new CodePluginFailureException(CodePluginFailureKind.ProjectLoadFailure);

        Assert.Equal(CodePluginFailureKind.ProjectLoadFailure, exception.FailureKind);
    }
}
