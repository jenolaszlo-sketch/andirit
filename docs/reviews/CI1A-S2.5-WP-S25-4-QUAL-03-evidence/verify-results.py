"""QUAL-03 independent receipt verifier. Expected sites originate in fixture bytes.
Does not import production code or producer checking scripts.
"""
import base64, collections, hashlib, json, pathlib, re, struct, xml.etree.ElementTree as ET, zipfile
E=pathlib.Path(__file__).resolve().parent
W=E.parent
def load(p): return json.loads(p.read_bytes())
def sha(b): return hashlib.sha256(b).hexdigest()
def encode(*values):
    b=[str(x).encode('utf-8') for x in values]
    return sha(b''.join(struct.pack('>I',len(x))+x for x in b))
def dump(n,v): (E/n).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
audit=[]
for folder,archive in [('production','production.zip'),('baseline','baseline.zip')]:
    with zipfile.ZipFile(W/archive) as z:
        for n in z.namelist():
            if n.endswith('/'): continue
            raw=z.read(n)
            assert (W/folder/n).read_bytes()==raw,(folder,n)
            if n.startswith(('src/','tests/')) or n.endswith(('.props','.targets','.slnx','.json','.yml','.yaml','.csproj')) and not n.startswith('docs/'):
                audit.append(dict(profile=folder,path=n,bytes=len(raw),sha256=sha(raw)))
dump('execution-source-verification.json',audit)

profiles={}
for profile,root,prefix in [('R1',W/'records/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence','b3'),('R2',E,'b3'),('R2mixed',E,'b3-mixed')]:
    results=[]
    for phase in ['publish','reopen']:
        d=load(root/f'probes/corrected/{prefix}-{phase}.json'); assert not d['failures']
        files=d['sourceInputs']; fields=d['expectedDefiningCompctx1Fields']
        repo='review:s254:binding'; owner='csharp:project:'+sha(b'Caller/Caller.csproj')
        dep='csharp:project:'+sha(b'Dependency/Dependency.csproj'); doc='M:QualifierDependency.Api.Mark'
        assert fields['ProjectUnitId']==dep
        items=[]
        for source in fields['Sources']:
            assert source['ContentHash']=='sha256:'+sha(files[source['Path']].encode())
            items.append('|'.join(source[k] for k in ['Path','ContentHash','LanguageVersion','PreprocessorSymbols']))
        assert fields['References']==sorted(fields['References'])
        ctx='compctx1:sha256:'+encode('compctx1-context','compctx1',repo,dep,fields['AssemblyName'],fields['MetadataIdentity'],fields['ModuleName'],fields['OptionsDigest'],*items,*fields['References'])
        assert ctx==fields['DescriptorDigest']==d['observedDefiningCompctx1']['DescriptorDigest']
        components=[encode('asmctx3-identifier','Mark'),encode('asmctx3-declarations',doc),encode('asmctx3-target','method|0|')]
        target='asmctx3:sha256:'+encode('asmctx3-context','compctx1',ctx,fields['MetadataIdentity'],fields['ModuleName'],*components)
        sites={}
        for path,content in files.items():
            if not re.fullmatch(r'Caller/Fixture\d+\.cs',path): continue
            for line,text in enumerate(content.splitlines(),1):
                for m in re.finditer(r'QualifierDependency\.Api\.(Mark)\(\);',text):
                    start,end=m.span(1)
                    span=dict(Path=path,StartLine=line,StartColumn=start+1,EndLine=line,EndColumn=end+1)
                    raw='sha256:'+sha(content.encode())
                    ident=encode('usage4',repo,'penghou.hetu.csharp','usage4-b2c-exp-r2-20261101',owner,path,raw,line,start+1,line,end+1,doc,target,'method-invocation')
                    sites[ident]=dict(span=span,sourceHash=raw,owner=owner,target=target)
        assert len(sites)==50
        declaration=files['Dependency/Api.cs']
        token='public static int Mark() => 7;'; pos=declaration.index(token)
        line=declaration[:pos].count('\n')+1; col=pos-declaration.rfind('\n',0,pos)
        expected_decl=dict(Path='Dependency/Api.cs',StartLine=line,StartColumn=col,EndLine=line,EndColumn=col+len(token))
        for facts in [d['result']['Occurrences'],d['decodedCallerUnit']['Facts']]:
            assert len(facts)==50 and {f['Id']['Value'] for f in facts}==set(sites)
            for fact in facts:
                s=sites[fact['Id']['Value']]
                assert fact['Span']==s['span'] and fact['SourceUnitId']==owner and fact['TargetId']['Value']==target
                if 'DeclarationSpan' in fact: assert fact['DeclarationSpan']==expected_decl
                if 'SourceHash' in fact: assert fact['SourceHash']==s['sourceHash']
        for c in [d['initialCoverage'],d['result']['Coverage'],d['decodedCallerUnit']['Coverage']]:
            assert [c[k] for k in ['InvocationFacts','External','Unresolved','Ambiguous']]==[50,1,int(profile=='R2mixed'),int(profile=='R2mixed')]
        assert d['externalObserved']['resolution']=='UnsupportedExternalEvidence'
        results.append(dict(phase=phase,pid=d['processId'],context=ctx,components=components,referenceInputs=fields['References'],sites=sites))
    assert results[0]['pid']!=results[1]['pid'] and results[0]['sites']==results[1]['sites']
    profiles[profile]=results
