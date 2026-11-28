# Cross-host and cross-path campaign

## 1. Windows x64 — xUnit host

`S64D3C4SuccessorTests` runs inside the xUnit test host and publishes the real
C4 corpus with the production plugin.

| Field | Value |
| --- | --- |
| Target | `asmctx4:sha256:8258e165d53336f87abf3b1d1ae3f220190dfaf848eaae2906222bf49940555f` |
| Cardinality | 51 |
| Distribution | 50 + 1 |
| Defining context | `compctx4:sha256:476de9e8c407f13756a931f8ca285ce7d254c19309be433cdf84d022165e31be` |
| Result | Pass (all four variants) |

## 2. Windows x64 — console host

`tools/console-host-probe` runs the same corpus in a plain console process.

| Field | Value |
| --- | --- |
| Target | `asmctx4:sha256:8258e165d53336f87abf3b1d1ae3f220190dfaf848eaae2906222bf49940555f` |
| Cardinality | 51 |
| Distribution | 50 + 1 |
| Reference count | 172 |
| Reference-set SHA-256 | `d582ffc45bbb505067ac40a203b009e791c3a4e9d8785a4a4f5646bb51bb1bcf` |
| Result | Pass; artifact `evidence/host-probe-console.json` |

The console and xUnit hosts produce the **same target, defining context and
occurrence ledger**, even though their process composition differs. The
predecessor implementation produced different identities in these hosts because
it consumed process TPA and absolute paths.

## 3. Cross-path determinism

- `SameCorpusAtDifferentPhysicalPaths_ProducesIdenticalTargetAndFacts` copies the
  C4 corpus to two distinct temporary roots and asserts identical target,
  defining context, production descriptor and all 51 occurrence IDs.
- `Wp7_Test6_DifferentPath_SameContentIsEquivalent` captures the same framework
  assembly at two absolute paths and asserts equal `refctx1` identity, equal
  defining context and equal target.
- `Wp7_Test6_CrossProfileEqualityIsNotClaimed` asserts the successor identity has
  no absolute path and does not substitute historical fixture labels.

## 4. Framework closure

`FrameworkClosure_ExcludesHostAndTestAssembliesAndAbsolutePaths` verifies:

- every reference is `refctx1|...`;
- no `C:`, backslash, `testhost`, `xunit`, `Andirit` or `Microsoft.TestPlatform`
  item appears;
- the reference assembly-name set equals the managed assembly set of the running
  `Microsoft.NETCore.App` shared framework directory;
- repeated analysis is deterministic.

## 5. Hosted campaign

| Host | Scope | Result |
| --- | --- | --- |
| GitHub Actions `windows-latest` x64 | exact-candidate regression + coverage + package smoke | **success** (job `140939754018`) |
| GitHub Actions `ubuntu-latest` x64 | genuine Linux regression + coverage | **success** (job `140939753793`) |
| GitHub Actions `macos-latest` x64 | regression only; no S6.5 claim | **success** (job `140939754002`) |

Hosted run: `https://github.com/jenolaszlo-sketch/andirit/actions/runs/46132247172` on candidate `77ec887368c62b404e0d9a8034d34b7f8cac8007`; all four jobs (Windows, Linux, macOS, pack) succeeded. The sealed candidate commit `c54ab638f51a321ef46144f50b84b356736a8cac` also passed all four jobs in run `46132997340`. Local Windows x64 exact-candidate results are recorded in
`evidence/local-release-tests.log`, `evidence/local-csharp-coverage.log` and
`evidence/local-package-smoke.log`.

## 6. Interpretation

The contract removes **incidental host composition and installation paths** from
identity. It does not claim that different runtime images (for example a Windows
versus Linux `System.Private.CoreLib.dll`) have identical bytes. Exact digests
are bound to the referenced runtime file contents per the accepted S2.0 §5 rule;
the semantic relations, domains, cardinality and contributor union are portable
and are verified on each host.


