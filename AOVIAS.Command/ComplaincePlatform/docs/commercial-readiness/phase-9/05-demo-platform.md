# 05 — Demo Platform

**Phase:** 9.3  
**Sprint:** 3  
**Date:** 2026-07-26

## Delivered

| Asset | Path |
|-------|------|
| Demo runbook | `commercial-launch/demo/DEMO_RUNBOOK.md` |
| QM day | `commercial-launch/demo/scenarios/qm-day.md` |
| Audit prep | `commercial-launch/demo/scenarios/audit-prep.md` |
| CAPA | `commercial-launch/demo/scenarios/capa.md` |
| AI assistant | `commercial-launch/demo/scenarios/ai-assistant.md` |
| Risk register | `commercial-launch/demo/scenarios/risk-register.md` |
| Management review | `commercial-launch/demo/scenarios/management-review.md` |

## Data dependency

Runbook centers on **DemoDataSeeder** (`dry-run` → `seed` → `verify`) plus any required `seed-data` / master-data prerequisites for frameworks. Seeder does not create users or truncate databases—see `DemoDataSeeder/README.md`.

## Environment policy

- Staging/local only  
- Enterprise entitlements recommended for full six-scenario tour  
- Never demo against production PHI  

## Exit

SE-ready demo narrative chained to sales enablement and published docs.
