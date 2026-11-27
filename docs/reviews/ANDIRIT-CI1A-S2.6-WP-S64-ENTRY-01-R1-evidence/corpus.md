# D3 corpus and provenance

## Frozen inputs

`corpus-manifest.json` binds every corpus source to its exact byte length and
SHA-256. The manifest is self-excluding and covers 52 source/project files and
14 expected-ledger artifacts.

| Corpus | Role | Composition |
| --- | --- | --- |
| **C1** | F12/R2 semantic corpus | 5 projects, 10 project source files, 1 external metadata image, 118 graph nodes and 350 graph edges by the independent oracle |
| **C2** | WP-S25-4 W continuity fixture | `Caller` + `Dependency`, 37 graph nodes and 97 graph edges; exact ID equality with the accepted S2.5 census |
| **C3** | Linked-file precedent | `ALib` + `BLib` + `App`; one shared source file, two project contexts, 14 nodes / 22 edges |
| **C4** | Added occurrence-contributor unit | C2 plus `Caller2`; one target (`QualifierDependency.Api.Mark`) and 11 call edges from two units; graph frozen, occurrence ledger outstanding |
| **H1** | Historical SQ-04 stores | Copy policy only in re-entry; DB/WAL used only as disposable copies per the existing provenance document |

C4 is the smallest extra caller unit derived from the W/App shape. C1 and C2
already provide nonempty call and reference witnesses; C4 exists for the
two-unit occurrence-contributor union required by the original traceability
matrix.

## Compilation inputs

- C# 14, `DocumentationMode.Diagnose`, deterministic Release DLL, nullable
  disabled, concurrent build disabled.
- Runtime metadata references from the host trust platform set; project
  references as compilation references.
- The C1 `External/ExternalLib.cs` image is compiled in memory as
  `Corpus.External`; it is not a project unit and therefore remains an external
  target for D3 negative controls.
- Defines for the symbol variants are `CORE_FEATURE` (Core) and `LEAF_TRACE`
  (Leaf); the reference-missing variant removes `App -> Core`.

## Oracle

`d3-oracle/` is a standalone Roslyn console. It reimplements the documented
legacy identity recipes:

- project/file/package node IDs: `csharp:<category>:sha256(canonical)`;
- symbol node IDs: `csharp:symbol:sha256(projectPath + "\n" + documentationId)`;
- declaration IDs: `csharp:sha256(scopedKey + "\n" + path + "\n" + span)`;
- edge IDs: `csharp:sha256(kind + "\n" + source + "\n" + target + "\n" + discriminator)`.

It emits structural (`contains`, `declares`, `depends-on`), semantic (`calls`,
`references`, `implements`, `inherits`, `imports`) and first-site evidence
locations. It does not invoke the production extractor.

## Validation

`validation.json` records 27 passing checks:

- C2 node and edge ID sets exactly equal the accepted S2.5 continuity census;
- C1 source-changed keeps all node IDs and replaces exactly six `declares` edges
  caused by the variant's inserted comment lines;
- C1 symbols-core replaces one `declares` edge; symbols-leaf adds one node and
  two edges; reference-missing removes 79 edges;
- C1 Solo removal removes 11 nodes / 20 edges;
- C2 Caller removal removes 32 nodes / 91 edges;
- C3 exposes two linked contexts and App calls the BLib context; ALib removal
  removes 4 nodes / 6 edges;
- C4 has one target called from 11 methods across `Caller` and `Caller2`;
- F12 occurrence minima are met and the complete-empty/refusal controls hold.

## Determinism

Each oracle run was repeated and produced byte-identical JSON for C1–C4.
`validation.json` and `corpus-manifest.json` bind the frozen ledgers.

## Outstanding corpus item

The exact `usage4`/target occurrence ledger for the C4 two-unit contributor
union is not yet derived. The graph ledger and contributor topology are frozen;
the occurrence ledger must be produced from the accepted authority with the
production plugin/repository identities before the C4 occurrence case can be
qualified. This is the sole corpus/oracle preparation gap; it is recorded in
the re-entry findings and assigned to the implementation commission.
