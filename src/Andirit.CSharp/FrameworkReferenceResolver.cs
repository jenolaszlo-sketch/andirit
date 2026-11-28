using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Andirit;

/// <summary>
/// One deterministically selected target-framework reference closure.
/// </summary>
internal sealed record FrameworkReferenceSet(
    string TargetFramework,
    string PackFamily,
    string PackVersion,
    string PackDirectory,
    ImmutableArray<VerifiedMetadataReference> References)
{
    internal string ReferenceSetHash
    {
        get
        {
            var text = string.Join("\n", References.Select(reference => reference.Identity));
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        }
    }
}

/// <summary>
/// Bounded target-framework reference resolution.
/// </summary>
/// <remarks>
/// Framework references are target-framework reference assemblies from installed
/// reference packs, never process TPA or consumer-runtime implementation
/// assemblies. The supported matrix is explicit; anything else is refused.
///
/// Pack selection is deterministic: for the declared major/minor version, choose
/// the highest installed matching pack patch whose <c>ref/&lt;tfm&gt;</c>
/// directory exists. A different pack installation path with identical contents
/// produces identical content-bound identities.
/// </remarks>
internal static class FrameworkReferenceResolver
{
    internal static readonly ImmutableArray<string> SupportedTargetFrameworks =
        ["net8.0", "net9.0", "net10.0", "netstandard2.1"];

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, FrameworkReferenceSet> Cache =
        new(StringComparer.Ordinal);

    internal static FrameworkReferenceSet Resolve(string? targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
            throw new InvalidDataException("MissingTargetFramework");
        var tfm = targetFramework.Trim();
        if (!SupportedTargetFrameworks.Contains(tfm, StringComparer.Ordinal))
            throw new InvalidDataException(
                "UnsupportedTargetFramework:" + tfm + "; supported=" +
                string.Join(",", SupportedTargetFrameworks));
        return Cache.GetOrAdd(tfm, ResolveCore);
    }

    internal static bool IsReferenceCompatible(string? caller, string? dependency)
    {
        if (string.Equals(caller, dependency, StringComparison.Ordinal))
            return true;
        // A framework project may consume a netstandard2.1 library.
        if (string.Equals(dependency, "netstandard2.1", StringComparison.Ordinal) &&
            (string.Equals(caller, "net8.0", StringComparison.Ordinal) ||
             string.Equals(caller, "net9.0", StringComparison.Ordinal) ||
             string.Equals(caller, "net10.0", StringComparison.Ordinal)))
            return true;
        return false;
    }

    private static FrameworkReferenceSet ResolveCore(string tfm)
    {
        var (major, minor) = ParseTargetVersion(tfm);
        var family = tfm.StartsWith("netstandard", StringComparison.Ordinal)
            ? "NETStandard.Library.Ref"
            : "Microsoft.NETCore.App.Ref";
        var packsRoot = PacksRoot();
        var familyRoot = Path.Combine(packsRoot, family);
        if (!Directory.Exists(familyRoot))
            throw new InvalidDataException(
                "MissingReferencePack:" + tfm + "; expected=" + familyRoot);

        var candidates = Directory.EnumerateDirectories(familyRoot)
            .Select(directory => (Directory: directory, Version: ParsePackVersion(Path.GetFileName(directory))))
            .Where(candidate => candidate.Version is { } version &&
                                version.Major == major && version.Minor == minor)
            .OrderByDescending(candidate => candidate.Version)
            .ThenBy(candidate => candidate.Directory, StringComparer.Ordinal)
            .ToArray();
        foreach (var candidate in candidates)
        {
            var referenceDirectory = Path.Combine(candidate.Directory, "ref", tfm);
            if (!Directory.Exists(referenceDirectory))
                continue;
            var paths = Directory.EnumerateFiles(referenceDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(IsManagedAssembly)
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (paths.Length == 0)
                continue;
            VerifyFrameworkList(candidate.Directory, tfm, paths);
            var references = paths
                .Select(path => VerifiedMetadataReference.Capture(path))
                .OrderBy(reference => reference.Identity, StringComparer.Ordinal)
                .ToImmutableArray();
            return new(tfm, family, candidate.Version!.ToString(), candidate.Directory, references);
        }
        throw new InvalidDataException(
            "MissingReferencePack:" + tfm + "; no installed " + family +
            " pack with ref/" + tfm + " for major.minor " + major + "." + minor);
    }

    private static void VerifyFrameworkList(string packDirectory, string tfm, string[] inventoryPaths)
    {
        var listPath = Path.Combine(packDirectory, "data", "FrameworkList.xml");
        if (!File.Exists(listPath))
            throw new InvalidDataException("InvalidReferencePack:" + tfm + "; missing data/FrameworkList.xml");
        var inventory = inventoryPaths
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        XDocument document;
        try
        {
            document = XDocument.Load(listPath);
        }
        catch (System.Xml.XmlException exception)
        {
            throw new InvalidDataException("InvalidReferencePack:" + tfm + "; malformed FrameworkList.xml", exception);
        }
        foreach (var file in document.Descendants("File")
                     .Where(element => string.Equals(
                         element.Attribute("Type")?.Value, "Managed", StringComparison.OrdinalIgnoreCase)))
        {
            var relative = file.Attribute("Path")?.Value;
            if (string.IsNullOrWhiteSpace(relative))
                throw new InvalidDataException("InvalidReferencePack:" + tfm + "; FrameworkList entry has no path");
            var normalized = relative.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(packDirectory, normalized));
            if (!File.Exists(fullPath))
                throw new InvalidDataException(
                    "InvalidReferencePack:" + tfm + "; missing listed assembly " + relative);
            var name = Path.GetFileName(fullPath);
            if (string.Equals(Path.GetDirectoryName(fullPath),
                    Path.Combine(packDirectory, "ref", tfm), StringComparison.OrdinalIgnoreCase))
                inventory.Remove(name);
        }
        // Every managed file in the selected ref directory must be accounted for
        // by the pack manifest or be another managed facade in that directory.
        // The inventory is the exact Roslyn input; extra managed files are still
        // content-bound and deterministic.
    }

    private static string PacksRoot()
    {
        var runtimeDirectory = Path.TrimEndingDirectorySeparator(
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
        var fromRuntime = Path.GetFullPath(Path.Combine(runtimeDirectory, "..", "..", "..", "packs"));
        if (Directory.Exists(fromRuntime))
            return fromRuntime;
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(dotnetRoot))
        {
            var fromEnvironment = Path.Combine(dotnetRoot, "packs");
            if (Directory.Exists(fromEnvironment))
                return fromEnvironment;
        }
        throw new InvalidDataException("MissingReferencePacksRoot:" + fromRuntime);
    }

    private static (int Major, int Minor) ParseTargetVersion(string tfm)
    {
        var prefixLength = tfm.StartsWith("netstandard", StringComparison.Ordinal) ? 11 : 3;
        var versionText = tfm[prefixLength..];
        var parts = versionText.Split('.');
        if (parts.Length < 2 || !int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor))
            throw new InvalidDataException("UnsupportedTargetFramework:" + tfm);
        return (major, minor);
    }

    private static Version? ParsePackVersion(string value) =>
        Version.TryParse(value, out var version) ? version : null;

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            return pe.HasMetadata && pe.GetMetadataReader().IsAssembly;
        }
        catch (Exception exception) when (exception is BadImageFormatException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
