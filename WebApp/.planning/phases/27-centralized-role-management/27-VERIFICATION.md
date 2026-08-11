# Phase 27 — Verification

**Status:** passed (manual code review + `ng build`)

## must_haves (from 27-01-PLAN)

| Criterion | Evidence |
|-----------|----------|
| Nine canonical `RoleId` values | `src/app/core/auth/roles/role-definitions.ts` — `ROLE_IDS` / `ROLE_LABELS` |
| `RoleRegistryService` API | `allRoles`, `labelOf`, `normalize`, `isValidRole`, `unknownLabel` |
| POST bodies use ids | Team select stores `r.id`; `onboard-submit.mapper` unchanged — still `m.role.trim()` (now canonical id from UI) |
| Team `<select>` value = id, label in UI | `team-invitation-step.component.ts` template |
| Review shows labels | `ai-setup-assistant.component.ts` `getCompletionSummary` → `labelOf(m.role)` |
| Unknown role | `labelOf` → `Unknown Role` + one-time `logger.warn` per distinct raw value |
| `roleGuard` | `RoleId[]` + docstring; no routes use it yet in repo |
| Build | `ng build` succeeds |

## Notes

- **`RoleLabelPipe`** is available for future member lists; completion step receives pre-resolved labels from `getCompletionSummary`.
- **Backend contract**: Identity API should emit canonical ids; legacy **display** strings on login are mapped when listed in `LEGACY_ROLE_ALIASES`.
