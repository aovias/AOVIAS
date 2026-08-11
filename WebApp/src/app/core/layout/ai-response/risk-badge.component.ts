import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ComplianceSeverity } from '../../../features/control-center/services/ai-assistant-api.service';

/**
 * Severity pill used by findings and risks.
 * Critical = red, High = orange, Medium = amber, Low = green.
 */
@Component({
  selector: 'app-risk-badge',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="risk-badge"
      [class.risk-badge--critical]="severity() === 'Critical'"
      [class.risk-badge--high]="severity() === 'High'"
      [class.risk-badge--medium]="severity() === 'Medium'"
      [class.risk-badge--low]="severity() === 'Low'"
    >
      <span class="risk-badge-dot" aria-hidden="true"></span>
      <span class="risk-badge-text">{{ label() }}</span>
    </span>
  `,
  styles: [
    `
      .risk-badge {
        display: inline-flex;
        align-items: center;
        gap: 5px;
        padding: 2px 8px;
        border-radius: 999px;
        font-size: 10px;
        font-weight: 700;
        letter-spacing: 0.03em;
        text-transform: uppercase;
        line-height: 1.4;
        white-space: nowrap;
        border: 1px solid transparent;
      }

      .risk-badge-dot {
        width: 6px;
        height: 6px;
        border-radius: 999px;
        background: currentColor;
        flex-shrink: 0;
      }

      .risk-badge--critical {
        color: var(--color-status-danger);
        background: color-mix(in srgb, var(--color-status-danger) 16%, transparent);
        border-color: color-mix(in srgb, var(--color-status-danger) 40%, transparent);
      }

      .risk-badge--high {
        color: color-mix(in srgb, var(--color-status-warning) 55%, var(--color-status-danger));
        background: color-mix(in srgb, var(--color-status-warning) 18%, transparent);
        border-color: color-mix(in srgb, var(--color-status-warning) 45%, transparent);
      }

      .risk-badge--medium {
        color: var(--color-status-warning);
        background: color-mix(in srgb, var(--color-status-warning) 16%, transparent);
        border-color: color-mix(in srgb, var(--color-status-warning) 38%, transparent);
      }

      .risk-badge--low {
        color: var(--color-status-success);
        background: color-mix(in srgb, var(--color-status-success) 16%, transparent);
        border-color: color-mix(in srgb, var(--color-status-success) 38%, transparent);
      }
    `,
  ],
})
export class RiskBadgeComponent {
  readonly severity = input<ComplianceSeverity>('Medium');
  readonly labelOverride = input<string | null>(null);

  readonly label = computed(() => this.labelOverride() ?? `${this.severity()} Impact`);
}
