# Plan: OE evidence + ComplianceTask invariants (PRAMAN)

**Status:** Planning only — no implementation in this document.  
**Scope:** Behavioral refactor of existing .NET backend (EF Core, Web API).  
**Saved:** `planning/plan-oe-evidence-compliance-task.md`

---

## Objective

Refactor the current evidence upload + compliance task flow to enforce:

1. Evidence **MUST** always be linked to a `ComplianceTask`.
2. `ComplianceTask` **MUST** always be assigned to a user (`AssignedToUserId` required, non-empty).
3. If a user uploads evidence and **no** `ComplianceTask` exists for `(HospitalId, ObjectiveElementId)`, the system **MUST** automatically create a task **assigned to the logged-in user**.
4. **Self-consistent at write time** — no background repair or sync flows as part of core correctness.

**Constraints:**

- `/compliance-tasks/sync` must **not** be part of the core flow.
- Missing tasks handled **inline** during upload / assign.
- **Do not** add new public upload endpoints — modify existing behavior.
- Reuse existing implementation where possible (behavioral adjustment, not rewrite).

---

## 1. Current flow summary

### ComplianceTask — created


| Location                                                         | Behavior                                                                                                                                                                                                             |
| ---------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ComplianceTaskGenerationService.GenerateTasksForJourneyAsync`   | Creates tasks for applicable `ObjectiveElement`s for the journey’s framework; idempotent per OE (skips if row exists). Sets `AssignedToUserId = string.Empty`; `RegulationId` from hospital+framework when possible. |
| `ComplianceJourneyService` (journey create)                      | Calls generation best-effort (exceptions swallowed).                                                                                                                                                                 |
| `JourneysController.SyncComplianceTasks`                         | Calls `GenerateTasksForJourneyAsync` for manual backfill.                                                                                                                                                            |
| `TasksController.Assign` (`POST .../programs/{programId}/tasks`) | Placeholder: empty task list; does not create real tasks today.                                                                                                                                                      |


### ComplianceTask — queried (especially `HospitalId` + `ObjectiveElementId`)


| Location                                                                     | Pattern                                                                                                                                                            |
| ---------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `JourneyWorkspaceAppService.RegisterObjectiveElementEvidenceAsync`           | `ComplianceTasks` where `HospitalId == journey.HospitalId` && `ObjectiveElementId`; latest by `CreatedAt`.                                                         |
| `JourneyWorkspaceAppService.RegisterObjectiveElementEvidenceFromUploadAsync` | Same.                                                                                                                                                              |
| `JourneyWorkspaceAppService.EnsureEvidenceBelongsToObjectiveElementAsync`    | Same (read-only).                                                                                                                                                  |
| `ComplianceTaskGenerationService`                                            | Existence check for batch of OE ids before insert.                                                                                                                 |
| Other                                                                        | `ComplianceContextAssembler`, `AuditSimulationService`, `HospitalComplianceRepository`, `TasksController.GetByProgramId`, `EvidenceController` (by `taskId`), etc. |


### ComplianceTask — updated


| Location                                                                      | Notes                                                              |
| ----------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `TasksController.CompleteTask` → `ComplianceCommandService.CompleteTaskAsync` | Completes by taskId + hospital + framework.                        |
| Wider codebase                                                                | Grep for additional update/assignment paths during implementation. |


### Evidence upload (journey / objective element)


| Layer                 | Role                                                                                                                                                  |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Entry**             | `JourneysController.PostOeEvidence` — JSON + multipart (same route).                                                                                  |
| **Service**           | `JourneyWorkspaceAppService`: `RegisterObjectiveElementEvidenceAsync`, `RegisterObjectiveElementEvidenceFromUploadAsync`; bulk calls upload per file. |
| **TaskId resolution** | Load `ComplianceTask` by `journey.HospitalId` + `objectiveElementId`; build `Evidence` with `task.Id`.                                                |
| **If task missing**   | Throws `InvalidOperationException` (message may reference `/compliance-tasks/sync`).                                                                  |


### Separate path: task-scoped evidence


| Entry      | `EvidenceController.Create` — `POST .../compliance/tasks/{taskId}/evidence` |
| ---------- | --------------------------------------------------------------------------- |
| **TaskId** | Client supplies `taskId`; no automatic task creation.                       |


### `/compliance-tasks/sync`


| Purpose     | Compensate missing tasks when generation was skipped, failed, or data inconsistent.                                                                                          |
| ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Callers** | `POST .../journeys/{id}/compliance-tasks/sync` on ComplianceService and Gateway (`SyncComplianceTasksCommand`). Error text in `JourneyWorkspaceAppService` may reference it. |


### Authentication context


| API                  | `IUserContextService`: `UserId` (`Guid`), `OrganizationId`, `HospitalId`, roles, email — JWT claims. |
| -------------------- | ---------------------------------------------------------------------------------------------------- |
| **Journey evidence** | Controller passes `_userContext.UserId` into workspace service methods.                              |


---

## 2. Gap analysis vs new business rules


| Rule                                                              | Gap                                                                                                |
| ----------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Evidence always linked to `ComplianceTask`                        | Satisfied once task exists; **fails** today if no row.                                             |
| `ComplianceTask` assigned to a user                               | **Violated**: generation uses `AssignedToUserId = string.Empty` (EF required allows empty string). |
| Auto-create task on upload if missing, assigned to logged-in user | **Missing**; only sync / journey-time generation.                                                  |
| Self-consistent at write time; no sync as core                    | **Violated**: sync + errors steer operators to repair.                                             |
| `/sync` not part of core                                          | **Violated** today as documented recovery path.                                                    |


**Additional gaps**

- **Concurrency:** No `UNIQUE(HospitalId, ObjectiveElementId)`; parallel first uploads may duplicate tasks.
- **Ownership:** Multiple tasks per OE + hospital → “latest by `CreatedAt`” is ambiguous.
- **Bulk / JSON registration:** Same task lookup; must share `EnsureTask` path.

---

## 3. Refactor plan (minimal)

### 3.1 `EnsureTask` (write-time primitive)

**Conceptual signature:**

`Task<ComplianceTask> EnsureComplianceTaskForObjectiveElementAsync(ComplianceJourney journey, Guid objectiveElementId, Guid assignedUserId, CancellationToken ct)`

- Include **journey** for `HospitalId`, `FrameworkId`, tenant alignment, regulation resolution.
- **Try** load by `HospitalId` + `ObjectiveElementId`.
- If missing: **insert** with `AssignedToUserId = assignedUserId.ToString()`, resolved `RegulationId`, defaults aligned with `ComplianceTaskGenerationService`.
- If exists with empty assignee: **policy** — backfill assignee vs fail (decide in implementation).
- **Concurrency:** unique index + transaction; on unique violation, re-read and return.

**Optional:** Validate OE belongs to journey’s framework before create.

### 3.2 Modify existing endpoints only

- `RegisterObjectiveElementEvidenceFromUploadAsync` / `RegisterObjectiveElementEvidenceAsync`: call `EnsureTask` before evidence create; **never** fail for “missing task” alone.
- Bulk upload: covered via shared register method.

### 3.3 Task assignment flow (when built)

- Operate on the **same** `ComplianceTask`; if missing, `EnsureTask` then update assignee/status.

### 3.4 All task creation paths

- `ComplianceTaskGenerationService`: every new task must have **non-empty** `AssignedToUserId`.
  - **Option A:** Pass `creatingUserId` from journey create and assign all new tasks to that user.
  - **Option B:** Stop bulk create at journey create; rely only on `EnsureTask` on first touch (larger product change).

### 3.5 `/compliance-tasks/sync`

- Remove from core docs and exception messages.
- Deprecate, admin-only, or internal-only; Gateway route gated or removed for normal clients.

---

## 4. Code change points (files / methods)


| Area             | File / symbol                                                                                                         |
| ---------------- | --------------------------------------------------------------------------------------------------------------------- |
| Ensure logic     | New `IComplianceTaskEnsuranceService` or methods on `JourneyWorkspaceAppService`                                      |
| Evidence         | `JourneyWorkspaceAppService.RegisterObjectiveElementEvidenceAsync`, `RegisterObjectiveElementEvidenceFromUploadAsync` |
| Bulk             | `BulkUploadObjectiveElementEvidenceAsync` (indirect)                                                                  |
| Generation       | `ComplianceTaskGenerationService.GenerateTasksForJourneyAsync`                                                        |
| Journey create   | `ComplianceJourneyService` — pass user id if option A                                                                 |
| Messages         | `JourneyWorkspaceAppService` — remove sync from errors                                                                |
| Sync deprecation | `JourneysController.SyncComplianceTasks`, Gateway `SyncComplianceTasksCommand`* + route                               |
| EF               | `ComplianceTask` / `ComplianceDbContext` — unique index; stricter assignee if needed                                  |
| Tests            | Idempotent ensure, concurrent create, upload without pre-existing task                                                |


---

## 5. Risks and mitigations


| Risk                           | Mitigation                                                                                     |
| ------------------------------ | ---------------------------------------------------------------------------------------------- |
| Duplicate tasks                | `UNIQUE(HospitalId, ObjectiveElementId)` + handle violation + retry read                       |
| Legacy duplicates              | Data cleanup script; runtime pick canonical row                                                |
| OE / framework mismatch        | Validate OE framework = journey framework                                                      |
| Regulation missing             | Fail fast with clear error; align task `RegulationId` with `ResolveRegulationForEvidenceAsync` |
| `AssignedToUserId` is `string` | `userId.ToString()` + whitespace validation on write                                           |


---

## 6. Migration plan

1. **Backfill** tasks with empty `AssignedToUserId` (system user / admin / evidence uploader — **product decision**).
2. **Resolve duplicate** `(HospitalId, ObjectiveElementId)` rows before unique index.
3. **Add** unique index `UX_ComplianceTasks_HospitalId_ObjectiveElementId`.
4. **Deploy** code with `EnsureTask` + deprecated sync.
5. **Verify:** integration tests for upload-without-task, parallel upload, regression on journey create.

---

## 7. Output checklist (for execution phase)

- Current flow summary validated against repo
- `EnsureTask` implemented with concurrency safety
- Journey OE evidence paths never fail solely for missing task
- Generation assigns non-empty user (option A or B locked)
- Sync removed from core UX and messages; endpoint deprecated or restricted
- Migration + backfill executed
- Tests added

---

*Generated from architecture discussion; implementation TBD.*