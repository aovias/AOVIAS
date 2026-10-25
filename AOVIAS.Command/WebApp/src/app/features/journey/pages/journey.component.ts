import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AppContextService } from '../../../core/services/app-context.service';
import { AccreditationProgressCardComponent } from '../../../shared/ui/accreditation-progress-card/accreditation-progress-card.component';
import { ActionBarAction, ActionBarComponent } from '../components/action-bar/action-bar.component';
import { ChartCardComponent } from '../components/chart-card/chart-card.component';
import { JourneyFrameworkContextComponent } from '../components/journey-framework-context/journey-framework-context.component';
import { JourneyCardComponent } from '../components/journey-card/journey-card.component';
import { MetricCardComponent } from '../components/metric-card/metric-card.component';
import { StageProgressStripComponent } from '../components/stage-progress-strip/stage-progress-strip.component';
import { OperationalMode } from '../models/nabh.enums';
import { JourneyTrendPoint } from '../models/journey-tree.model';
import { JourneyDataService } from '../services/journey-data.service';
import { JourneyWorkspaceStore } from '../state/journey-workspace.store';
import {
  ChartCardVm,
  JourneyModeCardVariant,
  JourneyModeCardVm,
  JourneyModeIconKey,
  MetricCardVm,
  PrimaryActionIntent,
  PrimaryActionVm,
} from '../models/journey-dashboard-vm.model';

