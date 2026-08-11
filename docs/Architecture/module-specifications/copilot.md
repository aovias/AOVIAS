# Module Specification: Compliance Copilot

[← Module Catalog](../04-Module-Catalog.md) · [AI Architecture](../15-AI-Copilot-Architecture.md)

**Implementation status:** **Complete** (Sprint 13A–13F, DB 13G)  
**Route:** `/app/copilot`

## Purpose

Persona-driven compliance assistant: recommendations, drafts, approvals, activity, analytics — without autonomous operational writes.

## Responsibilities

- Rule-based recommendations from SSOT reads
- Template draft generation
- Proposal approval workflow
- Persona workspace experience
- Effectiveness observability

## Does NOT Own

- Task/evidence/deficiency records (except via executed proposals)
- Hospital operational analytics SSOT

## Key Files

| Layer | Path |
|-------|------|
| Domain | `ComplianceService/Domain/Copilot/` |
| APIs | `Controllers/V1/Copilot/*` |
| Gateway | `Gateway/Controllers/CopilotController.cs` |
| UI | `WebApp/src/app/features/copilot/` |

## APIs

See [11-API-Architecture.md](../11-API-Architecture.md) Copilot section.

## Known Gaps

- Empty data until `POST recommendations/refresh`
- Activity recording gaps (TD-H02)
- Analytics N+1 (TD-H01)
