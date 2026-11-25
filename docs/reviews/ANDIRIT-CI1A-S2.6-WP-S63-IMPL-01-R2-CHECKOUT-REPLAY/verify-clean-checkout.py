"""Restore only verified line-ending conversions in a disposable R2 worktree."""

from pathlib import Path
import subprocess
import sys

root = Path(sys.argv[1]).resolve(strict=True)
subject = "a96c19a844f8ea1719508198b47df23c4e203571"
allowed = {"andirit-s63-r2-verify", "andirit-s63-r2-clean"}
assert root.parent == Path("D:/temp").resolve() and root.name in allowed


def git(*args):
    return subprocess.check_output(["git", "-C", str(root), *args])


assert git("rev-parse", "HEAD").decode().strip() == subject
pending = []
for row in git("status", "--porcelain=v1", "-z", "--untracked-files=all").split(b"\0"):
    if not row:
        continue
    path = row[3:].decode("utf-8")
    assert row.startswith(b" M ") and path.startswith("docs/reviews/CI1A-S2.4-")
    target = (root / path).resolve(strict=True)
    assert target.is_relative_to(root) and target.name in (".gitattributes", ".gitignore")
    committed = git("cat-file", "blob", subject + ":" + path)
    disk = target.read_bytes()
    assert disk != committed and disk.replace(b"\r\n", b"\n") == committed
    pending.append((target, committed))
    print("Checkout-only CRLF conversion:", path)

for path, blob in pending:
    path.write_bytes(blob)
    assert path.read_bytes() == blob

assert not git("status", "--porcelain=v1", "--untracked-files=all")
print(f"{root} at {subject}: {len(pending)} checkout-only conversions restored to exact committed bytes, Git status clean")
