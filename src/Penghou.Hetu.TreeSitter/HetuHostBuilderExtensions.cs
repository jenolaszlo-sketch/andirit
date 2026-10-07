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
}
