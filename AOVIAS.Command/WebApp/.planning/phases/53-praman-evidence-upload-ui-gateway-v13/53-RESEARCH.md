# Phase 53 — Research: Evidence Upload UI (Gateway v1.3)

## RESEARCH COMPLETE

### Authoritative API (this phase)

Use **only** these multipart endpoints (version segment must match deployed Gateway, typically aligned with `environment.apiBaseUrl`):

| Operation | Method | Path suffix (after `/api/v{version}`) |
|-----------|--------|----------------------------------------|
| Single | `POST` | `/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence` |
| Bulk | `POST` | `/journeys/{journeyId}/objective-elements/{objectiveElementId}/evidence/bulk` |

**Single `multipart/form-data` fields:** `file` (required), `evidenceType` (required), `title` (optional), `metadata` (optional — JSON string per multipart convention).

**Bulk `multipart/form-data` fields:** multiple `files` parts (or `files[]` per server contract — **confirm exact part names** against Gateway OpenAPI when wiring), `evidenceType` (required), `sharedMetadata` (optional JSON string).

**Spec rules to enforce client-side:** max **10 MB** per file; max **15** files per bulk request; journey lock may block the **entire** request (surface as future error handling; no POST in this phase).

### Local docs vs Gateway

- `docs/backend/journey-screen-api-spec.md` documents **single** `POST …/evidence` only. **Bulk** (`…/evidence/bulk`) is introduced for **v1.3** per product brief; add a short § to that doc (or a v1.3 delta note) when implementing so frontend/backend stay aligned.

### Codebase touchpoints

| Area | Notes |
|------|--------|
| `oe-execution-wizard.component.html` | Evidence step (`@case (2)`) contains inactive `.oe-wizard__dropzone` (“Drop files here or click to browse”) — **no** `<input type="file">` today. |
| `JourneyDataService` | `postEvidenceMultipart` builds `FormData` with `file`, `evidenceType`, optional `title`; **no** `metadata` yet; **no** bulk. Extend/add methods; keep existing call sites stable. |
| `JourneyWorkspaceStore` | Phase 49 may expose `uploadEvidenceFile`; **do not** wire new UI to store upload in this phase unless plan explicitly keeps store API-compatible (prefer wizard-local state first). |

### UX constraints (locked for Phase 53)

- Clicks on the upload area **must not** open a file picker; show **“Upload will be enabled shortly”** (or snackbar) and **`matTooltip`**: “Upload not enabled yet”.
- **Optional dev-only simulation** (e.g. `ngDevMode` + private test hook or hidden e2e-only control **not shipped to prod**) may call `handleFileSelect`/`handleBulkSelect` with synthetic `File[]` — document in plan if used; default is **no** picker.

### Dependencies

- **Phase 50** wizard layout/step indices — Evidence remains step index **2** (0-based five-step wizard); do not rename steps.
- **DESIGN_RULES.md** / tokens — extend dropzone disabled/hover using existing `--space-*`, `--radius-*`, `--color-*`; no arbitrary hex.
