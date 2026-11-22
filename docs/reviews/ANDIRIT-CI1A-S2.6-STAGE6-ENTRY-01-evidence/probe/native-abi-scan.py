#!/usr/bin/env python3
"""Native ABI cancellation scan for ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.

Enumerates the exported symbols of the pinned LatticeDbSharp 0.2.0 native
assets (PE and ELF) and the declared capability set, and reports whether any
cancellation, interruption or abort entry point exists. The scan is read-only:
it never loads or executes the native binaries.

Usage:
    python native-abi-scan.py <native-assets-root> <capabilities.json> <output-file>
"""

from __future__ import annotations

import hashlib
import json
import re
import struct
import sys
from pathlib import Path

CANCEL_PATTERN = re.compile(
    r"(?i)(cancel|interrupt|abort|terminate|shutdown|kill|stop|revoke|withdraw)"
)
IDENTIFIER_PATTERN = re.compile(rb"[A-Za-z_][A-Za-z0-9_]{3,}")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def read_c_string(data: bytes, offset: int) -> str:
    end = data.index(b"\x00", offset)
    return data[offset:end].decode("ascii", "replace")


def pe_exports(path: Path) -> list[str] | None:
    """Return the PE export names, or None when the file is not a PE image."""
    data = path.read_bytes()
    if data[:2] != b"MZ":
        return None
    pe_offset = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe_offset:pe_offset + 4] != b"PE\x00\x00":
        return None

    coff = pe_offset + 4
    section_count = struct.unpack_from("<H", data, coff + 2)[0]
    optional_size = struct.unpack_from("<H", data, coff + 16)[0]
    optional = coff + 20
    magic = struct.unpack_from("<H", data, optional)[0]
    directory_offset = optional + (96 if magic == 0x10B else 112)
    export_rva, export_size = struct.unpack_from("<II", data, directory_offset)
    if export_rva == 0:
        return []

    sections = []
    section_table = optional + optional_size
    for index in range(section_count):
        base = section_table + index * 40
        virtual_size, virtual_address, raw_size, raw_pointer = struct.unpack_from(
            "<IIII", data, base + 8
        )
        sections.append((virtual_address, max(virtual_size, raw_size), raw_pointer))

    def to_offset(rva: int) -> int:
        for virtual_address, span, raw_pointer in sections:
            if virtual_address <= rva < virtual_address + span:
                return raw_pointer + (rva - virtual_address)
        raise ValueError(f"RVA 0x{rva:x} is outside every section")

    directory = to_offset(export_rva)
    name_count = struct.unpack_from("<I", data, directory + 24)[0]
    names_rva = struct.unpack_from("<I", data, directory + 32)[0]
    names_offset = to_offset(names_rva)

    names = []
    for index in range(name_count):
        name_rva = struct.unpack_from("<I", data, names_offset + index * 4)[0]
        names.append(read_c_string(data, to_offset(name_rva)))
    return names


def elf_exports(path: Path) -> list[str] | None:
    """Return the ELF dynamic symbol names, or None when the file is not ELF."""
    data = path.read_bytes()
    if data[:4] != b"\x7fELF":
        return None

    is_64 = data[4] == 2
    little = data[5] == 1
    endian = "<" if little else ">"

    if is_64:
        section_header_offset = struct.unpack_from(endian + "Q", data, 0x28)[0]
        section_entry_size = struct.unpack_from(endian + "H", data, 0x3A)[0]
        section_count = struct.unpack_from(endian + "H", data, 0x3C)[0]
    else:
        section_header_offset = struct.unpack_from(endian + "I", data, 0x20)[0]
        section_entry_size = struct.unpack_from(endian + "H", data, 0x2E)[0]
        section_count = struct.unpack_from(endian + "H", data, 0x30)[0]

    sections = []
    for index in range(section_count):
        base = section_header_offset + index * section_entry_size
        if is_64:
            name_offset, sh_type = struct.unpack_from(endian + "II", data, base)
            sh_offset, sh_size = struct.unpack_from(endian + "QQ", data, base + 0x18)
            sh_link = struct.unpack_from(endian + "I", data, base + 0x28)[0]
            sh_entsize = struct.unpack_from(endian + "Q", data, base + 0x38)[0]
        else:
            name_offset, sh_type = struct.unpack_from(endian + "II", data, base)
            sh_offset, sh_size = struct.unpack_from(endian + "II", data, base + 0x10)
            sh_link = struct.unpack_from(endian + "I", data, base + 0x18)[0]
            sh_entsize = struct.unpack_from(endian + "I", data, base + 0x24)[0]
        sections.append({
            "name_offset": name_offset,
            "type": sh_type,
            "offset": sh_offset,
            "size": sh_size,
            "link": sh_link,
            "entry_size": sh_entsize,
        })

    names: list[str] = []
    for section in sections:
        if section["type"] != 11:  # SHT_DYNSYM
            continue
        strings = sections[section["link"]]
        entry_size = section["entry_size"] or 24
        count = section["size"] // entry_size
        for index in range(count):
            base = section["offset"] + index * entry_size
            if is_64:
                st_name = struct.unpack_from(endian + "I", data, base)[0]
            else:
                st_name = struct.unpack_from(endian + "I", data, base)[0]
            if st_name == 0:
                continue
            names.append(read_c_string(data, strings["offset"] + st_name))
    return names


