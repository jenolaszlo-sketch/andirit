using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Andirit.TreeSitter.Tests;

public sealed class TreeSitterEcmaExportShapeTests
{
    [Fact]
    public void ExtractsDirectDeclarationsWithSyntaxGroundedProjectionAndSpans()
    {
        const string source = """
            export const first = 1, second = 2;
            export let third = 3;
            export var fourth = 4;
            export async function load() {}
            export class Project {}
            export abstract class AbstractProject {}
            export interface Model {}
            export type ModelId = string;
            export enum Status { Active }
            export namespace Configuration {}
            """;

        var result = Observe("typescript", "src/direct.ts", source, out var hasParseError);

        Assert.False(hasParseError);
        Assert.Equal(
            ["first", "second", "third", "fourth", "load", "Project", "AbstractProject", "Model", "ModelId", "Status", "Configuration"],
            result.Select(observation => observation.ExportedName));
        Assert.All(result, observation =>
        {
            Assert.Equal("src/direct.ts", observation.SourcePath);
            Assert.Equal("typescript", observation.Language);
            Assert.Equal(EcmaScriptExportKind.Named, observation.ExportKind);
            Assert.Equal(EcmaScriptExportForm.DirectDeclaration, observation.ExportForm);
            Assert.False(observation.TypeOnly);
            Assert.Equal(observation.ExportedName, observation.LocalName);
            Assert.Equal(new CodeLocation("src/direct.ts", observation.NameLocation.StartLine, 1, observation.NameLocation.StartLine, source.Split('\n')[observation.NameLocation.StartLine - 1].TrimEnd('\r').Length + 1), observation.ConstructLocation);
        });
        Assert.Equal(EcmaScriptNamespaceProjection.Value, result[0].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Value, result[4].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Both, result[5].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Type, result[7].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Type, result[8].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Both, result[9].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Unknown, result[10].Projection);
        Assert.Equal(new CodeLocation("src/direct.ts", 1, 14, 1, 19), result[0].NameLocation);
        Assert.Equal(new CodeLocation("src/direct.ts", 1, 25, 1, 31), result[1].NameLocation);
    }

    [Fact]
    public void KeepsDirectOverloadSitesAsSeparateOccurrences()
    {
        const string source = "export function parse(value: string): void;\nexport function parse(value: any) {}";

        var observations = Observe("typescript", "src/overload.ts", source, out var hasParseError);

        Assert.False(hasParseError);
        Assert.Equal(2, observations.Count);
        Assert.All(observations, observation =>
        {
            Assert.Equal("parse", observation.ExportedName);
            Assert.Equal("parse", observation.LocalName);
            Assert.Equal(EcmaScriptExportForm.DirectDeclaration, observation.ExportForm);
            Assert.Equal(EcmaScriptNamespaceProjection.Value, observation.Projection);
        });
        Assert.NotEqual(observations[0].NameLocation, observations[1].NameLocation);
    }
    [Fact]
    public void PreservesEachLocalNameAliasDuplicateAndDefaultAlias()
    {
        const string source = "export { value, value as first, value as second, left as repeated, right as repeated, value as default };";

        var observations = Observe("typescript", "src/lists.ts", source, out var hasParseError);

        Assert.False(hasParseError);
        Assert.Equal(
            ["value", "first", "second", "repeated", "repeated", "default"],
            observations.Select(observation => observation.ExportedName));
        Assert.Equal("value", observations[0].LocalName);
        Assert.Equal(EcmaScriptExportForm.LocalName, observations[0].ExportForm);
        Assert.All(observations, observation =>
        {
            Assert.Equal(EcmaScriptNamespaceProjection.Unknown, observation.Projection);
            Assert.False(observation.TypeOnly);
        });
        Assert.Equal(EcmaScriptExportForm.LocalName, observations[0].ExportForm);
        Assert.All(observations.Skip(1), observation =>
            Assert.Equal(EcmaScriptExportForm.LocalAlias, observation.ExportForm));
        Assert.Equal("left", observations[3].LocalName);
        Assert.Equal("right", observations[4].LocalName);
        Assert.NotEqual(observations[3].NameLocation, observations[4].NameLocation);
        Assert.Equal(EcmaScriptExportKind.Default, observations[5].ExportKind);
        Assert.Equal("value", observations[5].LocalName);
        Assert.Equal(EcmaScriptExportForm.LocalAlias, observations[5].ExportForm);
    }

