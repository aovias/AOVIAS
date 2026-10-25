---
plan: 19-01
phase: 19
title: Geo configuration + GeoSelectorService (India-first, cached)
date: 2026-04-02
---

## What shipped

- **Dependency added**: `country-state-city` installed for client-side geo data.
- **Geo config layer**: `GEO_CONFIG` InjectionToken with India-first defaults (`IN`, country selection disabled).
- **Shared geo types**: minimal reusable models for country/state/city and the outbound selection contract.
- **Cached geo data service**: `GeoSelectorService` provides sorted states and on-demand cities with in-memory caching.

## Files changed

- `package.json`, `package-lock.json`
- `src/app/core/config/geo.config.ts`
- `src/app/shared/models/geo.models.ts`
- `src/app/shared/services/geo-selector.service.ts`
- `src/app/app.config.ts`

## Verification

- `npm run build` ✅

## Commits

- `chore(19-01): add country-state-city dependency` — `2a0e272`
- `feat(19-01): add geo config and cached geo service` — `4424653`

