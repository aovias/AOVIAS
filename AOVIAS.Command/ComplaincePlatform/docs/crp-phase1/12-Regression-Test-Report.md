# CRP Phase 1 — Regression Test Report

## Automated

| Suite | Result |
|-------|--------|
| `EmailVerificationModeResolverTests` (7) | Passed |
| Existing IdentityService.Tests (broader) | Run in CI after process unlock; no intentional breaking API removals |

## Manual / API checklist

| Scenario | Expected |
|----------|----------|
| Identity self-register | 200; verification email when mode ≠ None |
| Resend verification | Success message; cooldown enforced |
| Verify valid token | EmailConfirmed; audit EmailVerified |
| Verify expired/used | Clear error; audit failure |
| Login Async unconfirmed | Allowed |
| Login RequiredBeforeLogin unconfirmed | Forbidden |
| Org signup Async | Org+hospital+admin+JWT; onboarding continues |
| Org signup RequiredBeforeOrganizationProvisioning anonymous | 403 |
| Password reset | HTML email; single-use token |
| Logout / logout-all / revoke session | Sessions cleared |
| `/signup` → `/onboarding` | Unchanged product path |

## Out of scope regressions (not Phase 1)

Trial license auto-issue, commercial provision — deferred.
