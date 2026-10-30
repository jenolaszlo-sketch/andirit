# Consumer scenarios and pinned real-source validation

## 1. Change the validation helper

"I am changing CodeGraphBatchLimits.Positive. Show every call and the arguments
I should inspect."

[CodeGraphBatch.cs](../../../src/Andirit.Abstractions/CodeGraphBatch.cs) has eight
calls in the constructor, lines 108-125. Separate Roslyn inspection confirms
eight targets to M:Andirit.CodeGraphBatchLimits.Positive(System.Int32,System.String).
Current caller query: ONE aggregate edge at the first site; reference query:
one overlapping name-token edge. Source reading is required for the other seven.

Minimum proposed answer: selected Positive ID/declaration, eight expression
spans, one constructor owner group with usageCount=8, static-bound Resolved proof,
source hash/publication and supported-category completeness.
A constructor may OWN an invocation although selecting a constructor TARGET is
deferred. Arguments are inspected via bounded source handles, not unbounded text.

## 2. Change a cross-file publication wrapper

"I am changing FindDeclarationsWithProvenanceAsync. Which caller is outside
this file, and where do I inspect it?"

One call is observed from CodeGraphPublicationQuery.FindDeclarationsAsync,
[CodeGraphPublicationQuery.cs](../../../src/Andirit/CodeGraphPublicationQuery.cs)
55:23-58:31, to the service declaration in
[CodeGraphQueryService.cs](../../../src/Andirit/CodeGraphQueryService.cs).

Actual selected qualified name contains unqualified CancellationToken: an error
type in the raw plugin compilation context. Do not invent a fully bound identity.
A future V1 identity gate may refuse confirmation until that context is adequate.

Minimum proposed answer: selected declaration, site, owner declaration and
binding-context qualification in one shape. Consumers should not join separate
node/edge/contributor arrays merely to find a cross-file call.

## 3. Internal body change to the publication guard

"I plan to change EnsurePublication's body. Show calls, then explain whether
I must edit every caller."

The stronger SDK-implicit-using Roslyn context finds eleven invocations across
the service and bound-view files. Three occur in GetInterfaceImpactV1Async
(lines 50, 56, 58); three in GetDeclarationsInFileWithProvenanceAsync
(lines 457, 482, 494). Baseline retains only one calls edge, from
CodeGraphPublicationQuery.Require. This is NOT eleven simply collapsing to one:
raw plugin binding also omits many async expressions. Binding omissions and
occurrence collapse are separate findings.

Minimum proposed answer: distinct proven sites, unresolved-context coverage,
and potential inspection candidates. A body change does not prove callers need
edits. The packet does not promise eleven future confirmations unless approved
compilation inputs can prove them. No EC-6D behavior is changed for this example.

## 4. Interface target and runtime frontier

"I may change ICodeGraphReader.GetLatestPublicationAsync. Do these interface calls
prove calls to InMemoryCodeGraphStore's implementation?"

Baseline has seven incoming calls edges; the stronger oracle finds nine
invocations, including three in one GetInterfaceImpactV1Async caller.
The bound interface member is a static target; implementations are not substituted.
A type implements edge does not prove a call's runtime receiver.

Minimum response: interface member ID, sites, interface-bound classification,
runtime dispatch notEvaluated, direct owners and missing coverage. Interface
contract impact remains separately authorizable. Reflection/external callers/
generated code are coverage boundaries, not fabricated relationships.

## Real-source binding and observed comparison

[real-baseline.json](evidence/real-baseline.json) records exact names/IDs, locations,
call/reference edges, publication, timings, unit coverage and oracle sites.
[source-binding.json](evidence/source-binding.json) checks every input against
cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2.

The successful subset contains every top-level .cs/.csproj from
src/Andirit.Abstractions and src/Andirit: 47 files, 382,944 decoded UTF-8 bytes.
No obj-generated sources, tests, planning code or synthetic relationships enter it.
Its coverage includes three cross-project call attempts and 1,082 cross-project
reference attempts. No selected method is mislabeled a cross-project example;
controlled C03 supplies that independent requirement.

| Selected target | Stored call edges | Stored reference edges | Oracle invocations |
| --- | ---: | ---: | ---: |
| EnsurePublication | 1 | 1 | 11 |
| FindSymbolsByQualifiedNameAsync | 2 | 2 | 2 |
| FindDeclarationsWithProvenanceAsync | 1 | 1 | 1 |
| CodeGraphBatchLimits.Positive | 1 | 1 | 8 |
| ICodeGraphReader.GetLatestPublicationAsync | 7 | 7 | 9 |

The oracle is separately written but shares Roslyn and the packet author.
It explicitly adds seven standard SDK implicit global usings for real sources;
the plugin does not. It uses host references and source project references,
not MSBuild/restore/build semantics. Differences are not same-context recall.
Positive and the controlled fixture isolate occurrence loss without this ambiguity.

No selected ambiguous site was found in this subset; C11 remains the controlled
negative requirement. Observed external/unresolved counts cannot attribute a
frontier to one selected method. They must be labeled unit-scoped unknowns.

Adding the C# plugin source project fails in existing AddSymbol diagnostic
formatting. [Failure log](evidence/wider-source-failure.txt) preserves it.
The underlying inconsistent-symbol trigger and full-repository self-indexing
remain unqualified; a source fix would require separate authorization.

## Consumer procedure and future validation

Each baseline target uses exact lookup, declarations, callers, references:
four public queries, followed by source search/read and joins. Open-publication
is one shared additional call. The planning harness's recorded node collection
is investigation machinery, not a requirement for future consumers.

CI-1A should supply identity/declarations/distinct invocation sites/owners/
evidence/coverage in one bounded operation AFTER selection.
Candidate examples are proposed, not performance prototypes.
See [measurement protocol](performance-evaluation-plan.md).

After approval, repeat on this same source revision with an independently
reviewed binding context; add a successful broader real-repository run only
after the existing extraction failure is resolved under its own authority.
Keep unresolved/ambiguous/external frontiers explicit rather than replacing
them with source-search guesses.
