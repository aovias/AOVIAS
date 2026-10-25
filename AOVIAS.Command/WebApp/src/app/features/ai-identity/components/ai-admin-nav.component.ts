import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { filter, map, startWith } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { SETTINGS_BASE } from '../../settings/models/settings-nav.model';
import {
  AI_ADMIN_BASE,
  getAiAdminNavItems,
  resolveAiAdminBreadcrumb,
} from '../models/ai-admin-nav.model';

@Component({
  selector: 'app-ai-admin-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="ai-admin-nav" aria-label="AI administration">
      <ol class="ai-admin-nav__breadcrumbs">
        <li><a [routerLink]="settingsBase">Settings</a></li>
        <li><a [routerLink]="aiBase">AI</a></li>
        @if (activeLabel() !== 'Dashboard') {
          <li aria-current="page">{{ activeLabel() }}</li>
        }
      </ol>

      <div class="ai-admin-nav__tabs" role="tablist">
        @for (item of navItems; track item.route) {
          @if (item.enabled && !item.comingSoon) {
            <a
              class="ai-admin-nav__tab"
              [routerLink]="item.route"
              routerLinkActive="ai-admin-nav__tab--active"
              [routerLinkActiveOptions]="{
                paths: item.segment ? 'subset' : 'exact',
                queryParams: 'ignored',
                fragment: 'ignored',
                matrixParams: 'ignored',
              }"
              role="tab"
            >
              <mat-icon aria-hidden="true">{{ item.icon }}</mat-icon>
              <span>{{ item.label }}</span>
            </a>
          } @else {
            <span class="ai-admin-nav__tab ai-admin-nav__tab--disabled" role="tab" aria-disabled="true">
              <mat-icon aria-hidden="true">{{ item.icon }}</mat-icon>
              <span>{{ item.label }}</span>
              <mat-icon class="ai-admin-nav__soon" aria-hidden="true" title="Coming soon">schedule</mat-icon>
            </span>
          }
        }
      </div>
    </nav>
  `,
  styles: [
    `
      .ai-admin-nav {
        display: flex;
        flex-direction: column;
        gap: 16px;
        margin-bottom: 8px;
      }
      .ai-admin-nav__breadcrumbs {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
        margin: 0;
        padding: 0;
        list-style: none;
        font-size: 13px;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-admin-nav__breadcrumbs li:not(:last-child)::after {
        content: '/';
        margin-left: 8px;
        color: var(--color-text-secondary, #9ca3af);
      }
      .ai-admin-nav__breadcrumbs a {
        color: var(--color-primary);
        text-decoration: none;
      }
      .ai-admin-nav__breadcrumbs a:hover {
        text-decoration: underline;
      }
      .ai-admin-nav__tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .ai-admin-nav__tab {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 8px 14px;
        border-radius: 999px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
        color: var(--color-text-secondary, #6b7280);
        font-size: 13px;
        font-weight: 500;
        text-decoration: none;
        transition: border-color 0.15s ease, color 0.15s ease, background 0.15s ease;
      }
      .ai-admin-nav__tab mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }
      .ai-admin-nav__tab:hover:not(.ai-admin-nav__tab--disabled) {
        border-color: color-mix(in srgb, var(--color-primary) 35%, transparent);
        color: var(--color-text-primary, #111827);
      }
      .ai-admin-nav__tab--active {
        border-color: color-mix(in srgb, var(--color-primary) 45%, transparent);
        background: color-mix(in srgb, var(--color-primary) 8%, transparent);
        color: var(--color-primary);
      }
      .ai-admin-nav__tab--disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
      .ai-admin-nav__soon {
        font-size: 14px !important;
        width: 14px !important;
        height: 14px !important;
      }
    `,
  ],
})
export class AiAdminNavComponent {
  private readonly router = inject(Router);

  readonly settingsBase = SETTINGS_BASE;
  readonly aiBase = AI_ADMIN_BASE;
  readonly navItems = getAiAdminNavItems();

  readonly activeLabel = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => resolveAiAdminBreadcrumb(this.router.url)),
      startWith(resolveAiAdminBreadcrumb(this.router.url)),
    ),
    { initialValue: resolveAiAdminBreadcrumb(this.router.url) },
  );
}
