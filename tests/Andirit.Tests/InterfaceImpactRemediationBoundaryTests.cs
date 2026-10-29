using System.Text.Json.Nodes;

namespace Andirit.Tests;

public sealed partial class InterfaceImpactTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Remediation_UnsupportedHeritageRemainsStoredAndObservable(bool durable, bool anonymous)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", anonymous ? "export interface Widget {}\n" : "export interface Widget<T> {}\n");
        fixture.Write("consumer.ts", anonymous ? "import { Widget } from './types'; export const Implementation = class implements Widget {};\n" : "import { Widget } from './types'; export class Generic implements Widget<string> {}\n");
        await fixture.Index();
        var target = await fixture.Target();
        var result = await fixture.Query(target);
        Assert.Empty(result.ConfirmedImplementations);
        var unknown = Assert.Single(result.Unknowns);
        Assert.Equal(anonymous ? "nested-scope-heritage" : "unsupported-heritage-form", unknown.Reason);
        var context = Assert.Single((await fixture.Store.GetPublishedUnitsAsync(fixture.Repository)).SelectMany(u => u.Edges), e => e.Kind.Value == "heritage-context");
        Assert.Equal("Widget", RemediationData(context)["terminalIdentifier"]!.GetValue<string>());
        Assert.Contains(unknown.Evidence, e => e.FactId == context.Id.Value && e.Origin.SourcePath == "consumer.ts" && e.Origin.SourceHash is not null);
        Assert.Equal(1, result.Coverage.HeritageObserved);
        if (anonymous) Assert.Equal("other", RemediationData(context)["ownerKind"]!.GetValue<string>());
        if (durable)
        {
            ((IDisposable)fixture.Store).Dispose(); fixture.Store = new LatticeCodeGraphStore(fixture.Database);
            Assert.Equal(result.ToJson(), (await fixture.Query(target)).ToJson());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Remediation_ImportEqualsPreservesTypeOnlyWithoutBinding(bool durable)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import type Typed = require('./types');\nimport Ordinary = require('./types');\nexport class A implements Typed {}\nexport class B implements Ordinary {}\n");
        await fixture.Index();
        var result = await fixture.Query(await fixture.Target());
        Assert.Empty(result.ConfirmedImplementations); Assert.Empty(result.PossibleConsumers);
        Assert.Equal(2, result.Unknowns.Count); Assert.All(result.Unknowns, u => Assert.Equal("unsupported-import-form", u.Reason));
        var imports = (await fixture.Store.GetPublishedUnitsAsync(fixture.Repository)).SelectMany(u => u.Edges).Where(e => e.Kind.Value == "imports-name").Select(RemediationData).ToArray();
        Assert.True(Assert.Single(imports, d => d["localName"]!.GetValue<string>() == "Typed")["isTypeOnly"]!.GetValue<bool>());
        Assert.False(Assert.Single(imports, d => d["localName"]!.GetValue<string>() == "Ordinary")["isTypeOnly"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 100000)]
    [InlineData(true, 100000)]
    public async Task Remediation_MandatoryInputValidationPrecedesRelationshipBudget(bool durable, int limit)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", "export interface Widget {}\n");
        await fixture.Index(); var target = await fixture.Target();
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var altered = units.Select(u => new CodeIndexUnitReplacement(u.Origin, u.Nodes, u.Declarations, u.Edges.Where(e => e.Kind.Value != "interface-profile").ToArray())).ToArray();
        var reader = Proxy(fixture.Store, (method, _) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(altered) : null);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Query(target, new(maxCandidateScans: limit), reader));
        var valid = await fixture.Query(target, new(maxCandidateScans: limit));
        Assert.Equal(target, valid.Target.DeclarationNodeId); Assert.NotNull(valid.Target.DeclarationRecordId); Assert.NotNull(valid.Target.Span); Assert.Equal("types.ts", valid.Target.Path);
        if (limit == 1) { Assert.True(valid.Coverage.Partial); Assert.True(valid.Truncation.CountsAreLowerBounds); }
        var old = units.Select(u => new CodeIndexUnitReplacement(new(u.Origin.RepositoryId, u.Origin.PluginId, u.Origin.PluginVersion.Replace(".rem1.p10", ".p10", StringComparison.Ordinal), u.Origin.IndexRunId, u.Origin.IndexUnitId, u.Origin.SourcePath, u.Origin.SourceHash), u.Nodes, u.Declarations, u.Edges)).ToArray();
        var oldReader = Proxy(fixture.Store, (method, _) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(old) : null);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Query(target, new(maxCandidateScans: limit), oldReader));
    }

    [Theory]
    [InlineData("RepositoryId")]
    [InlineData("IndexRunId")]
    [InlineData("IndexIdentity")]
    [InlineData("DeclarationNodeId")]
    public async Task Remediation_OfficialTransportAndAuthorityRejectMissingIdentities(string missing)
    {
        using var fixture = new Fixture(false); fixture.Write("types.ts", "export interface Widget {}\n"); await fixture.Index();
        var result = await fixture.Query(await fixture.Target());
        var data = JsonNode.Parse(result.ToJson())!.AsObject();
        data["Publication"]!["IsConsistentSnapshot"] = true; data["Publication"]!["SnapshotIdentity"] = "snapshot:identified"; data["Coverage"]!["Qualification"] = "consistent";
        var parent = data[missing == "DeclarationNodeId" ? "Target" : "Publication"]!.AsObject(); parent.Remove(missing);
        Assert.Throws<ArgumentException>(() => InterfaceImpactResultV1.FromJson(data.ToJsonString()));
        var malformed = result with { Publication = result.Publication with { RepositoryId = null!, IsConsistentSnapshot = true, SnapshotIdentity = "snapshot:identified" }, Coverage = result.Coverage with { Qualification = "consistent" } };
        Assert.Throws<ArgumentException>(() => malformed.RequireConsistentSnapshot()); Assert.Throws<ArgumentException>(() => malformed.ToJson());
        Assert.Equal(result.ToJson(), InterfaceImpactResultV1.FromJson(result.ToJson()).ToJson());
    }

    [Theory]
    [InlineData(false, "ambiguous")]
    [InlineData(true, "ambiguous")]
    [InlineData(false, "missing")]
    [InlineData(true, "missing")]
    [InlineData(false, "ineligible")]
    [InlineData(true, "ineligible")]
    public async Task Remediation_BareAttributionFrontierDoesNotInventReasonSubjects(bool durable, string shape)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", shape switch { "ambiguous" => "export interface Widget {} export interface Widget { x: number }\n", "missing" => "namespace Hidden { interface Widget {} } export { Widget };\n", _ => "export declare interface Widget {}\n" });
        fixture.Write("consumer.ts", "import type { Widget } from './types';\n");
        await fixture.Index(); var result = await fixture.Query(await fixture.Target());
        Assert.Empty(result.ConfirmedImplementations); Assert.Empty(result.Unknowns);
        Assert.Contains(result.Coverage.NotEvaluated, text => text.StartsWith("1 bare import attribution outcomes", StringComparison.Ordinal));
        Assert.Equal("consumer.ts", Assert.Single(result.PossibleConsumers).Path);
    }

    [Theory]
    [InlineData(false, "status")]
    [InlineData(true, "status")]
    [InlineData(false, "proof")]
    [InlineData(true, "proof")]
    [InlineData(false, "policy")]
    [InlineData(true, "policy")]
    public async Task Remediation_ResolverVersionAndPolicyAreIndependentIntegrityChecks(bool durable, string corruption)
    {
        using var fixture = new Fixture(durable); fixture.Write("types.ts", "export interface Widget {}\n"); fixture.Write("consumer.ts", "import { Widget } from './types'; export class Correct implements Widget {}\n");
        await fixture.Index(); var target = await fixture.Target(); Assert.Equal("Correct", Assert.Single((await fixture.Query(target)).ConfirmedImplementations).ClassName);
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var altered = units.Select(u => new CodeIndexUnitReplacement(u.Origin, u.Nodes, u.Declarations, u.Edges.Select(e =>
        {
            if (corruption == "policy" && e.Kind.Value == "resolver-status")
            {
                var data = RemediationData(e); data["policy"] = "different-policy";
                return new CodeGraphEdge(e.Id, e.SourceId, e.TargetId, e.Kind, e.Evidence, new Dictionary<string, CodePropertyValue> { ["ec6d.data"] = new CodeTextProperty(data.ToJsonString()) });
            }
            return e.Kind.Value == (corruption == "proof" ? "resolves-to" : "resolver-status") ? new CodeGraphEdge(e.Id, e.SourceId, e.TargetId, e.Kind, new(e.Evidence.Quality, e.Evidence.Provider, "wrong.version", e.Evidence.Location), e.Properties) : e;
        }).ToArray())).ToArray();
        var reader = Proxy(fixture.Store, (method, _) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(altered) : null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Query(target, reader: reader));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 7)]
    [InlineData(true, 7)]
    public async Task Remediation_WitnessWorkIsChargedAsStepsExecuteBeforeOutput(bool durable, int steps)
    {
        using var fixture = new Fixture(durable); fixture.Write("types.ts", "interface Widget {}\n" + string.Concat(Enumerable.Repeat("export { Widget };\n", 20))); fixture.Write("consumer.ts", "import { Widget } from './types'; export class Correct implements Widget {}\n");
        await fixture.Index(); var target = await fixture.Target();
        var complete = await fixture.Query(target); var confirmed = Assert.Single(complete.ConfirmedImplementations); Assert.Equal(20, Assert.Single(confirmed.Witnesses).Steps.Count(s => s.StepKind == "export-occurrence"));
        var result = await fixture.Query(target, new(maxWitnessSteps: steps));
        Assert.Empty(result.ConfirmedImplementations); Assert.Equal(steps, result.Truncation.ChargedWork.WitnessSteps); Assert.True(result.Coverage.Partial); Assert.True(result.Truncation.CountsAreLowerBounds); Assert.Equal("binding", result.Truncation.Stage);
        Assert.Equal(result.ToJson(), (await fixture.Query(target, new(maxWitnessSteps: steps))).ToJson());
    }
}
