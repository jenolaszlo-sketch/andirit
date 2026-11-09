# WP-S25-1 bound authentic historical fixtures — provenance and handling record

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-1-BIND-01`. **Recorded:** 2026-11-09.
**Authority chain:** S2.5 entry owner decision `adr-ci1-01-ci1a-s2-5-entry-owner-01.md` §6 and
[work-package plan](../../../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md) (WP-S25-1);
S2.5 entry assessment §4; REM-01 owner acceptance (P13b); R2 historical-matrix evidence
(H-01/H-02); acceptance matrix §13.

The three files under this directory are **exact byte copies** of two accepted authentic
prior-writer fixture sets. They are immutable evidence: they are never opened by a store,
never repaired, regenerated, or line-ending transformed (this directory is guarded by
`.gitattributes` with `* -text`). Every execution stages exact copies to a disposable work
directory and opens only those copies.

## Set A — pre-REM occurrence store (authentic Andirit prior writer)

| Item | Value |
| --- | --- |
| Committed copies | `prerem/prerem-authentic.ltdb` — 86,016 bytes; `prerem/prerem-authentic.ltdb-wal` — 4,096 bytes |
| Complete SHA-256 (`.ltdb`) | `7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885` |
| Complete SHA-256 (`.ltdb-wal`) | `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4` |
| Source package | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-boundary-rem-01-q1\evidence\` (accepted REM-01-Q1 package, manifest 33/33 verified) |
| Writer provenance | Pinned Q1B baseline production code `4c6d26a1d103a62fffe2e2743467eedb5f21afc2` (tree `320e965f219d0b615da29665d7fa3e85f8bbfc15`), extracted with `git archive`; one graph unit and one occurrence fact published through the pre-REM production path (old four-field `ComputeTargetId`; **no** canonical `asmctx3:sha256:` prefix). Writer sources retained in the source package (`repro/prerem/writer/`). |
| Recorded behavior | Opening a disposable copy with the accepted store throws `CodeOccurrenceSchemaException` — refusal without reinterpretation (probe P13b, REM-01-Q1; store-wide refusal including graph-only access). |
| Staging rule | `.ltdb` and `.ltdb-wal` are staged **together** to a fresh disposable path before any open. |

## Set B — R2 baseline graph-only store (authentic Andirit prior writer)

| Item | Value |
| --- | --- |
| Committed copy | `baseline/baseline-graph-only.master.db` — 163,840 bytes |
| Complete SHA-256 | `f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0` |
| Source package | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-exp-02-r2\evidence\historical-master\baseline-graph-only.master.db` (R2 controlling reference incorporated by OWNER §F) |
| Writer provenance | Frozen baseline `CSharpCodeGraphPlugin` + `LatticeCodeGraphStore` at `31c33a934aa13e373d9a3a6ffbc54d789eef092c` (tree `a265f561243de37e1636490f99acb0b34245809e`); graph-only publication over the frozen repro corpus; store schema version 2; publication run `historical-master-1`. Per-file writer source hashes are recorded in the source package `provenance.json`. |
| Recorded behavior | H-01: the authentic baseline graph-only publication remains usable by legacy callers (callers ≥ 1); H-02: the occurrence query reports `AnalysisUnavailable`, never a successful empty. |
| WAL policy | The source package's adjacent `baseline-graph-only.master.db-wal` is **not** part of this fixture: it has no recorded hash, and the accepted R2 procedure copied the `.db` only to a fresh disposable copy before opening. It is not imported and no claim is made about it. |
| Open-mutation disclosure | Opening a legitimate LatticeDB file mutates bytes (recovery/metadata initialization). **No stable-byte claim is made for an opened copy**; only disposable copies are opened, and the committed master is never opened. |

## Non-negotiable handling rules

1. The committed masters are immutable inputs; tests receive staged copy paths only.
2. Complete SHA-256 values (never abbreviated) are verified before staging and after every
   execution; a mismatch stops the affected case.
3. The pre-REM database and WAL are staged together.
4. Temporary and staged artifacts remain outside committed evidence (system temp; the
   evidence `.gitignore` blocks `staging/` and `work/` as a second guard).
5. Constructed or tampered controls are never labeled authentic historical evidence.
6. Import authorization: the WP-S25-1 commission authorizes narrowly required fixture
   bindings or copies; provenance above was verified before import, and the committed copies
   were re-hashed after import (all three match the complete recorded values).
