# Baseline and owner authorization

## Exact baseline

| Subject | Identity |
| --- | --- |
| Candidate | `77ec887368c62b404e0d9a8034d34b7f8cac8007` |
| Candidate tree | `40a2c42123e71bf65746b10198ca2af29c17d928` |
| Producer seal | `880a888da2256608a91010026b1c3f39d0de76b6` |
| Producer seal SHA-256 | `240c5db417b81d932af4782ed5178910b72650a29d94125368a4d276a8a77b1c` |
| Qualification payload | `d112fe7322cdaacbc6de7e7bd1e9608446a33c7a` |
| Qualification seal | `c6c2dcfd5697460318634d34d26eba6b88f6d4e1` |
| Qualification seal SHA-256 | `426576fe6b685948912de953cc222b1a6b6aabfb2c9e0b354f0a85a1cb08c688` |
| Regression inventory | 1,044 |
| Dependency | LatticeDbSharp `0.2.0` |
| Release boundary | `v0.2.0-preview.7` |

The independent qualification disposition **C — NOT SUPPORTED** is accepted.
The failed target-framework reference-resolution requirement is acknowledged.

## Owner authorization

Bounded remediation is authorized:

- preserve the versioned identity architecture and supported portions;
- reject the host-runtime implementation closure as the default semantic
  authority;
- resolve references from locally installed target-framework reference packs;
- represent the declared target framework in the canonical compilation contract;
- correct persisted compatibility detection ordering;
- preserve historical oracle, store and seal evidence;
- rebuild only by re-ingestion.

DF-1 and S6.4 remain open. D3 remains suspended.

## Preserved predecessor seals

The producer seal, qualification seal, R1-06 exception and all earlier seals are
unmodified. This remediation adds a generation-3 marker for corrected
experimental stores; R1 generation-2 stores are refused without mutation.
