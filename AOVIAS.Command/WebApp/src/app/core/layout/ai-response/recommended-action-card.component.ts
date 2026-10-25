import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RecommendedActionDto } from '../../../features/control-center/services/ai-assistant-api.service';

/** A single prioritized recommended action. */
@Component({
  selector: 'app-recommended-action-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="action-card">
      <span class="action-priority" aria-hidden="true">{{ action().priority }}</span>
      <div class="action-body">
        <p class="action-text">
          <span class="action-priority-label">Priority {{ action().priority }}</span>
          {{ action().action }}
        </p>
        @if (action().expectedOutcome) {
          <p class="action-outcome">
            <span class="action-outcome-icon" aria-hidden="true">
              <svg
                width="12"
                height="12"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.4"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <polyline points="20 6 9 17 4 12" />
              </svg>
            </span>
            {{ action().expectedOutcome }}
          </p>
        }
      </div>
    </article>
  `,
  styles: [
    `
      .action-card {
        display: flex;
        align-items: flex-start;
        gap: 9px;
        border-radius: 10px;
        padding: 9px 10px;
        border: 1px solid var(--border-color);
        background: color-mix(in srgb, var(--bg-tertiary) 92%, transparent);
      }

      .action-priority {
        flex-shrink: 0;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 22px;
        height: 22px;
        border-radius: 7px;
        font-size: 12px;
        font-weight: 700;
        color: var(--color-text-inverse);
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--primary-color) 90%, transparent),
          color-mix(in srgb, var(--primary-hover) 82%, transparent)
        );
      }

      .action-body {
        min-width: 0;
        flex: 1;
      }

      .action-text {
        margin: 0;
        font-size: 12px;
        line-height: 1.4;
        color: var(--text-primary);
      }

      .action-priority-label {
        display: inline-block;
        margin-right: 4px;
        font-size: 10px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--primary-color);
      }

      .action-outcome {
        display: flex;
        align-items: flex-start;
        gap: 5px;
        margin: 5px 0 0;
        font-size: 11px;
        line-height: 1.35;
        color: var(--text-secondary);
      }

      .action-outcome-icon {
        display: inline-flex;
        margin-top: 1px;
        color: var(--color-status-success);
        flex-shrink: 0;
      }
    `,
  ],
})
export class RecommendedActionCardComponent {
  readonly action = input.required<RecommendedActionDto>();
}
