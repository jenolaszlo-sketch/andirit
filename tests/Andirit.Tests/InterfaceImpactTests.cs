using System.Reflection;
using System.Text.Json;

namespace Andirit.Tests;

public sealed partial class InterfaceImpactTests
{
    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Andirit.slnx"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "andirit-ec6d", Guid.NewGuid().ToString("N"));
        internal readonly CodeRepositoryId Repository = new("repo:interface-impact");
        internal ICodeGraphStore Store;
        internal string Database => Path.Combine(Root, "graph.db");
        internal Fixture(bool durable)
        {
            Directory.CreateDirectory(Root);
            Store = durable ? new LatticeCodeGraphStore(Database) : new InMemoryCodeGraphStore();
        }
        internal void Write(string path, string text)
        {
            var full = Path.Combine(Root, "source", path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
        }
        internal async Task Index(string run = "run:one", ICodeGraphPlugin? plugin = null) => await new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([plugin ?? new TreeSitterTypeScriptPlugin()]), Store).IndexAsync(
                new CodeRepositoryDescriptor(Repository, Path.Combine(Root, "source")), new CodeIndexRunId(run));
        internal async Task<CodeNodeId> Target(string name = "Widget", string? path = null)
        {
            var units = await Store.GetPublishedUnitsAsync(Repository);
            return units.SelectMany(u => u.Nodes).DistinctBy(n => n.Id).Single(n => n.Kind == CodeNodeKinds.Interface && n.Name == name &&
                (path is null || units.SelectMany(u => u.Declarations).Any(d => d.SymbolNodeId == n.Id && d.Location.Path == path))).Id;
        }
        internal async Task<InterfaceImpactResultV1> Query(CodeNodeId target, InterfaceImpactOptionsV1? options = null, ICodeGraphReader? reader = null) =>
            await (await new CodeGraphQueryService(reader ?? Store).OpenLatestPublicationAsync(Repository))!.GetInterfaceImpactV1Async(target, options);
        public void Dispose()
        {
            (Store as IDisposable)?.Dispose();
            Directory.Delete(Root, true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ControlledCorpus_ExactClassificationsCompleteStoredWitnessesAndLegacyCompatibility(bool durable)
    {
        using var fixture = new Fixture(durable);
        var corpus = Path.Combine(RepositoryRoot, "docs/planning/ec6d/corpus/ts-interface-impact");
        foreach (var file in Directory.GetFiles(corpus, "*.ts", SearchOption.AllDirectories)) fixture.Write(Path.GetRelativePath(corpus, file), File.ReadAllText(file));
        await fixture.Index();
        var target = await fixture.Target("Renderable", "src/contracts/Renderable.ts");
        var result = await fixture.Query(target);
        Assert.Equal(["AbstractRenderer", "AliasRenderer", "HtmlRenderer", "SvgRenderer"], result.ConfirmedImplementations.Select(c => c.ClassName));
        Assert.Equal(["Preview", "AbstractRenderer", "AliasRenderer", "HtmlRenderer", "SvgRenderer"], result.PossibleConsumers.Select(c => Path.GetFileNameWithoutExtension(c.Path)));
        Assert.Equal(["re-export-barrier", "unresolved-module"], result.Unknowns.Select(u => u.Reason));
        Assert.All(result.Unknowns, u => Assert.Equal("occurrence", u.SubjectKind));
        Assert.Equal(3, result.Unknowns[0].BarrierWitnesses.Count);
        Assert.All(result.Unknowns[0].BarrierWitnesses, w => Assert.Equal("export-resolution", Assert.Single(w.SupportingFacts!).StepKind));
        Assert.Equal("qualified", result.Publication.Qualification);
        Assert.Throws<InvalidOperationException>(() => result.RequireConsistentSnapshot());
        Assert.Contains("Absence from the confirmed list is not evidence of absence", result.CoverageStatement);
        Assert.Contains(result.Coverage.NotEvaluated, s => s.Contains("ordinary type-use", StringComparison.Ordinal));
        Assert.False(result.Coverage.Partial);
        Assert.Equal(4, result.Coverage.ConfirmedImplementationCount);
        Assert.Equal(5, result.Coverage.PossibleConsumerModuleCount);
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var factIds = units.SelectMany(u => u.Edges.Select(e => e.Id.Value).Concat(u.Nodes.Select(n => n.Id.Value)).Concat(u.Declarations.Select(d => d.Id.Value))).ToHashSet(StringComparer.Ordinal);
        Assert.All(result.ConfirmedImplementations, c =>
        {
            Assert.Equal(result.Target.DeclarationRecordId, c.TargetDeclarationRecordId);
            Assert.Equal(target, c.TargetDeclarationNodeId);
            var witness = Assert.Single(c.Witnesses);
            Assert.StartsWith("binding1.local-v1:", witness.WitnessId);
            Assert.Equal(EvidenceQuality.Resolved, witness.Quality);
            Assert.Equal(["heritage", "context", "import", "resolution", "export-entry", "export-occurrence", "attribution", "declaration"], witness.Steps.Select(s => s.StepKind));
            Assert.EndsWith(".p8", witness.Steps[0].Evidence!.ProviderVersion);
            Assert.All(witness.Steps, s => Assert.Contains(s.FactId, factIds));
            Assert.All(witness.Steps.Where(s => s.StepKind != "resolution"), s => Assert.False(string.IsNullOrWhiteSpace(s.Origin.SourceHash)));
        });
        Assert.Equal(result.ToJson(), (await fixture.Query(target)).ToJson());
        Assert.Equal(result.ToJson(), InterfaceImpactResultV1.FromJson(result.ToJson()).ToJson());
        var legacy = await new CodeGraphQueryService(fixture.Store).FindImplementationsAsync(fixture.Repository, target);
        Assert.Single(legacy.Nodes);
        if (durable)
        {
            ((IDisposable)fixture.Store).Dispose();
            fixture.Store = new LatticeCodeGraphStore(fixture.Database);
            Assert.Equal(result.ToJson(), (await fixture.Query(target)).ToJson());
        }
    }

    public static IEnumerable<object?[]> AdversarialCases()
    {
        var q2 = new (string Case, string? Positive, string? Reason)[]
        {
            ("collision", null, "conflicting-local-binding"), ("nested-shadow", null, "nested-scope-heritage"),
            ("merged", null, "ambiguous-export"), ("wrong-export", null, "unresolved-heritage-name"),
            ("nested-export", null, "unattributed-export"), ("barrel", "Direct", "re-export-barrier"),
            ("idle", null, null), ("duplicate-imports", null, "duplicate-local-name"),
            ("multi-specifier", "A", null), ("export-alias", "Renamed", null),
            ("unsupported", null, "qualified-heritage-unresolved"), ("type-value", null, "ambiguous-export"),
            ("same-file", "Local", null), ("repeated-export", "ViaOne", null)
        };
        var r2 = new (string Case, string? Positive, string? Reason)[]
        {
            ("absent-export", null, "missing-export"), ("barrel-named", null, "re-export-barrier"),
            ("barrel-star", null, "re-export-barrier"), ("barrel-missing", null, "re-export-barrier"),
            ("barrel-multi", null, "re-export-barrier"), ("barrel-no-declaration", null, "re-export-barrier"),
            ("barrel-ordinary-consumer", null, "missing-export"), ("barrel-missing-consumer", null, "re-export-barrier"),
            ("forms-import-equals", null, "unsupported-import-form"), ("forms-inline-type", "ViaInline", null),
            ("forms-dynamic", null, null), ("expression-heritage", null, "unsupported-heritage-form"),
            ("interface-extends", null, "heritage-kind-not-supported"), ("same-file-merged", null, "ambiguous-local-declaration"),
            ("ambient-export", null, "ineligible-declaration"), ("ambient-script", null, "ambient-or-global-context"),
            ("recovered-clause", null, "recovered-heritage-syntax"), ("ghost", null, "unattributed-export"),
            ("external-package", null, "external-module"), ("barrier-scope", "Direct", "re-export-barrier"),
            ("nested-unrelated", "ViaNestedContrast", null), ("default-implement", null, "unsupported-import-form"),
            ("qualified-member", null, "qualified-heritage-unresolved")
        };
        foreach (var durable in new[] { false, true })
        {
            foreach (var c in q2) yield return ["q2", c.Case, c.Positive, c.Reason, durable];
            foreach (var c in r2) yield return ["r2", c.Case, c.Positive, c.Reason, durable];
        }
    }

    [Theory]
    [MemberData(nameof(AdversarialCases))]
    public async Task AcceptedAdversarialCorpus_UsesReviewedExpectations(string corpus, string name, string? positive, string? reason, bool durable)
    {
        using var fixture = new Fixture(durable);
        var manifest = corpus == "q2" ? "docs/reviews/ec6d-scope-q2-evidence/cases.json" : "docs/planning/ec6d/analysis/fixtures/r2/cases.json";
        using var cases = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, manifest)));
        foreach (var file in cases.RootElement.GetProperty(name).EnumerateObject()) fixture.Write(file.Name, file.Value.GetString()!);
        await fixture.Index();
        var targetName = name == "export-alias" ? "Internal" : name is "ambient-script" or "same-file-merged" ? "Local" : "Widget";
        var targetPath = name == "duplicate-imports" ? "a.ts" : name is "nested-unrelated" ? "types.ts" : null;
        var result = await fixture.Query(await fixture.Target(targetName, targetPath));
        Assert.Equal(positive is null ? [] : new[] { positive }, result.ConfirmedImplementations.Select(c => c.ClassName));
        if (reason is null) Assert.Empty(result.Unknowns);
        else if (name == "unsupported") Assert.Equal(["unsupported-import-form", "qualified-heritage-unresolved"], result.Unknowns.Select(u => u.Reason));
        else Assert.All(result.Unknowns, u => Assert.Equal(reason, u.Reason));
        if (reason is not null) Assert.NotEmpty(result.Unknowns);
        Assert.DoesNotContain(result.Unknowns, u => u.Reason is "unsupported-export-form" or "unsupported-declaration-form");
        Assert.False(result.Coverage.Partial);
        if (name == "merged")
        {
            Assert.Null(result.Target.DeclarationRecordId);
            Assert.Null(result.Target.Span);
            var attribution = Assert.Single(Assert.Single(result.Unknowns).Evidence, s => s.StepKind == "export-attribution");
            using var data = JsonDocument.Parse(((CodeTextProperty)attribution.Properties!["ec6d.data"]).Value);
            Assert.Equal(2, data.RootElement.GetProperty("candidateDeclarationRecordIds").GetArrayLength());
        }
        if (name == "same-file")
            Assert.Equal(["heritage", "context", "declaration"], Assert.Single(Assert.Single(result.ConfirmedImplementations).Witnesses).Steps.Select(s => s.StepKind));
        if (name == "barrel-multi") Assert.Equal(2, Assert.Single(result.Unknowns).BarrierWitnesses.Count);
        if (name == "barrier-scope") Assert.DoesNotContain(result.Unknowns, u => u.Span.Path.StartsWith("unrelated", StringComparison.Ordinal));
        if (name == "forms-dynamic")
            Assert.Contains((await fixture.Store.GetPublishedUnitsAsync(fixture.Repository)).SelectMany(u => u.Edges), e => e.Kind.Value == "imports-name" && ((CodeTextProperty)e.Properties["ec6d.data"]).Value.Contains("\"form\":\"dynamic\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task WorkBudgets_AreChargedAndDeterministicallyTruncated(int bound)
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import { Widget } from './types'; export class A implements Widget {} export class B implements Widget {}\n");
        await fixture.Index();
        var options = new InterfaceImpactOptionsV1(maxCandidateScans: bound == 0 ? 1 : 100000,
            maxHeritageScans: bound == 1 ? 1 : 10000, maxResolverJoins: bound == 2 ? 1 : 10000,
            maxAttributionLookups: bound == 3 ? 1 : 10000, maxWitnessSteps: bound == 4 ? 1 : 100000);
        var target = await fixture.Target();
        var result = await fixture.Query(target, options);
        Assert.True(result.Coverage.Partial);
        Assert.Equal("truncated", result.Truncation.State);
        Assert.True(result.Truncation.CountsAreLowerBounds);
        Assert.True(result.Truncation.OmittedFrontierCount > 0);
        Assert.Equal(result.ToJson(), (await fixture.Query(target, options)).ToJson());
        Assert.All(result.ConfirmedImplementations.SelectMany(c => c.Witnesses), w => Assert.True(w.Steps.Count >= 3));
    }

    [Theory]
    [InlineData(false, null, "qualified")]
    [InlineData(false, "snapshot:provided", "qualified")]
    [InlineData(true, null, "qualified")]
    [InlineData(true, "   ", "qualified")]
    [InlineData(true, "snapshot:identified", "consistent")]
    public async Task QualificationTruthTable_SerializationAndCheckedAuthority(bool consistent, string? snapshot, string expected)
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {}\nexport class Local implements Widget {}\n");
        await fixture.Index();
        var original = (await fixture.Store.GetLatestPublicationAsync(fixture.Repository))!;
        var synthetic = new CodeGraphPublication(original.RepositoryId, original.IndexRunId, snapshot, consistent, original.IndexIdentity);
        var reader = Proxy(fixture.Store, (method, args) => method == "GetLatestPublicationAsync" ? new ValueTask<CodeGraphPublication?>(synthetic) : null);
        var result = await fixture.Query(await fixture.Target(), reader: reader);
        Assert.Equal(expected, result.Publication.Qualification);
        Assert.Equal(expected, result.Coverage.Qualification);
        var restored = InterfaceImpactResultV1.FromJson(result.ToJson());
        Assert.Equal(result.ToJson(), restored.ToJson());
        if (expected == "consistent") Assert.Same(restored, restored.RequireConsistentSnapshot());
        else Assert.Throws<InvalidOperationException>(() => restored.RequireConsistentSnapshot());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TargetInventoryTransitions_RemoveStaleFactsAndRetainLastPublicationOnFailure(bool durable)
    {
        using var fixture = new Fixture(durable);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import { Widget } from './types'; export class A implements Widget {}\n");
        await fixture.Index();
        var target = await fixture.Target();
        Assert.Single((await fixture.Query(target)).ConfirmedImplementations);
        fixture.Write("types.ts", "export interface Widget {}\nexport interface Widget { merged: number; }\n");
        await fixture.Index("run:merge");
        Assert.Equal("ambiguous-export", Assert.Single((await fixture.Query(target)).Unknowns).Reason);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("new-target.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import { Widget } from './new-target'; export class A implements Widget {}\n");
        await fixture.Index("run:rename");
        Assert.Empty((await fixture.Query(target)).ConfirmedImplementations);
        var other = await fixture.Target("Widget", "new-target.ts");
        Assert.Single((await fixture.Query(other)).ConfirmedImplementations);
        File.Delete(Path.Combine(fixture.Root, "source/new-target.ts"));
        await fixture.Index("run:delete-target");
        Assert.Equal("unresolved-module", Assert.Single((await fixture.Query(target)).Unknowns).Reason);
        fixture.Write("consumer.ts", "export const noLongerImports = 1;\n");
        await fixture.Index("run:remove-import");
        Assert.Empty((await fixture.Query(target)).Unknowns);
        var before = await fixture.Query(target);
        fixture.Write("consumer.ts", "import { Widget } from './types'; export class A implements Widget {}\n");
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Index("run:failure", new FailingPlugin()));
        Assert.Equal(before.ToJson(), (await fixture.Query(target)).ToJson());
    }

