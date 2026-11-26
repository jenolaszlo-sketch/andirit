"""Resolve the commissioned Git objects and independently verify byte-sealed evidence."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
from zipfile import ZipFile

repo, frozen_authority, q1, hosted = sys.argv[1:5]
refs = {
    "frozen R1": "d3c9359d1abd8c89a7214e37f074802e5d2fa238",
    "R1 evidence": "dd484af2d32ff1f5b7951a85f4f3104c4b4b5402",
    "R2 tested": "a96c19a844f8ea1719508198b47df23c4e203571",
    "R2 payload": "f01465cf39677536794cc30db2b2875c76912a97",
    "R2 seal": "ab2bd168ab2436ffb2a07de8466b33cecd79720b",
    "R2 report": "f4dfec468c819fb922c8dfac61d4accece95ac28",
    "clean replay payload": "00a026a6ea7565175d4c4e7e6d2c9c83ce5a94ca",
    "published and clean replay seal": "f19587b0d0c19f5877d3ccfaefb4d6a5bc6f3c60",
    "Q1 review": "a69c13ce447839330793be2e4cbc7a1dd732d1dd",
    "pre-breach": "33b5867b593c7c7e43108b538bdcddad6fc4fb15",
}
prefix = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-"
baseline, tested, sealed, published = (refs[name] for name in (
    "R1 evidence", "R2 tested", "R2 seal", "published and clean replay seal"))
original, defective, erratum = (prefix + suffix for suffix in (
    "IMPL-01-evidence", "IMPL-01-R1-evidence", "IMPL-01-R2-R1-ERRATUM"))


def git(repository, *args):
    return subprocess.check_output(["git", "-C", repository, *args])


def text(repository, *args):
    return git(repository, *args).decode().strip()


def blob(repository, rev, path):
    return git(repository, "cat-file", "blob", f"{rev}:{path}")


def sha(data):
    return hashlib.sha256(data).hexdigest()


def directory(repository, rev, package):
    return {part.decode().removeprefix(package + "/") for part in
            git(repository, "ls-tree", "-r", "--name-only", "-z", rev, "--", package).split(b"\0") if part}


def verify(repository, revision, package, manifest_bytes=None):
    filename = package + "/manifest.json"
    observed = blob(repository, revision, filename)
    if manifest_bytes is not None:
        assert observed == manifest_bytes
    doc = json.loads(observed)
    entries = doc["files"]
    assert directory(repository, revision, package) == {row["path"] for row in entries} | {"manifest.json"}
    assert len({row["path"] for row in entries}) == len(entries)
    for row in entries:
        data = blob(repository, revision, package + "/" + row["path"])
        assert len(data) == row["bytes"] and sha(data) == row["sha256"], row["path"]
    print("seal", revision, package, "tree", text(repository, "rev-parse", f"{revision}:{package}"),
          "manifest_git", text(repository, "rev-parse", f"{revision}:{filename}"),
          "manifest_bytes_sha256", sha(observed), "identity", doc["manifest_sha256"], "entries", len(entries))
    return doc


for name, revision in refs.items():
    source = q1 if name == "Q1 review" else frozen_authority if name == "pre-breach" else repo
    print(name, text(source, "show", "-s", "--format=%H %T %P %an <%ae> | %cn <%ce> %s", revision))
for rev in (baseline, refs["frozen R1"]):
    print("delta", rev, tested, text(repo, "diff", "--name-status", rev, tested))
    assert not git(repo, "diff", "--name-only", rev, tested, "--", "src", "Directory.Build.props",
                   "Directory.Build.targets", ".github/workflows/publish.yml")
assert not git(repo, "diff", "--name-only", baseline, published, "--", original, defective)
assert not git(repo, "diff", "--name-only", tested, published, "--", "tests", "src", ".github", "Andirit.slnx")
expected_author = "jenol <jenolaszlo-sketch@users.noreply.github.com>"
revision_range = text(repo, "rev-list", "--reverse", "--first-parent", f"{baseline}..{published}").splitlines()
previous = baseline
for revision in revision_range:
    assert text(repo, "show", "-s", "--format=%P", revision) == previous
    assert text(repo, "show", "-s", "--format=%an <%ae>%n%cn <%ce>", revision).splitlines() == [expected_author] * 2
    assert "co-authored-by:" not in text(repo, "show", "-s", "--format=%B", revision).lower()
    previous = revision
assert previous == published
print("fast-forward parent chain", revision_range)
for path in (original, defective):
    print("preserved", path, text(repo, "rev-parse", f"{baseline}:{path}"), text(repo, "rev-parse", f"{published}:{path}"))
print("recovery ref", text(frozen_authority, "rev-parse", "refs/backups/s63-impl-original"))
pre_manifest = blob(frozen_authority, refs["pre-breach"], original + "/manifest.json")
assert len(verify(frozen_authority, refs["pre-breach"], original, pre_manifest)["files"]) == 17
assert len(verify(repo, published, original, pre_manifest)["files"]) == 17
assert text(frozen_authority, "rev-parse", f"{refs['pre-breach']}:{original}") == text(repo, "rev-parse", f"{published}:{original}")
q1doc = verify(q1, refs["Q1 review"], prefix + "QUAL-01-evidence")
assert len(q1doc["files"]) == 24
compact = lambda entries: sha(json.dumps(entries, sort_keys=True, separators=(",", ":")).encode())
assert compact(q1doc["files"]) == q1doc["manifest_sha256"]

for package, seal, count in ((prefix + "IMPL-01-R2-evidence", sealed, 12), (erratum, sealed, 7),
                            (prefix + "IMPL-01-R2-CHECKOUT-REPLAY", published, 5)):
    doc = verify(repo, published, package)
    assert len(doc["files"]) == count and compact(doc["files"]) == doc["manifest_sha256"]
    payload = doc["payloadCommit"]
    assert text(repo, "rev-parse", f"{payload}^{{tree}}") == doc["payloadRepositoryTree"]
    assert text(repo, "rev-parse", f"{payload}:{package}") == doc["payloadPackageTree"]
    assert directory(repo, payload, package) == {row["path"] for row in doc["files"]}
    assert not git(repo, "diff", "--name-only", seal, published, "--", package)
    for entry in doc["files"]:
        path = package + "/" + entry["path"]
        assert blob(repo, payload, path) == blob(repo, seal, path)

historic_manifest = blob(repo, baseline, defective + "/manifest.json")
assert sha(historic_manifest) == "c52701c2a01c2531905138bfc05b52165c2d6835769b538dc0f1a2e60987ddb0"
print("historical R1 manifest", text(repo, "rev-parse", f"{baseline}:{defective}/manifest.json"), sha(historic_manifest))
missing = "hosted-ci/coverage-latticedb-ubuntu.cobertura.xml"
result = {}
for entry in json.loads(historic_manifest)["files"]:
    name = entry["path"]
    try:
        historic = blob(repo, baseline, defective + "/" + name)
    except subprocess.CalledProcessError:
        assert name == missing
        historic = None
    if historic is not None and sha(historic) == entry["sha256"] and len(historic) == entry["bytes"]:
        continue
    recovered = blob(repo, published, erratum + "/recovered/" + name)
    assert (sha(recovered), len(recovered)) == (entry["sha256"], entry["bytes"])
    if historic is None:
        run = Path(hosted) / "45639342986"
        meta = json.loads((run / "artifacts.json").read_text(encoding="utf-8-sig"))["artifacts"]
        artifact = next(item for item in meta if item["id"] == 15034992001)
        archive = run / "coverage-results-ubuntu-latest.zip"
        assert "sha256:" + sha(archive.read_bytes()) == artifact["digest"]
        assert artifact["workflow_run"]["head_sha"] == refs["frozen R1"]
        with ZipFile(archive) as archive_file:
            assert archive_file.read("coverage-latticedb/coverage.cobertura.xml") == recovered
    else:
        assert historic.replace(b"\r\n", b"\n").replace(b"\n", b"\r\n") == recovered
    result[name] = "missing" if historic is None else sha(historic)
    print("recovered", name, "previous", result[name], "preserved expected", sha(recovered), len(recovered))
assert set(result) == {missing, "probes/probe-receipt.txt", "attribution-verification.txt"}
