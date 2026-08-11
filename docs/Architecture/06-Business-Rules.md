# Business Rules

[← README](./README.md) · [Decision Log](./24-Decision-Log.md)

Authoritative business rules inferred from domain code, sprint reports, and constitution. **Rationale** included for each.

---

## Governance vs Execution

| Rule | Rationale |
|------|-----------|
| Maturity stages and unlock rules are **governance**; OE/task work is **execution** | Prevents conflating “allowed to proceed” with “work completed” |
| Stage unlocking uses audited policies (`NabhJourneyPolicy`, etc.) | Accreditation frameworks differ materially |

---

## Findings vs Deficiencies

| Rule | Rationale |
|------|-----------|
| **Findings** are operational observations in the findings workspace | Supports internal QA and audit simulation without forcing deficiency lifecycle |
| **Deficiencies** are promoted, governed records with lifecycle and RCA | Accreditation traceability requires stable deficiency IDs |
| Promotion is explicit (`Finding` → deficiency) with eligibility checks | Avoids duplicate deficiency creation |

---

## Evidence

| Rule | Rationale |
|------|-----------|
| Evidence is owned by the **task or OE context** that requested it | Accountability for collection |
| **Verification** authority lives in evidence module / review queue | Single verification SSOT |
| Binder is **read-oriented** presentation; uploads go through library/tasks | Separation of audit view vs operational upload |

---

## Recommendations & Copilot

| Rule | Rationale |
|------|-----------|
| Recommendation engine is **deterministic** (rule-based), not free-form LLM for core recommendations | Auditability, repeatability |
| Recommendations **do not write** operational data directly | AI trust model |
| **Proposals** require human approve/reject before execute | Human-in-the-loop |
| Copilot reads operational SSOT; writes only Copilot aggregates + approved executions | Boundary integrity |

---

## CAPA Lifecycle

| Rule | Rationale |
|------|-----------|
| CAPA links to deficiencies / findings origin | Traceability to root cause |
| Effectiveness tracking is part of CAPA domain | Accreditation expects verification of corrective action |

---

## Assignment Routing

| Rule | Rationale |
|------|-----------|
| Routing uses department ownership catalogs and labels | Hospital org structure varies |
| Routing decisions are logged (`AssignmentRoutingLog`) | Audit trail for workload disputes |

---

## SLA & Escalation

| Rule | Rationale |
|------|-----------|
| SLA policies attach to escalation policies | Unified breach → escalation path |
| Escalation levels are ordered | Predictable escalation chains |

---

## Analytics SSOT

| Rule | Rationale |
|------|-----------|
| `AnalyticsDailySnapshot` is the **analytics write model** for trends | Avoid ad-hoc KPI SQL in UI |
| Analytics APIs are **read-only** relative to operational domains | CQRS separation |
| Copilot analytics is **Copilot effectiveness** not hospital operational SSOT | TD-H04 boundary |

---

## Stage Unlocking

| Rule | Rationale |
|------|-----------|
| Unlock requires policy evaluation against obligation/score state | Framework-specific gates |
| Unlock attempts are audited | Governance transparency |

---

## AI Trust Model

| Rule | Rationale |
|------|-----------|
| AI Orchestrator may explain/suggest; Copilot proposes; humans approve | Regulatory and clinical risk |
| Sidebar AI assistant is **conversational**; Copilot workspace is **structured** | Different UX contracts |

---

## Multi-Tenancy

| Rule | Rationale |
|------|-----------|
| `organizationId` from JWT scopes all compliance data | Tenant isolation |
| `hospitalId` header/context scopes hospital-scoped operations | Multi-hospital orgs |

---

## API Response Envelope

| Rule | Rationale |
|------|-----------|
| All public APIs return `ApiResponse<T>` with standardized codes | Client interceptor handling (`apiResponseInterceptor`) |

See also: [decisions/](./decisions/) for ADRs on these rules.
