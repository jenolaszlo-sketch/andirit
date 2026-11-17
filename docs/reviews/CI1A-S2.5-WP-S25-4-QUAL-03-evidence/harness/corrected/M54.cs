using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static partial class Probe
{
    private static async Task MissingDefiningContinuity(string phase, string path)
    {
        const string callerProject = "Application/Application.csproj";
        const string dependencyProject = "Library/Library.csproj";
        const string hiddenProject = "Ancestor/Ancestor.csproj";
        const string callerPath = "Application/Run.cs";
        const string dependencyPath = "Library/Api.cs";
        const string doc = "M:EvidenceLibrary.Api.Mark";
        var directReference = phase.Contains("direct", StringComparison.Ordinal);
        var returned = phase.Contains("returned", StringComparison.Ordinal);
        var interfaced = phase.Contains("interface", StringComparison.Ordinal);
        var publish = phase.EndsWith("-publish", StringComparison.Ordinal);
        var repo = new CodeRepositoryId(directReference ? "review:r2:m54:direct" : "review:r2:m54:transitive");
        const string projectStart = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>";
        const string libraryReference = "<ProjectReference Include=\"../Library/Library.csproj\" />";
        const string ancestorReference = "<ProjectReference Include=\"../Ancestor/Ancestor.csproj\" />";
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [callerProject] = projectStart + "<ItemGroup>" + libraryReference +
                (directReference ? ancestorReference : "") + "</ItemGroup></Project>",
            [dependencyProject] = projectStart + "<ItemGroup>" + ancestorReference + "</ItemGroup></Project>",
            [hiddenProject] = projectStart + "</Project>",
            [callerPath] = """
                namespace EvidenceCaller;
                public static class Run
                {
                    public static int Probe(EvidenceLibrary.Child value)
                    {
                        value.Inherited();
                        EvidenceLibrary.Child.InheritedStatic();
                        return EvidenceLibrary.Api.Mark();
                    }
                }
                """,
            [dependencyPath] = """
                namespace EvidenceLibrary;
                public class Child : EvidenceAncestor.Parent {}
                public static class Api { public static int Mark() => 7; }
                """,
            ["Ancestor/Parent.cs"] = """
                namespace EvidenceAncestor;
                public class Parent
                {
                    public void Inherited() {}
                    public static void InheritedStatic() {}
                }
                """
        };
        if (returned || interfaced)
        {
            files["Ancestor/Parent.cs"] += "\npublic interface IParent { void Inherited(); }";
            files[dependencyPath] = "namespace EvidenceLibrary; public class Child : EvidenceAncestor.Parent {} public static class Api { public static int Mark() => 7; public static " +
                (interfaced ? "EvidenceAncestor.IParent" : "EvidenceAncestor.Parent") + " Get() => null!; }";
            files[callerPath] = "namespace EvidenceCaller; public static class Run { public static int Probe() { EvidenceLibrary.Api.Get().Inherited(); return EvidenceLibrary.Api.Mark(); } }";
        }
        var inputs = files.ToDictionary(pair => pair.Key, pair => Encoding.UTF8.GetBytes(pair.Value));
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "m54", inputs));
        VerifiedCSharpCompilation Verified(string project) =>
            analysis.Projects.Single(candidate => candidate.Project.Path == project).Verified!;
        var caller = Verified(callerProject);
        var defining = Verified(dependencyProject);
        var ancestor = Verified(hiddenProject);
        var tree = caller.Compilation.SyntaxTrees.Single();
        var model = caller.Compilation.GetSemanticModel(tree);
        var bindings = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(site =>
            {
                var info = model.GetSymbolInfo(site);
                var method = info.Symbol as IMethodSymbol;
                return new
                {
                    syntax = site.ToString(),
                    span = site.GetLocation().GetLineSpan().Span.ToString(),
                    symbol = method?.ToDisplayString(),
                    assembly = method?.ContainingAssembly.Identity.ToString(),
                    reference = method?.ContainingAssembly is { } assembly
                        ? caller.Compilation.GetMetadataReference(assembly)?.GetType().Name : null,
                    resolution = method is null ? null :
                        analysis.Universe.ResolveDefining(method, null, caller.Compilation).Status.ToString(),
                    reason = info.CandidateReason.ToString(),
                    candidates = info.CandidateSymbols.Select(candidate => candidate.ToDisplayString()).ToArray()
                };
            }).ToArray();
        var authority = AdrCi1Exp.Shared.CompilationProvenance.Describe(repo.Value, ProjectUnit(dependencyProject),
            defining.Compilation, [(dependencyPath, inputs[dependencyPath])]);
        var target = new CodeOccurrenceTargetId("asmctx3:sha256:" + Digest("asmctx3-context", "compctx1",
            authority.DescriptorDigest, authority.MetadataIdentity, authority.ModuleName,
            Digest("asmctx3-identifier", "Mark"), Digest("asmctx3-declarations", doc), Digest("asmctx3-target", "method|0|")));
        const string invocation = "Mark";
        var lines = files[callerPath].Split('\n');
        var lineIndex = Array.FindIndex(lines, line => line.Contains(".Mark()", StringComparison.Ordinal));
        var column = lines[lineIndex].IndexOf(invocation, StringComparison.Ordinal) + 1;
        var expectedId = Usage(repo.Value, CSharp.Id.Value, ProjectUnit(callerProject),
            new(callerPath, lineIndex + 1, column, lineIndex + 1, column + invocation.Length),
            FileDigest(inputs[callerPath]), doc, target.Value);
        var extraction = VerifiedCSharpOccurrenceExtractor.Extract(analysis.Universe, new(
            repo, CSharp.Id, caller,
            method => Digest("asmctx3-identifier", method.Name),
            method => Digest("asmctx3-declarations", method.GetDocumentationCommentId()!),
            method => Digest("asmctx3-target", "method|0|"),
            method => method.GetDocumentationCommentId()!));
        Receipt["sources"] = files;
        Receipt["hashes"] = inputs.ToDictionary(pair => pair.Key, pair => FileDigest(pair.Value));
        Receipt["scope"] = analysis.Universe.Scope.Units;
        Receipt["verifiedMembers"] = analysis.Universe.Entries.Select(unit => Describe(unit.Descriptor));
        Receipt["callerReferences"] = caller.Dependencies.Select(unit => unit.Descriptor.ProjectUnitId).ToArray();
        Receipt["containedAncestorReference"] = caller.Compilation.References.OfType<CompilationReference>()
            .Any(reference => ReferenceEquals(reference.Compilation, ancestor.Compilation));
        Receipt["binding"] = bindings;
        Receipt["diagnostics"] = caller.Compilation.GetDiagnostics().Select(diagnostic => diagnostic.ToString()).ToArray();
        Receipt["initialCoverage"] = extraction.Coverage;
        Receipt["initialFacts"] = extraction.Facts;
        var expectedUnresolved = directReference ? 0 : returned || interfaced ? 1 : 2;
        var expectedFacts = directReference ? 3 : returned || interfaced ? 2 : 1;
        var exactMissing = bindings.Count(item => item.resolution == "MissingDefiningCompilation");
        Receipt["oracle"] = new { expectedUnresolved, expectedFacts, expectedId, target = target.Value };
        Receipt["exactMissingDefiningCompilationSites"] = exactMissing;
        Check(analysis.Universe.IsComplete && extraction.Authority is not null, "A verified declaration and publication scope is required");
        Check(extraction.Coverage.Unresolved == expectedUnresolved && extraction.Coverage.External == 0 &&
            extraction.Coverage.Ambiguous == 0 && extraction.Coverage.InvocationFacts == expectedFacts,
            "Exact extraction counters require two unresolved transitive calls or three resolved directly referenced sites");

        using (var store = new LatticeCodeGraphStore(path))
        {
            if (publish)
                await PublishCSharp(store, repo, "m54", inputs);
            var result = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                new(repo, target), analysis.Universe, caller.Compilation));
            Receipt["query"] = result;
            Check(result.IsComplete && result.Publication?.IndexRunId.Value == "m54" &&
                result.Occurrences.Select(hit => hit.Id.Value).SequenceEqual([expectedId]) &&
                result.Coverage?.Unresolved == expectedUnresolved && result.Coverage.External == 0,
                "Native authority-bound query matches the exact positive identity and diagnostics");
        }
        using var native = JsonDocument.Parse(JsonSerializer.Serialize(ReadNative(path)));
        Receipt["native"] = native.RootElement.Clone();
        var row = native.RootElement.GetProperty("AndiritOccurrenceUnit").EnumerateArray()
            .Single(item => item.GetProperty("key").GetString()!.EndsWith("\n" + ProjectUnit(callerProject), StringComparison.Ordinal));
        using var stored = JsonDocument.Parse(Convert.FromBase64String(row.GetProperty("payload").GetString()!));
        Receipt["decodedCallerUnit"] = stored.RootElement.Clone();
        var coverage = stored.RootElement.GetProperty("Coverage");
        Check(coverage.GetProperty("Unresolved").GetInt32() == expectedUnresolved &&
            coverage.GetProperty("External").GetInt32() == 0 &&
            stored.RootElement.GetProperty("Facts").GetArrayLength() == expectedFacts,
            "Complete extraction-to-native chain preserves coverage and fact counts");
        Receipt["exactM54ContinuityEstablished"] = !directReference && exactMissing == expectedUnresolved &&
            Failures.Count == 0;
    }
}