def stable(profile): return sorted((s['span']['Path'],s['span']['StartLine'],s['span']['StartColumn'],s['sourceHash'],s['owner']) for s in profiles[profile][0]['sites'].values())
assert stable('R1')==stable('R2')==stable('R2mixed')
assert profiles['R1'][0]['components']==profiles['R2'][0]['components']==profiles['R2mixed'][0]['components']
dump('independent-b3-identities.json',dict(profiles=profiles,positiveSites=50,hostScoped=True,stableInputsPreserved=True))

ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
discovered=collections.Counter(x.strip() for x in (E/'gates/discovery.log').read_text(encoding='utf-8-sig').splitlines() if x.startswith('    Andirit.'))
executed=collections.Counter(); suites={}
for p in (E/'gates/trx').glob('*.trx'):
    tree=ET.parse(p); counts=tree.find('t:ResultSummary/t:Counters',ns).attrib
    assert all(int(counts[k])==0 for k in ['failed','error','notExecuted'])
    methods={x.attrib['id']:x.find('t:TestMethod',ns).attrib for x in tree.findall('t:TestDefinitions/t:UnitTest',ns)}
    assembly=pathlib.PureWindowsPath(next(iter(methods.values()))['codeBase']).name
    suites[assembly]=int(counts['passed'])
    for r in tree.findall('t:Results/t:UnitTestResult',ns):
        assert r.attrib['outcome']=='Passed'
        name=r.attrib['testName']
        name=re.sub(r'\\u(d[89ab][0-9a-f]{2})\\u(d[c-f][0-9a-f]{2})',lambda m:chr(0x10000+((int(m[1],16)-0xd800)<<10)+(int(m[2],16)-0xdc00)),name,flags=re.I)
        executed[name]+=1
assert executed==discovered and sum(executed.values())==917
prior=collections.Counter(x.strip() for x in (W/'records/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence/gates/discovery.log').read_text(encoding='utf-8-sig').splitlines() if x.startswith('    Andirit.'))
assert not prior-discovered and sum((discovered-prior).values())==8
raw=(E/'gates/oracle-r2.json').read_bytes()
assert len(raw)==146851 and sha(raw)=='97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9'
dump('regression-verification.json',dict(suites=suites,passed=917,failed=0,skipped=0,discoveryExactlyMatchesExecution=True,retained=909,newCases=list((discovered-prior).elements()),oracleCases=185,oracleBytes=len(raw),oracleSha256=sha(raw)))

summary=[]
for p in sorted((E/'probes/corrected').glob('*.json')):
    d=load(p)
    if 'case' not in d: continue
    summary.append(dict(case=d['case'],pid=d['processId'],failures=d['failures'],coverage=d.get('initialCoverage'),result=d.get('result',d.get('query')),missingDefiningSites=d.get('exactMissingDefiningCompilationSites')))
dump('probe-summary.json',summary)
for r in summary:
    if r['case'] in ['m54-returned-publish','m54-returned-reopen','m54-interface-publish','m54-interface-reopen']:
        # The pre-registered semantic hypothesis was disproved: Roslyn also refuses
        # Get() because its return type needs the absent direct reference. Keep
        # original hypothesis failures; validate the actual causal receipt separately.
        d=load(E/'probes/corrected'/f"{r['case']}.json")
        assert len(r['failures'])==3 and 'unhandled' not in d
        assert [x['symbol'] is None for x in d['binding']]==[True,True,False]
        assert d['binding'][1]['reason']=='OverloadResolutionFailure'
        for c in [d['initialCoverage'],d['query']['Coverage'],d['decodedCallerUnit']['Coverage']]:
            assert (c['Unresolved'],c['External'],c['InvocationFacts'])==(2,0,1)
        assert d['exactMissingDefiningCompilationSites']==0 and not d['exactM54ContinuityEstablished']
        r['assessment']='Semantic hypothesis disproved, correctly persisted two pre-resolver unresolved sites; not a production defect or M5.4 pass.'
    else:
        assert not r['failures'],(r['case'],r['failures'])
dump('probe-summary.json',summary)
print('Independent source, 50 B3 site identities per profile/phase, 917-test inventory, oracle and probe verification PASS')
