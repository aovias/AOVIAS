# CRP Phase 2 — Regression Validation Report

## Automated

| Suite | Filter | Result |
|-------|--------|--------|
| OrganizationService.Tests | `LicenseAppServiceTests` | **9 passed** (includes duplicate-trial, get-or-issue, seat idempotency) |
| Gateway.UnitTests | `SignupCommandHandlerTrialProvisioningTests` | **3 passed** (happy path order, issue failure rollback, AutoIssue skip) |

Built with alternate output `_build_verify_crp_p2` to avoid locks from running Gateway/OrganizationService processes.

## Manual / regression checklist

- [ ] Async email mode: signup proceeds; verify email still sent by Identity  
- [ ] JWT returned; WebApp stores tokens and navigates `/onboarding`  
- [ ] Welcome step shows trial days remaining when snapshot present  
- [ ] Wizard still completes and sets `Hospital.IsOnboarded`  
- [ ] Second get-or-issue for same org reuses key (no second Trial)  
- [ ] Org name conflict remains 409  
- [ ] Welcome email failure does not undo org/license  

## DoD mapping

| Criterion | Status |
|-----------|--------|
| Org + hospital + admin + membership | Existing + preserved |
| Trial LicenseKey 30d activated | Implemented |
| Admin seat | Implemented |
| Defaults ensured | Implemented |
| Welcome email attempted | Implemented |
| Redirect onboarding | WebApp updated |
| Idempotent / recoverable | Guards + rollback + tests |
| No billing/SSO | Out of scope respected |
