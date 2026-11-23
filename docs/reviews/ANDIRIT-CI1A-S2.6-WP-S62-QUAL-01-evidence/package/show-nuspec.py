import zipfile, hashlib
from pathlib import Path

base = zipfile.ZipFile(r'D:\m54b-stage6\qual-s61\pkg\pkg-base\Andirit.LatticeDb.0.2.0-preview.7.nupkg')
impl = zipfile.ZipFile(r'D:\m54b-stage6\qual-s61\pkg\pkg-impl\Andirit.LatticeDb.0.2.0-preview.7.nupkg')
b = base.read('Andirit.LatticeDb.nuspec')
i = impl.read('Andirit.LatticeDb.nuspec')
print('baseline bytes', len(b), hashlib.sha256(b).hexdigest())
print('impl     bytes', len(i), hashlib.sha256(i).hexdigest())
bl = b.decode('utf-8').splitlines()
il = i.decode('utf-8').splitlines()
print('--- line-by-line diff ---')
for n in range(max(len(bl), len(il))):
    x = bl[n] if n < len(bl) else '<missing>'
    y = il[n] if n < len(il) else '<missing>'
    if x != y:
        print(f'{n+1}  BASE : {x}')
        print(f'{n+1}  IMPL : {y}')