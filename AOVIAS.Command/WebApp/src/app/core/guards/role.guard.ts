import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { UserContextService } from '../services/user-context.service';
import { normalizeRoleId, type RoleId } from '../auth/roles/role-definitions';

/**
 * Route guard: `allowedRoles` must be **canonical role ids** (e.g. `ORG_ADMIN`), matching
 * `UserContextService` / login API — not display labels.
 * Raw claim aliases (e.g. `Department Head`) are normalized via {@link normalizeRoleId}.
 */
export const roleGuard = (allowedRoles: readonly RoleId[]): CanActivateFn => {
  return (_route, _state) => {
    const userContext = inject(UserContextService);
    const router = inject(Router);

    // Defensive: `authGuard` normally hydrates first, but a `roleGuard` route reached without a
    // parent `authGuard` would otherwise evaluate an empty role set. No-op when already populated.
    userContext.hydrateFromToken();

    const raw = userContext.getUserRoles() ?? [];
    const combined = new Set<string>([...raw, ...raw.map((r) => normalizeRoleId(r) ?? r)]);

    if (allowedRoles.some((allowed) => combined.has(allowed))) {
      return true;
    }

    router.navigate(['/app/dashboard']);
    return false;
  };
};
