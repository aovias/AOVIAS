# Gateway — Knowledge Base API specification

This document describes the **public HTTP API** exposed by the Gateway service’s `KnowledgeBaseController`. It proxies the internal KnowledgeBase (PRAMAN cognitive engine) service. Use it to implement Angular (or any) clients: models, `HttpClient` calls, and response binding.

**Related:** For **Objective Element wizard** read-only field binding (`nodeCode`-keyed GETs, UI matrix, ambiguity notes), see [Knowledge Base — wizard UI binding (read-only)](./knowledge-base-wizard-ui-binding.md).

**Base path:** `/api/v{version}/knowledge-base`  
**Supported API version:** `1.0` (default when omitted in clients that send `Api-Version` or URL segment per your Gateway setup).

**Authentication:** All endpoints require a valid JWT and the `**ComplianceScope`** policy (typically scope `**compliance.api`**). Send:

```http
Authorization: Bearer <access_token>
```

Optional tracing (recommended):

```http
X-Correlation-Id: <uuid-or-opaque-id>
```

**JSON naming:** Responses use **camelCase** property names unless your Gateway overrides JSON options.

---

## Standard response envelope (`ApiResponse<T>`)

Every successful or error payload from these endpoints follows the shared envelope (`Shared.Infrastructure.DTO.ApiResponse<T>`).

```typescript
// Angular-friendly sketch — align field names with runtime JSON (camelCase)
interface ApiResponse<T> {
  success: boolean;
  code: string;           // e.g. "SUCCESS", "VALIDATION_FAILED", "RESOURCE_NOT_FOUND"
  message: string;
  data: T | null;         // null when success is false
  errors: ApiError[];
  meta: {
    requestId?: string;
    timestamp: string;    // ISO-8601 UTC
  };
}

interface ApiError {
  code: string;
  message: string;
  status?: number;
  details?: unknown;
}
```

### Dummy success wrapper (generic)

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": {},
  "errors": [],
  "meta": {
    "requestId": "0HN1234567890:00000001",
    "timestamp": "2026-04-12T10:15:30.0000000Z"
  }
}
```

### Dummy error wrapper

```json
{
  "success": false,
  "code": "VALIDATION_FAILED",
  "message": "Validation failed",
  "data": null,
  "errors": [],
  "meta": {
    "requestId": "0HN1234567890:00000001",
    "timestamp": "2026-04-12T10:15:30.0000000Z"
  }
}
```

---

## Domain DTOs (appear inside `data`)

These mirror KnowledgeBase application DTOs; bind Angular interfaces to these shapes.

### `FrameworkDto`

```json
{
  "id": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7",
  "code": "NABH_HOSP_6TH",
  "name": "NABH Hospital 6th Edition",
  "version": "6",
  "description": "Full accreditation hospital standards",
  "isActive": true
}
```

### `ComplianceNodeDto`

```json
{
  "id": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
  "frameworkId": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7",
  "parentId": null,
  "level": 1,
  "code": "PRE.1",
  "title": "Patient rights",
  "description": "…",
  "nodeType": "ObjectiveElement",
  "sequence": 3
}
```

### `KnowledgeItemDto`

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "nodeId": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
  "explanation": "Short explanation of the requirement.",
  "whyItMatters": "Why this matters clinically and for accreditation.",
  "howToComply": "Steps to demonstrate compliance.",
  "evidenceRequired": "Policies, records, audits…",
  "auditChecklist": "Auditor checklist bullets.",
  "riskLevel": "Medium",
  "impactLevel": "High",
  "tags": "patient-rights;documentation",
  "version": 1,
  "isActive": true
}
```

### `RequirementDetailDto`

```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "nodeId": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
  "intent": "Ensure…",
  "measurableElements": "ME1; ME2",
  "classification": "Core",
  "isMandatory": true,
  "applicableDepartments": "All clinical",
  "requiresDocumentation": true
}
```

### `RequirementScoreDto`

```json
{
  "id": "33333333-3333-3333-3333-333333333333",
  "nodeId": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
  "maxScore": 10,
  "weight": 1.5,
  "classificationId": "44444444-4444-4444-4444-444444444444"
}
```

### `NodeDetailsDto` (`data` for node details)

```json
{
  "node": { },
  "requirement": { },
  "knowledge": [ { } ],
  "requirementScore": { }
}
```

*(Replace nested objects with full `ComplianceNodeDto`, `RequirementDetailDto`, `KnowledgeItemDto[]`, `RequirementScoreDto` as above.)*

### `AiAskRequestDto` (POST body)

```json
{
  "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "frameworkId": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7",
  "nodeId": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
  "query": "How do we evidence PRE.1 for our ICU?"
}
```

### `AiAskResponseDto` (`data` for POST ai/ask)

```json
{
  "interactionId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "response": "Suggested evidence includes…",
  "modelUsed": "mock-or-gpt",
  "tokensUsed": 120
}
```

