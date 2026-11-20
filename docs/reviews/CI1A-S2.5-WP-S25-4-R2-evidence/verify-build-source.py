import hashlib
import json
from pathlib import Path
import subprocess
import sys

root = Path(sys.argv[1])
repo = Path("D:/projects/Andirit")
start = "6d4d45f7aedc85f4b52310cf9a3fa6082f885296"
fix1 = "786574e34de0e5e1b242a784a5dc7297e0162bb1"
fix2 = "25bc6e3b4305c7ce088deb36cfb000138dc76648"

def git(*args):
    return subprocess.run(["git", f"--git-dir={repo / '.git'}", *args],
                          check=True, capture_output=True).stdout

assert git("rev-parse", f"{fix1}^").decode().strip() == start
assert git("rev-parse", f"{fix2}^").decode().strip() == fix1
assert git("rev-parse", f"{fix2}^{{tree}}").decode().strip() == "2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3"
paths = git("diff", "--name-only", start, fix2).decode().splitlines()
assert paths == [
    "src/Andirit.CSharp/CSharpCodeGraphPlugin.Compilation.cs",
    "src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs",
    "src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs",
    "tests/Andirit.CSharp.Tests/S23PublicationIntegrationTests.cs",
]
for path in paths:
    expected = git("cat-file", "blob", f"{fix2}:{path}")
    assert (root / path).read_bytes() == expected, path
    print(json.dumps(dict(path=path, bytes=len(expected), sha256=hashlib.sha256(expected).hexdigest(),
                          revision=fix2)))

