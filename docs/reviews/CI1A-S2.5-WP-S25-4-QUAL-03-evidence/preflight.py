import hashlib, json, os, pathlib, subprocess
R=pathlib.Path(r'D:\projects\Andirit')
E=pathlib.Path(__file__).resolve().parent
START='9a550c26dcb4559a317a9663ce8fec827e01bd40'
PROD='25bc6e3b4305c7ce088deb36cfb000138dc76648'
def git(*a):
    return subprocess.check_output(['git','--no-optional-locks','-C',str(R),*a]).decode().strip()
def sha(b): return hashlib.sha256(b).hexdigest()
record={'reviewerSession':os.environ.get('CODEX_SESSION_ID'),'reviewerThread':os.environ.get('CODEX_THREAD_ID'),
        'R2ProducerSession':'01a1e288-3681-7a82-8b58-298834f17a05','R1ProducerSession':'01a1e008-79e0-7263-b968-0368dbf25f28',
        'independence':'Fresh reviewer session with no role in R2 implementation, verification or evidence construction. Independent expectations from contracts and fixtures; inherited harness replays labeled. Organizational independence unestablished.'}
assert record['reviewerSession']=='01a1e38c-27d1-7491-a12c-63e0eecb704f'
assert git('rev-parse','HEAD')==START
assert git('rev-parse','HEAD^{tree}')=='606c17871555a3e2a41ce8eb7536befd332e97dc'
assert git('branch','--show-current')=='main' and not git('status','--porcelain=v1')
assert not git('stash','list')
assert git('worktree','list','--porcelain').count('worktree ')==1
assert git('rev-list','--left-right','--count','origin/main...HEAD').split()==['0','8']
assert git('rev-parse','origin/main')=='48370711a6247b972dfc83628618924e864be374'
record.update(head=START,tree=git('rev-parse','HEAD^{tree}'),reviewed=PROD,reviewedTree=git('rev-parse',PROD+'^{tree}'),ancestry=git('log','-9','--format=%H %T %P %s'))
assert record['reviewedTree']=='2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3'
record['implementationFiles']=git('diff','--name-status','6d4d45f7aedc85f4b52310cf9a3fa6082f885296',PROD)
for commit in ['786574e34de0e5e1b242a784a5dc7297e0162bb1',PROD]:
    (E/(commit[:7]+'.diff')).write_text(git('show','--format=fuller',commit),encoding='utf-8')
(E/'preflight.json').write_text(json.dumps(record,indent=2)+'\n')
receipts=[]
for name,count in [('R2',184),('QUAL-02',245)]:
    root=R/'docs/reviews'/('CI1A-S2.5-WP-S25-4-'+name+'-evidence')
    m=json.loads((root/'manifest.json').read_bytes())
    assert len(m['files'])==count
    expected_head=PROD if name=='R2' else 'afa136f4033f35d9a657d8c5f1d5db92d736b679'
    assert m['reviewedHead']==expected_head
    assert git('rev-parse',expected_head+'^{tree}')==m['reviewedTree']
    git('merge-base','--is-ancestor',expected_head,START)
    rel=root.relative_to(R).as_posix()
    seal_commit=START if name=='R2' else '6d4d45f7aedc85f4b52310cf9a3fa6082f885296'
    assert git('rev-parse',seal_commit+':'+rel)==git('rev-parse',START+':'+rel)
    assert {p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}=={f['path'] for f in m['files']}|{'manifest.json'}
    for f in m['files']:
        raw=(root/f['path']).read_bytes()
        assert len(raw)==f['bytes'] and sha(raw)==f['sha256'].lower(),f
    receipts.append(dict(package=name,entries=count,manifestSha256=sha((root/'manifest.json').read_bytes()),reviewedHead=m['reviewedHead'],reviewedTree=m['reviewedTree']))
historical=subprocess.run(['python',str(R/'docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence/verify-integrity.py')],capture_output=True,text=True)
(E/'historical-integrity.jsonl').write_text(historical.stdout,encoding='utf-8')
assert historical.returncode==0,historical.stderr
assert sum(json.loads(s).get('entries',0) for s in historical.stdout.splitlines())==381
(E/'integrity.json').write_text(json.dumps(receipts,indent=2)+'\n')
print(json.dumps({'preflight':'PASS','entries':184+245+381,'reviewer':record['reviewerSession']}))
