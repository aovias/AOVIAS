# CRP Phase 6 — Entitlement Architecture

EntitlementEngine parses EntitlementsJson:

- Legacy: `["code"]` → Boolean
- Structured: `{ code, kind, value, expiresAtUtc?, custom? }`
- Kinds: Boolean, Numeric, Usage, TimeBased, Custom

APIs: check, list, evaluate batch. Cached short TTL; invalidated on issue/suspend/archive paths.

Feature decisions: Enabled / Disabled / Limited.
