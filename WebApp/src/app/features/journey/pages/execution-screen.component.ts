import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft } from '@ng-icons/lucide';
import { combineLatest, of } from 'rxjs';
import { catchError, filter, switchMap } from 'rxjs/operators';
import { JourneyWorkspaceStore, MaturityStageFilter } from '../state/journey-workspace.store';
import {
  ExecutionChapterNavItemVm,
  ExecutionChapterPanelVm,
  ExecutionOeItemVm,
  ExecutionStandardItemVm,
  ExecutionStandardsPanelVm,
} from '../models/execution-screen-vm.model';
import { JourneyFrameworkContextComponent } from '../components/journey-framework-context/journey-framework-context.component';
import { ExecutionChapterPanelComponent } from '../components/execution-chapter-panel/execution-chapter-panel.component';
import { ExecutionStandardsPanelComponent } from '../components/execution-standards-panel/execution-standards-panel.component';
import { JourneyContextPanelComponent } from '../components/journey-context-panel/journey-context-panel.component';
import { StageProgressStripComponent } from '../components/stage-progress-strip/stage-progress-strip.component';
import { oeRowStatus } from '../utils/execution-oe-vm.util';
import {
  journeyWorkflowStatusLabel,
  oeEvidenceCountLabel,
  oeJourneyStatus,
} from '../utils/journey-oe-status.util';
import {
  deficienciesQueryForOe,
  evidenceQueryForOe,
  oeNavigationFromStore,
  tasksQueryForOe,
} from '../utils/journey-execution-navigation.util';
import { openOeHistoryDialog } from '../utils/oe-history-dialog.util';
import { DeficiencyDataService } from '../services/deficiency-data.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { TaskApiService } from '../../tasks/task-api.service';
import type { DeficiencyDto } from '../models/deficiency-api.model';
import type { TaskListItem } from '../../tasks/models';
import { maturityStageStatusLabel } from '../../../shared/models/maturity-stage.model';
import { evidenceStatusLabel } from '../../../shared/models/evidence-status.model';

const VALID_STAGES: MaturityStageFilter[] = ['core', 'commitment', 'achievement', 'excellence'];

