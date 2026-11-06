# WP-S23-2 carrier contract (defined before source edits)

Baseline b576acb004747c89d257d8e61721c17babe6c5a6, tree 3d7c152eb2d15fe87f940ba165f2b5c2d44cfc2a.

Use an internal per-source-owned-unit authority envelope on the existing completed
AndiritOccurrenceUnit record. Key: (repository, plugin, storage index unit), with
source semantic unit and fact-id keyed defining-context receipts inside that envelope.
Each fact receipt binds usage4, payload hash, asmctx3, defining project unit,
recorded compctx2, and the caller-supplied S-01 identity components. Unit evidence
binds exact coverage and the complete verified universe's ordered unit/context
membership. Zero-fact analysis retains that unit/universe evidence independently
of any fact. No global asmctx3 -> compctx2 dictionary is used.

Version 1 is an additive optional JSON extension, not a new native schema format.
AndiritOccurrenceUnit/Page/Meta namespace and versions stay unchanged. Unit and
meta mandatory-authority markers and a unit publication binding prevent a missing
receipt from becoming current. Authority and facts use the same existing native
publication transaction; retained units rebind to the accepted publication.
Replacement replaces the whole envelope; deletion removes it with its unit.

Only the internal verified extractor/mint chain captures receipts. A query-scoped
internal verifier consumes an independently constructed VerifiedCSharpUniverse and
requires the exact live caller in it. It validates all analyzed units conservatively,
including source membership, exact coverage, per-fact ownership/identity/payload,
current defining context and recomputed structural target using the accepted mapper.
A stale member or one stale defining association refuses the whole query, with no
partial successful completeness. Conflicting universes retain existing merge refusal.

Use existing statuses: AnalysisUnavailable for missing recorded/current authority;
StalePublication for changed context/universe/publication; UnsupportedSchema for an
unsupported authority version; CorruptedIndex for malformed/inconsistent authority.
Verified current complete-empty and retired-target queries remain Available-zero;
Partial analysis remains IncompleteAnalysis. Legacy synthetic/raw readers retain
their prior behavior, but the verified gate never promotes legacy missing authority.
Authority-required publications refuse raw queries without current verification.

Q1-Q1: envelope/facts do not enter graph-unit or stage JSON. Only a mandatory marker
survives segments; resuming without re-staging must refuse verified authority.
Complete publication and fresh-process reopen, not segment resumption, prove test 8b.
WP-S23-3 must re-stage/reverify or separately resolve segment durability before relying
on resumption. No general segment repair is implemented.

Q1-Q2: parse the additive envelope as opaque JSON during restore, and interpret it
at the query gate. Bad/unsupported carrier data refuses there, introducing no new
carrier-validation exception inside recovery rebuild. Existing reconstruction-failure
qualification and unknown-outcome policy remain unchanged.

No identity formula, S-01 canonicalization, production plugin/sink publication wiring,
public authority API, migration, dual write, background repair, or successor work.
