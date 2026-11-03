# Reduced extension review

## Accepted rule

Reduced extension-method symbols normalize to the unreduced defining
declaration via `ReducedFrom`, falling back to `OriginalDefinition` only when
the method is not reduced. The rule predates R2 (`SemanticInspector`) and is
now shared: `CanonicalTarget.CanonicalSymbol` is the single implementation,
used by `SemanticInspector.SymbolKey`, the main identity view, the lifecycle
projection, and the cross-view section.

## Evidence

- `q2-f01-preflight.json`: under the R1 code, S06 resolved to the reduced
  doc-id `M:Corpus.Core.StringExtensions.Shout` (missing `(System.String)`)
  and both WordCount calls likewise; the probe's canonical column shows the
  unreduced declarations the accepted pathway produces.
- Qualified run (`oracle-r2-run-1.json`): S06 resolves
  `M:Corpus.Core.StringExtensions.Shout(System.String)`, shape `method|1`,
  usage4 `561e2287…`; INV-31 resolves
  `M:Corpus.Core.StringExtensions.WordCount(System.String)`, usage4
  `da3a32d1…`; `semantic.S06`, `semantic.INV-31`, `semantic.INV-32` freeze
  these symbol keys.
- `crossView.S06` and `crossView.INV-31` confirm main-view/lifecycle-view
  agreement on the normalized declarations.

## External mutation control

`mutations/reduced-normalization-disabled/` replaces `CanonicalSymbol` with
plain `OriginalDefinition` (the R1 lifecycle behavior): exit 1, nine failures —
`semantic.S06`, `semantic.INV-31`, `semantic.INV-32`, `crossView.S06`,
`crossView.INV-31`, `crossView.all-sites-agree`, `expected.semantics`,
`expected.crossView`, `expected.lifecycle`. The frozen semantic symbol keys
alone reject the unnormalized form; the cross-view and lifecycle sections
reject the resulting identity drift.