### `CreateDecisionRequestDto` (POST body)

```json
{
  "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "frameworkId": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7",
  "nodeId": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
  "aiInteractionId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "problemStatement": "Gap in documentation",
  "suggestedAction": "Add SOP",
  "finalDecision": "Accept with action plan",
  "outcome": "Pending",
  "confidenceScore": 0.85
}
```

### `DecisionLogDto` (`data` for POST decisions)

```json
{
  "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
  "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "frameworkId": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7",
  "nodeId": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
  "aiInteractionId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "problemStatement": "…",
  "suggestedAction": "…",
  "finalDecision": "…",
  "outcome": "…",
  "confidenceScore": 0.85,
  "createdAt": "2026-04-12T10:20:00Z"
}
```

### `CreateAssessmentRequestDto` (POST body)

```json
{
  "facilityId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
  "frameworkId": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7"
}
```

### `AssessmentDto` (`data` for POST assessments)

```json
{
  "id": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
  "facilityId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
  "frameworkId": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7",
  "status": "Draft",
  "createdAt": "2026-04-12T10:25:00Z"
}
```

### `UpdateAssessmentItemRequestDto` (PUT body)

```json
{
  "score": 8,
  "complianceStatus": "Met",
  "remarks": "Evidence uploaded",
  "hasActionPlan": false,
  "documentationAdequate": true
}
```

### `AssessmentSummaryResponseDto` (`data` for GET summary)

```json
{
  "overallPercent": 82.5,
  "meetsOverallPercent": true,
  "coreRulePassed": true,
  "coreFailureNodeIds": [],
  "chapterAverages": {
    "b7cf29c6-2d92-41e1-b754-0035bddf8d6e": 90.0
  },
  "standardAverages": {
    "11111111-1111-1111-1111-111111111111": 85.0
  },
  "chapterAveragesPassed": true,
  "standardAveragesPassed": true,
  "commitmentLevelPassed": true,
  "tooManyLowScoresInStandard": false,
  "standardsWithMultipleLowScores": [],
  "actionPlanViolationNodeIds": []
}
```

---

## Endpoints

All paths below are **relative to** `/api/v1.0/knowledge-base` (replace `1.0` if you version explicitly).


| Method | Relative path                       | Description                                      |
| ------ | ----------------------------------- | ------------------------------------------------ |
| GET    | `/frameworks`                       | List frameworks                                  |
| GET    | `/frameworks/{id}`                  | Get framework by id                              |
| GET    | `/nodes?frameworkId={guid}`         | List nodes for framework                         |
| GET    | `/nodes/{id}`                       | Get node by id                                   |
| GET    | `/nodes/{id}/children`              | Children of node                                 |
| GET    | `/nodes/{id}/details`               | Node + requirement + knowledge + score           |
| GET    | `/nodes/code/{nodeCode}/details`    | Same as `/nodes/{id}/details`, keyed by **code** |
| GET    | `/nodes/{nodeId}/decisions`         | Decision logs for node                           |
| GET    | `/knowledge/node/{nodeId}`          | Knowledge items for node id                      |
| GET    | `/knowledge/node/code/{nodeCode}`   | Knowledge items by node **code**                 |
| GET    | `/knowledge/search?q=&frameworkId=` | Search knowledge                                 |
| POST   | `/ai/ask`                           | AI question                                      |
| POST   | `/decisions`                        | Create decision                                  |
| POST   | `/assessments`                      | Create assessment                                |
| PUT    | `/assessments/{id}/items/{nodeId}`  | Upsert assessment line                           |
| GET    | `/assessments/{id}/summary`         | Assessment summary                               |


---

### GET `/frameworks`

