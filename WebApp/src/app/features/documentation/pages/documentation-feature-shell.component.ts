import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {
  PageTabsComponent,
  type PageTabItem,
} from '../../../shared/ui/navigation/page-tabs/page-tabs.component';

/**
 * Documentation workspace chrome. Uses the shared page-tabs pattern (UI-4) so module
 * navigation is identical across features, and .aov-module-nav so the strip
 * aligns with the page content beneath it.
 */
@Component({
  selector: 'app-documentation-feature-shell',
  standalone: true,
  imports: [RouterOutlet, PageTabsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="aov-module-nav">
      <app-page-tabs
        [tabs]="tabs"
        variant="primary"
        [inset]="true"
        ariaLabel="Documentation workspace"
      />
    </div>
    <router-outlet />
  `,
})
export class DocumentationFeatureShellComponent {
  protected readonly tabs: readonly PageTabItem[] = [
    { id: 'dashboard', label: 'Dashboard', route: '/app/documentation/dashboard' },
    { id: 'register', label: 'Controlled Documents', route: '/app/documentation/register' },
    { id: 'policies', label: 'Policies', route: '/app/documentation/policies' },
    { id: 'sops', label: 'SOPs', route: '/app/documentation/sops' },
    { id: 'manuals', label: 'Manuals', route: '/app/documentation/manuals' },
    { id: 'forms', label: 'Forms & Templates', route: '/app/documentation/forms' },
    { id: 'authoring', label: 'Authoring Studio', route: '/app/documentation/authoring' },
    { id: 'explorer', label: 'Explorer', route: '/app/documentation/explorer' },
    { id: 'graph', label: 'Compliance Graph', route: '/app/documentation/graph' },
    {
      id: 'review-calendar',
      label: 'Review Calendar',
      route: '/app/documentation/review-calendar',
    },
    {
      id: 'acknowledgements',
      label: 'Acknowledgements',
      route: '/app/documentation/acknowledgements',
    },
    { id: 'analytics', label: 'Analytics', route: '/app/documentation/analytics' },
    { id: 'settings', label: 'Settings', route: '/app/documentation/settings' },
  ];
}
