import { Component, ChangeDetectionStrategy, signal, computed, inject, DestroyRef, afterNextRender } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { combineLatest, debounceTime, distinctUntilChanged, interval, skip } from 'rxjs';
import { filter } from 'rxjs';
import { ExecutiveSummaryComponent } from '../components/executive-summary.component';
import { GaugeChartComponent } from '../components/gauge-chart.component';
import { RiskDistributionComponent } from '../components/risk-distribution.component';
import { HighRiskChaptersComponent } from '../components/high-risk-chapters.component';
import { ChapterBreakdownComponent } from '../components/chapter-breakdown.component';
import { ScoreTrendChartComponent } from '../components/score-trend-chart.component';
import { HospitalOverviewComponent } from '../components/hospital-overview.component';
import { TaskTypeBreakdownComponent } from '../components/task-type-breakdown.component';
import { MilestonesComponent } from '../components/milestones.component';
import { ActivityStreamComponent } from '../components/activity-stream.component';
import { ComplianceHeatmapComponent, HeatmapItem } from '../components/compliance-heatmap.component';
import { DashboardWidgetHostComponent } from '../components/dashboard-widget-host.component';
import { DashboardWidgetSkeletonComponent } from '../components/dashboard-widget-skeleton.component';
import { DashboardExperienceFacade } from '../facades/dashboard-experience.facade';
import { HospitalDashboardService } from '../services/hospital-dashboard.service';
import { DepartmentComplianceDto } from '../models/hospital-dashboard.model';
import { resolveDashboardCompletionRateRatio } from '../utils/dashboard-readiness.util';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { UserContextService } from '../../../core/services/user-context.service';
import { RoleExperienceHome, RoleExperienceService } from '../../../core/services/role-experience.service';
import { normalizeRoleId } from '../../../core/auth/roles/role-definitions';
import { sortByCpFrameworkCodeKeys } from '../../../shared/utils/framework-code-sort';
import { PerformanceTimelineService } from '../../../core/performance/performance-timeline.service';
import {
  KpiCardComponent,
  PriorityNbaStripComponent,
  RiskListComponent,
  type RiskListItem,
} from '../../../shared/ui/analytics';

export type DashboardPersona = 'quality-manager' | 'department-head' | 'hospital-admin' | 'consultant';

