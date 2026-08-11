# 29-01 Summary: Remove Header Hospital & Framework Selectors

## Status: Complete

## What Changed
- Removed the `<div class="hierarchy-selectors">` block from the header template (hospital selector dropdown, framework selector dropdown, divider)
- Removed all associated CSS styles: `.hierarchy-selectors`, `.divider`, `.selector-wrapper`, `.selector-button`, `.selector-icon`, `.selector-content`, `.selector-label`, `.selector-value`, `.selector-dropdown`, `.dropdown-option`, `.option-content`, `.option-org`, `.option-name`, and related hover/active/disabled states
- Kept `@keyframes slideDown` (used by notification and user dropdowns)
- Removed `Hospital` interface, `hospitalMenuOpen`, `frameworkMenuOpen`, `selectedHospital`, `selectedOrganization`, `selectedFramework`, `hospitals`, `frameworks` signals/properties
- Removed `toggleHospitalMenu()`, `toggleFrameworkMenu()`, `selectHospital()`, `selectFramework()`, `ngOnInit()`, `initializeHospitals()`, `applyOrganizationHospitals()` methods
- Removed `OrganizationService` and `ComplianceContextService` injections and `OnInit` implementation
- Removed `@media (max-width: 1024px)` rule for `.hierarchy-selectors` (no longer needed)
- Header visual balance maintained: logo on left, controls on right

## Files Modified
- `src/app/core/layout/header.component.ts`

## Commit
- `c7e587f` — feat(29-01): remove header hospital and framework selectors
