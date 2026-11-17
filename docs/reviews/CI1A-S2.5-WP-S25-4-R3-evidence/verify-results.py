"""Validate exact membership, native diagnostic accounting and regression receipts."""
import base64
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

E = Path(__file__).resolve().parent
Q = Path(sys.argv[1])


def load(path):
    return json.loads(path.read_bytes())


def dump(name, content):
    (E / name).write_text(json.dumps(content, indent=2) + '\n', encoding='utf-8', newline='\n')


def units(data):
    return [json.loads(base64.b64decode(row['payload'])) for row in data['native']['AndiritOccurrenceUnit']]


def inventory(data):
    scope = data['scope']
    if isinstance(scope, dict):
        declared = {unit['ProjectUnitId'] for unit in scope['Units']}
        verified = declared - set(scope['MissingScopeUnits'])
    else:
        declared = {unit['ProjectUnitId'] for unit in scope}
        verified = {unit['ProjectUnitId'] for unit in data['verifiedMembers']}
    return sorted(declared), sorted(verified)


counter_fields = ('InvocationFacts', 'External', 'Ambiguous', 'Unresolved',
                  'Unsupported', 'UnsupportedCategories', 'ConditionalInactive')
membership_fields = ('DeclaredUnitCount', 'VerifiedUnitCount')
cases = {
    'control-ctor': ((2, 2), (50, 1, 0, 0, 1, 1, 0), 50),
    'control-empty': ((2, 2), (0, 0, 0, 0, 0, 0, 0), 0),
    'm54-direct': ((3, 3), (3, 0, 0, 0, 0, 0, 0), 1),
    'control-incomplete': ((3, 2), (0, 0, 0, 0, 0, 0, 0), 0),
    'control-multiple': ((4, 2), (0, 0, 0, 0, 0, 0, 0), 0),
}
rows = []
for profile in ('baseline', 'corrected'):
    for case, (membership, counters, hits) in cases.items():
        pair = []
        for phase in ('publish', 'reopen'):
            receipt = E / 'probes' / profile / f'{case}-{phase}.json'
            data = load(receipt)
            assert not data['failures']
            declared, verified = inventory(data)
            assert (len(declared), len(verified)) == membership
            complete = len(declared) == len(verified)
            native = units(data)
            assert sorted(row['IndexUnitId'] for row in native) == verified
            expected = tuple(n * len(verified) for n in membership) if profile == 'baseline' and complete else membership
            result = data.get('result', data.get('query'))
            assert result['Availability'] == (0 if complete else 5)
            assert result['IsComplete'] == complete and len(result['Occurrences']) == hits
            assert tuple(result['Coverage'][key] for key in membership_fields) == expected
            assert tuple(result['Coverage'][key] for key in counter_fields) == counters
            assert tuple(sum(row['Coverage'][key] for row in native) for key in membership_fields) == expected
            assert tuple(sum(row['Coverage'][key] for row in native) for key in counter_fields) == counters
            assert sum(len(row['Facts']) for row in native) == counters[0]
            for row in native:
                if complete:
                    envelope = row['Authority']
                    assert envelope['SourceUnitId'] == row['IndexUnitId']
                    assert sorted(member['ProjectUnitId'] for member in envelope['Members']) == verified
                    assert tuple(envelope['Coverage'][key] for key in membership_fields) == membership
                    assert tuple(row['Coverage'][key] for key in membership_fields) == (membership if profile == 'baseline' else (1, 1))
                else:
                    assert row['Authority'] is None and row['Coverage']['VerifiedUnitCount'] == 1
            pair.append(data['processId'])
            rows.append(dict(receipt=receipt.relative_to(E).as_posix(), profile=profile, case=case,
                             phase=phase, pid=data['processId'], declared=declared, verified=verified,
                             expected=membership, observed=expected, complete=complete, hits=hits,
                             counters=dict(zip(counter_fields, counters))))
        assert len(set(pair)) == 2
dump('membership-reconciliation.json', rows)

for phase in ('publish', 'reopen'):
    receipt = E / 'probes/corrected' / f'review-transition-{phase}.json'
    data = load(receipt)
    assert not data['failures'] and data['result']['Availability'] == 5
    assert data['obsoleteAuthority']['Availability'] == 1 and data['authorityRefusal'] == 'IncompleteCompilationUniverse'
    if phase == 'publish':
        assert data['stale']['Availability'] == 3
        assert tuple(data['before']['Coverage'][key] for key in membership_fields) == (2, 2)
for case in ('control-ctor', 'control-empty', 'm54-direct'):
    data = load(E / 'probes/corrected' / f'legacy-{case}-reopen.json')
    result = data.get('result', data.get('query'))
    assert result['Availability'] == 3 and 'current verified universe' in result['RefusalReason']
    assert result['Coverage'] is None and not result['Occurrences']

