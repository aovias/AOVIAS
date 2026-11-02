# Confluence canonical record index

Confluence is the canonical authority for AccredaOne product and architecture decisions. This index is a **local synchronization catalogue** only. It does not approve or implement anything.

- **Site:** https://nishant11soni.atlassian.net
- **Space:** ACCREDAONE
- **Page URL pattern:** `https://nishant11soni.atlassian.net/wiki/spaces/ACCREDAONE/pages/{page_id}`
- **Last index synchronization:** 2026-11-02 (DEV-01 Phase 2; statuses per Product Owner authorization baseline)

**Page version note:** Confluence **page version** numbers were not re-read live in this synchronization session unless noted. Where page version is unknown, the register uses **OPEN**. Record **product/document version** (e.g. D1-01 v1.0) when stated in the canonical baseline.

## Programme and control standards

| Record ID | Title | Page ID | Canonical status | Record version | Local mirror | Sync status | Implementation boundary |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| DEV-PLAN-01 | Development Readiness & Scope Control | 491848 | Current programme roadmap and gate authority | OPEN | — | Index synchronized | Programme gates; not implementation authorization |
| DOC-08 | Product Principles, Architecture Guardrails & Decision Traceability | 524289 | Approved control standard | OPEN | — | Index synchronized | Traceability rules; not implementation authorization |
| DOC-21 | Documentation Readiness Audit & Remediation Register | 557573 | Initial baseline audit; partial review | OPEN | — | Index synchronized | Remediation backlog; see §Outstanding DOC-21 findings |

## Product and requirements

| Record ID | Title | Page ID | Canonical status | Record version | Local mirror | Sync status | Implementation boundary |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| D1-01 | Product Definition & First Vertical Slice | 623063 | Approved | v1.0 | [../product/README.md](../product/README.md) (pointer) | Index synchronized | Product scope authority; does not pass G1 or authorize Phase 1 |
| D2 | First-Cycle Requirements & Acceptance Criteria | 623087 | Approved | v1.0 | [../requirements/README.md](../requirements/README.md) (pointer) | Index synchronized | Requirements authority; does not authorize implementation |
| D3 | Architecture Decisions Reconciliation | 491942 | For Product Owner review; not approved | OPEN | — | Index synchronized | Not approved; do not treat as reconciliation sign-off |

## Architecture decision records

| Record ID | Title | Page ID | Canonical status | Record version | Local mirror | Sync status | Implementation boundary |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| ADR-002 | Microservices Architecture & Service Data Ownership | 557361 | Approved | OPEN | — | Index synchronized | Direction within ADR scope; implementation NOT AUTHORIZED |
| ADR-003 | Frontend Architecture | 524570 | Approved direction | OPEN | — | Index synchronized | Direction within ADR scope; implementation NOT AUTHORIZED |
| ADR-004 | Backend Architecture | 524587 | Approved direction | OPEN | — | Index synchronized | Direction within ADR scope; implementation NOT AUTHORIZED |
| ADR-005 | PostgreSQL & .NET Persistence Strategy | 557377 | Approved direction | OPEN | — | Index synchronized | Direction within ADR scope; implementation NOT AUTHORIZED |
| ADR-006 | Source-First Knowledge Architecture & Version Lifecycle | 622786 | Approved | OPEN | — | Index synchronized | Direction within ADR scope; topology clarification vs ADR-002 open (DOC-21) |
| ADR-007 | Hybrid Search, Retrieval & AI Orchestration | 622802 | Approved | OPEN | — | Index synchronized | Direction within ADR scope; implementation NOT AUTHORIZED |
| ADR-008 | Identity, Registration & Access Model | 622843 | Approved direction; implementation details open | OPEN | — | Index synchronized | Direction only; separate from CI-01/CI-02 drafts |
| ADR-009 | Docker-Based Deployment & Infrastructure Operating Model | 622818 | Approved | OPEN | — | Index synchronized | Direction within ADR scope; implementation NOT AUTHORIZED |
| ADR-010 | Agent Identity / Delegation Architecture | 557541 | Approved — architecture direction only | OPEN | — | Index synchronized | Architecture direction only; implementation boundaries open |
| ADR-011 | Agentic Architecture | 623000 | Selected design directions — formal ADR approval pending | OPEN | — | Index synchronized | **Not** an approved ADR; do not treat as ADR-002–010 approval |

## Central Identity (separate from ADR-008 implementation detail)

| Record ID | Title | Page ID | Canonical status | Record version | Local mirror | Sync status | Implementation boundary |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| CI-01 | AOVIAS Central Identity Capability Assessment | 1015879 | Draft | v0.1 | [../security/README.md](../security/README.md) (pointer) | Index synchronized | Planning; static assessment baseline; runtime verification open |
| CI-02 | Central Identity Architecture & Security Design | 623127 | Draft | v0.1 | [../security/README.md](../security/README.md) (pointer) | Index synchronized | Option A boundary selected; provider/implementation open; integration NOT AUTHORIZED |

## Outstanding DOC-21 findings (visibility — not closed)

These items remain open unless and until closed in Confluence with recorded evidence. ADR-010/ADR-011 presence does **not** close implementation-level questions.

| Finding area | Current status | Evidence needed for closure |
| --- | --- | --- |
| Source-rights verification | NOT VERIFIED | Formal source-specific permitted-use evidence recorded |
| Customer discovery evidence | ON HOLD | Authorized discovery evidence and PO release of hold |
| Product metrics and success measures | OPEN | Defined metrics and thresholds approved in Confluence |
| DOC-20 governance-register operationalization | OPEN | Operational register per DOC-20 |
| ADR-006 topology vs ADR-002 | OPEN | Clarification recorded against ADR-002 |
| Training-programme scope | OPEN | Scope clarification in Confluence |
| Framework-isolation design reconciliation | OPEN | Reconciled working design in Confluence |
| Architecture verification evidence | OPEN | Verification evidence recorded |
| ADR-010 / ADR-011 formalization | PARTIAL | ADR-011 formal approval; implementation boundaries explicit |
| Decision 13 | UNLOCKED | Decision 13 closed in canonical register |

See DOC-21 (557573). Later approved D1/D2 supersede only the specific DOC-21 findings they demonstrably resolve.
