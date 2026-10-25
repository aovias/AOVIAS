# Phase 3 — Personal Productivity (Sprint 6)

**Sprint:** 6  
**Status:** Delivered  
**Routes:** `/app/me`, `/app/productivity` (same facade)

## Facade modules

| Card | Deep-link |
|------|-----------|
| My Tasks | `/app/tasks` |
| My Reviews | `/app/evidence/review-queue` |
| My CAPAs | `/app/audit/corrective-actions` (DCC) |
| My Audits | `/app/auditor/sessions` (auditor pack or upcoming audits &gt; 0) |
| Role home | `/app/dashboard` |

Attention KPIs come from Role Experience `attentionCounts` (best-effort analytics).

## Bookmarks / saved filters

| Store | Key | Notes |
|-------|-----|-------|
| Favorites / recent | `praman.nav.favorites.v1` / `praman.nav.recent.v1` | Surfaced on My work + sidebar/header |
| Saved filters | `praman.productivity.savedFilters.v1` | `SavedFiltersService` — label + href |

## Nav

Action Center → **My work** (`/app/me`, subset active).

## Out of scope

No new personal-task API; facade composes existing modules only.
