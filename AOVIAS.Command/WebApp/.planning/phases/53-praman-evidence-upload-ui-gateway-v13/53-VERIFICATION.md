# Phase 53 — Verification checklist

**Post–53 audit:** See `.planning/phases/54-evidence-upload-ui-verification-refinement/54-VERIFICATION.md` for the checklist compliance report (Phase 54).

## Goal-backward (phase success)

- Gateway v1.3 **single** and **bulk** routes documented and implemented in `JourneyDataService` (methods exist; UI may not call them).
- **FormData** builders match spec field names (`file`, `evidenceType`, `title`, `metadata`; bulk: repeated `files` + `evidenceType`, `sharedMetadata`).
- **10 MB / file** and **15 files** bulk limits enforced in validation helper.
- OE wizard Evidence step: **no** file picker; click shows **“Upload will be enabled shortly”**; tooltip **“Upload not enabled yet”** (readonly: read-only copy).
- **selectedFiles** + **uploadMode** + preview list + bulk label when applicable.
- Hooks exist: `handleFileSelect`, `handleBulkSelect`, `buildSinglePayload`, `buildBulkPayload`, `triggerUpload` — **no** `HttpClient` usage from wizard for evidence POST in this phase.

## Automated / build

- `ng build` clean.
- Unit tests for validation + `FormData` builders + service URL assertions where added.

## Manual smoke

- DevTools: no evidence POST from user flows on Evidence step (**human_needed** — optional manual pass).