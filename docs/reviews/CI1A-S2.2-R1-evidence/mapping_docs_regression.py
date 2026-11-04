from pathlib import Path
import re, sys

root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r"D:\projects\Andirit")
base = root / "docs/planning/adr-ci1-01-ci1a-s2-2"
mapping = (base / "mapping-contract.md").read_text(encoding="utf-8")
confirm = (base / "g-confirm-package.md").read_text(encoding="utf-8")
mint = (base / "g-mint-package.md").read_text(encoding="utf-8")
checks = {
    "profiles are explicitly separated": "comparison profiles" in mapping.lower() and "production-capture profile" in mapping.lower() and "frozen r2 fixture profile" in mapping.lower(),
    "production profile preserves actual captured reference identity": "actual captured path" in mapping.lower() and "actual verified inventory" in mapping.lower(),
    "frozen profile enumerates its three runtime references": all(s in mapping for s in ("runtime/System.Runtime.dll", "runtime/System.Private.CoreLib.dll", "runtime/Microsoft.CSharp.dll")),
    "frozen profile values and scope anchors are explicit": all(s in mapping for s in ("404c5e48c3106b47786f14cad594139477c651fd78d0e050f8fd5bcab18fcb5f", "942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3")),
    "test-only preimage adaptation is disclosed and bounded": "test-only" in mapping.lower() and "must never substitute fixture labels" in mapping.lower(),
    "source inventory is distinct from analysis scope": "source inventory" in mapping.lower() and "analysisscope1" in mapping.lower(),
    "target helper shape checks are distinct from live authority": "shape may be accepted" in mapping.lower() and "live verified resolver/association gate" in mapping.lower(),
    "equal full inputs reproduce identity across instances": "equivalent complete accepted inputs" in mapping.lower() and "same `asmctx3`" in mapping.lower(),
    "compctx2 remains outside identity tuples": "compctx2" in mapping and "never enters `asmctx3`/`usage4`" in mapping,
    "G-CONFIRM row 5 names exact comparison profile": "row 5 comparison-profile confirmation" in confirm.lower() and "frozen corpus digest is required only under its complete fixture profile" in confirm.lower() and "test-only adaptation" in confirm.lower(),
    "pre-mint falsification is separated from S2.3 reopen": "before wp5" in mint.lower() and "s2.3" in mint.lower() and "native reopen" in mint.lower(),
    "minting gate does not require S2.3 reopen": bool(re.search(r"native reopen\s+is not required to pass g-mint in s2\.2", mint, re.I)),
}
failed = [name for name, passed in checks.items() if not passed]
for name, passed in checks.items(): print(("PASS" if passed else "FAIL") + " - " + name)
print(f"RESULT: {len(checks)-len(failed)}/{len(checks)} passed")
sys.exit(1 if failed else 0)






