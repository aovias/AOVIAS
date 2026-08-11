import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { ControlCenterService } from '../services/control-center.service';
import { environment } from '../../../../environments/environment';
import {
  ActivityEventDto,
  AlertDto,
  ControlCenterOverviewDto,
  RecommendedActionDto,
} from '../models/control-center.models';
import { AIInsight, RecommendedAction } from '../components/ai-intelligence-panel.component';
import { KPICard } from '../components/executive-kpi-bar.component';
import { HighRiskChapter, DepartmentRisk } from '../components/risk-command-center.component';
import { BlockingTask } from '../components/action-center.component';
import {
  ChapterComplianceData,
  ComplianceTrendData,
  EvidenceCoverageData,
} from '../components/compliance-analytics.component';
import { AuditReadinessData } from '../components/audit-readiness.component';
import { ActivityItem } from '../components/activity-stream.component';
import { SmartAlert } from '../components/smart-alerts.component';

const AUTO_REFRESH_MS = 30_000;

/**
 * Signal-based state for the Compliance Command Center. Owns the aggregate overview, loading/error
 * state and the 30-second (visibility-aware) auto-refresh, and exposes DTO->view-model mappers so
 * the existing child components keep their current interfaces. SignalR-ready: a future hub can push
 * updates through the single {@link applyOverview} entry point.
 */
@Injectable({ providedIn: 'root' })
export class ControlCenterFacade {
  private readonly service = inject(ControlCenterService);

  private readonly _overview = signal<ControlCenterOverviewDto | null>(null);
  private readonly _isLoading = signal<boolean>(false);
  private readonly _isRefreshing = signal<boolean>(false);
  private readonly _error = signal<string | null>(null);
  private readonly _lastUpdated = signal<Date | null>(null);

  readonly overview = this._overview.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly isRefreshing = this._isRefreshing.asReadonly();
  readonly error = this._error.asReadonly();
  readonly lastUpdated = this._lastUpdated.asReadonly();

  readonly isLive = computed(() => this._overview()?.header.isLive ?? true);
  readonly hasData = computed(() => this._overview() !== null);
  readonly isEmpty = computed(() => {
    const o = this._overview();
    if (!o) {
      return false;
    }
    return (
      o.highRiskChapters.length === 0 &&
      o.actionCenter.length === 0 &&
      o.recentActivity.length === 0 &&
      o.alerts.length === 0 &&
      o.kpis.complianceReadiness === 0
    );
  });

  private timer: ReturnType<typeof setInterval> | null = null;
  private visibilityHandler: (() => void) | null = null;

  // ---------------------------------------------------------------------
  // Loading
  // ---------------------------------------------------------------------
  load(): void {
    if (this._overview()) {
      if (this.isCacheFresh()) {
        return;
      }
      this.refresh(true);
      return;
    }
    this._isLoading.set(true);
    this._error.set(null);
    this.fetch(() => this._isLoading.set(false), false);
  }

  refresh(background = false): void {
    if (this._isRefreshing()) {
      return;
    }
    this._isRefreshing.set(true);
    this._error.set(null);
    this.fetch(() => this._isRefreshing.set(false), background);
  }

  private isCacheFresh(): boolean {
    const last = this._lastUpdated();
    if (!last) {
      return false;
    }
    return Date.now() - last.getTime() < environment.controlCenterCacheTtlMs;
  }

  private fetch(done: () => void, background: boolean): void {
    const request$ = background ? this.service.getOverviewBackground() : this.service.getOverview();
    request$
      .pipe(finalize(done))
      .subscribe({
        next: (dto) => this.applyOverview(dto),
        error: () => this._error.set('Unable to load compliance command center data. Please retry.'),
      });
  }

  /** Single entry point for applying a fresh overview (used by fetch and future SignalR pushes). */
  applyOverview(dto: ControlCenterOverviewDto): void {
    this._overview.set(dto);
    this._error.set(null);
    const stamp = dto?.header?.lastUpdated ? new Date(dto.header.lastUpdated) : new Date();
    this._lastUpdated.set(stamp);
  }

