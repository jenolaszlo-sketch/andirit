# ANDIRIT-CI1A-S2.2-G-CONFIRM-Q1 — independent tuple reconstruction
#
# Standalone, dependency-free reimplementation of the accepted R2/`usage4`
# and `compctx1` length-prefixed tuple algebra. It does NOT import or call any
# Andirit production helper or the repository oracle. It exists to reproduce the
# frozen NEW-02 `usage4` anchors and the oracle self-test `compctx1` anchor from
# the contract documents alone.
#
# Run:  python -X utf8 independent-reconstruction.py
import hashlib
import struct


def encode(domain, parts):
    """Domain-separated tuple: each value prefixed with its big-endian u32 byte length."""
    buf = bytearray()

    def append(value):
        raw = value.encode("utf-8")
        buf.extend(struct.pack(">I", len(raw)))
        buf.extend(raw)

    append(domain)
    for part in parts:
        append(part)
    return bytes(buf)


def digest(domain, parts):
    return hashlib.sha256(encode(domain, parts)).hexdigest()


def main():
    # --- frozen NEW-02 usage4 anchors (identity-contract.md §3.1) -------------
    base = [
        "experiment:andirit-31c33a9",
        "experiment.csharp.occurrences",
        "usage4-b2c-exp-r2-20261101",
        "caller-unit",
        "Caller.cs",
        "sha256:2f7803daa5910bf2f5fb5927b2e9f999ebd1b7074340096f620df37278df484a",
        "1", "72", "1", "77",
        "M:N.C.M",
    ]
    u1 = digest("usage4", base + [
        "asmctx3:sha256:632e7953365d1dbadbb55414b7cbcb991c9bc2653ec6cafb6f6d9d95a9c2032d",
        "method-invocation"])
    u2 = digest("usage4", base + [
        "asmctx3:sha256:fb3e0561272eb342a549036ecf0095c2ec2a6d5d7bc159fc943dee11fe669c26",
        "method-invocation"])
    print("usage4-a", u1,
          "match=", u1 == "bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af")
    print("usage4-b", u2,
          "match=", u2 == "b874bdc5afa194e0e237d0b1d7d36b3fa3dd767799e0dc62f5f1399f00b90bca")

    # --- oracle self-test compctx1 anchor (R2Identity.SelfTest) ---------------
    source = "src/App.cs|sha256:" + "0" * 64 + "|Latest|FEATURE,TRACE"
    c1 = digest("compctx1-context", [
        "compctx1", "repo:test", "src/App.csproj", "App", "App|1.0.0.0||", "App.dll",
        "sha256:" + "a" * 64,
        source,
        "file|/ref/System.Runtime.dll|sha256:" + "1" * 64,
    ])
    print("compctx1-a", "compctx1:sha256:" + c1,
          "match=", c1 == "71d89657b28013dc7e6fcf6f755980a4412451802437a85aea8e67a3fe4f1333")


if __name__ == "__main__":
    main()
