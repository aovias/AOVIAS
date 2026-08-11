# Phase 2 — Scheduling Engine (Sprint 5)

**Sprint:** 5  
**Status:** Delivered  
**Principle:** Consume organization working days + holidays when calculating SLA due dates / remaining hours. Chase open SLA daily without duplicating `TaskOverdue` emission.

## Business calendar

### Entity (OrganizationService)

| Entity | Fields |
|--------|--------|
| `OrgHoliday` | `Id`, `OrganizationId`, `Date` (date), `Name`, `CreatedAt` |

Migration: `OrganizationService/Infrastructure/Migrations/20260726160000_Phase2_OrgHolidays.cs`

### Settings source

- `OrganizationSettings.WorkingDaysJson`
- `OrganizationSettings.WorkingHoursStart` / `WorkingHoursEnd`
- `OrgHolidays` table

### APIs

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/api/v1/organizations/{orgId}/settings/holidays?year=` | Org-scoped list |
| POST | `/api/v1/organizations/{orgId}/settings/holidays` | Org admin |
| DELETE | `/api/v1/organizations/{orgId}/settings/holidays/{holidayId}` | Org admin |
| GET | `/internal/organizations/{organizationId}/business-calendar?year=` | Internal for Compliance |

## SLA evaluation (ComplianceService)

`SlaEvaluationService` extended:

1. **Assign SLA due date** via `BusinessCalendarCalculator.AddWorkingHours` (working days + holidays + daily hours window).
2. **Status / remaining / overdue hours** via `CountWorkingHours` against the org calendar from `IBusinessCalendarProvider` (`OrganizationBusinessCalendarProvider` → OrganizationService HTTP, 15‑minute cache).
3. Falls back to Mon–Fri 09:00–18:00 when OrganizationService is unavailable.

Key types:

- `ComplianceService/Application/Scheduling/BusinessCalendarCalculator.cs`
- `ComplianceService/Application/Contracts/IBusinessCalendarProvider.cs`

## Hangfire jobs

| Job ID | Schedule | Behavior |
|--------|----------|----------|
| `task-overdue-escalation` | Daily 05:00 UTC | Emits `TaskOverdue` once per overdue episode (unchanged) |
| `sla-chase` | Daily 06:00 UTC | Re-evaluates open deficiencies (SLA events via evaluation); CAPA-overdue notifications; overdue-task chase notifications **without** re-emitting `TaskOverdue` |

## Tests

- `BusinessCalendarCalculatorTests` — weekend/holiday skip, working-hour counts, SLA status with calendar
- Existing `SlaEvaluationServiceTests` — calendar-hour `CalculateStatus` retained

## Build verify

```bash
dotnet test ComplianceService.Tests --filter "FullyQualifiedName~BusinessCalendarCalculatorTests|FullyQualifiedName~SlaEvaluationServiceTests"
dotnet build CompliancePlatform.slnx -o _build_verify_phase0\P2S5
```
