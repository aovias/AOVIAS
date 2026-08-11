# Phase 24 — Research: Framework binding, selection IDs, dynamic icons

## Current implementation (post–Phase 23)

- **`FrameworkLookup`:** `frameworkId`, `name`, `authority` (currently filled as `authority ?? code`), `code`, `isActive`.
- **API reality (sample):** Items have `frameworkId`, `name`, `code`, `validityPeriodMonths`, `isActive` — **no `authority` field yet**; `<p>` therefore shows **code** via fallback.
- **Selection:** Template uses `frameworkId`; store/parent uses `selectedFrameworks: string[]` and emits `frameworkIds`; **`Hospital.frameworkPlatformIds`** naming predates API field names.
- **Icon:** First letter of `name` in `.framework-icon` — **placeholder**, not API-driven.
- **Change detection:** `ChangeDetectorRef.detectChanges()` after load/retry/toggle (mirrors departments step).

## Requirements interpretation

1. **Authority binding:** When API adds `authority`, map only that property into the `<p>` binding. Until then: either **(a)** keep minimal fallback to `code` with a single comment + product agreement, or **(b)** hide `<p>` when authority empty to avoid implying wrong semantics.
2. **platformId:** Normalize in **one** internal field used for selection/storage (recommend `platformId` on `FrameworkLookup`, populated from `platformId ?? frameworkId`). Never render in UI. Payload/store may keep `frameworkPlatformIds` JSON name for backward compatibility but values are these IDs.
3. **Icons:** App has **no** FontAwesome / Heroicons / Lucide today (`package.json`). Prefer **Lucide** + **`@ng-icons/core`** + **`@ng-icons/lucide`** (standalone-friendly, tree-shakeable) or **`angular-fontawesome`** if brand already favors FA — **pick one** in execution.
4. **Dynamic icons without hardcoding in template:** Small **`FrameworkCardIconComponent`** (or shared `LookupIconComponent`) that accepts `iconKey: string | null` from API; internally resolves via a **whitelist map** (`Record<string, NgIcon>` or similar) with a **default** icon for unknown keys — scalable, no `*ngSwitch` with twenty cases in the parent template.
5. **State integrity:** Avoid mutating parent `@Input()` arrays in isolation; optionally maintain a **writable local copy** synced from `@Input` on `ngOnChanges`, emit immutable updates — reduces desync when step re-enters.

## RESEARCH COMPLETE