@Component({
  selector: 'app-journey',
  standalone: true,
  imports: [
    CommonModule,
    JourneyCardComponent,
    AccreditationProgressCardComponent,
    ActionBarComponent,
    MetricCardComponent,
    ChartCardComponent,
    JourneyFrameworkContextComponent,
    StageProgressStripComponent,
  ],
  templateUrl: './journey.component.html',
  styleUrl: './journey.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JourneyComponent implements OnInit {
  readonly store = inject(JourneyWorkspaceStore);
  private readonly journeyData = inject(JourneyDataService);
  private readonly appContext = inject(AppContextService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly modeCards = computed<JourneyModeCardVm[]>(() => this.buildModeCards());
  readonly primaryActions = computed<PrimaryActionVm[]>(() => this.buildPrimaryActions());
  readonly metricCards = computed<MetricCardVm[]>(() => this.buildMetricCards());
  readonly chartCards = computed<ChartCardVm[]>(() => this.buildChartCards());

  /** Compliance readiness % from server (IComplianceScoreService via dashboard API). */
  readonly accreditationReadinessPercent = computed(() => {
    const snap = this.store.snapshot();
    const api = snap?.accreditationReadinessPercent;
    if (typeof api === 'number' && Number.isFinite(api)) {
      return Math.round(Math.max(0, Math.min(100, api)));
    }
    const acc = this.store.accreditationSnapshot();
    if (acc && typeof acc.overallCompliancePercent === 'number') {
      return Math.round(Math.max(0, Math.min(100, acc.overallCompliancePercent)));
    }
    const ov = this.store.overview();
    return ov ? Math.round(Math.max(0, Math.min(100, ov.coreCompliancePercent))) : 0;
  });

  readonly progressCardTitle = computed(() => this.store.operationalModeUi().progressCardTitle);
  readonly progressCardSubtitle = computed(() => this.store.operationalModeUi().progressCardSubtitle);

  readonly actionBarDangerHidden = computed(() => !this.store.operationalModeUi().showHighRiskOesAction);
  readonly actionBarDangerEmphasis = computed(() => this.store.operationalModeUi().dangerActionEmphasis);
  readonly actionBarDangerDisabled = computed(() => {
    const overview = this.store.overview();
    const ui = this.store.operationalModeUi();
    if (!ui.showHighRiskOesAction) return true;
    return this.store.readOnly() || (overview?.openActionPlansCount ?? 0) === 0;
  });

  constructor() {
    this.appContext.hospitalSwitched$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.journeyData.clearWorkspaceCache();
      this.store.initFromRoute();
    });
  }

  ngOnInit(): void {
    this.journeyData.clearWorkspaceCache();
  }

  onModeSelected(mode: OperationalMode): void {
    if (this.store.isModeLocked(mode)) return;
    this.store.setLifecycleMode(mode);
  }

  onAccreditationRefresh(): void {
    this.store.refreshWorkspace();
  }

  onPrimaryAction(action: ActionBarAction): void {
    const mapped: PrimaryActionIntent =
      action === 'primary'
        ? 'resume-selected-standard'
        : action === 'secondary'
          ? 'open-most-risky-chapter'
          : 'review-open-action-plans';

    if (mapped === 'resume-selected-standard') {
      void this.router.navigate(['/app/journey/execution']);
      return;
    }
    if (mapped === 'open-most-risky-chapter') {
      const target = this.store.highestRiskStandardTarget();
      if (target) this.store.selectChapter(target.chapterId);
      return;
    }
    this.store.setChapterSearch('action');
  }

  onChartDrilldown(intent: ChartCardVm['intent']): void {
    if (intent === 'risk-evolution') {
      this.store.toggleChapterRiskSort();
      return;
    }
    this.store.setChapterSearch('');
  }

  getProgressClass(value: number | null | undefined): string {
    const bounded = Math.max(0, Math.min(100, Math.round(value ?? 0)));
    const bucket = Math.round(bounded / 10) * 10;
    return `progress-${bucket}`;
  }

  trendRows(points: JourneyTrendPoint[], toneClass: string): Array<{
    label: string;
    valueLabel: string;
    widthClass: string;
    toneClass: string;
    tooltip: string;
  }> {
    return points.map((point) => {
      const bounded = Math.max(0, Math.min(100, Math.round(point.value)));
      const bucket = Math.round(bounded / 10) * 10;
      return {
        label: point.label,
        valueLabel: `${point.value}%`,
        widthClass: `progress-${bucket}`,
        toneClass,
        tooltip: `${point.label}: ${point.value}%`,
      };
    });
  }

  private buildModeCards(): JourneyModeCardVm[] {
    const selected = this.store.lifecycleMode();
    const lifecycleOrder: ReadonlyArray<OperationalMode> = [
      OperationalMode.SelfAssessment,
      OperationalMode.InternalAudit,
      OperationalMode.FinalAudit,
      OperationalMode.Surveillance,
    ];
    const totalSteps = lifecycleOrder.length;
    const selectedIdx = lifecycleOrder.indexOf(selected);

    const modeConfig: ReadonlyArray<{
      mode: OperationalMode;
      label: string;
      description: string;
      iconKey: JourneyModeIconKey;
    }> = [
      {
        mode: OperationalMode.SelfAssessment,
        label: 'Self Assessment',
        description: 'Conduct initial assessment, data entry, and gap analysis.',
        iconKey: 'self',
      },
      {
        mode: OperationalMode.InternalAudit,
        label: 'Internal Audit',
        description: 'Run structured mock audits before external review.',
        iconKey: 'internal',
      },
      {
        mode: OperationalMode.FinalAudit,
        label: 'Final Audit',
        description: 'Official phase with read-only controls after submission.',
        iconKey: 'final',
      },
      {
        mode: OperationalMode.Surveillance,
        label: 'Surveillance',
        description: 'Post-accreditation monitoring and risk continuity checks.',
        iconKey: 'surveillance',
      },
    ];

    return modeConfig.map((entry, i) => {
      const locked = this.store.isModeLocked(entry.mode);
      const state = locked ? 'locked' : entry.mode === selected ? 'active' : 'inactive';
      const modeIdx = lifecycleOrder.indexOf(entry.mode);
      const isNextPhase =
        !locked && entry.mode !== selected && modeIdx === selectedIdx + 1;
      const variant: JourneyModeCardVariant = locked
        ? 'locked'
        : entry.mode === selected
          ? 'active'
          : 'available';
      const lockReason = locked ? this.store.modeLockReason(entry.mode) : null;

      return {
        mode: entry.mode,
        label: entry.label,
        description: entry.description,
        iconKey: entry.iconKey,
        state,
        isSelected: entry.mode === selected,
        stepIndex: i + 1,
        totalSteps,
        lockReason,
        variant,
        isNextPhase,
      };
    });
  }

  private buildPrimaryActions(): PrimaryActionVm[] {
    const readOnly = this.store.readOnly();
    const overview = this.store.overview();
    const ui = this.store.operationalModeUi();
    const hasOpenActionPlans = (overview?.openActionPlansCount ?? 0) > 0;
    const actions: PrimaryActionVm[] = [
      {
        id: 'resume-selected-standard',
        label: 'Continue Journey',
        detail: 'Resume current chapter and standard progress.',
        intent: 'resume-selected-standard',
        disabled: false,
      },
      {
        id: 'open-most-risky-chapter',
        label: 'Resume Last OE',
        detail: 'Jump back to recently active objective elements.',
        intent: 'open-most-risky-chapter',
        disabled: false,
      },
    ];
    if (ui.showHighRiskOesAction) {
      actions.push({
        id: 'review-open-action-plans',
        label: 'View High Risk OEs',
        detail: 'Inspect open action plans and low-scoring objectives.',
        intent: 'review-open-action-plans',
        disabled: readOnly || !hasOpenActionPlans,
      });
    }
    return actions;
  }

  private buildMetricCards(): MetricCardVm[] {
    const overview = this.store.overview();
    const ui = this.store.operationalModeUi().metrics;
    if (!overview) return [];
    const all: MetricCardVm[] = [
      {
        id: 'core-compliance',
        label: 'Core Compliance %',
        value: `${overview.coreCompliancePercent}%`,
        tone: overview.coreCompliancePercent >= 80 ? 'success' : 'warning',
        helperText: 'Core objective elements within required score band.',
        iconName: 'lucideCirclePercent',
      },
      {
        id: 'failed-core',
        label: 'Failed Core OEs',
        value: `${overview.failedCoreCount}`,
        tone: overview.failedCoreCount > 0 ? 'danger' : 'success',
        helperText: 'Core objective elements currently below threshold.',
        iconName: 'lucideCircleAlert',
      },
      {
        id: 'low-score',
        label: 'Low Score OEs',
        value: `${overview.oesAtOrBelowThreeCount}`,
        tone: overview.oesAtOrBelowThreeCount > 0 ? 'warning' : 'success',
        helperText: 'Objective elements with score less than or equal to 3.',
        iconName: 'lucideTrendingDown',
      },
      {
        id: 'open-plans',
        label: 'Open Action Plans',
        value: `${overview.openActionPlansCount}`,
        tone: overview.openActionPlansCount > 0 ? 'warning' : 'neutral',
        helperText: 'Operational action plans pending closure.',
        iconName: 'lucideListTodo',
      },
    ];
    return all.filter((m) => {
      switch (m.id) {
        case 'core-compliance':
          return ui.coreCompliance;
        case 'failed-core':
          return ui.failedCore;
        case 'low-score':
          return ui.lowScore;
        case 'open-plans':
          return ui.openActionPlans;
        default:
          return true;
      }
    });
  }

  private buildChartCards(): ChartCardVm[] {
    const snap = this.store.snapshot();
    if (!snap) return [];
    return [
      this.toChartVm(
        'oe-trend',
        'OE Score Trend',
        'Last 7 days',
        snap.oeScoreTrend,
        '--color-chart-line-primary',
      ),
      this.toChartVm(
        'chapter-improvement',
        'Chapter Improvement',
        'Last 7 days',
        snap.chapterImprovementTrend,
        '--color-chart-bar-positive',
      ),
      this.toChartVm(
        'risk-evolution',
        'Risk Evolution',
        'Last 7 days',
        snap.riskEvolutionTrend,
        '--color-chart-line-secondary',
      ),
    ];
  }

  private toChartVm(
    id: ChartCardVm['id'],
    title: string,
    subtitle: string,
    trend: JourneyTrendPoint[],
    chartColorToken: string,
  ): ChartCardVm {
    const last = trend.at(-1)?.value ?? 0;
    const previous = trend.at(-2)?.value ?? last;
    const trendDirection = last === previous ? 'flat' : last > previous ? 'up' : 'down';
    return {
      id,
      title,
      subtitle,
      metricValue: `${last}%`,
      intent: id,
      trendDirection,
      chartColorToken,
    };
  }
}