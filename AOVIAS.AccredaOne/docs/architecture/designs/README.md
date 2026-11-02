# Technical design documents

Technical designs use the identifier series `DES-NNNN`.

No technical design is approved.

## Rules

- Create a document from [../../../templates/technical-design.md](../../../templates/technical-design.md).
- A design cites the `REQ` and `ADR` records it elaborates.
- A design does not approve architecture. If the cited `ADR` is not `Approved`, the design cannot be `Approved`.
- A design is not permission to write production code. Phase 1 is **not authorized**.
- Do not embed schemas, API contracts, or stack choices as settled design while those gates are unapproved.

Store documents in this folder as `DES-NNNN-short-title.md`.
