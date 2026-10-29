namespace Andirit.TreeSitter.Tests;

/// <summary>EC-5 fixtures for file-level TypeScript and JavaScript variables.</summary>
public sealed class TreeSitterEcmaVariableTests
{
    [Fact]
    public async Task ExtractAsync_ModelsTypeScriptFileVariablesWithWrittenModifiersAndLocations()
    {
        const string source = """
            const projectName = "Andirit";
            let status = "draft";
            var legacyFlag = true;
            export const projects: Project[] = [];
            export let currentProject: Project | null = null;
            export var compatibilityMode = true;
            const first = 1, second = 2;
            const client = createClient();
            export const handler = () => createHandler();
            class Registry {}
            interface Settings {}
            function initialize() {}
            """;

        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/data.ts", source));

        var variables = extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field).ToArray();
        Assert.Equal(
            [
                "client", "compatibilityMode", "currentProject", "first", "handler",
                "legacyFlag", "projectName", "projects", "second", "status"
            ],
            variables.Select(node => node.Name).Order(StringComparer.Ordinal));
        Assert.Equal(variables.Length, variables.Select(node => node.Id).Distinct().Count());
        Assert.All(variables, node => Assert.NotNull(node.SymbolId));

        Assert.Equal("const", Modifiers(Variable(variables, "projectName")));
        Assert.Equal("let", Modifiers(Variable(variables, "status")));
        Assert.Equal("var", Modifiers(Variable(variables, "legacyFlag")));
        Assert.Equal("const export", Modifiers(Variable(variables, "projects")));
        Assert.Equal("export let", Modifiers(Variable(variables, "currentProject")));
        Assert.Equal("export var", Modifiers(Variable(variables, "compatibilityMode")));

        var projects = Variable(variables, "projects");
        var projectsDeclaration = Assert.Single(
            extracted.Declarations,
            declaration => declaration.SymbolId == projects.SymbolId);
        Assert.Equal("src/data.ts", projectsDeclaration.Location.Path);
        Assert.Equal(4, projectsDeclaration.Location.StartLine);

        const string projectsDeclarator = "projects: Project[] = []";
        var projectsDeclaratorColumn =
            source.Split('\n')[3].IndexOf(projectsDeclarator, StringComparison.Ordinal) + 1;
        Assert.Equal(projectsDeclaratorColumn, projectsDeclaration.Location.StartColumn);
        Assert.Equal(
            projectsDeclaratorColumn + projectsDeclarator.Length,
            projectsDeclaration.Location.EndColumn);

        var first = Variable(variables, "first");
        var second = Variable(variables, "second");
        var declarators = extracted.Declarations
            .Where(declaration => declaration.SymbolId == first.SymbolId || declaration.SymbolId == second.SymbolId)
            .ToArray();
        Assert.Equal(2, declarators.Length);
        Assert.Equal(2, declarators.Select(declaration => declaration.Location.StartColumn).Distinct().Count());

