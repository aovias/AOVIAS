import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

interface RiskData {
  label: string;
  value: number;
  color: string;
}

@Component({
  selector: 'app-risk-distribution',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="risk-container">
      <div class="risk-header">
        <h3 class="risk-title">{{ title() }}</h3>
        <span class="risk-total-chip">{{ total() }} total</span>
      </div>

      <!-- Stacked share bar gives an at-a-glance split even when one band dominates -->
      <div class="share-bar" role="img" [attr.aria-label]="title() + ' distribution'">
        @for (row of rows(); track row.label) {
          @if (row.value > 0) {
            <span
              class="share-seg"
              [style.flex-grow]="row.value"
              [style.background]="row.color"
              [attr.title]="row.label + ': ' + row.value + ' (' + row.percent + '%)'"
            ></span>
          }
        }
        @if (total() === 0) {
          <span class="share-seg share-seg--empty"></span>
        }
      </div>

      <div class="rows">
        @for (row of rows(); track row.label) {
          <div class="bar-row" [class.is-zero]="row.value === 0">
            <span class="dot" [style.background]="row.color"></span>
            <span class="bar-label">{{ row.label }}</span>
            <div class="bar-track">
              <div class="bar-fill" [style.width.%]="row.percent" [style.background]="row.color"></div>
            </div>
            <span class="bar-value">{{ row.value }}</span>
            <span class="bar-pct">{{ row.percent }}%</span>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
      }

      .risk-container {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
        transition: all 0.3s ease;
        height: 100%;
        box-sizing: border-box;
        display: flex;
        flex-direction: column;
      }

      /* Bars take the remaining height and space out so the card never looks empty */
      .rows {
        flex: 1;
        justify-content: space-evenly;
      }

      .risk-container:hover {
        box-shadow: var(--shadow-md);
      }

      .risk-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 12px;
      }

      .risk-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .risk-total-chip {
        font-size: 11px;
        font-weight: 700;
        color: var(--text-secondary);
        background: var(--hover-bg);
        padding: 2px 10px;
        border-radius: 12px;
      }

      /* Stacked proportional bar — fills full card width */
      .share-bar {
        display: flex;
        height: 10px;
        width: 100%;
        border-radius: 5px;
        overflow: hidden;
        background: var(--border-color);
        margin-bottom: 14px;
      }

      .share-seg {
        height: 100%;
        min-width: 2px;
        transition: flex-grow 0.6s ease;
      }

      .share-seg--empty {
        flex: 1;
        background: var(--border-color);
      }

      .rows {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .bar-row {
        display: grid;
        grid-template-columns: 14px minmax(70px, auto) 1fr 36px 44px;
        align-items: center;
        gap: 10px;
      }

      .bar-row.is-zero {
        opacity: 0.55;
      }

      .dot {
        width: 10px;
        height: 10px;
        border-radius: 3px;
      }

      .bar-label {
        font-size: 13px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .bar-track {
        height: 8px;
        background: var(--border-color);
        border-radius: 4px;
        overflow: hidden;
      }

      .bar-fill {
        height: 100%;
        border-radius: 4px;
        transition: width 0.6s ease;
      }

      .bar-value {
        font-size: 15px;
        font-weight: 700;
        color: var(--text-primary);
        text-align: right;
      }

      .bar-pct {
        font-size: 12px;
        font-weight: 600;
        color: var(--text-secondary);
        text-align: right;
      }
    `,
  ],
})
export class RiskDistributionComponent {
  title = input<string>('Risk Distribution');
  data = input.required<RiskData[]>();

  total = computed(() => this.data().reduce((sum, item) => sum + item.value, 0));

  rows = computed(() => {
    const total = this.total();
    return this.data().map((item) => ({
      label: item.label,
      color: item.color,
      value: item.value,
      percent: total > 0 ? Math.round((item.value / total) * 100) : 0,
    }));
  });
}
