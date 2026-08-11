# Phase 23 — Research: Dynamic Compliance Frameworks (Onboarding)

## Goal

Enable planning and implementation of API-driven framework selection in the onboarding wizard without hardcoded framework rows.

## API contract (from requirements)

| Field | Role |
|--------|------|
| `platformId` | Unique identifier — store in selection state and submit in payload |
| `name` | Display title — bind to `<h3>` |
| `authority` | Secondary line — bind to `<p>` (replaces static `description`) |

**Endpoint:** `GET /frameworks/all-frameworks`  
Resolved URL with current app config: `{apiBaseUrl}/frameworks/all-frameworks` (e.g. `https://localhost:7216/api/v1/frameworks/all-frameworks`).

## Existing codebase anchors

- **Static source:** `OnboardingDataService.getFrameworks()` returns hardcoded `ComplianceFramework[]` with `id`, `name`, `description`, `icon`, `recommended`.
- **UI:** `FrameworkSelectorStepComponent` loads frameworks in `ngOnInit` from `OnboardingDataService`, uses `framework.id` for selection, `<h3>{{ framework.name }}</h3>`, `<p>{{ framework.description }}</p>`.
- **State:** `OnboardingState.selectedFrameworks: string[]` — today holds slug IDs (`nabh`, etc.). **Backward compatibility:** keep the field name; change stored values to `platformId` strings from the API (callers must stop assuming slug keys).
- **Completion summary:** `AiSetupAssistantComponent.getCompletionSummary()` maps slugs to display names via a hardcoded `frameworkNames` map — **must** derive labels from loaded framework rows (or API `name`) instead.
- **Pattern to mirror:** `CommonLookupService.loadDepartments()` — cached list, in-flight de-dupe, `timeout`, envelope/array normalization, `finalize`, retry-friendly error surface for the component.
- **Signup flow (`SignupComponent` + `OnboardingStore`):** does not include framework selection today. Payload extension (`OnboardingRequest` / `Hospital`) should be added only where backend expects it; confirm field name (e.g. `frameworkPlatformIds` on `Hospital`) with API contract.

## Risks / open questions

1. **Response shape:** Backend may use PascalCase or wrap in `{ data: [...] }`. Follow the same resilience pattern as departments (interceptor unwrap + fallback).
2. **Optional UI fields:** Current cards use `icon` and `recommended`. If API omits them, hide “recommended” badge or use a neutral placeholder icon (no hardcoded framework-specific emoji list).
3. **Input mutation:** `FrameworkSelectorStepComponent` mutates `@Input() selectedFrameworks` in place — acceptable for current parent binding; do not change behavior unless fixing selection bugs.

## RESEARCH COMPLETE
