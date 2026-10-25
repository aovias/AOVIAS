# Journey Module — API Specification (Frontend Integration)

This document describes **all Journey execution and navigation APIs** exposed to the **Web App** through the **Gateway**. Contracts match the **ComplianceService** implementation and the shared `**ApiResponse<T>`** envelope.

**Document version:** 1.0  
**API surface:** Gateway `JourneysController` → ComplianceService `JourneysController`  
**Last aligned with backend:** Compliance Platform repository (Journey screen API v2 alignment)

---

## 1. Overview

### 1.1 Purpose

These APIs power the **Compliance Journey** experience: session context, dashboard metrics, chapter navigation, standards and objective elements (OEs), scoring updates, evidence registration, and **operational mode** progression (NABH-style phases).

### 1.2 Scope


| Area                 | Coverage                                                                   |
| -------------------- | -------------------------------------------------------------------------- |
| Journey execution UI | Session, dashboard, chapters index, chapter standards, OE detail, PATCH OE |
| Evidence             | JSON register, multipart file upload, list, delete                         |
| Operational mode     | Read on session; update via PATCH                                          |
| Legacy aggregate     | GET workspace (full tree snapshot)                                         |


### 1.3 Key concepts

- **Journey (`journeyId`)** — A hospital’s compliance run for a given framework (`ComplianceJourney`). Tenant isolation uses the authenticated organization.
- **OperationalMode** — Execution phase (0–4), **persisted** on the journey. Drives which OE classifications appear in workspace views and read-only rules.
- **Hierarchy** — **Chapter → Standard → Objective Element (OE)**. Standards belong to a chapter; OEs belong to a standard.
- **Read-only** — When the journey is locked after submission (or policy says snapshot read-only), mutating calls (PATCH OE, evidence POST/DELETE, PATCH operational mode) are rejected.

### 1.4 Base URL and versioning


| Item                    | Value                                               |
| ----------------------- | --------------------------------------------------- |
| **Gateway base**        | `{GATEWAY_ORIGIN}` (e.g. `https://api.example.com`) |
| **Path prefix**         | `/api/v{version}/journeys`                          |
| **Recommended version** | `1.0` → `/api/v1.0/journeys/...`                    |


All paths below are **relative to** `{GATEWAY_ORIGIN}` unless noted.

### 1.5 Response envelope (all endpoints)

Every JSON response uses `**ApiResponse<T>`**:


| Field     | Type         | Description                                        |
| --------- | ------------ | -------------------------------------------------- |
| `success` | boolean      | `true` for 2xx success                             |
| `code`    | string       | Machine code (e.g. `SUCCESS`, `VALIDATION_FAILED`) |
| `message` | string       | Human-readable summary from code registry          |
| `data`    | T            | null                                               |
| `errors`  | `ApiError[]` | Structured errors; often empty on success          |
| `meta`    | object       | `requestId`, `timestamp` (UTC)                     |


`**ApiError`** (typical):


| Field     | Type    | Description         |
| --------- | ------- | ------------------- |
| `field`   | string? | Optional field name |
| `message` | string  | Detail message      |


> **Note:** This is **not** RFC 7807 Problem Details. Clients should parse `success`, `code`, `http status`, and `errors[]`. Gateway may return downstream ComplianceService JSON **as-is** for proxied routes (same envelope).

---

## 2. Authentication and authorization

### 2.1 Mechanism

- **Scheme:** HTTP **Bearer** JWT (`Authorization: Bearer <access_token>`).
- **Policy (Gateway):** `ComplianceScope` — token must include scope `**compliance.api`** (see `AuthConstants.Policies.ComplianceScope` / `AuthConstants.Scopes.ComplianceApi`).

### 2.2 Tenant and scoping

- **Organization / tenant** is resolved from JWT claims (e.g. `organizationId`). The backend enforces that the journey belongs to the caller’s organization.
- **Hospital** is implicit on the journey record (`journeyId`); there is **no** required `X-Hospital-Id` header for these routes in the current implementation.

### 2.3 Recommended headers


| Header             | Required   | Description                                                    |
| ------------------ | ---------- | -------------------------------------------------------------- |
| `Authorization`    | Yes        | `Bearer <JWT>`                                                 |
| `Content-Type`     | For bodies | `application/json` or `multipart/form-data` as applicable      |
| `X-Correlation-Id` | No         | Correlation id for distributed tracing (forwarded to services) |


---

## 3. OperationalMode enum


| Value | Name              | Typical use              |
| ----- | ----------------- | ------------------------ |
| `0`   | `SelfAssessment`  | Default for new journeys |
| `1`   | `InternalAudit`   | Internal audit phase     |
| `2`   | `FinalAudit`      | Final audit phase        |
| `3`   | `Surveillance`    | Surveillance cycle       |
| `4`   | `ReAccreditation` | Re-accreditation         |


