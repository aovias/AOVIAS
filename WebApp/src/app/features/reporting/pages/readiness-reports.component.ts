import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { ReadinessReport } from '../models/report.models';
import { ReportsApiService } from '../services/reports-api.service';
import { exportReportFile, loadReport } from '../services/report-page.helper';

@Component({
  selector: 'app-readiness-reports',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="readiness-reports-page">
      <div class="page-header">
        <h1 class="page-title">Readiness Reports</h1>
        <p class="page-description">Evaluate NABH accreditation readiness with risk indicators and compliance gaps.</p>
      </div>

      <div class="readiness-score-card">
        <div class="score-gauge">
          <svg class="gauge-svg" viewBox="0 0 200 200">
            <circle class="gauge-bg" cx="100" cy="100" r="80" fill="none" stroke-width="20"/>
            <circle class="gauge-fill" cx="100" cy="100" r="80" fill="none" stroke-width="20"
              [style.stroke-dasharray]="getGaugeDashArray()" style="stroke: #10b981"/>
            <text class="gauge-score" x="100" y="95" text-anchor="middle">{{ readinessScore }}%</text>
            <text class="gauge-label" x="100" y="115" text-anchor="middle">{{ readinessScore >= 80 ? 'Ready' : 'In Progress' }}</text>
          </svg>
        </div>
        <div class="score-details">
          <h2 class="score-title">NABH Readiness Score</h2>
          <p class="score-description">Your organization shows strong compliance readiness with minor gaps to address</p>
        </div>
      </div>

      <div class="risk-indicators">
        <h3 class="section-title">High Risk Chapters</h3>
        <div class="risk-grid">
          @for (risk of highRiskChapters; track risk.chapter) {
            <div class="risk-card">
              <div class="risk-header">
                <span class="risk-badge">{{ risk.chapter }}</span>
                <span class="risk-level" [class]="risk.level">{{ risk.level }}</span>
              </div>
              <div class="risk-details">{{ risk.details }}</div>
            </div>
          }
        </div>
      </div>

      <div class="gaps-section">
        <h3 class="section-title">Clauses Missing Evidence</h3>
        <div class="gaps-list">
          @for (gap of evidenceGaps; track gap.clause) {
            <div class="gap-item">
              <div class="gap-clause">{{ gap.clause }}</div>
              <div class="gap-description">{{ gap.description }}</div>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .readiness-reports-page {
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
      margin: 0 0 32px 0;
    }

    .readiness-score-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 32px;
      display: flex;
      align-items: center;
      gap: 40px;
      margin-bottom: 32px;
    }

    .score-gauge {
      width: 200px;
      height: 200px;
      flex-shrink: 0;
    }

    .gauge-svg {
      width: 100%;
      height: 100%;
    }

    .gauge-bg {
      stroke: var(--border-color);
    }

    .gauge-fill {
      stroke-linecap: round;
      transform: rotate(-90deg);
      transform-origin: center;
      transition: stroke-dasharray 1s ease;
    }

    .gauge-score {
      font-size: 42px;
      font-weight: 700;
      fill: var(--text-primary);
    }

    .gauge-label {
      font-size: 14px;
      font-weight: 500;
      fill: var(--text-secondary);
    }

    .score-details {
      flex: 1;
    }

    .score-title {
      font-size: var(--font-size-section-title);
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 12px 0;
    }

    .score-description {
      font-size: 14px;
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.6;
    }

    .risk-indicators {
      margin-bottom: 32px;
    }

    .section-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 16px 0;
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

    .risk-badge {
      padding: 4px 12px;
      background: var(--primary-bg-light);
      color: var(--primary-color);
      border-radius: 6px;
      font-size: 13px;
      font-weight: 600;
    }

    .risk-level {
      padding: 4px 10px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
    }

    .risk-level.high {
      background: rgba(239, 68, 68, 0.15);
      color: #ef4444;
    }

    .risk-level.medium {
      background: rgba(245, 158, 11, 0.15);
      color: #f59e0b;
    }

    .risk-details {
      font-size: 13px;
      color: var(--text-secondary);
      line-height: 1.5;
    }

    .gaps-section {
      margin-bottom: 24px;
    }

    .gaps-list {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      overflow: hidden;
    }

    .gap-item {
      padding: 16px 20px;
      border-bottom: 1px solid var(--border-color);
    }

    .gap-item:last-child {
      border-bottom: none;
    }

    .gap-clause {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .gap-description {
      font-size: 13px;
      color: var(--text-secondary);
    }

    @media (max-width: 768px) {
      .readiness-score-card {
        flex-direction: column;
        text-align: center;
      }
    }
  `]
})
export class ReadinessReportsComponent implements OnInit {
  private readonly api = inject(ReportsApiService);
  private readonly context = inject(ComplianceContextService);
  private readonly destroyRef = inject(DestroyRef);

  loading = signal(false);
  error = signal<string | null>(null);
  readinessScore = 0;
  highRiskChapters: { chapter: string; level: string; details: string }[] = [];
  evidenceGaps: { clause: string; description: string }[] = [];
  recommendations: { text: string; priority: string }[] = [];

  ngOnInit(): void {
    this.context.hospitalId$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    this.load();
  }

  load(): void {
    loadReport(() => this.api.getReadinessReport(), (r) => this.apply(r), this.loading, this.error);
  }

  onExport(): void {
    exportReportFile(this.api, 'readiness', 'pdf');
  }

  getGaugeDashArray(): string {
    const circumference = 2 * Math.PI * 80;
    const fillLength = (this.readinessScore / 100) * circumference;
    return `${fillLength} ${circumference}`;
  }

  private apply(r: ReadinessReport): void {
    this.readinessScore = r.readinessScore;
    this.highRiskChapters = (r.highRiskChapters ?? []).map((c) => ({
      chapter: c.chapterCode || c.chapterName,
      level: c.level,
      details: c.details,
    }));
    this.evidenceGaps = (r.missingEvidenceClauses ?? []).map((m) => ({
      clause: m.clauseCode,
      description: m.description,
    }));
    this.recommendations = r.recommendations ?? [];
  }
}
