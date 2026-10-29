"""Read-only Git/protected-file audit; run from the Q2 worktree."""
import hashlib, json, os, pathlib, subprocess, sys
ROOT = pathlib.Path.cwd()
OUT = ROOT / 'docs/reviews/ec6d-scope-q2-evidence'
ENV = dict(os.environ, GIT_OPTIONAL_LOCKS='0')
def git(repo, *args):
    p = subprocess.run(['git', '-C', str(repo), *args], capture_output=True, env=ENV)
    return {'command': list(args), 'exit': p.returncode, 'stdout': p.stdout.decode('utf-8', 'replace').strip(), 'stderr': p.stderr.decode('utf-8', 'replace').strip()}
def protected(repo):
    listing = git(repo, 'ls-files', '-z')
    if listing['exit']: return {'error': listing}
    hashes = {}
    for name in listing['stdout'].split('\0'):
        if not name: continue
        path = repo / name
        hashes[name] = hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else 'MISSING'
    return {'head': git(repo, 'rev-parse', 'HEAD'), 'branch': git(repo, 'branch', '--show-current'),
            'commonDir': git(repo, 'rev-parse', '--path-format=absolute', '--git-common-dir'),
            'status': git(repo, 'status', '--porcelain=v1', '--untracked-files=all'), 'trackedSha256': hashes}
repos = [ROOT.parent/'hetu-ec6d-consumer-intelligence', ROOT.parent/'hetu-ec6d-plan-r1', ROOT.parent.parent/'Penghou.Hetu', ROOT.parent.parent/'Penghou.Hetu.review-ec6d-scope-q1']
phase = sys.argv[1]
result = {'protected': {str(p): protected(p) for p in repos}}
if phase == 'before':
    commits = ['558f8ac82ac1b5e07da68f4a71a70240eaf38409','2c3090cba6d8a56102a5c47ceb026c5b5fea3234','91a3ed53a256e89ed71d7d11c204252c417400a6','0e1b213737d42c74df20aef4744ab5718058e266','d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc']
    commands = [['show','-s','--format=%H%n%T%n%P',c] for c in commits]
    commands += [['merge-base','--is-ancestor',b,t] for b in commits[:2] for t in commits[2:4]]
    commands += [['fsck','--full'],['diff','--name-status',commits[1],commits[2]],['diff','--name-status',commits[2],commits[3]],['diff',commits[1],commits[3],'--','src','tests'],['ls-tree','-r',commits[2],'docs/planning/ec6d'],['ls-tree',commits[2]+':docs/planning/ec6d/corpus/ts-interface-impact'],['rev-parse',commits[2]+':docs/planning/ec6d/corpus/ts-interface-impact'],['worktree','list','--porcelain'],['remote','-v'],['config','--get','core.autocrlf']]
    result['mirrorChecks'] = [git(ROOT,*c) for c in commands]
    result['canonicalChecks'] = [git(repos[2],*c) for c in [['cat-file','-t',commits[2]],['cat-file','-t','d79e5814485040a36c77bcde332ebc937f312476'],['rev-parse','refs/heads/main','refs/remotes/origin/main'],['remote','-v'],['ls-remote','origin','refs/heads/planning/*ec6d*']]]
    result['alternates'] = {str(p): ((p/'objects/info/alternates').read_text() if (p/'objects/info/alternates').exists() else None) for p in [ROOT.parent/'.hetu-ec6d-planning-mirror.git',repos[2]/'.git']}
    result['q1Evidence'] = git(ROOT,'ls-tree','-r',commits[4],'docs/reviews/ec6d-scope-q1-evidence','docs/reviews/ec6d-scope-q1-independent-review.md')
else:
    before = json.loads((OUT/'git-before.json').read_text())
    result['protectedUnchanged'] = result['protected'] == before['protected']
    result['reviewChanges'] = git(ROOT,'diff','--name-status','0e1b213','HEAD')
    result['productionAndPlanningDiff'] = git(ROOT,'diff','0e1b213','--','src','tests','docs/planning/ec6d')
(OUT/f'git-{phase}.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ['protected','mirrorChecks']},indent=2))