### 3.1 JSON representation

- **Session / dashboard / chapters / standards / OE detail:** `operationalMode` is serialized as an **integer 0–4** on spec-facing DTOs (e.g. `JourneySessionDto`).
- **GET …/workspace (legacy):** `operationalMode` may still appear as a **string enum** in `JourneyWorkspaceSnapshotDto` for backward compatibility.

### 3.2 Persistence and progression

- Stored on `**ComplianceJourney`**. Updates only via `**PATCH .../session/operational-mode`**.
- **Linear progression only:** `0→1→2→3→4`, one step at a time (no skip, no backward via API).
- **Conflict (409):** Invalid transition (e.g. jump two steps).
- **Forbidden (403):** Policy denies change (e.g. locked after submission).

### 3.3 Mode-based UI behavior

- **Workspace classification filtering:** Which OEs are in scope for scoring/navigation depends on mode + internal classification policy.
- **Read-only:** When `readOnly` / `snapshotReadOnly` on session (or journey locked), mutations must be disabled in UI.

---

## 4. Query parameters: operational mode resolution

For endpoints that accept mode, the server resolves **effective** `OperationalMode` as follows:


| Query                | Type    | Description                                                 |
| -------------------- | ------- | ----------------------------------------------------------- |
| `operationalMode`    | int?    | Optional. If present, must match journey context for reads. |
| `auditLifecycleMode` | string? | Optional alternate encoding (see below).                    |


`**auditLifecycleMode` accepted tokens (case-insensitive):**

- `self_assessment`, `selfassessment` → 0  
- `internal_audit`, `internalaudit` → 1  
- `final_audit`, `finalaudit` → 2  
- `surveillance` → 3  
- `re_accreditation`, `reaccreditation` → 4

**Rules:**

1. If **both** `operationalMode` and `auditLifecycleMode` are sent, they must **agree**; otherwise **400** (`VALIDATION_FAILED`).
2. If **neither** is sent, the **stored** journey `OperationalMode` is used.
3. Invalid `auditLifecycleMode` string → **400**.

---

## 5. API endpoints (core)

Unless stated, `**Content-Type: application/json`** applies to requests with bodies.

---

### 5.A Session

#### `GET /api/v1.0/journeys/{journeyId}/session`

**Description:** Returns journey execution **metadata**: framework labels, persisted operational mode, read-only flags, catalog hints, allowed next mode transitions, and server sync time. Use for **shell / header / mode picker**.

**UI:** Journey layout, mode selector, global read-only banner.

##### Path parameters


| Name        | Type | Required | Description        |
| ----------- | ---- | -------- | ------------------ |
| `journeyId` | UUID | Yes      | Journey identifier |


##### Query parameters


| Name                 | Type   | Required | Description |
| -------------------- | ------ | -------- | ----------- |
| `operationalMode`    | int    | No       | See §4      |
| `auditLifecycleMode` | string | No       | See §4      |


##### Success `data`: `JourneySessionDto`


| Field              | Type                    | Description                                     |
| ------------------ | ----------------------- | ----------------------------------------------- |
| `journeyId`        | UUID                    | Journey id                                      |
| `frameworkCode`    | string                  | Framework code                                  |
| `frameworkName`    | string                  | Framework display name                          |
| `operationalMode`  | int                     | 0–4, persisted effective mode for display       |
| `readOnly`         | boolean                 | Workspace mutations blocked                     |
| `snapshotReadOnly` | boolean                 | Snapshot-style read-only (e.g. approved locked) |
| `lastSyncedAt`     | string (ISO 8601)       | Server UTC timestamp                            |
| `catalog`          | `JourneyCatalogMetaDto` | Edition, counts, partial flag                   |
| `modeProgression`  | object?                 | Allowed next modes + lock reasons               |


`**JourneyCatalogMetaDto`**


| Field                    | Type    | Description                    |
| ------------------------ | ------- | ------------------------------ |
| `standardEdition`        | string? | Catalog edition label          |
| `effectiveFrom`          | string? | Effective date string if known |
| `totalStandardsExpected` | int?    | Expected standard count        |
| `totalOesExpected`       | int?    | Expected OE count              |
| `isPartialCatalog`       | boolean | Master data incomplete         |


`**JourneyModeProgressionDto`**


| Field                    | Type   | Description                                      |
| ------------------------ | ------ | ------------------------------------------------ |
| `allowedNextModes`       | int[]  | Next mode values allowed (typically at most one) |
| `lockReasonByTargetMode` | object | Map string keys `"0"`…`"4"` to reason or null    |


