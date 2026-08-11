import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { filter, map, startWith } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { SETTINGS_BASE } from '../../settings/models/settings-nav.model';
import {
  NOTIFICATION_ADMIN_BASE,
  getNotificationAdminNavItems,
  resolveNotificationAdminBreadcrumb,
} from '../models/notification-admin-nav.model';

@Component({
  selector: 'app-notification-admin-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="notification-admin-nav" aria-label="Notification administration">
      <ol class="notification-admin-nav__breadcrumbs">
        <li><a [routerLink]="settingsBase">Settings</a></li>
        <li><a [routerLink]="notificationBase">Notifications</a></li>
        @if (activeLabel() !== 'Overview') {
          <li aria-current="page">{{ activeLabel() }}</li>
        }
      </ol>
      <div class="notification-admin-nav__tabs" role="tablist">
        @for (item of navItems; track item.route) {
          <a
            class="notification-admin-nav__tab"
            [routerLink]="item.route"
            routerLinkActive="notification-admin-nav__tab--active"
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
      .notification-admin-nav {
        display: flex;
        flex-direction: column;
        gap: 16px;
        margin-bottom: 8px;
      }
      .notification-admin-nav__breadcrumbs {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin: 0;
        padding: 0;
        list-style: none;
        font-size: 13px;
        color: var(--color-text-secondary, #6b7280);
      }
      .notification-admin-nav__breadcrumbs li:not(:last-child)::after {
        content: '/';
        margin-left: 8px;
      }
      .notification-admin-nav__breadcrumbs a {
        color: var(--color-primary);
        text-decoration: none;
      }
      .notification-admin-nav__tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .notification-admin-nav__tab {
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
      .notification-admin-nav__tab--active {
        border-color: color-mix(in srgb, var(--color-primary) 45%, transparent);
        background: color-mix(in srgb, var(--color-primary) 8%, transparent);
        color: var(--color-primary);
      }
      .notification-admin-nav__tab mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }
    `,
  ],
})
export class NotificationAdminNavComponent {
  private readonly router = inject(Router);
  readonly settingsBase = SETTINGS_BASE;
  readonly notificationBase = NOTIFICATION_ADMIN_BASE;
  readonly navItems = getNotificationAdminNavItems();
  readonly activeLabel = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => resolveNotificationAdminBreadcrumb(this.router.url)),
      startWith(resolveNotificationAdminBreadcrumb(this.router.url)),
    ),
    { initialValue: resolveNotificationAdminBreadcrumb(this.router.url) },
  );
}
