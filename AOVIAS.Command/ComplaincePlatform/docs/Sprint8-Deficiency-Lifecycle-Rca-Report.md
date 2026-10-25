# Sprint 8 — Deficiency Lifecycle & Root Cause Analysis

**Status:** Complete  
**Scope:** Mature the deficiency domain for future CAPA, escalation, SLA, and analytics — without implementing those workflows.

---

## Phase 1 — Domain Audit (Summary)

### Existing (pre-Sprint 8)

| Area | State |
|------|--------|
| `ComplianceDeficiency` | Core entity with severity, status, assignment, root cause category enum |
| Task linkage | `SourceDeficiencyId` on remediation tasks; lifecycle sync via `DeficiencyTaskLifecycleService` |
| Evidence / verification | Projected onto deficiency rows from obligation + evidence pipeline |
| Origin tracing | Sprint 7B: `OriginType`, `OriginId`, promotion metadata |
| DCC | Grid, dashboard KPIs (severity/origin), detail panel with basic root cause dropdown |

### Gaps addressed in Sprint 8

- No RCA narrative fields or 5 Whys
- No risk matrix (likelihood × impact)
- No remediation planning storage
- No effectiveness review placeholder
- Lifecycle labels not aligned to operational states (Awaiting Evidence, Verification Pending)
- No deficiency-specific audit timeline
- Dashboard lacked lifecycle/risk KPIs and analytics breakdowns
- Evidence approved mapped to `ReviewPending` instead of `Verified`

---

## Phase 2 — Lifecycle State Machine

**Enum:** `DeficiencyLifecycleStatus` (UI/analytics canonical labels)

**Persisted:** `DeficiencyStatus` (unchanged storage; extended with `Cancelled = 7`)

| Lifecycle (display) | Persisted status |
|---------------------|------------------|
| Identified | `Identified` |
| Assigned | `Assigned` |
| In Progress | `InProgress` |
| Awaiting Evidence | `EvidenceSubmitted` |
| Verification Pending | `ReviewPending` |
| Verified | `Verified` |
| Closed | `Closed` |
| Cancelled (optional) | `Cancelled` |

**Rules:** `DeficiencyLifecycleTransitions` (Domain)

- Monotonic forward progression (with regression allowed to `InProgress` from evidence rejection)
- Terminal states: `Closed`, `Cancelled`
- Risk score: `Likelihood × Impact` → Low / Medium / High / Critical

---

## Phase 3 — Lifecycle Automation

`DeficiencyTaskLifecycleService` integrates with existing task/evidence pipeline:

| Trigger | Deficiency transition | Audit action |
|---------|----------------------|--------------|
| Task owner assigned | `Identified` → `Assigned` | `Assigned` |
| Task started | → `InProgress` | `InProgress` |
| Evidence uploaded | → `EvidenceSubmitted` | `EvidenceUploaded` |
| Evidence approved | → `Verified` | `Verified` |
| Obligation verified + deficiency verified | → `Closed` | `Closed` |
| Manual assign | → `Assigned` | `Assigned` / `OwnerChanged` |

No manual sync — reuses existing projection/outbox patterns.

---

## Phases 4–9 — Domain Extensions

### RCA workspace (`ComplianceDeficiency` + detail DTO)

- Problem Statement, Root Cause Summary, Impact Description
- Corrective / Preventive action summaries
- Why 1–5, Final Root Cause
- Root Cause Category (existing enum)

### Ownership

- Owner (reuses `AssignedTo` + task owner projection)
- Department, Assigned Date, Target Resolution Date, Actual Resolution Date

### Risk assessment

- `RiskLikelihood`, `RiskImpact` (1–5), `RiskScore`, `AssessedRiskLevel`

### Remediation planning

- Action Plan, Expected Outcome, Remediation Target Completion, Success Criteria

### Effectiveness review placeholder

- `EffectivenessReviewStatus`: NotRequired, Pending, Effective, NotEffective

---

## Phase 10 — Timeline

**Entity:** `DeficiencyAuditTrail`  
**Service:** `IDeficiencyAuditTrailService`

Captured actions: Assigned, OwnerChanged, InProgress, EvidenceUploaded, Verified, Closed, RootCauseUpdated, RiskUpdated, etc.

Loaded in workspace detail (`Timeline` on `DeficiencyWorkspaceDetailDto`).

---

## Phase 11–12 — Dashboard & Analytics

**New KPIs:** Awaiting Assignment, In Progress, Awaiting Evidence, Verification Pending, Verified, High Risk, Critical Risk

**Breakdowns:** By Root Cause Category, Risk Level, Lifecycle Status, Department, Stage

---

## Phase 13 — API (extended, non-breaking)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/deficiencies/command-center/dashboard` | Extended KPIs + breakdowns |
| GET | `/deficiencies/command-center/workspace` | Grid with lifecycle/ownership/risk |
| GET | `/deficiencies/command-center/workspace/{id}` | Full detail + timeline |
| PATCH | `/deficiencies/{id}/root-cause` | Legacy category-only (unchanged) |
| PATCH | `/deficiencies/{id}/rca` | Full RCA + 5 Whys |
| PATCH | `/deficiencies/{id}/remediation` | Risk, ownership, plan, effectiveness |

Gateway proxies all routes via existing MediatR handlers.

---

## Frontend (DCC)

- **Dashboard:** Lifecycle + risk KPI row; analytics breakdown cards
- **Grid:** Lifecycle, risk level, department, target resolution columns
- **Detail panel:** RCA section, 5 Whys, ownership, risk matrix, remediation plan, effectiveness status, timeline
- **Export CSV:** Extended columns for Sprint 8 fields

---

## Validation Flow

```
Finding → Promoted → Deficiency (Identified)
Task Assigned → Assigned
Task Started → In Progress
Evidence Uploaded → Awaiting Evidence (EvidenceSubmitted)
Evidence Approved → Verified
Obligation Verified → Closed → Readiness / Stage Unlock evaluated
```

---

## Migration

`20260624050819_Sprint8_DeficiencyLifecycleRca`

- Adds RCA, ownership, risk, remediation, effectiveness columns to `ComplianceDeficiencies`
- Creates `DeficiencyAuditTrails` table
- Updates unique open-deficiency index filter (excludes Closed + Cancelled)

---

## Key Files

| Layer | Path |
|-------|------|
| Lifecycle | `Domain/DeficiencyLifecycleTransitions.cs` |
| Entity | `Domain/ComplianceDeficiency.cs` |
| Audit | `Domain/DeficiencyAuditTrail.cs`, `Application/Services/DeficiencyAuditTrailService.cs` |
| Commands | `Application/Services/PatchDeficiencyRcaCommandHandler.cs` |
| Query | `Application/Services/DeficiencyCommandCenterQueryService.cs` |
| DTOs | `Application/DTOs/DeficiencyCommandCenterDtos.cs` |
| Frontend | `WebApp/.../deficiency-command-center/*` |
| Tests | `ComplianceService.Tests/DeficiencyLifecycleTransitionsTests.cs` |

---

## Out of Scope (by design)

- CAPA workflows
- Escalation / SLA engines
- AI-assisted RCA
- DCC layout redesign

These sprints are enabled by the data model and lifecycle foundation delivered here.
