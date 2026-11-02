# Foundation Sprint 0 report

## Addendum — Historical pointer (2026-11-02)

**Preserve this report as historical audit evidence.** The body below records the **2026-10-31** Foundation Sprint 0 outcome only.

Subsequent **Confluence verification** and Product Owner **DEV-01 Phase 2** governance synchronization supersede **engineering interpretation** of gate statements in this report (for example blanket "architecture not approved" and "MVP not approved"). Current engineering authority is [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md), [../traceability/CONFLUENCE-INDEX.md](../traceability/CONFLUENCE-INDEX.md), and [../traceability/REGISTER.md](../traceability/REGISTER.md).

Git ownership Option A is recorded in [../../CONTRIBUTING.md](../../CONTRIBUTING.md). Parent AOVIAS index reconciliation remains outside this nested repository.

---

Date: 2026-10-31  
Repository: AccredaOne  
Path: `V:\DevelopmentRepo\AOVIAS.Cursor\AOVIAS.AccredaOne`  
Sprint: Foundation Sprint 0

This report lists what the sprint created. It does not approve product scope, architecture, or technology.

## Outcome

AccredaOne is a standalone Git repository with a technology-neutral documentation structure, governance status, agent instructions, templates, and traceability rules.

No production behavior was implemented. The register contains no `DEC`, `REQ`, or `ADR` records. Every gate named in the assignment remains unapproved.

## Governance recorded

| Item | Status |
| --- | --- |
| G1 | NOT PASSED |
| Phase 1 implementation | NOT AUTHORIZED |
| MVP scope | NOT APPROVED |
| Architecture | NOT APPROVED |
| Technology selection | NOT APPROVED |
| NABH content rights | NOT VERIFIED |

The only product statement stored as given is the assigned promise: "AccredaOne transforms fragmented accreditation resources into an accessible, connected and engaging knowledge experience." The assigned category label is Compliance Knowledge & Readiness Platform. Neither statement approves features or an operating model.

## Files created

### Repository control

| File | Purpose |
| --- | --- |
| `README.md` | Repository introduction, layout, and governance banner. Replaces the previous monorepo scaffold README. |
| `CONTRIBUTING.md` | Contributor rules and commit traceability. |
| `AGENTS.md` | Agent instruction to consult canonical Confluence before product decisions. |
| `GOVERNANCE-STATUS.md` | Explicit gate status and the rule for changing it. |
| `.gitignore` | Hygiene patterns. Not a stack selection. |
| `.editorconfig` | Encoding, indentation, and newlines. |
| `.gitattributes` | Store text files as LF. |

### Documentation map

| File | Purpose |
| --- | --- |
| `docs/README.md` | Documentation index and separation rules. |
| `docs/product/README.md` | Local product-documentation home. |
| `docs/requirements/README.md` | `REQ` series rules. |
| `docs/decisions/README.md` | `DEC` series rules. |
| `docs/domain/README.md` | Reserved domain-modelling area. |
| `docs/architecture/README.md` | Architecture area. Architecture is not approved. |
| `docs/architecture/adr/README.md` | `ADR` series rules. |
| `docs/architecture/designs/README.md` | `DES` series rules. |
| `docs/security/README.md` | Security area. No control set is approved. |
| `docs/content-governance/README.md` | Content rights. NABH rights are not verified. |
| `docs/delivery/README.md` | Delivery index. |
| `docs/delivery/SETUP.md` | Setup instructions. |
| `docs/delivery/FOUNDATION-SPRINT-0-REPORT.md` | This report. |
| `docs/delivery/tasks/README.md` | Future `TASK` records. |
| `docs/delivery/test-plans/README.md` | Future `TP` records. |
| `docs/delivery/test-reports/README.md` | Future `TR` records. |
| `docs/traceability/CONVENTION.md` | `DEC`, `REQ`, and `ADR` citation rules. |
| `docs/traceability/REGISTER.md` | Empty identifier register. |

### Templates

| File | Series |
| --- | --- |
| `templates/README.md` | How to copy a template |
| `templates/decision-record.md` | `DEC` |
| `templates/product-requirement.md` | `REQ` |
| `templates/architecture-decision-record.md` | `ADR` |
| `templates/technical-design.md` | `DES` |
| `templates/engineering-task.md` | `TASK` |
| `templates/test-plan.md` | `TP` |
| `templates/test-report.md` | `TR` |

### Separation of concerns

