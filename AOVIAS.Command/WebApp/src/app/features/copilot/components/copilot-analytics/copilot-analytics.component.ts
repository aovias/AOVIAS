import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, EMPTY } from 'rxjs';
import { CopilotApiService } from '../../services/copilot-api.service';
import type {
  CopilotGovernanceDashboard,
  RecommendationEffectivenessMetrics,
  DraftEffectivenessMetrics,
  ProposalConversionMetrics,
  PersonaAdoptionMetrics,
  CopilotTrends,
  RecommendationImpactSummary,
  CopilotAnalyticsTab,
  CategoryMetric,
} from '../../models/copilot-analytics.model';

@Component({
  selector: 'app-copilot-analytics',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="copilot-analytics">
      <header class="analytics-header">
        <div>
          <h2>Copilot Observability</h2>
          <p class="analytics-subtitle">Effectiveness, adoption, and ROI across recommendations and drafts.</p>
        </div>
        <button type="button" class="refresh-btn" (click)="reload()" [disabled]="loading()">Refresh</button>
      </header>

      @if (error()) {
        <p class="error-banner">{{ error() }}</p>
      }

      <nav class="tab-nav" aria-label="Copilot analytics sections">
        @for (tab of tabs; track tab.id) {
          <button
            type="button"
            class="tab-btn"
            [class.active]="activeTab() === tab.id"
            (click)="activeTab.set(tab.id)"
          >
            {{ tab.label }}
          </button>
        }
      </nav>

      @if (loading()) {
        <p class="loading">Loading analytics…</p>
      } @else {
        @switch (activeTab()) {
          @case ('overview') {
            @if (overview(); as o) {
              <section class="panel-grid">
                <article class="panel">
                  <h3>Active Users (30d)</h3>
                  <p class="big-number">{{ o.usage.activeUsers }}</p>
                </article>
                <article class="panel">
                  <h3>Workspace Views</h3>
                  <p class="big-number">{{ o.usage.workspaceViews }}</p>
                </article>
                <article class="panel">
                  <h3>Recommendation Acceptance</h3>
                  <p class="big-number">{{ o.trust.recommendationAcceptanceRate | number: '1.0-1' }}%</p>
                </article>
                <article class="panel">
                  <h3>Proposal Approval</h3>
                  <p class="big-number">{{ o.trust.proposalApprovalRate | number: '1.0-1' }}%</p>
                </article>
              </section>
              <section class="panel">
                <h3>Trust & Effectiveness</h3>
                <table>
                  <tbody>
                    <tr><td>Draft → Proposal rate</td><td>{{ o.trust.draftToProposalRate | number: '1.0-1' }}%</td></tr>
                    <tr><td>Ignored recommendations</td><td>{{ o.trust.ignoredRecommendations }}</td></tr>
                    <tr><td>Monthly activity events</td><td>{{ o.activity.monthlyEvents }}</td></tr>
                    <tr><td>Persona manual overrides</td><td>{{ o.personas.manualOverrides }}</td></tr>
                  </tbody>
                </table>
              </section>
            }
          }
          @case ('recommendations') {
            @if (recommendations(); as r) {
              <section class="panel-grid">
                <article class="panel"><h3>Generated</h3><p class="big-number">{{ r.totalGenerated }}</p></article>
                <article class="panel"><h3>Accepted</h3><p class="big-number">{{ r.accepted }}</p></article>
                <article class="panel"><h3>Acceptance Rate</h3><p class="big-number">{{ r.acceptanceRate }}%</p></article>
                <article class="panel"><h3>Execution Rate</h3><p class="big-number">{{ r.executionRate }}%</p></article>
              </section>
              <ng-container *ngTemplateOutlet="metricTable; context: { $implicit: r.byCategory, title: 'By Category' }" />
              <ng-container *ngTemplateOutlet="metricTable; context: { $implicit: r.byPersona, title: 'By Persona' }" />
            }
          }
          @case ('drafts') {
            @if (drafts(); as d) {
              <section class="panel-grid">
                <article class="panel"><h3>Total Drafts</h3><p class="big-number">{{ d.totalDrafts }}</p></article>
                <article class="panel"><h3>Utilization</h3><p class="big-number">{{ d.utilizationRate }}%</p></article>
                <article class="panel"><h3>Regeneration Rate</h3><p class="big-number">{{ d.regenerationRate }}%</p></article>
                <article class="panel"><h3>Proposal Conversion</h3><p class="big-number">{{ d.proposalConversionRate }}%</p></article>
              </section>
              <ng-container *ngTemplateOutlet="metricTable; context: { $implicit: d.byDraftType, title: 'By Draft Type' }" />
            }
          }
          @case ('proposals') {
            @if (proposals(); as p) {
              <section class="panel-grid">
                <article class="panel"><h3>Proposed</h3><p class="big-number">{{ p.proposed }}</p></article>
                <article class="panel"><h3>Approved</h3><p class="big-number">{{ p.approved }}</p></article>
                <article class="panel"><h3>Executed</h3><p class="big-number">{{ p.executed }}</p></article>
                <article class="panel"><h3>Approval Rate</h3><p class="big-number">{{ p.approvalRate }}%</p></article>
              </section>
              <section class="panel">
                <h3>Funnel Timing</h3>
                <table>
                  <tbody>
                    <tr><td>Avg approval time</td><td>{{ p.averageApprovalTimeHours ?? '—' }} hrs</td></tr>
                    <tr><td>Avg execution time</td><td>{{ p.averageExecutionTimeHours ?? '—' }} hrs</td></tr>
                    <tr><td>Rejection rate</td><td>{{ p.rejectionRate }}%</td></tr>
                  </tbody>
                </table>
              </section>
              <ng-container *ngTemplateOutlet="metricTable; context: { $implicit: p.byProposalType, title: 'By Proposal Type' }" />
            }
          }
          @case ('personas') {
            @if (personas(); as per) {
              <section class="panel">
                <p class="meta">
                  Most used: <strong>{{ per.mostUsedPersona ?? '—' }}</strong> ·
                  Highest conversion: <strong>{{ per.highestConversionPersona ?? '—' }}</strong> ·
                  Manual overrides: <strong>{{ per.manualOverrides }}</strong>
                </p>
                <table>
                  <thead>
                    <tr>
                      <th>Persona</th><th>Events</th><th>Recs Viewed</th><th>Drafts</th><th>Approved</th><th>Conversion</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (row of per.personas; track row.persona) {
                      <tr>
                        <td>{{ row.persona }}</td>
                        <td>{{ row.sessionEvents }}</td>
                        <td>{{ row.recommendationsViewed }}</td>
                        <td>{{ row.draftsGenerated }}</td>
                        <td>{{ row.proposalsApproved }}</td>
                        <td>{{ row.conversionRate }}%</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </section>
            }
          }
          @case ('trends') {
            @if (trends(); as t) {
              <section class="panel">
                <h3>Daily Activity (30d)</h3>
                @if (t.activityTrend.length) {
                  <table>
                    <thead><tr><th>Date</th><th>Events</th></tr></thead>
                    <tbody>
                      @for (point of t.activityTrend; track point.date) {
                        <tr><td>{{ point.date }}</td><td>{{ point.count }}</td></tr>
                      }
                    </tbody>
                  </table>
                } @else {
                  <p class="empty">No trend data yet.</p>
                }
              </section>
              <section class="panel">
                <h3>Effectiveness Snapshots</h3>
                @if (t.snapshots.length) {
                  <table>
                    <thead>
                      <tr>
                        <th>Date</th><th>Recs</th><th>Accepted</th><th>Drafts</th><th>Executed</th><th>Users</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (s of t.snapshots; track s.snapshotDate) {
                        <tr>
                          <td>{{ s.snapshotDate }}</td>
                          <td>{{ s.recommendationsGenerated }}</td>
                          <td>{{ s.recommendationsAccepted }}</td>
                          <td>{{ s.draftsGenerated }}</td>
                          <td>{{ s.proposalsExecuted }}</td>
                          <td>{{ s.activeUsers }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                } @else {
                  <p class="empty">Snapshots will appear after daily capture.</p>
                }
              </section>
              @if (impact(); as imp) {
                <section class="panel">
                  <h3>Recommendation Impact (ROI)</h3>
                  <p class="meta">
                    {{ imp.recommendationsWithExecution }} executed of {{ imp.recommendationsWithProposal }} with proposals
                    ({{ imp.executionConversionRate }}% conversion)
                  </p>
                  <table>
                    <thead><tr><th>Title</th><th>Category</th><th>Executed</th><th>Outcome</th></tr></thead>
                    <tbody>
                      @for (row of imp.topImpacts; track row.recommendationId) {
                        <tr>
                          <td>{{ row.title }}</td>
                          <td>{{ row.category }}</td>
                          <td>{{ row.proposalExecuted ? 'Yes' : 'No' }}</td>
                          <td>{{ row.actualOutcome ?? row.estimatedImpact ?? '—' }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </section>
              }
            }
          }
        }
      }

      <ng-template #metricTable let-items let-title="title">
        <section class="panel">
          <h3>{{ title }}</h3>
          @if (items?.length) {
            <table>
              <thead><tr><th>Label</th><th>Count</th><th>Rate</th></tr></thead>
              <tbody>
                @for (item of items; track item.label) {
                  <tr>
                    <td>{{ item.label }}</td>
                    <td>{{ item.count }}</td>
                    <td>{{ item.rate }}%</td>
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <p class="empty">No data.</p>
          }
        </section>
      </ng-template>
    </div>
  `,
  styles: [
    `
      .copilot-analytics {
        display: flex;
        flex-direction: column;
        gap: 16px;
        color: var(--text-primary);
      }
      .analytics-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 16px;
        flex-wrap: wrap;
      }
      h2 {
        margin: 0;
        font-size: var(--font-size-section-title);
        font-weight: 700;
        color: var(--text-primary);
      }
      .analytics-subtitle {
        margin: 4px 0 0;
        font-size: var(--font-size-body-sm);
        color: var(--text-secondary);
      }
      .refresh-btn {
        padding: 8px 14px;
        border-radius: 8px;
        border: 1px solid var(--border-color);
        background: var(--card-bg);
        color: var(--text-primary);
        cursor: pointer;
        font-size: 13px;
        font-weight: 600;
        box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
      }
      .refresh-btn:hover:not(:disabled) {
        background: var(--hover-bg);
        border-color: color-mix(in srgb, var(--color-primary) 35%, var(--border-color));
      }
      .refresh-btn:disabled { opacity: 0.5; cursor: default; }
      .tab-nav {
        display: flex;
        gap: 4px;
        flex-wrap: wrap;
        background: var(--hover-bg);
        border: 1px solid var(--border-color);
        border-radius: 10px;
        padding: 4px;
      }
      .tab-btn {
        padding: 8px 12px;
        border: none;
        background: transparent;
        color: var(--text-secondary);
        font-weight: 600;
        font-size: 13px;
        cursor: pointer;
        border-radius: 8px;
        transition: background 0.15s, color 0.15s;
      }
      .tab-btn.active {
        color: var(--text-primary);
        background: var(--card-bg);
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
        border: 1px solid var(--border-color);
      }
      .panel-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 12px;
      }
      .panel {
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        background: var(--card-bg);
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
      }
      .panel h3 {
        margin: 0 0 8px;
        font-size: 11px;
        font-weight: 700;
        color: var(--text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }
      .big-number { font-size: 28px; font-weight: 700; margin: 0; color: var(--text-primary); }
      table { width: 100%; border-collapse: collapse; font-size: 13px; }
      th, td {
        padding: 8px;
        border-bottom: 1px solid var(--border-color);
        text-align: left;
        color: var(--text-primary);
      }
      th { color: var(--text-secondary); font-weight: 600; font-size: 12px; }
      .meta { font-size: 13px; color: var(--text-secondary); }
      .empty, .loading { color: var(--text-secondary); font-size: 14px; }
      .error-banner {
        color: #b91c1c;
        font-size: 14px;
        padding: 10px 12px;
        background: #fef2f2;
        border: 1px solid #fecaca;
        border-radius: 8px;
      }
    `,
  ],
})
export class CopilotAnalyticsComponent implements OnInit {
  private readonly api = inject(CopilotApiService);

  readonly tabs: { id: CopilotAnalyticsTab; label: string }[] = [
    { id: 'overview', label: 'Overview' },
    { id: 'recommendations', label: 'Recommendations' },
    { id: 'drafts', label: 'Drafts' },
    { id: 'proposals', label: 'Proposals' },
    { id: 'personas', label: 'Personas' },
    { id: 'trends', label: 'Trends' },
  ];

  readonly activeTab = signal<CopilotAnalyticsTab>('overview');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly overview = signal<CopilotGovernanceDashboard | null>(null);
  readonly recommendations = signal<RecommendationEffectivenessMetrics | null>(null);
  readonly drafts = signal<DraftEffectivenessMetrics | null>(null);
  readonly proposals = signal<ProposalConversionMetrics | null>(null);
  readonly personas = signal<PersonaAdoptionMetrics | null>(null);
  readonly trends = signal<CopilotTrends | null>(null);
  readonly impact = signal<RecommendationImpactSummary | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getAnalyticsOverview().pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to load analytics');
        this.loading.set(false);
        return EMPTY;
      }),
    ).subscribe(o => {
      this.overview.set(o);
      this.recommendations.set(o.recommendations);
      this.drafts.set(o.drafts);
      this.proposals.set(o.proposals);
      this.personas.set(o.personas);
      this.loading.set(false);
    });

    this.api.getAnalyticsTrends(30).pipe(catchError(() => EMPTY)).subscribe(t => this.trends.set(t));
    this.api.getAnalyticsImpact(15).pipe(catchError(() => EMPTY)).subscribe(i => this.impact.set(i));
  }
}
