/**
 * Legacy sidebar/header nav types + compatibility shims.
 * SSOT for menu data is `registry/menu.registry.ts` (NAV-3).
 *
 * @deprecated Prefer NavigationQueryService / surface providers for new code.
 */

import { sidebarIcon } from '../../layout/sidebar-icons.registry';
import { NavigationBadgeKeys, type NavigationBadgeKey } from '../models/badge.model';
import { MENU_REGISTRY } from '../registry/menu.registry';
import { COMPLIANCE_GRAPH_NAV_ROLES } from '../registry/module.registry';
import {
  isComplianceGraphShellNavPath,
  isDocumentationShellNavPath,
  isJourneyShellNavPath,
  isPatientSafetyShellNavPath,
  isQualityIntelligenceShellNavPath,
  isRisksShellNavPath,
} from '../registry/route-match.util';
import { shouldShowComplianceGraphNav } from '../engine/providers/role-visibility.provider';

export type BadgeVariant = 'neutral' | 'warning' | 'critical';

/** Wired to Action Center badge signals (mock or API-backed). */
export type NavBadgeKey = NavigationBadgeKey;

export { NavigationBadgeKeys };

/** Future NAV-3 context for dynamic/permission filters. */
export interface NavResolveContext {
  roles: readonly string[];
  permissions?: readonly string[];
  workspaceId?: string | null;
}

export type NavItemResolver = (item: SidebarMenuItem, ctx: NavResolveContext) => boolean;

export interface SidebarMenuItem {
  id?: string;
  label: string;
  icon: string;
  route: string;
  primary?: boolean;
  children?: SidebarMenuItem[];
  disabled?: boolean;
  disabledReason?: string;
  tooltip?: string;
  ariaLabel?: string;
  navBadgeKey?: NavBadgeKey;
  subsetActive?: boolean;
  requiredRoles?: string[];
  filter?: NavItemResolver;
}

export type NavItem = SidebarMenuItem;

export interface SidebarMenuSection {
  id: string;
  title: string;
  items: SidebarMenuItem[];
}

export interface BottomNavItem {
  label: string;
  route: string;
  icon: string;
}

export interface HeaderNavItem {
  label: string;
  icon: string;
  route: string;
  disabled?: boolean;
  disabledReason?: string;
  exactActive?: boolean;
  subsetActive?: boolean;
}

export interface HeaderNavGroup {
  id: string;
  title: string;
  sectionIcon: string;
  items: HeaderNavItem[];
}

export {
  COMPLIANCE_GRAPH_NAV_ROLES,
  isComplianceGraphShellNavPath,
  isDocumentationShellNavPath,
  isJourneyShellNavPath,
  isPatientSafetyShellNavPath,
  isQualityIntelligenceShellNavPath,
  isRisksShellNavPath,
  shouldShowComplianceGraphNav,
};

/** @deprecated Use NavigationQueryService.sidebarViewSections / SidebarProvider. */
export function getSidebarMenuSections(): SidebarMenuSection[] {
  const byCat = new Map<string, { title: string; order: number; items: SidebarMenuItem[] }>();
  const titleByCat: Record<string, string> = {
    dashboard: 'Dashboard',
    'quality-management': 'Quality Management',
    'compliance-execution': 'Compliance Execution',
    'action-center': 'Action Center',
    administration: 'Administration',
  };
  const orderByCat: Record<string, number> = {
    dashboard: 10,
    'quality-management': 20,
    'compliance-execution': 30,
    'action-center': 40,
    administration: 50,
  };

  for (const item of MENU_REGISTRY) {
    if (!item.surfaces.includes('sidebar')) continue;
    const cat = byCat.get(item.categoryId) ?? {
      title: titleByCat[item.categoryId] ?? item.categoryId,
      order: orderByCat[item.categoryId] ?? 99,
      items: [],
    };
    cat.items.push({
      id: item.id,
      label: item.title,
      icon: sidebarIcon(item.icon),
      route: item.route ?? '/',
      primary: item.primary,
      disabled: item.disabled,
      disabledReason: item.disabledReason,
      tooltip: item.tooltip,
      navBadgeKey: item.badgeKey,
      subsetActive: item.subsetActive,
      requiredRoles: item.roleKeys ? [...item.roleKeys] : undefined,
    });
    byCat.set(item.categoryId, cat);
  }

  return [...byCat.entries()]
    .sort((a, b) => a[1].order - b[1].order)
    .map(([id, cat]) => ({
      id,
      title: cat.title,
      items: cat.items.sort((a, b) => {
        const ao = MENU_REGISTRY.find((m) => m.id === a.id)?.order ?? 0;
        const bo = MENU_REGISTRY.find((m) => m.id === b.id)?.order ?? 0;
        return ao - bo;
      }),
    }));
}

