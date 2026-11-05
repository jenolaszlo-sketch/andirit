namespace Andirit;

/// <summary>
/// WP4 pre-mint authority outcome for one candidate target. <see cref="Minted"/> is
/// produced only when a supplied, non-null recorded defining `compctx2` equals the
/// current verified context of exactly one live defining compilation in a complete
/// declared universe and the frozen `asmctx3` artifact can be constructed.
/// </summary>
internal enum VerifiedTargetMintStatus
{
    /// <summary>Exactly one verified live defining compilation, a validated recorded `compctx2`, and a canonical `asmctx3`.</summary>
    Minted = 0,

    /// <summary>No supplied, non-null recorded defining `compctx2` (mandatory before minting).</summary>
    MissingRecordedContext = 1,

    /// <summary>The declared scope is not completely covered by verified contexts (or carries refusals).</summary>
    Incomplete = 2,

    /// <summary>More than one verified live compilation binds the same target symbol.</summary>
    Ambiguous = 3,

    /// <summary>No verified live compilation in the universe is the target's defining assembly.</summary>
    MissingDefiningCompilation = 4,

    /// <summary>The defining project unit is outside the declared analysis scope.</summary>
    UnsupportedExternalEvidence = 5,

    /// <summary>A supplied recorded defining `compctx2` does not equal the current verified context.</summary>
    StaleContext = 6,
}

/// <summary>
/// The WP4 authority-gated `asmctx3` result. <see cref="TargetId"/> and
/// <see cref="Compctx1"/> are present only on <see cref="VerifiedTargetMintStatus.Minted"/>;
/// every other status carries no authority-bearing artifact. This constructs no
/// `usage4`, emits no fact and persists nothing (WP5/S2.3).
/// </summary>
internal sealed record VerifiedTargetMint(
    VerifiedTargetMintStatus Status,
    string Detail,
    CodeOccurrenceTargetId? TargetId = null,
    CompilationProvenanceDescriptor? Compctx1 = null)
{
    /// <summary>True only when an authority-gated `asmctx3` was produced.</summary>
    internal bool IsMinted =>
        Status == VerifiedTargetMintStatus.Minted && TargetId is not null && Compctx1 is not null;
}
