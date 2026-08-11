# Plan 27-01 — Summary

## Completed

- **`role-definitions.ts`**: `ROLE_IDS`, `RoleId`, `ROLE_LABELS`, `LEGACY_ROLE_ALIASES`, `normalizeRoleId`, `asRoleIds`.
- **`role-registry.service.ts`**: `allRoles()`, `labelOf()`, `normalize()`, `isValidRole()`, unknown-role warn-once logging.
- **`role-label.pipe.ts`**: Standalone `roleLabel` pipe (for templates listing roles).
- **Team invitation**: Options bind `[value]="r.id"` / `{{ r.label }}`; removed `OnboardingDataService.getRoles()`.
- **`AiOnboardingWizardStore`**: Session load maps legacy team role strings to canonical ids where possible.
- **`getCompletionSummary`**: Team review table uses `labelOf()` for the role column (display only).
- **Login**: Normalizes `response.role` to canonical id when alias exists; otherwise keeps raw string for context.
- **`roleGuard`**: Typed with `RoleId[]`; comparison fixed for `string[]` user roles.
- **Docs**: `User`, `LoginResponse.role`, `OrganizationMember.role` TSDoc; `POST_ONBOARD_PAYLOAD.md` example uses `ORG_ADMIN`.

## Verification

- `npx ng build --configuration=development` — **passed**.
