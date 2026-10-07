using System.Runtime.InteropServices;

namespace Penghou.Hetu;

/// <summary>
/// Resolves bundled Tree-sitter grammar natives to absolute paths.
/// Grammars ship inside this package under <c>runtimes/&lt;rid&gt;/native</c>;
/// the resolved absolute path is handed to the parsing runtime, so hosts
/// need no probing configuration and no network access. Only the runtimes
/// this package ships are supported; anything else is an honest
/// <see cref="CodePluginFailureKind.ProviderUnavailable"/> instead of a
/// loader crash.
/// </summary>
internal static class TreeSitterNative
{
    internal static string ResolveGrammarLibrary(TreeSitterGrammarDescriptor grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        var (rid, fileName) = RuntimeLibrary(grammar.NativeLibrary);
        var assemblyDirectory = Path.GetDirectoryName(typeof(TreeSitterNative).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            var packaged = Path.Combine(assemblyDirectory, "runtimes", rid, "native", fileName);
            if (File.Exists(packaged))
                return packaged;

            var adjacent = Path.Combine(assemblyDirectory, fileName);
            if (File.Exists(adjacent))
                return adjacent;
        }

        throw new CodePluginFailureException(
            CodePluginFailureKind.ProviderUnavailable,
            $"The {grammar.NativeLibrary} native library for runtime '{rid}' was not found next to the Tree-sitter integration.");
    }

    private static (string Rid, string FileName) RuntimeLibrary(string baseName)
    {
        if (OperatingSystem.IsWindows() && Architecture.X64 == RuntimeInformation.ProcessArchitecture)
            return ("win-x64", $"{baseName}.dll");
        if (OperatingSystem.IsLinux() && Architecture.X64 == RuntimeInformation.ProcessArchitecture)
            return ("linux-x64", $"lib{baseName}.so");
        if (OperatingSystem.IsMacOS() && Architecture.Arm64 == RuntimeInformation.ProcessArchitecture)
            return ("osx-arm64", $"lib{baseName}.dylib");

        throw new CodePluginFailureException(
            CodePluginFailureKind.ProviderUnavailable,
            $"The Tree-sitter integration ships no {baseName} native library for " +
            $"'{RuntimeInformation.OSDescription}/{RuntimeInformation.ProcessArchitecture}'.");
    }
}
