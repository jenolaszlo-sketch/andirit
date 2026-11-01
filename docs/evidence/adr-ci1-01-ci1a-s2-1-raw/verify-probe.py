"""Independent canonical-tuple check; no production or R2 hashing helper imported."""
import hashlib
import json
from pathlib import Path
import struct

root = Path(__file__).resolve().parent
report = json.loads((root / "reference-probe.log").read_text(encoding="utf-8-sig"))

def hash_parts(parts):
    return hashlib.sha256(b"".join(struct.pack(">I", len(p.encode("utf-8")))
                                   + p.encode("utf-8") for p in parts)).hexdigest()

for descriptor in report["DefiningContexts"] + report["CallingContexts"]:
    parts = ["compctx1-context"] + [descriptor[name] for name in (
        "ContractVersion", "RepositoryId", "ProjectUnitId", "AssemblyName",
        "MetadataIdentity", "ModuleName", "OptionsDigest")]
    parts += ["|".join(source[name] for name in (
        "Path", "ContentHash", "LanguageVersion", "PreprocessorSymbols"))
        for source in descriptor["Sources"]]
    parts += descriptor["References"]
    expected = "compctx1:sha256:" + hash_parts(parts)
    assert expected == descriptor["DescriptorDigest"], (expected, descriptor)

assert report["CallingContexts"][0]["DescriptorDigest"] == report["CallingContexts"][1]["DescriptorDigest"]
assert report["CallingContexts"][0]["DescriptorDigest"] == report["CallingContexts"][2]["DescriptorDigest"]
assert len({d["DescriptorDigest"] for d in report["DefiningContexts"]}) == 3
assert report["ObservedReturnTypes"] == ["int", "string"]
authority = root / "authority" / "CompilationProvenance.cs"
assert hashlib.sha256(authority.read_bytes()).hexdigest() == report["OriginalAuthoritySha256"]
print("PASS: six exact compctx1 tuples independently recompute; three distinct dependency contexts share one caller context; API int -> string; frozen R2 source hash matches.")
