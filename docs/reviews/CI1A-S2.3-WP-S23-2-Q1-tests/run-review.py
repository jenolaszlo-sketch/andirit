"""Additive independent qualification runner. Never rewrites producer evidence."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import sys

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUT = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q1-evidence'
OUT.mkdir(exist_ok=True)
COMMANDS = json.loads((OUT / 'commands.json').read_text()) if (OUT / 'commands.json').exists() else []
REVIEWED = '0f124eae8ba70bab3a6eaf4c2cd1e049f3435a51'
BASE = 'b576acb004747c89d257d8e61721c17babe6c5a6'


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)


def save(name, data):
    (OUT / name).write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')


def run(name, command):
    print('RUN ' + name, flush=True)
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (OUT / (name + '.log')).write_bytes(result.stdout)
    COMMANDS.append({'name': name, 'command': command, 'exit': result.returncode})
    save('commands.json', COMMANDS)
    print(result.stdout.decode('utf-8', errors='replace')[-1800:], flush=True)
    return result.returncode


def integrity():
    producer = 'docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-IMPL-01-evidence/'
    manifest = json.loads(git('show', REVIEWED + ':' + producer + 'manifest.json'))
    rows = []
    for entry in manifest['files']:
        path = producer + entry['path']
        blob = git('show', REVIEWED + ':' + path)
        actual = hashlib.sha256(blob).hexdigest().upper()
        rows.append({**entry, 'git_blob': git('rev-parse', REVIEWED + ':' + path).decode().strip(),
                     'actual_bytes': len(blob), 'actual_sha256': actual,
                     'pass': len(blob) == entry['bytes'] and actual == entry['sha256']})
    paths = {p[len(producer):] for p in git('ls-tree', '-r', '--name-only', REVIEWED, producer).decode().splitlines()}
    inventory = paths == {e['path'] for e in manifest['files']} | {'manifest.json'}
    authority = []
    for entry in json.loads(git('show', REVIEWED + ':' + producer + 'authority-inputs.json')):
        blob = git('show', REVIEWED + ':' + entry['path'])
        baseline = git('show', BASE + ':' + entry['path'])
        working = (ROOT / entry['path']).read_bytes()
        authority.append({**entry, 'git_sha256': hashlib.sha256(blob).hexdigest().upper(),
                          'baseline_equal': blob == baseline,
                          'worktree_equal': working == blob,
                          'worktree_hash_equal': hashlib.sha256(working).hexdigest().upper() == entry['sha256'],
                          'hash_equal': hashlib.sha256(blob).hexdigest().upper() == entry['sha256']})
    source = json.loads(git('show', REVIEWED + ':' + producer + 'source-binding.json'))
    bindings = []
    for entry in source['files']:
        blob = git('show', source['implementation_commit'] + ':' + entry['path'])
        bindings.append({'path': entry['path'], 'pass': len(blob) == entry['bytes'] and
                         hashlib.sha256(blob).hexdigest().upper() == entry['git_blob_sha256'] and
                         git('rev-parse', source['implementation_commit'] + ':' + entry['path']).decode().strip() == entry['git_blob_sha1']})
    protected = ['docs/decisions', 'docs/planning', 'docs/evidence', 'src/Andirit.Abstractions',
                 'src/Andirit.CSharp/VerifiedCSharpCompilation.cs', 'src/Andirit.CSharp/CompilationProvenanceDescriptor.cs',
                 'src/Andirit.CSharp/VerifiedCSharpSource.cs', 'docs/reviews/CI1A-S2.3-NEW-01-Q1-tests',
                 'docs/reviews/CI1A-S2.3-NEW-01-Q1-evidence']
    changes = git('diff', '--name-only', BASE, REVIEWED, '--', *protected).decode().splitlines()
    save('provenance.json', {'reviewed': REVIEWED, 'reviewed_tree': git('rev-parse', REVIEWED + '^{tree}').decode().strip(),
         'baseline': BASE, 'worktrees': git('worktree', 'list', '--porcelain').decode(),
         'manifest_entries': rows, 'inventory_equal': inventory, 'authority_inputs': authority,
         'source_bindings': bindings, 'protected_changes': changes,
         'production_diff_sha256': hashlib.sha256(git('diff', BASE, '51369b3', '--', 'src')).hexdigest()})
    (OUT / 'production-diff.txt').write_bytes(git('diff', BASE, '51369b3', '--', 'src'))
    assert len(rows) == 38 and all(e['pass'] for e in rows) and inventory
    # Retain raw mismatches as findings; never normalize or silently repair evidence.
    assert all(e['baseline_equal'] and e['worktree_hash_equal'] for e in authority)
    assert all(e['pass'] for e in bindings) and not changes
    print(f'Exact Git-byte manifest PASS: {len(rows)} entries, {len(bindings)} source bindings; '
          f'authority Git hash mismatches: {sum(not e["hash_equal"] for e in authority)} of {len(authority)}', flush=True)


def processes():
    project = str(HERE / 'ReviewerProbe.csproj')
    assert run('reviewer-build', ['dotnet', 'build', project, '-c', 'Release']) == 0
    dll = str(HERE / 'bin/Release/net10.0/Andirit.CSharp.Tests.dll')
    matrix = []
    with tempfile.TemporaryDirectory(prefix='andirit-q1-review-') as temporary:
        temporary = Path(temporary)
        corpora = {}
        sources = {'Seed.cs': 'namespace Review; public static class Seed { public static int Read() => 11; }',
                   'Bridge.cs': 'namespace Review; public static class Bridge { public static int Read() => Seed.Read(); }',
                   'Api.cs': 'namespace Review; public static class Api { public static int Fetch() => Bridge.Read(); }',
                   'Client.cs': 'namespace Review; public static class Client { public static int Go() => Api.Fetch(); }',
                   'Empty.cs': 'namespace Review; public class Empty { }', 'overflow.txt': 'false'}
        for variant in ['a', 'transitive', 'options', 'direct']:
            corpus = temporary / variant
            corpus.mkdir()
            for path, text in sources.items():
                if variant == 'transitive' and path == 'Seed.cs': text = text.replace('11', '29')
                if variant == 'options' and path == 'overflow.txt': text = 'true'
                if variant == 'direct' and path == 'Api.cs': text = text.replace('Bridge.Read()', 'Bridge.Read() + 1')
                (corpus / path).write_text(text, encoding='utf-8')
            corpora[variant] = corpus
        database = temporary / 'sequence.ltdb'

        def invoke(name, action, variant='a', db=database, detail='', expected='Available', hits=1, complete=True, strict=True):
            evidence = OUT / (name + '.json')
            code = run(name, ['dotnet', dll, action, str(corpora[variant]), str(db), str(evidence), detail])
            if code != 0:
                matrix.append({'probe': name, 'exit': code, 'pass': False})
                save('probe-matrix.json', matrix)
                raise RuntimeError(name + ' failed to execute')
            observed = json.loads(evidence.read_text(encoding='utf-8'))
            result = observed['result']
            passed = result is not None and result['Availability'] == expected and len(result['Occurrences']) == hits and result['IsComplete'] == complete
            # Enum names are recorded as integer by System.Text.Json.
            enums = {'Available': 0, 'AnalysisUnavailable': 1, 'UnsupportedSchema': 2, 'StalePublication': 3, 'CorruptedIndex': 4, 'IncompleteAnalysis': 5}
            passed = result is not None and result['Availability'] == enums[expected] and len(result['Occurrences']) == hits and result['IsComplete'] == complete
            if expected not in ['Available', 'IncompleteAnalysis'] and result is not None: passed &= result['Examined'] == 0
            matrix.append({'probe': name, 'expected': expected, 'expected_hits': hits, 'expected_complete': complete,
                           'actual': result, 'open_error': observed['openError'], 'pass': passed, 'strict': strict})
            save('probe-matrix.json', matrix)
            if strict and not passed: raise AssertionError(name)
            return observed

        a = invoke('P01-publish-a', 'publish', detail='a')
        fresh = invoke('P02-fresh-a', 'query')
        invoke('P03-no-current', 'raw', expected='AnalysisUnavailable', hits=0, complete=False)
        invoke('P04-substitute-live-caller', 'substitute')
        transitive = invoke('P05-transitive-only', 'query', 'transitive', expected='StalePublication', hits=0, complete=False)
        options = invoke('P06-options-only', 'query', 'options', expected='StalePublication', hits=0, complete=False)
        invoke('P07-direct-change', 'query', 'direct', expected='StalePublication', hits=0, complete=False)
        failed = invoke('P08-before-commit-failure', 'fault', 'transitive', detail='failed-b', expected='StalePublication', hits=0, complete=False)
        recovered = invoke('P09-a-after-failure', 'query')
        invoke('P10-aborted-b', 'abort', 'transitive', detail='aborted-b', expected='StalePublication', hits=0, complete=False)
        invoke('P11-a-after-abort', 'query')
        b = invoke('P12-publish-b', 'publish', 'transitive', detail='b')
        invoke('P13-fresh-b', 'query', 'transitive')
        invoke('P14-old-a-after-b', 'query', expected='StalePublication', hits=0, complete=False)
        invoke('P15-unknown-outcome', 'unknown', 'options', detail='unknown-options')
        invoke('P16-options-fresh', 'query', 'options')
        retired_db = temporary / 'retired.ltdb'
        invoke('P17-retire-setup', 'publish', db=retired_db, detail='retire-a')
        invoke('P18-retire-client', 'retire', db=retired_db, detail='retired', hits=0)
        invoke('P19-retired-fresh', 'query', db=retired_db, hits=0)
        graph_db = temporary / 'graph-only.ltdb'
        invoke('P20-graph-only', 'graph-only', db=graph_db, detail='graph-only', expected='AnalysisUnavailable', hits=0, complete=False)
        empty_db = temporary / 'empty.ltdb'
        invoke('P21-empty', 'empty', db=empty_db, detail='empty', hits=0)
        invoke('P22-empty-missing-current', 'raw', db=empty_db, expected='AnalysisUnavailable', hits=0, complete=False)
        invoke('P23-empty-stale', 'query', 'transitive', db=empty_db, expected='StalePublication', hits=0, complete=False)
        stage_db = temporary / 'resume.ltdb'
        invoke('P24-stage-process', 'stage', db=stage_db, detail='stage', expected='AnalysisUnavailable', hits=0, complete=False)
        invoke('P25-resume-process', 'resume', db=stage_db, detail='stage', expected='AnalysisUnavailable', hits=0, complete=False)
        invoke('P26-resume-raw', 'raw', db=stage_db, expected='AnalysisUnavailable', hits=0, complete=False)
        mutations = {'missing': 'AnalysisUnavailable', 'null': 'AnalysisUnavailable', 'malformed': 'CorruptedIndex',
                     'unsupported': 'UnsupportedSchema', 'forged-context': 'StalePublication', 'null-receipts': 'CorruptedIndex',
                     'wrong-owner': 'CorruptedIndex', 'wrong-payload': 'CorruptedIndex', 'wrong-publication': 'StalePublication',
                     'graph-orphan': 'CorruptedIndex', 'meta-snapshot': 'StalePublication', 'page-missing-id': 'CorruptedIndex'}
        for number, (mutation, expected) in enumerate(mutations.items(), 27):
            db = temporary / (mutation + '.ltdb')
            invoke(f'C{number}-control-{mutation}', 'publish', db=db, detail='mutation-a')
            invoke(f'P{number}-{mutation}', 'mutate', db=db, detail=mutation, expected=expected, hits=0, complete=False, strict=False)
        partial_db = temporary / 'partial.ltdb'
        invoke('P39-partial-control', 'partial', db=partial_db, detail='partial', expected='IncompleteAnalysis', complete=False)
        invoke('P40-meta-complete', 'mutate', db=partial_db, detail='meta-complete', expected='CorruptedIndex', hits=0, complete=False, strict=False)
        retained_db = temporary / 'retained-resume.ltdb'
        invoke('P41-retained-setup', 'publish', db=retained_db, detail='retained-a')
        invoke('P42-retained-stage', 'stage-retain', db=retained_db, detail='retained-resume')
        invoke('P43-retained-resume', 'resume', db=retained_db, detail='retained-resume', expected='AnalysisUnavailable', hits=0, complete=False, strict=False)
        invoke('P44-retained-fresh', 'query', db=retained_db, expected='AnalysisUnavailable', hits=0, complete=False, strict=False)
        protected_labels = ('HetuUnit|', 'HetuIndexState|', 'AndiritOccurrence')
        protected = lambda report: {k: v for k, v in report['nativeRecords'].items() if k.startswith(protected_labels)}
        relationships = {'matching_reopen_exact_rows': protected(a) == protected(fresh),
                         'transitive_stable_target': a['target'] == transitive['target'],
                         'options_stable_target': a['target'] == options['target'],
                         'transitive_changed_context': a['definingCompctx2'] != transitive['definingCompctx2'],
                         'options_changed_context': a['definingCompctx2'] != options['definingCompctx2'],
                         'stale_read_exact_rows': protected(a) == protected(transitive) == protected(options),
                         'failed_commit_exact_published_rows': protected(a) == protected(failed) == protected(recovered),
                         'fresh_b_new_context': b['definingCompctx2'] == transitive['definingCompctx2'],
                         'fresh_b_stable_target': b['target'] == a['target']}
        save('relationships.json', relationships)
        assert all(relationships.values())
    print('PROBES COMPLETE: ' + str(sum(e['pass'] for e in matrix)) + '/' + str(len(matrix)) + ' expectations met', flush=True)


def regressions():
    gates = [
        ('environment', ['dotnet', '--info']),
        ('solution-tests', ['dotnet', 'test', 'Andirit.slnx', '-c', 'Release', '--no-restore']),
        ('new01-tests', ['dotnet', 'test', 'docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj', '-c', 'Release', '--no-restore']),
        ('oracle-r2', ['dotnet', 'run', '--project', 'docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj', '-c', 'Release', '--no-restore', '--',
                       '--corpus', 'docs/planning/adr-ci1-01-ci1a-s2-0/corpus', '--expected', 'docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json', '--output', str(OUT / 'oracle-r2.json')]),
        ('format', ['dotnet', 'format', 'Andirit.slnx', '--verify-no-changes', '--no-restore']),
        ('build', ['dotnet', 'build', 'Andirit.slnx', '-c', 'Release', '--no-restore']),
    ]
    for name, command in gates: run(name, command)
    oracle = OUT / 'oracle-r2.json'
    save('oracle-integrity.json', {'bytes': oracle.stat().st_size, 'sha256': hashlib.sha256(oracle.read_bytes()).hexdigest().upper(),
         'expected_observed_sha256': '97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9'})


def confirm_orphan():
    """Repeat the blocking counterexample with an additional separate reader process."""
    observed = json.loads((OUT / 'C36-control-graph-orphan.json').read_text())
    dll = str(HERE / 'bin/Release/net10.0/Andirit.CSharp.Tests.dll')
    with tempfile.TemporaryDirectory(prefix='andirit-q1-orphan-confirm-') as temporary:
        corpus = Path(temporary) / 'corpus'
        corpus.mkdir()
        for unit in observed['inputs']:
            for source in unit['sources']:
                (corpus / source['Path']).write_text(source['text'], encoding='utf-8')
        (corpus / 'overflow.txt').write_text('false', encoding='utf-8')
        database = Path(temporary) / 'orphan.ltdb'
        for name, action, detail in [('confirm-publish', 'publish', 'confirm-a'),
                                     ('confirm-remove-graph-owner', 'mutate', 'graph-orphan'),
                                     ('confirm-separate-reader', 'query', '')]:
            assert run(name, ['dotnet', dll, action, str(corpus), str(database), str(OUT / (name + '.json')), detail]) == 0
        control = json.loads((OUT / 'confirm-publish.json').read_text())
        removed = json.loads((OUT / 'confirm-remove-graph-owner.json').read_text())
        fresh = json.loads((OUT / 'confirm-separate-reader.json').read_text())
        save('orphan-confirmation.json', {
            'distinct_process_ids': len({r['processId'] for r in [control, removed, fresh]}) == 3,
            'only_graph_owner_removed': [k for k in sorted(set(control['nativeRecords']) | set(removed['nativeRecords']))
                                        if control['nativeRecords'].get(k) != removed['nativeRecords'].get(k)],
            'mutation_and_fresh_reader_exact_rows_equal': removed['nativeRecords'] == fresh['nativeRecords'],
            'expected': 'Explicit refusal, no occurrence hits, no successful completeness',
            'actual': fresh['result'], 'defect_reproduced': fresh['result']['Availability'] == 0 and fresh['result']['IsComplete']
        })


def conflict():
    """Independent stable-target mixed-universe publication and rollback control."""
    assert run('reviewer-conflict-build', ['dotnet', 'build', str(HERE / 'ReviewerProbe.csproj'), '-c', 'Release', '--no-restore']) == 0
    a = json.loads((OUT / 'P01-publish-a.json').read_text())
    b = json.loads((OUT / 'P05-transitive-only.json').read_text())
    dll = str(HERE / 'bin/Release/net10.0/Andirit.CSharp.Tests.dll')
    with tempfile.TemporaryDirectory(prefix='andirit-q1-conflict-') as temporary:
        corpora = []
        for name, report in [('a', a), ('b', b)]:
            corpus = Path(temporary) / name
            corpus.mkdir()
            for unit in report['inputs']:
                for source in unit['sources']:
                    (corpus / source['Path']).write_text(source['text'], encoding='utf-8')
            (corpus / 'overflow.txt').write_text('false', encoding='utf-8')
            corpora.append(corpus)
        database = Path(temporary) / 'conflict.ltdb'
        for name, action, corpus, detail in [('conflict-publish-a', 'publish', corpora[0], 'conflict-a'),
                                            ('conflict-mixed', 'mixed', corpora[1], 'mixed-b'),
                                            ('conflict-fresh-a', 'query', corpora[0], '')]:
            assert run(name, ['dotnet', dll, action, str(corpus), str(database), str(OUT / (name + '.json')), detail]) == 0
        control, mixed, fresh = [json.loads((OUT / (name + '.json')).read_text())
                                 for name in ['conflict-publish-a', 'conflict-mixed', 'conflict-fresh-a']]
        published = lambda report: {k: v for k, v in report['nativeRecords'].items()
                                    if k.startswith(('HetuUnit|', 'HetuIndexState|', 'AndiritOccurrence'))}
        checks = {'three_processes': len({r['processId'] for r in [control, mixed, fresh]}) == 3,
                  'stable_target': control['target'] == mixed['target'],
                  'different_defining_context': control['definingCompctx2'] != mixed['definingCompctx2'],
                  'conflict_rejected': mixed['rejected'], 'b_stale': mixed['result']['Availability'] == 3,
                  'a_still_usable': fresh['result']['Availability'] == 0 and len(fresh['result']['Occurrences']) == 1,
                  'published_rows_conserved': published(control) == published(mixed) == published(fresh)}
        save('conflict-checks.json', checks)
        assert all(checks.values())


if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'all'
    if mode in ['all', 'integrity']: integrity()
    if mode in ['all', 'processes']: processes()
    if mode in ['all', 'regressions']: regressions()
    if mode == 'confirm-orphan': confirm_orphan()
    if mode == 'conflict': conflict()