##### Example (200)

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": {
    "journeyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "frameworkCode": "NABH",
    "frameworkName": "NABH 6th Edition",
    "operationalMode": 1,
    "readOnly": false,
    "snapshotReadOnly": false,
    "lastSyncedAt": "2026-04-11T12:34:56.789Z",
    "catalog": {
      "standardEdition": "6.0",
      "effectiveFrom": "2024-01-01",
      "totalStandardsExpected": 120,
      "totalOesExpected": 640,
      "isPartialCatalog": false
    },
    "modeProgression": {
      "allowedNextModes": [2],
      "lockReasonByTargetMode": {
        "0": null,
        "1": null,
        "2": null,
        "3": "Advance one step at a time.",
        "4": "Advance one step at a time."
      }
    }
  },
  "errors": [],
  "meta": {
    "requestId": "0HMVTEST123456",
    "timestamp": "2026-04-11T12:34:56.790Z"
  }
}
```

##### Errors


| HTTP | Scenario                                      | `code` (typical)     |
| ---- | --------------------------------------------- | -------------------- |
| 400  | Bad query pair / invalid `auditLifecycleMode` | `VALIDATION_FAILED`  |
| 401  | Missing or invalid token                      | `UNAUTHORIZED`       |
| 403  | Tenant / journey access denied                | `FORBIDDEN`          |
| 404  | Journey not found                             | `RESOURCE_NOT_FOUND` |
| 500  | Unexpected failure                            | `INTERNAL_ERROR`     |


##### Example error (400)

```json
{
  "success": false,
  "code": "VALIDATION_FAILED",
  "message": "Input validation failed",
  "data": null,
  "errors": [
    { "field": null, "message": "operationalMode and auditLifecycleMode disagree." }
  ],
  "meta": {
    "requestId": "0HMVTEST123457",
    "timestamp": "2026-04-11T12:35:00.000Z"
  }
}
```

---

### 5.B Dashboard

#### `GET /api/v1.0/journeys/{journeyId}/dashboard`

**Description:** Aggregated **overview**, **accreditation** metrics, optional **accreditationReadinessPercent**, and three **trend** series. Use for **home / dashboard** widgets.

**UI:** Journey dashboard, charts.

##### Path parameters

Same as session (`journeyId`).

##### Query parameters

Same as session (`operationalMode`, `auditLifecycleMode`).

##### Success `data`: `JourneyDashboardDto`


| Field                           | Type                               | Description                                          |
| ------------------------------- | ---------------------------------- | ---------------------------------------------------- |
| `overview`                      | `JourneyOverviewMetricsDto`        | Core %, failed core count, OEs ≤3, open action plans |
| `accreditation`                 | `JourneyDashboardAccreditationDto` | Percentages, averages, violations list               |
| `accreditationReadinessPercent` | number?                            | Nullable readiness                                   |
| `oeScoreTrend`                  | `TrendPointDto[]`                  | Time series                                          |
| `chapterImprovementTrend`       | `TrendPointDto[]`                  | Time series                                          |
| `riskEvolutionTrend`            | `TrendPointDto[]`                  | Time series                                          |


`**JourneyOverviewMetricsDto`**


| Field                    | Type   |
| ------------------------ | ------ |
| `coreCompliancePercent`  | number |
| `failedCoreCount`        | int    |
| `oesAtOrBelowThreeCount` | int    |
| `openActionPlansCount`   | int    |


`**JourneyDashboardAccreditationDto`**


| Field                      | Type     |
| -------------------------- | -------- |
| `overallCompliancePercent` | number   |
| `coreOeAverageScore`       | number   |
| `averageStandardScore`     | number   |
| `averageChapterScore`      | number   |
| `violations`               | string[] |


`**TrendPointDto`:** `label` (string), `value` (number).

##### Example (200) — excerpt

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": {
    "overview": {
      "coreCompliancePercent": 87.5,
      "failedCoreCount": 2,
      "oesAtOrBelowThreeCount": 14,
      "openActionPlansCount": 3
    },
    "accreditation": {
      "overallCompliancePercent": 82.3,
      "coreOeAverageScore": 4.1,
      "averageStandardScore": 4.0,
      "averageChapterScore": 3.9,
      "violations": ["Core OE C12 below threshold in Chapter 3"]
    },
    "accreditationReadinessPercent": 78.0,
    "oeScoreTrend": [{ "label": "W1", "value": 3.8 }, { "label": "W2", "value": 4.0 }],
    "chapterImprovementTrend": [{ "label": "W1", "value": 1.2 }],
    "riskEvolutionTrend": [{ "label": "W1", "value": 12.0 }]
  },
  "errors": [],
  "meta": { "requestId": "0HMVTEST123458", "timestamp": "2026-04-11T12:36:00.000Z" }
}
```

