"""Deterministic R1 evidence manifest: sorted relative path, byte length, SHA-256.

The manifest excludes itself. Raw R1 process/control reports and logs are sealed
exactly as produced; nothing is normalized or rewritten.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence'


def main():
    files = []
    for path in sorted(p for p in OUT.rglob('*') if p.is_file()):
        relative = path.relative_to(OUT).as_posix()
        if relative in ('manifest.json', '.gitignore', '.gitattributes'):
            continue
        data = path.read_bytes()
        files.append({'path': relative, 'bytes': len(data),
                      'sha256': hashlib.sha256(data).hexdigest().upper()})
    manifest = {'root': 'docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence',
                'file_count': len(files), 'files': files}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print('sealed ' + str(len(files)) + ' files')


if __name__ == '__main__':
    main()
