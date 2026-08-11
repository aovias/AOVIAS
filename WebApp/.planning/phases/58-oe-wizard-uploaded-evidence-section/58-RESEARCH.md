# Phase 58 — Research: Uploaded Evidence section (OE wizard Evidence step)

## Reference UI (stakeholder)

Saved asset (workspace): `assets/c__Users_HP_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-24aaae6e-833d-4bf4-974c-3b0619baa749.png`

**Structure (top → bottom):**

1. **Card** — dark surface, subtle border, rounded corners.
2. **Header** — small upload icon + title **Evidence required**, then muted body copy (KB `evidenceRequired` paragraphs).
3. **Uploaded Evidence** — inner region with its own border/title **Uploaded Evidence**; vertical list of rows separated by hairline dividers.
4. **Row** — left: file-type affordance (PDF / spreadsheet / Word styling); stacked **filename** (strong) + **timestamp** (muted, e.g. “Today at 10:23 a.m.”); middle/right: **size + type** (e.g. `1.2 MB PDF`); far right: **Download** control (icon button).
5. **Drop zone** — dashed border, large upload icon, **Drop files or click to browse** (existing wizard copy when upload flag is on).

**Note:** Exact **byte size** may not be available from current list APIs — see Data gaps.

## Current implementation

- Template: `oe-execution-wizard.component.html` — Evidence step (`@case (2)`) uses a single `article.oe-wizard__card` with header, KB text, then **drop zone first**, then pending `File[]` list with remove — order **does not** match the reference.
- Upload + refresh: `triggerUpload()` → `JourneyDataService.uploadSingleEvidence` / `uploadBulkEvidence` → `store.refreshSelectedObjectiveElementFromApi()` (Phase 54.2).
- Domain model: `ObjectiveElement.evidence: OeEvidenceItem[]` — `id`, `name`, `verificationStatus`, `uploadedAt`, `uploadedBy`, `expiryAt` (`objective-element.model.ts`).
- List GET exists: `JourneyDataService.getObjectiveElementEvidence(journeyId, oeId)` maps to same `OeEvidenceItem` shape; OE detail PATCH/load also hydrates `evidence[]`.

## APIs already in WebApp

- **Download:** `downloadObjectiveElementEvidenceFile(journeyId, objectiveElementId, evidenceId): Observable<Blob>` — Gateway §5.7 (`journey-data.service.ts`). No UI caller yet; `apiResponse.interceptor` passes `Blob` through.
- **Delete** (optional future): `deleteEvidence` — out of scope unless product asks for remove in this step.

## Data gaps vs reference

| Reference element | Source today |
| ----------------- | ------------ |
| Filename | `OeEvidenceItem.name` |
| Relative time | `uploadedAt` — format with `DatePipe` / `Intl` / small helper (match app date style) |
| “PDF / Excel / Word” label | Derive from `name` extension (`.pdf`, `.xlsx`, `.docx`, …) with safe fallback **Document** |
| File size (e.g. `1.2 MB`) | **Not** on `OeEvidenceItem` / `OeEvidenceItemApiDto`. Options: (a) omit column until Gateway adds field; (b) show **em dash** or tooltip “Size unavailable”; (c) **not recommended**: fetch `Blob.size` only after download (misleading in list). **Plan default:** omit size or show type-only on the right until API provides size. |

## UX / policy

- **Read-only** operational mode (`evidenceSection === 'readonly'`): hide or disable **drop zone** and pending selection (existing); **Download** of already-uploaded files should remain useful — keep list visible, disable only upload affordances (align with audit-read-only semantics).
- **Tokens:** All new styles must use CSS variables from `src/styles/tokens/*` per `DESIGN_RULES.md` (no ad-hoc hex for navy/blue/gray — map to existing semantic/surface tokens).

## must_haves for planning

- Reorder Evidence step content to: header + KB → **Uploaded Evidence** list → drop zone → pending files + Upload button.
- Bind list to snapshot OE evidence via `JourneyWorkspaceStore.findOe(selectedOeId)?.oe.evidence`.
- Wire Download using existing service + in-browser save (object URL or project pattern).
- Empty state for Uploaded Evidence when `evidence.length === 0`.

## RESEARCH COMPLETE
