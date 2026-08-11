# Module Specification: Findings & Deficiencies

[← Module Catalog](../04-Module-Catalog.md)

**Implementation status:** **Complete** (~88%)  
**Routes:** `/app/audit/findings`, DCC embedded UX

## Purpose

Capture findings, promote to deficiencies, manage deficiency lifecycle and RCA (Deficiency Command Center).

## Owns

- `Finding` aggregate (findings workspace)
- `ComplianceDeficiency` + audit trails

## Workflow

Finding → eligibility check → promote → deficiency lifecycle → optional CAPA

See [13-Workflow-Architecture.md](../13-Workflow-Architecture.md).

## Key APIs

- `/findings/workspace/*`
- `/findings/{id}/promote`
- `/deficiencies/*`

## Sprints

- 6: DCC
- 7A/7A1: Findings foundation + UX
- 7B: Promotion
- 8: Lifecycle & RCA
