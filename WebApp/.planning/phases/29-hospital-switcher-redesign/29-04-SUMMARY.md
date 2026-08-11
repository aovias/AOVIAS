# 29-04 Summary: State Integration, Dialog Wiring & Data Refresh

## Status: Complete

## What Changed

### Sidebar Component — Dialog Wiring
- `onChangeHospital()` opens `HospitalSelectorDialogComponent` via `MatDialog.open()` with:
  - Hospital list mapped from `OrganizationService.getHospitals()`
  - Current `selectedHospitalId` from `ComplianceContextService.getHospitalId()`
  - Panel class, backdrop class, width/maxWidth/maxHeight configuration
- `afterClosed()` result filtered for truthy (user confirmed) and triggers `AppContextService.switchHospital()` + local display update
- Uses `takeUntilDestroyed(destroyRef)` for automatic subscription cleanup

### Sidebar Component — Context Reactivity
- `initializeHospitalCard()` subscribes to `complianceContext.hospitalId$` to stay in sync across navigation
- Handles initial load: uses cached organization if available, otherwise loads from API
- `applyInitialHospital()` sets default hospital if none selected
- `updateHospitalDisplay()` looks up hospital name from org service on any change
- `isSwitching` signal bound to `appContext.switching$` for loading state

### AppContextService — Central Orchestrator
- Enhanced `switchHospital(hospitalId)` to:
  1. Set `switching$` to true
  2. Update `complianceContext.setHospitalId()`
  3. Call `loadFrameworkForHospital()` (clears framework for reload)
  4. Emit `hospitalSwitched$` with new hospital ID
  5. Set `switching$` to false
- Added `hospitalSwitched$` BehaviorSubject for downstream subscribers
- Added `switching$` BehaviorSubject for UI loading states
- Uses `LoggerService` for structured logging

### ComplianceContextService
- Updated `setHospitalId`, `setFrameworkId`, `setOrganizationId` to accept `string | null` (was `string` only)
- Enables framework clearing during hospital switch

## Files Modified
- `src/app/core/layout/sidebar.component.ts`
- `src/app/core/services/app-context.service.ts`
- `src/app/core/services/compliance-context.service.ts`

## Commit
- `58a4fa4` — feat(29-04): wire state integration and hospital switch orchestration
