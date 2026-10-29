"""Focused documentation/provenance checks; never executes product or oracle code.

Run from the acceptance worktree: python docs/planning/ec6d/analysis/owner-01-validate.py
Optional --output writes a JSON evidence report; --contracts pins the corrected commit.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[4]
Q3 = 'f3de16ffd04015f345fcdd65e8ca996fdb19555d'
R2 = '3ce7dd93c105e7e7375a04edb083be3a5d97f672'
R1 = '0e1b213737d42c74df20aef4744ab5718058e266'
PREFIX = 'docs/planning/ec6d/'
checks = []
commands = []


def git(*args):
    proc = subprocess.run(['git', *args], cwd=ROOT, capture_output=True)
    commands.append({'command': ['git', *args], 'exit': proc.returncode})
    if proc.returncode:
        raise RuntimeError(proc.stderr.decode('utf-8', errors='replace'))
    return proc.stdout


def check(name, condition, detail=None):
    if not condition:
        raise AssertionError(name)
    checks.append({'check': name, 'result': 'PASS', 'detail': detail})


def old(name):
    return git('show', f'{Q3}:{PREFIX}{name}').decode().replace('\r\n', '\n')


def current(name):
    return (ROOT / PREFIX / name).read_text(encoding='utf-8')


def between(text, start, end):
    return text.split(start, 1)[1].split(end, 1)[0]


parser = argparse.ArgumentParser()
parser.add_argument('--output')
parser.add_argument('--contracts')
args = parser.parse_args()
identities = {
    Q3: ('77b5eacf2fdfea8be9f41be10da11c074be566ba', '9ec3d7f4306cffa33858bcb49a798945f81dd044'),
    '9ec3d7f4306cffa33858bcb49a798945f81dd044': ('4e4de139b80bd4bef40579f4f2b1edd5ebd8694e', R2),
    R2: ('a10b91d10523515de9c038b9fa2e8cc51000c19d', R1),
    R1: ('ddf777aa155852daca40b6ed8748c028c67d5822', '91a3ed53a256e89ed71d7d11c204252c417400a6'),
    '91a3ed53a256e89ed71d7d11c204252c417400a6': ('d79e5814485040a36c77bcde332ebc937f312476', '3b5e8d91e89e082fd236452656c1d66a4afd3662'),
    '2c3090cba6d8a56102a5c47ceb026c5b5fea3234': ('ece772f1f2b2497891f4019d0d85f4d852b197e1', '5e79418f8b3864e8b829a4be10ab62c6b5744d89'),
    '558f8ac82ac1b5e07da68f4a71a70240eaf38409': ('08a01a1b7d5d3109b991bcd05d57272fd3f5f018', '0da1f56d3afc4448fb04e33a6aa84ebff3513599'),
}
for commit, (tree, parent) in identities.items():
    actual = git('show', '-s', '--format=%T %P', commit).decode().strip()
    check(f'identity {commit}', actual == f'{tree} {parent}')
for ancestor, tip in [(R1, R2), ('91a3ed53a256e89ed71d7d11c204252c417400a6', R1),
                      ('2c3090cba6d8a56102a5c47ceb026c5b5fea3234', Q3),
                      ('558f8ac82ac1b5e07da68f4a71a70240eaf38409', Q3), (Q3, 'HEAD')]:
    git('merge-base', '--is-ancestor', ancestor, tip)
    check(f'ancestry {ancestor} -> {tip}', True)
fsck = git('fsck', '--full').decode()
check('object connectivity', True, fsck.strip() or 'no diagnostics')
surface = git('diff', '--name-status', R1, R2).decode().splitlines()
check('Q3-F01 Git surface 12 modified + 6 added',
      sum(x.startswith('M\t') for x in surface) == 12 and
      sum(x.startswith('A\t') for x in surface) == 6 and
      all(x.split('\t')[-1].startswith(PREFIX) for x in surface), surface)
index = current('evidence-index.md')
check('Q3-F01 corrected index', 'twelve modified and six added' in index and
      '`README.md`, `evidence-index.md`' in index)
bad = [str(p.relative_to(ROOT)) for p in (ROOT / PREFIX).rglob('*.md')
       if re.search(r'ten modified and five added|10 modified\s*\+\s*5 added', p.read_text(encoding='utf-8'))]
check('no active incorrect R2 count', not bad, bad)

base = ROOT / 'docs/reviews/ec6d-scope-q3-evidence'
validation = json.loads((base / 'validation.json').read_text())
for name, digest in validation['artifactByteSha256'].items():
    blob = git('show', f'{Q3}:docs/reviews/ec6d-scope-q3-evidence/{name}')
    check(f'Q3 committed artifact SHA256 {name}', hashlib.sha256(blob).hexdigest() == digest)
q2 = '1bfcc84bfd95974fe9c54637a347e0e4d4b158e7'
check('Q2 reconciled content report blob', git('rev-parse', '6630624f976d53161e2657b638c7a67c41f9c0ed:docs/reviews/ec6d-scope-q2-independent-verification.md').decode().strip()
      == 'ff2d18826a350c809785a8d24c5355abbfb36f38')
for ancestor, tip in [('6e34452647fb28ee12be14798142921838f997bd', '6630624f976d53161e2657b638c7a67c41f9c0ed'),
                      ('6630624f976d53161e2657b638c7a67c41f9c0ed', q2)]:
    git('merge-base', '--is-ancestor', ancestor, tip)
    check('Q2 supersession ' + ancestor, True)
q3run = json.loads((base / 'q3-oracle-run.json').read_text())
check('recorded oracle semantics only (no rerun)', q3run['compiler'] == '5.9.3' and
      len(q3run['cases']['merged']['heritage'][0]['bindsTo']) == 2 and
      not q3run['cases']['merged']['diagnostics'])
p9 = json.loads(git('show', f'{q2}:docs/reviews/ec6d-scope-q2-evidence/p9-adversarial-results.json'))
merged = [n for n in p9['merged']['nodes'] if n['kind'] == 'interface' and n['Name'] == 'Widget']
check('merged p9 one node/two physical records', len(merged) == 1 and len(merged[0]['physicalDeclarations']) == 2)

sym = current('symbol-binding-contract.md')
query = current('consumer-query-contract.md')
acceptance = current('acceptance-contract.md')
check('lexical gates and import route unchanged', between(sym, '### 3.1', '### 3.5') ==
      between(old('symbol-binding-contract.md'), '### 3.1', '### 3.5'))
check('attribution contract unchanged', between(sym, '## 2.', '## 3.') ==
      between(old('symbol-binding-contract.md'), '## 2.', '## 3.'))
for name in ['publication-consistency-contract.md', 'scope-proposal.md', 'risk-register.md']:
    check(f'{name} semantic baseline unchanged', current(name) == old(name))
reason_pattern = r'^\| `([a-z-]+)` \|'
reasons = re.findall(reason_pattern, query, re.M)
check('closed reason codes unchanged (20 emittable + 2 reserved)',
      reasons == re.findall(reason_pattern, old('consumer-query-contract.md'), re.M) and len(reasons) == 22)
check('Q3-F02 two trigger paths and fixture distinction', 'OR §3.4 step 1' in query and
      'not r2 `ambient-export`' in query and 'synthetic fact A named occurrence' in acceptance)
check('Q3-F03 different same-file declaration omitted', 'omit this candidate' in sym and
      'completed proof to different D → omitted' in acceptance)
check('Q3-F04 merged target outcome pinned', 'target.declarationRecordId = null' in acceptance and
      'exactly one ambiguous-export unknown' in acceptance and '`target.declarationRecordId` is `null`' in query)
check('Q3-F05 explicit union/local names/identity provenance', all(t in query for t in
      ['explicit union', 'L(sourceFile)', 'stored context-record identity', 'retain all discovery origins', 'all** bindings']))
for start, end in [('### 3.2 Possible consumer', '### 3.3 Candidate'),
                   ('## 5.', '## 7.')]:
    check('query invariant ' + start, between(query, start, end) == between(old('consumer-query-contract.md'), start, end))

changed = git('diff', '--name-only', Q3).decode().splitlines()
untracked = git('ls-files', '--others', '--exclude-standard').decode().splitlines()
paths = sorted(set(changed + untracked))
check('authorized path scope only', all(p.startswith(PREFIX) or p ==
      'docs/decisions/ec6d-owner-01-contract-acceptance.md' for p in paths), paths)
check('historical reports/production/tests unchanged', not git('diff', '--name-only', Q3, '--',
      'src', 'tests', 'docs/reviews', 'docs/evidence').strip())
for mode in [[], ['--cached']]:
    git('diff', *mode, '--check')
    check('whitespace ' + ('staged' if mode else 'unstaged'), True)

snapshot = json.loads((ROOT / PREFIX / 'analysis/owner-01-preflight.json').read_text(encoding='utf-8-sig'))
refs = git('show-ref').decode().splitlines()
check('all pre-existing mirror refs unchanged', all(ref in refs for ref in snapshot['refs']))
extra_refs = [ref for ref in refs if ref not in snapshot['refs']]
check('only acceptance branch added to mirror', len(extra_refs) == 1 and
      extra_refs[0].endswith(' refs/heads/acceptance/hetu-ec6d-owner-01'))
before_blocks = '\n'.join(snapshot['worktrees']).strip().split('\n\n')
after_blocks = git('worktree', 'list', '--porcelain').decode().strip().split('\n\n')
check('protected mirror worktree identities/inventory unchanged',
      all(block in after_blocks for block in before_blocks) and len(after_blocks) == len(before_blocks) + 1)
for entry in snapshot['states']:
    path = entry['path']
    status = git('-C', path, 'status', '--porcelain=v1', '--untracked-files=all').decode().splitlines()
    check('protected worktree ' + path, status == entry['status'])

missing = []
links = 0
docs = list((ROOT / PREFIX).rglob('*.md'))
record = ROOT / 'docs/decisions/ec6d-owner-01-contract-acceptance.md'
if record.exists():
    docs.append(record)
for path in docs:
    for match in re.finditer(r'(?<!!)\[[^\]\n]+\]\(([^)\n]+)\)', path.read_text(encoding='utf-8')):
        dest = match.group(1).strip().split(' "')[0].strip('<>')
        if re.match(r'[a-zA-Z][a-zA-Z0-9+.-]*:', dest) or dest.startswith('#'):
            continue
        links += 1
        target = path.parent / unquote(dest.split('#')[0])
        if not target.exists():
            missing.append([str(path.relative_to(ROOT)), dest])
check('relative Markdown link existence', not missing, {'documents': len(docs), 'links': links, 'missing': missing,
      'limitation': 'existence only; anchors not validated'})
contracts = None
if args.contracts:
    contracts = {'commit': args.contracts, 'tree': git('rev-parse', f'{args.contracts}^{{tree}}').decode().strip()}
    for name in ['symbol-binding-contract.md', 'consumer-query-contract.md', 'publication-consistency-contract.md',
                 'acceptance-contract.md', 'scope-proposal.md']:
        check('accepted contract unchanged since corrected commit: ' + name,
              git('show', f'{args.contracts}:{PREFIX}{name}').decode().replace('\r\n', '\n') == current(name))
if record.exists():
    decision = record.read_text(encoding='utf-8')
    check('three explicit owner decisions and authority', all(text in decision for text in [
        'explicit owner instruction', 'ADR-1: ACCEPT OPTION A',
        'ADR-2: ACCEPT QUALIFIED RESULTS WITH MANDATORY SAFEGUARDS',
        'ADR-3: AUTHORIZE NARROW EC-6D IMPORT/EXPORT ATTRIBUTION ACROSS THE TRACK B DEFERRAL']))
    check('inherited qualifications and authorization limits explicit', all(text in decision for text in [
        'R16 remains OPEN', 'WSL2 is not native Linux', 'FQ1-EV-1', 'QN-1',
        'performance NOT EVALUATED', 'no new facts/query implementation',
        'does not authorize production', 'ACCEPT WITH QUALIFICATIONS']))
    canonical = ROOT / PREFIX / 'analysis/owner-01-canonical-validation.json'
    canonical_report = json.loads(canonical.read_text(encoding='utf-8-sig'))
    check('separate canonical verification report', canonical_report['result'] == 'PASS' and
          canonical_report['refsUnchanged'] and canonical_report['worktreeInventoryUnchanged'] and
          len(canonical_report['worktrees']) == 25 and canonical_report['q3ObjectAbsent'])
report = {'milestone': 'EC6D-OWNER-01', 'result': 'PASS', 'contracts': contracts,
          'checks': checks, 'commands': commands,
          'limits': ['documentation/provenance verification only', 'no oracle or production tests rerun',
                     'canonical worktree verification is separately recorded; this checker validates mirror worktrees',
                     'protected paths in the committed preflight snapshot refer to this local Windows environment']}
if args.output:
    target = (ROOT / args.output).resolve()
    if not target.is_relative_to(ROOT / PREFIX):
        raise ValueError('Output must remain under docs/planning/ec6d')
    target.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'result': 'PASS', 'checks': len(checks), 'links': links, 'documents': len(docs),
                  'changedPaths': len(paths), 'output': args.output}))
