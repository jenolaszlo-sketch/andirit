# Cross-host, cross-platform and hosted evidence

## Local controls

| Control | Result |
| --- | --- |
| Windows x64, .NET 10.0.12 | 1,059 passed, 0 failed |
| WSL Ubuntu 26.04 x86_64, .NET 10.0.114, exact candidate `f8c8a28` | 1,059 passed, 0 failed |
| net10.0 on .NET 10 consumer | pack 10.0.12, 167 refs, complete |
| net8.0 on .NET 10 consumer | pack 8.0.31, complete, distinct descriptor |
| net9.0 on .NET 10 consumer | pack 9.0.20, complete, distinct descriptor |
| netstandard2.1 on .NET 10 consumer | NETStandard.Library.Ref 2.1.0, valid source complete |
| `netstandard2.0` | explicit refusal |
| two copies of the same reference content | equal `refctx1` identity |
| different reference content | different identity |
| C4 corpus at two physical roots | identical target/context/facts (test) |
| R1 generation-2 store | non-mutating refusal |
| reconstructed store | complete query and logical repeat equality |

Cross-OS exact identity equality is not claimed from test counts. Exact identity
equality is claimed only for equivalent reference-pack content.

## Hosted campaign

| Host | Scope | Result |
| --- | --- | --- |
| GitHub Actions `windows-latest` x64 | exact-candidate tree regression/coverage/package | **success** (run 46143882731) |
| GitHub Actions `ubuntu-latest` x64 | genuine Linux x64 regression/coverage | **success** (run 46143882731) |
| GitHub Actions `macos-latest` ARM64 | inherited bounded regression only | **success** (run 46143882731) |
| GitHub Actions seal run `46145138043` | Windows x64, Linux x64, macOS ARM64, pack | **success** (tree `73d2a83…`) |

The R1 hosted runs are accepted only under the owner's narrow tree-equivalence
exception and are recorded as synthetic PR merge commits, not exact candidate
checkouts. The R2 campaign prefers exact candidate/seal evidence; if the existing
workflow still checks out a synthetic merge, the tree-equivalence facts and any
further owner authorization requirement are recorded.


