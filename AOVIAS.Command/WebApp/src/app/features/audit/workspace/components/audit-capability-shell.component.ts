import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {
  PageTabsComponent,
  type PageTabItem,
} from '../../../../shared/ui/navigation/page-tabs/page-tabs.component';

/**
 * Shared shell for a capability that spans several pre-existing pages.
 *
 * Phase 2 consolidation: rather than each capability inventing its own sub-tab
 * markup, Overview / Execution / Reports all compose this one shell. That is
 * what makes the sub-tab height, spacing, alignment, and typography identical
 * across every tab of the workspace (Step 7).
 *
 * It only arranges navigation — the hosted pages, their services, and their
 * business logic are untouched.
 */
@Component({
  selector: 'app-audit-capability-shell',
  standalone: true,
  imports: [RouterOutlet, PageTabsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="capability-shell">
      @if (tabs().length > 1) {
        <div class="capability-shell__nav">
          <app-page-tabs [tabs]="tabs()" variant="secondary" [ariaLabel]="ariaLabel()" />
        </div>
      }
      <router-outlet />
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }

      .capability-shell {
        display: flex;
        flex-direction: column;
        min-inline-size: 0;
      }

      /*
       * Aligned to the page gutter so the sub-tab strip starts at the same x as
       * the hosted page content below it. No bottom padding — the hosted page
       * supplies its own top padding.
       */
      .capability-shell__nav {
        padding: var(--page-padding) var(--page-padding) 0;
      }
    `,
  ],
})
export class AuditCapabilityShellComponent {
  readonly tabs = input.required<readonly PageTabItem[]>();
  readonly ariaLabel = input<string>('Capability sections');
}
