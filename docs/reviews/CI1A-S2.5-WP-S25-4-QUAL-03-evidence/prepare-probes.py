from pathlib import Path
import shutil
W=Path(__file__).resolve().parent.parent
P=W/'probe'
p=P/'Additional.cs'
s=p.read_text()
s=s.replace('var snippet = profile switch','var partial = profile is "incomplete" or "declared" or "multiple" or "mixedmissing";\n        var missingCount = profile is "multiple" or "mixedmissing" ? 2 : 1;\n        var snippet = ReviewerSites.TryGetValue(profile, out var annotated) ? annotated.Source : profile switch')
s=s.replace('public static int Count => 5; }','public static int Count => 5; public int Count2 {get;set;} public Value Next => this; public int this[int i] => 0; public static int Field; } public sealed class Choice { public Choice(string x) {} public Choice(System.Text.StringBuilder x) {} }')
s=s.replace('if (profile is "empty" or "incomplete" or "declared")','if (profile == "empty" || partial)')
s=s.replace('if (profile is "incomplete" or "declared")','if (partial)')
s=s.replace('if (profile is "declared" or "incomplete")','if (partial)')
s=s.replace('if (profile == "declared")','if (profile is "declared" or "mixedmissing")')
s=s.replace('Receipt["sources"] = files.ToDictionary', '''if (missingCount == 2)
            files["Review.slnx"] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files["Review.slnx"])
                .Replace("</Solution>", "<Project Path=\\"Absent2/Absent2.csproj\\" /></Solution>"));
        Receipt["siteInventory"] = ReviewerSites.TryGetValue(profile, out var inventory) ? inventory.Sites : [];
        Receipt["sources"] = files.ToDictionary''')
s=s.replace('analysis.Universe.MissingScopeUnits.SequenceEqual([ProjectUnit("Absent/Absent.csproj")])','analysis.Universe.MissingScopeUnits.Order().SequenceEqual((missingCount == 2 ? new[] { ProjectUnit("Absent/Absent.csproj"), ProjectUnit("Absent2/Absent2.csproj") } : new[] { ProjectUnit("Absent/Absent.csproj") }).Order())')
s=s.replace('var expectedUnsupported = profile is', 'var expectedUnsupported = ReviewerSites.TryGetValue(profile, out var sites) ? sites.Sites.Length : profile is')
s=s.replace('profile is "empty" or "incomplete" or "declared" ? 0 : 50','profile == "empty" || partial ? 0 : 50')
s=s.replace('extraction.Coverage.UnsupportedCategories == expectedUnsupported &&','extraction.Coverage.Unsupported == expectedUnsupported && extraction.Coverage.UnsupportedCategories == expectedUnsupported &&')
s=s.replace('Receipt["controlSymbols"] =', 'Receipt["initialFacts"] = extraction.Facts;\n        Receipt["diagnostics"] = caller.Compilation.GetDiagnostics().Select(d => d.ToString()).ToArray();\n        Receipt["controlSymbols"] =')
s=s.replace('symbol = model.GetSymbolInfo(node).Symbol?.ToDisplayString(),','operation = model.GetOperation(node)?.Kind.ToString(), symbol = model.GetSymbolInfo(node).Symbol?.ToDisplayString(),')
s=s.replace('DeclaredUnitCount == 3','DeclaredUnitCount == 2 + missingCount')
s=s.replace('result.Coverage?.UnsupportedCategories == expectedUnsupported &&','result.Coverage?.Unsupported == expectedUnsupported && result.Coverage?.UnsupportedCategories == expectedUnsupported &&')
s=s.replace('Receipt["native"] = ReadNative(database);','''Receipt["native"] = ReadNative(database);
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
                decoded.Sum(row => row.GetProperty("Facts").GetArrayLength()) == expectedFacts, "Native site inventory and facts");''')
p.write_text(s)
# Extend the transitive family before observation, retaining all original receipts/assertions.
p=P/'M54.cs';s=p.read_text()
s=s.replace('var directReference = phase.Contains("-direct-", StringComparison.Ordinal);','var directReference = phase.Contains("direct", StringComparison.Ordinal);\n        var returned = phase.Contains("returned", StringComparison.Ordinal);\n        var interfaced = phase.Contains("interface", StringComparison.Ordinal);')
s=s.replace('var inputs = files.ToDictionary', '''if (returned || interfaced)
        {
            files["Ancestor/Parent.cs"] += "\\npublic interface IParent { void Inherited(); }";
            files[dependencyPath] = "namespace EvidenceLibrary; public class Child : EvidenceAncestor.Parent {} public static class Api { public static int Mark() => 7; public static " +
                (interfaced ? "EvidenceAncestor.IParent" : "EvidenceAncestor.Parent") + " Get() => null!; }";
            files[callerPath] = "namespace EvidenceCaller; public static class Run { public static int Probe() { EvidenceLibrary.Api.Get().Inherited(); return EvidenceLibrary.Api.Mark(); } }";
        }
        var inputs = files.ToDictionary''')
s=s.replace('var expectedUnresolved = directReference ? 0 : 2;', 'var expectedUnresolved = directReference ? 0 : returned || interfaced ? 1 : 2;')
s=s.replace('var expectedFacts = directReference ? 3 : 1;', 'var expectedFacts = directReference ? 3 : returned || interfaced ? 2 : 1;')
p.write_text(s)
# Baseline uses identical reviewer harness and expected contract outcomes.
shutil.copytree(P,W/'baseline-probe',dirs_exist_ok=True)
print('Reviewer harness prepared; no production or existing tests modified.')
