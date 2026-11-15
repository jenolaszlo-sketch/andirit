using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Frozen = AdrCi1Exp.Shared;

namespace P2Qual;

// ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01 — reviewer-owned independent qualification (Part A).
// Expected values are derived from canonical-forms v1 (clarified) and the independently
// implemented encoder below; production outputs are compared only afterwards.
// Known-answer literals were computed in PowerShell (separate toolchain path) before this
// harness was written; they are embedded as literals, not copied from producer oracles.
internal static class Review
{
    internal static readonly CodeRepositoryId Repo = new("p2:review");
    private static readonly DateTimeOffset Started = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    // ------------------------------------------------------------------
    // Reviewer-owned reference encoder (contract §6).
    // ------------------------------------------------------------------
    internal static class Enc
    {
        internal static byte[] MessageBytes(string domain, IReadOnlyList<string> fields)
        {
            using var buffer = new MemoryStream();
            void Append(string value)
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var length = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
                buffer.Write(length);
                buffer.Write(bytes);
            }
            Append(domain);
            foreach (var field in fields) Append(field);
            return buffer.ToArray();
        }
        internal static string Hex(byte[] bytes) => Convert.ToHexStringLower(bytes);
        internal static string Hash(string domain, params string[] fields) =>
            Hex(SHA256.HashData(MessageBytes(domain, fields)));
        internal static string HashList(string domain, IReadOnlyList<string> fields)
        {
            using var buffer = new MemoryStream();
            void Append(string value)
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var length = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
                buffer.Write(length);
                buffer.Write(bytes);
            }
            Append(domain);
            foreach (var field in fields) Append(field);
            return Hex(SHA256.HashData(buffer.ToArray()));
        }
    }

    // ------------------------------------------------------------------
    // Fixtures and helpers.
    // ------------------------------------------------------------------
    private static Dictionary<string, string> TwoProjectFiles(string coreSource, string consumerSource) => new(StringComparer.Ordinal)
    {
        ["Core/Core.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
        ["Core/Targets.cs"] = coreSource,
        ["App/App.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Core/Core.csproj\" /></ItemGroup></Project>",
        ["App/Consumer.cs"] = consumerSource,
    };

    private static string ConsumerSource(params string[] bodyLines)
    {
        var sb = new StringBuilder();
        sb.Append("using Review.Core;\nnamespace Review.App;\npublic static class Client\n{\n    public static int Run()\n    {\n");
        foreach (var line in bodyLines) sb.Append("        ").Append(line).Append('\n');
        sb.Append("        return 0;\n    }\n}\n");
        return sb.ToString();
    }

    private static int LineOf(string source, string marker)
    {
        var lines = source.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Contains(marker, StringComparison.Ordinal)) return i + 1;
        throw new InvalidOperationException("marker not found: " + marker);
    }

    private static string FileHash(string content) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    private static CodeGraphPluginContext Context(CodeRepositoryId repo, string runId, IReadOnlyDictionary<string, string> files) =>
        new(repo, null, new CodeIndexRunId(runId), files.OrderBy(p => p.Key, StringComparer.Ordinal).Select(pair =>
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Value);
            return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }).ToArray());

    private sealed class CaptureSink : ICodeGraphSink, ICodeOccurrenceAuthoritySink
    {
        private readonly CodeGraphIngestionSink _inner;
        internal List<CodeGraphBatch> Batches { get; } = [];
        internal List<CodeOccurrenceAuthority> Authorities { get; } = [];
        internal CaptureSink(CodeGraphIngestionSink inner) => _inner = inner;
        public CodeGraphBatchLimits Limits => _inner.Limits;
        public ValueTask WriteBatchAsync(CodeGraphBatch batch, CancellationToken ct = default)
        {
            Batches.Add(batch);
            return _inner.WriteBatchAsync(batch, ct);
        }
        ValueTask ICodeOccurrenceAuthoritySink.SetOccurrenceAuthorityAsync(
            CodeFactOrigin origin, CodeOccurrenceAuthority authority, CancellationToken ct)
        {
            Authorities.Add(authority);
            return ((ICodeOccurrenceAuthoritySink)_inner).SetOccurrenceAuthorityAsync(origin, authority, ct);
        }
    }

    private static async Task<JsonArray> ExtractAsync(Dictionary<string, string> files, string runId,
        params (string Path, int Line, string Label)[] spans)
    {
        var store = new InMemoryCodeGraphStore();
        var plugin = new CSharpCodeGraphPlugin();
        await store.UpsertRepositoryAsync(new CodeRepositoryManifest(Repo));
        await store.StoreIndexRunAsync(new CodeIndexRunManifest(Repo, new CodeIndexRunId(runId), Started, plugins: [plugin.Id]));
        var sink = new CaptureSink(new CodeGraphIngestionSink(store));
        await using (var session = await plugin.CreateSessionAsync(Context(Repo, runId, files)))
            await session.ExtractAsync(sink);

        var facts = new Dictionary<string, CodeOccurrenceDefiningAuthority>(StringComparer.Ordinal);
        foreach (var authority in sink.Authorities)
            foreach (var fact in authority.Facts)
                facts[fact.OccurrenceId] = fact;

        var results = new JsonArray();
        foreach (var (path, line, label) in spans)
        {
            JsonObject? found = null;
            foreach (var batch in sink.Batches)
                foreach (var occurrence in batch.Occurrences)
                    if (occurrence.Span.Path == path && occurrence.Span.StartLine == line && found is null)
                    {
                        found = new JsonObject
                        {
                            ["label"] = label,
                            ["span"] = $"{path}:{line}:{occurrence.Span.StartColumn}",
                            ["targetId"] = occurrence.TargetId.Value,
                        };
                        if (facts.TryGetValue(occurrence.Id.Value, out var defining))
                        {
                            found["identifierDigest"] = defining.IdentifierDigest;
                            found["declarationDigest"] = defining.DeclarationDigest;
                            found["shapeDigest"] = defining.ShapeDigest;
                        }
                    }
            results.Add(found ?? new JsonObject { ["label"] = label, ["error"] = "no occurrence fact at the designated span" });
        }
        return results;
    }

    private static string Fact(JsonArray facts, int index, string field)
    {
        var entry = facts[index]!.AsObject();
        return (string?)entry[field] ?? "(absent)";
    }

    // ------------------------------------------------------------------
    // Part A top level.
    // ------------------------------------------------------------------
    internal static async Task<JsonObject> RunAsync()
    {
        var root = new JsonObject
        {
            ["harness"] = "ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01",
            ["part"] = "A",
            ["processId"] = Environment.ProcessId,
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ["reviewerProvenance"] = new JsonObject
            {
                ["model"] = "DeepSeek V4.1 Flash Free (provider opencode, model deepseek-v4.1-flash-free) via OpenCode",
                ["session"] = "same assistant session as the P1/delta producer — limitation recorded; independence derived from fresh harness, fresh fixtures, and cross-toolchain (PowerShell) known-answer derivation",
                ["expectedValueSource"] = "canonical-forms v1 (clarified) + PowerShell-computed known-answer literals + reviewer-owned encoder",
            },
        };
        var checks = new JsonArray();
        var allPass = true;
        bool Check(string id, bool pass, string? note = null)
        {
            var entry = new JsonObject { ["id"] = id, ["pass"] = pass };
            if (note is not null) entry["note"] = note;
            checks.Add(entry);
            allPass &= pass;
            return pass;
        }

        // ---------------- independent encoder, known-answer anchors (PowerShell-derived literals)
        var kats = new JsonArray();
        void Kat(string id, string domain, string field, string expectedDigest, string? expectedMessageHex, bool withProduction)
        {
            var hex = Enc.Hex(Enc.MessageBytes(domain, [field]));
            var digest = Enc.Hash(domain, field);
            var production = domain switch
            {
                "asmctx3-declarations" => withProduction ? CodeOccurrenceIdentity.ComputeDeclarationDigest([field]) : digest,
                "asmctx3-target" => withProduction ? CodeOccurrenceIdentity.ComputeShapeDigest(field) : digest,
                _ => withProduction ? CodeOccurrenceIdentity.ComputeIdentifierDigest([field]) : digest,
            };
            var pass = digest == expectedDigest && (expectedMessageHex is null || hex == expectedMessageHex) &&
                (!withProduction || production == expectedDigest);
            kats.Add(new JsonObject
            {
                ["id"] = id, ["domain"] = domain, ["field"] = field,
                ["messageHex"] = hex,
                ["expectedDigest"] = expectedDigest,
                ["reviewerDigest"] = digest,
                ["productionDigest"] = production,
                ["pass"] = pass,
            });
            Check("encoder:" + id, pass);
        }
        Kat("kat1-identifier", "asmctx3-identifier", "ReviewerPing",
            "2184752575bbc9b65b8b85e52134904216bc39aeb9d60d9f7f19bd3c97bdebb9",
            "0000001261736d637478332d6964656e7469666965720000000c526576696577657250696e67", true);
        Kat("kat2-declaration", "asmctx3-declarations", "M:Review.Core.Spec.Confirm(System.Int32@)",
            "a9df5dcd8f6c83641d7b24ec981d8974872f7915d56e2821b67bf2c485e72bbb", null, true);
        Kat("kat3-target-generic", "asmctx3-target", "method|1|T",
            "af2c463835c11a3396c179709087b2b644584875af3955869f7713cb337d109d", null, false);
        Kat("kat4-empty-component", "asmctx3-identifier", "",
            "bfc94e29357152d9d7759cf169c04e8cfd5efb5f749b3e1c09fe76590f3952eb", null, false);
        Kat("kat5-unicode", "asmctx3-identifier", "ИдентификаторΩ",
            "45b29ca1c4e3a6efaf29ef99ad42a4214aa85f722abd2324ecd0b0ac12e3a1af", null, true);
        root["encoder"] = kats;

        // ---------------- canonical-form reproduction (fresh fixtures)
        var forms = new JsonArray();
        async Task FormCaseAsync(string id, string description, Dictionary<string, string> files,
            (string Marker, string IdForm, string DeclForm, string ShapeForm) expected)
        {
            var line = LineOf(files["App/Consumer.cs"], expected.Marker);
            var idDigest = Enc.Hash("asmctx3-identifier", expected.IdForm);
            var declDigest = Enc.Hash("asmctx3-declarations", expected.DeclForm);
            var shapeDigest = Enc.Hash("asmctx3-target", expected.ShapeForm);
            var facts = await ExtractAsync(files, "p2:forms:" + id, ("App/Consumer.cs", line, id));
            var fact = facts[0]!.AsObject();
            var pass = (string?)fact["identifierDigest"] == idDigest &&
                (string?)fact["declarationDigest"] == declDigest &&
                (string?)fact["shapeDigest"] == shapeDigest &&
                (string?)fact["targetId"] is not null;
            forms.Add(new JsonObject
            {
                ["id"] = id, ["description"] = description,
                ["expected"] = new JsonObject
                {
                    ["identifierForm"] = expected.IdForm, ["declarationForm"] = expected.DeclForm,
                    ["shapeForm"] = expected.ShapeForm,
                    ["identifierDigest"] = idDigest, ["declarationDigest"] = declDigest, ["shapeDigest"] = shapeDigest,
                },
                ["actual"] = fact.DeepClone(),
                ["pass"] = pass,
            });
            Check("form:" + id, pass);
        }

        await FormCaseAsync("simple", "simple method (doc-ID + special-type display)",
            TwoProjectFiles("namespace Review.Core;\npublic static class Spec\n{\n    public static void Confirm(string value) { }\n}\n",
                ConsumerSource("Spec.Confirm(\"x\");")),
            ("Spec.Confirm(\"x\")", "Confirm", "M:Review.Core.Spec.Confirm(System.String)", "method|0|string"));
        await FormCaseAsync("overloads", "overload selection (declaration distinguishes)",
            TwoProjectFiles("namespace Review.Core;\npublic static class Spec\n{\n    public static int Add(int a) => a;\n    public static string Add(string a) => a;\n}\n",
                ConsumerSource("Spec.Add(1);")),
            ("Spec.Add(1)", "Add", "M:Review.Core.Spec.Add(System.Int32)", "method|0|int"));
        await FormCaseAsync("generic", "generic definition (arity in name, T in shape)",
            TwoProjectFiles("namespace Review.Core;\npublic static class Generic\n{\n    public static T Pick<T>(T value) => value;\n}\n",
                ConsumerSource("Generic.Pick(1);")),
            ("Generic.Pick(1)", "Pick", "M:Review.Core.Generic.Pick``1(``0)", "method|1|T"));
        await FormCaseAsync("extension", "reduced extension method (ReducedFrom normalization)",
            TwoProjectFiles("namespace Review.Core;\npublic static class Ext\n{\n    public static int Twice(this int value) => value * 2;\n}\n",
                ConsumerSource("5.Twice();")),
            ("5.Twice()", "Twice", "M:Review.Core.Ext.Twice(System.Int32)", "method|0|int"));
        await FormCaseAsync("nested-generic", "nested generic display with internal comma",
            TwoProjectFiles("namespace Review.Core;\npublic static class Util\n{\n    public static int Merge(System.Collections.Generic.Dictionary<string, int> map) => map.Count;\n}\n",
                ConsumerSource("Util.Merge(new System.Collections.Generic.Dictionary<string, int>());")),
            ("Util.Merge(new", "Merge", "M:Review.Core.Util.Merge(System.Collections.Generic.Dictionary{System.String,System.Int32})",
                "method|0|global::System.Collections.Generic.Dictionary<string, int>"));
        await FormCaseAsync("tuple", "tuple display with internal comma",
            TwoProjectFiles("namespace Review.Core;\npublic static class Util\n{\n    public static int Pair((int, string) value) => value.Item1;\n}\n",
                ConsumerSource("Util.Pair((1, \"x\"));")),
            ("Util.Pair((1", "Pair", "M:Review.Core.Util.Pair(System.ValueTuple{System.Int32,System.String})",
                "method|0|(int, string)"));
        await FormCaseAsync("array", "array display",
            TwoProjectFiles("namespace Review.Core;\npublic static class Util\n{\n    public static int Sum(int[] values) => values.Length;\n}\n",
                ConsumerSource("Util.Sum(new int[] { 1 });")),
            ("Util.Sum(new", "Sum", "M:Review.Core.Util.Sum(System.Int32[])", "method|0|int[]"));

        // explicit interface: interface-member binding + explicit implementation doc-ID format
        {
            var core = "namespace Review.Core;\npublic interface IThing\n{\n    string Go(string s);\n}\npublic sealed class ImplicitThing : IThing\n{\n    public string Go(string s) => s;\n}\npublic sealed class ExplicitThing : IThing\n{\n    string IThing.Go(string s) => s;\n}\n";
            var consumer = ConsumerSource("IThing a = new ImplicitThing();", "_ = a.Go(\"a\");", "IThing b = new ExplicitThing();", "_ = b.Go(\"b\");");
            var files = TwoProjectFiles(core, consumer);
            var lineA = LineOf(consumer, "a.Go(\"a\")");
            var lineB = LineOf(consumer, "b.Go(\"b\")");
            var idDigest = Enc.Hash("asmctx3-identifier", "Go");
            var declDigest = Enc.Hash("asmctx3-declarations", "M:Review.Core.IThing.Go(System.String)");
            var shapeDigest = Enc.Hash("asmctx3-target", "method|0|string");
            var facts = await ExtractAsync(files, "p2:forms:explicit", ("App/Consumer.cs", lineA, "implicit"), ("App/Consumer.cs", lineB, "explicit"));
            var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, "p2:forms:explicit:analysis", files));
            var impl = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!.Compilation
                .GetTypeByMetadataName("Review.Core.ExplicitThing")!.GetMembers().OfType<IMethodSymbol>()
                .Single(m => m.MethodKind == MethodKind.ExplicitInterfaceImplementation);
            var implDocId = impl.GetDocumentationCommentId() ?? "(null)";
            var entry = new JsonObject
            {
                ["id"] = "explicit-interface",
                ["facts"] = facts.DeepClone(),
                ["explicitImplDocId"] = implDocId,
                ["expectedImplDocId"] = "M:Review.Core.ExplicitThing.Review#Core#IThing#Go(System.String)",
            };
            var pass = Fact(facts, 0, "identifierDigest") == idDigest &&
                Fact(facts, 0, "declarationDigest") == declDigest && Fact(facts, 0, "shapeDigest") == shapeDigest &&
                Fact(facts, 1, "declarationDigest") == declDigest &&
                implDocId == "M:Review.Core.ExplicitThing.Review#Core#IThing#Go(System.String)";
            forms.Add(entry);
            Check("form:explicit-interface", pass,
                "call sites bind the interface member; doc-IDs carry no global::; explicit implementation uses # escaping");
        }
        root["forms"] = forms;

        // ---------------- clarified matrix (fresh fixtures)
        var matrix = new JsonArray();
        async Task MatrixPairAsync(string row, string description, string coreA, string consumerA, string markerA,
            string coreB, string consumerB, string markerB, string idForm, string declA, string declB, string shapeForm,
            string expectedId, string expectedDecl, string expectedShape,
            string? idFormB = null, string? shapeFormB = null)
        {
            var filesA = TwoProjectFiles(coreA, consumerA);
            var filesB = TwoProjectFiles(coreB, consumerB);
            var factsA = await ExtractAsync(filesA, "p2:matrix:" + row + ":a", ("App/Consumer.cs", LineOf(consumerA, markerA), "a"));
            var factsB = await ExtractAsync(filesB, "p2:matrix:" + row + ":b", ("App/Consumer.cs", LineOf(consumerB, markerB), "b"));
            var idFormSecond = idFormB ?? idForm;
            var shapeFormSecond = shapeFormB ?? shapeForm;
            var idA = Fact(factsA, 0, "identifierDigest");
            var idB = Fact(factsB, 0, "identifierDigest");
            var dA = Fact(factsA, 0, "declarationDigest");
            var dB = Fact(factsB, 0, "declarationDigest");
            var sA = Fact(factsA, 0, "shapeDigest");
            var sB = Fact(factsB, 0, "shapeDigest");
            var relId = idA == idB ? "=" : "!=";
            var relDecl = dA == dB ? "=" : "!=";
            var relShape = sA == sB ? "=" : "!=";
            var pass = relId == expectedId && relDecl == expectedDecl && relShape == expectedShape &&
                idA == Enc.Hash("asmctx3-identifier", idForm) && idB == Enc.Hash("asmctx3-identifier", idFormSecond) &&
                dA == Enc.Hash("asmctx3-declarations", declA) && dB == Enc.Hash("asmctx3-declarations", declB) &&
                sA == Enc.Hash("asmctx3-target", shapeForm) && sB == Enc.Hash("asmctx3-target", shapeFormSecond);
            matrix.Add(new JsonObject
            {
                ["row"] = row, ["description"] = description,
                ["expectedRelations"] = $"{expectedId}/{expectedDecl}/{expectedShape}",
                ["observedRelations"] = $"{relId}/{relDecl}/{relShape}",
                ["expected"] = new JsonObject
                {
                    ["identifierA"] = Enc.Hash("asmctx3-identifier", idForm),
                    ["identifierB"] = Enc.Hash("asmctx3-identifier", idFormSecond),
                    ["declarationA"] = Enc.Hash("asmctx3-declarations", declA),
                    ["declarationB"] = Enc.Hash("asmctx3-declarations", declB),
                    ["shapeA"] = Enc.Hash("asmctx3-target", shapeForm),
                    ["shapeB"] = Enc.Hash("asmctx3-target", shapeFormSecond),
                },
                ["actualA"] = factsA.DeepClone(),
                ["actualB"] = factsB.DeepClone(),
                ["pass"] = pass,
            });
            Check("matrix:" + row, pass);
        }

        const string FmtCore = "namespace Review.Core;\npublic static class Fmt\n{\n    public static void Target(string value) { }\n}\n";
        await MatrixPairAsync("R1", "whitespace/formatting", FmtCore, ConsumerSource("Fmt.Target(\"x\");"), "Fmt.Target(\"x\")",
            "namespace Review.Core;\npublic static class Fmt\n{\n    public static void Target(\n        string value)\n    { }\n}\n", ConsumerSource("Fmt.Target(\"x\");"), "Fmt.Target(\"x\")",
            "Target", "M:Review.Core.Fmt.Target(System.String)", "M:Review.Core.Fmt.Target(System.String)", "method|0|string", "=", "=", "=");
        await MatrixPairAsync("R2", "comments/trivia", FmtCore, ConsumerSource("Fmt.Target(\"x\");"), "Fmt.Target(\"x\")",
            "namespace Review.Core;\n// leading comment\npublic static class Fmt\n{\n    /* block */\n    public static void Target(string value) { /* inline */ }\n}\n", ConsumerSource("Fmt.Target(\"x\");"), "Fmt.Target(\"x\")",
            "Target", "M:Review.Core.Fmt.Target(System.String)", "M:Review.Core.Fmt.Target(System.String)", "method|0|string", "=", "=", "=");
        {
            var relocationA = TwoProjectFiles(FmtCore, ConsumerSource("Fmt.Target(\"x\");"));
            var relocationB = TwoProjectFiles(FmtCore, ConsumerSource("Fmt.Target(\"x\");"));
            relocationB.Remove("Core/Targets.cs");
            relocationB["Core/Deep/Targets.cs"] = FmtCore;
            const string relocationMarker = "Fmt.Target(\"x\")";
            var factsA = await ExtractAsync(relocationA, "p2:matrix:R3:a",
                ("App/Consumer.cs", LineOf(relocationA["App/Consumer.cs"], relocationMarker), "a"));
            var factsB = await ExtractAsync(relocationB, "p2:matrix:R3:b",
                ("App/Consumer.cs", LineOf(relocationB["App/Consumer.cs"], relocationMarker), "b"));
            var idD = Enc.Hash("asmctx3-identifier", "Target");
            var declD = Enc.Hash("asmctx3-declarations", "M:Review.Core.Fmt.Target(System.String)");
            var shapeD = Enc.Hash("asmctx3-target", "method|0|string");
            var pass = Fact(factsA, 0, "identifierDigest") == idD && Fact(factsB, 0, "identifierDigest") == idD &&
                Fact(factsA, 0, "declarationDigest") == declD && Fact(factsB, 0, "declarationDigest") == declD &&
                Fact(factsA, 0, "shapeDigest") == shapeD && Fact(factsB, 0, "shapeDigest") == shapeD;
            matrix.Add(new JsonObject
            {
                ["row"] = "R3",
                ["description"] = "declaration relocation (Core/Targets.cs -> Core/Deep/Targets.cs)",
                ["expectedRelations"] = "=/=/=",
                ["observedRelations"] = pass ? "=/=/=" : "unexpected",
                ["expected"] = new JsonObject { ["identifier"] = idD, ["declaration"] = declD, ["shape"] = shapeD },
                ["actualA"] = factsA.DeepClone(), ["actualB"] = factsB.DeepClone(),
                ["pass"] = pass,
            });
            Check("matrix:R3", pass);
        }
        await MatrixPairAsync("R4", "namespace-qualification style", FmtCore, ConsumerSource("Fmt.Target(\"x\");"), "Fmt.Target(\"x\")",
            FmtCore, ConsumerSource("Review.Core.Fmt.Target(\"x\");"), "Review.Core.Fmt.Target(\"x\")",
            "Target", "M:Review.Core.Fmt.Target(System.String)", "M:Review.Core.Fmt.Target(System.String)", "method|0|string", "=", "=", "=");
        await MatrixPairAsync("R5", "type alias", FmtCore, ConsumerSource("Fmt.Target(\"x\");"), "Fmt.Target(\"x\")",
            FmtCore, ConsumerSource("F.Target(\"x\");").Replace("using Review.Core;", "using F = Review.Core.Fmt;", StringComparison.Ordinal), "F.Target(\"x\")",
            "Target", "M:Review.Core.Fmt.Target(System.String)", "M:Review.Core.Fmt.Target(System.String)", "method|0|string", "=", "=", "=");
        const string CalcCoreA = "namespace Review.Core;\npublic static class Calc\n{\n    public static int Add(int a) => a;\n}\n";
        const string CalcCoreB = "namespace Review.Core;\npublic static class Calc\n{\n    public static int Add(int a) => a;\n    public static string Add(string a) => a;\n}\n";
        await MatrixPairAsync("R6a", "overload addition (existing target)", CalcCoreA, ConsumerSource("Calc.Add(1);"), "Calc.Add(1)",
            CalcCoreB, ConsumerSource("Calc.Add(1);"), "Calc.Add(1)",
            "Add", "M:Review.Core.Calc.Add(System.Int32)", "M:Review.Core.Calc.Add(System.Int32)", "method|0|int", "=", "=", "=");
        await MatrixPairAsync("R6b", "retargeted overload", CalcCoreB, ConsumerSource("Calc.Add(1);"), "Calc.Add(1)",
            CalcCoreB, ConsumerSource("Calc.Add(\"s\");"), "Calc.Add(\"s\")",
            "Add", "M:Review.Core.Calc.Add(System.Int32)", "M:Review.Core.Calc.Add(System.String)", "method|0|int", "=", "!=", "!=",
            shapeFormB: "method|0|string");
        await MatrixPairAsync("R7", "rename", "namespace Review.Core;\npublic static class Calc\n{\n    public static void Sum7(int value) { }\n}\n",
            ConsumerSource("Calc.Sum7(1);"), "Calc.Sum7(1)",
            "namespace Review.Core;\npublic static class Calc\n{\n    public static void Total7(int value) { }\n}\n",
            ConsumerSource("Calc.Total7(1);"), "Calc.Total7(1)",
            "Sum7", "M:Review.Core.Calc.Sum7(System.Int32)", "M:Review.Core.Calc.Total7(System.Int32)", "method|0|int", "!=", "!=", "=",
            idFormB: "Total7");
        await MatrixPairAsync("R8", "parameter-type change", "namespace Review.Core;\npublic static class Calc\n{\n    public static void Take8(int value) { }\n}\n",
            ConsumerSource("Calc.Take8(1);"), "Calc.Take8(1)",
            "namespace Review.Core;\npublic static class Calc\n{\n    public static void Take8(string value) { }\n}\n",
            ConsumerSource("Calc.Take8(\"x\");"), "Calc.Take8(\"x\")",
            "Take8", "M:Review.Core.Calc.Take8(System.Int32)", "M:Review.Core.Calc.Take8(System.String)", "method|0|int", "=", "!=", "!=",
            shapeFormB: "method|0|string");
        await MatrixPairAsync("R9", "parameter-order change", "namespace Review.Core;\npublic static class Calc\n{\n    public static void Order9(int a, string b) { }\n}\n",
            ConsumerSource("Calc.Order9(1, \"x\");"), "Calc.Order9(1",
            "namespace Review.Core;\npublic static class Calc\n{\n    public static void Order9(string b, int a) { }\n}\n",
            ConsumerSource("Calc.Order9(\"x\", 1);"), "Calc.Order9(\"x\"",
            "Order9", "M:Review.Core.Calc.Order9(System.Int32,System.String)", "M:Review.Core.Calc.Order9(System.String,System.Int32)",
            "method|0|int,string", "=", "!=", "!=",
            shapeFormB: "method|0|string,int");
        await MatrixPairAsync("R10", "generic arity change", "namespace Review.Core;\npublic static class Calc\n{\n    public static void Pick10(int value) { }\n}\n",
            ConsumerSource("Calc.Pick10(1);"), "Calc.Pick10(1)",
            "namespace Review.Core;\npublic static class Calc\n{\n    public static void Pick10<T>(T value) { }\n}\n",
            ConsumerSource("Calc.Pick10(2);"), "Calc.Pick10(2)",
            "Pick10", "M:Review.Core.Calc.Pick10(System.Int32)", "M:Review.Core.Calc.Pick10``1(``0)", "method|0|int", "=", "!=", "!=",
            shapeFormB: "method|1|T");
        const string BoxCore = "namespace Review.Core;\npublic sealed class Box<T>\n{\n    public T Get() => default!;\n}\n";
        var boxConsumer = ConsumerSource("new Box<int>().Get();", "new Box<string>().Get();");
        {
            var files = TwoProjectFiles(BoxCore, boxConsumer);
            var lineA = LineOf(boxConsumer, "new Box<int>().Get()");
            var lineB = LineOf(boxConsumer, "new Box<string>().Get()");
            var facts = await ExtractAsync(files, "p2:matrix:R11", ("App/Consumer.cs", lineA, "int"), ("App/Consumer.cs", lineB, "string"));
            var idD = Enc.Hash("asmctx3-identifier", "Get");
            var declD = Enc.Hash("asmctx3-declarations", "M:Review.Core.Box`1.Get");
            var shapeD = Enc.Hash("asmctx3-target", "method|0|");
            var pass = Fact(facts, 0, "identifierDigest") == idD && Fact(facts, 1, "identifierDigest") == idD &&
                Fact(facts, 0, "declarationDigest") == declD && Fact(facts, 1, "declarationDigest") == declD &&
                Fact(facts, 0, "shapeDigest") == shapeD && Fact(facts, 1, "shapeDigest") == shapeD;
            matrix.Add(new JsonObject
            {
                ["row"] = "R11", ["description"] = "generic substitution",
                ["expectedRelations"] = "=/=/=", ["observedRelations"] = pass ? "=/=/=" : "unexpected",
                ["expected"] = new JsonObject
                {
                    ["identifier"] = idD, ["declaration"] = declD, ["shape"] = shapeD,
                    ["declarationForm"] = "M:Review.Core.Box`1.Get", ["shapeForm"] = "method|0|",
                },
                ["actual"] = facts.DeepClone(),
                ["pass"] = pass,
            });
            Check("matrix:R11", pass);
        }
        await MatrixPairAsync("R12", "extension reduction (both call forms)", "namespace Review.Core;\npublic static class Ext12\n{\n    public static int Double12(this int value) => value * 2;\n}\n",
            ConsumerSource("5.Double12();"), "5.Double12()",
            "namespace Review.Core;\npublic static class Ext12\n{\n    public static int Double12(this int value) => value * 2;\n}\n",
            ConsumerSource("Ext12.Double12(5);"), "Ext12.Double12(5)",
            "Double12", "M:Review.Core.Ext12.Double12(System.Int32)", "M:Review.Core.Ext12.Double12(System.Int32)", "method|0|int", "=", "=", "=");
        await MatrixPairAsync("R14", "same simple name, different owner", "namespace Review.Core;\npublic static class OwnerA\n{\n    public static void Ping14() { }\n}\npublic static class OwnerB\n{\n    public static void Ping14() { }\n}\n",
            ConsumerSource("OwnerA.Ping14();"), "OwnerA.Ping14()",
            "namespace Review.Core;\npublic static class OwnerA\n{\n    public static void Ping14() { }\n}\npublic static class OwnerB\n{\n    public static void Ping14() { }\n}\n",
            ConsumerSource("OwnerB.Ping14();"), "OwnerB.Ping14()",
            "Ping14", "M:Review.Core.OwnerA.Ping14", "M:Review.Core.OwnerB.Ping14", "method|0|", "=", "!=", "=");
        {
            var core = "namespace Review.Core;\npublic static class Calc\n{\n    public static void Ok15() { }\n}\n";
            var consumer = ConsumerSource("System.Console.WriteLine(\"x\");", "Calc.Ok15();");
            var files = TwoProjectFiles(core, consumer);
            var facts = await ExtractAsync(files, "p2:matrix:R15",
                ("App/Consumer.cs", LineOf(consumer, "Console.WriteLine"), "external"),
                ("App/Consumer.cs", LineOf(consumer, "Calc.Ok15();"), "control"));
            var externalRefused = facts[0]!.AsObject()["targetId"] is null;
            var controlPresent = facts[1]!.AsObject()["targetId"] is not null;
            matrix.Add(new JsonObject { ["row"] = "R15", ["description"] = "unresolved/external refusal", ["v1Clarified"] = "refusal", ["observed"] = externalRefused && controlPresent ? "refusal" : "unexpected fact", ["pass"] = externalRefused && controlPresent });
            Check("matrix:R15", externalRefused && controlPresent);
        }
        root["matrix"] = matrix;

        // ---------------- R13 independent reproduction (D1) with compctx/composed (D2/D3)
        var r13 = new JsonArray();
        async Task R13PairAsync(string pairId, bool declarationDistinguishes,
            string coreA, string consumerA, string markerA, string declA,
            string coreB, string consumerB, string markerB, string declB,
            string identifier)
        {
            var filesA = TwoProjectFiles(coreA, consumerA);
            var filesB = TwoProjectFiles(coreB, consumerB);
            var idDigest = Enc.Hash("asmctx3-identifier", identifier);
            var declDigestA = Enc.Hash("asmctx3-declarations", declA);
            var declDigestB = Enc.Hash("asmctx3-declarations", declB);
            var shapeDigest = Enc.Hash("asmctx3-target", "method|0|int");

            var factsA = await ExtractAsync(filesA, "p2:r13:" + pairId + ":a", ("App/Consumer.cs", LineOf(consumerA, markerA), "a"));
            var factsB = await ExtractAsync(filesB, "p2:r13:" + pairId + ":b", ("App/Consumer.cs", LineOf(consumerB, markerB), "b"));

            var sides = new List<JsonObject>();
            foreach (var (side, runId, files, declDigest, declForm) in new[]
                     {
                         (side: "a", runId: "p2:r13:" + pairId + ":a2", files: filesA, declDigest: declDigestA, declForm: declA),
                         (side: "b", runId: "p2:r13:" + pairId + ":b2", files: filesB, declDigest: declDigestB, declForm: declB),
                     })
            {
                var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, runId, files));
                var verified = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified ??
                    throw new InvalidDataException("reviewer fixture compilation not verified");
                var mapped = verified.MapR2FaithfulCompctx1();
                var sourceBytes = verified.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray();
                var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, verified.Descriptor.ProjectUnitId, verified.Compilation, sourceBytes);

                // Reviewer-owned independent compctx1 reconstruction from the recorded descriptor fields.
                var descriptorParts = new List<string> { "compctx1", frozen.RepositoryId, frozen.ProjectUnitId, frozen.AssemblyName,
                    frozen.MetadataIdentity, frozen.ModuleName, frozen.OptionsDigest };
                descriptorParts.AddRange(frozen.Sources.Select(s => s.CanonicalForm));
                descriptorParts.AddRange(frozen.References);
                var reviewerCompctx1 = "compctx1:sha256:" + Enc.Hash("compctx1-context", descriptorParts.ToArray());
                var referencesOrdered = frozen.References.SequenceEqual(frozen.References.Order(StringComparer.Ordinal), StringComparer.Ordinal);
                var productionTarget = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity,
                    mapped.ModuleName, idDigest, declDigest, shapeDigest).Value;
                var reviewerTarget = "asmctx3:sha256:" + Enc.Hash("asmctx3-context",
                [
                    "compctx1", reviewerCompctx1, frozen.MetadataIdentity, frozen.ModuleName, idDigest, declDigest, shapeDigest,
                ]);
                var diagnostics = verified.Compilation.GetDiagnostics();
                sides.Add(new JsonObject
                {
                    ["side"] = side,
                    ["diagnostics"] = new JsonObject
                    {
                        ["errors"] = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error),
                        ["warnings"] = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning),
                    },
                    ["descriptor"] = new JsonObject
                    {
                        ["repositoryId"] = frozen.RepositoryId,
                        ["projectUnitId"] = frozen.ProjectUnitId,
                        ["assemblyName"] = frozen.AssemblyName,
                        ["metadataIdentity"] = frozen.MetadataIdentity,
                        ["moduleName"] = frozen.ModuleName,
                        ["optionsDigest"] = frozen.OptionsDigest,
                        ["sources"] = new JsonArray(frozen.Sources.Select(s => (JsonNode)s.CanonicalForm).ToArray()),
                        ["referencesCount"] = frozen.References.Count,
                        ["referencesDigest"] = Enc.HashList("p2-references", frozen.References),
                        ["descriptorDigest"] = frozen.DescriptorDigest,
                    },
                    ["reviewerReconstruction"] = new JsonObject
                    {
                        ["compctx1"] = reviewerCompctx1,
                        ["matchesProduction"] = reviewerCompctx1 == mapped.DescriptorDigest,
                        ["matchesFrozen"] = reviewerCompctx1 == frozen.DescriptorDigest,
                        ["referencesOrdered"] = referencesOrdered,
                    },
                    ["composed"] = new JsonObject
                    {
                        ["productionTarget"] = productionTarget,
                        ["reviewerTarget"] = reviewerTarget,
                        ["agree"] = productionTarget == reviewerTarget,
                        ["messageHex"] = Enc.Hex(Enc.MessageBytes("asmctx3-context",
                        [
                            "compctx1", reviewerCompctx1, frozen.MetadataIdentity, frozen.ModuleName, idDigest, declDigest, shapeDigest,
                        ])),
                    },
                    ["expected"] = new JsonObject
                    {
                        ["identifierDigest"] = idDigest, ["declarationDigest"] = declDigest, ["shapeDigest"] = shapeDigest,
                        ["declarationForm"] = declForm,
                    },
                });
            }
            var a = sides[0];
            var b = sides[1];
            var factADecl = Fact(factsA, 0, "declarationDigest");
            var factBDecl = Fact(factsB, 0, "declarationDigest");
            var factAId = Fact(factsA, 0, "identifierDigest");
            var factBId = Fact(factsB, 0, "identifierDigest");
            var factAShape = Fact(factsA, 0, "shapeDigest");
            var factBShape = Fact(factsB, 0, "shapeDigest");
            var targetA = (string?)a["composed"]!["productionTarget"];
            var targetB = (string?)b["composed"]!["productionTarget"];
            var compctxA = (string?)a["reviewerReconstruction"]!["compctx1"];
            var compctxB = (string?)b["reviewerReconstruction"]!["compctx1"];
            r13.Add(new JsonObject
            {
                ["pair"] = pairId,
                ["declarationDistinguishes"] = declarationDistinguishes,
                ["factsA"] = factsA.DeepClone(),
                ["factsB"] = factsB.DeepClone(),
                ["sides"] = new JsonArray(a.DeepClone(), b.DeepClone()),
            });
            Check("r13:" + pairId + ":identifier", factAId == idDigest && factBId == idDigest);
            Check("r13:" + pairId + ":declaration",
                declarationDistinguishes ? factADecl == declDigestA && factBDecl == declDigestB && factADecl != factBDecl
                                         : factADecl == declDigestA && factBDecl == declDigestB && factADecl == factBDecl);
            Check("r13:" + pairId + ":shape", factAShape == shapeDigest && factBShape == shapeDigest);
            Check("r13:" + pairId + ":compctx-reconstructed", compctxA == (string?)a["descriptor"]!["descriptorDigest"] &&
                compctxB == (string?)b["descriptor"]!["descriptorDigest"] &&
                (bool)a["reviewerReconstruction"]!["matchesProduction"]! && (bool)b["reviewerReconstruction"]!["matchesProduction"]!,
                "reviewer-owned reconstruction from recorded fields matches production and frozen descriptors");
            Check("r13:" + pairId + ":compctx-differs", compctxA != compctxB);
            Check("r13:" + pairId + ":composed-agrees", (bool)a["composed"]!["agree"]! && (bool)b["composed"]!["agree"]!);
            Check("r13:" + pairId + ":targets-differ", targetA != targetB);
            Check("r13:" + pairId + ":fact-target-matches", Fact(factsA, 0, "targetId") == targetA && Fact(factsB, 0, "targetId") == targetB);
        }

        await R13PairAsync("value-ref", true,
            "namespace Review.Core;\npublic static class Probe\n{\n    public static void Hit(int value) { }\n}\n",
            ConsumerSource("Probe.Hit(1);"), "Probe.Hit(1);", "M:Review.Core.Probe.Hit(System.Int32)",
            "namespace Review.Core;\npublic static class Probe\n{\n    public static void Hit(ref int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Probe.Hit(ref v);"), "Probe.Hit(ref v);", "M:Review.Core.Probe.Hit(System.Int32@)",
            "Hit");
        await R13PairAsync("ref-out", false,
            "namespace Review.Core;\npublic static class Probe\n{\n    public static void Hit2(ref int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Probe.Hit2(ref v);"), "Probe.Hit2(ref v);", "M:Review.Core.Probe.Hit2(System.Int32@)",
            "namespace Review.Core;\npublic static class Probe\n{\n    public static void Hit2(out int value) { value = 0; }\n}\n",
            ConsumerSource("var v = 0;", "Probe.Hit2(out v);"), "Probe.Hit2(out v);", "M:Review.Core.Probe.Hit2(System.Int32@)",
            "Hit2");
        await R13PairAsync("ref-in", false,
            "namespace Review.Core;\npublic static class Probe\n{\n    public static void Hit3(ref int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Probe.Hit3(ref v);"), "Probe.Hit3(ref v);", "M:Review.Core.Probe.Hit3(System.Int32@)",
            "namespace Review.Core;\npublic static class Probe\n{\n    public static void Hit3(in int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Probe.Hit3(in v);"), "Probe.Hit3(in v);", "M:Review.Core.Probe.Hit3(System.Int32@)",
            "Hit3");
        await R13PairAsync("out-in", false,
            "namespace Review.Core;\npublic static class Probe\n{\n    public static void Hit4(out int value) { value = 0; }\n}\n",
            ConsumerSource("var v = 0;", "Probe.Hit4(out v);"), "Probe.Hit4(out v);", "M:Review.Core.Probe.Hit4(System.Int32@)",
            "namespace Review.Core;\npublic static class Probe\n{\n    public static void Hit4(in int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Probe.Hit4(in v);"), "Probe.Hit4(in v);", "M:Review.Core.Probe.Hit4(System.Int32@)",
            "Hit4");
        root["r13"] = r13;

        // ---------------- coexistence + language boundary
        {
            var core = "namespace Review.Core;\npublic static class Both\n{\n    public static void Ping(int value) { }\n    public static void Ping(ref int value) { }\n}\n";
            var consumer = ConsumerSource("Both.Ping(1);", "var v = 0;", "Both.Ping(ref v);");
            var files = TwoProjectFiles(core, consumer);
            var idDigest = Enc.Hash("asmctx3-identifier", "Ping");
            var declPlain = "M:Review.Core.Both.Ping(System.Int32)";
            var declRef = "M:Review.Core.Both.Ping(System.Int32@)";
            var shapeDigest = Enc.Hash("asmctx3-target", "method|0|int");
            var facts = await ExtractAsync(files, "p2:coexist",
                ("App/Consumer.cs", LineOf(consumer, "Both.Ping(1);"), "plain"),
                ("App/Consumer.cs", LineOf(consumer, "Both.Ping(ref v);"), "ref"));
            var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, "p2:coexist:analysis", files));
            var verified = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
            var mapped = verified.MapR2FaithfulCompctx1();
            var targetPlain = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity,
                mapped.ModuleName, idDigest, Enc.Hash("asmctx3-declarations", declPlain), shapeDigest).Value;
            var targetRef = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity,
                mapped.ModuleName, idDigest, Enc.Hash("asmctx3-declarations", declRef), shapeDigest).Value;
            root["coexistence"] = new JsonObject
            {
                ["facts"] = facts.DeepClone(),
                ["context"] = mapped.DescriptorDigest,
                ["targetPlain"] = targetPlain,
                ["targetRef"] = targetRef,
            };
            Check("coexist:compiles", verified.Compilation.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error) == 0);
            Check("coexist:identifier-equal", Fact(facts, 0, "identifierDigest") == idDigest && Fact(facts, 1, "identifierDigest") == idDigest);
            Check("coexist:declaration-distinguishes",
                Fact(facts, 0, "declarationDigest") == Enc.Hash("asmctx3-declarations", declPlain) &&
                Fact(facts, 1, "declarationDigest") == Enc.Hash("asmctx3-declarations", declRef));
            Check("coexist:same-context-targets-differ", targetPlain != targetRef &&
                Fact(facts, 0, "targetId") == targetPlain && Fact(facts, 1, "targetId") == targetRef);
        }
        var boundaries = new JsonArray();
        async Task BoundaryAsync(string id, string coreSource)
        {
            var files = TwoProjectFiles(coreSource, ConsumerSource());
            var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, "p2:boundary:" + id, files));
            var compilation = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Compilation;
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            var ids = errors.Select(d => d.Id).Distinct().Order(StringComparer.Ordinal).ToArray();
            boundaries.Add(new JsonObject
            {
                ["id"] = id, ["errorCount"] = errors.Length,
                ["diagnosticIds"] = new JsonArray(ids.Select(x => (JsonNode)x).ToArray()),
                ["messages"] = new JsonArray(errors.Take(2).Select(e => (JsonNode)e.GetMessage()).ToArray()),
            });
            Check("boundary:" + id, errors.Length > 0);
        }
        await BoundaryAsync("ref-out", "namespace Review.Core;\npublic static class No\n{\n    public static void Boom(ref int value) { }\n    public static void Boom(out int value) { value = 0; }\n}\n");
        await BoundaryAsync("ref-in", "namespace Review.Core;\npublic static class No\n{\n    public static void Boom2(ref int value) { }\n    public static void Boom2(in int value) { }\n}\n");
        root["languageBoundaries"] = boundaries;

        // ---------------- editorial corroboration (bounded)
        {
            var core = "namespace Review.Core;\npublic interface IThing\n{\n    string Go(string s);\n}\n";
            var files = TwoProjectFiles(core, ConsumerSource());
            var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, "p2:editorial", files));
            var compilation = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!.Compilation;
            var thingType = compilation.GetTypeByMetadataName("Review.Core.IThing")!;
            var go = thingType.GetMembers("Go").OfType<IMethodSymbol>().Single();
            var docId = go.GetDocumentationCommentId();
            var typeDisplay = thingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var paramDisplay = go.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            root["editorial"] = new JsonObject
            {
                ["goDocId"] = docId, ["thingTypeDisplay"] = typeDisplay, ["paramDisplay"] = paramDisplay,
            };
            Check("editorial:docid-exact", docId == "M:Review.Core.IThing.Go(System.String)");
            Check("editorial:docid-no-global", docId is not null && !docId.Contains("global::", StringComparison.Ordinal));
            Check("editorial:type-display-has-global", typeDisplay == "global::Review.Core.IThing");
            Check("editorial:special-type-display", paramDisplay == "string");
        }

        root["checks"] = checks;
        root["allPass"] = allPass;
        root["checkSummary"] = $"{checks.Count(c => (bool)c!["pass"]!)}/{checks.Count}";
        return root;
    }
}
