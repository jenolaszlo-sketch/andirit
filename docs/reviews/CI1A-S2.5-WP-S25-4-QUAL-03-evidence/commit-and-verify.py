"""Commit only the additive QUAL-03 evidence; never fetch/push or modify protected paths."""
import hashlib,json,pathlib,shutil,subprocess,sys
R=pathlib.Path(r'D:\projects\Andirit')
W=pathlib.Path(__file__).resolve().parent.parent
START='9a550c26dcb4559a317a9663ce8fec827e01bd40'
PROD='25bc6e3b4305c7ce088deb36cfb000138dc76648'
REPORT='docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-03.md'
PACKAGE='docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-03-evidence'
def git(*args):
    p=subprocess.run(['git','--no-optional-locks','-C',str(R),*args],capture_output=True,text=True)
    assert p.returncode==0,(args,p.stdout,p.stderr)
    return p.stdout.strip()
def manifest(root):
    m=json.loads((root/'manifest.json').read_bytes())
    for f in m['files']:
        raw=(root/f['path']).read_bytes()
        assert len(raw)==f['bytes'] and hashlib.sha256(raw).hexdigest()==f['sha256'].lower(),f
    return m
def state(ahead):
    assert git('branch','--show-current')=='main'
    assert not git('status','--porcelain=v1')
    assert not git('stash','list')
    assert git('worktree','list','--porcelain').count('worktree ')==1
    assert git('rev-parse','origin/main')=='48370711a6247b972dfc83628618924e864be374'
    assert git('rev-list','--left-right','--count','origin/main...HEAD').split()==['0',str(ahead)]
def verify_legacy():
    counts={}
    for name,count in [('R2',184),('QUAL-02',245)]:
        root=R/'docs/reviews'/('CI1A-S2.5-WP-S25-4-'+name+'-evidence')
        m=manifest(root); assert len(m['files'])==count
        assert git('rev-parse',m['reviewedHead']+'^{tree}')==m['reviewedTree']
        rel=root.relative_to(R).as_posix()
        assert git('rev-parse',START+':'+rel)==git('rev-parse','HEAD:'+rel)
        counts[name]=count
    p=subprocess.run(['python',str(R/'docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence/verify-integrity.py')],capture_output=True,text=True)
    assert p.returncode==0,p.stderr
    assert sum(json.loads(x).get('entries',0) for x in p.stdout.splitlines())==381
    counts['earlier']=381
    return counts
if '--commit' in sys.argv:
    assert git('rev-parse','HEAD')==START
    assert git('rev-parse','HEAD^{tree}')=='606c17871555a3e2a41ce8eb7536befd332e97dc'
    state(8); verify_legacy()
    package=W/'evidence'; m=manifest(package)
    assert not (R/REPORT).exists() and not (R/PACKAGE).exists()
    shutil.copy2(W/pathlib.Path(REPORT).name,R/REPORT)
    shutil.copytree(package,R/PACKAGE)
    subprocess.run(['python',str(R/PACKAGE/'seal.py'),'--verify',str(R/PACKAGE)],check=True)
    expected=sorted([REPORT,PACKAGE+'/manifest.json',*[PACKAGE+'/'+x['path'] for x in m['files']]])
    git('add','--',REPORT,PACKAGE)
    actual=git('diff','--cached','--name-status','--no-renames').splitlines()
    assert sorted(actual)==['A\t'+n for n in expected],actual
    print(git('commit','-m','docs(review): independently qualify R2 and assess M5.4 production reachability (ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-03)'))
head=git('rev-parse','HEAD'); tree=git('rev-parse','HEAD^{tree}')
assert git('rev-parse','HEAD^')==START
assert git('rev-parse',PROD+'^{tree}')=='2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3'
state(9)
counts=verify_legacy()
subprocess.run(['python',str(R/PACKAGE/'seal.py'),'--verify',str(R/PACKAGE)],check=True)
m=manifest(R/PACKAGE)
expected=sorted([REPORT,PACKAGE+'/manifest.json',*[PACKAGE+'/'+x['path'] for x in m['files']]])
changes=git('diff','--name-status','--no-renames',START,head).splitlines()
assert sorted(changes)==['A\t'+n for n in expected]
assert not git('diff','--name-only',START,head,'--','.',':(exclude)'+REPORT,':(exclude)'+PACKAGE+'/**')
batch=subprocess.run(['git','-C',str(R),'cat-file','--batch'],input=''.join(head+':'+n+'\n' for n in expected).encode(),capture_output=True,check=True).stdout
offset=0
for name in expected:
    end=batch.index(b'\n',offset)
    object_id,kind,size=batch[offset:end].split()
    assert kind==b'blob'
    count=int(size); offset=end+1
    assert batch[offset:offset+count]==(R/name).read_bytes(),('Committed byte mismatch',name)
    offset+=count+1
assert offset==len(batch)
receipt=dict(startingHead=START,startingTree='606c17871555a3e2a41ce8eb7536befd332e97dc',finalHead=head,finalTree=tree,reviewedHead=PROD,reviewedTree='2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3',parentVerified=True,changes=changes,legacyEntries=counts,newManifestEntries=len(m['files']),newManifestSha256=hashlib.sha256((R/PACKAGE/'manifest.json').read_bytes()).hexdigest(),protectedPathsUnchanged=True,clean=True,worktrees=1,stashes=0,ahead=9,behind=0,pushed=False)
receipt['allNewCommittedBlobBytesMatch']=True
receipt['startingTree']=git('rev-parse',START+'^{tree}')
print(json.dumps(receipt,indent=2))
if '--output' in sys.argv:
    pathlib.Path(sys.argv[sys.argv.index('--output')+1]).write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
