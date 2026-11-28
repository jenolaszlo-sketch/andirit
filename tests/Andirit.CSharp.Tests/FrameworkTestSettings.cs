namespace Andirit.CSharp.Tests;

/// <summary>
/// Explicit target-framework configuration for synthetic test fixtures that do
/// not declare a TargetFramework property. Production projects must declare one;
/// loose/synthetic fixtures use this explicit caller setting instead of an
/// ambient runtime fallback.
/// </summary>
internal static class FrameworkTestSettings
{
    internal static IReadOnlyDictionary<string, string> Net10 { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["csharp.targetFramework"] = "net10.0",
        };
}