    [Fact]
    public void CapturesGrammarDerivedTypeOnlyAndContextualTypeNames()
    {
        const string typeOnlySource = """
            interface Model {}
            export { type Model as Shape, Model as Plain };
            export type { Model as ExportedModel };
            """;

        var typeObservations = Observe("typescript", "src/types.ts", typeOnlySource, out var typeParseError);

        Assert.False(typeParseError);
        Assert.Equal(["Shape", "Plain", "ExportedModel"], typeObservations.Select(item => item.ExportedName));
        Assert.True(typeObservations[0].TypeOnly);
        Assert.Equal(EcmaScriptNamespaceProjection.Type, typeObservations[0].Projection);
        Assert.Equal("Model", typeObservations[0].LocalName);
        Assert.False(typeObservations[1].TypeOnly);
        Assert.Equal(EcmaScriptNamespaceProjection.Unknown, typeObservations[1].Projection);
        Assert.True(typeObservations[2].TypeOnly);
        Assert.Equal(EcmaScriptNamespaceProjection.Type, typeObservations[2].Projection);

        var contextual = Observe("typescript", "src/contextual-type.ts", "const type = 1;\nexport { type };", out var contextualParseError);
        Assert.True(contextualParseError);
        var contextualObservation = Assert.Single(contextual);
        Assert.Equal("type", contextualObservation.ExportedName);
        Assert.Equal("type", contextualObservation.LocalName);
        Assert.False(contextualObservation.TypeOnly);
        Assert.Equal(EcmaScriptNamespaceProjection.Unknown, contextualObservation.Projection);

        var contextualAlias = Observe("typescript", "src/contextual-alias.ts", "export { type as kind };", out var contextualAliasParseError);
        Assert.True(contextualAliasParseError);
        var contextualAliasObservation = Assert.Single(contextualAlias);
        Assert.Equal("kind", contextualAliasObservation.ExportedName);
        Assert.Equal("type", contextualAliasObservation.LocalName);
        Assert.Equal(EcmaScriptExportForm.LocalAlias, contextualAliasObservation.ExportForm);
        Assert.False(contextualAliasObservation.TypeOnly);
        Assert.Equal(EcmaScriptNamespaceProjection.Unknown, contextualAliasObservation.Projection);
    }
    [Fact]
    public void CapturesImportedLocalExportWithoutRemoteBinding()
    {
        const string source = "import { value as local } from \"./dep\";\nexport { local as publicName };";

        var extraction = TreeSitterTestSupport.ExtractSyntax("typescript", "src/main.ts", source, out var hasParseError);

        Assert.False(hasParseError);
        var observation = Assert.Single(extraction.ExportObservations);
        Assert.Equal("publicName", observation.ExportedName);
        Assert.Equal("local", observation.LocalName);
        Assert.Equal(EcmaScriptExportForm.LocalAlias, observation.ExportForm);
        Assert.Equal(EcmaScriptNamespaceProjection.Unknown, observation.Projection);
        Assert.Single(extraction.ModuleReferences);
        Assert.Equal("./dep", extraction.ModuleReferences[0].Specifier);
    }

