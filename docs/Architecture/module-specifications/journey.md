# Module Specification: Journey

[← Module Catalog](../04-Module-Catalog.md)

**Implementation status:** **Complete** (~92%)  
**Routes:** `/app/journey/*`

## Purpose

Execute accreditation journey: chapters, standards, objective elements, stage progression, OE execution wizard.

## Owns

- `ComplianceJourney`, journey history, framework configuration
- Journey policy evaluation (NABH, NABL, etc.)

## Dependencies

- KnowledgeBase (framework structure)
- Organization (hospital context)
- Tasks/Evidence (execution outputs)

## Key APIs

- Gateway `/journeys/*`, `/journey/*`
- ComplianceService `JourneysController`

## UI

- `JourneyWorkspaceStore` (signals)
- `OeExecutionWizardComponent`, `ExecutionScreenComponent`

## Future

- Additional framework packs (JCI, ISO)
- External auditor journey view (read-only)
