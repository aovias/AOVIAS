import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScoreTrend } from '../models/dashboard-snapshot.model';
import { compareFrameworkCodeOrder } from '../../../shared/utils/framework-code-sort';

@Component({
  selector: 'app-score-trend-chart',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="trend-container">
      <h3 class="trend-title">{{ title() }}</h3>
      <div class="chart-wrapper">
        <svg class="line-chart" [attr.viewBox]="'0 0 ' + chartWidth + ' ' + chartHeight">
          <!-- Y-axis grid lines -->
          @for (yLine of yGridLines; track yLine.value) {
            <line
              [attr.x1]="marginLeft"
              [attr.y1]="yLine.y"
              [attr.x2]="chartWidth - marginRight"
              [attr.y2]="yLine.y"
              class="grid-line"
            />
            <text
              [attr.x]="marginLeft - 8"
              [attr.y]="yLine.y + 4"
              class="axis-label"
              text-anchor="end"
            >
              {{ yLine.value }}
            </text>
          }

          <!-- X-axis labels -->
          @for (point of avgPoints(); track point.label) {
            <text
              [attr.x]="point.x"
              [attr.y]="chartHeight - 8"
              class="axis-label"
              text-anchor="middle"
            >
              {{ point.label }}
            </text>
          }

          <!-- Framework lines -->
          @for (line of frameworkLines(); track line.code) {
            <polyline
              [attr.points]="line.points"
              [attr.stroke]="line.color"
              fill="none"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
              class="trend-line"
            />
            @for (dot of line.dots; track dot.x) {
              <circle
                [attr.cx]="dot.x"
                [attr.cy]="dot.y"
                r="4"
                [attr.fill]="line.color"
                [attr.stroke]="'var(--card-bg)'"
                stroke-width="2"
                class="trend-dot"
              />
            }
          }

          <!-- Average line (thicker) -->
          <polyline
            [attr.points]="avgPolyline()"
            stroke="var(--primary-color)"
            fill="none"
            stroke-width="3"
            stroke-linecap="round"
            stroke-linejoin="round"
            class="trend-line avg-line"
          />
          @for (point of avgPoints(); track point.label) {
            <circle
              [attr.cx]="point.x"
              [attr.cy]="point.y"
              r="5"
              fill="var(--primary-color)"
              [attr.stroke]="'var(--card-bg)'"
              stroke-width="2"
              class="trend-dot"
            />
          }

          <!-- X axis -->
          <line
            [attr.x1]="marginLeft"
            [attr.y1]="chartHeight - marginBottom"
            [attr.x2]="chartWidth - marginRight"
            [attr.y2]="chartHeight - marginBottom"
            stroke="var(--border-color)"
            stroke-width="1"
          />
        </svg>
      </div>

      <!-- Legend -->
      <div class="legend">
        <div class="legend-item">
          <span class="legend-line" style="background: var(--primary-color); height: 3px;"></span>
          <span class="legend-label">Average</span>
        </div>
        @for (fw of frameworkCodes(); track fw.code) {
          <div class="legend-item">
            <span class="legend-line" [style.background]="fw.color"></span>
            <span class="legend-label">{{ fw.code }}</span>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
      .trend-container {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
        transition: all 0.3s ease;
        height: 100%;
        display: flex;
        flex-direction: column;
      }

      .trend-container:hover {
        box-shadow: var(--shadow-md);
      }

      .trend-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
        margin-bottom: 12px;
      }

      .chart-wrapper {
        overflow-x: auto;
        flex: 1;
        display: flex;
        flex-direction: column;
        justify-content: center;
      }

      .line-chart {
        width: 100%;
        min-width: 500px;
        height: auto;
      }

      .grid-line {
        stroke: var(--border-color);
        stroke-width: 1;
        stroke-dasharray: 4 4;
        opacity: 0.5;
      }

      .axis-label {
        font-size: 11px;
        fill: var(--text-secondary);
      }

      .trend-line {
        transition: opacity 0.3s ease;
      }

      .trend-dot {
        transition: r 0.2s ease;
      }

      .trend-dot:hover {
        r: 6;
      }

      .legend {
        display: flex;
        align-items: center;
        gap: 20px;
        margin-top: 10px;
        padding-top: 10px;
        border-top: 1px solid var(--border-color);
        flex-wrap: wrap;
      }

      .legend-item {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .legend-line {
        display: inline-block;
        width: 24px;
        height: 2px;
        border-radius: 1px;
      }

      .legend-label {
        font-size: 12px;
        font-weight: 500;
        color: var(--text-secondary);
      }

      @media (max-width: 640px) {
        .legend {
          gap: 12px;
        }
      }
    `,
  ],
})
export class ScoreTrendChartComponent {
  title = input<string>('Compliance Score Trend');
  data = input.required<ScoreTrend[]>();

  readonly chartWidth = 600;
  readonly chartHeight = 210;
  readonly marginTop = 12;
  readonly marginBottom = 24;
  readonly marginLeft = 34;
  readonly marginRight = 18;

  private readonly fwColors: Record<string, string> = {
    'NABH-5': '#22c55e',
    JCI: '#f59e0b',
    'ISO-27001': '#0d9488',
  };

  private readonly fallbackColors = ['#06b6d4', '#ec4899', '#14b8a6', '#f97316'];

  readonly yGridLines = [
    { value: 100, y: this.marginTop },
    { value: 80, y: this.marginTop + ((100 - 80) / 50) * (this.chartHeight - this.marginTop - this.marginBottom) },
    { value: 60, y: this.marginTop + ((100 - 60) / 50) * (this.chartHeight - this.marginTop - this.marginBottom) },
  ];

  frameworkCodes = computed(() => {
    const trends = this.data();
    if (!trends.length) return [];
    const codes = Object.keys(trends[0].frameworks);
    codes.sort((a, b) =>
      compareFrameworkCodeOrder(
        { code: a, name: a },
        { code: b, name: b },
      ),
    );
    let fallbackIdx = 0;
    return codes.map((code) => ({
      code,
      color: this.fwColors[code] ?? this.fallbackColors[fallbackIdx++ % this.fallbackColors.length],
    }));
  });

  avgPoints = computed(() => {
    const trends = this.data();
    if (!trends.length) return [];
    const innerW = this.chartWidth - this.marginLeft - this.marginRight;
    const innerH = this.chartHeight - this.marginTop - this.marginBottom;
    const step = trends.length > 1 ? innerW / (trends.length - 1) : 0;

    return trends.map((t, i) => {
      const x = this.marginLeft + i * step;
      const y = this.marginTop + ((100 - t.avgScore) / 50) * innerH;
      const d = new Date(t.date);
      const label = d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      return { x, y, label };
    });
  });

  avgPolyline = computed(() => {
    return this.avgPoints()
      .map((p) => `${p.x},${p.y}`)
      .join(' ');
  });

  frameworkLines = computed(() => {
    const trends = this.data();
    const fwCodes = this.frameworkCodes();
    if (!trends.length || !fwCodes.length) return [];

    const innerW = this.chartWidth - this.marginLeft - this.marginRight;
    const innerH = this.chartHeight - this.marginTop - this.marginBottom;
    const step = trends.length > 1 ? innerW / (trends.length - 1) : 0;

    return fwCodes.map((fw) => {
      const dots = trends.map((t, i) => {
        const score = t.frameworks[fw.code] ?? 0;
        return {
          x: this.marginLeft + i * step,
          y: this.marginTop + ((100 - score) / 50) * innerH,
        };
      });
      return {
        code: fw.code,
        color: fw.color,
        points: dots.map((d) => `${d.x},${d.y}`).join(' '),
        dots,
      };
    });
  });
}