    [Theory]
    [InlineData("missing-context")]
    [InlineData("missing-resolver")]
    [InlineData("publication-movement")]
    [InlineData("cancel")]
    [InlineData("store-failure")]
    [InlineData("reversed")]
    public async Task QueryFailureBoundaries_ReturnNoPartialClassifications(string scenario)
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import { Widget } from './types'; export class A implements Widget {}\n");
        await fixture.Index();
        var target = await fixture.Target();
        var baseline = await fixture.Query(target);
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var publication = (await fixture.Store.GetLatestPublicationAsync(fixture.Repository))!;
        using var cancellation = new CancellationTokenSource();
        var latestReads = 0;
        var reader = Proxy(fixture.Store, (method, args) =>
        {
            if (method == "GetLatestPublicationAsync" && scenario == "publication-movement" && ++latestReads >= 3)
                return new ValueTask<CodeGraphPublication?>(new CodeGraphPublication(publication.RepositoryId, new("run:same-content-replacement"), publication.SnapshotIdentity, publication.IsConsistentSnapshot, publication.IndexIdentity));
            if (method != "GetPublishedUnitsAsync") return null;
            if (scenario == "cancel") cancellation.Cancel();
            if (scenario == "store-failure") throw new IOException("Injected query store failure.");
            var transformed = units.Select(u => new CodeIndexUnitReplacement(u.Origin, u.Nodes.Reverse().ToArray(), u.Declarations.Reverse().ToArray(),
                u.Edges.Where(e => scenario != "missing-context" || e.Kind.Value != "heritage-context").Where(e => scenario != "missing-resolver" || e.Kind.Value != "resolver-status").Reverse().ToArray())).Reverse().ToArray();
            return new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(transformed);
        });
        var query = new CodeGraphQueryService(reader).Bind(publication);
        if (scenario == "cancel") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => query.GetInterfaceImpactV1Async(target, cancellationToken: cancellation.Token).AsTask());
        else if (scenario == "store-failure") await Assert.ThrowsAsync<IOException>(() => query.GetInterfaceImpactV1Async(target).AsTask());
        else if (scenario == "publication-movement") await Assert.ThrowsAsync<CodeGraphPublicationChangedException>(() => query.GetInterfaceImpactV1Async(target).AsTask());
        else if (scenario == "missing-resolver") await Assert.ThrowsAsync<InvalidOperationException>(() => query.GetInterfaceImpactV1Async(target).AsTask());
        else if (scenario == "missing-context")
        {
            var result = await query.GetInterfaceImpactV1Async(target);
            Assert.Empty(result.ConfirmedImplementations);
            Assert.Contains(result.Unknowns, u => u.Reason == "binding-context-unavailable");
        }
        else Assert.Equal(baseline.ToJson(), (await query.GetInterfaceImpactV1Async(target)).ToJson());
    }

    [Fact]
    public void InvalidBounds_AreRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new InterfaceImpactOptionsV1(maxWitnessSteps: 0));

    [Fact]
    public async Task ForeignLanguageHeritage_DoesNotBecomeATargetRelationship()
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {}\n");
        await fixture.Index();
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var publication = (await fixture.Store.GetLatestPublicationAsync(fixture.Repository))!;
        var file = new CodeGraphNode(new("foreign:file"), CodeNodeKinds.File, "Foreign.cs", "Foreign.cs");
        var owner = new CodeGraphNode(new("foreign:class"), CodeNodeKinds.Type, "Foreign");
        var placeholder = new CodeGraphNode(new("foreign:placeholder"), CodeNodeKinds.Type, "Widget");
        var origin = new CodeFactOrigin(fixture.Repository, new("plugin:foreign"), "1.0", publication.IndexRunId, new("foreign:unit"), "Foreign.cs", "sha256:foreign");
        var edge = new CodeGraphEdge(new("foreign:heritage"), owner.Id, placeholder.Id, CodeEdgeKinds.Implements,
            new CodeEvidence(EvidenceQuality.Structural, "plugin:foreign", "1.0", new("Foreign.cs", 1, 1, 1, 9)));
        var synthetic = units.Append(new CodeIndexUnitReplacement(origin, [file, owner, placeholder], edges: [edge])).ToArray();
        var reader = Proxy(fixture.Store, (method, args) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(synthetic) : null);
        var result = await fixture.Query(await fixture.Target(), reader: reader);
        Assert.Empty(result.ConfirmedImplementations);
        Assert.Empty(result.Unknowns);
        Assert.Equal(0, result.Coverage.HeritageObserved);
    }

    [Fact]
    public async Task RepeatedClausesAndExportOccurrences_PreserveEveryCompleteWitness()
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "interface Widget {}\nexport { Widget };\nexport { Widget };\n");
        fixture.Write("consumer.ts", "import { Widget } from './types'; export class A implements Widget, Widget {}\n");
        await fixture.Index();
        var result = await fixture.Query(await fixture.Target());
        var implementation = Assert.Single(result.ConfirmedImplementations);
        Assert.Equal(2, implementation.Witnesses.Count);
        Assert.All(implementation.Witnesses, witness => Assert.Equal(2, witness.Steps.Count(s => s.StepKind == "export-occurrence")));
        Assert.True(implementation.DiscoveryOrigins.Count >= 2);
    }

    [Fact]
    public async Task Occurrences_PreserveUtf16SpecifierSpansAndPerSpecifierTypeOnlyStatus()
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {} export interface Other {}\n");
        fixture.Write("consumer.ts", "/* 😀 */ import { type Widget as W, Other as O } from './types';\nimport { Widget as Again } from './types';\nimport { Widget as Escaped } from './ty\\u0070es';\nimport Alias = Some.Namespace;\nexport * as NS from './types';\n");
        await fixture.Index();
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var imports = units.SelectMany(u => u.Edges).Where(e => e.Kind.Value == "imports-name").OrderBy(e => e.Evidence.Location!.StartLine).ToArray();
        Assert.Equal(5, imports.Length);
        Assert.Equal(5, imports.Select(e => e.Id).Distinct().Count());
        var first = imports.Where(e => e.Evidence.Location!.StartLine == 1).Select(e => JsonDocument.Parse(((CodeTextProperty)e.Properties["ec6d.data"]).Value)).ToArray();
        try
        {
            var typed = Assert.Single(first, d => d.RootElement.GetProperty("localName").GetString() == "W");
            var ordinary = Assert.Single(first, d => d.RootElement.GetProperty("localName").GetString() == "O");
            Assert.True(typed.RootElement.GetProperty("isTypeOnly").GetBoolean());
            Assert.False(ordinary.RootElement.GetProperty("isTypeOnly").GetBoolean());
            Assert.Equal(10, typed.RootElement.GetProperty("statementSpan").GetProperty("StartColumn").GetInt32());
            Assert.Equal("./types", typed.RootElement.GetProperty("moduleSpecifier").GetString());
        }
        finally { foreach (var document in first) document.Dispose(); }
        Assert.Contains(imports, e => ((CodeTextProperty)e.Properties["ec6d.data"]).Value.Contains("ty\\\\u0070es", StringComparison.Ordinal));
        Assert.Contains(imports, e => ((CodeTextProperty)e.Properties["ec6d.data"]).Value.Contains("import-equals", StringComparison.Ordinal));
        Assert.Single(units.SelectMany(u => u.Edges), e => e.Kind.Value == "export-reference");
        var result = await fixture.Query(await fixture.Target());
        Assert.Empty(result.Unknowns);
        Assert.True(result.Coverage.UnsupportedImportFormOccurrences >= 2);
    }

    [Theory]
    [InlineData("type-parameter-shadowing")]
    [InlineData("unresolved-alias")]
    [InlineData("same-file-different-target")]
    [InlineData("unsupported-resolver")]
    public async Task AdditionalLexicalGuards_PreserveIdentityAndAliasUncertainty(string scenario)
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", scenario switch
        {
            "type-parameter-shadowing" => "import { Widget } from './types'; export class A<Widget> implements Widget {}\n",
            "unresolved-alias" => "import { Widget as MissingAlias } from './missing'; export class A implements MissingAlias {}\n",
            "unsupported-resolver" => "import { Widget as Bad } from './ty\\u0070es'; export class A implements Bad {}\n",
            _ => "export const Widget = 1; export class A implements Widget {}\n"
        });
        await fixture.Index();
        var result = await fixture.Query(await fixture.Target());
        Assert.Empty(result.ConfirmedImplementations);
        if (scenario == "same-file-different-target") Assert.Empty(result.Unknowns);
        else Assert.Equal(scenario switch { "unresolved-alias" => "unresolved-module", "unsupported-resolver" => "unsupported-import-form", _ => scenario }, Assert.Single(result.Unknowns).Reason);
    }

    [Theory]
    [InlineData("confirmed")]
    [InlineData("occurrences")]
    [InlineData("unknowns")]
    public async Task OutputCaps_AreIndependentOfChargedExecutionBounds(string category)
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import { Widget } from './types'; import { Widget as Alias } from './types'; export class A implements Widget {} export class B implements Alias {}\n");
        fixture.Write("unknown.ts", "import { Widget } from './missing'; export class C implements Widget {} export class D implements Widget {}\n");
        await fixture.Index();
        var options = new InterfaceImpactOptionsV1(maxConfirmed: category == "confirmed" ? 1 : 1000,
            maxOccurrences: category == "occurrences" ? 1 : 10000, maxUnknowns: category == "unknowns" ? 1 : 10000);
        var result = await fixture.Query(await fixture.Target(), options);
        Assert.True(result.Coverage.Partial);
        Assert.Equal(category switch { "confirmed" => "confirmed-output", "occurrences" => "occurrence-output", _ => "unknown-output" }, result.Truncation.Stage);
        Assert.True(result.Truncation.ChargedWork.CandidateScans > 1);
    }

    [Theory]
    [InlineData("missing-export-evidence")]
    [InlineData("wrong-attribution-record")]
    [InlineData("wrong-context-owner")]
    [InlineData("stale-hash")]
    [InlineData("provider-version")]
    [InlineData("missing-declaration-context")]
    public async Task ContradictoryOrSupersededFacts_FailClosed(string corruption)
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {}\n");
        fixture.Write("consumer.ts", "import { Widget } from './types'; export class A implements Widget {}\n");
        await fixture.Index();
        var units = await fixture.Store.GetPublishedUnitsAsync(fixture.Repository);
        var target = await fixture.Target();
        var transformed = units.Select(u => new CodeIndexUnitReplacement(
            corruption == "stale-hash" && u.Origin.SourcePath == "consumer.ts" ? new CodeFactOrigin(u.Origin.RepositoryId, u.Origin.PluginId, u.Origin.PluginVersion, u.Origin.IndexRunId, u.Origin.IndexUnitId, u.Origin.SourcePath, "sha256:wrong") : u.Origin,
            u.Nodes, u.Declarations,
            u.Edges.Where(e => corruption != "missing-export-evidence" || e.Kind != CodeEdgeKinds.Exports)
                .Where(e => corruption != "missing-declaration-context" || e.Kind.Value != "declaration-context").Select(e =>
            {
                if (corruption == "provider-version" && e.Kind.Value == "heritage-context")
                    return new CodeGraphEdge(e.Id, e.SourceId, e.TargetId, e.Kind, new CodeEvidence(e.Evidence.Quality, e.Evidence.Provider, "wrong.version", e.Evidence.Location), e.Properties);
                if ((corruption == "wrong-context-owner" && e.Kind.Value == "heritage-context") || (corruption == "wrong-attribution-record" && e.Kind.Value == "export-attribution"))
                {
                    var data = System.Text.Json.Nodes.JsonNode.Parse(((CodeTextProperty)e.Properties["ec6d.data"]).Value)!;
                    data[corruption == "wrong-context-owner" ? "ownerNode" : "declarationRecordId"] = "missing:fact";
                    return new CodeGraphEdge(e.Id, e.SourceId, e.TargetId, e.Kind, e.Evidence, new Dictionary<string, CodePropertyValue> { ["ec6d.data"] = new CodeTextProperty(data.ToJsonString()) });
                }
                return e;
            }).ToArray())).ToArray();
        var reader = Proxy(fixture.Store, (method, args) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(transformed) : null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Query(target, reader: reader));
    }

    [Theory]
    [InlineData("unknown-target")]
    [InlineData("non-interface")]
    [InlineData("missing-provider")]
    public async Task InvalidTargetsAndUnsupportedPublications_AreInputErrors(string scenario)
    {
        using var fixture = new Fixture(false);
        fixture.Write("types.ts", "export interface Widget {} export class A {}\n");
        await fixture.Index();
        var target = await fixture.Target();
        ICodeGraphReader reader = fixture.Store;
        if (scenario == "unknown-target") target = new("missing:target");
        if (scenario == "non-interface") target = (await fixture.Store.GetPublishedUnitsAsync(fixture.Repository)).SelectMany(u => u.Nodes).First(n => n.Kind == CodeNodeKinds.File).Id;
        if (scenario == "missing-provider")
        {
            var units = (await fixture.Store.GetPublishedUnitsAsync(fixture.Repository)).Select(u => new CodeIndexUnitReplacement(u.Origin, u.Nodes, u.Declarations, u.Edges.Where(e => e.Kind.Value != "interface-profile").ToArray())).ToArray();
            reader = Proxy(fixture.Store, (method, args) => method == "GetPublishedUnitsAsync" ? new ValueTask<IReadOnlyList<CodeIndexUnitReplacement>>(units) : null);
        }
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Query(target, reader: reader));
    }

    private sealed class FailingPlugin : ICodeGraphPlugin
    {
        private readonly TreeSitterTypeScriptPlugin _inner = new();
        public CodePluginId Id => _inner.Id;
        public string Version => _inner.Version;
        public string Language => _inner.Language;
        public IReadOnlyCollection<string> FileExtensions => _inner.FileExtensions;
        public CodeGraphCapabilities Capabilities => _inner.Capabilities;
        public bool CanHandle(string path) => _inner.CanHandle(path);
        public ValueTask<ICodeGraphExtractionSession> CreateSessionAsync(CodeGraphPluginContext context, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Injected indexing failure.");
    }

    private static ICodeGraphReader Proxy(ICodeGraphReader inner, Func<string, object?[]?, object?> intercept)
    {
        var reader = DispatchProxy.Create<ICodeGraphReader, ReaderProxy>();
        ((ReaderProxy)reader).Inner = inner;
        ((ReaderProxy)reader).Intercept = intercept;
        return reader;
    }
    public class ReaderProxy : DispatchProxy
    {
        public ICodeGraphReader Inner = null!;
        public Func<string, object?[]?, object?> Intercept = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            try { return Intercept(targetMethod!.Name, args) ?? targetMethod.Invoke(Inner, args); }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }
    }
}
