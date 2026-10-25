import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { MembershipScopeService } from '../../../core/services/membership-scope.service';
import {
  RoleExperienceHome,
  RoleExperienceService,
} from '../../../core/services/role-experience.service';
import { TaskApiService } from '../../tasks/task-api.service';
import { EvidenceApiService } from '../../evidence/services/evidence-api.service';
import { AnalyticsApiService } from '../../analytics/services/analytics-api.service';
import { HospitalDashboardService } from '../../dashboard/services/hospital-dashboard.service';
import {
  KpiCardComponent,
  PriorityNbaStripComponent,
  RiskListComponent,
  type PriorityNbaItem,
  type RiskListItem,
} from '../../../shared/ui/analytics';
import type { TaskListItem } from '../../tasks/models';

@Component({
  selector: 'app-department-head-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    KpiCardComponent,
    PriorityNbaStripComponent,
    RiskListComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="dh-home">
      <header class="dh-home__header">
        <div>
          <h1>Department Head Workspace</h1>
          <p class="dh-home__subtitle">
            @if (departmentName()) {
              Scoped to <strong>{{ departmentName() }}</strong>
            } @else {
              Your department compliance queue
            }
          </p>
        </div>
        <div class="dh-home__actions">
          @for (a of quickActions(); track a.label) {
            <a class="btn" [class.btn--primary]="$first" [routerLink]="routePath(a.href)" [queryParams]="routeQuery(a.href)">
              {{ a.label }}
            </a>
          }
        </div>
      </header>

      <app-priority-nba-strip
        title="Next best actions"
        subtitle="Priorities for your department"
        [items]="nbaItems()"
      />

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      <div class="kpi-row">
        <app-kpi-card label="Assigned tasks" [value]="assignedCount()" />
        <app-kpi-card label="Pending evidence" [value]="pendingEvidence()" tone="warning" />
        <app-kpi-card label="Rejected evidence" [value]="rejectedEvidence()" tone="critical" />
        <app-kpi-card label="Open CAPAs" [value]="openCapas()" tone="warning" />
        <app-kpi-card label="Overdue CAPAs" [value]="overdueCapas()" tone="critical" />
        <app-kpi-card label="Dept readiness" [value]="readinessLabel()" />
      </div>

      <div class="panels">
        <section class="panel">
          <header class="panel__head">
            <h2>Assigned tasks</h2>
            <a routerLink="/app/tasks" [queryParams]="taskQuery()">View all</a>
          </header>
          @if (loading()) {
            <p class="muted">Loading…</p>
          } @else if (!tasks().length) {
            <p class="muted">No open tasks for this department.</p>
          } @else {
            <ul class="list">
              @for (t of tasks(); track t.id) {
                <li>
                  <a [routerLink]="['/app/tasks']" [queryParams]="{ focusTaskId: t.id }">{{ t.title || 'Untitled task' }}</a>
                  <span class="muted">{{ t.status }} · {{ t.dueDate ? (t.dueDate | date: 'mediumDate') : 'No due date' }}</span>
                </li>
              }
            </ul>
          }
        </section>

        <section class="panel">
          <header class="panel__head">
            <h2>Attention</h2>
            <a routerLink="/app/audit/capa" [queryParams]="capaQuery()">CAPA queue</a>
          </header>
          <app-risk-list [items]="attentionItems()" />
        </section>
      </div>
    </div>
  `,
  styles: [
    `
      .dh-home { padding: 8px 4px 24px; }
      .dh-home__header {
        display: flex; justify-content: space-between; gap: 16px; flex-wrap: wrap;
        margin-bottom: 16px; align-items: flex-start;
      }
      .dh-home__header h1 { margin: 0; font-size: var(--font-size-page-title); font-weight: 700; }
      .dh-home__subtitle { margin: 4px 0 0; color: var(--text-secondary, #6b7280); font-size: var(--font-size-subtitle); }
      .dh-home__actions { display: flex; gap: 8px; flex-wrap: wrap; }
      .btn {
        display: inline-flex; align-items: center; justify-content: center;
        block-size: var(--control-height-md);
        padding-inline: var(--control-padding-inline);
        border-radius: var(--radius-control); text-decoration: none;
        font-size: var(--font-size-body-sm); font-weight: var(--font-weight-semibold);
        border: var(--border-width-hairline) solid var(--border-color); color: inherit;
        transition: var(--transition-control);
      }
      .btn:hover { background: var(--state-hover-bg); }
      .btn--primary {
        background: var(--primary-color); color: var(--color-text-inverse);
        border-color: var(--primary-color);
      }
      .btn--primary:hover { background: var(--color-primary-hover); border-color: var(--color-primary-hover); }
      .kpi-row {
        display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
        gap: 10px; margin: 16px 0 20px;
      }
      .panels { display: grid; grid-template-columns: 1.2fr 1fr; gap: 16px; }
      @media (max-width: 900px) { .panels { grid-template-columns: 1fr; } }
      .panel {
        background: var(--card-bg, #fff); border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 10px; padding: 14px 16px;
      }
      .panel__head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
      .panel__head h2 { margin: 0; font-size: var(--font-size-section-title); }
      .panel__head a { font-size: 12px; }
      .list { list-style: none; margin: 0; padding: 0; }
      .list li {
        display: flex; flex-direction: column; gap: 2px;
        padding: 8px 0; border-bottom: 1px solid var(--border-color, #f3f4f6);
      }
      .list a { font-weight: 600; text-decoration: none; color: inherit; }
      .muted { color: var(--text-secondary, #6b7280); font-size: 12px; }
      .banner.error {
        background: #fef2f2; color: #b91c1c; padding: 10px 12px; border-radius: 8px; margin-bottom: 12px;
      }
    `,
  ],
})
export class DepartmentHeadHomeComponent implements OnInit {
  private readonly roleExperience = inject(RoleExperienceService);
  private readonly membership = inject(MembershipScopeService);
  private readonly context = inject(ComplianceContextService);
  private readonly tasksApi = inject(TaskApiService);
  private readonly evidenceApi = inject(EvidenceApiService);
  private readonly analyticsApi = inject(AnalyticsApiService);
  private readonly hospitalDashboard = inject(HospitalDashboardService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly departmentName = signal<string | null>(null);
  readonly departmentId = signal<string | null>(null);
  readonly home = signal<RoleExperienceHome | null>(null);
  readonly tasks = signal<TaskListItem[]>([]);
  readonly assignedCount = signal(0);
  readonly pendingEvidence = signal(0);
  readonly rejectedEvidence = signal(0);
  readonly openCapas = signal(0);
  readonly overdueCapas = signal(0);
  readonly readiness = signal<number | null>(null);
  readonly nbaItems = signal<PriorityNbaItem[]>([]);
  readonly attentionItems = signal<RiskListItem[]>([]);

  readonly quickActions = signal<{ label: string; href: string }[]>([
    { label: 'Upload evidence', href: '/app/evidence' },
    { label: 'Complete tasks', href: '/app/tasks?quick=myTasks' },
    { label: 'Respond to CAPA', href: '/app/audit/capa' },
  ]);

  ngOnInit(): void {
    this.roleExperience.getHome(this.context.getFrameworkId()).subscribe((h) => {
      this.home.set(h);
      if (h.quickActions?.length) this.quickActions.set(h.quickActions);
      this.nbaItems.set(
        (h.priorities ?? []).map((p) => ({
          title: p.title,
          rationale: p.rationale,
          href: p.href,
          severity: p.severity,
        })),
      );
    });

    this.membership
      .resolveDepartmentScope$()
      .pipe(catchError(() => of({ departmentId: null, departmentName: null, isDepartmentHead: true })))
      .subscribe((scope) => {
        this.departmentId.set(scope.departmentId);
        this.departmentName.set(scope.departmentName);
        this.loadWorkspace(scope.departmentId, scope.departmentName);
      });
  }

  readinessLabel(): string {
    const v = this.readiness();
    return v == null ? '—' : `${Math.round(v)}%`;
  }

  taskQuery(): Record<string, string> {
    const q: Record<string, string> = { quick: 'myTasks' };
    const d = this.departmentId();
    if (d) q['departmentId'] = d;
    return q;
  }

  capaQuery(): Record<string, string> {
    const name = this.departmentName();
    return name ? { search: name } : {};
  }

  routePath(href: string): string {
    return href.split('?')[0] || href;
  }

  routeQuery(href: string): Record<string, string> | undefined {
    const q = href.includes('?') ? href.split('?')[1] : '';
    if (!q) return undefined;
    const out: Record<string, string> = {};
    for (const part of q.split('&')) {
      const [k, v] = part.split('=');
      if (k) out[decodeURIComponent(k)] = decodeURIComponent(v ?? '');
    }
    const d = this.departmentId();
    if (d && this.routePath(href).includes('/tasks') && !out['departmentId']) {
      out['departmentId'] = d;
    }
    return out;
  }

  private loadWorkspace(departmentId: string | null, departmentName: string | null): void {
    const frameworkId = this.context.getFrameworkId();
    const hospitalId = this.context.getHospitalId();
    if (!frameworkId || !hospitalId) {
      this.error.set('Select a hospital and framework to load the department workspace.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const taskFilters = {
      departmentId: departmentId || undefined,
      quick: 'myTasks' as const,
    };

    forkJoin({
      tasks: this.tasksApi.getTasksPaged(frameworkId, taskFilters, 1, 8, 'table').pipe(
        catchError(() => of({ items: [], totalCount: 0 })),
      ),
      evidencePending: this.evidenceApi
        .getEvidenceList({
          frameworkId,
          departmentId: departmentId || undefined,
          status: 'pendingReview',
          page: 1,
          pageSize: 1,
        })
        .pipe(catchError(() => of({ items: [], totalCount: 0 }))),
      evidenceRejected: this.evidenceApi
        .getEvidenceList({
          frameworkId,
          departmentId: departmentId || undefined,
          status: 'rejected',
          page: 1,
          pageSize: 1,
        })
        .pipe(catchError(() => of({ items: [], totalCount: 0 }))),
      capa: this.analyticsApi.getCapa(frameworkId).pipe(catchError(() => of(null))),
      operational: this.analyticsApi.getOperational(frameworkId).pipe(catchError(() => of(null))),
      departments: this.analyticsApi.getDepartments(frameworkId).pipe(catchError(() => of(null))),
      hospitalDash: this.hospitalDashboard.getHospitalDashboard(hospitalId).pipe(catchError(() => of(null))),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (bundle) => {
          const taskItems = (bundle.tasks as { items?: TaskListItem[]; totalCount?: number }).items ?? [];
          const taskTotal = (bundle.tasks as { totalCount?: number }).totalCount ?? taskItems.length;
          this.tasks.set(taskItems);
          this.assignedCount.set(taskTotal);

          this.pendingEvidence.set(
            (bundle.evidencePending as { totalCount?: number }).totalCount ?? 0,
          );
          this.rejectedEvidence.set(
            (bundle.evidenceRejected as { totalCount?: number }).totalCount ?? 0,
          );

          const deptNeedle = (departmentName ?? '').toLowerCase();
          const capaRows = bundle.capa?.departmentCapaPerformance ?? [];
          const capaMatch = deptNeedle
            ? capaRows.find((r) => r.label.toLowerCase().includes(deptNeedle))
            : null;
          this.openCapas.set(capaMatch?.count ?? bundle.capa?.pendingReviews ?? 0);

          const overdue =
            bundle.operational?.overdueWork?.departmentOverdueCapas?.find((r) =>
              deptNeedle ? r.label.toLowerCase().includes(deptNeedle) : false,
            )?.count ??
            bundle.operational?.overdueWork?.overdueCapas ??
            0;
          this.overdueCapas.set(overdue);

          const analyticsDept = bundle.departments?.departments?.find((d) =>
            deptNeedle ? d.department.toLowerCase().includes(deptNeedle) : false,
          );
          const heatmap = bundle.hospitalDash?.departmentBreakdown?.find((d) =>
            deptNeedle ? d.department.toLowerCase().includes(deptNeedle) : false,
          );
          this.readiness.set(
            analyticsDept?.readinessScore ?? heatmap?.complianceScore ?? null,
          );

          const risks: RiskListItem[] = [];
          if (this.pendingEvidence() > 0) {
            risks.push({
              label: `${this.pendingEvidence()} evidence pending review`,
              href: '/app/evidence',
              severity: 'medium',
            });
          }
          if (this.rejectedEvidence() > 0) {
            risks.push({
              label: `${this.rejectedEvidence()} rejected evidence to fix`,
              href: '/app/evidence',
              severity: 'high',
            });
          }
          if (this.overdueCapas() > 0) {
            risks.push({
              label: `${this.overdueCapas()} overdue CAPAs`,
              href: '/app/audit/capa',
              severity: 'high',
            });
          }
          if (!risks.length) {
            risks.push({ label: 'No critical department blockers right now', severity: 'low' });
          }
          this.attentionItems.set(risks);
        },
        error: () => this.error.set('Unable to load department head workspace.'),
      });
  }
}