##### Errors

Same pattern as session (400/401/403/404/500).

---

### 5.C Chapters index

#### `GET /api/v1.0/journeys/{journeyId}/chapters`

**Description:** **Mode-aware** chapter list for the **left rail / chapter picker** with Phase 48 display fields.

**UI:** Chapter list, filters.

##### Path parameters


| Name        | Type | Required |
| ----------- | ---- | -------- |
| `journeyId` | UUID | Yes      |


##### Query parameters


| Name                 | Type   | Required | Description                         |
| -------------------- | ------ | -------- | ----------------------------------- |
| `operationalMode`    | int    | No       | §4                                  |
| `auditLifecycleMode` | string | No       | §4                                  |
| `search`             | string | No       | Server-side filter when implemented |
| `sort`               | string | No       | Sort key when implemented           |


##### Success `data`: `JourneyChaptersIndexDto`


| Field   | Type                           | Description  |
| ------- | ------------------------------ | ------------ |
| `items` | `JourneyChapterIndexItemDto[]` | Chapter rows |


`**JourneyChapterIndexItemDto` (field coverage for UI)**


| Field                    | Type     | UI use                                   |
| ------------------------ | -------- | ---------------------------------------- |
| `id`                     | UUID     | Chapter id (route to standards)          |
| `code`                   | string   | Short code                               |
| `name`                   | string   | Title                                    |
| `complianceScorePercent` | number?  | Aggregate score %                        |
| `risk`                   | string?  | Risk label                               |
| `coreComplianceStatus`   | string?  | Status text                              |
| `openIssuesCount`        | int?     | Open issues                              |
| `pendingAudit`           | boolean? | Pending audit flag                       |
| `coreFailureFlag`        | boolean? | Core failure                             |
| `issueBadgeLabel`        | string?  | Badge                                    |
| `passFailLabel`          | string?  | Pass/fail                                |
| `changedSinceLastAudit`  | boolean? | **May be null** if history not available |
| `maturityLabel`          | string?  | Maturity                                 |
| `highlightHighRisk`      | boolean? | Emphasis                                 |


##### Example (200)

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": {
    "items": [
      {
        "id": "b2c3d4e5-f678-90ab-cdef-1234567890ab",
        "code": "COP",
        "name": "Care of Patients",
        "complianceScorePercent": 88.4,
        "risk": "Medium",
        "coreComplianceStatus": "Compliant",
        "openIssuesCount": 2,
        "pendingAudit": false,
        "coreFailureFlag": false,
        "issueBadgeLabel": "2 open",
        "passFailLabel": "Pass",
        "changedSinceLastAudit": null,
        "maturityLabel": "Maturing",
        "highlightHighRisk": false
      }
    ]
  },
  "errors": [],
  "meta": { "requestId": "0HMVTEST123459", "timestamp": "2026-04-11T12:37:00.000Z" }
}
```

---

### 5.D Chapter standards

#### `GET /api/v1.0/journeys/{journeyId}/chapters/{chapterId}/standards`

**Description:** **Paginated** list of standards for one chapter, each with nested **objective elements** and standard-level labels. Use for **chapter detail → standards grid**.

##### Path parameters


| Name        | Type | Required |
| ----------- | ---- | -------- |
| `journeyId` | UUID | Yes      |
| `chapterId` | UUID | Yes      |


##### Query parameters


| Name                 | Type    | Default | Description |
| -------------------- | ------- | ------- | ----------- |
| `operationalMode`    | int?    | —       | §4          |
| `auditLifecycleMode` | string? | —       | §4          |
| `page`               | int     | 1       | Page index  |
| `pageSize`           | int     | 50      | Page size   |


> **Implementation note:** Response envelope wraps `**IReadOnlyList<ComplianceStandardDto>`** in `data` (array). Pagination may be reflected in future metadata; clients should send `page` / `pageSize` as implemented.

##### Success `data`: `ComplianceStandardDto[]`

`**ComplianceStandardDto`**


| Field                   | Type                    | UI                                                              |
| ----------------------- | ----------------------- | --------------------------------------------------------------- |
| `id`                    | UUID                    | Standard id                                                     |
| `chapterId`             | UUID                    | Parent chapter                                                  |
| `code`                  | string                  | Standard code                                                   |
| `name`                  | string                  | Title (spec calls this “title” in places; field name is `name`) |
| `description`           | string?                 | Long text                                                       |
| `sequence`              | int                     | Order                                                           |
| `weight`                | number                  | Weight                                                          |
| `objectiveElements`     | `ObjectiveElementDto[]` | Child OEs                                                       |
| `averageScore`          | number?                 | Average                                                         |
| `coreOeAverage`         | number?                 | Core subset                                                     |
| `naCount`               | int                     | NA count                                                        |
| `completionPercent`     | number?                 | **Completion %**                                                |
| `riskBadge`             | string?                 | Risk                                                            |
| `coreSummary`           | string?                 | Summary                                                         |
| `openIssuesCount`       | int?                    | Issues                                                          |
| `complianceStatusLabel` | string?                 | Status label                                                    |
| `evidenceGapLabel`      | string?                 | Gap label                                                       |
| `changeTrend`           | string?                 | `up`                                                            |
| `previouslyFailed`      | boolean?                | History                                                         |
| `excellenceBadge`       | boolean?                | Excellence                                                      |


##### Example (200) — one standard excerpt

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": [
    {
      "id": "c3d4e5f6-a789-01bc-def0-2345678901bc",
      "chapterId": "b2c3d4e5-f678-90ab-cdef-1234567890ab",
      "code": "COP.1",
      "name": "Patient assessment",
      "description": "Organization has documented assessment processes.",
      "sequence": 1,
      "weight": 1.0,
      "averageScore": 4.2,
      "coreOeAverage": 4.0,
      "naCount": 0,
      "completionPercent": 92.5,
      "riskBadge": "Low",
      "coreSummary": "Core OEs on track",
      "openIssuesCount": 0,
      "complianceStatusLabel": "Compliant",
      "evidenceGapLabel": "No gap",
      "changeTrend": "up",
      "previouslyFailed": false,
      "excellenceBadge": true,
      "objectiveElements": []
    }
  ],
  "errors": [],
  "meta": { "requestId": "0HMVTEST123460", "timestamp": "2026-04-11T12:38:00.000Z" }
}
```

