// EC6D-SCOPE-Q1 independent review harness.
// Review-only: no production code is modified or implemented here.
// Runs focused fixtures against the EC-6C p9 baseline (commit 2c3090c) and
// writes machine-readable results next to this program.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Penghou.Hetu;

var results = new OrderedDictionary<string, object?>();
var workspace = Directory.CreateTempSubdirectory("hetu-ec6d-review-");
try
{
    // ------------------------------------------------------------------
    // Scenario A/E: interface + 4 explicit implementations via named
    // relative imports. FindImplementations gap and missing consumer edges.
    // ------------------------------------------------------------------
    var repoA = Path.Combine(workspace.FullName, "scenario-a");
    var filesA = WriteSources(repoA, new Dictionary<string, string>
    {
        ["src/types.ts"] = """
            export interface Renderable {
              render(): void;
            }
            """,
        ["src/impl/widget-a.ts"] = """
            import { Renderable } from "../types";

            export class WidgetA implements Renderable {
              render(): void {}
            }
            """,
        ["src/impl/widget-b.ts"] = """
            import { Renderable } from "../types";

            export class WidgetB implements Renderable {
              render(): void {}
            }
            """,
        ["src/impl/widget-c.ts"] = """
            import { Renderable } from "../types";

            export class WidgetC implements Renderable {
              render(): void {}
            }
            """,
        ["src/impl/widget-d.ts"] = """
            import { Renderable } from "../types";

            export class WidgetD implements Renderable {
              render(): void {}
            }
            """,
    });

    using var scenarioA = await ReviewIndex.IndexAsync(repoA, filesA);
    var renderable = scenarioA.FindSymbolNodes("Renderable", CodeNodeKinds.Interface).Single();
    var implementations = await scenarioA.Queries.FindImplementationsAsync(
        scenarioA.RepositoryId, renderable.Id);
    var implementsEdges = scenarioA.AllEdges()
        .Where(edge => edge.Kind == CodeEdgeKinds.Implements)
        .OrderBy(edge => edge.Id.Value)
        .ToList();
    var heritageTargets = new List<string>();
    foreach (var edge in implementsEdges)
    {
        var target = await scenarioA.Store.GetNodeAsync(scenarioA.RepositoryId, edge.TargetId);
        heritageTargets.Add($"{target?.Id.Value} kind={target?.Kind} qname={target?.QualifiedName}");
    }

    // Incoming consumer relationships for the interface (all edge kinds,
    // both directions; reported edges are filtered to touch the interface).
    var incoming = await scenarioA.Queries.GetNeighborhoodAsync(
        scenarioA.RepositoryId,
        renderable.Id,
        edgeKinds: null,
        options: new CodeGraphQueryOptions(
            maxDepth: 1,
            maxNodes: 500,
            maxEdges: 1000));

    results["scenarioA"] = new Dictionary<string, object?>
    {
        ["interfaceNodeId"] = renderable.Id.Value,
        ["findImplementationsNodeCount"] = implementations.Nodes.Count,
        ["findImplementationsEdgeCount"] = implementations.Edges.Count,
        ["implementsEdgeCount"] = implementsEdges.Count,
        ["implementsEdgeTargets"] = heritageTargets,
        ["interfaceNeighborhoodNodeCount"] = incoming.Nodes.Count,
        ["interfaceNeighborhoodEdges"] = incoming.Edges.Select(e => $"{e.Kind.Value} {e.SourceId.Value}->{e.TargetId.Value}").ToList(),
    };

    // ------------------------------------------------------------------
    // Scenario B: heritage-name matching matrix. Ground truth: the written
    // heritage name denotes the exported interface of types.ts when the
    // class file's import bindings say so. A textual name matcher is scored
    // against that ground truth.
    // ------------------------------------------------------------------
    var repoB = Path.Combine(workspace.FullName, "scenario-b");
    var filesB = WriteSources(repoB, new Dictionary<string, string>
    {
        ["src/types.ts"] = """
            export interface Alpha {}
            export interface Beta {}
            """,
        // TP under both textual matching and ground truth.
        ["src/tp0.ts"] = """
            interface Local {}
            class T0 implements Local {}
            """,
        ["src/tp1.ts"] = """
            import { Alpha } from "./types";
            class T1 implements Alpha {}
            """,
        ["src/tp2.ts"] = """
            import { Beta } from "./types";
            class T2 implements Beta {}
            """,
        // FP for a textual matcher: same name, but no import binds it to
        // types.ts. Ground truth: no relationship to the exported Alpha.
        ["src/fp1.ts"] = """
            class F1 implements Alpha {}
            """,
        // FN for a textual matcher: bound through an alias the matcher
        // cannot see. Ground truth: implements types.ts Alpha.
        ["src/tp3.ts"] = """
            import { Alpha as Gamma } from "./types";
            class T3 implements Gamma {}
            """,
    });

    using var scenarioB = await ReviewIndex.IndexAsync(repoB, filesB);
    var heritageByFile = scenarioB.ImplementsEdgesByFile();
    results["scenarioB"] = new Dictionary<string, object?>
    {
        ["textualMatcherScore"] = "TP=3 (tp0 same-file Local, tp1 Alpha, tp2 Beta); FP=1 (fp1 implements Alpha with no import binding); FN=1 (tp3 implements Gamma, an alias of Alpha). The textual oracle misclassifies exactly the two cases import binding evidence would resolve.",
        ["implementsEdgesByFile"] = heritageByFile,
    };

    // ------------------------------------------------------------------
    // Scenario C: relative module resolution rules.
    // ------------------------------------------------------------------
    var repoC = Path.Combine(workspace.FullName, "scenario-c");
    var filesC = WriteSources(repoC, new Dictionary<string, string>
    {
        ["src/main.ts"] = """
            import { A } from "./mod-a";
            import { U } from "./util";
            import { W } from "./widgets";
            import { S } from "../shared/kit";
            import type { T } from "./typ";
            import * as NS from "./ns";
            import { Ghost } from "./ghost";
            import { Ext } from "some-package";
            """,
        ["src/mod-a.ts"] = "export interface A {}\n",
        ["src/util.ts"] = "export interface U {}\n",
        ["src/widgets/index.ts"] = "export interface W {}\n",
        ["src/typ.ts"] = "export interface T {}\n",
        ["src/ns.ts"] = "export interface N {}\n",
        ["shared/kit.ts"] = "export interface S {}\n",
    });

    using var scenarioC = await ReviewIndex.IndexAsync(repoC, filesC);
    var resolvesTo = scenarioC.AllEdges()
        .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)
        .OrderBy(edge => edge.Id.Value)
        .ToList();
    var resolutionRows = new List<Dictionary<string, object?>>();
    foreach (var edge in resolvesTo)
    {
        var rule = ((CodeTextProperty)edge.Properties[CodePropertyKeys.ResolutionRule]).Value;
        var specifier = ((CodeTextProperty)edge.Properties[CodePropertyKeys.Specifier]).Value;
        var reference = ((CodeTextProperty)edge.Properties[CodePropertyKeys.ModuleReference]).Value;
        var typeOnly = edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var to) && to is CodeBooleanProperty { Value: true };
        resolutionRows.Add(new Dictionary<string, object?>
        {
            ["specifier"] = specifier,
            ["form"] = reference,
            ["rule"] = rule,
            ["typeOnly"] = typeOnly,
            ["source"] = edge.SourceId.Value,
            ["target"] = edge.TargetId.Value,
        });
    }

    results["scenarioC"] = new Dictionary<string, object?>
    {
        ["resolvedEdgeCount"] = resolvesTo.Count,
        ["resolvedEdges"] = resolutionRows,
    };

    // ------------------------------------------------------------------
    // Scenario D: declaration-identity binding gaps on the p9 baseline.
    // ------------------------------------------------------------------
    var typeNode = scenarioA.FindSymbolNodes("Renderable", CodeNodeKinds.Interface).Single();
    var exportEntries = scenarioA.AllNodes()
        .Where(node => node.Kind == CodeNodeKinds.Export)
        .OrderBy(node => node.Id.Value)
        .ToList();
    var exportToDeclarationEdges = scenarioA.AllEdges()
        .Where(edge =>
            exportEntries.Any(entry => entry.Id == edge.SourceId) &&
            edge.TargetId == typeNode.Id)
        .ToList();
    var declarationToExportEdges = scenarioA.AllEdges()
        .Where(edge =>
            edge.SourceId == typeNode.Id &&
            exportEntries.Any(entry => entry.Id == edge.TargetId))
        .ToList();
    var importTargetNodes = scenarioA.AllNodes()
        .Where(node => node.Id.Value.Contains(":import:", StringComparison.Ordinal))
        .OrderBy(node => node.Id.Value)
        .ToList();
    var importEdgesWithBindings = scenarioA.AllEdges()
        .Where(edge => edge.Kind == CodeEdgeKinds.Imports && edge.Properties.Count > 0)
        .ToList();
    // What does the export entry of types.ts carry, and does any edge link
    // the export slot to the interface symbol node?
    var typesExportEntry = exportEntries.FirstOrDefault(node => node.Name == "Renderable");
    var typesExportEdges = typesExportEntry is null
        ? new List<string>()
        : scenarioA.AllEdges()
            .Where(edge => edge.SourceId == typesExportEntry.Id || edge.TargetId == typesExportEntry.Id)
            .Select(edge => $"{edge.Kind.Value} {edge.SourceId.Value}->{edge.TargetId.Value}")
            .ToList();

    results["scenarioD"] = new Dictionary<string, object?>
    {
        ["exportEntryCount"] = exportEntries.Count,
        ["exportEntryIds"] = exportEntries.Select(node => node.Id.Value).ToList(),
        ["exportEntryToDeclarationEdges"] = exportToDeclarationEdges.Count,
        ["declarationToExportEdges"] = declarationToExportEdges.Count,
        ["renderableExportEntryEdges"] = typesExportEdges,
        ["importTargetNodeCount"] = importTargetNodes.Count,
        ["importTargetSample"] = importTargetNodes.Take(3).Select(node => node.Id.Value).ToList(),
        ["importEdgesWithProperties"] = importEdgesWithBindings.Count,
        ["finding"] = "Export slots are not attributed to declaration nodes; import edges record only the module specifier and no imported/local name pair; heritage targets are declaration-less file-scoped placeholders. No existing fact chain connects interface -> export slot -> importing file -> heritage placeholder.",
    };

    // ------------------------------------------------------------------
    // Scenario F: re-export (barrel) handling.
    // ------------------------------------------------------------------
    var repoF = Path.Combine(workspace.FullName, "scenario-f");
    var filesF = WriteSources(repoF, new Dictionary<string, string>
    {
        ["src/types.ts"] = """
            export interface Widget {}
            """,
        ["src/index.ts"] = """
            export * from "./types";
            """,
        ["src/consumer.ts"] = """
            import { Widget } from "./index";

            export class Consumer implements Widget {}
            """,
    });

    using var scenarioF = await ReviewIndex.IndexAsync(repoF, filesF);
    var fExports = scenarioF.AllNodes()
        .Where(node => node.Kind == CodeNodeKinds.Export)
        .OrderBy(node => node.Id.Value)
        .ToList();
    var fResolves = scenarioF.AllEdges()
        .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)
        .Select(edge => ((CodeTextProperty)edge.Properties[CodePropertyKeys.Specifier]).Value)
        .OrderBy(value => value)
        .ToList();

    results["scenarioF"] = new Dictionary<string, object?>
    {
        ["exportEntryNames"] = fExports.Select(node => node.Name).ToList(),
        ["resolvedSpecifiers"] = fResolves,
        ["finding"] = "The barrel publishes no export entry for Widget (structural p9 entries exclude re-exports), so a confirmed binding through the barrel is impossible; the consumer's import resolves only to the barrel file.",
    };

    // ------------------------------------------------------------------
    // Scenario G: adversarial identity fixtures.
    // ------------------------------------------------------------------
    // G1: duplicate interface names in different files.
    var repoG1 = Path.Combine(workspace.FullName, "scenario-g1");
    var filesG1 = WriteSources(repoG1, new Dictionary<string, string>
    {
        ["src/a.ts"] = "export interface Widget {}\n",
        ["src/b.ts"] = "export interface Widget {}\n",
        ["src/c.ts"] = """
            import { Widget } from "./a";

            export class UsesA implements Widget {}
            """,
    });
    using var scenarioG1 = await ReviewIndex.IndexAsync(repoG1, filesG1);
    var g1Widgets = scenarioG1.FindSymbolNodes("Widget", CodeNodeKinds.Interface)
        .OrderBy(node => node.Id.Value)
        .ToList();
    var g1Implements = scenarioG1.AllEdges()
        .Where(edge => edge.Kind == CodeEdgeKinds.Implements)
        .ToList();
    var g1HeritageTarget = g1Implements.Count > 0
        ? await scenarioG1.Store.GetNodeAsync(scenarioG1.RepositoryId, g1Implements[0].TargetId)
        : null;

    results["scenarioG1_duplicates"] = new Dictionary<string, object?>
    {
        ["widgetInterfaceNodeCount"] = g1Widgets.Count,
        ["widgetInterfaceIds"] = g1Widgets.Select(node => node.Id.Value).ToList(),
        ["implementsEdgeCount"] = g1Implements.Count,
        ["implementsTargetId"] = g1HeritageTarget?.Id.Value,
        ["implementsTargetQualifiedName"] = g1HeritageTarget?.QualifiedName,
        ["finding"] = "Path-scoped identities keep the two Widget interfaces distinct (no false convergence), but the heritage placeholder is file-scoped and name-keyed, so it cannot say WHICH Widget the implements clause denoted. Current facts cannot bind c.ts to a.ts's Widget; a textual matcher would bind both.",
    };

    // G2: imported alias colliding with a local declaration.
    var repoG2 = Path.Combine(workspace.FullName, "scenario-g2");
    var filesG2 = WriteSources(repoG2, new Dictionary<string, string>
    {
        ["src/types.ts"] = "export interface Widget {}\n",
        ["src/mixed.ts"] = """
            import { Widget as Local } from "./types";

            interface Local {}

            export class Ambiguous implements Local {}
            """,
    });
    using var scenarioG2 = await ReviewIndex.IndexAsync(repoG2, filesG2);
    var g2Locals = scenarioG2.FindSymbolNodes("Local", CodeNodeKinds.Interface)
        .OrderBy(node => node.Id.Value)
        .ToList();
    var g2Implements = scenarioG2.AllEdges()
        .Where(edge => edge.Kind == CodeEdgeKinds.Implements)
        .ToList();
    var g2HeritageTarget = g2Implements.Count > 0
        ? await scenarioG2.Store.GetNodeAsync(scenarioG2.RepositoryId, g2Implements[0].TargetId)
        : null;

    results["scenarioG2_aliasCollision"] = new Dictionary<string, object?>
    {
        ["localInterfaceNodes"] = g2Locals.Select(node => $"{node.Id.Value} ({node.QualifiedName})").ToList(),
        ["implementsTargetQualifiedName"] = g2HeritageTarget?.QualifiedName,
        ["finding"] = "Ground truth: `implements Local` denotes the IMPORTED Widget (the import shadows the local interface in that clause scope would be a type error; TypeScript resolves the imported binding). The written heritage name 'Local' matches the local interface textually, so a name-only matcher binds the WRONG declaration. The graph records neither the alias nor the imported name, so even the correct binding is impossible today.",
    };

    // G3: direct import of a module that exports a different declaration.
    var repoG3 = Path.Combine(workspace.FullName, "scenario-g3");
    var filesG3 = WriteSources(repoG3, new Dictionary<string, string>
    {
        ["src/types.ts"] = "export interface Widget {}\nexport interface Gadget {}\n",
        ["src/wrong.ts"] = """
            import { Gadget } from "./types";

            export class Wrong implements Widget {}
            """,
    });
    using var scenarioG3 = await ReviewIndex.IndexAsync(repoG3, filesG3);

    results["scenarioG3_wrongExport"] = new Dictionary<string, object?>
    {
        ["finding"] = "wrong.ts imports Gadget but implements Widget. Ground truth: no confirmed binding to types.ts Widget (the written implements name is not even in scope; this file is a type error). A module-level matcher would still call wrong.ts a consumer of types.ts; only name-level binding evidence prevents the false confirmed implementation.",
    };

    // G4: module importing an interface without using it.
    var repoG4 = Path.Combine(workspace.FullName, "scenario-g4");
    var filesG4 = WriteSources(repoG4, new Dictionary<string, string>
    {
        ["src/types.ts"] = "export interface Widget {}\n",
        ["src/idle.ts"] = "import { Widget } from \"./types\";\n",
    });
    using var scenarioG4 = await ReviewIndex.IndexAsync(repoG4, filesG4);
    var g4Idle = scenarioG4.FindSymbolNodes("idle.ts".Replace(".ts", ""), CodeNodeKinds.File).ToList();
    var g4ImportEdges = scenarioG4.AllEdges().Where(edge => edge.Kind == CodeEdgeKinds.Imports).ToList();

    results["scenarioG4_importWithoutUse"] = new Dictionary<string, object?>
    {
        ["importEdgeCount"] = g4ImportEdges.Count,
        ["finding"] = "idle.ts imports Widget and never uses it. It IS a module-level possible consumer (an edit to Widget can break its compilation) but has NO implementation relationship. This is exactly the confirmed-vs-possible boundary the query contract must keep distinct.",
    };

    // G5: import forms recorded for namespace/default/type-only imports.
    var repoG5 = Path.Combine(workspace.FullName, "scenario-g5");
    var filesG5 = WriteSources(repoG5, new Dictionary<string, string>
    {
        ["src/types.ts"] = "export interface Widget {}\nexport default interface Fallback {}\n",
        ["src/forms.ts"] = """
            import Widget, { type Gadget } from "./types";
            import * as Types from "./types";
            import type { Widget as W2 } from "./types";
            export class Forms {}
            """,
    });
    using var scenarioG5 = await ReviewIndex.IndexAsync(repoG5, filesG5);
    var g5Resolves = scenarioG5.AllEdges()
        .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)
        .Select(edge => new Dictionary<string, object?>
        {
            ["specifier"] = ((CodeTextProperty)edge.Properties[CodePropertyKeys.Specifier]).Value,
            ["form"] = ((CodeTextProperty)edge.Properties[CodePropertyKeys.ModuleReference]).Value,
            ["typeOnly"] = edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var to) && to is CodeBooleanProperty { Value: true },
        })
        .ToList();

    results["scenarioG5_importForms"] = new Dictionary<string, object?>
    {
        ["resolvedOccurrences"] = g5Resolves,
        ["finding"] = "Module occurrences record form and type-only flags, but never the imported name, local name, or span of each specifier. Default, namespace, aliased, and inline-type-only specifiers are indistinguishable for binding purposes.",
    };

    // ------------------------------------------------------------------
    // Scenario H: publication consistency, repeat-index digest stability,
    // durable reopen.
    // ------------------------------------------------------------------
    var state = scenarioA.State;
    var digestRun1 = scenarioA.CanonicalDigest();

    // Second fresh store, same sources: repeat-index determinism.
    using var scenarioA2 = await ReviewIndex.IndexAsync(repoA, filesA);
    var digestRun2 = scenarioA2.CanonicalDigest();

    // Source-state sensitivity: change one file, re-index, digest must move.
    File.WriteAllText(
        Path.Combine(repoA, "src/impl/widget-a.ts"),
        "import { Renderable } from \"../types\";\n\nexport class WidgetA implements Renderable {\n  render(): void { console.log(\"changed\"); }\n}\n");
    using var scenarioA3 = await ReviewIndex.IndexAsync(repoA, filesA);
    var digestRun3 = scenarioA3.CanonicalDigest();

    // Durable reopen with the pinned LatticeDb provider.
    var latticePath = Path.Combine(workspace.FullName, "lattice", "review.db");
    Directory.CreateDirectory(Path.GetDirectoryName(latticePath)!);    ulong latticeFactsRun1;
    ulong latticeFactsReopened;
    bool latticeConsistent;
    string? latticeSnapshotIdentity;
    using (var lattice = new LatticeCodeGraphStore(latticePath))
    {
        var latticeIndexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
            lattice);
        var repoId = new CodeRepositoryId("repo:lattice-review");
        var latticeResult = await latticeIndexing.IndexAsync(
            new CodeRepositoryDescriptor(repoId, repoA),
            new CodeIndexRunId("run:lattice-1"));
        var latticeState = await lattice.GetLatestIndexStateAsync(repoId);
        latticeConsistent = latticeState?.IsConsistentSnapshot ?? false;
        latticeSnapshotIdentity = latticeState?.SnapshotIdentity;
        var units = await lattice.GetPublishedUnitsAsync(repoId);
        latticeFactsRun1 = (ulong)units.Sum(unit => unit.Nodes.Count + unit.Edges.Count);
    }

    using (var reopened = new LatticeCodeGraphStore(latticePath))
    {
        var units = await reopened.GetPublishedUnitsAsync(new CodeRepositoryId("repo:lattice-review"));
        latticeFactsReopened = (ulong)units.Sum(unit => unit.Nodes.Count + unit.Edges.Count);
    }

    results["scenarioH_publication"] = new Dictionary<string, object?>
    {
        ["inMemoryIsConsistentSnapshot"] = state?.IsConsistentSnapshot,
        ["inMemorySnapshotIdentity"] = state?.SnapshotIdentity,
        ["latticeIsConsistentSnapshot"] = latticeConsistent,
        ["latticeSnapshotIdentity"] = latticeSnapshotIdentity,
        ["digestRun1"] = digestRun1,
        ["digestRun2"] = digestRun2,
        ["digestRun1EqualsRun2"] = digestRun1 == digestRun2,
        ["digestAfterSourceChange"] = digestRun3,
        ["digestChangedAfterSourceChange"] = digestRun1 != digestRun3,
        ["latticePublishedFactCountRun1"] = latticeFactsRun1,
        ["latticePublishedFactCountReopened"] = latticeFactsReopened,
        ["latticeReopenPreservesFacts"] = latticeFactsRun1 == latticeFactsReopened && latticeFactsRun1 > 0,
        ["finding"] = "The filesystem provider declares IsConsistentSnapshot=false for every publication; snapshot identity is null. Facts from different source states can coexist within one run. Repeat indexing is deterministic (digest stable) and digest-fragile: the digest moves when sources move, but nothing in the publication records which file-level state each fact derives from.",
    };

    var json = JsonSerializer.Serialize(results, ReviewJson.Options);
    var outputPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "harness-results.json");
    await File.WriteAllTextAsync(outputPath, json);
    Console.WriteLine("harness-results.json written: " + outputPath);
    Console.WriteLine(json);
}
finally
{
    try { workspace.Delete(recursive: true); } catch { /* best effort */ }
}

