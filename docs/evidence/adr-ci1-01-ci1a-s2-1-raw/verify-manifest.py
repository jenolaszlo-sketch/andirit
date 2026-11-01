from pathlib import Path
import hashlib
import json
import subprocess

root = Path(__file__).resolve().parent
repo = next(p for p in root.parents if (p / 'Andirit.slnx').exists())
manifest = json.loads((root / 'source-evidence-manifest.json').read_text(encoding='utf-8'))
def git(*args):
    return subprocess.check_output(['git', *args], cwd=repo)
for group in ('SourceFiles', 'EvidenceFiles'):
    for item in manifest[group]:
        blob = git('cat-file', 'blob', item['GitBlob'])
        assert hashlib.sha256(blob).hexdigest() == item['Sha256CanonicalGitBytes'], item['Path']
        assert git('hash-object', item['Path']).decode().strip() == item['GitBlob'], item['Path']
        if group == 'SourceFiles':
            assert git('rev-parse', manifest['Implementation']['Commit'] + ':' + item['Path']).decode().strip() == item['GitBlob']
for key in ('Baseline', 'Contract', 'Implementation'):
    assert git('rev-parse', manifest[key]['Commit'] + '^{tree}').decode().strip() == manifest[key]['Tree']
print(f'PASS: {len(manifest["SourceFiles"])} source/test and {len(manifest["EvidenceFiles"])} evidence/documentation canonical Git blobs and SHA-256 values verified; baseline/contract/implementation trees match; manifest self-excluded.')
