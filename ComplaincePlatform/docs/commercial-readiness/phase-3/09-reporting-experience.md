# Phase 3 — Reporting Experience (Sprint 6)

**Sprint:** 6  
**Status:** Delivered  
**UI:** `WebApp/src/app/features/reporting/`

## Role-filtered catalog

Catalog SSOT: `role-report-catalog.ts`  
Filter helper: `filterReportCatalogByPack(packId)`  
Landing loads pack via `RoleExperienceService.getHome()` (local fallback on error).

### Default sets by pack

| Pack | Report ids |
|------|------------|
| hospital-admin (Owner) | compliance, audit, tasks, readiness, custom |
| quality-manager | compliance, audit, tasks, evidence, readiness, custom |
| department-head | tasks, evidence |
| consultant | compliance, audit, readiness, custom |
| auditor | audit, evidence |

## Export

| Action | Behavior |
|--------|----------|
| Export CSV | Client-side CSV of the **visible** catalog rows |
| Print | `window.print()` with print CSS hiding chrome |

Per-report PDF/Excel export remains on individual report pages via `ReportsApiService.exportReport` (pre-existing).

## A11y / shell

- Loading / empty / error banners on landing  
- Breadcrumb Home → Reports  
- Tablet grid breakpoints (≤1024 / ≤768)  
- Design tokens (`--color-*` with legacy fallbacks)
