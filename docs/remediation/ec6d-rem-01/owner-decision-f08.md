# F08: concrete owner decision request

Accepted query contract section 4 says `bindingRoute: "import" | "same-file"`, requires one confirmed entity per `(class node, target declaration record)`, and preserves every distinct witness. Symbol section 3.5 says the same-file proof has no import/resolution/export steps. A valid self-import permits one class to have both routes to one physical interface, so one entry cannot label every witness with the singular route. No current clarification resolves this case.

Proposed bounded additive correction:

- Keep one entity, every complete witness, the exact witness hash policy and original step tuple.
- Add `BindingRoute` and `HeritageSpan` to each public Witness, populated from that witness's actual proof. No new public type or stored fact is introduced.
- Preserve existing Implementation.BindingRoute and HeritageSpan as the canonical first-proof summary, explicitly documented as summary fields; select that proof deterministically by span then route and witness ID.
- The accepted mixed case then exposes one same-file and one import witness accurately. Existing callers retain the original fields; consumers needing all routes use per-witness metadata.
- Preserve historical accepted contract blobs; record this explicit, narrow owner supplement here rather than rewriting history.

Illustrative result (IDs/steps omitted only in this proposal):

```json
{
  "className": "Multi",
  "bindingRoute": "same-file",
  "heritageSpan": { "Path": "types.ts", "StartLine": 3, "StartColumn": 31, "EndLine": 3, "EndColumn": 37 },
  "witnesses": [
    { "bindingRoute": "same-file", "heritageSpan": { "Path": "types.ts", "StartLine": 3, "StartColumn": 31, "EndLine": 3, "EndColumn": 37 } },
    { "bindingRoute": "import", "heritageSpan": { "Path": "types.ts", "StartLine": 3, "StartColumn": 39, "EndLine": 3, "EndColumn": 44 } }
  ]
}
```

The preserved original probe asserts that the entry's one route describes **every** witness. Under this additive correction that assertion is superseded for mixed routes only; it must remain in the original bundle, continue to run, and be reported explicitly. A separately named supplemental regression must verify every witness route/span, one entity, two complete proofs and unchanged hash derivation. No other assertion is relaxed.

Until an explicit owner answer, this correction is paused while other findings continue.
