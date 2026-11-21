// M54-B-QUAL-01 harness — shared deterministic fixture (reviewer-owned).
// Compiled into BOTH the test host and the distinct-process reopen host, so each
// process independently reconstructs the same admissible universe from the same
// constants (descriptor determinism is a qualified production property).
using Andirit;
using Microsoft.CodeAnalysis.CSharp;

namespace M54B;

internal static class SharedFixture
{
    internal const string RepositoryId = "fixture:m54b";
    internal static readonly DateTimeOffset Started = new(2026, 11, 21, 0, 0, 0, TimeSpan.Zero);
    internal const string LibSource = "namespace N { public static class Lib { public static int Get() => 7; } }";
    internal const string AppSource =
        "using System; namespace N { public static class App { public static int Run() { Missing.Method(); Console.WriteLine(\"x\"); var c = Console.Out; return Lib.Get(); } } }";

    internal static (VerifiedCSharpUniverse Universe, VerifiedCSharpCompilation App,
        VerifiedCSharpCompilation Lib, CSharpCompilation AppCompilation) Build()
    {
        var platform = UniverseFactory.CapturePlatform();
        var console = UniverseFactory.CaptureConsole();
        var lib = UniverseFactory.CreateVerified("Lib/Lib.csproj",
            UniverseFactory.Compile("Lib", [UniverseFactory.Tree("Lib/Lib.cs", LibSource)], [platform.Reference]),
            [UniverseFactory.ReadTextSource("Lib/Lib.cs", LibSource)], [platform], []);
        var appSource = UniverseFactory.ReadTextSource("App/App.cs", AppSource);
        var appCompilation = UniverseFactory.Compile("App", [UniverseFactory.Tree(appSource)],
            [platform.Reference, console.Reference, lib.Compilation.ToMetadataReference()]);
        var app = UniverseFactory.CreateVerified("App/App.csproj", appCompilation, [appSource],
            [platform, console], [lib]);
        var scope = UniverseFactory.DeclareScope(
            ("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
            ("App/App.csproj", "App/App.csproj", ["App/App.cs"]));
        var universe = UniverseFactory.CreateUniverse(scope, [lib, app]);
        return (universe, app, lib, appCompilation);
    }

    // Standalone app (no project dependency) for the incomplete-universe cases:
    // a universe member that depends on nothing, so the universe can legitimately
    // carry a missing scope unit without tripping the dependency-membership guard.
    internal static VerifiedCSharpCompilation StandaloneApp()
    {
        var platform = UniverseFactory.CapturePlatform();
        var console = UniverseFactory.CaptureConsole();
        const string source =
            "using System; namespace N { public static class App { public static int Run() { Missing.Method(); Console.WriteLine(\"x\"); return 42; } } }";
        var compilation = UniverseFactory.Compile("App",
            [UniverseFactory.Tree("App/App.cs", source)], [platform.Reference, console.Reference]);
        return UniverseFactory.CreateVerified("App/App.csproj", compilation,
            [UniverseFactory.ReadTextSource("App/App.cs", source)], [platform, console], []);
    }

    internal static VerifiedOccurrenceExtraction Extract(VerifiedCSharpUniverse universe,
        VerifiedCSharpCompilation app) =>
        VerifiedCSharpOccurrenceExtractor.Extract(universe, new VerifiedOccurrenceExtractionRequest(
            new CodeRepositoryId(RepositoryId), new CodePluginId("plugin:m54b"), app,
            _ => UniverseFactory.Digest("id"),
            _ => UniverseFactory.Digest("decl"),
            _ => UniverseFactory.Digest("shape"),
            UniverseFactory.TargetDocument));
}