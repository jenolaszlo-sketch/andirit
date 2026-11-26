"""Review obligation and remediation surfaces through immutable source blobs."""
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")
repo = sys.argv[1]
r1 = "d3c9359d1abd8c89a7214e37f074802e5d2fa238"
r2 = "a96c19a844f8ea1719508198b47df23c4e203571"
surfaces = (
    "docs/decisions/adr-ci1-01-owner-decision.md",
    "docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md",
    "docs/decisions/adr-ci1-01-ci1a-s2-obligation-register.md",
    "docs/planning/adr-ci1-01-ci1a-impl-01/specification.md",
    "docs/planning/adr-ci1-01-ci1a-impl-01/acceptance-matrix.md",
    "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md",
    "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md",
    "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01.md",
    "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01.md",
    "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-qualification-plan.md",
    "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-traceability-matrix.md",
    "docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s63-impl-01.md",
    "src/Andirit.LatticeDb/LatticeCodeGraphStore.cs",
    "src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs",
    "src/Andirit/CodeOccurrenceReader.cs",
    "src/Andirit/InMemoryCodeGraphStore.cs",
)
expression = re.compile(
    r"crash\w*|durab\w*|persist\w*|recover\w*|atomic\w*|halt\w*|discontinu\w*|"
    r"should|may not|cannot|must not|must |supersed\w*|reopen\w*|concurr\w*|republicat\w*|"
    r"snapshot\w*|freshness|generat\w*|authority|order\w*|limit\w*|contributor\w*|"
    r"delet\w*|union\w*|conflict\w*|stale\w*|unverif\w*|unbound|scan\w*|determinist\w*|"
    r"equivalen\w*|semantic\w*|rebuild\w*|restor\w*|deadline|monotonic|cancel\w*|"
    r"bound\w*|examined|artifact|provenance|hosted|process|lock\w*|commit\w*", re.IGNORECASE,
)


def git(*args):
    return subprocess.check_output(["git", "-C", repo, *args]).decode("utf-8-sig").strip("\n")


def scan(path, old_blob, new_blob):
    print(f"== {path}  R1={old_blob}  R2={new_blob} ==")
    for number, line in enumerate(git("cat-file", "blob", new_blob).splitlines(), 1):
        if expression.search(line):
            print(f"{number} {line}")


for path in surfaces:
    old_blob = git("rev-parse", f"{r1}:{path}")
    new_blob = git("rev-parse", f"{r2}:{path}")
    assert old_blob == new_blob, f"Re-derive changed authority or production contract: {path}"
    scan(path, old_blob, new_blob)

for line in git("diff", "--name-status", f"{r2}^", r2).splitlines():
    status, path = line.split("\t")
    prior = git("rev-parse", f"{r1}:{path}") if status == "M" else "added"
    scan(path, prior, git("rev-parse", f"{r2}:{path}"))
