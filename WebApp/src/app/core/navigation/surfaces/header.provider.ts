import { Injectable, computed, inject } from '@angular/core';
import { sidebarIcon } from '../../layout/sidebar-icons.registry';
import { NavigationQueryService } from '../engine/navigation-query.service';
import type { HeaderNavGroup, HeaderNavItem } from '../sidebar/sidebar.models';

@Injectable({ providedIn: 'root' })
export class HeaderProvider {
  private readonly query = inject(NavigationQueryService);

  readonly modules = this.query.headerModules;

  /** Legacy HeaderNavGroup shape for Module Switcher during cutover. */
  readonly headerNavGroups = computed((): HeaderNavGroup[] =>
    this.query.headerModules().map((m) => ({
      id: m.id.startsWith('module.') ? `header-${m.id.slice('module.'.length)}` : m.id,
      title: m.title,
      sectionIcon: sidebarIcon(m.icon),
      items: m.items.map(
        (i): HeaderNavItem => ({
          label: i.title,
          icon: sidebarIcon(i.icon),
          route: i.route ?? '/',
          disabled: i.disabled,
          disabledReason: i.disabledReason,
          exactActive: i.exactActive,
          subsetActive: i.subsetActive,
        }),
      ),
    })),
  );
}
