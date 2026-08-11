# Phase 55 — Wave 1 summary

## Delivered

- **`docs/backend/gateway-evidence-api-reference.md`** — canonical pointer to `ComplaincePlatform/docs/backend/gateway-evidence-api-specification.md` (v1.3), multipart §5.3/5.5 responsibilities.
- **`docs/backend/journey-screen-api-spec.md`** — references the new doc; `v1` vs `v1.0` / `api-version` caveat under Gateway v1.3 evidence section.
- **`journey-api.model.ts`** — `BulkEvidenceUploadItemDto`, `totalFiles` on `BulkEvidenceUploadResponseDto`, typed `items`.
- **`evidence-http-error.ts`** — `formatEvidenceHttpError()` for ASP.NET validation + Problem Details; debug log when `enableDebugLogging`.
- **`oe-execution-wizard`** + **`journey-workspace.store`** (`uploadEvidenceFile`) — use shared formatter for upload errors.

## Verification

- `npx ng test --no-watch --include="**/journey-data.service.spec.ts"` — pass.
- `npx ng test --no-watch --include="**/oe-evidence-upload.payload.spec.ts"` — pass.

## Notes

- FormData keys unchanged (`file`, `evidenceType`, `files`, `sharedMetadata`).
