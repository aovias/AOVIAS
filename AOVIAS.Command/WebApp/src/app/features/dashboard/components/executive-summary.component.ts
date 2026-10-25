import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

interface SummaryCard {
  title: string;
  value: string | number;
  icon: string;
  color: 'primary' | 'success' | 'warning' | 'danger';
  label?: string;
  actionText?: string;
  actionLink?: string;
  trend?: {
    value: number;
    direction: 'up' | 'down';
  };
}

@Component({
  selector: 'app-executive-summary',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="summary-grid">
      @for (card of cards(); track card.title) {
        <div class="summary-card" [attr.data-color]="card.color" [class.has-action]="card.actionText">
          <div class="card-main">
            <div class="card-icon" [attr.data-color]="card.color">
              <span [innerHTML]="card.icon"></span>
            </div>
            <div class="card-content">
              <div class="card-title">{{ card.title }}</div>
              <div class="card-value" [attr.data-color]="card.color">{{ card.value }}</div>
              @if (card.label) {
                <div class="card-label">{{ card.label }}</div>
              }
              @if (card.trend) {
                <div
                  class="card-trend"
                  [class.trend-up]="card.trend.direction === 'up'"
                  [class.trend-down]="card.trend.direction === 'down'"
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    @if (card.trend.direction === 'up') {
                      <path
                        d="M12 19V5m0 0l-7 7m7-7l7 7"
                        stroke-width="2"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      />
                    } @else {
                      <path
                        d="M12 5v14m0 0l7-7m-7 7l-7-7"
                        stroke-width="2"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      />
                    }
                  </svg>
                  <span>{{ card.trend.value }}% from last week</span>
                </div>
              }
            </div>
          </div>
          @if (card.actionText) {
            <a href="{{ card.actionLink || '#' }}" class="card-action">
              {{ card.actionText }}
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path d="M5 12h14m-7-7l7 7-7 7" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
              </svg>
            </a>
          }
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
        display: flex;
        flex-direction: column;
        gap: 12px;
        height: 100%;
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        box-shadow: var(--shadow-sm);
        position: relative;
        overflow: hidden;
      }

      /* Readiness (first card) is the executive focal point */
      .summary-card:first-child {
        box-shadow: var(--shadow-md);
      }

      .summary-card:first-child::before {
        height: 4px;
      }

      .summary-card:first-child .card-value {
        font-size: 34px;
      }

      .summary-card::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 3px;
        background: transparent;
        transition: background 0.3s ease;
      }

      .summary-card[data-color='primary']::before {
        background: var(--primary-color);
      }

      .summary-card[data-color='success']::before {
        background: var(--success-color);
      }

      .summary-card[data-color='warning']::before {
        background: var(--warning-color);
      }

      .summary-card[data-color='danger']::before {
        background: var(--danger-color);
      }

      /* Overdue / critical cards get a danger tint for at-a-glance risk */
      .summary-card[data-color='danger'] {
        background: linear-gradient(0deg, rgba(239, 68, 68, 0.05), rgba(239, 68, 68, 0.05)), var(--card-bg);
        border-color: rgba(239, 68, 68, 0.25);
      }

      .summary-card:hover {
        transform: translateY(-4px);
        box-shadow: var(--shadow-lg);
        border-color: var(--border-color);
      }

      .card-main {
        display: flex;
        gap: 12px;
        flex: 1;
      }

      .card-icon {
        width: 40px;
        height: 40px;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        transition: all 0.3s ease;
      }

      .card-icon[data-color='primary'] {
        background: rgba(47, 107, 255, 0.1);
        color: var(--primary-color);
      }

      .card-icon[data-color='success'] {
        background: rgba(34, 197, 94, 0.1);
        color: var(--success-color);
      }

      .card-icon[data-color='warning'] {
        background: rgba(245, 158, 11, 0.1);
        color: var(--warning-color);
      }

      .card-icon[data-color='danger'] {
        background: rgba(239, 68, 68, 0.1);
        color: var(--danger-color);
      }

      .summary-card:hover .card-icon {
        transform: scale(1.08) rotate(5deg);
      }

      .card-content {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 4px;
      }

      .card-title {
        font-size: 13px;
        font-weight: 600;
        color: var(--text-secondary);
        letter-spacing: 0.3px;
        text-transform: uppercase;
      }

      .card-value {
        font-size: 30px;
        font-weight: 700;
        line-height: 1.1;
        color: var(--text-primary);
      }

      .card-value[data-color='danger'] {
        color: var(--danger-color);
      }

      .card-value[data-color='success'] {
        color: var(--success-color);
      }

      .card-value[data-color='warning'] {
        color: var(--warning-color);
      }

      .card-label {
        font-size: 12px;
        font-weight: 500;
        color: var(--text-secondary);
        line-height: 1.4;
      }

      .card-trend {
        display: flex;
        align-items: center;
        gap: 5px;
        font-size: 12px;
        font-weight: 600;
        margin-top: 2px;
        padding: 4px 8px;
        border-radius: 6px;
        width: fit-content;
      }

      .card-trend.trend-up {
        color: var(--success-color);
        background: rgba(34, 197, 94, 0.08);
      }

      .card-trend.trend-down {
        color: var(--danger-color);
        background: rgba(239, 68, 68, 0.08);
      }

      .card-action {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 6px;
        padding: 7px 12px;
        background: var(--hover-bg);
        border-radius: 8px;
        font-size: 12px;
        font-weight: 600;
        color: var(--primary-color);
        text-decoration: none;
        transition: all 0.2s ease;
        margin-top: auto;
      }

      .card-action:hover {
        background: var(--primary-bg-light);
        transform: translateX(2px);
      }

      .card-action svg {
        transition: transform 0.2s ease;
      }

      .card-action:hover svg {
        transform: translateX(3px);
      }

      @media (max-width: 1024px) {
        .summary-grid {
          grid-template-columns: repeat(2, 1fr);
        }
      }

      @media (max-width: 640px) {
        .summary-grid {
          grid-template-columns: 1fr;
        }

        .card-value {
          font-size: 28px;
        }
      }
    `,
  ],
})
export class ExecutiveSummaryComponent {
  cards = input.required<SummaryCard[]>();
}
