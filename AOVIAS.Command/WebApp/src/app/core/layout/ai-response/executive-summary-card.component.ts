import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

interface SummaryCount {
  label: string;
  value: number;
}

/** Large highlighted card that opens every compliance briefing. */
@Component({
  selector: 'app-executive-summary-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="exec-card">
      <header class="exec-head">
        <span class="exec-icon" aria-hidden="true">
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <path d="M12 2a7 7 0 0 0-4 12.7V17h8v-2.3A7 7 0 0 0 12 2Z" />
            <line x1="9" y1="21" x2="15" y2="21" />
          </svg>
        </span>
        <h4 class="exec-title">Executive Summary</h4>
      </header>
      <p class="exec-body">{{ summary() }}</p>

      @if (counts().length) {
        <div class="exec-counts">
          @for (count of counts(); track count.label) {
            <span class="exec-chip">
              <span class="exec-chip-value">{{ count.value }}</span>
              <span class="exec-chip-label">{{ count.label }}</span>
            </span>
          }
        </div>
      }
    </section>
  `,
  styles: [
    `
      .exec-card {
        border-radius: 12px;
        padding: 12px;
        border: 1px solid color-mix(in srgb, var(--primary-color) 38%, transparent);
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--primary-color) 16%, var(--card-bg)) 0%,
          color-mix(in srgb, var(--primary-color) 7%, var(--card-bg)) 100%
        );
      }

      .exec-head {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 6px;
      }

      .exec-icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 24px;
        height: 24px;
        border-radius: 7px;
        color: var(--primary-color);
        background: color-mix(in srgb, var(--primary-color) 18%, transparent);
      }

      .exec-title {
        margin: 0;
        font-size: 13px;
        font-weight: 700;
        color: var(--text-primary);
      }

      .exec-body {
        margin: 0;
        font-size: 12.5px;
        line-height: 1.45;
        color: var(--text-primary);
      }

      .exec-counts {
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
        margin-top: 10px;
      }

      .exec-chip {
        display: inline-flex;
        align-items: baseline;
        gap: 4px;
        padding: 3px 9px;
        border-radius: 999px;
        background: color-mix(in srgb, var(--bg-tertiary) 90%, transparent);
        border: 1px solid var(--border-color);
      }

      .exec-chip-value {
        font-size: 13px;
        font-weight: 700;
        color: var(--text-primary);
      }

      .exec-chip-label {
        font-size: 10px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--text-secondary);
      }
    `,
  ],
})
export class ExecutiveSummaryCardComponent {
  readonly summary = input.required<string>();
  readonly counts = input<SummaryCount[]>([]);
}
