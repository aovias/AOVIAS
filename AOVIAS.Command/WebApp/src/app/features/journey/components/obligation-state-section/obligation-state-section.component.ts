import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ObligationStateApiDto } from '../../models/journey-api.model';
import { ObligationReadinessBarComponent } from '../obligation-readiness-bar/obligation-readiness-bar.component';

/**
 * Obligation state section for the OE context panel.
 * Shows implementation/assessment/evidence/verification badges, readiness score, dates, and next action CTA.
 */
@Component({
  selector: 'app-obligation-state-section',
  standalone: true,
  imports: [CommonModule, ObligationReadinessBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (obligationState()) {
      <article class="obl-state" aria-label="Obligation compliance state">
        <header class="obl-state__header">
          <h3 class="obl-state__title">Obligation State</h3>
          <span class="obl-state__readiness-score">
            {{ obligationState()!.readinessScore | number: '1.0-0' }}%
          </span>
        </header>

        <app-obligation-readiness-bar
          [score]="obligationState()!.readinessScore"
          label="Readiness"
        />

        <div class="obl-state__badges" role="list" aria-label="Status indicators">
          <div class="obl-state__badge-item" role="listitem">
            <span class="obl-state__badge-label">Assessment</span>
            <span
              class="obl-state__badge"
              [attr.data-status]="statusTone(obligationState()!.assessmentStatus)"
            >
              {{ obligationState()!.assessmentStatus | titlecase }}
            </span>
          </div>
          <div class="obl-state__badge-item" role="listitem">
            <span class="obl-state__badge-label">Implementation</span>
            <span
              class="obl-state__badge"
              [attr.data-status]="statusTone(obligationState()!.implementationStatus)"
            >
              {{ obligationState()!.implementationStatus | titlecase }}
            </span>
          </div>
          <div class="obl-state__badge-item" role="listitem">
            <span class="obl-state__badge-label">Evidence</span>
            <span
              class="obl-state__badge"
              [attr.data-status]="statusTone(obligationState()!.evidenceStatus)"
            >
              {{ obligationState()!.evidenceStatus | titlecase }}
            </span>
          </div>
          <div class="obl-state__badge-item" role="listitem">
            <span class="obl-state__badge-label">Verification</span>
            <span
              class="obl-state__badge"
              [attr.data-status]="statusTone(obligationState()!.verificationStatus)"
            >
              {{ obligationState()!.verificationStatus | titlecase }}
            </span>
          </div>
        </div>

        @if (obligationState()!.lastAssessmentDate) {
          <p class="obl-state__date">
            Last assessed:
            {{ obligationState()!.lastAssessmentDate | date: 'mediumDate' }}
          </p>
        }
        @if (obligationState()!.lastVerificationDate) {
          <p class="obl-state__date">
            Last verified:
            {{ obligationState()!.lastVerificationDate | date: 'mediumDate' }}
          </p>
        }

        @if (obligationState()!.nextActionLabel) {
          <div
            class="obl-state__next-action"
            [class.obl-state__next-action--urgent]="obligationState()!.isNextActionUrgent"
          >
            <span class="obl-state__next-action-label">Next action</span>
            <span class="obl-state__next-action-value">
              {{ obligationState()!.nextActionLabel }}
            </span>
          </div>
        }
      </article>
    }
  `,
  styles: [
    `
      .obl-state {
        background: var(--color-bg-subtle, #f8fafc);
        border: 1px solid var(--color-border-subtle, #e2e8f0);
        border-radius: 10px;
        padding: 14px;
        display: flex;
        flex-direction: column;
        gap: 10px;
      }

      .obl-state__header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .obl-state__title {
        font-size: 13px;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0;
      }

      .obl-state__readiness-score {
        font-size: 15px;
        font-weight: 700;
        color: var(--color-text-primary);
      }

      .obl-state__badges {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 6px;
      }

      .obl-state__badge-item {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }

      .obl-state__badge-label {
        font-size: 10px;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--color-text-tertiary);
      }

      .obl-state__badge {
        font-size: 11px;
        font-weight: 600;
        padding: 2px 7px;
        border-radius: 4px;
        display: inline-block;
        background: var(--color-bg-subtle);

        &[data-status='success'] {
          background: color-mix(in srgb, var(--color-success, #16a34a) 12%, transparent);
          color: var(--color-success, #16a34a);
        }
        &[data-status='warning'] {
          background: color-mix(in srgb, var(--color-warning, #d97706) 12%, transparent);
          color: var(--color-warning, #d97706);
        }
        &[data-status='danger'] {
          background: color-mix(in srgb, var(--color-danger, #dc2626) 12%, transparent);
          color: var(--color-danger, #dc2626);
        }
        &[data-status='neutral'] {
          background: var(--color-bg-subtle, #f1f5f9);
          color: var(--color-text-tertiary);
        }
      }

      .obl-state__date {
        font-size: 11px;
        color: var(--color-text-tertiary);
        margin: 0;
      }

      .obl-state__next-action {
        display: flex;
        flex-direction: column;
        gap: 2px;
        padding: 8px 10px;
        border-radius: 6px;
        background: color-mix(in srgb, var(--color-info, #0284c7) 10%, transparent);
        border-left: 3px solid var(--color-info, #0284c7);
      }

      .obl-state__next-action--urgent {
        background: color-mix(in srgb, var(--color-warning, #d97706) 10%, transparent);
        border-left-color: var(--color-warning, #d97706);
      }

      .obl-state__next-action-label {
        font-size: 10px;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--color-text-tertiary);
      }

      .obl-state__next-action-value {
        font-size: 12px;
        font-weight: 600;
        color: var(--color-text-primary);
      }
    `,
  ],
})
export class ObligationStateSectionComponent {
  readonly obligationState = input<ObligationStateApiDto | null>(null);

  statusTone(status: string): 'success' | 'warning' | 'danger' | 'neutral' {
    const s = status?.toLowerCase() ?? '';
    if (['passed', 'implemented', 'verified', 'completed'].includes(s))
      return 'success';
    if (['partiallypassed', 'partiallyimplemented', 'partial', 'submitted', 'pending', 'inprogress'].includes(s))
      return 'warning';
    if (['failed', 'notimplemented', 'rejected', 'missing'].includes(s))
      return 'danger';
    return 'neutral';
  }
}