**Response `data`:** `FrameworkDto[]`

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": [
    {
      "id": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7",
      "code": "NABH_HOSP_6TH",
      "name": "NABH Hospital 6th Edition",
      "version": "6",
      "description": "…",
      "isActive": true
    }
  ],
  "errors": [],
  "meta": { "requestId": "…", "timestamp": "2026-04-12T10:00:00Z" }
}
```

---

### GET `/frameworks/{id}`

**Path:** `id` — GUID.

**Response `data`:** `FrameworkDto` or failure `RESOURCE_NOT_FOUND`.

---

### GET `/nodes`

**Query:** `frameworkId` (required, GUID).

**Response `data`:** `ComplianceNodeDto[]`

---

### GET `/nodes/{id}`

**Response `data`:** `ComplianceNodeDto` or `RESOURCE_NOT_FOUND`.

---

### GET `/nodes/{id}/children`

**Response `data`:** `ComplianceNodeDto[]`

---

### GET `/nodes/{id}/details`

**Response `data`:** `NodeDetailsDto` or `RESOURCE_NOT_FOUND`.

---

### GET `/nodes/code/{nodeCode}/details`

**Path:** `nodeCode` — compliance node **code** (e.g. `PRE.1`, `CH1`). URL-encode reserved characters when building the path.

**Response `data`:** `NodeDetailsDto` — **same shape** as `GET /nodes/{id}/details` for the resolved node.

**Errors:**

- `VALIDATION_FAILED` — empty `nodeCode`.
- `RESOURCE_NOT_FOUND` — no node with that code, or node missing extended data in an inconsistent state.
- `RESOURCE_CONFLICT` — more than one node in the database shares the same `code` (ambiguous lookup).

**Dummy success `data` (`NodeDetailsDto`):**

```json
{
  "node": {
    "id": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
    "frameworkId": "0fe8a9c0-9ea0-4970-9097-d4f73d45dca7",
    "parentId": null,
    "level": 1,
    "code": "CH1",
    "title": "Chapter",
    "description": "",
    "nodeType": "Chapter",
    "sequence": 1
  },
  "requirement": null,
  "knowledge": [],
  "requirementScore": null
}
```

---

### GET `/nodes/{nodeId}/decisions`

**Response `data`:** `DecisionLogDto[]`

---

### GET `/knowledge/node/{nodeId}`

**Response `data`:** `KnowledgeItemDto[]`

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "nodeId": "b7cf29c6-2d92-41e1-b754-0035bddf8d6e",
      "explanation": "…",
      "whyItMatters": "…",
      "howToComply": "…",
      "evidenceRequired": "…",
      "auditChecklist": "…",
      "riskLevel": "Medium",
      "impactLevel": "High",
      "tags": "…",
      "version": 1,
      "isActive": true
    }
  ],
  "errors": [],
  "meta": { "requestId": "…", "timestamp": "2026-04-12T10:00:00Z" }
}
```

---

### GET `/knowledge/node/code/{nodeCode}`

**Path:** `nodeCode` — compliance node **code** (e.g. `PRE.1`, `CH1`). URL-encode reserved characters (`/`, `%`, spaces, etc.).

**Response `data`:** `KnowledgeItemDto[]` (same shape as by node id).

**Errors:**

- `RESOURCE_NOT_FOUND` — no node with that code.
- `RESOURCE_CONFLICT` — more than one node in the database shares the same `code` (ambiguous lookup).

---

### GET `/knowledge/search`

**Query:**


| Parameter     | Required | Description          |
| ------------- | -------- | -------------------- |
| `q`           | No       | Free-text search     |
| `frameworkId` | No       | Limit to a framework |


**Response `data`:** `KnowledgeItemDto[]`

Example: `/knowledge/search?q=patient&frameworkId=0fe8a9c0-9ea0-4970-9097-d4f73d45dca7`

---

### POST `/ai/ask`

**Request body:** `AiAskRequestDto` (JSON).

**Response `data`:** `AiAskResponseDto`

**Validation:** Backend may return `VALIDATION_FAILED` if `frameworkId` is empty or `query` is empty.

---

### POST `/decisions`

**Request body:** `CreateDecisionRequestDto`

**Response `data`:** `DecisionLogDto`

**Errors:** e.g. `RESOURCE_NOT_FOUND` if referenced entities are missing.

---

### POST `/assessments`

**Request body:** `CreateAssessmentRequestDto`

**Response `data`:** `AssessmentDto`

**Validation:** `facilityId` and `frameworkId` must be non-empty GUIDs.

---

### PUT `/assessments/{id}/items/{nodeId}`

**Request body:** `UpdateAssessmentItemRequestDto`

**Response `data`:** often `null` on success with `SUCCESS` (confirm in your deployed build).

**Errors:** `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND` (depends on backend rules).

---

### GET `/assessments/{id}/summary`

**Response `data`:** `AssessmentSummaryResponseDto`

**Errors:** `RESOURCE_NOT_FOUND` if assessment does not exist.

---

## Angular binding tips

1. Define **interfaces** matching the JSON (`FrameworkDto`, `KnowledgeItemDto`, `ApiResponse<T>`).
2. Use `**HttpClient`** with `observe: 'body'` and map `response.data` after checking `response.success`.
3. For **POST/PUT**, send plain objects; `Content-Type: application/json`.
4. Pass **Bearer token** via an HTTP interceptor on the Gateway base URL.
5. For `**nodeCode`** paths, use `encodeURIComponent(code)` when building the URL if codes contain dots only, still safe; **must** encode if codes contain `/` or spaces.

---

## Document history


| Date       | Change                                                                                                |
| ---------- | ----------------------------------------------------------------------------------------------------- |
| 2026-04-12 | Initial specification for Gateway `KnowledgeBaseController`                                           |
| 2026-04-12 | Added `GET /nodes/code/{nodeCode}/details` (same contract as node details by id)                      |
| 2026-04-13 | Removed optional `frameworkId` query from `knowledge/node/code/...` and `nodes/code/.../details`      |
| 2026-04-13 | Linked [wizard UI binding (read-only)](./knowledge-base-wizard-ui-binding.md) for OE `nodeCode` flows |


