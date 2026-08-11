import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { AuditReport } from '../models/report.models';
import { ReportsApiService } from '../services/reports-api.service';
import { exportReportFile, loadReport, periodToDateRange } from '../services/report-page.helper';

@Component({
  selector: 'app-audit-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="audit-reports-page">
      <div class="page-header">
        <div class="header-content">
          <h1 class="page-title">Audit Reports</h1>
          <p class="page-description">
            Analyze audit findings by chapter, department, and severity with comprehensive insights.
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
        </select>
        <button type="button" class="btn-export" (click)="load()">Apply</button>
      </div>
      @if (loading()) { <p class="page-description">Loading…</p> }
      @if (error()) { <p class="page-description" style="color:#ef4444">{{ error() }} <button type="button" (click)="load()">Retry</button></p> }

      <div class="metrics-grid">
        <div class="metric-card critical">
          <div class="metric-value">{{ criticalFindings }}</div>
          <div class="metric-label">Critical Findings</div>
        </div>
        <div class="metric-card major">
          <div class="metric-value">{{ majorFindings }}</div>
          <div class="metric-label">Major Findings</div>
        </div>
        <div class="metric-card minor">
          <div class="metric-value">{{ minorFindings }}</div>
          <div class="metric-label">Minor Findings</div>
        </div>
        <div class="metric-card total">
          <div class="metric-value">{{ totalFindings }}</div>
          <div class="metric-label">Total Findings</div>
        </div>
      </div>

      <div class="charts-section">
        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">Findings by Chapter</h3>
          </div>
          <div class="chart-body">
            <div class="bar-chart">
              @for (item of findingsByChapter; track item.chapter) {
                <div class="chart-row">
                  <div class="chart-label">{{ item.chapter }}</div>
                  <div class="chart-bars">
                    <div class="severity-bar critical" [style.width.%]="(item.critical / item.total) * 100"></div>
                    <div class="severity-bar major" [style.width.%]="(item.major / item.total) * 100"></div>
                    <div class="severity-bar minor" [style.width.%]="(item.minor / item.total) * 100"></div>
                  </div>
                  <div class="chart-value">{{ item.total }}</div>
                </div>
              }
            </div>
          </div>
        </div>

        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">Findings by Department</h3>
          </div>
          <div class="chart-body">
            <div class="donut-chart">
              @for (dept of departmentData; track dept.name) {
                <div class="dept-item">
                  <div class="dept-color" [style.background]="dept.color"></div>
                  <div class="dept-name">{{ dept.name }}</div>
                  <div class="dept-count">{{ dept.count }}</div>
                </div>
              }
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .audit-reports-page {
      padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      background: var(--bg-primary);
      min-height: calc(100vh - 64px);
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      margin-bottom: 24px;
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
    }

    .filters-bar {
      display: flex;
      gap: 12px;
      margin-bottom: 24px;
    }

    .filter-select {
      padding: 10px 32px 10px 12px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      color: var(--text-primary);
      font-size: 14px;
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
    }

    .metric-card.critical {
      border-left-color: #ef4444;
    }

    .metric-card.major {
      border-left-color: #f59e0b;
    }

    .metric-card.minor {
      border-left-color: #3b82f6;
    }

    .metric-card.total {
      border-left-color: var(--color-chart-chapter-trend);
    }

    .metric-value {
      font-size: 32px;
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: 8px;
    }

    .metric-label {
      font-size: 13px;
      font-weight: 500;
      color: var(--text-secondary);
      text-transform: uppercase;
    }

    .charts-section {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(450px, 1fr));
      gap: 24px;
    }

    .chart-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
    }

    .chart-header {
      padding: 20px;
      border-bottom: 1px solid var(--border-color);
    }

    .chart-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .chart-body {
      padding: 20px;
    }

    .bar-chart {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .chart-row {
      display: grid;
      grid-template-columns: 60px 1fr 40px;
      gap: 12px;
      align-items: center;
    }

    .chart-label {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .chart-bars {
      display: flex;
      height: 32px;
      background: var(--bg-primary);
      border-radius: 6px;
      overflow: hidden;
    }

    .severity-bar {
      height: 100%;
    }

    .severity-bar.critical {
      background: #ef4444;
    }

    .severity-bar.major {
      background: #f59e0b;
    }

    .severity-bar.minor {
      background: #3b82f6;
    }

    .chart-value {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      text-align: right;
    }

    .donut-chart {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .dept-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px;
      background: var(--bg-primary);
      border-radius: 6px;
    }

    .dept-color {
      width: 12px;
      height: 12px;
      border-radius: 3px;
    }

    .dept-name {
      flex: 1;
      font-size: 14px;
      color: var(--text-primary);
    }

    .dept-count {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
    }

    @media (max-width: 768px) {
      .charts-section {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AuditReportsComponent implements OnInit {
  private readonly api = inject(ReportsApiService);
  private readonly context = inject(ComplianceContextService);
  private readonly destroyRef = inject(DestroyRef);

  selectedPeriod = 'month';
  loading = signal(false);
  error = signal<string | null>(null);

  criticalFindings = 0;
  majorFindings = 0;
  minorFindings = 0;
  totalFindings = 0;

  findingsByChapter: { chapter: string; critical: number; major: number; minor: number; total: number }[] = [];
  departmentData: { name: string; count: number; color: string }[] = [];

  private readonly deptColors = ['#3b82f6', '#10b981', '#f59e0b', '#0d9488', '#ef4444'];

  ngOnInit(): void {
    this.context.hospitalId$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    this.load();
  }

  load(): void {
    loadReport(
      () => this.api.getAuditReport(periodToDateRange(this.selectedPeriod)),
      (r) => this.apply(r),
      this.loading,
      this.error,
    );
  }

  onExport(): void {
    exportReportFile(this.api, 'audit', 'pdf', periodToDateRange(this.selectedPeriod));
  }

  private apply(r: AuditReport): void {
    this.criticalFindings = r.criticalFindings;
    this.majorFindings = r.majorFindings;
    this.minorFindings = r.minorFindings;
    this.totalFindings = r.totalFindings;
    this.findingsByChapter = (r.findingsByChapter ?? []).map((c) => ({
      chapter: c.chapterCode || c.chapterName,
      critical: c.critical,
      major: c.major,
      minor: c.minor,
      total: c.total,
    }));
    this.departmentData = (r.findingsByDepartment ?? []).map((d, i) => ({
      name: d.department,
      count: d.count,
      color: this.deptColors[i % this.deptColors.length],
    }));
  }
}
