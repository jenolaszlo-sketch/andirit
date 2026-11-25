"""Execute R2 gates from a newly checked-out, detached tested revision."""

import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

root, qualification, output = (Path(value).resolve() for value in sys.argv[1:])
script = Path(__file__).resolve().parent
output.mkdir()
native = "Andirit.LatticeDb.Tests"
worker = native + ".Worker"
worker_source = root / "tests" / worker / "bin/Release/net10.0"
worker_home = root / "tests" / native / "bin/Release/net10.0"
worker_dll = worker_home / (worker + ".dll")
all_publication = "FullyQualifiedName~NativePublicationCorrectnessTests"
d07 = "FullyQualifiedName~AbruptProcessTermination_DurableStateConsistent"
multiple = d07 + "|FullyQualifiedName~ConcurrentMultiProcessAccess_FileLockingAndConsistency|FullyQualifiedName~P15_MultiProcess"
negative_controls = "FullyQualifiedName~D07_ExitBeforeReadyFailsPromptly|FullyQualifiedName~D07_RecoveryOracleRejectsInvalidObservation|FullyQualifiedName~D05_CancellationOracleRejectsLeakedStageAndLostPublication|FullyQualifiedName~NativeOpenDeadlineTests"
evidence = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-"
evidence_q1 = qualification / (evidence + "QUAL-01-evidence")
prepared_source = root / (evidence + "IMPL-01-R1-evidence") / "probes"
os.environ.update(DOTNET_CLI_UI_LANGUAGE="en-US",
                  DOTNET_NOLOGO="1", RUNNER_OS="Windows", RUNNER_ARCH="X64",
                  GITHUB_RUN_ID="local-r2", GITHUB_RUN_ATTEMPT="1")
results = {}


def run(name, *argv, expect=0, require=None):
    cmd = [str(arg) for arg in argv]
    path = output / (name + ".log")
    started = time.monotonic()
    with path.open("wb") as stream:
        stream.write((json.dumps({"cwd": str(root), "argv": cmd}) + "\n").encode())
        stream.flush()
        result = subprocess.run(cmd, cwd=root, stdout=stream, stderr=subprocess.STDOUT, check=False)
        stream.write(f"\nexit={result.returncode} elapsed_seconds={time.monotonic()-started:.3f}\n".encode())
    log = path.read_text(encoding="utf-8", errors="replace")
    results[name] = result.returncode == expect and (require is None or require(log))
    print(name, result.returncode, "OK" if results[name] else "FAILED", flush=True)
    return results[name]


def test(name, project, folder, *args, **kwargs):
    return run(name, "dotnet", "test", f"tests/{project}/{project}.csproj",
               "-c", "Release", "--no-restore", "--results-directory", folder,
               "--logger", "trx;LogFileName=tests.trx", *args, **kwargs)


def coverage(key, project, include, threshold):
    return test("coverage-" + key, project, output / "test-results" / key,
                "/p:CollectCoverage=true", f"/p:CoverletOutput={output.as_posix()}/coverage/{key}/",
                "/p:CoverletOutputFormat=cobertura", f"/p:Include={include}",
                f"/p:Threshold={threshold}", "/p:ThresholdType=line%2cbranch")


run("checkout", "git", "status", "--porcelain=v1", "--branch", require=lambda log: "HEAD (no branch)" in log)
assert not worker_source.exists() and not worker_home.exists(), "Expected a clean per-project build"
run("sdk", "dotnet", "--info")
if not run("restore", "dotnet", "restore", "Andirit.slnx"):
    sys.exit(1)

# First build only the LatticeDb test project to verify the worker dependency.
if not coverage("latticedb", native, "[Andirit.LatticeDb]*", 70):
    sys.exit(1)
with (output / "worker-hashes.txt").open("w", encoding="utf-8", newline="\n") as receipt:
    for extension in (".dll", ".deps.json", ".runtimeconfig.json"):
        name = worker + extension
        original, copied = (hashlib.sha256(folder.joinpath(name).read_bytes()).hexdigest()
                            for folder in (worker_source, worker_home))
        print(name, original, copied, file=receipt)
        assert original == copied

