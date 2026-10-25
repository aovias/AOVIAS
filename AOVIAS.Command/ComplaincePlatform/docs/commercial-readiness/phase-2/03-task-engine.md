# Phase 2 — Task Management Engine (Sprint 3)

**Sprint:** 3  
**Status:** Delivered  
**Principle:** Extend `ComplianceTask` as SSOT — do **not** introduce a separate task microservice.

## Model (SSOT)

| Field / entity | Purpose |
|----------------|---------|
| `TaskCategory` | Compliance, CAPA, Audit, Training, DocumentReview, General |
| `TaskKind` | Standard, Approval, Review |
| `BlockedByTaskId` | Optional predecessor; Complete blocked until blocker is Done |
| `RecurringDefinitionId` | Link when materialized from a recurrence template |
| `OverdueEscalatedAt` | Dedupes `TaskOverdue` escalation emissions |
| `ComplianceTaskOwnershipHistory` | Reassignment / ownership audit trail |
| `RecurringTaskDefinition` | Hangfire-materialized recurrence template |

## APIs

### List / summary filters (`GET /api/v1/tasks`, `GET /api/v1/tasks/summary`)

Existing filters plus:

| Query | Purpose |
|-------|---------|
| `category` | Filter by `TaskCategory` |
| `taskKind` | Filter by `TaskKind` |
| `teamOnly` | Exclude current user's tasks (team queue) |
| `dueFrom` / `dueTo` | Inclusive due-date window (calendar) |
| `search` | Title / description / OE / standard code |
| `overdueOnly` | Overdue open tasks |
| `ownerId` | My tasks when set to current user |

Views: `table` \| `kanban` \| `risk` \| `calendar` \| `department` (calendar/department use paged list + client grouping).

### Reassign

| Method | Route |
|--------|--------|
| POST | `/api/v1/tasks/{taskId}/reassign?frameworkId=` |

Body: `{ assignedToUserId, reason? }`  
Requires `If-Match: <rowVersion>`.  
Writes `ComplianceTaskOwnershipHistory`, emits `TaskReassigned`.  
AccessGovernance HTTP evaluate is not wired in ComplianceService (broker-agnostic); reassignment always records history.

### Complete dependency gate

`PUT /api/v1/tasks/{taskId}/complete` fails with validation when `BlockedByTaskId` points to a non-completed task (`TASK_DEPENDENCY_BLOCKED`).

## Background jobs (Hangfire)

| Job ID | Schedule | Behavior |
|--------|----------|----------|
| `recurring-task-materialization` | Hourly UTC | Materializes due `RecurringTaskDefinition` → `ComplianceTask` |
| `task-overdue-escalation` | Daily 05:00 UTC | Emits `TaskOverdue` once per overdue episode |

## Events

| Event | Value |
|-------|-------|
| `TaskOverdue` | 42 |

Emitted via `IComplianceEventEmitter` / outbox (`ComplianceEvent.TaskOverdue`).

## Frontend (`WebApp` features/tasks)

- Views: Table, Kanban, Risk, **Calendar**, **Department**
- Quick filters: **My tasks**, **Team**, **Overdue** (+ existing pills)
- Category filter + search wired to list APIs
- Kanban/List retained

## Tests

- `TaskDependencyGateTests` — dependency complete block
- `RecurringTaskMaterializerTests` — recurrence materializer smoke

## Build verify

```text
dotnet build ComplianceService -o _build_verify_phase0\P2S3
dotnet test ComplianceService.Tests --filter "FullyQualifiedName~TaskDependencyGate|FullyQualifiedName~RecurringTaskMaterializer"
```
