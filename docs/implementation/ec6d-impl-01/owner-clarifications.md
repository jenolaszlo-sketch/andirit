# Owner clarifications recorded during implementation

These are explicit user decisions in the EC6D-IMPL-01 implementation chat on
2026-10-30. They are not independent-review dispositions. Historical contracts
and original fixture bytes remain unchanged.

1. **Version/API snapshots.** The user answered “Allow version/API snapshot
   updates.” Existing tests pinned p9 and the exact public type set, while
   the assignment required p10 and a new public versioned result. Only those
   expected version/API inventories were updated.
2. **Additive inventory scoping.** The user answered “Allow additive inventory
   scoping.” Two p9 tests treated every edge as an old provider/count. Their
   original expected counts/providers and original p8 fact fingerprints were
   preserved over the original facts, excluding the separately identified
   `ec6d.data` additions. New facts receive separate acceptance tests.
3. **Two bounded semantic corrections.** The user answered “Authorize both
   bounded corrections” to this concrete request:
   - Add p10 ambient named-export slots solely for ineligible-declaration
     attribution, preserving every existing p9 fact. The p9 allowlist omitted
     the ambient wrapper, making A26's requested attribution otherwise
     impossible. These slots never produce attributed declarations.
   - Use one bounded relative module-file lookup for recognized unsupported
     import forms solely to discover unknown candidates such as
     `import W = require('./types')`. The stored `relevanceTarget` is explicitly
     `relative-v1.relevance-only`. The real resolver outcome remains unsupported;
     this association cannot confirm a binding or become a possible consumer.
4. **A11 orphan alias.** The user answered “Authorize this A11 clarification.”
   Q2's original `MissingAlias` imports `Ghost` from an absent module, with no
   stored association to queried `Widget`. It remains an unresolved coverage
   occurrence, without inventing a Widget relationship. An additional fixture
   importing `Widget as MissingAlias` proves the required alias unknown. The
   original Q2 fixture is unchanged.

The expression-heritage case needed no amendment: symbol-binding gate 5
requires an associated *written name*. `factory(Widget)` is not that name;
its associated terminal identifier reaches expression gate 7. This preserves
both the specified gate order and the accepted expression reason.
