"""Independent membership accounting check: use scope/unit inventories, not counters as oracle."""
import json,pathlib
E=pathlib.Path(__file__).resolve().parent
rows=[]
for profile,case,key in [('baseline','control-ctor','result'),('corrected','control-ctor','result'),('corrected','control-empty','result'),('corrected','m54-direct','query'),('corrected','control-incomplete','result'),('corrected','control-declared','result'),('corrected','control-multiple','result'),('corrected','control-mixedmissing','result')]:
    for phase in ['publish','reopen']:
        p=E/f'probes/{profile}/{case}-{phase}.json'; d=json.loads(p.read_bytes())
        scope=d['scope']['Units'] if isinstance(d['scope'],dict) else d['scope']
        declared=len({u['ProjectUnitId'] for u in scope})
        verified=len(d['verifiedMembers']) if 'verifiedMembers' in d else declared-len(d['scope']['MissingScopeUnits'])
        c=d[key]['Coverage']
        observed=[c['DeclaredUnitCount'],c['VerifiedUnitCount']]
        expected=[declared,verified]
        matches=observed==expected
        rows.append(dict(receipt=str(p.relative_to(E)).replace('\\','/'),phase=phase,processId=d['processId'],scope=scope,expected=expected,observed=observed,matches=matches))
        if case in ['control-ctor','control-empty','m54-direct']:
            assert not matches and observed==[declared*declared,declared*verified]
        else: assert matches
(E/'membership-counterexample.json').write_text(json.dumps(rows,indent=2)+'\n')
print('Preserved complete-scope aggregate discrepancy in R1 and R2; partial-scope membership agrees in all four cases and both processes.')
