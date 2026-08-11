# 30-02 Summary: Redesign Sidebar Card + Wire Framework State

## Status: Complete

## Changes Made

### `src/app/core/layout/sidebar.component.ts`
- **Template**: Replaced `.hospital-card` with `.context-panel` featuring hospital row (circular icon, name uppercase bold, org, action icon) + framework row ("Selected Framework" label, shield badge, name, org) + `.context-separator`
- **Styles**: Replaced all `.hospital-card*` styles with `.context-panel`, `.context-hospital*`, `.context-framework*`, `.context-separator`, `.context-action` styles; icon is circular (`border-radius: 50%`); card is borderless
- **Logic**: Added `selectedFrameworkName` signal, `CommonLookupService` injection, `loadFrameworksCache()`, `subscribeToFrameworkChanges()`, `updateFrameworkDisplay()`; updated `onChangeHospital()` to pass frameworks and handle `HospitalFrameworkSelection` result via `switchContext()`

### `src/app/core/services/app-context.service.ts`
- Added `switchContext(hospitalId, frameworkId)` method that updates both hospital and framework in `ComplianceContextService` atomically

### `src/styles.css`
- Made `--hospital-card-bg` and `--hospital-card-border` transparent in both themes
- Added new CSS variables: `--context-framework-badge-bg`, `--context-framework-badge-color`, `--context-framework-value-bg`, `--context-framework-value-border`, `--context-separator-color` for both light and dark themes

## Commits
- `feat(30-02): redesign sidebar context panel with hospital + framework` — 536159e
- `feat(30-02): add switchContext method for dual hospital+framework switching` — 1854c7b
- `feat(30-02): update CSS variables for borderless card and framework badge theming` — 4febb84
