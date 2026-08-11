# Phase 53 — Wave 1 summary

## Delivered

- `**docs/backend/journey-screen-api-spec.md**` — Gateway v1.3 subsection: bulk route `…/evidence/bulk`, multipart field tables, 10 MB / 15 files, journey lock note.
- `**oe-evidence-upload.constants.ts**` — `EVIDENCE_MAX_FILE_SIZE_BYTES`, `EVIDENCE_BULK_MAX_FILES`, `OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE`, `validateEvidenceFilesForMode`.
- `**oe-evidence-upload.payload.ts**` — `buildSingleEvidenceFormData`, `buildBulkEvidenceFormData` (repeated `files` parts + `sharedMetadata`).
- `**JourneyDataService**` — `uploadSingleEvidence`, `uploadBulkEvidence`; `postEvidenceMultipart` delegates via `buildSingleEvidenceFormData` + `uploadSingleEvidence`.
- **Tests** — `oe-evidence-upload.constants.spec.ts`, `oe-evidence-upload.payload.spec.ts`, `journey-data.service.spec.ts`.

## Verification

- `ng build` — pass.
- `ng test` (includes above specs) — pass.

## Notes

- Bulk multipart field name `**files`** — verify against deployed Gateway OpenAPI if 400/415.