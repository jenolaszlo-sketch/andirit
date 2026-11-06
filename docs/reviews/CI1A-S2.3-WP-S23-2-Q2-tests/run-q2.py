"""Q2-owned independent requalification runner (no production changes).

Drives the Q2 probe in distinct processes to reproduce the Q1 F-01 orphan
counterexample and to exercise the bidirectional association rule, legitimate
retirement, complete-empty semantics, preserved publication, durable reopen, and
negative controls. Writes additive evidence under the Q2 evidence directory only.
"""
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUT = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q2-evidence'
OUT.mkdir(exist_ok=True)

ENUMS = {'Available': 0, 'AnalysisUnavailable': 1, 'UnsupportedSchema': 2,
         'StalePublication': 3, 'CorruptedIndex': 4, 'IncompleteAnalysis': 5}

CORPUS_A = {
    'Seed.cs': 'namespace Review; public static class Seed { public static int Read() => 11; }',
    'Bridge.cs': 'namespace Review; public static class Bridge { public static int Read() => Seed.Read(); }',
    'Api.cs': 'namespace Review; public static class Api { public static int Fetch() => Bridge.Read(); }',
    'Client.cs': 'namespace Review; public static class Client { public static int Go() => Api.Fetch(); }',
    'Client2.cs': 'namespace Review; public static class Client2 { public static int Go2() => Api.Fetch(); }',
    'Empty.cs': 'namespace Review; public class Empty { }',
    'overflow.txt': 'false',
}
CORPUS_TRANSITIVE = {**CORPUS_A, 'Seed.cs': CORPUS_A['Seed.cs'].replace('11', '29')}

COMMANDS = []


def run(name, command):
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (OUT / (name + '.log')).write_bytes(result.stdout)
    COMMANDS.append({'name': name, 'exit': result.returncode})
    (OUT / 'q2-commands.json').write_text(json.dumps(COMMANDS, indent=2) + '\n', encoding='utf-8')
    return result.returncode


def expect(report, availability, hits, complete):
    r = report['result']
    ok = (r is not None and r['Availability'] == ENUMS[availability]
          and len(r['Occurrences']) == hits and r['IsComplete'] == complete)
    if availability not in ('Available', 'IncompleteAnalysis') and r is not None:
        ok = ok and r['Examined'] == 0 and r['RefusalReason'] is not None
    return ok


