import base64,json,pathlib
E=pathlib.Path(__file__).resolve().parent
def read(p): return json.loads(p.read_bytes())
def units(d): return [json.loads(base64.b64decode(x['payload'])) for x in d['native']['AndiritOccurrenceUnit']]
checks=[]
for folder in ['probes/corrected','supplemental']:
    for p in sorted((E/folder).glob('control-*-publish.json')):
        other=p.with_name(p.name.replace('-publish','-reopen'))
        if not other.exists(): continue
        a,b=read(p),read(other)
        assert not a['failures'] and not b['failures']
        assert a['processId']!=b['processId'] and a['native']==b['native']
        for d in [a,b]:
            rows=units(d); facts=[f for u in rows for f in u['Facts']]
            expected=d['expected']
            assert len(facts)==expected['expectedFacts']
            assert sum(u['Coverage']['UnsupportedCategories'] for u in rows)==expected['expectedUnsupported']
            assert sum(u['Coverage']['Unsupported'] for u in rows)==expected['expectedUnsupported']
            initial={f['Id']['Value'] for f in d['initialFacts']}
            assert initial=={f['Id']['Value'] for f in facts}=={f['Id']['Value'] for f in d['result']['Occurrences']}
            if d['result']['Availability']==5:
                n=len(d['scope']['Units']); verified=n-len(d['scope']['MissingScopeUnits'])
                assert d['result']['Coverage']['DeclaredUnitCount']==n
                assert d['result']['Coverage']['VerifiedUnitCount']==verified
                assert not d['result']['IsComplete'] and not d['authorityIssued'] and not facts
        checks.append(dict(case=a['case'],expected=a['expected'],distinctProcesses=True,nativeBytePayloadsIdentical=True,supportedIdsPreserved=True))
baseline=[]
for case in ['ctor','property','reference','incomplete','declared']:
    a,b=[read(E/f'probes/baseline/control-{case}-{phase}.json') for phase in ['publish','reopen']]
    assert a['processId']!=b['processId'] and a['native']==b['native']
    for d in [a,b]:
        assert d['failures'] and 'unhandled' not in d
        if case in ['ctor','property','reference']:
            assert d['initialCoverage']['Unsupported']==d['result']['Coverage']['Unsupported']==0
            assert sum(u['Coverage']['Unsupported'] for u in units(d))==0
        else:
            assert d['result']['Availability']==1 and not d['result']['IsComplete'] and not units(d)
    baseline.append(dict(case=case,historicalFailureReproduced=True,publishFailures=a['failures'],reopenFailures=b['failures']))
(E/'case-verification.json').write_text(json.dumps(dict(corrected=checks,baseline=baseline),indent=2)+'\n')
print(f'{len(checks)} corrected category/membership pairs and five historical failure pairs independently verified.')
