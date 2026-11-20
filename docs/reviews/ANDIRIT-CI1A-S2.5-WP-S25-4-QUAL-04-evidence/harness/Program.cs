// QUAL-04 independent reviewer harness (reviewer-owned; not a production test project).
// Exercises the real C# plugin/sink publication path, native persistence and
// fresh-process reopen against pinned production revisions. All observations are
// emitted as JSON receipts; the authoritative comparison against pre-registered
// reviewer expectations is performed by the external checker (check.py), which is
// independent of this harness.
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Frozen = AdrCi1Exp.Shared;

internal static partial class Probe
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly DateTimeOffset Started = new(2026, 11, 18, 0, 0, 0, TimeSpan.Zero);
    private static readonly CSharpCodeGraphPlugin CSharp = new();
    private static readonly List<string> Failures = [];
    private static readonly Dictionary<string, object?> Receipt = [];

    public static async Task<int> Main(string[] args)
    {
        Receipt["case"] = args[0];
        Receipt["processId"] = Environment.ProcessId;
        Receipt["database"] = args[1];
        var extra = args.Length > 3 ? args[3] : null; // fixture root where required
        Receipt["productionAssembly"] = typeof(CodeOccurrenceAuthority).Assembly.FullName;
        Receipt["csharpAssembly"] = typeof(CSharpCodeGraphPlugin).Assembly.FullName;
        Receipt["platformAssemblyCount"] = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Length;
        Receipt["andiritAssemblyReferences"] = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => Path.GetFileNameWithoutExtension(path).StartsWith("Andirit", StringComparison.Ordinal)
                || Path.GetFileNameWithoutExtension(path).StartsWith("LatticeDb", StringComparison.Ordinal))
            .Select(path => Path.GetFileName(path))
            .Order(StringComparer.Ordinal).ToArray();
        try
        {
            if (args[0].StartsWith("M", StringComparison.Ordinal) && char.IsDigit(args[0][1]))
                await Membership(args[0], args[1], args[2]);
            else if (args[0].StartsWith("A", StringComparison.Ordinal) && char.IsDigit(args[0][1]))
                await Adversarial(args[0], args[1], args[2]);
            else if (args[0] == "lifecycle")
                await Lifecycle(args[0], args[1], args[2]);
            else if (args[0] == "X1")
                await GenuineLegacy(args[0], args[1], args[2]);
            else if (args[0] == "X2")
                await Legacy("X2", args[1], args[2]);
            else if (args[0].StartsWith("X", StringComparison.Ordinal) && char.IsDigit(args[0][1]))
                await Legacy(args[0], args[1], args[2]);
            else if (args[0].StartsWith("F254-", StringComparison.Ordinal) || args[0].StartsWith("b3", StringComparison.Ordinal))
                await Preservation(args[0], args[1], extra ?? args[2]);
            else
                throw new ArgumentException("unknown case " + args[0]);
        }
        catch (Exception error)
        {
            Receipt["unhandled"] = error.ToString();
            Failures.Add("Unhandled probe exception");
        }

        Receipt["failures"] = Failures;
        File.WriteAllText(args[2], JsonSerializer.Serialize(Receipt, Json));
        return Failures.Count == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string assertion)
    {
        if (!condition)
            Failures.Add(assertion);
    }

    private static string Digest(params string[] fields)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> size = stackalloc byte[4];
        foreach (var field in fields)
        {
            var utf8 = Encoding.UTF8.GetBytes(field);
            BinaryPrimitives.WriteInt32BigEndian(size, utf8.Length);
            hash.AppendData(size);
            hash.AppendData(utf8);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static string FileDigest(byte[] data) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(data));

    // Matches CSharpProjectDiscovery.ProjectUnitId exactly: the production unit id
