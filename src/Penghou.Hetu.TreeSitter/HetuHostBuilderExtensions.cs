using Penghou.Hetu;

namespace Penghou.Hetu.TreeSitter;

/// <summary>Tree-sitter-specific extensions for <see cref="HetuHostBuilder"/>.</summary>
public static class HetuHostBuilderExtensions
{
    /// <summary>Adds the Tree-sitter structural Elixir extraction plugin.</summary>
    public static HetuHostBuilder AddTreeSitterElixirPlugin(this HetuHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPlugin(new TreeSitterElixirPlugin());
    }

    /// <summary>
    /// Adds the Tree-sitter structural TypeScript extraction plugin for
    /// <c>.ts</c>/<c>.mts</c>/<c>.cts</c>/<c>.tsx</c> files.
    /// </summary>
    public static HetuHostBuilder AddTreeSitterTypeScriptPlugin(this HetuHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPlugin(new TreeSitterTypeScriptPlugin());
    }

    /// <summary>
    /// Adds the Tree-sitter structural JavaScript extraction plugin for
    /// <c>.js</c>/<c>.mjs</c>/<c>.cjs</c>/<c>.jsx</c> files.
    /// </summary>
    public static HetuHostBuilder AddTreeSitterJavaScriptPlugin(this HetuHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPlugin(new TreeSitterJavaScriptPlugin());
    }
}
