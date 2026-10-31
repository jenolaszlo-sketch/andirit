// ============================================================================
// CI-1-SCOPE-R3 independent identity probe (Q3-style, separately authored).
// Derives the identity facts from first principles and compares:
//   * the R2 eligibility rule (source-built set membership),
//   * the R3 exact-definition rule (defining compilation + assembly identity +
//     documentation ID + declaration location).
// Scenarios: S1 two-source-built assemblies with one doc ID; S2 the F01
// platform/local collision under the R3 metadata rule; S3 the lexical-owner
// channel; S4 the cross-project reference edge.
// ============================================================================

using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)
    .Select(p => MetadataReference.CreateFromFile(p)).ToArray<MetadataReference>();

CSharpCompilation Build(string name, (string Path, string Text)[] sources, MetadataReference[]? extra = null)
    => CSharpCompilation.Create(name,
        sources.Select(s => CSharpSyntaxTree.ParseText(s.Text, new CSharpParseOptions(LanguageVersion.Latest), s.Path)),
        references.Concat(extra ?? []), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false));

static string? Doc(ISymbol? s)
{
    if (s is null) return null;
    var norm = s is IMethodSymbol m ? (m.ReducedFrom ?? m) : s;
    return norm.OriginalDefinition.GetDocumentationCommentId();
}
static ISymbol Def(ISymbol s) => (s is IMethodSymbol m ? (m.ReducedFrom ?? m) : s).OriginalDefinition;

// R2 eligibility: source-built assembly set membership + any source location.
bool R2Eligible(ISymbol? target, IEnumerable<CSharpCompilation> set)
{
    if (target is null) return false;
    var def = Def(target);
    var asm = def.ContainingAssembly;
    if (asm is null || !set.Any(c => c.Assembly.Identity.Equals(asm.Identity))) return false;
    return def.Locations.Any(l => l.IsInSource && l.SourceTree is not null);
}

// R3 exact-definition descriptor.
Desc? Describe(ISymbol? target, IEnumerable<CSharpCompilation> comps)
{
    if (target is null) return null;
    var def = Def(target);
    var asm = def.ContainingAssembly;
    if (asm is null) return null;
    var loc = def.Locations.FirstOrDefault(l => l.IsInSource && l.SourceTree is not null);
    if (loc is null) return new Desc("metadata", asm.Identity.ToString(), Doc(def) ?? "", null, null, null);
    var tree = loc.SourceTree!;
    return new Desc(comps.FirstOrDefault(c => c.SyntaxTrees.Contains(tree))?.AssemblyName ?? "unknown",
        asm.Identity.ToString(), Doc(def) ?? "", tree.FilePath, loc.SourceSpan.Start, loc.SourceSpan.End);
}
static bool Same(Desc a, Desc b) => a.DocId == b.DocId && a.Compilation == b.Compilation && a.Assembly == b.Assembly
    && a.Tree == b.Tree && a.Start == b.Start && a.End == b.End;

List<ISymbol> DeclsByDoc(CSharpCompilation comp, string docId)
{
    var found = new List<(ISymbol Sym, string Tree, int S, int E)>();
    foreach (var tree in comp.SyntaxTrees)
    {
        var model = comp.GetSemanticModel(tree);
        foreach (var n in tree.GetRoot().DescendantNodes())
        {
            ISymbol? ds = n switch
            {
                MethodDeclarationSyntax md => model.GetDeclaredSymbol(md),
                ClassDeclarationSyntax cl => model.GetDeclaredSymbol(cl),
                _ => null,
            };
            if (ds is not null && Doc(ds) == docId)
            {
                var sp = ds.Locations.FirstOrDefault(l => l.IsInSource)?.SourceSpan ?? default;
                found.Add((ds.OriginalDefinition, tree.FilePath, sp.Start, sp.End));
            }
        }
    }
    return found.GroupBy(f => (f.Tree, f.S, f.E)).Select(g => g.First().Sym).ToList();
}

var scenarios = new List<object>();

// ---- S1: two source-built assemblies expose the same documentation ID.
var decl = "namespace N { public static class C { public static void M() { } } }";
var compA = Build("A-src", new[] { ("A.cs", decl) });
var compB = Build("B-src", new[] { ("B.cs", decl) });
var caller = Build("caller-probe", new[] { ("Caller.cs", "namespace N { public static class CallerP { public static void Run() { C.M(); } } }") }, extra: [compB.ToMetadataReference()]);
var callerTree = caller.SyntaxTrees.Single();
var inv = callerTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
var bound = (IMethodSymbol)caller.GetSemanticModel(callerTree).GetSymbolInfo(inv).Symbol!;
var abComps = new[] { compA, compB, caller };
var doc = Doc(bound)!;
var descBound = Describe(bound, abComps)!;
var descA = Describe(DeclsByDoc(compA, doc).Single(), abComps)!;
var descB = Describe(DeclsByDoc(compB, doc).Single(), abComps)!;
scenarios.Add(new
{
    scenario = "S1 two-source-built-assemblies-same-doc-id (Q3-F02)",
    docId = doc,
    boundContainingAssembly = bound.ContainingAssembly!.Identity.ToString(),
    r2EligibleLocalOfBoundTarget = R2Eligible(bound, abComps),
    r2WouldDeriveClass = R2Eligible(bound, abComps) ? "confirmed" : "external",
    r2RuleWouldAcceptEitherExpectation = R2Eligible(bound, abComps) && doc == Doc(DeclsByDoc(compA, doc).Single()),
    boundDefinition = descBound,
    expectedA = descA,
    expectedB = descB,
    r3ExactMatchToA = Same(descBound, descA),
    r3ExactMatchToB = Same(descBound, descB),
    r3RejectsWrongAssemblyExpectation = !Same(descBound, descA) && descBound.Compilation != descA.Compilation,
    requirementSatisfied = doc == "M:N.C.M" && R2Eligible(bound, abComps) && !Same(descBound, descA) && Same(descBound, descB),
});

