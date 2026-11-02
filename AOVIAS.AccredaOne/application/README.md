# Application

This directory is reserved for the future production application.

**DEV-03 reversible scaffolding** (frontend and backend skeletons) lives under `frontend/` and `backend/` for local build validation only. This is **not** Phase 1 product implementation. Phase 1 is **not authorized**. The **first development slice** is **not approved**.

## Current decision and gate context

Engineering authorization and canonical approval states are recorded in [../GOVERNANCE-STATUS.md](../GOVERNANCE-STATUS.md). Canonical product and architecture records live in Confluence (space **ACCREDAONE**); see [../docs/traceability/CONFLUENCE-INDEX.md](../docs/traceability/CONFLUENCE-INDEX.md).

| Topic | Status in this repository (summary) |
| --- | --- |
| **D1-01** product definition | **Approved** (Confluence v1.0) |
| **D2** requirements baseline | **Approved** (Confluence v1.0) |
| **D3** architecture reconciliation | **Not approved** |
| **ADR-002** through **ADR-010** | **Approved** within each record's stated scope |
| **ADR-011** | Selected design directions — **formal ADR approval pending** |
| **Technology direction** | **Approved** within the scope of the relevant ADRs |
| **Technology / architecture implementation** | **Not authorized** |
| **First development slice** | **Not approved** |

Approved Confluence documentation does **not** by itself authorize code under `application/`.

## Rules

- Do not add projects, packages, services, schemas, APIs, or infrastructure here unless [../GOVERNANCE-STATUS.md](../GOVERNANCE-STATUS.md) and assigned work explicitly authorize that class of implementation.
- Do not place prototypes here. Experiments belong in `prototypes/`.
- Do not copy AOVIAS Command code or database structures here.
- Adding production code requires a human update to governance gates (for example Phase 1 authorized and first development slice approved), aligned Confluence evidence, and traceability citations — not inference from approved direction alone.

Until Phase 1 and the first development slice are approved beyond scaffolding, treat `frontend/` and `backend/` as **controlled engineering foundation** only — not authorized product implementation. DEV-03 increment 1 adds the application shell and API foundation ([../docs/architecture/APPLICATION-FOUNDATION.md](../docs/architecture/APPLICATION-FOUNDATION.md)). DEV-04 adds PostgreSQL persistence configuration and readiness only ([../docs/architecture/PERSISTENCE-FOUNDATION.md](../docs/architecture/PERSISTENCE-FOUNDATION.md)); the default host does not connect to a database.
