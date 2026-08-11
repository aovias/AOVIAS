# CRP Phase 6 — Unified API Specification

## Canonical

- `GET/POST /api/v1/licenses/*` — issue, activate, status, seats, heartbeat, entitlements, suspend, archive
- `GET /api/v1/licenses/summary` and `GET /api/v1/licensing/summary` — dual-read façade
- `POST /api/v1/licenses/catalog/backfill` — CommercialAdmin
- Internal: `/internal/licenses/catalog/sync`, entitlements/evaluate

## Catalogue (deprecated as operational)

- `/api/v1/subscriptions*` — metadata assign/upgrade/current with dual-read enrichment from license
