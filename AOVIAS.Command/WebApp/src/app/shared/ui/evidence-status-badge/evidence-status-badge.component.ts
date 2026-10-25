import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  evidenceStatusLabel,
  evidenceStatusToken,
  type EvidenceStatusPresentationContext,
} from '../../models/evidence-status.model';

@Component({
  selector: 'app-evidence-status-badge',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="ev-status-badge"
      [attr.data-status]="token()"
      [attr.title]="label()"
      role="status"
    >
      {{ label() }}
    </span>
  `,
  styles: [
    `
      .ev-status-badge {
        display: inline-flex;
        align-items: center;
        padding: 2px 8px;
        border-radius: 999px;
        font-size: 11px;
        font-weight: 600;
        line-height: 1.35;
        border: 1px solid transparent;
        white-space: nowrap;
      }

      .ev-status-badge[data-status='draft'] {
        background: color-mix(in srgb, var(--color-text-secondary, #64748b) 12%, transparent);
        color: var(--color-text-secondary, #64748b);
        border-color: color-mix(in srgb, var(--color-text-secondary, #64748b) 24%, transparent);
      }

      .ev-status-badge[data-status='uploaded'],
      .ev-status-badge[data-status='submitted'] {
        background: color-mix(in srgb, var(--color-info, #0284c7) 12%, transparent);
        color: var(--color-info, #0284c7);
        border-color: color-mix(in srgb, var(--color-info, #0284c7) 26%, transparent);
      }

      .ev-status-badge[data-status='pendingReview'] {
        background: color-mix(in srgb, var(--color-warning, #d97706) 14%, transparent);
        color: var(--color-warning, #d97706);
        border-color: color-mix(in srgb, var(--color-warning, #d97706) 28%, transparent);
      }

      .ev-status-badge[data-status='approved'],
      .ev-status-badge[data-status='verified'] {
        background: color-mix(in srgb, var(--color-success, #16a34a) 14%, transparent);
        color: var(--color-success, #16a34a);
        border-color: color-mix(in srgb, var(--color-success, #16a34a) 28%, transparent);
      }

      .ev-status-badge[data-status='rejected'] {
        background: color-mix(in srgb, var(--color-danger, #dc2626) 12%, transparent);
        color: var(--color-danger, #dc2626);
        border-color: color-mix(in srgb, var(--color-danger, #dc2626) 26%, transparent);
      }

      .ev-status-badge[data-status='needsMoreEvidence'] {
        background: color-mix(in srgb, var(--color-warning, #ea580c) 14%, transparent);
        color: var(--color-warning, #ea580c);
        border-color: color-mix(in srgb, var(--color-warning, #ea580c) 28%, transparent);
      }

      @media (prefers-color-scheme: dark) {
        .ev-status-badge {
          font-weight: 650;
        }
      }
    `,
  ],
})
export class EvidenceStatusBadgeComponent {
  readonly status = input.required<string | null | undefined>();
  readonly context = input<EvidenceStatusPresentationContext>('evidence');
  readonly size = input<'default' | 'large'>('default');

  readonly token = computed(() => evidenceStatusToken(this.status(), this.context()));
  readonly label = computed(() => evidenceStatusLabel(this.status(), this.context()));
}
