import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface HeatmapItem {
  label: string;
  score: number;
  metadata?: string;
}

interface HeatCell {
  label: string;
  score: number;
  level: 'critical' | 'low' | 'medium' | 'high' | 'excellent';
  metadata?: string;
}

@Component({
  selector: 'app-compliance-heatmap',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="heatmap-container">
      <div class="heatmap-header">
        <h3 class="heatmap-title">{{ title() }}</h3>
        <div class="legend">
          @for (band of legendBands; track band.label) {
            <div class="legend-item">
              <span class="legend-swatch" [attr.data-level]="band.level"></span>
              <span class="legend-label">{{ band.label }}</span>
            </div>
          }
        </div>
      </div>

      @if (cells().length === 0) {
        <div class="empty-state">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <rect x="3" y="3" width="7" height="7" rx="1" stroke-width="2" />
            <rect x="14" y="3" width="7" height="7" rx="1" stroke-width="2" />
            <rect x="3" y="14" width="7" height="7" rx="1" stroke-width="2" />
            <rect x="14" y="14" width="7" height="7" rx="1" stroke-width="2" />
          </svg>
          <span>No readiness data available</span>
        </div>
      } @else {
        <div class="heatmap-grid" [style.--col-count]="gridCols()">
          @for (cell of cells(); track cell.label) {
            <div
              class="heatmap-cell"
              [attr.data-level]="cell.level"
              [attr.title]="cell.label + ': ' + cell.score + '%'"
            >
              <span class="cell-score">{{ cell.score }}%</span>
              <span class="cell-label">{{ cell.label }}</span>
              @if (cell.metadata) {
                <span class="cell-metadata">{{ cell.metadata }}</span>
              }
            </div>
          }
        </div>

        <!-- Score range summary -->
        <div class="range-summary">
          <div class="range-stat">
            <span class="range-value range-min">{{ minScore() }}%</span>
            <span class="range-label">Lowest</span>
          </div>
          <div class="range-divider"></div>
          <div class="range-stat">
            <span class="range-value range-avg">{{ avgScore() }}%</span>
            <span class="range-label">Average</span>
          </div>
          <div class="range-divider"></div>
          <div class="range-stat">
            <span class="range-value range-max">{{ maxScore() }}%</span>
            <span class="range-label">Highest</span>
          </div>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .heatmap-container {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
        transition: box-shadow 0.3s ease;
      }

      .heatmap-container:hover {
        box-shadow: var(--shadow-md);
      }

      .heatmap-header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 16px;
        margin-bottom: 14px;
        flex-wrap: wrap;
      }

      .heatmap-title {
        font-size: 16px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .legend {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;
        align-items: center;
      }

      .legend-item {
        display: flex;
        align-items: center;
        gap: 5px;
      }

      .legend-swatch {
        width: 12px;
        height: 12px;
        border-radius: 3px;
        flex-shrink: 0;
      }

      .legend-swatch[data-level='critical'] { background: #ef4444; }
      .legend-swatch[data-level='low']      { background: #f97316; }
      .legend-swatch[data-level='medium']   { background: #f59e0b; }
      .legend-swatch[data-level='high']     { background: #84cc16; }
      .legend-swatch[data-level='excellent']{ background: #22c55e; }

      .legend-label {
        font-size: 11px;
        font-weight: 500;
        color: var(--text-secondary);
      }

      .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 12px;
        padding: 32px 16px;
        color: var(--text-secondary);
        font-size: 14px;
      }

      .empty-state svg {
        opacity: 0.4;
      }

      /* ── Grid ── */
      .heatmap-grid {
        display: grid;
        grid-template-columns: repeat(var(--col-count, 4), 1fr);
        gap: 8px;
        margin-bottom: 14px;
      }

      .heatmap-cell {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 4px;
        padding: 10px 8px;
        border-radius: 10px;
        text-align: center;
        cursor: default;
        transition: transform 0.2s ease, box-shadow 0.2s ease;
        min-height: 64px;
      }

      .heatmap-cell:hover {
        transform: scale(1.04);
        box-shadow: var(--shadow-md);
        z-index: 1;
        position: relative;
      }

      .heatmap-cell[data-level='critical'] {
        background: rgba(239, 68, 68, 0.15);
        border: 1px solid rgba(239, 68, 68, 0.3);
      }

      .heatmap-cell[data-level='low'] {
        background: rgba(249, 115, 22, 0.12);
        border: 1px solid rgba(249, 115, 22, 0.25);
      }

      .heatmap-cell[data-level='medium'] {
        background: rgba(245, 158, 11, 0.12);
        border: 1px solid rgba(245, 158, 11, 0.25);
      }

      .heatmap-cell[data-level='high'] {
        background: rgba(132, 204, 22, 0.12);
        border: 1px solid rgba(132, 204, 22, 0.25);
      }

      .heatmap-cell[data-level='excellent'] {
        background: rgba(34, 197, 94, 0.12);
        border: 1px solid rgba(34, 197, 94, 0.25);
      }

      .cell-score {
        font-size: 22px;
        font-weight: 700;
        line-height: 1;
        color: var(--text-primary);
      }

      .heatmap-cell[data-level='critical'] .cell-score { color: #dc2626; }
      .heatmap-cell[data-level='low']      .cell-score { color: #ea580c; }
      .heatmap-cell[data-level='medium']   .cell-score { color: #d97706; }
      .heatmap-cell[data-level='high']     .cell-score { color: #65a30d; }
      .heatmap-cell[data-level='excellent'] .cell-score { color: #16a34a; }

      .cell-label {
        font-size: 12px;
        font-weight: 600;
        color: var(--text-primary);
        text-align: center;
        line-height: 1.3;
      }

      .cell-metadata {
        font-size: 10px;
        font-weight: 500;
        color: var(--text-secondary);
        text-align: center;
      }

      /* ── Range Summary ── */
      .range-summary {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0;
        padding: 10px 14px;
        background: var(--hover-bg);
        border-radius: 10px;
      }

      .range-stat {
        flex: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 4px;
      }

      .range-divider {
        width: 1px;
        height: 36px;
        background: var(--border-color);
      }

      .range-value {
        font-size: 20px;
        font-weight: 700;
        line-height: 1;
      }

      .range-min { color: var(--danger-color); }
      .range-avg { color: var(--primary-color); }
      .range-max { color: var(--success-color); }

      .range-label {
        font-size: 11px;
        font-weight: 500;
        color: var(--text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.3px;
      }

      /* ── Responsive ── */
      @media (max-width: 1024px) {
        .heatmap-grid {
          --col-count: 3 !important;
        }
      }

      @media (max-width: 640px) {
        .heatmap-grid {
          --col-count: 2 !important;
        }

        .heatmap-header {
          flex-direction: column;
        }

        .legend {
          gap: 8px;
        }
      }
    `,
  ],
})
export class ComplianceHeatmapComponent {
  title = input<string>('Compliance Readiness Heatmap');
  data = input.required<HeatmapItem[]>();

  readonly legendBands = [
    { label: '< 60%', level: 'critical' },
    { label: '60–69%', level: 'low' },
    { label: '70–79%', level: 'medium' },
    { label: '80–89%', level: 'high' },
    { label: '≥ 90%', level: 'excellent' },
  ];

  cells = computed<HeatCell[]>(() =>
    this.data().map((item) => ({
      label: item.label,
      score: Math.round(item.score),
      level: this.scoreToLevel(item.score),
      metadata: item.metadata,
    }))
  );

  gridCols = computed(() => {
    const count = this.cells().length;
    if (count <= 2) return 2;
    if (count <= 4) return count;
    return 4;
  });

  minScore = computed(() => {
    const scores = this.cells().map((c) => c.score);
    return scores.length ? Math.min(...scores) : 0;
  });

  maxScore = computed(() => {
    const scores = this.cells().map((c) => c.score);
    return scores.length ? Math.max(...scores) : 0;
  });

  avgScore = computed(() => {
    const cells = this.cells();
    if (!cells.length) return 0;
    return Math.round(cells.reduce((sum, c) => sum + c.score, 0) / cells.length);
  });

  private scoreToLevel(score: number): HeatCell['level'] {
    if (score >= 90) return 'excellent';
    if (score >= 80) return 'high';
    if (score >= 70) return 'medium';
    if (score >= 60) return 'low';
    return 'critical';
  }
}
