import hashlib
import json
from pathlib import Path
import subprocess
import zipfile

work = Path(__file__).resolve().parent
revision = '25bc6e3b4305c7ce088deb36cfb000138dc76648'
tree = subprocess.check_output(['git', 'ls-tree', '-rz', revision])
entries = {}
for record in tree.split(b'\0'):
    if not record:
        continue
    header, path = record.split(b'\t')
    _, kind, oid = header.split()
    assert kind == b'blob'
    entries[path.decode()] = oid.decode()

with zipfile.ZipFile(work / 'r2.zip') as archive:
    assert {name for name in archive.namelist() if not name.endswith('/')} == set(entries)
    for name, oid in entries.items():
        raw = archive.read(name)
        assert hashlib.sha1(f'blob {len(raw)}\0'.encode() + raw).hexdigest() == oid, name
        assert raw == (work / 'production' / name).read_bytes(), name

receipt = {'revision': revision, 'tree': '2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3', 'exactBlobsVerified': len(entries)}
(work / 'baseline-receipts' / 'archive.json').write_text(json.dumps(receipt, indent=2) + '\n')
print(receipt)
