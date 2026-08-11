# 08 — Licensing Portal

**Phase:** 9.8  
**Sprint:** 4  
**Date:** 2026-07-26  
**Product:** AOVIAS Command v1.0.0

## Scope clarification

v1.0.0 **does not** ship a payments or self-serve purchase portal. “Licensing portal” = operational packaging of existing surfaces:

| Surface | Evidence |
|---------|----------|
| Gateway licenses API | `Gateway/Controllers/LicensesController.cs` → `/api/v1/licenses/*` |
| Commercial ops | `Gateway/Controllers/CommercialOpsController.cs` → `/api/v1/commercial/*` |
| WebApp UI | Settings → Platform → Licensing; Settings → Platform → Commercial Ops |
| Phase 6 docs | [`../phase-6/01-licensing-platform.md`](../phase-6/01-licensing-platform.md), [`../phase-6/07-commercial-operations.md`](../phase-6/07-commercial-operations.md) |

## Delivered (content)

| Artifact | Path |
|----------|------|
| Activation guide (online / offline) | [`commercial-launch/licensing/activation-guide.md`](../../../commercial-launch/licensing/activation-guide.md) |
| Portal launch checklist | [`commercial-launch/licensing/portal-launch-checklist.md`](../../../commercial-launch/licensing/portal-launch-checklist.md) |
| Renewal process (manual keys) | [`commercial-launch/licensing/renewal-process.md`](../../../commercial-launch/licensing/renewal-process.md) |

## Capabilities covered

- Online activation (`offline: false`)
- Offline activation with signed payload (`offline: true`)
- Heartbeat, seats, entitlements, grace / expiry
- Trial provision + customer health + diagnostics
- Manual renewal without payment integration

## Explicit deferrals

- Payment processor / Stripe renewals  
- Public multi-tenant billing portal  
- Automated dunning beyond Grace status messaging  

## Exit

CS and customers can activate, validate, and renew licenses using documented UI/API paths without new product code in Phase 9.