---

### 5.E Objective element — detail

#### `GET /api/v1.0/journeys/{journeyId}/objective-elements/{objectiveElementId}`

**Description:** Single **OE** with workspace fields, evidence list, history, action plans. Use for **OE detail / execution** screen.

##### Path parameters


| Name                 | Type | Required |
| -------------------- | ---- | -------- |
| `journeyId`          | UUID | Yes      |
| `objectiveElementId` | UUID | Yes      |


##### Query parameters


| Name                 | Type   | Required |
| -------------------- | ------ | -------- |
| `operationalMode`    | int    | No       |
| `auditLifecycleMode` | string | No       |


##### Success `data`: `ObjectiveElementDto`

**Core identity**


| Field            | Type   |
| ---------------- | ------ |
| `id`             | UUID   |
| `standardId`     | UUID?  |
| `code`           | string |
| `title`          | string |
| `description`    | string |
| `classification` | int    |


**Rules / metadata**


| Field                     | Type    |
| ------------------------- | ------- |
| `isMandatory`             | boolean |
| `isScorable`              | boolean |
| `isApplicable`            | boolean |
| `isDocumentationRequired` | boolean |
| `weight`                  | number  |
| `tags`                    | string? |
| `department`              | string? |
| `responsibleRole`         | string? |
| `frequency`               | string? |
| `evidenceCategory`        | string? |


**Scoring / audit**


| Field                  | Type                 |
| ---------------------- | -------------------- |
| `score`                | int?                 |
| `scoreLabel`           | string?              |
| `isNa`                 | boolean              |
| `naJustification`      | string?              |
| `isCoreCompliant`      | boolean              |
| `observationList`      | `OeObservationDto[]` |
| `evidenceVerifiedFlag` | boolean              |
| `evidenceStatus`       | string?              |
| `auditRemarks`         | string?              |
| `scoreRationale`       | string?              |
| `auditStatus`          | string?              |


**Collections**


| Field         | Type                  |
| ------------- | --------------------- |
| `actionPlans` | `OeActionPlanDto[]`   |
| `evidence`    | `OeEvidenceItemDto[]` |
| `history`     | `OeHistoryEventDto[]` |


**Phase 48 / workflow**


| Field                   | Type     |
| ----------------------- | -------- |
| `journeyWorkflowStatus` | string?  |
| `changedSinceLastAudit` | boolean? |
| `excellenceNote`        | string?  |
| `version`               | int?     |
| `lastUpdatedAt`         | string?  |
| `dueAt`                 | string?  |
| `nextReviewAt`          | string?  |
| `assigneeDisplayName`   | string?  |
| `departmentLabel`       | string?  |


`**OeEvidenceItemDto`:** `id`, `name`, `verificationStatus`, `uploadedAt`, `expiryAt`, `uploadedBy`.