    [Fact]
    public void CapturesDefaultDeclarationExpressionAndLocalNameForms()
    {
        var cases = new (string Source, EcmaScriptExportForm Form, string? LocalName, EcmaScriptNamespaceProjection Projection)[]
        {
            ("export default function named() {}", EcmaScriptExportForm.DefaultNamedDeclaration, "named", EcmaScriptNamespaceProjection.Value),
            ("export default function() {}", EcmaScriptExportForm.DefaultAnonymousDeclaration, null, EcmaScriptNamespaceProjection.Value),
            ("export default class Named {}", EcmaScriptExportForm.DefaultNamedDeclaration, "Named", EcmaScriptNamespaceProjection.Both),
            ("export default class {}", EcmaScriptExportForm.DefaultAnonymousDeclaration, null, EcmaScriptNamespaceProjection.Both),
            ("export default 42;", EcmaScriptExportForm.DefaultExpression, null, EcmaScriptNamespaceProjection.Value),
            ("export default current;", EcmaScriptExportForm.DefaultExpression, "current", EcmaScriptNamespaceProjection.Value)
        };

        foreach (var (source, form, localName, projection) in cases)
        {
            var observations = Observe("typescript", "src/default.ts", source, out var hasParseError);
            Assert.False(hasParseError, source);
            var observation = Assert.Single(observations);
            Assert.Equal("default", observation.ExportedName);
            Assert.Equal(EcmaScriptExportKind.Default, observation.ExportKind);
            Assert.Equal(form, observation.ExportForm);
            Assert.Equal(localName, observation.LocalName);
            Assert.Equal(projection, observation.Projection);
            Assert.Equal(new CodeLocation("src/default.ts", 1, 8, 1, 15), observation.NameLocation);
        }
    }

    [Fact]
    public void AcceptsSimpleQuotedAndUnicodeNamesButRejectsEscapesAndInvalidNames()
    {
        var unicode = Observe("typescript", "src/quoted.ts", "export { local as \"public😀\" };", out var unicodeError);
        Assert.False(unicodeError);
        var unicodeObservation = Assert.Single(unicode);
        Assert.Equal("public😀", unicodeObservation.ExportedName);
        Assert.Equal("local", unicodeObservation.LocalName);
        Assert.Equal(10, unicodeObservation.NameLocation.EndColumn - unicodeObservation.NameLocation.StartColumn);

        var grammarIdentifier = Observe("typescript", "src/unicode-identifier.ts", "export const ℘ = 1;", out var grammarIdentifierError);
        Assert.False(grammarIdentifierError);
        Assert.Equal("℘", Assert.Single(grammarIdentifier).ExportedName);

        Assert.Empty(Observe("typescript", "src/escaped-string.ts", "export { local as \"bad\\u0061\" };", out _));
        Assert.Empty(Observe("typescript", "src/escaped-id.ts", "export const \\u0061 = 1;", out _));
        Assert.Empty(Observe("typescript", "src/empty.ts", "export { local as \"\" };", out _));
        Assert.Empty(Observe("typescript", "src/whitespace.ts", "export { local as \"   \" };", out _));
    }

