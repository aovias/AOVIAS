import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { DeficiencySummaryApiDto } from '../../models/deficiency-api.model';

/**
 * Compact severity summary card for a hospital × framework deficiency profile.
 * Displays Critical (red), Major (orange), Moderate (amber), Minor (blue) count chips
 * and an open-count badge. Can be scoped to a specific maturity stage via `stageLabel`.
 *
 * Used in journey-maturity-dashboard below the Obligation Summary section.
 */
@Component({
  selector: 'app-deficiency-summary-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="def-card"
      [class.def-card--empty]="!summary() || summary()!.total === 0"
      [attr.aria-label]="(stageLabel() ?? 'All stages') + ' compliance deficiencies'"
    >
      <header class="def-card__header">
        <span class="def-card__title">
          @if (stageLabel()) {
            <span class="def-card__stage">{{ stageLabel() }}</span>
          }
          Deficiencies
        </span>
        <span
          class="def-card__open-badge"
          [attr.aria-label]="openCount() + ' open deficiencies'"
        >
          {{ openCount() }} open
        </span>
      </header>

      @if (summary()) {
        <div class="def-card__chips" role="list" aria-label="Deficiency severity breakdown">
          <div class="def-chip def-chip--critical" role="listitem">
            <span class="def-chip__count">{{ summary()!.critical }}</span>
            <span class="def-chip__label">Critical</span>
          </div>
          <div class="def-chip def-chip--major" role="listitem">
            <span class="def-chip__count">{{ summary()!.major }}</span>
            <span class="def-chip__label">Major</span>
          </div>
          <div class="def-chip def-chip--moderate" role="listitem">
            <span class="def-chip__count">{{ summary()!.moderate }}</span>
            <span class="def-chip__label">Moderate</span>
          </div>
          <div class="def-chip def-chip--minor" role="listitem">
            <span class="def-chip__count">{{ summary()!.minor }}</span>
            <span class="def-chip__label">Minor</span>
          </div>
        </div>
      } @else {
        <p class="def-card__empty-msg">No data</p>
      }
    </article>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .def-card {
        background: var(--color-bg-surface);
        border: 1.5px solid var(--color-border-subtle);
        border-radius: 10px;
        padding: 14px 16px;
        display: flex;
        flex-direction: column;
        gap: 10px;
        height: 100%;
        box-sizing: border-box;
      }

      .def-card__header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .def-card__title {
        font-size: 12px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--color-text-secondary);
        display: flex;
        align-items: center;
        gap: 6px;
      }

      .def-card__stage {
        background: var(--color-bg-surface-alt);
        border: 1px solid var(--color-border-subtle);
        border-radius: 4px;
        padding: 1px 6px;
        font-size: 10px;
        color: var(--color-text-primary);
      }

      .def-card__open-badge {
        font-size: 11px;
        font-weight: 600;
        color: var(--color-text-secondary);
        background: var(--color-bg-surface-alt);
        border: 1px solid var(--color-border-subtle);
        border-radius: 20px;
        padding: 2px 8px;
      }

      .def-card__chips {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 6px;
      }

      .def-chip {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 8px 6px;
        border-radius: 8px;
        gap: 2px;
        border: 1px solid transparent;
      }

      .def-chip__count {
        font-size: 18px;
        font-weight: 800;
        line-height: 1;
      }

      .def-chip__label {
        font-size: 10px;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        font-weight: 600;
      }

      /* Critical — red tones */
      .def-chip--critical {
        background: color-mix(in srgb, var(--color-status-danger) 12%, var(--color-bg-surface-alt));
        border-color: color-mix(in srgb, var(--color-status-danger) 30%, var(--color-border-subtle));
        .def-chip__count { color: var(--color-status-danger); }
        .def-chip__label { color: var(--color-text-secondary); }
      }

      /* Major — orange tones */
      .def-chip--major {
        background: color-mix(in srgb, var(--color-status-warning) 14%, var(--color-bg-surface-alt));
        border-color: color-mix(in srgb, var(--color-status-warning) 32%, var(--color-border-subtle));
        .def-chip__count { color: var(--color-status-warning); }
        .def-chip__label { color: var(--color-text-secondary); }
      }

      /* Moderate — amber tones */
      .def-chip--moderate {
        background: color-mix(in srgb, var(--color-status-warning) 10%, var(--color-bg-surface-alt));
        border-color: color-mix(in srgb, var(--color-status-warning) 24%, var(--color-border-subtle));
        .def-chip__count { color: var(--color-status-warning); }
        .def-chip__label { color: var(--color-text-secondary); }
      }

      /* Minor — blue tones */
      .def-chip--minor {
        background: color-mix(in srgb, var(--color-status-info) 12%, var(--color-bg-surface-alt));
        border-color: color-mix(in srgb, var(--color-status-info) 30%, var(--color-border-subtle));
        .def-chip__count { color: var(--color-status-info); }
        .def-chip__label { color: var(--color-text-secondary); }
      }

      .def-card__empty-msg {
        font-size: 12px;
        color: var(--color-text-secondary);
        text-align: center;
        padding: 6px 0;
        margin: 0;
      }
    `,
  ],
})
export class DeficiencySummaryCardComponent {
  readonly summary = input<DeficiencySummaryApiDto | null>(null);
  readonly stageLabel = input<string | null>(null);

  protected openCount(): number {
    return this.summary()?.openCount ?? 0;
  }
}
