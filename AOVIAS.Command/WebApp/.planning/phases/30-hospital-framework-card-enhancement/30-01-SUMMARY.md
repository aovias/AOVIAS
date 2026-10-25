# 30-01 Summary: Enhanced Hospital & Framework Selector Dialog

## Status: Complete

## Changes Made

### `src/app/shared/ui/hospital-selector/hospital-selector-dialog.component.ts`
- **Interfaces**: Added `FrameworkSelectorItem`, `HospitalFrameworkSelection`; extended `HospitalSelectorDialogData` with `frameworks` and `selectedFrameworkId`
- **Template**: Rewrote to dual-section layout with "Hospital" and "Framework" section labels, close (X) button in header, framework radio list, content divider
- **Styles**: Added `.dialog-close`, `.section-label`, `.content-divider`, `.framework-item`, `.framework-list` styles; renamed item classes to shared `.item-radio`, `.item-name`, `.item-sub`, `.item-check`
- **Logic**: Renamed `selectedId` → `selectedHospitalId`; added `selectedFrameworkId` signal; added `selectFramework()`, `onClose()` methods; updated `onConfirm()` to return `HospitalFrameworkSelection`

## Commits
- `feat(30-01): enhanced hospital & framework selector dialog` — 0e972c5