  // ---------------------------------------------------------------------
  // Auto-refresh (visibility-aware)
  // ---------------------------------------------------------------------
  startAutoRefresh(intervalMs: number = AUTO_REFRESH_MS): void {
    this.stopAutoRefresh();
    this.timer = setInterval(() => {
      if (typeof document !== 'undefined' && document.hidden) {
        return;
      }
      this.refresh(true);
    }, intervalMs);

    if (typeof document !== 'undefined') {
      this.visibilityHandler = () => {
        if (!document.hidden) {
          this.refresh(true);
        }
      };
      document.addEventListener('visibilitychange', this.visibilityHandler);
    }
  }

  stopAutoRefresh(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
    if (this.visibilityHandler && typeof document !== 'undefined') {
      document.removeEventListener('visibilitychange', this.visibilityHandler);
      this.visibilityHandler = null;
    }
  }

  // ---------------------------------------------------------------------
  // DTO -> view-model mappers (preserve existing child component interfaces)
  // ---------------------------------------------------------------------
  readonly aiInsights = computed<AIInsight[]>(() =>
    (this._overview()?.intelligence.insights ?? []).map((i) => ({
      type: this.toInsightType(i.severity),
      message: i.message,
      metric: i.metric ?? undefined,
    })),
  );

  readonly recommendedActions = computed<RecommendedAction[]>(() =>
    (this._overview()?.recommendedActions ?? []).map((a: RecommendedActionDto) => ({
      title: a.title,
      impact: a.description,
      priority: this.toPriority(a.priority),
    })),
  );

  readonly kpiCards = computed<KPICard[]>(() => {
    const k = this._overview()?.kpis;
    if (!k) {
      return [];
    }
    return [
      this.kpi('Compliance Readiness', k.complianceReadiness, '%', k.complianceReadinessTrend, k.complianceReadinessSparkline, '#10b981', '✓'),
      this.kpi('Audit Confidence', k.auditConfidence, '%', k.auditConfidenceTrend, k.auditConfidenceSparkline, '#3b82f6', '🎯'),
      this.kpi('Open Findings', k.openFindings, undefined, k.openFindingsTrend, [], '#f59e0b', '⚠️'),
      this.kpi('Critical Tasks', k.criticalTasks, undefined, k.criticalTasksTrend, [], '#ef4444', '📋'),
      this.kpi('Evidence Coverage', k.evidenceCoverage, '%', k.evidenceCoverageTrend, k.evidenceCoverageSparkline, '#0d9488', '📄'),
      this.kpi('Upcoming Audits', k.upcomingAudits, undefined, k.upcomingAuditsTrend, [], '#2f6bff', '📅'),
    ];
  });

  readonly highRiskChapters = computed<HighRiskChapter[]>(() =>
    (this._overview()?.highRiskChapters ?? []).map((c) => ({
      chapter: c.chapter,
      complianceScore: c.complianceScore,
      riskLevel: this.toRisk(c.riskLevel),
      openIssues: c.openIssues,
    })),
  );

  readonly departmentRisks = computed<DepartmentRisk[]>(() =>
    (this._overview()?.departmentRisks ?? []).map((d) => ({
      department: d.department,
      riskScore: d.riskScore,
      riskLevel: this.toRisk(d.riskLevel),
    })),
  );

  readonly blockingTasks = computed<BlockingTask[]>(() =>
    (this._overview()?.actionCenter ?? []).map((t) => ({
      id: t.id,
      title: t.title,
      complianceImpact: t.complianceImpact,
      owner: t.owner,
      department: t.department,
      dueDate: t.dueDate ? new Date(t.dueDate) : new Date(),
      priority: this.toActionPriority(t.priority),
      status: this.toActionStatus(t.status),
    })),
  );

  readonly trendData = computed<ComplianceTrendData[]>(() =>
    (this._overview()?.trend ?? []).map((p) => ({ date: p.date, score: p.compliance })),
  );

  readonly chapterData = computed<ChapterComplianceData[]>(() =>
    (this._overview()?.chapterCompliance ?? []).map((c) => ({ chapter: c.chapter, score: c.compliance })),
  );

  readonly evidenceData = computed<EvidenceCoverageData>(() => {
    const e = this._overview()?.evidenceCoverage;
    return { approved: e?.approved ?? 0, pending: e?.pending ?? 0, rejected: e?.rejected ?? 0 };
  });

