# 29-03 Summary: Hospital Selector Modal Dialog

## Status: Complete

## What Changed
- Created `HospitalSelectorDialogComponent` in `src/app/shared/ui/hospital-selector/`
- Exported `HospitalSelectorDialogData` and `HospitalSelectorItem` interfaces
- Dialog template includes:
  - Header with title "Select Hospital" and subtitle "Choose a hospital to continue"
  - Scrollable list of hospitals as radio-button-style items
  - Each item shows: custom radio indicator, hospital icon, name (bold), organization (light), checkmark when selected
  - Non-onboarded hospitals shown as disabled with "Not Onboarded" badge
  - Bottom actions: "Cancel" (secondary) and "Confirm Selection" (primary, disabled until new selection)
- Custom radio button with scale animation on selection
- Hover effects on hospital items (background + border glow)
- Selected item has primary color border/glow and background highlight
- Confirm button disabled when: no selection, or selection unchanged from current
- Global dialog styles in `styles.css`: `.hospital-selector-panel` (no padding, border-radius) and `.hospital-selector-backdrop` (dark overlay + blur)
- Responsive: min-width unset on mobile (≤640px)
- Uses Angular Material `MatDialogModule` and CDK `A11yModule` for focus management

## Files Modified
- `src/app/shared/ui/hospital-selector/hospital-selector-dialog.component.ts` (new)
- `src/styles.css`

## Commit
- `fa36d6f` — feat(29-03): create hospital selector modal dialog
