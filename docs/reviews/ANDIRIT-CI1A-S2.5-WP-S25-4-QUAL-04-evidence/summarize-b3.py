#!/usr/bin/env python3
"""Summarize the independent B3 identity/ownership checks from final receipts."""
from __future__ import annotations

import json
import sys
from pathlib import Path


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def phase(receipt: dict) -> dict:
    coverage = receipt["query"]["coverage"]
    rows = []
    for unit in receipt["native"]["units"]:
        rows.append({
            "indexUnitId": unit["indexUnitId"],
            "facts": unit["facts"],
            "coverage": unit["coverage"],
            "authoritySourceUnit": unit["authoritySourceUnit"],
            "authorityCoverage": unit["authorityCoverage"],
            "authorityMembers": unit["authorityMembers"],
        })
    return {
        "identityMatch": receipt["identityMatch"],
        "targetMatch": receipt["targetMatch"],
        "callerOwnershipMatch": receipt["callerOwnershipMatch"],
        "expectedIdCount": receipt["expectedIdCount"],
        "observedUniqueIdCount": len(set(receipt["observedQueryIds"])),
        "nativeFactCount": receipt["nativeFactCount"],
        "externalOccurrenceFacts": receipt["externalOccurrenceFacts"],
        "expectedTarget": receipt["expectedTarget"],
        "expectedDeclarationSpan": receipt["expectedDeclarationSpan"],
        "nativeDeclarationSpans": receipt["nativeDeclarationSpans"],
        "callerProjectUnitId": receipt["callerProjectUnitId"],
        "dependencyProjectUnitId": receipt["dependencyProjectUnitId"],
        "universeComplete": receipt["universeComplete"],
        "verifiedEntryCount": len(receipt["verifiedEntries"]),
        "coverage": coverage,
        "nativeRows": rows,
        "failures": receipt["failures"],
    }


def main() -> int:
    if len(sys.argv) != 4:
        print("usage: summarize-b3.py <publish> <verify> <output-json>")
        return 2
    publish = phase(load(Path(sys.argv[1])))
    verify = phase(load(Path(sys.argv[2])))
    accepted = all([
        publish["identityMatch"], verify["identityMatch"],
        publish["targetMatch"], verify["targetMatch"],
        publish["callerOwnershipMatch"], verify["callerOwnershipMatch"],
        publish["expectedIdCount"] == verify["expectedIdCount"] == 50,
        publish["observedUniqueIdCount"] == verify["observedUniqueIdCount"] == 50,
        publish["nativeFactCount"] == verify["nativeFactCount"] == 50,
        publish["externalOccurrenceFacts"] == verify["externalOccurrenceFacts"] == 0,
        publish["nativeDeclarationSpans"] == verify["nativeDeclarationSpans"] ==
            ["Dependency/Api.cs:4:5:4:35"],
        publish["universeComplete"] and verify["universeComplete"],
        publish["verifiedEntryCount"] == verify["verifiedEntryCount"] == 2,
        not publish["failures"] and not verify["failures"],
    ])
    result = {
        "case": "B3 independent 50-site identity/ownership reconstruction",
        "publish": publish,
        "fresh_process_verify": verify,
        "accepted": accepted,
    }
    Path(sys.argv[3]).write_text(
        json.dumps(result, indent=2) + "\n", encoding="utf-8", newline="\n"
    )
    print(json.dumps({
        "publishIdentities": publish["observedUniqueIdCount"],
        "verifyIdentities": verify["observedUniqueIdCount"],
        "nativeFacts": verify["nativeFactCount"],
        "externalFacts": verify["externalOccurrenceFacts"],
        "declarationSpans": verify["nativeDeclarationSpans"],
        "accepted": accepted,
    }, indent=2))
    return 0 if accepted else 1


if __name__ == "__main__":
    sys.exit(main())
