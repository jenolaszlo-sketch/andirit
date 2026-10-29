import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent
reports = root / 'migration-reports'
checks = []

def read(label, profile):
    data = json.loads((reports / f'{label}-{profile}.json').read_text(encoding='utf-8-sig'))
    assert data['health']['IsHealthy'] and data['health']['SchemaVersion'] == 2
    return data

def observations(data):
    return {o['language']: o for o in data['observations']}

def stable(data):
    return json.dumps(data, sort_keys=True, separators=(',', ':'))

def compare(old, new, allow_profile_change=False):
    result = {}
    for language in ('ts', 'js'):
        a, b = observations(old)[language], observations(new)[language]
        entry = {'added': {}, 'profileEvidenceUpdates': []}
        for kind in ('nodes', 'declarations', 'edges'):
            old_facts = {f['Id']: json.loads(f['Json']) for f in a[kind]}
            new_facts = {f['Id']: json.loads(f['Json']) for f in b[kind]}
            assert old_facts.keys() <= new_facts.keys(), (language, kind, 'lost identities')
            for identity, fact in old_facts.items():
                actual = new_facts[identity]
                if fact != actual and allow_profile_change and kind == 'edges':
                    before, after = fact['Evidence']['ProviderVersion'], actual['Evidence']['ProviderVersion']
                    assert before.endswith('.p6') and after == before[:-3] + '.p8'
                    fact['Evidence']['ProviderVersion'] = after
                    entry['profileEvidenceUpdates'].append({'id': identity, 'before': before, 'after': after})
                assert fact == actual, (language, kind, identity, 'unexplained changed fact')
            entry['added'][kind] = sorted(new_facts.keys() - old_facts.keys())
        assert set(a['lookup']) <= set(b['lookup']), 'Existing exact-name candidate lost'
        assert set(a['defaultNeighborhoodNodeIds']) <= set(b['defaultNeighborhoodNodeIds'])
        assert set(a['defaultNeighborhoodEdgeIds']) <= set(b['defaultNeighborhoodEdgeIds'])
        result[language] = entry
    return result

for profile in ('projection', 'native'):
    seed = read('p6-seed', profile)
    before = read('direct-before', profile)
    direct = read('direct-p9', profile)
    p8 = read('staged-p8', profile)
    staged = read('staged-p9', profile)
    assert seed['observations'] == before['observations'], 'New implementation cannot read original p6'
    assert seed['observations'] == read('p6-seed-reopen', profile)['observations'], 'Seed not recoverable'
    assert direct['observations'] == staged['observations'], 'Direct and staged logical facts differ'
    direct_delta = compare(seed, direct, True)
    p8_delta = compare(seed, p8, True)
    p9_delta = compare(p8, staged)
    for language in ('ts', 'js'):
        a, b, c = (observations(d)[language] for d in (seed, p8, direct))
        assert [len(a[k]) for k in ('nodes', 'declarations', 'edges')] == [7, 4, 10]
        assert [len(b[k]) for k in ('nodes', 'declarations', 'edges')] == [7, 4, 12]
        assert [len(c[k]) for k in ('nodes', 'declarations', 'edges')] == [12, 4, 17]
        assert all(u['version'].endswith('.p9') for u in c['units'])
        assert len([n for n in c['nodes'] if n['Kind'] == 'export']) == 5
        assert len([e for e in c['edges'] if e['Kind'] == 'exports']) == 5
        assert len(c['exportLookup']) == 1
    for label, expected in [('direct-repeat', direct), ('direct-cancel', direct), ('staged-repeat', staged), ('staged-reopen', staged)]:
        data = read(label, profile)
        assert data['observations'] == expected['observations'], label
        if label.endswith('repeat'):
            assert all(s['status'] == 'Completed' and s['PluginsExecuted'] == 0 for s in data['statuses'])
        if label.endswith('cancel'):
            assert all(s['status'] == 'OperationCanceledException' for s in data['statuses'])
    checks.append({'profile': profile, 'passed': True, 'directP6ToP9': direct_delta, 'p6ToP8': p8_delta, 'p8ToP9': p9_delta,
                   'logicalP9Sha256': hashlib.sha256(stable(direct['observations']).encode()).hexdigest(),
                   'repeat': 'Completed; zero plugins executed; identical facts',
                   'cancellation': 'OperationCanceledException; unchanged publication', 'schema': 2,
                   'provider': 'LatticeDbSharp 0.2.0', 'sqlite': 'not implemented by this accepted repository'})
assert read('direct-p9', 'projection')['observations'] == read('direct-p9', 'native')['observations']
result = {'passed': True, 'platform': 'Windows 11 x64', 'checks': checks,
          'historicalHash': 'ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f',
          'historicalHashQualification': 'Still unreproducible; neither regenerated nor replaced by these new logical hashes.'}
(root / 'migration-verification.json').write_text(json.dumps(result, indent=2) + '\n')
print('PASS: direct p6->p9 and staged p6->p8->p9, projection/native, TS/JS; identity, fact, reopen, repeat, cancellation and seed-recovery checks.')
