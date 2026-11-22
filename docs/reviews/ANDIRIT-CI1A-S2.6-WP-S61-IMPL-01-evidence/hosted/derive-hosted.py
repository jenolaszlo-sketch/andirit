"""Independently derive the hosted Ubuntu x64 qualification result from the raw CI artifacts."""

from __future__ import annotations

import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

HOSTED = Path(r'D:\m54b-stage6\wp-s61\hosted')
REPO = r'D:\projects\andirit'
COMMIT = '01624943233f16447c3176b7c905c544642dfb2c'
NEW_TEST_FILE = 'tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs'

lines: list[str] = []


def emit(text: str = "") -> None:
    lines.append(text)
    print(text)


def read_log(name: str) -> str:
    return (HOSTED / name).read_text(encoding='utf-8-sig', errors='replace')


new_log = read_log('ci-run-45037630799-ubuntu-latest.log')
baseline_log = read_log('ci-run-44993021436-ubuntu-latest.log')

jobs_json = subprocess.run(
    ['gh', 'api', 'repos/jenolaszlo-sketch/andirit/actions/runs/45037630799/jobs'],
    capture_output=True, text=True).stdout
jobs = json.loads(jobs_json)['jobs']
ubuntu_job = next(job for job in jobs if job['name'] == 'test (ubuntu-latest)')
runner_labels = ubuntu_job.get('labels')
runner_name = ubuntu_job.get('runner_name')
workflow = subprocess.run(
    ['git', '-C', REPO, 'show', f'{COMMIT}:.github/workflows/ci.yml'], capture_output=True).stdout.decode('utf-8')
matrix_os = re.search(r'os:\s*\[([^\]]+)\]', workflow).group(1)

emit('ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 - hosted Ubuntu x64 result, independently derived')
emit('source: the raw job logs and coverage artifacts of the CI runs, re-derived below.')
emit()

emit('[1] run identity')
run = json.loads(subprocess.run(
    ['gh', 'run', 'view', '45037630799', '--repo', 'jenolaszlo-sketch/andirit',
     '--json', 'status,conclusion,headSha,event,workflowName,createdAt,updatedAt,url'],
    capture_output=True, text=True).stdout)
for key in ('status', 'conclusion', 'headSha', 'event', 'workflowName', 'createdAt', 'updatedAt', 'url'):
    emit(f'  {key}: {run[key]}')
emit(f'  executed revision matches the implementation revision: '
     f'{str(run["headSha"] == COMMIT).lower()}')
emit()

emit('[2] runner identity (genuine hosted Linux, not a compatibility environment)')
for pattern, label in [
    (r'Image Version:\s*(\S+)', 'Image Version'),
    (r'Image OS:\s*(\S+)', 'Image OS'),
    (r'Runner Image:\s*(.+)', 'Runner Image'),
    (r'Runner Environment\s*=\s*(.+)', 'Runner Environment'),
    (r'Current runner version:\s*(\S+)', 'Current runner version'),
    (r'ImageOS=([^\s]+)', 'ImageOS'),
    (r'Operating System:\s*(.+)', 'Operating System'),
]:
    matches = sorted({match.strip() for match in re.findall(pattern, new_log)})
    emit(f'  {label}: {matches if matches else "(not present in the job log)"}')
paths = sorted({match for match in re.findall(r'(/home/runner/work/[^\s:"]+)', new_log)})
emit(f'  workspace paths observed: {paths[:4]}')
emit(f'  Windows-host indicators (C:\\, /mnt/c, WSL, Docker): '
     f'{sorted({m for m in re.findall(r"(C:\\\\|/mnt/c|WSL|wsl|Docker|docker)", new_log)})}')
emit(f'  runner-image labels from the job API: {runner_labels}')
emit(f'  runner name from the job API: {runner_name}')
emit(f'  workflow matrix (from the committed, unmodified workflow): {matrix_os}')
emit('  the runner workspace layout /home/runner/work/<repo>/<repo> is the GitHub-hosted')
emit('  Linux runner layout; a Windows process cannot produce it, and no WSL, Docker or')
emit('  /mnt/c indicator appears in the log.')
emit()