const PERSONA_WIDGETS: Record<DashboardPersona, ReadonlySet<string>> = {
  'quality-manager': new Set([
    'home-executive-kpi-strip',
    'home-alert-banner',
    'home-compliance-gauge',
    'home-department-heatmap',
    'home-risk-distribution',
    'home-milestones',
    'home-framework-overview',
    'home-task-type-breakdown',
  ]),
  'department-head': new Set([
    'home-alert-banner',
    'home-department-heatmap',
    'home-risk-distribution',
    'home-milestones',
  ]),
  'hospital-admin': new Set([
    'home-executive-kpi-strip',
    'home-alert-banner',
    'home-compliance-gauge',
    'home-hospital-overview',
    'home-framework-overview',
    'home-department-heatmap',
    'home-risk-distribution',
    'home-milestones',
    'home-score-trend',
  ]),
  consultant: new Set([
    'home-executive-kpi-strip',
    'home-alert-banner',
    'home-compliance-gauge',
    'home-framework-overview',
    'home-milestones',
    'home-risk-distribution',
  ]),
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  providers: [DashboardExperienceFacade],
  imports: [
    CommonModule,
    RouterLink,
    ExecutiveSummaryComponent,
    GaugeChartComponent,
    RiskDistributionComponent,
    HighRiskChaptersComponent,
    ChapterBreakdownComponent,
    ScoreTrendChartComponent,
    HospitalOverviewComponent,
    TaskTypeBreakdownComponent,
    MilestonesComponent,
    ActivityStreamComponent,
    ComplianceHeatmapComponent,
    DashboardWidgetHostComponent,
    DashboardWidgetSkeletonComponent,
    PriorityNbaStripComponent,
    KpiCardComponent,
    RiskListComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="dashboard-container">
      <!-- Phase 0: Shell always visible -->
      <div class="dashboard-header">
        <div>
          <h1 class="dashboard-title">Compliance Dashboard</h1>
          <p class="dashboard-subtitle">{{ dashboardSubtitle() }}</p>
        </div>
        <div class="header-actions">
          @if (lastUpdated()) {
            <div class="last-updated-inline">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <circle cx="12" cy="12" r="10" stroke-width="2" />
                <path d="M12 6v6l4 2" stroke-width="2" stroke-linecap="round" />
              </svg>
              Last updated: {{ lastUpdated() }}
            </div>
          }
          <button class="refresh-button" [class.loading]="facade.isRefreshing()" (click)="refreshDashboard()">
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              [class.spinning]="facade.isRefreshing()"
            >
              <path d="M1 4v6h6M23 20v-6h-6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
              <path d="M20.49 9A9 9 0 005.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 013.51 15" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            {{ facade.isRefreshing() ? 'Refreshing...' : 'Refresh Dashboard' }}
          </button>
        </div>
      </div>

      <div class="persona-tabs" role="tablist" aria-label="Role dashboard view">
        @for (p of personas; track p.id) {
          <button
            type="button"
            role="tab"
            class="persona-tab"
            [class.active]="persona() === p.id"
            [attr.aria-selected]="persona() === p.id"
            (click)="setPersona(p.id)"
          >
            {{ p.label }}
          </button>
        }
      </div>

      <app-priority-nba-strip
        title="Priority next best actions"
        [subtitle]="nbaSubtitle()"
        [items]="nbaItems()"
        [loading]="roleHomeLoading()"
        [error]="roleHomeError()"
        emptyMessage="No priority actions for this role right now. Check My work for tasks and reviews."
      />

      @if (roleQuickActions().length) {
        <section class="role-quick-actions" aria-label="Role quick actions">
          <h2 class="role-quick-actions__title">Quick actions</h2>
          <div class="role-quick-actions__grid">
            @for (action of roleQuickActions(); track action.href + action.label) {
              <a class="role-quick-action" [routerLink]="quickActionPath(action.href)">{{ action.label }}</a>
            }
          </div>
        </section>
      }

      @if (persona() === 'hospital-admin' && ownerAttentionKpis().length) {
        <section class="owner-attention" aria-label="Executive attention">
          <div class="owner-attention__kpis">
            @for (kpi of ownerAttentionKpis(); track kpi.label) {
              <app-kpi-card
                [label]="kpi.label"
                [value]="kpi.value"
                [hint]="kpi.hint"
                [tone]="kpi.tone"
              />
            }
          </div>
          <app-risk-list title="Top risks & critical CAPAs" [items]="ownerRiskItems()" />
        </section>
      }

      @if (facade.snapshot(); as snap) {
        @if (snap.isSnapshotAvailable === false) {
          <div class="snapshot-calculating-banner" role="status">
            <span class="snapshot-calculating-dot"></span>
            Calculating compliance score…
          </div>
        }
      }

      <div class="dashboard-content">
        <!-- Phase 1: Summary KPI strip -->
        <app-dashboard-widget-host
          [descriptor]="facade.widgetById('home-executive-kpi-strip') ?? null"
          [visible]="showWidget('home-executive-kpi-strip')"
          [loading]="facade.experienceLoading() && !facade.experience()"
          [error]="summaryWidgetError()"
          [dataReady]="summaryCards().length > 0"
          (retry)="onWidgetRetry($event)"
        >
          @if (summaryCards().length) {
            <app-executive-summary [cards]="summaryCards()" />
          }
        </app-dashboard-widget-host>

        @if (facade.experienceLoading() && !facade.experience() && !summaryCards().length) {
          <div class="summary-skeleton-row" aria-hidden="true">
            @for (i of [1, 2, 3, 4]; track i) {
              <app-dashboard-widget-skeleton [minHeight]="120" ariaLabel="Loading summary card" />
            }
          </div>
        }

        <!-- Alert banner -->
        <app-dashboard-widget-host
          [descriptor]="facade.widgetById('home-alert-banner') ?? null"
          [visible]="showWidget('home-alert-banner')"
          [loading]="facade.snapshotLoading() && !facade.snapshot()"
          [error]="snapshotWidgetError()"
          [dataReady]="!!facade.snapshot() || !!facade.experience()"
          (retry)="onWidgetRetry($event)"
        >
          @if (hasCriticalIssues()) {
            <div class="alert-banner">
              <div class="alert-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
                  <path d="M12 9v4m0 4h.01" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </div>
              <div class="alert-content">
                <div class="alert-title">{{ alertTitle() }}</div>
                <div class="alert-message">{{ alertMessage() }}</div>
              </div>
              <div class="alert-count">
                <span class="count-badge">{{ unacknowledgedAlertCount() }}</span>
                <span class="count-label">{{ unacknowledgedAlertCount() === 1 ? 'alert' : 'alerts' }}</span>
              </div>
            </div>
          }
        </app-dashboard-widget-host>

        <!-- Phase 2: Compliance trend + gauges -->
        <app-dashboard-widget-host
          [descriptor]="facade.widgetById('home-compliance-gauge') ?? null"
          [visible]="showWidget('home-compliance-gauge')"
          [loading]="facade.snapshotLoading() && !facade.snapshot()"
          [error]="snapshotWidgetError()"
          [dataReady]="!!facade.snapshot() && facade.isSnapshotAvailable()"
          [skeletonMinHeight]="280"
          (retry)="onWidgetRetry($event)"
        >
          @if (facade.snapshot(); as snap) {
            <div class="compliance-row">
              @if (snap.scoreTrends?.length) {
                <app-score-trend-chart [data]="snap.scoreTrends ?? []" />
              } @else {
                <div></div>
              }
              <div class="gauge-rail">
                <app-gauge-chart title="Avg Compliance Score" [value]="complianceScore()" id="compliance" />
                <app-gauge-chart title="Completion Rate" [value]="completionRatePercent()" id="completion" />
              </div>
            </div>
          }
        </app-dashboard-widget-host>

        <!-- Phase 3: Department heatmap -->
        <app-dashboard-widget-host
          [descriptor]="facade.widgetById('home-department-heatmap') ?? null"
          [visible]="showWidget('home-department-heatmap')"
          [loading]="heatmapLoading()"
          [error]="heatmapError()"
          [dataReady]="departmentHeatmapData().length > 0 || !heatmapLoading()"
          [skeletonMinHeight]="220"
          (retry)="onWidgetRetry($event)"
        >
          <section class="heatmap-section">
            <div class="heatmap-section__header">
              <p class="heatmap-section__caption" id="department-readiness-heatmap-caption">
                {{ heatmapCaption() }}
              </p>
              <a class="heatmap-section__drilldown" routerLink="/app/analytics/departments">
                Department performance →
              </a>
              <a class="heatmap-section__drilldown heatmap-section__drilldown--secondary" routerLink="/app/compliance-maturity">
                Maturity →
              </a>
            </div>
            <app-compliance-heatmap
              title="Department Readiness Heatmap"
              [data]="departmentHeatmapData()"
              [attr.aria-describedby]="heatmapCaptionId"
            />
          </section>
        </app-dashboard-widget-host>

        <!-- Operational widgets -->
        <app-dashboard-widget-host
          [descriptor]="facade.widgetById('home-hospital-overview') ?? null"
          [visible]="showWidget('home-hospital-overview')"
          [loading]="facade.snapshotLoading() && !facade.snapshot()"
          [error]="snapshotWidgetError()"
          [dataReady]="!!(facade.snapshot()?.hospitals?.length)"
          (retry)="onWidgetRetry($event)"
        >
          @if (facade.snapshot()?.hospitals?.length) {
            <app-hospital-overview [hospitals]="facade.snapshot()!.hospitals ?? []" />
          }
        </app-dashboard-widget-host>

        <app-dashboard-widget-host
          [descriptor]="facade.widgetById('home-framework-overview') ?? null"
          [visible]="showWidget('home-framework-overview')"
          [loading]="facade.snapshotLoading() && !facade.snapshot()"
          [error]="snapshotWidgetError()"
          [dataReady]="frameworkOverview().length > 0"
          [skeletonMinHeight]="260"
          (retry)="onWidgetRetry($event)"
        >
          @if (frameworkOverview().length) {
            <div class="framework-row">
              <app-high-risk-chapters
                title="Framework Overview"
                [chapters]="frameworkOverview()"
                [showTaskColumns]="true"
              />
              <app-chapter-breakdown title="Framework Score Comparison" [data]="chapterBreakdown()" />
            </div>
          }
        </app-dashboard-widget-host>

        <!-- Analytics widgets -->
        <app-dashboard-widget-host
          [descriptor]="facade.widgetById('home-risk-distribution') ?? null"
          [visible]="showWidget('home-risk-distribution')"
          [loading]="facade.snapshotLoading() && !facade.snapshot()"
          [error]="snapshotWidgetError()"
          [dataReady]="riskData().length > 0"
          (retry)="onWidgetRetry($event)"
        >
          @if (riskData().length) {
            <div class="distribution-row">
              <app-risk-distribution title="Task Priority Distribution" [data]="riskData()" />
              @if (facade.snapshot()?.tasksSummary?.byType) {
                <app-task-type-breakdown [byType]="facade.snapshot()!.tasksSummary!.byType" />
              }
            </div>
          }
        </app-dashboard-widget-host>

        <app-dashboard-widget-host
          [descriptor]="facade.widgetById('home-milestones') ?? null"
          [visible]="showWidget('home-milestones')"
          [loading]="facade.snapshotLoading() && !facade.snapshot()"
          [error]="snapshotWidgetError()"
          [dataReady]="!!((facade.snapshot()?.milestones?.length) || (facade.snapshot()?.frameworks?.length))"
          (retry)="onWidgetRetry($event)"
        >
          @if ((facade.snapshot()?.milestones?.length) || (facade.snapshot()?.frameworks?.length)) {
            <div class="bottom-grid">
              @if (facade.snapshot()?.milestones?.length) {
                <app-milestones
                  title="Upcoming Milestones"
                  [milestones]="facade.snapshot()!.milestones ?? []"
                />
              }
              @if (facade.snapshot()?.frameworks?.length && hasActivity()) {
                <app-activity-stream
                  title="Recent Activity"
                  [frameworks]="facade.snapshot()!.frameworks ?? []"
                />
              }
            </div>
          }
        </app-dashboard-widget-host>
      </div>
    </div>
  `,
  styles: [
    `
      .dashboard-container {
        --dash-gap: 16px;
        --dash-card-pad: 16px;
        --dash-radius: 12px;
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        margin: 0 auto;
      }

      /* Single spacing source: all top-level sections share one vertical rhythm */
      .dashboard-content {
        display: flex;
        flex-direction: column;
        gap: var(--dash-gap);
      }

      .snapshot-calculating-banner {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 12px 16px;
        margin-bottom: 20px;
        border-radius: 10px;
        background: color-mix(in srgb, var(--primary-color, var(--color-primary)) 12%, transparent);
        color: var(--text-primary);
        font-size: 14px;
        font-weight: 500;
      }

      .snapshot-calculating-dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: var(--primary-color, var(--color-primary));
        animation: snapshot-pulse 1.2s ease-in-out infinite;
      }

      @keyframes snapshot-pulse {
        0%,
        100% {
          opacity: 0.35;
          transform: scale(0.9);
        }
        50% {
          opacity: 1;
          transform: scale(1);
        }
      }

      .snapshot-pending-header {
        margin-bottom: 0;
      }

      /* ── Header ── */
      .dashboard-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 16px;
      }

      .persona-tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin-bottom: 20px;
      }

      .persona-tab {
        border: 1px solid var(--border-color, #cbd5e1);
        background: var(--surface, #fff);
        color: var(--text-secondary, #64748b);
        border-radius: 999px;
        padding: 6px 14px;
        font-size: 13px;
        font-weight: 600;
        cursor: pointer;
      }

      .persona-tab.active {
        background: var(--primary-color, var(--color-primary));
        border-color: var(--primary-color, var(--color-primary));
        color: #fff;
      }

      /* Role quick actions — chip/tile strip (must wrap; never inline run-on text) */
      .role-quick-actions {
        margin-bottom: 20px;
      }

      .role-quick-actions__title {
        margin: 0 0 10px;
        font-size: 15px;
        font-weight: 650;
        color: var(--text-primary, #0f172a);
      }

      .role-quick-actions__grid {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }

      .role-quick-action {
        display: inline-flex;
        align-items: center;
        padding: 8px 12px;
        border-radius: 999px;
        border: 1px solid var(--border-color, #cbd5e1);
        background: var(--card-bg, #fff);
        color: var(--text-primary, #0f172a);
        text-decoration: none;
        font-size: 13px;
        font-weight: 600;
        white-space: nowrap;
        line-height: 1.25;
      }

      .role-quick-action:hover {
        border-color: var(--primary-color, var(--color-primary));
        color: var(--primary-color, var(--color-primary));
      }

      .owner-attention {
        display: flex;
        flex-direction: column;
        gap: 16px;
        margin-bottom: 20px;
      }

      .owner-attention__kpis {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 12px;
      }

      .dashboard-title {
        font-size: var(--font-size-page-title);
        font-weight: 700;
        color: var(--text-primary);
        margin-bottom: 4px;
      }

      .dashboard-subtitle {
        font-size: var(--font-size-subtitle);
        color: var(--text-secondary);
        font-weight: 500;
      }

      .header-actions {
        display: flex;
        align-items: center;
        gap: 16px;
      }

      .last-updated-inline {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 13px;
        color: var(--text-secondary);
        padding: 8px 12px;
        background: var(--hover-bg);
        border-radius: 8px;
      }

      .refresh-button {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 18px;
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 8px;
        color: var(--text-primary);
        font-size: 14px;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.2s ease;
        white-space: nowrap;
      }

      .refresh-button:hover:not(.loading) {
        background: var(--primary-color);
        color: white;
        border-color: var(--primary-color);
        transform: translateY(-1px);
        box-shadow: var(--shadow-md);
      }

      .refresh-button.loading {
        opacity: 0.7;
        cursor: not-allowed;
      }

      .refresh-button svg {
        transition: transform 0.3s ease;
      }

      .refresh-button svg.spinning {
        animation: spin 1s linear infinite;
      }

      @keyframes spin {
        from { transform: rotate(0deg); }
        to { transform: rotate(360deg); }
      }

      /* ── Priority-ordered paired rows ── */
      .compliance-row {
        display: grid;
        grid-template-columns: minmax(0, 2fr) minmax(260px, 1fr);
        gap: var(--dash-gap);
        align-items: stretch;
      }

      .gauge-rail {
        display: flex;
        flex-direction: column;
        gap: var(--dash-gap);
      }

      .gauge-rail app-gauge-chart {
        display: block;
        flex: 1;
      }

      .compliance-row app-score-trend-chart {
        display: block;
        height: 100%;
      }

      .framework-row {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: var(--dash-gap);
        align-items: stretch;
      }

      /* auto-fit so a single child (e.g. no task-type data) fills the full width — no dead zone */
      .distribution-row {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
        gap: var(--dash-gap);
        align-items: stretch;
      }

      /* Cards stretch to equal heights within their row → consistent, no dead zones */
      app-score-trend-chart,
      app-task-type-breakdown,
      app-chapter-breakdown,
      app-risk-distribution,
      app-high-risk-chapters,
      app-milestones,
      app-activity-stream {
        display: block;
        min-width: 0;
      }

      .framework-row > *,
      .distribution-row > *,
      .bottom-grid > * {
        height: 100%;
      }

      /* ── Alert Banner ── */
      .alert-banner {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 16px 20px;
        background: rgba(239, 68, 68, 0.08);
        border: 1px solid rgba(239, 68, 68, 0.2);
        border-left: 4px solid #ef4444;
        border-radius: 12px;
        margin-bottom: 24px;
      }

      .alert-icon {
        width: 40px;
        height: 40px;
        background: rgba(239, 68, 68, 0.12);
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #ef4444;
        flex-shrink: 0;
      }

      .alert-content {
        flex: 1;
      }

      .alert-title {
        font-size: 15px;
        font-weight: 600;
        color: var(--text-primary);
        margin-bottom: 4px;
      }

      .alert-message {
        font-size: 13px;
        color: var(--text-secondary);
        line-height: 1.5;
      }

      .alert-count {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 2px;
        flex-shrink: 0;
      }

      .count-badge {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        background: #ef4444;
        color: white;
        border-radius: 50%;
        font-size: 16px;
        font-weight: 700;
      }

      .count-label {
        font-size: 11px;
        color: var(--text-secondary);
        text-transform: uppercase;
        font-weight: 600;
        letter-spacing: 0.3px;
      }

      .summary-skeleton-row {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: var(--dash-gap);
      }

      /* ── Loading Skeleton (legacy styles retained for reference) ── */
      .dashboard-skeleton {
        display: flex;
        flex-direction: column;
        gap: 24px;
      }

      .skeleton-header {
        display: flex;
        flex-direction: column;
        gap: 8px;
      }

      .skeleton-line {
        border-radius: 8px;
        background: var(--hover-bg);
        position: relative;
        overflow: hidden;
      }

      .skeleton-line::after {
        content: '';
        position: absolute;
        top: 0;
        left: -100%;
        width: 100%;
        height: 100%;
        background: linear-gradient(
          90deg,
          transparent,
          rgba(255, 255, 255, 0.08),
          transparent
        );
        animation: shimmer 1.5s infinite;
      }

      @keyframes shimmer {
        0% { left: -100%; }
        100% { left: 100%; }
      }

      .skeleton-title {
        width: 280px;
        height: 32px;
      }

      .skeleton-subtitle {
        width: 400px;
        height: 18px;
      }

      .skeleton-cards {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 20px;
      }

      .skeleton-card {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 24px;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .skeleton-card-icon {
        width: 48px;
        height: 48px;
        border-radius: 12px;
      }

      .skeleton-card-value {
        width: 80px;
        height: 36px;
      }

      .skeleton-card-label {
        width: 160px;
        height: 14px;
      }

      .skeleton-row {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
        gap: 20px;
      }

      .skeleton-chart {
        height: 260px;
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        position: relative;
        overflow: hidden;
      }

      .skeleton-chart::after {
        content: '';
        position: absolute;
        top: 0;
        left: -100%;
        width: 100%;
        height: 100%;
        background: linear-gradient(
          90deg,
          transparent,
          rgba(255, 255, 255, 0.04),
          transparent
        );
        animation: shimmer 1.5s infinite;
      }

      .skeleton-chart.wide {
        grid-column: 1 / -1;
      }

      /* ── Error State ── */
      .dashboard-error {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 16px;
        padding: 80px 32px;
        text-align: center;
      }

      .error-icon {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        background: rgba(239, 68, 68, 0.08);
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--danger-color);
      }

      .error-title {
        font-size: 22px;
        font-weight: 700;
        color: var(--text-primary);
      }

      .error-message {
        font-size: 14px;
        color: var(--text-secondary);
        max-width: 400px;
        line-height: 1.5;
      }

      .retry-button {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 24px;
        background: var(--primary-color);
        color: white;
        border: none;
        border-radius: 8px;
        font-size: 14px;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.2s ease;
      }

      .retry-button:hover {
        transform: translateY(-1px);
        box-shadow: var(--shadow-md);
        opacity: 0.9;
      }

      /* ── Department readiness heatmap (de-nested: single card surface) ── */
      .heatmap-section {
        display: block;
      }

      .heatmap-section__caption {
        margin: 0;
        flex: 1 1 auto;
        font-size: 12px;
        font-weight: 600;
        color: var(--text-secondary);
      }

      app-compliance-heatmap {
        display: block;
      }

      /* ── Bottom Row: Milestones + Activity (auto-fit avoids a lone card dead zone) ── */
      .bottom-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(360px, 1fr));
        gap: var(--dash-gap);
        align-items: stretch;
      }

      /* ── Responsive ── */
      /* Chart components have a 500px min-width, so collapse 2-col rows below 1280 */
      @media (max-width: 1280px) {
        .compliance-row,
        .framework-row,
        .distribution-row,
        .bottom-grid {
          grid-template-columns: 1fr;
        }

        .gauge-rail {
          flex-direction: row;
        }
      }

      @media (max-width: 1024px) {
        .dashboard-container {
          padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        }

        .dashboard-header {
          flex-direction: column;
          gap: 16px;
        }

        .refresh-button {
          align-self: flex-start;
        }
      }

      @media (max-width: 768px) {
        .dashboard-container {
          padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        }

        .dashboard-title {
          font-size: 22px;
        }

        .gauge-rail {
          flex-direction: column;
        }
      }
    `,
  ],
})
export class DashboardComponent {
  readonly facade = inject(DashboardExperienceFacade);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly hospitalDashboardService = inject(HospitalDashboardService);
  private readonly userContext = inject(UserContextService);
  private readonly roleExperience = inject(RoleExperienceService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly perf = inject(PerformanceTimelineService);

  readonly personas: { id: DashboardPersona; label: string }[] = [
    { id: 'quality-manager', label: 'Quality Manager' },
    { id: 'department-head', label: 'Department Head' },
    { id: 'hospital-admin', label: 'Hospital Admin' },
    { id: 'consultant', label: 'Consultant' },
  ];

  readonly persona = signal<DashboardPersona>(this.resolveDefaultPersona());
  readonly roleHome = signal<RoleExperienceHome | null>(null);
  readonly roleHomeLoading = signal(true);
  readonly roleHomeError = signal<string | null>(null);

  readonly nbaItems = computed(() =>
    (this.roleHome()?.priorities ?? []).map((p) => ({
      title: p.title,
      rationale: p.rationale,
      href: p.href,
      severity: p.severity,
    })),
  );

  readonly nbaSubtitle = computed(() => {
    const counts = this.roleHome()?.attentionCounts;
    if (!counts) return '';
    return `${counts.overdueTasks} overdue tasks · ${counts.pendingReviews} pending reviews · ${counts.criticalCapas} critical CAPAs · ${counts.upcomingAudits} upcoming audits`;
  });

  readonly roleQuickActions = computed(() => this.roleHome()?.quickActions ?? []);

  readonly ownerAttentionKpis = computed(() => {
    if (this.persona() !== 'hospital-admin') return [];
    const counts = this.roleHome()?.attentionCounts;
    const depts = this.departmentBreakdown();
    const avgDept =
      depts.length > 0
        ? Math.round(depts.reduce((sum, d) => sum + d.complianceScore, 0) / depts.length)
        : null;
    const lowest = depts.length
      ? [...depts].sort((a, b) => a.complianceScore - b.complianceScore)[0]
      : null;

    return [
      {
        label: 'Compliance score',
        value: `${this.complianceScore()}%`,
        hint: 'Hospital average',
        tone: (this.complianceScore() < 70 ? 'critical' : this.complianceScore() < 85 ? 'warning' : 'default') as
          | 'default'
          | 'warning'
          | 'critical',
      },
      {
        label: 'Dept performance',
        value: avgDept != null ? `${avgDept}%` : '—',
        hint: lowest ? `Lowest: ${lowest.department}` : 'Open analytics',
        tone: (avgDept != null && avgDept < 70 ? 'warning' : 'default') as 'default' | 'warning' | 'critical',
      },
      {
        label: 'Critical CAPAs',
        value: counts?.criticalCapas ?? 0,
        hint: 'Overdue corrective actions',
        tone: ((counts?.criticalCapas ?? 0) > 0 ? 'critical' : 'default') as 'default' | 'warning' | 'critical',
      },
      {
        label: 'Upcoming audits',
        value: counts?.upcomingAudits ?? 0,
        hint: 'Open audit sessions',
        tone: 'default' as const,
      },
    ];
  });

  readonly ownerRiskItems = computed<RiskListItem[]>(() => {
    if (this.persona() !== 'hospital-admin') return [];
    const counts = this.roleHome()?.attentionCounts;
    const items: RiskListItem[] = [];
    const weakDepts = [...this.departmentBreakdown()]
      .sort((a, b) => a.complianceScore - b.complianceScore)
      .filter((d) => d.complianceScore < 70)
      .slice(0, 3);

    for (const d of weakDepts) {
      items.push({
        label: `${d.department} at ${Math.round(d.complianceScore)}%`,
        detail: 'Department readiness below target',
        severity: d.complianceScore < 50 ? 'critical' : 'high',
        href: '/app/analytics/departments',
      });
    }
    if ((counts?.criticalCapas ?? 0) > 0) {
      items.push({
        label: `${counts!.criticalCapas} critical CAPAs`,
        detail: 'Overdue corrective actions need leadership chase',
        severity: 'critical',
        href: '/app/audit/capa',
      });
    }
    if ((counts?.pendingReviews ?? 0) > 0) {
      items.push({
        label: `${counts!.pendingReviews} pending approvals`,
        detail: 'Decisions waiting in review queues',
        severity: 'medium',
        href: '/app/governance/approvals',
      });
    }
    return items;
  });

  departmentBreakdown = signal<DepartmentComplianceDto[]>([]);
  heatmapLoading = signal(false);
  heatmapError = signal<string | null>(null);
  readonly heatmapCaptionId = 'department-readiness-heatmap-caption';

  constructor() {
    this.perf.mark('dashboard_constructor');
    this.perf.mark('dashboard_route_activate');
    this.facade.initialize();
    this.loadRoleExperienceHome();

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const raw = params.get('persona');
      if (raw && this.isDashboardPersona(raw) && this.persona() !== raw) {
        this.persona.set(raw);
      }
    });

    afterNextRender(() => {
      this.perf.mark('dashboard_shell_rendered');
      this.perf.measure('shell_rendered_ms', 'dashboard_route_activate', 'dashboard_shell_rendered');
      this.perf.mark('dashboard_render_complete');
      this.scheduleDepartmentBreakdownLoad();
    });

    combineLatest([
      this.complianceContext.hospitalId$,
      this.complianceContext.frameworkId$,
    ])
      .pipe(
        skip(1),
        debounceTime(100),
        distinctUntilChanged(
          (prev, curr) => prev[0] === curr[0] && prev[1] === curr[1],
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.facade.initialize();
        this.loadRoleExperienceHome();
        this.scheduleDepartmentBreakdownLoad();
      });

    this.complianceContext.hospitalId$
      .pipe(skip(1), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.scheduleDepartmentBreakdownLoad();
      });

    interval(5 * 60 * 1000)
      .pipe(
        filter(() => !document.hidden && !this.facade.snapshotLoading() && !this.facade.isRefreshing()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.facade.retrySnapshot();
      });
  }

  refreshDashboard(): void {
    this.facade.refreshAll();
    this.loadRoleExperienceHome();
    this.scheduleDepartmentBreakdownLoad();
  }

  setPersona(persona: DashboardPersona): void {
    this.persona.set(persona);
  }

  quickActionPath(href: string): string {
    const q = href.indexOf('?');
    return q >= 0 ? href.slice(0, q) : href;
  }

  showWidget(widgetId: string): boolean {
    if (!this.facade.isWidgetEnabled(widgetId)) {
      return false;
    }

    const allowList = this.roleHome()?.widgetAllowList;
    if (allowList?.length) {
      return allowList.includes(widgetId);
    }

    const descriptor = this.facade.widgetById(widgetId);
    const packTag = `role-pack:${this.persona()}`;
    const packTags = descriptor?.permissions?.filter((p) => p.startsWith('role-pack:')) ?? [];
    if (packTags.length > 0) {
      return packTags.includes(packTag) && PERSONA_WIDGETS[this.persona()].has(widgetId);
    }

    return PERSONA_WIDGETS[this.persona()].has(widgetId);
  }

  private loadRoleExperienceHome(): void {
    const frameworkId = this.complianceContext.getFrameworkId();
    this.roleHomeLoading.set(true);
    this.roleHomeError.set(null);
    this.roleExperience
      .getHome(frameworkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (home) => {
          this.roleHome.set(home);
          this.roleHomeLoading.set(false);
          if (!this.route.snapshot.queryParamMap.get('persona')) {
            const fromPack = this.packIdToPersona(home.packId);
            if (fromPack) {
              this.persona.set(fromPack);
            }
          }
        },
        error: () => {
          this.roleHomeLoading.set(false);
          this.roleHomeError.set('Unable to load role priorities.');
          this.roleHome.set(this.roleExperience.resolveLocalPack());
        },
      });
  }

  private packIdToPersona(packId: string): DashboardPersona | null {
    switch (packId) {
      case 'hospital-admin':
        return 'hospital-admin';
      case 'quality-manager':
        return 'quality-manager';
      case 'department-head':
        return 'department-head';
      case 'consultant':
        return 'consultant';
      default:
        return null;
    }
  }

  private isDashboardPersona(value: string): value is DashboardPersona {
    return (
      value === 'quality-manager' ||
      value === 'department-head' ||
      value === 'hospital-admin' ||
      value === 'consultant'
    );
  }

  private resolveDefaultPersona(): DashboardPersona {
    const rawRoles = this.userContext.getUserRoles();
    const roles = rawRoles.map((r) => normalizeRoleId(r) ?? r);
    // Prefer Department Head (claim alias / DEPARTMENT_HEAD) before MANAGER → quality-manager.
    if (
      roles.some((r) => r === 'DEPARTMENT_HEAD') ||
      rawRoles.some((r) => /department.?head/i.test(r) || r === 'DEPARTMENT_HEAD' || r === 'DepartmentHead') ||
      this.roleExperience.resolveLocalPack(rawRoles).packId === 'department-head'
    ) {
      return 'department-head';
    }
    if (roles.some((r) => r === 'ORG_ADMIN' || r === 'GROUP_ADMIN' || r === 'SUPER_ADMIN')) {
      return 'hospital-admin';
    }
    if (roles.some((r) => r === 'EXTERNAL_AUDITOR')) {
      return 'consultant';
    }
    if (roles.some((r) => r === 'REGULATORY_ADMIN' || r === 'COMPLIANCE_VIEWER' || r === 'AUTHORITY')) {
      return 'consultant';
    }
    if (roles.some((r) => r === 'MANAGER')) {
      return 'quality-manager';
    }
    return 'quality-manager';
  }

  onWidgetRetry(widgetId: string): void {
    if (widgetId.startsWith('home-department-heatmap')) {
      this.scheduleDepartmentBreakdownLoad(true);
      return;
    }
    this.facade.retryWidget(widgetId);
  }

  summaryWidgetError = computed(() => {
    if (this.facade.experienceError() && !this.summaryCards().length) {
      return this.facade.experienceError();
    }
    return null;
  });

  snapshotWidgetError = computed(() => {
    if (this.facade.snapshotError() && !this.facade.snapshot()) {
      return this.facade.snapshotError();
    }
    return null;
  });

  private scheduleDepartmentBreakdownLoad(force = false): void {
    if (typeof requestIdleCallback === 'function') {
      requestIdleCallback(() => this.loadDepartmentBreakdown(force));
    } else {
      setTimeout(() => this.loadDepartmentBreakdown(force), 0);
    }
  }

  private loadDepartmentBreakdown(force = false): void {
    const hospitalId = this.complianceContext.getHospitalId();
    if (!hospitalId) {
      this.departmentBreakdown.set([]);
      return;
    }

    const cached = this.hospitalDashboardService.getCached(hospitalId);
    if (cached?.departmentBreakdown && !force) {
      this.setDepartmentBreakdownData(cached.departmentBreakdown);
    }

    this.heatmapLoading.set(true);
    this.heatmapError.set(null);
    this.perf.mark('hospital_dashboard_http_start');
    this.hospitalDashboardService.getHospitalDashboard(hospitalId, { background: true }).subscribe({
      next: (data) => {
        this.perf.mark('hospital_dashboard_http_end');
        this.heatmapLoading.set(false);
        this.setDepartmentBreakdownData(data.departmentBreakdown ?? []);
      },
      error: (err) => {
        console.error('Failed to load department readiness:', err);
        this.heatmapLoading.set(false);
        this.heatmapError.set(err?.message ?? 'Failed to load department readiness');
        this.departmentBreakdown.set([]);
      },
    });
  }

  private setDepartmentBreakdownData(items: DepartmentComplianceDto[]): void {
    const sorted = [...items].sort((a, b) => b.complianceScore - a.complianceScore);
    this.departmentBreakdown.set(sorted);
  }

  // ── Computed: Header ───────────────────────────────
  dashboardSubtitle = computed(() => {
    const snap = this.facade.snapshot();
    const summary = this.facade.summary();
    if (snap?.isSnapshotAvailable === false) {
      return 'Your compliance score is being calculated';
    }
    if (snap) {
      const fw = snap.summary?.frameworksActive ?? 0;
      const org = snap.organizationName ?? summary?.organization?.name ?? '';
      return org ? `${org} — ${fw} Active Frameworks` : `${fw} Active Frameworks`;
    }
    if (summary) {
      const apps = summary.applications?.length ?? 0;
      return apps ? `${apps} active application${apps === 1 ? '' : 's'}` : 'Loading compliance overview…';
    }
    return 'Loading compliance overview…';
  });

  lastUpdated = computed(() => {
    const snap = this.facade.snapshot();
    const summary = this.facade.summary();
    if (summary?.lastUpdated) {
      return this.formatRelativeTime(summary.lastUpdated);
    }
    if (!snap) return '';
    const cal = snap.calculatedAt;
    if (cal) {
      const d = new Date(cal);
      const now = new Date();
      const diffMs = now.getTime() - d.getTime();
      const diffMin = Math.floor(diffMs / 60000);
      if (diffMin < 1) return 'just now';
      if (diffMin < 60) return `${diffMin} minute${diffMin > 1 ? 's' : ''} ago`;
      const diffHrs = Math.floor(diffMin / 60);
      if (diffHrs < 24) return `${diffHrs} hour${diffHrs > 1 ? 's' : ''} ago`;
      return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }
    const gen = snap.generatedAt;
    if (!gen) return '';
    const d = new Date(gen);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMin = Math.floor(diffMs / 60000);
    if (diffMin < 1) return 'just now';
    if (diffMin < 60) return `${diffMin} minute${diffMin > 1 ? 's' : ''} ago`;
    const diffHrs = Math.floor(diffMin / 60);
    if (diffHrs < 24) return `${diffHrs} hour${diffHrs > 1 ? 's' : ''} ago`;
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  });

  private formatRelativeTime(iso: string): string {
    const d = new Date(iso);
    const now = new Date();
    const diffMin = Math.floor((now.getTime() - d.getTime()) / 60000);
    if (diffMin < 1) return 'just now';
    if (diffMin < 60) return `${diffMin} minute${diffMin > 1 ? 's' : ''} ago`;
    const diffHrs = Math.floor(diffMin / 60);
    if (diffHrs < 24) return `${diffHrs} hour${diffHrs > 1 ? 's' : ''} ago`;
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  // ── Computed: Alerts ───────────────────────────────
  hasCriticalIssues = computed(() => {
    const exp = this.facade.summary();
    if ((exp?.criticalAlerts ?? 0) > 0 && !this.facade.snapshot()) {
      return true;
    }
    return (this.facade.snapshot()?.alerts ?? []).some((a) => !a.acknowledged);
  });

  unacknowledgedAlertCount = computed(() => {
    const snapAlerts = (this.facade.snapshot()?.alerts ?? []).filter((a) => !a.acknowledged).length;
    if (snapAlerts > 0) {
      return snapAlerts;
    }
    return this.facade.summary()?.criticalAlerts ?? 0;
  });

  alertTitle = computed(() => {
    const alerts = (this.facade.snapshot()?.alerts ?? []).filter((a) => !a.acknowledged);
    if (!alerts.length && (this.facade.summary()?.criticalAlerts ?? 0) > 0) {
      return 'Critical compliance alerts detected';
    }
    const highAlerts = alerts.filter((a) => a.severity === 'High');
    if (highAlerts.length > 0) return 'Critical compliance alerts detected';
    return 'Compliance alerts require attention';
  });

  alertMessage = computed(() => {
    const alerts = (this.facade.snapshot()?.alerts ?? []).filter((a) => !a.acknowledged);
    if (!alerts.length) {
      const count = this.facade.summary()?.criticalAlerts ?? 0;
      return count > 0 ? `${count} critical alert${count === 1 ? '' : 's'} require review.` : '';
    }
    if (alerts.length === 1) return alerts[0].message;
    const highCount = alerts.filter((a) => a.severity === 'High').length;
    if (highCount > 0) {
      return `${highCount} high-severity alert${highCount > 1 ? 's' : ''} and ${alerts.length - highCount} other alert${alerts.length - highCount !== 1 ? 's' : ''} require review.`;
    }
    return `${alerts.length} alerts require your attention.`;
  });

  // ── Computed: Executive Summary Cards ──────────────
  summaryCards = computed(() => {
    const snap = this.facade.snapshot();
    const summary = this.facade.summary();
    if (snap) {
      const s = snap.summary;
      const t = snap.tasksSummary;
      const readinessRatio = resolveDashboardCompletionRateRatio(snap);
      const readinessPercent = Math.round(readinessRatio * 100);
      const frameworksActive = s?.frameworksActive ?? snap.frameworks?.length ?? 0;
      const totalTasks = t?.total ?? 0;
      const overdueTasks = s?.overdueTasks ?? t?.overdue ?? 0;
      const upcomingExpiries = s?.upcomingExpiries ?? 0;

      if (this.persona() === 'hospital-admin') {
        const depts = this.departmentBreakdown();
        const avgDept =
          depts.length > 0
            ? Math.round(depts.reduce((sum, d) => sum + d.complianceScore, 0) / depts.length)
            : null;
        const criticalCapas = this.roleHome()?.attentionCounts?.criticalCapas ?? 0;
        const upcomingAudits = this.roleHome()?.attentionCounts?.upcomingAudits ?? 0;

        return [
          {
            title: 'Readiness',
            value: `${readinessPercent}%`,
            label: 'Overall Compliance Readiness',
            icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M22 4L12 14.01l-3-3" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
            color: readinessRatio >= 0.8 ? 'success' as const : readinessRatio >= 0.5 ? 'warning' as const : 'danger' as const,
            actionText: 'Readiness report',
            actionLink: '/app/reports/readiness',
            trend: undefined,
          },
          {
            title: 'Compliance Score',
            value: `${this.complianceScore()}%`,
            label: 'Hospital average score',
            icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 12h-4l-3 9L9 3l-3 9H2" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
            color: this.complianceScore() >= 80 ? 'success' as const : this.complianceScore() >= 60 ? 'warning' as const : 'danger' as const,
            actionText: 'View trends',
            actionLink: '/app/analytics',
          },
          {
            title: 'Dept Performance',
            value: avgDept != null ? `${avgDept}%` : '—',
            label: depts.length ? `${depts.length} departments` : 'Load hospital context',
            icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="3" width="7" height="7" rx="1" stroke-width="2"/><rect x="14" y="3" width="7" height="7" rx="1" stroke-width="2"/><rect x="3" y="14" width="7" height="7" rx="1" stroke-width="2"/><rect x="14" y="14" width="7" height="7" rx="1" stroke-width="2"/></svg>',
            color: avgDept != null && avgDept < 70 ? 'warning' as const : 'primary' as const,
            actionText: 'Department analytics',
            actionLink: '/app/analytics/departments',
          },
          {
            title: criticalCapas > 0 ? 'Critical CAPAs' : 'Upcoming Audits',
            value: criticalCapas > 0 ? criticalCapas : upcomingAudits,
            label: criticalCapas > 0 ? 'Overdue corrective actions' : 'Open audit sessions',
            icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M12 9v4m0 4h.01" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
            color: criticalCapas > 0 ? 'danger' as const : 'primary' as const,
            actionText: criticalCapas > 0 ? 'Review CAPAs' : 'Audit calendar',
            actionLink: criticalCapas > 0 ? '/app/audit/capa' : '/app/audit/calendar',
          },
        ];
      }

      return [
        {
          title: 'Readiness',
          value: `${readinessPercent}%`,
          label: 'Overall Compliance Readiness',
          icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M22 4L12 14.01l-3-3" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
          color: readinessRatio >= 0.8 ? 'success' as const : readinessRatio >= 0.5 ? 'warning' as const : 'danger' as const,
          trend: undefined,
        },
        {
          title: 'Total Tasks',
          value: totalTasks,
          label: `Across ${frameworksActive} frameworks`,
          icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 11l3 3L22 4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
          color: 'primary' as const,
          actionText: 'View Tasks',
          actionLink: '/app/tasks',
        },
        {
          title: 'Overdue Tasks',
          value: overdueTasks,
          label: 'Requires immediate attention',
          icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M12 9v4m0 4h.01" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
          color: overdueTasks > 0 ? 'danger' as const : 'success' as const,
          actionText: overdueTasks > 0 ? 'View Overdue' : undefined,
          actionLink: '/app/tasks?filter=overdue',
        },
        {
          title: 'Upcoming Expiries',
          value: upcomingExpiries,
          label: 'Journeys expiring soon',
          icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="4" width="18" height="18" rx="2" ry="2" stroke-width="2"/><path d="M16 2v4M8 2v4m-5 4h18" stroke-width="2" stroke-linecap="round"/></svg>',
          color: upcomingExpiries > 2 ? 'warning' as const : 'primary' as const,
        },
      ];
    }

    if (!summary) {
      return [];
    }

    const score = Math.round(summary.overallHealth?.score ?? 0);
    const pending = summary.pendingTasks ?? 0;
    const critical = summary.criticalAlerts ?? 0;
    const healthStatus = summary.overallHealth?.status ?? 'Unknown';

    return [
      {
        title: 'Health',
        value: healthStatus,
        label: summary.overallHealth?.message ?? 'Platform health',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M22 4L12 14.01l-3-3" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: healthStatus === 'Healthy' ? 'success' as const : healthStatus === 'Warning' ? 'warning' as const : 'danger' as const,
      },
      {
        title: 'Readiness',
        value: score > 0 ? `${score}%` : '—',
        label: 'Compliance score',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: score >= 80 ? 'success' as const : score >= 60 ? 'warning' as const : 'danger' as const,
      },
      {
        title: 'Pending Tasks',
        value: pending,
        label: 'Open work items',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 11l3 3L22 4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: 'primary' as const,
        actionText: 'View Tasks',
        actionLink: '/app/tasks',
      },
      {
        title: 'Critical Alerts',
        value: critical,
        label: 'Requires attention',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2"/></svg>',
        color: critical > 0 ? 'danger' as const : 'success' as const,
      },
    ];
  });

  // ── Computed: Gauge Charts ─────────────────────────
  complianceScore = computed(() => {
    const snap = this.facade.snapshot();
    if (!snap) return 0;
    if (typeof snap.overallScore === 'number') return Math.round(snap.overallScore);
    return Math.round(snap.summary?.avgComplianceScore ?? 0);
  });

  completionRatePercent = computed(() => {
    const snap = this.facade.snapshot();
    return Math.round(resolveDashboardCompletionRateRatio(snap) * 100);
  });

  departmentHeatmapData = computed<HeatmapItem[]>(() => {
    return this.departmentBreakdown().map((dept) => {
      const score = Math.round(dept.complianceScore);
      const meta =
        dept.totalTasks > 0
          ? `${dept.completedTasks}/${dept.totalTasks} tasks`
          : 'No tasks reported';

      return {
        label: dept.department,
        score,
        metadata: meta,
      };
    });
  });

  heatmapCaption = computed(() => {
    const departments = this.departmentBreakdown();
    if (!departments.length) {
      return 'Department readiness appears once a hospital context is chosen.';
    }
    const totalTasks = departments.reduce((sum, dept) => sum + dept.totalTasks, 0);
    const completedTasks = departments.reduce((sum, dept) => sum + dept.completedTasks, 0);
    const taskSummary = totalTasks
      ? `${completedTasks}/${totalTasks} tasks validated`
      : 'No task data yet';
    return `${departments.length} departments · ${taskSummary}`;
  });

  // ── Computed: Risk Distribution (by Priority) ─────
  riskData = computed(() => {
    const bp = this.facade.snapshot()?.tasksSummary?.byPriority;
    if (!bp) return [];
    return [
      { label: 'High', value: bp['High'] ?? 0, color: '#ef4444' },
      { label: 'Medium', value: bp['Medium'] ?? 0, color: '#f59e0b' },
      { label: 'Low', value: bp['Low'] ?? 0, color: '#3b82f6' },
    ];
  });

  // ── Computed: Framework Overview Table ─────────────
  frameworkOverview = computed(() => {
    const snap = this.facade.snapshot();
    if (!snap) return [];
    const byFw = snap.tasksSummary?.byFramework ?? [];
    const frameworks = sortByCpFrameworkCodeKeys(
      snap.frameworks ?? [],
      (fw) => fw.frameworkCode,
      (fw) => fw.frameworkName,
    );

    return frameworks.map((fw) => {
      const taskEntry = byFw.find((t) => t.frameworkCode === fw.frameworkCode);
      return {
        chapter: fw.frameworkCode,
        score: Math.round(fw.avgScore),
        riskLevel: (fw.avgScore < 70 ? 'High' : fw.avgScore < 80 ? 'Medium' : 'Low') as
          | 'High'
          | 'Medium'
          | 'Low',
        openTasks: taskEntry?.open ?? 0,
        overdueTasks: taskEntry?.overdue ?? 0,
      };
    });
  });

  // ── Computed: Activity Stream ───────────────────────
  hasActivity = computed(() =>
    (this.facade.snapshot()?.frameworks ?? []).some((fw) => (fw.recentActivity?.length ?? 0) > 0),
  );

  chapterBreakdown = computed(() => {
    const fws = this.facade.snapshot()?.frameworks;
    if (!fws) return [];
    const sorted = sortByCpFrameworkCodeKeys(fws, (fw) => fw.frameworkCode, (fw) => fw.frameworkName);
    return sorted.map((fw) => ({
      chapter: fw.frameworkCode,
      score: Math.round(fw.avgScore),
    }));
  });
}
