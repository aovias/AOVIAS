# Gateway — Evidence API (canonical reference)

**Purpose:** Point the WebApp to the **authoritative** Gateway evidence specification. The Journey UI spec (`journey-screen-api-spec.md`) summarizes journey routes; **multipart field names and bulk response shapes** must stay aligned with this document.

| Item | Value |
| ---- | ----- |
| **Canonical path (repo sibling)** | `ComplaincePlatform/docs/backend/gateway-evidence-api-specification.md` |
| **Document version** | **1.4** |
| **Base path (Gateway)** | `/api/v{version}/journeys` — see §3 in that doc for `v1` vs `v1.0` / `api-version` |

## Multipart (must match builders in code)

- **Single upload** (`POST …/objective-elements/{oeId}/evidence`, `multipart/form-data`): **`file`** (required), **`evidenceType`** (required), optional **`name`**, **`title`**, **`metadata`** (JSON string). Backend: **`name` takes precedence over `title`** when both are sent. See spec §5.3.
- **Bulk upload** (`POST …/evidence/bulk`): **`files`** (one part per file), **`evidenceType`** (required), optional **`sharedMetadata`**. See spec §5.5.
- **New version** (`POST …/evidence/{evidenceId}/versions`): **`file`** (required), optional **`notes`**. See spec §5.9.

Implementation: `src/app/features/journey/utils/oe-evidence-upload.payload.ts`.

**CORS / preflight:** Gateway uses `AllowAnyHeader()` so `Content-Type: multipart/form-data; boundary=…` is allowed with auth and correlation headers (spec Phase 05). If a host narrows CORS, `Content-Type` must be explicitly allowed or uploads fail.

## Related

- `docs/backend/journey-screen-api-spec.md` — §3.2 Write / Gateway evidence table (cross-linked).
