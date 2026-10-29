import hashlib
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
mode = sys.argv[1] if len(sys.argv) > 1 else 'verify'
if mode == 'write':
    lines = []
    for path in sorted(root.rglob('*')):
        if path.is_file() and path.name != 'SHA256SUMS' and '__pycache__' not in path.parts:
            lines.append(f'{hashlib.sha256(path.read_bytes()).hexdigest()}  {path.relative_to(root).as_posix()}')
    (root / 'SHA256SUMS').write_text('\n'.join(lines) + '\n', encoding='utf-8')
else:
    count = 0
    for line in (root / 'SHA256SUMS').read_text().splitlines():
        digest, relative = line.split('  ', 1)
        assert hashlib.sha256((root / relative).read_bytes()).hexdigest() == digest, relative
        count += 1
    print(f'{count} evidence hashes verified')
errors = []
for path in root.glob('*.md'):
    for target in re.findall(r'\]\(([^)]+)\)', path.read_text(encoding='utf-8')):
        if '://' in target or target.startswith('#'):
            continue
        resolved = (path.parent / target.split('#')[0]).resolve()
        if not resolved.exists():
            errors.append(f'{path.name}: {target}')
assert not errors, errors
print('Integration Markdown relative links verified')
