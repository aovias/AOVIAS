import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs/operators';

import { MatIconModule } from '@angular/material/icon';

import { HeaderComponent } from '../../../core/layout/header.component';
import { ContextDrawerComponent } from '../../../shared/ui/context-drawer/context-drawer.component';
import { ContextDrawerService } from '../../../shared/ui/context-drawer/context-drawer.service';

import { getAdministrationTabs, resolveAdministrationTab } from '../models/settings-nav.model';
import { getAdministrationEnvironmentLabel } from '../utils/administration-environment.util';

const CONTEXT_SECTION_ID = 'administration-configuration';

/**
 * Enterprise Administration Workspace shell — Stage 5A.
 *
 * One workspace, twelve tabs. The tab strip selects a domain; the aside lists that
 * domain's pages. A tab never brings its own sidebar — that is why the per-feature
 * admin navs (AI, Notifications, Workforce, Integrations) are redundant here.
 */
@Component({
  selector: 'app-administration-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatIconModule,
    HeaderComponent,
    ContextDrawerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="admin-shell">
      <app-header [administrationMode]="true" />

      <div class="admin-shell__context" role="region" aria-label="Administration context">
        <div class="admin-shell__context-left">
          <button
            type="button"
            class="admin-shell__nav-toggle"
            (click)="toggleMobileNav()"
            [attr.aria-expanded]="mobileNavOpen()"
            aria-controls="administration-nav"
            aria-label="Toggle administration navigation"
          >
            <mat-icon aria-hidden="true">menu</mat-icon>
          </button>
          <a
            routerLink="/app/dashboard"
            class="admin-shell__back"
            aria-label="Back to AOVIAS Command"
          >
            <mat-icon aria-hidden="true">arrow_back</mat-icon>
            <span>Back to AOVIAS Command</span>
          </a>
          <span class="admin-shell__context-divider" aria-hidden="true"></span>
          <nav class="admin-shell__breadcrumbs" aria-label="Breadcrumb">
            <ol>
              <li><a routerLink="/app/settings">Administration</a></li>
              @if (activeTab(); as tab) {
                <li>
                  <mat-icon aria-hidden="true">chevron_right</mat-icon>
                  <a [routerLink]="tab.route">{{ tab.label }}</a>
                </li>
              }
              @if (activeItemLabel(); as itemLabel) {
                <li aria-current="page">
                  <mat-icon aria-hidden="true">chevron_right</mat-icon>
                  <span>{{ itemLabel }}</span>
                </li>
              }
            </ol>
          </nav>
        </div>
        <div class="admin-shell__context-right">
          <button
            type="button"
            class="admin-shell__drawer-toggle"
            (click)="toggleContextDrawer()"
            [attr.aria-pressed]="contextDrawer.isOpen()"
            aria-label="Toggle context drawer"
          >
            <mat-icon aria-hidden="true">info</mat-icon>
            <span>Context</span>
          </button>
          <span class="admin-shell__env" [attr.data-env]="environmentLabel">{{
            environmentLabel
          }}</span>
        </div>
      </div>

      <div
        class="admin-shell__tabs-strip"
        role="navigation"
        aria-label="Administration workspace tabs"
      >
        <div class="admin-shell__tabs praman-scrollbar praman-scrollbar--dense">
          @for (tab of tabs; track tab.id) {
            <a
              class="admin-shell__tab"
              [class.admin-shell__tab--active]="activeTab()?.id === tab.id"
              [routerLink]="tab.route"
              [attr.aria-current]="activeTab()?.id === tab.id ? 'page' : null"
              (click)="closeMobileNav()"
            >
              <mat-icon aria-hidden="true">{{ tab.icon }}</mat-icon>
              <span>{{ tab.label }}</span>
            </a>
          }
        </div>
      </div>

      <div class="admin-shell__body">
        <aside
          id="administration-nav"
          class="admin-shell__nav"
          [class.admin-shell__nav--open]="mobileNavOpen()"
          [attr.aria-label]="(activeTab()?.label || 'Administration') + ' pages'"
        >
          @if (activeTab(); as tab) {
            <p class="admin-shell__section-title">{{ tab.label }}</p>
            @for (item of tab.items; track item.route) {
              <a
                class="admin-shell__link"
                [class.admin-shell__link--soon]="item.comingSoon"
                [routerLink]="item.route"
                routerLinkActive="admin-shell__link--active"
                [routerLinkActiveOptions]="{
                  paths: item.exactMatch ? 'exact' : 'subset',
                  queryParams: 'ignored',
                  fragment: 'ignored',
                  matrixParams: 'ignored',
                }"
                (click)="closeMobileNav()"
              >
                <mat-icon aria-hidden="true">{{ item.icon }}</mat-icon>
                <span class="admin-shell__link-label">{{ item.label }}</span>
                @if (item.comingSoon) {
                  <mat-icon class="admin-shell__soon-icon" aria-hidden="true" title="Coming soon">
                    schedule
                  </mat-icon>
                }
              </a>
            }
          }
        </aside>

        @if (mobileNavOpen()) {
          <button
            type="button"
            class="admin-shell__backdrop"
            aria-label="Close navigation"
            (click)="closeMobileNav()"
          ></button>
        }

        <main class="admin-shell__content">
          <div class="admin-shell__content-inner">
            <router-outlet />
          </div>
        </main>
      </div>

      <app-context-drawer />
    </div>
  `,
  styles: [
    `
      .admin-shell {
        min-height: 100vh;
        min-height: 100dvh;
        display: flex;
        flex-direction: column;
        background: var(--color-bg-canvas, var(--bg-color, #f8fafc));
      }

      .admin-shell__context {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        padding: 10px 24px;
        border-bottom: 1px solid var(--color-border-subtle, var(--border-color, #e5e7eb));
        background: var(--color-bg-surface, var(--card-bg, #fff));
      }

      .admin-shell__context-left {
        display: flex;
        align-items: center;
        gap: 12px;
        min-width: 0;
      }

      .admin-shell__context-right {
        display: flex;
        align-items: center;
        gap: 10px;
        flex-shrink: 0;
      }

      .admin-shell__nav-toggle {
        display: none;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        border-radius: 8px;
        background: transparent;
        color: var(--color-text-secondary, #6b7280);
        cursor: pointer;
        transition:
          background 0.15s ease,
          color 0.15s ease;
      }

      .admin-shell__nav-toggle:hover {
        background: var(--color-bg-muted, #f3f4f6);
        color: var(--color-text-primary, #111827);
      }

      .admin-shell__nav-toggle:focus-visible,
      .admin-shell__back:focus-visible,
      .admin-shell__drawer-toggle:focus-visible,
      .admin-shell__tab:focus-visible,
      .admin-shell__link:focus-visible {
        outline: 2px solid var(--color-brand-primary, var(--color-primary));
        outline-offset: 2px;
      }

      .admin-shell__back {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 6px 10px;
        border-radius: 8px;
        font-size: 13px;
        font-weight: 500;
        color: var(--color-text-secondary, #6b7280);
        text-decoration: none;
        transition:
          background 0.15s ease,
          color 0.15s ease;
        white-space: nowrap;
      }

      .admin-shell__back:hover {
        background: var(--color-bg-muted, #f3f4f6);
        color: var(--color-text-primary, #111827);
      }

      .admin-shell__back mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }

      .admin-shell__context-divider {
        width: 1px;
        height: 20px;
        background: var(--color-border-subtle, #e5e7eb);
      }

      .admin-shell__breadcrumbs {
        min-width: 0;
      }

      .admin-shell__breadcrumbs ol {
        display: flex;
        align-items: center;
        gap: 4px;
        margin: 0;
        padding: 0;
        list-style: none;
        font-size: 13px;
        min-width: 0;
      }

      .admin-shell__breadcrumbs li {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        min-width: 0;
      }

      .admin-shell__breadcrumbs a {
        color: var(--color-text-secondary, #6b7280);
        text-decoration: none;
        white-space: nowrap;
      }

      .admin-shell__breadcrumbs a:hover {
        color: var(--color-text-primary, #111827);
      }

      .admin-shell__breadcrumbs [aria-current='page'] span {
        font-weight: 600;
        color: var(--color-text-primary, #111827);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .admin-shell__breadcrumbs mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
        color: var(--color-text-secondary, #9ca3af);
      }

      .admin-shell__drawer-toggle {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 6px 10px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        border-radius: 8px;
        background: transparent;
        font-size: 13px;
        font-weight: 500;
        color: var(--color-text-secondary, #6b7280);
        cursor: pointer;
        transition:
          background 0.15s ease,
          color 0.15s ease;
      }

      .admin-shell__drawer-toggle:hover,
      .admin-shell__drawer-toggle[aria-pressed='true'] {
        background: var(--color-bg-muted, #f3f4f6);
        color: var(--color-text-primary, #111827);
      }

      .admin-shell__drawer-toggle mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }

      .admin-shell__env {
        flex-shrink: 0;
        padding: 4px 10px;
        border-radius: 999px;
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.04em;
        text-transform: uppercase;
      }

      .admin-shell__env[data-env='Production'] {
        background: color-mix(in srgb, #059669 14%, transparent);
        color: #047857;
      }

      .admin-shell__env[data-env='Development'] {
        background: color-mix(in srgb, var(--color-primary) 12%, transparent);
        color: var(--color-primary-hover);
      }

      .admin-shell__env[data-env='Demo'] {
        background: color-mix(in srgb, #d97706 14%, transparent);
        color: #b45309;
      }

      .admin-shell__tabs-strip {
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
      }

      .admin-shell__tabs {
        display: flex;
        align-items: stretch;
        gap: 2px;
        padding: 0 16px;
        overflow-x: auto;
        overflow-y: hidden;
      }

      .admin-shell__tab {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 12px 14px;
        border-bottom: 2px solid transparent;
        font-size: 13px;
        font-weight: 500;
        white-space: nowrap;
        color: var(--color-text-secondary, #6b7280);
        text-decoration: none;
        transition:
          color 0.15s ease,
          border-color 0.15s ease,
          background 0.15s ease;
      }

      .admin-shell__tab:hover {
        color: var(--color-text-primary, #111827);
        background: var(--color-bg-muted, #f3f4f6);
      }

      .admin-shell__tab mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }

      .admin-shell__tab--active {
        color: var(--color-brand-primary, var(--color-primary));
        border-bottom-color: var(--color-brand-primary, var(--color-primary));
        font-weight: 600;
      }

      .admin-shell__body {
        display: grid;
        grid-template-columns: 280px minmax(0, 1fr);
        flex: 1;
        min-height: 0;
        position: relative;
      }

      .admin-shell__nav {
        border-right: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
        padding: 20px 14px 40px;
        overflow-y: auto;
      }

      .admin-shell__section-title {
        margin: 0 0 10px;
        padding: 0 12px;
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--color-text-secondary, #9ca3af);
      }

      .admin-shell__link {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 10px 12px;
        margin-bottom: 4px;
        border-radius: 10px;
        border-left: 3px solid transparent;
        font-size: 14px;
        font-weight: 500;
        color: var(--color-text-secondary, #6b7280);
        text-decoration: none;
        transition:
          background 0.15s ease,
          color 0.15s ease,
          border-color 0.15s ease,
          transform 0.15s ease;
      }

      .admin-shell__link mat-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
        flex-shrink: 0;
        transition: color 0.15s ease;
      }

      .admin-shell__link-label {
        flex: 1;
        min-width: 0;
      }

      .admin-shell__link:hover {
        background: var(--color-bg-muted, #f3f4f6);
        color: var(--color-text-primary, #111827);
        transform: translateX(2px);
      }

      .admin-shell__link--active {
        background: color-mix(
          in srgb,
          var(--color-brand-primary, var(--color-primary)) 10%,
          transparent
        );
        border-left-color: var(--color-brand-primary, var(--color-primary));
        color: var(--color-brand-primary, var(--color-primary));
        font-weight: 600;
      }

      .admin-shell__link--active mat-icon {
        color: var(--color-brand-primary, var(--color-primary));
      }

      .admin-shell__link--soon {
        opacity: 0.82;
      }

      .admin-shell__soon-icon {
        font-size: 16px !important;
        width: 16px !important;
        height: 16px !important;
        color: var(--color-text-secondary, #9ca3af);
      }

      .admin-shell__content {
        padding: var(--page-padding-block-start) var(--page-padding-inline)
          var(--page-padding-block-end);
        overflow-y: auto;
        animation: adminContentIn 0.3s ease;
      }

      .admin-shell__content-inner {
        width: 100%;
        margin: 0 auto;
      }

      .admin-shell__backdrop {
        display: none;
      }

      @keyframes adminContentIn {
        from {
          opacity: 0;
          transform: translateY(6px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      @media (max-width: 900px) {
        .admin-shell__nav-toggle {
          display: inline-flex;
        }

        .admin-shell__body {
          grid-template-columns: 1fr;
        }

        .admin-shell__nav {
          position: fixed;
          top: 0;
          left: 0;
          bottom: 0;
          z-index: 300;
          width: min(300px, 88vw);
          transform: translateX(-105%);
          transition: transform 0.25s ease;
          box-shadow: 8px 0 32px rgba(0, 0, 0, 0.12);
        }

        .admin-shell__nav--open {
          transform: translateX(0);
        }

        .admin-shell__backdrop {
          display: block;
          position: fixed;
          inset: 0;
          z-index: 250;
          border: none;
          background: rgba(15, 23, 42, 0.4);
          cursor: pointer;
        }

        .admin-shell__back span,
        .admin-shell__drawer-toggle span {
          display: none;
        }

        .admin-shell__context-divider {
          display: none;
        }

        .admin-shell__breadcrumbs li:not([aria-current='page']) {
          display: none;
        }
      }
    `,
  ],
})
export class AdministrationShellComponent {
  private readonly router = inject(Router);
  readonly contextDrawer = inject(ContextDrawerService);

  readonly tabs = getAdministrationTabs();
  readonly environmentLabel = getAdministrationEnvironmentLabel();
  readonly mobileNavOpen = signal(false);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly activeTab = computed(() => resolveAdministrationTab(this.url()));

  /**
   * The aside item matching the current URL. Longest route wins so a parent overview
   * does not shadow the child page the user is actually on.
   */
  readonly activeItemLabel = computed(() => {
    const tab = this.activeTab();
    if (!tab) return null;

    const path = this.url().split(/[?#]/)[0].replace(/\/+$/, '');
    let label: string | null = null;
    let bestLength = 0;

    for (const item of tab.items) {
      const matches = item.exactMatch
        ? path === item.route
        : path === item.route || path.startsWith(`${item.route}/`);
      if (matches && item.route.length > bestLength) {
        label = item.label;
        bestLength = item.route.length;
      }
    }

    return label;
  });

  constructor() {
    this.contextDrawer.setSections([
      {
        id: CONTEXT_SECTION_ID,
        label: 'Configuration',
        glyph: 'CFG',
        load: () =>
          import('../components/administration-context-panel.component').then(
            (m) => m.AdministrationContextPanelComponent,
          ),
      },
    ]);
  }

  toggleContextDrawer(): void {
    this.contextDrawer.toggle(CONTEXT_SECTION_ID);
  }

  toggleMobileNav(): void {
    this.mobileNavOpen.update((open) => !open);
  }

  closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }
}
