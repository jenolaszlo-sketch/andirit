namespace Penghou.Hetu;

/// <summary>
/// Classifies why a language provider could not complete its analysis.
/// Fallback handles unavailable capability, not suspicious analysis:
/// only some kinds are eligible for fallback, depending on policy.
/// Cancellation and live-source conflicts never permit fallback.
/// </summary>
public enum CodePluginFailureKind
{
    /// <summary>The provider does not support the requested language.</summary>
    UnsupportedLanguage = 0,
    /// <summary>The provider cannot be reached or started.</summary>
    ProviderUnavailable = 1,
    /// <summary>The provider or grammar version is incompatible.</summary>
    VersionMismatch = 2,
    /// <summary>The project or workspace could not be loaded.</summary>
    ProjectLoadFailure = 3,
    /// <summary>Source parsing failed.</summary>
    ParseFailure = 4,
    /// <summary>No semantic model could be constructed for otherwise parsed sources.</summary>
    SemanticModelUnavailable = 5,
    /// <summary>The provider crashed or threw an unexpected error.</summary>
    ProviderCrash = 6,
    /// <summary>The provider produced only partial output it does not stand behind.</summary>
    PartialResult = 7,
    /// <summary>Extraction was cancelled. Fallback never applies.</summary>
    Cancelled = 8,
    /// <summary>Live sources changed during analysis. Fallback never applies.</summary>
    SourceChanged = 9
}

/// <summary>
/// Reports a classified language-provider failure. Plugins throw this to
/// let Hetu decide whether a configured fallback provider may take over.
/// Any other exception escapes as <see cref="CodePluginFailureKind.ProviderCrash"/>.
/// </summary>
public sealed class CodePluginFailureException : Exception
{
    public CodePluginFailureException(
        CodePluginFailureKind kind,
        string? message = null,
        Exception? innerException = null)
        : base(message ?? $"The language provider failed ({kind}).", innerException)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        FailureKind = kind;
    }

    public CodePluginFailureKind FailureKind { get; }
}
