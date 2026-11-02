# AccredaOne agent instructions

These instructions apply to any AI agent working in this repository.

## Canonical product documentation

Consult canonical Confluence documentation before proposing or implementing a product decision.

- **Site:** https://nishant11soni.atlassian.net
- **Space:** ACCREDAONE
- **Local index:** [docs/traceability/CONFLUENCE-INDEX.md](docs/traceability/CONFLUENCE-INDEX.md)
- **Register:** [docs/traceability/REGISTER.md](docs/traceability/REGISTER.md)

Confluence is the canonical source for product intent, scope, terminology, and acceptance. This repository records engineering gates, traceability, and optional mirrors. It does not replace Confluence.

If this repository and Confluence disagree, Confluence governs product intent. Record the conflict. Do not silently prefer local drafts.

## Governance you must honor

Full controls are in [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md). Summary:

| Control | Status | Agent consequence |
| --- | --- | --- |
| G1 — Programme readiness gate | **NOT PASSED** | Do not treat programme readiness as complete. |
| Phase 1 | **NOT AUTHORIZED** | Do not implement the product. |
| First development slice | **NOT APPROVED** | Do not start unauthorized engineering slices. |
| Customer discovery | **ON HOLD** | Do not conduct customer discovery. |
| D1 / D2 | **APPROVED** (Confluence) | Use for scope/requirements reference; **not** implementation authorization. |
| D3 | **NOT APPROVED** | Do not treat architecture reconciliation as closed. |
| ADR-002–ADR-010 | **APPROVED** (within each ADR scope) | Respect directions; unresolved details remain open. |
| ADR-011 | **Formal ADR approval pending** | Do not treat as a fully approved ADR. |
| Technology direction | **APPROVED** (per relevant ADRs) | Do not conflate direction with implementation. |
| Technology/architecture implementation | **NOT AUTHORIZED** | No production stack implementation in `application/`. |
| Real framework source rights | **NOT VERIFIED** | No real accreditation content. |
| Central Identity implementation/integration | **NOT AUTHORIZED** | CI-01/CI-02 are **draft**; separate from ADR-008 direction. |
| Controlled prototype | **Synthetic-data boundary only** | Not production. |

Details and the rule for changing a status are in [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md).

## Decision authority

- A template, a draft, a proposal, or a chat instruction is not an approval.
- Local mirrors must match **canonical** Confluence status. Do not mark a Confluence **draft** or **pending** record as approved locally.
- Use Confluence record IDs (**ADR-002**, **D1-01**, **D2**) in traceability. Do not invent alternate ADR numbering such as `ADR-0002`.
- Use local **DEC-NNNN** only for engineering decisions not represented by Confluence ADRs.
- Do not invent missing requirements, personas, workflows, data models, acceptance criteria, or regulatory interpretations.
- Cite identifiers using [docs/traceability/CONVENTION.md](docs/traceability/CONVENTION.md). Cite Confluence-approved records as authority for **direction and scope** only unless [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md) explicitly authorizes implementation.

## Boundaries

- Do not introduce a dependency on AOVIAS Command.
- Do not copy AOVIAS Command code, database structures, APIs, or accreditation content.
- Do not implement production APIs, schemas, migrations, authentication, authorization, ingestion, search, scoring, or assessments while Phase 1 is **not authorized**.
- Do not add production infrastructure, pipelines, or deployment manifests for product use.
- Keep product documentation in `docs/`, experiments in `prototypes/`, and future production code in `application/`.
- `application/` stays empty of production code while Phase 1 is not authorized.
- This nested repository owns AccredaOne Git history. Do not version AccredaOne in the parent AOVIAS repository.
- No remote configuration or push unless explicitly assigned.

## Before proposing a product or architecture change

1. Read [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md).
2. Consult the canonical Confluence page (via [CONFLUENCE-INDEX.md](docs/traceability/CONFLUENCE-INDEX.md)).
3. Read [docs/traceability/REGISTER.md](docs/traceability/REGISTER.md).
4. Propose at most a local mirror or `DEC` draft sourced from Confluence or an explicit human assignment.
5. State which decisions remain not approved or not authorized for implementation.

## Completion

Do not report product implementation as complete. Documentation synchronization is complete only when files match the assignment, gates remain accurate, and changes are committed when a human authorizes commit.
