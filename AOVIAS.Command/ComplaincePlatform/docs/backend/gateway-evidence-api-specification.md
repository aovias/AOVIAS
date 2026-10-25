# Gateway — Evidence Storage, Tracking & Management API

**Document version:** 1.4  
**Scope:** APIs exposed by **Gateway** for **objective-element (OE) evidence** within a **compliance journey**.  
**Backend:** Requests are proxied to **ComplianceService** via `IProxyService` (`ComplianceService` HTTP client). Physical file storage (`local://` URIs, `IFileStorageService`) is implemented in ComplianceService, not in Gateway.

### Phase 05 — Browser multipart (CORS + validation)

- **CORS:** Gateway and ComplianceService use `AllowAnyHeader()` so browser preflight allows `Content-Type: multipart/form-data; boundary=...` together with `Authorization`, `api-version`, `X-Correlation-Id`, `X-Hospital-Id`, etc. Narrowing to a fixed header list would need to include `Content-Type` explicitly or uploads fail.
- **Multipart validation (400):** Missing/empty `file` or empty `evidenceType` returns **400** with `ApiResponse<T>` and `errors[]` entries including `**field`** + `**message**` (not only the generic validation message).

### Phase 03 refinement (implementation notes)

- **Download streaming:** Gateway uses `**ProxyComplianceServiceStreamGetAsync`** for `GET .../evidence/{evidenceId}/download` — HTTP response bytes are streamed (`HttpCompletionOption.ResponseHeadersRead` + `CopyToAsync` to the client response). Downstream `**Content-Type`** and `**Content-Disposition`** are forwarded; file bodies are **not** read into a single string/buffer in Gateway.
- **Delete + storage (aggregate removal):** On `**DELETE .../evidence/{evidenceId}`**, ComplianceService unlinks the OE–evidence row. If that was the **last** link to the evidence, the aggregate is removed. **Before** deleting `Evidence` / cascaded `EvidenceVersions` rows, ComplianceService deletes **every distinct** `local://` URL referenced by the current evidence row **and** all version rows (`IFileStorageService.DeleteAsync`), so no orphan blobs remain for removed aggregates. If other OE links still reference the same `Evidence`, only the link row is removed — files are **not** deleted.
- **Observability:** ComplianceService application logs for evidence operations include structured **JourneyId**, **EvidenceId**, and **CorrelationId** (from inbound `X-Correlation-ID` / `X-Correlation-Id`) on multipart save, evidence created, download, verify/reject, version save/upload, bulk per-item failure, and bulk summary. Gateway continues to attach correlation headers on outbound proxy calls and logs stream timing.

**Related documents:**

- Full journey UI surface (includes these routes in context): `[journey-module-api-specification.md](journey-module-api-specification.md)`
- Backend discovery (storage abstraction, entities): `[evidence-system-discovery-and-extension.md](evidence-system-discovery-and-extension.md)`

---

## 1. Purpose


