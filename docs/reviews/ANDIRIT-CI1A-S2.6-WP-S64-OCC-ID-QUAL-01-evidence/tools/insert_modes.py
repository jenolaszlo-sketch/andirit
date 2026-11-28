from pathlib import Path
p=Path(r"D:\opencode\s64-qual\qual-tools\store-probe\Program.cs")
b=Path(r"D:\opencode\s64-qual\qual-tools\insert.cs").read_text(encoding="utf-8")
s=p.read_text(encoding="utf-8")
marker='Console.Error.WriteLine("usage: inspect|open|tamper <db> ...");'
if 'tamper-meta' not in s:
    s=s.replace(marker,b+"\n"+marker)
    p.write_text(s,encoding="utf-8")
print("inserted", 'tamper-meta' in s)
