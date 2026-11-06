"""Deterministic Q2 evidence manifest and integrity receipts.

The manifest excludes itself, .gitignore, and .gitattributes. Integrity receipts
record the reviewed commits/trees, the production change scope, protected-path
conservation, and the frozen oracle digest. Nothing is normalized or rewritten.
"""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q2-evidence'

REVIEWED_PRODUCTION = '1bce70ee2f52ed5100a2e68bc004462f9824d2f2'
HANDOFF_COMMIT = '0bc509164363013899fa5ddbf825dd7086c9c5df'
Q1_TREE = '53966a01b69c876bfff29a60c576c595bdb9e84b'


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT).decode().strip()


def blob(path, rev='HEAD'):
    return hashlib.sha256(subprocess.check_output(['git', 'show', f'{rev}:{path}'], cwd=ROOT)).hexdigest().upper()


def main():
    integrity = {
        'reviewed_production_commit': REVIEWED_PRODUCTION,
        'reviewed_production_tree': git('show', '--no-patch', '--format=%T', REVIEWED_PRODUCTION),
        'reviewed_handoff_commit': HANDOFF_COMMIT,
        'reviewed_handoff_tree': git('show', '--no-patch', '--format=%T', HANDOFF_COMMIT),
        'q1_reviewed_tree': Q1_TREE,
        'production_change_scope': git('diff', '--name-only', 'd94052d', REVIEWED_PRODUCTION, '--', 'src').splitlines(),
        'production_changes_after_remediation': git('diff', '--name-only', REVIEWED_PRODUCTION, HANDOFF_COMMIT, '--', 'src').splitlines(),
        'production_blob_at_remediation': blob('src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs', REVIEWED_PRODUCTION),
        'production_blob_before': blob('src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs', 'd94052d'),
        'review_modified_production_or_tests': False,
        'review_worktree_changes': git('status', '--porcelain=v1', '--', 'src', 'tests').splitlines(),
        'q1_report_blob': blob('docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-Q1.md'),
        'r1_report_blob': blob('docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-R1.md'),
        'producer_q2_handoff_blob': blob('docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-Q2.md'),
        'frozen_oracle_output_bytes': (OUT / 'oracle-r2.json').stat().st_size,
        'frozen_oracle_output_sha256': hashlib.sha256((OUT / 'oracle-r2.json').read_bytes()).hexdigest().upper(),
        'expected_observed_oracle_sha256': '97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9',
    }
    (OUT / 'q2-integrity.json').write_text(json.dumps(integrity, indent=2) + '\n', encoding='utf-8')

    files = []
    for path in sorted(p for p in OUT.rglob('*') if p.is_file()):
        relative = path.relative_to(OUT).as_posix()
        if relative in ('manifest.json', '.gitignore', '.gitattributes'):
            continue
        data = path.read_bytes()
        files.append({'path': relative, 'bytes': len(data),
                      'sha256': hashlib.sha256(data).hexdigest().upper()})
    manifest = {'root': 'docs/reviews/CI1A-S2.3-WP-S23-2-Q2-evidence',
                'file_count': len(files), 'files': files}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print('sealed ' + str(len(files)) + ' files; integrity written')


if __name__ == '__main__':
    main()
