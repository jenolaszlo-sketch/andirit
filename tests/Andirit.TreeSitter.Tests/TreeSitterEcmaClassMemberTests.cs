using System.Text.Json;

namespace Andirit.TreeSitter.Tests;

/// <summary>
/// EC-3B extraction qualification: ordinary TypeScript and JavaScript class
/// fields become <c>property</c> members with owner-derived identity, exact
/// declarations, containment, and written modifier metadata. Unsupported
/// member shapes emit no guessed facts.
/// </summary>
public sealed class TreeSitterEcmaClassMemberTests
{
    private const string TypeScriptSettingsSource = """
        export class Settings {
          static defaults = 1;
          public readonly name: string = "n";
          protected retries?: number;
          private secret!: string;
          counter = createCounter();
          handler = () => execute();
        }
        """;

    private const string JavaScriptStoreSource = """
        export class Store {
          static items = [];
          count = 0;
          pending;
        }
        """;

    private const string CollisionSource = """
        class Collision {
          value = 1;
          value(): number {
            return 1;
          }
        }
        """;

    private const string InitializerSource = """
        class Service {
          client = createClient();
          handler = () => execute();
        }
        """;

    private const string UnsupportedFieldSource = """
        class UnsupportedFields {
          [computed] = 1;
          "literal" = 2;
          123 = 3;
          #hidden = 4;
          accessor auto = 5;
          get value(): number {
            return 1;
          }
        }
        """;

    private const string BrokenFieldSource = """
        class BrokenFields {
          ok = 1;
          bad = (
        }
        """;

    [Fact]
    public async Task ExtractAsync_ModelsTypeScriptClassFieldsWithContainmentAndLocations()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/settings.ts", TypeScriptSettingsSource));

        var settings = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Settings");

        var names = new[]
        {
            "Settings.defaults",
            "Settings.name",
            "Settings.retries",
            "Settings.secret",
            "Settings.counter",
            "Settings.handler"
        };
        foreach (var qualifiedName in names)
        {
            var member = Assert.Single(
                extracted.Nodes,
                node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == qualifiedName);
            Assert.NotNull(member.SymbolId);
            Assert.Equal(
                "typescript",
                ((CodeTextProperty)member.Properties[CodePropertyKeys.Language]).Value);
            Assert.Contains(
                extracted.Edges,
                edge => edge.Kind == CodeEdgeKinds.Contains &&
                    edge.SourceId == settings.Id &&
                    edge.TargetId == member.Id);
            Assert.Contains(
                extracted.Edges,
                edge => edge.Kind == CodeEdgeKinds.Declares &&
                    edge.TargetId == member.Id);
        }

        // The declaration covers the field with an accurate location; the
        // containment edge keeps the precise name span.
        var counter = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Settings.counter");
        var counterDeclaration = Assert.Single(
            extracted.Declarations,
            declaration => declaration.SymbolId == counter.SymbolId);
        Assert.Equal("src/settings.ts", counterDeclaration.Location.Path);
        Assert.Equal(6, counterDeclaration.Location.StartLine);
        Assert.Equal(3, counterDeclaration.Location.StartColumn);

