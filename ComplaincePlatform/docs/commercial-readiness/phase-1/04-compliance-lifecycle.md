# Phase 1 — Compliance Lifecycle

**Sprint:** 2 (guardrails)  
**Status:** Delivered (contracts preserved)

## Ownership

- **Only** `ComplianceJourney` + `ComplianceLifecycleState` own hospital-framework lifecycle.  
- Transitions go through `ComplianceJourneyService.TransitionJourneyAsync` / state machine — unchanged in Phase 1.

## Phase 1 interaction with catalog

| Concern | Behavior |
|---------|----------|
| Enroll | Rejected unless framework `IsActive` and catalog `Status == Active` |
| Journey transitions | Unchanged |
| Framework Retire/Archive | Does not auto-transition journeys; blocks new enrollments |

## Expiry (existing + related)

| Job | Purpose |
|-----|---------|
| `ComplianceExpiryJob` | Approved journeys past ExpiryDate → Expired via journey service |
| `EvidenceExpiryJob` (Sprint 4) | Evidence with `ExpiresAt <= UtcNow` → LifecycleStatus Rejected + notification log hook |

## Exit criteria

Hospital enroll and journey transition APIs remain the same shapes; catalog Status is an additional enroll gate only.
