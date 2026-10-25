# Phase 48 — Plan 02 Summary

## Completed

- Journey dashboard: `progressCardTitle` / `progressCardSubtitle`, `accreditationReadinessPercent` tweaks per mode (Internal/Surveillance/ReAccreditation).
- Action bar: `dangerHidden`, `dangerEmphasis`, `dangerDisabled`; two-column grid when danger hidden.
- Metrics filtered by policy (`metrics.*` flags).
- Mode cards use `store.modeLockReason(mode)`; progression thresholds via `operationalModeProgressionBlocked`.

## Files

- `src/app/features/journey/pages/journey.component.ts`, `journey.component.html`
- `src/app/features/journey/components/action-bar/*`
- `src/app/shared/ui/accreditation-progress-card/*` (bindings only via parent; title/subtitle from policy)

## PLANNING COMPLETE
