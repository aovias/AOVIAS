---
plan: 19-02
phase: 19
title: Reusable GeoSelector UI component (reactive forms + autocomplete)
date: 2026-04-02
---

## What shipped

- **Standalone shared UI component**: `GeoSelectorComponent` under `src/app/shared/ui/geo-selector/` with **OnPush**.
- **Reactive forms plug-and-play**: implements **ControlValueAccessor** for `GeoSelectionValue` so parent forms can bind a single `FormControl`.
- **UX behavior**:
  - Sorted **state dropdown** (from `GeoSelectorService`)
  - **City autocomplete** with configurable min-chars and option cap
  - **Manual city entry fallback** when city list is empty
- **Separation of concerns**: component consumes `GeoSelectorService` + `GEO_CONFIG` (no direct `country-state-city` calls).

## Files changed

- `src/app/shared/ui/geo-selector/geo-selector.component.ts`
- `src/app/shared/ui/geo-selector/geo-selector.component.html`
- `src/app/shared/ui/geo-selector/geo-selector.component.scss`

## Verification

- `npm run build` ✅

## Commits

- `feat(19-02): add reusable geo selector component` — `b7c3dc2`