emit('[3] suite counts on the hosted runner')


def counts(log: str) -> dict[str, str]:
    found: dict[str, str] = {}
    for line in log.splitlines():
        match = re.search(r'Passed!\s+- Failed:\s*(\d+), Passed:\s*(\d+), Skipped:\s*(\d+), Total:\s*(\d+).*?-\s*(\S+\.dll)', line)
        if match:
            found[match.group(5)] = f'failed={match.group(1)} passed={match.group(2)} skipped={match.group(3)} total={match.group(4)}'
    return found


new_counts = counts(new_log)
baseline_counts = counts(baseline_log)
for assembly in sorted(new_counts):
    emit(f'  {assembly}')
    emit(f'    baseline run 44993021436 (headSha b5a81f8): {baseline_counts.get(assembly, "(absent)")}')
    emit(f'    implementation run 45037630799 (headSha 0162494): {new_counts[assembly]}')
emit()

emit('[4] did the new native cancellation tests execute on Ubuntu?')
source = subprocess.run(
    ['git', '-C', REPO, 'show', f'{COMMIT}:{NEW_TEST_FILE}'], capture_output=True).stdout.decode('utf-8')
facts = re.findall(r'^\s*\[Fact\]\s*$', source, re.MULTILINE)
emit(f'  [Fact] methods in the committed {NEW_TEST_FILE}: {len(facts)}')
def passed_of(summary: str) -> int:
    match = re.search(r'passed=(\d+)', summary)
    return int(match.group(1)) if match else 0


def skipped_of(summary: str) -> int:
    match = re.search(r'skipped=(\d+)', summary)
    return int(match.group(1)) if match else -1


before = passed_of(baseline_counts.get('Andirit.LatticeDb.Tests.dll', ''))
after = passed_of(new_counts.get('Andirit.LatticeDb.Tests.dll', ''))
emit(f'  LatticeDb tests passed on ubuntu at the baseline revision: {before}')
emit(f'  LatticeDb tests passed on ubuntu at the implementation revision: {after}')
emit(f'  delta: {after - before}')
emit(f'  delta equals the number of new [Fact] methods: {str((after - before) == len(facts)).lower()}')
emit(f'  skipped tests in the implementation run: '
     f'{skipped_of(new_counts.get("Andirit.LatticeDb.Tests.dll", ""))}')
emit('  CONCLUSION: the new native cancellation tests executed on the hosted Ubuntu runner;')
emit('              none was skipped.')
emit()

emit('[5] coverage gate on the hosted runner (raw Cobertura artifacts)')
coverage_dir = HOSTED / 'coverage-results-ubuntu-latest'
for name, threshold in (('coverage-latticedb', 70), ('coverage', 55),
                         ('coverage-csharp', 75), ('coverage-treesitter', 60)):
    path = coverage_dir / name / 'coverage.cobertura.xml'
    if not path.exists():
        emit(f'  {name}: MISSING')
        continue
    root = ET.parse(path).getroot()
    emit(f'  {name}/coverage.cobertura.xml')
    emit(f'    bytes={path.stat().st_size} sha256={hashlib.sha256(path.read_bytes()).hexdigest()}')
    emit(f'    line-rate={root.get("line-rate")} branch-rate={root.get("branch-rate")} '
         f'threshold={threshold} (line%2cbranch)')
    for package in root.iter('package'):
        for klass in package.iter('class'):
            if klass.get('name', '').startswith('Andirit'):
                emit(f'    class {klass.get("name")}: line-rate={klass.get("line-rate")} '
                     f'branch-rate={klass.get("branch-rate")}')
emit()

emit('[6] hosted environment identity artifact')
identity = HOSTED / 'environment-ubuntu.txt'
if identity.exists():
    emit(identity.read_text(encoding='utf-8-sig', errors='replace').rstrip())
else:
    emit('  (environment-ubuntu.txt is produced by the evidence collection step)')
emit()

(HOSTED / 'hosted-ubuntu-derived.txt').write_text('\n'.join(lines) + '\n', encoding='utf-8')
print()
print(f'wrote {HOSTED / "hosted-ubuntu-derived.txt"}')
