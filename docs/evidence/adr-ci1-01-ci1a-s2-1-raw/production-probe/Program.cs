using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../fixture"));
var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
    .Where(p => p.EndsWith(".cs") || p.EndsWith(".csproj"))
    .Order(StringComparer.Ordinal).Select(p => (Path: Path.GetRelativePath(root, p).Replace('\\', '/'), Bytes: File.ReadAllBytes(p))).ToArray();
CodeGraphPluginContext Context(bool feature) => new(new("fixture:s2.1:production"), null, new(args.FirstOrDefault() ?? "run:excluded"), files.Select(f =>
{
    var bytes = f.Bytes;
    if (feature && f.Path == "Lib/Lib.csproj")
        bytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("<DefineConstants>TRACE</DefineConstants>",
            "<DefineConstants>FEATURE;TRACE</DefineConstants>"));
    return new CodeGraphSource(f.Path, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
        _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
}).ToArray());
var timer = Stopwatch.StartNew();
var original = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(false));
var firstDuration = timer.Elapsed;
timer.Restart();
var changed = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(true));
var secondDuration = timer.Elapsed;
if (!original.Universe.IsComplete || !changed.Universe.IsComplete)
    throw new InvalidDataException(string.Join(";", original.Universe.Refusals.Concat(changed.Universe.Refusals)));
foreach (var analysis in new[] { original, changed })
    foreach (var entry in analysis.Universe.Entries)
        if (entry.Compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            throw new InvalidDataException(string.Join(";", entry.Compilation.GetDiagnostics()));
object Describe(VerifiedCSharpAnalysis analysis) => new
{
    Scope = analysis.Universe.Scope.Identity,
    Universe = analysis.Universe.Identity,
    Contexts = analysis.Universe.Entries.Select(e => e.Descriptor),
    ActualInputs = analysis.Universe.Entries.Select(e => new
    {
        Context = e.Descriptor.DescriptorDigest,
        Options = new
        {
            OutputKind = e.Compilation.Options.OutputKind.ToString(),
            NullableContextOptions = e.Compilation.Options.NullableContextOptions.ToString(),
            AllowUnsafe = e.Compilation.Options.AllowUnsafe.ToString(),
            OptimizationLevel = e.Compilation.Options.OptimizationLevel.ToString(),
            Platform = e.Compilation.Options.Platform.ToString(),
            CheckOverflow = e.Compilation.Options.CheckOverflow.ToString(),
            Deterministic = e.Compilation.Options.Deterministic.ToString(),
            WarningLevel = e.Compilation.Options.WarningLevel.ToString(System.Globalization.CultureInfo.InvariantCulture),
            GeneralDiagnosticOption = e.Compilation.Options.GeneralDiagnosticOption.ToString(),
            SpecificDiagnosticOptions = string.Join(",", e.Compilation.Options.SpecificDiagnosticOptions.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value))
        },
        Trees = e.Compilation.SyntaxTrees.Select(t => new { Path = t.FilePath, Text = t.GetText().ToString(),
            LanguageVersion = ((Microsoft.CodeAnalysis.CSharp.CSharpParseOptions)t.Options).LanguageVersion.ToString(),
            Symbols = ((Microsoft.CodeAnalysis.CSharp.CSharpParseOptions)t.Options).PreprocessorSymbolNames.Order(StringComparer.Ordinal) }),
        ReferenceCount = e.Compilation.References.Count(),
        LiveDependencyLinks = e.Compilation.References.OfType<CompilationReference>().All(r => e.Dependencies.Any(d => ReferenceEquals(d.Compilation, r.Compilation)))
    }),
    Sources = analysis.Sources.Select(s => new { s.Path, s.ContentHash, s.EncodingName, Text = s.Text.ToString() }),
    PublicApiReturnType = ((IMethodSymbol)analysis.Projects.Single(p => p.Project.Path == "App/App.csproj")
        .Compilation.GetTypeByMetadataName("ProvenanceFixture.Api")!.GetMembers("Value").Single()).ReturnType.ToDisplayString()
};
if (original.Universe.Identity == changed.Universe.Identity) throw new InvalidDataException("Expected context propagation absent.");
Console.WriteLine(JsonSerializer.Serialize(new { Original = Describe(original), Feature = Describe(changed) },
    new JsonSerializerOptions { WriteIndented = true }));
Console.Error.WriteLine($"First acquisition/preparation {firstDuration.TotalMilliseconds:F2} ms; second {secondDuration.TotalMilliseconds:F2} ms; includes lazy TPA image capture on first call. Diagnostic measurement only.");