  readonly auditReadinessData = computed<AuditReadinessData>(() => {
    const a = this._overview()?.auditReadiness;
    return {
      confidenceScore: a?.confidenceScore ?? 0,
      criticalFindings: a?.criticalFindings ?? 0,
      correctiveActionsPending: a?.correctiveActionsPending ?? 0,
      clausesMissingEvidence: a?.clausesMissingEvidence ?? 0,
      auditPassProbability: a?.auditPassProbability ?? 0,
    };
  });

  readonly activities = computed<ActivityItem[]>(() =>
    (this._overview()?.recentActivity ?? []).map((a: ActivityEventDto) => ({
      id: a.id,
      type: this.toActivityType(a.type),
      title: a.title,
      description: a.description,
      timestamp: new Date(a.timestamp),
      user: a.user,
    })),
  );

  readonly smartAlerts = computed<SmartAlert[]>(() =>
    (this._overview()?.alerts ?? []).map((a: AlertDto) => ({
      id: a.id,
      severity: this.toAlertSeverity(a.severity),
      title: a.title,
      message: a.description,
      timestamp: new Date(a.timestamp),
      actions: a.actions.map((x) => ({
        label: x.label,
        type: x.type === 'secondary' ? 'secondary' : 'primary',
        action: x.action,
      })),
    })),
  );

  // ---------------------------------------------------------------------
  // Mapping helpers
  // ---------------------------------------------------------------------
  private kpi(
    title: string,
    value: number,
    unit: string | undefined,
    trendValue: number,
    sparklineData: number[],
    color: string,
    icon: string,
  ): KPICard {
    const trend: KPICard['trend'] = trendValue > 0 ? 'up' : trendValue < 0 ? 'down' : 'neutral';
    const sign = trendValue > 0 ? '+' : '';
    const formatted = unit === '%' ? `${sign}${this.round(trendValue)}%` : `${sign}${this.round(trendValue)}`;
    return {
      title,
      value: this.round(value),
      unit,
      trend,
      trendValue: trendValue === 0 ? '0' : formatted,
      sparklineData: sparklineData?.length ? sparklineData : [value, value],
      color,
      icon,
    };
  }

  private round(value: number): number {
    return Math.round((value + Number.EPSILON) * 100) / 100;
  }

  private toInsightType(severity: string): AIInsight['type'] {
    switch ((severity || '').toLowerCase()) {
      case 'critical':
        return 'critical';
      case 'warning':
        return 'warning';
      case 'success':
        return 'success';
      default:
        return 'info';
    }
  }

  private toPriority(priority: string): RecommendedAction['priority'] {
    switch ((priority || '').toLowerCase()) {
      case 'high':
        return 'high';
      case 'low':
        return 'low';
      default:
        return 'medium';
    }
  }

  private toRisk(level: string): 'high' | 'medium' | 'low' {
    switch ((level || '').toLowerCase()) {
      case 'high':
      case 'critical':
        return 'high';
      case 'medium':
        return 'medium';
      default:
        return 'low';
    }
  }

  private toActionPriority(priority: string): BlockingTask['priority'] {
    switch ((priority || '').toLowerCase()) {
      case 'critical':
        return 'critical';
      case 'high':
        return 'high';
      default:
        return 'medium';
    }
  }

  private toActionStatus(status: string): BlockingTask['status'] {
    switch ((status || '').toLowerCase()) {
      case 'overdue':
        return 'overdue';
      case 'due-soon':
        return 'due-soon';
      default:
        return 'on-track';
    }
  }

  private toActivityType(type: string): ActivityItem['type'] {
    switch ((type || '').toLowerCase()) {
      case 'evidence':
        return 'evidence';
      case 'audit':
        return 'audit';
      case 'finding':
        return 'finding';
      default:
        return 'task';
    }
  }

  private toAlertSeverity(severity: string): SmartAlert['severity'] {
    switch ((severity || '').toLowerCase()) {
      case 'critical':
        return 'critical';
      case 'warning':
        return 'warning';
      default:
        return 'info';
    }
  }
}
