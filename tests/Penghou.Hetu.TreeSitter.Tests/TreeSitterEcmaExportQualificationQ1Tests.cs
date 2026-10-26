using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>
/// Qualification-only tests for EC-6C.2 (independent review GLM-Q1).
/// Expectations are re-derived from the accepted EC6C-A2 contract text
/// (architecture §3–§11, A29 closure) and never from implementation code.
/// These tests are additive evidence; they do not modify production code.
/// </summary>
public sealed class TreeSitterEcmaExportQualificationQ1Tests
{
    private const string ExportProvider = "penghou.hetu.tree-sitter.ecmascript-export-surface";

    // ------------------------------------------------------------------
    // A. Export entry identity
    // ------------------------------------------------------------------

    [Fact]
    public async Task Q1_A_EntryIdentityIsModuleScopedCaseSensitiveAndUnnormalized()
    {
        var nfcName = "caf\u00e9";       // U+00E9 precomposed
        var nfdName = "cafe\u0301";      // decomposed
        var sources = new (string Path, string Source)[]
        {
            ("src/one.ts", "export { local as currentProject };"),
            ("src/two.ts", "export { local as currentProject };"),
            ("src/three.ts", "export { local as currentproject };"),
            ("src/four.ts", $"export {{ local as \"{nfcName}\" }};"),
            ("src/five.ts", $"export {{ local as \"{nfdName}\" }};"),
        };

        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            sources.Select(value => (value.Path, value.Source)).ToArray());
        var entries = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();

        Assert.Equal(5, entries.Length);
        Assert.Equal(5, entries.Select(node => node.Id.Value).Distinct(StringComparer.Ordinal).Count());

        foreach (var entry in entries)
        {
            Assert.Equal(entry.Name, entry.QualifiedName);
            Assert.Null(entry.SymbolId);
            Assert.Equal(
                ExpectedEntryId("typescript", entry.Name, ModulePathOf(extraction, entry)),
                entry.Id.Value);
            Assert.Single(entry.Properties);
            Assert.True(entry.Properties.ContainsKey(CodePropertyKeys.Language));
        }

        var byModule = entries.ToDictionary(entry => ModulePathOf(extraction, entry), entry => entry.Name, StringComparer.Ordinal);
        Assert.Equal("currentProject", byModule["src/one.ts"]);
        Assert.Equal("currentProject", byModule["src/two.ts"]);
        Assert.Equal("currentproject", byModule["src/three.ts"]);
        Assert.Equal(nfcName, byModule["src/four.ts"]);
        Assert.Equal(nfdName, byModule["src/five.ts"]);

