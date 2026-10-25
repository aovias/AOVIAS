import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GapCategoryKpi } from '../models/gap-analysis-vm.model';

@Component({
  selector: 'app-gap-category-kpi-strip',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="summary-grid" role="list" aria-label="Gap category summary">
      @for (card of kpis; track card.title) {
        <div class="summary-card" [attr.data-color]="card.color" role="listitem">
          <div class="card-title">{{ card.title }}</div>
          <div class="card-value" [attr.data-color]="card.color">{{ card.value }}</div>
          <div class="card-label">{{ card.label }}</div>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .summary-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 16px;
      }

      .summary-card {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
      }

      .card-title {
        font-size: 13px;
        font-weight: 500;
        color: var(--text-secondary);
        margin-bottom: 8px;
      }

      .card-value {
        font-size: 28px;
        font-weight: 700;
        line-height: 1.2;
      }

      .card-value[data-color='danger'] {
        color: var(--danger-color, #dc2626);
      }

      .card-value[data-color='warning'] {
        color: var(--warning-color, #d97706);
      }

      .card-value[data-color='primary'] {
        color: var(--primary-color, var(--color-primary));
      }

      .card-value[data-color='success'] {
        color: var(--success-color, #059669);
      }

      .card-label {
        font-size: 12px;
        color: var(--text-secondary);
        margin-top: 4px;
      }

      @media (max-width: 1199px) {
        .summary-grid {
          grid-template-columns: repeat(2, 1fr);
        }
      }

      @media (max-width: 480px) {
        .summary-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class GapCategoryKpiStripComponent {
  @Input({ required: true }) kpis: GapCategoryKpi[] = [];
}
