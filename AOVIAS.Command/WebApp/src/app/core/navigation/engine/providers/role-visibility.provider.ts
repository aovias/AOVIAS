import { Injectable } from '@angular/core';
import type { NavigationContext, NavigationItem } from '../../models/navigation.model';
import type { NavigationModule } from '../../models/module.model';
import { COMPLIANCE_GRAPH_NAV_ROLES } from '../../registry/module.registry';
import type { RoleVisibilityProvider } from './navigation.tokens';

/**
 * Mirrors NAV-1/2 visibility: item `roleKeys` + Compliance Graph module gate.
 * Force-show flag kept for parity with prior `SHOW_COMPLIANCE_GRAPH_NAV_FORCE`.
 */
const SHOW_COMPLIANCE_GRAPH_NAV_FORCE = false;

@Injectable({ providedIn: 'root' })
export class DefaultRoleVisibilityProvider implements RoleVisibilityProvider {
  isItemVisible(item: NavigationItem, ctx: NavigationContext): boolean {
    if (!item.roleKeys?.length) return true;
    return this.hasAnyRole(item.roleKeys, ctx.roles);
  }

  isModuleVisible(module: NavigationModule, ctx: NavigationContext): boolean {
    if (SHOW_COMPLIANCE_GRAPH_NAV_FORCE && module.id === 'module.compliance-graph') {
      return true;
    }
    if (!module.roleKeys?.length) return true;
    return this.hasAnyRole(module.roleKeys, ctx.roles);
  }

  private hasAnyRole(required: readonly string[], userRoles: readonly string[]): boolean {
    const roleSet = new Set(userRoles.map((r) => r.trim().toUpperCase()).filter(Boolean));
    return required.some((r) => roleSet.has(r.trim().toUpperCase()));
  }
}

/** @deprecated Prefer Engine role visibility; kept for layout shims. */
export function shouldShowComplianceGraphNav(userRoles: readonly string[] = []): boolean {
  if (SHOW_COMPLIANCE_GRAPH_NAV_FORCE) return true;
  const roleSet = new Set(userRoles.map((r) => r.trim().toUpperCase()));
  return COMPLIANCE_GRAPH_NAV_ROLES.some((r) => roleSet.has(r));
}
