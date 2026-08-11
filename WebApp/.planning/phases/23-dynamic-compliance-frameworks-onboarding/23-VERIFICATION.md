# Phase 23 verification: Dynamic Compliance Frameworks (Onboarding)

**Status:** passed  
**Date:** 2026-04-03

## Phase goal

Replace static framework selection in the AI onboarding wizard with API-driven data from `GET /frameworks/all-frameworks`, use `platformId` for selection, bind `name` / `authority`, handle loading and errors, and carry selected IDs into onboarding payload models.

## must_haves (codebase check)

| # | Criterion | Evidence |
|---|-----------|----------|
| 1 | Dynamic data — no static framework list in onboarding data service | `OnboardingDataService` has no `getFrameworks()`; frameworks come from `CommonLookupService.loadFrameworks()`. |
| 2 | Identity — selection uses `platformId` | `FrameworkSelectorStepComponent` uses `framework.platformId` for selection, toggle, and emitted `platformIds`. |
| 3 | Payload — IDs modeled on hospital + store | `Hospital.frameworkPlatformIds?`; `OnboardingStore.frameworkPlatformIds` + `buildRequestPayload()` merges into `hospital.frameworkPlatformIds`. |
| 4 | Resilience — loading/error/empty | Component sets `frameworksLoading`, `frameworksError`, retry via `refreshFrameworks()`, empty message when no rows. |
| 5 | UX parity | Existing card grid, checkbox row, dark theme tokens preserved; loading/error blocks styled like hospital profile departments. |

## Build

- `ng build --configuration=development` — OK.

## Gaps / follow-ups

- Backend must expose `GET {apiBaseUrl}/frameworks/all-frameworks` with expected fields; integration testing against a live API is recommended.
- If signup flow later collects frameworks in the UI, merge with `setFrameworkPlatformIds` semantics as needed.
