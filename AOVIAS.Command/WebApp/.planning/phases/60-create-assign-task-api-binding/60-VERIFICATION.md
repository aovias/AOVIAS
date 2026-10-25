# Phase 60 — Verification

## must_haves (plan)

| Requirement | Status |
|-------------|--------|
| Organization members GET client + tests | Done — `OrganizationMembersApiService` + spec |
| Department `mat-select` + `departmentId` / name on POST | Done — `CommonLookupService` + form |
| Assignees from new API; single assignee | Done — chips field single-select; `assignedToUserId` |
| `POST /compliance/compliance-tasks` + request mapping | Done — `ComplianceTaskApiService` |
| Priority → criticality; 409 handling | Done — drawer `onCreateTask` |
| `ng build` | Passed |
| Unit tests for new/edited specs | New service tests pass; drawer spec passes; `app.spec.ts` pre-existing failure |

## human_needed

- E2E against real Gateway: organization-members route shape, compliance create, **409** duplicate task.
- Confirm **master departments** GUIDs match Compliance **`departmentId`** for your hospital; replace loader if a hospital-scoped API is introduced.

## Goal verdict

**passed** (implementation + build + targeted tests; runtime API parity not exercised in CI).
