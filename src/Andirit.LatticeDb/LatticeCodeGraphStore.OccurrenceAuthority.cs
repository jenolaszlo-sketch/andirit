using System.Text.Json;
using static Andirit.DurableCommandLog;

namespace Andirit;

public sealed partial class LatticeCodeGraphStore
{
    // Derived from the same committed occurrence rows as the reader projection.
    // Updated only after a known successful commit, or together with recovery.
    private sealed record OccurrenceAuthorityPublication(
        PersistedOccurrenceMeta Meta, IReadOnlyList<PersistedOccurrenceUnit> Units);

    private Dictionary<string, OccurrenceAuthorityPublication> _authorityPublications = new(StringComparer.Ordinal);

    private async ValueTask<CodeOccurrenceQueryResult?> CheckOccurrenceAuthorityAsync(
        CodeOccurrenceQuery query, CancellationToken cancellationToken)
    {
        if (!_authorityPublications.TryGetValue(query.RepositoryId.Value, out var stored))
            return null; // Legitimate graph-only publication: existing availability mapping.
        if (!stored.Meta.RequiresAuthority && query.CurrentAuthority is null &&
            !stored.Units.Any(u => u.RequiresAuthority || u.Authority is not null))
            return null; // Existing synthetic/raw contract; never promoted by the verified gate.
        var publication = await _inner.GetLatestPublicationAsync(query.RepositoryId, cancellationToken).ConfigureAwait(false);
        CodeOccurrenceQueryResult Refuse(CodeOccurrenceAvailability status, string reason) =>
            new(status, publication, [], 0, 0, reason);
        if (publication is null || query.ExpectedPublication is { } expected && expected != publication ||
            stored.Meta.PublicationIndexRunId != publication.IndexRunId.Value ||
            stored.Meta.IndexIdentity != publication.IndexIdentity.Value)
            return Refuse(CodeOccurrenceAvailability.StalePublication, "The authority is not bound to the accepted publication.");
        if (stored.Meta.UnitCount != stored.Units.Count ||
            stored.Meta.FactCount != stored.Units.Sum(u => u.Facts.Count))
            return Refuse(CodeOccurrenceAvailability.CorruptedIndex, "Authority publication accounting differs from native units.");
        // Cross-projection integrity (WP-S23-2-R1 / F-01): the occurrence
        // authority projection must correspond to the actual accepted published
        // graph owner set in both directions. A retired or directly removed graph
        // unit must not leave an orphaned authority-bearing occurrence unit that
        // would still be served as a complete current result, and a published
        // graph unit that requires authority must carry its occurrence
        // association. This is a durable correspondence check, not a query-only
        // filter; the projection is reconstructed independently at open.
        var graphUnits = await _inner.GetPublishedUnitsAsync(query.RepositoryId, cancellationToken)
            .ConfigureAwait(false);
        var graphOwners = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var graphUnit in graphUnits)
        {
            if (!string.Equals(graphUnit.Origin.RepositoryId.Value, query.RepositoryId.Value, StringComparison.Ordinal))
                continue;
            graphOwners[graphUnit.Origin.PluginId.Value + "\n" + graphUnit.Origin.IndexUnitId.Value] =
                graphUnit.RequiresOccurrenceAuthority;
        }
        var occurrenceOwners = new HashSet<string>(
            stored.Units.Select(unit => unit.PluginId + "\n" + unit.IndexUnitId), StringComparer.Ordinal);
        foreach (var owner in occurrenceOwners)
        {
            if (!graphOwners.ContainsKey(owner))
                return Refuse(CodeOccurrenceAvailability.CorruptedIndex,
                    "An authority-bearing occurrence unit is orphaned from the published graph units.");
        }
        foreach (var (owner, requiresAuthority) in graphOwners)
        {
            if (requiresAuthority && !occurrenceOwners.Contains(owner))
                return Refuse(CodeOccurrenceAvailability.CorruptedIndex,
                    "A published graph unit requiring occurrence authority has no occurrence association.");
        }
        if (query.CurrentAuthority is null)
            return Refuse(CodeOccurrenceAvailability.AnalysisUnavailable, "Current verified compilation authority is required.");
        foreach (var unit in stored.Units.OrderBy(u => u.PluginId, StringComparer.Ordinal)
                     .ThenBy(u => u.IndexUnitId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (unit.DurableKey != unit.RepositoryId + "\n" + unit.PluginId + "\n" + unit.IndexUnitId)
                return Refuse(CodeOccurrenceAvailability.CorruptedIndex, "Authority ownership differs from the native unit key.");
            if (unit.Authority is not { ValueKind: JsonValueKind.Object } raw)
                return Refuse(unit.Authority is null || unit.Authority.Value.ValueKind == JsonValueKind.Null
                    ? CodeOccurrenceAvailability.AnalysisUnavailable : CodeOccurrenceAvailability.CorruptedIndex,
                    "Missing or malformed recorded occurrence authority; re-ingestion required.");
            if (!raw.TryGetProperty("Version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number))
                return Refuse(CodeOccurrenceAvailability.CorruptedIndex, "Missing or malformed authority version.");
            if (number != 1)
                return Refuse(CodeOccurrenceAvailability.UnsupportedSchema, "Unsupported occurrence authority version.");
            if (unit.RepositoryId != query.RepositoryId.Value ||
                unit.AuthorityPublicationIndexRunId != stored.Meta.PublicationIndexRunId)
                return Refuse(CodeOccurrenceAvailability.StalePublication, "Occurrence authority unit/publication association differs.");
            try
            {
                var authority = raw.Deserialize<CodeOccurrenceAuthority>(SerializerOptions);
                if (authority is null)
                    return Refuse(CodeOccurrenceAvailability.CorruptedIndex, "Invalid occurrence authority.");
                var refused = query.CurrentAuthority.Validate(authority, unit.RepositoryId, unit.PluginId,
                    unit.IndexUnitId, unit.Facts, unit.Coverage);
                if (refused is not null)
                    return Refuse(refused.Availability, refused.Reason);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException)
            {
                return Refuse(CodeOccurrenceAvailability.CorruptedIndex, "Malformed occurrence authority: " + exception.Message);
            }
        }
        return null;
    }
}
