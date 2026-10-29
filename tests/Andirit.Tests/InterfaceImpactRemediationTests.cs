using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Andirit.Tests;

public sealed partial class InterfaceImpactTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Remediation_NestedImportIsRelevanceOnly_NotAVisibleBinding(bool durable)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import { Widget as Top } from './types';\nexport namespace Hidden { import { Widget as Local } from './types'; class Inside implements Local {} }\nexport class Outside implements Local {}\nexport class Correct implements Top {}\n");
        await fixture.Index();
        var target = await fixture.Target();
        var result = await fixture.Query(target);
        var confirmed = Assert.Single(result.ConfirmedImplementations);
        Assert.Equal("Correct", confirmed.ClassName);
        Assert.Equal(target, confirmed.TargetDeclarationNodeId);
        Assert.Equal(result.Target.DeclarationRecordId, confirmed.TargetDeclarationRecordId);
        Assert.Equal(new[] { "nested-scope-heritage", "unresolved-heritage-name" }, result.Unknowns.Select(u => u.Reason).Order(StringComparer.Ordinal));
        Assert.All(confirmed.Witnesses, w => Assert.DoesNotContain(w.Steps, s => s.Properties is not null && s.Properties.TryGetValue("ec6d.data", out var p) && p is CodeTextProperty t && t.Value.Contains("\"localName\":\"Local\"", StringComparison.Ordinal)));
        if (durable)
        {
            ((IDisposable)fixture.Store).Dispose();
            fixture.Store = new LatticeCodeGraphStore(fixture.Database);
            Assert.Equal(result.ToJson(), (await fixture.Query(target)).ToJson());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Remediation_RehashedAttributionCannotOmitPhysicalContributor(bool durable)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", "interface Widget {}\nconst Widget = 1;\nexport { Widget };\n");
        fixture.Write("consumer.ts", "import { Widget } from './types'; export class Ambiguous implements Widget {}\n");
        await fixture.Index();
        var target = await fixture.Target();
        var original = await fixture.Query(target);
        Assert.Empty(original.ConfirmedImplementations);
        Assert.Equal("ambiguous-export", Assert.Single(original.Unknowns).Reason);
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var record = units.SelectMany(u => u.Declarations).Single(d => d.SymbolNodeId == target);
        var altered = units.Select(u => new CodeIndexUnitReplacement(u.Origin, u.Nodes, u.Declarations, u.Edges.Select(e =>
        {
            if (e.Kind.Value != "export-attribution") return e;
            var data = RemediationData(e);
            if (data["exportedName"]!.GetValue<string>() != "Widget") return e;
            data["status"] = "attributed"; data["eligiblePhysicalDeclarationCount"] = 1;
            data["candidateDeclarationRecordIds"] = new JsonArray(record.Id.Value); data["ineligibleDeclarationRecordIds"] = new JsonArray();
            data["declarationRecordId"] = record.Id.Value; data["declarationNodeId"] = target.Value; data["symbolId"] = record.SymbolId.Value;
            return RemediationRehash(e, data);
        }).ToArray())).ToArray();
        var reader = Proxy(fixture.Store, (method, _) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(altered) : null);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Query(target, reader: reader));
        Assert.Contains("all physical contributors", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Remediation_AliasedExtendsIsUnknown_DirectImplementsStillConfirms(bool durable)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import type { Widget as Local } from './types'; export interface Child extends Local {} export class Correct implements Local {}\n");
        await fixture.Index();
        var target = await fixture.Target();
        var result = await fixture.Query(target);
        Assert.Equal("Correct", Assert.Single(result.ConfirmedImplementations).ClassName);
        Assert.Equal("heritage-kind-not-supported", Assert.Single(result.Unknowns).Reason);
        Assert.Equal("heritage-context", Assert.Single(result.Unknowns).Evidence[0].StepKind);
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var damaged = units.Select(u => new CodeIndexUnitReplacement(u.Origin, u.Nodes, u.Declarations, u.Edges.Where(e => e.Kind.Value != "implements").ToArray())).ToArray();
        var reader = Proxy(fixture.Store, (method, _) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(damaged) : null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Query(target, reader: reader));
    }

    private static JsonObject RemediationData(CodeGraphEdge edge) => JsonNode.Parse(((CodeTextProperty)edge.Properties["ec6d.data"]).Value)!.AsObject();
    private static CodeGraphEdge RemediationRehash(CodeGraphEdge edge, JsonObject data)
    {
        var payload = data.ToJsonString();
        var id = "typescript:ec6d:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{edge.Kind.Value}\n{edge.SourceId.Value}\n{edge.TargetId.Value}\n{payload}")));
        return new(new(id), edge.SourceId, edge.TargetId, edge.Kind, edge.Evidence, new Dictionary<string, CodePropertyValue> { ["ec6d.data"] = new CodeTextProperty(payload) });
    }
}
