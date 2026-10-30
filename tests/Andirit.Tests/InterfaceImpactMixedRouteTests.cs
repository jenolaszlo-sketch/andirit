using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Andirit.Tests;

public sealed partial class InterfaceImpactTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Remediation_MixedRoutesRetainPerWitnessMetadataAndCanonicalSummary(bool durable)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", "import type { Widget as Alias } from './types';\nexport interface Widget {}\nexport class Multi implements Widget, Alias {}\n");
        await fixture.Index(); var target = await fixture.Target();
        var result = await fixture.Query(target);
        var implementation = Assert.Single(result.ConfirmedImplementations);
        Assert.Equal("Multi", implementation.ClassName); Assert.Equal(target, implementation.TargetDeclarationNodeId);
        Assert.Equal(result.Target.DeclarationRecordId, implementation.TargetDeclarationRecordId);
        Assert.Empty(result.Unknowns); Assert.Equal(2, implementation.Witnesses.Count);
        var sameFile = Assert.Single(implementation.Witnesses, w => w.BindingRoute == "same-file");
        var imported = Assert.Single(implementation.Witnesses, w => w.BindingRoute == "import");
        Assert.Equal(new[] { "heritage", "context", "declaration" }, sameFile.Steps.Select(s => s.StepKind));
        Assert.Equal(new[] { "heritage", "context", "import", "resolution", "export-entry", "export-occurrence", "attribution", "declaration" }, imported.Steps.Select(s => s.StepKind));
        Assert.Equal(sameFile.HeritageSpan, implementation.HeritageSpan); Assert.Equal("same-file", implementation.BindingRoute);
        Assert.NotEqual(sameFile.HeritageSpan, imported.HeritageSpan);
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var ids = units.SelectMany(u => u.Nodes.Select(n => n.Id.Value).Concat(u.Declarations.Select(d => d.Id.Value)).Concat(u.Edges.Select(e => e.Id.Value))).ToHashSet(StringComparer.Ordinal);
        foreach (var witness in implementation.Witnesses)
        {
            Assert.Equal(witness.Steps[0].Evidence!.Location, witness.HeritageSpan);
            Assert.All(witness.Steps, step => { Assert.Contains(step.FactId, ids); Assert.NotNull(step.Origin); });
            var tuple = witness.Steps.Select(s => s.FactId).Append(target.Value).Append("binding1.local-v1").ToArray();
            Assert.Equal("binding1.local-v1:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tuple)))), witness.WitnessId);
        }
        Assert.Equal(result.ToJson(), InterfaceImpactResultV1.FromJson(result.ToJson()).ToJson());
        var reader = Proxy(fixture.Store, (method, _) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(units.Reverse().Select(u => new CodeIndexUnitReplacement(u.Origin, u.Nodes.Reverse().ToArray(), u.Declarations.Reverse().ToArray(), u.Edges.Reverse().ToArray())).ToArray()) : null);
        Assert.Equal(result.ToJson(), (await fixture.Query(target, reader: reader)).ToJson());
        if (durable)
        {
            ((IDisposable)fixture.Store).Dispose(); fixture.Store = new LatticeCodeGraphStore(fixture.Database);
            Assert.Equal(result.ToJson(), (await fixture.Query(target)).ToJson());
        }
    }
}
