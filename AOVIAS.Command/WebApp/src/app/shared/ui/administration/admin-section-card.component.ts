import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/**
 * Shared Administration section / capability card for overview dashboards.
 */
@Component({
  selector: 'app-admin-section-card',
  standalone: true,
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="admin-section-card" [class.admin-section-card--soon]="comingSoon">
      <mat-icon aria-hidden="true">{{ icon }}</mat-icon>
      <div class="admin-section-card__body">
        <div class="admin-section-card__title">
          <strong>{{ title }}</strong>
          <ng-content select="[badge]" />
        </div>
        @if (description) {
          <span>{{ description }}</span>
        }
        <ng-content />
      </div>
    </div>
  `,
  styles: [
    `
      .admin-section-card {
        display: flex;
        gap: 12px;
        align-items: flex-start;
        padding: 16px;
        border-radius: 16px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
        color: inherit;
      }
      .admin-section-card--soon {
        opacity: 0.92;
      }
      .admin-section-card mat-icon {
        color: var(--color-primary);
      }
      .admin-section-card__title {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
        margin-bottom: 4px;
      }
      .admin-section-card strong {
        font-size: 14px;
      }
      .admin-section-card span {
        display: block;
        font-size: 12px;
        line-height: 1.4;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class AdminSectionCardComponent {
  @Input({ required: true }) title!: string;
  @Input() description = '';
  @Input() icon = 'widgets';
  @Input() comingSoon = false;
}
