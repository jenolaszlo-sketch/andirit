using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Frozen = AdrCi1Exp.Shared;

internal static partial class Probe
{
    private const string DependencyPath = "Dependency/Api.cs";
    private const string DependencyProject = "Dependency/Dependency.csproj";
    private const string CallerProject = "Caller/Caller.csproj";
    private const string MarkDocument = "M:QualifierDependency.Api.Mark";

    private static Dictionary<string, byte[]> LoadControlFiles(string path) =>
        Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToDictionary(
            file => Path.GetRelativePath(path, file).Replace('\\', '/'), File.ReadAllBytes);

    private static string MarkTarget(string repository, VerifiedCSharpCompilation defining, byte[] source)
    {
        var frozen = Frozen.CompilationProvenance.Describe(repository, ProjectUnit(DependencyProject),
            defining.Compilation, [(DependencyPath, source)]);
        return "asmctx3:sha256:" + Digest("asmctx3-context", "compctx1", frozen.DescriptorDigest,
            frozen.MetadataIdentity, frozen.ModuleName, Digest("asmctx3-identifier", "Mark"),
            Digest("asmctx3-declarations", MarkDocument), Digest("asmctx3-target", "method|0|"));
    }

    private static async Task Additional(string phase, string database, string fixture)
    {
        var parts = phase.Split('-');
        var profile = parts[1];
        var publish = parts[2] == "publish";
        var repository = new CodeRepositoryId("review:s254:control:" + profile);
        var files = LoadControlFiles(fixture);
        if (profile == "provenance")
        {
            await Provenance(repository, files);
            return;
        }

        var partial = profile is "incomplete" or "declared" or "multiple" or "mixedmissing";
        var missingCount = profile is "multiple" or "mixedmissing" ? 2 : 1;
        var snippet = ReviewerSites.TryGetValue(profile, out var annotated) ? annotated.Source : profile switch
        {
            "ctor" => "_ = new QualifierDependency.Value();",
            "property" => "_ = QualifierDependency.Value.Count;",
            "reference" => "System.Func<int> method = QualifierDependency.Api.Mark;",
            "local" => "static int Local() => 3; _ = Local();",
            "inactive" => "\n#if DEBUG\nQualifierDependency.Api.Mark();\n#endif\n",
            _ => ""
        };
        files[DependencyPath] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files[DependencyPath]) +
             "\npublic sealed class Value { public Value() {} public static int Count => 5; public int Count2 {get;set;} public Value Next => this; public int this[int i] => 0; public static int Field; } public sealed class Choice { public Choice(string x) {} public Choice(System.Text.StringBuilder x) {} }\n");
        if (profile == "empty" || partial)
            foreach (var file in files.Keys.Where(key => key.StartsWith("Caller/Fixture", StringComparison.Ordinal)).ToArray())
                files[file] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files[file])
                    .Replace("QualifierDependency.Api.Mark();", "", StringComparison.Ordinal)
                    .Replace("_ = string.Concat(\"left\", \"right\");", "", StringComparison.Ordinal));
        files["Caller/Review.cs"] = Encoding.UTF8.GetBytes(
            "namespace QualifierCaller; public static class Review { public static void Run() { " + snippet + " } }");
        if (partial)
            files["Review.slnx"] = Encoding.UTF8.GetBytes(
                "<Solution><Project Path=\"Caller/Caller.csproj\" /><Project Path=\"Dependency/Dependency.csproj\" />" +
                "<Project Path=\"Absent/Absent.csproj\" /></Solution>");
        if (profile is "declared" or "mixedmissing")
            files["Absent/Absent.csproj"] = Encoding.UTF8.GetBytes(
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework>" +
                "</PropertyGroup><ItemGroup><Compile Include=\"Missing.cs\" /></ItemGroup></Project>");
        if (missingCount == 2)
            files["Review.slnx"] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files["Review.slnx"])
                .Replace("</Solution>", "<Project Path=\"Absent2/Absent2.csproj\" /></Solution>"));
        Receipt["siteInventory"] = ReviewerSites.TryGetValue(profile, out var inventory) ? inventory.Sites : [];
        Receipt["sources"] = files.ToDictionary(pair => pair.Key, pair => Encoding.UTF8.GetString(pair.Value));
        Receipt["hashes"] = files.ToDictionary(pair => pair.Key, pair => FileDigest(pair.Value));
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repository, phase, files));
        var caller = analysis.Projects.Single(project => project.Project.Path == CallerProject).Verified!;
        var defining = analysis.Projects.Single(project => project.Project.Path == DependencyProject).Verified!;
        Receipt["scope"] = new { analysis.Universe.IsComplete, analysis.Universe.Scope.Units,
            analysis.Universe.Refusals, analysis.Universe.MissingScopeUnits };
        if (partial)
            Check(analysis.Universe.MissingScopeUnits.Order().SequenceEqual((missingCount == 2 ? new[] { ProjectUnit("Absent/Absent.csproj"), ProjectUnit("Absent2/Absent2.csproj") } : new[] { ProjectUnit("Absent/Absent.csproj") }).Order()),
                "Exactly the independently identified declared unit is missing verified evidence");
        var expectedUnsupported = ReviewerSites.TryGetValue(profile, out var sites) ? sites.Sites.Length : profile is "ctor" or "property" or "reference" or "local" ? 1 : 0;
        var expectedInactive = profile == "inactive" ? 1 : 0;
        var expectedFacts = profile == "empty" || partial ? 0 : 50;
        Receipt["expected"] = new { expectedUnsupported, expectedInactive, expectedFacts };
        var extraction = VerifiedCSharpOccurrenceExtractor.Extract(analysis.Universe, new(
            repository, CSharp.Id, caller, _ => Digest("asmctx3-identifier", "Mark"),
            _ => Digest("asmctx3-declarations", MarkDocument), _ => Digest("asmctx3-target", "method|0|"),
            _ => MarkDocument));
        Receipt["initialCoverage"] = extraction.Coverage;
        Receipt["authorityIssued"] = extraction.Authority is not null;
        var controlTree = caller.Compilation.SyntaxTrees.Single(tree => tree.FilePath == "Caller/Review.cs");
        var model = caller.Compilation.GetSemanticModel(controlTree);
        Receipt["initialFacts"] = extraction.Facts;
        Receipt["diagnostics"] = caller.Compilation.GetDiagnostics().Select(d => d.ToString()).ToArray();
        Receipt["controlSymbols"] = controlTree.GetRoot().DescendantNodes().Where(node =>
            node is ObjectCreationExpressionSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax)
            .Select(node => new { syntax = node.ToString(), span = node.GetLocation().GetLineSpan().Span.ToString(),
                operation = model.GetOperation(node)?.Kind.ToString(), symbol = model.GetSymbolInfo(node).Symbol?.ToDisplayString(),
                kind = model.GetSymbolInfo(node).Symbol?.Kind.ToString() }).ToArray();
        Check(extraction.Coverage.Unsupported == expectedUnsupported && extraction.Coverage.UnsupportedCategories == expectedUnsupported &&
            extraction.Coverage.ConditionalInactive == expectedInactive &&
            extraction.Coverage.InvocationFacts == expectedFacts,
            "Initial unsupported and inactive categories and occurrence counts match the independent matrix oracle");

        var id = new CodeOccurrenceTargetId(MarkTarget(repository.Value, defining, files[DependencyPath]));
        using (var store = new LatticeCodeGraphStore(database))
        {
            if (publish)
                await PublishCSharp(store, repository, "controls", files);
            var query = new CodeOccurrenceQuery(repository, id, maxResults: 100);
            if (partial)
            {
                try
                {
                    _ = VerifiedCSharpOccurrenceAuthority.Bind(query, analysis.Universe, caller.Compilation);
                    Check(false, "Incomplete current scope must not issue query authority");
                }
                catch (InvalidDataException error)
                {
                    Receipt["authorityRefusal"] = error.Message;
                    Check(error.Message == "IncompleteCompilationUniverse", "Incomplete scope authority refusal");
                }
                var unavailable = await store.FindOccurrencesAsync(query);
                Receipt["result"] = unavailable;
                Check(extraction.Authority is null && !unavailable.IsComplete &&
                    unavailable.Availability == CodeOccurrenceAvailability.IncompleteAnalysis && unavailable.Occurrences.Count == 0 &&
                    unavailable.Coverage?.DeclaredUnitCount == 2 + missingCount && unavailable.Coverage.VerifiedUnitCount == 2,
                    "Incomplete scope cannot serve a complete-empty result");
            }
            else
            {
                var result = await store.FindOccurrencesAsync(
                    VerifiedCSharpOccurrenceAuthority.Bind(query, analysis.Universe, caller.Compilation));
                Receipt["result"] = result;
                Check(result.IsComplete && result.Availability == CodeOccurrenceAvailability.Available &&
                    result.Occurrences.Count == expectedFacts && result.Coverage?.Unsupported == expectedUnsupported && result.Coverage?.UnsupportedCategories == expectedUnsupported &&
                    result.Coverage?.ConditionalInactive == expectedInactive,
                    "Bound query preserves the expected counts and complete-empty boundary");
            }
        }
        Receipt["native"] = ReadNative(database);
        using var native = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(Receipt["native"]));
        var decoded = native.RootElement.GetProperty("AndiritOccurrenceUnit").EnumerateArray().Select(row =>
            System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(row.GetProperty("payload").GetString()!)).RootElement.Clone()).ToArray();
        Receipt["decodedUnits"] = decoded;
        if (partial)
            Check(decoded.Sum(row => row.GetProperty("Coverage").GetProperty("DeclaredUnitCount").GetInt32()) == 2 + missingCount &&
                decoded.Sum(row => row.GetProperty("Coverage").GetProperty("VerifiedUnitCount").GetInt32()) == 2 &&
                decoded.All(row => row.GetProperty("Facts").GetArrayLength() == 0), "Native partial membership and zero facts");
        else
            Check(decoded.Sum(row => row.GetProperty("Coverage").GetProperty("Unsupported").GetInt32()) == expectedUnsupported &&
                decoded.Sum(row => row.GetProperty("Coverage").GetProperty("UnsupportedCategories").GetInt32()) == expectedUnsupported &&
                decoded.Sum(row => row.GetProperty("Facts").GetArrayLength()) == expectedFacts, "Native site inventory and facts");
    }

    private static async Task Provenance(CodeRepositoryId repository, Dictionary<string, byte[]> original)
    {
        var changed = new Dictionary<string, byte[]>(original)
        {
            [DependencyPath] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(original[DependencyPath]) + "\n// Byte delta\n")
        };
        var configured = new Dictionary<string, byte[]>(original)
        {
            [DependencyProject] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(original[DependencyProject])
                .Replace("</PropertyGroup>", "<Nullable>enable</Nullable></PropertyGroup>", StringComparison.Ordinal))
        };
        var rows = new List<object>();
        var digest = new List<string>();
        var targets = new List<string>();
        var ids = new List<string>();
        foreach (var files in new[] { original, changed, configured })
        {
            var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repository, "provenance", files));
            var defining = analysis.Projects.Single(project => project.Project.Path == DependencyProject).Verified!;
            var caller = analysis.Projects.Single(project => project.Project.Path == CallerProject).Verified!;
            var refDigest = FileDigest(files[DependencyPath]);
            var target = MarkTarget(repository.Value, defining, files[DependencyPath]);
            var frozen = Frozen.CompilationProvenance.Describe(repository.Value, ProjectUnit(DependencyProject),
                defining.Compilation, [(DependencyPath, files[DependencyPath])]);
            Check(frozen.DescriptorDigest == defining.MapR2FaithfulCompctx1().DescriptorDigest &&
                defining.Descriptor.Sources.Single().ContentHash == refDigest, "Exact indexed bytes and frozen descriptor agree");
            var extraction = VerifiedCSharpOccurrenceExtractor.Extract(analysis.Universe, new(
                repository, CSharp.Id, caller, _ => Digest("asmctx3-identifier", "Mark"),
                _ => Digest("asmctx3-declarations", MarkDocument), _ => Digest("asmctx3-target", "method|0|"),
                _ => MarkDocument));
            var expected = new List<string>();
            foreach (var pair in files.Where(pair => pair.Key.StartsWith("Caller/Fixture", StringComparison.Ordinal)))
            {
                var lines = Encoding.UTF8.GetString(pair.Value).Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    var found = lines[i].IndexOf("Api.Mark();", StringComparison.Ordinal);
                    if (found < 0) continue;
                    var span = new CodeLocation(pair.Key, i + 1, found + 5, i + 1, found + 9);
                    expected.Add(Usage(repository.Value, CSharp.Id.Value, ProjectUnit(CallerProject), span,
                        FileDigest(pair.Value), MarkDocument, target));
                }
            }
            var actual = extraction.Facts.Select(fact => fact.Id.Value).Order(StringComparer.Ordinal).ToArray();
            Check(expected.Count == 50 && expected.Order(StringComparer.Ordinal).SequenceEqual(actual) &&
                extraction.Facts.All(fact => fact.TargetId.Value == target),
                "Independently recomputed usage and target identities match for the differential");
            digest.Add(frozen.DescriptorDigest);
            targets.Add(target);
            ids.Add(actual[0]);
            rows.Add(new { frozen, target, expected, actual, declaredOptions = Encoding.UTF8.GetString(files[DependencyProject]),
                bytes = Convert.ToBase64String(files[DependencyPath]), definingDescriptor = Describe(defining.Descriptor) });
        }
        Receipt["differentials"] = rows;
        Check(digest.Distinct().Count() == 3 && targets.Distinct().Count() == 3 && ids.Distinct().Count() == 3,
            "Independent source and options deltas change compctx1, asmctx3, and usage4");

        var falseSource = new CodeGraphSource(DependencyPath, FileDigest(original[DependencyPath]),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(changed[DependencyPath], writable: false)));
        var goodSources = Context(repository, "provenance", original).Sources.Where(source => source.Path != DependencyPath);
        var input = new CodeGraphPluginContext(repository, null, new("false-hash"), [.. goodSources, falseSource]);
        var refused = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(input);
        Receipt["falseHash"] = new { declared = falseSource.ContentHash, actual = FileDigest(changed[DependencyPath]),
            refused.Universe.IsComplete, refused.Universe.Refusals, refused.Universe.MissingScopeUnits };
        Check(!refused.Universe.IsComplete &&
            refused.Universe.Refusals.Any(error => error.Contains("SourceHashMismatch:" + DependencyPath, StringComparison.Ordinal)),
            "False declared indexed hash must fail verified authority");

        var missingReference = new Dictionary<string, byte[]>(original);
        missingReference.Remove(DependencyProject);
        var missing = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repository, "missing-reference", missingReference));
        Receipt["missingReference"] = new { missing.Universe.IsComplete, missing.Universe.Refusals, missing.Universe.MissingScopeUnits };
        Check(!missing.Universe.IsComplete && missing.Universe.Refusals.Any(error =>
            error.Contains("MissingOrCyclicProjectDependency", StringComparison.Ordinal)), "Missing declared project reference is refused");
    }
}



