"""Demonstrate that the CI evidence verifier fails closed on altered receipts."""

import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

verifier, source, sha = sys.argv[1:]
source = Path(source)
trx_path = "latticedb/tests.trx"
namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
ET.register_namespace("", namespace)


def update_trx(folder, kind):
    path = folder / trx_path
    document = ET.parse(path)
    results = document.find(".//{*}Results")
    case = next(node for node in results if ".P12_Concurrent" in node.get("testName", ""))
    counters = document.find(".//{*}Counters")
    if kind == "missing":
        results.remove(case)
        for counter in ("total", "executed", "passed"):
            counters.set(counter, str(int(counters.get(counter)) - 1))
    elif kind == "skipped":
        case.set("outcome", "NotExecuted")
    elif kind == "failed":
        case.set("outcome", "Failed")
    elif kind == "missing-worker-receipt":
        named = next(node for node in results if ".P15_MultiProcess" in node.get("testName", ""))
        stdout = named.find(".//{*}StdOut")
        assert stdout is not None
        stdout.text = ""
    document.write(path, encoding="utf-8", xml_declaration=True)


def invoke(directory, expected):
    return subprocess.run(["pwsh", "-NoProfile", "-File", verifier, "-Results", str(directory),
                           "-ExpectedSha", expected], text=True, errors="replace", capture_output=True)


original = invoke(source, sha)
assert original.returncode == 0, original.stdout + original.stderr
print("valid exact-subject receipts accepted")

controls = {
    "missing": "Mandatory case P-12",
    "skipped": "NotExecuted",
    "failed": "Failed",
    "missing-worker-receipt": "Mandatory case P-15",
    "invalid-xml": "Exception calling",
    "missing-file": "Expected exactly one bound TRX",
    "digest": "Missing or unbound result file",
    "revision": "lacks the exact commit",
}
for name, expected_error in controls.items():
    with tempfile.TemporaryDirectory(prefix="s63-trx-") as temporary:
        folder = Path(temporary) / "contents"
        shutil.copytree(source, folder)
        manifest = folder / "subject.json"
        metadata = json.loads(manifest.read_text(encoding="utf-8-sig"))
        expected_sha = sha
        if name in ("missing", "skipped", "failed", "missing-worker-receipt"):
            update_trx(folder, name)
        elif name == "invalid-xml":
            (folder / trx_path).write_text("<incomplete", encoding="utf-8")
        elif name == "missing-file":
            (folder / trx_path).unlink()
        elif name == "digest":
            with (folder / trx_path).open("ab") as stream:
                stream.write(b"\n")
        elif name == "revision":
            expected_sha = "0" * 40
        if name in ("missing", "skipped", "failed", "missing-worker-receipt", "invalid-xml"):
            metadata["trxSha256"][trx_path] = hashlib.sha256((folder / trx_path).read_bytes()).hexdigest()
            manifest.write_text(json.dumps(metadata), encoding="utf-8")
        actual = invoke(folder, expected_sha)
        assert actual.returncode != 0 and expected_error in actual.stderr, (name, actual.stdout, actual.stderr)
        print(f"{name} rejected, exit={actual.returncode}, diagnostic={expected_error}")
