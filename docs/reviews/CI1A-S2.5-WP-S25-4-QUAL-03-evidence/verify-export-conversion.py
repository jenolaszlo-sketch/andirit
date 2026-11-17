import hashlib,json,pathlib,zipfile
W=pathlib.Path(__file__).resolve().parent.parent
rows=[]
for name in ['start.zip','production.zip','baseline.zip']:
    with zipfile.ZipFile(W/'checkout-archives'/name) as old, zipfile.ZipFile(W/name) as exact:
        assert old.namelist()==exact.namelist()
        changes=[]
        for n in exact.namelist():
            a,b=old.read(n),exact.read(n)
            if a!=b:
                assert a.replace(b'\r\n',b'\n')==b.replace(b'\r\n',b'\n'),(name,n)
                changes.append(dict(path=n,checkoutBytes=len(a),gitBytes=len(b),checkoutSha256=hashlib.sha256(a).hexdigest(),gitSha256=hashlib.sha256(b).hexdigest()))
        rows.append(dict(archive=name,changedFiles=len(changes),allDifferencesOnlyCRLF=True,changes=changes))
(W/'evidence/export-conversion-verification.json').write_text(json.dumps(rows,indent=2)+'\n')
print(json.dumps([{k:v for k,v in r.items() if k!='changes'} for r in rows]))
