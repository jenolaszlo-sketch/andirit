"""QUAL-03 fixture encoder applied to successor receipts."""
import sys
import base64, collections, hashlib, json, pathlib, re, struct, xml.etree.ElementTree as ET, zipfile
E=pathlib.Path(__file__).resolve().parent
W=E.parent
def load(p): return json.loads(p.read_bytes())
def sha(b): return hashlib.sha256(b).hexdigest()
def encode(*values):
    b=[str(x).encode('utf-8') for x in values]
    return sha(b''.join(struct.pack('>I',len(x))+x for x in b))
def dump(n,v): (E/n).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
profiles={}
for profile,root,prefix in [('R2', pathlib.Path(sys.argv[1]), 'b3'), ('R3', E, 'b3'), ('R3mixed', E, 'b3-mixed')]:
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
            assert [c[k] for k in ['InvocationFacts','External','Unresolved','Ambiguous']]==[50,1,int(profile=='R3mixed'),int(profile=='R3mixed')]
        assert d['externalObserved']['resolution']=='UnsupportedExternalEvidence'
        results.append(dict(phase=phase,pid=d['processId'],context=ctx,components=components,referenceInputs=fields['References'],sites=sites))
    assert results[0]['pid']!=results[1]['pid'] and results[0]['sites']==results[1]['sites']
    profiles[profile]=results
def stable(profile): return sorted((s['span']['Path'],s['span']['StartLine'],s['span']['StartColumn'],s['sourceHash'],s['owner']) for s in profiles[profile][0]['sites'].values())
assert stable('R2')==stable('R3')==stable('R3mixed')
assert profiles['R2'][0]['components']==profiles['R3'][0]['components']==profiles['R3mixed'][0]['components']
dump('independent-b3-identities.json',dict(profiles=profiles,positiveSites=50,hostScoped=True,stableInputsPreserved=True))

