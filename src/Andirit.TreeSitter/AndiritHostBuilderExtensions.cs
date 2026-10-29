using Andirit;

namespace Andirit.TreeSitter;

/// <summary>Tree-sitter-specific extensions for <see cref="AndiritHostBuilder"/>.</summary>
public static class AndiritHostBuilderExtensions
{
    /// <summary>Adds the Tree-sitter structural Elixir extraction plugin.</summary>
    public static AndiritHostBuilder AddTreeSitterElixirPlugin(this AndiritHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPlugin(new TreeSitterElixirPlugin());
    }

    /// <summary>
    /// Adds the Tree-sitter structural TypeScript extraction plugin for
    /// <c>.ts</c>/<c>.mts</c>/<c>.cts</c>/<c>.tsx</c> files.
    /// </summary>
    public static AndiritHostBuilder AddTreeSitterTypeScriptPlugin(this AndiritHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPlugin(new TreeSitterTypeScriptPlugin());
    }

    /// <summary>
    /// Adds the Tree-sitter structural JavaScript extraction plugin for
    /// <c>.js</c>/<c>.mjs</c>/<c>.cjs</c>/<c>.jsx</c> files.
    /// </summary>
    public static AndiritHostBuilder AddTreeSitterJavaScriptPlugin(this AndiritHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPlugin(new TreeSitterJavaScriptPlugin());
    }
}
