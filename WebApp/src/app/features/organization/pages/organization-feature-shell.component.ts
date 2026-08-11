import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { provideIcons } from '@ng-icons/core';
import {
  lucideBuilding2,
  lucideClipboardList,
  lucideGitBranch,
  lucideHospital,
  lucideLayoutDashboard,
  lucideNetwork,
  lucideSettings,
  lucideUsers,
} from '@ng-icons/lucide';
import {
  PageTabsComponent,
  type PageTabItem,
} from '../../../shared/ui/navigation/page-tabs/page-tabs.component';

/**
 * Organization administration console shell.
 * Reference workspace pattern for multi-page admin modules (same PageTabs as Risks).
 */
@Component({
  selector: 'app-organization-feature-shell',
  standalone: true,
  imports: [RouterOutlet, PageTabsComponent],
  viewProviders: [
    provideIcons({
      lucideLayoutDashboard,
      lucideHospital,
      lucideBuilding2,
      lucideGitBranch,
      lucideUsers,
      lucideClipboardList,
      lucideNetwork,
      lucideSettings,
    }),
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="aov-module-nav">
      <app-page-tabs
        [tabs]="tabs"
        variant="primary"
        [inset]="true"
        ariaLabel="Organization"
      />
    </div>
    <router-outlet />
  `,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }
    `,
  ],
})
export class OrganizationFeatureShellComponent {
  /** Flat tabs ordered by conceptual groups: Overview | Structure | People | Compliance | Insights | Settings */
  protected readonly tabs: readonly PageTabItem[] = [
    {
      id: 'overview',
      label: 'Overview',
      route: '/app/organization/overview',
      icon: 'lucideLayoutDashboard',
    },
    {
      id: 'hospitals',
      label: 'Hospitals',
      route: '/app/organization/hospitals',
      icon: 'lucideHospital',
    },
    {
      id: 'facility',
      label: 'Facility',
      route: '/app/organization/facility',
      icon: 'lucideBuilding2',
    },
    {
      id: 'departments',
      label: 'Departments',
      route: '/app/organization/departments',
      icon: 'lucideGitBranch',
    },
    {
      id: 'membership',
      label: 'Membership',
      route: '/app/organization/membership',
      icon: 'lucideUsers',
    },
    {
      id: 'oe-ownership',
      label: 'OE Ownership',
      route: '/app/organization/oe-ownership',
      icon: 'lucideClipboardList',
    },
    {
      id: 'assignments',
      label: 'Assignments',
      route: '/app/organization/assignments',
      icon: 'lucideClipboardList',
    },
    {
      id: 'graph',
      label: 'Graph',
      route: '/app/organization/graph',
      icon: 'lucideNetwork',
    },
    {
      id: 'settings',
      label: 'Settings',
      route: '/app/organization/settings',
      icon: 'lucideSettings',
    },
  ];
}