def macho_string_scan(path: Path) -> list[str]:
    """Best-effort identifier scan for Mach-O binaries (no export-trie parser)."""
    data = path.read_bytes()
    identifiers = {
        match.group(0).decode("ascii", "replace")
        for match in IDENTIFIER_PATTERN.finditer(data)
    }
    return sorted(identifier for identifier in identifiers if CANCEL_PATTERN.search(identifier))


def main() -> int:
    if len(sys.argv) != 4:
        print(__doc__)
        return 2

    root = Path(sys.argv[1])
    capabilities_path = Path(sys.argv[2])
    output_path = Path(sys.argv[3])

    lines: list[str] = []
    lines.append("Native ABI cancellation scan — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01")
    lines.append("Target: LatticeDbSharp 0.2.0 native assets (read-only static scan)")
    lines.append("")

    assets = sorted(
        path for path in root.rglob("*")
        if path.is_file() and path.name in {"lattice.dll", "liblattice.so", "liblattice.dylib"}
    )
    if not assets:
        lines.append("ERROR: no native assets found under " + str(root))
        output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        return 1

    total_cancellation_entries = 0
    for asset in assets:
        lines.append("=== native asset")
        lines.append(f"path: {asset}")
        lines.append(f"bytes: {asset.stat().st_size}")
        lines.append(f"sha256: {sha256(asset)}")

        exports: list[str] | None
        exports = pe_exports(asset)
        kind = "PE"
        if exports is None:
            exports = elf_exports(asset)
            kind = "ELF"
        if exports is None:
            kind = "Mach-O (identifier scan only)"
            cancellation_identifiers = macho_string_scan(asset)
            lattice_identifiers = [
                identifier for identifier in cancellation_identifiers
                if identifier.lower().startswith("lattice")
            ]
            lines.append(f"format: {kind}")
            lines.append(f"cancellation-like identifiers: {len(cancellation_identifiers)}")
            lines.append(f"lattice-namespace-cancellation-identifiers: {len(lattice_identifiers)}")
            for identifier in lattice_identifiers:
                lines.append(f"  lattice-identifier: {identifier}")
            lines.append(
                "non-lattice-runtime-identifiers: C runtime / Swift concurrency symbols; "
                "not transaction-control entry points"
            )
            for identifier in cancellation_identifiers:
                if identifier not in lattice_identifiers:
                    lines.append(f"  runtime-identifier: {identifier}")
            total_cancellation_entries += len(lattice_identifiers)
            lines.append("")
            continue

        unique = sorted(set(exports))
        cancellation = [name for name in unique if CANCEL_PATTERN.search(name)]
        lattice_cancellation = [
            name for name in cancellation if name.lower().startswith("lattice")
        ]
        runtime_cancellation = [name for name in cancellation if name not in lattice_cancellation]
        lines.append(f"format: {kind}")
        lines.append(f"exported-or-dynamic-symbols: {len(unique)}")
        lines.append(f"cancellation-like-symbols: {len(cancellation)}")
        lines.append(f"lattice-namespace-cancellation-symbols: {len(lattice_cancellation)}")
        for name in lattice_cancellation:
            lines.append(f"  lattice-symbol: {name}")
        lines.append(
            f"non-lattice-runtime-symbols-matching-the-pattern: {len(runtime_cancellation)} "
            "(C runtime / Swift concurrency symbols; not transaction-control entry points)"
        )
        for name in runtime_cancellation:
            lines.append(f"  runtime-symbol: {name}")
        lines.append("transaction-and-lifecycle-symbols:")
        for name in unique:
            if re.search(r"(?i)(lattice_(begin|commit|rollback|close|open|txn|transaction))", name):
                lines.append(f"  symbol: {name}")
        total_cancellation_entries += len(lattice_cancellation)
        lines.append("")

    lines.append("=== declared capability set")
    capabilities = json.loads(capabilities_path.read_text(encoding="utf-8"))
    features = capabilities.get("features", {})
    lines.append(f"capabilities file: {capabilities_path}")
    lines.append(f"declared features: {len(features)}")
    cancellation_features = [
        name for name in features if CANCEL_PATTERN.search(name)
    ]
    lines.append(f"cancellation-like features: {len(cancellation_features)}")
    for name in sorted(cancellation_features):
        lines.append(f"  feature: {name} -> {json.dumps(features[name], sort_keys=True)}")
    lines.append("all declared features:")
    for name in sorted(features):
        lines.append(f"  {name}: {features[name].get('status', '?')}")

    upstream_path = capabilities_path.parent / "upstream.json"
    if upstream_path.exists():
        upstream = json.loads(upstream_path.read_text(encoding="utf-8"))
        lines.append("")
        lines.append("=== pinned upstream")
        lines.append(f"repository: {upstream.get('repository')}")
        lines.append(f"reference: {upstream.get('reference')}")
        lines.append(f"commit: {upstream.get('commit')}")
        lines.append(f"supportedRuntimeIdentifiers: {upstream.get('supportedRuntimeIdentifiers')}")
        lines.append(f"candidateRuntimeIdentifiers: {upstream.get('candidateRuntimeIdentifiers')}")
        lines.append(f"status: {upstream.get('status')}")

    lines.append("")
    lines.append(
        "RESULT lattice-namespace cancellation-capable native entry points found: "
        f"{total_cancellation_entries}"
    )
    text = "\n".join(lines) + "\n"
    output_path.write_text(text, encoding="utf-8")
    print(text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
