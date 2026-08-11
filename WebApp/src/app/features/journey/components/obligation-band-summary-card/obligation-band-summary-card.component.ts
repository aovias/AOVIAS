import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ObligationBandSummaryApiDto } from '../../models/journey-api.model';
import { ObligationReadinessBarComponent } from '../obligation-readiness-bar/obligation-readiness-bar.component';

export interface ObligationBandSummaryVm {
  stage: 'core' | 'commitment' | 'achievement' | 'excellence';
  label: string;
  summary: ObligationBandSummaryApiDto;
}

/**
 * Executive obligation summary card for a single maturity band (Core/Commitment/Achievement/Excellence).
 * Used in the Maturity Dashboard obligation summary section.
 */
@Component({
  selector: 'app-obligation-band-summary-card',
  standalone: true,
  imports: [CommonModule, ObligationReadinessBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="obs-card"
      [attr.data-stage]="vm().stage"
      [attr.aria-label]="vm().label + ' obligation summary'"
    >
      <header class="obs-card__header">
        <span class="obs-card__stage-label">{{ vm().label }}</span>
        <span class="obs-card__total" [attr.aria-label]="vm().summary.total + ' total obligations'">
          {{ vm().summary.total }}
        </span>
      </header>

      <app-obligation-readiness-bar
        [score]="vm().summary.readinessScore"
        label="Readiness"
      />

      <dl class="obs-card__counts">
        <div class="obs-card__count-item obs-card__count-item--verified">
          <dt>Verified</dt>
          <dd>{{ vm().summary.verified }}</dd>
        </div>
        <div class="obs-card__count-item obs-card__count-item--implemented">
          <dt>Implemented</dt>
          <dd>{{ vm().summary.implemented }}</dd>
        </div>
        <div class="obs-card__count-item obs-card__count-item--partial">
          <dt>Partial</dt>
          <dd>{{ vm().summary.partial }}</dd>
        </div>
        <div class="obs-card__count-item obs-card__count-item--deficient">
          <dt>Deficient</dt>
          <dd>{{ vm().summary.deficient }}</dd>
        </div>
        <div class="obs-card__count-item obs-card__count-item--not-started">
          <dt>Not Started</dt>
          <dd>{{ vm().summary.notStarted ?? 0 }}</dd>
        </div>
        <div class="obs-card__count-item obs-card__count-item--unknown">
          <dt>Unknown</dt>
          <dd>{{ vm().summary.unknown ?? 0 }}</dd>
        </div>
      </dl>
      <p class="obs-card__reconcile" aria-label="Status bucket reconciliation">
        Status total: {{ statusBucketTotal() }} / {{ vm().summary.total }} obligations
      </p>
    </article>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .obs-card {
        background: var(--color-bg-surface);
        border: 1.5px solid var(--color-border-subtle);
        border-radius: 10px;
        padding: 16px;
        display: flex;
        flex-direction: column;
        gap: 12px;
        height: 100%;
        box-sizing: border-box;

        &[data-stage='core']        { --band-color: var(--color-status-success); }
        &[data-stage='commitment']  { --band-color: var(--color-status-info); }
        &[data-stage='achievement'] { --band-color: var(--color-status-warning); }
        &[data-stage='excellence']  { --band-color: var(--color-brand-primary); }

        border-top: 3px solid var(--band-color);
      }

      .obs-card__header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .obs-card__stage-label {
        font-size: 12px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--band-color);
      }

      .obs-card__total {
        font-size: 24px;
        font-weight: 800;
        color: var(--color-text-primary);
        line-height: 1;
      }

      .obs-card__counts {
        display: grid;
        grid-template-columns: 1fr 1fr 1fr;
        gap: 6px;
        margin: 0;
      }

      .obs-card__count-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 6px 8px;
        border-radius: 6px;
        background: var(--color-bg-surface-alt);
        border: 1px solid var(--color-border-subtle);

        dt {
          font-size: 10px;
          text-transform: uppercase;
          letter-spacing: 0.04em;
          color: var(--color-text-secondary);
        }

        dd {
          font-size: 16px;
          font-weight: 700;
          color: var(--color-text-primary);
          margin: 0;
        }
      }

      .obs-card__count-item--verified {
        background: color-mix(in srgb, var(--color-status-success) 12%, var(--color-bg-surface-alt));
        border-color: color-mix(in srgb, var(--color-status-success) 28%, var(--color-border-subtle));
      }

      .obs-card__count-item--verified dd {
        color: var(--color-status-success);
      }

      .obs-card__count-item--implemented {
        background: color-mix(in srgb, var(--color-status-info) 12%, var(--color-bg-surface-alt));
        border-color: color-mix(in srgb, var(--color-status-info) 28%, var(--color-border-subtle));
      }

      .obs-card__count-item--implemented dd {
        color: var(--color-status-info);
      }

      .obs-card__count-item--partial {
        background: color-mix(in srgb, var(--color-status-warning) 12%, var(--color-bg-surface-alt));
        border-color: color-mix(in srgb, var(--color-status-warning) 28%, var(--color-border-subtle));
      }

      .obs-card__count-item--partial dd {
        color: var(--color-status-warning);
      }

      .obs-card__count-item--deficient {
        background: color-mix(in srgb, var(--color-status-danger) 12%, var(--color-bg-surface-alt));
        border-color: color-mix(in srgb, var(--color-status-danger) 28%, var(--color-border-subtle));
      }

      .obs-card__count-item--deficient dd {
        color: var(--color-status-danger);
      }

      .obs-card__count-item--not-started {
        background: var(--color-bg-surface-alt);
      }

      .obs-card__count-item--not-started dd {
        color: var(--color-text-secondary);
      }

      .obs-card__count-item--unknown {
        background: var(--color-bg-surface-alt);
      }

      .obs-card__count-item--unknown dd {
        color: var(--color-text-tertiary, var(--color-text-secondary));
      }

      .obs-card__reconcile {
        font-size: 11px;
        color: var(--color-text-tertiary, var(--color-text-secondary));
        margin: 0;
        text-align: right;
      }
    `,
  ],
})
export class ObligationBandSummaryCardComponent {
  readonly vm = input.required<ObligationBandSummaryVm>();

  /** Sum of mutually exclusive primary status buckets — should equal total. */
  statusBucketTotal = () => {
    const s = this.vm().summary;
    return (s.verified ?? 0) + (s.implemented ?? 0) + (s.partial ?? 0)
      + (s.deficient ?? 0) + (s.notStarted ?? 0) + (s.unknown ?? 0);
  };
}
