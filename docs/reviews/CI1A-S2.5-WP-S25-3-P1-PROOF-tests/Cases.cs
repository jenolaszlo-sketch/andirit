using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using Microsoft.CodeAnalysis;

namespace P1Proof;

internal static class Cases
{
    internal static string BuildConsumer(out List<(string Path, int Line)> positions, string usingLine, params string[] calls)
    {
        var sb = new StringBuilder();
        sb.Append(usingLine).Append('\n');
        sb.Append("namespace Fixture.App;\npublic static class Consumer\n{\n    public static int Run()\n    {\n");
        positions = Extract.AppendCalls(sb, calls);
        sb.Append("        return 0;\n    }\n}\n");
        return sb.ToString();
    }

    private static (Dictionary<string, string> Files, (string Path, int Line) Span) Fixture(string core, string usingLine, params string[] calls)
    {
        var consumer = BuildConsumer(out var positions, usingLine, calls);
        var files = Extract.TwoProjectFiles(core, consumer);
        return (files, positions[0]);
    }

    // ------------------------------------------------------------------
    // Part A form cases: literal expected forms -> independent digests -> compare.
    // ------------------------------------------------------------------
    internal static async Task<JsonArray> RunFormsAsync()
    {
        var results = new JsonArray();

        async Task CaseAsync(string id, string description, Dictionary<string, string> files, (string Path, int Line) span,
            string identifierForm, string declarationForm, string shapeForm, string? note = null)
        {
            var run = await Extract.RunAsync(files, "form-" + id, id);
            var occurrence = Extract.FindBySpan(run, span.Path, span.Line);
            var entry = new JsonObject
            {
                ["id"] = id, ["description"] = description,
                ["expectedIdentifierForm"] = identifierForm,
                ["expectedDeclarationForm"] = declarationForm,
                ["expectedShapeForm"] = shapeForm,
                ["expectedIdentifierDigest"] = RefEnc.ExpectedIdentifier(identifierForm),
                ["expectedDeclarationDigest"] = RefEnc.ExpectedDeclaration(declarationForm),
                ["expectedShapeDigest"] = RefEnc.ExpectedShape(shapeForm),
                ["sourceFiles"] = Sources(files),
            };
            if (note is not null) entry["note"] = note;
            if (occurrence is null)
            {
                entry["pass"] = false;
                entry["error"] = "no occurrence fact at the designated span";
            }
            else
            {
                entry["actualTargetId"] = (string?)occurrence["targetId"];
                entry["actualIdentifierDigest"] = (string?)occurrence["identifierDigest"];
                entry["actualDeclarationDigest"] = (string?)occurrence["declarationDigest"];
                entry["actualShapeDigest"] = (string?)occurrence["shapeDigest"];
                entry["identifierMatch"] = (string?)occurrence["identifierDigest"] == RefEnc.ExpectedIdentifier(identifierForm);
                entry["declarationMatch"] = (string?)occurrence["declarationDigest"] == RefEnc.ExpectedDeclaration(declarationForm);
                entry["shapeMatch"] = (string?)occurrence["shapeDigest"] == RefEnc.ExpectedShape(shapeForm);
                entry["pass"] = (bool)entry["identifierMatch"]! && (bool)entry["declarationMatch"]! && (bool)entry["shapeMatch"]!;
            }
            results.Add(entry);
        }

        // FC-1 simple method.
        var (f1, s1) = Fixture(
            "namespace Fixture.Core;\npublic static class Logger\n{\n    public static void Write(string value) { }\n}\n",
            "using Fixture.Core;",
            "Logger.Write(\"x\");");
        await CaseAsync("FC-1", "simple static method invocation", f1, s1,
            "Write", "M:Fixture.Core.Logger.Write(System.String)", "method|0|string");

        // FC-2 same simple name, different owners (two calls in one fixture).
        var core2 = "namespace Fixture.Core;\npublic static class Alpha\n{\n    public static void Ping() { }\n}\npublic static class Beta\n{\n    public static void Ping() { }\n}\n";
        var consumer2 = BuildConsumer(out var pos2, "using Fixture.Core;", "Alpha.Ping();", "Beta.Ping();");
        var f2 = Extract.TwoProjectFiles(core2, consumer2);
        await CaseAsync("FC-2a", "same simple name, owner Alpha", f2, pos2[0], "Ping", "M:Fixture.Core.Alpha.Ping", "method|0|");
        await CaseAsync("FC-2b", "same simple name, owner Beta", f2, pos2[1], "Ping", "M:Fixture.Core.Beta.Ping", "method|0|");

        // FC-3 overload selection (int overload).
        var (f3, s3) = Fixture(
            "namespace Fixture.Core;\npublic static class Methods\n{\n    public static int Add(int a) => a;\n    public static string Add(string a) => a;\n}\n",
            "using Fixture.Core;",
            "Methods.Add(1);");
        await CaseAsync("FC-3", "overload selection Add(int)", f3, s3,
            "Add", "M:Fixture.Core.Methods.Add(System.Int32)", "method|0|int");

        // FC-4 generic method definition (inferred instantiation).
        var (f4, s4) = Fixture(
            "namespace Fixture.Core;\npublic static class Generic\n{\n    public static T Pick<T>(T value) => value;\n}\n",
            "using Fixture.Core;",
            "Generic.Pick(1);");
        await CaseAsync("FC-4", "generic method definition (inferred call)", f4, s4,
            "Pick", "M:Fixture.Core.Generic.Pick``1(``0)", "method|1|T");

        // FC-5 reduced extension method.
        var (f5, s5) = Fixture(
            "namespace Fixture.Core;\npublic static class Ext\n{\n    public static int DoubleIt(this int value) => value * 2;\n}\n",
            "using Fixture.Core;",
            "5.DoubleIt();");
        await CaseAsync("FC-5", "reduced extension method call", f5, s5,
            "DoubleIt", "M:Fixture.Core.Ext.DoubleIt(System.Int32)", "method|0|int");

        // FC-6 byref parameter (ref) -> doc-ID '@' suffix; shape shows the type only.
        var (f6, s6) = Fixture(
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Swap(ref int first) { }\n}\n",
            "using Fixture.Core;",
            "int x = 0;\n        Mods.Swap(ref x);");
        var s6line = (s6.Path, s6.Line + 1);
        await CaseAsync("FC-6", "ref parameter (doc-ID @)", f6, s6line,
            "Swap", "M:Fixture.Core.Mods.Swap(System.Int32@)", "method|0|int",
            "shape uses the parameter TYPE display; ref-kind is not part of the type.");

