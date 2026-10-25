---
phase: 20
plan: 20-01
status: complete
---

# Summary 20-01: Onboarding departments from API (no hardcoded values)

## What changed

- Added shared typed lookup model `DepartmentLookup` and new cached `CommonLookupService` for `GET /common/departments`
- Removed all hardcoded onboarding departments (deleted `OnboardingDataService.getDepartments()`)
- Migrated onboarding hospital profile state from `departments: string[]` to `departmentIds: string[]`
- Updated Hospital Profile step UI to:
  - Fetch departments on init via service (no direct HttpClient)
  - Render checkbox-card grid with `name` display and `id` value
  - Support loading, error + retry, empty list states
  - Protect data integrity: blocks Continue if saved ids aren’t in the current lookup list, with a “Clear unavailable selections” action

## Files modified

- `src/app/shared/models/lookup.models.ts`
- `src/app/shared/services/common-lookup.service.ts`
- `src/app/features/onboarding/services/onboarding-data.service.ts`
- `src/app/features/onboarding/models/onboarding-state.model.ts`
- `src/app/features/onboarding/pages/ai-setup-assistant.component.ts`
- `src/app/features/onboarding/components/hospital-profile-step.component.ts`

## Commits

- `e71c330` — feat(20-01): add cached departments lookup service
- `2ef3f2e` — refactor(20-01): remove hardcoded onboarding departments
- `1229162` — refactor(20-01): store departmentIds in onboarding state
- `a2f9036` — feat(20-01): load departments from API in hospital profile step
- `b5d2c90` — fix(20-01): show departments API errors and block invalid selections

## Verification notes

- Lints: no new linter issues in modified files
- Static department list no longer referenced in onboarding feature (verified by grep)
- API base URL includes `/api/v1`, so `'/common/departments'` resolves to `GET /api/v1/common/departments`

