import hashlib,json,pathlib,subprocess,zipfile
W=pathlib.Path(__file__).resolve().parent.parent
repo=r'D:\projects\Andirit'
receipts=[]
for archive,rev in [('start.zip','9a550c26dcb4559a317a9663ce8fec827e01bd40'),('production.zip','25bc6e3b4305c7ce088deb36cfb000138dc76648'),('baseline.zip','afa136f4033f35d9a657d8c5f1d5db92d736b679')]:
    tree=subprocess.check_output(['git','-C',repo,'ls-tree','-rz',rev])
    entries={}
    for record in tree.split(b'\0'):
        if not record: continue
        meta,path=record.split(b'\t'); mode,kind,oid=meta.split()
        assert kind==b'blob'
        entries[path.decode()]=oid.decode()
    with zipfile.ZipFile(W/archive) as z:
        names={n for n in z.namelist() if not n.endswith('/')}
        assert names==set(entries),(archive,len(names),len(entries))
        for n in names:
            b=z.read(n)
            assert hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()==entries[n],(archive,n)
    receipts.append(dict(archive=archive,revision=rev,archiveSha256=hashlib.sha256((W/archive).read_bytes()).hexdigest(),gitBlobCount=len(entries),exactInventoryAndGitBlobBytes=True))
(W/'evidence/archive-verification.json').write_text(json.dumps(receipts,indent=2)+'\n')
print(json.dumps(receipts))
