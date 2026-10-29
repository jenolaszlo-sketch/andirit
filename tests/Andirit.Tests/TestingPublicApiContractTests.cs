using Andirit.Testing;

namespace Andirit.Tests;

public sealed class TestingPublicApiContractTests
{
    [Fact]
    public void Testing_PublicTypeSnapshot_IsIntentional()
    {
        var actual = typeof(CodeGraphStoreConformanceSuite).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "Andirit.Testing.CodeGraphStoreConformanceException",
            "Andirit.Testing.CodeGraphStoreConformanceReport",
            "Andirit.Testing.CodeGraphStoreConformanceSuite",
            "Andirit.Testing.ICodeGraphStoreFixture"
        };

        Assert.Equal(expected, actual);
    }
}