| File | Area |
| --- | --- |
| `docs/` | Product and engineering documentation |
| `prototypes/README.md` | Controlled prototype rules. No prototype is included. |
| `application/README.md` | Reserved production tree. No application code. |

## Conventions established

1. **Three-way separation.** Product documentation stays in `docs/`. Experiments stay in `prototypes/`. Future production code is reserved in `application/`.
2. **Gate file.** `GOVERNANCE-STATUS.md` is the authorization record. Agents do not move a gate to passed, authorized, approved, or verified.
3. **Confluence first.** Agents consult canonical Confluence before proposing or implementing product decisions. This repository does not replace Confluence.
4. **Identifier series.** `DEC`, `REQ`, and `ADR` are the authority series. `DES`, `TASK`, `TP`, and `TR` are supporting series and cannot approve product scope.
5. **Status vocabulary.** `Draft`, `Proposed`, `Approved`, `Rejected`, `Superseded`, `Withdrawn`. New records start as `Draft`. Only a named human sets `Approved`.
6. **Commit trailers.** Changes that depend on approved scope use `DEC:`, `REQ:`, and `ADR:` trailers. Missing or unapproved identifiers are not cited.
7. **Empty register.** No identifier was allocated in this sprint. Examples in the convention are illustrations, not records.
8. **Template rule.** Copying a template does not approve the copy.
9. **Prototype control.** An experiment needs a human assignment and a brief, and it cannot be promoted into `application/` while Phase 1 is unauthorized.
10. **Content rule.** Accreditation content stays out of the repository until NABH content rights are verified and a content decision allows the use.
11. **Command boundary.** This repository does not depend on AOVIAS Command and does not contain Command code, schemas, or content.

## Assumptions

1. The assigned path is the repository root, even though that path is currently nested inside the Cursor worktree directory. Nesting is a filesystem location. It is not a product dependency, and the folder was not moved.
2. Creating the standalone repository includes a local Git history and an initial commit. No remote was requested, so none was added.
3. The previous `README.md` in this folder was a monorepo scaffold. It described a public discovery surface and Command integration questions. Those statements were not adopted as decisions. The scaffold text was replaced.
4. The parent worktree `V:\DevelopmentRepo\AOVIAS.Cursor` already tracked `AOVIAS.AccredaOne/README.md` on branch `agent/cursor/phase1ir-w5-website-migration`. That index entry was left unchanged because that branch is unrelated to this sprint. Detaching the path from the parent repository is a human decision.
5. The Confluence location was not supplied. It is recorded as unknown rather than guessed.
6. `.gitattributes` and a `DEC` template were included so line endings and the `DEC` series are usable. The assignment named ADR, requirement, design, task, test-plan, and test-report templates; the `DEC` template exists because the required traceability convention needs a place to write `DEC` records.
7. `.gitignore` lists outputs from several ecosystems so secrets and build debris stay untracked. Listing a pattern does not approve that ecosystem.
8. No license was selected. The repository grants no rights.
9. "Compliance Knowledge & Readiness Platform" is the category label from the assignment. It is not an approved MVP or domain model.
10. Foundation Sprint 0 itself is not given a `TASK` identifier. The register stays empty.

## Outstanding decisions

| Topic | State |
| --- | --- |
| G1 evidence and criteria | Not in this repository. G1 has not passed. |
| Canonical Confluence space and pages | Not recorded. |
| MVP scope | Not approved. |
| Architecture | Not approved. |
| Technology selection | Not approved. |
| NABH content rights and permitted use | Not verified. |
| Domain model | Not defined. |
| Security and identity model | Not defined. |
| Content provenance and publication rules | Not defined. |
| License | Not selected. |
| Git remote and hosting | Not selected. |
| Whether the parent AOVIAS repository should stop tracking this path | Not decided. Not changed on the current parent branch. |
| Any relationship to other AOVIAS products | Not approved. No dependency was introduced. |

## Explicit non-actions

- No APIs, database schemas, or migrations
- No authentication or authorization
- No framework ingestion or knowledge search
- No readiness scoring or assessments
- No production infrastructure or deployment
- No technology stack
- No invented requirements and no record marked approved
- No AOVIAS Command code, database structures, or accreditation content

## Readiness for the next phase

The repository can accept product-definition drafts and, later, architecture proposals without changing these gates. The next phase still needs canonical Confluence, human decisions, and updates to `GOVERNANCE-STATUS.md` before any gate can be treated as passed or approved.
