import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  COMPLIANCE_CLASSIFICATION_LABEL,
  type ComplianceItemClassification,
} from '../../models/compliance-classification.model';

@Component({
  selector: 'app-compliance-classification-badge',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="cc-badge"
      [attr.data-classification]="classification()"
      [attr.title]="label()"
    >
      {{ label() }}
    </span>
  `,
  styles: [`
    .cc-badge {
      display: inline-flex;
      align-items: center;
      padding: 2px 8px;
      border-radius: 999px;
      font-size: 10px;
      font-weight: 700;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      line-height: 1.4;
      border: 1px solid transparent;
    }

    .cc-badge[data-classification='auditFinding'] {
      background: color-mix(in srgb, var(--color-info, #0284c7) 12%, transparent);
      color: var(--color-info, #0284c7);
      border-color: color-mix(in srgb, var(--color-info, #0284c7) 28%, transparent);
    }

    .cc-badge[data-classification='complianceDeficiency'] {
      background: color-mix(in srgb, var(--color-warning, #d97706) 12%, transparent);
      color: var(--color-warning, #d97706);
      border-color: color-mix(in srgb, var(--color-warning, #d97706) 28%, transparent);
    }

    .cc-badge[data-classification='predictedGap'] {
      background: color-mix(in srgb, var(--color-text-secondary, #64748b) 10%, transparent);
      color: var(--color-text-secondary, #64748b);
      border-color: color-mix(in srgb, var(--color-text-secondary, #64748b) 22%, transparent);
    }
  `],
})
export class ComplianceClassificationBadgeComponent {
  readonly classification = input.required<ComplianceItemClassification>();

  readonly label = computed(
    () => COMPLIANCE_CLASSIFICATION_LABEL[this.classification()] ?? 'Compliance Item',
  );
}
