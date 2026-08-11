# Phase 38 — Inventory / verify work

## Baseline (pre-fix)

Hardcoded hex/rgba appeared primarily in **inline `styles` in `.component.ts`** under `src/app/features/journey/components/` (journey-tabs, lifecycle-mode-switch, chapter-navigation-panel, oe-detail-drawer, oe-card, accreditation-decision-panel, action-plan-list, journey-nabh-metrics-bar, standard-section, oe-history-timeline, journey-evidence-tab, journey-trends-mini) and **journey-main-workspace**.

SCSS under `pages/` and execution panels was already largely token-based.

## Post-fix

- **Global:** `foundation.tokens.css` — spacing aliases, `--radius-card`, `--button-height`, `--input-height`, `--card-padding`.
- **Global:** `theme.tokens.css` — `--color-overlay-backdrop`, `--shadow-elevated-panel`, `--shadow-focus-ring-primary`, legacy `--overlay-backdrop`.
- **Journey:** inline styles refactored to `var(--space-*)`, `var(--radius-*)`, `var(--font-size-*)`, theme legacy/semantic colors, `color-mix()` for tints; **no** `#` hex or `rgba(...)` literals left in Journey TS/SCSS (template refs `#evidenceInput` excluded).

## Residual px

- Micro motion: `translateY(-1px)` / `-2px` in execution SCSS hover (acceptable; not spacing tokens).
