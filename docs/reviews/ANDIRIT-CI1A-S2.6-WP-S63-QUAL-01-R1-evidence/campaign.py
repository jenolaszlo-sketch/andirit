"""Run independent Windows qualification from exact, detached source subjects."""

import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

root, q1, published, output = (Path(arg).resolve() for arg in sys.argv[1:5])
which = sys.argv[5]
frozen = "d3c9359d1abd8c89a7214e37f074802e5d2fa238"
tested = "a96c19a844f8ea1719508198b47df23c4e203571"
producer = "f19587b0d0c19f5877d3ccfaefb4d6a5bc6f3c60"
review = "a69c13ce447839330793be2e4cbc7a1dd732d1dd"
output.mkdir()
os.environ["DOTNET_CLI_UI_LANGUAGE"] = "en-US"
os.environ["DOTNET_NOLOGO"] = "1"
suites = {
    "core": ("Andirit.Tests", "[Andirit]*%2c[Andirit.Abstractions]*", 55),
    "csharp": ("Andirit.CSharp.Tests", "[Andirit.CSharp]*", 75),
    "treesitter": ("Andirit.TreeSitter.Tests", "[Andirit.TreeSitter]*", 60),
    "latticedb": ("Andirit.LatticeDb.Tests", "[Andirit.LatticeDb]*", 70),
}
project = suites["latticedb"][0]
worker = project + ".Worker"
worker_dir = root / "tests" / worker / "bin/Release/net10.0"
copied_dir = root / "tests" / project / "bin/Release/net10.0"
worker_dll = copied_dir / (worker + ".dll")
d07 = "FullyQualifiedName~AbruptProcessTermination_DurableStateConsistent"
outcomes = {}


def git(repo, *args):
    return subprocess.check_output(["git", "-C", str(repo), *args])


def run(label, *arguments, want=0, predicate=lambda _: True):
    arguments = [str(arg) for arg in arguments]
    begin = time.monotonic()
    with (output / (label + ".log")).open("wb") as log:
        log.write((json.dumps({"cwd": str(root), "argv": arguments}) + "\n").encode())
        log.flush()
        process = subprocess.run(arguments, cwd=root, stdout=log, stderr=subprocess.STDOUT, check=False)
        log.write(f"\nexit={process.returncode} seconds={time.monotonic()-begin:.3f}\n".encode())
    receipt = (output / (label + ".log")).read_text(encoding="utf-8", errors="replace")
    outcomes[label] = {"exit": process.returncode, "matches": process.returncode == want and predicate(receipt)}
    print(label, outcomes[label], flush=True)
    return outcomes[label]["matches"]


def test(label, name, *options, **expectation):
    return run(label, "dotnet", "test", f"tests/{name}/{name}.csproj", "-c", "Release", "--no-restore",
               "--results-directory", output / label, "--logger", "trx;LogFileName=tests.trx", *options, **expectation)


def coverage(label):
    name, include, threshold = suites[label]
    return test(label, name, "/p:CollectCoverage=true",
                f"/p:CoverletOutput={(output/label/'coverage').as_posix()}/", "/p:CoverletOutputFormat=cobertura",
                f"/p:Include={include}", f"/p:Threshold={threshold}", "/p:ThresholdType=line%2cbranch")


def export(repo, revision, prefix, names, destination):
    destination.mkdir(parents=True)
    for name in names:
        data = git(repo, "cat-file", "blob", f"{revision}:{prefix}/{name}")
        destination.joinpath(name).write_bytes(data)


def replay_q1():
    fixture = root / "obj/q2-original-q1"
    path = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-evidence/reviewer"
    export(q1, review, path, ("Reviewer.csproj", "Program.cs"), fixture)
    if run("build-q1", "dotnet", "build", fixture / "Reviewer.csproj", "-c", "Release",
           f"-p:FrozenRoot={root}", "-p:BuildProjectReferences=false"):
        run("q1-replay", "dotnet", fixture / "bin/Release/net10.0/Reviewer.dll", worker_dll,
            want=0 if which == "R1" else 1,
            predicate=lambda log: (log.count("late_success repetition=") == 5 and log.count("\nPASS ") == 6)
            if which == "R1" else (log.count("timeout repetition=") == 5 and
                                  log.count("\nPASS ") == 5 and log.count("\nFAIL ") == 1
                                  and "Late success was not reproduced" in log))


