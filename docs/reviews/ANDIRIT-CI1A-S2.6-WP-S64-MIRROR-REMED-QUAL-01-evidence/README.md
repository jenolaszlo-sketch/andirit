# Independent qualification evidence

This package supports
[ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-QUAL-01](../ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-QUAL-01.md).
It was produced by the independent qualification authority from an isolated
clone. It does not modify the producer evidence or production source.

## Reviewed identities

| Artifact | Identity |
| --- | --- |
| Entry baseline | `dda4358bc2588368efc6ab6a7bd774857d998026` |
| Implementation | `ec984bfe63e5fbe13047cee8920e00b634ab17b0` |
| Implementation tree | `8ec0eb7a5e647a171f54fa8969dadbb6c65da179` |
| Evidence payload | `b167fed057dfa2b2b1e4da1fde26826ff5f01ec9` |
| Payload tree | `bb9a0386fdfc4f4a3e9c744b1611c154f2ebca40` |
| Producer seal | `870b600acf1490337af9f483f8894a5e8166a90e` |
| Producer seal SHA-256 | `b473dd41742510d5676c1d8c033244afbc2c769dc52bf3941fa776769a6704f2` |
| Authorized checkout merge | `c549852450d950412ff7eb01f0916134f2539c2a` |
| Candidate hosted CI | `46035752304` attempt 1 |
| Seal hosted CI | `46036959497` attempt 1 |

## Contents

| Path | Purpose |
| --- | --- |
| `identities.json` | Machine-readable reviewed identities |
| `seal-verification.json`, `tools/verify_seal.py` | Independent seal verification |
| `hosted-verification.json`, `tools/qual_hosted.py`, `hosted/` | Independent hosted receipt audit and raw TRX |
| `local-regression.json`, `tools/qual_local.py` | Independently executed regression and named-case parse |
| `summary.json`, `tools/qual_summary.py` | Baseline controls, accepted inventory and coverage |
| `harness/` | Independent defect/ownership/recovery harness |
| `results/candidate/` | Candidate scenario outputs |
| `results/baseline/` | Entry-baseline scenario outputs |
| `results/recovery/` | Producer-sealed and independently constructed recovery |
| `results/affected-store/` | Independently constructed affected store and SHA-256 |
| `results/probe-delete.json` | Independent native non-cascade observation |
| `package-smoke.txt` | Pack-consumer result |
| `oracle.md` | Independently derived expected-state model |
| `methodology.md` | Isolation, equivalence, executed-versus-audited limits |
| `gate-matrix.md` | Gate results |
| `manifest.json` | Self-excluding qualification evidence seal |

## Reproduction

```text
git clone https://github.com/jenolaszlo-sketch/andirit.git repo
git -C repo worktree add --detach base  dda4358bc2588368efc6ab6a7bd774857d998026
git -C repo worktree add --detach impl  ec984bfe63e5fbe13047cee8920e00b634ab17b0
git -C repo worktree add --detach seal  870b600acf1490337af9f483f8894a5e8166a90e
python tools/verify_seal.py repo
```

Build and run the independent harness against a tree:

```text
dotnet build harness/QualHarness.csproj -c Release \
  -p:AndiritRoot=<tree> -o <harness-output>
dotnet <harness-output>/QualHarness.dll <scenario> <fresh.ltdb> <out.json>
```

Hosted audit from the artifact download root:

```text
python tools/qual_hosted.py <downloaded-artifact-root>
```

Local regression and coverage follow the repository CI commands; the parsed
result is `local-regression.json` and `summary.json`.

## Seal verification

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-QUAL-01-evidence/seal.py verify HEAD
```

The qualification seal covers this directory and the independent report. The
producer seal remains valid at `870b600` and is not modified.
