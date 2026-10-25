# Phase 60 — Wave 2 summary: Departments, assignee, POST create task

## Completed

- **`ComplianceTaskApiService.createTask`** → `POST /compliance/compliance-tasks` with **`CreateComplianceTaskRequest`** / **`ComplianceTaskDetailDto`** (`compliance-task-api.model.ts`).
- **Department** field: **`mat-select`** bound to **`CommonLookupService.loadDepartments()`** (`/common/master-departments`); form control **`departmentId`**; **`departmentName`** derived from selected row on submit.
- **Assignee:** **`OrganizationMembersApiService`** in drawer; **`AssigneeChipsFieldComponent`** refactored to **single-select** (one `userId`); value stored as **`assignedToUserId`**.
- **`CreateTaskDraftVm`:** **`assigneeIds[]` → `assignedToUserId`**; **`mergeTaskDraftFromSession`** + store merge migrate legacy **`assigneeIds`** from sessionStorage.
- **Create task:** Maps **priority → `TaskCriticalityLevel`**; sends **`objectiveElementId`**, **`complianceStandardId`**, **`assignedToUserId`**, **`departmentId`**, optional **`dueDate`**, **`title`**, **`description`**, **`departmentName`**; **409** shows duplicate-OE message; other errors use envelope/message when present.
- **UX:** Warning when linkage IDs missing; **Create** disabled without standard/OE context or while submitting; loading/error for departments list.
- **Tests:** Drawer spec updated (Session mock + HTTP flush for members + departments).

## Files

- `src/app/features/journey/models/compliance-task-api.model.ts`
- `src/app/features/journey/services/compliance-task-api.service.ts`
- `src/app/features/journey/models/create-task-draft-vm.model.ts`
- `src/app/features/journey/state/journey-workspace.store.ts`
- `src/app/features/journey/components/create-assign-task-drawer/*`
- `src/app/features/journey/components/create-assign-task-drawer/create-assign-task-drawer.component.spec.ts`

## Out of scope / follow-up

- Hospital-specific department endpoint (swap loader in drawer when API is confirmed).
- Tags, effort, status, task type remain **draft-only** (not sent on create per spec).