        var counterEdge = Assert.Single(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == settings.Id &&
                edge.TargetId == counter.Id);
        Assert.Equal(6, counterEdge.Evidence.Location!.StartLine);
        Assert.Equal(3, counterEdge.Evidence.Location.StartColumn);
        Assert.Equal(10, counterEdge.Evidence.Location.EndColumn);
        Assert.Equal(EvidenceQuality.Structural, counterEdge.Evidence.Quality);
        Assert.DoesNotContain(extracted.Result.WarningCodes, code => code == "typescript.parse-error");
    }

    [Fact]
    public async Task ExtractAsync_RecordsTypeScriptFieldModifiersAndAccess()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/settings.ts", TypeScriptSettingsSource));

        var defaults = Property(extracted, "Settings.defaults");
        var name = Property(extracted, "Settings.name");
        var retries = Property(extracted, "Settings.retries");
        var secret = Property(extracted, "Settings.secret");
        var counter = Property(extracted, "Settings.counter");

        Assert.Equal("static", Modifiers(defaults));
        Assert.Null(Access(defaults));
        Assert.Equal("readonly", Modifiers(name));
        Assert.Equal("public", Access(name));
        Assert.Equal("optional", Modifiers(retries));
        Assert.Equal("protected", Access(retries));
        Assert.Null(Modifiers(secret));
        Assert.Equal("private", Access(secret));
        Assert.Null(Modifiers(counter));
        Assert.Null(Access(counter));
    }

    [Fact]
    public async Task ExtractAsync_ModelsJavaScriptClassFields()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/store.js", JavaScriptStoreSource));

        var store = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Store");
        var items = Property(extracted, "Store.items");
        var count = Property(extracted, "Store.count");
        var pending = Property(extracted, "Store.pending");

        Assert.Equal("static", Modifiers(items));
        Assert.Null(Modifiers(count));
        Assert.Null(Modifiers(pending));
        Assert.Null(Access(items));

        foreach (var member in new[] { items, count, pending })
        {
            Assert.Equal(
                "javascript",
                ((CodeTextProperty)member.Properties[CodePropertyKeys.Language]).Value);
            Assert.Contains(
                extracted.Edges,
                edge => edge.Kind == CodeEdgeKinds.Contains &&
                    edge.SourceId == store.Id &&
                    edge.TargetId == member.Id);
        }

        // A field with no initializer emits no invocation facts.
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.InvokesName);
        Assert.DoesNotContain("javascript.parse-error", extracted.Result.WarningCodes);
    }

    [Fact]
    public async Task ExtractAsync_KeepsFieldAndMethodWithTheSameNameDistinct()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/collision.ts", CollisionSource));

        var memberKinds = extracted.Nodes
            .Where(node => node.QualifiedName == "Collision.value" && node.SymbolId is not null)
            .Select(node => node.Kind)
            .ToArray();
        var field = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Collision.value");
        var method = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Collision.value");

        Assert.Equal(2, memberKinds.Length);
        Assert.NotEqual(field.Id, method.Id);
        Assert.NotEqual(field.SymbolId, method.SymbolId);
    }

    [Fact]
    public async Task ExtractAsync_AttributesInitializerInvocationsToTheField()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/service.ts", InitializerSource));

        var service = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Service");
        var client = Property(extracted, "Service.client");
        var handler = Property(extracted, "Service.handler");
        var invocations = extracted.Edges
            .Where(edge => edge.Kind == CodeEdgeKinds.InvokesName)
            .ToArray();

        Assert.Equal(2, invocations.Length);
        Assert.DoesNotContain(invocations, edge => edge.SourceId == service.Id);
        Assert.Equal(
            "createClient",
            InvokedName(extracted, Assert.Single(invocations, edge => edge.SourceId == client.Id)));
        Assert.Equal(
            "execute",
            InvokedName(extracted, Assert.Single(invocations, edge => edge.SourceId == handler.Id)));
        Assert.All(invocations, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Calls);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.References);
    }

    [Fact]
    public async Task ExtractAsync_SkipsComputedPrivateAndAccessorFields()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/unsupported-fields.ts", UnsupportedFieldSource));

        var unsupported = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "UnsupportedFields");

        // Computed, string, numeric, #private, and auto-accessor fields are
        // unsupported: they emit no nodes, declarations, or containment.
        Assert.DoesNotContain(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Property &&
            node.QualifiedName is not null &&
            node.QualifiedName.StartsWith("UnsupportedFields.", StringComparison.Ordinal));

        // The getter remains a callable member, never an ordinary field, and
        // it is the only member the class contains.
        var getter = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable &&
                node.QualifiedName == "UnsupportedFields.value");
        var contained = extracted.Edges
            .Where(edge => edge.Kind == CodeEdgeKinds.Contains && edge.SourceId == unsupported.Id)
            .Select(edge => Assert.Single(extracted.Nodes, node => node.Id == edge.TargetId))
            .ToArray();
        Assert.Equal(getter.Id, Assert.Single(contained).Id);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.InvokesName);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Calls);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.References);
        Assert.DoesNotContain("typescript.parse-error", extracted.Result.WarningCodes);
    }

    [Fact]
    public async Task ExtractAsync_PreservesValidClassFactsFromPartialFiles()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/broken-fields.ts", BrokenFieldSource));

        Assert.Contains("typescript.parse-error", extracted.Result.WarningCodes);
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "BrokenFields");
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "BrokenFields.ok");

        // The partially recovered field emits no fabricated member.
        Assert.DoesNotContain(
            extracted.Nodes,
            node => node.QualifiedName == "BrokenFields.bad");
        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    [Fact]
    public async Task ExtractAsync_RepeatsClassMemberFactsDeterministically()
    {
        var first = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/settings.ts", TypeScriptSettingsSource),
            ("src/collision.ts", CollisionSource),
            ("src/broken-fields.ts", BrokenFieldSource));
        var second = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/broken-fields.ts", BrokenFieldSource),
            ("src/collision.ts", CollisionSource),
            ("src/settings.ts", TypeScriptSettingsSource));

        Assert.Equal(JsonSerializer.Serialize(first.Nodes), JsonSerializer.Serialize(second.Nodes));
        Assert.Equal(JsonSerializer.Serialize(first.Declarations), JsonSerializer.Serialize(second.Declarations));
        Assert.Equal(JsonSerializer.Serialize(first.Edges), JsonSerializer.Serialize(second.Edges));
    }

    [Fact]
    public async Task IndexAsync_AnswersExactClassMemberAndEnclosingSymbolQueries()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ts-members-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            await File.WriteAllTextAsync(Path.Combine(src.FullName, "settings.ts"), """
                export class Settings {
                  counter = createCounter();
                }
                """);
            var repositoryId = new CodeRepositoryId("repo:ts-members");
            var store = new InMemoryCodeGraphStore();
            var indexing = new CodeIndexingService(
                new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
                new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
                store);
            var result = await indexing.IndexAsync(
                new CodeRepositoryDescriptor(repositoryId, directory.FullName),
                new CodeIndexRunId("run:ts-members"));
            Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);

            var queries = new CodeGraphQueryService(store);
            var settings = Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Settings")).Candidates,
                node => node.SymbolId is not null);
            var counter = Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Settings.counter")).Candidates);
            Assert.Equal(CodeNodeKinds.Type, settings.Kind);
            Assert.Equal(CodeNodeKinds.Property, counter.Kind);

            var declarations = await queries.FindDeclarationsAsync(repositoryId, counter.SymbolId!);
            var declaration = Assert.Single(declarations);
            Assert.Equal("src/settings.ts", declaration.Location.Path);
            Assert.Equal(2, declaration.Location.StartLine);
            Assert.Equal(3, declaration.Location.StartColumn);

            // A position inside the initializer resolves to the field, not to
            // the class or a fabricated anonymous owner.
            var enclosing = await queries.FindEnclosingSymbolAsync(
                repositoryId,
                "src/settings.ts",
                2,
                20);
            Assert.NotNull(enclosing);
            Assert.Equal("Settings.counter", enclosing.Symbol.QualifiedName);

            var view = (await queries.OpenLatestPublicationAsync(repositoryId))!;
            var neighborhood = await view.GetNeighborhoodAsync(
                settings.Id,
                options: new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 200));
            Assert.Contains(
                neighborhood.Result.Nodes,
                node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Settings.counter");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static CodeGraphNode Property(Extraction extracted, string qualifiedName) =>
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == qualifiedName);

    private static string InvokedName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.TargetId).Name;

    private static string? Modifiers(CodeGraphNode node) =>
        node.Properties.TryGetValue(CodePropertyKeys.Modifiers, out var value)
            ? ((CodeTextProperty)value).Value
            : null;

    private static string? Access(CodeGraphNode node) =>
        node.Properties.TryGetValue(CodePropertyKeys.Access, out var value)
            ? ((CodeTextProperty)value).Value
            : null;
}
