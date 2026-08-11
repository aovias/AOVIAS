# ComplianceTask API specification (v1)

This document describes **ComplianceService** and **Gateway** HTTP APIs for execution tasks. All responses use the shared `ApiResponse<T>` envelope (`Success`, `Code`, `Message`, `Data`, `Errors`, `Meta.RequestId`).

**Authentication:** Bearer JWT with compliance scope (`ComplianceScope`). **Hospital context** is enforced from the token (and `ValidateHospitalId` where applied on ComplianceService).

**Base paths**


| Layer             | Base URL                     |
| ----------------- | ---------------------------- |
| Gateway           | `/api/v{version}/compliance` |
| ComplianceService | `/api/v{version}/compliance` |


Use header `api-version: 1.0` (or route placeholder `v1`).

---

## Enums

### Task status (`ComplianceTaskStatus`)

Stored and serialized as **string** values: `Pending`, `InProgress`, `OnHold`, `Blocked`, `Completed`.


| Value        | Meaning                                                                       |
| ------------ | ----------------------------------------------------------------------------- |
| `Pending`    | Not started                                                                   |
| `InProgress` | Active work                                                                   |
| `OnHold`     | Paused                                                                        |
| `Blocked`    | Blocked                                                                       |
| `Completed`  | Done — **only** via **Complete task** endpoint (aggregate), **not** via PATCH |


### Task criticality (`TaskCriticalityLevel`)

`Critical`, `High`, `Medium`, `Low` (JSON string names). Used for **scoring** semantics.

### Execution priority (`ComplianceTaskPriority`)

`Urgent`, `High`, `Medium`, `Low` (JSON string names). Stored as int (`Urgent` = 1 … `Low` = 4). This is the **execution** priority, distinct from **`criticality`**.

---

## DTOs (response)

### `ComplianceTaskListItemDto` / `ComplianceTaskDetailDto`


| Field                   | Type                   | Notes                                                                                       |
| ----------------------- | ---------------------- | ------------------------------------------------------------------------------------------- |
| `id`                    | uuid                   |                                                                                             |
| `hospitalId`            | uuid                   |                                                                                             |
| `programId`             | uuid?                  | Optional tag only                                                                           |
| `complianceStandardId`  | uuid                   |                                                                                             |
| `objectiveElementId`    | uuid                   | Unique per hospital                                                                         |
| `departmentId`          | uuid?                  | Organization department id                                                                  |
| `departmentName`        | string?                | Denormalized display                                                                        |
| `assignedToUserId`      | string                 |                                                                                             |
| `assignedToDisplayName` | string?                | Set when assignee is the **current user** (from JWT claims); otherwise null — UI may enrich |
| `dueDate`               | date-time?             | UTC                                                                                         |
| `status`                | `ComplianceTaskStatus` |                                                                                             |
| `title`                 | string?                |                                                                                             |
| `description`           | string?                |                                                                                             |
| `criticality`           | `TaskCriticalityLevel` | Scoring-related                                                                             |
| `priority`              | `ComplianceTaskPriority` | Execution queue / urgency (distinct from `criticality`)                                 |
| `weight`                | decimal                |                                                                                             |
| `objectiveElementCode`  | string                 |                                                                                             |
| `objectiveElementName`  | string                 |                                                                                             |
| `standardCode`          | string                 |                                                                                             |
| `createdAt`             | date-time              |                                                                                             |
| `maturityState`         | string?                | **Detail only**                                                                             |


---

## ComplianceService endpoints

### 1. List tasks

`GET /compliance/compliance-tasks?frameworkId={optional}`

- **frameworkId:** optional; filters tasks whose standard’s chapter belongs to this framework.
- **200** `ApiResponse<IReadOnlyList<ComplianceTaskListItemDto>>`

### 2. Get task by id

`GET /compliance/compliance-tasks/{taskId}`

- **200** `ApiResponse<ComplianceTaskDetailDto>`
- **404** if not found / wrong hospital

### 3. Create task

`POST /compliance/compliance-tasks`

**Body (`CreateComplianceTaskRequest`):**


