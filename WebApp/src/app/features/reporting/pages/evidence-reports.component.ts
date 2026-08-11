import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { EvidenceReport } from '../models/report.models';
import { ReportsApiService } from '../services/reports-api.service';
import { exportReportFile, loadReport } from '../services/report-page.helper';

@Component({
  selector: 'app-evidence-reports',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="evidence-reports-page">
      <div class="page-header">
        <h1 class="page-title">Evidence Reports</h1>
        <p class="page-description">Review documentation coverage, approval status, and evidence upload trends.</p>
      </div>

      <div style="margin-bottom:12px">
        <button type="button" (click)="load()">Refresh</button>
        <button type="button" (click)="onExport()">Export</button>
      </div>
      @if (loading()) { <p class="page-description">Loading…</p> }
      @if (error()) { <p class="page-description" style="color:#ef4444">{{ error() }} <button type="button" (click)="load()">Retry</button></p> }
      <div class="metrics-grid">
        <div class="metric-card" style="border-left-color: #10b981">
          <div class="metric-value">{{ approvedEvidence }}</div>
          <div class="metric-label">Approved Evidence</div>
        </div>
        <div class="metric-card" style="border-left-color: #f59e0b">
          <div class="metric-value">{{ pendingReview }}</div>
          <div class="metric-label">Pending Review</div>
        </div>
        <div class="metric-card" style="border-left-color: #ef4444">
          <div class="metric-value">{{ rejectedEvidence }}</div>
          <div class="metric-label">Rejected</div>
        </div>
        <div class="metric-card" style="border-left-color: #3b82f6">
          <div class="metric-value">{{ coverageRatio }}%</div>
          <div class="metric-label">Coverage Ratio</div>
        </div>
      </div>

      <div class="chart-card">
        <div class="chart-header">
          <h3 class="chart-title">Evidence by Chapter</h3>
        </div>
        <div class="chart-body">
          @for (chapter of evidenceByChapter; track chapter.name) {
            <div class="evidence-row">
              <div class="evidence-label">{{ chapter.name }}</div>
              <div class="evidence-bar-container">
                <div class="evidence-bar approved" [style.width.%]="chapter.approved"></div>
                <div class="evidence-bar pending" [style.width.%]="chapter.pending"></div>
              </div>
              <div class="evidence-count">{{ chapter.total }}</div>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .evidence-reports-page {
      padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      background: var(--bg-primary);
      min-height: calc(100vh - 64px);
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
      margin: 0 0 24px 0;
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
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .evidence-row {
      display: grid;
      grid-template-columns: 80px 1fr 60px;
      gap: 16px;
      align-items: center;
    }

    .evidence-label {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .evidence-bar-container {
      display: flex;
      height: 32px;
      background: var(--bg-primary);
      border-radius: 6px;
      overflow: hidden;
    }

    .evidence-bar {
      height: 100%;
    }

    .evidence-bar.approved {
      background: #10b981;
    }

    .evidence-bar.pending {
      background: #f59e0b;
    }

    .evidence-count {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      text-align: right;
    }
  `]
})
export class EvidenceReportsComponent implements OnInit {
  private readonly api = inject(ReportsApiService);
  private readonly context = inject(ComplianceContextService);
  private readonly destroyRef = inject(DestroyRef);

  loading = signal(false);
  error = signal<string | null>(null);
  approvedEvidence = 0;
  pendingReview = 0;
  rejectedEvidence = 0;
  coverageRatio = 0;
  evidenceByChapter: { name: string; approved: number; pending: number; total: number }[] = [];

  ngOnInit(): void {
    this.context.hospitalId$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    this.load();
  }

  load(): void {
    loadReport(() => this.api.getEvidenceReport(), (r) => this.apply(r), this.loading, this.error);
  }

  onExport(): void {
    exportReportFile(this.api, 'evidence', 'pdf');
  }

  private apply(r: EvidenceReport): void {
    this.approvedEvidence = r.approvedEvidence;
    this.pendingReview = r.pendingReview;
    this.rejectedEvidence = r.rejectedEvidence;
    this.coverageRatio = r.coverageRatio;
    this.evidenceByChapter = (r.chapterEvidence ?? []).map((c) => {
      const total = Math.max(1, c.total);
      return {
        name: c.chapterCode || c.chapterName,
        approved: Math.round((c.approved / total) * 100),
        pending: Math.round((c.pending / total) * 100),
        total: c.total,
      };
    });
  }
}