for file in (E / 'membership-tests').glob('*.log'):
    assert file.read_text(encoding='utf-8-sig').rstrip().endswith('Exit code: 0'), file
for label in ('first', 'partial', 'restored', 'repeat'):
    pair = []
    for action in ('publish', 'verify'):
        receipt = (E / 'membership-tests' / f'{action}-{label}.log').read_text(encoding='utf-8-sig')
        pair.append(re.search(r'pid=(\d+)', receipt).group(1))
        expected = 'IncompleteAnalysis 3/2 complete=False hits=0 facts=0 unresolved=2' if label == 'partial' else (
            'Available 2/2 complete=True hits=1 facts=2 unresolved=0')
        assert f'run:membership-{label} ' + expected in receipt
    assert len(set(pair)) == 2

disjoint = [load(E / f'disjoint-{phase}.json') for phase in ('publish', 'reopen')]
assert disjoint[0]['processId'] != disjoint[1]['processId']
for data in disjoint:
    result = data['result']
    assert result['Availability'] == 5 and not result['IsComplete'] and not result['Occurrences']
    native = [entry['unit']['Coverage'] for entry in data['units']]
    assert [tuple(row[key] for key in membership_fields) for row in native] == [(2, 1), (2, 2)]
    assert tuple(result['Coverage'][key] for key in membership_fields) == (4, 3)
    assert tuple(result['Coverage'][key] for key in counter_fields) == (0, 3, 1, 2, 1, 1, 1)
    assert tuple(sum(row[key] for row in native) for key in counter_fields) == (0, 3, 1, 2, 1, 1, 1)

assert [row['resolver'] for row in load(E / 'm54-results.json')['records']] == [None, 'Complete', 'Complete', 'UnsupportedExternalEvidence']


def discovered(log):
    return Counter(line.strip() for line in log.read_text(encoding='utf-8-sig').splitlines()
                   if line.startswith('    Andirit.'))


gates = E / 'gates-final'
current = discovered(gates / 'discovery-serial.log')
prior = discovered(Q / 'gates/discovery.log')
executed = Counter()
ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
for file in (gates / 'trx').glob('*.trx'):
    test_run = ET.parse(file)
    counts = test_run.find('t:ResultSummary/t:Counters', ns).attrib
    assert all(int(counts[key]) == 0 for key in ('failed', 'error', 'notExecuted'))
    for result in test_run.findall('t:Results/t:UnitTestResult', ns):
        assert result.attrib['outcome'] == 'Passed'
        name = re.sub(r'\\u(d[89ab][0-9a-f]{2})\\u(d[c-f][0-9a-f]{2})',
                      lambda match: chr(0x10000 + ((int(match[1], 16) - 0xd800) << 10) + int(match[2], 16) - 0xdc00),
                      result.attrib['testName'], flags=re.I)
        executed[name] += 1
assert not prior - current and executed == current and sum(prior.values()) == 917 and sum(current.values()) == 926, (
    sum(prior.values()), sum(current.values()), sum(executed.values()), prior - current, current - executed, executed - current)
assert all('CompleteScopeMembership' in name for name in (current - prior))
oracle = (gates / 'oracle-r2.json').read_bytes()
oracle_hash = hashlib.sha256(oracle).hexdigest()
assert len(oracle) == 146851 and oracle_hash == '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9'
assert oracle == (Q / 'gates/oracle-r2.json').read_bytes()
gate_results = {}
for name in ('release', 'full', 'discovery', 'discovery-serial', 'r1', 'f02', 'f03', 'binding', 'publication', 'native', 'oracle', 'format', 'checkout-format'):
    log = (gates / f'{name}.log').read_text(encoding='utf-8-sig')
    expected_exit = 2 if name == 'format' else 0
    assert log.rstrip().endswith(f'Exit code: {expected_exit}'), name
    gate_results[name] = expected_exit
    if name == 'format':
        errors = [line for line in log.splitlines() if ': error ' in line]
        assert len(errors) == 7 and all('error WHITESPACE:' in line and '\\r\\n' in line for line in errors)
        assert all(any(file in line for file in ('CSharpProjectDiscovery.cs', 'VerifiedCSharpCompilation.cs')) for line in errors)
    if name == 'release':
        assert '0 Warning(s)' in log and '0 Error(s)' in log
    if name == 'oracle':
        assert '185/185' in log
    for label, count in [('r1', 2), ('f02', 5), ('f03', 3), ('native', 52)]:
        if name == label:
            assert re.search(rf'Passed:\s+{count},\s+Skipped:\s+0', log)
dump('regression-reconciliation.json', dict(passed=926, failed=0, skipped=0, retained=917, added=list((current-prior).elements()),
     gateExits=gate_results, oracleCases=185, oracleSha256=oracle_hash, oracleByteIdentical=True))
print('Membership and native receipts, distinct PIDs, M5.4, test inventory, formatter profiles and frozen oracle verified.')
