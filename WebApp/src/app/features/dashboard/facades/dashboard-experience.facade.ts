import { Injectable, computed, inject, signal } from '@angular/core';
import { DashboardExperienceStore } from '../../../core/services/dashboard-experience.store';
import { DashboardService } from '../services/dashboard.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { PerformanceTimelineService } from '../../../core/performance/performance-timeline.service';
import {
  DashboardExperienceQueryParams,
  DashboardExperienceResult,
  DashboardLoadingPhase,
  DashboardWidgetDescriptor,
} from '../models/dashboard-experience.model';
import { DashboardSnapshot, SnapshotQueryParams } from '../models/dashboard-snapshot.model';
import { environment } from '../../../../environments/environment';

@Injectable()
export class DashboardExperienceFacade {
  private readonly experienceStore = inject(DashboardExperienceStore);
  private readonly dashboardService = inject(DashboardService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly perf = inject(PerformanceTimelineService);

  readonly experience = signal<DashboardExperienceResult | null>(null);
  readonly experienceLoading = signal(false);
  readonly experienceError = signal<string | null>(null);

  readonly snapshot = signal<DashboardSnapshot | null>(null);
  readonly snapshotLoading = signal(false);
  readonly snapshotError = signal<string | null>(null);
  readonly isRefreshing = signal(false);

  readonly shellReady = signal(true);
  readonly phase1Ready = signal(false);
  readonly phase2Ready = signal(false);
  readonly phase3Ready = signal(false);

  readonly widgets = computed(() => this.experience()?.widgets ?? []);
  readonly sections = computed(() => this.experience()?.sections ?? []);

  readonly summary = computed(() => this.experience()?.summary ?? null);

  readonly isSnapshotAvailable = computed(() => {
    const snap = this.snapshot();
    return snap?.isSnapshotAvailable !== false;
  });

  initialize(): void {
    this.loadExperience();
    const cached = this.dashboardService.getCached(this.getSnapshotParams());
    if (cached) {
      this.snapshot.set(cached);
      this.snapshotLoading.set(false);
      this.perf.mark('snapshot_received');
      this.updatePhaseReadiness();
      if (!this.dashboardService.isFresh(this.getSnapshotParams(), environment.dashboardSnapshotCacheTtlMs)) {
        this.loadSnapshot({ background: true });
      }
      return;
    }
    this.loadSnapshot({ background: true });
  }

  refreshAll(): void {
    if (this.isRefreshing()) {
      return;
    }
    this.isRefreshing.set(true);
    this.experienceStore.invalidate(this.getExperienceParams());
    this.dashboardService.getProgramOverviewSnapshot(this.getSnapshotParams(), { forceRefresh: true }).subscribe({
      next: (data) => {
        this.snapshot.set(data);
        this.snapshotError.set(null);
        this.isRefreshing.set(false);
        this.updatePhaseReadiness();
      },
      error: () => this.isRefreshing.set(false),
    });
    this.loadExperience(true);
  }

  retryExperience(): void {
    this.experienceStore.invalidate(this.getExperienceParams());
    this.loadExperience(true);
  }

  retrySnapshot(): void {
    this.loadSnapshot({ background: false, forceRefresh: true });
  }

  retryWidget(_widgetId: string): void {
    this.retrySnapshot();
  }

  isWidgetEnabled(widgetId: string): boolean {
    const flags = environment.dashboardWidgetFlags ?? {};
    if (widgetId in flags) {
      return flags[widgetId] !== false;
    }
    return true;
  }

  widgetById(widgetId: string): DashboardWidgetDescriptor | undefined {
    return this.widgets().find((w) => w.id === widgetId);
  }

  widgetsForPhase(phase: DashboardLoadingPhase): DashboardWidgetDescriptor[] {
    return this.widgets().filter((w) => w.phase === phase && w.visibility === 'Visible' && this.isWidgetEnabled(w.id));
  }

  private loadExperience(forceRefresh = false): void {
    this.experienceLoading.set(true);
    this.experienceError.set(null);
    this.perf.mark('dashboard_experience_http_start');

    this.experienceStore.getExperience(this.getExperienceParams(), { forceRefresh }).subscribe({
      next: (data) => {
        this.perf.mark('dashboard_experience_received');
        this.experience.set(data);
        this.experienceLoading.set(false);
        this.phase1Ready.set(true);
        this.perf.mark('dashboard_phase_1_complete');
        this.perf.measure('phase_1_ms', 'dashboard_route_activate', 'dashboard_phase_1_complete');
        if (!this.snapshot()) {
          this.perf.mark('dashboard_interactive');
        }
        this.recordPhaseReport();
      },
      error: (err) => {
        this.experienceLoading.set(false);
        this.experienceError.set(err?.message ?? 'Failed to load dashboard summary');
        this.phase1Ready.set(true);
      },
    });
  }

  private loadSnapshot(options: { background?: boolean; forceRefresh?: boolean }): void {
    const background = options.background ?? false;
    if (!background) {
      this.snapshotLoading.set(true);
      this.snapshotError.set(null);
    }

    const cached = this.dashboardService.getCached(this.getSnapshotParams());
    if (cached && !options.forceRefresh) {
      this.snapshot.set(cached);
      this.snapshotLoading.set(false);
      this.perf.mark('snapshot_received');
      this.updatePhaseReadiness();
      if (background || !this.phase1Ready()) {
        this.perf.mark('dashboard_interactive');
      }
      if (!this.dashboardService.isFresh(this.getSnapshotParams(), environment.dashboardSnapshotCacheTtlMs)) {
        this.loadSnapshot({ background: true, forceRefresh: false });
      }
      return;
    }

    this.perf.mark(background ? 'snapshot_http_background_start' : 'snapshot_http_start');
    this.dashboardService
      .getProgramOverviewSnapshot(this.getSnapshotParams(), {
        background,
        forceRefresh: options.forceRefresh,
      })
      .subscribe({
        next: (data) => {
          this.perf.mark(background ? 'snapshot_http_background_end' : 'snapshot_http_end');
          this.perf.mark('snapshot_received');
          this.snapshot.set(data);
          this.snapshotLoading.set(false);
          this.snapshotError.set(null);
          this.updatePhaseReadiness();
          if (!background) {
            this.perf.mark('dashboard_interactive');
          }
          this.recordPhaseReport();
        },
        error: (err) => {
          if (!background) {
            this.snapshotLoading.set(false);
            this.snapshotError.set(
              err?.error?.message ?? err?.message ?? 'Failed to load dashboard data.',
            );
          }
          this.updatePhaseReadiness();
        },
      });
  }

  private updatePhaseReadiness(): void {
    const snap = this.snapshot();
    if (snap && snap.isSnapshotAvailable !== false) {
      this.phase2Ready.set(true);
      this.perf.mark('dashboard_phase_2_complete');
      this.perf.measure('phase_2_ms', 'dashboard_route_activate', 'dashboard_phase_2_complete');

      const hasAnalytics =
        (snap.scoreTrends?.length ?? 0) > 0 ||
        (snap.frameworks?.length ?? 0) > 0 ||
        (snap.tasksSummary?.byPriority
          ? Object.keys(snap.tasksSummary.byPriority).length > 0
          : false);
      if (hasAnalytics) {
        this.phase3Ready.set(true);
        this.perf.mark('dashboard_phase_3_complete');
        this.perf.measure('phase_3_ms', 'dashboard_route_activate', 'dashboard_phase_3_complete');
      }
    }
    this.recordPhaseReport();
  }

  private recordPhaseReport(): void {
    if (this.phase1Ready() && this.snapshot()) {
      this.perf.exportReport();
    }
  }

  private getExperienceParams(): DashboardExperienceQueryParams {
    return {
      hospitalId: this.complianceContext.getHospitalId(),
      frameworkId: this.complianceContext.getFrameworkId(),
    };
  }

  private getSnapshotParams(): SnapshotQueryParams {
    return {
      hospitalId: this.complianceContext.getHospitalId(),
      frameworkId: this.complianceContext.getFrameworkId(),
    };
  }
}
