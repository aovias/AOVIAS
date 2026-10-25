import { Injectable, computed, inject } from '@angular/core';
import { ShellSidebarService } from '../../layout/shell-sidebar.service';
import { NavigationQueryService } from '../engine/navigation-query.service';
import { NavigationStateService } from '../engine/navigation-state.service';
import { SidebarProvider } from '../surfaces/sidebar.provider';
import type { SidebarMenuSection } from './sidebar.models';
import type { SidebarViewMode } from './sidebar.state';

/**
 * Facade over shell rail layout + NAV-1 UI state (peek, recent, view mode).
 * Menu tree comes from NavigationQuery / SidebarProvider (NAV-3).
 */
@Injectable({ providedIn: 'root' })
export class SidebarService {
  private readonly shell = inject(ShellSidebarService);
  private readonly query = inject(NavigationQueryService);
  private readonly navState = inject(NavigationStateService);
  private readonly sidebarProvider = inject(SidebarProvider);

  readonly recentExpanded = this.navState.recentExpanded;
  readonly viewMode = this.navState.viewMode;

  readonly menuSections = computed(
    () => this.sidebarProvider.sections() as unknown as SidebarMenuSection[],
  );

  readonly labelsHidden = this.shell.labelsHidden;
  readonly isRailCollapsed = this.shell.isSidebarRailCollapsed;
  readonly isPeekExpanded = this.shell.isSidebarPeekExpanded;
  readonly isOverlayMode = this.shell.isOverlayMode;
  readonly columnPx = this.shell.shellSidebarColumnPx;

  setUserRoles(roles: string[]): void {
    this.query.setUserRoles(roles);
  }

  setViewMode(mode: SidebarViewMode): void {
    this.navState.setViewMode(mode);
  }

  toggleRecentExpanded(): void {
    this.navState.toggleRecentExpanded();
  }

  setRecentExpanded(expanded: boolean): void {
    this.navState.setRecentExpanded(expanded);
  }

  toggleRailCollapsed(): void {
    this.shell.toggleSidebarRailCollapsed();
  }

  setPeekExpanded(peek: boolean): void {
    this.shell.setPeekExpanded(peek);
  }

  /** Tooltips when labels are hidden (collapsed rail, not peeking, not overlay). */
  navTooltipDisabled(): boolean {
    return !this.labelsHidden();
  }

  /** Exposed for tests / future workspace switching. */
  resolveMenuSections(roles: string[]): SidebarMenuSection[] {
    this.query.setUserRoles(roles);
    return this.menuSections();
  }
}
