import hashlib, json, subprocess, sys
from pathlib import Path
ROOT = Path(r"C:\Windows\Temp\opencode\s64-qual\occ-id-qual")
C = "ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R1"
EV = f"docs/reviews/{C}-evidence"
REPORT = f"docs/reviews/{C}.md"
MANIFEST = f"{EV}/manifest.json"
PAYLOAD = "17fe571c5e23c6335ca8b847f2d5ee86da670f93"
SEAL = "880a888da2256608a91010026b1c3f39d0de76b6"

def git(*a):
    return subprocess.check_output(["git","-C",str(ROOT),*a])
def text(*a):
    return git(*a).decode().strip()
def blob(c,p):
    return git("cat-file","blob",f"{c}:{p}")
def sha(b): return hashlib.sha256(b).hexdigest()

out = {"commission": C, "payload": PAYLOAD, "seal": SEAL}
out["payload_tree"] = text("rev-parse", PAYLOAD+"^{tree}")
out["seal_tree"] = text("rev-parse", SEAL+"^{tree}")
out["seal_parent"] = text("rev-parse", SEAL+"^")
out["parent_is_payload"] = out["seal_parent"] == PAYLOAD
out["seal_delta"] = text("diff-tree","--no-commit-id","--no-renames","--name-status","-r",PAYLOAD,SEAL).splitlines()
out["seal_adds_only_manifest"] = out["seal_delta"] == ["A\t"+MANIFEST]
raw = blob(SEAL, MANIFEST)
m = json.loads(raw)
out["manifest_sha256"] = sha(raw)
out["manifest_schema"] = m.get("schema")
out["manifest_payload"] = m.get("payload")
out["manifest_payload_matches"] = m.get("payload",{}).get("commit")==PAYLOAD and m.get("payload",{}).get("tree")==out["payload_tree"]
entries = m.get("entries",[])
out["entry_count"] = len(entries)
out["entries_sorted"] = [e["path"] for e in entries] == sorted(e["path"] for e in entries)
out["entries_unique"] = len({e["path"] for e in entries}) == len(entries)
problems=[]
for e in entries:
    p=e["path"]
    try:
        data=blob(PAYLOAD,p)
    except subprocess.CalledProcessError:
        problems.append({"path":p,"problem":"missing_at_payload"}); continue
    actual={"path":p,"git_blob":text("rev-parse",f"{PAYLOAD}:{p}"),"bytes":len(data),"sha256":sha(data)}
    if actual != e:
        problems.append({"path":p,"expected":e,"actual":actual})
    try:
        if blob(SEAL,p)!=data:
            problems.append({"path":p,"problem":"differs_at_seal"})
    except subprocess.CalledProcessError:
        problems.append({"path":p,"problem":"missing_at_seal"})
out["entry_problems"] = problems
# Files present under evidence at payload, excluding manifest, plus report.
listed = set(text("ls-tree","-r","--name-only",PAYLOAD,"--",EV).splitlines())
expected = {REPORT, *(p for p in listed if p != MANIFEST)}
out["expected_paths"] = len(expected)
out["missing_from_manifest"] = sorted(expected - {e["path"] for e in entries})
out["extra_in_manifest"] = sorted({e["path"] for e in entries} - expected)
# Historical seals and predecessor evidence preservation.
for name, commit in {
    "mirror_producer":"870b600acf1490337af9f483f8894a5e8166a90e",
    "mirror_qualification":"b3951c790768ad257fb3418e5abe41cc5ecc5322",
    "reentry":"1776ea8ca12ba54dc85e817484711663480569a2",
    "blocked_d3":"d79617727dcdcdbc0ed659c1c69dc0e175787806",
    "investigation":"81102d21b4d6f6ed510b0555bb0d034aa1705c44",
    "implementation":"ec984bfe63e5fbe13047cee8920e00b634ab17b0",
}.items():
    out[name] = text("rev-parse", commit+"^{tree}")
# Investigation evidence unchanged between baseline and candidate.
inv = f"docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-evidence"
out["investigation_evidence_delta"] = text("diff","--name-status","81102d2","77ec887","--",inv).splitlines()
# Successor implementation report's own predecessor evidence path untouched by seal payload.
out["successor_evidence_files"] = len(listed)
print(json.dumps(out, indent=2))
sys.exit(0 if all([out["parent_is_payload"], out["seal_adds_only_manifest"], out["manifest_payload_matches"], out["entries_sorted"], out["entries_unique"], not problems, not out["missing_from_manifest"], not out["extra_in_manifest"], not out["investigation_evidence_delta"]]) else 1)