// is "csharp:project:" + lowercase hex SHA-256 of the UTF-8 project path.
    private static string ProjectUnit(string path) =>
        "csharp:project:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(path)));

    private static CodeGraphPluginContext Context(CodeRepositoryId repo, string run, Dictionary<string, byte[]> files) =>
        new(repo, null, new CodeIndexRunId(run), files
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new CodeGraphSource(pair.Key, FileDigest(pair.Value),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(pair.Value, writable: false))))
            .ToArray());

    private static async Task Run(LatticeCodeGraphStore store, CodeRepositoryId repo, string run,
        CodePluginId[] plugins, Func<Task> stage)
    {
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, new(run), Started, plugins: plugins));
        await stage();
        await store.CompleteIndexRunAsync(
            new(repo, new(run), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), plugins),
            new(repo, new(run), []));
    }

    private static async Task PublishCSharp(LatticeCodeGraphStore store, CodeRepositoryId repo, string run,
        Dictionary<string, byte[]> files)
    {
        await Run(store, repo, run, [CSharp.Id], async () =>
        {
            await using var sink = new CodeGraphIngestionSink(store);
            await using var session = await CSharp.CreateSessionAsync(Context(repo, run, files));
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        });
    }

    // Reviewer-owned fixture: a solution of independent C# projects, each with its
    // own namespace/type/method. Project i contains calls[i] internal call sites
    // (Get invoked by Call/Call2...). `missing` projects are declared in the
    // solution but absent from the source set.
    internal static Dictionary<string, byte[]> MembershipFixture(int projects, int missing, int[] calls,
        bool reordered = false)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var solution = new StringBuilder("<Solution>");
        for (var i = 0; i < projects + missing; i++)
        {
            var path = $"P{i}/P{i}.csproj";
            solution.Append("<Project Path=\"").Append(path).Append("\"/>");
            if (i >= projects)
                continue;
            files[path] = Encoding.UTF8.GetBytes("<Project Sdk=\"Microsoft.NET.Sdk\" />");
            var body = new StringBuilder("namespace P").Append(i)
                .Append("; public static class Value { public static int Get() => 1; ");
            for (var c = 0; c < calls[i]; c++)
                body.Append("public static int Call").Append(c).Append("() => Get(); ");
            body.Append('}');
            files[$"P{i}/Value.cs"] = Encoding.UTF8.GetBytes(body.ToString());
        }

        files["Membership.slnx"] = Encoding.UTF8.GetBytes(solution.Append("</Solution>").ToString());
        if (reordered)
            // Reverse the file dictionary insertion order only: solution text and
            // project paths are unchanged; the plugin must order projects itself.
            files = files.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        return files;
    }

    // Independent target derivation for a fixture method from the live verified
    // universe (documented asmctx3 = hash(compctx1-mapped context, identifier,
    // declaration, shape); mapping contract §2).
    internal static async Task<CodeOccurrenceTargetId> FixtureTargetAsync(CodeRepositoryId repo, string run,
        Dictionary<string, byte[]> files, string projectPath, string typeName, string methodName)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
        var defining = analysis.Universe.Entries.Single(entry =>
            entry.Descriptor.ProjectUnitId == ProjectUnit(projectPath));
        var method = (IMethodSymbol)defining.Compilation.GetTypeByMetadataName(typeName)!
            .GetMembers(methodName).Single();
        var mapped = defining.MapR2FaithfulCompctx1();
        return CodeOccurrenceIdentity.ComputeTargetId(
            mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]),
            CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
                string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))));
    }

    private static object DescribeResult(CodeOccurrenceQueryResult result) => new
    {
        availability = result.Availability.ToString(),
        isComplete = result.IsComplete,
        occurrences = result.Occurrences.Select(fact => new
        {
            id = fact.Id.Value,
            sourceUnit = fact.SourceUnitId,
            target = fact.TargetId.Value,
            span = new { fact.Span.Path, fact.Span.StartLine, fact.Span.StartColumn, fact.Span.EndLine, fact.Span.EndColumn },
            category = fact.Category.ToString(),
            contributors = fact.Contributors.Select(c => c.IndexUnitId).ToArray(),
            contributorPlugin = fact.Contributors.Select(c => c.PluginId.Value).ToArray(),
        }).ToArray(),
        publication = result.Publication?.IndexRunId.Value?.ToString(),
        coverage = result.Coverage is { } coverage
            ? new
            {
                invocationFacts = coverage.InvocationFacts,
                unsupportedCategories = coverage.UnsupportedCategories,
                state = coverage.State.ToString(),
                scopeId = coverage.ScopeId,
                universeId = coverage.UniverseId,
                declaredUnitCount = coverage.DeclaredUnitCount,
                verifiedUnitCount = coverage.VerifiedUnitCount,
                external = coverage.External,
                ambiguous = coverage.Ambiguous,
                unresolved = coverage.Unresolved,
                unsupported = coverage.Unsupported,
                conditionalInactive = coverage.ConditionalInactive,
                reasons = coverage.UnavailableReasons.OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => p.Key + "=" + p.Value.ToString(CultureInfo.InvariantCulture)).ToArray(),
            }
            : null,
        unitCoverage = result.UnitCoverage.Select(unit => new
        {
            indexUnit = unit.IndexUnitId,
            state = unit.State.ToString(),
            invocationFacts = unit.InvocationFacts,
            unsupportedCategories = unit.UnsupportedCategories,
            external = unit.External,
            ambiguous = unit.Ambiguous,
            unresolved = unit.Unresolved,
            unsupported = unit.Unsupported,
            conditionalInactive = unit.ConditionalInactive,
        }).ToArray(),
        refusal = result.RefusalReason,
        examined = result.Examined,
        totalMatches = result.TotalMatches,
    };

    internal static object ReadNative(string path)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginReadTransaction();
        var output = new SortedDictionary<string, object>();
        foreach (var label in new[]
                 {
                     "AndiritOccurrenceMeta", "AndiritOccurrencePage", "AndiritOccurrenceUnit"
                 })
        {
            using var command = database.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
            using var result = command.Execute(txn);
            output[label] = result.ReadAll().Select(row => new
            {
                key = row.GetValue(0).AsString(),
                payload = row.GetValue(1).AsString()
            }).OrderBy(row => row.key, StringComparer.Ordinal).ToArray();
        }

        txn.Commit();
        return output;
    }

    // Decoded native occurrence rows: coverage, authority and facts per unit.
    internal static JsonElement[] DecodedUnits(string path)
    {
        using var raw = JsonDocument.Parse(JsonSerializer.Serialize(ReadNative(path)));
        return raw.RootElement.GetProperty("AndiritOccurrenceUnit").EnumerateArray()
            .Select(row => JsonDocument.Parse(Convert.FromBase64String(
                row.GetProperty("payload").GetString()!)).RootElement.Clone())
            .ToArray();
    }

    internal static object NativeRowSummary(string path)
    {
        // Schema-tolerant scalar readers: the persisted row payload shape is
        // observed, never assumed (string- or number-encoded scalars both work).
        static string? Text(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString()
            : null;
        static int Int(JsonElement element, string owner, string name) => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(owner, out var child) && child.ValueKind == JsonValueKind.Object
            && child.TryGetProperty(name, out var value)
            ? value.ValueKind == JsonValueKind.Number ? value.GetInt32() : int.TryParse(value.ToString(), out var parsed) ? parsed : 0
            : 0;
        var units = DecodedUnits(path).Select(unit => new
        {
            indexUnitId = Text(unit, "IndexUnitId"),
            facts = unit.GetProperty("Facts").GetArrayLength(),
            factIds = unit.GetProperty("Facts").EnumerateArray()
                .Select(fact => fact.GetProperty("Id").GetProperty("Value").GetString())
                .Order(StringComparer.Ordinal).ToArray(),
            coverage = new
            {
                declared = Int(unit, "Coverage", "DeclaredUnitCount"),
                verified = Int(unit, "Coverage", "VerifiedUnitCount"),
                facts = Int(unit, "Coverage", "InvocationFacts"),
                state = Text(unit.TryGetProperty("Coverage", out var coverage0) ? coverage0 : default, "State"),
                external = Int(unit, "Coverage", "External"),
                unresolved = Int(unit, "Coverage", "Unresolved"),
                unsupported = Int(unit, "Coverage", "Unsupported"),
                unsupportedCategories = Int(unit, "Coverage", "UnsupportedCategories"),
            },
            authoritySourceUnit = unit.GetProperty("Authority") is JsonElement { ValueKind: JsonValueKind.Object } authority
                ? Text(authority, "SourceUnitId")
                : null,
            authorityCoverage = unit.GetProperty("Authority") is JsonElement { ValueKind: JsonValueKind.Object } envelope
                ? new
                {
                    declared = Int(envelope, "Coverage", "DeclaredUnitCount"),
                    verified = Int(envelope, "Coverage", "VerifiedUnitCount"),
                    facts = Int(envelope, "Coverage", "InvocationFacts"),
                }
                : null,
            authorityMembers = unit.GetProperty("Authority") is JsonElement { ValueKind: JsonValueKind.Object } members
                ? members.GetProperty("Members").GetArrayLength()
                : 0,
            // Full decoded row: unarguable evidence of the persisted shape.
            raw = unit.Clone(),
        }).OrderBy(unit => unit.indexUnitId, StringComparer.Ordinal).ToArray();
        using var raw = JsonDocument.Parse(JsonSerializer.Serialize(ReadNative(path)));
        var meta = raw.RootElement.GetProperty("AndiritOccurrenceMeta").EnumerateArray()
            .Select(row => JsonDocument.Parse(Convert.FromBase64String(
                row.GetProperty("payload").GetString()!)).RootElement.Clone())
            .ToArray();
        return new
        {
            unitCount = meta.Select(m => m.GetProperty("UnitCount").GetInt32()).ToArray(),
            factCount = meta.Select(m => m.GetProperty("FactCount").GetInt32()).ToArray(),
            completeCoverage = meta.Select(m => m.GetProperty("CompleteCoverage").GetBoolean()).ToArray(),
            requiresAuthority = meta.Select(m => m.GetProperty("RequiresAuthority").GetBoolean()).ToArray(),
            units
        };
    }
}