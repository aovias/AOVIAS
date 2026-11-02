# AccredaOne

Compliance Knowledge & Readiness Platform.

> AccredaOne transforms fragmented accreditation resources into an accessible, connected and engaging knowledge experience.

This repository is the standalone engineering foundation for AccredaOne. It holds governance, traceability indexes, decision-record structure, and room for controlled prototypes. It does not contain a production application.

## Governance

Engineering authorization is recorded in [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md). Canonical product and architecture decisions live in Confluence (space **ACCREDAONE**). See [docs/traceability/CONFLUENCE-INDEX.md](docs/traceability/CONFLUENCE-INDEX.md).

| Control | Status (summary) |
| --- | --- |
| G1 — Programme readiness gate | **NOT PASSED** |
| Phase 1 | **NOT AUTHORIZED** |
| D1 product definition | **APPROVED** (Confluence) |
| D2 requirements baseline | **APPROVED** (Confluence) |
| Architecture directions ADR-002–ADR-010 | **APPROVED** (within each ADR scope) |
| ADR-011 | **Formal ADR approval pending** |
| Technology/architecture implementation | **NOT AUTHORIZED** |

Read [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md) before any other work. Agents must follow [AGENTS.md](AGENTS.md).

## Repository layout

| Path | Role |
| --- | --- |
| `docs/product/` | Pointers and optional mirrors; canonical product definition is **D1-01** in Confluence. |
| `docs/requirements/` | Pointers and optional mirrors; canonical requirements baseline is **D2** in Confluence. |
| `docs/decisions/` | Local `DEC` records for engineering decisions not represented by Confluence ADRs. |
| `docs/domain/` | Domain modelling (empty until assigned). |
| `docs/architecture/` | Architecture area; canonical ADRs in Confluence. |
| `docs/security/` | Security and identity pointers; **CI-01/CI-02** are draft in Confluence. |
| `docs/content-governance/` | Content rights and source-use rules. |
| `docs/delivery/` | Setup, tasks, test plans, reports, DEV-01 synchronization report. |
| `docs/traceability/` | Confluence index, register, and citation convention. |
| `templates/` | Record templates. Copying a template does not approve it. |
| `prototypes/` | Controlled experiments within the synthetic-data boundary only. |
| `application/` | Reserved for future production code. Phase 1 is not authorized. |

These three areas stay separate:

1. **Product documentation** lives under `docs/` and Confluence.
2. **Prototype experimentation** lives under `prototypes/`.
3. **Future production application code** is reserved under `application/`.

## Start here

1. [Governance status](GOVERNANCE-STATUS.md)
2. [Agent instructions](AGENTS.md)
3. [Contributing](CONTRIBUTING.md)
4. [Confluence index](docs/traceability/CONFLUENCE-INDEX.md)
5. [Traceability register](docs/traceability/REGISTER.md)
6. [Setup](docs/delivery/SETUP.md)
7. [Foundation Sprint 0 report](docs/delivery/FOUNDATION-SPRINT-0-REPORT.md)

## Current authority

**Confluence** is canonical for approved product definition (**D1-01**), requirements (**D2**), and architecture directions (**ADR-002** through **ADR-010**, with **ADR-011** pending formal ADR approval). Local files summarize and index; they do not supersede Confluence.

Approved documentation does **not** authorize Phase 1 implementation, Central Identity integration, or use of real accreditation source content without verified rights.

No license has been selected. This repository does not grant rights to its contents or to any accreditation material.

This repository does not depend on AOVIAS Command. Do not copy AOVIAS Command code, database structures, or accreditation content into it.
