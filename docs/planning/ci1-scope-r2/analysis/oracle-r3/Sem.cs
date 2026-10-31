// ============================================================================
// CI-1-SCOPE-R3 oracle — independently derived semantic facts.
// Every fact used by the audit is computed here from raw fixture bytes and
// Roslyn bindings; nothing is taken from the manifest's editable fields.
// ============================================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CI1ScopeOracleR3;

public static class Sem
{
    public static string? Doc(ISymbol? s)
    {
        if (s is null) return null;
        var norm = s is IMethodSymbol m ? (m.ReducedFrom ?? m) : s;
        return norm.OriginalDefinition.GetDocumentationCommentId() ?? s.OriginalDefinition.GetDocumentationCommentId();
    }

    public static ISymbol Def(ISymbol s)
    {
        var norm = s is IMethodSymbol m ? (m.ReducedFrom ?? m) : s;
        return norm.OriginalDefinition;
    }

    // R2-compatible eligibility (set membership): containing assembly identity is
    // one of the source-built compilations AND the original definition has a
    // source location. Kept for continuity evidence; NOT sufficient for identity
    // in R3 (see DescribeDefinition / exact-identity check).
    public static bool EligibleLocal(ISymbol? target, IEnumerable<CSharpCompilation> eligibleSet)
    {
        if (target is null) return false;
        var def = Def(target);
        var asm = def.ContainingAssembly;
        if (asm is null) return false;
        var identity = asm.Identity;
        if (!eligibleSet.Any(c => c.Assembly.Identity.Equals(identity))) return false;
        return def.Locations.Any(l => l.IsInSource && l.SourceTree is not null);
    }

    // Exact-definition descriptor of the bound target's original definition.
    // Returns a "metadata" descriptor when the definition has no fixture source
    // location (platform/external definitions).
    public static DefDescriptor? DescribeDefinition(ISymbol? target, OracleEnvironment env)
    {
        if (target is null) return null;
        var def = Def(target);
        var asm = def.ContainingAssembly;
        if (asm is null) return null;
        var doc = Doc(def) ?? "";
        var srcLoc = def.Locations.FirstOrDefault(l => l.IsInSource && l.SourceTree is not null);
        if (srcLoc is null)
            return new DefDescriptor("metadata", asm.Identity.ToString(), doc, null, null, null);
        var tree = srcLoc.SourceTree!;
        var compName = env.SourceCompilations.FirstOrDefault(c => c.SyntaxTrees.Contains(tree))?.AssemblyName ?? "unknown";
        return new DefDescriptor(compName, asm.Identity.ToString(), doc, tree.FilePath, srcLoc.SourceSpan.Start, srcLoc.SourceSpan.End);
    }

    public static bool DescriptorEquals(DefDescriptor a, DefDescriptor b)
    {
        if (!string.Equals(a.DocId, b.DocId, StringComparison.Ordinal)) return false;
        if (!string.Equals(a.Compilation, b.Compilation, StringComparison.Ordinal)) return false;
        if (!string.Equals(a.AssemblyIdentity, b.AssemblyIdentity, StringComparison.Ordinal)) return false;
        if (!string.Equals(a.TreePath, b.TreePath, StringComparison.Ordinal)) return false;
        if (a.DeclStart != b.DeclStart || a.DeclEnd != b.DeclEnd) return false;
        return true;
    }

