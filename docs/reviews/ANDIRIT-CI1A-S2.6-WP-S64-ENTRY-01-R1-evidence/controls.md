# Corpus and oracle controls

## Positive controls

| Control | Expected |
| --- | --- |
| C1 baseline | 118 nodes / 350 edges; 53 invocations, 21 references, 5 overload sets |
| C2 baseline | 37 nodes / 97 edges, exact IDs equal to the accepted S2.5 continuity census |
| C3 baseline | Two `Example.Helper` contexts; one App call to the BLib context |
| C4 baseline | One `Api.Mark` target; 11 call edges across `Caller` and `Caller2` |
| C1 source-changed | All node IDs stable; six `declares` edges replaced; target stable |
| C1 symbols-leaf | One new node and two new edges |
| C1 reference-missing | 79 edges removed; node IDs stable |
| C1 no-Solo | 11 nodes / 20 edges removed |
| C2 no-Caller | 32 nodes / 91 edges removed; Dependency survives |
| C3 no-ALib | 4 nodes / 6 edges removed; BLib and App survive |

## Negative controls

| Control | Expected |
| --- | --- |
| `Solo` | Complete verified scope with zero invocation occurrences |
| `N01` ambiguous call | No confirmed call edge; ambiguous outcome |
| `N02` unresolved call | No confirmed call edge; unresolved outcome |
| `N03` dynamic call | No confirmed call edge; dynamic outcome |
| `E01` external metadata target | No source-defined edge; external outcome |
| `I01` inactive preprocessor branch | No occurrence and no graph relationship |
| `RM01` missing Core reference | S01 unresolved; not a confirmed target |
| Wrong overload same-name shadow | No matching edge to the wrong target |
| Substituted endpoint | Comparator rejects the edge |
| Extra occurrence fact injected into graph | Graph census rejects it |

## Comparator controls required in implementation

The D3 implementation must include mutation controls that alter a disposable
copy of an expected ledger or receipt, not production data:

- remove one expected node or edge;
- substitute one endpoint or target;
- add a same-count wrong-ID edge;
- add an occurrence fact to a graph surface;
- reorder a traversal result where order is contractual;
- delete a manifest entry while retaining the physical row.

Each mutation must be detected by the named comparator.

## Boundary

These controls are corpus/oracle preparation. They do not replace the
independent qualification authority's own adversarial campaign under the
successor `ANDIRIT-CI1A-S2.6-WP-S64-QUAL-01`.
