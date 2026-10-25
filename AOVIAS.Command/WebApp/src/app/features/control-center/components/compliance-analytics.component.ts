import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ComplianceTrendData {
  date: string;
  score: number;
}

export interface ChapterComplianceData {
  chapter: string;
  score: number;
}

export interface EvidenceCoverageData {
  approved: number;
  pending: number;
  rejected: number;
}

@Component({
  selector: 'app-compliance-analytics',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="compliance-analytics">
      <h2 class="section-title">Compliance Analytics</h2>

      <div class="analytics-grid">
        <div class="chart-card trend-chart">
          <div class="chart-header">
            <h3 class="chart-title">Compliance Trend</h3>
            <div class="chart-legend">
              <span class="legend-item">
                <span class="legend-dot" style="background: var(--color-primary);"></span>
                Compliance Score
              </span>
              <span class="legend-item">
                <span class="legend-dot" style="background: #dc2626;"></span>
                Target (80%)
              </span>
            </div>
          </div>
          <div class="chart-container">
            <svg viewBox="0 0 600 200" class="line-chart">
              <line x1="0" y1="80" x2="600" y2="80" stroke="#dc2626" stroke-width="2" stroke-dasharray="5,5" opacity="0.5"/>
              <text x="610" y="85" font-size="12" fill="#6b7280">80%</text>

              <polyline
                [attr.points]="getTrendPoints()"
                fill="none"
                stroke="#2f6bff"
                stroke-width="3"
                stroke-linecap="round"
                stroke-linejoin="round"
              />

              <g *ngFor="let point of trendData; let i = index">
                <circle
                  [attr.cx]="i * (600 / (trendData.length - 1))"
                  [attr.cy]="200 - (point.score * 2)"
                  r="4"
                  fill="#2f6bff"
                  class="data-point"
                />
              </g>

              <g class="x-axis">
                <line x1="0" y1="200" x2="600" y2="200" stroke="#e5e7eb" stroke-width="2"/>
                <text *ngFor="let point of trendData; let i = index"
                      [attr.x]="i * (600 / (trendData.length - 1))"
                      y="215"
                      font-size="11"
                      fill="#6b7280"
                      text-anchor="middle">
                  {{ point.date }}
                </text>
              </g>
            </svg>
          </div>
        </div>

        <div class="chart-card chapter-chart">
          <div class="chart-header">
            <h3 class="chart-title">Chapter Compliance</h3>
            <span class="chart-subtitle">Sorted by lowest compliance</span>
          </div>
          <div class="chart-container">
            <div class="bar-chart">
              <div *ngFor="let chapter of chapterData" class="bar-item">
                <div class="bar-label">{{ chapter.chapter }}</div>
                <div class="bar-wrapper">
                  <div class="bar-track">
                    <div class="bar-fill"
                         [style.width.%]="chapter.score"
                         [class.bar-low]="chapter.score < 60"
                         [class.bar-medium]="chapter.score >= 60 && chapter.score < 80"
                         [class.bar-high]="chapter.score >= 80">
                    </div>
                  </div>
                  <span class="bar-value">{{ chapter.score }}%</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="chart-card evidence-chart">
          <div class="chart-header">
            <h3 class="chart-title">Evidence Coverage</h3>
            <span class="chart-subtitle">{{ getTotalEvidence() }} total items</span>
          </div>
          <div class="chart-container donut-container">
            <svg viewBox="0 0 200 200" class="donut-chart">
              <circle cx="100" cy="100" r="80" fill="none" stroke="#e5e7eb" stroke-width="30"/>

              <circle
                cx="100"
                cy="100"
                r="80"
                fill="none"
                stroke="#10b981"
                stroke-width="30"
                [attr.stroke-dasharray]="getDonutSegment(evidenceData.approved, 0)"
                stroke-dashoffset="0"
                transform="rotate(-90 100 100)"
              />

              <circle
                cx="100"
                cy="100"
                r="80"
                fill="none"
                stroke="#f59e0b"
                stroke-width="30"
                [attr.stroke-dasharray]="getDonutSegment(evidenceData.pending, evidenceData.approved)"
                [attr.stroke-dashoffset]="-getCircumference() * (evidenceData.approved / getTotalEvidence())"
                transform="rotate(-90 100 100)"
              />

              <circle
                cx="100"
                cy="100"
                r="80"
                fill="none"
                stroke="#ef4444"
                stroke-width="30"
                [attr.stroke-dasharray]="getDonutSegment(evidenceData.rejected, evidenceData.approved + evidenceData.pending)"
                [attr.stroke-dashoffset]="-getCircumference() * ((evidenceData.approved + evidenceData.pending) / getTotalEvidence())"
                transform="rotate(-90 100 100)"
              />

              <text x="100" y="95" text-anchor="middle" font-size="32" font-weight="700" fill="var(--text-primary)">
                {{ getApprovedPercentage() }}%
              </text>
              <text x="100" y="115" text-anchor="middle" font-size="14" fill="var(--text-secondary)">
                Approved
              </text>
            </svg>

            <div class="evidence-legend">
              <div class="legend-item">
                <span class="legend-color" style="background: #10b981;"></span>
                <span class="legend-label">Approved</span>
                <span class="legend-value">{{ evidenceData.approved }}</span>
              </div>
              <div class="legend-item">
                <span class="legend-color" style="background: #f59e0b;"></span>
                <span class="legend-label">Pending</span>
                <span class="legend-value">{{ evidenceData.pending }}</span>
              </div>
              <div class="legend-item">
                <span class="legend-color" style="background: #ef4444;"></span>
                <span class="legend-label">Rejected</span>
                <span class="legend-value">{{ evidenceData.rejected }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .compliance-analytics {
      margin-bottom: 24px;
    }

    .section-title {
      font-size: 20px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 20px 0;
    }

    .analytics-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 20px;
    }

    .trend-chart {
      grid-column: 1 / 2;
    }

    .chapter-chart {
      grid-column: 2 / 3;
      grid-row: 1 / 3;
    }

    .evidence-chart {
      grid-column: 1 / 2;
    }

    .chart-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 24px;
      box-shadow: var(--shadow-sm);
    }

    .chart-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .chart-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .chart-subtitle {
      font-size: var(--font-size-body-sm);
      color: var(--text-secondary);
    }

    .chart-legend {
      display: flex;
      gap: 16px;
    }

    .legend-item {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
      color: var(--text-secondary);
    }

    .legend-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
    }

    .chart-container {
      position: relative;
    }

    .line-chart {
      width: 100%;
      height: auto;
    }

    .data-point {
      cursor: pointer;
      transition: r 0.2s ease;
    }

    .data-point:hover {
      r: 6;
    }

    .bar-chart {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .bar-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .bar-label {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .bar-wrapper {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .bar-track {
      flex: 1;
      height: 8px;
      background: var(--bg-tertiary);
      border-radius: 4px;
      overflow: hidden;
    }

    .bar-fill {
      height: 100%;
      border-radius: 4px;
      transition: width 0.6s ease;
    }

    .bar-low {
      background: var(--danger-color);
    }

    .bar-medium {
      background: var(--warning-color);
    }

    .bar-high {
      background: var(--success-color);
    }

    .bar-value {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
      min-width: 40px;
      text-align: right;
    }

    .donut-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 24px;
    }

    .donut-chart {
      width: 200px;
      height: 200px;
    }

    .evidence-legend {
      display: flex;
      gap: 24px;
    }

    .evidence-legend .legend-item {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .legend-color {
      width: 12px;
      height: 12px;
      border-radius: 3px;
    }

    .legend-label {
      font-size: 13px;
      color: var(--text-secondary);
    }

    .legend-value {
      font-size: 14px;
      font-weight: 700;
      color: var(--text-primary);
    }

    @media (max-width: 1024px) {
      .analytics-grid {
        grid-template-columns: 1fr;
      }

      .trend-chart,
      .chapter-chart,
      .evidence-chart {
        grid-column: 1;
        grid-row: auto;
      }
    }
  `]
})
export class ComplianceAnalyticsComponent implements OnInit {
  @Input() trendData: ComplianceTrendData[] = [];
  @Input() chapterData: ChapterComplianceData[] = [];
  @Input() evidenceData: EvidenceCoverageData = { approved: 0, pending: 0, rejected: 0 };

  ngOnInit(): void {
    this.chapterData = [...this.chapterData].sort((a, b) => a.score - b.score);
  }

  getTrendPoints(): string {
    return this.trendData
      .map((point, index) => {
        const x = index * (600 / (this.trendData.length - 1));
        const y = 200 - (point.score * 2);
        return `${x},${y}`;
      })
      .join(' ');
  }

  getTotalEvidence(): number {
    return this.evidenceData.approved + this.evidenceData.pending + this.evidenceData.rejected;
  }

  getApprovedPercentage(): number {
    const total = this.getTotalEvidence();
    return total > 0 ? Math.round((this.evidenceData.approved / total) * 100) : 0;
  }

  getCircumference(): number {
    return 2 * Math.PI * 80;
  }

  getDonutSegment(value: number, offset: number): string {
    const total = this.getTotalEvidence();
    const circumference = this.getCircumference();
    const percentage = value / total;
    const segmentLength = circumference * percentage;
    return `${segmentLength} ${circumference - segmentLength}`;
  }
}
