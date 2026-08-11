# Phase 2 — CAPA Management (Sprint 1)

**Sprint:** 1  
**Status:** Delivered  
**Principle:** Extend `ComplianceDeficiency` CAPA fields — do **not** revive orphan `Capa` table writes.

## Model (SSOT)

CAPA lifecycle lives on `ComplianceDeficiency`:

| Field / method | Purpose |
|----------------|---------|
| `CapaNumber` | Human CAPA id (`CAPA-{yyyy}-{seq:D5}`), unique per organization when set |
| `OwnerAcceptedAt` / `OwnerAcceptedBy` | Owner acceptance |
| `OwnerAcceptanceRejected` (+ rejected at/by/reason) | Ownership rejection |
| `LinkedSourceType` | Optional source label; typed provenance remains `OriginType` |
| `AcceptOwnership` | Records acceptance; `Assigned` → `InProgress` |
| `RejectOwnership` | Clears acceptance; leaves assignment |
| `AssignCapaNumber` | One-time CAPA number assignment |
| `EnsureCloseable` / `Close` | Close gates before terminal close |

### Origin types

`DeficiencyOriginType`: EvaluationEngine, Manual, AuditFinding, Incident, Risk, Complaint, ObjectiveElement, PolicyDocument, Training.

AuditFinding-origin open CAPAs are excluded from the unique-open-per-obligation index so one finding can spawn multiple open CAPAs.

## Workflow

```text
Finding (Accepted)
  → Promote (1..N) → ComplianceDeficiency (OriginType=AuditFinding, CapaNumber allocated)
  → Assign owner
  → Accept ownership (Assigned → InProgress)  |  Reject ownership
  → Corrective / Preventive / Effectiveness
  → Verified
  → Close (EnsureCloseable gates)
```

### Close gates (`EnsureCloseable`)

1. Status must be **Verified**.
2. If any CAPA activity exists (number, corrective/preventive started/summaries, etc.), corrective action must be **Completed**.
3. If preventive action ≠ NotStarted, it must be **Completed**.
4. Effectiveness review must be **Effective**, **NotEffective**, or **NotRequired**.

## APIs

### ComplianceService (internal)

| Method | Route |
|--------|--------|
| POST | `/internal/v1/deficiencies/{id}/accept` |
| POST | `/internal/v1/deficiencies/{id}/reject-ownership` |
| PATCH | `/internal/v1/deficiencies/{id}/corrective-action` (allocates `CapaNumber` when CAPA starts) |
| PATCH | `/internal/v1/deficiencies/{id}/status` (Close runs `EnsureCloseable`) |
| GET | command-center dashboard / workspace (includes CAPA number + metrics) |

### Gateway (public)

| Method | Route |
|--------|--------|
| POST | `/api/v1/deficiencies/{id}/accept` |
| POST | `/api/v1/deficiencies/{id}/reject-ownership` |

Finding promotion continues via existing Findings promote API; multi-CAPA allowed for Accepted findings. CAPA numbers use `IPlatformSequenceStore` prefix `CAPA-{hospitalId:N}`.

## Events

| Event | Value |
|-------|-------|
| `DeficiencyAcceptedByOwner` | 40 |
| `DeficiencyOwnershipRejected` | 41 |

Emitted via `DeficiencyComplianceEventService` / `IComplianceEventEmitter`.

## Metrics (`CapaMetricsDto` / DCC dashboard)

- Aging buckets: Within30, Days31To60, Days61To90, Over90 (open CAPAs by `DetectedDate` age)
- `AverageOpenCapaAgeDays`
- `DepartmentOverdueCapas` — `{ Label, Count }`

## Graph

Finding → CAPA edges prefer `ComplianceDeficiencies` where `OriginId == finding.Id` (and `OriginType == AuditFinding`). Legacy `Capas` rows are secondary only when no deficiency CAPAs exist.

## WebApp (DCC)

Path: `WebApp/src/app/features/audit/deficiency-command-center/`

- Accept / Reject ownership actions when status is Assigned and not yet accepted
- CAPA number on detail header and grid
- Dashboard aging buckets + overdue department highlight when present

## Migration

`ComplianceService/Migrations/20260726140000_Phase2_CapaLifecycle.cs`

- Adds CAPA ownership / number columns
- Unique filtered index `(OrganizationId, CapaNumber)` where `CapaNumber IS NOT NULL`
- Updates `IX_ComplianceDeficiencies_UniqueOpenPerObligationType` filter to allow multiple open AuditFinding CAPAs
