import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { provideIcons } from '@ng-icons/core';
import { lucideGrid2x2, lucidePlus, lucideTable } from '@ng-icons/lucide';
import {
  PageTabsComponent,
  type PageTabItem,
} from '../../../shared/ui/navigation/page-tabs/page-tabs.component';

/**
 * Risks module chrome. Uses the shared page-tabs pattern (UI-4) so module
 * navigation matches every other feature. The bar is sticky and shares the
 * standard page inline padding, so content below it never shifts horizontally
 * relative to other pages.
 */
@Component({
  selector: 'app-risks-feature-shell',
  standalone: true,
  imports: [RouterOutlet, PageTabsComponent],
  viewProviders: [provideIcons({ lucideTable, lucideGrid2x2, lucidePlus })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="aov-module-nav">
      <app-page-tabs
        [tabs]="tabs"
        variant="primary"
        [inset]="true"
        ariaLabel="Enterprise risk register"
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
export class RisksFeatureShellComponent {
  protected readonly tabs: readonly PageTabItem[] = [
    { id: 'register', label: 'Risk Register', route: '/app/risks/register', icon: 'lucideTable' },
    { id: 'matrix', label: 'Risk Matrix', route: '/app/risks/matrix', icon: 'lucideGrid2x2' },
    { id: 'new', label: 'New Risk', route: '/app/risks/new', icon: 'lucidePlus' },
  ];
}
