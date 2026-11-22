"""Environment and native-dependency identity receipt.

Records the execution environment of the qualification run and the identity of the
pinned native dependency, as loaded by the production build.
"""

from __future__ import annotations

import hashlib
import json
import os
import platform
import subprocess
import sys
from pathlib import Path


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    if len(sys.argv) < 3:
        print("usage: environment-receipt.py <repo> <output-file>")
        return 2

    repo = str(Path(sys.argv[1]).resolve())
    output = Path(sys.argv[2])
    lines: list[str] = []

    def emit(text: str = "") -> None:
        lines.append(text)

    emit("ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 - execution environment receipt")
    emit()

    emit("[host]")
    emit(f"platform: {platform.platform()}")
    emit(f"system: {platform.system()}")
    emit(f"release: {platform.release()}")
    emit(f"version: {platform.version()}")
    emit(f"machine: {platform.machine()}")
    emit(f"processor: {platform.processor()}")
    emit(f"python: {sys.version.splitlines()[0]}")
    emit(f"cwd: {os.getcwd()}")
    emit()

    emit("[dotnet]")
    for args in (["--version"], ["--list-sdks"], ["--list-runtimes"]):
        result = subprocess.run(["dotnet", *args], capture_output=True, text=True)
        for line in result.stdout.splitlines():
            emit(f"dotnet {' '.join(args)}: {line}")
    emit()

    emit("[native-dependency: LatticeDbSharp 0.2.0]")
    packages = Path(os.environ.get("USERPROFILE", str(Path.home()))) / ".nuget" / "packages" / "latticedbsharp" / "0.2.0"
    emit(f"nuget-package-path: {packages}")
    if packages.exists():
        for item in sorted(packages.rglob("*")):
            if item.is_file():
                emit(f"  {item.relative_to(packages).as_posix()} bytes={item.stat().st_size} sha256={sha256(item)}")
    else:
        emit("  (package directory not found)")
    emit()

    emit("[pinned-dependency-reference]")
    csproj = Path(repo) / "src" / "Andirit.LatticeDb" / "Andirit.LatticeDb.csproj"
    for line in csproj.read_text(encoding="utf-8").splitlines():
        if "PackageReference" in line or "LatticeDbSharp" in line:
            emit(f"  {line.strip()}")
    emit()

    emit("[repository-state-at-receipt]")
    for args in (
        ["rev-parse", "HEAD"],
        ["rev-parse", "HEAD^{tree}"],
        ["status", "--porcelain"],
    ):
        result = subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True)
        emit(f"git {' '.join(args)}: {result.stdout.strip() or '(clean)'}")
    emit()

    output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {output} ({len(lines)} lines)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
