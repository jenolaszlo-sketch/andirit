"""Q2-owned independent conflict-refusal control using the unchanged Q1 probe.

Publishes corpus A, attempts a mixed-universe completion with a transitively
changed corpus B under the identical stable target, and confirms the accepted
refusal plus preserved A publication. Writes Q2-owned evidence only.
"""
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[3]
Q1_TESTS = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests'
OUT = ROOT / 'docs/reviews/CI1A-S2.3-WP-S23-2-Q2-evidence'

SOURCES = {'Seed.cs': 'namespace Review; public static class Seed { public static int Read() => 11; }',
           'Bridge.cs': 'namespace Review; public static class Bridge { public static int Read() => Seed.Read(); }',
           'Api.cs': 'namespace Review; public static class Api { public static int Fetch() => Bridge.Read(); }',
           'Client.cs': 'namespace Review; public static class Client { public static int Go() => Api.Fetch(); }',
           'Empty.cs': 'namespace Review; public class Empty { }', 'overflow.txt': 'false'}
TRANSITIVE = {**SOURCES, 'Seed.cs': SOURCES['Seed.cs'].replace('11', '29')}


def main():
    dll = Q1_TESTS / 'bin/Release/net10.0/Andirit.CSharp.Tests.dll'
    assert dll.exists()
    with tempfile.TemporaryDirectory(prefix='andirit-q2-conflict-') as temporary:
        temporary = Path(temporary)
        corpora = []
        for name, source in [('a', SOURCES), ('b', TRANSITIVE)]:
            corpus = temporary / name
            corpus.mkdir()
            for path, text in source.items():
                (corpus / path).write_text(text, encoding='utf-8')
            corpora.append(corpus)
        database = temporary / 'conflict.ltdb'
        steps = [('rq-conflict-publish-a', 'publish', corpora[0], 'conflict-a'),
                 ('rq-conflict-mixed', 'mixed', corpora[1], 'mixed-b'),
                 ('rq-conflict-fresh-a', 'query', corpora[0], '')]
        reports = []
        for name, action, corpus, detail in steps:
            evidence = OUT / (name + '.json')
            result = subprocess.run(['dotnet', str(dll), action, str(corpus), str(database), str(evidence), detail],
                                    cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            (OUT / (name + '.log')).write_bytes(result.stdout)
            assert result.returncode == 0, name
            reports.append(json.loads(evidence.read_text(encoding='utf-8')))
        control, mixed, fresh = reports
        published = lambda report: {k: v for k, v in report['nativeRecords'].items()
                                    if k.startswith(('HetuUnit|', 'HetuIndexState|', 'AndiritOccurrence'))}
        checks = {'three_processes': len({r['processId'] for r in reports}) == 3,
                  'stable_target': control['target'] == mixed['target'],
                  'conflict_rejected': mixed['rejected'],
                  'mixed_stale': mixed['result']['Availability'] == 3,
                  'fresh_a_available': fresh['result']['Availability'] == 0 and len(fresh['result']['Occurrences']) == 1,
                  'published_rows_conserved': published(control) == published(mixed) == published(fresh)}
        (OUT / 'q2-conflict-checks.json').write_text(json.dumps(checks, indent=2) + '\n', encoding='utf-8')
        print(json.dumps(checks, indent=2))
        assert all(checks.values())


if __name__ == '__main__':
    main()
