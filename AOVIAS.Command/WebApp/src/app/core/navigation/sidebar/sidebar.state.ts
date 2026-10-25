/**
 * Sidebar UI state keys and helpers (NAV-1).
 * Intentional collapse persists via ShellSidebarService; peek and Recent expansion live here.
 */

export const SIDEBAR_RECENT_EXPANDED_STORAGE_KEY = 'praman.nav.recentExpanded.v1';
export const SIDEBAR_MODE_STORAGE_KEY = 'praman-sidebar-mode';

export type SidebarViewMode = 'nav' | 'assistant';

export interface SidebarUiStateSnapshot {
  /** Intentionally collapsed rail (persisted by ShellSidebarService). */
  railCollapsed: boolean;
  /** Temporary hover expand — never persisted. */
  peekExpanded: boolean;
  /** Recent section open/closed — persisted. */
  recentExpanded: boolean;
  viewMode: SidebarViewMode;
}

export function readRecentExpanded(storage: Storage | null, fallback = true): boolean {
  if (!storage) return fallback;
  const v = storage.getItem(SIDEBAR_RECENT_EXPANDED_STORAGE_KEY);
  if (v === '0') return false;
  if (v === '1') return true;
  return fallback;
}

export function writeRecentExpanded(storage: Storage | null, expanded: boolean): void {
  storage?.setItem(SIDEBAR_RECENT_EXPANDED_STORAGE_KEY, expanded ? '1' : '0');
}

export function readSidebarViewMode(
  storage: Storage | null,
  fallback: SidebarViewMode = 'nav',
): SidebarViewMode {
  if (!storage) return fallback;
  const v = storage.getItem(SIDEBAR_MODE_STORAGE_KEY);
  return v === 'assistant' || v === 'nav' ? v : fallback;
}

export function writeSidebarViewMode(storage: Storage | null, mode: SidebarViewMode): void {
  storage?.setItem(SIDEBAR_MODE_STORAGE_KEY, mode);
}
