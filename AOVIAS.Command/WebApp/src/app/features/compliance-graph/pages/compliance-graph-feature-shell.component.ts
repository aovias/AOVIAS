import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {
  PageTabsComponent,
  type PageTabItem,
} from '../../../shared/ui/navigation/page-tabs/page-tabs.component';

/**
 * Compliance Graph workspace chrome. Uses the shared page-tabs pattern (UI-4) so module
 * navigation is identical across features, and .aov-module-nav so the strip
 * aligns with the page content beneath it.
 */
@Component({
  selector: 'app-compliance-graph-feature-shell',
  standalone: true,
  imports: [RouterOutlet, PageTabsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/compliance-graph-workspace.layout.scss',
  template: `
    <div class="aov-module-nav">
      <app-page-tabs
        [tabs]="tabs"
        variant="primary"
        [inset]="true"
        ariaLabel="Compliance Graph workspace"
      />
    </div>
    <router-outlet />
  `,
})
export class ComplianceGraphFeatureShellComponent {
  protected readonly tabs: readonly PageTabItem[] = [
    { id: 'viewer', label: 'Graph Viewer', route: '/app/compliance-graph/viewer' },
    { id: 'search', label: 'Enterprise Search', route: '/app/compliance-graph/search' },
    { id: 'traceability', label: 'Traceability', route: '/app/compliance-graph/traceability' },
    { id: 'executive', label: 'Executive Impact', route: '/app/compliance-graph/executive' },
    { id: 'analytics', label: 'Analytics', route: '/app/compliance-graph/analytics' },
  ];
}
