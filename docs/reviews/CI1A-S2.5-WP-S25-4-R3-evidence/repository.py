import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

WORK = Path(__file__).resolve().parent
ROOT = Path(r'D:\projects\Andirit')
START = '495e73a0f24413d2dc8ce5e75e8dd3bf58833228'
R2 = '25bc6e3b4305c7ce088deb36cfb000138dc76648'
REMOTE = '48370711a6247b972dfc83628618924e864be374'
PREFIX = 'docs/reviews/CI1A-S2.5-WP-S25-4-'
PACKAGE = PREFIX + 'R3-evidence'
REPORT = 'docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-R3.md'
FILES = sorted([
    'src/Andirit/CodeOccurrenceAuthority.cs',
    'src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs',
    'src/Andirit.CSharp/VerifiedCSharpOccurrenceAuthority.cs',
    'src/Andirit.LatticeDb/LatticeCodeGraphStore.cs',
    'tests/Andirit.CSharp.Tests/S23PublicationIntegrationTests.cs',
])


def git(*args):
    return subprocess.check_output(['git', '--no-optional-locks', *args], cwd=ROOT)


def txt(*args):
    return git(*args).decode().strip()


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def dump(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8', newline='\n')


def state(ahead):
    assert txt('branch', '--show-current') == 'main'
    assert not txt('status', '--porcelain=v1')
    assert txt('worktree', 'list', '--porcelain').count('worktree ') == 1
    assert not txt('stash', 'list')
    assert txt('rev-parse', 'origin/main') == REMOTE
    assert txt('rev-list', '--left-right', '--count', 'origin/main...HEAD').split() == ['0', str(ahead)]


def history(head):
    older = PREFIX + 'QUAL-02-evidence'
    counts = {}
    for name, count, seal in [('R2', 184, '9a550c26dcb4559a317a9663ce8fec827e01bd40'),
                              ('QUAL-02', 245, '6d4d45f7aedc85f4b52310cf9a3fa6082f885296'),
                              ('QUAL-03', 535, START)]:
        package = PREFIX + name + '-evidence'
        root = ROOT / package
        manifest = json.loads((root / 'manifest.json').read_bytes())
        assert len(manifest['files']) == count
        assert txt('rev-parse', manifest['reviewedHead'] + '^{tree}') == manifest['reviewedTree']
        assert txt('rev-parse', f'{seal}:{package}') == txt('rev-parse', f'{head}:{package}')
        assert {p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()} == {
            entry['path'] for entry in manifest['files']} | {'manifest.json'}
        for item in manifest['files']:
            raw = (root / item['path']).read_bytes()
            assert len(raw) == item['bytes'] and sha(raw) == item['sha256'].lower(), item
        counts[name] = dict(entries=count, manifestSha256=sha((root / 'manifest.json').read_bytes()),
                            reviewedHead=manifest['reviewedHead'], reviewedTree=manifest['reviewedTree'])
    prior = subprocess.check_output([sys.executable, ROOT / older / 'verify-integrity.py'], cwd=ROOT).decode()
    earlier = [json.loads(line) for line in prior.splitlines()]
    assert sum(row.get('entries', 0) for row in earlier) == 381
    return dict(packages=counts, earlier=earlier, entries=1345)


def staged_files(action, files):
    assert sorted(txt('diff', '--cached', '--name-status', '--no-renames').splitlines()) == [
        action + '\t' + file for file in sorted(files)]
    for file in files:
        assert git('show', ':' + file) == (ROOT / file).read_bytes(), ('staged blob bytes', file)


def identity(revision):
    return dict(head=txt('rev-parse', revision), tree=txt('rev-parse', revision + '^{tree}'))


def verify_manifest(root):
    manifest = json.loads((root / 'manifest.json').read_bytes())
    assert {item['path'] for item in manifest['files']} | {'manifest.json'} == {
        p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}
    for item in manifest['files']:
        raw = (root / item['path']).read_bytes()
        assert item['bytes'] == len(raw) and item['sha256'] == sha(raw)
    return manifest


assert identity(START)['tree'] == '804bdf4aa7fdedc9ca7c30d6dac0caf9d174592d'
assert identity(R2)['tree'] == '2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3'
assert txt('rev-parse', START + '~2') == R2
assert txt('rev-parse', START + '~9') == REMOTE
if sys.argv[1] == 'implementation':
    assert txt('rev-parse', 'HEAD') == START
    state(9)
    preflight = dict(start=identity(START), reviewed=identity(R2), recordedRemote=identity(REMOTE),
                     clean=True, worktrees=1, stashes=0, ahead=9, behind=0,
                     ancestry=txt('log', '--format=%H %T %P %s', REMOTE + '..' + START).splitlines(),
                     evidence=history(START))
    changed = []
    with zipfile.ZipFile(WORK / 'r2.zip') as archive:
        for name in archive.namelist():
            if not name.endswith('/') and (WORK / 'production' / name).read_bytes() != archive.read(name):
                changed.append(name)
    assert sorted(changed) == FILES, changed
    dump(WORK / 'evidence' / 'preflight.json', preflight)
    for file in FILES:
        shutil.copyfile(WORK / 'production' / file, ROOT / file)
    git('-c', 'core.autocrlf=false', 'add', '--', *FILES)
    staged_files('M', FILES)
    git('diff', '--cached', '--check')
    print(txt('commit', '-m', 'fix(analysis): correct complete-scope membership accounting (Q03-F-01)'))
    state(10)
    assert txt('rev-parse', 'HEAD^') == START
    result = dict(**identity('HEAD'), changes=txt('diff', '--name-status', START, 'HEAD').splitlines(),
                  exactTestedBytes={file: sha(git('show', 'HEAD:' + file)) for file in FILES})
    for file in FILES:
        assert git('show', 'HEAD:' + file) == (WORK / 'production' / file).read_bytes()
    dump(WORK / 'evidence' / 'implementation.json', result)
    print(json.dumps(result, indent=2))
else:
    implementation = json.loads((WORK / 'evidence' / 'implementation.json').read_bytes())
    assert txt('rev-parse', implementation['head'] + '^') == START
    assert txt('rev-parse', implementation['head'] + '^{tree}') == implementation['tree']
    expected_impl = ['M\t' + file for file in FILES]
    assert sorted(txt('diff', '--name-status', START, implementation['head']).splitlines()) == expected_impl
    if sys.argv[1] == 'evidence':
        state(10)
        assert txt('rev-parse', 'HEAD') == implementation['head']
        manifest = verify_manifest(WORK / 'evidence')
        assert not (ROOT / PACKAGE).exists() and not (ROOT / REPORT).exists()
        shutil.copytree(WORK / 'evidence', ROOT / PACKAGE)
        shutil.copyfile(WORK / Path(REPORT).name, ROOT / REPORT)
        paths = [REPORT, *(PACKAGE + '/' + x['path'] for x in manifest['files']), PACKAGE + '/manifest.json']
        git('-c', 'core.autocrlf=false', 'add', '--force', '--', *paths)
        staged_files('A', paths)
        print(txt('commit', '-m', 'docs(review): record membership remediation and M5.4 investigation (ANDIRIT-CI1A-S2.5-WP-S25-4-R3)'))
    manifest = verify_manifest(ROOT / PACKAGE)
    state(11)
    final = identity('HEAD')
    assert txt('rev-parse', 'HEAD^') == implementation['head']
    paths = [REPORT, *(PACKAGE + '/' + x['path'] for x in manifest['files']), PACKAGE + '/manifest.json']
    assert sorted(txt('diff', '--name-status', implementation['head'], final['head']).splitlines()) == ['A\t' + p for p in sorted(paths)]
    for file in paths:
        assert git('show', 'HEAD:' + file) == (ROOT / file).read_bytes()
    result = dict(start=identity(START), reviewed=identity(R2), implementation=implementation, final=final,
                  ancestry=txt('log', '--format=%H %T %P %s', START + '..HEAD').splitlines(),
                  changes=txt('diff', '--name-status', START, 'HEAD').splitlines(),
                  oldManifests=history(final['head']), newManifestEntries=len(manifest['files']),
                  newManifestSha256=sha((ROOT / PACKAGE / 'manifest.json').read_bytes()),
                  exactCommittedEvidenceBytes=True, protectedPathsUnchanged=True,
                  clean=True, worktrees=1, stashes=0, recordedRemote=REMOTE, ahead=11, behind=0, fetched=False, pushed=False)
    dump(WORK / 'post-commit.json', result)
    print({k: result[k] for k in ['implementation', 'final', 'newManifestEntries', 'newManifestSha256', 'ahead', 'behind']})
