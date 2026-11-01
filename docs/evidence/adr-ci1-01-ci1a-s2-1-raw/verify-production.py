"""Producer-owned independent oracle using Python stdlib, never the C# helper."""
from pathlib import Path
import hashlib
import json
import struct

root = Path(__file__).resolve().parent
report = json.loads((root / 'production-probe-final.log').read_text(encoding='utf-8-sig'))
repeat = json.loads((root / 'production-probe-final-repeat.log').read_text(encoding='utf-8-sig'))
assert report == repeat, 'Separate processes / run IDs changed the manifest'

def h(parts):
    encoded = [p.encode('utf-8') for p in parts]
    return hashlib.sha256(b''.join(struct.pack('>I', len(p)) + p for p in encoded)).hexdigest()

for name in ('Original', 'Feature'):
    variant = report[name]
    descriptors = variant['Contexts']
    actual = {d['Context']: d for d in variant['ActualInputs']}
    snapshots = {s['Path']: s for s in variant['Sources']}
    for source in snapshots.values():
        data = (root / 'fixture' / source['Path']).read_bytes()
        if name == 'Feature' and source['Path'] == 'Lib/Lib.csproj':
            data = data.replace(b'<DefineConstants>TRACE</DefineConstants>', b'<DefineConstants>FEATURE;TRACE</DefineConstants>')
        assert source['ContentHash'] == 'sha256:' + hashlib.sha256(data).hexdigest()
        assert source['Text'] == data.decode('utf-8-sig')
    for d in descriptors:
        observed = actual[d['DescriptorDigest']]
        options = observed['Options']
        assert d['OptionsDigest'] == h(['compctx1-options'] + [options[k] for k in (
            'OutputKind', 'NullableContextOptions', 'AllowUnsafe', 'OptimizationLevel', 'Platform',
            'CheckOverflow', 'Deterministic', 'WarningLevel', 'GeneralDiagnosticOption', 'SpecificDiagnosticOptions')])
        assert observed['LiveDependencyLinks']
        assert observed['ReferenceCount'] == len(d['References'])
        trees = {t['Path']: t for t in observed['Trees']}
        assert set(trees) == {s['Path'] for s in d['Sources']}
        for source in d['Sources']:
            tree = trees[source['Path']]
            assert tree['Text'] == snapshots[source['Path']]['Text']
            assert source['ContentHash'] == snapshots[source['Path']]['ContentHash']
            assert source['LanguageVersion'] == tree['LanguageVersion']
            assert source['PreprocessorSymbols'] == ','.join(tree['Symbols'])
        fields = [d[k] for k in ('ContractVersion', 'RepositoryId', 'ProjectUnitId', 'AssemblyName',
                                'MetadataIdentity', 'ModuleName', 'OptionsDigest')]
        fields += ['|'.join(s[k] for k in ('Path', 'ContentHash', 'LanguageVersion', 'PreprocessorSymbols'))
                   for s in d['Sources']]
        fields += d['References']
        assert d['DescriptorDigest'] == 'compctx2:sha256:' + h(['compctx2-context'] + fields)
        for reference in d['References']:
            if reference.startswith('compilation-context|'):
                assert reference.split('|', 1)[1] in {e['DescriptorDigest'] for e in descriptors}
            else:
                path, digest = reference[len('file|'):].rsplit('|', 1)
                assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == digest
    repo = 'fixture:s2.1:production'
    units = [('csharp:project:' + hashlib.sha256(p.encode()).hexdigest(), p)
             for p in ('Lib/Lib.csproj', 'App/App.csproj')]
    assert {u for u, _ in units} == {d['ProjectUnitId'] for d in descriptors}
    scope = 'analysisscope1:sha256:' + h(['analysisscope1-scope', repo] + [u + '|' + p for u, p in sorted(units)])
    assert variant['Scope'] == scope
    assert variant['Universe'] == 'compilation-universe1:sha256:' + h(
        ['compilation-universe1-universe', repo, 'compctx2', scope] +
        [d['DescriptorDigest'] for d in sorted(descriptors, key=lambda d: d['ProjectUnitId'])])
assert report['Original']['PublicApiReturnType'] == 'int'
assert report['Feature']['PublicApiReturnType'] == 'string'
assert report['Original']['Scope'] == report['Feature']['Scope']
assert report['Original']['Universe'] != report['Feature']['Universe']
for a, b in zip(report['Original']['Contexts'], report['Feature']['Contexts']):
    assert a['DescriptorDigest'] != b['DescriptorDigest']
print('PASS: exact source bytes/text/tree membership, live options digests, captured PE file hashes, four compctx2 contexts, both scope/universe tuples and dependency propagation independently recompute. Separate processes with different run IDs are identical.')
