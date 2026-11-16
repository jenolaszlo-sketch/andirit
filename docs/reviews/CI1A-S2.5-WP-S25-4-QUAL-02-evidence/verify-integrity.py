import hashlib, json, pathlib, re, subprocess, sys

repo = pathlib.Path(r"D:\projects\Andirit")
reviews = repo / "docs/reviews"
start = "d48feeaa316f76901b6e37264d6d115f689a7000"
old = "48370711a6247b972dfc83628618924e864be374"
qual = "8ab2d25d058e5bb4301094078f1737b139868e6a"
r1 = "afa136f4033f35d9a657d8c5f1d5db92d736b679"

def git(*args):
    return subprocess.run(["git", f"--git-dir={repo / '.git'}", *args],
                          check=True, capture_output=True).stdout

def sha(data):
    return hashlib.sha256(data).hexdigest()

def verify(name, head, tree, count, introducing_range, seal=None):
    root = reviews / name
    raw = (root / "manifest.json").read_bytes()
    if seal:
        assert sha(raw) == seal, name
    manifest = json.loads(raw)
    assert manifest["reviewedHead"] == head and manifest.get("reviewedTree", tree) == tree, name
    assert git("rev-parse", f"{head}^{{tree}}").decode().strip() == tree, name
    git("merge-base", "--is-ancestor", head, start)
    entries = manifest["files"]
    paths = {x["path"] for x in entries}
    assert len(entries) == len(paths) == count, name
    extra = sorted({p.relative_to(root).as_posix() for p in root.rglob("*") if p.is_file()} - paths - {"manifest.json"})
    if seal is None:
        assert not extra, (name, extra)
    for entry in entries:
        path = root / entry["path"]
        data = path.read_bytes()
        assert (len(data), sha(data)) == (entry["bytes"], entry["sha256"].lower()), path
    # Locate the first immutable commit with the complete sealed directory.
    rel = root.relative_to(repo).as_posix()
    history = git("log", "--format=%H", "--reverse", introducing_range, "--", rel).decode().splitlines()
    current_tree = git("rev-parse", f"{start}:{rel}").strip()
    first = next(commit for commit in history if git("rev-parse", f"{commit}:{rel}").strip() == current_tree)
    assert not git("diff", "--name-only", first, start, "--", rel), name
    # Verify the checked-in manifest itself has identical bytes.
    assert git("cat-file", "blob", f"{start}:{rel}/manifest.json") == raw, name
    print(json.dumps(dict(package=name, reviewedHead=head, reviewedTree=tree,
                          entries=count, manifestSha256=sha(raw), identicalSince=first, unmanifestedHistoricalFiles=extra)))

inventory = (repo / "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-evidence-inventory.md").read_text(encoding="utf-8")
pins = re.findall(r"/reviews/([^/)]+)/manifest\.json\) \| \x60([a-f0-9]{40})\x60 \| \x60([a-f0-9]{40})\x60 \| (\d+) \| \x60([a-f0-9]{64})\x60", inventory)
assert len(pins) == 9 and sum(int(row[3]) for row in pins) == 183
for name, head, tree, count, seal in pins:
    git("merge-base", "--is-ancestor", head, old)
    verify(name, head, tree, int(count), old, seal)
verify("CI1A-S2.5-WP-S25-4-QUAL-01-evidence", old, "3483bd278c7a628383a372cb788559cbdaf5c717", 126, f"{old}..{qual}")
verify("CI1A-S2.5-WP-S25-4-R1-evidence", r1, "2962442c4c1528193fe79df9ea0ecc31701a463d", 72, f"{r1}..{start}")

oracle = "97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9"
for rel in ("CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/oracle-r2.json",
            "CI1A-S2.5-WP-S25-4-QUAL-01-evidence/oracle-r2.json",
            "CI1A-S2.5-WP-S25-4-R1-evidence/verification/oracle-r2.json"):
    data = (reviews / rel).read_bytes()
    assert len(data) == 146851 and sha(data) == oracle, rel
    print(json.dumps(dict(oracle=rel, bytes=len(data), sha256=oracle)))


