# Phase 55 — Research: Gateway Evidence API ↔ WebApp binding

**Canonical spec (source of truth):**  
`ComplaincePlatform/docs/backend/gateway-evidence-api-specification.md` (v1.3, 2026-04-16)

**WebApp secondary doc:**  
`docs/backend/journey-screen-api-spec.md` (Journey UI surface; § Gateway v1.3 multipart must stay aligned with canonical Gateway doc)

---

## 1. Multipart contracts (already implemented)

| Gateway §5.3 single upload | `oe-evidence-upload.payload.ts` | Match |
| -------------------------- | --------------------------------- | ----- |
| `file` (required)          | `fd.append('file', …)`          | Yes   |
| `evidenceType` (required)  | `fd.append('evidenceType', …)`  | Yes   |
| `title` (optional)         | conditional append              | Yes   |
| `metadata` (optional JSON string) | conditional append       | Yes   |

| Gateway §5.5 bulk | Payload builder | Match |
| ----------------- | --------------- | ----- |
| `files` (≥1 parts) | `fd.append('files', …)` per file | Yes   |
| `evidenceType`    | `fd.append('evidenceType', …)`  | Yes   |
| `sharedMetadata`  | optional append                 | Yes   |

**Implication:** A `400` with `"file"` and `"evidenceType"` required on the **single** route usually means the request did not reach model binding as multipart (wrong `Content-Type`/boundary, empty body, or **bulk** shape posted to **non-bulk** URL). Field **names** in code match the spec; verify **URL** (`…/evidence` vs `…/evidence/bulk`) and **Network** payload in DevTools.

---

## 2. API versioning (§3)

Spec allows `/api/v{version}/journeys` with default **`1.0`** via `api-version: 1.0` or URL segment **`/api/v1.0/...`**. WebApp uses `environment.apiBaseUrl` (e.g. `…/api/v1`). **Action:** Confirm deployed Gateway accepts `v1` vs `v1.0` (Phase 49 already flagged this); document in `environment` comment — no silent mismatch.

---

## 3. Response typing gaps

- **Bulk success (§5.5):** `BulkEvidenceUploadResponseDto` — spec lists `totalFiles`, `successCount`, `failureCount`, `items[]` with `fileName`, `status` (`Success` / `Failed`), `evidenceId`, `errorMessage`. WebApp model is partially loose (`items?: unknown[]`).
- **Single/multipart success:** `{ evidenceId }` inside `ApiResponse<data>` — existing `RegisterOeEvidenceResponseDto` + interceptor unwrap is consistent.

---

## 4. Endpoints present in spec, partial or missing in `JourneyDataService`

| Spec § | Method / path | WebApp today |
| ------ | ------------- | -------------- |
| 5.1 | GET …/evidence (list) | `getObjectiveElementEvidence` |
| 5.2 | POST …/evidence (JSON) | `postEvidenceJson` |
| 5.3 | POST …/evidence (multipart) | `postEvidenceMultipart`, `uploadSingleEvidence` |
| 5.4 | DELETE …/evidence/{id} | `deleteEvidence` |
| 5.5 | POST …/evidence/bulk | `uploadBulkEvidence` |
| 5.6 | GET …/evidence/{id} (detail) | **Not exposed** on service |
| 5.7 | GET …/evidence/{id}/download | **Not exposed** (stream) |
| 5.8 | POST …/evidence/{id}/verify | **Not exposed** |
| 5.9 | POST …/evidence/{id}/versions (multipart `file`, `notes`) | **Not exposed** |

**Implication:** Upload binding work is done; **parity** for detail / download / verify / version requires new typed methods + optional small FormData helper for §5.9.

---

## 5. Auth & headers (§3, §7)

- Bearer + `compliance.api` — existing auth interceptor.
- `X-Correlation-Id` — optional client header for trace alignment (spec §7); optional enhancement.

---

## RESEARCH COMPLETE

Findings support a **two-wave** plan: Wave 1 — documentation, bulk DTO hardening, env/version notes, improved upload error surfacing; Wave 2 — `JourneyDataService` methods + DTOs for §5.6–5.9 (no mandatory UI scope unless product asks).
