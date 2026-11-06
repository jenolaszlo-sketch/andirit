"""WP-S23-2-R1 orphan-owner fresh-process reproduction.

Reuses the unchanged independent Q1 reviewer probe executable
(`CI1A-S2.3-WP-S23-2-Q1-tests`, rebuilt against the corrected production source)
without editing that probe or its evidence. It runs the exact three-process
counterexample from Q1 F-01 -- publish a published graph unit with occurrences,
remove only its durable HetuUnit graph owner, then read from a separate fresh
process -- and captures the before/after behavior into additive R1 evidence.
"""
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
Q1_TESTS = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests'
OUT = ROOT / 'docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence'
OUT.mkdir(exist_ok=True)

SOURCES = {
    'Seed.cs': 'namespace Review; public static class Seed { public static int Read() => 11; }',
    'Bridge.cs': 'namespace Review; public static class Bridge { public static int Read() => Seed.Read(); }',
    'Api.cs': 'namespace Review; public static class Api { public static int Fetch() => Bridge.Read(); }',
    'Client.cs': 'namespace Review; public static class Client { public static int Go() => Api.Fetch(); }',
    'Empty.cs': 'namespace Review; public class Empty { }',
    'overflow.txt': 'false',
}

COMMANDS = []


def run(name, command):
    print('RUN ' + name, flush=True)
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (OUT / (name + '.log')).write_bytes(result.stdout)
    COMMANDS.append({'name': name, 'command': [str(c) for c in command], 'exit': result.returncode})
    (OUT / 'r1-commands.json').write_text(json.dumps(COMMANDS, indent=2) + '\n', encoding='utf-8')
    print(result.stdout.decode('utf-8', errors='replace')[-600:], flush=True)
    return result.returncode


def main():
    assert run('r1-reviewer-build', ['dotnet', 'build', str(Q1_TESTS / 'ReviewerProbe.csproj'),
                                     '-c', 'Release', '--no-restore']) == 0
    dll = Q1_TESTS / 'bin/Release/net10.0/Andirit.CSharp.Tests.dll'
    with tempfile.TemporaryDirectory(prefix='andirit-r1-orphan-') as temporary:
        temporary = Path(temporary)
        corpus = temporary / 'corpus'
        corpus.mkdir()
        for path, text in SOURCES.items():
            (corpus / path).write_text(text, encoding='utf-8')
        database = temporary / 'orphan.ltdb'
        sequence = [
            ('r1-orphan-publish', 'publish', 'r1-confirm-a'),
            ('r1-orphan-remove-owner', 'mutate', 'graph-orphan'),
            ('r1-orphan-fresh-reader', 'query', ''),
        ]
        for name, action, detail in sequence:
            code = run(name, ['dotnet', str(dll), action, str(corpus), str(database),
                              str(OUT / (name + '.json')), detail])
            assert code == 0, name

    publish = json.loads((OUT / 'r1-orphan-publish.json').read_text(encoding='utf-8'))
    removed = json.loads((OUT / 'r1-orphan-remove-owner.json').read_text(encoding='utf-8'))
    fresh = json.loads((OUT / 'r1-orphan-fresh-reader.json').read_text(encoding='utf-8'))

    def published(report):
        return {k: v for k, v in report['nativeRecords'].items()
                if k.startswith(('HetuUnit|', 'HetuIndexState|', 'AndiritOccurrence'))}

    changed = [k for k in sorted(set(publish['nativeRecords']) | set(removed['nativeRecords']))
               if publish['nativeRecords'].get(k) != removed['nativeRecords'].get(k)]
    fresh_result = fresh['result']
    summary = {
        'q1_reviewed_tree': '53966a01b69c876bfff29a60c576c595bdb9e84b',
        'distinct_process_ids': len({r['processId'] for r in [publish, removed, fresh]}) == 3,
        'only_graph_owner_removed': changed,
        'mutation_and_fresh_reader_exact_rows_equal': removed['nativeRecords'] == fresh['nativeRecords'],
        'fresh_reader_graph_units': [u['unit'] for u in fresh['graphUnits']],
        'before_availability': publish['result']['Availability'],
        'before_hits': len(publish['result']['Occurrences']),
        'before_complete': publish['result']['IsComplete'],
        'after_availability': fresh_result['Availability'],
        'after_hits': len(fresh_result['Occurrences']),
        'after_examined': fresh_result['Examined'],
        'after_complete': fresh_result['IsComplete'],
        'after_refusal_reason': fresh_result['RefusalReason'],
        'expected_after': {'Availability': 4, 'hits': 0, 'Examined': 0, 'IsComplete': False},
        'corrected': (fresh_result['Availability'] == 4 and len(fresh_result['Occurrences']) == 0
                      and fresh_result['Examined'] == 0 and not fresh_result['IsComplete']
                      and fresh_result['RefusalReason'] is not None),
    }
    (OUT / 'r1-orphan-summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(summary, indent=2))
    assert summary['distinct_process_ids']
    assert changed == ['HetuUnit|review:q1-native-authority\nreview.q1.csharp\nClient.csproj']
    assert summary['corrected'], 'R1 orphan reproduction did not show the corrected refusal'


if __name__ == '__main__':
    main()
