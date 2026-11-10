using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;

namespace P1Proof;

// ---------------------------------------------------------------------------
// Independent reference encoder — implements s01-canonical-form-contract.md §6
// (SHA-256 over domain + 4-byte big-endian length-prefixed UTF-8 parts) WITHOUT
// calling CodeOccurrenceIdentity. The explicit message bytes are the central
// evidence; expected digests come from this path or hand-written byte literals.
// ---------------------------------------------------------------------------
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
        foreach (var field in fields) Append(field ?? string.Empty);
        return buffer.ToArray();
    }

    internal static string Hex(byte[] bytes) => Convert.ToHexStringLower(bytes);

    internal static string Hash(string domain, params string[] fields) =>
        Convert.ToHexStringLower(SHA256.HashData(MessageBytes(domain, fields)));

    internal static byte[] FromHex(string hex) => Convert.FromHexString(hex);

    internal static string ExpectedIdentifier(string form) => Hash("asmctx3-identifier", form);
    internal static string ExpectedDeclaration(string form) => Hash("asmctx3-declarations", form);
    internal static string ExpectedShape(string form) => Hash("asmctx3-target", form);
}

// ---------------------------------------------------------------------------
// Fixture framework: real plugin extraction into an in-memory store through the
// accepted session/sink path, capturing authority envelopes + occurrence batches.
// ---------------------------------------------------------------------------
internal sealed class CaptureSink : ICodeGraphSink, ICodeOccurrenceAuthoritySink
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

internal sealed record ComponentDigests(string Identifier, string Declaration, string Shape, string TargetId);

