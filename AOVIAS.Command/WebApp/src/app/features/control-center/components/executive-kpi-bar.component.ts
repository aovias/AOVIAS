import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface KPICard {
  title: string;
  value: number | string;
  unit?: string;
  trend: 'up' | 'down' | 'neutral';
  trendValue: string;
  sparklineData: number[];
  color: string;
  icon?: string;
}

@Component({
  selector: 'app-executive-kpi-bar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="kpi-bar">
      <div *ngFor="let kpi of kpis"
           class="kpi-card"
           (click)="onCardClick(kpi)">
        <div class="kpi-header">
          <div class="kpi-icon" [style.background]="kpi.color + '20'">
            <span [style.color]="kpi.color">{{ kpi.icon || '📊' }}</span>
          </div>
          <div class="trend-indicator" [class.trend-up]="kpi.trend === 'up'"
               [class.trend-down]="kpi.trend === 'down'"
               [class.trend-neutral]="kpi.trend === 'neutral'">
            <span class="trend-arrow">
              {{ kpi.trend === 'up' ? '↗' : kpi.trend === 'down' ? '↘' : '→' }}
            </span>
            <span class="trend-value">{{ kpi.trendValue }}</span>
          </div>
        </div>

        <div class="kpi-body">
          <h3 class="kpi-title">{{ kpi.title }}</h3>
          <div class="kpi-value-wrapper">
            <span class="kpi-value">{{ kpi.value }}</span>
            <span *ngIf="kpi.unit" class="kpi-unit">{{ kpi.unit }}</span>
          </div>
        </div>

        <div class="kpi-sparkline">
          <svg [attr.viewBox]="'0 0 ' + kpi.sparklineData.length * 10 + ' 40'" preserveAspectRatio="none">
            <polyline
              [attr.points]="getSparklinePoints(kpi.sparklineData)"
              [attr.stroke]="kpi.color"
              stroke-width="2"
              fill="none"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
            <polyline
              [attr.points]="getSparklinePoints(kpi.sparklineData)"
              [attr.fill]="kpi.color + '20'"
              stroke="none"
            />
          </svg>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .kpi-bar {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
    }

    .kpi-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
      transition: all 0.2s ease;
      cursor: pointer;
      position: relative;
      overflow: hidden;
    }

    .kpi-card:hover {
      transform: translateY(-2px);
      box-shadow: var(--shadow-md);
    }

    .kpi-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .kpi-icon {
      width: 40px;
      height: 40px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 20px;
    }

    .trend-indicator {
      display: flex;
      align-items: center;
      gap: 4px;
      padding: 4px 10px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
    }

    .trend-up {
      background: rgba(34, 197, 94, 0.1);
      color: var(--success-color);
    }

    .trend-down {
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color);
    }

    .trend-neutral {
      background: var(--bg-tertiary);
      color: var(--text-secondary);
    }

    .trend-arrow {
      font-size: 14px;
    }

    .kpi-body {
      margin-bottom: 16px;
    }

    .kpi-title {
      font-size: 13px;
      font-weight: 500;
      color: var(--text-secondary);
      margin: 0 0 8px 0;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .kpi-value-wrapper {
      display: flex;
      align-items: baseline;
      gap: 4px;
    }

    .kpi-value {
      font-size: 28px;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
    }

    .kpi-unit {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-secondary);
    }

    .kpi-sparkline {
      height: 40px;
      margin: 0 -4px;
    }

    .kpi-sparkline svg {
      width: 100%;
      height: 100%;
    }

    @media (max-width: 768px) {
      .kpi-bar {
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 12px;
      }

      .kpi-card {
        padding: 16px;
      }

      .kpi-value {
        font-size: 24px;
      }
    }
  `]
})
export class ExecutiveKPIBarComponent {
  @Input() kpis: KPICard[] = [];

  getSparklinePoints(data: number[]): string {
    const max = Math.max(...data);
    const min = Math.min(...data);
    const range = max - min || 1;

    const points = data.map((value, index) => {
      const x = index * 10;
      const y = 40 - ((value - min) / range) * 35;
      return `${x},${y}`;
    });

    return points.join(' ');
  }

  onCardClick(kpi: KPICard): void {
    console.log('KPI clicked:', kpi);
  }
}
