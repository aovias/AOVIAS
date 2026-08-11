# Phase 56 — Verification

## Automated

- `npx tsc -p tsconfig.app.json --noEmit` — pass
- `npx vitest run` (or project test command) — `journey-http-error.spec.ts`, `journey-data.service.spec.ts` (long-read context), `journey-workspace.store.spec.ts`

## Acceptance criteria (product)

| Criterion | Evidence |
|-----------|----------|
| Long-running Journey GETs complete within agreed budget when server responds in time | `environment.journeyLongReadTimeoutMs` (default 120s) + `JOURNEY_LONG_READ_CONTEXT`; must align with Gateway (see `journey-screen-api-spec.md` §1.1) |
| No spurious cancellation while waiting on same screen | `JourneyFeatureShellComponent` single `JourneyWorkspaceStore`; workspace `switchMap` + chapter `switchMap` only supersede on newer intent |
| Navigation cancels only per product (leave Journey = destroy store) | Unchanged: leaving `/app/journey` destroys shell |
| Client timeout ≥ Gateway chain | Ops fills `journeyLongReadTimeoutMs`; `0` disables RxJS timeout |
| Loading / error UX | `formatJourneyHttpError` for timeout / 0 / 504; recoverable copy |
| Retries | No automatic `retry()` on Journey loads (per plan) |
| Observability (dev) | `logJourneyHttpDev` → `LoggerService.debug` with `{ kind, status?, url? }` |

## Manual (recommended)

1. Throttle network → open Journey → stay on dashboard until metrics load → navigate to execution; workspace should not restart from duplicate store.
2. Rapid chapter changes → standards panel shows one loading state; final data matches last chapter.
3. Optional: set `journeyLongReadTimeoutMs` very low → confirm timeout message (not generic server error).

## Status

**passed** — implementation complete; **human_needed** for Gateway SLA numbers and production proxy parity.