        // Same name, different modules → different identities.
        Assert.NotEqual(
            entries.Single(entry => ModulePathOf(extraction, entry) == "src/one.ts").Id,
            entries.Single(entry => ModulePathOf(extraction, entry) == "src/two.ts").Id);
        // Case-distinct names in different modules → different identities, exact spellings.
        Assert.NotEqual(
            entries.Single(entry => ModulePathOf(extraction, entry) == "src/one.ts").Id,
            entries.Single(entry => ModulePathOf(extraction, entry) == "src/three.ts").Id);
        // Canonically equivalent but differently encoded spellings → different identities.
        Assert.NotEqual(
            entries.Single(entry => ModulePathOf(extraction, entry) == "src/four.ts").Id,
            entries.Single(entry => ModulePathOf(extraction, entry) == "src/five.ts").Id);
        Assert.Equal(nfcName, entries.Single(entry => ModulePathOf(extraction, entry) == "src/four.ts").Name);
        Assert.Equal(nfdName, entries.Single(entry => ModulePathOf(extraction, entry) == "src/five.ts").Name);
    }

    [Fact]
    public async Task Q1_A_EntryAndEdgeIdentityStableAcrossRepeatedExtraction_BothLanguages()
    {
        const string source = "const value = 1;\nexport { value as stable };\nexport default 1;\n";
        var firstTypeScript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(), ("src/stable.ts", source));
        var secondTypeScript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(), ("src/stable.ts", source));
        var firstJavaScript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(), ("src/stable.js", source));
        var secondJavaScript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(), ("src/stable.js", source));

        foreach (var (first, second, language) in new[]
                 {
                     (firstTypeScript, secondTypeScript, "typescript"),
                     (firstJavaScript, secondJavaScript, "javascript")
                 })
        {
            Assert.Equal(
                first.Nodes.Select(node => node.Id.Value).OrderBy(value => value, StringComparer.Ordinal),
                second.Nodes.Select(node => node.Id.Value).OrderBy(value => value, StringComparer.Ordinal));
            Assert.Equal(
                first.Edges.Select(edge => edge.Id.Value).OrderBy(value => value, StringComparer.Ordinal),
                second.Edges.Select(edge => edge.Id.Value).OrderBy(value => value, StringComparer.Ordinal));
            Assert.Contains(first.Nodes, node => node.Id.Value == ExpectedEntryId(language, "stable", language == "typescript" ? "src/stable.ts" : "src/stable.js"));
            Assert.Contains(first.Nodes, node => node.Id.Value == ExpectedEntryId(language, "default", language == "typescript" ? "src/stable.ts" : "src/stable.js"));
        }
    }

    // ------------------------------------------------------------------
    // B. Structural export edges
    // ------------------------------------------------------------------

    [Fact]
    public async Task Q1_B_EdgeFactsRecomputedFromContract_AcceptedTypeScriptForms()
    {
        const string path = "src/forms.ts";
        var source = string.Join('\n',
            "export const alpha = 1;",
            "export function beta() {}",
            "export class Gamma {}",
            "export interface Delta {}",
            "export type Epsilon = string;",
            "export enum Zeta {}",
            "export namespace Eta {}",
            "const theta = 1;",
            "export { theta };",
            "const iota = 1;",
            "export { iota as IotaPublic };",
            "export default function Kappa() {}",
            "export default class Lambda {}",
            "export default 42;",
            "export type { Gamma as GammaType };",
            "export { type Mu as MuInline };",
            "const nu = 1;",
            "export { nu as \"quoted-public\" };",
            "");
        var lines = source.Split('\n');

        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(), (path, source));
        var entries = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();
        var exportEdges = extraction.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();

        // 13 distinct public names; Kappa, Lambda, and the literal expression
        // share the module-scoped `default` entry, so 15 written occurrences
        // produce 15 edges over 13 entries.
        Assert.Equal(13, entries.Length);
        Assert.Equal(15, exportEdges.Length);

        // Every entry: exact identity, no SymbolId, only language property.
        foreach (var entry in entries)
        {
            Assert.Null(entry.SymbolId);
            Assert.Equal(entry.Name, entry.QualifiedName);
            Assert.Equal(
                ExpectedEntryId("typescript", entry.Name, path),
                entry.Id.Value);
            Assert.Single(entry.Properties);
            Assert.True(entry.Properties.ContainsKey(CodePropertyKeys.Language));
        }

        // Every occurrence edge: complete contract recomputation from the
        // independently derived expected fact tuple.
        foreach (var edge in exportEdges)
        {
            var entry = entries.Single(node => node.Id == edge.TargetId);
            var exportedName = entry.Name;
            var (kind, via, localName, typeOnly, projection) =
                ExpectedOccurrenceFacts(exportedName, edge.Evidence.Location!);
            Assert.Equal(ExpectedFileId("typescript", path), edge.SourceId.Value);
            Assert.Equal(CodeEdgeKinds.Exports, edge.Kind);
            Assert.Equal(ExpectedEdgeId(
                "typescript",
                edge.SourceId.Value,
                edge.TargetId.Value,
                exportedName,
                kind,
                via,
                localName,
                typeOnly,
                projection,
                edge.Evidence.Location!),
                edge.Id.Value);

            // Evidence contract.
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
            Assert.Equal(ExportProvider, edge.Evidence.Provider);
            Assert.Equal(PackageVersionPrefix(new TreeSitterTypeScriptPlugin().Version) + "+exports1.local-v1", edge.Evidence.ProviderVersion);

            // Property contract: exact set, no construct span, type-only only when true.
            var expectedProperties = new List<string> { CodePropertyKeys.ExportKind, CodePropertyKeys.ExportVia, CodePropertyKeys.NamespaceProjection };
            if (localName is not null)
                expectedProperties.Add(CodePropertyKeys.LocalName);
            if (typeOnly)
                expectedProperties.Add(CodePropertyKeys.TypeOnly);
            Assert.Equal(expectedProperties.OrderBy(key => key, StringComparer.Ordinal), edge.Properties.Keys.OrderBy(key => key, StringComparer.Ordinal));
            Assert.Equal(kind, TextProperty(edge, CodePropertyKeys.ExportKind));
            Assert.Equal(via, TextProperty(edge, CodePropertyKeys.ExportVia));
            Assert.Equal(projection, TextProperty(edge, CodePropertyKeys.NamespaceProjection));
            if (localName is not null)
                Assert.Equal(localName, TextProperty(edge, CodePropertyKeys.LocalName));
            else
                Assert.False(edge.Properties.ContainsKey(CodePropertyKeys.LocalName));
            Assert.Equal(typeOnly, edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var value) &&
                Assert.IsType<CodeBooleanProperty>(value).Value);
            Assert.DoesNotContain(edge.Properties.Keys, key => key.Contains("span", StringComparison.OrdinalIgnoreCase));

            // Location is the public-name token span on its own line.
            var location = Assert.IsType<CodeLocation>(edge.Evidence.Location);
            Assert.Equal(path, location.Path);
            var token = exportedName == "default" ? "default" : exportedName;
            var line = lines[location.StartLine - 1];
            var tokenIndex = line.IndexOf(exportedName == "quoted-public" ? "\"quoted-public\"" : token, StringComparison.Ordinal);
            Assert.True(tokenIndex >= 0, $"Token '{token}' not found on line {location.StartLine}.");
            Assert.Equal(tokenIndex + 1, location.StartColumn);
            Assert.Equal(
                tokenIndex + (exportedName == "quoted-public" ? "\"quoted-public\"" : token).Length + 1,
                location.EndColumn);
        }

        // Spot-check the full expected fact set for selected single-occurrence forms.
        AssertEntryWithSingleEdge(entries, exportEdges, "alpha", "named", "direct", "alpha", false, "value");
        AssertEntryWithSingleEdge(entries, exportEdges, "beta", "named", "direct", "beta", false, "value");
        AssertEntryWithSingleEdge(entries, exportEdges, "Gamma", "named", "direct", "Gamma", false, "both");
        AssertEntryWithSingleEdge(entries, exportEdges, "Delta", "named", "direct", "Delta", false, "type");
        AssertEntryWithSingleEdge(entries, exportEdges, "Epsilon", "named", "direct", "Epsilon", false, "type");
        AssertEntryWithSingleEdge(entries, exportEdges, "Zeta", "named", "direct", "Zeta", false, "both");
        AssertEntryWithSingleEdge(entries, exportEdges, "Eta", "named", "direct", "Eta", false, "unknown");
        AssertEntryWithSingleEdge(entries, exportEdges, "theta", "named", "local", "theta", false, "unknown");
        AssertEntryWithSingleEdge(entries, exportEdges, "IotaPublic", "named", "local", "iota", false, "unknown");
        AssertEntryWithSingleEdge(entries, exportEdges, "GammaType", "named", "local", "Gamma", true, "type");
        AssertEntryWithSingleEdge(entries, exportEdges, "MuInline", "named", "local", "Mu", true, "type");
        AssertEntryWithSingleEdge(entries, exportEdges, "quoted-public", "named", "local", "nu", false, "unknown");

        // The shared `default` entry carries three occurrences: a named default
        // function (direct/value), a named default class (direct/both), and a
        // default expression (expression, no local name).
        var defaultEntry = Assert.Single(entries, node => node.Name == "default");
        var defaultEdges = exportEdges.Where(edge => edge.TargetId == defaultEntry.Id).ToArray();
        Assert.Equal(3, defaultEdges.Length);
        Assert.Contains(defaultEdges, edge =>
            "direct" == TextProperty(edge, CodePropertyKeys.ExportVia) &&
            "Kappa" == TextProperty(edge, CodePropertyKeys.LocalName) &&
            "value" == TextProperty(edge, CodePropertyKeys.NamespaceProjection));
        Assert.Contains(defaultEdges, edge =>
            "direct" == TextProperty(edge, CodePropertyKeys.ExportVia) &&
            "Lambda" == TextProperty(edge, CodePropertyKeys.LocalName) &&
            "both" == TextProperty(edge, CodePropertyKeys.NamespaceProjection));
        Assert.Contains(defaultEdges, edge =>
            "expression" == TextProperty(edge, CodePropertyKeys.ExportVia) &&
            !edge.Properties.ContainsKey(CodePropertyKeys.LocalName) &&
            "value" == TextProperty(edge, CodePropertyKeys.NamespaceProjection));

        // Export facts must not enter relationship coverage or unresolved counts.
        Assert.DoesNotContain(
            extraction.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.Exports.Value);
        Assert.DoesNotContain(extraction.Edges, edge => edge.Kind.Value is "exported-as" or "binds");
    }

    [Fact]
    public async Task Q1_B_EdgeFactsRecomputedFromContract_JavaScriptBasics()
    {
        const string path = "src/forms.js";
        var source = string.Join('\n',
            "export const alpha = 1;",
            "export function beta() {}",
            "export class Gamma {}",
            "export default 42;",
            "const theta = 1;",
            "export { theta as Public };",
            "");
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(), (path, source));
        var entries = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();
        var exportEdges = extraction.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();

        Assert.Equal(5, entries.Length);
        Assert.Equal(5, exportEdges.Length);
        foreach (var edge in exportEdges)
        {
            Assert.Equal(ExpectedFileId("javascript", path), edge.SourceId.Value);
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
            Assert.Equal(ExportProvider, edge.Evidence.Provider);
            Assert.Equal(PackageVersionPrefix(new TreeSitterJavaScriptPlugin().Version) + "+exports1.local-v1", edge.Evidence.ProviderVersion);
        }

        AssertEntryWithSingleEdge(entries, exportEdges, "alpha", "named", "direct", "alpha", false, "value");
        AssertEntryWithSingleEdge(entries, exportEdges, "beta", "named", "direct", "beta", false, "value");
        AssertEntryWithSingleEdge(entries, exportEdges, "Gamma", "named", "direct", "Gamma", false, "both");
        AssertEntryWithSingleEdge(entries, exportEdges, "default", "default", "expression", null, false, "value");
        AssertEntryWithSingleEdge(entries, exportEdges, "Public", "named", "local", "theta", false, "unknown");
    }

    [Fact]
    public async Task Q1_B_PublicNameTokenSpan_MultilineSupplementaryScalarAndQuotedAlias()
    {
        const string path = "src/spans.ts";
        var supplementary = char.ConvertFromUtf32(0x1F600); // 2 UTF-16 code units
        var source = "const local = 1;\nexport {\n  local as \"" + supplementary + "x" + supplementary + "\",\n};\n";
        var lines = source.Split('\n');
        var quotedToken = "\"" + supplementary + "x" + supplementary + "\"";

        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(), (path, source));
        var exportEdges = extraction.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();
        var edge = Assert.Single(exportEdges);
        var entry = extraction.Nodes.Single(node => node.Id == edge.TargetId && node.Kind == CodeNodeKinds.Export);

        // The public name preserves the supplementary scalars exactly.
        Assert.Equal(supplementary + "x" + supplementary, entry.Name);

        // One-based, end-exclusive UTF-16 columns, computed independently.
        var location = Assert.IsType<CodeLocation>(edge.Evidence.Location);
        Assert.Equal(path, location.Path);
        Assert.Equal(3, location.StartLine);
        Assert.Equal(3, location.EndLine); // the quoted token lies wholly on line 3
        var line = lines[2];
        var tokenIndex = line.IndexOf(quotedToken, StringComparison.Ordinal);
        Assert.Equal(tokenIndex + 1, location.StartColumn);
        Assert.Equal(tokenIndex + quotedToken.Length + 1, location.EndColumn);
        // The quoted token spans more UTF-16 units than its scalar count (A29 counts scalars; columns count UTF-16 units).
        Assert.Equal(quotedToken.Length, location.EndColumn - location.StartColumn);
    }

    // ------------------------------------------------------------------
    // C. Occurrence identity tuples and collision behavior
    // ------------------------------------------------------------------

    [Fact]
    public void Q1_C_TupleDiscriminators_PresenceBitTypeOnlyProjectionSpanAndName()
    {
        const string path = "src/collision.ts";
        // TreeSitterTestSupport.ExtractSyntax publishes internally against this
        // file-node ID, so explicit republishes must use the same binding.
        var fileNodeId = new CodeNodeId("typescript:file:shape-test");
        var pluginVersion = TreeSitterTypeScriptPlugin.PluginVersion;

        CodeGraphEdge Publish(EcmaScriptExportObservation observation)
        {
            var result = new TreeSitterExtractionResult();
            result.ExportObservations.Add(observation);
            EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, pluginVersion, result);
            return Assert.Single(result.Edges.Values);
        }

        var location = new CodeLocation(path, 2, 10, 2, 15);
        var baseObservation = new EcmaScriptExportObservation(
            path, "typescript", "public1", "local1",
            EcmaScriptExportKind.Named, EcmaScriptExportForm.LocalAlias, false,
            EcmaScriptNamespaceProjection.Unknown, location,
            new CodeLocation(path, 2, 1, 2, 30));

        // Baseline publish works.
        var baseline = Publish(baseObservation);
        Assert.Equal(ExpectedEdgeId(
            "typescript",
            fileNodeId.Value,
            baseline.TargetId.Value,
            "public1", "named", "local", "local1", false, "unknown", location),
            baseline.Id.Value);

        // Different local name (same presence) → different identity.
        var renamedLocal = Publish(baseObservation with { LocalName = "otherLocal" });
        Assert.NotEqual(baseline.Id, renamedLocal.Id);

        // Presence-bit discrimination: same public name and span, local name
        // present versus absent produce different materials and identities.
        var absentLocal = Publish(baseObservation with { LocalName = null, ExportForm = EcmaScriptExportForm.LocalName });
        Assert.False(absentLocal.Properties.ContainsKey(CodePropertyKeys.LocalName));
        Assert.NotEqual(baseline.Id, absentLocal.Id);
        Assert.NotEqual(
            ExpectedEdgeId("typescript", fileNodeId.Value, baseline.TargetId.Value, "public1", "named", "local", null, false, "unknown", location),
            ExpectedEdgeId("typescript", fileNodeId.Value, baseline.TargetId.Value, "public1", "named", "local", "local1", false, "unknown", location));

        // Type-only state changes identity and adds the type-only property.
        var typeOnly = Publish(baseObservation with { TypeOnly = true, Projection = EcmaScriptNamespaceProjection.Type });
        Assert.True(typeOnly.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var flag) &&
            Assert.IsType<CodeBooleanProperty>(flag).Value);
        Assert.NotEqual(baseline.Id, typeOnly.Id);

        // Projection changes identity.
        var valueProjection = Publish(baseObservation with { Projection = EcmaScriptNamespaceProjection.Value });
        Assert.NotEqual(baseline.Id, valueProjection.Id);

        // Source span participates in identity; construct span does not.
        var movedName = Publish(baseObservation with { NameLocation = new CodeLocation(path, 3, 10, 3, 15) });
        Assert.NotEqual(baseline.Id, movedName.Id);
        var movedConstruct = Publish(baseObservation with { ConstructLocation = new CodeLocation(path, 9, 1, 9, 30) });
        Assert.Equal(baseline.Id, movedConstruct.Id);

        // Export kind and via change identity. LocalName and LocalAlias both
        // publish via=`local`, so those two internal forms share one published
        // fact set and therefore one identity (idempotent duplicates).
        var differentVia = Publish(baseObservation with { ExportForm = EcmaScriptExportForm.DefaultExpression, ExportKind = EcmaScriptExportKind.Default });
        Assert.NotEqual(baseline.Id, differentVia.Id);
        var defaultKind = Publish(baseObservation with
        {
            ExportedName = "default",
            ExportKind = EcmaScriptExportKind.Default,
            ExportForm = EcmaScriptExportForm.DefaultExpression,
            LocalName = null
        });
        Assert.Equal("default", TextProperty(defaultKind, CodePropertyKeys.ExportKind));
        Assert.Equal("expression", TextProperty(defaultKind, CodePropertyKeys.ExportVia));
    }

    [Fact]
    public void Q1_C_LocalNameContainingNewlineKeepsTailAnchoredIdentityDistinct()
    {
        // The accepted contract argues the newline-joined tuple is unambiguous
        // because the trailing fields are fixed vocabulary and the span is last.
        // Grammar-produced local names cannot contain LF (string content and
        // identifiers exclude line breaks), so this boundary is exercised at the
        // publisher contract level only: even a hostile local name cannot make
        // two distinct facts share an identity.
        const string path = "src/hostile.ts";
        var fileNodeId = new CodeNodeId(ExpectedFileId("typescript", path));
        var pluginVersion = TreeSitterTypeScriptPlugin.PluginVersion;
        var location = new CodeLocation(path, 1, 1, 1, 20);

        CodeGraphEdge PublishWithLocalName(string localName)
        {
            var result = new TreeSitterExtractionResult();
            result.ExportObservations.Add(new EcmaScriptExportObservation(
                path, "typescript", "public1", localName,
                EcmaScriptExportKind.Named, EcmaScriptExportForm.LocalAlias, false,
                EcmaScriptNamespaceProjection.Unknown, location,
                new CodeLocation(path, 1, 1, 1, 20)));
            EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, pluginVersion, result);
            return Assert.Single(result.Edges.Values);
        }

        var ordinary = PublishWithLocalName("local1");
        var hostile = PublishWithLocalName("local1\ntrue\ntype\n1:1:1:2");
        Assert.NotEqual(ordinary.Id, hostile.Id);
        Assert.Equal("local1\ntrue\ntype\n1:1:1:2", TextProperty(hostile, CodePropertyKeys.LocalName));
    }

    [Fact]
    public void Q1_C_RepublishIsIdempotent()
    {
        const string path = "src/idempotent.ts";
        var result = TreeSitterTestSupport.ExtractSyntax(
            "typescript", path, "export { local as result };\nexport default 1;\n", out var hasParseError);
        Assert.False(hasParseError);
        // ExtractSyntax publishes internally against this file-node ID.
        var fileNodeId = new CodeNodeId("typescript:file:shape-test");

        EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, TreeSitterTypeScriptPlugin.PluginVersion, result);
        var nodesAfterFirst = Snapshot(result.Nodes.Values);
        var edgesAfterFirst = Snapshot(result.Edges.Values);
        var exportEdgeCount = result.Edges.Values.Count(edge => edge.Kind == CodeEdgeKinds.Exports);

        EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, TreeSitterTypeScriptPlugin.PluginVersion, result);
        Assert.Equal(nodesAfterFirst, Snapshot(result.Nodes.Values));
        Assert.Equal(edgesAfterFirst, Snapshot(result.Edges.Values));
        Assert.Equal(exportEdgeCount, result.Edges.Values.Count(edge => edge.Kind == CodeEdgeKinds.Exports));
        Assert.Equal(2, exportEdgeCount);
    }

    [Fact]
    public void Q1_C_SameIdDifferentFactFailsClosedWithCompleteDiagnosticsAndNoMutation()
    {
        const string path = "src/failclosed.ts";
        // TreeSitterTestSupport.ExtractSyntax publishes internally against this
        // file-node ID; republishes must use the same binding.
        var fileNodeId = new CodeNodeId("typescript:file:shape-test");
        var pluginVersion = TreeSitterTypeScriptPlugin.PluginVersion;

        // --- Edge collision ---
        var result = TreeSitterTestSupport.ExtractSyntax(
            "typescript", path, "export { local as result };", out var hasParseError);
        Assert.False(hasParseError);
        EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, pluginVersion, result);
        var original = Assert.Single(result.Edges.Values, edge => edge.Kind == CodeEdgeKinds.Exports);

        // Replace the stored edge with a same-ID fact that differs in one
        // property, then publish again: the conflict must be rejected before
        // insertion and both complete facts must appear in the diagnostic.
        var conflictingEdge = new CodeGraphEdge(
            original.Id, original.SourceId, original.TargetId, original.Kind,
            original.Evidence,
            new Dictionary<string, CodePropertyValue>(original.Properties)
            {
                [CodePropertyKeys.NamespaceProjection] = new CodeTextProperty("value")
            });
        result.Edges[conflictingEdge.Id.Value] = conflictingEdge;
        var edgesWithConflict = Snapshot(result.Edges.Values);
        var nodesWithConflict = Snapshot(result.Nodes.Values);

        var edgeException = Assert.Throws<InvalidOperationException>(() =>
            EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, pluginVersion, result));
        Assert.Contains(original.Id.Value, edgeException.Message, StringComparison.Ordinal);
        Assert.Contains("Existing fact:", edgeException.Message, StringComparison.Ordinal);
        Assert.Contains("Candidate fact:", edgeException.Message, StringComparison.Ordinal);
        // Complete facts: endpoints, kind, evidence (provider, version, location), properties.
        Assert.Contains($"SourceId={System.Text.Json.JsonSerializer.Serialize(original.SourceId.Value)}", edgeException.Message, StringComparison.Ordinal);
        Assert.Contains($"TargetId={System.Text.Json.JsonSerializer.Serialize(original.TargetId.Value)}", edgeException.Message, StringComparison.Ordinal);
        Assert.Contains($"Kind={System.Text.Json.JsonSerializer.Serialize(original.Kind.Value)}", edgeException.Message, StringComparison.Ordinal);
        Assert.Contains(ExportProvider, edgeException.Message, StringComparison.Ordinal);
        Assert.Contains("exports1.local-v1", edgeException.Message, StringComparison.Ordinal);
        Assert.Contains($"\"{path}\"", edgeException.Message, StringComparison.Ordinal);
        Assert.Contains("unknown", edgeException.Message, StringComparison.Ordinal);
        Assert.Contains("value", edgeException.Message, StringComparison.Ordinal);
        // No partial mutation: the dictionaries are exactly as after the
        // deliberate replacement and before the failed publish — the rejected
        // candidate inserted nothing.
        Assert.Equal(edgesWithConflict, Snapshot(result.Edges.Values));
        Assert.Equal(nodesWithConflict, Snapshot(result.Nodes.Values));

        // --- Node collision ---
        var nodeResult = TreeSitterTestSupport.ExtractSyntax(
            "typescript", path, "export { local as result };", out hasParseError);
        Assert.False(hasParseError);
        EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, pluginVersion, nodeResult);
        var entry = Assert.Single(nodeResult.Nodes.Values, node => node.Kind == CodeNodeKinds.Export);
        var conflictingNode = new CodeGraphNode(
            entry.Id, entry.Kind, "different", "different",
            properties: new Dictionary<string, CodePropertyValue>(StringComparer.Ordinal)
            {
                [CodePropertyKeys.Language] = new CodeTextProperty("typescript")
            });
        nodeResult.Nodes[conflictingNode.Id.Value] = conflictingNode;
        var nodesWithNodeConflict = Snapshot(nodeResult.Nodes.Values);

        var nodeException = Assert.Throws<InvalidOperationException>(() =>
            EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, pluginVersion, nodeResult));
        Assert.Contains(entry.Id.Value, nodeException.Message, StringComparison.Ordinal);
        Assert.Contains("Name=\"different\"", nodeException.Message, StringComparison.Ordinal);
        Assert.Contains("Name=\"result\"", nodeException.Message, StringComparison.Ordinal);
        Assert.Contains($"SymbolId={System.Text.Json.JsonSerializer.Serialize<string?>(null)}", nodeException.Message, StringComparison.Ordinal);
        Assert.Equal(nodesWithNodeConflict, Snapshot(nodeResult.Nodes.Values));
    }

    // ------------------------------------------------------------------
    // Shared helpers (qualification-local, independent of implementation code)
    // ------------------------------------------------------------------

    internal static string ExpectedEntryId(string language, string exportedName, string modulePath) =>
        $"{language}:export:{Hash(modulePath + "\n" + exportedName)}";

    internal static string ExpectedEdgeId(
        string language,
        string fileNodeId,
        string entryNodeId,
        string exportedName,
        string exportKind,
        string exportVia,
        string? localName,
        bool typeOnly,
        string projection,
        CodeLocation location) =>
        $"{language}:{Hash(string.Join('\n',
            "exports",
            fileNodeId,
            entryNodeId,
            exportedName,
            exportKind,
            exportVia,
            localName is null ? "0" : "1",
            localName ?? string.Empty,
            typeOnly ? "true" : "false",
            projection,
            $"{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}"))}";

    internal static string ExpectedFileId(string language, string path) =>
        $"{language}:file:{Hash(path)}";

    internal static string PackageVersionPrefix(string pluginVersion) =>
        pluginVersion.Split('+', 2)[0];

    internal static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    internal static string TextProperty(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeTextProperty>(edge.Properties[key]).Value;

    private static string ModulePathOf(Extraction extraction, CodeGraphNode entry)
    {
        // The occurrence edges bind the entry to the file; recover the module path from
        // the file node that sources an edge targeting this entry.
        var fileEdge = extraction.Edges.FirstOrDefault(edge => edge.TargetId == entry.Id)
            ?? throw new InvalidOperationException($"No occurrence edge found for entry '{entry.Id.Value}'.");
        return Assert.Single(extraction.Nodes, node => node.Id == fileEdge.SourceId && node.Kind == CodeNodeKinds.File).QualifiedName
            ?? throw new InvalidOperationException($"File node for entry '{entry.Id.Value}' has no qualified path.");
    }

    private static (string Kind, string Via, string? LocalName, bool TypeOnly, string Projection) ExpectedOccurrenceFacts(
        string exportedName,
        CodeLocation location) =>
        exportedName switch
        {
            "alpha" => ("named", "direct", "alpha", false, "value"),
            "beta" => ("named", "direct", "beta", false, "value"),
            "Gamma" => ("named", "direct", "Gamma", false, "both"),
            "Delta" => ("named", "direct", "Delta", false, "type"),
            "Epsilon" => ("named", "direct", "Epsilon", false, "type"),
            "Zeta" => ("named", "direct", "Zeta", false, "both"),
            "Eta" => ("named", "direct", "Eta", false, "unknown"),
            "theta" => ("named", "local", "theta", false, "unknown"),
            "IotaPublic" => ("named", "local", "iota", false, "unknown"),
            "default" => location.StartLine switch
            {
                12 => ("default", "direct", "Kappa", false, "value"),   // export default function Kappa
                13 => ("default", "direct", "Lambda", false, "both"),   // export default class Lambda
                14 => ("default", "expression", null, false, "value"),  // export default 42
                _ => throw new InvalidOperationException($"Unexpected default occurrence line {location.StartLine}.")
            },
            "GammaType" => ("named", "local", "Gamma", true, "type"),
            "MuInline" => ("named", "local", "Mu", true, "type"),
            "quoted-public" => ("named", "local", "nu", false, "unknown"),
            _ => throw new InvalidOperationException($"Unexpected export name '{exportedName}'.")
        };

    private static void AssertEntryWithSingleEdge(
        IReadOnlyList<CodeGraphNode> entries,
        IReadOnlyList<CodeGraphEdge> edges,
        string exportedName,
        string kind,
        string via,
        string? localName,
        bool typeOnly,
        string projection)
    {
        var entry = Assert.Single(entries, node => node.Name == exportedName);
        var edge = Assert.Single(edges, candidate => candidate.TargetId == entry.Id);
        Assert.Equal(kind, TextProperty(edge, CodePropertyKeys.ExportKind));
        Assert.Equal(via, TextProperty(edge, CodePropertyKeys.ExportVia));
        Assert.Equal(projection, TextProperty(edge, CodePropertyKeys.NamespaceProjection));
        if (localName is null)
            Assert.False(edge.Properties.ContainsKey(CodePropertyKeys.LocalName));
        else
            Assert.Equal(localName, TextProperty(edge, CodePropertyKeys.LocalName));
        Assert.Equal(typeOnly, edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var value) &&
            Assert.IsType<CodeBooleanProperty>(value).Value);
    }

    private static IReadOnlyList<string> Snapshot<T>(IEnumerable<T> values) where T : notnull =>
        values.Select(value => System.Text.Json.JsonSerializer.Serialize(value)).OrderBy(value => value, StringComparer.Ordinal).ToArray();

    // ------------------------------------------------------------------
    // D. Namespace projection
    // ------------------------------------------------------------------

    [Fact]
    public async Task Q1_D_ProjectionIsSyntaxGroundedAndNeverSharpenedBySemantics()
    {
        const string path = "src/projection.ts";
        // A same-name interface exists for the local alias `currentProject`, and
        // an imported local `helper` is exported; neither may sharpen `unknown`.
        var source = string.Join('\n',
            "import { helper } from \"./dep\";",
            "interface currentProject {}",
            "const local = 1;",
            "const currentProjectValue = 2;",
            "export { local as currentProject };",
            "export { helper as ImportedHelper };",
            "");
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(), (path, source), ("src/dep.ts", "export const helper = 1;"));
        var entries = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();

        var aliased = Assert.Single(entries, node => node.Name == "currentProject");
        var aliasedEdge = Assert.Single(extraction.Edges, edge => edge.TargetId == aliased.Id);
        Assert.Equal("unknown", TextProperty(aliasedEdge, CodePropertyKeys.NamespaceProjection));
        Assert.False(aliasedEdge.Properties.ContainsKey(CodePropertyKeys.TypeOnly));

        var imported = Assert.Single(entries, node => node.Name == "ImportedHelper");
        var importedEdge = Assert.Single(extraction.Edges, edge => edge.TargetId == imported.Id);
        Assert.Equal("unknown", TextProperty(importedEdge, CodePropertyKeys.NamespaceProjection));
        // No remote provenance on the export edge: source is the file, target the entry.
        Assert.Equal(ExpectedFileId("typescript", path), importedEdge.SourceId.Value);
    }

    [Fact]
    public async Task Q1_D_JavaScriptStatementLevelTypeOnlyExcluded_InlineRecoveryBehaviorRecorded()
    {
        // Statement-level `export type { ... }` is invalid JavaScript; the
        // collector excludes the whole clause (no entries, no edges).
        var statement = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(), ("src/js-type.js", "const value = 1;\nexport type { value };\n"));
        Assert.DoesNotContain(statement.Nodes, node => node.Kind == CodeNodeKinds.Export);
        Assert.DoesNotContain(statement.Edges, edge => edge.Kind == CodeEdgeKinds.Exports);
        Assert.Contains("javascript.parse-error", statement.Result.WarningCodes);

        // Inline `export { type Model }` is also invalid JavaScript. The pinned
        // JavaScript grammar recovers a clean specifier sibling to an ERROR
        // token, and the accepted positive-only rule (A17: valid siblings may
        // emit) records an entry. The implementation handoff claims JavaScript
        // type-only-like recovery is "excluded"; the recorded behavior is a
        // qualification finding (see compatibility audit): the exclusion holds
        // at statement level only. The entry carries no type-only flag and an
        // unknown projection, and the parse error stays visible.
        var inline = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(), ("src/js-type-inline.js", "const Model = 1;\nexport { type Model as Shape };\n"));
        Assert.Contains("javascript.parse-error", inline.Result.WarningCodes);
        var shapeEntry = inline.Nodes.Single(node => node.Kind == CodeNodeKinds.Export);
        Assert.Equal("Shape", shapeEntry.Name);
        var shapeEdge = Assert.Single(inline.Edges, edge => edge.Kind == CodeEdgeKinds.Exports);
        Assert.False(shapeEdge.Properties.ContainsKey(CodePropertyKeys.TypeOnly));
        Assert.Equal("unknown", TextProperty(shapeEdge, CodePropertyKeys.NamespaceProjection));
    }

    // ------------------------------------------------------------------
    // E. Unicode and A29 name policy
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, true)]
    [InlineData(257, false)]
    public async Task Q1_E_SupplementaryScalarBoundary(int scalarCount, bool accepted)
    {
        var supplementary = char.ConvertFromUtf32(0x1F600);
        var name = string.Concat(Enumerable.Repeat(supplementary, scalarCount));
        const string path = "src/boundary.ts";
        var source = $"const local = 1;\nexport {{ local as \"{name}\" }};\n";

        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(), (path, source));
        var entries = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();
        var edges = extraction.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();

        if (accepted)
        {
            var entry = Assert.Single(entries);
            Assert.Equal(name, entry.Name);
            Assert.Equal(ExpectedEntryId("typescript", name, path), entry.Id.Value);
            Assert.Single(edges);
            Assert.Equal(entry.Id, Assert.Single(edges).TargetId);
        }
        else
        {
            Assert.Empty(entries);
            Assert.Empty(edges);
        }
    }

    [Fact]
    public async Task Q1_E_EmptyWhitespaceControlAndEscapedNamesRejected()
    {
        const string path = "src/invalid-names.ts";
        var source = string.Join('\n',
            "const local = 1;",
            "export { local as \"\" };",
            "export { local as \"   \" };",
            "export { local as \"a\\u0001b\" };",       // escaped control (escape sequence) — rejected by escape rule
            "export { local as a\\u0041b };",           // escaped identifier — rejected
            "export { local as validName };",           // control: exactly one accepted occurrence
            "");
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(), (path, source));
        var entries = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();
        var edges = extraction.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal("validName", entry.Name);
        Assert.Equal(ExpectedEntryId("typescript", "validName", path), entry.Id.Value);
        var edge = Assert.Single(edges);
        Assert.Equal(entry.Id, edge.TargetId);
    }

    [Fact]
    public void Q1_E_MalformedSurrogateSequencesRejectedByValidatorBeforeIdentity()
    {
        // Validator-level probe: malformed UTF-16 must be rejected before any
        // hashing (a fallback encoder would replace malformed input silently).
        Assert.False(EcmaScriptExportNameValidator.IsValid("\ud800", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("\udc00", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("\ud800\ud800", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("\udc00\udc00", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("a\ud800b", out _));
        Assert.True(EcmaScriptExportNameValidator.IsValid("\ud83d\ude00", out var pairCount));
        Assert.Equal(1, pairCount); // a surrogate pair counts as one scalar
        Assert.False(EcmaScriptExportNameValidator.IsValid(string.Empty, out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("   ", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("a\u0001b", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("a\u000Ab", out _));
        Assert.True(EcmaScriptExportNameValidator.IsValid("a\u2028b", out _)); // line separator is not a control scalar
    }

    [Fact]
    public async Task Q1_E_DurableReopenPreservesExactDistinctiveNameAndIdentity()
    {
        using var directory = TemporaryDirectory.Create("hetu-q1-unicode-durable-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "durable.ts");
        var supplementary = char.ConvertFromUtf32(0x1F600);
        var distinctName = supplementary + "q" + supplementary + "1" + supplementary; // alternating scalars
        await File.WriteAllTextAsync(filePath, $"const local = 1;\nexport {{ local as \"{distinctName}\" }};\n");

        var repositoryId = new CodeRepositoryId("repo:q1-unicode-durable");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var databasePath = Path.Combine(directory.FullName, "graph.db");
        string entryId;
        using (var store = new LatticeCodeGraphStore(databasePath))
        {
            var indexed = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:q1-unicode-first"));
            Assert.Equal(CodeIndexRunStatus.Completed, indexed.Diagnostics.Status);
            var queries = new CodeGraphQueryService(store);
            var lookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, distinctName);
            var entry = Assert.Single(lookup.Candidates);
            entryId = entry.Id.Value;
            Assert.Equal(ExpectedEntryId("typescript", distinctName, "src/durable.ts"), entryId);
        }

        using var reopened = new LatticeCodeGraphStore(databasePath);
        var reopenedQueries = new CodeGraphQueryService(reopened);
        var reopenedLookup = await reopenedQueries.FindSymbolsByQualifiedNameAsync(repositoryId, distinctName);
        var reopenedEntry = Assert.Single(reopenedLookup.Candidates);
        Assert.Equal(distinctName, reopenedEntry.Name);
        Assert.Equal(distinctName, reopenedEntry.QualifiedName);
        Assert.Null(reopenedEntry.SymbolId);
        Assert.Equal(entryId, reopenedEntry.Id.Value);
        Assert.Equal(ExpectedEntryId("typescript", distinctName, "src/durable.ts"), reopenedEntry.Id.Value);
        var neighborhood = await reopenedQueries.GetNeighborhoodAsync(
            repositoryId,
            new CodeNodeId(ExpectedFileId("typescript", "src/durable.ts")),
            [CodeEdgeKinds.Exports],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 50, maxEdges: 50));
        var edge = Assert.Single(neighborhood.Edges);
        Assert.Equal(reopenedEntry.Id, edge.TargetId);
        Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
    }

    // ------------------------------------------------------------------
    // F. Publication lifecycle (both stores)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Q1_F_MovementKeepsEntryIdAndChangesEdgeId_RenameReplacesBoth(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-q1-move-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "main.ts");
        await File.WriteAllTextAsync(filePath, "const x = 1;\nexport { x as a };\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-move-{(durable ? "lattice" : "memory")}");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var indexing = CreateIndexing(store, new TreeSitterTypeScriptPlugin());
            var queries = new CodeGraphQueryService(store);
            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q1-move-1"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);

            var firstEdges = await ExportEdges(queries, repositoryId, "src/main.ts");
            var firstEntryId = firstEdges.Single().TargetId;
            Assert.Equal(ExpectedEntryId("typescript", "a", "src/main.ts"), firstEntryId.Value);

            // Pure movement (the occurrence lands on a different line): the
            // entry slot identity is unchanged, the occurrence identity changes
            // with its span.
            await File.WriteAllTextAsync(filePath, "const x = 1;\n\nexport { x as a };\n");
            var moved = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q1-move-2"));
            Assert.Equal(CodeIndexRunStatus.Completed, moved.Diagnostics.Status);
            var movedEdges = await ExportEdges(queries, repositoryId, "src/main.ts");
            var movedEdge = Assert.Single(movedEdges);
            Assert.Equal(firstEntryId, movedEdge.TargetId);
            Assert.NotEqual(firstEdges.Single().Id, movedEdge.Id);

            // Alias rename: both the entry and the occurrence identities change.
            await File.WriteAllTextAsync(filePath, "const x = 1;\n\nexport { x as b };\n");
            var renamed = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q1-move-3"));
            Assert.Equal(CodeIndexRunStatus.Completed, renamed.Diagnostics.Status);
            var renamedEdges = await ExportEdges(queries, repositoryId, "src/main.ts");
            var renamedEdge = Assert.Single(renamedEdges);
            Assert.NotEqual(firstEntryId, renamedEdge.TargetId);
            Assert.Equal(ExpectedEntryId("typescript", "b", "src/main.ts"), renamedEdge.TargetId.Value);
            Assert.Null(await store.GetNodeAsync(repositoryId, firstEntryId));
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Q1_F_DuplicateOccurrenceRemovalRetainsSiblingAndEntry(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-q1-dup-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "main.ts");
        await File.WriteAllTextAsync(filePath,
            "const x = 1;\nexport { x as c };\nexport { x as c };\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-dup-{(durable ? "lattice" : "memory")}");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var indexing = CreateIndexing(store, new TreeSitterTypeScriptPlugin());
            var queries = new CodeGraphQueryService(store);
            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q1-dup-1"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
            var firstEdges = await ExportEdges(queries, repositoryId, "src/main.ts");
            Assert.Equal(2, firstEdges.Count);
            Assert.Single(firstEdges.Select(edge => edge.TargetId).Distinct());
            var surviving = firstEdges.OrderBy(edge => edge.Evidence.Location!.StartLine).First();

            // Removing one of two duplicate occurrences removes exactly one
            // occurrence edge and retains the shared entry and sibling edge.
            await File.WriteAllTextAsync(filePath, "const x = 1;\nexport { x as c };\n");
            var second = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q1-dup-2"));
            Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);
            var secondEdges = await ExportEdges(queries, repositoryId, "src/main.ts");
            var secondEdge = Assert.Single(secondEdges);
            Assert.Equal(surviving.Id, secondEdge.Id);
            Assert.Equal(surviving.TargetId, secondEdge.TargetId);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Q1_F_EmptyReplacementAndDeletionPreserveUnrelatedFacts(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-q1-empty-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "main.ts");
        await File.WriteAllTextAsync(filePath, "const kept = 5;\nexport { kept as gone };\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-empty-{(durable ? "lattice" : "memory")}");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var indexing = CreateIndexing(store, new TreeSitterTypeScriptPlugin());
            var queries = new CodeGraphQueryService(store);
            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q1-empty-1"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
            Assert.Single(await ExportEdges(queries, repositoryId, "src/main.ts"));
            var fileId = new CodeNodeId(ExpectedFileId("typescript", "src/main.ts"));

            // Empty replacement removes every export fact of the file while
            // retaining the unrelated declaration fact of the same file.
            await File.WriteAllTextAsync(filePath, "const kept = 5;\n");
            var emptied = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q1-empty-2"));
            Assert.Equal(CodeIndexRunStatus.Completed, emptied.Diagnostics.Status);
            Assert.Empty(await ExportEdges(queries, repositoryId, "src/main.ts"));
            Assert.Null(await store.GetNodeAsync(repositoryId, new CodeNodeId(ExpectedEntryId("typescript", "gone", "src/main.ts"))));
            var declarations = await queries.GetDeclarationsInFileAsync(repositoryId, "src/main.ts");
            var keptDeclaration = Assert.Single(declarations);
            var keptNode = await store.GetNodeAsync(repositoryId, keptDeclaration.SymbolNodeId);
            Assert.Equal("kept", keptNode!.Name);
            Assert.NotNull(await store.GetNodeAsync(repositoryId, fileId));

            // File deletion removes the file unit entirely: no file node and no
            // export facts anywhere in the published units.
            File.Delete(filePath);
            var deleted = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q1-empty-3"));
            Assert.Equal(CodeIndexRunStatus.Completed, deleted.Diagnostics.Status);
            Assert.Null(await store.GetNodeAsync(repositoryId, fileId));
            Assert.DoesNotContain(
                (await store.GetPublishedUnitsAsync(repositoryId)).SelectMany(unit => unit.Edges),
                edge => edge.Kind == CodeEdgeKinds.Exports);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Q1_F_FailureAndCancellationBeforeCommitRetainCompletePriorPublication(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-q1-fail-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "main.ts");
        await File.WriteAllTextAsync(filePath, "import './dep';\nexport { value as result };\n");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "dep.ts"), "export const value = 1;\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-fail-{(durable ? "lattice" : "memory")}");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var initial = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:q1-fail-success"));
            Assert.Equal(CodeIndexRunStatus.Completed, initial.Diagnostics.Status);
            var queries = new CodeGraphQueryService(store);
            var publishedEdge = Assert.Single(await ExportEdges(queries, repositoryId, "src/main.ts"));
            var priorState = (await store.GetPublishedUnitsAsync(repositoryId)).Select(SerializeUnit).OrderBy(value => value, StringComparer.Ordinal).ToArray();

            // --- Failure before commit ---
            await File.WriteAllTextAsync(filePath, "// shifted\nimport './dep';\nexport { value as renamed };\n");
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await CreateIndexing(store, new TreeSitterTypeScriptPlugin(new FailingModuleResolver()))
                    .IndexAsync(descriptor, new CodeIndexRunId("run:q1-fail-failure")));
            var failedRun = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:q1-fail-failure"));
            Assert.Equal(CodeIndexRunStatus.Failed, failedRun!.Status);
            Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
            Assert.Equal(priorState, (await store.GetPublishedUnitsAsync(repositoryId)).Select(SerializeUnit).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Assert.Equal(publishedEdge.Id, Assert.Single(await ExportEdges(queries, repositoryId, "src/main.ts")).Id);

            // --- Cancellation before commit ---
            using var cancellation = new CancellationTokenSource();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await CreateIndexing(store, new TreeSitterTypeScriptPlugin(new CancellingModuleResolver(cancellation)))
                    .IndexAsync(descriptor, new CodeIndexRunId("run:q1-fail-cancelled"), cancellationToken: cancellation.Token));
            var cancelledRun = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:q1-fail-cancelled"));
            Assert.Equal(CodeIndexRunStatus.Cancelled, cancelledRun!.Status);
            Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
            Assert.Equal(priorState, (await store.GetPublishedUnitsAsync(repositoryId)).Select(SerializeUnit).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Assert.Equal(publishedEdge.Id, Assert.Single(await ExportEdges(queries, repositoryId, "src/main.ts")).Id);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    // ------------------------------------------------------------------
    // H. Consumer compatibility
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Q1_H_ConsumerQueriesDistinguishExportEntriesFromDeclarations(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-q1-consumer-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "main.ts");
        await File.WriteAllTextAsync(filePath, "export class Widget { method() {} }\nexport const widget2 = 1;\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-consumer-{(durable ? "lattice" : "memory")}");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var indexed = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:q1-consumer"));
            Assert.Equal(CodeIndexRunStatus.Completed, indexed.Diagnostics.Status);
            var queries = new CodeGraphQueryService(store);

            // Exact-name lookup is an additive candidate set: the export entry
            // and the class declaration are distinguishable by Kind/SymbolId.
            var widgetLookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Widget");
            Assert.Equal(2, widgetLookup.Candidates.Count);
            var exportEntry = Assert.Single(widgetLookup.Candidates, node => node.Kind == CodeNodeKinds.Export);
            var declaration = Assert.Single(widgetLookup.Candidates, node => node.Kind == CodeNodeKinds.Type);
            Assert.Null(exportEntry.SymbolId);
            Assert.NotNull(declaration.SymbolId);

            // Declaration navigation continues through the symbol-bearing node only.
            var declarations = await queries.FindDeclarationsAsync(repositoryId, declaration.SymbolId!);
            Assert.Single(declarations);
            Assert.Equal(declaration.Id, Assert.Single(declarations).SymbolNodeId);

            // Containment-preset and references-preset traversals must not leak
            // export facts; exports appear only on explicit or unfiltered requests.
            var containment = await queries.GetNeighborhoodAsync(
                repositoryId,
                new CodeNodeId(ExpectedFileId("typescript", "src/main.ts")),
                [CodeEdgeKinds.Contains],
                new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
            Assert.DoesNotContain(containment.Edges, edge => edge.Kind == CodeEdgeKinds.Exports);
            var references = await queries.FindReferencesAsync(repositoryId, exportEntry.Id);
            Assert.Empty(references.Edges);
            var explicitExports = await queries.GetNeighborhoodAsync(
                repositoryId,
                new CodeNodeId(ExpectedFileId("typescript", "src/main.ts")),
                [CodeEdgeKinds.Exports],
                new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
            Assert.Equal(2, explicitExports.Edges.Count);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    // ------------------------------------------------------------------
    // Shared helpers (qualification-local, independent of implementation code)
    // ------------------------------------------------------------------

    private static ICodeGraphStore CreateStore(bool durable, string databasePath) =>
        durable ? new LatticeCodeGraphStore(databasePath) : new InMemoryCodeGraphStore();

    private static CodeIndexingService CreateIndexing(ICodeGraphStore store, ICodeGraphPlugin plugin) =>
        new(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([plugin]),
            store);

    private static async Task<IReadOnlyList<CodeGraphEdge>> ExportEdges(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        string path)
    {
        var result = await queries.GetNeighborhoodAsync(
            repositoryId,
            new CodeNodeId(ExpectedFileId("typescript", path)),
            [CodeEdgeKinds.Exports],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
        return result.Edges;
    }

    private static string SerializeUnit(CodeIndexUnitReplacement unit) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            UnitId = unit.Origin.IndexUnitId.Value,
            PluginVersion = unit.Origin.PluginVersion,
            Nodes = unit.Nodes.Select(SerializeNode).OrderBy(value => value, StringComparer.Ordinal),
            Declarations = unit.Declarations.Select(SerializeDeclaration).OrderBy(value => value, StringComparer.Ordinal),
            Edges = unit.Edges.Select(SerializeEdge).OrderBy(value => value, StringComparer.Ordinal)
        });

    private static string SerializeNode(CodeGraphNode node) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            Id = node.Id.Value,
            Kind = node.Kind.Value,
            node.Name,
            node.QualifiedName,
            SymbolId = node.SymbolId?.Value,
            Properties = SortedProperties(node.Properties)
        });

    private static string SerializeDeclaration(CodeGraphDeclaration declaration) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            Id = declaration.Id.Value,
            SymbolNodeId = declaration.SymbolNodeId.Value,
            SymbolId = declaration.SymbolId.Value
        });

    private static string SerializeEdge(CodeGraphEdge edge) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            Id = edge.Id.Value,
            SourceId = edge.SourceId.Value,
            TargetId = edge.TargetId.Value,
            Kind = edge.Kind.Value,
            Evidence = new
            {
                Quality = edge.Evidence.Quality.ToString(),
                edge.Evidence.Provider,
                edge.Evidence.ProviderVersion,
                Location = edge.Evidence.Location is null ? null : new
                {
                    edge.Evidence.Location.Path,
                    edge.Evidence.Location.StartLine,
                    edge.Evidence.Location.StartColumn,
                    edge.Evidence.Location.EndLine,
                    edge.Evidence.Location.EndColumn
                },
                edge.Evidence.Confidence
            },
            Properties = SortedProperties(edge.Properties)
        });

    private static IReadOnlyList<(string Key, string Value)> SortedProperties(IReadOnlyDictionary<string, CodePropertyValue> properties) =>
        properties.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => (pair.Key, Value: pair.Value switch
            {
                CodeTextProperty text => "t:" + text.Value,
                CodeBooleanProperty boolean => "b:" + boolean.Value,
                CodeIntegerProperty integer => "i:" + integer.Value.ToString(CultureInfo.InvariantCulture),
                CodeNumberProperty number => "n:" + number.Value.ToString("R", CultureInfo.InvariantCulture),
                CodeTextListProperty list => "l:" + string.Join(",", list.Values),
                _ => "o:" + pair.Value.ToString()
            }))
            .ToArray();

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(DirectoryInfo directory) => Directory = directory;

        internal DirectoryInfo Directory { get; }
        internal string FullName => Directory.FullName;

        internal static TemporaryDirectory Create(string prefix) =>
            new(System.IO.Directory.CreateTempSubdirectory(prefix));

        public void Dispose() => Directory.Delete(recursive: true);
    }

    private sealed class FailingModuleResolver : ITreeSitterModuleResolver
    {
        public string Language => "typescript";
        public string Provider => EcmaScriptModuleResolver.ResolverProvider;
        public string ProviderVersion => "0.2.0-preview.6+resolver2.relative-v1";

        public TreeSitterModuleResolutionResult Resolve(
            IReadOnlyList<TreeSitterModuleFile> inventory,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Q1 qualification: injected module-resolution failure after extraction.");
    }

    private sealed class CancellingModuleResolver(CancellationTokenSource cancellation) : ITreeSitterModuleResolver
    {
        public string Language => "typescript";
        public string Provider => EcmaScriptModuleResolver.ResolverProvider;
        public string ProviderVersion => "0.2.0-preview.6+resolver2.relative-v1";

        public TreeSitterModuleResolutionResult Resolve(
            IReadOnlyList<TreeSitterModuleFile> inventory,
            CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Q1 qualification: cancellation was not observed.");
        }
    }
}
