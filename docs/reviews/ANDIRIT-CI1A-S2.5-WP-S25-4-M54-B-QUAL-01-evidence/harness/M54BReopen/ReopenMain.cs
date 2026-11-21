// M54-B-QUAL-01 harness — distinct-process reopen leg.
// Rebuilds the same admissible universe from the same constants, binds the
// query-scoped current authority (a live-only object; persisted strings cannot
// construct it), reopens the durable store through the production store, and
// prints the query result as JSON.
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis.CSharp;

namespace M54B;

public static class ReopenMain
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length is < 3 or > 4)
        {
            Console.Error.WriteLine("usage: <database> <repositoryId> <targetId> [bind:1|0]");
            return 2;
        }

        var bind = args.Length < 4 || args[3] == "1";

        try
        {
            var (universe, app, _, appCompilation) = SharedFixture.Build();
            using var store = new LatticeCodeGraphStore(args[0]);
            var baseQuery = new CodeOccurrenceQuery(
                new CodeRepositoryId(args[1]), new CodeOccurrenceTargetId(args[2]));
            var query = bind
                ? VerifiedCSharpOccurrenceAuthority.Bind(baseQuery, universe, appCompilation)
                : baseQuery;
            var result = await store.FindOccurrencesAsync(query);
            var payload = new
            {
                processId = Environment.ProcessId,
                bound = bind,
                availability = result.Availability.ToString(),
                refusalReason = result.RefusalReason,
                occurrenceCount = result.Occurrences.Count,
                totalMatches = result.TotalMatches,
                occurrences = result.Occurrences.Select(hit => new
                {
                    id = hit.Id.Value,
                    target = hit.TargetId.Value,
                    category = hit.Category.ToString(),
                    sourceUnit = hit.SourceUnitId,
                }).ToArray(),
                coverage = result.Coverage is null
                    ? null
                    : new
                    {
                        state = result.Coverage.State.ToString(),
                        invocationFacts = result.Coverage.InvocationFacts,
                        unresolved = result.Coverage.Unresolved,
                        external = result.Coverage.External,
                        ambiguous = result.Coverage.Ambiguous,
                        unsupported = result.Coverage.Unsupported,
                        unsupportedCategories = result.Coverage.UnsupportedCategories,
                        declaredUnitCount = result.Coverage.DeclaredUnitCount,
                        verifiedUnitCount = result.Coverage.VerifiedUnitCount,
                    },
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.ToString());
            return 3;
        }
    }
}