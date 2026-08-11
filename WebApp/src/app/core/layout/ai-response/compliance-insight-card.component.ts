import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ComplianceFindingDto } from '../../../features/control-center/services/ai-assistant-api.service';
import { RiskBadgeComponent } from './risk-badge.component';

/** A single key finding rendered as a card with a severity badge. */
@Component({
  selector: 'app-compliance-insight-card',
  standalone: true,
  imports: [CommonModule, RiskBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="insight-card"
      [class.insight-card--critical]="finding().severity === 'Critical'"
      [class.insight-card--high]="finding().severity === 'High'"
      [class.insight-card--medium]="finding().severity === 'Medium'"
      [class.insight-card--low]="finding().severity === 'Low'"
    >
      <header class="insight-head">
        <app-risk-badge
          [severity]="finding().severity"
          [labelOverride]="finding().severity + ' Impact Gap'"
        />
      </header>
      <h5 class="insight-title">{{ finding().title }}</h5>
      @if (finding().impact) {
        <p class="insight-impact">
          <span class="insight-label">Impact:</span> {{ finding().impact }}
        </p>
      }
      @if (finding().detail) {
        <p class="insight-detail">{{ finding().detail }}</p>
      }
    </article>
  `,
  styles: [
    `
      .insight-card {
        border-radius: 10px;
        padding: 10px;
        border: 1px solid var(--border-color);
        border-left-width: 3px;
        background: color-mix(in srgb, var(--bg-tertiary) 92%, transparent);
      }

      .insight-card--critical {
        border-left-color: var(--color-status-danger);
      }
      .insight-card--high {
        border-left-color: color-mix(
          in srgb,
          var(--color-status-warning) 55%,
          var(--color-status-danger)
        );
      }
      .insight-card--medium {
        border-left-color: var(--color-status-warning);
      }
      .insight-card--low {
        border-left-color: var(--color-status-success);
      }

      .insight-head {
        margin-bottom: 6px;
      }

      .insight-title {
        margin: 0 0 4px;
        font-size: 12.5px;
        font-weight: 700;
        line-height: 1.3;
        color: var(--text-primary);
      }

      .insight-impact,
      .insight-detail {
        margin: 4px 0 0;
        font-size: 11.5px;
        line-height: 1.4;
        color: var(--text-secondary);
      }

      .insight-label {
        font-weight: 700;
        color: var(--text-primary);
      }
    `,
  ],
})
export class ComplianceInsightCardComponent {
  readonly finding = input.required<ComplianceFindingDto>();
}
