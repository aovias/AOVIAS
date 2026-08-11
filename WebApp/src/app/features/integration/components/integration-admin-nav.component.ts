import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { filter, map, startWith } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { SETTINGS_BASE } from '../../settings/models/settings-nav.model';
import {
  INTEGRATION_ADMIN_BASE,
  getIntegrationAdminNavItems,
  resolveIntegrationAdminBreadcrumb,
} from '../models/integration-admin-nav.model';

@Component({
  selector: 'app-integration-admin-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="integration-admin-nav" aria-label="Enterprise integrations">
      <ol class="integration-admin-nav__breadcrumbs">
        <li><a [routerLink]="settingsBase">Settings</a></li>
        <li><a [routerLink]="settingsBase + '/platform/integrations'">Platform</a></li>
        <li aria-current="page">{{ activeLabel() }}</li>
      </ol>
      <div class="integration-admin-nav__tabs" role="tablist">
        @for (item of navItems; track item.route) {
          <a
            class="integration-admin-nav__tab"
            [routerLink]="item.route"
            routerLinkActive="integration-admin-nav__tab--active"
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
        }
      </div>
    </nav>
  `,
  styles: [
    `
      .integration-admin-nav {
        display: flex;
        flex-direction: column;
        gap: 16px;
        margin-bottom: 8px;
      }
      .integration-admin-nav__breadcrumbs {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin: 0;
        padding: 0;
        list-style: none;
        font-size: 13px;
        color: var(--color-text-secondary, #6b7280);
      }
      .integration-admin-nav__breadcrumbs li:not(:last-child)::after {
        content: '/';
        margin-left: 8px;
      }
      .integration-admin-nav__breadcrumbs a {
        color: var(--color-primary);
        text-decoration: none;
      }
      .integration-admin-nav__tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .integration-admin-nav__tab {
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
      }
      .integration-admin-nav__tab--active {
        border-color: color-mix(in srgb, var(--color-primary) 45%, transparent);
        background: color-mix(in srgb, var(--color-primary) 8%, transparent);
        color: var(--color-primary);
      }
      .integration-admin-nav__tab mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }
    `,
  ],
})
export class IntegrationAdminNavComponent {
  private readonly router = inject(Router);
  readonly settingsBase = SETTINGS_BASE;
  readonly navItems = getIntegrationAdminNavItems();
  readonly activeLabel = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => resolveIntegrationAdminBreadcrumb(this.router.url)),
      startWith(resolveIntegrationAdminBreadcrumb(this.router.url)),
    ),
    { initialValue: resolveIntegrationAdminBreadcrumb(this.router.url) },
  );
}
