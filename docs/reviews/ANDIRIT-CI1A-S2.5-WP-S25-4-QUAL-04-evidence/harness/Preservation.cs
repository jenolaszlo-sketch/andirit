// QUAL-04 reviewer-owned preservation controls (commission �10): F-S254-01
// (PE-only external classification with the real 50-site corpus), F-S254-02
// (unsupported non-invocation site accounting), F-S254-03 (missing declared
// membership vs complete-empty/unavailable), and the independent reconstruction
// of all 50 positive B3 occurrence identities including the native declaration
// spans and fresh-process reopen.
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Frozen = AdrCi1Exp.Shared;

internal static partial class Probe
{
    private static async Task Preservation(string name, string path, string receiptPath)
    {
        switch (name)
        {
            case "F254-01":
            {
                var repo = new CodeRepositoryId("qual4:f254-01");
                var run = "run:f254-01";
                var fixtures = receiptPath; // the folder argument carries the fixture root
                var files = Directory.EnumerateFiles(fixtures, "*", SearchOption.AllDirectories)
                    .ToDictionary(file => Path.GetRelativePath(fixtures, file).Replace('\\', '/'),
                        File.ReadAllBytes, StringComparer.Ordinal);
                var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
                var caller = analysis.Projects.Single(p => p.Project.Path == "Caller/Caller.csproj").Verified!;
                var defining = analysis.Projects.Single(p => p.Project.Path == "Dependency/Dependency.csproj").Verified!;
                var universe = analysis.Universe;
                Receipt["declaredScopeUnits"] = universe.Scope.Units.Select(u => u.ProjectUnitId)
                    .Order(StringComparer.Ordinal).ToArray();
                Receipt["verifiedEntries"] = universe.Entries.Select(e => e.Descriptor.ProjectUnitId)
                    .Order(StringComparer.Ordinal).ToArray();
                Receipt["universeComplete"] = universe.IsComplete;

                // Independent target derivation: the frozen R2 Describe of the
                // defining compilation (historical authority) hashed through the
                // documented asmctx3 formula with the caller-supplied
                // identifier/declaration/shape digests in the established corpus form.
                var method = (IMethodSymbol)defining.Compilation.GetTypeByMetadataName("QualifierDependency.Api")!
                    .GetMembers("Mark").Single();
                var mapped = Frozen.CompilationProvenance.Describe(repo.Value,
                    defining.Descriptor.ProjectUnitId, defining.Compilation,
                    [("Dependency/Api.cs", files["Dependency/Api.cs"])]);
                var target = new CodeOccurrenceTargetId("asmctx3:sha256:" + Digest(
                    "asmctx3-context", "compctx1", mapped.DescriptorDigest,
                    mapped.MetadataIdentity, mapped.ModuleName,
                    Digest("asmctx3-identifier", "Mark"),
                    Digest("asmctx3-declarations", "M:QualifierDependency.Api.Mark"),
                    Digest("asmctx3-target", "method|0|")));
                Receipt["expectedTarget"] = target.Value;
                // Native rows are keyed by content-hashed project unit ids, so the
                // reviewer records which row belongs to which project explicitly.
                Receipt["callerProjectUnitId"] = caller.Descriptor.ProjectUnitId;
                Receipt["dependencyProjectUnitId"] = defining.Descriptor.ProjectUnitId;

                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                        await PublishCSharp(store, repo, run, files);
                }

                using (var store = new LatticeCodeGraphStore(path))
                {
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                        universe, caller.Compilation);
                    var result = await store.FindOccurrencesAsync(query);
                    Receipt["query"] = DescribeResult(result);

                    // Independent identity reconstruction of all 50 positive sites.
                    var expectedIds = BuildExpectedIdentities(repo, files, target.Value)
                        .Order(StringComparer.Ordinal).ToArray();
                    Receipt["expectedIdCount"] = expectedIds.Length;
                    Receipt["observedQueryIds"] = result.Occurrences.Select(f => f.Id.Value)
                        .Order(StringComparer.Ordinal).ToArray();
                    Receipt["identityMatch"] = expectedIds.SequenceEqual(
                        result.Occurrences.Select(f => f.Id.Value).Order(StringComparer.Ordinal).ToArray());
                    Receipt["callerOwnershipMatch"] = result.Occurrences.All(f => f.SourceUnitId == caller.Descriptor.ProjectUnitId);
                    Receipt["targetMatch"] = result.Occurrences.All(f => f.TargetId.Value == target.Value);
                    Receipt["externalOccurrenceFacts"] = result.Occurrences.Count(f =>
                        f.TargetId.Value != target.Value);
                    var apiBytes = files["Dependency/Api.cs"];
                    Receipt["expectedDeclarationSpan"] = MarkDeclarationSpan(apiBytes);
                    // Declaration spans are evidence on the persisted facts (the
                    // query hit DTO does not carry them); decode them after the
                    // store is closed, because LatticeDB locks the file.
                }

                Receipt["declarationSpans"] = DecodedUnits(path)
                    .SelectMany(unit => unit.GetProperty("Facts").EnumerateArray())
                    .Select(fact => fact.GetProperty("DeclarationSpan").GetProperty("Path").GetString() + ":" +
                        fact.GetProperty("DeclarationSpan").GetProperty("StartLine").GetInt32() + ":" +
                        fact.GetProperty("DeclarationSpan").GetProperty("StartColumn").GetInt32() + ":" +
                        fact.GetProperty("DeclarationSpan").GetProperty("EndLine").GetInt32() + ":" +
                        fact.GetProperty("DeclarationSpan").GetProperty("EndColumn").GetInt32())
                    .Distinct(StringComparer.Ordinal).ToArray();
                Receipt["native"] = NativeRowSummary(path);
                Receipt["nativeDeclarationSpans"] = DecodedUnits(path)
                    .SelectMany(unit => unit.GetProperty("Facts").EnumerateArray())
                    .Select(fact => fact.GetProperty("DeclarationSpan").GetProperty("Path").GetString() + ":" +
                        fact.GetProperty("DeclarationSpan").GetProperty("StartLine").GetInt32() + ":" +
                        fact.GetProperty("DeclarationSpan").GetProperty("StartColumn").GetInt32() + ":" +
                        fact.GetProperty("DeclarationSpan").GetProperty("EndLine").GetInt32() + ":" +
                        fact.GetProperty("DeclarationSpan").GetProperty("EndColumn").GetInt32())
                    .Distinct(StringComparer.Ordinal).ToArray();
                Receipt["nativeFactCount"] = DecodedUnits(path)
                    .SelectMany(unit => unit.GetProperty("Facts").EnumerateArray()).Count();
                break;
            }

