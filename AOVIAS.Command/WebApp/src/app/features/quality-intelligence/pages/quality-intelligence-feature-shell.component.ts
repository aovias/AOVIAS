import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {
  PageTabsComponent,
  type PageTabItem,
} from '../../../shared/ui/navigation/page-tabs/page-tabs.component';

/**
 * Quality Intelligence workspace chrome. Uses the shared page-tabs pattern (UI-4) so module
 * navigation is identical across features, and .aov-module-nav so the strip
 * aligns with the page content beneath it.
 */
@Component({
  selector: 'app-quality-intelligence-feature-shell',
  standalone: true,
  imports: [RouterOutlet, PageTabsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="aov-module-nav">
      <app-page-tabs
        [tabs]="tabs"
        variant="primary"
        [inset]="true"
        ariaLabel="Quality Intelligence workspace"
      />
    </div>
    <router-outlet />
  `,
})
export class QualityIntelligenceFeatureShellComponent {
  protected readonly tabs: readonly PageTabItem[] = [
    { id: 'dashboard', label: 'Dashboard', route: '/app/quality-intelligence/dashboard' },
    {
      id: 'indicators',
      label: 'Clinical Indicators',
      route: '/app/quality-intelligence/indicators',
    },
    {
      id: 'scorecards',
      label: 'Department Scorecards',
      route: '/app/quality-intelligence/scorecards',
    },
    { id: 'trends', label: 'Quality Trends', route: '/app/quality-intelligence/trends' },
    { id: 'benchmarking', label: 'Benchmarking', route: '/app/quality-intelligence/benchmarking' },
    {
      id: 'thresholds',
      label: 'Threshold Management',
      route: '/app/quality-intelligence/thresholds',
    },
    {
      id: 'investigations',
      label: 'Investigations',
      route: '/app/quality-intelligence/investigations',
    },
    { id: 'analytics', label: 'Analytics', route: '/app/quality-intelligence/analytics' },
    { id: 'executive', label: 'Executive', route: '/app/quality-intelligence/executive' },
    { id: 'settings', label: 'Settings', route: '/app/quality-intelligence/settings' },
  ];
}
