from pathlib import Path
import json,sys
p=Path(sys.argv[1])
for profile in ['projection','native']:
    baseline=json.loads((p/f'baseline-{profile}.json').read_text())
    for phase in ['reopen','repeat','cancel','after']:
        candidate=json.loads((p/f'{phase}-{profile}.json').read_text())
        assert candidate['health']['IsHealthy'] and candidate['health']['SchemaVersion']==2
        assert candidate['observations']==baseline['observations'],(profile,phase)
        if phase=='repeat': assert all(s['PluginsExecuted']==0 and s['status']=='Completed' for s in candidate['statuses'])
        if phase=='cancel': assert all(s['status']=='OperationCanceledException' for s in candidate['statuses'])
print('PASS: exact cross-version facts, queries, provenance, publication snapshots and zero-plugin repeat in both profiles')
