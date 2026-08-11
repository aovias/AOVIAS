# Phase 60 — Research: Create & Assign Task API binding

## RESEARCH COMPLETE

### Sources

- **Compliance task contract:** `ComplaincePlatform/COMPLIANCE_TASK_API_SPEC.md` (v1) — Gateway base `/api/v{version}/compliance`, envelope `ApiResponse<T>`, header `api-version: 1.0` (aligned with existing WebApp `environment.apiBaseUrl` + interceptors).
- **User-provided org members route:** Controller `[Route("api/v{version:apiVersion}/organization-members")]`, action `[HttpGet("organization/{organizationId:guid}")]` → effective client path: **`GET {apiBaseUrl}/organization-members/organization/{organizationId}`** (e.g. `https://localhost:7216/api/v1/organization-members/organization/{guid}`).
- **Current WebApp patterns:** `OrganizationService.loadOrganizationDetails()` uses **`GET /tenant/{organizationId}/organization-detail`** and embeds **`members: OrganizationMember[]`** — **not** the dedicated `organization-members` endpoint. Phase 60 should add an explicit client for the **organization-members** API so field names match that contract (verify DTO vs `OrganizationMember` in `organization.service.ts`).
- **Departments today:** `CommonLookupService.loadDepartments()` → **`GET /common/master-departments`** (cached), normalized to `DepartmentLookup { id, name, … }`. Onboarding Phase 20/26 used **master** departments for **hospital profile** multi-select; team invitation binds **`departmentId`** to allowed profile IDs.

### Create task request (authoritative mapping)

From **`POST /compliance/compliance-tasks`**, body **`CreateComplianceTaskRequest`**:

| API field | Required | Notes |
|-----------|----------|--------|
| `objectiveElementId` | yes | UUID — from journey/OE context (`JourneyWorkspaceStore`, `findOe`) |
| `complianceStandardId` | yes | UUID — must match OE’s standard |
| `assignedToUserId` | yes | Non-empty string (treat as user id GUID in practice) |
| `departmentId` | yes | Non-empty GUID — **organization department** (spec table) |
| `departmentName` | no | Recommended for list/detail labels — set from selected department row |
| `dueDate` | no | ISO date-time UTC |
| `title` | no | Defaults server-side from OE if omitted |
| `description` | no | |
| `criticality` | no | Default `Medium` — enum **`TaskCriticalityLevel`**: `Critical`, `High`, `Medium`, `Low` |
| `programId` | no | Optional |

**Not in create body:** tags, effort estimate, task “type”, multiple assignees, attachments. Those remain **draft-only** or require **future** APIs / PATCH workflows.

### Phase 59 UI vs API — gaps

| Drawer / draft (Phase 59) | Compliance create API |
|----------------------------|------------------------|
| `assigneeIds[]` (multi) | **Single** `assignedToUserId` — **must change UX or** send primary only **with product sign-off** |
| `priority` (low/medium/high/critical) | `criticality` — map: critical→Critical, high→High, medium→Medium, low→Low |
| `status` / `taskType` | Not on create DTO — **omit** on POST; follow-up PATCH if product needs |
| `department` free text + `departmentId` in VM | **Required** `departmentId` + optional `departmentName` from **dropdown** |
| `standardId` / `objectiveElementId` in draft | Map to `complianceStandardId` / `objectiveElementId` (UUID strings) |

### List/detail DTOs (read path, future)

`ComplianceTaskListItemDto` includes `departmentName`, `assignedToDisplayName` (may be null for other users) — UI may still resolve names from org members cache.

### Open question: **which departments endpoint?**

**Requirement:** Department control = **dropdown** bound to **hospital departments** for the active hospital context.

**Candidates in repo:**

1. **`CommonLookupService.loadDepartments()`** — **master** catalog; IDs match onboarding if backend uses same GUIDs for org departments.
2. **Hospital-scoped** API (unknown) — e.g. departments for `AppContextService` / session **hospitalId** only.

**Action:** Confirm with **API owner** one of:

- Reuse **`/common/master-departments`** (or current documented **`/common/departments`**) for compliance task `departmentId`, **or**
- Use a **hospital-specific** route (please provide method + path + response shape).

Until confirmed, **Plan 60-01** implements **org members** binding first; **Plan 60-02** wires department dropdown behind a **single service** with a **pluggable loader** (master-departments default) and a **TODO** swap once the hospital endpoint is confirmed.

### Security / context

- JWT **ComplianceScope**; **hospital context** from token — client must continue sending `Authorization` and consistent **`api-version`** (existing interceptors).
- **Duplicate task:** `409` when a task already exists for `(hospitalId, objectiveElementId)` — surface message from envelope.

---

## RESEARCH COMPLETE
