using System.Text;
using System.Text.Json;
using Andirit;

internal static partial class Probe
{
    private static async Task ReviewerPublication(string phase, string database, string fixture)
    {
        var kind = phase.Split('-')[1];
        var publish = phase.EndsWith("publish", StringComparison.Ordinal);
        var repo = new CodeRepositoryId("review:qual03:" + kind);
        var files = LoadControlFiles(fixture);
        var complete = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "before", files));
        var caller = complete.Projects.Single(p => p.Project.Path == CallerProject).Verified!;
        var defining = complete.Projects.Single(p => p.Project.Path == DependencyProject).Verified!;
        var target = new CodeOccurrenceTargetId(MarkTarget(repo.Value, defining, files[DependencyPath]));
        var bound = VerifiedCSharpOccurrenceAuthority.Bind(new(repo, target, maxResults:100), complete.Universe, caller.Compilation);
        var partial = new Dictionary<string, byte[]>(files);
        partial["Review.slnx"] = Encoding.UTF8.GetBytes("<Solution><Project Path=\"Caller/Caller.csproj\"/><Project Path=\"Dependency/Dependency.csproj\"/><Project Path=\"Gone/Gone.csproj\"/></Solution>");
        if (kind == "none")
            partial = new() { ["Review.slnx"] = Encoding.UTF8.GetBytes("<Solution><Project Path=\"Gone/Gone.csproj\"/></Solution>") };
        if (kind == "refusal")
            partial[CallerProject] = Encoding.UTF8.GetBytes("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup Condition=\"'x'=='x'\"><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><ProjectReference Include=\"../Dependency/Dependency.csproj\"/></ItemGroup></Project>");
        Receipt["sources"] = partial.ToDictionary(p=>p.Key,p=>Encoding.UTF8.GetString(p.Value));
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "after", partial));
        Receipt["membership"] = new { declared=analysis.Universe.Scope.Units, verified=analysis.Universe.Entries.Select(e=>e.Descriptor.ProjectUnitId), analysis.Universe.Refusals, analysis.Universe.IsComplete };
        CodeGraphPublication? old = null;
        using (var store = new LatticeCodeGraphStore(database))
        {
            if (publish && kind == "transition")
            {
                await PublishCSharp(store, repo, "before", files);
                var before = await store.FindOccurrencesAsync(bound);
                Receipt["before"] = before;
                Check(before.IsComplete && before.Occurrences.Count == 50, "Prior accepted complete generation has 50 facts");
                old = before.Publication;
            }
            if (publish) await PublishCSharp(store, repo, "after", partial);
            var result = await store.FindOccurrencesAsync(new(repo,target));
            Receipt["result"] = result;
            var expected = kind == "transition" ? CodeOccurrenceAvailability.IncompleteAnalysis : CodeOccurrenceAvailability.AnalysisUnavailable;
            Check(result.Availability == expected && !result.IsComplete && result.Occurrences.Count == 0, "Current generation has exact partial/unavailable distinction and zero facts");
            if (kind == "transition")
            {
                Check(result.Coverage?.DeclaredUnitCount == 3 && result.Coverage.VerifiedUnitCount == 2, "Partial counts are 3/2 despite formerly accepted state");
                var obsoleteAuthority = await store.FindOccurrencesAsync(bound);
                Receipt["obsoleteAuthority"] = obsoleteAuthority;
                Check(!obsoleteAuthority.IsComplete && obsoleteAuthority.Occurrences.Count == 0 && obsoleteAuthority.Availability != CodeOccurrenceAvailability.Available, "Old complete authority cannot bypass partial coverage");
                if (old is not null)
                {
                    var stale = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(new(repo,target,old),complete.Universe,caller.Compilation));
                    Receipt["stale"] = stale;
                    Check(stale.Availability == CodeOccurrenceAvailability.StalePublication && !stale.IsComplete && stale.Occurrences.Count==0, "Prior publication handle is stale");
                }
                try { _ = VerifiedCSharpOccurrenceAuthority.Bind(new(repo,target),analysis.Universe,analysis.Projects.Single(p=>p.Project.Path==CallerProject).Compilation); Check(false,"Partial authority refused"); }
                catch (InvalidDataException e) { Receipt["authorityRefusal"]=e.Message; Check(e.Message=="IncompleteCompilationUniverse","Precise authority refusal"); }
            }
        }
        var raw = ReadNative(database);
        Receipt["native"] = raw;
        using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(raw));
        var units = parsed.RootElement.GetProperty("AndiritOccurrenceUnit").EnumerateArray().Select(r=>JsonDocument.Parse(Convert.FromBase64String(r.GetProperty("payload").GetString()!)).RootElement.Clone()).ToArray();
        Receipt["decodedUnits"] = units;
        Check(units.All(u=>u.GetProperty("Facts").GetArrayLength()==0),"No obsolete or fabricated native facts");
    }
}
