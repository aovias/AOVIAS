# Phase 26 — Research: AI onboarding wizard state, cross-step flow, final submit

## RESEARCH COMPLETE

### Current implementation (codebase)

| Area | Finding |
|------|---------|
| **Route / shell** | `AiSetupAssistantComponent` hosts **6** interaction steps in the template: **1** Welcome, **2** Hospital Profile, **3** Frameworks, **4** Assessment, **5** Team Invitation, **6** Completion (“Review” UX). Stepper UI maps to the same numbering. |
| **State storage** | `state: OnboardingState` is a **plain object** on the component — **lost on full page refresh**. Only **`OnboardingStore.setFrameworkCodes`** persists framework **codes** to **`sessionStorage`** (`onboarding_state`) for **signup** merge; the rest of the wizard is **not** serialized. |
| **OnboardingStore** | Used for **signup wizard** (`organization`, `hospital`, `adminUser`, `frameworkCodes`). **Not** the single source of truth for **AI setup assistant** today. |
| **Final action** | **`handleComplete()`** sets `completed` and **`Router.navigate`** to **`/control-center/compliance-command-center`** — **no** `POST` to complete onboarding. |
| **OnboardingService** | Exposes **`createHospital` → `POST /onboarding/signup`** only. **No** **`/onboarding/onboard`** in the repo. |
| **Team invitation** | **Department** is a **free-text `<input>`** — **not** restricted to departments chosen in Step 2 (Hospital Profile). **`hasValidMembers()`** checks name/email/role only; **no duplicate-email** check across rows. |
| **Completion step** | Shows **counts** + framework **badges** + score — **does not** list hospital name/geo, full assessment Q&A, or invited members. |
| **Framework IDs** | Product requirement mentions **platformId**; app currently persists **`FrameworkLookup.code`** as **`selectedFrameworks`** / **`frameworkCodes`** — **align payload field names** with backend contract in implementation phase. |

### Gaps vs requested behavior

1. **Single source of truth + persistence** — Needs a dedicated **AI wizard store** (or extended model) + **sessionStorage** (or `sessionStorage` + version key) for refresh/partial completion **without** clobbering signup `onboarding_state` unless intentionally merged.
2. **`POST /onboarding/onboard`** — **Confirm** OpenAPI/backend payload; add method on **`OnboardingService`**; map from **`OnboardingState`** + org/hospital context (**`OrganizationService`** / active hospital).
3. **Departments → Team** — Pass **`departmentIds`** (and resolved **labels** from `CommonLookupService` or cached list) into **`TeamInvitationStepComponent`**; replace free text with **`<select>`** (or searchable select) **scoped to selected IDs**; validate department required when member row is non-empty.
4. **Validation** — Centralize **per-step** rules; block **Next** with visible errors; optional **step completeness** flags for final submit.
5. **Duplicate emails** — Validate uniqueness **case-insensitive** trim in team step before emit.
6. **Submit failure** — Keep wizard state; show **NotificationService** error; **Retry** without clearing form.
7. **Session expiry** — Covered globally by Phase **25**; optional: persist wizard draft to **`sessionStorage`** so post-login return is smoother (stretch).

### Non-goals (unless backend requires)

- Changing **signup** `POST /onboarding/signup` contract.
- Replacing **Signals** with NgRx.

### must_haves for PLAN

- One **canonical** `OnboardingState` (or equivalent) driving all steps.
- **Persist** wizard draft for same browser session (refresh).
- **Team department** dropdown sourced from **Step 1** selections only.
- **`POST /onboarding/onboard`** (or confirmed alias) with typed request model.
- **Review** step reflects all sections accurately.
- **Validation** + **duplicate email** + **submit error** UX as specified.
