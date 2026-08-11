import type { SidebarSemanticIconId } from '../../layout/sidebar-icons.registry';
import type { NavigationBadgeKey } from './badge.model';
import type { NavigationFeatureKey } from './feature.model';
import type { NavigationPermissionKey } from './permission.model';

export type NavigationSurface = 'sidebar' | 'header' | 'bottom' | 'search' | 'breadcrumb';

export interface NavigationCategory {
  readonly id: string;
  readonly title: string;
  readonly order: number;
  readonly surface: NavigationSurface;
}

export interface NavigationItem {
  readonly id: string;
  readonly title: string;
  readonly description?: string;
  readonly icon: SidebarSemanticIconId;
  readonly moduleId: string;
  readonly parentId?: string;
  readonly route?: string;
  readonly order: number;
  readonly categoryId: string;
  readonly featureKey?: NavigationFeatureKey;
  readonly badgeKey?: NavigationBadgeKey;
  readonly permissionKeys?: readonly NavigationPermissionKey[];
  readonly roleKeys?: readonly string[];
  readonly surfaces: readonly NavigationSurface[];
  readonly visible: boolean;
  readonly enabled: boolean;
  readonly searchable: boolean;
  readonly favoriteAllowed: boolean;
  readonly recentTracking: boolean;
  readonly breadcrumb: boolean;
  readonly subsetActive?: boolean;
  readonly exactActive?: boolean;
  readonly primary?: boolean;
  readonly disabled?: boolean;
  readonly disabledReason?: string;
  readonly tooltip?: string;
}

export interface NavigationContext {
  readonly roles: readonly string[];
  readonly permissions?: readonly string[];
  readonly featureOverrides?: Readonly<Partial<Record<NavigationFeatureKey, boolean>>>;
  readonly workspaceId?: string | null;
}

export interface NavigationTreeNode extends NavigationItem {
  readonly children: readonly NavigationTreeNode[];
}

export interface NavigationSidebarSection {
  readonly id: string;
  readonly title: string;
  readonly order: number;
  readonly items: readonly NavigationTreeNode[];
}

export interface NavigationHeaderModule {
  readonly id: string;
  readonly title: string;
  readonly icon: SidebarSemanticIconId;
  readonly route: string;
  readonly order: number;
  readonly disabled: boolean;
  readonly items: readonly NavigationItem[];
}

export interface NavigationBottomItem {
  readonly id: string;
  readonly title: string;
  readonly route: string;
  readonly icon: SidebarSemanticIconId;
  readonly order: number;
}

export interface NavigationBreadcrumbItem {
  readonly id: string;
  readonly title: string;
  readonly route?: string;
}

export interface NavigationExtension {
  readonly modules?: readonly import('./module.model').NavigationModule[];
  readonly items?: readonly NavigationItem[];
  readonly categories?: readonly NavigationCategory[];
}

/** Resolved presentation row for sidebar (SVG icon string). */
export interface SidebarViewItem {
  id: string;
  label: string;
  icon: string;
  route: string;
  primary?: boolean;
  children?: SidebarViewItem[];
  disabled?: boolean;
  disabledReason?: string;
  tooltip?: string;
  ariaLabel?: string;
  navBadgeKey?: NavigationBadgeKey;
  subsetActive?: boolean;
  requiredRoles?: string[];
}

export interface SidebarViewSection {
  id: string;
  title: string;
  items: SidebarViewItem[];
}