            case "F254-02":
            {
                var repo = new CodeRepositoryId("qual4:f254-02");
                var run = "run:f254-02";
                var files = UnsupportedFixture();
                var target = await FixtureTargetAsync(repo, run, files, "Dep/Dep.csproj", "Dep.Widget", "Use");
                var depAnalysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
                Receipt["depProjectUnitId"] = depAnalysis.Projects
                    .Single(p => p.Project.Path == "Dep/Dep.csproj").Verified!.Descriptor.ProjectUnitId;
                Receipt["useProjectUnitId"] = depAnalysis.Projects
                    .Single(p => p.Project.Path == "Use/Use.csproj").Verified!.Descriptor.ProjectUnitId;
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                        await PublishCSharp(store, repo, run, files);
                }
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
                    var query = VerifiedCSharpOccurrenceAuthority.Bind(new CodeOccurrenceQuery(repo, target),
                        analysis.Universe, analysis.Projects.Single(p => p.Project.Path == "Dep/Dep.csproj").Compilation);
                    Receipt["query"] = DescribeResult(await store.FindOccurrencesAsync(query));
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            case "F254-03-none":
            {
                // Zero usable source analysis: an unsupported project evaluation
                // (package reference) refuses the universe, so occurrence analysis
                // is unavailable, never an empty complete answer.
                var repo = new CodeRepositoryId("qual4:f254-03-none");
                var run = "run:f254-03-none";
                var files = UnanalyzableFixture();
                var target = new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('0', 64));
                if (Phase != "verify")
                {
                    using (var store = new LatticeCodeGraphStore(path))
                        await PublishCSharp(store, repo, run, files);
                }
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, run, files));
                    Receipt["universeComplete"] = analysis.Universe.IsComplete;
                    Receipt["universeRefusals"] = analysis.Universe.Refusals.Order(StringComparer.Ordinal).ToArray();
                    Receipt["verifiedEntries"] = analysis.Universe.Entries.Length;
                    Receipt["query"] = DescribeResult(await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)));
                }

                Receipt["native"] = NativeRowSummary(path);
                break;
            }

            default:
                throw new ArgumentException(name);
        }
    }

    // Reviewer fixture: one ordinary invocation plus exactly one constructor, one
    // property reference and one method group (the F-S254-02 non-invocation
    // categories). The dependency is complete-empty (no in-project invocation).
    private static Dictionary<string, byte[]> UnsupportedFixture() => new(StringComparer.Ordinal)
    {
        ["Dep/Dep.csproj"] = Encoding.UTF8.GetBytes("<Project Sdk=\"Microsoft.NET.Sdk\" />"),
        ["Dep/Widget.cs"] = Encoding.UTF8.GetBytes(
            "namespace Dep;\npublic class Widget {\n" +
            "    public Widget() { }\n" +
            "    public int Size { get; set; }\n" +
            "    public void Report() { }\n" +
            "    public static int Use() => 7;\n}\n"),
        ["Use/Use.csproj"] = Encoding.UTF8.GetBytes(
            "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>" +
            "<ProjectReference Include=\"../Dep/Dep.csproj\" /></ItemGroup></Project>"),
        ["Use/Consumer.cs"] = Encoding.UTF8.GetBytes(
            "namespace Use;\n" +
            "public static class Consumer {\n" +
            "    public static int Run() {\n" +
            "        var widget = new Dep.Widget();\n" +
            "        System.Action action = widget.Report;\n" +
            "        return Dep.Widget.Use() + widget.Size + (action is null ? 0 : 1);\n" +
            "    }\n}\n"),
        ["Unsupported.slnx"] = Encoding.UTF8.GetBytes(
            "<Solution><Project Path=\"Dep/Dep.csproj\"/><Project Path=\"Use/Use.csproj\"/></Solution>"),
    };

    private static Dictionary<string, byte[]> UnanalyzableFixture() => new(StringComparer.Ordinal)
    {
        ["Pkg/Pkg.csproj"] = Encoding.UTF8.GetBytes(
            "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>" +
            "<PackageReference Include=\"Some.Package\" Version=\"1.0.0\" /></ItemGroup></Project>"),
        ["Pkg/Value.cs"] = Encoding.UTF8.GetBytes("namespace Pkg; public static class Value { public static int Get() => 1; }"),
        ["Unanalyzable.slnx"] = Encoding.UTF8.GetBytes("<Solution><Project Path=\"Pkg/Pkg.csproj\"/></Solution>"),
    };

    // Independent derivation of the expected Mark declaration span from the exact
    // fixture bytes (the full member declaration span, 1-based columns).
    private static string MarkDeclarationSpan(byte[] apiBytes)
    {
        var text = Encoding.UTF8.GetString(apiBytes);
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var column = line.IndexOf("public static int Mark()", StringComparison.Ordinal);
            if (column < 0)
                continue;
            var end = column + "public static int Mark() => 7;".Length;
            return "Dependency/Api.cs:" + (i + 1) + ":" + (column + 1) + ":" + (i + 1) + ":" + (end + 1);
        }

        return "not-found";
    }

    // Independent reconstruction of all 50 positive B3 occurrence identities from
    // the fixture bytes and the documented usage4 formula.
    private static List<string> BuildExpectedIdentities(CodeRepositoryId repo,
        Dictionary<string, byte[]> files, string target)
    {
        var plugin = new CodePluginId("penghou.hetu.csharp");
        var owner = ProjectUnit("Caller/Caller.csproj");
        var ids = new List<string>();
        const string document = "M:QualifierDependency.Api.Mark";
        foreach (var (path, bytes) in files.Where(pair => pair.Key.StartsWith("Caller/Fixture", StringComparison.Ordinal))
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var sourceHash = FileDigest(bytes);
            var lines = Encoding.UTF8.GetString(bytes).Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var found = lines[i].IndexOf("Api.Mark();", StringComparison.Ordinal);
                if (found < 0)
                    continue;
                // The minted site span is the identifier token "Mark" inside the call.
                var startColumn = found + "Api.".Length + 1;
                var endColumn = startColumn + "Mark".Length;
                ids.Add(Digest("usage4", repo.Value, plugin.Value,
                    CodeOccurrenceIdentity.SemanticsVersion, owner, path, sourceHash,
                    (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    startColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    endColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    document, target, CodeOccurrenceIdentity.InvocationCategory));
            }
        }

        return ids;
    }
}
