# Sprint 13 — Compliance Copilot

**Status:** Complete (13A–13F) + migration 13G  
**Full reports:** `ComplaincePlatform/docs/Sprint13A-*` through `Sprint13F-*`

## Problem

Quality teams need persona-aware guidance across readiness, evidence, CAPA, and SLA — without unsafe autonomous AI actions.

## Solution

| Phase | Deliverable |
|-------|-------------|
| 13A | Copilot domain, personas, base APIs |
| 13B | 17-rule recommendation engine |
| 13C | 4 draft templates + versioning |
| 13D | Proposal approve/reject/execute |
| 13E | Persona workspace, activity feed |
| 13F | Analytics overview, trends, impact |
| 13G | EF migration consolidating Copilot tables |

## Architecture Decisions

- Deterministic rules ([ADR-005](../decisions/ADR-005-deterministic-copilot-recommendations.md))
- Human approval ([ADR-006](../decisions/ADR-006-human-approval-copilot-execution.md))

## Files Affected (representative)

- `ComplianceService/Domain/Copilot/*`
- `ComplianceService/Controllers/V1/Copilot/*`
- `Gateway/Controllers/CopilotController.cs`
- `WebApp/src/app/features/copilot/*`
- Migration `20260625101307_Sprint13G_ModelSnapshotSync`

## Business Impact

Single Copilot workspace in sidebar; recommendations deep-link to operational screens.

## Risks / Debt

TD-H01–H04 in technical debt register.

## Future Work

Scheduled snapshots, LLM enrichment, external auditor persona, Sprint 14 integration.

## Lessons Learned

Manual EF migrations without snapshot sync block deployment — use `dotnet ef migrations add`.
