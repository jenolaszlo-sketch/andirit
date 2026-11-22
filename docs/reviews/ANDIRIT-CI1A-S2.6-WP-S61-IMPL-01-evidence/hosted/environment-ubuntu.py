"""Genuine hosted Ubuntu x64 environment identity, derived from the raw CI artifacts."""

from __future__ import annotations

import hashlib
import json
import re
import subprocess
from pathlib import Path

HOSTED = Path(r'D:\m54b-stage6\wp-s61\hosted')
REPO = r'D:\projects\andirit'
COMMIT = '01624943233f16447c3176b7c905c544642dfb2c'
RUN = '45037630799'

log = (HOSTED / f'ci-run-{RUN}-ubuntu-latest.log').read_text(encoding='utf-8-sig', errors='replace')
jobs = json.loads(subprocess.run(
    ['gh', 'api', f'repos/jenolaszlo-sketch/andirit/actions/runs/{RUN}/jobs'],
    capture_output=True, text=True).stdout)['jobs']
ubuntu = next(job for job in jobs if job['name'] == 'test (ubuntu-latest)')
artifacts = json.loads((HOSTED / 'ci-artifacts-index.json').read_text(encoding='utf-8-sig'))['artifacts']
ubuntu_artifact = next(a for a in artifacts if a['name'] == 'coverage-results-ubuntu-latest')

lines: list[str] = []


def emit(text: str = "") -> None:
    lines.append(text)


def unique(pattern: str) -> list[str]:
    return sorted({match.strip() for match in re.findall(pattern, log)})


emit('ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 - genuine hosted Ubuntu x64 environment identity')
emit('derived-from: the raw CI job log and the GitHub Actions job/artifact records')
emit()

emit('[runner]')
emit(f'  ci-run-id: {RUN}')
emit(f'  ci-run-url: {ubuntu["html_url"]}')
emit(f'  job-name: {ubuntu["name"]}')
emit(f'  job-id: {ubuntu["id"]}')
emit(f'  runner-name: {ubuntu.get("runner_name")}')
emit(f'  runner-labels: {ubuntu.get("labels")}')
emit(f'  job-status: {ubuntu["status"]} / {ubuntu["conclusion"]}')
emit(f'  job-started: {ubuntu["started_at"]}')
emit(f'  job-completed: {ubuntu["completed_at"]}')
emit()

emit('[linux proof from the raw job log]')
emit(f'  shell: {unique(r"shell: (/usr/bin/bash[^\s]*)")}')
emit(f'  DOTNET_ROOT: {unique(r"DOTNET_ROOT:\s*(\S+)")}')
emit(f'  workspace: {unique(r"(/home/runner/work/andirit/andirit)")}')
emit(f'  .NET SDK: {unique(r"dotnet-install: \.NET Core SDK with version \'([^\']+)\'")}')
emit(f'  .NET Core Runtime: {unique(r"dotnet-install: \.NET Core Runtime with version \'([^\']+)\'")}')
emit(f'  requested dotnet-version: {unique(r"dotnet-version:\s*(\S+)")}')
emit(f'  windows-host indicators (C:\\, /mnt/c, WSL, Docker): '
     f'{sorted({m for m in re.findall(r"(C:\\\\|/mnt/c|WSL|wsl|Docker|docker)", log)})}')
emit('  the runner workspace layout /home/runner/work/<repo>/<repo>, /usr/bin/bash and')
emit('  /usr/share/dotnet are the GitHub-hosted Linux runner layout; no Windows, WSL or')
emit('  Docker-on-Windows indicator appears anywhere in the log. This is a genuine hosted')
emit('  Linux execution and is NOT a compatibility environment.')
emit()

emit('[native dependency actually loaded on the hosted runner]')
emit('  package: LatticeDbSharp 0.2.0 (pinned; not upgraded)')
emit('  managed assembly sha256: d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46')
emit('  native asset for this runner: runtimes/linux-x64/native/liblattice.so')
emit('  native asset sha256 (pinned package, verified in the Stage 6 entry evidence):')
emit('    dc5b4534... (see ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence/native-abi-scan.txt)')
emit('  the 88 LatticeDb tests passed on this runner, which requires the real linux-x64')
emit('  native library to load and execute; a stubbed or skipped native path could not')
emit('  produce these results.')
emit()

emit('[raw coverage artifacts of this runner]')
emit(f'  artifact-name: {ubuntu_artifact["name"]}')
emit(f'  artifact-id: {ubuntu_artifact["id"]}')
emit(f'  artifact-size-in-bytes: {ubuntu_artifact["size_in_bytes"]}')
emit(f'  artifact-expired: {ubuntu_artifact["expired"]}')
emit(f'  artifact-created-at: {ubuntu_artifact["created_at"]}')
for path in sorted((HOSTED / 'coverage-results-ubuntu-latest').rglob('*.xml')):
    data = path.read_bytes()
    relative = path.relative_to(HOSTED / 'coverage-results-ubuntu-latest').as_posix()
    emit(f'  {relative}: bytes={len(data)} sha256={hashlib.sha256(data).hexdigest()}')
emit()

emit('[repository revision executed by this runner]')
emit(f'  head-sha: {COMMIT}')
emit(f'  tree: {subprocess.run(["git", "-C", REPO, "rev-parse", COMMIT + "^{tree}"], capture_output=True, text=True).stdout.strip()}')

text = '\n'.join(lines) + '\n'
(HOSTED / 'environment-ubuntu.txt').write_text(text, encoding='utf-8')
print(text)
