using Andirit;

namespace Andirit.LatticeDb.Tests;

public sealed class LatticeDbPublicApiContractTests
{
    [Fact]
    public void LatticeDb_PublicTypeSnapshot_IsIntentional()
    {
        var actual = typeof(LatticeCodeGraphStore).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "Andirit.LatticeCodeGraphSchemaException",
            "Andirit.LatticeCodeGraphStore",
            "Andirit.LatticeDb.AndiritHostBuilderExtensions",
            "Andirit.LatticeDb.LatticeDbStoreOptions"
        }.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
    }
}
