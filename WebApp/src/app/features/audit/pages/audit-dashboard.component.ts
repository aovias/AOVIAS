import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { ControlCenterService } from '../../control-center/services/control-center.service';
import { AuditorApiService } from '../../auditor/services/auditor-api.service';
import { ReportsApiService } from '../../reporting/services/reports-api.service';
import { FindingsWorkspaceService } from '../findings-workspace/services/findings-workspace.service';
import type { FindingsWorkspaceDashboard } from '../findings-workspace/models/findings-workspace.model';
import { enumIn } from '../../../shared/utils/enum-label.util';
import {
  AuditManagementApiService,
  type ScheduledAuditDto,
} from '../services/audit-management-api.service';
import type {
  ActivityEventDto,
  AuditReadinessDto,
  ComplianceCommandCenterKpiDto,
} from '../../control-center/models/control-center.models';
import type { AuditorDashboard } from '../../auditor/models/auditor-workspace.model';
import type { AuditReport } from '../../reporting/models/report.models';
import { RoleDashboardComponent } from '../../../shared/ui/workspace/role-dashboard.component';
import { injectAuditWorkspace } from '../workspace/services/audit-workspace-participation.service';

interface SummaryCard {
  title: string;
  value: string | number;
  color: string;
  trend?: string;
}

interface FindingHeatmap {
  chapter: string;
  critical: number;
  major: number;
  minor: number;
}

