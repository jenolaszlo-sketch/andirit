import json,hashlib,xml.etree.ElementTree as ET
from pathlib import Path
base=Path(r'C:\Windows\Temp\opencode\s64-qual\qual-tools\artifacts-46132247172')
for os_dir in sorted(base.glob('test-results-*')):
    subj=json.load(open(os_dir/'subject.json',encoding='utf-8-sig'))
    print('==',os_dir.name,'head',subj.get('headSha'),'arch',subj.get('runnerArchitecture'),'os',subj.get('runnerOS'),'sdk',subj.get('sdk'))
    for trx in sorted(os_dir.glob('*/tests.trx')):
        data=trx.read_bytes(); h=hashlib.sha256(data).hexdigest()
        rel=str(trx.relative_to(os_dir)).replace('\\','/')
        print(' ',rel,'sha',h,'match',h==subj.get('trxSha256',{}).get(rel))
        root=ET.fromstring(data)
        ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
        counters=root.find('.//t:Counters',ns)
        print('   counters',counters.attrib if counters is not None else None)
