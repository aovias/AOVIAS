# Sprint 11 — CAPA Domain Audit

## Scope

CAPA is embedded in **Issues & Risks (Deficiency Command Center)**. `ComplianceDeficiency` remains the primary remediation object; tasks, evidence, and verification workflows are reused without duplication.

## Existing CAPA-Ready Fields (Sprint 8)

| Area | Fields | Notes |
|------|--------|-------|
| RCA | `RootCauseCategory`, `ProblemStatement`, `RootCauseSummary`, `ImpactDescription`, `Why1–5`, `FinalRootCause` | Full 5-Why support |
| Summaries | `CorrectiveActionSummary`, `PreventiveActionSummary` | Text only; Sprint 11 formalizes ownership/dates/status |
| Remediation | `ActionPlan`, `ExpectedOutcome`, `RemediationTargetCompletion`, `SuccessCriteria` | Task-aligned plan |
| Effectiveness | `EffectivenessReviewStatus` | Placeholder enum; Sprint 11 activates workflow |

## Missing Fields (Added Sprint 11)

| Field | Purpose |
|-------|---------|
| `CorrectiveActionOwner`, `StartDate`, `TargetDate`, `CompletedDate`, `Status` | Formal corrective action tracking |
| `PreventiveActionOwner`, `TargetDate`, `CompletedDate`, `Status`, `SuccessCriteria` | Formal preventive action tracking |
| `EffectivenessReviewScheduledDate`, `DaysOffset`, `Date`, `Reviewer`, `Notes`, `Outcome` | Effectiveness review activation |
| `IsRecurringIssue`, `RecurrenceReason`, `RecurrenceDetectedAt` | Recurrence persistence |

## Reuse Opportunities

- **Tasks** — primary work object; no duplicate CAPA tasks
- **Evidence** — proof object; lifecycle step derived from deficiency status
- **Verification** — validation authority; unchanged
- **SLA Engine (Sprint 10)** — CAPA patch handlers call `ISlaEvaluationService.EvaluateAsync`
- **Audit trail** — timeline events: `CorrectiveActionCreated/Completed`, `PreventiveActionCreated/Completed`, `EffectivenessReviewScheduled/Completed`, `RecurrenceDetected`
- **Standalone `Capa` entity** — Control Center only; **not** used for deficiency CAPA

## Lifecycle Integration

```
Deficiency → RCA → Corrective Action → Preventive Action → Evidence → Verification → Effectiveness Review → Closed
```

State machine: existing `DeficiencyStatus` transitions unchanged; CAPA steps are orthogonal metadata on the same deficiency.

## API Extensions (Non-Breaking)

- `PATCH /deficiencies/{id}/corrective-action`
- `PATCH /deficiencies/{id}/preventive-action`
- `PATCH /deficiencies/{id}/effectiveness-review`
- `GET /deficiencies/{id}/capa`

## Analytics Foundation (Sprint 12 Prep)

`CapaQueryService.GetMetricsAsync` exposes: effectiveness rate, recurrence rate, average closure days, top root causes, department CAPA performance.
