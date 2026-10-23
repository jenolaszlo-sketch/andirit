using System.Security.Cryptography;
using System.Text;

namespace Penghou.Hetu.TreeSitter.Tests;

// REVIEW-ONLY (EC5-GLM-Q1): adversarial probes for the EC-5 independent review.
// Not part of the production qualification suite; may be removed with the review branch.
public sealed class ReviewEc5GlmQ1VariableProbes
{
    // A1: local scopes never emit file-level variables; unrelated top-level facts survive.
    [Fact]
    public async Task Probe_LocalScopesNeverEmitFileVariablesAndSiblingsSurvive()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/locals.ts", """
                const topLevel = 1;
                function wrap() {
                  const fnLocal = 1;
                  let fnLet = 2;
                  var fnVar = 3;
                  {
                    const blockLocal = 4;
                  }
                }
                if (topLevel) {
                  let ifLocal = 5;
                }
                for (const forOf of []) {}
                for (let i = 0; i < 1; i++) {}
                while (false) { var whileVar = 6; }
                class Holder {
                  method() {
                    const methodLocal = 7;
                  }
                }
                """));

        var fields = extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field).ToArray();
        Assert.Equal(["topLevel"], fields.Select(node => node.Name));
        Assert.DoesNotContain(extracted.Nodes, node =>
            node.Name is "fnLocal" or "fnLet" or "fnVar" or "blockLocal" or "ifLocal" or
            "forOf" or "i" or "whileVar" or "methodLocal");
        Assert.DoesNotContain("typescript.parse-error", extracted.Result.WarningCodes);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Declares &&
            edge.TargetId == Assert.Single(fields).Id);
    }

    // A2: destructuring/pattern bindings are skipped; their initializer calls still attribute to file.
    [Fact]
    public async Task Probe_PatternBindingsSkippedButInitializerInvocationsRemain()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/patterns.ts", """
                const { name, age, ...rest } = readPerson();
                const { nested: { deep } } = outer();
                const { renamed = fallback() } = options;
                const [head, , tail = compute()] = items();
                """));

        Assert.DoesNotContain(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field);
        Assert.DoesNotContain(extracted.Nodes, node =>
            node.Name is "name" or "age" or "rest" or "deep" or "renamed" or "head" or "tail");
        var file = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.File);
        foreach (var invoked in new[] { "readPerson", "outer", "fallback", "compute", "items" })
        {
            var edge = Assert.Single(extracted.Edges, candidate =>
                candidate.Kind == CodeEdgeKinds.InvokesName &&
                extracted.Nodes.Single(node => node.Id == candidate.TargetId).Name == invoked);
            Assert.Equal(file.Id, edge.SourceId);
        }

        Assert.DoesNotContain(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls || edge.Kind == CodeEdgeKinds.References);
    }

    // A3/D4: anonymous default exports fabricate no variable.
    [Fact]
    public async Task Probe_AnonymousDefaultExportsProduceNoVariables()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/defaults.ts", """
                const site = process.env.SITE_URL ?? 'https://example.dev';
                const base = process.env.BASE_PATH ?? '/';
                export default defineConfig({ site, base, trailingSlash: 'ignore' });
                """),
            ("src/anonymous.ts", "export default function () { return 1; }\n"));

        Assert.Equal(
            ["base", "site"],
            extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field)
                .Select(node => node.Name).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Field && node.Name is "defineConfig" or "anonymous");
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.InvokesName &&
            extracted.Nodes.Single(node => node.Id == edge.TargetId).Name == "defineConfig");
        Assert.DoesNotContain("typescript.parse-error", extracted.Result.WarningCodes);
    }

    // C5: shadowing inside a callable does not merge identities or emit nested variables.
    [Fact]
    public async Task Probe_ShadowedNestedDeclarationEmitsOnlyFileLevelVariable()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/shadow.ts", """
                const value = 1;
                function example() {
                  const value = 2;
                  return value;
                }
                """));

        var field = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field);
        Assert.Equal("value", field.Name);
        var declaration = Assert.Single(extracted.Declarations, candidate => candidate.SymbolId == field.SymbolId);
        Assert.Equal(1, declaration.Location.StartLine);
        var callable = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Callable && node.Name == "example");
        var functionDeclaration = Assert.Single(
            extracted.Declarations, candidate => candidate.SymbolId == callable.SymbolId);
        Assert.Equal(2, functionDeclaration.Location.StartLine);
        Assert.Equal(2, extracted.Declarations.Count);
    }

    // A5: keyword variants, missing initializers, and ambient declarations.
    [Fact]
    public async Task Probe_KeywordVariantsMissingInitializersAndAmbientDeclarations()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/variants.ts", """
                var legacyNoInit;
                let openLet;
                declare const ambientConst: number;
                export declare const ambientExport: string;
                const brokenConst;
                """));

        var fields = extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field).ToArray();
        Assert.Equal(["legacyNoInit", "openLet"], fields.Select(node => node.Name).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(extracted.Nodes, node =>
            node.Name is "ambientConst" or "ambientExport" or "brokenConst");
    }

    // A6: namespace members are not file-level variables.
    [Fact]
    public async Task Probe_NamespaceMembersAreNotFileVariables()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/namespace.ts", """
                namespace Configuration {
                  export const inner = 1;
                  const hidden = 2;
                }
                const outer = 3;
                """));

        var fields = extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field).ToArray();
        Assert.Equal(["outer"], fields.Select(node => node.Name));
        Assert.DoesNotContain(extracted.Nodes, node => node.Name is "inner" or "hidden");
        Assert.DoesNotContain("typescript.parse-error", extracted.Result.WarningCodes);
    }

    // A7: malformed declarators do not suppress unrelated valid facts.
    [Fact]
    public async Task Probe_MalformedDeclaratorDoesNotSuppressSiblingFacts()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/malformed.ts", """
                const broken = ;
                const healthy = 1;
                class AfterError {}
                """));

        var fields = extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field).ToArray();
        Assert.Equal(["healthy"], fields.Select(node => node.Name));
        Assert.Contains(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.Name == "AfterError");
        Assert.Contains("typescript.parse-error", extracted.Result.WarningCodes);
    }

    // C-extra: identifier bounds and unicode names.
    [Fact]
    public async Task Probe_UnicodeNamesEmittedAndOverlongNamesSkipped()
    {
        const string longName = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/bounds.ts", $"const \u03c0 = 3;\nconst {longName}X = 1;\nconst \u03c0Second = 4;\n"));

        var fields = extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field).ToArray();
        Assert.Equal(["\u03c0", "\u03c0Second"], fields.Select(node => node.Name).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(fields, node => node.Name.Length > 256);
        Assert.Equal(2, fields.Select(node => node.Id).Distinct().Count());
    }

    // C-formula: node and symbol IDs match the documented SHA-256 formula (lowercase hex).
    [Fact]
    public async Task Probe_IdentityMatchesDocumentedSha256Formula()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/ids.ts", "export const config = {};\n"));

        var field = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field);
        var expected = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes("variable\nsrc/ids.ts\nconfig")));
        Assert.Equal($"typescript:symbol:{expected}", field.Id.Value);
        Assert.Equal($"typescript:{expected}", field.SymbolId!.Value);
        Assert.Equal("config", field.QualifiedName);
    }

    // C1: path normalization keeps backslash and slash inputs on one identity.
    [Fact]
    public async Task Probe_IdentityUsesForwardSlashNormalizedPaths()
    {
        var backslash = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src\\sub\\ids.ts", "export const config = {};\n"));
        var forward = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/sub/ids.ts", "export const config = {};\n"));

        var backslashField = Assert.Single(backslash.Nodes, node => node.Kind == CodeNodeKinds.Field);
        var forwardField = Assert.Single(forward.Nodes, node => node.Kind == CodeNodeKinds.Field);
        Assert.Equal(forwardField.Id, backslashField.Id);
        Assert.Equal(forwardField.SymbolId, backslashField.SymbolId);
        Assert.Equal("src/sub/ids.ts", backslash.Declarations.Single().Location.Path);
    }

    // C2/B: file variable, instance field, and static field with one name never converge.
    [Fact]
    public async Task Probe_FileVariableAndClassFieldsStayDistinct()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/kinds.ts", """
                export const configuration = {};
                let value = 1;
                class Service {
                  configuration = {};
                  static value = 2;
                  value2 = 3;
                }
                """));

        var fileConfiguration = Assert.Single(
            extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "configuration");
        var service = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.Name == "Service");
        var instanceConfiguration = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Service.configuration");
        var staticValue = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Service.value");
        var fileValue = Assert.Single(
            extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "value");
        Assert.NotEqual(fileConfiguration.Id, instanceConfiguration.Id);
        Assert.NotEqual(fileValue.Id, staticValue.Id);
        Assert.Equal(5, extracted.Nodes.Count(node =>
            node.SymbolId is not null &&
            node.Kind != CodeNodeKinds.File &&
            node.Kind != CodeNodeKinds.Type));

        // The class neighborhood exposes only the class members, never the file variables.
        var classDeclarationEdge = Assert.Single(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Contains && edge.TargetId == instanceConfiguration.Id);
        Assert.Equal(service.Id, classDeclarationEdge.SourceId);
        Assert.DoesNotContain(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Contains && edge.TargetId == fileConfiguration.Id && edge.SourceId == service.Id);
        var file = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.File);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Contains && edge.TargetId == fileConfiguration.Id && edge.SourceId == file.Id);
    }

    // C3: duplicate same-file declarations converge honestly.
    [Fact]
    public async Task Probe_DuplicateDeclarationsConvergeWithModifierIntersection()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/duplicates.mjs", """
                var item = 1;
                var item = 2;
                export var shared = 1;
                var shared = 2;
                """));

        var item = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "item");
        var itemDeclarations = extracted.Declarations
            .Where(declaration => declaration.SymbolId == item.SymbolId).ToArray();
        Assert.Equal(2, itemDeclarations.Length);
        Assert.Equal("var", Modifiers(item));
        Assert.All(itemDeclarations, declaration => Assert.Equal("var", Modifiers(declaration.Properties)));
        Assert.Equal(2, itemDeclarations.Select(declaration => declaration.Location.StartLine).Distinct().Count());

        var shared = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "shared");
        Assert.Equal("var", Modifiers(shared));
        var sharedDeclarations = extracted.Declarations
            .Where(declaration => declaration.SymbolId == shared.SymbolId).ToArray();
        Assert.Equal(2, sharedDeclarations.Length);
        Assert.Equal(
            ["export var", "var"],
            sharedDeclarations.Select(declaration => Modifiers(declaration.Properties)).Order(StringComparer.Ordinal));
        Assert.DoesNotContain("javascript.parse-error", extracted.Result.WarningCodes);
    }

    // C4: multi-declarator statements produce independent identities and spans.
    [Fact]
    public async Task Probe_MultiDeclaratorIdentitiesAndSpansAreIndependent()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/multi.ts", "const first = 1, second = 2;\n"));

        var first = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "first");
        var second = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "second");
        Assert.NotEqual(first.Id, second.Id);
        var firstDeclaration = Assert.Single(extracted.Declarations, declaration => declaration.SymbolId == first.SymbolId);
        var secondDeclaration = Assert.Single(extracted.Declarations, declaration => declaration.SymbolId == second.SymbolId);
        Assert.Equal((1, 7), (firstDeclaration.Location.StartLine, firstDeclaration.Location.StartColumn));
        Assert.Equal((1, 18), (secondDeclaration.Location.StartLine, secondDeclaration.Location.StartColumn));
        Assert.Equal(2, extracted.Edges.Count(edge => edge.Kind == CodeEdgeKinds.Declares));
        Assert.Equal(2, extracted.Edges.Count(edge => edge.Kind == CodeEdgeKinds.Contains));
    }

    // C6-extra: identities survive unrelated edits (declaration IDs are offset-free).
    [Fact]
    public async Task Probe_IdentityStableUnderUnrelatedEdits()
    {
        const string before = "const target = 1;\nfunction helper() {}\n";
        const string after = "const target = 1;\n\n// a new unrelated comment\nfunction helper() { return 2; }\n";
        var first = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), ("src/stable.ts", before));
        var second = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), ("src/stable.ts", after));

        var firstField = Assert.Single(first.Nodes, node => node.Kind == CodeNodeKinds.Field);
        var secondField = Assert.Single(second.Nodes, node => node.Kind == CodeNodeKinds.Field);
        Assert.Equal(firstField.Id, secondField.Id);
        Assert.Equal(
            first.Declarations.Single(declaration => declaration.SymbolId == firstField.SymbolId).Id,
            second.Declarations.Single(declaration => declaration.SymbolId == secondField.SymbolId).Id);
    }

    // D: only directly written export is recorded; export lists and re-exports fabricate nothing.
    [Fact]
    public async Task Probe_ExportMetadataRecordsOnlyDirectWrittenExport()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/exports.ts", """
                export const direct = 1;
                const local = 2;
                export { local };
                export { external } from "./other";
                export { direct as alias };
                const typed: Missing = 3;
                """));

        var direct = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "direct");
        Assert.Equal("const export", Modifiers(direct));
        var local = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "local");
        Assert.Equal("const", Modifiers(local));
        var typed = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "typed");
        Assert.Equal("const", Modifiers(typed));
        Assert.Single(extracted.Declarations, declaration => declaration.SymbolId == local.SymbolId);
        Assert.DoesNotContain(extracted.Nodes, node =>
            node.Name is "external" or "alias" or "Missing");
        Assert.Equal(["direct", "local", "typed"],
            extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field)
                .Select(node => node.Name).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(extracted.Edges, edge =>
            (edge.Kind == CodeEdgeKinds.Calls || edge.Kind == CodeEdgeKinds.References || edge.Kind == CodeEdgeKinds.Imports) &&
            extracted.Nodes.Any(node => node.Id == edge.TargetId && node.Name is "direct" or "local"));
        Assert.DoesNotContain("typescript.parse-error", extracted.Result.WarningCodes);
    }

    // D: export wrapper with multiple declarators emits each declarator exactly once.
    [Fact]
    public async Task Probe_ExportWrapperDoesNotDuplicateDeclarations()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/wrapper.ts", "export const alpha = 1, beta = 2;\n"));

        Assert.Equal(
            ["alpha", "beta"],
            extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field)
                .Select(node => node.Name).Order(StringComparer.Ordinal));
        Assert.Equal(2, extracted.Declarations.Count(declaration =>
            extracted.Nodes.Any(node => node.SymbolId == declaration.SymbolId && node.Kind == CodeNodeKinds.Field)));
        Assert.Equal(2, extracted.Edges.Count(edge => edge.Kind == CodeEdgeKinds.Declares));
        Assert.Equal(2, extracted.Edges.Count(edge => edge.Kind == CodeEdgeKinds.Contains));
        Assert.DoesNotContain("typescript.parse-error", extracted.Result.WarningCodes);
    }

    // E: initializer invocation attribution stays lexical and spans are exact.
    [Fact]
    public async Task Probe_InitializerInvocationAttributionRemainsLexical()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/initializers.ts", """
                export const client = createClient();
                export const handler = () => execute();
                const result = factory().configure();
                """));

        var file = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.File);
        var client = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "client");
        var handler = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Field && node.Name == "handler");
        var handlerCallable = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Callable && node.Name == "handler");

        var createClient = Assert.Single(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.InvokesName &&
            extracted.Nodes.Single(node => node.Id == edge.TargetId).Name == "createClient");
        Assert.Equal(file.Id, createClient.SourceId);
        Assert.Equal(1, createClient.Evidence.Location!.StartLine);
        Assert.Equal(23, createClient.Evidence.Location.StartColumn);

        var execute = Assert.Single(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.InvokesName &&
            extracted.Nodes.Single(node => node.Id == edge.TargetId).Name == "execute");
        Assert.Equal(handlerCallable.Id, execute.SourceId);
        Assert.NotEqual(handler.Id, execute.SourceId);

        var factory = Assert.Single(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.InvokesName &&
            extracted.Nodes.Single(node => node.Id == edge.TargetId).Name == "factory");
        Assert.Equal(file.Id, factory.SourceId);
        Assert.Equal(3, factory.Evidence.Location!.StartLine);

        // A call-result object is an unnameable dynamic target: the outer
        // member call is excluded by documented invocation coverage, while
        // the inner simple call stays attributed to the file.
        Assert.DoesNotContain(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.InvokesName &&
            extracted.Nodes.Single(node => node.Id == edge.TargetId).Name == "configure");
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Declares && edge.TargetId == client.Id &&
            edge.SourceId == file.Id);
        Assert.Equal(3, extracted.Edges.Count(edge => edge.Kind == CodeEdgeKinds.InvokesName));
        Assert.DoesNotContain(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls || edge.Kind == CodeEdgeKinds.References);
        Assert.All(extracted.Edges, edge => Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    // C-extra: source line endings do not change identities or spans.
    [Fact]
    public async Task Probe_SourceLineEndingsDoNotChangeFacts()
    {
        const string lf = "export const alpha = 1;\nlet beta = run();\n";
        const string crlf = "export const alpha = 1;\r\nlet beta = run();\r\n";
        var lfExtraction = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), ("src/eol.ts", lf));
        var crlfExtraction = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), ("src/eol.ts", crlf));

        Assert.Equal(
            lfExtraction.Nodes.Select(node => (node.Id, node.SymbolId, node.Kind, node.Name)),
            crlfExtraction.Nodes.Select(node => (node.Id, node.SymbolId, node.Kind, node.Name)));
        Assert.Equal(
            lfExtraction.Declarations.Select(declaration => (declaration.Id, declaration.Location.StartLine, declaration.Location.StartColumn, declaration.Location.EndLine, declaration.Location.EndColumn)),
            crlfExtraction.Declarations.Select(declaration => (declaration.Id, declaration.Location.StartLine, declaration.Location.StartColumn, declaration.Location.EndLine, declaration.Location.EndColumn)));
        Assert.Equal(
            lfExtraction.Edges.Select(edge => (edge.Id, edge.Kind, edge.SourceId, edge.TargetId)),
            crlfExtraction.Edges.Select(edge => (edge.Id, edge.Kind, edge.SourceId, edge.TargetId)));
        Assert.Equal(2, crlfExtraction.Nodes.Count(node => node.Kind == CodeNodeKinds.Field));
    }

    private static string? Modifiers(CodeGraphNode node) =>
        node.Properties.TryGetValue(CodePropertyKeys.Modifiers, out var value)
            ? ((CodeTextProperty)value).Value
            : null;

    private static string? Modifiers(IReadOnlyDictionary<string, CodePropertyValue> properties) =>
        properties.TryGetValue(CodePropertyKeys.Modifiers, out var value)
            ? ((CodeTextProperty)value).Value
            : null;
}