    [Fact]
    public void AppliesNameBoundToWrittenQuotedAliases()
    {
        var acceptedName = new string('a', 256);
        var accepted = Observe("typescript", "src/boundary.ts", $"export {{ local as \"{acceptedName}\" }};", out var acceptedError);
        Assert.False(acceptedError);
        Assert.Equal(acceptedName, Assert.Single(accepted).ExportedName);

        var rejectedName = new string('a', 257);
        var rejected = Observe("typescript", "src/over-boundary.ts", $"export {{ local as \"{rejectedName}\" }};", out _);
        Assert.Empty(rejected);

        var normalizedNames = Observe(
            "typescript",
            "src/normalization.ts",
            "export { local as \"é\" };\nexport { local as \"e\u0301\" };",
            out var normalizationError);
        Assert.False(normalizationError);
        Assert.Equal(["é", "e\u0301"], normalizedNames.Select(item => item.ExportedName));
        Assert.NotEqual(normalizedNames[0].ExportedName, normalizedNames[1].ExportedName);
    }
    [Fact]
    public void ValidatesStrictUnicodeScalarBoundariesWithoutNormalization()
    {
        var supplementary = char.ConvertFromUtf32(0x1f600);
        Assert.True(EcmaScriptExportNameValidator.IsValid(new string('a', 256), out var count));
        Assert.Equal(256, count);
        Assert.False(EcmaScriptExportNameValidator.IsValid(new string('a', 257), out _));
        Assert.True(EcmaScriptExportNameValidator.IsValid(string.Concat(Enumerable.Repeat(supplementary, 256)), out count));
        Assert.Equal(256, count);
        Assert.False(EcmaScriptExportNameValidator.IsValid(string.Concat(Enumerable.Repeat(supplementary, 257)), out _));
        Assert.True(EcmaScriptExportNameValidator.IsValid(string.Concat(Enumerable.Repeat(supplementary, 128)) + new string('a', 128), out count));
        Assert.Equal(256, count);
        Assert.False(EcmaScriptExportNameValidator.IsValid(string.Concat(Enumerable.Repeat(supplementary, 129)) + new string('a', 128), out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid(string.Empty, out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("   ", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("\ud800", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("\udc00", out _));
        Assert.False(EcmaScriptExportNameValidator.IsValid("name\u0001", out _));
        Assert.True(EcmaScriptExportNameValidator.IsValid("e\u0301", out count));
        Assert.Equal(2, count);
        Assert.True(EcmaScriptExportNameValidator.IsValid("é", out count));
        Assert.Equal(1, count);
        Assert.NotEqual("é", "e\u0301");
        Assert.NotEqual("Name", "name");
    }

    [Fact]
    public void PreservesUtf16SpansLineEndingsAndDeterministicOccurrenceOrder()
    {
        const string source = "const emoji = \"😀\"; export { local as repeated }; export { other as repeated };";
        var first = Observe("typescript", "src/span.ts", source, out var firstError);
        var second = Observe("typescript", "src/span.ts", source, out var secondError);

        Assert.False(firstError);
        Assert.False(secondError);
        Assert.Equal(first, second);
        Assert.Equal(2, first.Count);
        var firstNameIndex = source.IndexOf("repeated", StringComparison.Ordinal);
        var secondNameIndex = source.LastIndexOf("repeated", StringComparison.Ordinal);
        Assert.Equal(firstNameIndex + 1, first[0].NameLocation.StartColumn);
        Assert.Equal(secondNameIndex + 1, first[1].NameLocation.StartColumn);
        Assert.Equal(first[0].NameLocation.StartColumn + "repeated".Length, first[0].NameLocation.EndColumn);
        Assert.NotEqual(first[0].NameLocation, first[1].NameLocation);

        const string lf = "export { x as result };\nexport { y as result };";
        const string crlf = "export { x as result };\r\nexport { y as result };";
        var lfResult = Observe("typescript", "src/lines.ts", lf, out _);
        var crlfResult = Observe("typescript", "src/lines.ts", crlf, out _);
        Assert.Equal(lfResult.Select(item => item.NameLocation), crlfResult.Select(item => item.NameLocation));
        Assert.Equal([1, 2], lfResult.Select(item => item.NameLocation.StartLine));
    }

    [Fact]
    public void OrdersDuplicateNamesAcrossFilesBySourcePath()
    {
        const string source = "export { local as same };";
        var laterPath = Observe("typescript", "z/file.ts", source, out _).Single();
        var earlierPath = Observe("typescript", "a/file.ts", source, out _).Single();
        var ordered = new[] { laterPath, earlierPath }
            .OrderBy(observation => observation, EcmaScriptExportObservation.SourceOrderComparer)
            .ToArray();

        Assert.Equal(["a/file.ts", "z/file.ts"], ordered.Select(item => item.SourcePath));
        Assert.Equal("same", ordered[0].ExportedName);
        Assert.Equal("same", ordered[1].ExportedName);
        Assert.NotEqual(ordered[0].SourcePath, ordered[1].SourcePath);
    }
    [Fact]
    public void ExcludesReexportsCommonJsAmbientDestructuringAndRecoveredNames()
    {
        const string reexports = """
            export { remote as publicName } from "./dep";
            export * from "./dep";
            export * as namespace from "./dep";
            """;
        var reexportResult = TreeSitterTestSupport.ExtractSyntax("typescript", "src/reexports.ts", reexports, out var reexportError);
        Assert.False(reexportError);
        Assert.Empty(reexportResult.ExportObservations);

        const string mixedSource = "export const observed = 1;\nexport { remote as hidden } from \"./dep\";\nexport * from \"./other\";";
        var mixed = TreeSitterTestSupport.ExtractSyntax("typescript", "src/mixed.ts", mixedSource, out var mixedError);
        Assert.False(mixedError);
        Assert.Equal(["observed"], mixed.ExportObservations.Select(item => item.ExportedName));
        Assert.Equal(2, mixed.ModuleReferences.Count);
        Assert.Equal(3, reexportResult.ModuleReferences.Count);

        const string unsupported = """
            export declare const ambient: number;
            export const { destructured } = source;
            export = value;
            module.exports = value;
            exports.name = value;
            """;
        var unsupportedResult = Observe("typescript", "src/unsupported.ts", unsupported, out _);
        Assert.Empty(unsupportedResult);

        const string nested = "namespace N { export const nested = 1; }\nexport namespace Outer { export const nested = 2; }";
        var nestedResult = Observe("typescript", "src/nested.ts", nested, out var nestedError);
        Assert.False(nestedError);
        var outer = Assert.Single(nestedResult);
        Assert.Equal("Outer", outer.ExportedName);
        Assert.Equal(EcmaScriptNamespaceProjection.Unknown, outer.Projection);

        var malformed = Observe("typescript", "src/malformed.ts", "export { broken as };", out var malformedError);
        Assert.True(malformedError);
        Assert.Empty(malformed);

        Assert.Empty(Observe("javascript", "src/type-only.js", "export type { Model };", out _));
    }

    [Fact]
    public void CapturesJavaScriptValueExportsOnly()
    {
        const string source = "export const value = 1;\nexport function run() {}\nexport class Project {}\nexport default 42;";
        var observations = Observe("javascript", "src/module.js", source, out var hasParseError);

        Assert.False(hasParseError);
        Assert.Equal(["value", "run", "Project", "default"], observations.Select(item => item.ExportedName));
        Assert.Equal(EcmaScriptNamespaceProjection.Value, observations[0].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Value, observations[1].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Both, observations[2].Projection);
        Assert.Equal(EcmaScriptNamespaceProjection.Value, observations[3].Projection);
        Assert.All(observations, item => Assert.Equal("javascript", item.Language));
    }

    [Fact]
    public async Task PublishedGraphAddsExportsWithoutChangingPreChangeP8Facts()
    {
        const string typescriptMain = """
            import { type Remote } from "./dep";
            export const value = 1;
            export function run() { return value; }
            export { value as current };
            export { remote as forwarded } from "./dep";
            """;
        const string typescriptDep = "export interface Remote {}\nexport const remote = 2;";
        const string javascriptMain = """
            import { item } from "./dep";
            export const value = 1;
            export default function run() { return item; }
            export { value as current };
            export { remote as forwarded } from "./dep";
            """;
        const string javascriptDep = "export const item = 1;\nexport const remote = 2;";

        var typescript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/main.ts", NormalizeLineEndings(typescriptMain)),
            ("src/dep.ts", NormalizeLineEndings(typescriptDep)));
        var javascript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/main.js", NormalizeLineEndings(javascriptMain)),
            ("src/dep.js", NormalizeLineEndings(javascriptDep)));

        Assert.Equal(9, EcmaScriptExtractionProfile.ProfileRevision);
        Assert.EndsWith(".p9", new TreeSitterTypeScriptPlugin().Version);
        Assert.EndsWith(".p9", new TreeSitterJavaScriptPlugin().Version);
        Assert.Equal((5, 12, 4, 12, 17), (
            typescript.Nodes.Count(node => node.Kind == CodeNodeKinds.Export),
            typescript.Nodes.Count,
            typescript.Declarations.Count,
            typescript.Edges.Count(edge => edge.Kind != CodeEdgeKinds.Exports),
            typescript.Edges.Count));
        Assert.Equal((5, 12, 4, 12, 17), (
            javascript.Nodes.Count(node => node.Kind == CodeNodeKinds.Export),
            javascript.Nodes.Count,
            javascript.Declarations.Count,
            javascript.Edges.Count(edge => edge.Kind != CodeEdgeKinds.Exports),
            javascript.Edges.Count));
        Assert.Equal("84c13587db0ef90751bbb6f9c561b1294b541dc7fade44d15bdf54a6fb5722bf", PreExportGraphFingerprint(typescript));
        Assert.Equal("3b2d3761334e5f4b87423cf0f84fa8824c2618450f4e5b98386ee861fdf32cdd", PreExportGraphFingerprint(javascript));
        Assert.Contains(typescript.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Contains(javascript.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal(5, typescript.Nodes.Count(node => node.Kind == CodeNodeKinds.Export));
        Assert.Equal(5, javascript.Nodes.Count(node => node.Kind == CodeNodeKinds.Export));
        Assert.DoesNotContain(typescript.Nodes.Where(node => node.Kind == CodeNodeKinds.Export), node => node.SymbolId is not null);
        Assert.DoesNotContain(javascript.Nodes.Where(node => node.Kind == CodeNodeKinds.Export), node => node.SymbolId is not null);
        Assert.DoesNotContain(typescript.Edges, edge => edge.Kind.Value is "exported-as" or "binds");
        Assert.DoesNotContain(javascript.Edges, edge => edge.Kind.Value is "exported-as" or "binds");
        Assert.DoesNotContain(typescript.Result.RelationshipCoverage, item => item.RelationshipKind == CodeEdgeKinds.Exports.Value);
        Assert.DoesNotContain(javascript.Result.RelationshipCoverage, item => item.RelationshipKind == CodeEdgeKinds.Exports.Value);
        Assert.All(typescript.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports), edge =>
        {
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
            Assert.Equal(EcmaScriptExportGraphPublisher.Provider, edge.Evidence.Provider);
            Assert.Contains("+exports1.local-v1", edge.Evidence.ProviderVersion!, StringComparison.Ordinal);
        });
        Assert.All(javascript.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports), edge =>
        {
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
            Assert.Equal(EcmaScriptExportGraphPublisher.Provider, edge.Evidence.Provider);
            Assert.Contains("+exports1.local-v1", edge.Evidence.ProviderVersion!, StringComparison.Ordinal);
        });
        Assert.All(
            typescript.Edges.Where(edge => edge.Kind != CodeEdgeKinds.Exports &&
                edge.Evidence.Provider == new TreeSitterTypeScriptPlugin().Id.Value),
            edge => Assert.EndsWith(".p8", edge.Evidence.ProviderVersion, StringComparison.Ordinal));
        Assert.All(
            javascript.Edges.Where(edge => edge.Kind != CodeEdgeKinds.Exports &&
                edge.Evidence.Provider == new TreeSitterJavaScriptPlugin().Id.Value),
            edge => Assert.EndsWith(".p8", edge.Evidence.ProviderVersion, StringComparison.Ordinal));
        Assert.Contains(typescript.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo && edge.Evidence.ProviderVersion!.Contains("resolver2.relative-v1", StringComparison.Ordinal));
        Assert.Contains(javascript.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo && edge.Evidence.ProviderVersion!.Contains("resolver2.relative-v1", StringComparison.Ordinal));
    }

    private static IReadOnlyList<EcmaScriptExportObservation> Observe(
        string language,
        string path,
        string source,
        out bool hasParseError) =>
        TreeSitterTestSupport.ExtractSyntax(language, path, source, out hasParseError).ExportObservations;

    private static string PreExportGraphFingerprint(Extraction extraction)
    {
        var json = JsonSerializer.Serialize(new
        {
            Nodes = extraction.Nodes.Where(node => node.Kind != CodeNodeKinds.Export).ToArray(),
            extraction.Declarations,
            Edges = extraction.Edges.Where(edge => edge.Kind != CodeEdgeKinds.Exports).ToArray()
        });
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string NormalizeLineEndings(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal);

}
