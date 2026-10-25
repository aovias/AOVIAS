# Phase 24 verification: Framework binding, selection, icons

**Status:** passed  
**Date:** 2026-04-03

## must_haves

| # | Check | Result |
|---|--------|--------|
| 1 | Authority mapping from API with documented `code` fallback | `normalizeFramework` sets `authority` from `authority`/`Authority` then `code`; JSDoc on `FrameworkLookup`. |
| 2 | `platformId` canonical; not shown in card template | Template binds `name` / `authority` only; selection uses `platformId`. |
| 3 | Dynamic Lucide + whitelist | `FrameworkCardIconComponent` + `provideIcons`. |
| 4 | Checkbox ↔ stored IDs | `localSelectedIds` toggles; synced from `@Input` on changes. |
| 5 | No per-row icon ladder in selector | Single `<app-framework-card-icon [iconKey]="framework.iconKey" />`. |

## Build

- `ng build --configuration=development` — OK.

## Follow-ups

- Extend **`FRAMEWORK_ICON_KEY_MAP`** when backend publishes stable `iconKey` values.
- When API adds **`authority`** for all rows, cards will prefer it automatically; `code` remains secondary.