@Component({
  selector: 'app-audit-dashboard',
  standalone: true,
  imports: [CommonModule, RoleDashboardComponent],
  template: `
    <div class="audit-dashboard">
      <div class="page-header">
        <div class="header-content">
          <h1 class="page-title">Internal Audit</h1>
          <p class="page-description">
            Internal audit program dashboard — live KPIs from Control Center, auditor workspace, and
            audit analytics.
          </p>
        </div>
      </div>

      <!--
        Stage 2, Phase 1: attention and actions come before the KPI grid, so the
        overview opens on what to do rather than what to read. Renders whatever
        the signed-in user's persona resolves to (Phase 2).
      -->
      <app-role-dashboard />

      @if (loading()) {
        <p class="muted">Loading audit KPIs…</p>
      }
      @if (error()) {
        <p class="error">{{ error() }} <button type="button" (click)="load()">Retry</button></p>
      }

      <div class="summary-cards">
        @for (card of summaryCards(); track card.title) {
          <div class="summary-card" [style.border-left-color]="card.color">
            <div class="card-content">
              <div class="card-value">{{ card.value }}</div>
              <div class="card-title">{{ card.title }}</div>
              @if (card.trend) {
                <div class="card-trend">{{ card.trend }}</div>
              }
            </div>
          </div>
        }
      </div>

      <div class="dashboard-grid">
        <div class="heatmap-section">
          <div class="section-header">
            <h2 class="section-title">Findings by chapter</h2>
            <p class="section-description">From hospital audit report analytics</p>
          </div>
          <div class="heatmap-card">
            <div class="heatmap-table">
              <div class="heatmap-row header-row">
                <div class="heatmap-cell">Chapter</div>
                <div class="heatmap-cell">Critical</div>
                <div class="heatmap-cell">Major</div>
                <div class="heatmap-cell">Minor</div>
                <div class="heatmap-cell">Total</div>
              </div>
              @for (row of heatmapData(); track row.chapter) {
                <div class="heatmap-row">
                  <div class="heatmap-cell">
                    <strong>{{ row.chapter }}</strong>
                  </div>
                  <div class="heatmap-cell">{{ row.critical }}</div>
                  <div class="heatmap-cell">{{ row.major }}</div>
                  <div class="heatmap-cell">{{ row.minor }}</div>
                  <div class="heatmap-cell">{{ row.critical + row.major + row.minor }}</div>
                </div>
              } @empty {
                <div class="heatmap-row">
                  <div class="heatmap-cell muted">No chapter findings yet.</div>
                </div>
              }
            </div>
          </div>
        </div>

        <div class="activity-section">
          <div class="section-header">
            <h2 class="section-title">Recent activity</h2>
            <p class="section-description">Control Center activity stream</p>
          </div>
          <div class="activity-list">
            @for (item of activity(); track item.id) {
              <div class="activity-item">
                <div class="activity-body">
                  <div class="activity-title">{{ item.title }}</div>
                  <div class="activity-desc">{{ item.description }}</div>
                  <div class="activity-meta">
                    {{ item.user || 'System' }} · {{ item.timestamp | date: 'short' }}
                  </div>
                </div>
              </div>
            } @empty {
              <p class="muted">No recent audit activity.</p>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .audit-dashboard {
        padding: 24px;
      }
      .page-header {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        margin-bottom: 24px;
        flex-wrap: wrap;
      }
      .page-title {
        margin: 0 0 8px;
        font-size: var(--font-size-page-title);
        font-weight: 600;
        color: var(--text-primary, #111);
      }
      .page-description {
        margin: 0;
        color: var(--text-secondary, #666);
        font-size: var(--font-size-subtitle);
      }
      .header-actions {
        display: flex;
        gap: 8px;
        align-items: start;
        flex-wrap: wrap;
      }
      .link-btn {
        padding: 8px 14px;
        border-radius: 6px;
        border: 1px solid var(--border-color, #ddd);
        text-decoration: none;
        color: var(--text-primary, #111);
        font-size: 13px;
      }
      .link-btn.primary {
        background: var(--primary-color, var(--color-primary));
        color: #fff;
        border-color: transparent;
      }
      .summary-cards {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 12px;
        margin-bottom: 24px;
      }
      .summary-card {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-left: 4px solid;
        border-radius: 8px;
        padding: 16px;
      }
      .card-value {
        font-size: 28px;
        font-weight: 700;
      }
      .card-title {
        font-size: 13px;
        color: var(--text-secondary, #666);
        margin-top: 4px;
      }
      .card-trend {
        font-size: 12px;
        color: var(--text-secondary, #888);
        margin-top: 6px;
      }
      .dashboard-grid {
        display: grid;
        grid-template-columns: 1.4fr 1fr;
        gap: 20px;
      }
      .section-title {
        margin: 0 0 4px;
        font-size: 18px;
      }
      .section-description {
        margin: 0 0 12px;
        font-size: 13px;
        color: var(--text-secondary, #666);
      }
      .heatmap-card,
      .activity-list {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 12px;
      }
      .heatmap-row {
        display: grid;
        grid-template-columns: 1.2fr repeat(4, 1fr);
        gap: 8px;
        padding: 8px 4px;
        border-bottom: 1px solid var(--border-color, #f0f0f0);
        font-size: 13px;
      }
      .header-row {
        font-weight: 600;
        color: var(--text-secondary, #666);
      }
      .activity-item {
        padding: 10px 4px;
        border-bottom: 1px solid var(--border-color, #f0f0f0);
      }
      .activity-title {
        font-weight: 600;
        font-size: 14px;
      }
      .activity-desc,
      .activity-meta,
      .muted {
        font-size: 13px;
        color: var(--text-secondary, #666);
      }
      .error {
        color: #ef4444;
      }
      @media (max-width: 960px) {
        .dashboard-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class AuditDashboardComponent implements OnInit {
  private readonly cc = inject(ControlCenterService);
  private readonly auditorApi = inject(AuditorApiService);
  private readonly reportsApi = inject(ReportsApiService);
  private readonly scheduleApi = inject(AuditManagementApiService);
  private readonly findingsApi = inject(FindingsWorkspaceService);
  private readonly destroyRef = inject(DestroyRef);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly summaryCards = signal<SummaryCard[]>([]);
  readonly heatmapData = signal<FindingHeatmap[]>([]);
  readonly activity = signal<ActivityEventDto[]>([]);

  ngOnInit(): void {
    this.publishParticipation();
    this.load();
  }

  /**
   * Overview's declaration.
   *
   * `stage: null` — the programme dashboard is a vantage point over the whole
   * lifecycle, not a step within it, so it contributes no toolbar actions and
   * the status bar stays hidden. It still declares a subject and timeline so
   * the panel and the assistant have something to work with.
   */
  private publishParticipation(): void {
    this.workspace.set({
      stage: null,
      subject: { kind: 'none', title: 'Internal audit programme' },
      relatedRecords: this.summaryCards().map((c) => ({
        id: c.title,
        label: c.title,
        kind: 'Programme KPI',
        status: String(c.value),
      })),
      timeline: this.activity()
        .slice(0, 8)
        .map((e) => ({
          id: e.id,
          label: e.title,
          at: e.timestamp,
          actor: e.user || undefined,
        })),
      aiContext: {
        facts: Object.fromEntries(this.summaryCards().map((c) => [c.title, String(c.value)])),
      },
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const year = new Date().getFullYear();

    forkJoin({
      kpis: this.cc
        .getKpis()
        .pipe(catchError(() => of(null as ComplianceCommandCenterKpiDto | null))),
      readiness: this.cc
        .getAuditReadiness()
        .pipe(catchError(() => of(null as AuditReadinessDto | null))),
      activity: this.cc.getActivityStream().pipe(catchError(() => of([] as ActivityEventDto[]))),
      auditor: this.auditorApi
        .getDashboard()
        .pipe(catchError(() => of(null as AuditorDashboard | null))),
      report: this.reportsApi
        .getAuditReport()
        .pipe(catchError(() => of(null as AuditReport | null))),
      schedule: this.scheduleApi
        .getCalendar({ year })
        .pipe(catchError(() => of([] as ScheduledAuditDto[]))),
      // Authoritative source for finding counts — the same service the Findings Workbench renders.
      // Previously the Overview inferred them from a fallback chain across unrelated payloads.
      findings: this.findingsApi
        .getDashboard()
        .pipe(catchError(() => of(null as FindingsWorkspaceDashboard | null))),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe(({ kpis, readiness, activity, auditor, report, schedule, findings }) => {
        if (!kpis && !readiness && !auditor && !report) {
          this.error.set('Unable to load audit dashboard data.');
        }

        const now = Date.now();
        const upcoming = schedule.filter((a) => new Date(a.scheduledDate).getTime() >= now).length;
        const delayed = schedule.filter((a) => {
          const t = new Date(a.scheduledDate).getTime();
          // Case-insensitive: the schedule API emits camelCase (`completed`), so `!==` against
          // PascalCase counted every finished audit as delayed.
          return t < now && !enumIn(a.status, ['Completed', 'Closed']);
        }).length;

        // One definition of "open findings", shared with the Findings Workbench. The previous
        // fallback chain substituted `report.totalFindings` — a *total* — when the KPI feed omitted
        // the field, so the two screens disagreed (15 here vs 11 there) for the same measure.
        const openFindings = findings?.openFindings ?? kpis?.openFindings ?? 0;
        const criticalFindings = findings?.criticalFindings ?? readiness?.criticalFindings ?? 0;

        this.summaryCards.set([
          {
            title: 'Active audit sessions',
            value: auditor?.activeSessions ?? 0,
            color: '#3b82f6',
            trend: `${auditor?.observations ?? 0} observations`,
          },
          {
            title: 'Upcoming scheduled',
            value: upcoming || kpis?.upcomingAudits || 0,
            color: '#6366f1',
            trend: `${delayed} delayed`,
          },
          {
            title: 'Open findings',
            value: openFindings,
            color: '#f59e0b',
            trend: kpis
              ? `Trend ${kpis.openFindingsTrend >= 0 ? '+' : ''}${kpis.openFindingsTrend}`
              : undefined,
          },
          {
            title: 'Critical findings',
            // Same authoritative source as "Open findings" — not `auditor.criticalRisks`, which
            // counts risks rather than findings.
            value: criticalFindings,
            color: '#ef4444',
          },
          {
            title: 'Corrective actions pending',
            value: readiness?.correctiveActionsPending ?? 0,
            color: '#f59e0b',
          },
          {
            title: 'Audit readiness',
            value: `${Math.round(readiness?.confidenceScore ?? kpis?.auditConfidence ?? auditor?.readinessSnapshot ?? 0)}%`,
            color: '#10b981',
            trend: readiness?.status ?? undefined,
          },
        ]);

        this.heatmapData.set(
          (report?.findingsByChapter ?? []).map((c) => ({
            chapter: c.chapterCode || c.chapterName,
            critical: c.critical,
            major: c.major,
            minor: c.minor,
          })),
        );

        this.activity.set((activity ?? []).slice(0, 8));
        // Re-declare now the KPIs and activity stream have resolved.
        this.publishParticipation();
      });
  }
}
