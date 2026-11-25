"""Create and verify self-excluding R2 manifests directly from Git objects."""

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument("mode", choices=["create", "verify"])
parser.add_argument("repo", type=Path)
parser.add_argument("revision")
parser.add_argument("package", type=PurePosixPath)
args = parser.parse_args()
prefix = args.package.as_posix()
manifest_path = prefix + "/manifest.json"
schema = "self-excluding-sha256-manifest-v1"


def git(*words):
    return subprocess.check_output(["git", "-C", str(args.repo), *words])


def oid(revision_path):
    return git("rev-parse", "--verify", revision_path).decode("ascii").strip()


def sha(data):
    return hashlib.sha256(data).hexdigest()


def entries(revision):
    listing = git("ls-tree", "-r", "--name-only", "-z", revision, "--", prefix).decode("utf-8").split("\0")
    paths = sorted(path for path in listing if path and path != manifest_path)
    assert paths and all(path.startswith(prefix + "/") for path in paths)
    result = []
    for path in paths:
        data = git("cat-file", "blob", revision + ":" + path)
        result.append({"path": path.removeprefix(prefix + "/"), "bytes": len(data), "sha256": sha(data)})
    return result


def package_hash(files):
    return sha(json.dumps(files, sort_keys=True, separators=(",", ":")).encode("utf-8"))


revision = oid(args.revision + "^{commit}")
if args.mode == "create":
    assert manifest_path.encode() not in git("ls-tree", "-r", "--name-only", "-z", revision, "--", manifest_path)
    files = entries(revision)
    manifest = {"schema": schema, "package": prefix, "payloadCommit": revision,
                "payloadRepositoryTree": oid(revision + "^{tree}"), "payloadPackageTree": oid(revision + ":" + prefix),
                "manifest_sha256": package_hash(files), "files": files}
    destination = args.repo.joinpath(*args.package.parts, "manifest.json")
    destination.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")
else:
    manifest = json.loads(git("cat-file", "blob", revision + ":" + manifest_path))
    assert manifest["schema"] == schema and manifest["package"] == prefix
    files = entries(revision)
    payload = manifest["payloadCommit"]
    assert oid(payload + "^{tree}") == manifest["payloadRepositoryTree"]
    assert oid(payload + ":" + prefix) == manifest["payloadPackageTree"]
    assert entries(payload) == files == manifest["files"], "Missing, extra, or mismatching evidence blob"
    assert package_hash(files) == manifest["manifest_sha256"], "Package digest differs"
print(f"{args.mode} {prefix} at {revision}: {len(files)} entries, package {manifest['manifest_sha256']}")
