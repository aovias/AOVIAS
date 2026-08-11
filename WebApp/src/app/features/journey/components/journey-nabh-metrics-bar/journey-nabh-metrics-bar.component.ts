import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JourneyOverviewMetrics } from '../../models/journey-tree.model';

@Component({
  selector: 'app-journey-nabh-metrics-bar',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (metrics()) {
      <div class="metrics" role="region" aria-label="NABH compliance metrics">
        <div class="m">
          <span class="lbl">Core compliance</span>
          <span class="val">{{ metrics()!.coreCompliancePercent }}%</span>
        </div>
        <div class="m">
          <span class="lbl">Failed core OEs</span>
          <span class="val" [attr.data-warn]="metrics()!.failedCoreCount > 0">{{
            metrics()!.failedCoreCount
          }}</span>
        </div>
        <div class="m">
          <span class="lbl">OEs ≤ 3</span>
          <span class="val">{{ metrics()!.oesAtOrBelowThreeCount }}</span>
        </div>
        <div class="m">
          <span class="lbl">Open action plans</span>
          <span class="val">{{ metrics()!.openActionPlansCount }}</span>
        </div>
      </div>
    }
  `,
  styles: `
    .metrics {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(8.75rem, 1fr));
      gap: var(--space-2);
      margin-bottom: var(--space-3);
    }
    .m {
      border: var(--border-width-hairline) solid var(--border-color);
      border-radius: 0.625rem;
      padding: var(--space-2) var(--space-3);
      background: var(--bg-tertiary);
      box-shadow: var(--shadow-xs);
    }
    .lbl {
      display: block;
      font-size: 0.6875rem;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--text-secondary);
    }
    .val {
      font-size: var(--font-size-heading-md);
      font-weight: var(--font-weight-bold);
      color: var(--text-primary);
    }
    .val[data-warn='true'] {
      color: var(--danger-color);
    }
  `,
})
export class JourneyNabhMetricsBarComponent {
  readonly metrics = input<JourneyOverviewMetrics | null>(null);
}
