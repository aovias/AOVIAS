# Phase 4 — Root Cause Analysis (Structured) (Sprint 3)

**Sprint:** 3  
**Status:** Delivered  
**Principle:** Guided structured RCA — not free-text / raw JSON only. CAPA actions from RCA land on `ComplianceDeficiency`.

## Typed API contract

`SaveRcaRequestDto` / `IncidentRcaDto`:

| DTO | Purpose |
|-----|---------|
| `FiveWhysDto` | Why1–Why5 + FinalRootCause |
| `CauseCategoriesDto` | Human / Process / Environmental / Equipment / Organization lists |
| `FishboneSchemaDto` | Future-ready Ishikawa: Effect + Categories[{ Name, Causes[] }] |
| `CorrectiveActionSummary` | Promoted onto deficiency corrective CAPA |
| `PreventiveActionSummary` | Promoted onto deficiency preventive CAPA |
| `RootCauseCategory` | Maps to `RootCauseCategory` enum on promote |

Legacy `*Json` fields remain for backward-compatible reads/writes when typed objects are omitted. Persistence still uses JSON columns on `IncidentRca` via `IncidentRcaMapper`.

## Persist → promote

1. Investigator saves RCA in WebApp guided workspace (`/app/patient-safety/rca`).
2. On `PromoteToDeficiencyAsync`, service loads `Incident.RootCauseAnalysis` and:
   - `ComplianceDeficiency.UpdateRca(...)` (5 Whys + summaries + category)
   - `UpdateCorrectiveAction` / `UpdatePreventiveAction` when summaries present
   - `SetLinkedSourceType("Incident")`
3. Never creates rows in orphan `Capas` table.

## WebApp

`features/patient-safety/pages/rca-workspace-page.component.ts`:

- Structured inputs for 5 Whys (not JSON textarea)
- Cause category comma-separated fields
- Fishbone effect + category causes
- Corrective / preventive action textareas
- Closed/cancelled incidents are read-only

## Methods (`RcaMethod`)

| Value | UI |
|-------|----|
| `FiveWhys` | Five sequential why fields |
| `Fishbone` | `FishboneSchemaDto` editor |
| `ContributingFactors` | Cause categories focus |

## Related

- [03-incident-management.md](./03-incident-management.md)
- Deficiency RCA workspace (Sprint 8): deficiency command-center RCA patches remain separate SSOT for deficiency-native RCA.