        var file = Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.File);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Contains && edge.SourceId == file.Id && edge.TargetId == projects.Id);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Declares && edge.SourceId == file.Id && edge.TargetId == projects.Id);

        // Plain initializers retain lexical file attribution, and arrow-valued
        // variables retain the existing callable projection as a separate fact.
        var invocation = Assert.Single(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.InvokesName && InvokedName(extracted, edge) == "createClient");
        Assert.Equal(file.Id, invocation.SourceId);
        Assert.NotEqual(Variable(variables, "client").Id, invocation.SourceId);
        Assert.Single(extracted.Nodes, node => node.Kind == CodeNodeKinds.Callable && node.Name == "handler");

        Assert.Contains(extracted.Nodes, node => node.Kind == CodeNodeKinds.Type && node.Name == "Registry");
        Assert.Contains(extracted.Nodes, node => node.Kind == CodeNodeKinds.Interface && node.Name == "Settings");
        Assert.Contains(extracted.Nodes, node => node.Kind == CodeNodeKinds.Callable && node.Name == "initialize");
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Calls);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.References);
        Assert.All(extracted.Edges, edge => Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    [Fact]
    public async Task ExtractAsync_ModelsJavaScriptVariablesThroughMjsPipeline()
    {
        Assert.True(new TreeSitterJavaScriptPlugin().CanHandle("src/data.mjs"));
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/data.mjs", """
                export const projects = [];
                export let currentProject = null;
                export var compatibilityMode = true;
                const first = 1, second = 2;
                """));

        var variables = extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field).ToArray();
        Assert.Equal(5, variables.Length);
        Assert.Equal("const export", Modifiers(Variable(variables, "projects")));
        Assert.Equal("export let", Modifiers(Variable(variables, "currentProject")));
        Assert.Equal("export var", Modifiers(Variable(variables, "compatibilityMode")));
        Assert.Equal("const", Modifiers(Variable(variables, "first")));
        Assert.Equal("const", Modifiers(Variable(variables, "second")));
        Assert.DoesNotContain("javascript.parse-error", extracted.Result.WarningCodes);
    }

    [Fact]
    public async Task ExtractAsync_UsesEachSupportedTypeScriptExtensionAndTsxGrammar()
    {
        var plugin = new TreeSitterTypeScriptPlugin();
        foreach (var extension in new[] { ".ts", ".mts", ".cts", ".tsx" })
            Assert.True(plugin.CanHandle("src/data" + extension));

        var extracted = await TreeSitterTestSupport.ExtractAsync(
            plugin,
            ("src/module.mts", "export const moduleData = [];"),
            ("src/common.cts", "export let commonData = null;"),
            ("src/component.tsx", "export const componentData: string = 'tsx';"));

        Assert.Equal(3, extracted.Nodes.Count(node => node.Kind == CodeNodeKinds.Field));
        Assert.Equal("const export", Modifiers(Variable(extracted.Nodes, "moduleData")));
        Assert.Equal("export let", Modifiers(Variable(extracted.Nodes, "commonData")));
        Assert.Equal("const export", Modifiers(Variable(extracted.Nodes, "componentData")));
        Assert.DoesNotContain("typescript.parse-error", extracted.Result.WarningCodes);
    }

    [Fact]
    public async Task ExtractAsync_KeepsVariableIdentitiesFileScopedKindScopedAndDeterministic()
    {
        const string repeated = "const repeated = 1;\nexport let repeated = 2;\nconst shared = 3;\nfunction shared() {}";
        var first = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/one.ts", repeated),
            ("src/two.ts", "const repeated = 4;"));
        var second = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/one.ts", repeated),
            ("src/two.ts", "const repeated = 4;"));

        var repeatedVariables = first.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Field && node.Name == "repeated")
            .ToArray();
        Assert.Equal(2, repeatedVariables.Length);
        Assert.Equal(2, repeatedVariables.Select(node => node.Id).Distinct().Count());

        var sameFileRepeated = Assert.Single(repeatedVariables, node =>
            first.Declarations.Count(declaration => declaration.SymbolId == node.SymbolId) == 2);
        var repeatedDeclarations = first.Declarations
            .Where(declaration => declaration.SymbolId == sameFileRepeated.SymbolId)
            .ToArray();
        Assert.Equal(2, repeatedDeclarations.Length);
        Assert.Null(Modifiers(sameFileRepeated));
        Assert.Equal(
            ["const", "export let"],
            repeatedDeclarations
                .Select(declaration => Modifiers(declaration.Properties))
                .Order(StringComparer.Ordinal));

        var sameNameDifferentKinds = first.Nodes
            .Where(node => node.Name == "shared" && node.SymbolId is not null)
            .ToArray();
        Assert.Equal(2, sameNameDifferentKinds.Length);
        Assert.Equal(2, sameNameDifferentKinds.Select(node => node.Id).Distinct().Count());
        Assert.Contains(sameNameDifferentKinds, node => node.Kind == CodeNodeKinds.Field);
        Assert.Contains(sameNameDifferentKinds, node => node.Kind == CodeNodeKinds.Callable);

        Assert.Equal(first.Nodes.Select(node => node.Id), second.Nodes.Select(node => node.Id));
        Assert.Equal(first.Declarations.Select(declaration => declaration.Id), second.Declarations.Select(declaration => declaration.Id));
        Assert.Equal(first.Edges.Select(edge => edge.Id), second.Edges.Select(edge => edge.Id));
    }

    [Fact]
    public async Task ExtractAsync_SkipsLocalDestructuredAndMalformedBindings()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/boundaries.ts", """
                const { name, age } = person;
                const [first, second] = values;
                export default createProject();
                export { local };
                function example() {
                  const local = 10;
                  {
                    let blockLocal = 20;
                  }
                }
                if (true) {
                  let nested = 1;
                }
                for (const item of []) {}
                const stable = 1;
                const broken = ;
                """));

        var variables = extracted.Nodes.Where(node => node.Kind == CodeNodeKinds.Field).ToArray();
        Assert.Equal(["stable"], variables.Select(node => node.Name));
        Assert.DoesNotContain(extracted.Nodes, node => node.SymbolId is not null &&
            (node.Name is "name" or "age" or "first" or "second" or "local" or "blockLocal" or "nested" or "item" or "broken"));
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind is { } kind &&
            (kind == CodeEdgeKinds.Calls || kind == CodeEdgeKinds.References));
        Assert.Contains("typescript.parse-error", extracted.Result.WarningCodes);
    }

    [Fact]
    public async Task ExtractAsync_RetainsJavaScriptDuplicateVariableOccurrences()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/repeated.mjs", "let repeated = 1;\nexport let repeated = 2;"));

        var repeated = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Field && node.Name == "repeated");
        Assert.Equal("let", Modifiers(repeated));
        var declarations = extracted.Declarations
            .Where(declaration => declaration.SymbolId == repeated.SymbolId)
            .ToArray();
        Assert.Equal(2, declarations.Length);
        Assert.Equal(
            ["export let", "let"],
            declarations
                .Select(declaration => Modifiers(declaration.Properties))
                .Order(StringComparer.Ordinal));
        Assert.Equal(2, extracted.Edges.Count(edge =>
            edge.Kind == CodeEdgeKinds.Declares && edge.TargetId == repeated.Id));
        Assert.DoesNotContain("javascript.parse-error", extracted.Result.WarningCodes);
    }

    private static CodeGraphNode Variable(IReadOnlyList<CodeGraphNode> variables, string name) =>
        Assert.Single(variables, node => node.Kind == CodeNodeKinds.Field && node.Name == name);

    private static string? Modifiers(CodeGraphNode node) =>
        node.Properties.TryGetValue(CodePropertyKeys.Modifiers, out var value)
            ? ((CodeTextProperty)value).Value
            : null;

    private static string? Modifiers(IReadOnlyDictionary<string, CodePropertyValue> properties) =>
        properties.TryGetValue(CodePropertyKeys.Modifiers, out var value)
            ? ((CodeTextProperty)value).Value
            : null;

    private static string InvokedName(Extraction extraction, CodeGraphEdge edge) =>
        extraction.Nodes.Single(node => node.Id == edge.TargetId).Name;
}