parked = root / "obj/s63-r2-parked-worker"
parked.parent.mkdir(exist_ok=True)
shutil.move(worker_source, parked)
try:
    test("copied-worker", native, output / "copied-worker", "--no-build", "--filter", multiple)
    test("publication-16", native, output / "publication-16", "--no-build", "--filter", all_publication)
    test("new-controls", native, output / "new-controls", "--no-build", "--filter", negative_controls)
    for repeat in range(1, 21):
        name = f"d07-{repeat:02d}"
        test(name, native, output / name, "--no-build", "--filter", d07,
             require=lambda log: "Passed:     1" in log)

    early = root / "obj/s63-r2-early-worker"
    shutil.copytree(script / "early-worker", early)
    have_early = run("build-early-worker", "dotnet", "build", early / "EarlyWorker.csproj", "-c", "Release")
    original = worker_home / (worker + ".real.dll")
    worker_dll.rename(original)
    try:
        test("missing-worker", native, output / "missing-worker", "--no-build", "--filter", d07, expect=1,
             require=lambda log: "assembly was not built" in log)
        if have_early:
            shutil.copyfile(early / "bin/Release/net10.0/EarlyWorker.dll", worker_dll)
            test("early-worker", native, output / "early-worker", "--no-build", "--filter", d07, expect=1,
                 require=lambda log: "exited before READY (exit 19)" in log and "KILLED exit=" not in log)
    finally:
        original.replace(worker_dll)
finally:
    shutil.move(parked, worker_source)

run("format", "dotnet", "format", "Andirit.slnx", "--verify-no-changes", "--no-restore")
run("release", "dotnet", "build", "Andirit.slnx", "-c", "Release", "--no-restore")
run("solution", "dotnet", "test", "Andirit.slnx", "-c", "Release", "--no-build", "--no-restore",
    "--results-directory", output / "solution", "--logger", "trx;LogFilePrefix=solution")
coverage("core", "Andirit.Tests", "[Andirit]*%2c[Andirit.Abstractions]*", 55)
coverage("csharp", "Andirit.CSharp.Tests", "[Andirit.CSharp]*", 75)
coverage("treesitter", "Andirit.TreeSitter.Tests", "[Andirit.TreeSitter]*", 60)

commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
verify = root / ".github/scripts/verify-s63-results.ps1"
run("bind-test-results", "pwsh", "-NoProfile", "-File", verify, "-Capture", "-Results", output / "test-results",
    "-ExpectedSha", commit)
run("verify-test-results", "pwsh", "-NoProfile", "-File", verify, "-Results", output / "test-results",
    "-ExpectedSha", commit, "-ExpectedRunId", "local-r2", "-ExpectedRunAttempt", "1",
    "-ExpectedRunnerOS", "Windows", "-ExpectedRunnerArch", "X64")

prepared = root / "obj/s63-r2-prepared-probes"
prepared.mkdir()
for name in ("Program.cs", "Directory.Build.props"):
    shutil.copyfile(prepared_source / name, prepared / name)
project = "ReviewerProbes.csproj"
(prepared / project).write_text((prepared_source / project).read_text().replace(
    r"..\..\..\..\src\Andirit.LatticeDb\Andirit.LatticeDb.csproj",
    "../../src/Andirit.LatticeDb/Andirit.LatticeDb.csproj"), encoding="utf-8")
if run("build-prepared", "dotnet", "build", prepared / project, "-c", "Release", "-p:BuildProjectReferences=false"):
    run("prepared-A-I", "dotnet", prepared / "bin/Release/net10.0/Andirit.S63.ReviewerProbes.dll",
        require=lambda log: log.count(" PASS ") == 27)

reviewer = root / "obj/s63-r2-q1-replay"
shutil.copytree(evidence_q1 / "reviewer", reviewer)
if run("build-q1-replay", "dotnet", "build", reviewer / "Reviewer.csproj", "-c", "Release",
       f"-p:FrozenRoot={root}", "-p:BuildProjectReferences=false"):
    # Q1's exact harness expects the defect. Its one intentional assertion failure
    # after R2 must be accompanied by all five timeouts and all other probes passing.
    run("q1-replay-remediated", "dotnet", reviewer / "bin/Release/net10.0/Reviewer.dll", worker_dll, expect=1,
        require=lambda log: log.count("timeout repetition=") == 5 and "late_success repetition=" not in log
        and log.count("\nPASS ") == 5 and log.count("\nFAIL ") == 1 and "Late success was not reproduced" in log)

(output / "gates.json").write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
sys.exit(0 if all(results.values()) else 1)