##### Example (200) — excerpt

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": {
    "id": "d4e5f6a7-b890-12cd-ef01-3456789012cd",
    "standardId": "c3d4e5f6-a789-01bc-def0-2345678901bc",
    "code": "COP.1.1",
    "title": "Initial assessment within 24 hours",
    "description": "Patient receives timely initial nursing assessment.",
    "classification": 0,
    "isMandatory": true,
    "isScorable": true,
    "isApplicable": true,
    "isDocumentationRequired": true,
    "weight": 1.0,
    "score": 4,
    "scoreLabel": "4 — Fully met",
    "isNa": false,
    "isCoreCompliant": true,
    "evidenceVerifiedFlag": true,
    "evidenceStatus": "Verified",
    "auditStatus": "Accepted",
    "journeyWorkflowStatus": "In progress",
    "changedSinceLastAudit": null,
    "version": 3,
    "assigneeDisplayName": "Nurse Lead — Ward A",
    "departmentLabel": "Emergency",
    "evidence": [
      {
        "id": "e5f6a7b8-c901-23de-f012-4567890123de",
        "name": "Triage SOP 2025",
        "verificationStatus": "Verified",
        "uploadedAt": "2026-04-01T10:00:00Z",
        "expiryAt": null,
        "uploadedBy": "user-uuid-as-string"
      }
    ],
    "history": [],
    "actionPlans": []
  },
  "errors": [],
  "meta": { "requestId": "0HMVTEST123461", "timestamp": "2026-04-11T12:39:00.000Z" }
}
```

---

### 5.E (continued) — PATCH objective element

#### `PATCH /api/v1.0/journeys/{journeyId}/objective-elements/{objectiveElementId}`

**Description:** Update score, NA flags, remarks, rationale. Supports **optimistic concurrency** via `version`. Returns **updated OE fragment** plus optional **overview** metrics and new **version**.

**UI:** OE scoring form, NA toggle.

##### Path parameters

Same as GET OE.

##### Query parameters


| Name                 | Type   | Required |
| -------------------- | ------ | -------- |
| `operationalMode`    | int    | No       |
| `auditLifecycleMode` | string | No       |


##### Request body: `PatchObjectiveElementRequest`


| Field             | Type    | Required | Rules                                                                 |
| ----------------- | ------- | -------- | --------------------------------------------------------------------- |
| `version`         | int     | No       | Must equal server `ObjectiveElementDto.version` if sent; else **409** |
| `isNa`            | boolean | No       | If true, typically requires justification                             |
| `naJustification` | string  | No       | Required when marking NA (server validation)                          |
| `score`           | int     | No       | Within allowed range when scorable                                    |
| `auditRemarks`    | string  | No       |                                                                       |
| `scoreRationale`  | string  | No       |                                                                       |


##### Example request

```json
{
  "version": 3,
  "score": 4,
  "isNa": false,
  "auditRemarks": "Verified during mock drill",
  "scoreRationale": "Evidence complete for core requirements"
}
```

##### Success `data`: `PatchObjectiveElementResponseDto`


| Field              | Type                         |
| ------------------ | ---------------------------- |
| `objectiveElement` | `ObjectiveElementDto`        |
| `overview`         | `JourneyOverviewMetricsDto`? |
| `version`          | int                          |


##### Example (200)

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": {
    "objectiveElement": {
      "id": "d4e5f6a7-b890-12cd-ef01-3456789012cd",
      "score": 4,
      "version": 4
    },
    "overview": {
      "coreCompliancePercent": 87.6,
      "failedCoreCount": 2,
      "oesAtOrBelowThreeCount": 13,
      "openActionPlansCount": 3
    },
    "version": 4
  },
  "errors": [],
  "meta": { "requestId": "0HMVTEST123462", "timestamp": "2026-04-11T12:40:00.000Z" }
}
```

##### Errors


| HTTP | Scenario                                             | `code`               |
| ---- | ---------------------------------------------------- | -------------------- |
| 400  | Validation (NA without justification, invalid score) | `VALIDATION_FAILED`  |
| 401  | Not authenticated                                    | `UNAUTHORIZED`       |
| 403  | Forbidden / read-only journey                        | `FORBIDDEN`          |
| 404  | Journey or OE not found                              | `RESOURCE_NOT_FOUND` |
| 409  | Version mismatch                                     | `RESOURCE_CONFLICT`  |
| 500  | Server error                                         | `INTERNAL_ERROR`     |


---

### 5.F Evidence

#### `POST /api/v1.0/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence` (JSON)

**Description:** Register evidence by **reference** (`fileUrl`) with type and metadata. Creates DB evidence and links to OE.

**Content-Type:** `application/json`

##### Request body: `RegisterOeEvidenceRequest`


