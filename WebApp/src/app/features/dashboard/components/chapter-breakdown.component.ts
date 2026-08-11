import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

interface ChapterData {
  chapter: string;
  score: number;
}

@Component({
  selector: 'app-chapter-breakdown',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="breakdown-container">
      <h3 class="breakdown-title">{{ title() }}</h3>

      @if (bars().length <= 1) {
        <!-- Adaptive: a single framework renders as a compact horizontal bar (no blank chart area) -->
        <div class="single-list">
          @for (bar of bars(); track bar.chapter) {
            <div class="single-row">
              <span class="single-label">{{ bar.chapter }}</span>
              <div class="single-track">
                <div
                  class="single-fill"
                  [style.width.%]="bar.score"
                  [style.background]="bar.color"
                ></div>
              </div>
              <span class="single-score">{{ bar.score }}%</span>
            </div>
          } @empty {
            <div class="single-empty">No framework scores available</div>
          }
        </div>
      } @else {
      <div class="chart-wrapper">
        <svg class="bar-chart" [attr.viewBox]="'0 0 ' + chartWidth + ' ' + chartHeight">
          <g>
            @for (bar of bars(); track bar.chapter; let i = $index) {
              <g>
                <rect
                  class="bar-background"
                  [attr.x]="bar.x"
                  [attr.y]="marginTop"
                  [attr.width]="bar.width"
                  [attr.height]="chartInnerHeight"
                  fill="var(--hover-bg)"
                  rx="4"
                />

                <rect
                  class="bar"
                  [attr.x]="bar.x"
                  [attr.y]="bar.y"
                  [attr.width]="bar.width"
                  [attr.height]="bar.height"
                  [attr.fill]="bar.color"
                  rx="4"
                />

                <text
                  class="bar-label"
                  [attr.x]="bar.x + bar.width / 2"
                  [attr.y]="chartHeight - 5"
                  text-anchor="middle"
                  fill="var(--text-secondary)"
                >
                  {{ bar.chapter }}
                </text>

                <text
                  class="bar-value"
                  [attr.x]="bar.x + bar.width / 2"
                  [attr.y]="bar.y - 8"
                  text-anchor="middle"
                  fill="var(--text-primary)"
                >
                  {{ bar.score }}%
                </text>
              </g>
            }
          </g>

          <line
            class="axis-line"
            [attr.x1]="marginLeft"
            [attr.y1]="chartHeight - marginBottom"
            [attr.x2]="chartWidth - marginRight"
            [attr.y2]="chartHeight - marginBottom"
            stroke="var(--border-color)"
            stroke-width="2"
          />
        </svg>
      </div>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
      }

      .breakdown-container {
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

      .breakdown-container:hover {
        box-shadow: var(--shadow-md);
      }

      .breakdown-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
        margin-bottom: 12px;
      }

      .chart-wrapper {
        overflow-x: auto;
        flex: 1;
        display: flex;
        align-items: center;
      }

      /* ── Single-framework compact mode ── */
      .single-list {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .single-row {
        display: grid;
        grid-template-columns: minmax(80px, auto) 1fr 48px;
        align-items: center;
        gap: 12px;
      }

      .single-label {
        font-size: 13px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .single-track {
        height: 14px;
        background: var(--hover-bg);
        border-radius: 7px;
        overflow: hidden;
      }

      .single-fill {
        height: 100%;
        border-radius: 7px;
        transition: width 0.6s ease;
      }

      .single-score {
        font-size: 16px;
        font-weight: 700;
        color: var(--text-primary);
        text-align: right;
      }

      .single-empty {
        font-size: 13px;
        color: var(--text-secondary);
        padding: 8px 0;
      }

      .bar-chart {
        width: 100%;
        min-width: 500px;
        height: auto;
      }

      .bar {
        transition: all 0.3s ease;
        cursor: pointer;
      }

      .bar:hover {
        opacity: 0.8;
      }

      .bar-label {
        font-size: 12px;
        font-weight: 600;
      }

      .bar-value {
        font-size: 14px;
        font-weight: 700;
      }

      .axis-line {
        stroke-linecap: round;
      }
    `,
  ],
})
export class ChapterBreakdownComponent {
  title = input<string>('Chapter Compliance Breakdown');
  data = input.required<ChapterData[]>();

  readonly chartWidth = 600;
  readonly chartHeight = 230;
  readonly marginTop = 24;
  readonly marginBottom = 34;
  readonly marginLeft = 20;
  readonly marginRight = 20;

  readonly chartInnerHeight = this.chartHeight - this.marginTop - this.marginBottom;
  readonly chartInnerWidth = this.chartWidth - this.marginLeft - this.marginRight;

  bars = computed(() => {
    const data = this.data();
    const barCount = data.length;
    const barSpacing = 20;
    const totalSpacing = barSpacing * (barCount + 1);
    const barWidth = (this.chartInnerWidth - totalSpacing) / barCount;

    return data.map((item, index) => {
      const x = this.marginLeft + barSpacing + index * (barWidth + barSpacing);
      const maxHeight = this.chartInnerHeight;
      const height = (item.score / 100) * maxHeight;
      const y = this.chartHeight - this.marginBottom - height;

      let color = '#22c55e';
      if (item.score < 60) {
        color = '#ef4444';
      } else if (item.score < 75) {
        color = '#f59e0b';
      }

      return {
        chapter: item.chapter,
        score: item.score,
        x,
        y,
        width: barWidth,
        height,
        color,
      };
    });
  });
}
