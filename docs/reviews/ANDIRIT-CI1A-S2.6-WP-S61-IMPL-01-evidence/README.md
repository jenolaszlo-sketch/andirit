ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 evidence package.

Contents: the Git-object-derived preflight receipt, the contract-consistency gate receipt, the
Windows x64 qualification matrix receipts (Q-01..Q-12 plus the post-commit-cancellation regression
guard), the whole-solution regression / coverage / formatting / build / packaging receipts, the
public API surface diff against the baseline, the exact production delta, the genuine hosted
Ubuntu x64 CI evidence and the self-excluding SHA-256 manifest.

Re-generation: run-cases.ps1 (qualification matrix), preflight-receipt.py, contract-gate.py,
environment-receipt.py and make-manifest.py in this directory.

Byte preservation: the .gitattributes in this directory marks every file as binary (-text), so the
committed bytes are the bytes the manifest hashes.
