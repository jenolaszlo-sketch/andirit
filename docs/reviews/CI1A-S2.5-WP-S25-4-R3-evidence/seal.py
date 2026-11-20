"""Seal or verify a self-excluding evidence manifest against original file bytes."""
import hashlib
import json
from pathlib import Path
import sys

root = Path(__file__).resolve().parent
manifest_path = root / 'manifest.json'
implementation = json.loads((root / 'implementation.json').read_bytes())
entries = []
for path in sorted(root.rglob('*'), key=lambda file: file.relative_to(root).as_posix()):
    if path.is_file() and path != manifest_path:
        raw = path.read_bytes()
        entries.append({'path': path.relative_to(root).as_posix(), 'bytes': len(raw), 'sha256': hashlib.sha256(raw).hexdigest()})

manifest = {'format': 1, 'algorithm': 'SHA-256', 'selfExcluding': True,
            'implementationHead': implementation['head'], 'implementationTree': implementation['tree'], 'files': entries}
if '--verify' in sys.argv:
    assert manifest == json.loads(manifest_path.read_bytes())
else:
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8', newline='\n')
print(f'{len(entries)} SHA-256 entries, manifest {hashlib.sha256(manifest_path.read_bytes()).hexdigest()}')