| Field          | Type   | Required | Description                                     |
| -------------- | ------ | -------- | ----------------------------------------------- |
| `evidenceType` | string | Yes      | Type discriminator                              |
| `fileUrl`      | string | Yes      | URL or reference to artifact                    |
| `title`        | string | No       | Display title                                   |
| `metadata`     | string | No       | Opaque string (often JSON serialized by client) |


##### Example

```json
{
  "evidenceType": "PolicyDocument",
  "fileUrl": "https://cdn.example.com/hospital/triage-sop-2025.pdf",
  "title": "Triage SOP 2025",
  "metadata": "{\"revision\":\"1.3\"}"
}
```

##### Success `data`

```json
{ "evidenceId": "e5f6a7b8-c901-23de-f012-4567890123de" }
```

---

#### `POST /api/v1.0/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence` (multipart)

**Description:** Upload a **file**; server stores under app content root and sets `FileUrl` to a `local://...` reference.

**Content-Type:** `multipart/form-data`  
**Max body size:** **10 MB** (Gateway and ComplianceService). Larger uploads → **413** from host.

##### Form fields


| Field          | Type   | Required | Description                 |
| -------------- | ------ | -------- | --------------------------- |
| `file`         | file   | Yes      | Binary part name `**file`** |
| `evidenceType` | string | Yes      |                             |
| `title`        | string | No       |                             |
| `metadata`     | string | No       |                             |


##### Success `data`

Same shape as JSON: `{ "evidenceId": "<uuid>" }`.

---

#### `DELETE /api/v1.0/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}`

**Description:** Unlink evidence from OE; delete evidence row if no other links.

##### Success `data`

```json
{ "deleted": true }
```

##### Errors


| HTTP | Scenario                      |
| ---- | ----------------------------- |
| 403  | Locked journey / forbidden    |
| 400  | Validation (not linked, etc.) |


---

#### `GET /api/v1.0/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence`

**Description:** List evidence items linked to the OE (simpler list than embedded in OE detail). **Included in spec for parity with implementation.**

**Success `data`:** `OeEvidenceItemDto[]`.

---

#### `GET /api/v1.0/journeys/{journeyId}/objective-elements/{objectiveElementId}/history`

**Description:** History events for the OE (e.g. score changes). **Included for execution audit trail UI.**

**Success `data`:** `OeHistoryEventDto[]`.

---

### 5.G Operational mode

#### `PATCH /api/v1.0/journeys/{journeyId}/session/operational-mode`

**Description:** Advance **operational mode** by one step (or no-op if same). Returns **updated session** payload.

##### Request body: `PatchOperationalModeRequestDto`


| Field             | Type | Required | Description                         |
| ----------------- | ---- | -------- | ----------------------------------- |
| `operationalMode` | int  | Yes      | Target mode 0–4 (must be next step) |


##### Example

```json
{ "operationalMode": 2 }
```

##### Success `data`

Same as `**GET .../session**` (`JourneySessionDto`).

##### Errors


| HTTP | Scenario             | `code`              |
| ---- | -------------------- | ------------------- |
| 403  | Locked / not allowed | `FORBIDDEN`         |
| 409  | Invalid transition   | `RESOURCE_CONFLICT` |
| 401  | Unauthorized         | `UNAUTHORIZED`      |


---

### 5.H Wizard progress (optional / not implemented)

#### `POST /api/v1.0/journeys/{journeyId}/objective-elements/{objectiveElementId}/wizard/progress`

**Status:** **Not implemented** on the Journey controller in the current backend. Product options:

- Omit the route until Phase 48 UI requires it, or  
- Return **501 Not Implemented** if added as a stub.

**Planned body (if implemented):** JSON with step id, completion flags, timestamps — **TBD by product**.

---

## 6. Additional journey APIs (reference)

These are **journey-related** and available on the same Gateway controller; use for **lifecycle**, **workspace legacy**, or **discovery**.


