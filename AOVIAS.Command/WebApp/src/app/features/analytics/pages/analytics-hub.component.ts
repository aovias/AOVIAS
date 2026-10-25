import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AnalyticsApiService } from '../services/analytics-api.service';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import {
  AnalyticsBreakdownItem,
  AnalyticsMetricCard,
  AnalyticsOverview,
  AnalyticsSection,
  AnalyticsTrends,
  CapaAnalytics,
  DepartmentAnalytics,
  ExecutiveAnalytics,
  OperationalAnalytics,
  RcaAnalytics,
  SlaAnalytics,
  TrendSeries,
} from '../models/analytics.model';

const SECTIONS: { id: AnalyticsSection; label: string }[] = [
  { id: 'overview', label: 'Overview' },
  { id: 'executive', label: 'Executive' },
  { id: 'operational', label: 'Operational' },
  { id: 'rca', label: 'RCA' },
  { id: 'capa', label: 'CAPA' },
  { id: 'sla', label: 'SLA' },
  { id: 'departments', label: 'Departments' },
];

@Component({
  selector: 'app-analytics-hub',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="analytics-hub">
      <header class="hub-header">
        <div>
          <h1>Analytics Hub</h1>
          <p class="subtitle">Read-only compliance intelligence — execution remains in operational modules.</p>
          @if (frameworkName()) {
            <p class="context">{{ frameworkName() }}</p>
          }
        </div>
        <button type="button" class="refresh-btn" (click)="reload()" [disabled]="loading()">Refresh</button>
      </header>

      <nav class="section-nav" aria-label="Analytics sections">
        @for (section of sections; track section.id) {
          <a
            [routerLink]="section.id === 'overview' ? ['/app/analytics'] : ['/app/analytics', section.id]"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: section.id === 'overview' }"
            class="section-link"
          >
            {{ section.label }}
          </a>
        }
      </nav>

      @if (error()) {
        <div class="error-banner" role="alert">{{ error() }}</div>
      }

      @if (loading()) {
        <div class="loading">Loading analytics…</div>
      } @else {
        @switch (activeSection()) {
          @case ('overview') {
            @if (overview(); as data) {
              <section class="kpi-grid">
                @for (card of data.kpiCards; track card.key) {
                  <article class="kpi-card">
                    <span class="kpi-label">{{ card.label }}</span>
                    <span class="kpi-value">
                      @if (card.value != null) {
                        {{ card.value | number: '1.0-1' }}@if (card.unit) { {{ card.unit }} }
                      } @else if (card.unit) {
                        {{ card.unit }}
                      } @else {
                        —
                      }
                    </span>
                    @if (card.deepLink) {
                      <a class="deep-link" [routerLink]="card.deepLink.route" [queryParams]="card.deepLink.queryParams">
                        {{ card.deepLink.label }}
                      </a>
                    }
                  </article>
                }
              </section>
              @if (trends()?.series?.length) {
                <section class="panel">
                  <h2>Trend Datasets</h2>
                  @for (series of trends()!.series; track series.metricKey) {
                    <div class="trend-block">
                      <h3>{{ series.label }} ({{ series.granularity }})</h3>
                      @if (series.points.length) {
                        <table>
                          <thead><tr><th>Period</th><th>Value</th></tr></thead>
                          <tbody>
                            @for (point of series.points; track point.label) {
                              <tr><td>{{ point.label }}</td><td>{{ point.value | number: '1.0-2' }}</td></tr>
                            }
                          </tbody>
                        </table>
                      } @else {
                        <p class="empty">No historical points available yet.</p>
                      }
                    </div>
                  }
                </section>
              }
            }
          }
          @case ('executive') {
            @if (executive(); as data) {
              <section class="panel-grid">
                <article class="panel">
                  <h2>Compliance Readiness</h2>
                  <p class="big-number">{{ data.complianceReadiness.score | number: '1.0-1' }}%</p>
                  <!--
                    The API's source field is backend provenance (service + method name), not copy.
                    Rendering it put implementation detail in front of executives — see DEF-007.
                  -->
                  <p class="meta">Weighted score across all chapters in the active framework</p>
                </article>
                <article class="panel">
                  <h2>Accreditation Readiness</h2>
                  <p class="big-number">{{ data.accreditationReadiness.score | number: '1.0-1' }}%</p>
                  <p class="meta">Progress through the current accreditation journey</p>
                </article>
                <article class="panel">
                  <h2>SLA Health</h2>
                  <p class="big-number">{{ data.slaHealth.slaHealthScore | number: '1.0-1' }}%</p>
                  <p class="meta">Overdue: {{ data.slaHealth.overdueDeficiencies }}</p>
                </article>
                <article class="panel">
                  <h2>CAPA Effectiveness</h2>
                  <p class="big-number">{{ data.capaPerformance.capaEffectivenessRate | number: '1.0-1' }}%</p>
                  <p class="meta">Pending reviews: {{ data.capaPerformance.pendingReviews }}</p>
                </article>
              </section>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.departmentPerformance, title: 'Department Performance' }"></ng-container>
            }
          }
          @case ('operational') {
            @if (operational(); as data) {
              <section class="panel-grid">
                <article class="panel"><h2>Open Deficiencies</h2><p class="big-number">{{ data.deficiencyFunnel.open }}</p></article>
                <article class="panel"><h2>Task Backlog</h2><p class="big-number">{{ data.taskThroughput.notStarted + data.taskThroughput.inProgress }}</p></article>
                <article class="panel"><h2>Avg Resolution (hrs)</h2><p class="big-number">{{ data.averageResolutionHours ?? 0 | number: '1.0-1' }}</p></article>
                <article class="panel"><h2>Audit Completion</h2><p class="big-number">{{ data.auditCompletionRate ?? 0 | number: '1.0-1' }}%</p></article>
                <article class="panel"><h2>SLA Compliance</h2><p class="big-number">{{ data.slaComplianceRate ?? 0 | number: '1.0-1' }}%</p></article>
                <article class="panel"><h2>Overdue CAPA</h2><p class="big-number">{{ data.overdueWork?.overdueCapas ?? 0 }}</p></article>
                <article class="panel"><h2>Overdue Tasks</h2><p class="big-number">{{ data.overdueWork?.overdueTasks ?? 0 }}</p></article>
                <article class="panel"><h2>Recurring Issues</h2><p class="big-number">{{ data.recurringIssues }}</p></article>
              </section>
              <section class="panel">
                <div class="panel-header-row">
                  <h2>CAPA Aging</h2>
                  <button type="button" class="export-btn" (click)="exportOperationalCsv(data)">Export CSV</button>
                </div>
                @if (data.capaAging; as aging) {
                  <div class="aging-bars" aria-label="CAPA aging buckets">
                    <div class="aging-row"><span>≤30d</span><div class="bar" [style.width.%]="agingBarWidth(aging.within30, aging)"></div><span>{{ aging.within30 }}</span></div>
                    <div class="aging-row"><span>31–60d</span><div class="bar" [style.width.%]="agingBarWidth(aging.days31To60, aging)"></div><span>{{ aging.days31To60 }}</span></div>
                    <div class="aging-row"><span>61–90d</span><div class="bar" [style.width.%]="agingBarWidth(aging.days61To90, aging)"></div><span>{{ aging.days61To90 }}</span></div>
                    <div class="aging-row"><span>&gt;90d</span><div class="bar bar-warn" [style.width.%]="agingBarWidth(aging.over90, aging)"></div><span>{{ aging.over90 }}</span></div>
                  </div>
                  <p class="muted">Avg open CAPA age: {{ data.averageOpenCapaAgeDays ?? 0 | number: '1.0-1' }} days</p>
                } @else {
                  <p class="muted">Aging metrics unavailable.</p>
                }
              </section>
              <section class="panel">
                <h2>Deficiency Funnel</h2>
                <table>
                  <tbody>
                    <tr><td>Awaiting Assignment</td><td>{{ data.deficiencyFunnel.awaitingAssignment }}</td></tr>
                    <tr><td>In Progress</td><td>{{ data.deficiencyFunnel.inProgress }}</td></tr>
                    <tr><td>Awaiting Evidence</td><td>{{ data.deficiencyFunnel.awaitingEvidence }}</td></tr>
                    <tr><td>Verification Pending</td><td>{{ data.deficiencyFunnel.verificationPending }}</td></tr>
                    <tr><td>Verified</td><td>{{ data.deficiencyFunnel.verified }}</td></tr>
                    <tr><td>Closed</td><td>{{ data.deficiencyFunnel.closed }}</td></tr>
                  </tbody>
                </table>
              </section>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.departmentPerformance ?? [], title: 'Department Performance' }"></ng-container>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.overdueWork?.departmentOverdueCapas ?? [], title: 'Department Overdue CAPA' }"></ng-container>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.rootCauseTrend, title: 'Root Cause Trend' }"></ng-container>
            }
          }
          @case ('rca') {
            @if (rca(); as data) {
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.topRootCauses, title: 'Top Root Causes' }"></ng-container>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.rootCauseByDepartment, title: 'By Department' }"></ng-container>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.rootCauseByChapter, title: 'By Chapter' }"></ng-container>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.rootCauseByStage, title: 'By Stage' }"></ng-container>
            }
          }
          @case ('capa') {
            @if (capa(); as data) {
              <section class="panel-grid">
                <article class="panel"><h2>Effectiveness</h2><p class="big-number">{{ data.capaEffectivenessRate }}%</p></article>
                <article class="panel"><h2>Avg Closure Days</h2><p class="big-number">{{ data.averageClosureDays | number: '1.0-1' }}</p></article>
                <article class="panel"><h2>Pending Reviews</h2><p class="big-number">{{ data.pendingReviews }}</p></article>
                <article class="panel"><h2>Recurring</h2><p class="big-number">{{ data.recurringIssues }}</p></article>
              </section>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.departmentCapaPerformance, title: 'Department CAPA Performance' }"></ng-container>
            }
          }
          @case ('sla') {
            @if (sla(); as data) {
              <section class="panel-grid">
                <article class="panel"><h2>SLA Health</h2><p class="big-number">{{ data.slaHealthScore }}%</p></article>
                <article class="panel"><h2>Breach Rate</h2><p class="big-number">{{ data.breachRate }}%</p></article>
                <article class="panel"><h2>Escalation Rate</h2><p class="big-number">{{ data.escalationRate }}%</p></article>
                <article class="panel"><h2>Avg Resolution (hrs)</h2><p class="big-number">{{ data.averageResolutionHours | number: '1.0-1' }}</p></article>
              </section>
              <ng-container *ngTemplateOutlet="breakdownTable; context: { $implicit: data.departmentSlaCompliance, title: 'Department SLA Compliance' }"></ng-container>
            }
          }
          @case ('departments') {
            @if (departments(); as data) {
              <section class="panel">
                <h2>Department Scorecards</h2>
                <table>
                  <thead>
                    <tr>
                      <th>Department</th>
                      <th>Open Def.</th>
                      <th>CAPA</th>
                      <th>SLA Risk</th>
                      <th>Escalations</th>
                      <th>Tasks</th>
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (row of data.departments; track row.department) {
                      <tr>
                        <td>{{ row.department }}</td>
                        <td>{{ row.openDeficiencies }}</td>
                        <td>{{ row.capaOpen }}</td>
                        <td>{{ row.slaAtRisk }}</td>
                        <td>{{ row.escalations }}</td>
                        <td>{{ row.taskBacklog }}</td>
                        <td>
                          @if (row.deepLink) {
                            <a [routerLink]="row.deepLink.route" [queryParams]="row.deepLink.queryParams">Open</a>
                          }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </section>
            }
          }
        }
      }

      <ng-template #breakdownTable let-items let-title="title">
        <section class="panel">
          <h2>{{ title }}</h2>
          @if (items?.length) {
            <table>
              <thead><tr><th>Label</th><th>Count</th><th>Action</th></tr></thead>
              <tbody>
                @for (item of items; track item.label) {
                  <tr>
                    <td>{{ item.label }}</td>
                    <td>{{ item.count }}</td>
                    <td>
                      @if (item.deepLink) {
                        <a [routerLink]="item.deepLink.route" [queryParams]="item.deepLink.queryParams">Open</a>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <p class="empty">No data available.</p>
          }
        </section>
      </ng-template>
    </div>
  `,
  styles: [
    `
      .analytics-hub {
        min-height: 100%;
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
        background: var(--bg-primary);
        color: var(--text-primary);
      }

      .hub-header { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; }
      .hub-header h1 { margin: 0; font-size: var(--font-size-page-title); color: var(--text-primary); }

      .subtitle, .context, .meta { color: var(--text-secondary); margin: 0.25rem 0 0; }

      .section-nav { display: flex; flex-wrap: wrap; gap: 0.5rem; }

      .section-link {
        padding: 0.4rem 0.75rem;
        border-radius: 999px;
        text-decoration: none;
        border: 1px solid var(--border-color);
        color: var(--text-secondary);
        background: var(--card-bg);
        transition: background 0.2s ease, color 0.2s ease, border-color 0.2s ease;
      }

      .section-link:hover {
        background: var(--hover-bg);
        color: var(--text-primary);
        border-color: color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
      }

      .section-link.active {
        background: var(--primary-color);
        color: var(--color-text-inverse);
        border-color: transparent;
      }

      .kpi-grid, .panel-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 0.75rem; }

      .kpi-card, .panel {
        border: 1px solid var(--border-color);
        border-radius: 0.75rem;
        padding: 1rem;
        background: var(--card-bg);
        box-shadow: var(--shadow-xs);
      }

      .panel h2, .panel h3, .trend-block h3 { color: var(--text-primary); margin: 0 0 0.5rem; }

      .kpi-label { display: block; font-size: 0.85rem; color: var(--text-secondary); }
      .kpi-value { display: block; font-size: 1.4rem; font-weight: 600; margin-top: 0.25rem; color: var(--text-primary); }
      .big-number { font-size: 1.75rem; font-weight: 700; margin: 0.25rem 0; color: var(--text-primary); }

      .deep-link {
        display: inline-block;
        margin-top: 0.5rem;
        font-size: 0.85rem;
        color: var(--primary-color);
        text-decoration: none;
      }

      table a {
        font-size: 0.85rem;
        color: var(--primary-color);
        text-decoration: none;
      }

      .deep-link:hover, table a:hover {
        color: var(--primary-hover);
        text-decoration: underline;
      }

      table { width: 100%; border-collapse: collapse; }
      th { text-align: left; padding: 0.5rem; border-bottom: 1px solid var(--border-color); color: var(--text-secondary); font-weight: 600; }
      td { text-align: left; padding: 0.5rem; border-bottom: 1px solid var(--border-color); color: var(--text-primary); }

      .error-banner {
        padding: 0.75rem 1rem;
        border-radius: 0.5rem;
        background: color-mix(in srgb, var(--danger-color) 12%, var(--card-bg));
        color: var(--danger-color);
        border: 1px solid color-mix(in srgb, var(--danger-color) 28%, var(--border-color));
      }

      .loading, .empty { color: var(--text-secondary); }

      .refresh-btn {
        padding: 0.5rem 1rem;
        border-radius: 0.5rem;
        border: 1px solid var(--border-color);
        background: var(--card-bg);
        color: var(--text-primary);
        cursor: pointer;
        transition: background 0.2s ease, border-color 0.2s ease;
      }

      .refresh-btn:hover:not(:disabled) {
        background: var(--hover-bg);
        border-color: color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
      }

      .refresh-btn:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }

      .trend-block { margin-top: 1rem; }

      .panel-header-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.75rem;
        margin-bottom: 0.5rem;
      }

      .panel-header-row h2 { margin: 0; }

      .export-btn {
        padding: 0.35rem 0.75rem;
        border-radius: 0.4rem;
        border: 1px solid var(--border-color);
        background: var(--card-bg);
        color: var(--text-primary);
        cursor: pointer;
        font-size: 0.85rem;
      }

      .aging-bars { display: flex; flex-direction: column; gap: 0.45rem; }
      .aging-row {
        display: grid;
        grid-template-columns: 4.5rem 1fr 2.5rem;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.9rem;
      }
      .aging-row .bar {
        height: 0.55rem;
        border-radius: 999px;
        background: color-mix(in srgb, var(--primary-color) 70%, var(--border-color));
        min-width: 0.25rem;
      }
      .aging-row .bar-warn {
        background: color-mix(in srgb, var(--danger-color) 75%, var(--border-color));
      }
      .muted { color: var(--text-secondary); font-size: 0.85rem; margin: 0.5rem 0 0; }
    `,
  ],
})
export class AnalyticsHubComponent implements OnInit {
  private readonly api = inject(AnalyticsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly aiPageContext = inject(AiPageContextService);
  private readonly destroyRef = inject(DestroyRef);

  readonly sections = SECTIONS;
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly activeSection = signal<AnalyticsSection>('overview');
  readonly overview = signal<AnalyticsOverview | null>(null);
  readonly executive = signal<ExecutiveAnalytics | null>(null);
  readonly operational = signal<OperationalAnalytics | null>(null);
  readonly rca = signal<RcaAnalytics | null>(null);
  readonly capa = signal<CapaAnalytics | null>(null);
  readonly sla = signal<SlaAnalytics | null>(null);
  readonly departments = signal<DepartmentAnalytics | null>(null);
  readonly trends = signal<AnalyticsTrends | null>(null);

  readonly frameworkName = computed(() => {
    const ctx =
      this.overview()?.context ??
      this.executive()?.context ??
      this.operational()?.context;
    return ctx?.frameworkName ?? null;
  });

  ngOnInit(): void {
    this.aiPageContext.setPageContext({
      pageContext: 'AnalyticsHub',
      label: 'Analytics Hub',
    });
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const section = (params.get('section') ?? 'overview') as AnalyticsSection;
      this.activeSection.set(SECTIONS.some((s) => s.id === section) ? section : 'overview');
      this.reload();
    });
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    const section = this.activeSection();

    const handleError = (err: unknown) => {
      this.error.set(err instanceof Error ? err.message : 'Failed to load analytics.');
      this.loading.set(false);
    };

    if (section === 'overview') {
      this.api.getOverview().subscribe({
        next: (data) => {
          this.overview.set(data);
          this.api.getTrends().subscribe({
            next: (trends) => {
              this.trends.set(trends);
              this.loading.set(false);
            },
            error: () => this.loading.set(false),
          });
        },
        error: handleError,
      });
      return;
    }

    const loaders: Record<Exclude<AnalyticsSection, 'overview'>, () => void> = {
      executive: () =>
        this.api.getExecutive().subscribe({ next: (d) => { this.executive.set(d); this.loading.set(false); }, error: handleError }),
      operational: () =>
        this.api.getOperational().subscribe({ next: (d) => { this.operational.set(d); this.loading.set(false); }, error: handleError }),
      rca: () =>
        this.api.getRca().subscribe({ next: (d) => { this.rca.set(d); this.loading.set(false); }, error: handleError }),
      capa: () =>
        this.api.getCapa().subscribe({ next: (d) => { this.capa.set(d); this.loading.set(false); }, error: handleError }),
      sla: () =>
        this.api.getSla().subscribe({ next: (d) => { this.sla.set(d); this.loading.set(false); }, error: handleError }),
      departments: () =>
        this.api.getDepartments().subscribe({ next: (d) => { this.departments.set(d); this.loading.set(false); }, error: handleError }),
    };

    loaders[section]();
  }

  agingBarWidth(value: number, aging: { within30: number; days31To60: number; days61To90: number; over90: number }): number {
    const max = Math.max(aging.within30, aging.days31To60, aging.days61To90, aging.over90, 1);
    return Math.round((value / max) * 100);
  }

  exportOperationalCsv(data: OperationalAnalytics): void {
    const aging = data.capaAging;
    const rows: string[][] = [
      ['Metric', 'Value'],
      ['Open Deficiencies', String(data.deficiencyFunnel.open)],
      ['Avg Resolution Hours', String(data.averageResolutionHours ?? 0)],
      ['Avg Resolution Days', String(data.averageResolutionDays ?? 0)],
      ['Audit Completion Rate %', String(data.auditCompletionRate ?? 0)],
      ['SLA Compliance Rate %', String(data.slaComplianceRate ?? 0)],
      ['SLA Health Score', String(data.slaHealthScore ?? 0)],
      ['Overdue CAPAs', String(data.overdueWork?.overdueCapas ?? 0)],
      ['Overdue Tasks', String(data.overdueWork?.overdueTasks ?? 0)],
      ['Overdue Deficiencies', String(data.overdueWork?.overdueDeficiencies ?? 0)],
      ['CAPA Aging ≤30', String(aging?.within30 ?? 0)],
      ['CAPA Aging 31-60', String(aging?.days31To60 ?? 0)],
      ['CAPA Aging 61-90', String(aging?.days61To90 ?? 0)],
      ['CAPA Aging >90', String(aging?.over90 ?? 0)],
    ];
    const csv = rows.map((r) => r.map((c) => `"${c.replace(/"/g, '""')}"`).join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `operational-analytics-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
