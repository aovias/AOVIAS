import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { normalizeRoleId, type RoleId } from '../../../../core/auth/roles/role-definitions';
import { UserContextService } from '../../../../core/services/user-context.service';
import { AuditWorkflowEngine } from './audit-workflow.engine';

/**
 * Guard a workspace surface with the role restriction its workflow definition declares.
 *
 * <p>
 * **Why this exists.** The role restriction on a surface was written twice: once in the route
 * config as `roleGuard(AUDITOR_ROLE_IDS)` and once inside the shell component deciding which
 * sub-tabs to advertise. Two declarations of one rule is exactly the duplication that lets a
 * workspace offer a destination its own guard then rejects. Both now read the surface's
 * `requiresAnyRole` from the workflow definition.
 * </p>
 *
 * <p>
 * **Resolved at activation, not at import.** Route configuration is evaluated when the module
 * loads, before any tenant configuration exists. Taking the role set inside the guard body — which
 * Angular runs in an injection context — is what lets a configured lifecycle govern the route as
 * well as the tab strip.
 * </p>
 *
 * <p>
 * **This is navigation, not authorization.** A surface with no declared restriction is open here,
 * and a tenant editing its lifecycle can widen what the client offers. The server remains the
 * authority on what data anyone may actually read or write; this guard stops a user reaching a
 * screen that would be empty or confusing for them, which is a usability job.
 * </p>
 */
export const auditSurfaceGuard = (tab: string, surfaceId: string): CanActivateFn => {
  return (_route, _state) => {
    const workflow = inject(AuditWorkflowEngine);
    const userContext = inject(UserContextService);
    const router = inject(Router);

    const required = workflow.rolesFor(tab, surfaceId);
    if (!required) return true;

    // Defensive: `authGuard` normally hydrates first, but a guarded route reached without a parent
    // `authGuard` would otherwise evaluate an empty role set. No-op when already populated.
    userContext.hydrateFromToken();

    const raw = userContext.getUserRoles() ?? [];
    // Claims may arrive as display aliases ("Department Head"); compare against both forms, exactly
    // as `roleGuard` does, so consolidating the declaration does not narrow who gets in.
    const held = new Set<string>([...raw, ...raw.map((r) => normalizeRoleId(r) ?? r)]);

    if (required.some((role) => held.has(role as RoleId))) return true;

    router.navigate(['/app/dashboard']);
    return false;
  };
};
