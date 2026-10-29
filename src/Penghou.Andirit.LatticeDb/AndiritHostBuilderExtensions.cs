namespace Penghou.Andirit.LatticeDb;

/// <summary>LatticeDB-specific extensions for <see cref="AndiritHostBuilder"/>.</summary>
public static class AndiritHostBuilderExtensions
{
    /// <summary>Configures the host to use a durable LatticeDB store at the supplied file path.</summary>
    public static AndiritHostBuilder UseLatticeStore(
        this AndiritHostBuilder builder,
        string databasePath,
        LatticeDbStoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        return builder.UseStore(() => new LatticeCodeGraphStore(databasePath, options));
    }
}