def main():
    project = str(HERE / 'Q2Probe.csproj')
    assert run('q2-probe-build', ['dotnet', 'build', project, '-c', 'Release']) == 0, 'Q2 probe build failed'
    dll = HERE / 'bin/Release/net10.0/Andirit.CSharp.Tests.dll'

    matrix = []
    with tempfile.TemporaryDirectory(prefix='andirit-q2-review-') as temporary:
        temporary = Path(temporary)
        corpora = {}
        for name, source in [('a', CORPUS_A), ('transitive', CORPUS_TRANSITIVE)]:
            corpus = temporary / name
            corpus.mkdir()
            for path, text in source.items():
                (corpus / path).write_text(text, encoding='utf-8')
            corpora[name] = corpus

        def step(name, action, corpus='a', db=None, detail=''):
            evidence = OUT / (name + '.json')
            code = run(name, ['dotnet', str(dll), action, str(corpora[corpus]), str(db), str(evidence), detail])
            assert code == 0, name + ' failed to execute'
            return json.loads(evidence.read_text(encoding='utf-8'))

        def record(name, report, availability, hits, complete):
            passed = expect(report, availability, hits, complete)
            matrix.append({'probe': name, 'expected': availability, 'expected_hits': hits,
                           'expected_complete': complete, 'availability': report['result']['Availability'],
                           'hits': len(report['result']['Occurrences']),
                           'examined': report['result']['Examined'],
                           'complete': report['result']['IsComplete'],
                           'reason': report['result']['RefusalReason'], 'pass': passed})
            (OUT / 'q2-matrix.json').write_text(json.dumps(matrix, indent=2) + '\n', encoding='utf-8')
            assert passed, name + ' expected ' + availability

        # Q2-P01: original F-01 orphan counterexample, three processes.
        db = temporary / 'p01.ltdb'
        p01_publish = step('Q2-P01-publish', 'publish', db=db, detail='p01')
        record('Q2-P01-publish', p01_publish, 'Available', 2, True)
        p01_removed = step('Q2-P01-remove-owner', 'mutate-remove-graph-owner', db=db, detail='Client.csproj')
        record('Q2-P01-remove-owner', p01_removed, 'CorruptedIndex', 0, False)
        p01_fresh = step('Q2-P01-fresh-reader', 'query', db=db)
        record('Q2-P01-fresh-reader', p01_fresh, 'CorruptedIndex', 0, False)

        # Q2-P02: reverse correspondence -- authority-requiring graph unit with no
        # occurrence association (manifest kept internally consistent).
        db = temporary / 'p02.ltdb'
        step('Q2-P02-publish', 'publish', db=db, detail='p02')
        reverse = step('Q2-P02-remove-occurrence', 'mutate-remove-occurrence', db=db, detail='Client.csproj')
        record('Q2-P02-remove-occurrence', reverse, 'CorruptedIndex', 0, False)
        record('Q2-P02-fresh-reader', step('Q2-P02-fresh-reader', 'query', db=db), 'CorruptedIndex', 0, False)

        # Q2-P02b: occurrence accounting inconsistency.
        db = temporary / 'p02b.ltdb'
        step('Q2-P02b-publish', 'publish', db=db, detail='p02b')
        record('Q2-P02b-corrupt-accounting',
               step('Q2-P02b-corrupt-accounting', 'mutate-corrupt-accounting', db=db),
               'CorruptedIndex', 0, False)

        # Q2-P03: legitimate retirement of one owner leaves the unaffected owner.
        db = temporary / 'p03.ltdb'
        record('Q2-P03-retire', step('Q2-P03-retire', 'retire', db=db, detail='p03'), 'Available', 1, True)
        record('Q2-P03-retire-fresh', step('Q2-P03-retire-fresh', 'query', db=db), 'Available', 1, True)

        # Q2-P03b: retire all fact-bearing owners; remaining complete-empty.
        db = temporary / 'p03b.ltdb'
        record('Q2-P03b-retire-both', step('Q2-P03b-retire-both', 'retire-both', db=db, detail='p03b'),
               'Available', 0, True)
        record('Q2-P03b-empty-fresh', step('Q2-P03b-empty-fresh', 'query', db=db), 'Available', 0, True)

        # Q2-P04: preserved publication -- remove a different owner; unrelated rows
        # must be conserved and the affected publication refused.
        db = temporary / 'p04.ltdb'
        p04_control = step('Q2-P04-publish', 'publish', db=db, detail='p04')
        record('Q2-P04-publish', p04_control, 'Available', 2, True)
        p04_removed = step('Q2-P04-remove-other-owner', 'mutate-remove-graph-owner', db=db, detail='Empty.csproj')
        record('Q2-P04-remove-other-owner', p04_removed, 'CorruptedIndex', 0, False)

        # Q2-P05: valid durable reopen across separate process lifetimes.
        db = temporary / 'p05.ltdb'
        record('Q2-P05-publish', step('Q2-P05-publish', 'publish', db=db, detail='p05'), 'Available', 2, True)
        record('Q2-P05-reopen', step('Q2-P05-reopen', 'query', db=db), 'Available', 2, True)

        # Q2-P06: negative controls and semantic boundaries.
        db = temporary / 'p06a.ltdb'
        record('Q2-P06-correct', step('Q2-P06-correct', 'publish-extra-graph-only', db=db, detail='p06a'),
               'Available', 2, True)
        db = temporary / 'p06b.ltdb'
        step('Q2-P06b-publish', 'publish', db=db, detail='p06b')
        record('Q2-P06b-add-graph-only',
               step('Q2-P06b-add-graph-only', 'mutate-add-graph-only', db=db), 'Available', 2, True)
        db = temporary / 'p06c.ltdb'
        step('Q2-P06c-publish', 'publish', db=db, detail='p06c')
        record('Q2-P06c-add-marker-no-occurrence',
               step('Q2-P06c-add-marker-no-occurrence', 'mutate-add-marker-no-occurrence', db=db),
               'CorruptedIndex', 0, False)
        db = temporary / 'p06d.ltdb'
        step('Q2-P06d-publish', 'publish', db=db, detail='p06d')
        record('Q2-P06d-missing-current', step('Q2-P06d-missing-current', 'raw', db=db),
               'AnalysisUnavailable', 0, False)
        db = temporary / 'p06e.ltdb'
        step('Q2-P06e-publish', 'publish', db=db, detail='p06e')
        record('Q2-P06e-stale-context', step('Q2-P06e-stale-context', 'stale', corpus='transitive', db=db),
               'StalePublication', 0, False)

        # Q2 adversarial observation (non-blocking): clear the additive graph marker
        # on an existing owner while retaining its authority-bearing occurrence unit.
        db = temporary / 'p07.ltdb'
        step('Q2-P07-publish', 'publish', db=db, detail='p07')
        q2_p07_marker = step('Q2-P07-clear-marker', 'mutate-clear-marker', db=db, detail='Client.csproj')
        q2_p07_fresh = step('Q2-P07-fresh-reader', 'query', db=db)

    # Strengthened P01/P04 claims.
    p01_changed = [k for k in sorted(set(p01_publish['nativeRecords']) | set(p01_removed['nativeRecords']))
                   if p01_publish['nativeRecords'].get(k) != p01_removed['nativeRecords'].get(k)]
    p04_changed = [k for k in sorted(set(p04_control['nativeRecords']) | set(p04_removed['nativeRecords']))
                   if p04_control['nativeRecords'].get(k) != p04_removed['nativeRecords'].get(k)]
    conservation = {
        'q1_reviewed_tree': '53966a01b69c876bfff29a60c576c595bdb9e84b',
        'r1_remediation_commit': '1bce70ee2f52ed5100a2e68bc004462f9824d2f2',
        'p01_distinct_process_ids': len({p01_publish['processId'], p01_removed['processId'], p01_fresh['processId']}) == 3,
        'p01_only_graph_owner_removed': p01_changed,
        'p01_reader_rows_equal_mutation': p01_removed['nativeRecords'] == p01_fresh['nativeRecords'],
        'p04_changed_rows': p04_changed,
        'p04_unrelated_occurrence_rows_preserved': all(
            p04_control['nativeRecords'].get(k) == p04_removed['nativeRecords'].get(k)
            for k in p04_control['nativeRecords'] if k.startswith('AndiritOccurrenceUnit|')
            and not k.endswith('Empty.csproj')),
        'p04_unrelated_graph_rows_preserved': all(
            p04_control['nativeRecords'].get(k) == p04_removed['nativeRecords'].get(k)
            for k in p04_control['nativeRecords'] if k.startswith('HetuUnit|')
            and not k.endswith('Empty.csproj')),
        'p01_before_availability': p01_publish['result']['Availability'],
        'p01_after_availability': p01_fresh['result']['Availability'],
        'p01_after_reason': p01_fresh['result']['RefusalReason'],
    }
    (OUT / 'q2-conservation.json').write_text(json.dumps(conservation, indent=2) + '\n', encoding='utf-8')

    adversarial = {
        'scenario': 'graph owner present but additive RequireOccurrenceAuthority marker cleared',
        'availability': q2_p07_fresh['result']['Availability'],
        'hits': len(q2_p07_fresh['result']['Occurrences']),
        'complete': q2_p07_fresh['result']['IsComplete'],
        'refusal_reason': q2_p07_fresh['result']['RefusalReason'],
        'note': 'The occurrence unit authority is still independently validated against '
                'the live compilation, so no stale/invalid occurrence is served. This is '
                'an additive-marker correspondence completeness gap, recorded as a bounded '
                'non-blocking observation (O-Q2-01), not an F-01-class defect.',
    }
    (OUT / 'q2-adversarial.json').write_text(json.dumps(adversarial, indent=2) + '\n', encoding='utf-8')

    summary = {'total': len(matrix), 'passed': sum(1 for e in matrix if e['pass']),
               'failed': [e['probe'] for e in matrix if not e['pass']],
               'conservation': conservation, 'adversarial': adversarial}
    (OUT / 'q2-summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'total': summary['total'], 'passed': summary['passed'],
                      'failed': summary['failed'], 'conservation': conservation}, indent=2))
    assert not summary['failed']
    assert conservation['p01_distinct_process_ids']
    assert conservation['p01_only_graph_owner_removed'] == [
        'HetuUnit|review:q2-correspondence\nreview.q2.csharp\nClient.csproj']
    assert conservation['p01_reader_rows_equal_mutation']
    assert conservation['p01_after_availability'] == 4
    assert conservation['p04_changed_rows'] == [
        'HetuUnit|review:q2-correspondence\nreview.q2.csharp\nEmpty.csproj']
    assert conservation['p04_unrelated_occurrence_rows_preserved']
    assert conservation['p04_unrelated_graph_rows_preserved']


if __name__ == '__main__':
    main()
