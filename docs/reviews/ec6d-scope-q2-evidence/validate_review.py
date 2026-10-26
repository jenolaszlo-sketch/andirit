"""Validate only Q2 review artifacts and read immutable Git source identities."""
import hashlib, json, pathlib, re, subprocess, urllib.parse
root = pathlib.Path.cwd()
evidence = root/'docs/reviews/ec6d-scope-q2-evidence'
report = root/'docs/reviews/ec6d-scope-q2-independent-verification.md'
output = evidence/'validation.json'
if not output.exists(): output.write_text('{}\n')
def git(*args):
    p = subprocess.run(['git',*args],capture_output=True,text=True,encoding='utf-8')
    return {'command':list(args),'exit':p.returncode,'stdout':p.stdout.strip(),'stderr':p.stderr.strip()}
source = git('show','-s','--format=%H%n%T%n%P','0e1b213')
assert source['stdout'].splitlines() == ['0e1b213737d42c74df20aef4744ab5718058e266','ddf777aa155852daca40b6ed8748c028c67d5822','91a3ed53a256e89ed71d7d11c204252c417400a6']
protected = json.loads((evidence/'git-after.json').read_text())
assert protected['protectedUnchanged']
assert protected['productionAndPlanningDiff']['stdout']==''
surface = git('diff','--name-only','0e1b213')
untracked = git('ls-files','--others','--exclude-standard')
allowed = lambda p: p=='docs/reviews/ec6d-scope-q2-independent-verification.md' or p.startswith('docs/reviews/ec6d-scope-q2-evidence/')
assert all(allowed(p) for p in (surface['stdout']+'\n'+untracked['stdout']).splitlines() if p)
unchanged = git('diff','0e1b213','--','src','tests','docs/planning/ec6d')
assert unchanged['exit']==0 and unchanged['stdout']==''
prior_raw = git('diff','6e34452','--','docs/reviews/ec6d-scope-q2-evidence/fixtures','docs/reviews/ec6d-scope-q2-evidence/runs','docs/reviews/ec6d-scope-q2-evidence/tools')
assert prior_raw['exit']==0 and prior_raw['stdout']==''
checks = [git('diff','--check'),git('diff','--cached','--check')]
assert all(c['exit']==0 for c in checks)
links=[]
markdown=[report,*evidence.rglob('*.md'),*(root/'docs/planning/ec6d').glob('*.md')]
for file in markdown:
    if any(p.lower() in ('bin','obj') for p in file.parts): continue
    contents=file.read_text(encoding='utf-8-sig')
    for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)',contents):
        target=target.strip('<>')
        if re.match(r'^[a-zA-Z]+:',target) or target.startswith('#'):continue
        dest=urllib.parse.unquote(target.split('#')[0])
        resolved=(file.parent/dest).resolve()
        links.append({'from':file.relative_to(root).as_posix(),'target':target,'exists':resolved.exists()})
assert all(x['exists'] for x in links),[x for x in links if not x['exists']]
delta=git('diff','--name-only','558f8ac','2c3090c')['stdout'].splitlines()
groups={'source':sum(p.startswith('src/') for p in delta),'tests':sum(p.startswith('tests/') for p in delta),'docs':sum(p.startswith('docs/') for p in delta),'root':sum('/' not in p for p in delta)}
assert len(delta)==65 and groups=={'source':12,'tests':12,'docs':38,'root':3}
hashes={}
for file in [report,*evidence.rglob('*')]:
    if not file.is_file() or file==output or any(p.lower() in ('bin','obj','__pycache__') for p in file.parts):continue
    hashes[file.relative_to(root).as_posix()]=hashlib.sha256(file.read_bytes()).hexdigest()
result={'reviewedSource':source,'preCommitHead':git('show','-s','--format=%H%n%T%n%P','HEAD'),'protectedUnchanged':True,'productionPlanningDiff':unchanged,'priorCommittedRawEvidenceUnchanged':prior_raw,'allowedChangeSurface':True,'whitespace':checks,'localLinks':links,'integrationDelta':{'total':len(delta),'groups':groups},'artifactWorkingByteSha256':hashes,'hashScope':'Hashes captured before the final binding-only follow-up; excludes validation.json to avoid self-reference. Git commit trees bind normalized blobs.'}
output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'sourceVerified':True,'protectedUnchanged':True,'allowedSurface':True,'localLinks':len(links),'artifactsHashed':len(hashes),'integrationDelta':groups,'whitespace':'passed'},indent=2))
