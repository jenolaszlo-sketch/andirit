import json, glob, os, sys

tag = sys.argv[1]
base = r'C:\Windows\Temp\opencode\qual04\receipts-' + tag
for p in sorted(glob.glob(os.path.join(base, tag + '-*.json'))):
    d = json.load(open(p, encoding='utf-8'))
    c = d['case']
    q = d.get('query') or {}
    cov = q.get('coverage') or {}
    n = d.get('native') or {}
    u = n.get('units') or []
    print('%-14s %-38s %-20s c=%-5s d=%-4s v=%-4s hits=%-3s rows=%-3s rowcov=%-26s fails=%s' % (
        c, os.path.basename(p), q.get('availability'), q.get('isComplete'),
        cov.get('declaredUnitCount'), cov.get('verifiedUnitCount'),
        len(q.get('occurrences') or []), len(u),
        [(x['coverage']['declared'], x['coverage']['verified']) for x in u],
        (d.get('failures') or [])[:2]))