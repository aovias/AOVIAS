# CRP Phase 6 — Regression Validation Report

## Automated

- OrganizationService.Tests: LicenseAppService, EntitlementEngine, TrialLifecycle, CommercialAdmin Phase4
- Gateway.UnitTests: TrialAccessMiddleware, LicenseEntitlementMiddleware, Signup trial provisioning

## Manual checklist

- [ ] Signup trial issue → activate → seat → catalogue sync
- [ ] licensing/summary shows plan + entitlements
- [ ] AI POST without ai.write → 403; with entitlement → allowed
- [ ] Expired trial mutations still blocked (Phase 5)
- [ ] Commercial-admin backfill completes
- [ ] Convert trial issues Subscription LicenseKey

## Result

- OrganizationService + Gateway builds: success (alt output `_build_verify_crp_p6`)
- OrganizationService.Tests (License/Entitlement/TrialLifecycle/CommercialAdmin): **20 passed**
- Gateway.UnitTests (LicenseEntitlement/TrialAccess/Signup): **8 passed**

