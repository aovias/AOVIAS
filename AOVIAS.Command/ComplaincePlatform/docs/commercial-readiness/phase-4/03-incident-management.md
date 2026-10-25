# Phase 4 — Incident Management Harden (Sprint 3)

**Sprint:** 3  
**Status:** Delivered  
**Principle:** EXTEND Sprint 16 Patient Safety incidents — do **not** introduce a second CAPA engine. CAPA SSOT = `ComplianceDeficiency`.

## Model (SSOT)

| Entity / field | Purpose |
|----------------|---------|
| `Incident` | Operational patient-safety capture aggregate |
| `Incident.FindingId` | Optional human-approved link to Finding |
| `Incident.DeficiencyId` | CAPA SSOT link → `ComplianceDeficiency` |
| `Incident.CapaId` | **Legacy only** — never write Capas-table ids; prefer `DeficiencyId` |
| `IncidentRca` | Structured RCA (typed API + JSON persistence) |
| `IncidentRca.CorrectiveActionSummary` / `PreventiveActionSummary` | Applied to deficiency CAPA fields on promote |

### Promotion bridge

```text
Incident (human approve)
  → Finding (IncidentPromotionService.PromoteToFindingAsync)
  → ComplianceDeficiency (FindingPromotionService — never Capas table)
  → RCA corrective/preventive copied onto deficiency CAPA fields
```

Incidents **never** auto-promote. Closed / cancelled incidents are immutable (`Incident.EnsureMutable`).

## High severity notifications

On create (and severity raise via patch) when severity is **Major / Critical / Sentinel**, publish:

| Field | Value |
|-------|-------|
| `EventType` | `HighSeverityIncident` |
| Channel | `IComplianceNotificationPublisher` |

## Graph

`EnterpriseComplianceGraphService`:

- Incident → Deficiency / CAPA (via `DeficiencyId`, CAPA SSOT label)
- Deficiency ← Incident (reverse edge on `incident.DeficiencyId`)
- Legacy `CapaId` → Capas table only when `DeficiencyId` is absent

## APIs

| Method | Route | Notes |
|--------|-------|-------|
| POST | `/api/v1/incidents` | High-severity notify |
| PATCH | `/api/v1/incidents/{id}` | Rejects closed/cancelled |
| POST | `/api/v1/incidents/rca` | Typed structured RCA |
| POST | `/api/v1/incidents/promote-finding` | Finding only |
| POST | `/api/v1/incidents/promote-deficiency` | Deficiency CAPA SSOT + RCA apply |

## Migration

`20260726170000_Phase4_IncidentRcaCapaFields` — `CorrectiveActionSummary`, `PreventiveActionSummary`, `RootCauseCategory` on `IncidentRcas`.

## Tests

`IncidentCommandServiceTests`, `IncidentPromotionServiceTests`:

- Closed incident immutable on patch / RCA / promote
- Promote finding without Capas writes
- Promote deficiency applies RCA corrective/preventive to `ComplianceDeficiency`

## Related

- [04-root-cause-analysis.md](./04-root-cause-analysis.md)
- Sprint 16 report: `docs/Sprint16-Incident-Management-Patient-Safety-Report.md`
- CAPA SSOT: `docs/commercial-readiness/phase-2/01-capa-management.md`
