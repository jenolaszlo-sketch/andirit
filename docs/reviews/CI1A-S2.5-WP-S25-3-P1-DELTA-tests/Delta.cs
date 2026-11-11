using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Frozen = AdrCi1Exp.Shared;

namespace P1Delta;

// ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01 — bounded delta proof for the approved
// canonical-forms v1 clarification (addendum s01-canonical-form-v1-clarification-01.md).
//
// D1: R13a/R13b/R13c re-executed against the corrected E-3 relations.
// D2: direct defining-compilation context (compctx1) receipts for modifier-only revisions.
// D3: composed target identity receipts (production composition + independent frozen/RefEnc recomputation).
// Plus: coexistence challenge (value vs ref in one compilation), language boundary (CS0663),
// editorial corroboration (E-1/E-4), and encoding known-answer anchors for RefEnc.
internal static class Delta
{
    internal static readonly CodeRepositoryId Repo = new("delta:fixture");
    private static readonly DateTimeOffset Started = new(2026, 11, 11, 8, 0, 0, TimeSpan.Zero);

    // ------------------------------------------------------------------
    // Independent reference encoder (contract §6) — never calls production code.
    // ------------------------------------------------------------------
    internal static class RefEnc
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
    }

    // ------------------------------------------------------------------
    // Fixture builders.
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
        sb.Append("using Fixture.Core;\nnamespace Fixture.App;\npublic static class Consumer\n{\n    public static int Run()\n    {\n");
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

    // ------------------------------------------------------------------
    // Extraction capture (producer path, in-memory store).
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    // Compilation side: descriptor (compctx1), frozen cross-check, composed target.
    // ------------------------------------------------------------------
    private static async Task<JsonObject> AnalyzeSideAsync(string runId, Dictionary<string, string> files,
        string identifierDigest, string declarationDigest, string shapeDigest)
    {
        var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, runId, files));
        var project = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj");
        var verified = project.Verified ?? throw new InvalidDataException("delta fixture compilation not verified");
        var mapped = verified.MapR2FaithfulCompctx1();
        var sourceBytes = verified.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray();
        var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, verified.Descriptor.ProjectUnitId,
            verified.Compilation, sourceBytes);

        var productionSources = mapped.Sources.Select(s => s.CanonicalForm).Order(StringComparer.Ordinal).ToArray();
        var frozenSources = frozen.Sources.Select(s => s.CanonicalForm).Order(StringComparer.Ordinal).ToArray();
        var sourcesAgree = productionSources.SequenceEqual(frozenSources, StringComparer.Ordinal);
        var referencesAgree = mapped.References.SequenceEqual(frozen.References, StringComparer.Ordinal);
        var optionsAgree = mapped.OptionsDigest == frozen.OptionsDigest;
        var metadataAgrees = mapped.MetadataIdentity == frozen.MetadataIdentity && mapped.ModuleName == frozen.ModuleName;
        var frozenAgrees = mapped.DescriptorDigest == frozen.DescriptorDigest &&
            sourcesAgree && referencesAgree && optionsAgree && metadataAgrees;

        var diagnostics = verified.Compilation.GetDiagnostics();
        var errors = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        var warnings = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

        var productionTarget = CodeOccurrenceIdentity.ComputeTargetId(
            mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName,
            identifierDigest, declarationDigest, shapeDigest);
        var parts = new[] { "compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName,
            identifierDigest, declarationDigest, shapeDigest };
        var independentHash = Frozen.ProvenanceHashing.Hash("asmctx3-context", parts);
        var refEncHash = RefEnc.Hash("asmctx3-context", parts);
        var messageHex = RefEnc.Hex(RefEnc.MessageBytes("asmctx3-context", parts));
        var independentTarget = "asmctx3:sha256:" + independentHash;

        return new JsonObject
        {
            ["runId"] = runId,
            ["fileHashes"] = new JsonObject(files.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => KeyValuePair.Create<string, JsonNode?>(p.Key, FileHash(p.Value))).ToArray()),
            ["diagnostics"] = new JsonObject { ["errors"] = errors, ["warnings"] = warnings },
            ["descriptor"] = new JsonObject
            {
                ["contractVersion"] = mapped.ContractVersion,
                ["repositoryId"] = mapped.RepositoryId,
                ["projectUnitId"] = mapped.ProjectUnitId,
                ["assemblyName"] = mapped.AssemblyName,
                ["metadataIdentity"] = mapped.MetadataIdentity,
                ["moduleName"] = mapped.ModuleName,
                ["optionsDigest"] = mapped.OptionsDigest,
                ["sources"] = new JsonArray(frozenSources.Select(s => (JsonNode)s).ToArray()),
                ["referencesCount"] = frozen.References.Count,
                ["referencesDigest"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", frozen.References)))),
                ["descriptorDigest"] = frozen.DescriptorDigest,
            },
            ["frozenCrossCheck"] = new JsonObject
            {
                ["descriptorAgrees"] = mapped.DescriptorDigest == frozen.DescriptorDigest,
                ["sourcesAgree"] = sourcesAgree,
                ["referencesAgree"] = referencesAgree,
                ["optionsAgree"] = optionsAgree,
                ["metadataAgrees"] = metadataAgrees,
                ["frozenAgrees"] = frozenAgrees,
            },
            ["composed"] = new JsonObject
            {
                ["productionTarget"] = productionTarget.Value,
                ["independentTarget"] = independentTarget,
                ["refEncHashAgrees"] = refEncHash == independentHash,
                ["targetsAgree"] = productionTarget.Value == independentTarget,
                ["messageHex"] = messageHex,
            },
        };
    }

    // ------------------------------------------------------------------
    // Top level.
    // ------------------------------------------------------------------
    internal static async Task<JsonObject> RunAsync()
    {
        var root = new JsonObject
        {
            ["harness"] = "ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01",
            ["processId"] = Environment.ProcessId,
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
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

        // ---------------- encoding anchors (RefEnc vs literal hand-verified digests)
        var encodings = new JsonArray();
        void EncodingVector(string id, string? expectedHex, string domain, string field, string expectedDigest, bool productionAgrees)
        {
            var hex = RefEnc.Hex(RefEnc.MessageBytes(domain, [field]));
            var pass = (expectedHex is null || hex == expectedHex) &&
                RefEnc.Hash(domain, field) == expectedDigest && productionAgrees;
            encodings.Add(new JsonObject
            {
                ["id"] = id, ["domain"] = domain, ["field"] = field,
                ["messageHex"] = hex,
                ["expectedMessageHex"] = expectedHex ?? "(not constrained by this vector)",
                ["expectedDigest"] = expectedDigest, ["refEncDigest"] = RefEnc.Hash(domain, field),
                ["productionDigestAgrees"] = productionAgrees, ["pass"] = pass,
            });
            Check("encoding:" + id, pass);
        }
        EncodingVector("KAT-identifier-Write", "0000001261736d637478332d6964656e746966696572000000055772697465",
            "asmctx3-identifier", "Write", "4a468c685e4033c579aff4dcbf371062dc6a88c33318a89d54dedaf60f61af88",
            CodeOccurrenceIdentity.ComputeIdentifierDigest(["Write"]) ==
            "4a468c685e4033c579aff4dcbf371062dc6a88c33318a89d54dedaf60f61af88");
        EncodingVector("KAT-declaration-ref", null, "asmctx3-declarations", "M:Fixture.Core.Mods.Change13(System.Int32@)",
            "df577e8f759af780c518813a96270638ccf3dabbb8525fbbca28d4639f73bad0",
            CodeOccurrenceIdentity.ComputeDeclarationDigest(["M:Fixture.Core.Mods.Change13(System.Int32@)"]) ==
            "df577e8f759af780c518813a96270638ccf3dabbb8525fbbca28d4639f73bad0");
        EncodingVector("KAT-shape-int", null, "asmctx3-target", "method|0|int",
            "928f040e7a49ae25ac62d8360c726572d1c58bf32cc687c8adda9f1267dcfef8",
            CodeOccurrenceIdentity.ComputeShapeDigest("method|0|int") ==
            "928f040e7a49ae25ac62d8360c726572d1c58bf32cc687c8adda9f1267dcfef8");
        root["encodingVectors"] = encodings;

        // ---------------- revision pairs (D1 + D2 + D3)
        var pairs = new JsonArray();

        async Task<(JsonObject Receipt, string ProductionTarget, string Compctx1)> RevisionPair(
            string pairId, bool declarationDistinguishes,
            string coreA, string consumerA, string markerA, string declA,
            string coreB, string consumerB, string markerB, string declB,
            string identifier, string methodLabel)
        {
            var filesA = TwoProjectFiles(coreA, consumerA);
            var filesB = TwoProjectFiles(coreB, consumerB);
            var lineA = LineOf(consumerA, markerA);
            var lineB = LineOf(consumerB, markerB);

            var idDigest = RefEnc.Hash("asmctx3-identifier", identifier);
            var declDigestA = RefEnc.Hash("asmctx3-declarations", declA);
            var declDigestB = RefEnc.Hash("asmctx3-declarations", declB);
            var shapeForm = "method|0|int";
            var shapeDigest = RefEnc.Hash("asmctx3-target", shapeForm);

            var receipt = new JsonObject { ["pair"] = pairId, ["label"] = methodLabel, ["identifier"] = identifier };
            receipt["expected"] = new JsonObject
            {
                ["identifierForm"] = identifier,
                ["declarationFormA"] = declA, ["declarationFormB"] = declB,
                ["shapeForm"] = shapeForm,
                ["identifierDigest"] = idDigest,
                ["declarationDigestA"] = declDigestA, ["declarationDigestB"] = declDigestB,
                ["shapeDigest"] = shapeDigest,
                ["declarationDistinguishes"] = declarationDistinguishes,
            };

            var factsA = await ExtractAsync(filesA, "delta:" + pairId + ":a", ("App/Consumer.cs", lineA, "a"));
            var factsB = await ExtractAsync(filesB, "delta:" + pairId + ":b", ("App/Consumer.cs", lineB, "b"));
            receipt["factsA"] = factsA.DeepClone();
            receipt["factsB"] = factsB.DeepClone();
            var factA = factsA[0]!.AsObject();
            var factB = factsB[0]!.AsObject();

            var sideA = await AnalyzeSideAsync("delta:" + pairId + ":a2", filesA, idDigest, declDigestA, shapeDigest);
            var sideB = await AnalyzeSideAsync("delta:" + pairId + ":b2", filesB, idDigest, declDigestB, shapeDigest);
            receipt["sideA"] = sideA.DeepClone();
            receipt["sideB"] = sideB.DeepClone();

            // Input-diff disclosure: which fixture files changed between the revisions.
            var changed = new JsonArray();
            foreach (var path in filesA.Keys.Order(StringComparer.Ordinal))
                if (filesA[path] != filesB[path]) changed.Add(path);
            receipt["changedInputFiles"] = changed;

            var factAId = (string?)factA["identifierDigest"];
            var factBId = (string?)factB["identifierDigest"];
            var factADecl = (string?)factA["declarationDigest"];
            var factBDecl = (string?)factB["declarationDigest"];
            var factAShape = (string?)factA["shapeDigest"];
            var factBShape = (string?)factB["shapeDigest"];

            Check(pairId + ":facts-present", factA["targetId"] is not null && factB["targetId"] is not null);
            Check(pairId + ":identifier-equal-and-expected", factAId == idDigest && factBId == idDigest,
                "identifier digest equal across revisions and equal to the independent expected digest");
            Check(pairId + ":declaration-relations",
                declarationDistinguishes ? factADecl == declDigestA && factBDecl == declDigestB && factADecl != factBDecl
                                         : factADecl == declDigestA && factBDecl == declDigestB && factADecl == factBDecl,
                declarationDistinguishes
                    ? "declaration digests differ (value vs mode, doc-ID @) and match independent expectations"
                    : "declaration digests equal (within ref/out/in family, doc-ID @ uniform) and match independent expectations");
            Check(pairId + ":shape-equal-and-expected", factAShape == shapeDigest && factBShape == shapeDigest,
                "shape digest equal across revisions and equal to the independent expected digest");

            var ctxA = (string?)sideA["descriptor"]!["descriptorDigest"];
            var ctxB = (string?)sideB["descriptor"]!["descriptorDigest"];
            Check(pairId + ":compctx1-differs", ctxA is not null && ctxB is not null && ctxA != ctxB,
                "defining-compilation context differs for the revision pair");

            var iso = sideA["descriptor"]!.AsObject();
            var isoB = sideB["descriptor"]!.AsObject();
            var isolationHolds =
                (string?)iso["metadataIdentity"] == (string?)isoB["metadataIdentity"] &&
                (string?)iso["moduleName"] == (string?)isoB["moduleName"] &&
                (string?)iso["assemblyName"] == (string?)isoB["assemblyName"] &&
                (string?)iso["projectUnitId"] == (string?)isoB["projectUnitId"] &&
                (string?)iso["optionsDigest"] == (string?)isoB["optionsDigest"] &&
                (string?)iso["referencesDigest"] == (string?)isoB["referencesDigest"];
            var sourcesChanged = iso["sources"]!.AsArray().Select(s => (string)s!)
                .Zip(isoB["sources"]!.AsArray().Select(s => (string)s!), (x, y) => (x, y))
                .Where(p => p.x != p.y).Select(p => p.x).ToArray();
            var expectedSourceChange = sourcesChanged.Length == 1 && sourcesChanged[0].StartsWith("Core/Targets.cs|", StringComparison.Ordinal);
            Check(pairId + ":context-isolation", isolationHolds && expectedSourceChange,
                "only the defining project's Core/Targets.cs source identity differs; metadata/module/assembly/unit/options/references identical");

            var frozenA = (bool)sideA["frozenCrossCheck"]!["frozenAgrees"]!;
            var frozenB = (bool)sideB["frozenCrossCheck"]!["frozenAgrees"]!;
            Check(pairId + ":frozen-descriptor-agrees", frozenA && frozenB,
                "production compctx1 mapping equals the frozen R2 recomputation field-for-field on both sides");

            var targetA = (string?)sideA["composed"]!["productionTarget"];
            var targetB = (string?)sideB["composed"]!["productionTarget"];
            Check(pairId + ":composed-agrees", (bool)sideA["composed"]!["targetsAgree"]! && (bool)sideB["composed"]!["targetsAgree"]!);
            Check(pairId + ":fact-target-matches-composed", (string?)factA["targetId"] == targetA && (string?)factB["targetId"] == targetB,
                "extracted fact target equals the composed target from the same inputs");
            Check(pairId + ":composed-targets-differ", targetA != targetB,
                "composed targets differ for the revision pair (declaration and/or context)");

            pairs.Add(receipt);
            return (receipt, targetA!, ctxA!);
        }

        await RevisionPair("R13a", true,
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Change(int value) { }\n}\n",
            ConsumerSource("Mods.Change(1);"), "Mods.Change(1);", "M:Fixture.Core.Mods.Change(System.Int32)",
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void Change(ref int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Mods.Change(ref v);"), "Mods.Change(ref v);", "M:Fixture.Core.Mods.Change(System.Int32@)",
            "Change", "value vs ref");

        await RevisionPair("R13b", false,
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void ChangeB(ref int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Mods.ChangeB(ref v);"), "Mods.ChangeB(ref v);", "M:Fixture.Core.Mods.ChangeB(System.Int32@)",
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void ChangeB(out int value) { value = 0; }\n}\n",
            ConsumerSource("var v = 0;", "Mods.ChangeB(out v);"), "Mods.ChangeB(out v);", "M:Fixture.Core.Mods.ChangeB(System.Int32@)",
            "ChangeB", "ref vs out");

        await RevisionPair("R13c", false,
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void ChangeC(ref int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Mods.ChangeC(ref v);"), "Mods.ChangeC(ref v);", "M:Fixture.Core.Mods.ChangeC(System.Int32@)",
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void ChangeC(in int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Mods.ChangeC(in v);"), "Mods.ChangeC(in v);", "M:Fixture.Core.Mods.ChangeC(System.Int32@)",
            "ChangeC", "ref vs in");

        await RevisionPair("family-out-in", false,
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void ChangeD(out int value) { value = 0; }\n}\n",
            ConsumerSource("var v = 0;", "Mods.ChangeD(out v);"), "Mods.ChangeD(out v);", "M:Fixture.Core.Mods.ChangeD(System.Int32@)",
            "namespace Fixture.Core;\npublic static class Mods\n{\n    public static void ChangeD(in int value) { }\n}\n",
            ConsumerSource("var v = 0;", "Mods.ChangeD(in v);"), "Mods.ChangeD(in v);", "M:Fixture.Core.Mods.ChangeD(System.Int32@)",
            "ChangeD", "out vs in");

        root["revisionPairs"] = pairs;

        // ---------------- coexistence challenge (value vs ref in ONE compilation)
        {
            var core = "namespace Fixture.Core;\npublic static class Both\n{\n    public static void Ping(int value) { }\n    public static void Ping(ref int value) { }\n}\n";
            var consumer = ConsumerSource("Both.Ping(1);", "var v = 0;", "Both.Ping(ref v);");
            var files = TwoProjectFiles(core, consumer);
            var linePlain = LineOf(consumer, "Both.Ping(1);");
            var lineRef = LineOf(consumer, "Both.Ping(ref v);");
            var idDigest = RefEnc.Hash("asmctx3-identifier", "Ping");
            var declPlain = "M:Fixture.Core.Both.Ping(System.Int32)";
            var declRef = "M:Fixture.Core.Both.Ping(System.Int32@)";
            var shapeDigest = RefEnc.Hash("asmctx3-target", "method|0|int");

            var facts = await ExtractAsync(files, "delta:coexist", ("App/Consumer.cs", linePlain, "plain"), ("App/Consumer.cs", lineRef, "ref"));
            var side = await AnalyzeSideAsync("delta:coexist:analysis", files, idDigest, RefEnc.Hash("asmctx3-declarations", declPlain), shapeDigest);
            var factPlain = facts[0]!.AsObject();
            var factRef = facts[1]!.AsObject();
            var ctx = (string?)side["descriptor"]!["descriptorDigest"];

            var targetPlain = CodeOccurrenceIdentity.ComputeTargetId(ctx!, (string)side["descriptor"]!["metadataIdentity"]!,
                (string)side["descriptor"]!["moduleName"]!, idDigest, RefEnc.Hash("asmctx3-declarations", declPlain), shapeDigest).Value;
            var targetRef = CodeOccurrenceIdentity.ComputeTargetId(ctx!, (string)side["descriptor"]!["metadataIdentity"]!,
                (string)side["descriptor"]!["moduleName"]!, idDigest, RefEnc.Hash("asmctx3-declarations", declRef), shapeDigest).Value;

            root["coexistence"] = new JsonObject
            {
                ["fixture"] = "Both.Ping(int) + Both.Ping(ref int) in one compilation",
                ["facts"] = facts.DeepClone(),
                ["context"] = ctx,
                ["targetPlain"] = targetPlain,
                ["targetRef"] = targetRef,
                ["errors"] = side["diagnostics"]!["errors"]!.GetValue<int>(),
            };
            Check("coexist:compiles", side["diagnostics"]!["errors"]!.GetValue<int>() == 0);
            Check("coexist:identifier-equal", (string?)factPlain["identifierDigest"] == idDigest && (string?)factRef["identifierDigest"] == idDigest);
            Check("coexist:declaration-distinguishes",
                (string?)factPlain["declarationDigest"] == RefEnc.Hash("asmctx3-declarations", declPlain) &&
                (string?)factRef["declarationDigest"] == RefEnc.Hash("asmctx3-declarations", declRef));
            Check("coexist:shape-equal", (string?)factPlain["shapeDigest"] == shapeDigest && (string?)factRef["shapeDigest"] == shapeDigest);
            Check("coexist:same-context-targets-differ", targetPlain != targetRef &&
                (string?)factPlain["targetId"] == targetPlain && (string?)factRef["targetId"] == targetRef,
                "within one defining context, the declaration form alone separates value vs ref targets");
        }

        // ---------------- language boundary (CS0663 family)
        var boundaries = new JsonArray();
        async Task BoundaryAsync(string id, string coreSource)
        {
            var files = TwoProjectFiles(coreSource, ConsumerSource());
            var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, "delta:boundary:" + id, files));
            var compilation = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Compilation;
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            var ids = errors.Select(d => d.Id).Distinct().Order(StringComparer.Ordinal).ToArray();
            boundaries.Add(new JsonObject
            {
                ["id"] = id,
                ["errorCount"] = errors.Length,
                ["diagnosticIds"] = new JsonArray(ids.Select(x => (JsonNode)x).ToArray()),
                ["messages"] = new JsonArray(errors.Take(2).Select(e => (JsonNode)e.GetMessage()).ToArray()),
            });
            Check("boundary:" + id + ":rejected", errors.Length > 0, "modifier-only overload set must not compile");
        }
        await BoundaryAsync("ref-out",
            "namespace Fixture.Core;\npublic static class BadMods\n{\n    public static void Boom(ref int value) { }\n    public static void Boom(out int value) { value = 0; }\n}\n");
        await BoundaryAsync("ref-in",
            "namespace Fixture.Core;\npublic static class BadMods2\n{\n    public static void Boom2(ref int value) { }\n    public static void Boom2(in int value) { }\n}\n");
        root["languageBoundaries"] = boundaries;

        // ---------------- editorial corroboration (E-1 / E-4)
        {
            var rootDir = new DirectoryInfo(AppContext.BaseDirectory);
            while (rootDir is not null && !File.Exists(Path.Combine(rootDir.FullName, "Andirit.slnx"))) rootDir = rootDir.Parent;
            if (rootDir is null) throw new InvalidOperationException("repository root not found");
            var corpus = Path.Combine(rootDir.FullName, "docs", "planning", "adr-ci1-01-ci1a-s2-0", "corpus", "repo");
            var corpusFiles = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Core/Core.csproj"] = File.ReadAllText(Path.Combine(corpus, "Core", "Core.csproj")),
                ["Core/Types.cs"] = File.ReadAllText(Path.Combine(corpus, "Core", "Types.cs")),
            };
            var prepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, "delta:editorial", corpusFiles));
            var compilation = prepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Compilation;
            var greeterType = compilation.GetTypeByMetadataName("Corpus.Core.IGreeter")!;
            var greet = greeterType.GetMembers("Greet").OfType<IMethodSymbol>().Single();
            var greetDocId = greet.GetDocumentationCommentId();
            var greetDisplay = greet.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var greeterTypeDisplay = greeterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var impl = compilation.GetTypeByMetadataName("Corpus.Core.ExplicitGreeter")!.GetMembers().OfType<IMethodSymbol>()
                .Single(m => m.MethodKind == MethodKind.ExplicitInterfaceImplementation);
            var implDocId = impl.GetDocumentationCommentId();
            var greetParamDisplay = greet.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Local-function observation: a doc-ID-less symbol exercising the fallback formula.
            var fallbackConsumer = "using Fixture.Core;\nnamespace Fixture.App;\npublic static class Consumer\n{\n    public static int Run()\n    {\n        return Local(new System.Collections.Generic.Dictionary<string, int>());\n\n        static int Local(System.Collections.Generic.Dictionary<string, int> map) => map.Count;\n    }\n}\n";
            var fallbackFiles = TwoProjectFiles("namespace Fixture.Core;\npublic static class Anchor { }\n", fallbackConsumer);
            var fallbackPrepared = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(Repo, "delta:editorial:fallback", fallbackFiles));
            var fallbackCompilation = fallbackPrepared.Projects.Single(p => p.Project.Path == "Core/Core.csproj");
            _ = fallbackCompilation;
            var appCompilation = fallbackPrepared.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!.Compilation;
            var consumerTree = appCompilation.SyntaxTrees.Single(t => t.FilePath == "App/Consumer.cs");
            var localFunction = consumerTree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var localSymbol = (IMethodSymbol)appCompilation.GetSemanticModel(consumerTree).GetDeclaredSymbol(localFunction)!;
            var localDocId = localSymbol.GetDocumentationCommentId();
            var localParamDisplay = localSymbol.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            root["editorial"] = new JsonObject
            {
                ["greetDocId"] = greetDocId,
                ["greetMemberDisplay"] = greetDisplay,
                ["greeterTypeDisplay"] = greeterTypeDisplay,
                ["greetParamDisplay"] = greetParamDisplay,
                ["explicitImplDocId"] = implDocId,
                ["localFunctionDocId"] = localDocId ?? "(null)",
                ["localFunctionParamDisplay"] = localParamDisplay,
                ["observationMemberDisplay"] = "FullyQualifiedFormat renders member symbols with minimal member options (e.g. 'Greet'); type and parameter-type displays carry qualification (e.g. 'global::Corpus.Core.IGreeter', 'global::System.Collections.Generic.Dictionary<string, int>'). No supported occurrence target reaches the documented section-4 fallback path in this Roslyn generation (local functions and explicit-interface implementations both receive documentation IDs).",
            };
            Check("editorial:docid-exact", greetDocId == "M:Corpus.Core.IGreeter.Greet(System.String)");
            Check("editorial:docid-no-global", greetDocId is not null && !greetDocId.Contains("global::", StringComparison.Ordinal) &&
                !(implDocId ?? string.Empty).Contains("global::", StringComparison.Ordinal));
            Check("editorial:display-vs-docid-distinct", greeterTypeDisplay == "global::Corpus.Core.IGreeter" &&
                greeterTypeDisplay.Contains("global::", StringComparison.Ordinal) &&
                greetDocId is not null && !greetDocId.Contains("global::", StringComparison.Ordinal),
                "display construction (type display) uses global:: while documentation-ID construction never does");
            Check("editorial:fully-qualified-special", greetParamDisplay == "string");
            Check("editorial:fully-qualified-global", localParamDisplay == "global::System.Collections.Generic.Dictionary<string, int>",
                "documented FullyQualifiedFormat semantics (E-1) corroborated on a non-special type");
        }

        // ---------------- disposition
        root["checks"] = checks;
        root["allPass"] = allPass;
        var passed = checks.Count(c => (bool)c!["pass"]!);
        root["checkSummary"] = $"{passed}/{checks.Count}";
        return root;
    }
}
