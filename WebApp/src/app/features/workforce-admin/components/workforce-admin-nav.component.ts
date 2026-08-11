import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { filter, map, startWith } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { SETTINGS_BASE } from '../../settings/models/settings-nav.model';
import {
  WORKFORCE_ADMIN_BASE,
  getWorkforceAdminNavItems,
  resolveWorkforceAdminBreadcrumb,
} from '../models/workforce-admin-nav.model';

@Component({
  selector: 'app-workforce-admin-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="wf-admin-nav" aria-label="Workforce administration">
      <ol class="wf-admin-nav__breadcrumbs">
        <li><a [routerLink]="settingsBase">Settings</a></li>
        <li><a [routerLink]="workforceBase">Workforce</a></li>
        @if (activeLabel() !== 'Dashboard') {
          <li aria-current="page">{{ activeLabel() }}</li>
        }
      </ol>

      <div class="wf-admin-nav__tabs" role="tablist">
        @for (item of navItems; track item.route) {
          @if (item.enabled && !item.comingSoon) {
            <a
              class="wf-admin-nav__tab"
              [routerLink]="item.route"
              routerLinkActive="wf-admin-nav__tab--active"
              [routerLinkActiveOptions]="{
                paths: item.segment === 'dashboard' ? 'exact' : 'subset',
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
            <span class="wf-admin-nav__tab wf-admin-nav__tab--disabled" role="tab" aria-disabled="true">
              <mat-icon aria-hidden="true">{{ item.icon }}</mat-icon>
              <span>{{ item.label }}</span>
            </span>
          }
        }
      </div>
    </nav>
  `,
  styles: [
    `
      .wf-admin-nav {
        display: flex;
        flex-direction: column;
        gap: 16px;
        margin-bottom: 8px;
      }
      .wf-admin-nav__breadcrumbs {
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
      .wf-admin-nav__breadcrumbs li:not(:last-child)::after {
        content: '/';
        margin-left: 8px;
        color: var(--color-text-secondary, #9ca3af);
      }
      .wf-admin-nav__breadcrumbs a {
        color: var(--color-primary);
        text-decoration: none;
      }
      .wf-admin-nav__breadcrumbs a:hover {
        text-decoration: underline;
      }
      .wf-admin-nav__tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .wf-admin-nav__tab {
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
      .wf-admin-nav__tab mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }
      .wf-admin-nav__tab:hover:not(.wf-admin-nav__tab--disabled) {
        border-color: color-mix(in srgb, var(--color-primary) 35%, transparent);
        color: var(--color-text-primary, #111827);
      }
      .wf-admin-nav__tab--active {
        border-color: color-mix(in srgb, var(--color-primary) 45%, transparent);
        background: color-mix(in srgb, var(--color-primary) 8%, transparent);
        color: var(--color-primary);
      }
      .wf-admin-nav__tab--disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
    `,
  ],
})
export class WorkforceAdminNavComponent {
  private readonly router = inject(Router);

  readonly settingsBase = SETTINGS_BASE;
  readonly workforceBase = WORKFORCE_ADMIN_BASE;
  readonly navItems = getWorkforceAdminNavItems();

  readonly activeLabel = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => resolveWorkforceAdminBreadcrumb(this.router.url)),
      startWith(resolveWorkforceAdminBreadcrumb(this.router.url)),
    ),
    { initialValue: resolveWorkforceAdminBreadcrumb(this.router.url) },
  );
}
