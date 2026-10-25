# Phase 58 — Wave 1 summary

## Delivered

- **Evidence step reorder:** KB copy → **Uploaded Evidence** (server list + empty state) → hidden file input → **drop zone** → bulk label / validation → **Ready to upload** pending list + Upload.
- **`persistedWizardEvidence`:** `findOe(selectedOeId)?.oe.evidence`, sorted by `uploadedAt` descending.
- **Download:** `JourneyDataService.downloadObjectiveElementEvidenceFile` → object URL → anchor download → `revokeObjectURL`; `evidenceDownloadBusyIds` prevents double-clicks; errors via `formatEvidenceHttpError`.
- **Read-only:** Upload/drop zone/pending unchanged; downloads stay enabled for existing rows.
- **Styling:** Token-only `oe-wizard__uploaded-*` + `oe-wizard__pending-heading`; `position: relative` on shared `.oe-wizard__card` for visually hidden file input.
- **Icons:** `lucideDownload`, `lucideFileText`, `lucideFileSpreadsheet`, `lucideFile` with semantic tint modifiers.

## Notes

- List **does not** show file size (API list DTO has no byte size). Type column shows **PDF / Excel / Word / Document** via shared util (Wave 2).

## Verification

- `ng build` (development) succeeded.
