import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ComplianceContextService } from '../../core/services/compliance-context.service';
import { RoleExperienceHome, RoleExperienceService } from '../../core/services/role-experience.service';
import {
  Phase5AiApiService,
  type AceInsightDto,
  type ReadinessForecastDto,
} from '../aovias/services/phase5-ai-api.service';
import {
  KpiCardComponent,
  PriorityNbaStripComponent,
  RiskListComponent,
  type RiskListItem,
} from '../../shared/ui/analytics';

/**
 * Phase 3 Sprint 3 — Quality Manager operational home.
 * Composes NBA + attention KPIs + deep links to existing Tasks / DCC / Evidence / Audit surfaces.
 */
@Component({
  selector: 'app-qm-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    PriorityNbaStripComponent,
    KpiCardComponent,
    RiskListComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="qm-home">
      <header class="qm-home__header">
        <div>
          <h1>Quality Manager workspace</h1>
          <p>Today’s operational control — tasks, evidence, CAPAs, and audits.</p>
        </div>
        <div class="qm-home__header-actions">
          <a routerLink="/app/control-center" class="link-btn">Open control center</a>
          <a routerLink="/app/dashboard" [queryParams]="{ persona: 'quality-manager' }" class="link-btn link-btn--ghost">
            Dashboard widgets
          </a>
        </div>
      </header>

      <app-priority-nba-strip
        title="What to do first"
        [subtitle]="nbaSubtitle()"
        [items]="nbaItems()"
      />

      @if (aceInsights().length || readinessForecast()) {
        <section class="ace-panel" aria-label="ACE insights">
          <div class="ace-panel__head">
            <h2>ACE insights</h2>
            @if (readinessForecast(); as rf) {
              <span class="ace-forecast">
                Readiness {{ rf.currentReadinessPercent }}% → {{ rf.forecastReadinessPercent }}%
              </span>
            }
          </div>
          <div class="ace-chips">
            @for (insight of aceInsights(); track insight.id) {
              <button type="button" class="ace-chip" (click)="ackInsight(insight)">
                <strong>{{ insight.title }}</strong>
                <span>P{{ insight.priority }} · {{ insight.category }}</span>
              </button>
            } @empty {
              <span class="ace-empty">No open ACE insights.</span>
            }
          </div>
        </section>
      }

      <section class="kpi-row" aria-label="Operational attention">
        <app-kpi-card
          label="Overdue tasks"
          [value]="counts().overdueTasks"
          [tone]="counts().overdueTasks > 0 ? 'critical' : 'default'"
          hint="Needs assignment or chase"
        />
        <app-kpi-card
          label="Pending reviews"
          [value]="counts().pendingReviews"
          [tone]="counts().pendingReviews > 0 ? 'warning' : 'default'"
          hint="Evidence / CAPA reviews"
        />
        <app-kpi-card
          label="Critical CAPAs"
          [value]="counts().criticalCapas"
          [tone]="counts().criticalCapas > 0 ? 'critical' : 'default'"
          hint="Overdue corrective actions"
        />
        <app-kpi-card
          label="Upcoming audits"
          [value]="counts().upcomingAudits"
          hint="Open audit sessions"
        />
      </section>

      <section class="quick-actions" aria-label="Quality Manager quick actions">
        <h2>Quick actions</h2>
        <div class="quick-actions__grid">
          @for (action of quickActions(); track action.href + action.label) {
            <a class="quick-action" [routerLink]="actionHref(action.href)">{{ action.label }}</a>
          }
        </div>
      </section>

      <section class="ops-grid" aria-label="Operational queues">
        @for (card of opsCards; track card.title) {
          <a
            class="ops-card"
            [routerLink]="card.href"
            [queryParams]="card.queryParams ?? null"
          >
            <h3>{{ card.title }}</h3>
            <p>{{ card.detail }}</p>
            <span>{{ card.cta }}</span>
          </a>
        }
      </section>

      <app-risk-list title="Attention list" [items]="attentionRisks()" />
    </div>
  `,
  styles: [
    `
      .qm-home {
        padding: 24px;
        max-width: 1200px;
        margin: 0 auto;
        display: flex;
        flex-direction: column;
        gap: 20px;
      }
      .qm-home__header {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
        align-items: flex-start;
      }
      .qm-home__header h1 {
        margin: 0 0 4px;
        font-size: var(--font-size-page-title);
        font-weight: 700;
      }
      .qm-home__header p {
        margin: 0;
        color: var(--text-secondary, #64748b);
        font-size: 14px;
      }
      .qm-home__header-actions {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
      }
      .link-btn {
        display: inline-flex;
        align-items: center;
        padding: 8px 14px;
        border-radius: 8px;
        background: var(--primary-color, var(--color-primary));
        color: #fff;
        text-decoration: none;
        font-size: 13px;
        font-weight: 600;
      }
      .link-btn--ghost {
        background: transparent;
        color: var(--text-primary, #0f172a);
        border: 1px solid var(--border-color, #cbd5e1);
      }
      .kpi-row {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 12px;
      }
      .quick-actions h2,
      .ops-grid + app-risk-list {
        margin: 0;
      }
      .quick-actions h2 {
        font-size: var(--font-size-section-title);
        font-weight: 650;
        margin-bottom: 10px;
      }
      .quick-actions__grid {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .quick-action {
        padding: 8px 12px;
        border-radius: 999px;
        border: 1px solid var(--border-color, #cbd5e1);
        background: var(--card-bg, #fff);
        color: var(--text-primary, #0f172a);
        text-decoration: none;
        font-size: 13px;
        font-weight: 600;
      }
      .quick-action:hover {
        border-color: var(--primary-color, var(--color-primary));
        color: var(--primary-color, var(--color-primary));
      }
      .ops-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 12px;
      }
      .ops-card {
        display: flex;
        flex-direction: column;
        gap: 6px;
        padding: 16px;
        border-radius: 12px;
        border: 1px solid var(--border-color, #e2e8f0);
        background: var(--card-bg, #fff);
        text-decoration: none;
        color: inherit;
        min-height: 120px;
      }
      .ops-card:hover {
        border-color: var(--primary-color, var(--color-primary));
        box-shadow: var(--shadow-sm, 0 1px 2px rgb(0 0 0 / 6%));
      }
      .ops-card h3 {
        margin: 0;
        font-size: 15px;
        font-weight: 650;
      }
      .ops-card p {
        margin: 0;
        flex: 1;
        font-size: 13px;
        color: var(--text-secondary, #64748b);
        line-height: 1.45;
      }
      .ops-card span {
        font-size: 12px;
        font-weight: 650;
        color: var(--primary-color, var(--color-primary));
      }
      .ace-panel {
        padding: 14px 16px;
        border-radius: 12px;
        border: 1px solid var(--border-color, #e2e8f0);
        background: #f8fafc;
      }
      .ace-panel__head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 12px;
        margin-bottom: 10px;
      }
      .ace-panel h2 {
        margin: 0;
        font-size: var(--font-size-section-title);
        font-weight: 650;
      }
      .ace-forecast {
        font-size: 12px;
        color: #475569;
      }
      .ace-chips {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .ace-chip {
        text-align: left;
        padding: 8px 12px;
        border-radius: 999px;
        border: 1px solid #cbd5e1;
        background: #fff;
        cursor: pointer;
        max-width: 320px;
      }
      .ace-chip strong {
        display: block;
        font-size: 12px;
        font-weight: 650;
      }
      .ace-chip span {
        font-size: 11px;
        color: #64748b;
      }
      .ace-empty {
        font-size: 13px;
        color: #94a3b8;
      }
    `,
  ],
})
export class QmHomeComponent {
  private readonly roleExperience = inject(RoleExperienceService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly phase5Ai = inject(Phase5AiApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly home = signal<RoleExperienceHome | null>(null);
  readonly aceInsights = signal<AceInsightDto[]>([]);
  readonly readinessForecast = signal<ReadinessForecastDto | null>(null);

  readonly opsCards: Array<{
    title: string;
    detail: string;
    href: string;
    cta: string;
    queryParams?: Record<string, string>;
  }> = [
    {
      title: "Today's tasks",
      detail: 'Assign, reassign, and clear overdue work from the task engine.',
      href: '/app/tasks',
      cta: 'Open tasks →',
    },
    {
      title: 'Evidence waiting',
      detail: 'Review queue for pending verification and rejected resubmissions.',
      href: '/app/evidence/review-queue',
      cta: 'Review evidence →',
    },
    {
      title: 'CAPAs (DCC)',
      detail: 'Corrective actions, effectiveness reviews, and promote flows.',
      href: '/app/audit/capa',
      cta: 'Open DCC →',
    },
    {
      title: 'Upcoming audits',
      detail: 'Plan sessions and keep the audit calendar current.',
      href: '/app/audit/planning',
      cta: 'Launch audit →',
    },
    {
      title: 'Department heatmap',
      detail: 'See readiness by department on the quality-manager dashboard.',
      href: '/app/dashboard',
      queryParams: { persona: 'quality-manager' },
      cta: 'View heatmap →',
    },
    {
      title: 'Trends & analytics',
      detail: 'Operational KPIs, CAPA aging, and readiness trends.',
      href: '/app/analytics',
      cta: 'View trends →',
    },
  ];

  readonly counts = computed(
    () =>
      this.home()?.attentionCounts ?? {
        overdueTasks: 0,
        pendingReviews: 0,
        criticalCapas: 0,
        upcomingAudits: 0,
      },
  );

  readonly quickActions = computed(() => this.home()?.quickActions ?? []);

  readonly nbaItems = computed(() =>
    (this.home()?.priorities ?? []).map((p) => ({
      title: p.title,
      rationale: p.rationale,
      href: p.href,
      severity: p.severity,
    })),
  );

  readonly nbaSubtitle = computed(() => {
    const c = this.counts();
    return `${c.overdueTasks} overdue · ${c.pendingReviews} reviews · ${c.criticalCapas} CAPAs · ${c.upcomingAudits} audits`;
  });

  readonly attentionRisks = computed<RiskListItem[]>(() => {
    const c = this.counts();
    const items: RiskListItem[] = [];
    if (c.overdueTasks > 0) {
      items.push({
        label: `${c.overdueTasks} overdue tasks`,
        detail: 'Clear or reassign before SLA breach.',
        severity: 'high',
        href: '/app/tasks',
      });
    }
    if (c.pendingReviews > 0) {
      items.push({
        label: `${c.pendingReviews} pending reviews`,
        detail: 'Evidence or CAPA effectiveness waiting.',
        severity: 'medium',
        href: '/app/evidence/review-queue',
      });
    }
    if (c.criticalCapas > 0) {
      items.push({
        label: `${c.criticalCapas} critical CAPAs`,
        detail: 'Overdue corrective actions in DCC.',
        severity: 'critical',
        href: '/app/audit/capa',
      });
    }
    if (c.upcomingAudits > 0) {
      items.push({
        label: `${c.upcomingAudits} upcoming audits`,
        detail: 'Confirm plan and evidence readiness.',
        severity: 'medium',
        href: '/app/audit/planning',
      });
    }
    return items;
  });

  constructor() {
    this.loadHome();
    this.loadAce();
    this.complianceContext.frameworkId$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadHome();
      this.loadAce();
    });
  }

  /** Strip query string for routerLink; query handled via dashboard deep-link separately when needed. */
  actionHref(href: string): string {
    const q = href.indexOf('?');
    return q >= 0 ? href.slice(0, q) : href;
  }

  ackInsight(insight: AceInsightDto): void {
    this.phase5Ai.acknowledgeAceInsight(insight.id).subscribe({
      next: () => this.aceInsights.update((items) => items.filter((i) => i.id !== insight.id)),
      error: () => {},
    });
  }

  private loadHome(): void {
    const frameworkId = this.complianceContext.getFrameworkId();
    this.roleExperience
      .getHome(frameworkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((home) => this.home.set(home));
  }

  private loadAce(): void {
    this.phase5Ai
      .listAceInsights('Open', 8)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => this.aceInsights.set(Array.isArray(items) ? items : []),
        error: () => this.aceInsights.set([]),
      });
    this.phase5Ai
      .predictOverview('monthly')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (overview) => this.readinessForecast.set(overview.readiness ?? null),
        error: () => this.readinessForecast.set(null),
      });
  }
}
