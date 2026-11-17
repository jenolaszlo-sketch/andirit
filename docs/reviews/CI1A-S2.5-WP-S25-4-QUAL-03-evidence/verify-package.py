"""Final non-executing package checks, after all production runs have finished."""
import hashlib,json,pathlib,re,ast
E=pathlib.Path(__file__).resolve().parent
W=E.parent
for p in E.glob('*.py'): ast.parse(p.read_text(encoding='utf-8-sig'),filename=str(p))
def load(p):return json.loads(p.read_bytes())
gates={}
for name in ['release','discovery','full','f02','f03','r1','binding','publication','native','format','oracle']:
    content=(E/f'gates/{name}.log').read_text(encoding='utf-8-sig')
    expected_exit=2 if name=='format' else 0
    assert content.rstrip().endswith('Exit code: '+str(expected_exit)),name
    gates[name]=expected_exit
    if name=='format':
        errors=[line for line in content.splitlines() if ': error ' in line]
        assert errors and all('error WHITESPACE:' in line and '\\r\\n' in line and any(x in line for x in ['CSharpProjectDiscovery.cs','VerifiedCSharpCompilation.cs']) for line in errors)
        prior=(E/'attempts/checkout-profile/gates/format.log').read_text(encoding='utf-8-sig')
        assert prior.rstrip().endswith('Exit code: 0')
assert '0 Warning(s)' in (E/'gates/release.log').read_text(encoding='utf-8-sig')
assert '0 Error(s)' in (E/'gates/release.log').read_text(encoding='utf-8-sig')
assert '185/185' in (E/'gates/oracle.log').read_text(encoding='utf-8-sig')
expected={'f02':5,'f03':3,'r1':2,'binding':143,'publication':136,'native':52}
for name,count in expected.items():
    log=(E/f'gates/{name}.log').read_text(encoding='utf-8-sig')
    assert re.search(r'Passed:\s+'+str(count)+r',\s+Skipped:\s+0',log),name
cases=[]
for p in (E/'probes/corrected').glob('*.json'):
    d=load(p)
    if 'case' in d: cases.append(d['case'])
assert len(cases)==len(set(cases))==84,len(cases)
assert len(load(E/'case-verification.json')['corrected'])==20
assert len(load(E/'case-verification.json')['baseline'])==5
assert sum(load(E/'reconciliation-summary.json')['matrixCurrent'].values())==64
assert sum(load(E/'reconciliation-summary.json')['obligations'].values())==78
checks=[]
report=W/'ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-03.md'
for p in [report,*E.glob('*.md')]:
    for target in re.findall(r'\]\(([^)]+)\)',p.read_text(encoding='utf-8')):
        if target.startswith(('http','#')):continue
        target=target.split('#')[0]
        if not target:continue
        if target.startswith('CI1A-S2.5-WP-S25-4-QUAL-03-evidence/'):
            resolved=E/target.split('/',1)[1]
        elif target.startswith('../CI1A-S2.5-WP-S25-4-QUAL-02-evidence/'):
            resolved=W/'records/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence'/target.split('/',2)[2]
        elif target.startswith('../../decisions/') or target.startswith('../../planning/'):
            resolved=W/'records/docs'/target[6:]
        else:resolved=p.parent/target
        if resolved.name=='manifest.json' and resolved.parent==E:continue
        assert resolved.is_file(),(p.name,target,str(resolved))
        checks.append(dict(document=p.name,target=target))
result=dict(gates=gates,formattingQualification='Exact Git-blob profile exits 2 with line-ending WHITESPACE diagnostics in two unchanged files; original Windows checkout profile exits 0. Neither failure nor profile distinction is waived.',correctedPrimaryCases=len(cases),correctedPairedCategoryCases=20,baselinePairs=5,matrixRows=64,obligationEntries=78,markdownLinksVerified=len(checks),reportSha256=hashlib.sha256(report.read_bytes()).hexdigest())
(E/'package-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result))
