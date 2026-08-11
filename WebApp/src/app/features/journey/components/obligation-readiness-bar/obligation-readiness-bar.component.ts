import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Inline mini readiness progress bar for OE list rows and chapter panels.
 * Color derived from score: <40 → danger, 40-70 → warning, >70 → success.
 */
@Component({
  selector: 'app-obligation-readiness-bar',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="orb"
      [attr.aria-label]="'Obligation readiness ' + score() + ' percent'"
      role="progressbar"
      [attr.aria-valuenow]="score()"
      aria-valuemin="0"
      aria-valuemax="100"
    >
      @if (label()) {
        <span class="orb__label">{{ label() }}</span>
      }
      <div class="orb__track">
        <div
          class="orb__fill"
          [attr.data-tone]="tone()"
          [style.width.%]="clampedScore()"
        ></div>
      </div>
      <span class="orb__value" [attr.data-tone]="tone()">
        {{ score() | number: '1.0-0' }}%
      </span>
    </div>
  `,
  styles: [
    `
      .orb {
        display: flex;
        align-items: center;
        gap: 6px;
        min-width: 0;
      }

      .orb__label {
        font-size: 11px;
        color: var(--color-text-secondary);
        white-space: nowrap;
        flex-shrink: 0;
      }

      .orb__track {
        flex: 1;
        height: 4px;
        border-radius: 2px;
        background: var(--color-bg-surface-alt);
        border: 1px solid var(--color-border-subtle);
        overflow: hidden;
        min-width: 40px;
      }

      .orb__fill {
        height: 100%;
        border-radius: 2px;
        transition: width 0.4s ease;

        &[data-tone='success'] { background: var(--color-status-success); }
        &[data-tone='warning'] { background: var(--color-status-warning); }
        &[data-tone='danger']  { background: var(--color-status-danger); }
        &[data-tone='neutral'] { background: var(--color-text-muted); }
      }

      .orb__value {
        font-size: 11px;
        font-weight: 600;
        white-space: nowrap;
        flex-shrink: 0;

        &[data-tone='success'] { color: var(--color-status-success); }
        &[data-tone='warning'] { color: var(--color-status-warning); }
        &[data-tone='danger']  { color: var(--color-status-danger); }
        &[data-tone='neutral'] { color: var(--color-text-secondary); }
      }
    `,
  ],
})
export class ObligationReadinessBarComponent {
  /** 0–100 readiness score. */
  readonly score = input.required<number>();
  /** Optional label shown before the bar. */
  readonly label = input<string | null>(null);

  clampedScore(): number {
    return Math.min(100, Math.max(0, this.score()));
  }

  tone(): 'success' | 'warning' | 'danger' | 'neutral' {
    const s = this.score();
    if (s > 70) return 'success';
    if (s >= 40) return 'warning';
    if (s > 0) return 'danger';
    return 'neutral';
  }
}
