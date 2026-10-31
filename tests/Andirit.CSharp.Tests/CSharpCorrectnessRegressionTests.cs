using System.Text.Json;

namespace Andirit.CSharp.Tests;

/// <summary>
/// Permanent regressions for the two production defects remediated by
/// CSHARP-CORRECTNESS-01: canonical-name target confirmation (P-DEF-01) and
/// documented partial-type extraction (P-DEF-02).
/// </summary>
public sealed partial class CSharpCodeGraphPluginTests
{
    private const string ProjectDocumentationId = "M:Example.Helper.Go";

    private static string ProjectXml(
        string? assemblyName = null,
        string? projectReference = null)
    {
        var assembly = assemblyName is null
            ? string.Empty
            : $"    <AssemblyName>{assemblyName}</AssemblyName>\n";
        var reference = projectReference is null
            ? string.Empty
            : $"  <ItemGroup>\n    <ProjectReference Include=\"{projectReference}\" />\n  </ItemGroup>\n";
        return
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
            "  <PropertyGroup>\n" +
            "    <TargetFramework>net10.0</TargetFramework>\n" +
            assembly +
            "  </PropertyGroup>\n" +
            reference +
            "</Project>\n";
    }

    private static CodeNodeId ScopedSymbolNodeId(string projectPath, string canonical) =>
        new($"csharp:symbol:{Hash($"{projectPath}\n{canonical}")}");

    private static CodeRelationshipCoverage Coverage(Extraction extracted, CodeEdgeKind kind) =>
        extracted.Result.RelationshipCoverage.Single(value =>
            value.RelationshipKind == kind.Value);

    private static CodeGraphNode Callable(Extraction extracted, string qualifiedName) =>
        extracted.Nodes.Single(node =>
            node.Kind == CodeNodeKinds.Callable &&
            node.QualifiedName == qualifiedName);

    private static string Canonical(CodeGraphNode node) =>
        ((CodeTextProperty)node.Properties[CodePropertyKeys.CanonicalKey]).Value;

