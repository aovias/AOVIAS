# Phase 1 — Framework Hierarchy

**Sprint:** 2 (docs; structure APIs pre-existing)  
**Status:** Delivered (unchanged contracts)

## Hierarchy

```
ComplianceFramework
  └── ComplianceChapter
        └── ComplianceStandard
              └── ObjectiveElement
```

Unique framework identity: `(Name, Version)`. Catalog Status lives on the framework row only; chapters/OEs are not independently published.

## APIs (preserved)

| Surface | Path | Notes |
|---------|------|-------|
| Structure v1.1 | Gateway → ComplianceService structure endpoints | Unchanged by Phase 1 lifecycle |
| Questionnaire | `GET /api/v1/frameworks/{frameworkId}/questionnaire` | Unchanged |
| Enroll | `POST /api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll` | Now also requires catalog Status=Active |

## Rules

1. Hierarchy content (chapters/standards/OEs) is managed by master-data seeders / catalog replication — not by lifecycle endpoints.  
2. Activating a new version does not rewrite OE rows; hospitals stay on the enrolled `FrameworkId`.  
3. Sibling retirement affects catalog visibility / enroll only; existing journeys remain on their framework id.

## Related

See `02-framework-management.md` for catalog Status transitions.
