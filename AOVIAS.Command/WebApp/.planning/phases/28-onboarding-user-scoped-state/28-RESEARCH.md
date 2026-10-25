# Phase 28 — Research: User-scoped onboarding state (no cross-user leakage)

## RESEARCH COMPLETE

### Root cause

| Store / storage | Key / mechanism | User binding |
|-----------------|-----------------|--------------|
| **`AiOnboardingWizardStore`** | `sessionStorage` key **`ai_onboarding_wizard_v1`** (fixed) | **None** — any logged-in user loads the same blob. |
| **`OnboardingStore`** (signup wizard) | **`onboarding_state`** (fixed) | **None** — same issue for signup flow draft. |
| **Auth lifecycle** | `AuthService.logout()` clears tokens, org, compliance, lookups | **Does not** clear onboarding stores or wizard session keys. |
| **Login** | Tokens + `userId` saved in `SessionService` | **No** comparison with prior user before routing to `/onboarding`. |

### Risk

- User B logs in on a shared browser after User A: wizard can show A’s draft or allow confusion with pre-filled data.
- Submit payloads use **current** session `organizationId` from `SessionService` while **stale** wizard rows might still be edited — integrity risk unless backend enforces ownership (recommended as defense in depth).

### Design direction

1. **Primary:** **Namespace** `sessionStorage` keys by **`SessionService.getUserId()`** (e.g. `ai_onboarding_wizard_v1:<userId>`), or store a **wrapper** `{ ownerUserId, payload }` with validation on read.
2. **Mandatory:** On **logout**, remove all onboarding-related keys and reset in-memory signals (`clearWizardSession`, `OnboardingStore.reset()`).
3. **Login / token refresh:** If **previous** `userId` (before update) **≠** new `userId`, purge other users’ namespaced keys or call centralized **clear** — namespacing alone avoids reading wrong data; **clear on logout** removes residual keys for the user who left (optional: sweep `sessionStorage` keys matching prefix on logout).
4. **Missing `userId`:** Do not hydrate wizard from storage; start **default** state (already unauthenticated flows may not have id until after login — align signup vs AI onboarding routes).
5. **Multi-tab:** `window.storage` event on `sessionStorage` can notify other tabs when logout clears keys; optional follow-up for strict consistency.

### must_haves for PLAN

- User-scoped keys or embedded owner + validation.
- Logout + user-change hooks clear or invalidate onboarding state.
- Wizard init ignores stale / wrong-owner data safely.
- Document backend ownership validation as recommended.
