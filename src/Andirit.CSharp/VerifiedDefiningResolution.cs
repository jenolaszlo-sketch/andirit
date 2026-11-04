namespace Andirit;

/// <summary>
/// Outcome of resolving a candidate target to exactly one verified defining
/// compilation inside a complete declared scope, before any target or occurrence
/// is minted. The vocabulary mirrors the accepted refusal classifications
/// (mapping contract §3, Option A §5.1): a failed resolution never becomes a
/// successful empty analysis or a fabricated target.
/// </summary>
internal enum VerifiedDefiningStatus
{
    /// <summary>Exactly one live defining compilation, its verified context and a mapped R2-faithful `compctx1`.</summary>
    Complete = 0,

    /// <summary>The declared scope is not completely covered by verified contexts (or carries refusals).</summary>
    Incomplete = 1,

    /// <summary>More than one verified live compilation binds the same target symbol.</summary>
    Ambiguous = 2,

    /// <summary>No verified live compilation in the universe is the target's defining assembly.</summary>
    MissingDefiningCompilation = 3,

    /// <summary>The defining project unit is outside the declared analysis scope.</summary>
    UnsupportedExternalEvidence = 4,

    /// <summary>A recorded defining `compctx2` does not equal the current verified context (stale/substituted/fabricated).</summary>
    StaleContext = 5,
}

/// <summary>
/// The pre-mint association result for one candidate target. <see cref="Compctx1"/>
/// is the mapped R2-faithful defining `compctx1`; it is present only on
/// <see cref="VerifiedDefiningStatus.Complete"/>. Presence of this record is not a
/// minted target: `asmctx3`/`usage4` construction remains WP4/WP5.
/// </summary>
internal sealed record VerifiedDefiningResolution(
    VerifiedDefiningStatus Status,
    string Detail,
    VerifiedCSharpCompilation? Defining = null,
    CompilationProvenanceDescriptor? Compctx1 = null)
{
    /// <summary>True only when a single verified live defining context was resolved and mapped.</summary>
    internal bool IsComplete =>
        Status == VerifiedDefiningStatus.Complete && Defining is not null && Compctx1 is not null;
}
