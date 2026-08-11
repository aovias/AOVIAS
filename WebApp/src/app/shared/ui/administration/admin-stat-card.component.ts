import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

/**
 * Shared Administration metric / KPI card.
 */
@Component({
  selector: 'app-admin-stat-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="admin-stat-card" [class.admin-stat-card--placeholder]="placeholder">
      <span class="admin-stat-card__label">{{ label }}</span>
      <strong class="admin-stat-card__value">{{ value }}</strong>
      @if (hint) {
        <span class="admin-stat-card__hint">{{ hint }}</span>
      }
    </div>
  `,
  styles: [
    `
      .admin-stat-card {
        padding: 16px;
        border-radius: 12px;
        background: color-mix(in srgb, var(--color-bg-muted, #f3f4f6) 70%, transparent);
        border: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .admin-stat-card--placeholder .admin-stat-card__value {
        color: var(--color-text-secondary, #9ca3af);
      }
      .admin-stat-card__label {
        display: block;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
        margin-bottom: 6px;
      }
      .admin-stat-card__value {
        display: block;
        font-size: 20px;
        font-weight: 600;
        line-height: 1.2;
        color: var(--color-text-primary, #111827);
      }
      .admin-stat-card__hint {
        display: block;
        margin-top: 6px;
        font-size: 11px;
        color: var(--color-text-secondary, #9ca3af);
      }
    `,
  ],
})
export class AdminStatCardComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string | number;
  @Input() hint = '';
  @Input() placeholder = false;
}
