import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { ComplianceReport } from '../models/report.models';
import { ReportsApiService } from '../services/reports-api.service';
import { loadReport, periodToDateRange, exportReportFile } from '../services/report-page.helper';

interface ComplianceMetric {
  label: string;
  value: number | string;
  color: string;
  icon: string;
}

interface ChapterComplianceView {
  chapter: string;
  score: number;
  status: string;
}

@Component({
  selector: 'app-compliance-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="compliance-reports-page">
      <div class="page-header">
        <div class="header-content">
          <h1 class="page-title">Compliance Reports</h1>
          <p class="page-description">
            Monitor compliance performance across all chapters with detailed metrics and trend analysis.
          </p>
        </div>
        <div class="header-actions">
          <button class="btn-export" type="button" (click)="onExport()" [disabled]="loading()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3" stroke-width="2"/>
            </svg>
            Export Report
          </button>
        </div>
      </div>

      <div class="filters-bar">
        <select [(ngModel)]="selectedPeriod" class="filter-select" (ngModelChange)="load()">
          <option value="month">This Month</option>
          <option value="quarter">This Quarter</option>
          <option value="year">This Year</option>
        </select>
        <button type="button" class="btn-export" (click)="load()">Apply</button>
      </div>

      @if (loading()) {
        <p class="page-description">Loading compliance report…</p>
      }
      @if (error()) {
        <p class="page-description" style="color:#ef4444">{{ error() }} <button type="button" (click)="load()">Retry</button></p>
      }
      @if (!loading() && !error() && !hasData()) {
        <p class="page-description">No compliance data for the selected filters.</p>
      }

      <div class="metrics-grid">
        @for (metric of metrics; track metric.label) {
          <div class="metric-card" [style.border-left-color]="metric.color">
            <div class="metric-icon" [style.color]="metric.color" [innerHTML]="metric.icon"></div>
            <div class="metric-content">
              <div class="metric-value">{{ metric.value }}</div>
              <div class="metric-label">{{ metric.label }}</div>
            </div>
          </div>
        }
      </div>

      <div class="charts-section">
        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">Compliance Score by Chapter</h3>
            <select class="chart-filter">
              <option>Last 6 Months</option>
              <option>This Year</option>
            </select>
          </div>
          <div class="chart-body">
            <div class="bar-chart">
              @for (chapter of chapterData; track chapter.chapter) {
                <div class="chart-item">
                  <div class="chart-label">{{ chapter.chapter }}</div>
                  <div class="chart-bar-container">
                    <div
                      class="chart-bar"
                      [style.width.%]="chapter.score"
                      [class]="'bar-' + chapter.status"
                    ></div>
                    <span class="chart-value">{{ chapter.score }}%</span>
                  </div>
                </div>
              }
            </div>
          </div>
        </div>

        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">Compliance Trend Over Time</h3>
            <div class="chart-legend">
              <span class="legend-item">
                <span class="legend-dot compliant"></span>Compliant
              </span>
              <span class="legend-item">
                <span class="legend-dot partial"></span>Partial
              </span>
              <span class="legend-item">
                <span class="legend-dot non-compliant"></span>Non-Compliant
              </span>
            </div>
          </div>
          <div class="chart-body">
            <div class="line-chart">
              <div class="chart-grid">
                @for (line of [100, 80, 60, 40, 20, 0]; track line) {
                  <div class="grid-line">
                    <span class="grid-label">{{ line }}%</span>
                  </div>
                }
              </div>
              <div class="trend-line">
                <svg class="trend-svg" viewBox="0 0 600 200" preserveAspectRatio="none">
                  <polyline
                    points="0,80 100,70 200,65 300,60 400,55 500,50 600,48"
                    fill="none"
                    stroke="#3b82f6"
                    stroke-width="3"
                  />
                  <polyline
                    points="0,80 100,70 200,65 300,60 400,55 500,50 600,48"
                    fill="url(#gradient)"
                    opacity="0.2"
                  />
                  <defs>
                    <linearGradient id="gradient" x1="0%" y1="0%" x2="0%" y2="100%">
                      <stop offset="0%" stop-color="#3b82f6" stop-opacity="0.8"/>
                      <stop offset="100%" stop-color="#3b82f6" stop-opacity="0"/>
                    </linearGradient>
                  </defs>
                </svg>
              </div>
              <div class="trend-labels">
                @for (month of ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun']; track month) {
                  <span class="trend-label">{{ month }}</span>
                }
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="risk-section">
        <h2 class="section-title">Top Risk Chapters</h2>
        <div class="risk-grid">
          @for (risk of topRisks; track risk.chapter) {
            <div class="risk-card">
              <div class="risk-header">
                <span class="risk-chapter">{{ risk.chapter }}</span>
                <span class="risk-score" [class]="'score-' + risk.severity">{{ risk.score }}%</span>
              </div>
              <div class="risk-bar">
                <div
                  class="risk-bar-fill"
                  [style.width.%]="risk.score"
                  [class]="'fill-' + risk.severity"
                ></div>
              </div>
              <div class="risk-details">{{ risk.details }}</div>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .compliance-reports-page {
      padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      background: var(--bg-primary);
      min-height: calc(100vh - 64px);
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 24px;
      gap: 20px;
    }

    .header-content {
      flex: 1;
    }

    .page-title {
      font-size: var(--font-size-page-title);
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 8px 0;
    }

    .page-description {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.5;
    }

    .header-actions {
      display: flex;
      gap: 12px;
    }

    .btn-export {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 20px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-export:hover {
      background: var(--primary-hover);
      transform: translateY(-1px);
    }

    .filters-bar {
      display: flex;
      gap: 12px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }

    .filter-select {
      padding: 10px 32px 10px 12px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      color: var(--text-primary);
      font-size: 14px;
      cursor: pointer;
      transition: all 0.2s ease;
      appearance: none;
      background-image: url("data:image/svg+xml,%3Csvg width='12' height='12' viewBox='0 0 12 12' fill='none' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M3 4.5L6 7.5L9 4.5' stroke='%23666' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
      background-repeat: no-repeat;
      background-position: right 12px center;
    }

    .filter-select:focus {
      outline: none;
      border-color: var(--primary-color);
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 32px;
    }

    .metric-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-left: 4px solid;
      border-radius: 8px;
      padding: 20px;
      display: flex;
      align-items: center;
      gap: 16px;
      transition: all 0.2s ease;
    }

    .metric-card:hover {
      transform: translateY(-2px);
      box-shadow: var(--shadow-md);
    }

    .metric-icon {
      width: 48px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .metric-content {
      flex: 1;
    }

    .metric-value {
      font-size: 28px;
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .metric-label {
      font-size: 12px;
      font-weight: 500;
      color: var(--text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .charts-section {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
      gap: 24px;
      margin-bottom: 32px;
    }

    .chart-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      overflow: hidden;
    }

    .chart-header {
      padding: 20px;
      border-bottom: 1px solid var(--border-color);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .chart-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .chart-filter {
      padding: 6px 24px 6px 10px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      color: var(--text-primary);
      font-size: 13px;
      cursor: pointer;
      appearance: none;
      background-image: url("data:image/svg+xml,%3Csvg width='12' height='12' viewBox='0 0 12 12' fill='none' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M3 4.5L6 7.5L9 4.5' stroke='%23666' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
      background-repeat: no-repeat;
      background-position: right 8px center;
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

    .legend-dot.compliant {
      background: #10b981;
    }

    .legend-dot.partial {
      background: #f59e0b;
    }

    .legend-dot.non-compliant {
      background: #ef4444;
    }

    .chart-body {
      padding: 20px;
    }

    .bar-chart {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .chart-item {
      display: grid;
      grid-template-columns: 60px 1fr;
      gap: 12px;
      align-items: center;
    }

    .chart-label {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .chart-bar-container {
      position: relative;
      height: 32px;
      background: var(--bg-primary);
      border-radius: 6px;
      overflow: hidden;
    }

    .chart-bar {
      height: 100%;
      border-radius: 6px;
      transition: width 0.5s ease;
      position: relative;
    }

    .chart-bar.bar-compliant {
      background: #10b981;
    }

    .chart-bar.bar-partial {
      background: #f59e0b;
    }

    .chart-bar.bar-non-compliant {
      background: #ef4444;
    }

    .chart-value {
      position: absolute;
      right: 12px;
      top: 50%;
      transform: translateY(-50%);
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .line-chart {
      position: relative;
      height: 250px;
    }

    .chart-grid {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 30px;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
    }

    .grid-line {
      border-top: 1px solid var(--border-color);
      position: relative;
    }

    .grid-label {
      position: absolute;
      left: -40px;
      top: -8px;
      font-size: 11px;
      color: var(--text-secondary);
    }

    .trend-line {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 30px;
      padding-left: 20px;
    }

    .trend-svg {
      width: 100%;
      height: 100%;
    }

    .trend-labels {
      position: absolute;
      bottom: 0;
      left: 0;
      right: 0;
      display: flex;
      justify-content: space-around;
      padding: 0 20px;
    }

    .trend-label {
      font-size: 11px;
      color: var(--text-secondary);
    }

    .risk-section {
      margin-bottom: 24px;
    }

    .section-title {
      font-size: 20px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 20px 0;
    }

    .risk-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 16px;
    }

    .risk-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 20px;
    }

    .risk-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }

    .risk-chapter {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .risk-score {
      font-size: 18px;
      font-weight: 700;
    }

    .risk-score.score-high {
      color: #ef4444;
    }

    .risk-score.score-medium {
      color: #f59e0b;
    }

    .risk-bar {
      height: 8px;
      background: var(--bg-primary);
      border-radius: 4px;
      overflow: hidden;
      margin-bottom: 12px;
    }

    .risk-bar-fill {
      height: 100%;
      border-radius: 4px;
      transition: width 0.5s ease;
    }

    .risk-bar-fill.fill-high {
      background: #ef4444;
    }

    .risk-bar-fill.fill-medium {
      background: #f59e0b;
    }

    .risk-details {
      font-size: 13px;
      color: var(--text-secondary);
      line-height: 1.5;
    }

    @media (max-width: 768px) {
      .compliance-reports-page {
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      }

      .page-header {
        flex-direction: column;
      }

      .charts-section {
        grid-template-columns: 1fr;
      }

      .metrics-grid {
        grid-template-columns: 1fr;
      }

      .risk-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class ComplianceReportsComponent implements OnInit {
  private readonly api = inject(ReportsApiService);
  private readonly context = inject(ComplianceContextService);
  private readonly destroyRef = inject(DestroyRef);

  selectedPeriod = 'month';
  loading = signal(false);
  error = signal<string | null>(null);

  metrics: ComplianceMetric[] = [];
  chapterData: ChapterComplianceView[] = [];
  topRisks: { chapter: string; score: number; severity: string; details: string }[] = [];
  trendLabels: string[] = [];

  ngOnInit(): void {
    this.context.frameworkId$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    this.context.hospitalId$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    this.load();
  }

  load(): void {
    const range = periodToDateRange(this.selectedPeriod);
    loadReport(
      () => this.api.getComplianceReport(range),
      (report) => this.applyReport(report),
      this.loading,
      this.error,
    );
  }

  hasData(): boolean {
    return this.metrics.length > 0;
  }

  onExport(): void {
    exportReportFile(this.api, 'compliance', 'pdf', periodToDateRange(this.selectedPeriod));
  }

  private applyReport(report: ComplianceReport): void {
    this.metrics = [
      { label: 'Total Clauses', value: report.totalClauses, color: '#3b82f6', icon: this.iconDoc() },
      { label: 'Compliant', value: report.compliantClauses, color: '#10b981', icon: this.iconCheck() },
      { label: 'Partial', value: report.partialClauses, color: '#f59e0b', icon: this.iconWarn() },
      { label: 'Non-Compliant', value: report.nonCompliantClauses, color: '#ef4444', icon: this.iconX() },
      { label: 'Compliance Score', value: `${report.complianceScore}%`, color: '#0d9488', icon: this.iconBolt() },
    ];
    this.chapterData = (report.chapters ?? []).map((c) => ({
      chapter: c.chapterCode || c.chapterName,
      score: c.score,
      status: c.status,
    }));
    this.topRisks = (report.topRiskChapters ?? []).map((r) => ({
      chapter: r.chapterCode || r.chapterName,
      score: r.score,
      severity: r.severity,
      details: r.details,
    }));
    this.trendLabels = (report.trends ?? []).map((t) => new Date(t.date).toLocaleDateString(undefined, { month: 'short' }));
  }

  private iconDoc(): string {
    return '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2" stroke-width="2"/></svg>';
  }
  private iconCheck(): string {
    return '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" stroke-width="2"/></svg>';
  }
  private iconWarn(): string {
    return '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/></svg>';
  }
  private iconX(): string {
    return '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/></svg>';
  }
  private iconBolt(): string {
    return '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 10V3L4 14h7v7l9-11h-7z" stroke-width="2"/></svg>';
  }
}
