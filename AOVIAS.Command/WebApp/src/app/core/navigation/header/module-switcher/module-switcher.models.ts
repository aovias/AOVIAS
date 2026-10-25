import type {
  HeaderNavGroup,
  HeaderNavItem,
  NavItemResolver,
  NavResolveContext,
} from '../../sidebar/sidebar.models';

/**
 * Module Switcher entry (NAV-2).
 * Extension fields support NAV-3 favorites/pinning/permissions — unused in NAV-2.
 */
export interface ModuleEntry {
  id: string;
  title: string;
  icon: string;
  /** Primary navigation target for the module. */
  route: string;
  category?: string;
  items: HeaderNavItem[];
  disabled?: boolean;
  /** Extension: pin / favorite / recent (not implemented in NAV-2). */
  pinned?: boolean;
  favorite?: boolean;
  recentRank?: number;
  visibilityFilter?: NavItemResolver;
}

export interface ModuleSwitcherState {
  open: boolean;
  query: string;
  activeIndex: number;
}

export type { HeaderNavGroup, HeaderNavItem, NavResolveContext };
