# Source deletion lifecycle — DEL01

The oracle executes two Roslyn compilations from real fixture files and constructs source-owned expected facts using the independent R2 helpers.

- **Snapshot A:** authoritative source units `App/Bulk.cs` and `App/Sites.cs`, both complete. Each contributes resolved invocations to source-owned projections. The report records source hashes, `usage4` IDs per source, and App `compctx1`/`compctx2`.
- **Snapshot B:** authoritative manifest contains only `App/Bulk.cs`. Roslyn compiles this reduced set with no error diagnostics. All `App/Sites.cs` owned contributions are absent; the `App/Bulk.cs` `usage4` IDs remain represented in this concrete fixture. App compilation contexts are recomputed under R2.
- Repeated construction of Snapshot B reproduces the contexts and source-owned projection.

Negative controls:

- **DEL-N1:** deletion from the authoritative manifest is a complete reduced analysis.
- **DEL-N2:** if the manifest still declares `App/Sites.cs` but acquisition omits it, declared and acquired sets differ and the expected state is incomplete.
- **DEL-N3:** injecting prior `App/Sites.cs` facts into Snapshot B makes the source-owned projection unequal and fails.

The report does not exercise or claim native occurrence deletion/publication. `usage4` IDs are calculated from accepted R2 inputs; no identity stability is generalized beyond the observed surviving fixture facts.