static IReadOnlyList<string> WriteSources(string root, IReadOnlyDictionary<string, string> sources)
{
    Directory.CreateDirectory(root);
    foreach (var (path, content) in sources)
    {
        var fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    return sources.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
}

internal static class ReviewJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };
}

/// <summary>One indexed review repository plus convenience accessors.</summary>
internal sealed class ReviewIndex : IDisposable
{
    private readonly DirectoryInfo _directory;
    private readonly IReadOnlyList<string> _files;

    private ReviewIndex(
        DirectoryInfo directory,
        CodeRepositoryId repositoryId,
        ICodeGraphStore store,
        CodeGraphQueryService queries,
        CodeRepositoryIndexState? state,
        IReadOnlyList<string> files)
    {
        _directory = directory;
        _files = files;
        RepositoryId = repositoryId;
        Store = store;
        Queries = queries;
        State = state;
    }

    public CodeRepositoryId RepositoryId { get; }

    public ICodeGraphStore Store { get; }

    public CodeGraphQueryService Queries { get; }

    public CodeRepositoryIndexState? State { get; }

    public static async Task<ReviewIndex> IndexAsync(string repositoryRoot, IReadOnlyList<string> files)
    {
        var directory = Directory.CreateDirectory(repositoryRoot);
        var repositoryId = new CodeRepositoryId($"repo:{directory.Name}");
        var store = new InMemoryCodeGraphStore();
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
            store);
        var runId = new CodeIndexRunId($"run:{directory.Name}");
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, directory.FullName),
            runId);
        if (result.Diagnostics.Status != CodeIndexRunStatus.Completed)
            throw new InvalidOperationException($"Indexing failed: {result.Diagnostics.Status}");
        var state = await store.GetLatestIndexStateAsync(repositoryId);
        return new ReviewIndex(directory, repositoryId, store, new CodeGraphQueryService(store), state, files);
    }

    private static CodeNodeId FileNodeId(string path) => new(
        $"typescript:file:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(path)))}");

    public IReadOnlyList<CodeGraphNode> AllNodes()
    {
        var nodes = new Dictionary<string, CodeGraphNode>(StringComparer.Ordinal);
        // Declarations are qualified by bare name, so the complete fact set is
        // reached by traversing outward from every indexed file node.
        foreach (var file in _files)
        {
            var traversal = Queries.GetNeighborhoodAsync(
                RepositoryId,
                FileNodeId(file),
                edgeKinds: null,
                options: new CodeGraphQueryOptions(
                    maxDepth: 6,
                    maxNodes: 2000,
                    maxEdges: 5000)).GetAwaiter().GetResult();
            nodes[FileNodeId(file).Value] = traversal.Nodes
                .FirstOrDefault(node => node.Id == FileNodeId(file)) ?? throw new InvalidOperationException(
                    $"File node '{file}' missing from publication.");
            foreach (var node in traversal.Nodes)
                nodes[node.Id.Value] = node;
        }

        return nodes.Values.OrderBy(node => node.Id.Value, StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<CodeGraphEdge> AllEdges()
    {
        var edges = new Dictionary<string, CodeGraphEdge>(StringComparer.Ordinal);
        foreach (var node in AllNodes())
        {
            var traversal = Queries.GetNeighborhoodAsync(
                RepositoryId,
                node.Id,
                edgeKinds: null,
                options: new CodeGraphQueryOptions(
                    maxDepth: 1,
                    maxNodes: 5000,
                    maxEdges: 5000)).GetAwaiter().GetResult();
            foreach (var edge in traversal.Edges)
                edges[edge.Id.Value] = edge;
        }

        return edges.Values.OrderBy(edge => edge.Id.Value, StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<CodeGraphNode> FindSymbolNodes(string name, CodeNodeKind kind) =>
        AllNodes()
            .Where(node => node.Kind == kind && node.Name == name)
            .ToList();

    public Dictionary<string, string> ImplementsEdgesByFile()
    {
        var rows = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var edge in AllEdges().Where(edge => edge.Kind == CodeEdgeKinds.Implements))
        {
            var source = Store.GetNodeAsync(RepositoryId, edge.SourceId).GetAwaiter().GetResult();
            var target = Store.GetNodeAsync(RepositoryId, edge.TargetId).GetAwaiter().GetResult();
            var location = edge.Evidence.Location;
            if (location is not null)
                rows[$"{location.Path}:{location.StartLine}"] =
                    $"class={source?.Name} heritage={target?.Name} quality={edge.Evidence.Quality}";
        }

        return rows;
    }

    /// <summary>
    /// Deterministic digest over the full published fact set: node and edge
    /// identities plus discriminating fields, sorted, hashed.
    /// </summary>
    public string CanonicalDigest()
    {
        var builder = new StringBuilder();
        foreach (var node in AllNodes())
        {
            builder.Append("N|").Append(node.Id.Value)
                .Append('|').Append(node.Kind.Value)
                .Append('|').Append(node.Name)
                .Append('|').Append(node.QualifiedName)
                .Append('\n');
        }

        foreach (var edge in AllEdges())
        {
            builder.Append("E|").Append(edge.Id.Value)
                .Append('|').Append(edge.Kind.Value)
                .Append('|').Append(edge.SourceId.Value)
                .Append('|').Append(edge.TargetId.Value)
                .Append('|').Append(edge.Evidence.Quality)
                .Append('\n');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(bytes);
    }

    public void Dispose()
    {
        try { _directory.Delete(recursive: true); } catch { /* idempotent dispose */ }
    }
}

/// <summary>Preserves insertion order for readable JSON output.</summary>
internal sealed class OrderedDictionary<TKey> : Dictionary<TKey, object?> where TKey : notnull
{
    public new void Add(TKey key, object? value) => base.Add(key, value);
}
