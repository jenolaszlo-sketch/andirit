"""Q2-owned re-execution of the full Q1 56-invocation probe matrix.

Uses the unchanged Q1 reviewer probe executable, rebuilt against the corrected
production source, and writes all outputs into the additively-owned Q2 evidence
directory (names prefixed `rq-`). No Q1 artifact is read-modify-written.
"""
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[3]
Q1_TESTS = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests'
OUT = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q2-evidence'
OUT.mkdir(exist_ok=True)

ENUMS = {'Available': 0, 'AnalysisUnavailable': 1, 'UnsupportedSchema': 2,
         'StalePublication': 3, 'CorruptedIndex': 4, 'IncompleteAnalysis': 5}

COMMANDS = []


def run(name, command):
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (OUT / (name + '.log')).write_bytes(result.stdout)
    COMMANDS.append({'name': name, 'command': command, 'exit': result.returncode})
    (OUT / 'q2-q1-commands.json').write_text(json.dumps(COMMANDS, indent=2) + '\n', encoding='utf-8')
    return result.returncode


def main():
    project = str(Q1_TESTS / 'ReviewerProbe.csproj')
    assert run('q2-q1-reviewer-build', ['dotnet', 'build', project, '-c', 'Release']) == 0
    dll = str(Q1_TESTS / 'bin/Release/net10.0/Andirit.CSharp.Tests.dll')
    matrix = []
    with tempfile.TemporaryDirectory(prefix='andirit-q2-matrix-') as temporary:
        temporary = Path(temporary)
        sources = {'Seed.cs': 'namespace Review; public static class Seed { public static int Read() => 11; }',
                   'Bridge.cs': 'namespace Review; public static class Bridge { public static int Read() => Seed.Read(); }',
                   'Api.cs': 'namespace Review; public static class Api { public static int Fetch() => Bridge.Read(); }',
                   'Client.cs': 'namespace Review; public static class Client { public static int Go() => Api.Fetch(); }',
                   'Empty.cs': 'namespace Review; public class Empty { }', 'overflow.txt': 'false'}
        corpora = {}
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

        def invoke(name, action, variant='a', db=database, detail='', expected='Available',
                   hits=1, complete=True, strict=True):
            name = 'rq-' + name
            evidence = OUT / (name + '.json')
            code = run(name, ['dotnet', dll, action, str(corpora[variant]), str(db), str(evidence), detail])
            if code != 0:
                matrix.append({'probe': name, 'exit': code, 'pass': False, 'strict': strict})
                (OUT / 'q2-q1-matrix.json').write_text(json.dumps(matrix, indent=2) + '\n', encoding='utf-8')
                raise RuntimeError(name + ' failed to execute')
            observed = json.loads(evidence.read_text(encoding='utf-8'))
            result = observed['result']
            passed = (result is not None and result['Availability'] == ENUMS[expected]
                      and len(result['Occurrences']) == hits and result['IsComplete'] == complete)
            if expected not in ['Available', 'IncompleteAnalysis'] and result is not None:
                passed &= result['Examined'] == 0
            matrix.append({'probe': name, 'expected': expected, 'expected_hits': hits,
                           'expected_complete': complete, 'actual': result,
                           'open_error': observed['openError'], 'pass': passed, 'strict': strict})
            (OUT / 'q2-q1-matrix.json').write_text(json.dumps(matrix, indent=2) + '\n', encoding='utf-8')
            if strict and not passed:
                raise AssertionError(name)
            return observed

        a = invoke('P01-publish-a', 'publish', detail='a')
        fresh = invoke('P02-fresh-a', 'query')
        invoke('P03-no-current', 'raw', expected='AnalysisUnavailable', hits=0, complete=False)
        invoke('P04-substitute-live-caller', 'substitute')
        transitive = invoke('P05-transitive-only', 'query', 'transitive', expected='StalePublication', hits=0, complete=False)
        options = invoke('P06-options-only', 'query', 'options', expected='StalePublication', hits=0, complete=False)
        invoke('P07-direct-change', 'query', 'direct', expected='StalePublication', hits=0, complete=False)
        invoke('P08-before-commit-failure', 'fault', 'transitive', detail='failed-b', expected='StalePublication', hits=0, complete=False)
        invoke('P09-a-after-failure', 'query')
        invoke('P10-aborted-b', 'abort', 'transitive', detail='aborted-b', expected='StalePublication', hits=0, complete=False)
        invoke('P11-a-after-abort', 'query')
        invoke('P12-publish-b', 'publish', 'transitive', detail='b')
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
                         'fresh_b_stable_target': a['target'] == transitive['target']}
        (OUT / 'q2-q1-relationships.json').write_text(json.dumps(relationships, indent=2) + '\n', encoding='utf-8')
        assert all(relationships.values())

    total = len(matrix)
    met = sum(1 for e in matrix if e['pass'])
    strict_failures = [e['probe'] for e in matrix if not e['pass'] and e['strict']]
    nonstrict_unmet = [e['probe'] for e in matrix if not e['pass'] and not e['strict']]
    summary = {'total': total, 'predicates_met': met, 'strict_failures': strict_failures,
               'nonstrict_unmet': nonstrict_unmet}
    (OUT / 'q2-q1-summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(summary, indent=2))
    assert not strict_failures


if __name__ == '__main__':
    main()
