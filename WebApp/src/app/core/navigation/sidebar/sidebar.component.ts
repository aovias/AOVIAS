import {
  Component,
  ChangeDetectionStrategy,
  OnInit,
  OnDestroy,
  DestroyRef,
  signal,
  inject,
  effect,
  HostListener,
  PLATFORM_ID,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  NavigationEnd,
  IsActiveMatchOptions,
} from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatRippleModule } from '@angular/material/core';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { filter } from 'rxjs';
import { OrganizationService } from '../../services/organization.service';
import { ContextFrameworkDisplayService } from '../../services/context-framework-display.service';
import { ComplianceContextService } from '../../services/compliance-context.service';
import { AppContextService } from '../../services/app-context.service';
import {
  HospitalSelectorDialogComponent,
  HospitalSelectorDialogData,
  HospitalFrameworkSelection,
  FrameworkSelectorItem,
} from '../../../shared/ui/hospital-selector/hospital-selector-dialog.component';
import { sortByCpFrameworkCode } from '../../../shared/utils/framework-code-sort';
import { ShellSidebarService } from '../../layout/shell-sidebar.service';
import {
  ShellOperationalModeService,
  OperationalMode,
} from '../../layout/shell-operational-mode.service';
import { isJourneyShellNavPath, SidebarMenuItem, BadgeVariant } from './sidebar.models';
import { resolveNavBadge } from '../../layout/sidebar-badges.map';
import { sidebarIcon } from '../../layout/sidebar-icons.registry';
import { TrustHtmlPipe } from '../../layout/trust-html.pipe';
import { SidebarAiAssistantComponent } from '../../layout/sidebar-ai-assistant.component';
import { UserContextService } from '../../services/user-context.service';
import { FavoritesRecentService } from '../../services/favorites-recent.service';
import { normalizeRoleId } from '../../auth/roles/role-definitions';
import { NavigationHistoryService } from '../engine/navigation-history.service';
import { NavigationQueryService } from '../engine/navigation-query.service';
import { NavigationBadgeStore } from '../engine/providers/badge.provider';
import { FavoritesProvider } from '../surfaces/favorites.provider';
import { SidebarService } from './sidebar.service';
import { FavoritesService } from '../favorites/favorites.service';
import { PinnedService } from '../favorites/pinned.service';
import { PersonalizationService } from '../personalization/personalization.service';
import { WorkspacePreferencesService } from '../personalization/workspace-preferences.service';
import { FavoritesSectionComponent } from './favorites-section.component';
import { PinnedSectionComponent } from './pinned-section.component';
import { SidebarGroupComponent } from './sidebar-group.component';
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatTooltipModule,
    MatRippleModule,
    CdkTrapFocus,
    TrustHtmlPipe,
    SidebarAiAssistantComponent,
    FavoritesSectionComponent,
    PinnedSectionComponent,
    SidebarGroupComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.sidebar-host--collapsed]': 'shellSidebar.labelsHidden()',
    '[class.sidebar-host--peek]': 'shellSidebar.isSidebarPeekExpanded()',
    '[class.sidebar-host--compact]': 'navPrefs.compactMode()',
    '[class.sidebar-host--no-motion]': '!navPrefs.animations()',
    '(mouseenter)': 'onSidebarPointerEnter()',
    '(mouseleave)': 'onSidebarPointerLeave()',
  },
  template: `
    @if (shellSidebar.isOverlayMode() && shellSidebar.isOpen()) {
      <div class="sidebar-backdrop" (click)="shellSidebar.close()" aria-hidden="true"></div>
    }
    <aside
      id="app-sidebar-nav"
      class="sidebar"
      [class.open]="shellSidebar.isOpen()"
      cdkTrapFocus
      [cdkTrapFocusAutoCapture]="shellSidebar.isOverlayMode() && shellSidebar.isOpen()"
      aria-label="Main navigation"
    >
      <div class="sidebar-inner">
        <div class="sidebar-sticky">
          <!-- Mode toggle: Navigation | AI Assistant -->
          <div class="sidebar-mode-toggle" role="tablist" aria-label="Sidebar view">
            <button
              role="tab"
              type="button"
              class="sidebar-mode-btn"
              [class.sidebar-mode-btn--active]="navSidebar.viewMode() === 'nav'"
              [attr.aria-selected]="navSidebar.viewMode() === 'nav'"
              (click)="navSidebar.setViewMode('nav')"
              matRipple
              matTooltip="Navigation"
              matTooltipPosition="right"
              [matTooltipDisabled]="navTooltipDisabled()"
              aria-label="Navigation menu"
            >
              <span class="sidebar-mode-icon" aria-hidden="true">
                <svg
                  width="14"
                  height="14"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                >
                  <line x1="3" y1="6" x2="21" y2="6" />
                  <line x1="3" y1="12" x2="21" y2="12" />
                  <line x1="3" y1="18" x2="21" y2="18" />
                </svg>
              </span>
              <span class="sidebar-mode-label">Menu</span>
            </button>
            <button
              role="tab"
              type="button"
              class="sidebar-mode-btn sidebar-mode-btn--assistant"
              [class.sidebar-mode-btn--active]="navSidebar.viewMode() === 'assistant'"
              [attr.aria-selected]="navSidebar.viewMode() === 'assistant'"
              (click)="navSidebar.setViewMode('assistant')"
              matRipple
              matTooltip="AI Assistant"
              matTooltipPosition="right"
              [matTooltipDisabled]="navTooltipDisabled()"
              aria-label="AI Assistant"
            >
              <span class="assistant-identity-toggle">
                <img
                  class="assistant-identity-avatar"
                  src="/assets/images/ai-assistant-icon.png"
                  alt=""
                  aria-hidden="true"
                />
                <span class="assistant-identity-meta">
                  <span class="assistant-identity-name">AI Assistant</span>
                  <span class="assistant-identity-status">
                    <span class="assistant-identity-dot" aria-hidden="true"></span>
                    <span class="assistant-identity-status-text">Online</span>
                  </span>
                </span>
              </span>
            </button>
          </div>

          <!-- context-panel-shell: disabled for now — set showContextPanelShell = true in SidebarComponent to restore -->
          @if (showContextPanelShell) {
            <div class="context-panel-shell">
              <div class="context-panel">
                <div class="context-mode">
                  <label class="context-mode-label" for="sidebar-operational-mode"
                    >Operational mode</label
                  >
                  <select
                    id="sidebar-operational-mode"
                    class="context-mode-select"
                    [value]="opMode.mode()"
                    (change)="onOperationalModeChange($event)"
                  >
                    @for (m of operationalModeOptions; track m) {
                      <option [value]="m">{{ opMode.label(m) }}</option>
                    }
                  </select>
                </div>

                <div class="context-progress" role="status" aria-live="polite">
                  <span class="context-progress-label">Compliance progress</span>
                  @if (contextProgressAvailable()) {
                    <div class="context-progress-bar" aria-hidden="true">
                      <div
                        class="context-progress-fill"
                        [style.width.%]="contextProgressPercent()"
                      ></div>
                    </div>
                  } @else {
                    <span class="context-progress-muted">Progress unavailable</span>
                  }
                </div>
              </div>

              <div class="quick-actions" role="toolbar" aria-label="Quick actions">
                <a
                  class="quick-action"
                  routerLink="/app/tasks"
                  routerLinkActive="quick-action--active"
                  matRipple
                  matTooltip="Add or view tasks"
                  matTooltipPosition="right"
                  [matTooltipDisabled]="navTooltipDisabled()"
                  aria-label="Tasks"
                >
                  <span
                    class="quick-action-icon"
                    [innerHTML]="quickActionIcons.task | trustHtml"
                  ></span>
                  <span class="quick-action-label">Task</span>
                </a>
                <a
                  class="quick-action"
                  routerLink="/app/evidence"
                  routerLinkActive="quick-action--active"
                  matRipple
                  matTooltip="Upload or browse evidence"
                  matTooltipPosition="right"
                  [matTooltipDisabled]="navTooltipDisabled()"
                  aria-label="Evidence"
                >
                  <span
                    class="quick-action-icon"
                    [innerHTML]="quickActionIcons.evidence | trustHtml"
                  ></span>
                  <span class="quick-action-label">Evidence</span>
                </a>
                <a
                  class="quick-action"
                  routerLink="/app/audit/findings"
                  routerLinkActive="quick-action--active"
                  matRipple
                  matTooltip="Record a finding"
                  matTooltipPosition="right"
                  [matTooltipDisabled]="navTooltipDisabled()"
                  aria-label="Findings"
                >
                  <span
                    class="quick-action-icon"
                    [innerHTML]="quickActionIcons.finding | trustHtml"
                  ></span>
                  <span class="quick-action-label">Finding</span>
                </a>
              </div>
            </div>
          }
        </div>

        <!-- AI Assistant mode: full-height panel, shown only when assistant mode is active -->
        @if (navSidebar.viewMode() === 'assistant') {
          <div class="sidebar-ai-section">
            <app-sidebar-ai-assistant />
          </div>
        }

        @if (showSidebarApplicationNav && navSidebar.viewMode() === 'nav') {
          <div class="context-separator"></div>

          <div class="sidebar-scroll">
            <nav class="sidebar-nav" aria-label="Application">
              @if (navPrefs.showPinned()) {
                <app-sidebar-pinned-section
                  [pinned]="pinnedService.pinned()"
                  [tooltipDisabled]="navTooltipDisabled()"
                  (unpin)="unpinPinned($event.route)"
                />
              }
              @if (navPrefs.showFavorites()) {
                <app-sidebar-favorites-section
                  [favorites]="favoritesProvider.favorites()"
                  [tooltipDisabled]="navTooltipDisabled()"
                  (unpin)="unpinFavorite($event.route, $event.label)"
                  (moveUp)="moveFavorite($event.route, -1)"
                  (moveDown)="moveFavorite($event.route, 1)"
                />
              }
              @for (section of navSidebar.menuSections(); track section.id) {
                <app-sidebar-group
                  [section]="section"
                  [expandedLabel]="expandedItem()"
                  [tooltipDisabled]="navTooltipDisabled()"
                  [journeyActive]="journeyShellNavActiveSig()"
                  [exactActiveLinkOptions]="exactActiveLinkOptions"
                  [subsetActiveLinkOptions]="subsetActiveLinkOptions"
                  [resolveBadge]="actionBadgeFn"
                  [isItemDisabled]="isNavDisabledFn"
                  [itemDisabledReason]="navDisabledReasonFn"
                  (toggleExpand)="toggleExpanded($event)"
                />
              }
            </nav>
          </div>
        }
      </div>
    </aside>
  `,
  styles: [
    `
      :host {
        display: block;
        flex: 0 0 var(--sidebar-width-expanded, 240px);
        width: var(--sidebar-width-expanded, 240px);
        min-width: var(--sidebar-width-expanded, 240px);
        min-height: 0;
        align-self: stretch;
        transition:
          width var(--sidebar-motion-duration, 180ms) ease,
          min-width var(--sidebar-motion-duration, 180ms) ease,
          flex-basis var(--sidebar-motion-duration, 180ms) ease;
      }

      .sidebar-backdrop {
        display: none;
      }

      @media (max-width: 1023px) {
        /* Overlay/drawer stacking: above sticky headers, below modals */
        .sidebar-backdrop {
          display: block;
          position: fixed;
          top: 64px;
          left: 0;
          right: 0;
          bottom: 48px;
          z-index: var(--z-shell-sidebar-backdrop);
          background: var(--overlay-backdrop);
          animation: sidebarBackdropIn var(--sidebar-motion-duration, 180ms) ease;
        }
      }

      @keyframes sidebarBackdropIn {
        from {
          opacity: 0;
        }
        to {
          opacity: 1;
        }
      }

      .sidebar {
        width: var(--sidebar-width-expanded, 240px);
        height: 100%;
        min-height: 0;
        background: linear-gradient(180deg, var(--color-bg-page) 0%, var(--sidebar-bg) 100%);
        border-right: 1px solid var(--color-border-subtle);
        overflow: hidden;
        transition:
          background 0.3s ease,
          border-color 0.3s ease,
          transform var(--sidebar-motion-duration, 180ms) ease,
          width var(--sidebar-motion-duration, 180ms) ease;
        display: flex;
        flex-direction: column;
        position: relative;
        z-index: var(--z-shell-sidebar);
      }

      .sidebar-inner {
        flex: 1;
        min-height: 0;
        display: flex;
        flex-direction: column;
      }

      .sidebar-sticky {
        flex-shrink: 0;
        z-index: 2;
        background: var(--sidebar-bg);
      }

      .sidebar-scroll {
        flex: 1 1 auto;
        min-height: 0;
        overflow-x: hidden;
        overflow-y: auto;
        overscroll-behavior: contain;
        scrollbar-width: thin;
        scrollbar-color: var(--scrollbar-thumb-panel) transparent;
        background: var(--header-bg);
      }

      .sidebar-sticky--footer {
        flex-shrink: 0;
        z-index: 2;
        padding: 10px 12px 14px;
        border-top: 1px solid var(--color-border-subtle);
        background: var(--sidebar-bg);
      }

      /* ──────── Sidebar mode toggle ──────── */

      .sidebar-mode-toggle {
        display: flex;
        flex-direction: row;
        align-items: stretch;
        gap: 4px;
        padding: 8px 12px 4px;
        flex-shrink: 0;
        min-width: 0;
      }

      .sidebar-mode-btn {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 4px;
        padding: 4px 8px;
        border-radius: 8px;
        border: 1px solid transparent;
        background: transparent;
        color: var(--color-text-secondary);
        font-size: 11px;
        font-weight: 600;
        cursor: pointer;
        min-height: 32px;
        position: relative;
        overflow: hidden;
        transition:
          background 0.15s ease,
          color 0.15s ease,
          border-color 0.15s ease;
        white-space: nowrap;
      }

      /* Keep Menu compact so AI Assistant identity stays visible. */
      .sidebar-mode-btn:not(.sidebar-mode-btn--assistant) {
        flex: 0 0 auto;
        max-width: 72px;
        padding: 4px 6px;
      }

      .sidebar-mode-btn:hover {
        background: var(--color-bg-hover);
        color: var(--color-text-primary);
      }

      .sidebar-mode-btn:focus-visible {
        outline: 2px solid var(--color-brand-primary);
        outline-offset: 2px;
      }

      .sidebar-mode-btn--active {
        background: color-mix(in srgb, var(--color-brand-primary) 12%, transparent);
        color: var(--color-brand-primary);
        border-color: color-mix(in srgb, var(--color-brand-primary) 35%, transparent);
      }

      .sidebar-mode-icon {
        width: 14px;
        height: 14px;
        flex-shrink: 0;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      :host ::ng-deep .sidebar-mode-icon svg {
        display: block;
        width: 14px;
        height: 14px;
        color: inherit;
      }

      .sidebar-mode-label {
        font-size: 11px;
        font-weight: 600;
        line-height: 1;
      }

      /* AI Assistant toggle styled as the assistant identity (avatar + name + status). */
      .sidebar-mode-btn--assistant {
        flex: 1 1 auto;
        min-width: 0;
        gap: 0;
        padding: 4px 8px;
        min-height: 32px;
        justify-content: flex-start;
      }

      .assistant-identity-toggle {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        min-width: 0;
        max-width: 100%;
        overflow: hidden;
      }

      .assistant-identity-avatar {
        width: 22px;
        height: 22px;
        border-radius: 6px;
        object-fit: cover;
        flex-shrink: 0;
        border: 1px solid color-mix(in srgb, var(--color-border-subtle) 70%, transparent);
        background: linear-gradient(
          135deg,
          var(--color-brand-primary-hover),
          var(--color-brand-primary)
        );
      }

      .assistant-identity-meta {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        justify-content: center;
        min-width: 0;
        gap: 1px;
      }

      .assistant-identity-name {
        font-size: 12px;
        font-weight: 600;
        line-height: 1.1;
        max-width: 100%;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        color: inherit;
      }

      .assistant-identity-status {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        font-size: 10px;
        font-weight: 500;
        line-height: 1;
        color: var(--color-text-muted);
      }

      .assistant-identity-dot {
        width: 6px;
        height: 6px;
        border-radius: 999px;
        background: var(--color-status-success);
        box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-status-success) 26%, transparent);
        flex-shrink: 0;
      }

      /* ──────── AI section (full height when assistant mode is active) ──────── */

      .sidebar-ai-section {
        flex: 1;
        min-height: 0;
        display: flex;
        flex-direction: column;
        overflow: hidden;
        padding: 0;
      }

      .context-panel-shell {
        padding: 0 12px 12px;
      }

      .context-panel {
        padding: 14px 12px 12px;
        border-radius: 12px;
        background: var(--color-bg-surface);
        border: 1px solid var(--color-border-subtle);
        box-shadow: var(--shadow-xs);
      }

      .context-mode {
        margin-top: 12px;
      }

      .context-mode-label {
        display: block;
        font-size: 10px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.7px;
        color: var(--color-text-muted);
        margin-bottom: 6px;
      }

      .context-mode-select {
        width: 100%;
        padding: 8px 10px;
        border-radius: 8px;
        border: 1px solid var(--color-border-subtle);
        background: var(--color-bg-surface-alt);
        color: var(--color-text-primary);
        font-size: 12px;
        font-weight: 500;
      }

      .context-progress {
        margin-top: 12px;
        display: flex;
        flex-direction: column;
        gap: 6px;
      }

      .context-progress-label {
        font-size: 10px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.7px;
        color: var(--color-text-muted);
      }

      .context-progress-muted {
        font-size: 12px;
        color: var(--color-text-muted);
      }

      .context-progress-bar {
        height: 6px;
        border-radius: 4px;
        background: color-mix(in srgb, var(--color-text-primary) 8%, transparent);
        overflow: hidden;
      }

      .context-progress-fill {
        height: 100%;
        border-radius: 4px;
        background: linear-gradient(
          90deg,
          var(--color-brand-primary-hover),
          var(--color-brand-primary)
        );
      }

      .quick-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        padding: 8px 4px 4px;
        justify-content: flex-start;
      }

      .quick-action {
        flex: 1 1 calc(33.33% - 8px);
        min-width: 72px;
        min-height: var(--touch-target-min);
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 4px;
        padding: 8px 6px;
        border-radius: 10px;
        border: 1px solid var(--color-border-subtle);
        background: color-mix(in srgb, var(--color-brand-primary) 6%, transparent);
        color: var(--color-text-primary);
        text-decoration: none;
        font-size: 10px;
        font-weight: 600;
        transition:
          background 0.2s ease,
          border-color 0.2s ease;
        position: relative;
        overflow: hidden;
        box-sizing: border-box;
      }

      .quick-action:hover {
        background: var(--color-bg-hover);
        border-color: var(--color-brand-primary);
      }

      .quick-action:focus-visible {
        outline: 2px solid var(--color-brand-primary);
        outline-offset: 2px;
      }

      .quick-action--active {
        border-color: var(--color-brand-primary);
        background: color-mix(in srgb, var(--color-brand-primary) 12%, transparent);
        color: var(--color-brand-primary);
      }

      .quick-action-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: var(--icon-size-nav);
        height: var(--icon-size-nav);
        color: inherit;
      }

      :host ::ng-deep .quick-action-icon svg {
        display: block;
        width: var(--icon-size-nav) !important;
        height: var(--icon-size-nav) !important;
        color: inherit;
      }

      .quick-action-label {
        line-height: 1.2;
        text-align: center;
      }

      .nav-badge {
        flex-shrink: 0;
        margin-left: auto;
        min-width: 18px;
        height: 18px;
        padding: 0 5px;
        border-radius: 9px;
        font-size: 10px;
        font-weight: 700;
        display: inline-flex;
        align-items: center;
        justify-content: center;
      }

      .nav-badge--neutral {
        background: color-mix(in srgb, var(--color-text-muted) 22%, transparent);
        color: var(--color-text-primary);
      }

      .nav-badge--warning {
        background: color-mix(in srgb, var(--color-status-warning) 28%, transparent);
        color: var(--color-status-warning);
      }

      .nav-badge--critical {
        background: color-mix(in srgb, var(--color-status-danger) 26%, transparent);
        color: var(--color-status-danger);
      }

      .menu-item--primary:not(.menu-item--disabled) {
        border: 1px solid color-mix(in srgb, var(--color-brand-primary) 35%, transparent);
        background: color-mix(in srgb, var(--color-brand-primary) 8%, transparent);
        font-weight: 600;
        color: var(--color-brand-primary);
      }

      .menu-item--primary:not(.menu-item--disabled) .menu-icon {
        color: var(--color-brand-primary);
      }

      .menu-item--disabled,
      .submenu-item--disabled {
        opacity: 0.45;
        cursor: not-allowed;
        pointer-events: none;
      }

      :host(.sidebar-host--compact) {
        --sidebar-item-height: 32px;
        --sidebar-section-gap: 10px;
        --sidebar-section-header-size: 11px;
      }

      :host(.sidebar-host--no-motion) .menu-item,
      :host(.sidebar-host--no-motion) .sidebar {
        transition: none !important;
      }

      :host(.sidebar-host--collapsed) {
        flex-basis: 72px;
        width: 72px;
        min-width: 72px;
      }

      :host(.sidebar-host--collapsed) .sidebar {
        width: 72px;
      }

      /* Collapsed: hide text (pierce into extracted nav child components). */
      :host(.sidebar-host--collapsed) .context-mode-label,
      :host(.sidebar-host--collapsed) .context-progress-label,
      :host(.sidebar-host--collapsed) .context-mode,
      :host(.sidebar-host--collapsed) .context-progress,
      :host(.sidebar-host--collapsed) .quick-action-label,
      :host(.sidebar-host--collapsed) .section-label,
      :host(.sidebar-host--collapsed) .menu-label,
      :host(.sidebar-host--collapsed) .expand-icon,
      :host(.sidebar-host--collapsed) .submenu-label,
      :host(.sidebar-host--collapsed) .submenu-chevron,
      :host(.sidebar-host--collapsed) .nav-badge,
      :host(.sidebar-host--collapsed) ::ng-deep .section-label,
      :host(.sidebar-host--collapsed) ::ng-deep .menu-label,
      :host(.sidebar-host--collapsed) ::ng-deep .expand-icon,
      :host(.sidebar-host--collapsed) ::ng-deep .section-header,
      :host(.sidebar-host--collapsed) ::ng-deep .section-divider,
      :host(.sidebar-host--collapsed) ::ng-deep .section-toggle,
      :host(.sidebar-host--collapsed) ::ng-deep .bookmark-unpin,
      :host(.sidebar-host--collapsed) ::ng-deep .nav-badge {
        display: none;
      }

      :host(.sidebar-host--collapsed) .submenu {
        display: none;
      }

      /* Rail is 72px: nested padding was squeezing inner width < 42px icons → overflow + clip looked like overlap. */
      :host(.sidebar-host--collapsed) .context-panel-shell {
        padding: 0 6px 8px;
      }

      :host(.sidebar-host--collapsed) .context-panel {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 10px;
        padding: 8px 6px 10px;
      }

      :host(.sidebar-host--collapsed) .quick-actions {
        flex-direction: column;
        align-items: stretch;
        padding: 6px 2px 2px;
        gap: 6px;
      }

      :host(.sidebar-host--collapsed) .quick-action {
        flex: 0 0 auto;
        min-width: 0;
        max-width: 100%;
        width: 100%;
        box-sizing: border-box;
        justify-content: center;
        padding: 8px 4px;
      }

      :host(.sidebar-host--collapsed) .sidebar-sticky--footer {
        padding: 8px 6px 10px;
      }

      /* Collapsed rail: mode toggle becomes vertical icon-only strip. */
      :host(.sidebar-host--collapsed) .sidebar-mode-toggle {
        flex-direction: column;
        padding: 2px 6px 6px;
        gap: 4px;
      }

      :host(.sidebar-host--collapsed) .sidebar-mode-btn {
        flex: 0 0 auto;
        gap: 0;
        padding: 8px 4px;
        justify-content: center;
      }

      :host(.sidebar-host--collapsed) .sidebar-mode-label {
        display: none;
      }

      :host(.sidebar-host--collapsed) .sidebar-mode-btn:not(.sidebar-mode-btn--assistant) {
        max-width: none;
        width: 100%;
      }

      :host(.sidebar-host--collapsed) .sidebar-mode-btn--assistant {
        min-height: var(--touch-target-min);
        padding: 6px 4px;
        max-width: none;
        width: 100%;
      }

      :host(.sidebar-host--collapsed) .assistant-identity-meta {
        display: none;
      }

      :host(.sidebar-host--collapsed) .assistant-identity-toggle {
        gap: 0;
        justify-content: center;
      }

      /* Collapsed rail: AI section shrinks to just the collapsed pill. */
      :host(.sidebar-host--collapsed) .sidebar-ai-section {
        flex: 0 0 auto;
        min-height: 0;
        max-height: none;
        padding: 0;
      }

      :host(.sidebar-host--collapsed) .sidebar-nav {
        gap: 8px;
        padding: 8px 0 16px;
      }

      :host(.sidebar-host--collapsed) .menu-section {
        padding: 0 6px;
      }

      :host(.sidebar-host--collapsed) .menu-section:not(:last-child)::after {
        display: none;
      }

      :host(.sidebar-host--collapsed) .context-separator {
        margin: 0 8px;
      }

      :host(.sidebar-host--collapsed) .menu-item,
      :host(.sidebar-host--collapsed) button.menu-item {
        justify-content: center;
        gap: 0;
        padding-left: 10px;
        padding-right: 10px;
      }

      :host(.sidebar-host--collapsed) .menu-icon {
        display: flex;
        flex-shrink: 0;
        margin: 0;
      }

      :host(.sidebar-host--collapsed) .sidebar-scroll .menu-item {
        min-height: var(--touch-target-min);
      }

      :host(.sidebar-host--collapsed) .submenu-item {
        justify-content: center;
        padding-left: 10px;
        padding-right: 10px;
      }

      :host(.sidebar-host--collapsed) .submenu-item::before {
        display: none;
      }

      @media (prefers-reduced-motion: reduce) {
        .sidebar,
        :host(.sidebar-host--collapsed) {
          transition: none;
        }
      }

      @media (max-width: 1023px) {
        .sidebar {
          position: fixed;
          top: 64px;
          left: 0;
          bottom: 48px;
          height: auto;
          width: min(var(--sidebar-width-expanded, 240px), calc(100vw - 16px));
          max-width: 100vw;
          transform: translateX(-100%);
          border-right: 1px solid var(--color-border-subtle);
          box-shadow: var(--shadow-elevated-panel);
        }

        :host(.sidebar-host--collapsed) .sidebar {
          width: min(var(--sidebar-width-expanded, 240px), calc(100vw - 16px));
        }

        .sidebar.open {
          transform: translateX(0);
        }
      }

      .sidebar-scroll::-webkit-scrollbar {
        width: 4px;
      }

      .sidebar-scroll::-webkit-scrollbar-track {
        background: transparent;
      }

      .sidebar-scroll::-webkit-scrollbar-thumb {
        background: var(--scrollbar-thumb-panel);
        border-radius: 2px;
      }

      .sidebar-scroll::-webkit-scrollbar-thumb:hover {
        background: var(--scrollbar-thumb-panel-hover);
      }

      /* ──────── Context rows (Hospital + Framework) ──────── */

      /* Hospital Row */
      .context-hospital {
        display: flex;
        align-items: flex-start;
        min-width: 0;
      }

      .context-hospital-content {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 2px;
        min-width: 0;
      }

      .context-hospital-name {
        font-size: 11px;
        font-weight: 500;
        color: var(--hospital-card-name-color);
        text-transform: uppercase;
        letter-spacing: 0.3px;
        white-space: normal;
        overflow-wrap: anywhere;
        word-wrap: break-word;
      }

      .context-hospital-org {
        font-size: 11px;
        font-weight: 500;
        color: var(--hospital-card-org-color);
        white-space: normal;
        overflow-wrap: anywhere;
        word-wrap: break-word;
      }

      .context-panel > .context-mode:first-child {
        margin-top: 0;
      }

      /* Separator */
      .context-separator {
        height: 1px;
        margin: 0 16px;
        background: var(--context-separator-color);
        flex-shrink: 0;
      }

      /* ──────── Sidebar Nav ──────── */
      .sidebar-nav {
        display: flex;
        flex-direction: column;
        gap: 5px;
        padding: 12px 0 16px;
      }

      .menu-section {
        display: flex;
        flex-direction: column;
        gap: 2px;
        padding: 0 12px;
        position: relative;
      }

      .menu-section:not(:last-child)::after {
        content: '';
        position: absolute;
        bottom: -12px;
        left: 16px;
        right: 16px;
        height: 1px;
        background: var(--color-border-subtle);
      }

      .section-label {
        font-size: 11px;
        font-weight: 600;
        color: var(--color-text-muted);
        text-transform: uppercase;
        letter-spacing: 0.8px;
        padding: 8px 16px 12px 16px;
        margin-bottom: 4px;
      }

      .menu-item-wrapper {
        display: flex;
        flex-direction: column;
      }

      .nav-bookmarks .bookmark-group-label {
        margin: 4px 16px 2px;
        font-size: 10px;
        font-weight: 600;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--color-text-muted, #9ca3af);
      }

      .bookmark-row {
        flex-direction: row;
        align-items: center;
        gap: 2px;
      }

      .bookmark-row .bookmark-link {
        flex: 1;
        min-width: 0;
      }

      .bookmark-unpin {
        flex-shrink: 0;
        width: 28px;
        height: 28px;
        margin-right: 6px;
        border: none;
        border-radius: 6px;
        background: transparent;
        color: var(--color-text-muted, #9ca3af);
        cursor: pointer;
        font-size: 14px;
        line-height: 1;
      }

      .bookmark-unpin:hover {
        background: var(--hover-bg, rgba(0, 0, 0, 0.04));
        color: var(--color-primary);
      }

      :host(.sidebar-host--collapsed) .bookmark-unpin,
      :host(.sidebar-host--collapsed) .bookmark-group-label {
        display: none;
      }

      .menu-item {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 11px 16px;
        background: transparent;
        border: none;
        border-radius: 10px;
        color: var(--color-text-secondary);
        font-size: 14px;
        font-weight: 500;
        text-decoration: none;
        cursor: pointer;
        transition:
          background var(--sidebar-motion-duration, 180ms) ease,
          color var(--sidebar-motion-duration, 180ms) ease,
          box-shadow var(--sidebar-motion-duration, 180ms) ease;
        position: relative;
        overflow: hidden;
        min-height: var(--sidebar-item-height, 36px);
        height: var(--sidebar-item-height, 36px);
        width: 100%;
        text-align: left;
        margin: 0;
        padding: 0 var(--sidebar-item-pad-x, 12px);
        gap: 10px;
        font-size: 13px;
        border-radius: 8px;
      }

      .menu-item:focus-visible {
        outline: 2px solid var(--color-brand-primary);
        outline-offset: 2px;
      }

      .menu-item:hover {
        background: var(--color-bg-hover);
        color: var(--color-text-primary);
      }

      .menu-item:hover .menu-icon {
        color: var(--color-brand-primary);
      }

      .menu-item.active {
        background: color-mix(in srgb, var(--color-brand-primary) 14%, transparent);
        color: var(--color-text-primary);
        font-weight: 650;
        box-shadow: none;
      }

      .menu-item.active::before {
        content: '';
        position: absolute;
        left: 0;
        top: 6px;
        bottom: 6px;
        width: 3px;
        height: auto;
        background: var(--color-brand-primary);
        border-radius: 0 2px 2px 0;
        box-shadow: none;
        transform: none;
      }

      .menu-item.active .menu-icon {
        color: var(--color-brand-primary);
      }

      .menu-item.active .menu-label {
        color: var(--color-text-primary);
        font-weight: 650;
      }

      .menu-item.expanded {
        color: var(--color-text-primary);
        background: color-mix(in srgb, var(--color-text-primary) 4%, transparent);
      }

      .menu-icon {
        width: var(--icon-size-nav);
        height: var(--icon-size-nav);
        min-width: var(--icon-size-nav);
        min-height: var(--icon-size-nav);
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        transition: color 0.2s ease;
      }

      .menu-icon svg {
        width: var(--icon-size-nav);
        height: var(--icon-size-nav);
      }

      /* innerHTML SVGs need ::ng-deep so dimensions/color apply; Tailwind svg height:auto can collapse icons when labels are hidden. */
      :host ::ng-deep .sidebar-scroll .menu-icon svg {
        display: block;
        width: var(--icon-size-nav) !important;
        height: var(--icon-size-nav) !important;
        min-width: var(--icon-size-nav);
        min-height: var(--icon-size-nav);
        flex-shrink: 0;
        color: inherit;
        vertical-align: middle;
      }

      :host ::ng-deep .sidebar-scroll .menu-item:hover .menu-icon svg {
        color: var(--color-brand-primary);
      }

      :host ::ng-deep .sidebar-scroll .menu-item.active .menu-icon svg {
        color: var(--color-brand-primary);
      }

      :host ::ng-deep .sidebar-scroll .menu-item--primary:not(.menu-item--disabled) .menu-icon svg {
        color: var(--color-brand-primary);
      }

      .menu-label {
        flex: 1;
        text-align: left;
      }

      .expand-icon {
        flex-shrink: 0;
        transition: transform 0.2s ease;
        opacity: 0.65;
        color: var(--color-text-muted);
      }

      .expand-icon.rotate {
        transform: rotate(180deg);
      }

      .submenu {
        display: flex;
        flex-direction: column;
        gap: 1px;
        padding: 6px 0 4px 0;
        animation: slideDown 0.2s ease;
      }

      @keyframes slideDown {
        from {
          opacity: 0;
          transform: translateY(-8px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      .submenu-item {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 10px;
        padding: 9px 16px 9px 44px;
        background: transparent;
        border-radius: 8px;
        color: var(--color-text-secondary);
        font-size: 13px;
        font-weight: 500;
        text-decoration: none;
        cursor: pointer;
        transition:
          background 0.2s ease,
          color 0.2s ease;
        position: relative;
        overflow: hidden;
        min-height: var(--touch-target-min);
      }

      .submenu-item::before {
        content: '';
        position: absolute;
        left: 28px;
        top: 50%;
        transform: translateY(-50%);
        width: 5px;
        height: 5px;
        background: var(--color-text-muted);
        border-radius: 50%;
        transition:
          background 0.2s ease,
          box-shadow 0.2s ease;
      }

      .submenu-item:hover {
        background: var(--color-bg-hover);
        color: var(--color-text-primary);
      }

      .submenu-item:hover::before {
        background: var(--color-text-secondary);
      }

      .submenu-item.active {
        background: color-mix(in srgb, var(--color-brand-primary) 14%, transparent);
        color: var(--color-brand-primary);
        font-weight: 600;
      }

      .submenu-item.active::before {
        background: var(--color-brand-primary);
        box-shadow: 0 0 6px color-mix(in srgb, var(--color-brand-primary) 60%, transparent);
      }

      .submenu-label {
        flex: 1;
      }

      .submenu-chevron {
        opacity: 0;
        color: var(--color-text-muted);
        transition:
          opacity 0.2s ease,
          color 0.2s ease;
      }

      .submenu-item:hover .submenu-chevron {
        opacity: 1;
        color: var(--color-text-secondary);
      }

      .submenu-item.active .submenu-chevron {
        opacity: 1;
        color: var(--color-brand-primary);
      }

      /*
     * NAV-1: ≥1024 inline/fixed rail; ≤1023 overlay/drawer (host reserves 0 width).
     * Desktop in-flow flex rail; laptop/tablet-desktop use fixed rail under measured header.
     */
      @media (min-width: 1024px) and (max-width: 1599px) {
        :host,
        :host(.sidebar-host--collapsed) {
          flex: 0 0 0;
          width: 0;
          min-width: 0;
          max-width: 0;
          overflow: visible;
        }

        :host .sidebar {
          position: fixed;
          top: var(--app-shell-header-height, 64px);
          left: 0;
          bottom: 48px;
          height: auto;
          z-index: var(--z-shell-sidebar, 930);
          width: var(--sidebar-width-expanded, 240px);
        }

        :host(.sidebar-host--collapsed) .sidebar {
          width: var(--sidebar-width-collapsed, 72px);
        }

        :host(.sidebar-host--peek) .sidebar {
          width: var(--sidebar-width-expanded, 240px);
          box-shadow: 4px 0 24px color-mix(in srgb, var(--color-text-primary) 12%, transparent);
        }
      }

      @media (max-width: 1023px) {
        /*
       * Overlay/drawer — host never reserves flex width.
       */
        :host,
        :host(.sidebar-host--collapsed) {
          flex: 0 0 0;
          width: 0;
          min-width: 0;
          max-width: 0;
          overflow: visible;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        :host,
        .sidebar {
          transition: none;
        }
      }
    `,
  ],
})
export class SidebarComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly organizationService = inject(OrganizationService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly appContext = inject(AppContextService);
  private readonly contextFrameworkDisplay = inject(ContextFrameworkDisplayService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);

  readonly shellSidebar = inject(ShellSidebarService);
  readonly navSidebar = inject(SidebarService);
  readonly opMode = inject(ShellOperationalModeService);

  readonly operationalModeOptions: OperationalMode[] = [
    'self-assessment',
    'internal-audit',
    'final-audit',
  ];

  /**
   * When true, renders `.context-panel-shell` (operational mode, compliance progress, quick actions).
   * Temporarily off — turn on to restore that block without re-adding markup.
   */
  readonly showContextPanelShell = false;

  /**
   * When true, renders `.context-separator`, `.sidebar-scroll`, and `<nav class="sidebar-nav">` (main app menu).
   * Set to `true` to restore sidebar navigation without deleting the template.
   */
  readonly showSidebarApplicationNav = true;

  readonly exactActiveLinkOptions: IsActiveMatchOptions = {
    paths: 'exact',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  };

  readonly subsetActiveLinkOptions: IsActiveMatchOptions = {
    paths: 'subset',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  };

  /** Journey nav row — updates on route change (OnPush). */
  readonly journeyShellNavActiveSig = signal(false);

  private readonly badgeStore = inject(NavigationBadgeStore);

  /** Compliance progress (optional — wire to backend when available). */
  readonly contextProgressAvailable = signal(false);
  readonly contextProgressPercent = signal(42);

  readonly expandedItem = signal<string | null>(null);
  readonly selectedHospitalName = signal<string>('Select Hospital');
  readonly selectedOrganizationName = signal<string>('Organization');

  private readonly userContext = inject(UserContextService);
  /** Exposed for template favorites / recent nav strip. */
  public readonly favoritesRecent = inject(FavoritesRecentService);
  readonly favoritesProvider = inject(FavoritesProvider);
  readonly pinnedService = inject(PinnedService);
  readonly navPrefs = inject(WorkspacePreferencesService);
  private readonly favoritesSvc = inject(FavoritesService);
  private readonly navHistory = inject(NavigationHistoryService);
  private readonly navQuery = inject(NavigationQueryService);
  private readonly personalization = inject(PersonalizationService);

  readonly actionBadgeFn = (item: SidebarMenuItem) => this.actionBadge(item);
  readonly isNavDisabledFn = (item: SidebarMenuItem) => this.isNavDisabled(item);
  readonly navDisabledReasonFn = (item: SidebarMenuItem) => this.navDisabledReason(item);

  /** Icon tiles for Task / Evidence / Finding (registry SVGs — same routes as main nav). */
  readonly quickActionIcons = {
    task: sidebarIcon('tasks'),
    evidence: sidebarIcon('evidence'),
    finding: sidebarIcon('findings'),
  };

  /** Flag to prevent multiple auto-popup triggers */
  private autoPopupFired = false;

  constructor() {
    this.navSidebar.setUserRoles(this.userContext.getUserRoles());

    this.userContext.roles$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((roles) => {
      const combined = roles.map((r) => normalizeRoleId(r) ?? r);
      this.navSidebar.setUserRoles(combined);
    });

    effect(() => {
      if (!isPlatformBrowser(this.platformId)) {
        return;
      }
      const lock = this.shellSidebar.isOverlayMode() && this.shellSidebar.isOpen();
      document.body.style.overflow = lock ? 'hidden' : '';
    });

    effect(() => {
      if (this.shellSidebar.labelsHidden()) {
        this.expandedItem.set(null);
      }
    });
  }

  onSidebarPointerEnter(): void {
    if (this.shellSidebar.isSidebarRailCollapsed() && !this.shellSidebar.isOverlayMode()) {
      this.navSidebar.setPeekExpanded(true);
    }
  }

  onSidebarPointerLeave(): void {
    this.navSidebar.setPeekExpanded(false);
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent): void {
    if (
      event.key !== 'Escape' ||
      !this.shellSidebar.isOverlayMode() ||
      !this.shellSidebar.isOpen()
    ) {
      return;
    }
    this.shellSidebar.close();
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      queueMicrotask(() => this.badgeStore.seedMocks());
    }
    this.initializeHospitalCard();
    this.subscribeToFrameworkChanges();
    this.journeyShellNavActiveSig.set(isJourneyShellNavPath(this.router.url));
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((e) => {
        this.shellSidebar.close();
        this.journeyShellNavActiveSig.set(isJourneyShellNavPath(this.router.url));
        const path = e.urlAfterRedirects.split(/[?#]/)[0] || e.urlAfterRedirects;
        const label = this.labelForRoute(path);
        const item = this.navQuery.findByRoute(path);
        this.navHistory.trackVisit({
          id: item?.id,
          label,
          route: path,
          moduleId: item?.moduleId,
        });
        if (item) {
          this.personalization.trackUsage({
            id: item.id,
            route: path,
            moduleId: item.moduleId,
          });
        }
        if (path.startsWith('/app/audit') || path.startsWith('/app/auditor')) {
          this.expandedItem.set('Audit');
        }
      });
  }

  ngOnDestroy(): void {
    if (isPlatformBrowser(this.platformId)) {
      document.body.style.overflow = '';
    }
  }

  unpinFavorite(route: string, label: string): void {
    if (this.favoritesSvc.isFavorite(route)) {
      this.favoritesSvc.toggle(route, label);
    }
  }

  moveFavorite(route: string, direction: -1 | 1): void {
    this.favoritesSvc.move(route, direction);
  }

  unpinPinned(route: string): void {
    this.pinnedService.toggle(route);
  }

  private labelForRoute(path: string): string {
    const fromRegistry = this.navQuery.resolveTitleLabel(path);
    if (fromRegistry) return fromRegistry;
    const last = path.split('/').filter(Boolean).pop() ?? path;
    return last.replace(/[-_]/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }

  toggleExpanded(label: string): void {
    this.expandedItem.update((current) => (current === label ? null : label));
  }

  /** Tooltips when nav labels are hidden (collapsed rail, not peeking). */
  navTooltipDisabled(): boolean {
    return this.navSidebar.navTooltipDisabled();
  }

  onOperationalModeChange(event: Event): void {
    const v = (event.target as HTMLSelectElement).value as OperationalMode;
    this.opMode.setMode(v);
  }

  isNavDisabled(item: SidebarMenuItem): boolean {
    if (item.disabled) {
      return true;
    }
    if (item.label === 'Final Audit' && this.opMode.mode() === 'self-assessment') {
      return true;
    }
    return false;
  }

  navDisabledReason(item: SidebarMenuItem): string {
    if (item.disabledReason) {
      return item.disabledReason;
    }
    if (item.label === 'Final Audit' && this.opMode.mode() === 'self-assessment') {
      return 'Switch operational mode to Internal Audit or Final Audit to use this entry.';
    }
    return '';
  }

  actionBadge(item: SidebarMenuItem): { count: number; variant: BadgeVariant } | null {
    if (!this.navPrefs.showBadges()) return null;
    return resolveNavBadge(item, {
      findings: this.badgeStore.findings(),
      issues: this.badgeStore.issues(),
      alerts: this.badgeStore.alerts(),
    });
  }

  formatBadgeCount(count: number): string {
    return count > 99 ? '99+' : String(count);
  }

  onChangeHospital(): void {
    const organization = this.organizationService.getOrganization();
    const hospitals = this.organizationService.getHospitals();
    const organizationName = organization?.organizationName ?? 'Organization';

    // Build per-hospital journey map
    const hospitalJourneys: Record<string, FrameworkSelectorItem[]> = {};
    for (const h of hospitals) {
      hospitalJourneys[h.id] = sortByCpFrameworkCode(
        (h.journey ?? []).map((j) => ({
          id: j.journeyId,
          name: j.frameworkName,
          code: j.frameworkCode,
        })),
      );
    }

    const dialogData: HospitalSelectorDialogData = {
      hospitals: hospitals.map((h) => ({
        id: h.id,
        name: h.name,
        organizationName: organizationName,
        isOnboarded: h.isOnboarded,
      })),
      hospitalJourneys,
      selectedHospitalId: this.complianceContext.getHospitalId(),
      selectedJourneyId: this.complianceContext.getJourneyId(),
    };

    const dialogRef = this.dialog.open(HospitalSelectorDialogComponent, {
      data: dialogData,
      panelClass: 'hospital-selector-panel',
      backdropClass: 'hospital-selector-backdrop',
      width: '520px',
      maxWidth: '90vw',
      maxHeight: '80vh',
      autoFocus: false,
    });

    dialogRef
      .afterClosed()
      .pipe(
        filter(
          (result): result is HospitalFrameworkSelection =>
            !!result && !!result.hospitalId && !!result.journeyId,
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        const journeyId = result.journeyId;
        const frameworkId = this.resolveFrameworkIdForJourney(result.hospitalId, journeyId);
        this.appContext.switchContext(result.hospitalId, journeyId, frameworkId);
        this.updateHospitalDisplay(result.hospitalId);
        this.updateFrameworkDisplay(journeyId);
      });
  }

  private resolveFrameworkIdForJourney(hospitalId: string, journeyId: string): string | null {
    return this.organizationService.getFrameworkIdForJourney(hospitalId, journeyId);
  }

  private initializeHospitalCard(): void {
    // Subscribe to hospital context changes to stay in sync
    this.complianceContext.hospitalId$
      .pipe(
        filter((id): id is string => !!id),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((hospitalId) => {
        this.updateHospitalDisplay(hospitalId);
      });

    // Load organization data if not already cached
    const cachedOrg = this.organizationService.getOrganization();
    if (cachedOrg) {
      this.applyInitialContext(cachedOrg.organizationName);
    } else {
      this.organizationService
        .loadOrganizationDetails()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (org) => {
            if (org) {
              this.applyInitialContext(org.organizationName);
            }
          },
        });
    }
  }

  private applyInitialContext(organizationName: string): void {
    this.selectedOrganizationName.set(organizationName);

    // Try restoring persisted selections from session
    const persistedHospitalId = this.appContext.getPersistedHospitalId();
    const persistedJourneyId = this.appContext.getPersistedJourneyId();

    const currentHospitalId = persistedHospitalId ?? this.complianceContext.getHospitalId();
    if (currentHospitalId) {
      this.complianceContext.setHospitalId(currentHospitalId);
      this.updateHospitalDisplay(currentHospitalId);
    } else {
      // No hospital set yet — apply default
      const defaultHospital = this.organizationService.getDefaultHospital();
      if (defaultHospital) {
        this.selectedHospitalName.set(defaultHospital.name);
        this.complianceContext.setHospitalId(defaultHospital.id);
      }
    }

    const currentJourneyId = persistedJourneyId ?? this.complianceContext.getJourneyId();
    if (currentJourneyId) {
      this.complianceContext.setJourneyId(currentJourneyId);
      this.updateFrameworkDisplay(currentJourneyId);

      // Backfill the framework id for sessions persisted before the two were separated, and once
      // organization detail has loaded (the journey list is what carries the mapping).
      if (!this.complianceContext.getFrameworkId() && currentHospitalId) {
        const frameworkId = this.resolveFrameworkIdForJourney(currentHospitalId, currentJourneyId);
        if (frameworkId) {
          this.complianceContext.setFrameworkId(frameworkId);
        }
      }
    }

    // Auto-show popup if no persisted selection exists (first login)
    if (!this.autoPopupFired && (!persistedHospitalId || !persistedJourneyId)) {
      this.autoPopupFired = true;
      setTimeout(() => this.onChangeHospital(), 400);
    }
  }

  private updateHospitalDisplay(hospitalId: string): void {
    const hospitals = this.organizationService.getHospitals();
    const hospital = hospitals.find((h) => h.id === hospitalId);
    if (hospital) {
      this.selectedHospitalName.set(hospital.name);
    }

    const org = this.organizationService.getOrganization();
    if (org) {
      this.selectedOrganizationName.set(org.organizationName);
    }
  }

  private updateFrameworkDisplay(frameworkId: string): void {
    this.contextFrameworkDisplay.updateFromFrameworkId(frameworkId);
  }

  private subscribeToFrameworkChanges(): void {
    this.complianceContext.frameworkId$
      .pipe(
        filter((id): id is string => !!id),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((frameworkId) => {
        this.updateFrameworkDisplay(frameworkId);
      });
  }
}
