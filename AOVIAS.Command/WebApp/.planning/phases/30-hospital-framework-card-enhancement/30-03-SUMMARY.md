# 30-03 Execution Summary

## Plan: Click Target, Journey-Based Frameworks, Session Persistence & Auto-Popup

### Tasks Completed

| Task | Description | Status |
|------|------------|--------|
| 01 | Add `HospitalJourney` interface + `journey` field to `OrganizationHospital` + `getHospitalJourneys()` helper | ✅ |
| 02 | Update dialog: `hospitalJourneys: Record<string, FrameworkSelectorItem[]>`, `computed availableFrameworks`, auto-clear framework on hospital switch, `code` field on `FrameworkSelectorItem` | ✅ |
| 03 | Move `(click)` from `.context-panel` to `.context-action` only, remove panel hover/cursor | ✅ |
| 04 | Add `selectedFrameworkCode` signal, display `frameworkName` (top) + `frameworkCode` (bottom) | ✅ |
| 05 | Remove `CommonLookupService` dependency, build `hospitalJourneys` map from org data, search journeys for display | ✅ |
| 06 | `AppContextService`: persist to `sessionStorage` on `switchContext()`/`switchHospital()`, add `getPersistedHospitalId()`/`getPersistedFrameworkId()`/`clearPersistedSelection()` | ✅ |
| 07 | `AuthService.logout()`: call `appContextService.clearPersistedSelection()`, fix circular dep (replace `AuthService` with `SessionService` in `AppContextService`) | ✅ |
| 08 | Auto-popup on first login: `applyInitialContext()` checks persisted values, fires `onChangeHospital()` with 400ms delay if missing | ✅ |

### Files Modified
- `src/app/core/services/organization.service.ts` — added `HospitalJourney` interface, `journey` field, `getHospitalJourneys()` helper
- `src/app/shared/ui/hospital-selector/hospital-selector-dialog.component.ts` — `hospitalJourneys` Record, `computed availableFrameworks`, `code` field, framework reset on hospital switch, empty-state message
- `src/app/core/layout/sidebar.component.ts` — click-target fix, `selectedFrameworkCode` signal, journey-based framework display, session restore, auto-popup, removed `CommonLookupService`
- `src/app/core/services/app-context.service.ts` — `sessionStorage` persistence, `getPersistedHospitalId/Framework`, `clearPersistedSelection`, replaced `AuthService` → `SessionService`
- `src/app/core/auth/auth.service.ts` — inject `AppContextService`, call `clearPersistedSelection()` on logout

### Commits
1. `feat(30-03): add HospitalJourney interface to OrganizationHospital`
2. `feat(30-03): update dialog for hospital-dependent journey frameworks`
3. `feat(30-03): click-target fix, journey frameworks, frameworkCode display in sidebar`
4. `feat(30-03): add sessionStorage persistence for hospital/framework selection`
5. `feat(30-03): clear persisted selection on logout, fix circular dependency`

### Deviations from Plan
- **Circular dependency fix**: `AppContextService` previously imported `AuthService` (which now imports `AppContextService`). Resolved by replacing `AuthService` with `SessionService` in `AppContextService.initializeFromSession()` — functionally equivalent since `isAuthenticated()` delegates to `sessionService.isSessionValid()`.
- **`HospitalJourney` import removed from sidebar**: The type was imported but not used as a direct type annotation (only accessed via property chaining), so it was removed to keep imports clean.
