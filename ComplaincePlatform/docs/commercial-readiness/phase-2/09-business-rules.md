# Phase 2 — Business Rules (Sprint 6)

**Sprint:** 6  
**Status:** Delivered (enforced in domain / application services)

| # | Rule | Enforcement |
|---|------|-------------|
| 1 | Closed / Cancelled CAPA (deficiency) is immutable on field patches | `ComplianceDeficiency.EnsureMutable()` called from `UpdateCorrectiveAction`, `UpdatePreventiveAction`, `UpdateRca`, `UpdateRemediationPlan`, `UpdateRiskAssessment`, `UpdateEffectivenessReview`, `SetRootCause`, `Assign`, ownership accept/reject, `AssignCapaNumber`. Status transitions already reject terminal states. |
| 2 | Submitted / Closed audit session rejects new findings & observations | `AuditSession.EnsureAcceptsFindings()` in `FindingQueryService.CreateAsync` (when `AuditSessionId` set) and `AuditorWorkspaceQueryService.CreateObservationAsync`. |
| 3 | Only a reviewer can verify CAPA (status → Verified) | `PatchDeficiencyStatusCommandHandler.EnsureCanVerify`: when JWT roles are present, requires `MANAGER` / `ORG_ADMIN` / `GROUP_ADMIN` / `SUPER_ADMIN` / `REGULATORY_ADMIN` / `AUTHORITY`. Soft block: assignee cannot self-verify (`AssignedTo` ≠ verifier user id / updatedBy / email). System evidence-lifecycle auto-verify bypasses this handler. |
| 4 | Only assignee completes task | `TaskExecutionService.EnsureAssigneeCanComplete` — when `AssignedToUserId` is set, completing user must match `IUserContextService.UserId`. Unassigned tasks remain completable by hospital-scoped callers. |

## Notes / soft checks

- **Verify without roles:** If the request context has an empty role list (internal/system callers), role gate is skipped; self-verify block still applies when assignee identity matches.  
- **Audit “Completed”:** Product status vocabulary uses `Submitted` then `Closed` (no separate `Completed` enum). Both terminal states reject new findings.  
- Controllers stay thin; rules live in domain/application services per architecture constitution.

## Exit criteria

- Closed CAPA patches throw; unit tests cover corrective + RCA  
- Closed/Submitted sessions reject findings; unit tests cover gate  
- Verify + task-complete rules documented and enforced on primary paths  
