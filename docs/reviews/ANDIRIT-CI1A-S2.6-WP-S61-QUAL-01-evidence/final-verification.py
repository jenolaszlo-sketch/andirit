"""Final verification sweep for ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01."""
import hashlib, json, subprocess, sys
from pathlib import Path

REPO = r'D:\projects\andirit'
ENTRY = '2d1e8ec88e397d0c36b7f258eb1a1c0840e56157'
IMPL = '01624943233f16447c3176b7c905c544642dfb2c'
EVID = '3eb1339304ff843336f6165575904350df56d041'
QPKG = 'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01-evidence'

ok = True
out = []

def emit(t=''):
    out.append(t)
    print(t.encode('ascii', 'replace').decode('ascii'))

def check(label, cond, detail=''):
    global ok
    if not cond:
        ok = False
    emit(f'[{"PASS" if cond else "FAIL"}] {label}' + (f' - {detail}' if detail else ''))

def git(*a):
    return subprocess.run(['git', '-C', REPO, *a], capture_output=True, text=True).stdout.strip()

def blob(rev, path):
    r = subprocess.run(['git', '-C', REPO, 'cat-file', 'blob', f'{rev}:{path}'], capture_output=True)
    return r.stdout if r.returncode == 0 else None

emit('ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01 - final verification sweep')
emit()

emit('== reviewer did not modify production, tests, CI or any committed record ==')
head = git('rev-parse', 'HEAD')
emit(f'  HEAD: {head}')
status = git('status', '--porcelain')
changed = [l for l in status.splitlines() if l.strip()]
emit(f'  changed paths in this commission: {len(changed)}')
for l in changed:
    emit('    ' + l)
allowed_prefixes = (
    'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01-evidence/',
    'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01.md',
)
violations = [l for l in changed if not l[3:].startswith(allowed_prefixes)]
check('every changed path is inside the qualification deliverables', not violations, str(violations))
check('no production source modified', not any('/src/' in l for l in changed))
check('no committed implementation test modified',
      not any('tests/' in l for l in changed))
check('no CI/workflow modified', not any('.github/' in l for l in changed))
emit()

emit('== production source still byte-identical to the pinned implementation revision ==')
def staged_blob(path):
    r = subprocess.run(['git', '-C', REPO, 'show', f':{path}'], capture_output=True)
    return r.stdout if r.returncode == 0 else None


for path in ('src/Andirit.LatticeDb/LatticeCodeGraphStore.cs', 'src/Andirit/ICodeGraphStore.cs',
             'tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs'):
    staged = staged_blob(path)
    pinned = blob(EVID, path)
    check(f'  {path} unchanged from the evidence revision',
          staged == pinned and pinned is not None)
emit()

emit('== the local-only evidence revision is still preserved and unpushed ==')
remote = git('ls-remote', 'origin', 'refs/heads/main').split()[0]
check('origin/main is still the pinned implementation revision', remote == IMPL, remote)
ahead = int(git('rev-list', '--count', f'{IMPL}..HEAD'))
check('local HEAD is ahead by the qualification commit(s), unpushed',
      ahead >= 1 and remote == IMPL,
      f'{ahead} commits ahead; HEAD...origin/main = '
      + git('rev-list', '--left-right', '--count', 'HEAD...origin/main'))
check('no tag changed', git('ls-remote', 'origin', 'refs/tags/v0.2.0-preview.7').split()[0]
      == '6659647527a6e0eb822a3d7a05b8283c75fd6a3e')
emit()

emit('== the S6.1 implementation evidence package is untouched by this commission ==')
manifest = json.loads(blob(head, f'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence/manifest.json').decode('utf-8'))
mismatches = []
for e in manifest['files']:
    data = blob(head, f"docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence/{e['path']}")
    if data is None or len(data) != e['bytes'] or hashlib.sha256(data).hexdigest().lower() != e['sha256'].lower():
        mismatches.append(e['path'])
check('all 61 implementation evidence entries still byte-exact at HEAD', not mismatches, str(mismatches))
emit()

emit('== the qualification evidence package ==')
manifest = json.loads(blob(head, f'{QPKG}/manifest.json').decode('utf-8'))
listed = {e['path'] for e in manifest['files']}
actual = {p[len(QPKG) + 1:] for p in git('ls-tree', '-r', '--name-only', head, QPKG).splitlines()
          if p != f'{QPKG}/manifest.json'}
mismatches = []
for e in manifest['files']:
    data = blob(head, f"{QPKG}/{e['path']}")
    if data is None or len(data) != e['bytes'] or hashlib.sha256(data).hexdigest().lower() != e['sha256'].lower():
        mismatches.append(e['path'])
check('qualification package manifest complete over the committed tree', listed == actual,
      f'{len(listed)} listed / {len(actual)} committed')
check('qualification package byte-exact against the committed blobs', not mismatches, str(mismatches))
check('manifest is self-excluding', 'manifest.json' not in listed)
emit(f'  entries: {len(listed)}  bytes: {manifest["totalBytes"]}')
emit()

emit('== disposition vocabulary ==')
record = blob(head, 'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01.md').decode('utf-8')
long = 'S6.1 \u2014 QUALIFIED WITH QUALIFICATIONS'
short = '**S6.1 \u2014 QUALIFIED**'
none_ = 'S6.1 \u2014 NOT QUALIFIED'
found = [d for d, present in (('S6.1 \u2014 QUALIFIED WITH QUALIFICATIONS', long in record),
                              ('S6.1 \u2014 QUALIFIED (standalone)', short in record),
                              ('S6.1 \u2014 NOT QUALIFIED', none_ in record)) if present]
check('exactly one authorized disposition is selected', len(found) == 1, str(found))
check('the selected disposition is QUALIFIED WITH QUALIFICATIONS',
      found == ['S6.1 \u2014 QUALIFIED WITH QUALIFICATIONS'], str(found))
check('owner acceptance is explicitly not granted', 'owner acceptance is not granted' in record.replace('**', ''))
check('S6.2 entry is not authorized', 'S6.2 entry is not authorized' in record.replace('**', ''))
check('the eight findings are recorded', all(f'F-{n}' in record for n in range(1, 9)))
check('the recommended next commission is named', 'ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01' in record)
emit()

emit('RESULT: ' + ('ALL FINAL CHECKS PASSED' if ok else 'SOME FINAL CHECKS FAILED'))
Path(r'D:\m54b-stage6\qual-s61\final-verify.txt').write_text('\n'.join(out) + '\n', encoding='utf-8')
sys.exit(0 if ok else 1)
