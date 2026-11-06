"""Seal additive review evidence; does not stage or commit repository files."""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q1-evidence'
REVIEWED = '0f124eae8ba70bab3a6eaf4c2cd1e049f3435a51'


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)


def save(name, data):
    (EVIDENCE / name).write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')


assert git('rev-parse', 'HEAD').decode().strip() == REVIEWED
assert not git('diff', '--name-only').strip()
assert not git('diff', '--cached', '--name-only').strip()
worktrees = git('worktree', 'list', '--porcelain').decode()
assert worktrees.count('worktree ') == 1
assert git('branch', '--show-current').decode().strip() == 'main'
frozen = []
for path in ['docs/evidence/adr-ci1-01-ci1a-s2-1-raw/authority/CompilationProvenance.cs',
             'docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json']:
    blob = git('show', REVIEWED + ':' + path)
    current = (ROOT / path).read_bytes()
    frozen.append({'path': path, 'git_blob': git('rev-parse', REVIEWED + ':' + path).decode().strip(),
                   'bytes': len(blob), 'git_sha256': hashlib.sha256(blob).hexdigest().upper(),
                   'working_sha256': hashlib.sha256(current).hexdigest().upper(), 'exact_equal': blob == current})
assert all(item['exact_equal'] for item in frozen)
save('final-state-before-commit.json', {
    'reviewed_and_final_source_head': REVIEWED,
    'tree': git('rev-parse', 'HEAD^{tree}').decode().strip(),
    'branch': 'main', 'worktrees': worktrees,
    'status': git('status', '--porcelain=v1').decode(),
    'tracked_working_changes': [], 'preexisting_staged_changes': [],
    'frozen_inputs': frozen,
    'final_review_commit_and_tree': 'Reported in external completion handoff; this file precedes that additive commit.'
})
sources = []
for path in sorted([ROOT / 'docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-Q1.md',
                    *Path(__file__).resolve().parent.glob('*.cs'),
                    *Path(__file__).resolve().parent.glob('*.csproj'),
                    *Path(__file__).resolve().parent.glob('*.py')]):
    raw = path.read_bytes()
    sources.append({'path': path.relative_to(ROOT).as_posix(), 'bytes': len(raw),
                    'sha256': hashlib.sha256(raw).hexdigest().upper()})
save('reviewer-source-binding.json', sources)
entries = []
for path in sorted(EVIDENCE.rglob('*')):
    if not path.is_file() or path.name == 'manifest.json':
        continue
    raw = path.read_bytes()
    entries.append({'path': path.relative_to(EVIDENCE).as_posix(), 'bytes': len(raw),
                    'sha256': hashlib.sha256(raw).hexdigest().upper()})
save('manifest.json', {'scheme': 'SHA-256 over exact bytes; sorted relative paths; self-excluded; report/source bound by Git',
                       'files': entries})
print(f'SEALED {len(entries)} evidence entries and {len(sources)} reviewer source bindings')
