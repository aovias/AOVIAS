# Plan 01 — Summary (Phase 43)

**Status:** Complete  
**Date:** 2026-04-09

## Delivered

- **Token catalog** in `foundation.tokens.css`: `--scrollbar-width`, `--scrollbar-width-touch`, `--scrollbar-radius`, `--scrollbar-opacity-idle`, `--scrollbar-opacity-hover`, `--scrollbar-motion-duration`, `--scrollbar-motion-duration-reduced`, `--scrollbar-motion-easing`.
- **Theme tokens** in `theme.tokens.css` (light + dark): `--scrollbar-track`, primary thumb states (`--scrollbar-thumb` … `--scrollbar-thumb-focus`), panel/dense/timeline variant thumbs (`--scrollbar-thumb-panel*`, `--scrollbar-thumb-dense*`, `--scrollbar-thumb-timeline*` aliases).
- **`43-DESIGN-SPEC.md`** updated with implementation reference paths.
- **`DESIGN_RULES.md`**: new “Scrollbar system (Phase 43)” section (principles, utilities, responsive, forbidden patterns) and scrollbar entries under global token files.

## Verification

- `npx ng build --configuration=development` — passed.

## Notes

- All scrollbar colors use `color-mix(in srgb, var(--color-*) …)` — no new raw hex in feature CSS.
