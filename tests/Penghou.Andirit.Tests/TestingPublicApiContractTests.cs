using Penghou.Andirit.Testing;

namespace Penghou.Andirit.Tests;

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
            "Penghou.Andirit.Testing.CodeGraphStoreConformanceException",
            "Penghou.Andirit.Testing.CodeGraphStoreConformanceReport",
            "Penghou.Andirit.Testing.CodeGraphStoreConformanceSuite",
            "Penghou.Andirit.Testing.ICodeGraphStoreFixture"
        };

        Assert.Equal(expected, actual);
    }
}
