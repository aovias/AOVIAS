# T1 — Breadcrumb Integration Report

**Date:** 2026-07-20

---

## UI

TechnologyLayout enables `showBreadcrumb` on AppShell. Path-derived crumbs use `buildBreadcrumbsFromPath`.

## Labels added (`SEGMENT_LABELS`)

`technology`, `architecture`, `platform` → Platform Services, `ai`, `integrations`, `apis`, `developer`, `roadmap`  
(reuses existing `security`, `privacy`)

## Examples

- Technology  
- Technology / Architecture  
- Technology / AI at AOVIAS  
- Technology / Security  
- Technology / Integrations  

## JSON-LD

Landing and `[slug]` pages pass explicit breadcrumbs into `buildPageSchemas` via `technologyLandingBreadcrumbs` / `technologyTopicBreadcrumbs`.
