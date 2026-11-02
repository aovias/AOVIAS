# Decision traceability convention

This convention is in force for the AccredaOne repository. Adopting it does not approve any product, architecture, or technology decision.

Engineering changes that implement or amend product scope must link to approved `DEC`, `REQ`, and `ADR` identifiers. Supporting records (`DES`, `TASK`, `TP`, `TR`) point at those identifiers. They do not replace them.

## Identifier series

| Series | Meaning | Location | Can authorize product scope |
| --- | --- | --- | --- |
| `DEC-NNNN` | Product or governance decision | `docs/decisions/` | Yes, only when `Approved` |
| `REQ-NNNN` | Product requirement | `docs/requirements/` | Yes, only when `Approved` |
| `ADR-NNNN` | Architecture decision | `docs/architecture/adr/` | Yes, only when `Approved` |
| `DES-NNNN` | Technical design | `docs/architecture/designs/` | No. Elaborates approved `REQ` and `ADR` records. |
| `TASK-NNNN` | Engineering task specification | `docs/delivery/tasks/` | No. Assigns work. |
| `TP-NNNN` | Test plan | `docs/delivery/test-plans/` | No. |
| `TR-NNNN` | Test report | `docs/delivery/test-reports/` | No. |

`NNNN` is a four-digit decimal, starting at `0001` for the first real record in that series. Identifiers are immutable. Do not reuse or renumber them. Gaps are allowed when a draft is withdrawn.

## Status values

| Status | Meaning |
| --- | --- |
| `Draft` | Being written. Not authority. |
| `Proposed` | Ready for human decision. Not authority. |
| `Approved` | Accepted by a named human, with an approval reference in the record. |
| `Rejected` | Declined. Not authority. |
| `Superseded` | Replaced by a named later record. Not authority. |
| `Withdrawn` | Removed from consideration by a human. Not authority. |

New records start as `Draft`. Agents must not set `Approved`.

`Approved` on a single record does not override [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md). Phase 1 stays unauthorized until that file says otherwise. An approved Confluence **ADR direction** does not authorize **implementation** unless governance status explicitly authorizes that class of work.

## How to cite authority

In the record, list related identifiers and the status you relied on:

```text
Authorizing DEC: DEC-0007 (Approved)
Authorizing REQ: REQ-0012 (Approved)
Authorizing ADR: none
```

Use `none` when that series does not apply. Do not cite a missing identifier. Do not cite `Draft`, `Proposed`, `Rejected`, `Superseded`, or `Withdrawn` as authority.

In a Git commit that depends on approved scope, add trailers:

```text
DEC: DEC-0007
REQ: REQ-0012
```

Omit a trailer when that series does not apply.

Documentation and foundation maintenance that does not implement product behavior does not invent identifiers to fill trailers. The human assignment is the authority for that maintenance, and the commit message says so.

## Register

Every real identifier is listed in [REGISTER.md](REGISTER.md). Creating a file without a register row is incomplete. A register row does not approve the record.

## Checks before implementation

1. Each cited `DEC`, `REQ`, and `ADR` exists and is `Approved`.
2. [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md) allows the class of work.
3. The change stays inside the cited records.
4. NABH content is absent unless content rights are verified and a content `DEC` allows that use.

The examples `DEC-0007` and `REQ-0012` above illustrate the citation shape. They are not records.

## Addendum — Confluence-primary traceability (2026-11-02)

Product Owner approval (DEV-01 Phase 2, Decision E):

1. **Confluence record IDs and page IDs are canonical** for product and architecture decisions listed in [CONFLUENCE-INDEX.md](CONFLUENCE-INDEX.md).
2. **Local ADR mirror filenames** use Confluence numbering: `ADR-002-short-title.md`. Do **not** invent `ADR-0002` style IDs for Confluence ADRs.
3. **D1-01** and **D2** identifiers and D2 requirement IDs are preserved from Confluence when mirroring.
4. **DEC-NNNN** is for local engineering decisions **not** represented by Confluence ADRs.
5. Track **canonical status**, **local mirror status**, and **implementation authorization** independently in [REGISTER.md](REGISTER.md).
6. A local mirror must **not** imply approval when Confluence status is draft, pending, or under review.
7. Commit trailers may cite Confluence IDs, for example `Confluence-ADR: ADR-005`, `Confluence-D1: D1-01`, `Confluence-D2: D2`.

Local index and register rows were synchronized on **2026-11-02**. Confluence page version numbers remain **OPEN** where not verified live in that session.
