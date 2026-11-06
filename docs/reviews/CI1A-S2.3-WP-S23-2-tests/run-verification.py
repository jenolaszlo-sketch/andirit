"""Reproduce WP-S23-2 qualification-preparation evidence on the current checkout."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = ROOT / "docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-IMPL-01-evidence"
PROBE = "docs/reviews/CI1A-S2.3-WP-S23-2-tests/NativeAuthorityProcessProbe.csproj"
RESULTS = []


def run(name, arguments):
    command = ["dotnet", *arguments]
    print("RUN " + name, flush=True)
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (EVIDENCE / (name + ".log")).write_bytes(result.stdout)
    RESULTS.append({"name": name, "command": command, "exit": result.returncode})
    print(result.stdout.decode("utf-8", errors="replace")[-3500:], flush=True)
    if result.returncode:
        raise SystemExit(result.returncode)


run("environment", ["--info"])
run("process-probe-build", ["build", PROBE, "-c", "Release", "--no-restore"])
dll = ROOT / "docs/reviews/CI1A-S2.3-WP-S23-2-tests/bin/Release/net10.0/Andirit.CSharp.Tests.dll"
with tempfile.TemporaryDirectory(prefix="andirit-s23-native-") as temp:
    database = str(Path(temp) / "publication.ltdb")
    steps = [
        ("publish-a", "publish", "a", "Available"),
        ("fresh-a", "query", "a", "Available"),
        ("stale-source", "query", "source", "StalePublication"),
        ("stale-options", "query", "options", "StalePublication"),
        ("publish-b", "publish", "source", "Available"),
        ("fresh-b", "query", "source", "Available"),
        ("old-a-after-b", "query", "a", "StalePublication"),
    ]
    for name, action, variant, expected in steps:
        run("process-" + name, [str(dll), action, variant, expected, database, str(EVIDENCE / ("process-" + name + ".json"))])

run("wp-s23-2-tests", ["test", "tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj", "-c", "Release", "--no-restore", "--filter", "FullyQualifiedName~S23NativeAuthorityTests", "--logger", "console;verbosity=normal", "--logger", "trx;LogFileName=wp-s23-2.trx", "--results-directory", str(EVIDENCE / "trx")])
run("native-suite", ["test", "tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj", "-c", "Release", "--no-restore"])
run("new01-independent-probes", ["test", "docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj", "-c", "Release", "--no-restore"])
run("wp7-stale-context", ["test", "tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj", "-c", "Release", "--no-build", "--filter", "FullyQualifiedName~S2_2Wp7IdentityStabilityTests"])
run("wp8-corpus", ["test", "tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj", "-c", "Release", "--no-build", "--filter", "FullyQualifiedName~S2_2Wp8CorpusIntegrationTests"])
run("solution-suite", ["test", "Andirit.slnx", "-c", "Release", "--no-restore"])
run("oracle-r2", ["run", "--project", "docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj", "-c", "Release", "--no-restore", "--", "--corpus", "docs/planning/adr-ci1-01-ci1a-s2-0/corpus", "--expected", "docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json", "--output", str(EVIDENCE / "oracle-r2.json")])
run("format-verify", ["format", "Andirit.slnx", "--verify-no-changes", "--no-restore"])
run("process-format-verify", ["format", PROBE, "--verify-no-changes", "--no-restore"])
run("build-release", ["build", "Andirit.slnx", "-c", "Release", "--no-restore"])
expected = ROOT / "docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json"
assert hashlib.sha256(expected.read_bytes()).hexdigest().upper() == "274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF"
assert hashlib.sha256((EVIDENCE / "oracle-r2.json").read_bytes()).hexdigest().upper() == "97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9"
for item in json.loads((EVIDENCE / "authority-inputs.json").read_bytes()):
    assert hashlib.sha256((ROOT / item["path"]).read_bytes()).hexdigest().upper() == item["sha256"], item["path"]
(EVIDENCE / "verification-results.json").write_text(json.dumps(RESULTS, indent=2) + "\n", encoding="utf-8")
print("ALL GATES PASSED; frozen oracle and authority inputs unchanged", flush=True)
