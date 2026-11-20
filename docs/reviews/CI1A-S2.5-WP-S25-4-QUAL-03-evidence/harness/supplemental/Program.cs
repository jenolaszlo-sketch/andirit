using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Frozen = AdrCi1Exp.Shared;

internal static partial class Probe
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly DateTimeOffset Started = new(2026, 11, 16, 0, 0, 0, TimeSpan.Zero);
    private static readonly CSharpCodeGraphPlugin CSharp = new();
    private static readonly List<string> Failures = [];
    private static readonly Dictionary<string, object?> Receipt = [];

    public static async Task<int> Main(string[] args)
    {
        Receipt["case"] = args[0];
        Receipt["processId"] = Environment.ProcessId;
        Receipt["database"] = args[1];
        try
        {
            if (args[0].StartsWith("review-", StringComparison.Ordinal))
                await ReviewerPublication(args[0], args[1], args[3]);
            else if (args[0].StartsWith("b3", StringComparison.Ordinal))
                await Binding(args[0], args[1], args[3]);
            else if (args[0].StartsWith("control-", StringComparison.Ordinal))
                await Additional(args[0], args[1], args[3]);
            else if (args[0].StartsWith("state-", StringComparison.Ordinal))
                await StateControls(args[0], args[1], args[3]);
            else if (args[0] == "identity-discriminators")
                IdentityDiscriminators();
            else if (args[0].StartsWith("multiple-definitions-", StringComparison.Ordinal))
                await MultipleDefinitions(args[0], args[1], args[3]);
            else if (args[0].StartsWith("coverage-boundary-", StringComparison.Ordinal))
                await CoverageBoundary(args[0], args[1], args[3]);
            else if (args[0].StartsWith("m54-", StringComparison.Ordinal))
                await MissingDefiningContinuity(args[0], args[1]);
            else if (args[0] == "d4-schedule")
                await Schedule(args[1], args[3]);
            else if (args[0] == "d3")
                await GraphCase(args[1], args[2], args[3]);
            else if (args[0].StartsWith("d5-", StringComparison.Ordinal))
                Tamper(args[0], args[1]);
            else
                await Contributors(args[0], args[1], args[3]);
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

    private static string FileDigest(byte[] data) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(data));

    private static string Usage(string repo, string plugin, string owner, CodeLocation span, string sourceHash,
        string document, string target) =>
        Digest("usage4", repo, plugin, "usage4-b2c-exp-r2-20261101", owner, span.Path, sourceHash,
            span.StartLine.ToString(), span.StartColumn.ToString(), span.EndLine.ToString(), span.EndColumn.ToString(),
            document, target, "method-invocation");

    private static object ReadNative(string path)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginReadTransaction();
        var output = new SortedDictionary<string, object>();
        foreach (var label in new[] { "AndiritOccurrenceMeta", "AndiritOccurrencePage", "AndiritOccurrenceUnit" })
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

    private static CodeGraphPluginContext Context(CodeRepositoryId repo, string run, Dictionary<string, byte[]> files) =>
        new(repo, null, new CodeIndexRunId(run), files.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(file => new CodeGraphSource(file.Key, FileDigest(file.Value),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(file.Value, writable: false)))).ToArray());

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

    private static async Task Binding(string phase, string path, string fixtures)
    {
        const string doc = "M:QualifierDependency.Api.Mark";
        const string dependency = "Dependency/Api.cs";
        var repo = new CodeRepositoryId("review:s254:binding");
        var files = Directory.EnumerateFiles(fixtures, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(fixtures, file).Replace('\\', '/'), File.ReadAllBytes);
        var owner = ProjectUnit("Caller/Caller.csproj");
        var mode = phase[3..];
        var baseline = argsBaseline();
        bool argsBaseline() => Environment.GetEnvironmentVariable("S254_QUAL02_PROFILE") == "baseline";
        var mixed = mode.StartsWith("mixed-", StringComparison.Ordinal);
        if (mixed)
            mode = mode[6..];
        if (mode == "missing")
            files.Remove(dependency);

        if (mode == "ambiguous" || mixed)
        {
            files[dependency] = Encoding.UTF8.GetBytes(
                "namespace QualifierDependency; public static class Api { public static int Mark() => 7; " +
                "public static int Pick(string value) => 1; public static int Pick(char[] value) => 2; }");
            if (mixed)
                files["Caller/Diagnostics.cs"] = Encoding.UTF8.GetBytes(
                    "namespace QualifierCaller; public static class Diagnostics { public static void Check() { " +
                    "QualifierDependency.Api.Pick(null); QualifierDependency.Api.DoesNotExist(); } }");
            else
                files["Caller/Fixture1.cs"] = Encoding.UTF8.GetBytes(
                Encoding.UTF8.GetString(files["Caller/Fixture1.cs"]).Replace(
                    "string.Concat(\"left\", \"right\")", "QualifierDependency.Api.Pick(null)"));
        }
        Receipt["inputHashes"] = files.ToDictionary(pair => pair.Key, pair => FileDigest(pair.Value));
        Receipt["sourceInputs"] = files.ToDictionary(pair => pair.Key, pair => Encoding.UTF8.GetString(pair.Value));
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "binding", files));
        var caller = analysis.Projects.Single(project => project.Project.Path == "Caller/Caller.csproj").Verified;
        var defining = analysis.Projects.Single(project => project.Project.Path == "Dependency/Dependency.csproj").Verified;
        Receipt["scope"] = analysis.Universe.Scope.Units;
        Receipt["scopeRefusals"] = analysis.Universe.Refusals;
        Receipt["missingUnits"] = analysis.Universe.MissingScopeUnits;

        if (caller is null)
        {
            Check(mode == "missing", "Caller verified compilation is available");
            Receipt["callerVerified"] = false;
            return;
        }

        var symbols = caller.Compilation.SyntaxTrees.SelectMany(tree =>
        {
            var model = caller.Compilation.GetSemanticModel(tree);
            return tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Select(site =>
                {
                    var info = model.GetSymbolInfo(site);
                    var target = info.Symbol as IMethodSymbol;
                    var resolution = target is null ? null :
                        analysis.Universe.ResolveDefining(target, null, caller.Compilation);
                    return new
                    {
                        path = tree.FilePath,
                        span = site.GetLocation().GetLineSpan().Span.ToString(),
                        input = site.ToString(),
                        symbol = target?.ToDisplayString(),
                        assembly = target?.ContainingAssembly.Identity.ToString(),
                        candidates = info.CandidateSymbols.Select(candidate => candidate.ToDisplayString()).ToArray(),
                        candidateReason = info.CandidateReason.ToString(),
                        resolution = resolution?.Status.ToString(),
                        detail = resolution?.Detail,
                        definingOwner = resolution?.Defining?.Descriptor.ProjectUnitId
                    };
                }).ToArray();
        }).ToArray();
        Receipt["symbols"] = symbols;
        Receipt["callerDescriptor"] = Describe(caller.Descriptor);
        Receipt["callerErrors"] = caller.Compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToArray();
        Check(caller.Descriptor.ProjectUnitId == owner, "Caller unit matches the project-path identity");
        if (mode == "missing")
        {
            Check(symbols.All(symbol => symbol.resolution is not "Complete"), "Missing binding cannot resolve");
            return;
        }

        if (mode == "ambiguous")
        {
            var ambiguous = symbols.Single(symbol => symbol.input.Contains(".Pick(", StringComparison.Ordinal));
            Receipt["expectedAmbiguous"] = new[] { "Pick(string)", "Pick(char[])" };
            Receipt["actualAmbiguous"] = ambiguous;
            Check(ambiguous.symbol is null && ambiguous.candidates.Length == 2 &&
                ambiguous.candidateReason == "OverloadResolutionFailure", "The C# overload control has two candidates and no binding");
            var extraction = VerifiedCSharpOccurrenceExtractor.Extract(analysis.Universe, new(
                repo, CSharp.Id, caller, _ => Digest("asmctx3-identifier", "Mark"),
                _ => Digest("asmctx3-declarations", doc),
                _ => Digest("asmctx3-target", "method|0|"), _ => doc));
            Receipt["ambiguousCoverage"] = extraction.Coverage;
            Check(extraction.Coverage.Ambiguous == 1 && extraction.Coverage.InvocationFacts == 50,
                "The ambiguous site registers coverage without creating an occurrence");
            return;
        }

        if (defining is null)
            throw new InvalidDataException("Dependency compilation unavailable.");

        var frozen = Frozen.CompilationProvenance.Describe(repo.Value, ProjectUnit("Dependency/Dependency.csproj"),
            defining.Compilation, [(dependency, files[dependency])]);
        var parts = new[] { "asmctx3-context", "compctx1", frozen.DescriptorDigest,
            frozen.MetadataIdentity, frozen.ModuleName, Digest("asmctx3-identifier", "Mark"),
            Digest("asmctx3-declarations", doc), Digest("asmctx3-target", "method|0|") };
        var target = new CodeOccurrenceTargetId("asmctx3:sha256:" + Digest(parts));
        Receipt["expectedDefiningCompctx1Fields"] = frozen;
        Receipt["expectedTargetParts"] = parts;
        Receipt["expectedTarget"] = target.Value;
        Receipt["definingDescriptor"] = Describe(defining.Descriptor);
        Receipt["observedDefiningCompctx1"] = Describe(defining.MapR2FaithfulCompctx1());
        Check(frozen.DescriptorDigest == defining.MapR2FaithfulCompctx1().DescriptorDigest,
            "Frozen compctx1 from raw dependency bytes and live reference inputs matches mapped dependency");
        Check(caller.Compilation.References.OfType<CompilationReference>()
            .Any(reference => ReferenceEquals(reference.Compilation, defining.Compilation)),
            "Caller references exact live defining compilation");
        Check(!defining.Compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error),
            "Dependency compiles without errors");

        var expected = new SortedDictionary<string, object>();
        var expectedOrder = new List<(CodeLocation Span, string Id)>();
        foreach (var file in files.Where(file => file.Key.StartsWith("Caller/Fixture", StringComparison.Ordinal)))
        {
            var lines = Encoding.UTF8.GetString(file.Value).Split('\n');
            for (var line = 0; line < lines.Length; line++)
            {
                var column = lines[line].IndexOf("Api.Mark();", StringComparison.Ordinal) + 5;
                if (column == 4)
                    continue;
                var span = new CodeLocation(file.Key, line + 1, column, line + 1, column + 4);
                var id = Usage(repo.Value, CSharp.Id.Value, owner, span, FileDigest(file.Value), doc, target.Value);
                expectedOrder.Add((span, id));
                expected.Add(id,
                    new { span, sourceHash = FileDigest(file.Value), owner, doc, target = target.Value });
            }
        }

        Receipt["expectedFacts"] = expected;
        var externalSite = caller.Compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Single(site => site.Expression.ToString() == "string.Concat");
        var externalSymbol = (IMethodSymbol)caller.Compilation.GetSemanticModel(externalSite.SyntaxTree)
            .GetSymbolInfo(externalSite).Symbol!;
        var externalReference = caller.Compilation.GetMetadataReference(externalSymbol.ContainingAssembly)
            as PortableExecutableReference ?? throw new InvalidDataException("No PE reference for external control.");
        var referencePath = externalReference.FilePath ?? throw new InvalidDataException("No captured PE path.");
        var referenceIdentity = "file|" + referencePath.Replace('\\', '/') + "|" + FileDigest(File.ReadAllBytes(referencePath))[7..];
        Check(caller.Descriptor.References.Contains(referenceIdentity), "The exact PE reference is represented by its independent path and byte hash");
        var verified = analysis.Universe.ResolveDefining(externalSymbol, null, caller.Compilation).Status.ToString();
        var withoutCaller = analysis.Universe.ResolveDefining(externalSymbol).Status.ToString();
        var withoutCallerUniverse = VerifiedCSharpUniverse.Create(repo.Value,
            VerifiedCSharpScope.Declare(repo.Value, analysis.Universe.Scope.Units.Where(unit => unit.ProjectUnitId != owner)),
            [defining]);
        var unverifiedCaller = withoutCallerUniverse.ResolveDefining(externalSymbol, null, caller.Compilation).Status.ToString();
        var expectedClass = baseline ? "MissingDefiningCompilation" : "UnsupportedExternalEvidence";
        var expectedExternal = baseline ? 0 : 1;
        var expectedUnresolved = (baseline ? 1 : 0) + (mixed ? 1 : 0);
        var expectedAmbiguous = mixed ? 1 : 0;
        Receipt["counterOracle"] = new { expectedClass, expectedExternal, expectedUnresolved, expectedAmbiguous, externalOccurrences = 0 };
        Receipt["bindingBoundary"] = new { verified, withoutCaller, unverifiedCaller, referenceIdentity,
            exactPeReference = caller.Compilation.References.Any(reference => ReferenceEquals(reference, externalReference)) };
        Check(verified == expectedClass && withoutCaller == "MissingDefiningCompilation" &&
            unverifiedCaller == "MissingDefiningCompilation", "Zero live definitions requires an exact PE binding from a verified caller");
        var initialExtraction = VerifiedCSharpOccurrenceExtractor.Extract(analysis.Universe, new(
            repo, CSharp.Id, caller, _ => parts[5], _ => parts[6], _ => parts[7], _ => doc));
        Receipt["initialCoverage"] = initialExtraction.Coverage;
        Check(initialExtraction.Coverage.External == expectedExternal && initialExtraction.Coverage.Unresolved == expectedUnresolved &&
            initialExtraction.Coverage.Ambiguous == expectedAmbiguous && initialExtraction.Coverage.InvocationFacts == 50,
            "Initial extraction classification counts match the independent expectations");
        var dbOptions = new Andirit.LatticeDb.LatticeDbStoreOptions { UseNativeTraversal = true };
        using (var store = new LatticeCodeGraphStore(path, dbOptions))
        {
            if (mode == "publish")
                await PublishCSharp(store, repo, "binding", files);

            var unbound = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 100));
            Receipt["missingAuthority"] = unbound;
            Check(unbound.Availability == CodeOccurrenceAvailability.AnalysisUnavailable && !unbound.IsComplete,
                "Missing authority explicitly refuses analysis");
            var result = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                new(repo, target, maxResults: 100), analysis.Universe, caller.Compilation));
            if (result.Publication is { } publication)
            {
                var stale = new CodeGraphPublication(repo, new("older"), publication.SnapshotIdentity,
                    publication.IsConsistentSnapshot, publication.IndexIdentity);
                var staleResult = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                    new(repo, target, stale), analysis.Universe, caller.Compilation));
                Receipt["stalePublication"] = staleResult;
                Check(staleResult.Availability == CodeOccurrenceAvailability.StalePublication &&
                    staleResult.Occurrences.Count == 0 && !staleResult.IsComplete, "Stale publication refuses explicitly");
            }
            Receipt["result"] = result;
            if (mode == "reopen")
            {
                var knownOrder = expectedOrder.OrderBy(site => site.Span.Path, StringComparer.Ordinal)
                    .ThenBy(site => site.Span.StartLine).ThenBy(site => site.Span.StartColumn)
                    .ThenBy(site => site.Span.EndLine).ThenBy(site => site.Span.EndColumn)
                    .ThenBy(site => site.Id, StringComparer.Ordinal).Select(site => site.Id).ToArray();
                Receipt["expectedOrderedIds"] = knownOrder;
                var pages = new List<object>();
                var hits = new List<string>();
                string? continuation = null;
                var clock = new System.Diagnostics.Stopwatch();
                for (var index = 0; index < 8; index++)
                {
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(
                        new(repo, target, maxResults: 7) { ContinuationToken = continuation },
                        analysis.Universe, caller.Compilation);
                    clock.Restart();
                    var page = await store.FindOccurrencesAsync(query);
                    clock.Stop();
                    pages.Add(new { elapsedMs = clock.Elapsed.TotalMilliseconds, page });
                    hits.AddRange(page.Occurrences.Select(hit => hit.Id.Value));
                    Check(page.Occurrences.Count <= 7 && page.Examined <= 50, "Page and target-local examined bounds");
                    continuation = page.NextContinuationToken;
                    if (continuation is null)
                    {
                        Check(page.IsComplete, "Final continuation result is complete");
                        break;
                    }
                    Check(!page.IsComplete, "Intermediate page is not reported complete");
                }
                Receipt["pages"] = pages;
                Check(continuation is null && knownOrder.SequenceEqual(hits),
                    "Continuation traverses all expected IDs in independent full span order");
                var invalid = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                    new(repo, target, maxResults: 7) { ContinuationToken = "invalid-token" },
                    analysis.Universe, caller.Compilation));
                Receipt["invalidToken"] = invalid;
                Check(invalid.Occurrences.Count == 0 && !invalid.IsComplete &&
                    invalid.Continuation == CodeOccurrenceContinuationState.Invalid, "Invalid continuation refuses");
            }
            Check(expected.Count == 50 && expected.Keys.SequenceEqual(
                result.Occurrences.Select(fact => fact.Id.Value).Order(StringComparer.Ordinal)),
                "All 50 independently enumerated usage IDs match end-to-end");
            Check(result.Occurrences.All(fact => fact.SourceUnitId == owner &&
                fact.TargetId == target), "Every usage retains caller ownership and dependency target");
            var external = symbols.Single(symbol => symbol.input.StartsWith("string.Concat", StringComparison.Ordinal));
            Receipt["externalExpected"] = "External/UnsupportedExternalEvidence, zero minted usages for the metadata-only target";
            Receipt["externalObserved"] = external;
            Check(external.assembly?.StartsWith("System.Private.CoreLib,", StringComparison.Ordinal) == true,
                "The external invocation actually binds to host metadata");
            Check(external.resolution == expectedClass && result.Coverage?.External == expectedExternal &&
                result.Coverage?.Unresolved == expectedUnresolved && result.Coverage?.Ambiguous == expectedAmbiguous &&
                result.Occurrences.Count == 50, "Query classification, counters and inventory match the oracle");
        }

        var native = ReadNative(path);
        Receipt["native"] = native;
        using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(native));
        var ownerRow = parsed.RootElement.GetProperty("AndiritOccurrenceUnit").EnumerateArray()
            .Single(row => row.GetProperty("key").GetString()!.EndsWith("\n" + owner, StringComparison.Ordinal));
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(ownerRow.GetProperty("payload").GetString()!));
        using var stored = JsonDocument.Parse(payload);
        var facts = stored.RootElement.GetProperty("Facts").EnumerateArray().ToArray();
        var persisted = stored.RootElement.GetProperty("Coverage");
        Check(persisted.GetProperty("External").GetInt32() == expectedExternal &&
            persisted.GetProperty("Unresolved").GetInt32() == expectedUnresolved &&
            persisted.GetProperty("Ambiguous").GetInt32() == expectedAmbiguous &&
            persisted.GetProperty("InvocationFacts").GetInt32() == 50,
            "Raw persisted native counters agree with initial extraction and the bound query");
        Receipt["decodedCallerUnit"] = stored.RootElement.Clone();
        var storedIds = facts.Select(fact => fact.GetProperty("Id").GetProperty("Value").GetString())
            .Order(StringComparer.Ordinal);
        Check(expected.Keys.SequenceEqual(storedIds) && facts.All(fact =>
            fact.GetProperty("TargetDocumentId").GetString() == doc &&
            fact.GetProperty("TargetId").GetProperty("Value").GetString() == target.Value &&
            fact.GetProperty("SourceUnitId").GetString() == owner &&
            fact.GetProperty("DeclarationSpan").GetProperty("Path").GetString() == dependency),
            "Native unit records exactly the independently enumerated IDs, target, caller and dependency source");
    }

    private static string ProjectUnit(string path) => "csharp:project:" + FileDigest(Encoding.UTF8.GetBytes(path))[7..];

    private static object Describe(CompilationProvenanceDescriptor descriptor) => new
    {
        descriptor.ContractVersion,
        descriptor.RepositoryId,
        descriptor.ProjectUnitId,
        descriptor.AssemblyName,
        descriptor.MetadataIdentity,
        descriptor.ModuleName,
        descriptor.OptionsDigest,
        descriptor.Sources,
        descriptor.References,
        descriptor.DescriptorDigest
    };
}












