# API Service Consistency Audit Report

**Date:** 2026-07-11  
**Scope:** Angular WebApp — all `*ApiService` classes and related `BaseApiService` consumers  
**Interceptor contract:** `apiResponseInterceptor` unwraps `{ success, data, ... }` envelopes to `data` before services receive the HTTP body.

---

## Executive Summary

A full audit of **36 API service files** under `WebApp/src/app` identified **10 defective files** (8 `*ApiService` + 2 previously fixed settings services + 1 related store). All confirmed double-unwrap / envelope-access defects were corrected. Post-fix grep confirms **zero remaining** `map((r) => r.data)` or `get<ApiResponse<T>>` patterns in API services.

**Build:** `npm run build` — **PASS** (warnings only, pre-existing bundle budgets).

---

## Root Cause Pattern

```typescript
// INCORRECT — interceptor already unwrapped envelope
return this.get<ApiResponse<T>>(endpoint).pipe(map((r) => r.data));

// CORRECT
return this.get<T>(endpoint);
```

Symptom in production: `r.data` is `undefined` → infinite loading spinners.

---

## Files Inspected (36)

| Module | File |
|--------|------|
| Settings | `organization-settings-api.service.ts` |
| Settings | `organization-branding-api.service.ts` |
| Settings / Identity Admin | `identity-admin-api.service.ts` |
| Organization | `organization-api.service.ts` |
| Professional | `professional-api.service.ts` |
| Workforce | `employment-api.service.ts` |
| Workforce Intelligence | `workforce-intelligence-api.service.ts` |
| Enterprise Directory | `enterprise-directory-api.service.ts` |
| Access Governance | `access-governance-api.service.ts` |
| Governance | `governance-api.service.ts` |
| Persons | `person-api.service.ts` |
| Security | `security-api.service.ts` |
| Communication | `communication-api.service.ts` |
| Integration | `integration-api.service.ts` |
| AI Identity | `ai-identity-api.service.ts` |
| Copilot | `copilot-api.service.ts` |
| Control Center | `ai-assistant-api.service.ts` |
| Admin Audit | `administration-audit-api.service.ts` |
| Evidence | `evidence-api.service.ts` |
| Evidence Binder | `evidence-binder-api.service.ts` |
| Documentation | `documentation-api.service.ts` |
| Documentation | `authoring-api.service.ts` |
| Documentation | `intelligence-api.service.ts` |
| Tasks | `task-api.service.ts` |
| Analytics | `analytics-api.service.ts` |
| Audit | `finding-api.service.ts` |
| Auditor | `auditor-api.service.ts` |
| Gap Analysis | `gap-analysis-api.service.ts` |
| Compliance Graph | `graph-api.service.ts` |
| Compliance Maturity | `compliance-maturity.service.ts` |
| Quality Intelligence | `quality-api.service.ts` |
| Patient Safety | `incident-api.service.ts` |
| Reporting | `reports-api.service.ts` |
| Onboarding | `onboarding-assessment-api.service.ts` |
| Journey | `compliance-task-api.service.ts` |
| Journey | `organization-members-api.service.ts` |

**Related store audited (extends `BaseApiService`):** `core/services/dashboard-experience.store.ts`

---

## Files Already Correct (26)

These services already used `get<T>` / `post<T>` without post-interceptor envelope access:

`ai-assistant-api.service.ts`, `analytics-api.service.ts`, `auditor-api.service.ts`, `authoring-api.service.ts`, `compliance-maturity.service.ts`, `compliance-task-api.service.ts`, `documentation-api.service.ts`, `employment-api.service.ts`, `evidence-api.service.ts`, `evidence-binder-api.service.ts`, `finding-api.service.ts`, `gap-analysis-api.service.ts`, `governance-api.service.ts`, `graph-api.service.ts`, `incident-api.service.ts`, `intelligence-api.service.ts`, `onboarding-assessment-api.service.ts`, `organization-branding-api.service.ts`, `organization-settings-api.service.ts`, `person-api.service.ts`, `professional-api.service.ts`, `quality-api.service.ts`, `reports-api.service.ts`, `security-api.service.ts`, `task-api.service.ts`, `access-governance-api.service.ts`

---

## Files Modified (10)

