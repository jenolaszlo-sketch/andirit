# Ownership-safety verification

A structurally valid manifest is not, by itself, proof that every referenced
native object belongs to the mirror. This section records the adversarial
controls and the resulting ownership model.

## Native deletion semantics

Probed directly against LatticeDbSharp `0.2.0`:

| Operation | Observed behavior |
| --- | --- |
| `DeleteNode(n)` with an incident unrelated edge | Node is removed; the unrelated edge remains as a dangling edge (`source`/`target` references the removed id). No cascade. |
| `DeleteEdge(source, target, type)` | Only the addressed edge is removed; nodes and other edges remain. |

Consequence: the cleanup must never delete a mirror node that is an endpoint of
an object it intends to retain. The engine will not delete that object, but it
will leave it dangling. The implementation therefore refuses the operation
before deleting anything when ownership is ambiguous.

## Trusted-manifest rule

A manifest is accepted as ownership evidence only when:

1. it is structurally valid and base64/JSON decodable;
2. native node ids are distinct, native edge ids are distinct, and neither set is
   null;
3. every node id is a **mirror node**: label `HetuGraphNode` plus property
   `hetu_json`;
4. every edge id is a **mirror edge**: carries `hetu_json` and starts at a mirror
   node; and
5. every referenced edge connects two nodes named by the same manifest.

Otherwise the manifest owns nothing. Its entries are not used for deletion or
retention; the repository is rebuilt from authoritative facts. This prevents a
manifest from naming an authoritative command-log record, an unrelated node, an
unrelated edge, or a node from another repository as if it were its own.

## Ambiguous-incident rule

If a candidate mirror node has a non-mirror incident edge, deleting the node
would either remove the unrelated edge (wrong) or leave it dangling (wrong).
The rebuild throws an explicit `InvalidDataException` naming the node and edge,
and the native transaction rolls back. Nothing is deleted. After the ambiguous
relationship is repaired, a subsequent enabled open rebuilds normally.

Retained edges also protect their endpoint nodes, so a sweep cannot delete a
node that another repository's manifest still references. Inconsistent
manifests (edge named without its endpoint nodes) are untrusted and therefore
cannot create a retained edge with unprotected endpoints.

## Adversarial controls (all pass)

| # | Scenario | Observed result |
| --- | --- | --- |
| 1 | Validly encoded manifest referencing an unrelated native node | Manifest untrusted; unrelated node and properties preserved; mirror rebuilt |
| 2 | Manifest referencing an authoritative `HetuUnit` command-log record | Manifest untrusted; every authoritative row key/payload byte-identical after reopen |
| 3 | Manifest referencing an unrelated native edge | Manifest untrusted; unrelated edge and endpoints preserved |
| 4 | Unrelated edge incident to a protocol-marked mirror node | Explicit ownership refusal; unrelated node, properties and edge preserved; repair then reopen succeeds |
| 5 | Cross-repository native-ID reference | Other repository's manifest and mapping intact; both repositories rebuild canonically |
| 6 | Multiple repositories with malformed or missing manifests | No authoritative loss; both repositories rebuild canonically |
| 7 | Inconsistent manifest edge without endpoint nodes | Manifest untrusted; canonical rebuild; no dangling mirror edge |
| 8 | Refusal followed by reopen | Refusal leaves all objects intact; repaired store reopens and rebuilds |
| 9 | Unrelated sentinel nodes, properties, list property and relationship | Native ids, notes, numbers, `tags` list and sentinel edge all preserved |

Before/after authoritative comparison includes **content and native identity** of
every `HetuRepository`, `HetuRun`, `HetuUnit`, `HetuIndexState`,
`AndiritOccurrenceUnit` and `AndiritOccurrenceMeta` row, not counts alone.

## Reproduction

The controls are permanent tests:

```
dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj \
  -c Release --filter "FullyQualifiedName~NativeGraphMirrorOwnershipTests"
```

Eight tests, all passing on the remediation candidate.