    [Theory]
    [InlineData("src/A/A.csproj", "src/A/Shadow.cs", "src/Z/Z.csproj", "src/Z/App.cs")]
    [InlineData("src/Z/Z.csproj", "src/Z/Shadow.cs", "src/A/A.csproj", "src/A/App.cs")]
    public async Task ExtractAsync_ExternalCallIsNeverConfirmedAsAnUnrelatedDocumentationIdShadow(
        string shadowProject,
        string shadowPath,
        string callerProject,
        string callerPath)
    {
        var extracted = await ExtractAsync(
            (callerProject, ProjectXml()),
            (callerPath, """
                namespace App;

                public class App
                {
                    public void Run() => System.Console.WriteLine("outside");
                }
                """),
            (shadowProject, ProjectXml()),
            (shadowPath, """
                namespace System;

                /// <summary>An unrelated indexed declaration that happens to share the platform documentation ID.</summary>
                public static class Console
                {
                    public static void WriteLine(string value) { }
                }
                """));

        var shadowMethod = ScopedSymbolNodeId(shadowProject, "M:System.Console.WriteLine(System.String)");
        var shadowType = ScopedSymbolNodeId(shadowProject, "T:System.Console");
        var run = Callable(extracted, "App.App.Run()");

        Assert.Contains(extracted.Nodes, node => node.Id == shadowMethod);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == shadowMethod);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.References &&
                edge.SourceId == run.Id &&
                (edge.TargetId == shadowMethod || edge.TargetId == shadowType));

        var calls = Coverage(extracted, CodeEdgeKinds.Calls);
        Assert.Equal(0, calls.EdgesEmitted);
        Assert.Equal(0, calls.CrossProjectTargets);
        Assert.True(calls.ExternalTargets >= 1, $"external={calls.ExternalTargets}");
    }

    [Fact]
    public async Task ExtractAsync_CrossProjectCallResolvesToTheRealDependencyDespiteDuplicateDocumentationId()
    {
        var extracted = await ExtractAsync(
            ("src/AAUnrelated/AAUnrelated.csproj", ProjectXml()),
            ("src/AAUnrelated/Helper.cs", """
                namespace Example;

                public static class Helper
                {
                    public static void Go() { }
                }
                """),
            ("src/Lib/Lib.csproj", ProjectXml()),
            ("src/Lib/Helper.cs", """
                namespace Example;

                public static class Helper
                {
                    public static void Go() { }
                }
                """),
            ("src/App/App.csproj", ProjectXml(projectReference: "../Lib/Lib.csproj")),
            ("src/App/Program.cs", """
                namespace App;

                public class Program
                {
                    public void Run() => Example.Helper.Go();
                }
                """));

        var run = Callable(extracted, "App.Program.Run()");
        var dependencyTarget = ScopedSymbolNodeId("src/Lib/Lib.csproj", ProjectDocumentationId);
        var unrelatedTarget = ScopedSymbolNodeId(
            "src/AAUnrelated/AAUnrelated.csproj",
            ProjectDocumentationId);

        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls &&
            edge.SourceId == run.Id &&
            edge.TargetId == dependencyTarget);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == unrelatedTarget);

        var calls = Coverage(extracted, CodeEdgeKinds.Calls);
        Assert.Equal(1, calls.EdgesEmitted);
        Assert.Equal(1, calls.CrossProjectTargets);
        Assert.Equal(0, calls.AmbiguousTargets);
        Assert.Equal(0, calls.ExternalTargets);
    }

    [Fact]
    public async Task ExtractAsync_SameAssemblyNameUsesProjectProvenanceNotNameEquality()
    {
        var extracted = await ExtractAsync(
            ("src/ALib/ALib.csproj", ProjectXml(assemblyName: "Shared.Lib")),
            ("src/ALib/Helper.cs", """
                namespace Example;

                public static class Helper
                {
                    public static void Go() { }
                }
                """),
            ("src/BLib/BLib.csproj", ProjectXml(assemblyName: "Shared.Lib")),
            ("src/BLib/Helper.cs", """
                namespace Example;

                public static class Helper
                {
                    public static void Go() { }
                }
                """),
            ("src/App/App.csproj", ProjectXml(projectReference: "../BLib/BLib.csproj")),
            ("src/App/Program.cs", """
                namespace App;

                public class Program
                {
                    public void Run() => Example.Helper.Go();
                }
                """));

        var run = Callable(extracted, "App.Program.Run()");
        var expected = ScopedSymbolNodeId("src/BLib/BLib.csproj", ProjectDocumentationId);

        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls &&
            edge.SourceId == run.Id &&
            edge.TargetId == expected);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls &&
                edge.TargetId == ScopedSymbolNodeId("src/ALib/ALib.csproj", ProjectDocumentationId));
    }

    [Fact]
    public async Task ExtractAsync_LinkedSourceFileKeepsDistinctProjectContexts()
    {
        const string linked = """
            namespace Example;

            public static class Helper
            {
                public static void Go() { }
            }
            """;

        var extracted = await ExtractAsync(
            ("Shared/Linked.cs", linked),
            ("src/ALib/ALib.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <AssemblyName>ALib</AssemblyName>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="../../Shared/Linked.cs" />
                  </ItemGroup>
                </Project>
                """),
            ("src/ALib/Anchor.cs", "namespace ALib; public class Anchor { }"),
            ("src/BLib/BLib.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <AssemblyName>BLib</AssemblyName>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="../../Shared/Linked.cs" />
                  </ItemGroup>
                </Project>
                """),
            ("src/BLib/Anchor.cs", "namespace BLib; public class Anchor { }"),
            ("src/App/App.csproj", ProjectXml(projectReference: "../BLib/BLib.csproj")),
            ("src/App/Program.cs", """
                namespace App;

                public class Program
                {
                    public void Run() => Example.Helper.Go();
                }
                """));

        // The same physical file produces one node per project context.
        var helperNodes = extracted.Nodes
            .Where(node =>
                node.Kind == CodeNodeKinds.Type &&
                node.Properties.TryGetValue(CodePropertyKeys.CanonicalKey, out var value) &&
                value is CodeTextProperty text &&
                text.Value == "T:Example.Helper")
            .ToArray();
        Assert.Equal(2, helperNodes.Length);

        var run = Callable(extracted, "App.Program.Run()");
        var expected = ScopedSymbolNodeId("src/BLib/BLib.csproj", "M:Example.Helper.Go");
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls &&
            edge.SourceId == run.Id &&
            edge.TargetId == expected);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls &&
                edge.TargetId == ScopedSymbolNodeId("src/ALib/ALib.csproj", "M:Example.Helper.Go"));
    }

    [Fact]
    public async Task ExtractAsync_GenericDefinitionResolutionIgnoresUnrelatedDocumentationIdCollision()
    {
        var extracted = await ExtractAsync(
            ("src/AAUnrelated/AAUnrelated.csproj", ProjectXml()),
            ("src/AAUnrelated/Store.cs", """
                namespace Example;

                public class Store<T>
                {
                    public T Get(T fallback) => fallback;
                }
                """),
            ("src/Lib/Lib.csproj", ProjectXml()),
            ("src/Lib/Store.cs", """
                namespace Example;

                public class Store<T>
                {
                    public T Get(T fallback) => fallback;
                }
                """),
            ("src/App/App.csproj", ProjectXml(projectReference: "../Lib/Lib.csproj")),
            ("src/App/Program.cs", """
                namespace App;

                public class Program
                {
                    public string Run() => new Example.Store<string>().Get("hi");
                }
                """));

        var run = Callable(extracted, "App.Program.Run()");
        const string canonical = "M:Example.Store`1.Get(`0)";
        var expected = ScopedSymbolNodeId("src/Lib/Lib.csproj", canonical);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls &&
            edge.SourceId == run.Id &&
            edge.TargetId == expected);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls &&
                edge.TargetId == ScopedSymbolNodeId("src/AAUnrelated/AAUnrelated.csproj", canonical));
    }

    [Fact]
    public async Task ExtractAsync_ReducedExtensionResolutionIgnoresUnrelatedDocumentationIdCollision()
    {
        var extracted = await ExtractAsync(
            ("src/AAUnrelated/AAUnrelated.csproj", ProjectXml()),
            ("src/AAUnrelated/Extensions.cs", """
                namespace Example;

                public static class StringExtensions
                {
                    public static string Shout(this string value) => value;
                }
                """),
            ("src/Lib/Lib.csproj", ProjectXml()),
            ("src/Lib/Extensions.cs", """
                namespace Example;

                public static class StringExtensions
                {
                    public static string Shout(this string value) => value;
                }
                """),
            ("src/App/App.csproj", ProjectXml(projectReference: "../Lib/Lib.csproj")),
            ("src/App/Program.cs", """
                namespace App;

                using Example;

                public class Program
                {
                    public string Run(string input) => input.Shout();
                }
                """));

        var run = Callable(extracted, "App.Program.Run(string)");
        const string canonical = "M:Example.StringExtensions.Shout(System.String)";
        var expected = ScopedSymbolNodeId("src/Lib/Lib.csproj", canonical);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls &&
            edge.SourceId == run.Id &&
            edge.TargetId == expected);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls &&
                edge.TargetId == ScopedSymbolNodeId("src/AAUnrelated/AAUnrelated.csproj", canonical));
    }

    [Fact]
    public async Task ExtractAsync_OverloadResolutionUsesTheBoundSignatureAndDependencyContext()
    {
        var extracted = await ExtractAsync(
            ("src/AAUnrelated/AAUnrelated.csproj", ProjectXml()),
            ("src/AAUnrelated/Overloads.cs", """
                namespace Example;

                public static class Overloads
                {
                    public static void WriteLine(string value) { }
                }
                """),
            ("src/Lib/Lib.csproj", ProjectXml()),
            ("src/Lib/Overloads.cs", """
                namespace Example;

                public static class Overloads
                {
                    public static void WriteLine(string value) { }
                    public static void WriteLine(int value) { }
                }
                """),
            ("src/App/App.csproj", ProjectXml(projectReference: "../Lib/Lib.csproj")),
            ("src/App/Program.cs", """
                namespace App;

                public class Program
                {
                    public void Run() => Example.Overloads.WriteLine("text");
                }
                """));

        var run = Callable(extracted, "App.Program.Run()");
        const string stringOverload = "M:Example.Overloads.WriteLine(System.String)";
        var expected = ScopedSymbolNodeId("src/Lib/Lib.csproj", stringOverload);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls &&
            edge.SourceId == run.Id &&
            edge.TargetId == expected);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls &&
                edge.TargetId == ScopedSymbolNodeId(
                    "src/Lib/Lib.csproj",
                    "M:Example.Overloads.WriteLine(System.Int32)"));
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls &&
                edge.TargetId == ScopedSymbolNodeId("src/AAUnrelated/AAUnrelated.csproj", stringOverload));
    }

    [Fact]
    public async Task ExtractAsync_SameNameLocalDeclarationRemainsTheCurrentProjectTarget()
    {
        var extracted = await ExtractAsync(
            ("src/Lib/Lib.csproj", ProjectXml()),
            ("src/Lib/Helper.cs", """
                namespace Example;

                public static class Helper
                {
                    public static void Go() { }
                }
                """),
            ("src/App/App.csproj", ProjectXml(projectReference: "../Lib/Lib.csproj")),
            ("src/App/Helper.cs", """
                namespace Example;

                public static class Helper
                {
                    public static void Go() { }
                }
                """),
            ("src/App/Program.cs", """
                namespace App;

                public class Program
                {
                    public void Run() => Example.Helper.Go();
                }
                """));

        var run = Callable(extracted, "App.Program.Run()");
        var current = ScopedSymbolNodeId("src/App/App.csproj", ProjectDocumentationId);
        var dependency = ScopedSymbolNodeId("src/Lib/Lib.csproj", ProjectDocumentationId);

        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls &&
            edge.SourceId == run.Id &&
            edge.TargetId == current);
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == dependency);
    }

    [Fact]
    public async Task ExtractAsync_RepeatedExtractionKeepsValidIdentityAndRefusesTheShadow()
    {
        var sources = new[]
        {
            ("src/A/A.csproj", ProjectXml()),
            ("src/A/Shadow.cs", """
                namespace System;
                public static class Console { public static void WriteLine(string value) { } }
                """),
            ("src/Lib/Lib.csproj", ProjectXml()),
            ("src/Lib/Utility.cs", """
                namespace Lib;
                public static class Utility { public static int Add(int a, int b) => a + b; }
                """),
            ("src/Z/Z.csproj", ProjectXml(projectReference: "../Lib/Lib.csproj")),
            ("src/Z/App.cs", """
                namespace App;

                public class App
                {
                    public int Run()
                    {
                        System.Console.WriteLine("outside");
                        return Lib.Utility.Add(1, 2);
                    }
                }
                """)
        };

        var first = await ExtractAsync(sources);
        var second = await ExtractAsync(sources.Reverse().ToArray());

        var shadow = ScopedSymbolNodeId("src/A/A.csproj", "M:System.Console.WriteLine(System.String)");
        var add = ScopedSymbolNodeId("src/Lib/Lib.csproj", "M:Lib.Utility.Add(System.Int32,System.Int32)");
        var firstValid = first.Edges.Single(edge =>
            edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == add);
        var secondValid = second.Edges.Single(edge =>
            edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == add);
        Assert.Equal(firstValid.Id, secondValid.Id);
        Assert.DoesNotContain(
            first.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == shadow);
        Assert.DoesNotContain(
            second.Edges,
            edge => edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == shadow);

        Assert.Equal(JsonSerializer.Serialize(first.Nodes), JsonSerializer.Serialize(second.Nodes));
        Assert.Equal(JsonSerializer.Serialize(first.Edges), JsonSerializer.Serialize(second.Edges));
        Assert.Equal(
            JsonSerializer.Serialize(first.Result.RelationshipCoverage),
            JsonSerializer.Serialize(second.Result.RelationshipCoverage));
    }

    [Fact]
    public async Task ExtractAsync_RemovedOrReplacedTargetContributionDoesNotLeaveAStaleEdge()
    {
        var appProject = ("src/App/App.csproj", ProjectXml(projectReference: "../Lib/Lib.csproj"));
        var appSource = ("src/App/Program.cs", """
            namespace App;

            public class Program
            {
                public int Run() => Lib.Utility.Add(1, 2);
            }
            """);
        var libProject = ("src/Lib/Lib.csproj", ProjectXml());
        var libV1 = ("src/Lib/Utility.cs", """
            namespace Lib;
            public static class Utility { public static int Add(int a, int b) => a + b; }
            """);
        var libV2 = ("src/Lib/Utility.cs", """
            namespace Lib;
            public static class Utility { public static int Multiply(int a, int b) => a * b; }
            """);

        var before = await ExtractAsync(appProject, appSource, libProject, libV1);
        var add = ScopedSymbolNodeId("src/Lib/Lib.csproj", "M:Lib.Utility.Add(System.Int32,System.Int32)");
        Assert.Contains(before.Edges, edge => edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == add);

        var replaced = await ExtractAsync(appProject, appSource, libProject, libV2);
        Assert.DoesNotContain(replaced.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == add);
        Assert.True(
            Coverage(replaced, CodeEdgeKinds.Calls).UnresolvedTargets > 0,
            "the removed target must be reported as unresolved");

        var withoutProject = await ExtractAsync(appSource);
        Assert.DoesNotContain(withoutProject.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls && edge.TargetId == add);
        Assert.True(
            Coverage(withoutProject, CodeEdgeKinds.Calls).UnresolvedTargets > 0,
            "the missing project must leave the call unresolved");
    }

    [Fact]
    public async Task ExtractAsync_CrossProjectImportsInheritanceAndReferencesRemainResolved()
    {
        var extracted = await ExtractAsync(
            ("src/Lib/Lib.csproj", ProjectXml()),
            ("src/Lib/Contracts.cs", """
                namespace Lib.Sub;

                public interface IWorker
                {
                    void Execute();
                }

                public class Base
                {
                    public virtual void Run() { }
                }
                """),
            ("src/App/App.csproj", ProjectXml(projectReference: "../Lib/Lib.csproj")),
            ("src/App/Program.cs", """
                using Lib.Sub;

                namespace App;

                public class Program : Base, IWorker
                {
                    public override void Run() => base.Run();
                    public void Execute() { }
                }
                """));

        var program = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "App.Program");
        var baseType = ScopedSymbolNodeId("src/Lib/Lib.csproj", "T:Lib.Sub.Base");
        var worker = ScopedSymbolNodeId("src/Lib/Lib.csproj", "T:Lib.Sub.IWorker");
        var namespaceNode = ScopedSymbolNodeId("src/Lib/Lib.csproj", "N:Lib.Sub");

        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Inherits &&
            edge.SourceId == program.Id &&
            edge.TargetId == baseType);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Implements &&
            edge.SourceId == program.Id &&
            edge.TargetId == worker);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Imports &&
            edge.TargetId == namespaceNode);
    }

    [Fact]
    public async Task ExtractAsync_MergedCrossProjectNamespaceImportStaysAmbiguous()
    {
        var extracted = await ExtractAsync(
            ("src/Lib1/Lib1.csproj", ProjectXml()),
            ("src/Lib1/One.cs", "namespace Shared; public class One { }"),
            ("src/Lib2/Lib2.csproj", ProjectXml()),
            ("src/Lib2/Two.cs", "namespace Shared; public class Two { }"),
            ("src/App/App.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="../Lib1/Lib1.csproj" />
                    <ProjectReference Include="../Lib2/Lib2.csproj" />
                  </ItemGroup>
                </Project>
                """),
            ("src/App/Program.cs", """
                using Shared;

                namespace App;

                public class Program
                {
                    public void Run() { }
                }
                """));

        var imports = Coverage(extracted, CodeEdgeKinds.Imports);
        Assert.True(imports.AmbiguousTargets > 0, $"ambiguous={imports.AmbiguousTargets}");
        Assert.Equal(0, imports.EdgesEmitted);
    }

    // ---------------------------------------------------------------------
    // P-DEF-02: documented partial-type extraction
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_DocumentedSealedPartialTypeMergesDeclarationDocumentation()
    {
        var extracted = await ExtractAsync(
            ("src/P.One.cs", """
                namespace Example;

                public sealed partial class P
                {
                    public void A() { }
                }
                """),
            ("src/P.Two.cs", """
                namespace Example;

                /// <summary>Documented part.</summary>
                public sealed partial class P
                {
                    public void B() { }
                }
                """),
            ("src/Consumer.cs", """
                namespace Example;

                public class Consumer
                {
                    public void Run(P value) => value.A();
                }
                """));

        var type = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Example.P");
        Assert.Equal(
            2,
            extracted.Declarations.Count(declaration => declaration.SymbolNodeId == type.Id));
        Assert.Equal(
            "Documented part.",
            ((CodeTextProperty)type.Properties[CodePropertyKeys.DocSummary]).Value);

        var methodA = Callable(extracted, "Example.P.A()");
        var consumer = Callable(extracted, "Example.Consumer.Run(Example.P)");
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Contains &&
            edge.SourceId == type.Id &&
            edge.TargetId == methodA.Id);
        Assert.Contains(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Calls &&
            edge.SourceId == consumer.Id &&
            edge.TargetId == methodA.Id);
    }

    [Fact]
    public async Task ExtractAsync_UndocumentedPartialTypeHasNoDocumentationProperties()
    {
        var extracted = await ExtractAsync(
            ("src/P.One.cs", "namespace Example; public sealed partial class P { }"),
            ("src/P.Two.cs", "namespace Example; public sealed partial class P { }"));

        var type = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Example.P");
        Assert.False(type.Properties.ContainsKey(CodePropertyKeys.DocSummary));
        Assert.False(type.Properties.ContainsKey(CodePropertyKeys.DocRemarks));
        Assert.Equal(
            2,
            extracted.Declarations.Count(declaration => declaration.SymbolNodeId == type.Id));
    }

    [Fact]
    public async Task ExtractAsync_IdenticalPartialDocumentationIsStoredOnce()
    {
        var extracted = await ExtractAsync(
            ("src/P.One.cs", """
                namespace Example;

                /// <summary>Same summary.</summary>
                public sealed partial class P { }
                """),
            ("src/P.Two.cs", """
                namespace Example;

                /// <summary>Same summary.</summary>
                public sealed partial class P { }
                """));

        var type = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Example.P");
        Assert.Equal(
            "Same summary.",
            ((CodeTextProperty)type.Properties[CodePropertyKeys.DocSummary]).Value);
    }

    [Fact]
    public async Task ExtractAsync_DifferentPartialDocumentationMergesDeterministicallyAndOrderIndependently()
    {
        var forward = await ExtractAsync(
            ("src/P.One.cs", """
                namespace Example;

                /// <summary>Alpha summary.</summary>
                public sealed partial class P { }
                """),
            ("src/P.Two.cs", """
                namespace Example;

                /// <summary>Beta summary.</summary>
                public sealed partial class P { }
                """),
            ("src/P.Three.cs", """
                namespace Example;

                /// <summary>Gamma summary.</summary>
                public sealed partial class P { }
                """));

        var reverse = await ExtractAsync(
            ("src/P.Three.cs", """
                namespace Example;

                /// <summary>Gamma summary.</summary>
                public sealed partial class P { }
                """),
            ("src/P.Two.cs", """
                namespace Example;

                /// <summary>Beta summary.</summary>
                public sealed partial class P { }
                """),
            ("src/P.One.cs", """
                namespace Example;

                /// <summary>Alpha summary.</summary>
                public sealed partial class P { }
                """));

        var first = Assert.Single(forward.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Example.P");
        var second = Assert.Single(reverse.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Example.P");
        Assert.Equal(
            "Alpha summary.\n\nBeta summary.\n\nGamma summary.",
            ((CodeTextProperty)first.Properties[CodePropertyKeys.DocSummary]).Value);
        Assert.Equal(
            JsonSerializer.Serialize(forward.Nodes),
            JsonSerializer.Serialize(reverse.Nodes));
        Assert.Equal(
            JsonSerializer.Serialize(forward.Declarations),
            JsonSerializer.Serialize(reverse.Declarations));
        Assert.Equal(
            JsonSerializer.Serialize(forward.Edges),
            JsonSerializer.Serialize(reverse.Edges));
        Assert.Equal(
            JsonSerializer.Serialize(forward.Result.RelationshipCoverage),
            JsonSerializer.Serialize(reverse.Result.RelationshipCoverage));
    }

    [Fact]
    public async Task ExtractAsync_PartialDocumentationMergeIsBounded()
    {
        var parts = Enumerable.Range(0, 6)
            .Select(index =>
            {
                var summary = new string((char)('a' + index), 512);
                return (
                    $"src/P.{index}.cs",
                    "namespace Example;\n\n" +
                    $"/// <summary>{summary}</summary>\n" +
                    "public sealed partial class P { }\n");
            })
            .ToArray();

        var extracted = await ExtractAsync(parts);

        var type = Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Example.P");
        var merged = ((CodeTextProperty)type.Properties[CodePropertyKeys.DocSummary]).Value;
        Assert.Equal(2048, merged.Length);
        Assert.StartsWith(new string('a', 512), merged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractAsync_RepeatedDocumentedPartialExtractionIsDeterministic()
    {
        var sources = new[]
        {
            ("src/P.One.cs", """
                namespace Example;

                /// <summary>Alpha summary.</summary>
                public sealed partial class P { public void A() { } }
                """),
            ("src/P.Two.cs", """
                namespace Example;

                /// <summary>Beta summary.</summary>
                public sealed partial class P { public void B() { } }
                """)
        };

        var first = await ExtractAsync(sources);
        var second = await ExtractAsync(sources.Reverse().ToArray());
        Assert.Equal(JsonSerializer.Serialize(first.Nodes), JsonSerializer.Serialize(second.Nodes));
        Assert.Equal(
            JsonSerializer.Serialize(first.Declarations),
            JsonSerializer.Serialize(second.Declarations));
        Assert.Equal(JsonSerializer.Serialize(first.Edges), JsonSerializer.Serialize(second.Edges));
    }

    [Fact]
    public async Task ExtractAsync_ReplacementRemovesStalePartialDocumentation()
    {
        var documented = await ExtractAsync(
            ("src/P.One.cs", """
                namespace Example;

                /// <summary>Old summary.</summary>
                public sealed partial class P { }
                """),
            ("src/P.Two.cs", """
                namespace Example;

                /// <summary>Other summary.</summary>
                public sealed partial class P { }
                """));
        Assert.True(documented.Nodes.Single(node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Example.P")
            .Properties.ContainsKey(CodePropertyKeys.DocSummary));

        var undocumented = await ExtractAsync(
            ("src/P.One.cs", "namespace Example; public sealed partial class P { }"),
            ("src/P.Two.cs", "namespace Example; public sealed partial class P { }"));
        Assert.False(undocumented.Nodes.Single(node =>
            node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Example.P")
            .Properties.ContainsKey(CodePropertyKeys.DocSummary));
    }

    [Fact]
    public async Task ExtractAsync_GenuineInvariantConflictFailsWithTypeSafeDiagnostic()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExtractAsync(
                ("src/A.cs", "namespace Example; public static class Conflict { }"),
                ("src/B.cs", "namespace Example; public delegate void Conflict();")));

        Assert.Contains("inconsistent facts", exception.Message, StringComparison.Ordinal);
        Assert.Contains("modifiers", exception.Message, StringComparison.Ordinal);
        Assert.Contains("static", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sealed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractAsync_RealAndiritCSharpPluginPartialDeclarationsExtract()
    {
        var root = RepositoryRoot;
        var directories = new[]
        {
            "src/Andirit.Abstractions",
            "src/Andirit",
            "src/Andirit.CSharp"
        };
        var sources = directories
            .SelectMany(directory => Directory
                .EnumerateFiles(Path.Combine(root, directory), "*.cs", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(
                    Path.Combine(root, directory),
                    "*.csproj",
                    SearchOption.TopDirectoryOnly)))
            .Order(StringComparer.Ordinal)
            .Select(path => (
                Path: Path.GetRelativePath(root, path).Replace('\\', '/'),
                Content: File.ReadAllText(path)))
            .ToArray();

        var extracted = await ExtractAsync(sources);

        var plugin = Assert.Single(extracted.Nodes, node =>
            node.Properties.TryGetValue(CodePropertyKeys.CanonicalKey, out var value) &&
            value is CodeTextProperty text &&
            text.Value == "T:Andirit.CSharpCodeGraphPlugin");
        Assert.Equal(
            6,
            extracted.Declarations.Count(declaration => declaration.SymbolNodeId == plugin.Id));
        Assert.Equal(
            "Deterministic repository-aware C# extraction powered by Roslyn.",
            ((CodeTextProperty)plugin.Properties[CodePropertyKeys.DocSummary]).Value);

        var discovery = Assert.Single(extracted.Nodes, node =>
            node.Properties.TryGetValue(CodePropertyKeys.CanonicalKey, out var value) &&
            value is CodeTextProperty text &&
            text.Value == "T:Andirit.CSharpProjectDiscovery");
        Assert.Single(extracted.Declarations, declaration => declaration.SymbolNodeId == discovery.Id);

        // The wider three-project extraction progresses into relationships.
        Assert.True(
            Coverage(extracted, CodeEdgeKinds.Calls).Candidates > 0,
            "real-source extraction did not reach relationship extraction");
    }

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null &&
                   !File.Exists(Path.Combine(directory.FullName, "Andirit.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ??
                throw new InvalidOperationException("Repository root not found.");
        }
    }
}
