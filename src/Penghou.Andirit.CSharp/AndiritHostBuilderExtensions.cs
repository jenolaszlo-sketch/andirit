using Microsoft.Extensions.DependencyInjection;
using Penghou.Andirit;

namespace Penghou.Andirit.CSharp;

/// <summary>C#-specific extensions for <see cref="AndiritHostBuilder"/>.</summary>
public static class AndiritHostBuilderExtensions
{
    /// <summary>Adds the Roslyn-based C# extraction plugin.</summary>
    public static AndiritHostBuilder AddCSharpPlugin(this AndiritHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPlugin(new CSharpCodeGraphPlugin());
    }
}
