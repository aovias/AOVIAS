import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {
  PageTabsComponent,
  type PageTabItem,
} from '../../../shared/ui/navigation/page-tabs/page-tabs.component';

/**
 * Patient Safety workspace chrome. Uses the shared page-tabs pattern (UI-4) so module
 * navigation is identical across features, and .aov-module-nav so the strip
 * aligns with the page content beneath it.
 */
@Component({
  selector: 'app-patient-safety-feature-shell',
  standalone: true,
  imports: [RouterOutlet, PageTabsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="aov-module-nav">
      <app-page-tabs
        [tabs]="tabs"
        variant="primary"
        [inset]="true"
        ariaLabel="Patient Safety workspace"
      />
    </div>
    <router-outlet />
  `,
})
export class PatientSafetyFeatureShellComponent {
  protected readonly tabs: readonly PageTabItem[] = [
    { id: 'dashboard', label: 'Dashboard', route: '/app/patient-safety/dashboard' },
    {
      id: 'command-center',
      label: 'Incident Command Center',
      route: '/app/patient-safety/command-center',
    },
    { id: 'investigations', label: 'Investigations', route: '/app/patient-safety/investigations' },
    { id: 'rca', label: 'Root Cause Analysis', route: '/app/patient-safety/rca' },
    { id: 'near-misses', label: 'Near Misses', route: '/app/patient-safety/near-misses' },
    {
      id: 'sentinel-events',
      label: 'Sentinel Events',
      route: '/app/patient-safety/sentinel-events',
    },
    { id: 'feedback', label: 'Patient Feedback', route: '/app/patient-safety/feedback' },
    { id: 'lessons', label: 'Lessons Learned', route: '/app/patient-safety/lessons' },
    { id: 'analytics', label: 'Analytics', route: '/app/patient-safety/analytics' },
    { id: 'settings', label: 'Settings', route: '/app/patient-safety/settings' },
  ];
}