    // Resolve the unique declared symbol with the given documentation ID inside
    // one compilation. Returned list holds DISTINCT declarations (tree, span).
    public static List<ISymbol> ResolveDeclarationsByDoc(CSharpCompilation comp, string docId)
    {
        var found = new List<(ISymbol Sym, string Tree, int Start, int End)>();
        foreach (var tree in comp.SyntaxTrees)
        {
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            foreach (var n in root.DescendantNodes())
            {
                ISymbol? ds = n switch
                {
                    MethodDeclarationSyntax md => model.GetDeclaredSymbol(md),
                    ConstructorDeclarationSyntax cd => model.GetDeclaredSymbol(cd),
                    ClassDeclarationSyntax cl => model.GetDeclaredSymbol(cl),
                    InterfaceDeclarationSyntax it => model.GetDeclaredSymbol(it),
                    StructDeclarationSyntax st => model.GetDeclaredSymbol(st),
                    RecordDeclarationSyntax rd => model.GetDeclaredSymbol(rd),
                    DelegateDeclarationSyntax dd => model.GetDeclaredSymbol(dd),
                    _ => null,
                };
                if (ds is null) continue;
                var d = Doc(ds);
                if (d != docId) continue;
                var span = ds.Locations.FirstOrDefault(l => l.IsInSource && l.SourceTree is not null)?.SourceSpan ?? default;
                found.Add((ds.OriginalDefinition, tree.FilePath, span.Start, span.End));
            }
        }
        return found
            .GroupBy(f => (f.Tree, f.Start, f.End))
            .Select(g => g.First().Sym)
            .ToList();
    }

    public static bool AnySourceDeclarationWithDoc(IEnumerable<CSharpCompilation> comps, string docId)
        => comps.Any(c => ResolveDeclarationsByDoc(c, docId).Count > 0);

    // The single static-dispatch characteristic of a uniquely bound invocation,
    // derived from the binding and the syntax. Precedence: interface member,
    // base receiver, delegate invoke, virtual member, plain.
    public static DispatchFacet DeriveDispatch(ISymbol? target, SyntaxNode node)
    {
        if (target is IMethodSymbol m)
        {
            var norm = (m.ReducedFrom ?? m).OriginalDefinition;
            if (norm.ContainingType?.TypeKind == TypeKind.Interface)
                return DispatchFacet.InterfaceMember;
            if (node is InvocationExpressionSyntax inv &&
                inv.Expression is MemberAccessExpressionSyntax ma &&
                ma.Expression is BaseExpressionSyntax)
                return DispatchFacet.BaseReceiver;
            if (norm.ContainingType?.TypeKind == TypeKind.Delegate)
                return DispatchFacet.DelegateInvoke;
            if (norm is IMethodSymbol vm && (vm.IsVirtual || vm.IsOverride))
                return DispatchFacet.VirtualMember;
            return DispatchFacet.Plain;
        }
        if (target?.ContainingType?.TypeKind == TypeKind.Delegate)
            return DispatchFacet.DelegateInvoke;
        return DispatchFacet.Plain;
    }

    // Conditional-compilation activity: does a syntax node exist at exactly this
    // span when the fixture is parsed with the given preprocessor symbols?
    // This is inspected for EVERY site, independently of the declared label.
    public static bool ActiveAt(OracleEnvironment env, string fixturePath, int start, int end, string[] defines)
    {
        var (_, text) = env.ReadFixture(fixturePath);
        var parse = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: defines);
        var tree = CSharpSyntaxTree.ParseText(text, parse, fixturePath);
        var root = tree.GetRoot();
        var node = root.DescendantNodes().Where(n => n.Span.Start == start && n.Span.End == end)
            .OrderBy(n => n is InvocationExpressionSyntax ? 0 : 1).FirstOrDefault();
        if (node is null)
            node = root.DescendantNodes().OfType<ConstructorInitializerSyntax>()
                .SingleOrDefault(n => n.Span.Contains(start) && n.Span.End == end);
        return node is not null;
    }

    // Lexical owner: nearest enclosing method-like declaration (lexical
    // containment — explicitly NOT runtime caller attribution).
    public static string? LexicalOwner(SemanticModel model, SyntaxNode? node)
    {
        var ownerNode = node?.Ancestors().FirstOrDefault(n => n is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax);
        if (ownerNode is null) return null;
        var sym = model.GetDeclaredSymbol(ownerNode);
        if (sym is null)
        {
            // Constructor initializers attach to the constructor declaration.
            if (ownerNode is ConstructorDeclarationSyntax cd) sym = model.GetDeclaredSymbol(cd);
        }
        return sym is null ? null : Doc(sym);
    }

    public static string RowKey(string cs, string path, int[] span)
        => $"{cs}|{path}|{span[0]},{span[1]},{span[2]},{span[3]}";
}
