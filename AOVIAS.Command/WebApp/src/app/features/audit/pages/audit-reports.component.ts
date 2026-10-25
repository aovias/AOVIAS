import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { ReportsApiService } from '../../reporting/services/reports-api.service';
import { injectAuditWorkspace } from '../workspace/services/audit-workspace-participation.service';
import { AuditorApiService } from '../../auditor/services/auditor-api.service';
import {
  exportReportFile,
  loadReport,
  periodToDateRange,
} from '../../reporting/services/report-page.helper';
import type { AuditReport } from '../../reporting/models/report.models';
import { type AuditorSessionSummary } from '../../auditor/models/auditor-workspace.model';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-audit-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="audit-reports-page">
      <div class="page-header">
        <div>
          <h1 class="page-title">Final Audit Reports</h1>
          <p class="page-description">Hospital audit findings rollup and session report exports.</p>
        </div>
        <div class="header-actions">
          <button
            class="btn-export"
            type="button"
            (click)="onExport('excel')"
            [disabled]="loading()"
          >
            Export Excel
          </button>
          <button class="btn-export" type="button" (click)="onExport('pdf')" [disabled]="loading()">
            Export PDF
          </button>
        </div>
      </div>

      <div class="filters-bar">
        <select [(ngModel)]="selectedPeriod" class="filter-select" (ngModelChange)="load()">
          @for (p of periodOptions(); track p.value) {
            <option [value]="p.value">{{ p.label }}</option>
          }
        </select>
        <button type="button" class="btn-export" (click)="load()">Refresh</button>
      </div>

      @if (loading()) {
        <p class="muted">Loading…</p>
      }
      @if (error()) {
        <p class="error">{{ error() }} <button type="button" (click)="load()">Retry</button></p>
      }

      <div class="metrics-grid">
        <div class="metric-card critical">
          <div class="metric-value">{{ report()?.criticalFindings ?? 0 }}</div>
          <div class="metric-label">Critical</div>
        </div>
        <div class="metric-card major">
          <div class="metric-value">{{ report()?.majorFindings ?? 0 }}</div>
          <div class="metric-label">Major</div>
        </div>
        <div class="metric-card minor">
          <div class="metric-value">{{ report()?.minorFindings ?? 0 }}</div>
          <div class="metric-label">Minor</div>
        </div>
        <div class="metric-card total">
          <div class="metric-value">{{ report()?.totalFindings ?? 0 }}</div>
          <div class="metric-label">Total</div>
        </div>
      </div>

      <section class="panel">
        <h2>Findings by chapter</h2>
        <table>
          <thead>
            <tr>
              <th>Chapter</th>
              <th>Critical</th>
              <th>Major</th>
              <th>Minor</th>
              <th>Total</th>
            </tr>
          </thead>
          <tbody>
            @for (c of report()?.findingsByChapter ?? []; track c.chapterId || c.chapterCode) {
              <tr>
                <td>{{ c.chapterCode }} — {{ c.chapterName }}</td>
                <td>{{ c.critical }}</td>
                <td>{{ c.major }}</td>
                <td>{{ c.minor }}</td>
                <td>{{ c.total }}</td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5" class="muted">No findings in selected period.</td>
              </tr>
            }
          </tbody>
        </table>
      </section>

      <section class="panel">
        <h2>Session reports</h2>
        <table>
          <thead>
            <tr>
              <th>Session</th>
              <th>Type</th>
              <th>Status</th>
              <th>Findings</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (s of sessions(); track s.id) {
              <tr>
                <td>{{ s.sessionNumber }}</td>
                <td>{{ auditTypeLabel(s.auditType) }}</td>
                <td>{{ sessionStatusLabel(s.status) }}</td>
                <td>{{ s.findingsCount }}</td>
                <td><a [routerLink]="['/app/audit/execution/sessions', s.id]">Open</a></td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5" class="muted">No auditor sessions yet.</td>
              </tr>
            }
          </tbody>
        </table>
      </section>
    </div>
  `,
  styles: [
    `
      .audit-reports-page {
        padding: var(--page-padding-block-start) var(--page-padding-inline)
          var(--page-padding-block-end);
      }
      .page-header {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        margin-bottom: 20px;
        flex-wrap: wrap;
      }
      .page-title {
        margin: 0 0 8px;
        font-size: var(--font-size-page-title);
      }
      .page-description,
      .muted {
        margin: 0;
        color: var(--text-secondary, #666);
        font-size: 14px;
      }
      .header-actions {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
      }
      .btn-export,
      .btn-secondary {
        padding: 10px 16px;
        border-radius: 6px;
        border: none;
        cursor: pointer;
        font-size: 13px;
        text-decoration: none;
      }
      .btn-export {
        background: var(--primary-color, var(--color-primary));
        color: #fff;
      }
      .btn-secondary {
        background: transparent;
        border: 1px solid var(--border-color, #ddd);
        color: var(--text-primary, #111);
        display: inline-flex;
        align-items: center;
      }
      .filters-bar {
        display: flex;
        gap: 12px;
        margin-bottom: 20px;
      }
      .filter-select {
        padding: 10px 12px;
        border-radius: 6px;
        border: 1px solid var(--border-color, #ddd);
      }
      .metrics-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 12px;
        margin-bottom: 24px;
      }
      .metric-card {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-left: 4px solid;
        border-radius: 8px;
        padding: 16px;
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
        font-size: 28px;
        font-weight: 700;
      }
      .metric-label {
        font-size: 12px;
        color: var(--text-secondary, #666);
        text-transform: uppercase;
      }
      .panel {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 16px;
        margin-bottom: 20px;
      }
      table {
        width: 100%;
        border-collapse: collapse;
        font-size: 14px;
      }
      th,
      td {
        text-align: left;
        padding: 10px 8px;
        border-bottom: 1px solid var(--border-color, #f0f0f0);
      }
      .error {
        color: #ef4444;
      }
    `,
  ],
})
export class AuditReportsComponent implements OnInit {
  private readonly configuration = inject(PlatformConfigurationService);

  /** Reporting windows offered to the user — configuration, not a literal list. */
  readonly periodOptions = this.configuration.options(CONFIGURATION_CATEGORY.reportPeriod);

  private readonly reportsApi = inject(ReportsApiService);
  private readonly auditorApi = inject(AuditorApiService);
  private readonly destroyRef = inject(DestroyRef);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  /**
   * Session reports previously printed the raw wire values (`internalAudit`, `inProgress`) straight
   * into the table. Both labels now come from configuration, so this screen and the Execution
   * sessions grid cannot render the same value differently.
   */
  auditTypeLabel(type: string | null | undefined): string {
    return this.configuration.auditTypeLabel(type);
  }

  sessionStatusLabel(status: string | null | undefined): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.auditSessionStatus, status);
  }

  /**
   * Defaults to the programme year, not a rolling month.
   *
   * Audits run on a programme cycle, so a 30-day window frequently falls between cycles and the
   * report opens empty even when the year holds a full set of findings (verified: rolling month
   * → 0 findings, programme year → 24). An empty landing state reads as "no data exists" rather
   * than "no data in this narrow window" — the wrong first impression for a reporting screen.
   */
  selectedPeriod: 'programYear' | 'month' | 'quarter' = 'programYear';
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly report = signal<AuditReport | null>(null);
  readonly sessions = signal<AuditorSessionSummary[]>([]);

  ngOnInit(): void {
    this.auditorApi
      .getSessions()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of([] as AuditorSessionSummary[])),
      )
      .subscribe((list) => {
        this.sessions.set(list);
        this.publishParticipation();
      });
    this.publishParticipation();
    this.load();
  }

  load(): void {
    loadReport(
      () => this.reportsApi.getAuditReport(periodToDateRange(this.selectedPeriod)),
      (r) => this.report.set(r),
      this.loading,
      this.error,
    );
  }

  /** Reports close the lifecycle; export and generate are its operations. */
  private publishParticipation(): void {
    this.workspace.set({
      stage: 'reports',
      subject: { kind: 'none', title: 'Audit reports' },
      actions: [
        {
          id: 'reports.export-pdf',
          label: 'Export PDF',
          variant: 'primary',
          run: () => this.onExport('pdf'),
        },
        { id: 'reports.export-excel', label: 'Export Excel', run: () => this.onExport('excel') },
        { id: 'reports.refresh', label: 'Regenerate', run: () => this.load() },
      ],
      workflowState: {
        stage: 'reports',
        statusLabel: this.report() ? 'Report available' : 'Not generated',
        completionCriteria: 'Audit report generated and issued',
      },
      relatedRecords: this.sessions()
        .slice(0, 8)
        .map((sn) => ({
          id: sn.id,
          label: sn.scope || sn.sessionNumber,
          kind: 'Audit session',
          status: String(sn.status),
        })),
      aiContext: { facts: { sessionCount: String(this.sessions().length) } },
    });
  }

  onExport(format: 'excel' | 'pdf'): void {
    exportReportFile(this.reportsApi, 'audit', format, periodToDateRange(this.selectedPeriod));
  }
}
