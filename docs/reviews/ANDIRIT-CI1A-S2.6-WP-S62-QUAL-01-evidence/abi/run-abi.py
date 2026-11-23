import subprocess
from pathlib import Path

caps = Path(r'D:\m54b-stage6\qual-s62\abi\caps')
caps.mkdir(parents=True, exist_ok=True)
caps.joinpath('capabilities.json').write_text('{"features": {}}', encoding='utf-8')

root = str(Path.home() / r'.nuget\packages\latticedbsharp\0.2.0')
out = Path(r'D:\m54b-stage6\qual-s61\abi\reviewer-native-abi-scan.txt')
scanner = r'D:\projects\andirit\docs\reviews\ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence\probe\native-abi-scan.py'
result = subprocess.run(['python', scanner, root, str(caps / 'capabilities.json'), str(out)], capture_output=True, text=True)
print('exit:', result.returncode)
if result.stderr:
    print(result.stderr[-800:])
print('output written:', out.exists(), out.stat().st_size if out.exists() else 0)