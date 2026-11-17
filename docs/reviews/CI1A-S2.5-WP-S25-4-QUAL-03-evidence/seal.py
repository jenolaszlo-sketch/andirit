import hashlib,json,pathlib,sys
P=pathlib.Path(sys.argv[-1]).resolve()
verify='--verify' in sys.argv
manifest=P/'manifest.json'
m={'commission':'ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-03',
   'scheme':'SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git',
   'startingHead':'9a550c26dcb4559a317a9663ce8fec827e01bd40','startingTree':'606c17871555a3e2a41ce8eb7536befd332e97dc',
   'reviewedHead':'25bc6e3b4305c7ce088deb36cfb000138dc76648','reviewedTree':'2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3',
   'reviewerSession':'01a1e38c-27d1-7491-a12c-63e0eecb704f','files':[]}
for p in sorted(P.rglob('*'),key=lambda p:p.relative_to(P).as_posix()):
    if not p.is_file() or p==manifest: continue
    assert not p.is_symlink(),p
    raw=p.read_bytes(); m['files'].append(dict(path=p.relative_to(P).as_posix(),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest()))
if verify: assert m==json.loads(manifest.read_bytes()),'Manifest contents, membership, identity or digest differs'
else: manifest.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(entries=len(m['files']),manifestSha256=hashlib.sha256(manifest.read_bytes()).hexdigest(),verified=verify)))
