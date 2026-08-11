# Onboarding wizard — product rules and cross-layer contract

This document is the **single source of truth** for second-step onboarding (`POST /api/v1/onboarding/onboard`). Backend validation in Gateway must match these rules; the Angular app should mirror them for step guards and UX.

## Gateway endpoints

| Method | Path | Notes |
|--------|------|--------|
| `POST` | `/api/v1/onboarding/onboard` | Second-step wizard (validated + optional idempotency). Success: `ONBOARDING_WIZARD_COMPLETED`; cached replay: `ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY`. |
| `GET` | `/api/v1/onboarding/wizard/assessments/latest?hospitalId=` | Proxies Compliance latest assessment; `hospitalId` must match JWT `hospital_id`. |

## API payload (`OnboardWizardRequestDto`)

| Field | Rule |
|-------|------|
| `organizationId`, `hospitalId` | Required (non-empty GUIDs). Must match JWT `organization_id` / `hospital_id`. |
| `hospitalProfile.departmentIds` | **At least one** GUID (master department). |
| `hospitalProfile.hospitalId` | If set, must equal root `hospitalId`. |
| `frameworkCodes` | **At least one** non-whitespace code. |
| `teamInvites` | **Optional** — may be empty. |
| `teamInvites[]` (when any row exists) | Each row: non-empty `name`, valid `email`, non-empty `role`, `departmentId` required and **must be in** `hospitalProfile.departmentIds`. |
| `wizardVersion` | Carried through to org profile and compliance assessment. |

## Step model (UX)

1. **Hospital profile** — at least one department; block Next/Submit without selection.
2. **Frameworks** — at least one framework; block without selection.
3. **Assessment** — per product (optional empty answers allowed server-side).
4. **Team invites (optional)** — skip entirely allowed. If user adds rows, enforce full row validation; department dropdown **only** departments chosen in step 1.

## Authorization (Gateway)

- `POST /api/v1/onboarding/onboard` uses policy **`ComplianceOrOrganizationScope`**: the token must include **`compliance.api`** and/or **`organization.api`** in the `scope` claim.
- **Downstream:** Compliance HTTP APIs used by the wizard still require **Compliance scope** on the Compliance service. Tokens that only include `organization.api` pass Gateway auth but **Compliance proxy calls will fail** until Identity issues `compliance.api` for that user or Compliance policies are relaxed for specific onboarding routes. **Current login/refresh** in Identity typically issues **both** scopes for hospital users.

## Compliance services (wizard-related)

- **Journey create:** if a journey already exists for the hospital + framework, Compliance **reuses** it (no duplicate journey).
- **Assessment save:** for the same `hospitalId` and `wizardVersion`, Compliance **returns the existing assessment** and does not insert another row (first completion wins).

## Idempotency

- **`Idempotency-Key` header** — **optional**. If omitted, each request is processed normally (no replay cache).
- If provided: Gateway uses **in-memory** cache (`IMemoryCache`), key scoped by user + org + hospital + header value, TTL **24 hours**.
- Replay returns the **same** success body as the first completed run; downstream must not create duplicate assessments/journeys when combined with Compliance idempotent journey behavior.

## Angular checklist (sibling repo / frontend)

- [ ] Step 1: disable Next until `departmentIds.length >= 1` (and other required profile fields per UX).
- [ ] Framework step: disable Next until `frameworkCodes.length >= 1`.
- [ ] Invites: allow zero rows; if rows exist, validate all fields; department `select` options = only Step 1 `departmentIds`.
- [ ] Submit: send `Idempotency-Key` (e.g. UUID) for the wizard session when retry-safety is needed.
- Map `VALIDATION_FAILED` errors to field messages using `errors[]` / `field` from API.

## Cross-layer verification matrix

| Scenario | Expected UI | Expected API |
|----------|-------------|--------------|
| No frameworks selected | Block progression | `VALIDATION_FAILED` before downstream calls |
| No departments in step 1 | Block progression | `VALIDATION_FAILED` |
| Invite missing department | Inline error | `VALIDATION_FAILED` |
| Invite department not in step 1 | Inline error | `VALIDATION_FAILED` |
| Empty invites | Allow submit | Success (no invite calls or empty list) |
| Duplicate submit (same `Idempotency-Key`) | Optional “already submitted” UX | Same JSON body as first success, no duplicate rows |
| Journey already exists (Compliance) | N/A | Idempotent success path |

## Structured logging (Gateway)

Handler logs use event names `OnboardingWizard:Start`, `OnboardingWizard:Profile`, `OnboardingWizard:Assessment`, `OnboardingWizard:Frameworks`, `OnboardingWizard:Invites` with `organization` and `hospital` (and optional assessment/framework details).