@Component({
  selector: 'app-execution-screen',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    NgIcon,
    ExecutionChapterPanelComponent,
    ExecutionStandardsPanelComponent,
    JourneyContextPanelComponent,
    JourneyFrameworkContextComponent,
    StageProgressStripComponent,
  ],
  viewProviders: [provideIcons({ lucideArrowLeft })],
  templateUrl: './execution-screen.component.html',
  styleUrl: './execution-screen.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExecutionScreenComponent implements OnInit {
  readonly store = inject(JourneyWorkspaceStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);
  private readonly deficiencyApi = inject(DeficiencyDataService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly taskApi = inject(TaskApiService);

  /** Open deficiencies keyed by objective element id (existing deficiency API). */
  private readonly openDeficienciesByOeId = signal<Map<string, number>>(new Map());
  /** Open tasks keyed by objective element id (existing task API). */
  private readonly openTasksByOeId = signal<Map<string, number>>(new Map());
  readonly openDeficiencyTotal = signal(0);

  /**
   * UI-only: when true, context column is shown as a fixed slide-over (tablet) or bottom sheet (mobile).
   * Desktop always shows inline 3-column layout.
   */
  readonly contextDrawerOpen = signal(false);

  /** Maturity stage buttons shown in the stage-filter strip above the workspace. */
  readonly stageOptions: { value: MaturityStageFilter; label: string }[] = [
    { value: null, label: 'All Stages' },
    { value: 'core', label: 'Core' },
    { value: 'commitment', label: 'Commitment' },
    { value: 'achievement', label: 'Achievement' },
    { value: 'excellence', label: 'Excellence' },
  ];

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.syncStageFilterFromRoute());

    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.syncStageFilterFromRoute());

    combineLatest([
      this.complianceContext.hospitalId$,
      this.complianceContext.frameworkId$,
    ])
      .pipe(
        filter(([h, f]) => !!h && !!f),
        switchMap(([hospitalId, frameworkId]) =>
          combineLatest([
            this.deficiencyApi.getDeficiencies(hospitalId!, frameworkId!, 1, 500).pipe(
              catchError(() => of({ items: [] as DeficiencyDto[], totalCount: 0, page: 1, pageSize: 500 })),
            ),
            this.taskApi
              .getTasksPaged(frameworkId!, {}, 1, 500, 'table')
              .pipe(catchError(() => of({ items: [] as TaskListItem[], totalCount: 0, page: 1, pageSize: 500 }))),
          ]),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(([defList, taskPage]) => {
        const defMap = new Map<string, number>();
        let openTotal = 0;
        for (const d of defList.items ?? []) {
          if (d.status === 'Closed' || d.status === 'Verified') continue;
          openTotal++;
          defMap.set(d.objectiveElementId, (defMap.get(d.objectiveElementId) ?? 0) + 1);
        }
        const taskMap = new Map<string, number>();
        for (const t of taskPage.items ?? []) {
          if (t.status === 'Completed') continue;
          taskMap.set(t.objectiveElementId, (taskMap.get(t.objectiveElementId) ?? 0) + 1);
        }
        this.openDeficienciesByOeId.set(defMap);
        this.openTasksByOeId.set(taskMap);
        this.openDeficiencyTotal.set(openTotal);
      });
  }

  readonly maturityStages = computed(() => this.store.maturityStages());

  readonly activeStageLabel = computed(() => {
    const current = this.store.currentMaturityStage();
    if (current) {
      const match = this.store.maturityStages().find((s) => s.stage === current);
      return match?.label ?? current;
    }
    const active = this.store.maturityStages().find(
      (s) => s.status === 'inProgress' || s.status === 'available',
    );
    return active?.label ?? '—';
  });

  readonly stageReadinessPercent = computed(() => {
    const current = this.store.currentMaturityStage();
    const stages = this.store.maturityStages();
    const active = current
      ? stages.find((s) => s.stage === current)
      : stages.find((s) => s.status === 'inProgress' || s.status === 'available');
    return active?.readinessPercent ?? 0;
  });

  readonly stageStatusLabel = computed(() => {
    const current = this.store.currentMaturityStage();
    const stages = this.store.maturityStages();
    const active = current
      ? stages.find((s) => s.stage === current)
      : stages.find((s) => s.status === 'inProgress' || s.status === 'available');
    return active ? maturityStageStatusLabel(active.status) : '—';
  });

  readonly complianceReadinessPercent = computed(() => {
    const snap = this.store.snapshot();
    const api = snap?.accreditationReadinessPercent;
    if (typeof api === 'number' && Number.isFinite(api)) {
      return Math.round(api);
    }
    const ov = this.store.overview();
    return ov ? Math.round(ov.coreCompliancePercent) : 0;
  });

  private oeExecutionSignals(oeId: string, oe: import('../models/objective-element.model').ObjectiveElement) {
    const obl = this.store.obligationStateByOeId().get(oeId);
    const evidenceLabel = (obl?.evidenceStatus ?? oe.evidenceStatus ?? '—').toString();
    const verificationLabel = (obl?.verificationStatus ?? '—').toString();
    const obligationLabel = (obl?.implementationStatus ?? '—').toString();
    return {
      deficiencyCount: this.openDeficienciesByOeId().get(oeId) ?? 0,
      openTaskCount: this.openTasksByOeId().get(oeId) ?? 0,
      evidenceStatusLabel: evidenceStatusLabel(evidenceLabel, 'evidence'),
      verificationStatusLabel: evidenceStatusLabel(verificationLabel, 'obligation'),
      obligationStatusLabel: this.formatStatusLabel(obligationLabel),
      readinessPercent: obl?.readinessScore ?? null,
    };
  }

  private formatStatusLabel(raw: string): string {
    if (!raw || raw === '—') return '—';
    return raw.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }

  private selectedOeNav() {
    const oid = this.store.selectedOeId();
    if (!oid) return null;
    return oeNavigationFromStore(oid, (id) => this.store.findOe(id));
  }

  private syncStageFilterFromRoute(): void {
    const paramRaw = this.route.snapshot.paramMap.get('stage');
    const queryRaw = this.route.snapshot.queryParamMap.get('stage');
    const raw = paramRaw ?? queryRaw;
    const stage = raw?.toLowerCase() as MaturityStageFilter;
    const next: MaturityStageFilter =
      stage && (VALID_STAGES as string[]).includes(stage) ? stage : null;
    this.store.setMaturityStageFilter(next);
  }

  onStageFilterChange(stage: MaturityStageFilter): void {
    this.store.setMaturityStageFilter(stage);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: stage ? { stage } : { stage: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  readonly chapterPanelVm = computed<ExecutionChapterPanelVm>(() => {
    const chapters = this.store.filteredChapters();
    const selectedChapterId = this.store.selectedChapterId();
    const ui = this.store.operationalModeUi();

    const items: ExecutionChapterNavItemVm[] = chapters.map((chapter) => {
      const n = chapter.openIssuesCount;
      const issuesLabel = n === 0 ? '0 Issues' : n === 1 ? '1 Issue' : `${n} Issues`;
      const passFail = ui.execution.chapterShowPassFail
        ? chapter.complianceScorePercent >= 80
          ? 'PASS'
          : 'FAIL'
        : null;
      const changed = ui.execution.chapterShowChangedSinceAudit
        ? chapter.id.charCodeAt(0) % 2 === 0
        : false;
      const maturity = ui.execution.chapterShowMaturityIndicator
        ? chapter.risk === 'low'
          ? 'Maturity: strong'
          : 'Maturity: evolving'
        : null;
      return {
        id: chapter.id,
        code: chapter.code,
        name: chapter.name,
        risk: chapter.risk,
        compliancePercent: chapter.complianceScorePercent,
        progressLabel: `${chapter.complianceScorePercent}%`,
        openIssuesCount: n,
        issueBadgeLabel: issuesLabel,
        isActive: chapter.id === selectedChapterId,
        passFailLabel: passFail,
        changedSinceLastAudit: changed,
        maturityLabel: maturity,
        highlightHighRisk: ui.execution.chapterHighlightHighRisk && chapter.risk === 'high',
      };
    });

    return {
      title: 'Chapter Navigation',
      subtitle: `${items.length} total`,
      searchPlaceholder: 'Search chapters, standards, OEs...',
      sortByRiskLabel: 'Risk',
      items,
    };
  });

  readonly standardsPanelVm = computed<ExecutionStandardsPanelVm>(() => {
    const standards = this.store.standardsForSelectedChapter();
    const selectedStandardId = this.store.selectedStandardId();
    const ch = this.store.selectedChapter();
    const ui = this.store.operationalModeUi();

    const hero = ch
      ? {
          compliancePercent: ch.complianceScorePercent,
          risk: ch.risk,
          openIssuesCount: ch.openIssuesCount,
          chapterName: ch.name,
          chapterCode: ch.code,
        }
      : null;

    const classLabel = (c: string) =>
      c === 'core' ? 'Core' : c === 'commitment' ? 'Commitment' : c === 'achievement' ? 'Achievement' : c === 'excellence' ? 'Excellence' : c;

    const items: ExecutionStandardItemVm[] = standards.map((standard) => {
      const oeItems: ExecutionOeItemVm[] = this.store
        .filteredOesForStandard(standard)
        .map((oe) => {
          const row = oeRowStatus(oe);
          const j = oeJourneyStatus(oe);
          const changed =
            ui.execution.oeShowChangedSinceAudit && oe.id.charCodeAt(0) % 2 === 0;
          const excellenceNote =
            ui.execution.oeShowExcellenceDetail && (oe.classification ?? '') === 'excellence'
              ? 'Excellence band'
              : null;
          return {
            id: oe.id,
            code: oe.code,
            title: oe.description,
            classification: classLabel(oe.classification ?? ''),
            scoreLabel: `${oe.score}/5`,
            statusLabel: row.statusLabel,
            statusTone: row.statusTone,
            evidenceCountLabel: oeEvidenceCountLabel(oe),
            isNa: oe.isNa,
            hasActionPlan: oe.actionPlans.length > 0,
            isSelected: oe.id === this.store.selectedOeId(),
            departmentLabel: oe.departmentLabel?.trim() ?? null,
            journeyStatus: j,
            journeyStatusLabel: journeyWorkflowStatusLabel(j),
            changedSinceLastAudit: ui.execution.oeShowChangedSinceAudit ? changed : undefined,
            excellenceNote,
            ...this.oeExecutionSignals(oe.id, oe),
          };
        });

      const primaryClassifications = oeItems.map((o) => o.classification).filter(Boolean);
      const dominantClass = primaryClassifications.length
        ? primaryClassifications[0]
        : 'Core';

      const mockOpenIssues = standard.objectiveElements.filter((o) => o.score <= 3).length;

      return {
        id: standard.id,
        code: standard.code,
        title: standard.title,
        description: standard.description?.trim() || undefined,
        compliancePercent: standard.completionPercent ?? 0,
        classification: dominantClass.toUpperCase(),
        oeCountLabel: `${oeItems.length} objective elements`,
        isActive: standard.id === selectedStandardId,
        objectiveElements: oeItems,
        openIssuesCount: mockOpenIssues,
        complianceStatusLabel: ui.execution.standardShowComplianceStatus
          ? (standard.completionPercent ?? 0) >= 75
            ? 'PASS'
            : 'FAIL'
          : null,
        evidenceGapLabel: ui.execution.standardShowEvidenceGaps
          ? standard.objectiveElements.some((o) => (o.evidence?.length ?? 0) === 0)
            ? 'Evidence gaps present'
            : 'Evidence complete'
          : null,
        changeTrend: ui.execution.standardShowChangeTrend
          ? ((standard.id.charCodeAt(0) % 3 === 0
              ? 'up'
              : standard.id.charCodeAt(0) % 3 === 1
                ? 'flat'
                : 'down') as 'up' | 'flat' | 'down')
          : undefined,
        previouslyFailed: ui.execution.standardShowPreviouslyFailed
          ? (standard.completionPercent ?? 0) < 60
          : false,
        excellenceBadge: ui.execution.standardShowExcellenceBadge,
      };
    });

    return {
      title: 'Standards',
      subtitle: ch ? this.chapterSubtitle(ch.name, ch.code) : 'Select a chapter',
      hero,
      items,
    };
  });

  readonly executionModeUi = computed(() => this.store.operationalModeUi());

  onChapterSelected(chapterId: string): void {
    this.store.selectChapter(chapterId);
    this.scrollToAnchorIfNarrow('execution-anchor-standards');
  }

  onStandardSelected(standardId: string): void {
    this.store.selectStandard(standardId);
  }

  onOeContextSelect(standardId: string, oeId: string): void {
    this.store.selectExecutionContext(standardId, oeId);
    this.scrollToAnchorIfNarrow('execution-anchor-context');
  }

  onContextStartJourney(): void {
    const sid = this.store.selectedStandardId();
    const oid = this.store.selectedOeId();
    if (sid && oid) {
      this.store.selectExecutionContext(sid, oid);
    }
    void this.router.navigate(['/app/journey/execution', 'Progress', oid]);
  }

  onContextUploadEvidence(): void {
    const nav = this.selectedOeNav();
    if (!nav) return;
    void this.router.navigate(['/app/evidence'], { queryParams: evidenceQueryForOe(nav) });
  }

  onContextAddTask(): void {
    const nav = this.selectedOeNav();
    if (!nav) return;
    void this.router.navigate(['/app/tasks'], { queryParams: tasksQueryForOe(nav) });
  }

  onContextViewDeficiencies(): void {
    const nav = this.selectedOeNav();
    if (!nav) return;
    void this.router.navigate(['/app/audit/capa'], {
      queryParams: deficienciesQueryForOe(nav),
    });
  }

  onContextViewTimeline(): void {
    const nav = this.selectedOeNav();
    if (!nav) return;
    void this.router.navigate(['/app/evidence/binder'], {
      queryParams: {
        objectiveElementId: nav.oeId,
        chapterId: nav.chapterId ?? undefined,
        tab: 'evidence',
      },
    });
  }

  onContextViewHistory(): void {
    const oid = this.store.selectedOeId();
    if (!oid) return;
    const found = this.store.findOe(oid);
    if (!found) return;
    openOeHistoryDialog(this.dialog, found.oe.code, found.oe.history ?? []);
  }

  onChapterSearchChange(q: string): void {
    this.store.setChapterSearch(q);
  }

  onChapterSortMode(mode: 'risk' | 'code'): void {
    this.store.setChapterSortByRiskDesc(mode === 'risk');
  }

  toggleContextDrawer(): void {
    this.contextDrawerOpen.update((v) => !v);
  }

  closeContextDrawer(): void {
    this.contextDrawerOpen.set(false);
  }

  /** Small screens only: scroll to chapter list */
  scrollToChapters(): void {
    this.scrollToAnchorIfNarrow('execution-anchor-chapters');
  }

  /** Small screens only: scroll to standards / OE list */
  scrollToStandards(): void {
    this.scrollToAnchorIfNarrow('execution-anchor-standards');
  }

  /** Avoids duplicate chapter code when name already includes it, e.g. "… (AAC) (AAC)". */
  private chapterSubtitle(name: string, code: string): string {
    const trimmedName = name.trim();
    const codeSuffix = `(${code.trim()})`;
    if (trimmedName.endsWith(codeSuffix)) {
      return `Chapter: ${trimmedName}`;
    }
    return `Chapter: ${trimmedName} ${codeSuffix}`;
  }

  /** Smooth-scroll drill-down / back navigation — max-width matches desktop breakpoint (64rem). */
  private scrollToAnchorIfNarrow(anchorId: string): void {
    if (typeof window === 'undefined') return;
    if (!window.matchMedia('(max-width: 63.99rem)').matches) return;
    const run = () =>
      document.getElementById(anchorId)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    window.setTimeout(run, 0);
  }
}
