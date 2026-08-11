import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, finalize, forkJoin, of, tap } from 'rxjs';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { EvidenceBinderApiService } from './evidence-binder-api.service';
import type {
  BinderDrawerMode,
  BinderTab,
  EvidenceBinderAiMappingItem,
  EvidenceBinderChapter,
  EvidenceBinderEvidenceItem,
  EvidenceBinderObjectiveElement,
  EvidenceBinderOverview,
  EvidenceBinderSummary,
  EvidenceBinderChecklistItem,
  OeHistoryEvent,
} from './models';

const DRAWER_FULLSCREEN_MAX = 1023;
const DEFAULT_EVIDENCE_PAGE_SIZE = 20;

@Injectable({ providedIn: 'root' })
export class EvidenceBinderStore {
  private readonly api = inject(EvidenceBinderApiService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  readonly frameworkId = signal<string | null>(null);

  readonly summary = signal<EvidenceBinderSummary | null>(null);
  readonly chapters = signal<EvidenceBinderChapter[]>([]);
  readonly objectives = signal<EvidenceBinderObjectiveElement[]>([]);

  readonly selectedChapterId = signal<string | null>(null);
  readonly selectedObjectiveElementId = signal<string | null>(null);

  readonly drawerOpen = signal(false);
  readonly drawerMode = signal<BinderDrawerMode>('side');
  /** Plan-aligned composite for layout/analytics. */
  readonly drawerState = computed(() => ({ isOpen: this.drawerOpen(), mode: this.drawerMode() }));
  readonly activeTab = signal<BinderTab>('evidence');

  readonly overview = signal<EvidenceBinderOverview | null>(null);
  readonly evidences = signal<EvidenceBinderEvidenceItem[]>([]);
  readonly evidencesTotalCount = signal(0);
  readonly evidencesPage = signal(1);
  readonly evidencesPageSize = signal(DEFAULT_EVIDENCE_PAGE_SIZE);

  readonly checklist = signal<EvidenceBinderChecklistItem[] | null>(null);
  readonly checklistLoaded = signal(false);

  readonly aiMapping = signal<EvidenceBinderAiMappingItem[] | null>(null);
  readonly aiMappingLoaded = signal(false);

  readonly history = signal<OeHistoryEvent[] | null>(null);
  readonly historyLoaded = signal(false);

  readonly summaryLoading = signal(false);
  readonly chaptersLoading = signal(false);
  readonly objectivesLoading = signal(false);
  readonly drawerPrefetchLoading = signal(false);
  readonly evidencesLoading = signal(false);
  readonly checklistLoading = signal(false);
  readonly aiMappingLoading = signal(false);
  readonly historyLoading = signal(false);
  readonly uploadLoading = signal(false);

  readonly errorMessage = signal<string | null>(null);

  readonly selectedChapter = computed(() => {
    const id = this.selectedChapterId();
    if (!id) return null;
    return this.chapters().find((c) => c.chapterId === id) ?? null;
  });

  readonly selectedObjective = computed(() => {
    const id = this.selectedObjectiveElementId();
    if (!id) return null;
    return this.objectives().find((o) => o.objectiveElementId === id) ?? null;
  });

  readonly readinessCards = computed(() => {
    const s = this.summary();
    if (!s) return [];
    return [
      { label: 'Total Objectives', value: s.totalObjectives, color: 'blue' as const },
      { label: 'With Evidence', value: s.objectivesWithEvidence, color: 'green' as const },
      { label: 'Missing Evidence', value: s.objectivesMissingEvidence, color: 'red' as const },
      { label: 'Approved Evidence', value: s.approvedEvidenceCount, color: 'green' as const },
      { label: 'Evidence Readiness', value: `${s.readinessPercent}%`, color: 'blue' as const },
    ];
  });

  initFromContext(): void {
    this.frameworkId.set(this.complianceContext.getFrameworkId());
    this.refreshDrawerMode();
  }

  refreshDrawerMode(): void {
    const mode: BinderDrawerMode = typeof window !== 'undefined' && window.innerWidth <= DRAWER_FULLSCREEN_MAX ? 'fullscreen' : 'side';
    this.drawerMode.set(mode);
  }

  /** Call from page when query params change. */
  applyRouteParams(params: { chapterId?: string | null; objectiveElementId?: string | null; tab?: string | null }): void {
    const t = params.tab;
    const tab: BinderTab =
      t === 'checklist' || t === 'history' || t === 'ai-mapping' ? t : 'evidence';
    this.activeTab.set(tab);

    const ch = params.chapterId ?? null;
    const oe = params.objectiveElementId ?? null;

    if (ch !== this.selectedChapterId()) {
      this.selectedChapterId.set(ch);
      if (ch) {
        this.loadObjectives(ch);
      } else {
        this.objectives.set([]);
      }
    }

    if (oe !== this.selectedObjectiveElementId()) {
      this.selectedObjectiveElementId.set(oe);
      if (oe) {
        this.drawerOpen.set(true);
        this.prefetchOverviewAndEvidences(oe);
        this.checklistLoaded.set(false);
        this.checklist.set(null);
        this.aiMappingLoaded.set(false);
        this.aiMapping.set(null);
        this.historyLoaded.set(false);
        this.history.set(null);
      } else {
        this.drawerOpen.set(false);
        this.overview.set(null);
        this.evidences.set([]);
      }
    } else if (oe && !this.drawerOpen()) {
      this.drawerOpen.set(true);
      this.prefetchOverviewAndEvidences(oe);
    }

    if (oe && this.drawerOpen()) {
      if (tab === 'checklist') this.loadChecklistIfNeeded();
      if (tab === 'ai-mapping') this.loadAiMappingIfNeeded();
      if (tab === 'history') this.loadHistoryIfNeeded();
    }
  }

  loadSummaryAndChapters(): void {
    const fid = this.frameworkId();
    if (!fid) {
      this.errorMessage.set('Select a framework in context before opening the Evidence Binder.');
      return;
    }

    this.errorMessage.set(null);
    this.summaryLoading.set(true);
    this.chaptersLoading.set(true);

    forkJoin({
      summary: this.api.getSummary(fid).pipe(
        tap({ error: (e) => this.snackBar.open(this.errText('Summary', e), 'Dismiss', { duration: 6000 }) }),
        catchError(() => of(null)),
      ),
      chapters: this.api.getChapters(fid).pipe(
        tap({ error: (e) => this.snackBar.open(this.errText('Chapters', e), 'Dismiss', { duration: 6000 }) }),
        catchError(() => of(null)),
      ),
    })
      .pipe(
        finalize(() => {
          this.summaryLoading.set(false);
          this.chaptersLoading.set(false);
        }),
      )
      .subscribe((r) => {
        if (r.summary) this.summary.set(r.summary);
        if (r.chapters) this.chapters.set(r.chapters);
      });
  }

  selectChapter(chapterId: string): void {
    this.navigateQuery({ chapterId, objectiveElementId: null, tab: 'evidence' });
  }

  selectObjective(objectiveElementId: string): void {
    const ch = this.selectedChapterId();
    if (!ch) return;
    this.navigateQuery({ chapterId: ch, objectiveElementId, tab: 'evidence' });
  }

  closeDrawer(): void {
    const ch = this.selectedChapterId();
    this.navigateQuery({ chapterId: ch, objectiveElementId: null, tab: 'evidence' });
  }

  setTab(tab: BinderTab): void {
    const ch = this.selectedChapterId();
    const oe = this.selectedObjectiveElementId();
    if (!ch || !oe) return;
    this.navigateQuery({ chapterId: ch, objectiveElementId: oe, tab });
    if (tab === 'checklist') this.loadChecklistIfNeeded();
    if (tab === 'ai-mapping') this.loadAiMappingIfNeeded();
    if (tab === 'history') this.loadHistoryIfNeeded();
  }

  loadObjectives(chapterId: string): void {
    const fid = this.frameworkId();
    if (!fid) return;
    this.objectivesLoading.set(true);
    this.api
      .getObjectivesForChapter(chapterId, fid)
      .pipe(
        catchError((e) => {
          this.snackBar.open(this.errText('Objectives', e), 'Dismiss', { duration: 6000 });
          return of<EvidenceBinderObjectiveElement[]>([]);
        }),
        finalize(() => this.objectivesLoading.set(false)),
      )
      .subscribe((rows) => this.objectives.set(rows));
  }

  prefetchOverviewAndEvidences(objectiveElementId: string): void {
    const fid = this.frameworkId();
    if (!fid) return;
    this.drawerPrefetchLoading.set(true);
    this.evidencesLoading.set(true);
    this.evidencesPage.set(1);

    forkJoin({
      overview: this.api.getOverview(objectiveElementId, fid).pipe(
        tap({ error: (e) => this.snackBar.open(this.errText('Overview', e), 'Dismiss', { duration: 6000 }) }),
        catchError(() => of(null)),
      ),
      page: this.api.getEvidences(objectiveElementId, 1, this.evidencesPageSize(), fid).pipe(
        tap({ error: (e) => this.snackBar.open(this.errText('Evidences', e), 'Dismiss', { duration: 6000 }) }),
        catchError(() => of(null)),
      ),
    })
      .pipe(
        finalize(() => {
          this.drawerPrefetchLoading.set(false);
          this.evidencesLoading.set(false);
        }),
      )
      .subscribe((r) => {
        if (r.overview) this.overview.set(r.overview);
        if (r.page) {
          this.evidences.set(r.page.items ?? []);
          this.evidencesTotalCount.set(r.page.totalCount ?? 0);
          this.evidencesPage.set(r.page.page ?? 1);
          this.evidencesPageSize.set(r.page.pageSize ?? DEFAULT_EVIDENCE_PAGE_SIZE);
        }
      });
  }

  loadEvidencesPage(page: number): void {
    const fid = this.frameworkId();
    const oe = this.selectedObjectiveElementId();
    if (!fid || !oe) return;
    this.evidencesLoading.set(true);
    this.api
      .getEvidences(oe, page, this.evidencesPageSize(), fid)
      .pipe(
        catchError((e) => {
          this.snackBar.open(this.errText('Evidences', e), 'Dismiss', { duration: 6000 });
          return of(null);
        }),
        finalize(() => this.evidencesLoading.set(false)),
      )
      .subscribe((p) => {
        if (!p) return;
        this.evidences.set(p.items);
        this.evidencesTotalCount.set(p.totalCount);
        this.evidencesPage.set(p.page);
      });
  }

  loadChecklistIfNeeded(): void {
    if (this.checklistLoaded()) return;
    const fid = this.frameworkId();
    const oe = this.selectedObjectiveElementId();
    if (!fid || !oe) return;
    this.checklistLoading.set(true);
    this.api
      .getChecklist(oe, fid)
      .pipe(
        catchError((e) => {
          this.snackBar.open(this.errText('Checklist', e), 'Dismiss', { duration: 6000 });
          return of<EvidenceBinderChecklistItem[]>([]);
        }),
        finalize(() => this.checklistLoading.set(false)),
      )
      .subscribe((items) => {
        this.checklist.set(items);
        this.checklistLoaded.set(true);
      });
  }

  loadAiMappingIfNeeded(): void {
    if (this.aiMappingLoaded()) return;
    const fid = this.frameworkId();
    const oe = this.selectedObjectiveElementId();
    if (!fid || !oe) return;
    this.aiMappingLoading.set(true);
    this.api
      .getAiMapping(oe, fid)
      .pipe(
        catchError((e) => {
          this.snackBar.open(this.errText('AI mapping', e), 'Dismiss', { duration: 6000 });
          return of<EvidenceBinderAiMappingItem[]>([]);
        }),
        finalize(() => this.aiMappingLoading.set(false)),
      )
      .subscribe((items) => {
        this.aiMapping.set(items);
        this.aiMappingLoaded.set(true);
      });
  }

  loadHistoryIfNeeded(): void {
    if (this.historyLoaded()) return;
    const fid = this.frameworkId();
    const oe = this.selectedObjectiveElementId();
    if (!fid || !oe) return;
    this.historyLoading.set(true);
    this.api
      .getHistory(oe, fid)
      .pipe(
        catchError((e) => {
          this.snackBar.open(this.errText('History', e), 'Dismiss', { duration: 6000 });
          return of<OeHistoryEvent[]>([]);
        }),
        finalize(() => this.historyLoading.set(false)),
      )
      .subscribe((items) => {
        this.history.set(items);
        this.historyLoaded.set(true);
      });
  }

  uploadEvidenceFile(file: File): void {
    const fid = this.frameworkId();
    const oe = this.selectedObjectiveElementId();
    if (!fid || !oe || !file) return;
    this.uploadLoading.set(true);
    this.api
      .postEvidence(oe, file, fid)
      .pipe(
        catchError((e) => {
          this.snackBar.open(this.errText('Upload', e), 'Dismiss', { duration: 8000 });
          return of(null);
        }),
        finalize(() => this.uploadLoading.set(false)),
      )
      .subscribe((res) => {
        if (!res) return;
        this.snackBar.open('Evidence uploaded.', 'OK', { duration: 4000 });
        this.prefetchOverviewAndEvidences(oe);
        this.loadSummaryAndChapters();
        if (this.selectedChapterId()) {
          this.loadObjectives(this.selectedChapterId()!);
        }
      });
  }

  private navigateQuery(q: { chapterId: string | null; objectiveElementId: string | null; tab: BinderTab }): void {
    void this.router.navigate(['/app/evidence/binder'], {
      queryParams: {
        chapterId: q.chapterId || undefined,
        objectiveElementId: q.objectiveElementId || undefined,
        tab: q.tab === 'evidence' ? undefined : q.tab,
      },
      replaceUrl: true,
    });
  }

  private errText(label: string, err: unknown): string {
    const e = err as { error?: { message?: string }; message?: string };
    return `${label} failed: ${e?.error?.message ?? e?.message ?? 'Unknown error'}`;
  }
}
