import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-binder-summary-cards',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="readiness-cards">
      @if (loading()) {
        <div class="loading-row"><mat-spinner diameter="36" /></div>
      } @else {
        @for (card of cards(); track card.label) {
          <div class="readiness-card" [attr.data-color]="card.color">
            <div class="card-content">
              <div class="card-value">{{ card.value }}</div>
              <div class="card-label">{{ card.label }}</div>
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: [
    `
      .readiness-cards {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 16px;
        margin-bottom: 24px;
      }
      .readiness-card {
        padding: 18px;
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 12px;
      }
      .readiness-card[data-color='green'] {
        border-left: 3px solid #22c55e;
      }
      .readiness-card[data-color='red'] {
        border-left: 3px solid #ef4444;
      }
      .readiness-card[data-color='blue'] {
        border-left: 3px solid #3b82f6;
      }
      .card-value {
        font-size: 24px;
        font-weight: 700;
      }
      .card-label {
        font-size: 12px;
        color: var(--text-secondary, #6b7280);
        margin-top: 4px;
      }
      .loading-row {
        display: flex;
        justify-content: center;
        padding: 24px;
      }
    `,
  ],
})
export class SummaryCardsComponent {
  readonly cards = input<{ label: string; value: number | string; color: 'green' | 'red' | 'blue' }[]>([]);
  readonly loading = input(false);
}
