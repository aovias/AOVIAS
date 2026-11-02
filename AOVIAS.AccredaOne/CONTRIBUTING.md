# Contributing to AccredaOne

Read [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md) and [AGENTS.md](AGENTS.md) before changing this repository.

G1 — Programme readiness gate has **not passed**. Phase 1 is **not authorized**. **D1** and **D2** are **approved in Confluence** but do not authorize implementation. Technology and architecture **direction** is approved within relevant ADRs; **implementation** is **not authorized**. Real framework source rights are **not verified**.

## What may be added now

- Documentation that records an explicit human assignment or accurately synchronizes **canonical Confluence** status into [docs/traceability/REGISTER.md](docs/traceability/REGISTER.md) and [docs/traceability/CONFLUENCE-INDEX.md](docs/traceability/CONFLUENCE-INDEX.md).
- Local **DEC** records for engineering decisions not represented by Confluence ADRs.
- Optional local ADR mirrors using **Confluence ADR numbering** (e.g. `ADR-002-title.md`) when assigned; mirror status must match Confluence.
- Draft records copied from `templates/`, left in **Draft** or accurate canonical status, and entered in the register.
- Corrections to foundation documents that preserve accurate gates and do not grant implementation authorization.

## What must wait

Do not add any of the following until [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md) is updated by an explicit human for the relevant control:

- Production APIs, database schemas, or migrations
- Production authentication or authorization implementation
- Framework ingestion, knowledge search, or real source content processing
- Readiness scoring or assessments
- Production infrastructure or deployment for the product
- Application code under `application/`
- Real accreditation standard text, checklists, scores, or other source content without verified rights
- Code, schemas, or content copied from AOVIAS Command
- Central Identity implementation or AccredaOne integration

A prototype under `prototypes/` still requires a written brief, must stay within the **synthetic-data boundary**, and remains an experiment.

## How to add a record

1. Confirm the canonical Confluence source in [docs/traceability/CONFLUENCE-INDEX.md](docs/traceability/CONFLUENCE-INDEX.md).
2. Copy the matching file from `templates/` when creating a local mirror, or add a register/index row when Confluence-only.
3. Use **Confluence record IDs** for ADRs (`ADR-002`, not `ADR-0002`). Use **DEC-NNNN** for local engineering decisions not in Confluence.
4. Set local mirror status to match **canonical** status. Do not label a Confluence-approved record as `Draft` locally.
5. Add or update a row in [docs/traceability/REGISTER.md](docs/traceability/REGISTER.md).
6. Cite Confluence-approved records for scope and direction only unless implementation is explicitly authorized in [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md).

Templates and worked examples are not records. Do not fill gaps in product scope with plausible requirements.

## Commits

Use a concise message that states why the change exists.

When a change depends on approved Confluence scope or direction, add traceability trailers (examples):

```text
Confluence-D1: D1-01
Confluence-D2: D2
Confluence-ADR: ADR-005
DEC: DEC-0001
```

Include only identifiers that apply. Foundation and documentation maintenance that does not implement the product does not invent identifiers to satisfy the trailer rule.

**No commit or push is authorized** unless explicitly assigned. Parent AOVIAS repository staged deletions are outside this nested repository.

## Secrets and local files

Do not commit credentials, tokens, private keys, certificates, or `.env` files. If secret material appears, stop and report the path. Do not print the secret.

## Review expectation

A change is ready for human review when the diff matches its stated task, statuses remain accurate, and no production behavior has been introduced ahead of authorization.

## Addendum — Git repository ownership (2026-11-02)

Product Owner approval (DEV-01 follow-up, Option A):

- **Canonical AccredaOne Git repository path:** `V:\DevelopmentRepo\AOVIAS.Cursor\AOVIAS.AccredaOne`
- AccredaOne is an **independent nested Git repository**. Branches, commits, and history are managed **inside that repository only**.
- Parent **AOVIAS** linked worktrees (`V:\DevelopmentRepo\AOVIAS`, `V:\DevelopmentRepo\AOVIAS.Claude`, `V:\DevelopmentRepo\AOVIAS.Cursor`) must **not** track or independently maintain AccredaOne source files in the parent index.
- A shared parent Git exclusion at `V:\DevelopmentRepo\AOVIAS\.git\info\exclude` (`/AOVIAS.AccredaOne/`) is **intentional** and applies across linked AOVIAS worktrees.
- Legacy `AOVIAS.AccredaOne` directories under Main or Claude that contain only the old parent scaffold README are **not canonical**.
- **Do not** copy, merge, or synchronize AccredaOne files from this nested repository into parent worktree directories.
- All AccredaOne engineering changes must be made **inside this nested repository** (checkout, branch, commit, and review here).

## Addendum — Governance synchronization (2026-11-02)

Product Owner authorization (DEV-01 Phase 2):

- Confluence space **ACCREDAONE** is canonical for product and architecture decisions.
- Local [GOVERNANCE-STATUS.md](GOVERNANCE-STATUS.md), [CONFLUENCE-INDEX.md](docs/traceability/CONFLUENCE-INDEX.md), and [REGISTER.md](docs/traceability/REGISTER.md) reflect synchronized statuses; they do **not** grant implementation authorization.
- Do not infer Phase 1, Central Identity integration, or source-rights clearance from approved documentation alone.
