using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AdrCi1Exp.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Executes the exact frozen R2 descriptor builder, not a proposed replacement.
const string dependency = """
public static class Lib
{
#if FEATURE
    public static string Value() => "changed";
#else
    public static int Value() => 1;
#endif
}
""";
const string caller = "public static class App { public static object Read() => Lib.Value(); }";
var platform = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
    deterministic: true, concurrentBuild: false);
var parse = new CSharpParseOptions(LanguageVersion.Latest,
    documentationMode: DocumentationMode.Diagnose);
CSharpCompilation Dependency(CSharpParseOptions configuration, CSharpCompilationOptions settings) =>
    CSharpCompilation.Create("Lib",
        [CSharpSyntaxTree.ParseText(dependency, configuration, "Lib/Lib.cs")],
        [platform], settings);
CSharpCompilation Caller(CSharpCompilation referenced) => CSharpCompilation.Create("App",
    [CSharpSyntaxTree.ParseText(caller, parse, "App/App.cs")],
    [platform, referenced.ToMetadataReference()], options);
CompilationDescriptor Describe(CSharpCompilation compilation, string unit, string path, string text) =>
    CompilationProvenance.Describe("fixture:s2.1", unit, compilation,
        [(path, Encoding.UTF8.GetBytes(text))]).Verified();
string ReturnType(CSharpCompilation compilation) =>
    ((IMethodSymbol)compilation.GetTypeByMetadataName("Lib")!.GetMembers("Value").Single())
    .ReturnType.ToDisplayString();
void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var libA = Dependency(parse, options);
var libB = Dependency(parse.WithPreprocessorSymbols("FEATURE"), options);
var libC = Dependency(parse, options.WithNullableContextOptions(NullableContextOptions.Enable));
var appA = Caller(libA);
var appB = Caller(libB);
var appC = Caller(libC);
foreach (var compilation in new[] { libA, libB, libC, appA, appB, appC })
    Require(!compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error),
        string.Join("\n", compilation.GetDiagnostics()));

var dependencyA = Describe(libA, "Lib/Lib.csproj", "Lib/Lib.cs", dependency);
var dependencyB = Describe(libB, "Lib/Lib.csproj", "Lib/Lib.cs", dependency);
var dependencyC = Describe(libC, "Lib/Lib.csproj", "Lib/Lib.cs", dependency);
var parentA = Describe(appA, "App/App.csproj", "App/App.cs", caller);
var parentB = Describe(appB, "App/App.csproj", "App/App.cs", caller);
var parentC = Describe(appC, "App/App.csproj", "App/App.cs", caller);

Require(ReturnType(appA) == "int" && ReturnType(appB) == "string", "Reference API must differ.");
Require(dependencyA.DescriptorDigest != dependencyB.DescriptorDigest, "Dependency symbol configuration must discriminate.");
Require(dependencyA.DescriptorDigest != dependencyC.DescriptorDigest, "Dependency options must discriminate.");
Require(parentA.DescriptorDigest == parentB.DescriptorDigest, "Expected R2 caller identity collision absent.");
Require(parentA.DescriptorDigest == parentC.DescriptorDigest, "Expected R2 dependency-options collision absent.");
Require(parentA.References.SequenceEqual(parentB.References), "Expected R2 reference representation equality absent.");

var report = new
{
    Commission = "ADR-CI1-01-CI1A-IMPL-01-S2.1",
    Status = "REPRODUCED: canonical R2 project-reference representation omits material dependency inputs",
    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    Roslyn = typeof(CSharpCompilation).Assembly.FullName,
    SourceBytesHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(dependency))),
    OriginalAuthoritySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(
        Path.Combine(AppContext.BaseDirectory, "../../../../authority/CompilationProvenance.cs")))),
    DefiningContexts = new[] { dependencyA, dependencyB, dependencyC },
    CallingContexts = new[] { parentA, parentB, parentC },
    ObservedReturnTypes = new[] { ReturnType(appA), ReturnType(appB) },
    Assertions = new[]
    {
        "All six real Roslyn compilations have zero error diagnostics",
        "Exact dependency source bytes/path/assembly identity are unchanged",
        "FEATURE changes referenced Value return type from int to string",
        "Dependency compctx1 changes for FEATURE and nullable option variants",
        "Caller compctx1 and recorded reference identities remain equal",
        "No production Andirit code or accepted contract modified"
    }
};
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