assert git(root, "rev-parse", "HEAD").decode().strip() == (tested if which == "R2" else frozen)
assert not git(root, "status", "--porcelain")
assert not worker_dir.exists() and not copied_dir.exists()
run("checkout", "git", "status", "--short", "--branch")
run("sdk", "dotnet", "--info")
run("os", "cmd", "/c", "ver")
if which == "R1":
    if run("r1-build", "dotnet", "build", f"tests/{project}/{project}.csproj", "-c", "Release"):
        replay_q1()
else:
    if not run("restore", "dotnet", "restore", "Andirit.slnx"):
        sys.exit(1)
    # Per-project build precedes any solution build. Remove the original worker from resolution during the tests.
    if not coverage("latticedb"):
        sys.exit(1)
    with (output / "worker-hashes.txt").open("w", encoding="utf-8", newline="\n") as receipt:
        for suffix in (".dll", ".runtimeconfig.json", ".deps.json"):
            hashes = [hashlib.sha256((parent / (worker + suffix)).read_bytes()).hexdigest()
                      for parent in (worker_dir, copied_dir)]
            print(suffix, *hashes, file=receipt)
            assert hashes[0] == hashes[1]
    parked = root / "obj/q2-parked"
    parked.parent.mkdir(exist_ok=True)
    shutil.move(worker_dir, parked)
    try:
        test("copied-worker", project, "--no-build", "--filter",
             d07 + "|FullyQualifiedName~ConcurrentMultiProcessAccess_FileLockingAndConsistency|FullyQualifiedName~P15_MultiProcess")
        test("publication", project, "--no-build", "--filter", "FullyQualifiedName~NativePublicationCorrectnessTests")
        test("deadline", project, "--no-build", "--filter", "FullyQualifiedName~NativeOpenDeadlineTests")
        test("recovery-controls", project, "--no-build", "--filter",
             "FullyQualifiedName~D07_RecoveryOracleRejectsInvalidObservation|FullyQualifiedName~D05_CancellationOracleRejectsLeakedStageAndLostPublication|FullyQualifiedName~D07_ExitBeforeReadyFailsPromptly")
        for trial in range(1, 21):
            test(f"d07-{trial:02d}", project, "--no-build", "--filter", d07)
        fixture = root / "obj/q2-early-worker"
        prefix = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R2-evidence/early-worker"
        export(published, producer, prefix, ("EarlyWorker.csproj", "Program.cs"), fixture)
        fixture_ok = run("build-early-worker", "dotnet", "build", fixture / "EarlyWorker.csproj", "-c", "Release")
        saved = copied_dir / (worker + ".real.dll")
        worker_dll.rename(saved)
        try:
            test("missing-worker", project, "--no-build", "--filter", d07, want=1,
                 predicate=lambda log: "assembly was not built" in log)
            if fixture_ok:
                shutil.copyfile(fixture / "bin/Release/net10.0/EarlyWorker.dll", worker_dll)
                test("early-worker", project, "--no-build", "--filter", d07, want=1,
                     predicate=lambda log: "exited before READY (exit 19)" in log and "KILLED exit=" not in log)
        finally:
            saved.replace(worker_dll)
    finally:
        shutil.move(parked, worker_dir)
    run("format", "dotnet", "format", "Andirit.slnx", "--verify-no-changes", "--no-restore")
    run("release", "dotnet", "build", "Andirit.slnx", "-c", "Release", "--no-restore")
    run("solution", "dotnet", "test", "Andirit.slnx", "-c", "Release", "--no-build", "--no-restore",
        "--results-directory", output / "solution", "--logger", "trx;LogFilePrefix=solution")
    for name in ("core", "csharp", "treesitter"):
        coverage(name)
    prepared = root / "obj/q2-prepared"
    export(published, producer, "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R1-evidence/probes",
           ("Program.cs", "Directory.Build.props", "ReviewerProbes.csproj"), prepared)
    project_file = prepared / "ReviewerProbes.csproj"
    project_file.write_text(project_file.read_text().replace(
        r"..\..\..\..\src\Andirit.LatticeDb\Andirit.LatticeDb.csproj",
        "../../src/Andirit.LatticeDb/Andirit.LatticeDb.csproj"), encoding="utf-8")
    if run("build-prepared", "dotnet", "build", project_file, "-c", "Release", "-p:BuildProjectReferences=false"):
        run("prepared", "dotnet", prepared / "bin/Release/net10.0/Andirit.S63.ReviewerProbes.dll",
            predicate=lambda log: log.count(" PASS ") == 27)
    replay_q1()

assert not git(root, "status", "--porcelain")
(output / "results.json").write_text(json.dumps(outcomes, indent=2) + "\n", encoding="utf-8")
sys.exit(0 if all(case["matches"] for case in outcomes.values()) else 1)