        // FC-7 frozen-corpus S03 target: Corpus.Core.Methods.Add(int,int).
        var corpusFiles = Corpus.Load();
        if (corpusFiles is not null)
        {
            var s03 = Corpus.FindLine(corpusFiles, "App/Sites.cs", "methods.Add(1, 2)");
            await CaseAsync("FC-7", "frozen corpus S03: Corpus.Core.Methods.Add(int,int)", corpusFiles,
                ("App/Sites.cs", s03), "Add", "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)",
                "method|0|int,int");

            var s12 = Corpus.FindLine(corpusFiles, "App/Sites.cs", "explicitGreeter.Greet(\"b\")");
            await CaseAsync("FC-8", "frozen corpus S12: interface binding through explicit implementation", corpusFiles,
                ("App/Sites.cs", s12), "Greet", "M:Corpus.Core.IGreeter.Greet(System.String)",
                "method|0|string");

            // FC-14 (P0 observation): the explicit-interface implementation
            // declaration symbol itself. Record its documentation-ID availability
            // and the section 4 fallback form. This symbol is not an occurrence
            // target in the supported surface (the call-site fact binds the
            // interface member, FC-8); the record documents the execution-time
            // fact required by the P0 qualification without inventing behavior.
            var implPrep = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Extract.BuildContext(Extract.Repo, "forms-contract", corpusFiles));
            var implCompiled = implPrep.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
            var impl = implCompiled.Compilation.GetTypeByMetadataName("Corpus.Core.ExplicitGreeter")!
                .GetMembers().OfType<IMethodSymbol>()
                .Single(m => m.MethodKind == MethodKind.ExplicitInterfaceImplementation);
            var implDocId = impl.GetDocumentationCommentId();
            var implDisplay = impl.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var implFallback = $"{impl.Kind}:{implDisplay}";
            results.Add(new JsonObject
            {
                ["id"] = "FC-14",
                ["description"] = "P0 explicit-interface observation: implementation declaration symbol (not an occurrence target)",
                ["symbolDisplay"] = implDisplay,
                ["documentationCommentId"] = implDocId ?? "(null)",
                ["selectedPath"] = implDocId is null ? "fallback (section 4 formula)" : "doc-ID-first",
                ["canonicalForm"] = implDocId ?? implFallback,
                ["fallbackForm"] = implFallback,
                ["interfaceMemberDocId"] = impl.ExplicitInterfaceImplementations.Single().GetDocumentationCommentId() ?? "(null)",
                ["sourceFiles"] = Sources(corpusFiles),
                ["pass"] = true,
                ["note"] = "observation row; the supported occurrence surface at the call site binds the interface member",
            });
        }

        // ------------------------------------------------------------------
        // FC-9..FC-13: contract section 8 shape evidence. FC-9/FC-10/FC-11
        // assemble the expected shape per the contract section 5 formula from
        // the contract-named Roslyn primitives (FullyQualifiedFormat display,
        // GetDocumentationCommentId) over a preparation-only compilation; the
        // production canonicalization and encoding are not invoked for
        // expectations (digests still come from RefEnc).
        // ------------------------------------------------------------------
        {
            const string nestedCore = "namespace Fixture.Core;\npublic static class Nested\n{\n    public static void Use(System.Collections.Generic.Dictionary<string, int> map) { }\n}\n";
            var (nestedFiles, nestedSpan) = Fixture(nestedCore, "using Fixture.Core;", "Nested.Use(new System.Collections.Generic.Dictionary<string, int>());");
            var nestedForms = await ContractFormsAsync(nestedFiles, "Fixture.Core.Nested", "Use");
            await CaseAsync("FC-9", "nested generic parameter type (internal comma)", nestedFiles, nestedSpan,
                nestedForms.Identifier, nestedForms.Declaration, nestedForms.Shape, nestedForms.Note);

            const string tupleCore = "namespace Fixture.Core;\npublic static class Tuples\n{\n    public static void Handle((int, string) pair) { }\n}\n";
            var (tupleFiles, tupleSpan) = Fixture(tupleCore, "using Fixture.Core;", "Tuples.Handle((1, \"x\"));");
            var tupleForms = await ContractFormsAsync(tupleFiles, "Fixture.Core.Tuples", "Handle");
            await CaseAsync("FC-10", "tuple parameter type (internal comma)", tupleFiles, tupleSpan,
                tupleForms.Identifier, tupleForms.Declaration, tupleForms.Shape, tupleForms.Note);

            const string pairCore = "namespace Fixture.Core;\npublic static class Pairs\n{\n    public static void Handle(int first, string second) { }\n}\n";
            var (pairFiles, pairSpan) = Fixture(pairCore, "using Fixture.Core;", "Pairs.Handle(1, \"x\");");
            await CaseAsync("FC-10b", "two-parameter near-collision partner for the tuple case", pairFiles, pairSpan,
                "Handle", "M:Fixture.Core.Pairs.Handle(System.Int32,System.String)", "method|0|int,string");

            const string partnerCore = "namespace Fixture.Core;\npublic static class Partners\n{\n    public static void Handle(System.Collections.Generic.Dictionary<string> first, int second) { }\n}\n";
            var (partnerFiles, partnerSpan) = Fixture(partnerCore, "using Fixture.Core;", "Partners.Handle(new System.Collections.Generic.Dictionary<string>(), 2);");
            var partnerForms = await ContractFormsAsync(partnerFiles, "Fixture.Core.Partners", "Handle");
            await CaseAsync("FC-11", "nested generic near-collision partner (Dictionary<string> + int)", partnerFiles, partnerSpan,
                partnerForms.Identifier, partnerForms.Declaration, partnerForms.Shape, partnerForms.Note);

            const string arrayCore = "namespace Fixture.Core;\npublic static class Arrays\n{\n    public static void Sum(int[] values) { }\n}\n";
            var (arrayFiles, arraySpan) = Fixture(arrayCore, "using Fixture.Core;", "Arrays.Sum(new int[] { 1 });");
            await CaseAsync("FC-12", "array parameter type", arrayFiles, arraySpan,
                "Sum", "M:Fixture.Core.Arrays.Sum(System.Int32[])", "method|0|int[]");
        }

        // FC-13: return-type change must not alter any component (v1 section 5:
        // the return type is intentionally omitted).
        {
            var (rtA, rtSpanA) = Fixture("namespace Fixture.Core;\npublic static class Returns\n{\n    public static int Get(int value) => value;\n}\n",
                "using Fixture.Core;", "Returns.Get(1);");
            var (rtB, rtSpanB) = Fixture("namespace Fixture.Core;\npublic static class Returns\n{\n    public static string Get(int value) => value.ToString();\n}\n",
                "using Fixture.Core;", "Returns.Get(1);");
            var runA = await Extract.RunAsync(rtA, "form-FC-13a", "FC-13a");
            var runB = await Extract.RunAsync(rtB, "form-FC-13b", "FC-13b");
            var a = Extract.FindBySpan(runA, rtSpanA.Path, rtSpanA.Line);
            var b = Extract.FindBySpan(runB, rtSpanB.Path, rtSpanB.Line);
            var entry = new JsonObject
            {
                ["id"] = "FC-13",
                ["description"] = "return-type change: identifier/declaration/shape invariant (return type intentionally omitted)",
                ["expectedIdentifierForm"] = "Get",
                ["expectedDeclarationForm"] = "M:Fixture.Core.Returns.Get(System.Int32)",
                ["expectedShapeForm"] = "method|0|int",
                ["sourceBefore"] = Sources(rtA),
                ["sourceAfter"] = Sources(rtB),
            };
            if (a is null || b is null)
            {
                entry["pass"] = false;
                entry["error"] = "missing occurrence";
            }
            else
            {
                var idEq = (string?)a["identifierDigest"] == (string?)b["identifierDigest"];
                var declEq = (string?)a["declarationDigest"] == (string?)b["declarationDigest"];
                var shapeEq = (string?)a["shapeDigest"] == (string?)b["shapeDigest"];
                entry["intReturnShapeDigest"] = (string?)a["shapeDigest"];
                entry["stringReturnShapeDigest"] = (string?)b["shapeDigest"];
                entry["identifierInvariant"] = idEq;
                entry["declarationInvariant"] = declEq;
                entry["shapeInvariant"] = shapeEq;
                entry["pass"] = idEq && declEq && shapeEq;
            }
            results.Add(entry);
        }

        // FC-DELIM: summary of the delimiter collision challenge over the real
        // Roslyn-produced displays exercised above. (Finite cases are not a
        // universal proof; see the P1 report section 8.)
        {
            var shapes = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var node in results)
                if (node is JsonObject entry && entry["id"] is JsonValue idValue)
                    shapes[(string)idValue!] = (string?)entry["expectedShapeForm"];
            var pairs = new (string First, string Second)[] { ("FC-10", "FC-10b"), ("FC-9", "FC-11") };
            var distinct = true;
            foreach (var (first, second) in pairs)
                distinct &= shapes.GetValueOrDefault(first) is { } f && shapes.GetValueOrDefault(second) is { } s && f != s;
            results.Add(new JsonObject
            {
                ["id"] = "FC-DELIM",
                ["description"] = "shape delimiter collision challenge using real Roslyn displays",
                ["tupleSingle"] = shapes.GetValueOrDefault("FC-10"),
                ["twoParamPartner"] = shapes.GetValueOrDefault("FC-10b"),
                ["nestedGenericSingle"] = shapes.GetValueOrDefault("FC-9"),
                ["nestedGenericPartner"] = shapes.GetValueOrDefault("FC-11"),
                ["allChallengedPairsDistinct"] = distinct,
                ["pass"] = distinct,
            });
        }

        return results;
    }

    // Assembled expected forms per the contract section 4/5 formulas over the
    // contract-named Roslyn primitives, without touching production canonicalization.
    private static async Task<(string Identifier, string Declaration, string Shape, string Note)> ContractFormsAsync(
        Dictionary<string, string> files, string ownerType, string memberName)
    {
        var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Extract.BuildContext(Extract.Repo, "forms-contract", files));
        var compiled = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var owner = compiled.Compilation.GetTypeByMetadataName(ownerType)!;
        var method = owner.GetMembers(memberName).OfType<IMethodSymbol>().Single();
        var displays = method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToArray();
        var shape = "method|" + method.Arity + "|" + string.Join(",", displays);
        var declaration = method.GetDocumentationCommentId() ??
            $"{method.Kind}:{method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";
        var note = "expected assembled from contract section 4/5 formulas over Roslyn primitives; displays=" + string.Join(" ;; ", displays);
        return (method.Name, declaration, shape, note);
    }

    // Raw C# fixture sources for evidence ("source before/after" preservation).
    private static JsonObject Sources(Dictionary<string, string> files)
    {
        var sources = new JsonObject();
        foreach (var (path, text) in files.OrderBy(p => p.Key, StringComparer.Ordinal))
            if (path.EndsWith(".cs", StringComparison.Ordinal))
                sources[path] = text;
        return sources;
    }

    // ------------------------------------------------------------------
    // Normative 15-row invariance/difference matrix (contract v1 section 7).
    // For each row: expected relation per v1 (id/decl/shape), observed relation,
    // pass/fail. A "relation" is "=" (equal component digests) or "!=" (distinct).
    // ------------------------------------------------------------------
    internal static async Task<JsonArray> RunMatrixAsync()
    {
        var results = new JsonArray();

        async Task<JsonObject> CompareAsync(string id, string description,
            (Dictionary<string, string> Files, (string Path, int Line) Span) before,
            (Dictionary<string, string> Files, (string Path, int Line) Span) after,
            string expId, string expDecl, string expShape)
        {
            var runBefore = await Extract.RunAsync(before.Files, "mx-" + id + "-a", id + "-a");
            var runAfter = await Extract.RunAsync(after.Files, "mx-" + id + "-b", id + "-b");
            var a = Extract.FindBySpan(runBefore, before.Span.Path, before.Span.Line);
            var b = Extract.FindBySpan(runAfter, after.Span.Path, after.Span.Line);
            var entry = new JsonObject
            {
                ["id"] = id, ["description"] = description,
                ["sourceBefore"] = Sources(before.Files),
                ["sourceAfter"] = Sources(after.Files),
            };
            if (a is null || b is null)
            {
                entry["pass"] = false;
                entry["error"] = $"missing occurrence (before={(a is null ? "null" : "ok")}, after={(b is null ? "null" : "ok")})";
                results.Add(entry);
                return entry;
            }
            var relId = (string?)a["identifierDigest"] == (string?)b["identifierDigest"] ? "=" : "!=";
            var relDecl = (string?)a["declarationDigest"] == (string?)b["declarationDigest"] ? "=" : "!=";
            var relShape = (string?)a["shapeDigest"] == (string?)b["shapeDigest"] ? "=" : "!=";
            entry["beforeIdentifierDigest"] = (string?)a["identifierDigest"];
            entry["afterIdentifierDigest"] = (string?)b["identifierDigest"];
            entry["beforeDeclarationDigest"] = (string?)a["declarationDigest"];
            entry["afterDeclarationDigest"] = (string?)b["declarationDigest"];
            entry["beforeShapeDigest"] = (string?)a["shapeDigest"];
            entry["afterShapeDigest"] = (string?)b["shapeDigest"];
            entry["v1Identifier"] = expId; entry["observedIdentifier"] = relId;
            entry["v1Declaration"] = expDecl; entry["observedDeclaration"] = relDecl;
            entry["v1Shape"] = expShape; entry["observedShape"] = relShape;
            entry["identifierPass"] = expId == relId;
            entry["declarationPass"] = expDecl == relDecl;
            entry["shapePass"] = expShape == relShape;
            entry["pass"] = (bool)entry["identifierPass"]! && (bool)entry["declarationPass"]! && (bool)entry["shapePass"]!;
            results.Add(entry);
            return entry;
        }

        // Row 1: formatting.
        var (r1a, r1s) = Fixture("namespace Fixture.Core;\npublic static class Formatting\n{\n    public static void Target1() { }\n}\n", "using Fixture.Core;", "Formatting.Target1();");
        var (r1b, r1sb) = Fixture("namespace Fixture.Core;\npublic static class Formatting\n{\n    public static void Target1(\n        )\n    {\n    }\n}\n", "using Fixture.Core;", "Formatting.Target1();");
        await CompareAsync("R1", "whitespace/formatting of the target declaration", (r1a, r1s), (r1b, r1sb), "=", "=", "=");

        // Row 2: comments/trivia.
        var (r2a, r2s) = Fixture("namespace Fixture.Core;\npublic static class Comments\n{\n    public static void Target2() { }\n}\n", "using Fixture.Core;", "Comments.Target2();");
        var (r2b, r2sb) = Fixture("namespace Fixture.Core;\n// leading comment\npublic static class Comments\n{\n    /* block */\n    public static void Target2() { /* inline */ }\n}\n", "using Fixture.Core;", "Comments.Target2();");
        await CompareAsync("R2", "comments/trivia", (r2a, r2s), (r2b, r2sb), "=", "=", "=");

        // Row 3: declaration relocation (target file path changes).
        var core3a = "namespace Fixture.Core;\npublic static class Relocated\n{\n    public static void Target3() { }\n}\n";
        var r3a = Extract.TwoProjectFiles(core3a, BuildConsumer(out var r3sp, "using Fixture.Core;", "Relocated.Target3();"));
        var r3b = Extract.TwoProjectFiles(core3a, BuildConsumer(out var r3spb, "using Fixture.Core;", "Relocated.Target3();"));
        r3b.Remove("Core/Targets.cs");
        r3b["Core/Nested/Targets.cs"] = core3a;
        await CompareAsync("R3", "declaration relocation (file path change)", (r3a, r3sp[0]), (r3b, r3spb[0]), "=", "=", "=");

        // Row 4: namespace-qualification style at the call site.
        var core4 = "namespace Fixture.Core;\npublic static class Logger\n{\n    public static void Target4(string value) { }\n}\n";
        var (r4a, r4s) = Fixture(core4, "using Fixture.Core;", "Logger.Target4(\"x\");");
        var (r4b, r4sb) = Fixture(core4, "// fully qualified call", "Fixture.Core.Logger.Target4(\"x\");");
        await CompareAsync("R4", "namespace-qualification style", (r4a, r4s), (r4b, r4sb), "=", "=", "=");

        // Row 5: type alias at the call site.
        var (r5a, r5s) = Fixture(core4, "using Fixture.Core;", "Logger.Target4(\"x\");");
        var (r5b, r5sb) = Fixture(core4, "using L = Fixture.Core.Logger;", "L.Target4(\"x\");");
        await CompareAsync("R5", "type alias at the call site", (r5a, r5s), (r5b, r5sb), "=", "=", "=");

        // Row 6a: unrelated overload addition leaves the existing target invariant.
        var core6 = "namespace Fixture.Core;\npublic static class Methods\n{\n    public static int Add(int a) => a;\n}\n";
        var core6b = "namespace Fixture.Core;\npublic static class Methods\n{\n    public static int Add(int a) => a;\n    public static string Add(string a) => a;\n}\n";
        var (r6a, r6s) = Fixture(core6, "using Fixture.Core;", "Methods.Add(1);");
        var (r6b, r6sb) = Fixture(core6b, "using Fixture.Core;", "Methods.Add(1);");
        await CompareAsync("R6a", "unrelated overload addition (existing target invariant)", (r6a, r6s), (r6b, r6sb), "=", "=", "=");

        // Row 6b: retargeted overload.
        var (r6c, r6sc) = Fixture(core6b, "using Fixture.Core;", "Methods.Add(1);");
        var (r6d, r6sd) = Fixture(core6b, "using Fixture.Core;", "Methods.Add(\"a\");");
        await CompareAsync("R6b", "retargeted overload Add(int) -> Add(string)", (r6c, r6sc), (r6d, r6sd), "=", "!=", "!=");

        // Row 7: rename.
        var (r7a, r7s) = Fixture("namespace Fixture.Core;\npublic static class Rename\n{\n    public static void Before7(int value) { }\n}\n", "using Fixture.Core;", "Rename.Before7(1);");
        var (r7b, r7sb) = Fixture("namespace Fixture.Core;\npublic static class Rename\n{\n    public static void After7(int value) { }\n}\n", "using Fixture.Core;", "Rename.After7(1);");
        await CompareAsync("R7", "method rename", (r7a, r7s), (r7b, r7sb), "!=", "!=", "=");

        // Row 8: parameter-type change.
        var (r8a, r8s) = Fixture("namespace Fixture.Core;\npublic static class Take\n{\n    public static void Take8(int value) { }\n}\n", "using Fixture.Core;", "Take.Take8(1);");
        var (r8b, r8sb) = Fixture("namespace Fixture.Core;\npublic static class Take\n{\n    public static void Take8(string value) { }\n}\n", "using Fixture.Core;", "Take.Take8(\"x\");");
        await CompareAsync("R8", "parameter-type change (int -> string)", (r8a, r8s), (r8b, r8sb), "=", "!=", "!=");

        // Row 9: parameter-order change.
        var (r9a, r9s) = Fixture("namespace Fixture.Core;\npublic static class Two\n{\n    public static void Two9(int a, string b) { }\n}\n", "using Fixture.Core;", "Two.Two9(1, \"x\");");
        var (r9b, r9sb) = Fixture("namespace Fixture.Core;\npublic static class Two\n{\n    public static void Two9(string b, int a) { }\n}\n", "using Fixture.Core;", "Two.Two9(\"x\", 1);");
        await CompareAsync("R9", "parameter-order change", (r9a, r9s), (r9b, r9sb), "=", "!=", "!=");

        // Row 10: generic arity change.
        var (r10a, r10s) = Fixture("namespace Fixture.Core;\npublic static class Arity\n{\n    public static void Pick10(int value) { }\n}\n", "using Fixture.Core;", "Arity.Pick10(1);");
        var (r10b, r10sb) = Fixture("namespace Fixture.Core;\npublic static class Arity\n{\n    public static void Pick10<T>(T value) { }\n}\n", "using Fixture.Core;", "Arity.Pick10(2);");
        await CompareAsync("R10", "generic arity change (non-generic -> <T>)", (r10a, r10s), (r10b, r10sb), "=", "!=", "!=");

        // Row 11: generic substitution (two constructed receivers, same definition).
        var core11 = "namespace Fixture.Core;\npublic sealed class Box<T>\n{\n    public T Get() => default!;\n}\n";
        var consumer11 = BuildConsumer(out var pos11, "using Fixture.Core;", "new Box<int>().Get();", "new Box<string>().Get();");
        var f11 = Extract.TwoProjectFiles(core11, consumer11);
        await CompareAsync("R11", "generic substitution (Box<int> vs Box<string>)", (f11, pos11[0]), (f11, pos11[1]), "=", "=", "=");

        // Row 12: reduced extension vs static form.
        var core12 = "namespace Fixture.Core;\npublic static class Ext12\n{\n    public static int Double12(this int value) => value * 2;\n}\n";
        var consumer12 = BuildConsumer(out var pos12, "using Fixture.Core;", "5.Double12();", "Ext12.Double12(5);");
        var f12 = Extract.TwoProjectFiles(core12, consumer12);
        await CompareAsync("R12", "reduced extension call vs static form", (f12, pos12[0]), (f12, pos12[1]), "=", "=", "=");

        // Row 13a: value vs ref.
        var (r13a, r13s) = Fixture("namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Change13(int value) { }\n}\n", "using Fixture.Core;", "Mods.Change13(1);");
        var (r13b, r13sb) = Fixture("namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Change13(ref int value) { }\n}\n", "using Fixture.Core;", "int v = 0;\n        Mods.Change13(ref v);");
        await CompareAsync("R13a", "value vs ref parameter", (r13a, r13s), (r13b, (r13sb.Path, r13sb.Line + 1)), "=", "!=", "!=");

        // Row 13b: ref vs out.
        var (r13c, r13sc) = Fixture("namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Change13B(ref int value) { }\n}\n", "using Fixture.Core;", "int v = 0;\n        Mods.Change13B(ref v);");
        var (r13d, r13sd) = Fixture("namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Change13B(out int value) { value = 0; }\n}\n", "using Fixture.Core;", "int v;\n        Mods.Change13B(out v);");
        await CompareAsync("R13b", "ref vs out parameter", (r13c, (r13sc.Path, r13sc.Line + 1)), (r13d, (r13sd.Path, r13sd.Line + 1)), "=", "!=", "!=");

        // Row 13c: ref vs in.
        var (r13e, r13se) = Fixture("namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Change13C(ref int value) { }\n}\n", "using Fixture.Core;", "int v = 0;\n        Mods.Change13C(ref v);");
        var (r13f, r13sf) = Fixture("namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Change13C(in int value) { }\n}\n", "using Fixture.Core;", "int v = 0;\n        Mods.Change13C(in v);");
        await CompareAsync("R13c", "ref vs in parameter", (r13e, (r13se.Path, r13se.Line + 1)), (r13f, (r13sf.Path, r13sf.Line + 1)), "=", "!=", "!=");

        // Row 14: same simple name, different owner.
        var core14 = "namespace Fixture.Core;\npublic static class OwnerA\n{\n    public static void Ping14() { }\n}\npublic static class OwnerB\n{\n    public static void Ping14() { }\n}\n";
        var consumer14 = BuildConsumer(out var pos14, "using Fixture.Core;", "OwnerA.Ping14();", "OwnerB.Ping14();");
        var f14 = Extract.TwoProjectFiles(core14, consumer14);
        await CompareAsync("R14", "same simple name, different owner", (f14, pos14[0]), (f14, pos14[1]), "=", "!=", "=");

        // Row 15: unresolved semantic binding (external target) - refusal, no fact.
        var core15 = "namespace Fixture.Core;\npublic static class Known\n{\n    public static void Ok15() { }\n}\n";
        var consumer15 = BuildConsumer(out var pos15, "using Fixture.Core;", "System.Console.WriteLine(\"x\");", "Known.Ok15();");
        var f15 = Extract.TwoProjectFiles(core15, consumer15);
        var run15 = await Extract.RunAsync(f15, "mx-R15", "R15");
        var externalFact = Extract.FindBySpan(run15, pos15[0].Path, pos15[0].Line);
        var knownFact = Extract.FindBySpan(run15, pos15[1].Path, pos15[1].Line);
        var row15 = new JsonObject
        {
            ["id"] = "R15",
            ["description"] = "unresolved/external binding refused; no fact",
            ["v1Identifier"] = "refusal", ["v1Declaration"] = "refusal", ["v1Shape"] = "refusal",
            ["observedIdentifier"] = externalFact is null ? "refusal" : "fact-emitted",
            ["observedDeclaration"] = externalFact is null ? "refusal" : "fact-emitted",
            ["observedShape"] = externalFact is null ? "refusal" : "fact-emitted",
            ["knownControlFactPresent"] = knownFact is not null,
            ["sourceFiles"] = Sources(f15),
            ["pass"] = externalFact is null && knownFact is not null,
        };
        results.Add(row15);

        return results;
    }
}

