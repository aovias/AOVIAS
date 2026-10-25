# Phase 3 — Auditor Experience (Sprint 5)

**Sprint:** 5  
**Status:** Delivered  
**Principle:** Dedicated auditor workspace routes with role guard; stop reusing sessions stubs for observations/history.

## Access

`roleGuard` on `/app/auditor/*` allows:

- `EXTERNAL_AUDITOR`
- `REGULATOR`
- `REGULATORY_ADMIN`
- `SUPER_ADMIN`
- `ORG_ADMIN`

Guard normalizes claim aliases via `normalizeRoleId` before matching.

Pack home remains `/app/auditor/dashboard` for `EXTERNAL_AUDITOR` / `REGULATOR` (and aliases).

## Pages

| Route | Component | Notes |
|-------|-----------|--------|
| `/app/auditor/dashboard` | `AuditorDashboardPageComponent` | KPIs + recent sessions; **quick actions** from role-experience pack |
| `/app/auditor/sessions` | Sessions list | Unchanged |
| `/app/auditor/observations` | `AuditorObservationsPageComponent` | Flattens observations across sessions (not sessions stub) |
| `/app/auditor/history` | `AuditorHistoryPageComponent` | Closed / submitted / draft-report sessions only |
| `/app/auditor/evidence-review` | Assessment workspace | Unchanged |

## Quick actions

Default pack actions: Auditor dashboard, Sessions, Evidence review, Observations.  
Dashboard header renders pack `quickActions` (API or local fallback).

## Nav

Dashboard → **Auditor home** includes `REGULATORY_ADMIN` in `requiredRoles` so regulatory admins can open the workspace when authorized.
