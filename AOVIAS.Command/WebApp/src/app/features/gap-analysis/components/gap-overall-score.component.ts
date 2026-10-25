import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GaugeChartComponent } from '../../dashboard/components/gauge-chart.component';

@Component({
  selector: 'app-gap-overall-score',
  standalone: true,
  imports: [CommonModule, GaugeChartComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="gap-overall" aria-label="Overall assessment score">
      <div class="gap-overall__gauge">
        <app-gauge-chart
          [title]="'Overall Compliance Score'"
          [value]="overallPercent"
          [id]="'gap-overall'" />
      </div>
      <div class="gap-overall__meta">
        <p class="gap-overall__readiness">{{ readinessLabel }}</p>
        @if (!coreRulePassed) {
          <span class="gap-overall__chip gap-overall__chip--warn">CORE rule not met</span>
        } @else {
          <span class="gap-overall__chip gap-overall__chip--ok">CORE rule passed</span>
        }
        @if (generatedAt) {
          <p class="gap-overall__generated">Generated {{ generatedAt | date: 'medium' }}</p>
        }
      </div>
    </section>
  `,
  styles: [
    `
      .gap-overall {
        display: grid;
        grid-template-columns: minmax(240px, 320px) 1fr;
        gap: 24px;
        align-items: center;
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 24px;
        box-shadow: var(--shadow-sm);
      }

      .gap-overall__gauge {
        min-height: 200px;
      }

      .gap-overall__readiness {
        font-size: 20px;
        font-weight: 600;
        color: var(--text-primary);
        margin: 0 0 12px;
      }

      .gap-overall__chip {
        display: inline-block;
        padding: 4px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
      }

      .gap-overall__chip--warn {
        background: rgba(239, 68, 68, 0.12);
        color: var(--danger-color, #dc2626);
      }

      .gap-overall__chip--ok {
        background: rgba(16, 185, 129, 0.12);
        color: var(--success-color, #059669);
      }

      .gap-overall__generated {
        margin: 16px 0 0;
        font-size: 13px;
        color: var(--text-secondary);
      }

      @media (max-width: 768px) {
        .gap-overall {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class GapOverallScoreComponent {
  @Input({ required: true }) overallPercent = 0;
  @Input({ required: true }) readinessLabel = '';
  @Input({ required: true }) coreRulePassed = true;
  @Input() generatedAt: string | null = null;
}