// Frozen R2 corpus loader — the publishable subset established by the accepted
// FINAL-Q1 real-source evidence: App, Core, Leaf, Solo with their eight sources.
// The frozen corpus additionally declares Negatives (adversarial, intentionally
// non-compiling refusal scenarios) and External/ExternalLib.cs (compiled by the
// frozen oracle to an in-memory metadata image; explicitly "not a declared
// project unit of the corpus universe"). Neither is a publishable project unit:
// CSharpProjectDiscovery claims sources by project directory and unclaimed
// sources do not stage index units, so both remain oracle-side artifacts.
internal static class Corpus
{
    internal static readonly string[] ProjectFiles =
    [
        "App/App.csproj", "Core/Core.csproj", "Leaf/Leaf.csproj", "Solo/Solo.csproj"
    ];
    internal static readonly string[] SourceFiles =
    [
        "App/Bulk.cs", "App/Sites.cs", "Core/Extensions.cs", "Core/LeafUser.cs", "Core/Methods.cs", "Core/Types.cs",
        "Leaf/Helper.cs", "Solo/Declarations.cs"
    ];

    internal static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Andirit.slnx"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }

    internal static Dictionary<string, string>? Load()
    {
        var root = Path.Combine(Root(), "docs", "planning", "adr-ci1-01-ci1a-s2-0", "corpus");
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in ProjectFiles.Concat(SourceFiles))
        {
            var full = Path.Combine(root, "repo", path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full)) return null;
            files[path] = File.ReadAllText(full);
        }
        return files;
    }

    internal static int FindLine(Dictionary<string, string> files, string path, string marker)
    {
        var lines = files[path].Split('\n');
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Contains(marker, StringComparison.Ordinal)) return i + 1;
        throw new InvalidOperationException($"marker not found: {marker}");
    }
}

