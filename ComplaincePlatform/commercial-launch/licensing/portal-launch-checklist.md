# Licensing Portal Launch Checklist

**Product:** AOVIAS Command v1.0.0  
**Scope:** Existing licensing + commercial ops surfaces (not a new payments portal)

“Portal” for v1.0.0 means:

| Surface | Path / API |
|---------|------------|
| Customer licensing UI | WebApp **Settings → Platform → Licensing** |
| Commercial ops UI | WebApp **Settings → Platform → Commercial Ops** |
| Gateway façade | `/api/v1/licenses/*`, `/api/v1/commercial/*` |

## Pre-launch (AOVIAS internal)

| # | Item | Owner | Done |
|---|------|-------|------|
| 1 | License issue smoke on release candidate | Eng | ☐ |
| 2 | Online activate + heartbeat smoke | Eng | ☐ |
| 3 | Offline activate smoke (signed payload) | Eng | ☐ |
| 4 | Seat assign + MaxSeats enforcement | Eng | ☐ |
| 5 | Entitlements check for SKU codes | Product | ☐ |
| 6 | Trial provision → checklist path | CS | ☐ |
| 7 | Customer-health + diagnostics download | Support | ☐ |
| 8 | Grace / expiry messaging reviewed | Product | ☐ |
| 9 | Admin-only policy on commercial endpoints | Eng | ☐ |
| 10 | Docs published: activation + renewal | Docs | ☐ |

## Customer-facing readiness

| # | Item | Done |
|---|------|------|
| 1 | Activation guide linked from onboarding pack | ☐ |
| 2 | Pricing one-pager lists entitlements (no payment CTA) | ☐ |
| 3 | Support KB article: license activate | ☐ |
| 4 | Renewal process (manual keys) agreed with Sales | ☐ |
| 5 | Status page template ready for outages affecting licensing | ☐ |

## Explicit non-goals (v1.0.0)

- Stripe / payment processor checkout  
- Self-serve plan upgrade shopping cart  
- Public multi-tenant SaaS billing portal  

Renewals are **manual key issue** — see [renewal-process.md](renewal-process.md).

## Go / No-Go for licensing portal

| Gate | Go if |
|------|-------|
| Activate online | Status Active within 1 minute of issue |
| Activate offline | Offline activation record present |
| Soft gate | Missing entitlement returns expected denial |
| Diagnostics | Redacted JSON downloads without secrets |

**Launch Director sign-off:** _______________ Date: ________
