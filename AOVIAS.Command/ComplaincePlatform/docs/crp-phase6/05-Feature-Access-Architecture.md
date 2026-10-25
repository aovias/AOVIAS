# CRP Phase 6 — Feature Access Architecture

Feature request → Gateway middleware stack:

1. TrialAccessMiddleware — status gate (Expired/Revoked/Suspended/Archived → block mutations)
2. LicenseEntitlementMiddleware — capability gate

Mutations matching Licensing:FeatureGates require entitlement (fail-closed). Reads soft (advisory headers). Allowlist: auth, licenses, licensing, commercial-admin, health, subscriptions catalogue.

Default gates: `/api/v1/ai/` → `ai.write`, `/api/v1/frameworks` → `frameworks`.
