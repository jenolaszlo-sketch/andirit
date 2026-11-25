"""Recover only the three R1 extension entries named by independent finding F-S63Q-02."""

import hashlib
import json
from pathlib import Path
import subprocess
import sys
from zipfile import ZipFile

repo, qualification_zip, output = map(Path, sys.argv[1:])
revision = "dd484af2d32ff1f5b7951a85f4f3104c4b4b5402"
base = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R1-evidence/"
artifact = "45639342986/coverage-results-ubuntu-latest/coverage-latticedb/coverage.cobertura.xml"
coverage = "hosted-ci/coverage-latticedb-ubuntu.cobertura.xml"
texts = ("attribution-verification.txt", "probes/probe-receipt.txt")


def git(*args):
    return subprocess.check_output(["git", "-C", str(repo), *args])


def blob(name):
    return git("cat-file", "blob", f"{revision}:{base}{name}")


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


manifest_bytes = blob("manifest.json")
manifest = json.loads(manifest_bytes)
entries = {item["path"]: item for item in manifest["files"]}


def recover(name, data):
    entry = entries[name]
    assert sha(data) == entry["sha256"] and len(data) == entry["bytes"], name
    destination = output / "recovered" / name
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)


result = {
    "r1Commit": revision,
    "r1ExtensionTree": git("rev-parse", f"{revision}:{base.rstrip('/')}").decode().strip(),
    "originalManifestGitBlob": git("rev-parse", f"{revision}:{base}manifest.json").decode().strip(),
    "originalManifestSha256": sha(manifest_bytes),
    "originalManifestDeclaredId": manifest["manifest_sha256"],
    "qualificationReceiptZipSha256": sha(qualification_zip.read_bytes()),
    "findings": [],
}
for name in texts:
    committed = blob(name)
    # Reconstruct the original manifest input, preserving encoding and all bytes other than line endings.
    crlf = committed.replace(b"\r\n", b"\n").replace(b"\n", b"\r\n")
    recover(name, crlf)
    result["findings"].append({
        "path": name, "originalBlob": git("rev-parse", f"{revision}:{base}{name}").decode().strip(),
        "expectedSha256": entries[name]["sha256"], "expectedBytes": entries[name]["bytes"],
        "committedSha256": sha(committed), "committedBytes": len(committed), "recoveredSha256": sha(crlf),
        "changedBeyondCrlf": False,
    })

path_probe = subprocess.run(["git", "-C", str(repo), "cat-file", "-e", f"{revision}:{base}{coverage}"],
                            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False)
assert path_probe.returncode != 0, "Historical XML unexpectedly present"
with ZipFile(qualification_zip) as archive:
    xml = archive.read("hosted/" + artifact)
    metadata = json.loads(archive.read("hosted/45639342986-artifacts.json").decode("utf-8-sig"))
    hosted = next(item for item in metadata["artifacts"] if item["name"] == "coverage-results-ubuntu-latest")
    assert hosted["workflow_run"]["head_sha"] == "d3c9359d1abd8c89a7214e37f074802e5d2fa238"
    assert hosted["workflow_run"]["id"] == 45639342986
recover(coverage, xml)
result["findings"].append({
    "path": coverage, "originalBlob": None, "expectedSha256": entries[coverage]["sha256"],
    "expectedBytes": entries[coverage]["bytes"], "committedSha256": None, "recoveredSha256": sha(xml),
    "artifact": hosted, "qualificationArchiveMember": "hosted/" + artifact,
})
(output / "reconciliation.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8", newline="\n")
