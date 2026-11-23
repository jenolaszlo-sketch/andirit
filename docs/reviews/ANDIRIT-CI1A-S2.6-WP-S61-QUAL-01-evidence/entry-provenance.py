"""Entry & provenance verification for ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01 (independent reviewer)."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path

REPO = Path(r'D:\projects\andirit')
ENTRY = '2d1e8ec88e397d0c36b7f258eb1a1c0840e56157'
IMPL = '01624943233f16447c3176b7c905c544642dfb2c'
EVID = '3eb1339304ff843336f6165575904350df56d041'
QUALIFIED = 'eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6'
REMOTE_BASE = 'b5a81f81fade9807f706b4586329efe416cf1558'
TAG_OBJECT = '6659647527a6e0eb822a3d7a05b8283c75fd6a3e'
TAG_TARGET = '6ed05964d2fcc758648b654a01ec706647e52183'

out: list[str] = []
ok = True
notes: list[str] = []


def emit(text: str = '') -> None:
    out.append(text)
    print(text)


def git(*args: str, binary: bool = False):
    result = subprocess.run(['git', '-C', str(REPO), *args], capture_output=True)
    if result.returncode != 0:
        return None if binary else result.stderr.decode('utf-8', 'replace').strip()
    return result.stdout if binary else result.stdout.decode('utf-8', 'replace').strip()


def check(label: str, condition: bool, detail: str = '') -> None:
    global ok
    if not condition:
        ok = False
    emit(f'[{"PASS" if condition else "FAIL"}] {label}' + (f' - {detail}' if detail else ''))


def blob(rev: str, path: str) -> bytes | None:
    result = subprocess.run(['git', '-C', str(REPO), 'cat-file', 'blob', f'{rev}:{path}'], capture_output=True)
    return result.stdout if result.returncode == 0 else None


emit('ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01 - entry and provenance verification')
emit('reviewer role: independent qualification (not the implementation producer)')
emit()

emit('== 1. repository identities ==')
for label, rev in (('entry baseline', ENTRY), ('implementation', IMPL), ('evidence', EVID)):
    obj_type = git('cat-file', '-t', rev)
    tree = git('show', '-s', '--format=%T', rev)
    subject = git('show', '-s', '--format=%s', rev)
    emit(f'  {label:16s} {rev}')
    emit(f'  {"":16s} type={obj_type} tree={tree}')
    emit(f'  {"":16s} subject={subject}')
    check(f'{label} object exists and is a commit', obj_type == 'commit', str(obj_type))
emit()

emit('== 2. ancestry ==')
for label, a, b in (('entry -> implementation', ENTRY, IMPL),
                    ('implementation -> evidence', IMPL, EVID),
                    ('qualified implementation eafd632 -> evidence', QUALIFIED, EVID),
                    ('remote base -> implementation', REMOTE_BASE, IMPL)):
    code = subprocess.run(['git', '-C', str(REPO), 'merge-base', '--is-ancestor', a, b]).returncode
    check(f'{label} is an ancestor relation', code == 0, f'is-ancestor exit={code}')
nonancestor = subprocess.run(['git', '-C', str(REPO), 'merge-base', '--is-ancestor', IMPL, ENTRY]).returncode
check('implementation is NOT an ancestor of the entry baseline (forward direction only)', nonancestor != 0)
emit()

emit('== 3. working tree and branch state ==')
status = git('status', '--porcelain')
check('working tree clean', not status, status or '(clean)')
branch = git('rev-parse', '--abbrev-ref', 'HEAD')
check('on branch main', branch == 'main', branch)
head = git('rev-parse', 'HEAD')
check('HEAD is the pinned evidence revision', head == EVID, head)
counts = git('rev-list', '--left-right', '--count', 'HEAD...origin/main')
emit(f'  HEAD...origin/main = {counts}  (1 0 = local evidence commit is unpushed, by design)')
remote_main = git('ls-remote', 'origin', 'refs/heads/main').split()[0]
check('origin/main is the pinned implementation revision', remote_main == IMPL, remote_main)
check('the local-only evidence revision is preserved and not pushed', remote_main == IMPL and head == EVID)
emit()

emit('== 4. published state untouched ==')
tag_remote = git('ls-remote', 'origin', 'refs/tags/v0.2.0-preview.7').split()[0]
check('remote tag object unchanged', tag_remote == TAG_OBJECT, tag_remote)
tag_type = git('cat-file', '-t', TAG_OBJECT)
tag_peeled = git('rev-parse', f'{TAG_OBJECT}^{{}}')
emit(f'  tag object type   = {tag_type}')
emit(f'  tag peeled commit = {tag_peeled}')
check('tag is an annotated tag object', tag_type == 'tag', str(tag_type))
check('tag still peels to the same commit as at the entry baseline',
      tag_peeled == TAG_TARGET, tag_peeled)
version_line = None
for line in blob(EVID, 'Directory.Build.props').decode('utf-8').splitlines():
    if '<Version>' in line:
        version_line = line.strip()
        break
emit(f'  package version    = {version_line}')
check('release version still 0.2.0-preview.7', version_line is not None and '0.2.0-preview.7' in version_line,
      str(version_line))
check('Directory.Build.props unchanged between entry and evidence',
      blob(ENTRY, 'Directory.Build.props') == blob(EVID, 'Directory.Build.props'))
emit()

emit('== 5. change-set boundary (entry baseline -> implementation) ==')
emit('  5a. production / test / CI scope')
delta = git('diff', '--name-status', ENTRY, IMPL, '--', 'src', 'tests', '.github').splitlines()
for line in delta:
    emit('     ' + line)
names = [line.split('\t')[-1] for line in delta]
check('no change under .github/', not any(n.startswith('.github/') for n in names))
check('no production project file changed', not any(n.endswith('.csproj') for n in names if n.startswith('src/')))
check('no test project file changed', not any(n.endswith('.csproj') for n in names if n.startswith('tests/')))
check('exactly 3 changed paths in src/tests/.github', len(names) == 3, str(len(names)))
src_changes = sorted(n for n in names if n.startswith('src/'))
check('src changes limited to the two contract-documentation files',
      src_changes == ['src/Andirit.LatticeDb/LatticeCodeGraphStore.cs', 'src/Andirit/ICodeGraphStore.cs'],
      str(src_changes))
emit('  5b. full scope (entry baseline -> implementation) - documentation and evidence only')
full = git('diff', '--name-status', ENTRY, IMPL).splitlines()
code_paths = [l.split('\t')[-1] for l in full
              if l.split('\t')[-1].endswith(('.cs', '.csproj', '.props', '.targets', '.yml', '.yaml'))]
evidence_tools = [p for p in code_paths if '-evidence/' in p]
non_evidence_code = [p for p in code_paths if '-evidence/' not in p]
emit(f'     total changed paths: {len(full)}')
emit(f'     code/CI/config paths INSIDE the evidence package (isolated tools): {evidence_tools}')
emit(f'     code/CI/config paths outside the evidence package: {non_evidence_code}')
check('no code/CI/config file other than the two documented src files and the new test file',
      sorted(non_evidence_code) == ['src/Andirit.LatticeDb/LatticeCodeGraphStore.cs',
                                    'src/Andirit/ICodeGraphStore.cs',
                                    'tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs'],
      str(sorted(non_evidence_code)))
check('the only additional code files are the two isolated evidence tools inside the evidence package',
      sorted(evidence_tools) == sorted([
          'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence/tools/apidump/Program.cs',
          'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence/tools/apidump/apidump.csproj',
          'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence/tools/dllcompare/Program.cs',
          'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence/tools/dllcompare/dllcompare.csproj']),
      str(sorted(evidence_tools)))
emit('  5c. the evidence tools are not referenced by any production or test project')
refs = git('grep', '-l', 'apidump\|dllcompare', IMPL, '--', 'src', 'tests', '*.slnx', '*.props')
check('no production/test project references the evidence tools', not refs, repr(refs))
emit()

emit('== 6. independent structural check of the two src changes ==')
for path in src_changes:
    diff = git('diff', ENTRY, IMPL, '--', path)
    added = [l for l in diff.splitlines() if l.startswith('+') and not l.startswith('+++')]
    removed = [l for l in diff.splitlines() if l.startswith('-') and not l.startswith('---')]
    code_lines = [l for l in added
                  if l[1:].strip() and not l[1:].strip().startswith('//')
                  and '///' not in l and '*/' not in l and '<' not in l]
    emit(f'  {path}: +{len(added)} -{len(removed)}')
    emit(f'    non-comment/non-XML added lines: {len(code_lines)}')
    for l in code_lines:
        emit(f'      {l}')
    check(f'  {path} contains no executable added line', len(code_lines) == 0, str(code_lines))
    check(f'  {path} contains no removed line', not removed, str(removed))
emit()

emit('== 7. evidence package manifests, verified against committed Git blobs ==')
packages: dict[str, str] = {}
for line in git('ls-tree', '-r', '--name-only', EVID).splitlines():
    if line.endswith('/manifest.json'):
        packages[line.rsplit('/', 1)[0]] = line
emit(f'  repository packages with a manifest: {len(packages)}')
total_entries = 0
bad: list[tuple[str, list[str]]] = []
historical_gap: list[tuple[str, list[str]]] = []
KNOWN_GAP = 'docs/reviews/CI1A-S2.5-WP-S25-2-G2-EXEC-evidence'
for package, manifest_path in sorted(packages.items()):
    raw = blob(EVID, manifest_path)
    manifest = json.loads(raw.decode('utf-8-sig'))
    mismatches = []
    for entry in manifest['files']:
        data = blob(EVID, f"{package}/{entry['path']}")
        if data is None:
            mismatches.append(f"{entry['path']} (absent)")
        elif len(data) != entry['bytes']:
            mismatches.append(f"{entry['path']} (length)")
        elif hashlib.sha256(data).hexdigest().lower() != entry['sha256'].lower():
            mismatches.append(f"{entry['path']} (sha256)")
    total_entries += len(manifest['files'])
    if mismatches:
        (historical_gap if package == KNOWN_GAP else bad).append((package, mismatches))
    emit(f'    {package}: {len(manifest["files"])} entries, {len(mismatches)} mismatches')
check('all committed package manifests match their committed blobs, except the known pre-existing gap',
      not bad, str(bad))
check('the only mismatch anywhere is the known pre-existing 4-file gap in the S2.5 G2 EXEC package',
      len(historical_gap) == 1 and len(historical_gap[0][1]) == 4, str(historical_gap))
emit(f'  total manifest entries across the repository: {total_entries}')
emit('  independent re-verification that the 4-file gap is PRE-EXISTING (not introduced by this lineage):')
for rev, label in ((REMOTE_BASE, 'remote base b5a81f8'), (ENTRY, 'entry baseline 2d1e8ec'), (EVID, 'evidence 3eb1339')):
    manifest = json.loads(blob(rev, f'{KNOWN_GAP}/manifest.json').decode('utf-8-sig'))
    absent = [e['path'] for e in manifest['files'] if blob(rev, f"{KNOWN_GAP}/{e['path']}") is None]
    emit(f'    {label}: {len(absent)} of {len(manifest["files"])} manifest entries absent')
    check(f'    gap identical at {label}', set(absent) == {
        'coverage-results-ubuntu-latest/coverage/coverage.cobertura.xml',
        'coverage-results-ubuntu-latest/coverage-csharp/coverage.cobertura.xml',
        'coverage-results-ubuntu-latest/coverage-latticedb/coverage.cobertura.xml',
        'coverage-results-ubuntu-latest/coverage-treesitter/coverage.cobertura.xml'}, str(sorted(absent)))
emit()

newpkg = 'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence'
manifest = json.loads(blob(EVID, f'{newpkg}/manifest.json').decode('utf-8'))
listed = {e['path'] for e in manifest['files']}
actual = {p[len(newpkg) + 1:] for p in git('ls-tree', '-r', '--name-only', EVID, newpkg).splitlines()
          if p != f'{newpkg}/manifest.json'}
check('S6.1 evidence package manifest is complete over the committed tree',
      listed == actual, f'{len(listed)} listed / {len(actual)} committed')
check('S6.1 evidence package manifest is self-excluding', 'manifest.json' not in listed)
emit(f'  S6.1 package: {len(listed)} entries, {manifest["totalBytes"]} bytes')
emit()

emit('== 8. key evidence artifacts present at the pinned revisions ==')
required = [
    f'{newpkg}/preflight.txt',
    f'{newpkg}/contract-gate.txt',
    f'{newpkg}/contract-gate.py',
    f'{newpkg}/environment-windows.txt',
    f'{newpkg}/hosted/environment-ubuntu.txt',
    f'{newpkg}/hosted/hosted-ubuntu-derived.txt',
    f'{newpkg}/hosted/ci-run-45037630799-ubuntu-latest.log',
    f'{newpkg}/hosted/ci-run-44993021436-ubuntu-latest-baseline.log',
    f'{newpkg}/hosted/ci-artifacts-index.json',
    f'{newpkg}/hosted/derive-hosted.py',
    f'{newpkg}/dll-comparison.txt',
    f'{newpkg}/package-comparison.txt',
    f'{newpkg}/api-surface/api-diff.txt',
    f'{newpkg}/production-delta.diff',
    f'{newpkg}/case-receipts/case-summary.json',
    f'{newpkg}/tools/dllcompare/Program.cs',
    f'{newpkg}/tools/apidump/Program.cs',
    'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.md',
    'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-traceability-matrix.md',
    'docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s61-impl-01.md',
    'tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs',
]
missing = [p for p in required if blob(EVID, p) is None]
check('all required implementation/evidence artifacts exist at the evidence revision', not missing, str(missing))
emit()

emit('== 9. independent re-derivation of the implementation revision identity ==')
impl_tree_at_push = git('rev-parse', '--verify', f'{IMPL}^{{tree}}')
check('implementation tree resolves consistently', impl_tree_at_push == git('show', '-s', '--format=%T', IMPL))
impl_files = git('ls-tree', '-r', '--name-only', IMPL).splitlines()
check('the new test file is present in the pushed implementation revision',
      'tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs' in impl_files)
emit()

emit('RESULT: ' + ('ALL ENTRY CHECKS PASSED' if ok else 'SOME ENTRY CHECKS FAILED'))

target = Path(r'D:\m54b-stage6\qual-s61')
target.mkdir(parents=True, exist_ok=True)
(target / 'entry-provenance.txt').write_text('\n'.join(out) + '\n', encoding='utf-8')
print(f'\nwrote {target / "entry-provenance.txt"}')
sys.exit(0 if ok else 1)
