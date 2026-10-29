namespace Penghou.Andirit.TreeSitter.Tests;

/// <summary>
/// EC-3A identity qualification: classes and their members must never
/// silently converge when they belong to unrelated owners. Identities are
/// path-scoped for class declarations and owner-derived for class members;
/// see docs/ecmascript-symbol-identity.md.
/// </summary>
public sealed class TreeSitterEcmaClassIdentityTests
{
    [Fact]
    public async Task ExtractAsync_KeepsSameNameClassesAndMethodsDistinctAcrossTypeScriptModules()
    {
        const string module = """
            export class Service {
              process(): number {
                return 1;
              }
            }
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/inventory.ts", module),
            ("src/billing.ts", module));

        AssertSameNameClassesStayDistinct(extracted, "Service", "process");
    }

    [Fact]
    public async Task ExtractAsync_KeepsSameNameClassesAndMethodsDistinctAcrossJavaScriptModules()
    {
        const string module = """
            export class Service {
              process() {
                return 1;
              }
            }
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/inventory.js", module),
            ("src/billing.js", module));

        AssertSameNameClassesStayDistinct(extracted, "Service", "process");
    }

    [Fact]
    public async Task ExtractAsync_KeepsSameNameMembersOnUnrelatedClassesDistinct()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/io.ts", """
                class Reader {
                  read(): string {
                    return "r";
                  }
                }
                class Writer {
                  read(): string {
                    return "w";
                  }
                }
                """));

        var reader = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Reader");
        var writer = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Writer");
        var readerRead = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Reader.read");
        var writerRead = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Writer.read");

        Assert.NotEqual(readerRead.Id, writerRead.Id);
        Assert.NotEqual(readerRead.SymbolId, writerRead.SymbolId);
        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == reader.Id &&
                edge.TargetId == readerRead.Id);
        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == writer.Id &&
                edge.TargetId == writerRead.Id);
    }

    [Fact]
    public async Task ExtractAsync_KeepsStaticAndInstanceMethodsWithTheSameNameDistinct()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/counter.ts", """
                class Counter {
                  static create(): number {
                    return 0;
                  }
                  create(): number {
                    return 1;
                  }
                }
                """));

        var counter = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Counter");
        // Static and instance members share a display name but never an
        // identity: querying the name must return both candidates.
        var members = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Counter.create")
            .ToArray();

        Assert.Equal(2, members.Length);
        Assert.Equal(2, members.Select(node => node.Id).Distinct().Count());
        Assert.All(members, member => Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == counter.Id &&
                edge.TargetId == member.Id));
    }

    [Fact]
    public async Task ExtractAsync_KeepsMethodsAndAccessorsWithTheSameNameDistinct()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/holder.ts", """
                class Holder {
                  value(): number {
                    return 1;
                  }
                  get value(): number {
                    return 2;
                  }
                }
                """));

        var members = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Holder.value")
            .ToArray();

        Assert.Equal(2, members.Length);
        Assert.Equal(2, members.Select(node => node.Id).Distinct().Count());
        Assert.DoesNotContain(extracted.Result.WarningCodes, code => code == "typescript.parse-error");
    }

    [Fact]
    public async Task ExtractAsync_KeepsJavaScriptStaticAccessorsAndInstanceMethodsDistinct()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/store.js", """
                export class Store {
                  static get instance() {
                    return 1;
                  }
                  instance() {
                    return 2;
                  }
                }
                """));

        var members = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Store.instance")
            .ToArray();

        Assert.Equal(2, members.Length);
        Assert.Equal(2, members.Select(node => node.Id).Distinct().Count());
        Assert.DoesNotContain(extracted.Result.WarningCodes, code => code == "javascript.parse-error");
    }

    [Fact]
    public async Task ExtractAsync_HandlesJavaScriptCombinedStaticGetToken()
    {
        // The JavaScript grammar lexes `static get` before a newline as one
        // token; it still names a static accessor member.
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/combined.js", """
                export class Store {
                  static get
                  instance() {
                    return 1;
                  }
                  instance() {
                    return 2;
                  }
                }
                """));

        Assert.DoesNotContain("javascript.parse-error", extracted.Result.WarningCodes);
        var members = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Store.instance")
            .ToArray();
        Assert.Equal(2, members.Length);
        Assert.Equal(2, members.Select(node => node.Id).Distinct().Count());
    }

    [Fact]
    public async Task ExtractAsync_KeepsSameNameClassAndInterfaceDistinct()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/service.ts", """
                class Service {
                  process(): number {
                    return 1;
                  }
                }
                interface Service {
                  process(): number;
                }
                """));

        var serviceClass = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Service");
        var serviceInterface = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Interface && node.QualifiedName == "Service");

        Assert.NotEqual(serviceClass.Id, serviceInterface.Id);

        // The class method and the interface signature are different member
        // kinds under different owners, even with identical display names.
        var members = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Callable && node.Name == "process")
            .ToArray();
        Assert.Equal(2, members.Length);
        Assert.Equal(2, members.Select(node => node.Id).Distinct().Count());

        var classMethod = Assert.Single(members, member => extracted.Edges.Any(
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == serviceClass.Id &&
                edge.TargetId == member.Id));
        var interfaceMethod = Assert.Single(members, member => extracted.Edges.Any(
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == serviceInterface.Id &&
                edge.TargetId == member.Id));
        Assert.NotEqual(classMethod.Id, interfaceMethod.Id);
    }

    [Fact]
    public async Task ExtractAsync_NestedClassDeclarationsRetainTheirOwnerAndIdentity()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/nested.ts", """
                class Inner {
                  run(): number {
                    return 0;
                  }
                }
                class Outer {
                  build(): unknown {
                    class Inner {
                      run(): number {
                        return 1;
                      }
                    }
                    return Inner;
                  }
                }
                """));

        var inners = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Type && node.Name == "Inner")
            .ToArray();
        Assert.Equal(2, inners.Length);
        Assert.Equal(2, inners.Select(node => node.Id).Distinct().Count());

        var nestedInner = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Outer.build.Inner");
        var build = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Outer.build");
        var nestedRun = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable &&
                node.QualifiedName == "Outer.build.Inner.run");

        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == build.Id &&
                edge.TargetId == nestedInner.Id);
        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == nestedInner.Id &&
                edge.TargetId == nestedRun.Id);
    }

    [Fact]
    public async Task ExtractAsync_ConvergesDuplicateClassDeclarationsInOneFileWithSeparateDeclarations()
    {
        // Structural extraction cannot verify same-file declaration merging or
        // its absence: duplicates converge by name and keep one declaration
        // per physical block. This pins the documented limitation.
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/duplicate.ts", """
                class Duplicate {
                  run(): number {
                    return 1;
                  }
                }
                class Duplicate {
                  run(): number {
                    return 2;
                  }
                }
                """));

        var duplicate = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Duplicate");
        var classDeclarations = extracted.Declarations
            .Where(declaration => declaration.SymbolId == duplicate.SymbolId)
            .ToArray();
        Assert.Equal(2, classDeclarations.Length);
        Assert.Equal(2, classDeclarations.Select(declaration => declaration.Location.StartLine).Distinct().Count());

        var run = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Duplicate.run");
        var runDeclarations = extracted.Declarations
            .Where(declaration => declaration.SymbolId == run.SymbolId)
            .ToArray();
        Assert.Equal(2, runDeclarations.Length);
    }

    [Fact]
    public async Task ExtractAsync_KeepsClassIdentityStableAcrossUnrelatedEdits()
    {
        var before = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/target.ts", """
                class Target {
                  run(): number {
                    return 1;
                  }
                }
                """));
        var after = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/target.ts", """
                // An unrelated comment and declaration must not move identities.
                class Unrelated {
                  other(): number {
                    return 0;
                  }
                }
                class Target {
                  run(): number {
                    return 1;
                  }
                }
                """));

        var targetBefore = Assert.Single(
            before.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Target");
        var targetAfter = Assert.Single(
            after.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Target");
        Assert.Equal(targetBefore.Id, targetAfter.Id);
        Assert.Equal(targetBefore.SymbolId, targetAfter.SymbolId);

        var runBefore = Assert.Single(
            before.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Target.run");
        var runAfter = Assert.Single(
            after.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Target.run");
        Assert.Equal(runBefore.Id, runAfter.Id);
        Assert.Equal(runBefore.SymbolId, runAfter.SymbolId);
    }

    [Fact]
    public async Task ExtractAsync_HeritageTargetStaysDeclarationlessBesideSameNameClass()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/heritage.ts", """
                class Child extends Base {}
                class Base {}
                """));

        var baseClass = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type &&
                node.QualifiedName == "Base" &&
                node.SymbolId is not null);
        var inherits = Assert.Single(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Inherits);
        var target = Assert.Single(extracted.Nodes, node => node.Id == inherits.TargetId);

        Assert.Equal("Base", target.Name);
        Assert.Equal("Base", target.QualifiedName);
        Assert.Null(target.SymbolId);
        Assert.NotEqual(baseClass.Id, target.Id);
        Assert.DoesNotContain(extracted.Declarations, declaration => declaration.SymbolNodeId == target.Id);
        Assert.Equal(EvidenceQuality.Structural, inherits.Evidence.Quality);
    }

    [Fact]
    public async Task ExtractAsync_ShadowedHeritageTargetDoesNotConvergeWithOuterClass()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/heritage-shadow.ts", """
                class Base {}
                function create() {
                  class Base {}
                  class Derived extends Base {}
                  return Derived;
                }
                """));

        var topLevelBase = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type &&
                node.QualifiedName == "Base" &&
                node.SymbolId is not null);
        var nestedBase = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type &&
                node.QualifiedName == "create.Base" &&
                node.SymbolId is not null);
        var derived = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type &&
                node.QualifiedName == "create.Derived" &&
                node.SymbolId is not null);
        var inherits = Assert.Single(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Inherits);
        var target = Assert.Single(extracted.Nodes, node => node.Id == inherits.TargetId);

        Assert.Equal(derived.Id, inherits.SourceId);
        Assert.Equal("Base", target.Name);
        Assert.Equal("Base", target.QualifiedName);
        Assert.Null(target.SymbolId);
        Assert.NotEqual(topLevelBase.Id, target.Id);
        Assert.NotEqual(nestedBase.Id, target.Id);
        Assert.DoesNotContain(extracted.Declarations, declaration => declaration.SymbolNodeId == target.Id);
        Assert.Equal(EvidenceQuality.Structural, inherits.Evidence.Quality);
    }

    [Fact]
    public async Task ExtractAsync_HeritagePlaceholdersStayFileScoped()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/a.ts", "class Child extends Base {}"),
            ("src/b.ts", "class Child extends Base {}"));

        var placeholders = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Type && node.Name == "Base")
            .ToArray();
        Assert.Equal(2, placeholders.Length);
        Assert.Equal(2, placeholders.Select(node => node.Id).Distinct().Count());
        Assert.All(placeholders, node =>
            Assert.StartsWith("typescript:heritage-target:", node.Id.Value, StringComparison.Ordinal));
        Assert.All(placeholders, node => Assert.Null(node.SymbolId));

        var inherits = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Inherits).ToArray();
        Assert.Equal(2, inherits.Length);
        Assert.All(inherits, edge => Assert.Contains(
            placeholders,
            placeholder => placeholder.Id == edge.TargetId));
    }

    [Fact]
    public async Task ExtractAsync_ImplementsTargetStaysDeclarationLess()
    {
        // `implements Shape` remains a declaration-less structural target:
        // matching the interface declaration by name would overstate syntax
        // as semantic binding.
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/widget.ts", """
                interface Shape {
                  area(): number;
                }
                class Widget implements Shape {
                  area(): number {
                    return 1;
                  }
                }
                """));

        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Interface && node.QualifiedName == "Shape");
        var implements = Assert.Single(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Implements);
        var target = Assert.Single(extracted.Nodes, node => node.Id == implements.TargetId);

        Assert.Equal("Shape", target.Name);
        Assert.Null(target.SymbolId);
        Assert.Equal(EvidenceQuality.Structural, implements.Evidence.Quality);
    }

    private static void AssertSameNameClassesStayDistinct(
        Extraction extracted,
        string className,
        string memberName)
    {
        var classes = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Type && node.Name == className)
            .ToArray();
        Assert.Equal(2, classes.Length);
        Assert.Equal(2, classes.Select(node => node.Id).Distinct().Count());
        Assert.All(classes, node => Assert.NotNull(node.SymbolId));

        var members = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Callable && node.Name == memberName)
            .ToArray();
        Assert.Equal(2, members.Length);
        Assert.Equal(2, members.Select(node => node.Id).Distinct().Count());

        foreach (var member in members)
        {
            var owners = extracted.Edges
                .Where(edge => edge.Kind == CodeEdgeKinds.Contains && edge.TargetId == member.Id)
                .Select(edge => edge.SourceId)
                .ToArray();
            Assert.Single(owners);
            Assert.Contains(owners, owner => classes.Any(node => node.Id == owner));
        }
    }
}