| Method | Path                                                                 | Purpose                                                               |
| ------ | -------------------------------------------------------------------- | --------------------------------------------------------------------- |
| POST   | `/api/v1.0/hospitals/{hospitalId}/frameworks/{frameworkId}/journeys` | Create journey                                                        |
| GET    | `/api/v1.0/journeys/{journeyIdOrComposite}`                          | Details by id or `hospitalId_frameworkId` composite                   |
| GET    | `/api/v1.0/hospitals/{hospitalId}/frameworks/{frameworkId}/journey`  | Lookup by hospital + framework                                        |
| GET    | `/api/v1.0/journeys/{journeyId}/valid-transitions`                   | Lifecycle transitions                                                 |
| POST   | `/api/v1.0/journeys/{journeyId}/transition`                          | Lifecycle transition                                                  |
| POST   | `/api/v1.0/journeys/{journeyId}/approve`                             | Semantic approve                                                      |
| POST   | `/api/v1.0/journeys/{journeyId}/reject`                              | Semantic reject                                                       |
| POST   | `/api/v1.0/journeys/{journeyId}/suspend`                             | Suspend                                                               |
| POST   | `/api/v1.0/journeys/{journeyId}/revoke`                              | Revoke                                                                |
| POST   | `/api/v1.0/journeys/{journeyId}/reinstate`                           | Reinstate                                                             |
| GET    | `/api/v1.0/journeys/{journeyId}/workspace`                           | **Legacy** full snapshot; `operationalMode` as **string enum** in DTO |


---

## 7. OperationalMode behavior matrix (summary)


| Endpoint               | Mode query             | Stored mode if omitted | Read-only impact                      |
| ---------------------- | ---------------------- | ---------------------- | ------------------------------------- |
| GET session            | Yes                    | Used from journey      | Flags in `JourneySessionDto`          |
| GET dashboard          | Yes                    | Journey                | Metrics reflect filtered scope        |
| GET chapters           | Yes                    | Journey                | Items filtered for mode               |
| GET standards          | Yes                    | Journey                | Standards/OEs filtered                |
| GET OE                 | Yes                    | Journey                | OE hidden if not in scope             |
| PATCH OE               | Yes                    | Journey                | Blocked if read-only                  |
| Evidence POST/DELETE   | No mode query on route | —                      | Blocked if journey ≥ Submitted locked |
| PATCH operational-mode | Body                   | —                      | Blocked when policy says locked       |


---

## 8. Error handling standard

### 8.1 HTTP status ↔ `ApiResponseCode` (common)


| HTTP | Code                  | When                                    |
| ---- | --------------------- | --------------------------------------- |
| 200  | `SUCCESS`             | OK                                      |
| 400  | `VALIDATION_FAILED`   | Bad input, disagreeing query pair       |
| 401  | `UNAUTHORIZED`        | Missing/invalid JWT                     |
| 403  | `FORBIDDEN`           | Policy / tenant / read-only             |
| 404  | `RESOURCE_NOT_FOUND`  | Missing journey/OE                      |
| 409  | `RESOURCE_CONFLICT`   | OE version, operational mode transition |
| 413  | *(host)*              | Payload too large (multipart)           |
| 429  | `TOO_MANY_REQUESTS`   | Rate limit (if enforced)                |
| 500  | `INTERNAL_ERROR`      | Unhandled server error                  |
| 503  | `SERVICE_UNAVAILABLE` | Gateway upstream failure                |


### 8.2 Gateway upstream failures

If ComplianceService is down, Gateway may return `**SERVICE_UNAVAILABLE`**, `**SERVICE_TIMEOUT`**, or `**SERVICE_COMMUNICATION_ERROR**` with the same `ApiResponse` envelope.

### 8.3 Problem Details

The platform uses `**ApiResponse<T>**`, not RFC 7807 `application/problem+json`. Map `code` + HTTP status for UI alerts.

---

## 9. Versioning and compatibility

- **URL versioning:** `/api/v1.0/...` is the supported minor version in routes.
- **Backward compatibility:** `GET .../workspace` retains older DTO shape (string `operationalMode`). New screens should prefer **session + dashboard + chapters + OE** routes with **integer** mode on session.
- **Breaking:** `GET .../chapters` returns `**items`** wrapper (`JourneyChaptersIndexDto`), not a bare array.

---

## 10. Validation checklist (frontend)

- Send `Authorization: Bearer` on all calls.
- Use **integer** `operationalMode` (0–4) for new screens; align `auditLifecycleMode` only if using string alias.
- Do not send conflicting `operationalMode` and `auditLifecycleMode`.
- Respect `readOnly` / `snapshotReadOnly` from session before PATCH/POST/DELETE.
- Send `version` on PATCH OE when editing after load; handle **409** with refresh.
- Multipart: field name `**file`**, max **10 MB**.
- Correlation: optional `**X-Correlation-Id`** for support logs.

---

## 11. Document control


| Item             | Value                                                                                                                                                   |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Primary consumer | Web App (Angular or other)                                                                                                                              |
| Source of truth  | `Gateway/Controllers/JourneysController.cs`, `ComplianceService/Controllers/V1/JourneysController.cs`, DTOs under `ComplianceService/Application/DTOs/` |
| Envelope         | `Shared.Infrastructure/DTO/ApiResponse.cs`, `ApiResponseFactory`, `ApiResponseCodes`                                                                    |


---

*End of specification.*