// ---- S2: F01 collision under the R3 metadata rule.
var collisionRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : "";
var shadowSrc = File.ReadAllText(Path.Combine(collisionRoot, "f01-collision/A/Shadow.cs"));
var appSrc = File.ReadAllText(Path.Combine(collisionRoot, "f01-collision/Z/App.cs"));
var collisionShadow = Build("Shadow", new[] { ("Shadow.cs", shadowSrc) });
var collisionApp = Build("App", new[] { ("App.cs", appSrc) });
var collComps = new[] { collisionShadow, collisionApp };
var collTree = collisionApp.SyntaxTrees.Single();
var collInv = collTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
var collBound = (IMethodSymbol)collisionApp.GetSemanticModel(collTree).GetSymbolInfo(collInv).Symbol!;
var shadowDecl = (IMethodSymbol)collisionShadow.GetSemanticModel(collisionShadow.SyntaxTrees.Single())
    .GetDeclaredSymbol(collisionShadow.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single())!;
var collDoc = Doc(collBound)!;
var collBoundDesc = Describe(collBound, collComps)!;
var collShadowDesc = Describe(shadowDecl, collComps)!;
scenarios.Add(new
{
    scenario = "S2 F01 platform/local collision under the R3 metadata rule",
    docId = collDoc,
    docIdCoincides = collDoc == Doc(shadowDecl),
    indexedLocalIsSourceBuilt = collShadowDesc.Compilation != "metadata",
    boundTargetIsMetadataOnly = collBoundDesc.Compilation == "metadata",
    r3MetadataRuleAcceptsPlatformCall = collBoundDesc.Compilation == "metadata",
    r3MetadataRuleRejectsSourceBuiltShadow = collShadowDesc.Compilation != "metadata",
    requirementSatisfied = collBoundDesc.Compilation == "metadata" && collShadowDesc.Compilation != "metadata",
});

// ---- S3: the lexical-owner channel distinguishes same-doc-ID occurrences.
var ownerSrc = """
namespace O
{
    public class T { public void M() { } }
    public class O1 { public void Run() { var t = new T(); t.M(); } }
    public class O2 { public void Run() { var t = new T(); t.M(); } }
}
""";
var ownerComp = Build("owner-probe", new[] { ("Owners.cs", ownerSrc) });
var ownerModel = ownerComp.GetSemanticModel(ownerComp.SyntaxTrees.Single());
var ownerInvocations = ownerComp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
var ownerFacts = ownerInvocations.Select(i =>
{
    var t = ownerModel.GetSymbolInfo(i).Symbol;
    var ownerNode = i.Ancestors().FirstOrDefault(n => n is BaseMethodDeclarationSyntax);
    var ownerSym = ownerNode is null ? null : ownerModel.GetDeclaredSymbol(ownerNode);
    return new { boundDoc = Doc(t), lexicalOwnerDoc = ownerSym is null ? null : Doc(ownerSym) };
}).ToArray();
scenarios.Add(new
{
    scenario = "S3 lexical-owner channel distinguishes same-doc-ID occurrences",
    occurrences = ownerFacts,
    docIdsCoincide = ownerFacts.Select(f => f.boundDoc).Distinct().Count() == 1,
    ownersDistinguished = ownerFacts.Select(f => f.lexicalOwnerDoc).Distinct().Count() == 2,
    requirementSatisfied = ownerFacts.Select(f => f.boundDoc).Distinct().Count() == 1
        && ownerFacts.Select(f => f.lexicalOwnerDoc).Distinct().Count() == 2,
});

// ---- S4: the cross-project facet requires a real project-reference edge.
var libC = Build("lib", new[] { ("Lib.cs", "namespace X { public static class L { public static void W() { } } }") });
var appC = Build("app", new[] { ("App.cs", "namespace X { public static class A { public static void Run() { L.W(); } } }") }, extra: [libC.ToMetadataReference()]);
var appNoRef = Build("app-noref", new[] { ("App2.cs", "namespace X { public static class A2 { public static void Run() { } } }") });
var appTree = appC.SyntaxTrees.Single();
var appInv = appTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
var appBound = (IMethodSymbol)appC.GetSemanticModel(appTree).GetSymbolInfo(appInv).Symbol!;
var appBoundDesc = Describe(appBound, new[] { libC, appC, appNoRef })!;
scenarios.Add(new
{
    scenario = "S4 cross-project facet = defining compilation differs AND a project-reference edge exists",
    boundDefinition = appBoundDesc,
    auditedCompilation = "app",
    definingCompilation = appBoundDesc.Compilation,
    referenceEdgeRecorded = "app -> lib",
    crossProjectFacetDerived = appBoundDesc.Compilation == "lib",
    requirementSatisfied = appBoundDesc.Compilation == "lib",
});

var allSatisfied = scenarios.All(s => (bool)s.GetType().GetProperty("requirementSatisfied")!.GetValue(s)!);
var result = new
{
    probe = "CI-1-SCOPE-R3 independent identity probe",
    roslyn = typeof(CSharpCompilation).Assembly.GetName().Version?.ToString(),
    languageVersion = LanguageVersion.Latest.ToString(),
    scenarios,
    allRequirementsSatisfied = allSatisfied,
};
Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
return allSatisfied ? 0 : 1;

record Desc(string Compilation, string Assembly, string DocId, string? Tree, int? Start, int? End);