/** @deprecated Use NavigationQueryService / HeaderProvider. */
export function getHeaderNavGroups(userRoles: readonly string[] = []): HeaderNavGroup[] {
  const showGraph = shouldShowComplianceGraphNav(userRoles);
  const groups = new Map<string, HeaderNavGroup>();
  const moduleMeta: Record<
    string,
    { title: string; icon: Parameters<typeof sidebarIcon>[0]; order: number }
  > = {
    'module.compliance-graph': { title: 'Compliance Graph', icon: 'knowledgeGraph', order: 10 },
    'module.quality-intelligence': { title: 'Quality Intelligence', icon: 'analytics', order: 20 },
    'module.patient-safety': { title: 'Patient Safety', icon: 'issuesRisks', order: 30 },
    'module.risks': { title: 'Risks', icon: 'issuesRisks', order: 40 },
    'module.documentation': { title: 'Documentation', icon: 'policies', order: 50 },
    'module.reports': { title: 'Reports', icon: 'reports', order: 60 },
    'module.audit': { title: 'Audit', icon: 'internalAudit', order: 70 },
    'module.knowledge': { title: 'Knowledge', icon: 'knowledgeGraph', order: 80 },
    'module.settings': { title: 'Settings', icon: 'overview', order: 90 },
  };

  for (const item of MENU_REGISTRY) {
    if (!item.surfaces.includes('header')) continue;
    if (item.moduleId === 'module.compliance-graph' && !showGraph) continue;
    if (item.id === 'knowledge.graph' && !showGraph) continue;
    const meta = moduleMeta[item.moduleId];
    if (!meta) continue;
    const id = `header-${item.moduleId.slice('module.'.length)}`;
    const group = groups.get(id) ?? {
      id,
      title: meta.title,
      sectionIcon: sidebarIcon(meta.icon),
      items: [],
    };
    group.items.push({
      label: item.title,
      icon: sidebarIcon(item.icon),
      route: item.route ?? '/',
      disabled: item.disabled,
      disabledReason: item.disabledReason,
      exactActive: item.exactActive,
      subsetActive: item.subsetActive,
    });
    groups.set(id, group);
  }

  return [...groups.values()].sort(
    (a, b) =>
      (moduleMeta[`module.${a.id.replace(/^header-/, '')}`]?.order ?? 99) -
      (moduleMeta[`module.${b.id.replace(/^header-/, '')}`]?.order ?? 99),
  );
}

/** @deprecated Use NavigationQueryService.bottomItems(). */
export function getBottomNavItems(): BottomNavItem[] {
  return MENU_REGISTRY.filter((i) => i.surfaces.includes('bottom'))
    .slice()
    .sort((a, b) => a.order - b.order)
    .map((i) => ({
      label: i.title,
      route: i.route ?? '/',
      icon: sidebarIcon(i.icon),
    }));
}

export function navItemIdFromRoute(route: string, label: string): string {
  const fromRoute = route
    .replace(/^\/app\/?/, '')
    .replace(/[^\w/-]+/g, '')
    .replace(/\//g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
  return fromRoute || label.toLowerCase().replace(/\s+/g, '-');
}

export function withNavItemIds(sections: SidebarMenuSection[]): SidebarMenuSection[] {
  const assign = (item: SidebarMenuItem): SidebarMenuItem => ({
    ...item,
    id: item.id ?? navItemIdFromRoute(item.route, item.label),
    children: item.children?.map(assign),
  });
  return sections.map((section) => ({
    ...section,
    items: section.items.map(assign),
  }));
}

/** @deprecated Role filtering is applied by NavigationEngine RoleVisibilityProvider. */
export function filterSidebarMenuSections(
  sections: SidebarMenuSection[],
  userRoles: string[],
): SidebarMenuSection[] {
  const roleSet = new Set(userRoles.map((r) => r.trim().toUpperCase()).filter(Boolean));
  const allow = (item: SidebarMenuItem): boolean => {
    if (!item.requiredRoles?.length) return true;
    return item.requiredRoles.some((r) => roleSet.has(r.trim().toUpperCase()));
  };

  return withNavItemIds(sections)
    .map((section) => ({
      ...section,
      items: section.items
        .filter(allow)
        .map((item) =>
          item.children?.length ? { ...item, children: item.children.filter(allow) } : item,
        ),
    }))
    .filter((section) => section.items.length > 0);
}
