---
phase: 21
plan: 21-01
status: complete
---

# Summary 21-01: Post-login hospital routing and header selector sync

## What changed

- Added typed organization-detail contracts in `OrganizationService` for organization, hospitals, and members.
- Added helper methods in `OrganizationService`:
  - `getHospitals()`
  - `getDefaultHospital()` (prefers first `isOnboarded === true`)
  - `hasAnyOnboardedHospital()`
- Updated login success flow:
  - Loads organization details
  - Selects default hospital id into `ComplianceContextService`
  - Routes to `/app/dashboard` when any hospital is onboarded
  - Routes to `/onboarding` when no hospitals are onboarded
- Updated header hospital selector:
  - Removed hardcoded hospitals
  - Binds options from organization-detail hospitals
  - Selects onboarded hospital by default (fallback first)
  - Pushes selected hospital id into `ComplianceContextService`
  - Handles empty hospitals by disabling selector and showing placeholder

## Files modified

- `src/app/core/services/organization.service.ts`
- `src/app/login/login.component.ts`
- `src/app/core/layout/header.component.ts`

## Verification notes

- Type checks and lints pass for modified files.
- Routing decision is deterministic and based on `hospitals[].isOnboarded`.
- Header list now reflects backend hospital array instead of static entries.