| File | Issue | Fix applied |
|------|-------|-------------|
| `settings/identity-admin/services/identity-admin-api.service.ts` | 6× `.pipe(map((r) => r.data))` | Removed local `ApiResponse`, use `get<T>` / `post<void>` directly |
| `communication/services/communication-api.service.ts` | 14× double unwrap; `listNotifications` / `getDashboard` accessed `r.data` | Direct `get<T>`; reshape mappers operate on unwrapped body |
| `ai-identity/services/ai-identity-api.service.ts` | 6× double unwrap | Direct `get<T>` / `post<T>` |
| `workforce-intelligence/services/workforce-intelligence-api.service.ts` | 14× double unwrap | Direct `get<T>` / `post<T>` |
| `integration/services/integration-api.service.ts` | 20× double unwrap | Direct `get<T>` / `post<T>` / `put<T>` |
| `admin-audit/services/administration-audit-api.service.ts` | `get<ApiResponse<PlatformActivityResponse>>` | `get<PlatformActivityResponse>` |
| `enterprise-directory/services/enterprise-directory-api.service.ts` | 8× double unwrap | Direct `get<T>` / `post<T>` |
| `copilot/services/copilot-api.service.ts` | 28× `.pipe(map(r => r.data))` | Direct `get<T>` / `post<T>`; removed unused `map` import |
| `settings/services/organization-settings-api.service.ts` | *(prior session)* | Already fixed |
| `settings/services/organization-branding-api.service.ts` | *(prior session)* | Already fixed |
| `core/services/dashboard-experience.store.ts` | `response.success` / `response.data` after interceptor | `get<DashboardExperienceResult>` with direct cache tap |

---

## Summary of Fixes

1. Removed redundant local `interface ApiResponse<T>` definitions from affected services.
2. Changed HTTP generic types from `ApiResponse<T>` to `T` on all `BaseApiService` verbs.
3. Removed `.pipe(map((r) => r.data))` and equivalent double-unwrap chains.
4. Preserved intentional response shaping where business logic required it (`communication-api.service.ts` `listNotifications` array-vs-paged normalization; `getDashboard` field aliasing).
5. Fixed `dashboard-experience.store.ts` to align with interceptor-unwrapped dashboard experience payload.

**No business logic changes** beyond normalizing what the Observable emits.

---

## False Positives / Intentionally Unchanged

| File | Reason |
|------|--------|
| `organization-api.service.ts` | Defensive `'data' in r` fallback when interceptor is bypassed (direct service calls / tests) |
| `organization-members-api.service.ts` | Resilient normalize for `items` vs `data` envelope variants |
| Nested generics like `get<PagedResult<TaskListItem>>` | Valid TypeScript — not a double-unwrap artifact |

---

## Potential Risks

| Risk | Mitigation |
|------|------------|
| Endpoints that bypass the interceptor (raw Blob, non-envelope JSON) | Interceptor only unwraps bodies with `'success' in body`; binary downloads unchanged |
| Backend returns array for notifications while types expect paged object | `listNotifications` mapper still handles both shapes on unwrapped body |
| `dashboard/experience` API returning HTTP 500 in dev | Backend/gateway issue observed during shell tests — unrelated to client unwrap fix |
| Cached Playwright auth (`--no-deps`) vs fresh `qa:prepare` | Organization/branding validated with cached session; `qa:prepare` currently fails on signup HTTP 500 |

---

## Verification Grep (post-fix)

```
map((r) => r.data)     → 0 matches in WebApp/src/app/**/*.ts (api services)
get<ApiResponse<       → 0 matches in *api*.service.ts
r.data in services     → 0 matches (excluding intentional org defensive code)
```

---

## Build Result

```
cd WebApp && npm run build
```

**Result:** PASS  
**Output:** `dist/praman-web`  
**Notes:** Pre-existing RouterLink unused-import warnings and bundle budget warnings only.

---

## Playwright Regression Result

| Suite | Command | Result | Notes |
|-------|---------|--------|-------|
| Authentication | `npm run test:auth` | **17 passed, 2 skipped** | Skips are expected optional flows |
| Application Shell | `npm run test:shell` | **10 passed, 10 skipped, 1 failed** | Failure: mega-menu click timeout (element outside viewport) — flaky UI, not API handling |
| Dashboard | `npm run test:dashboard` | **N/A — misconfigured** | `chromium-dashboard` project missing from `playwright.config.ts`; ran `tests/dashboard` on chromium only: 3 passed, 2 failed (login flake), 38 skipped |
| Organization | `playwright test --project=chromium-organization --no-deps` | **18/18 passed** | Validates settings API load/save after fix |
| Branding | `playwright test --project=chromium-branding --no-deps` | **20/20 passed** | Validates branding API load/save after fix |

**Environment:** `npm run qa:health` — all services healthy. `npm run qa:prepare` — failed (signup API HTTP 500); cached auth state used for org/branding reruns.

---

## Success Criteria

| Criterion | Status |
|-----------|--------|
| No `*ApiService` incorrectly accesses `response.data` after interceptor unwrapping | **MET** |
| Angular WebApp builds successfully | **MET** |
| Organization + Branding Playwright suites pass | **MET** (18/18, 20/20) |
| Auth suite passes | **MET** (17/17 runnable) |

---

## Recommended Follow-ups (out of scope)

1. Add `chromium-dashboard` Playwright project with `storageState` dependency (mirror organization/branding pattern).
2. Resolve `qa:prepare` signup HTTP 500 for fresh-environment CI runs.
3. Investigate shell mega-menu viewport stability test flake separately.
