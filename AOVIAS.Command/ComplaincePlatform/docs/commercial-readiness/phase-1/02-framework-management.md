# Phase 1 — Framework Management (Catalog Lifecycle)

**Sprint:** 2  
**Status:** Delivered  
**Principle:** EXTEND `ComplianceFramework` — not Framework Studio.

## Model

| Field | Type | Notes |
|-------|------|-------|
| Status | `FrameworkCatalogStatus` | Draft, Published, Active, Retired, Archived (default Active) |
| PublishedAt / RetiredAt / ArchivedAt | `DateTime?` | Set on transitions |
| IsActive | `bool` | Kept in sync: `IsActive == (Status == Active)` |

Migration: `20260726120000_Phase1_FrameworkLifecycle` backfills Status from legacy IsActive (Active=2 / Retired=3).

## Transitions

| From | Action | To |
|------|--------|-----|
| Draft | Publish | Published |
| Published / Retired | Activate | Active |
| Active / Published | Retire | Retired |
| Retired | Archive | Archived |

Rules:

1. **Single active version per family** — on Activate, other Active rows with the same `Name` are Retired.  
2. **Archived is immutable** — all mutations rejected.  
3. Enroll requires `IsActive` **and** `Status == Active`.

## APIs

### ComplianceService

| Method | Path |
|--------|------|
| GET | `/api/v1/frameworks/catalog?includeInactive=` |
| POST | `/api/v1/frameworks/{id}/publish` |
| POST | `/api/v1/frameworks/{id}/activate` |
| POST | `/api/v1/frameworks/{id}/retire` |
| POST | `/api/v1/frameworks/{id}/archive` |

`FrameworkDto` now includes Version, Status, PublishedAt, RetiredAt, ArchivedAt.  
Service: `FrameworkLifecycleService`. Existing GET frameworks / enroll / structure v1.1 unchanged.

### Gateway

Thin MediatR proxies on `FrameworkController` for the same paths.

## Extension points

- Seed/new frameworks may start as Draft; activate after publish.  
- Future Framework Studio can emit Draft rows into this catalog without replacing lifecycle.
