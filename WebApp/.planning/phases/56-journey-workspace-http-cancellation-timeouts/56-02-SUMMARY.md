# Phase 56 — Plan 02 Summary

## Delivered

- `JourneyFeatureShellComponent` provides `JourneyWorkspaceStore` once for `/app/journey/**`; `routes.ts` nests dashboard + execution under it.
- Removed duplicate store providers from `JourneyComponent` and `JourneyExecutionShellComponent`; `initFromRoute()` runs from shell only; journey list `ngOnInit` no longer clears cache + re-inits (hospital switch still calls `initFromRoute`).
- `workspaceLoad$` + `switchMap` for `forkJoin` (session/dashboard/chapters); `reloadAfterOperationalModeChange` uses same pipeline with `resetChapterStandards`.
- `chapterStandardsTrigger$` + `switchMap` for chapter standards GET; latest chapter wins; merge skipped when `selectedChapterId` no longer matches.

## Verification

- `npx tsc -p tsconfig.app.json --noEmit` — pass
