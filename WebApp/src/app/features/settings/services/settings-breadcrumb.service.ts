import { Injectable } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AI_ADMIN_BASE, getAiAdminNavItems } from '../../ai-identity/models/ai-admin-nav.model';
import {
  NOTIFICATION_ADMIN_BASE,
  getNotificationAdminNavItems,
} from '../../notifications/models/notification-admin-nav.model';
import {
  INTEGRATION_ADMIN_BASE,
  getIntegrationAdminNavItems,
} from '../../integration/models/integration-admin-nav.model';
import {
  WORKFORCE_ADMIN_BASE,
  getWorkforceAdminNavItems,
} from '../../workforce-admin/models/workforce-admin-nav.model';
import { getSettingsNavSections, SETTINGS_BASE } from '../models/settings-nav.model';

export interface BreadcrumbItem {
  label: string;
  route?: string;
}

@Injectable({ providedIn: 'root' })
export class SettingsBreadcrumbService {
  constructor(private readonly router: Router) {}

  breadcrumbs$(): Observable<BreadcrumbItem[]> {
    return this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => this.build(this.router.url)),
      startWith(this.build(this.router.url)),
    );
  }

  build(url: string): BreadcrumbItem[] {
    const path = url.split(/[?#]/)[0].replace(/\/+$/, '') || SETTINGS_BASE;
    const crumbs: BreadcrumbItem[] = [{ label: 'Settings', route: SETTINGS_BASE }];

    if (path === SETTINGS_BASE) {
      return crumbs;
    }

    if (path.startsWith(NOTIFICATION_ADMIN_BASE)) {
      crumbs.push({ label: 'Notifications', route: NOTIFICATION_ADMIN_BASE });
      const items = getNotificationAdminNavItems()
        .slice()
        .sort((a, b) => b.route.length - a.route.length);
      for (const item of items) {
        if (
          path === item.route ||
          (item.segment && path.startsWith(`${NOTIFICATION_ADMIN_BASE}/${item.segment}`))
        ) {
          if (item.segment) {
            crumbs.push({ label: item.label, route: item.route });
          }
          return crumbs;
        }
      }
      return crumbs;
    }

    if (path.startsWith(INTEGRATION_ADMIN_BASE)) {
      crumbs.push({ label: 'Platform' });
      crumbs.push({ label: 'Enterprise Integrations', route: INTEGRATION_ADMIN_BASE });
      const items = getIntegrationAdminNavItems()
        .slice()
        .sort((a, b) => b.route.length - a.route.length);
      for (const item of items) {
        if (
          path === item.route ||
          (item.segment && path.startsWith(`${INTEGRATION_ADMIN_BASE}/${item.segment}`))
        ) {
          if (item.segment) {
            crumbs.push({ label: item.label, route: item.route });
          }
          const remainder = path.slice(item.route.length).replace(/^\//, '');
          if (remainder && !/^[0-9a-f-]{36}$/i.test(remainder.split('/')[0])) {
            crumbs.push({ label: this.humanize(remainder.split('/')[0]) });
          } else if (remainder) {
            crumbs.push({ label: 'Details' });
          }
          return crumbs;
        }
      }
      return crumbs;
    }

    if (path.startsWith(AI_ADMIN_BASE)) {
      crumbs.push({ label: 'AI', route: AI_ADMIN_BASE });
      const aiItems = getAiAdminNavItems()
        .slice()
        .sort((a, b) => b.route.length - a.route.length);
      for (const item of aiItems) {
        if (
          path === item.route ||
          (item.segment && path.startsWith(`${AI_ADMIN_BASE}/${item.segment}`))
        ) {
          if (item.segment) {
            crumbs.push({ label: item.label, route: item.route });
          }
          const remainder = path.slice(item.route.length).replace(/^\//, '');
          if (remainder) {
            const segment = remainder.split('/')[0];
            crumbs.push({ label: this.humanize(segment) });
          }
          return crumbs;
        }
      }
      return crumbs;
    }

    if (path.startsWith(WORKFORCE_ADMIN_BASE)) {
      crumbs.push({ label: 'Workforce', route: `${WORKFORCE_ADMIN_BASE}/dashboard` });
      const items = getWorkforceAdminNavItems()
        .slice()
        .sort((a, b) => b.route.length - a.route.length);
      for (const item of items) {
        if (
          path === item.route ||
          (item.segment && path.startsWith(`${WORKFORCE_ADMIN_BASE}/${item.segment}`))
        ) {
          if (item.segment && item.segment !== 'dashboard') {
            crumbs.push({ label: item.label, route: item.route });
          }
          const remainder = path.slice(item.route.length).replace(/^\//, '');
          if (remainder) {
            crumbs.push({ label: this.humanize(remainder.split('/')[0]) });
          }
          return crumbs;
        }
      }
      return crumbs;
    }

    if (path.startsWith(`${SETTINGS_BASE}/system/audit`)) {
      crumbs.push({ label: 'System' });
      crumbs.push({ label: 'Audit & Activity', route: `${SETTINGS_BASE}/system/audit` });
      return crumbs;
    }

    const sections = getSettingsNavSections();
    const navItems = sections
      .flatMap((section) => section.items.map((item) => ({ section, item })))
      .sort((a, b) => b.item.route.length - a.item.route.length);

    for (const { section, item } of navItems) {
      if (path === item.route || path.startsWith(item.route + '/')) {
        crumbs.push({ label: section.title });
        crumbs.push({ label: item.label, route: item.route });
        const remainder = path.slice(item.route.length + 1);
        if (remainder) {
          const segment = remainder.split('/')[0];
          crumbs.push({ label: this.humanize(segment) });
        }
        return crumbs;
      }
    }

    if (path.startsWith(`${SETTINGS_BASE}/account`)) {
      return [...crumbs, { label: 'Account & Security' }];
    }
    if (path.startsWith(`${SETTINGS_BASE}/theme`)) {
      return [...crumbs, { label: 'Theme Preferences' }];
    }

    return crumbs;
  }

  private humanize(segment: string): string {
    return segment
      .split('-')
      .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
      .join(' ');
  }
}
