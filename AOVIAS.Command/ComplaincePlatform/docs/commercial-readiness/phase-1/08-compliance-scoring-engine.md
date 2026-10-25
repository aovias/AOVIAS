# Phase 1 — Compliance Scoring Engine

**Sprint:** 5 (docs) / prior scoring work reused  
**Status:** Delivered (existing engine; no greenfield rewrite)

## Source of truth

| Component | Role |
|-----------|------|
| `IComplianceScoreService` / `ComplianceScoreService` | Canonical percentages for screens |
| `IComplianceScoringService` / `ComplianceScoringService` | Domain scoring calculations |
| `ComplianceSnapshotProjector` + snapshot queue | Async projection after lifecycle / evidence events |
| Dashboard experience / program overview snapshot APIs | Read models for WebApp |

All HTTP surfaces must call scoring services or snapshots — never invent percentages in controllers.

## Triggers that enqueue recalculation

- Evidence registration / review (approve, reject, needs-more)
- Obligation projection after evidence / deficiency changes
- Framework / journey lifecycle transitions that affect readiness

## Rules

1. Scores are hospital + framework scoped.  
2. Snapshot unavailability is surfaced as “calculating”, not as 0%.  
3. Enroll and journey transitions remain on `ComplianceJourneyService` / state machine — scoring does not own lifecycle.

## Exit criteria

Dashboard and maturity screens continue to read from the existing scoring/snapshot path without breaking enroll or journey contracts.
