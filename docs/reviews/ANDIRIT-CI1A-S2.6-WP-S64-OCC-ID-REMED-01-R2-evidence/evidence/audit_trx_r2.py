import json,hashlib,xml.etree.ElementTree as ET
from pathlib import Path
base=Path(r"D:\opencode\s64-qual\qual-tools-r2\artifacts-46143882731")
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
for osd in sorted(base.glob('test-results-*')):
    s=json.load(open(osd/'subject.json',encoding='utf-8-sig'))
    print(osd.name,s.get('runnerArchitecture'),s.get('headSha'))
    for trx in sorted(osd.glob('*/tests.trx')):
        rel=trx.relative_to(osd).as_posix(); data=trx.read_bytes(); c=ET.fromstring(data); ctr=c.find('.//t:Counters',ns)
        print(' ',rel,'total',ctr.attrib.get('total'),'failed',ctr.attrib.get('failed'),'sha-match',hashlib.sha256(data).hexdigest()==s.get('trxSha256',{}).get(rel))
