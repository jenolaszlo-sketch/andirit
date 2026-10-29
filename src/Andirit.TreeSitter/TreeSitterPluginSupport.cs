using System.Reflection;

namespace Andirit;

/// <summary>Shared scaffolding for Tree-sitter language plugins.</summary>
internal static class TreeSitterPluginSupport
{
    /// <summary>
    /// Gets the package version (without build metadata) that anchors every
    /// provider identity, so a package release invalidates previous indexes.
    /// </summary>
    internal static string PackageVersion(Type anchor)
    {
        var assembly = anchor.Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
            return informational.Split('+', 2)[0];

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    /// <summary>Gets whether a path carries one of the normalized extensions.</summary>
    internal static bool HasExtension(string path, IReadOnlyCollection<string> extensions)
    {
        foreach (var extension in extensions)
        {
            if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
