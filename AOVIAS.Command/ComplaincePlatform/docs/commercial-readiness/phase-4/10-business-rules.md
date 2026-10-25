# Phase 4 — Governance Business Rules (Sprint 6)

**Sprint:** 6  
**Status:** Delivered (enforced in Governance domain / application services)

| # | Rule | Enforcement |
|---|------|-------------|
| 1 | Cannot change attendance after minutes approved | `CommitteeMeeting.SetAttendance` |
| 2 | Cannot re-complete / mutate minutes after approval | `CommitteeMeeting.Complete` |
| 3 | Minutes approval requires Completed + non-empty minutes; no double-approve | `CommitteeMeeting.ApproveMinutes` |
| 4 | No new decisions / action items after minutes approved | `RecordDecision` / `AddActionItem` |
| 5 | Action item title required; closed items immutable | `CommitteeActionItem.Create` / `UpdateStatus` / `LinkTask` |
| 6 | Typed decision links prefer DeficiencyId / RiskId / TaskId (legacy governed resource kept) | `CommitteeDecision.Create` |
| 7 | Controlled documents past expiry / archived are immutable | Existing QDMS `EnsureMutable` (Sprint 1) |
| 8 | Closed risks are immutable | Existing `Risk.EnsureMutable` (Risk sprint) |

## Notes

Controllers stay thin; rules live on aggregates / app services. Gateway does not bypass minutes locks.
