---
phase: 19
title: GeoSelector Module (India-first) — Research Notes
date: 2026-04-02
---

## Context in this repo

- Angular **standalone** architecture (no NgModules).
- UI stack: **Angular Material**; shared presentational components live under `src/app/shared/ui/` (e.g. `form-field`).
- Feature code lives under `src/app/features/...` and should stay self-contained.
- State management guidance says **RxJS BehaviorSubjects**, but existing onboarding wizard uses **Angular Signals** in `OnboardingStore`. For GeoSelector, we should avoid introducing global state; keep state local to component + reactive forms, with caching in a singleton service.

## Requirement decomposition

- Data source: `country-state-city` npm package (client-side, no backend calls).
- India-first: default country **IN** and (for now) only active country unless config enables others.
- Performance: cache India states in memory; load cities on-demand per state; avoid repeated expensive lookups; guard against rapid switching.
- UX: state dropdown sorted; city input uses Material autocomplete; if city list empty or too large/unreliable, allow manual entry fallback.
- Integration: Onboarding submits only `{ countryCode: 'IN', stateCode, cityName }`; edit mode must rehydrate and validate legacy/invalid values gracefully.

## Proposed architecture

### Config (future-ready)

- Add a small geo config layer in `src/app/core/config/geo.config.ts`:
  - `defaultCountryCode` (initially `'IN'`)
  - `allowCountrySelection` boolean (initially `false`)
  - optionally `enabledCountryCodes` (initially `['IN']`)
  - option for `cityAutocompleteMinChars`, `maxAutocompleteOptions`
- Expose config via an `InjectionToken<GeoConfig>` so future rollout can override in `app.config.ts` without changing component/service logic.

### Service (data provider + caching)

- `GeoSelectorService` in `src/app/shared/services/geo-selector.service.ts`:
  - `getCountries()` (future; currently returns only enabled list)
  - `getStates(countryCode)` (cached, sorted)
  - `getCities(countryCode, stateCode)` (cached per key, sorted)
  - Uses `country-state-city` package under the hood; components do **not** call npm package directly.

### Shared UI component (reactive forms friendly)

- Standalone `GeoSelectorComponent` in `src/app/shared/ui/geo-selector/`:
  - Works with parent `FormGroup` via `@Input() formGroup` OR `ControlValueAccessor` (preferred for plug-and-play); for Phase 19, plan will implement CVA to make usage trivial across forms.
  - Inputs for config overrides: `countryDisabled`, `countryHidden`, `required`.
  - Emits value `{ countryCode, stateCode, cityName }` and supports patching initial value for edit mode.
  - State uses `<mat-select>`, city uses `<input matInput [matAutocomplete]>` with filtered options, plus “Enter manually” fallback when no matches.

### Onboarding integration

- Add fields to hospital form (or relevant onboarding step) bound to:
  - state → `stateCode`
  - city → `cityName`
  - countryCode fixed to config default (`IN`)
- On edit/rehydrate: component receives existing values, ensures state list loaded, then loads cities for that state before setting city control; if invalid legacy values, keep manual city and mark state invalid (or show warning).

## Risks / edge cases

- Package size/perf: `country-state-city` contains global data; we must avoid repeated lookups and avoid bundling unnecessary code paths (tree-shaking should help but we’ll still cache results).
- Rapid switching: cities request should cancel prior in-flight (if we wrap in Observables) or ignore stale responses (if Promise).
- Very large city lists: cap autocomplete options and allow manual entry.

## Implementation notes (post-execution)

- `GeoSelectorService` caches states per country and cities per `${countryCode}:${stateCode}` key, and returns sorted lists.
- `GeoSelectorComponent` is implemented as a standalone CVA and emits `null` when invalid so parent `Validators.required` behaves as expected.

