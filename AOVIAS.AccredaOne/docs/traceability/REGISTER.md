# Traceability register

Canonical Confluence records are authoritative. This register tracks **canonical status**, **local synchronization**, and **implementation authorization** separately.

- Index catalogue: [CONFLUENCE-INDEX.md](CONFLUENCE-INDEX.md)
- Engineering gates: [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md)
- Last synchronized: **2026-11-02** (DEV-01 Phase 2 local batch)

**Implementation authorization** defaults to **NOT AUTHORIZED** for Phase 1 product work unless [GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md) is explicitly updated by a human.

| confluence_record_id | page_id | title | canonical_version | canonical_status | local_mirror_path | local_mirror_status | last_synchronized | supersedes | implementation_authorization |
| --- | ---: | --- | --- | --- | --- | --- | --- | --- | --- |
| DEV-PLAN-01 | 491848 | Development Readiness & Scope Control | OPEN | Current programme roadmap and gate authority | — | NOT APPLICABLE | 2026-11-02 | — | NOT AUTHORIZED |
| DOC-08 | 524289 | Product Principles, Architecture Guardrails & Decision Traceability | OPEN | Approved control standard | docs/traceability/CONVENTION.md | Synchronized (policy pointer) | 2026-11-02 | — | NOT AUTHORIZED |
| DOC-21 | 557573 | Documentation Readiness Audit & Remediation Register | OPEN | Initial baseline audit; partial review | docs/traceability/CONFLUENCE-INDEX.md §DOC-21 | Synchronized (findings list) | 2026-11-02 | — | NOT APPLICABLE |
| D1-01 | 623063 | Product Definition & First Vertical Slice | v1.0 | Approved | docs/product/README.md | Synchronized (pointer) | 2026-11-02 | — | NOT AUTHORIZED |
| D2 | 623087 | First-Cycle Requirements & Acceptance Criteria | v1.0 | Approved | docs/requirements/README.md | Synchronized (pointer) | 2026-11-02 | — | NOT AUTHORIZED |
| D3 | 491942 | Architecture Decisions Reconciliation | OPEN | For Product Owner review; not approved | — | NOT APPLICABLE | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-002 | 557361 | Microservices Architecture & Service Data Ownership | OPEN | Approved | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-003 | 524570 | Frontend Architecture | OPEN | Approved direction | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-004 | 524587 | Backend Architecture | OPEN | Approved direction | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-005 | 557377 | PostgreSQL & .NET Persistence Strategy | OPEN | Approved direction | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-006 | 622786 | Source-First Knowledge Architecture & Version Lifecycle | OPEN | Approved | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-007 | 622802 | Hybrid Search, Retrieval & AI Orchestration | OPEN | Approved | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-008 | 622843 | Identity, Registration & Access Model | OPEN | Approved direction; implementation details open | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-009 | 622818 | Docker-Based Deployment & Infrastructure Operating Model | OPEN | Approved | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-010 | 557541 | Agent Identity / Delegation Architecture | OPEN | Approved — architecture direction only | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| ADR-011 | 623000 | Agentic Architecture | OPEN | Selected design directions — formal ADR approval pending | — | Index only | 2026-11-02 | — | NOT AUTHORIZED |
| CI-01 | 1015879 | AOVIAS Central Identity Capability Assessment | v0.1 | Draft | docs/security/README.md | Synchronized (pointer) | 2026-11-02 | — | NOT AUTHORIZED |
| CI-02 | 623127 | Central Identity Architecture & Security Design | v0.1 | Draft | docs/security/README.md | Synchronized (pointer) | 2026-11-02 | — | NOT AUTHORIZED |

No local `DEC-NNNN` engineering records exist yet. Use the `DEC` series for repository or process decisions not represented by the Confluence rows above.

Add rows when new canonical records or local mirrors are created. Do not renumber Confluence ADR IDs.
