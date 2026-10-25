---
phase: 23
plan: 23-01
status: complete
---

# Summary 23-01: Dynamic compliance frameworks (onboarding)

## What changed

- Added `FrameworkLookup` (`platformId`, `name`, `authority`, optional `isRecommended`, `displayOrder`) and cached `CommonLookupService.loadFrameworks()` / `refreshFrameworks()` for `GET /frameworks/all-frameworks` with envelope/array normalization (same resilience pattern as departments).
- Removed static `OnboardingDataService.getFrameworks()` and the obsolete `ComplianceFramework` model.
- Refactored `FrameworkSelectorStepComponent` to load frameworks on init, bind `name` → `<h3>`, `authority` → `<p>`, first-letter placeholder for icon, optional recommended badge from API, loading/error/retry/empty states, multi-select by `platformId`. Emits `{ platformIds, displayNames }` on Continue.
- Extended `OnboardingState` with `selectedFrameworkDisplayNames` for completion badges; `AiSetupAssistantComponent` persists platform IDs via `OnboardingStore.setFrameworkPlatformIds`, removes hardcoded slug map, generalizes AI insight copy.
- Added `Hospital.frameworkPlatformIds` and merged `frameworkPlatformIds` into `buildRequestPayload()` from the store when the AI wizard (or future flow) sets them; sessionStorage includes `frameworkPlatformIds`.

## Files modified

- `src/app/shared/models/lookup.models.ts`
- `src/app/shared/services/common-lookup.service.ts`
- `src/app/features/onboarding/models/onboarding-state.model.ts`
- `src/app/features/onboarding/models/hospital.model.ts`
- `src/app/features/onboarding/services/onboarding-data.service.ts`
- `src/app/features/onboarding/components/framework-selector-step.component.ts`
- `src/app/features/onboarding/pages/ai-setup-assistant.component.ts`
- `src/app/features/onboarding/state/onboarding.store.ts`

## Verification notes

- `npx ng build --configuration=development` succeeds.
