# AccredaOne governance status

Status date: 2026-11-02 (governance synchronization)
Prior baseline: 2026-10-31 (Foundation Sprint 0)
Repository: AccredaOne (standalone nested Git repository)

This document is the **local engineering record of authorization**. Confluence (space **ACCREDAONE**) is the canonical source for approved product and architecture decisions. See [docs/traceability/CONFLUENCE-INDEX.md](docs/traceability/CONFLUENCE-INDEX.md) and [docs/traceability/REGISTER.md](docs/traceability/REGISTER.md).

**Warning:** Synchronizing this file with canonical Confluence statuses does **not** grant Phase 1 implementation, technology/architecture **implementation**, Central Identity integration, or source-rights clearance.

## Status

| Control | Status | Meaning in this repository |
| --- | --- | --- |
| G1 — Programme readiness gate | **NOT PASSED** | Programme readiness gate not satisfied per DEV-PLAN-01. D1/D2 approval does not pass G1. |
| Phase 1 | **NOT AUTHORIZED** | Production product development must not start. |
| First development slice | **NOT APPROVED** | No authorized first engineering slice beyond approved foundation and controlled prototype rules. |
| Customer discovery | **ON HOLD** | Customer discovery activity is not authorized. |
| D1 product definition | **APPROVED** | Canonical **D1-01 v1.0** (Confluence). Frozen product scope in Confluence; not Phase 1 authorization. |
| D2 requirements baseline | **APPROVED** | Canonical **D2 v1.0** (Confluence). First-cycle requirements and acceptance criteria; not implementation authorization. |
| D3 architecture reconciliation | **NOT APPROVED** | D3 remains for Product Owner review. Do not treat as settled reconciliation. |
| Architecture directions ADR-002 through ADR-010 | **APPROVED** (within each record's stated scope) | Approved **directions** in Confluence. Unresolved topology, service, provider and detail items remain open per each ADR and DOC-21. |
| ADR-011 agentic architecture | **SELECTED DESIGN DIRECTIONS — FORMAL ADR APPROVAL PENDING** | Not an approved ADR. Do not cite as fully approved architecture. |
| Technology direction | **APPROVED** (within the scope of the relevant ADRs) | Stack **direction** per ADR-003, ADR-004, ADR-005, ADR-009 and related records. Versions, products and deployment choices remain open where ADRs say so. |
| Technology/architecture implementation | **NOT AUTHORIZED** | No production APIs, schemas, migrations, auth implementation, ingestion, search, deployment, or application code under `application/`. |
| Real framework source rights | **NOT VERIFIED** | No real accreditation source content ingestion, extraction, indexing, AI processing, paraphrasing or display until permitted-use evidence is recorded. |
| NABH information request | **Contact made; formal response awaited** | Do not treat contact as rights verification. |
| Central Identity planning | **Mandatory shared-platform planning workstream** | Option A direction; CI-01 and CI-02 are **draft** in Confluence. |
| Central Identity implementation/integration | **NOT AUTHORIZED** | No Central Identity build or AccredaOne integration. |
| Anonymous public discovery | **Must remain independent of Central Identity** | Anonymous read-only discovery must not depend on Central Identity. |
| Controlled prototype | **Permitted only within the existing synthetic-data boundary** | Not production. Synthetic data does not establish source rights or production retrieval quality. |
| DEV-01 repository baseline | **Git ownership reconciliation complete; Phase 2 governance synchronization complete in the working tree; pending Product Owner closure review and controlled commit** | See [docs/delivery/DEV-01-GOVERNANCE-SYNCHRONIZATION-REPORT.md](docs/delivery/DEV-01-GOVERNANCE-SYNCHRONIZATION-REPORT.md). Overall DEV-01 programme closure is **not** marked complete here. |

Product promise (assignment baseline, not a scope approval):

> AccredaOne transforms fragmented accreditation resources into an accessible, connected and engaging knowledge experience.

AccredaOne is described as a Compliance Knowledge & Readiness Platform. That label does not by itself approve features, release authorization, or an operating model.

## What this status allows

- Maintain this foundation and synchronize local indexes with canonical Confluence (read-only from Confluence; no Confluence writes from this repository).
- Add **local mirrors or DEC records** that accurately reflect Confluence status (see [docs/traceability/CONVENTION.md](docs/traceability/CONVENTION.md)).
- Prepare controlled prototype briefs under `prototypes/` within the synthetic-data boundary.
- Documentation and traceability work that does not implement the product.

## What this status withholds

- Phase 1 product implementation
- Technology and architecture **implementation** (even when direction is approved in Confluence)
- Production authentication, authorization, persistence, ingestion, search, scoring, assessments, infrastructure and deployment
- Central Identity implementation and AccredaOne integration
- Real framework source content and unverified accreditation material
- Copying AOVIAS Command code, database structures, or accreditation content
- Inferring implementation authorization from approved documentation alone

## How a status changes

An agent must not change a control in the table above from **NOT PASSED**, **NOT AUTHORIZED**, **NOT APPROVED**, or **NOT VERIFIED** to a passing, authorized, approved, or verified state without a human update.

A human updates this document when the corresponding gate actually changes, and records:

- the item that changed
- the new status
- the date
- the approver
- the evidence (Confluence page ID, decision record identifier, or other named source)

Until that update exists, every document in this repository must honor the controls above.

## Historical baseline (Foundation Sprint 0 — 2026-10-31)

The following table applied before Confluence governance synchronization. It is **preserved for audit history** and is **superseded for engineering interpretation** by the 2026-11-02 status table above.

| Item | Status (historical) |
| --- | --- |
| G1 | NOT PASSED |
| Phase 1 implementation | NOT AUTHORIZED |
| MVP scope | NOT APPROVED |
| Architecture | NOT APPROVED |
| Technology selection | NOT APPROVED |
| NABH content rights | NOT VERIFIED |

See [docs/delivery/FOUNDATION-SPRINT-0-REPORT.md](docs/delivery/FOUNDATION-SPRINT-0-REPORT.md).

## Related control documents

- [AGENTS.md](AGENTS.md)
- [CONTRIBUTING.md](CONTRIBUTING.md)
- [docs/traceability/CONVENTION.md](docs/traceability/CONVENTION.md)
- [docs/traceability/CONFLUENCE-INDEX.md](docs/traceability/CONFLUENCE-INDEX.md)
- [docs/traceability/REGISTER.md](docs/traceability/REGISTER.md)
- [docs/content-governance/README.md](docs/content-governance/README.md)
