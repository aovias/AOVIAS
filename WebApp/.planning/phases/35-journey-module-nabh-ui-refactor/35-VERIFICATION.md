# Phase 35 — Verification

## Automated

- [x] `npx ng build --configuration=development` — **pass** (2026-04-06)

## Goal-backward checks

- [x] **Clause** UI and types removed from implementation (only migration comments remain).
- [x] Domain hierarchy **Chapter → Standard → OE** reflected in models and mock fixture.
- [x] **JourneyDataService** returns typed `JourneyWorkspaceSnapshot` from mock.
- [x] **JourneyWorkspaceStore** holds selection, lifecycle, filters; immutable chapter list sorting.
- [x] **Accreditation panel** uses configurable thresholds (`nabh-thresholds.ts`).
- [x] **Lifecycle** modes bound; **read-only** for final/surveillance (placeholder).
- [x] **OE drawer**: NA + justification validation; action plans; evidence; history.
- [x] **Backend contract** documented in `BACKEND_CONTRACT_SPEC.md`.

## Manual (recommended)

- [ ] Navigate to `/app/journey` — load, select chapter, expand standard, open OE details.
- [ ] Toggle NA without justification — save blocked with message.
- [ ] Switch lifecycle — OE filter changes for Final/Surveillance (Core+Commitment / Core-only).

## Status

**Phase 35 implementation:** Complete (UI + mock + contract spec).
