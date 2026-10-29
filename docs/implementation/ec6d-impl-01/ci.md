# Hosted CI qualification

Recorded 2026-10-30 (Asia/Manila). Exact implementation/evidence commit:
`97c81b784a8b068904c8bfa78df3ab937d5d1a29`, tree
`2227568e92b5f8f3b2222912741c0d631cacc7f4`. Production source is
`093004e02a3438d3c1eb6a668602da4aef563d6a`; no production code changed in the
evidence commit or this CI/handoff update.

[GitHub Actions run 40955264718](https://github.com/jenolaszlo-sketch/andirit/actions/runs/40955264718)
completed successfully. Every job below passed:

| Job | Job ID | Evidence |
| --- | --- | --- |
| Ubuntu | 123733606547 | Restore, format, all four test/coverage suites, upload |
| Windows | 123733606817 | Restore, format, all four test/coverage suites, upload |
| macOS | 123733606932 | Restore, format, all four test/coverage suites, upload |
| Package/native consumer | 123733606677 | All six packages packed; native/managed providers, durable store, snapshot and conformance smoke |

The actual logs for each platform report core 274, CSharp 47, TreeSitter 157,
LatticeDB 27: **505 passed, zero failed, zero skipped per platform**.
All existing line/branch thresholds remain unchanged and passed. This is
producer hosted qualification; it does not issue independent acceptance.

The user explicitly approved the normal main push and evidence-only follow-ups
after automatic review rejected the initial direct-default-branch push.
No tag, release, publication workflow or NuGet publication was invoked.

This record binds an already completed run, not its own future commit. The
containing evidence-only update triggers its own CI; its exact commit/tree,
run and final synchronized state are verified after creation and reported in
the final producer response and external final-git-binding.json. Earlier green
CI is not substituted for that final run.
