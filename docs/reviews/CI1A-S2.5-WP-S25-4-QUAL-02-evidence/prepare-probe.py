from pathlib import Path
import shutil

w = Path(__file__).resolve().parent.parent
sealed = w / "corrected/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence"
probe = w / "probe"
shutil.copytree(sealed / "probe", probe)
file = probe / "Program.cs"
program = file.read_text(encoding="utf-8")
def change(before, after):
    global program
    if program.count(before) != 1:
        raise ValueError(before)
    program = program.replace(before, after)

change('var mode = phase[3..];', '''var mode = phase[3..];
        var baseline = argsBaseline();
        bool argsBaseline() => Environment.GetEnvironmentVariable("S254_QUAL02_PROFILE") == "baseline";
        var mixed = mode.StartsWith("mixed-", StringComparison.Ordinal);
        if (mixed)
            mode = mode[6..];''')
change('''        if (mode == "ambiguous")
        {
            files[dependency]''','''        if (mode == "ambiguous" || mixed)
        {
            files[dependency]''')
change('''            files["Caller/Fixture1.cs"] = Encoding.UTF8.GetBytes(''','''            if (mixed)
                files["Caller/Diagnostics.cs"] = Encoding.UTF8.GetBytes(
                    "namespace QualifierCaller; public static class Diagnostics { public static void Check() { " +
                    "QualifierDependency.Api.Pick(null); QualifierDependency.Api.DoesNotExist(); } }");
            else
                files["Caller/Fixture1.cs"] = Encoding.UTF8.GetBytes(''')
change('''        Receipt["sourceInputs"] = files.ToDictionary''','''        Receipt["inputHashes"] = files.ToDictionary(pair => pair.Key, pair => FileDigest(pair.Value));
        Receipt["sourceInputs"] = files.ToDictionary''')
change('''        var dbOptions = new Andirit.LatticeDb.LatticeDbStoreOptions { UseNativeTraversal = true };''','''        var externalSite = caller.Compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Single(site => site.Expression.ToString() == "string.Concat");
        var externalSymbol = (IMethodSymbol)caller.Compilation.GetSemanticModel(externalSite.SyntaxTree)
            .GetSymbolInfo(externalSite).Symbol!;
        var externalReference = caller.Compilation.GetMetadataReference(externalSymbol.ContainingAssembly)
            as PortableExecutableReference ?? throw new InvalidDataException("No PE reference for external control.");
        var referencePath = externalReference.FilePath ?? throw new InvalidDataException("No captured PE path.");
        var referenceIdentity = "file|" + referencePath.Replace('\\\\', '/') + "|" + FileDigest(File.ReadAllBytes(referencePath))[7..];
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
        var dbOptions = new Andirit.LatticeDb.LatticeDbStoreOptions { UseNativeTraversal = true };''')
change('''            Check(external.resolution == "UnsupportedExternalEvidence" && result.Coverage?.External == 1, "M5.2: external symbol and persisted coverage refusal classification");''','''            Check(external.resolution == expectedClass && result.Coverage?.External == expectedExternal &&
                result.Coverage?.Unresolved == expectedUnresolved && result.Coverage?.Ambiguous == expectedAmbiguous &&
                result.Occurrences.Count == 50, "Query classification, counters and inventory match the oracle");''')
change('''        var authority = stored.RootElement.GetProperty("Authority");''','''        var persisted = stored.RootElement.GetProperty("Coverage");
        Check(persisted.GetProperty("External").GetInt32() == expectedExternal &&
            persisted.GetProperty("Unresolved").GetInt32() == expectedUnresolved &&
            persisted.GetProperty("Ambiguous").GetInt32() == expectedAmbiguous &&
            persisted.GetProperty("InvocationFacts").GetInt32() == 50,
            "Raw persisted native counters agree with initial extraction and the bound query");''')
file.write_text(program, encoding="utf-8")