| Capability     | Gateway support                                                                                                                                                                          |
| -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Storage**    | Register evidence by **URL reference** (JSON) or **upload** a file (multipart). Bytes are stored by ComplianceService; Gateway forwards the request.                                     |
| **Tracking**   | **List** evidence items for an OE; **detail** (full metadata + versions); richer tracking also appears on **OE detail** and **history** endpoints (see §6).                              |
| **Management** | **Create** (JSON/multipart/**bulk**), **new version** (multipart), **verify/reject**, **delete**, **download** (stream). Mutations are blocked when the journey is locked (≥ Submitted). |


---

## 2. Architecture (client → Gateway → ComplianceService)

```text
Client (Web / Mobile)
    │  HTTPS + Bearer JWT (compliance.api scope)
    ▼
Gateway — JourneysController
    │  MediatR → *Evidence* handlers
    ▼
IProxyService
    │  Forward: Authorization, X-Correlation-Id / X-Correlation-ID
    │  JSON: ProxyToComplianceServiceAsync
    │  Multipart: ProxyToComplianceServiceRawAsync (stream body)
    │  Binary download: ProxyComplianceServiceStreamGetAsync (streams response body)
    ▼
ComplianceService — JourneysController
    │  JourneyWorkspaceAppService + EvidenceService
    ▼
Database (Evidences, EvidenceVersions, ObjectiveElementEvidences) + IFileStorageService (local disk today)
```

**Still not on Gateway (by design):**


| Capability                                                           | Notes                                                                        |
| -------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Task-scoped evidence (`POST .../compliance/tasks/{taskId}/evidence`) | ComplianceService only — journey-scoped OE evidence is the primary web flow. |


**Journey-scoped verify/reject** is available on Gateway (see §5.6). The older Compliance-only **v1.1** `api/.../evidence/objective-elements/{id}/verify/...` route remains for backward compatibility but is not required for Gateway clients.

---

## 3. Base URL, versioning, auth


| Item                | Value                                                                                              |
| ------------------- | -------------------------------------------------------------------------------------------------- |
| **Base**            | `{GATEWAY_ORIGIN}` (e.g. `https://api.example.com`)                                                |
| **Path prefix**     | `/api/v{version}/journeys`                                                                         |
| **Default version** | `1.0` — use header `api-version: 1.0` or URL segment `/api/v1.0/...` per your API versioning setup |
| **Auth**            | `Authorization: Bearer <access_token>`                                                             |
| **Policy**          | `ComplianceScope` — token must include `**compliance.api`** scope                                  |


**Organization context:** ComplianceService resolves tenant from the user context; missing organization returns `ORGANIZATION_CONTEXT_MISSING` (see §8).

---

## 4. Response envelope

All **JSON** success responses from ComplianceService use `**ApiResponse<T>`** (see `Shared.Infrastructure.Api.ApiResponseFactory` / `ApiResponseCodes`). Gateway returns downstream **status code and body** for proxied calls (including multipart responses).

**Typical success payload for evidence create:**

```json
{
  "success": true,
  "code": "SUCCESS",
  "data": { "evidenceId": "<uuid>" },
  "meta": { "requestId": "...", "timestamp": "..." }
}
```

---

## 5. Evidence endpoints (Gateway)

All routes are on `**Gateway/Controllers/JourneysController.cs**`: `[Route("api/v{version:apiVersion}/journeys")]` and `[Authorize(Policy = ComplianceScope)]`.  
Literal segment `**evidence/bulk**` is registered **before** routes with `{evidenceId:guid}` so `bulk` is never parsed as a GUID. `**GET .../evidence`** (list) has no `{evidenceId}` segment and is distinct from `**GET .../evidence/{evidenceId}`** (detail).

### 5.0 Quick reference (method → MediatR → proxy)


| Method   | Path (relative to `/api/v{version}/journeys`)                                         | MediatR request                                       | `IProxyService` surface                             |
| -------- | ------------------------------------------------------------------------------------- | ----------------------------------------------------- | --------------------------------------------------- |
| `GET`    | `/{journeyId}/objective-elements/{objectiveElementId}/evidence`                       | `GetObjectiveElementEvidenceQuery`                    | `ProxyToComplianceServiceAsync` (GET)               |
| `POST`   | `/{journeyId}/objective-elements/{objectiveElementId}/evidence`                       | `PostObjectiveElementEvidenceCommand`                 | `ProxyToComplianceServiceAsync` (POST JSON)         |
| `POST`   | `/{journeyId}/objective-elements/{objectiveElementId}/evidence`                       | `PostObjectiveElementEvidenceMultipartCommand`        | `ProxyToComplianceServiceRawAsync` (multipart body) |
| `POST`   | `/{journeyId}/objective-elements/{objectiveElementId}/evidence/bulk`                  | `PostObjectiveElementEvidenceBulkMultipartCommand`    | `ProxyToComplianceServiceRawAsync`                  |
| `GET`    | `/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}`          | `GetObjectiveElementEvidenceDetailQuery`              | `ProxyToComplianceServiceAsync` (GET)               |
| `GET`    | `/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}/download` | `GetObjectiveElementEvidenceDownloadQuery`            | `**ProxyComplianceServiceStreamGetAsync`**          |
| `POST`   | `/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}/verify`   | `PostObjectiveElementEvidenceVerifyCommand`           | `ProxyToComplianceServiceAsync` (POST JSON)         |
| `POST`   | `/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}/versions` | `PostObjectiveElementEvidenceVersionMultipartCommand` | `ProxyToComplianceServiceRawAsync`                  |
| `DELETE` | `/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}`          | `DeleteObjectiveElementEvidenceCommand`               | `ProxyToComplianceServiceAsync` (DELETE)            |


Gateway **request size limits** (controller attributes): single-file uploads and non-bulk multipart `**10_485_760`** bytes (10 MB); bulk `**15 × 10_485_760`** bytes cap.

### 5.1 List evidence for an objective element


|                   |                                                                                             |
| ----------------- | ------------------------------------------------------------------------------------------- |
| **Method / path** | `GET /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence` |
| **Downstream**    | Same path on ComplianceService                                                              |
| **Proxy**         | `GetObjectiveElementEvidenceQuery` → `ProxyToComplianceServiceAsync` (GET)                  |


**Success `data`:** `OeEvidenceItemDto[]`


| Field                | Type        | Description                                            |
| -------------------- | ----------- | ------------------------------------------------------ |
| `id`                 | `guid`      | Evidence identifier                                    |
| `name`               | `string`    | Display name (title or evidence type)                  |
| `verificationStatus` | `string`    | Link-level status (e.g. Pending / Verified / Rejected) |
| `uploadedAt`         | `datetime?` | Upload time                                            |
| `expiryAt`           | `datetime?` | Optional validity end                                  |
| `uploadedBy`         | `string?`   | Uploader identifier                                    |


> **Note:** This list does **not** include `fileUrl` in the DTO; use JSON registration or backend access if the client must show the stored URI.

---

### 5.2 Register evidence by reference (JSON)


|                   |                                                                                              |
| ----------------- | -------------------------------------------------------------------------------------------- |
| **Method / path** | `POST /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence` |
| **Content-Type**  | `application/json`                                                                           |
| **Downstream**    | Same path, `RegisterOeEvidenceRequest` body                                                  |
| **Proxy**         | `PostObjectiveElementEvidenceCommand` → `ProxyToComplianceServiceAsync` (POST, JSON body)    |


**Request body (`RegisterOeEvidenceRequest`):**


| Field          | Type     | Required | Description                                              |
| -------------- | -------- | -------- | -------------------------------------------------------- |
| `evidenceType` | `string` | Yes      | Type discriminator (e.g. policy, SOP)                    |
| `fileUrl`      | `string` | Yes      | Reference URI (external URL or existing `local://` path) |
| `title`        | `string` | No       | Display title                                            |
| `metadata`     | `string` | No       | Opaque JSON string for extra attributes                  |


**Success `data`:** `{ "evidenceId": "<uuid>" }`

---

### 5.3 Upload evidence file (multipart)


|                   |                                                                                                                              |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| **Method / path** | `POST /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence`                                 |
| **Content-Type**  | `multipart/form-data`                                                                                                        |
| **Max body size** | **10 MB** (Gateway `FormOptions.MultipartBodyLengthLimit` and `KestrelServerOptions.Limits.MaxRequestBodySize`)              |
| **Downstream**    | Same path on ComplianceService                                                                                               |
| **Proxy**         | `PostObjectiveElementEvidenceMultipartCommand` → `ProxyToComplianceServiceRawAsync` (request body streamed as `HttpContent`) |


**Form fields (ComplianceService):**


| Field          | Type   | Required | Description                                                        |
| -------------- | ------ | -------- | ------------------------------------------------------------------ |
| `file`         | file   | Yes      | Binary; must be non-empty                                          |
| `evidenceType` | string | Yes      | Type discriminator (non-empty after trim)                          |
| `name`         | string | No       | Display name; **takes precedence over** `title` when both are sent |
| `title`        | string | No       | Display title (used if `name` is omitted)                          |
| `metadata`     | string | No       | Extra JSON                                                         |


**Validation errors (400):** `success: false`, `code` per `VALIDATION_FAILED`, `errors`: `[{ "field": "file" \| "evidenceType", "message": "..." }]`.

**Success `data`:** `{ "evidenceId": "<uuid>" }` (same envelope as JSON).

**Storage (backend):** ComplianceService persists bytes via `IFileStorageService` and stores `Evidence.FileUrl` as `local://App_Data/evidence_uploads/...` (see discovery doc). Gateway does not store files.

---

### 5.4 Delete evidence from objective element


|                   |                                                                                                             |
| ----------------- | ----------------------------------------------------------------------------------------------------------- |
| **Method / path** | `DELETE /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}` |
| **Downstream**    | Same path on ComplianceService                                                                              |
| **Proxy**         | `DeleteObjectiveElementEvidenceCommand` → `ProxyToComplianceServiceAsync` (DELETE)                          |


**Behavior (backend):** Unlinks the `ObjectiveElementEvidence` row. If the evidence is not linked to **any** other objective element, ComplianceService **first** deletes all **distinct** `local://` files for that aggregate (current `Evidence.FileUrl` plus every `EvidenceVersions.FileUrl`), then removes the `Evidence` row (version rows cascade in the database). If other OE links remain, only the link is removed — **files are kept**. Blocked when journey is locked after submission (≥ Submitted).

---

### 5.5 Bulk upload (multiple files, same OE)


|                      |                                                                                                   |
| -------------------- | ------------------------------------------------------------------------------------------------- |
| **Method / path**    | `POST /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/bulk` |
| **Content-Type**     | `multipart/form-data`                                                                             |
| **Max request size** | Up to **15 × 10 MB** (configurable on ComplianceService; Gateway Kestrel/form limits aligned).    |
| **Per-file limit**   | **≤ 10 MB** each; max **15** files per request (`Evidence:BulkUploadMaxFiles`).                   |
| **Proxy**            | `PostObjectiveElementEvidenceBulkMultipartCommand` → `ProxyToComplianceServiceRawAsync`           |


**Form fields:**


| Field            | Required                     | Description                                                            |
| ---------------- | ---------------------------- | ---------------------------------------------------------------------- |
| `files`          | Yes (one or more file parts) | Each file becomes a **separate** `Evidence` row mapped to the same OE. |
| `evidenceType`   | Yes                          | Shared type for all files in the request.                              |
| `sharedMetadata` | No                           | Optional JSON string stored per file with other metadata.              |


**Success `data`:** `BulkEvidenceUploadResponseDto` — `totalFiles`, `successCount`, `failureCount`, `items[]` with `fileName`, `status` (`Success` / `Failed`), `evidenceId`, `errorMessage`.

**Rules:** If the journey is **locked** (≥ Submitted), the **entire** request fails before per-file processing. Otherwise **partial success** is allowed (per-file errors in `items`).

---

### 5.6 Evidence detail


|                   |                                                                                                          |
| ----------------- | -------------------------------------------------------------------------------------------------------- |
| **Method / path** | `GET /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}` |
| **Proxy**         | `GetObjectiveElementEvidenceDetailQuery` → `ProxyToComplianceServiceAsync` (GET JSON)                    |


**Success `data`:** `EvidenceDetailDto` — identity, title, types, `fileUrl` (opaque), upload fields, verification from OE link, ordered `versions[]`.

---

### 5.7 Download current file


|                   |                                                                                                                                                  |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Method / path** | `GET /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}/download`                                |
| **Proxy**         | `GetObjectiveElementEvidenceDownloadQuery` → `**ProxyComplianceServiceStreamGetAsync`** (streams bytes; does not buffer the whole file in JSON). |


**Response:** File stream with `Content-Type` and `Content-Disposition`/`filename` derived from stored metadata when present (`contentType`, `originalFileName` in `Evidence.Metadata` JSON for new uploads).

---

### 5.8 Verify or reject (OE link)


|                   |                                                                                                                  |
| ----------------- | ---------------------------------------------------------------------------------------------------------------- |
| **Method / path** | `POST /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}/verify` |
| **Content-Type**  | `application/json`                                                                                               |
| **Proxy**         | `PostObjectiveElementEvidenceVerifyCommand` → `ProxyToComplianceServiceAsync`                                    |


**Body (`VerifyOeEvidenceRequest`):**


| Field      | Type      | Description                       |
| ---------- | --------- | --------------------------------- |
| `accepted` | `boolean` | `true` = verify, `false` = reject |
| `remarks`  | `string?` | Optional comments                 |


The **auditor identity** is the authenticated user (`UserId`) from the token — not client-supplied.

---

### 5.9 Upload a new file version (same evidence id)


|                   |                                                                                                                    |
| ----------------- | ------------------------------------------------------------------------------------------------------------------ |
| **Method / path** | `POST /api/v{version}/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/{evidenceId}/versions` |
| **Content-Type**  | `multipart/form-data`                                                                                              |
| **Max body**      | **10 MB**                                                                                                          |
| **Proxy**         | `PostObjectiveElementEvidenceVersionMultipartCommand` → `ProxyToComplianceServiceRawAsync`                         |


**Form fields:** `file` (required), `notes` (optional). Backend appends a new `EvidenceVersions` row and updates `Evidence.CurrentVersion` / `FileUrl`.

---

## 6. Related tracking (same Gateway controller)

These support **execution** and **audit-style** visibility but are not-only-evidence APIs:


| Method / path                                                                  | Use                                                                 |
| ------------------------------------------------------------------------------ | ------------------------------------------------------------------- |
| `GET .../journeys/{journeyId}/objective-elements/{objectiveElementId}`         | OE detail includes embedded `evidence[]` (see journey module spec). |
| `GET .../journeys/{journeyId}/objective-elements/{objectiveElementId}/history` | Score / assessment history events (not file upload history).        |


---

## 7. Correlation and observability

- Gateway **outbound** requests to ComplianceService include `**X-Correlation-Id`** and `**X-Correlation-ID`** (see `Gateway/Services/ProxyService.cs`).
- Clients may send `**X-Correlation-Id`** (or `**X-Correlation-ID**`) on inbound Gateway calls; values are forwarded downstream.
- Gateway logs proxy operations (including stream GET completion) with correlation id and elapsed time.
- **ComplianceService** enriches application-level **evidence** logs with **JourneyId**, **EvidenceId**, and **CorrelationId** where applicable (upload paths, download, verify/reject, versioning, bulk). This complements request-scoped logging from ASP.NET Core middleware.

---

## 8. Common error codes (envelope `code`)


| Code                                                                      | Typical cause                                                         |
| ------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| `SUCCESS`                                                                 | 2xx success                                                           |
| `VALIDATION_FAILED`                                                       | Missing/invalid body, empty file, business rule (e.g. no task for OE) |
| `FORBIDDEN`                                                               | Journey not in tenant                                                 |
| `ORGANIZATION_CONTEXT_MISSING`                                            | No organization context on user                                       |
| `SERVICE_UNAVAILABLE` / `SERVICE_TIMEOUT` / `SERVICE_COMMUNICATION_ERROR` | ComplianceService unreachable or proxy failure                        |


Exact HTTP status per code is defined in `ApiResponseCodes.Registry`.

---

## 9. Client checklist (evidence flows)

1. **Bearer token** with `compliance.api`.
2. **JSON** vs **multipart**: same URL path; **Content-Type** selects the action. Multipart must not exceed **10 MB**.
3. **Locked journey:** POST/DELETE evidence returns validation failure when journey status ≥ Submitted (ComplianceService rule).
4. **Evidence ↔ OE:** Mapping is by `objectiveElementId` (framework codes like AAC.1.c resolve via master data / OE id, not this API path).

---

## 10. Change log


| Version | Date       | Notes                                                                                                                                                                                                       |
| ------- | ---------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1.0     | 2026-04-16 | Initial Gateway-focused spec; aligns with `JourneysController` + evidence handlers and ComplianceService evidence implementation.                                                                           |
| 1.1     | 2026-04-16 | Phase 02: detail, download (stream proxy), verify, version upload, bulk upload; metadata `originalFileName` / `contentType` for downloads.                                                                  |
| 1.2     | 2026-04-16 | Phase 03: stream proxy behavior, delete removes `local://` files for last-link removal, ComplianceService correlation-aware evidence logs.                                                                  |
| 1.3     | 2026-04-16 | **§5.0** endpoint quick reference (method, path, MediatR, `IProxyService`); route-order notes (`evidence/bulk` literal); expanded delete/storage and §7 observability; Gateway `RequestSizeLimit` call-out. |