| Field                  | Required | Notes                              |
| ---------------------- | -------- | ---------------------------------- |
| `objectiveElementId`   | yes      |                                    |
| `complianceStandardId` | yes      | Must match the OE’s standard       |
| `assignedToUserId`     | yes      | Non-empty string                   |
| `departmentId`         | yes      | Non-empty guid (execution model)   |
| `departmentName`       | no       | Recommended for list/detail labels |
| `dueDate`              | no       |                                    |
| `title`                | no       | Defaults to OE title if omitted    |
| `description`          | no       |                                    |
| `criticality`          | no       | Default `Medium`                   |
| `priority`             | no       | Default `Medium` (`ComplianceTaskPriority`) |
| `programId`            | no       | Optional                           |


- **200** `ApiResponse<ComplianceTaskDetailDto>`
- **409** / conflict code when duplicate `(hospitalId, objectiveElementId)`
- **400** validation / business rule failures

### 4. Patch task

`PATCH /compliance/compliance-tasks/{taskId}`

**Body (`PatchComplianceTaskRequest`):** all fields optional; only provided fields are updated.


| Field              | Notes                                               |
| ------------------ | --------------------------------------------------- |
| `assignedToUserId` | Non-empty if provided                               |
| `departmentId`     | Non-empty guid if provided                          |
| `departmentName`   |                                                     |
| `dueDate`          |                                                     |
| `title`            |                                                     |
| `description`      |                                                     |
| `criticality`      |                                                     |
| `priority`         |                                                     |
| `status`           | **Must not** be `Completed` — use complete endpoint |


- **400** if `status` is `Completed` or task is already completed
- **404** not found

### 5. Complete task (domain / aggregate)

`PUT /compliance/frameworks/{frameworkId}/tasks/{taskId}/complete`

- No body. Uses `ComplianceCommandService.CompleteTaskAsync` (scoring, events, outbox).
- **200** with `{ taskId, frameworkId, hospitalId }`

### 6. List tasks by program (legacy)

`GET /compliance/programs/{programId}/tasks`

- Returns the same list DTOs as (1), filtered by `program.ComplianceFrameworkId`.

### 7. Assign tasks by program (deprecated)

`POST /compliance/programs/{programId}/tasks`

- **410 Gone** — use **Create task** instead. Message explains replacement URL.

---

## Gateway endpoints (proxy)

Identical paths under Gateway base URL; **no business logic** — forwards to ComplianceService.


| Method | Path                                                           |
| ------ | -------------------------------------------------------------- |
| GET    | `/compliance/compliance-tasks`                                 |
| GET    | `/compliance/compliance-tasks/{taskId}`                        |
| POST   | `/compliance/compliance-tasks`                                 |
| PATCH  | `/compliance/compliance-tasks/{taskId}`                        |
| PUT    | `/compliance/frameworks/{frameworkId}/tasks/{taskId}/complete` |


---

## Sample JSON

**Create**

```json
POST /api/v1/compliance/compliance-tasks
{
  "objectiveElementId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "complianceStandardId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "assignedToUserId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "departmentId": "11111111-2222-3333-4444-555555555555",
  "departmentName": "Quality",
  "dueDate": "2026-05-01T00:00:00Z",
  "title": "Prepare evidence pack",
  "description": "Collect SOPs for OE",
  "criticality": "High"
}
```

**Patch**

```json
PATCH /api/v1/compliance/compliance-tasks/{taskId}
{
  "status": "InProgress",
  "assignedToUserId": "new-user-guid-string"
}
```

**Success envelope**

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "...",
  "data": { },
  "errors": [],
  "meta": { "requestId": "...", "timestamp": "..." }
}
```

**Error**

```json
{
  "success": false,
  "code": "RESOURCE_CONFLICT",
  "message": "...",
  "data": null,
  "errors": [ { "message": "A compliance task already exists for this hospital and objective element." } ],
  "meta": { "requestId": "...", "timestamp": "..." }
}
```

---

## Angular notes

- Call **Gateway** base URL for all traffic; keep `Authorization` and `api-version` consistent with other modules.
- Bind **status** and **criticality** to dropdowns using the enum **strings** returned by the API.
- **assignedToDisplayName** may be null; resolve names from your org/user directory when the list shows other users.
- **Complete** action must call **PUT** `.../frameworks/{frameworkId}/tasks/{taskId}/complete` with the correct framework id from context.
- Do **not** use journey sync or program assign for normal UI task creation.

---

## Domain rules (reference)

- At most **one** task per `(hospitalId, objectiveElementId)`.
- **Completion** only through the aggregate complete endpoint; **PATCH** must not set `Completed`.

