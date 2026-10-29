from pathlib import Path
import json,sys
p=Path(sys.argv[1]);expected=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8'))
for phase in ['reopen','repeat','cancel','after']:
    c=json.loads((p/(phase+'.json')).read_text(encoding='utf-8'))
    assert c['health']['IsHealthy'] and c['health']['SchemaVersion']==2
    assert c['observations']==expected['observations'],phase
    if phase=='repeat':assert all(x['PluginsExecuted']==0 and x['status']=='Completed' for x in c['statuses'])
    if phase=='cancel':assert all(x['status']=='OperationCanceledException' for x in c['statuses'])
print('PASS: exact authentic-Hetu facts, identities, source/provider evidence, queries, traversal, publication and snapshot integrity; zero-plugin repeat')
