import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Thin shared KPI card for role-experience / analytics surfaces.
 */
@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="kpi-card"
      [class.kpi-card--tone-critical]="tone === 'critical'"
      [class.kpi-card--tone-warning]="tone === 'warning'"
      role="group"
      [attr.aria-label]="ariaLabel || label + ': ' + value"
    >
      <span class="kpi-card__label" [id]="labelId">{{ label }}</span>
      <strong class="kpi-card__value" [attr.aria-describedby]="hint ? hintId : labelId">{{ value }}</strong>
      @if (hint) {
        <span class="kpi-card__hint" [id]="hintId">{{ hint }}</span>
      }
    </div>
  `,
  styles: [
    `
      .kpi-card {
        padding: 14px 16px;
        border-radius: 10px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
        min-width: 120px;
      }
      @media (max-width: 1024px) {
        .kpi-card {
          min-width: 0;
        }
        .kpi-card__value {
          font-size: 20px;
        }
      }
      .kpi-card__label {
        display: block;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
        margin-bottom: 4px;
      }
      .kpi-card__value {
        display: block;
        font-size: 22px;
        font-weight: 650;
        color: var(--color-text-primary, #111827);
        line-height: 1.2;
      }
      .kpi-card__hint {
        display: block;
        margin-top: 4px;
        font-size: 11px;
        color: var(--color-text-secondary, #9ca3af);
      }
      .kpi-card--tone-critical .kpi-card__value {
        color: var(--color-danger, #b91c1c);
      }
      .kpi-card--tone-warning .kpi-card__value {
        color: var(--color-warning, #b45309);
      }
    `,
  ],
})
export class KpiCardComponent {
  private static nextId = 0;
  private readonly uid = KpiCardComponent.nextId++;

  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string | number;
  @Input() hint = '';
  @Input() tone: 'default' | 'warning' | 'critical' = 'default';
  /** Optional override for the group aria-label. */
  @Input() ariaLabel = '';

  get labelId(): string {
    return `kpi-label-${this.uid}`;
  }

  get hintId(): string {
    return `kpi-hint-${this.uid}`;
  }
}
