"""Compare exact Git-blob LF and checkout CRLF inputs using the unchanged baseline harness."""
import hashlib, json, pathlib, subprocess, tempfile
root = pathlib.Path.cwd()
out = root/'docs/reviews/ec6d-scope-q2-evidence'
corpus = 'docs/planning/ec6d/corpus/ts-interface-impact'
dll = root/'docs/planning/ec6d/analysis/bin/Debug/net10.0/ConsumerBaseline.dll'
def git(*args): return subprocess.check_output(['git',*args])
def sha(b): return hashlib.sha256(b).hexdigest()
def run(path, label):
    p = subprocess.run(['dotnet',str(dll),str(path)],capture_output=True,check=True)
    (out/f'baseline-{label}.json').write_bytes(p.stdout)
    return json.loads(p.stdout)
def ids(r): return {'nodes': sorted(x['Node']['Value'] for x in r['Nodes']), 'edges': sorted(x['Value'] for x in r['Relationships'])}
def summary(r): return {k:r[k] for k in ['IndexIdentity','CanonicalFactsSha256','PublicationConsistent','Counts','DirectQueriesForSelectedDeclaration']}
original = json.loads((root/'docs/planning/ec6d/analysis/observed-results.json').read_text(encoding='utf-8-sig'))
r1text = (root/'docs/planning/ec6d/analysis/r1-reproduction-run.txt').read_text(encoding='utf-8-sig')
r1 = json.loads(r1text[r1text.index('{'):])
file_hashes = []
(out/'bin').mkdir(exist_ok=True)
with tempfile.TemporaryDirectory(prefix='hetu-q2-lf-', dir=out/'bin') as tmp:
    assert pathlib.Path(tmp).resolve().is_relative_to(out.resolve())
    for name in git('ls-tree','-r','--name-only','91a3ed5',corpus).decode().splitlines():
        blob = git('show','91a3ed5:'+name)
        dest = pathlib.Path(tmp)/pathlib.PurePosixPath(name).relative_to(corpus)
        dest.parent.mkdir(parents=True,exist_ok=True)
        dest.write_bytes(blob)
        disk = (root/name).read_bytes()
        old = (root.parent/'hetu-ec6d-consumer-intelligence'/name).read_bytes()
        file_hashes.append({'path':name,'gitBlob':git('rev-parse','91a3ed5:'+name).decode().strip(), 'filteredHashObject':git('hash-object',name).decode().strip(), 'rawHashObject':git('hash-object','--no-filters',name).decode().strip(),'lfSha256':sha(blob),'checkoutSha256':sha(disk),'originalWorkingSha256':sha(old),'originalCrlfCount':old.count(b'\r\n'),'originalBareLfCount':old.count(b'\n')-old.count(b'\r\n'),'onlyCrlfDifference':disk.replace(b'\r\n',b'\n')==blob==old.replace(b'\r\n',b'\n')})
    lf = run(tmp,'lf')
    for name in git('ls-tree','-r','--name-only','91a3ed5',corpus).decode().splitlines():
        (pathlib.Path(tmp)/pathlib.PurePosixPath(name).relative_to(corpus)).write_bytes((root.parent/'hetu-ec6d-consumer-intelligence'/name).read_bytes())
    mixed = run(tmp,'original-bytes')
crlf = run(root/corpus,'crlf')
repeat = run(root/corpus,'crlf-repeat')
result = {'fileHashes':file_hashes,'identitiesEqual':ids(original)==ids(r1)==ids(lf)==ids(crlf)==ids(repeat)==ids(mixed), 'lfMatchesOriginalDigests':all(lf[k]==original[k] for k in ['IndexIdentity','CanonicalFactsSha256']), 'originalWorkingBytesMatchOriginalDigests':all(mixed[k]==original[k] for k in ['IndexIdentity','CanonicalFactsSha256']), 'crlfMatchesR1Digests':all(crlf[k]==r1[k] for k in ['IndexIdentity','CanonicalFactsSha256']), 'repeatMatches': all(crlf[k]==repeat[k] for k in ['IndexIdentity','CanonicalFactsSha256']), 'runs':{k:summary(v) for k,v in [('original',original),('r1',r1),('q2Lf',lf),('q2Crlf',crlf),('q2OriginalBytes',mixed)]}}
(out/'digest-reconciliation.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ['fileHashes','runs']},indent=2))
for label,r in result['runs'].items(): print(label,r['IndexIdentity'],r['CanonicalFactsSha256'],r['Counts']['Nodes'],r['Counts']['Edges'])