internal static class Extract
{
    internal static readonly CodeRepositoryId Repo = new("p1:fixture");
    internal static readonly CodePluginId Plugin = new("probe.p1");
    internal static readonly DateTimeOffset Started = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    internal static Dictionary<string, string> TwoProjectFiles(string coreSource, string appSource) => new(StringComparer.Ordinal)
    {
        ["Core/Core.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
        ["Core/Targets.cs"] = coreSource,
        ["App/App.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Core/Core.csproj\" /></ItemGroup></Project>",
        ["App/Consumer.cs"] = appSource,
    };

    internal static List<(string Path, int Line)> AppendCalls(StringBuilder sb, params string[] calls)
    {
        var positions = new List<(string, int)>();
        foreach (var call in calls)
        {
            var line = sb.ToString().Count(character => character == '\n') + 1;
            sb.Append("        ").Append(call).Append('\n');
            positions.Add(("App/Consumer.cs", line));
        }
        return positions;
    }

    internal static CodeGraphPluginContext BuildContext(CodeRepositoryId repo, string runId, IReadOnlyDictionary<string, string> files) =>
        new(repo, null, new CodeIndexRunId(runId),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

    internal static async Task<JsonObject> RunAsync(Dictionary<string, string> files, string runId, string caseId)
    {
        var store = new InMemoryCodeGraphStore();
        var plugin = new CSharpCodeGraphPlugin();
        await store.UpsertRepositoryAsync(new CodeRepositoryManifest(Repo));
        await store.StoreIndexRunAsync(new CodeIndexRunManifest(Repo, new CodeIndexRunId(runId), Started, plugins: [plugin.Id]));
        var sink = new CaptureSink(new CodeGraphIngestionSink(store));
        var context = BuildContext(Repo, runId, files);
        await using var session = await plugin.CreateSessionAsync(context);
        await session.ExtractAsync(sink);

        var factsById = new Dictionary<string, CodeOccurrenceDefiningAuthority>(StringComparer.Ordinal);
        foreach (var authority in sink.Authorities)
            foreach (var fact in authority.Facts)
                factsById[fact.OccurrenceId] = fact;

        var occurrences = new JsonArray();
        foreach (var batch in sink.Batches)
            foreach (var occurrence in batch.Occurrences)
            {
                var entry = new JsonObject
                {
                    ["id"] = occurrence.Id.Value,
                    ["targetId"] = occurrence.TargetId.Value,
                    ["path"] = occurrence.Span.Path,
                    ["line"] = occurrence.Span.StartLine,
                    ["column"] = occurrence.Span.StartColumn,
                };
                if (factsById.TryGetValue(occurrence.Id.Value, out var defining))
                {
                    entry["identifierDigest"] = defining.IdentifierDigest;
                    entry["declarationDigest"] = defining.DeclarationDigest;
                    entry["shapeDigest"] = defining.ShapeDigest;
                }
                occurrences.Add(entry);
            }

        var coverage = new JsonObject
        {
            ["occurrenceFacts"] = factsById.Count,
            ["authorities"] = sink.Authorities.Count,
            ["batches"] = sink.Batches.Count,
        };
        return new JsonObject { ["case"] = caseId, ["run"] = runId, ["occurrences"] = occurrences, ["coverage"] = coverage };
    }

    internal static JsonObject? FindBySpan(JsonObject runResult, string path, int line)
    {
        foreach (var node in runResult["occurrences"]!.AsArray())
        {
            if (node is JsonObject entry && (string?)entry["path"] == path && (int?)entry["line"] == line)
                return entry;
        }
        return null;
    }
}

// ---------------------------------------------------------------------------
// Part A: known-answer vectors (encoding layer).
// ---------------------------------------------------------------------------
internal static class KnownAnswers
{
    internal static JsonArray Run()
    {
        var results = new JsonArray();

        // KAT-1: fully hand-written message bytes (domain + one field), expected digest
        // computed directly from the byte literal; cross-checked against production.
        const string domain = "asmctx3-identifier";
        const string field = "Write";
        const string messageHex = "00000012" + "61736d637478332d6964656e746966696572" + "00000005" + "5772697465";
        var messageBytes = RefEnc.FromHex(messageHex);
        var handDigest = Convert.ToHexStringLower(SHA256.HashData(messageBytes));
        var productionDigest = CodeOccurrenceIdentity.ComputeIdentifierDigest([field]);
        results.Add(VectorResult("KAT-1 hand-written bytes: asmctx3-identifier + 'Write'",
            domain, [field], messageHex, handDigest, productionDigest,
            handDigest == productionDigest && messageHex == RefEnc.Hex(RefEnc.MessageBytes(domain, [field]))));

        // KAT-2..N: reference-encoder vectors compared with production primitives.
        void Compare(string id, string dom, string[] fields, string production)
        {
            var expected = RefEnc.Hash(dom, fields);
            results.Add(VectorResult(id, dom, fields, RefEnc.Hex(RefEnc.MessageBytes(dom, fields)), expected, production, expected == production));
        }
        Compare("KAT-2 identifier Cyrillic", "asmctx3-identifier", ["Проверка"], CodeOccurrenceIdentity.ComputeIdentifierDigest(["Проверка"]));
        Compare("KAT-3 identifier Japanese", "asmctx3-identifier", ["日本語"], CodeOccurrenceIdentity.ComputeIdentifierDigest(["日本語"]));
        Compare("KAT-4 identifier multi-part", "asmctx3-identifier", ["Alpha", "Beta"], CodeOccurrenceIdentity.ComputeIdentifierDigest(["Alpha", "Beta"]));
        Compare("KAT-5 declaration", "asmctx3-declarations", ["M:Corpus.Core.Methods.Add(System.Int32,System.Int32)"],
            CodeOccurrenceIdentity.ComputeDeclarationDigest(["M:Corpus.Core.Methods.Add(System.Int32,System.Int32)"]));
        Compare("KAT-6 shape", "asmctx3-target", ["method|0|global::System.Int32"], CodeOccurrenceIdentity.ComputeShapeDigest("method|0|global::System.Int32"));

        // KAT-7: domain separation — same field sequences, different domains differ.
        var sepA = RefEnc.Hash("asmctx3-identifier", "x");
        var sepB = RefEnc.Hash("asmctx3-declarations", "x");
        var sepC = CodeOccurrenceIdentity.ComputeIdentifierDigest(["x"]);
        var sepD = CodeOccurrenceIdentity.ComputeDeclarationDigest(["x"]);
        results.Add(VectorResult("KAT-7 domain separation (identifier vs declarations, field 'x')",
            "asmctx3-identifier|asmctx3-declarations", ["x"], "-", sepA + " | " + sepB, sepC + " | " + sepD,
            sepA != sepB && sepA == sepC && sepB == sepD));

        // KAT-8: ordering sensitivity.
        var ab = RefEnc.Hash("asmctx3-identifier", "A", "B");
        var ba = RefEnc.Hash("asmctx3-identifier", "B", "A");
        results.Add(VectorResult("KAT-8 multi-part ordering sensitivity", "asmctx3-identifier", ["A", "B", "(vs)", "B", "A"], "-", ab + " | " + ba, "-", ab != ba && ab == CodeOccurrenceIdentity.ComputeIdentifierDigest(["A", "B"]) && ba == CodeOccurrenceIdentity.ComputeIdentifierDigest(["B", "A"])));

        // KAT-9: empty-field behavior — encoding layer permits length 0; form layer rejects.
        var emptyEncoded = RefEnc.Hash("asmctx3-identifier", "");
        var formRejected = false;
        string? rejection = null;
        try { CodeOccurrenceIdentity.ComputeIdentifierDigest([""]); }
        catch (Exception exception) { formRejected = true; rejection = exception.GetType().Name; }
        results.Add(VectorResult("KAT-9 empty field: encoding computes, form layer rejects",
            "asmctx3-identifier", ["(empty)"], RefEnc.Hex(RefEnc.MessageBytes("asmctx3-identifier", [""])), emptyEncoded, rejection ?? "(no exception)",
            emptyEncoded.Length == 64 && formRejected));

        // KAT-10: lowercase hex + length + target validation.
        var valid = "asmctx3:sha256:" + new string('a', 64);
        var checks = new JsonObject
        {
            ["validAccepted"] = CodeOccurrenceIdentity.IsCanonicalTarget(valid),
            ["uppercaseRejected"] = !CodeOccurrenceIdentity.IsCanonicalTarget("asmctx3:sha256:" + new string('A', 64)),
            ["shortRejected"] = !CodeOccurrenceIdentity.IsCanonicalTarget("asmctx3:sha256:" + new string('a', 63)),
            ["wrongPrefixRejected"] = !CodeOccurrenceIdentity.IsCanonicalTarget("asmctx3:sha257:" + new string('a', 64)),
            ["lowercaseOnly"] = sepC == sepC.ToLowerInvariant() && sepC.Length == 64,
        };
        var allValid = true;
        foreach (var pair in checks) allValid &= (bool)pair.Value!;
        results.Add(new JsonObject
        {
            ["id"] = "KAT-10 lowercase-hex length and canonical-target validation",
            ["domain"] = "asmctx3", ["fields"] = new JsonArray(), ["messageHex"] = "-",
            ["expected"] = "all validation predicates true", ["actual"] = checks, ["pass"] = allValid,
        });
        return results;

        JsonObject VectorResult(string id, string dom, string[] fields, string messageHex, string expected, string actual, bool pass)
        {
            var fieldArray = new JsonArray();
            foreach (var f in fields) fieldArray.Add(f);
            return new JsonObject { ["id"] = id, ["domain"] = dom, ["fields"] = fieldArray, ["messageHex"] = messageHex, ["expected"] = expected, ["actual"] = actual, ["pass"] = pass };
        }
    }
}
