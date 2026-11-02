# Product and governance decisions

Decision records use the identifier series **`DEC-NNNN`**.

**Confluence** (space **ACCREDAONE**) is the **canonical** repository for approved **product definition**, **requirements**, and **architecture decision records** (`ADR-002` and related IDs). See [../traceability/CONFLUENCE-INDEX.md](../traceability/CONFLUENCE-INDEX.md) and [../traceability/REGISTER.md](../traceability/REGISTER.md).

**Local `DEC-NNNN` files** in this folder record **engineering and repository decisions** that are **not** already represented by canonical Confluence records. **No local `DEC` files exist yet**, and **no local `DEC` is approved** — that does **not** mean no Confluence decisions are approved.

| Canonical area | Where authority lives | Examples (see register) |
| --- | --- | --- |
| Product definition | Confluence **D1-01** | Approved v1.0 |
| Requirements | Confluence **D2** | Approved v1.0 |
| Architecture reconciliation | Confluence **D3** | **Not approved** |
| Architecture directions | Confluence **ADR-002**–**ADR-010** | Approved within each ADR's scope |
| Agentic architecture | Confluence **ADR-011** | Formal ADR approval **pending** |

Architecture decisions in Confluence use the **`ADR`** series; pointers and rules live under [../architecture/adr/](../architecture/adr/README.md). Product requirements use **`REQ`** under [../requirements/](../requirements/README.md).

## Rules

- Create a local record from [../../templates/decision-record.md](../../templates/decision-record.md) when the decision is local/engineering scope and not a Confluence ADR.
- A **`DEC`** becomes authority only when its status is **`Approved`** and [../../GOVERNANCE-STATUS.md](../../GOVERNANCE-STATUS.md) agrees with that approval.
- Do not use a **`DEC`** to substitute for a Confluence **`ADR`**. **Technology and architecture approval** must be described **within the scope of the relevant ADR** (and **D3** remains **not approved**).
- Do not convert an open question into a decision by writing it down.

Store records in this folder as `DEC-NNNN-short-title.md`.
