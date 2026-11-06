"""WP-S23-2-R1 preserved-behavior fresh-process controls.

Runs representative independent Q1 probe scenarios (unmodified probe executable,
rebuilt against the corrected production source) to demonstrate that the
ownership cross-check does not over-refuse and that previously accepted statuses
are preserved. Outputs are additive under the R1 evidence directory only.
"""
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[3]
Q1_TESTS = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests'
OUT = ROOT / 'docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence'
OUT.mkdir(exist_ok=True)

ENUMS = {'Available': 0, 'AnalysisUnavailable': 1, 'UnsupportedSchema': 2,
         'StalePublication': 3, 'CorruptedIndex': 4, 'IncompleteAnalysis': 5}

SOURCE_A = {
    'Seed.cs': 'namespace Review; public static class Seed { public static int Read() => 11; }',
    'Bridge.cs': 'namespace Review; public static class Bridge { public static int Read() => Seed.Read(); }',
    'Api.cs': 'namespace Review; public static class Api { public static int Fetch() => Bridge.Read(); }',
    'Client.cs': 'namespace Review; public static class Client { public static int Go() => Api.Fetch(); }',
    'Empty.cs': 'namespace Review; public class Empty { }',
    'overflow.txt': 'false',
}
SOURCE_TRANSITIVE = {**SOURCE_A, 'Seed.cs': SOURCE_A['Seed.cs'].replace('11', '29')}

COMMANDS = []


def run(name, command):
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (OUT / (name + '.log')).write_bytes(result.stdout)
    COMMANDS.append({'name': name, 'exit': result.returncode})
    (OUT / 'r1-control-commands.json').write_text(json.dumps(COMMANDS, indent=2) + '\n', encoding='utf-8')
    return result.returncode


def main():
    dll = Q1_TESTS / 'bin/Release/net10.0/Andirit.CSharp.Tests.dll'
    assert dll.exists(), 'Build the Q1 ReviewerProbe first'
    matrix = []
    with tempfile.TemporaryDirectory(prefix='andirit-r1-controls-') as temporary:
        temporary = Path(temporary)
        corpora = {}
        for name, source in [('a', SOURCE_A), ('transitive', SOURCE_TRANSITIVE)]:
            corpus = temporary / name
            corpus.mkdir()
            for path, text in source.items():
                (corpus / path).write_text(text, encoding='utf-8')
            corpora[name] = corpus

        def invoke(name, action, variant='a', db=None, detail='', expected='Available',
                   hits=1, complete=True):
            db = db or (temporary / 'sequence.ltdb')
            evidence = OUT / (name + '.json')
            code = run(name, ['dotnet', str(dll), action, str(corpora[variant]), str(db),
                              str(evidence), detail])
            observed = json.loads(evidence.read_text(encoding='utf-8')) if code == 0 else {}
            result = observed.get('result')
            passed = (code == 0 and result is not None and
                      result['Availability'] == ENUMS[expected] and
                      len(result['Occurrences']) == hits and result['IsComplete'] == complete)
            if expected not in ('Available', 'IncompleteAnalysis') and result is not None:
                passed &= result['Examined'] == 0
            matrix.append({'probe': name, 'action': action, 'expected': expected,
                           'expected_hits': hits, 'expected_complete': complete,
                           'actual': result, 'pass': passed})
            (OUT / 'r1-control-matrix.json').write_text(json.dumps(matrix, indent=2) + '\n', encoding='utf-8')
            assert passed, name + ' expected ' + expected

        # Matching reopen / stale transitive refusal.
        invoke('r1-control-publish-a', 'publish', detail='a')
        invoke('r1-control-fresh-a', 'query')
        invoke('r1-control-transitive-stale', 'query', 'transitive',
               expected='StalePublication', hits=0, complete=False)
        invoke('r1-control-no-current', 'raw', expected='AnalysisUnavailable', hits=0, complete=False)

        # Accepted retirement of one unit leaves the remaining publication valid.
        retire_db = temporary / 'retire.ltdb'
        invoke('r1-control-retire-setup', 'publish', db=retire_db, detail='retire-a')
        invoke('r1-control-retire-client', 'retire', db=retire_db, detail='retired', hits=0)
        invoke('r1-control-retired-fresh', 'query', db=retire_db, hits=0)

        # Graph-only and complete-empty distinctions.
        graph_db = temporary / 'graph.ltdb'
        invoke('r1-control-graph-only', 'graph-only', db=graph_db, detail='graph-only',
               expected='AnalysisUnavailable', hits=0, complete=False)
        empty_db = temporary / 'empty.ltdb'
        invoke('r1-control-empty', 'empty', db=empty_db, detail='empty', hits=0)
        invoke('r1-control-empty-missing-current', 'raw', db=empty_db,
               expected='AnalysisUnavailable', hits=0, complete=False)
        invoke('r1-control-empty-stale', 'query', 'transitive', db=empty_db,
               expected='StalePublication', hits=0, complete=False)

        # Staged resumption remains conservatively unavailable (Q1-Q1).
        resume_db = temporary / 'resume.ltdb'
        invoke('r1-control-stage', 'stage', db=resume_db, detail='stage',
               expected='AnalysisUnavailable', hits=0, complete=False)
        invoke('r1-control-resume', 'resume', db=resume_db, detail='stage',
               expected='AnalysisUnavailable', hits=0, complete=False)

        # Retained-owner resumption across a store lifetime.
        retained_db = temporary / 'retained.ltdb'
        invoke('r1-control-retained-setup', 'publish', db=retained_db, detail='retained-a')
        invoke('r1-control-retained-stage', 'stage-retain', db=retained_db, detail='retained-resume')
        invoke('r1-control-retained-resume', 'resume', db=retained_db, detail='retained-resume',
               expected='AnalysisUnavailable', hits=0, complete=False)
        invoke('r1-control-retained-fresh', 'query', db=retained_db,
               expected='AnalysisUnavailable', hits=0, complete=False)

        # Representative per-record mutation refusals remain unchanged.
        mutations = {'missing': 'AnalysisUnavailable', 'malformed': 'CorruptedIndex',
                     'unsupported': 'UnsupportedSchema', 'forged-context': 'StalePublication',
                     'wrong-owner': 'CorruptedIndex', 'wrong-payload': 'CorruptedIndex',
                     'wrong-publication': 'StalePublication', 'meta-snapshot': 'StalePublication'}
        for mutation, expected in mutations.items():
            db = temporary / (mutation + '.ltdb')
            invoke('r1-control-' + mutation + '-publish', 'publish', db=db, detail='mutation-a')
            invoke('r1-control-' + mutation + '-mutate', 'mutate', db=db, detail=mutation,
                   expected=expected, hits=0, complete=False)

        # P38/O-01: a damaged native page ID list does not govern reconstructed
        # hits; unit facts rebuild the index, so the accepted result is unchanged.
        page_db = temporary / 'page-missing-id.ltdb'
        invoke('r1-control-page-missing-id-publish', 'publish', db=page_db, detail='mutation-a')
        invoke('r1-control-page-missing-id-mutate', 'mutate', db=page_db, detail='page-missing-id',
               expected='Available', hits=1, complete=True)

    passed = sum(1 for e in matrix if e['pass'])
    summary = {'total': len(matrix), 'passed': passed,
               'failed': [e['probe'] for e in matrix if not e['pass']]}
    (OUT / 'r1-control-summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    main()
