import { Injectable, inject, signal, computed } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { forkJoin, from, of } from 'rxjs';
import { catchError, concatMap, finalize, tap } from 'rxjs/operators';
import { map } from 'rxjs/operators';
import { TaskApiService } from './task-api.service';
import { ComplianceContextService } from '../../core/services/compliance-context.service';
import { OrganizationService } from '../../core/services/organization.service';
import { MembershipScopeService } from '../../core/services/membership-scope.service';
import { patchStatusForApi } from './task-patch.util';
import { taskHasPendingEvidenceReviews } from './task-completion-gate.util';
import type {
  FrameworkContext,
  TaskComment,
  TaskDetail,
  TaskEvidenceItem,
  TaskFilters,
  TaskHistoryEntry,
  TaskKanbanResponse,
  TaskListItem,
  TaskQuickFilter,
  TaskSummary,
  TaskDrawerTab,
} from './models';
import type { TaskApiStatus } from './task-risk.util';
import type { TaskRiskLevelApi } from './task-risk.util';

export type TasksViewType = 'table' | 'kanban' | 'risk' | 'calendar' | 'department';

@Injectable()
export class TaskStore {
  private readonly api = inject(TaskApiService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly org = inject(OrganizationService);
  private readonly membershipScope = inject(MembershipScopeService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly frameworkContext = signal<FrameworkContext | null>(null);

  readonly summary = signal<TaskSummary | null>(null);
  readonly tasks = signal<TaskListItem[]>([]);
  readonly kanban = signal<TaskKanbanResponse | null>(null);

  readonly filters = signal<TaskFilters>({});
  readonly pagination = signal({ page: 1, pageSize: 20, totalCount: 0 });

  readonly selectedTaskId = signal<string | null>(null);
  readonly drawerInitialTab = signal<TaskDrawerTab | null>(null);
  readonly drawer = signal<{ isOpen: boolean; mode: 'side' | 'fullscreen' }>({
    isOpen: false,
    mode: 'side',
  });

  readonly viewState = signal<{ type: TasksViewType }>({ type: 'table' });

  readonly selectedTaskIds = signal<Set<string>>(new Set());

  readonly assigneeOptions = signal<{ userId: string; label: string }[]>([]);

  readonly detail = signal<TaskDetail | null>(null);
  readonly evidences = signal<TaskEvidenceItem[]>([]);
  readonly comments = signal<TaskComment[]>([]);
  readonly history = signal<TaskHistoryEntry[]>([]);

  readonly loading = signal({
    summary: false,
    list: false,
    kanban: false,
    detail: false,
    evidences: false,
    comments: false,
    history: false,
    bulk: false,
  });

  readonly error = signal<string | null>(null);

  readonly riskViewSections = computed(() => {
    const bands: TaskRiskLevelApi[] = ['Critical', 'High', 'Medium', 'Low'];
    const rows = this.tasks();
    return bands.map((level) => ({
      level,
      tasks: rows
        .filter((t) => t.riskLevel === level)
        .sort((a, b) => {
          if (a.isOverdue !== b.isOverdue) return a.isOverdue ? -1 : 1;
          return b.riskScore - a.riskScore;
        }),
    }));
  });

  init(): void {
    const fw = this.complianceContext.getFrameworkId();
    const hid = this.complianceContext.getHospitalId();
    if (!fw) {
      this.error.set('Select a framework to view tasks.');
      return;
    }
    this.frameworkContext.set({ frameworkId: fw, hospitalId: hid });

    this.loadAssignees();
    this.route.queryParamMap.subscribe(() => this.syncFromRoute());

    // Department Head: default departmentId from membership when route omits it.
    this.membershipScope
      .resolveDepartmentScope$()
      .pipe(catchError(() => of({ departmentId: null, departmentName: null, isDepartmentHead: false })))
      .subscribe((scope) => {
        if (!scope.isDepartmentHead || !scope.departmentId) return;
        if (this.route.snapshot.queryParamMap.get('departmentId')) return;
        if (this.filters().departmentId) return;
        this.setFilters({ departmentId: scope.departmentId });
      });
  }

  private loadAssignees(): void {
    this.org
      .loadOrganizationDetails()
      .pipe(
        catchError(() => of(null)),
        map((o) => {
          const members = o?.members ?? [];
          const seen = new Set<string>();
          const rows: { userId: string; label: string }[] = [];
          for (const m of members) {
            const uid = m.userId?.trim();
            if (!uid || seen.has(uid)) continue;
            seen.add(uid);
            const name = [m.firstName, m.lastName].filter(Boolean).join(' ').trim();
            const label = name || m.email?.trim() || uid;
            rows.push({ userId: uid, label });
          }
          rows.sort((a, b) => a.label.localeCompare(b.label, undefined, { sensitivity: 'base' }));
          return rows;
        }),
      )
      .subscribe((rows) => this.assigneeOptions.set(rows));
  }

  syncFromRoute(): void {
    const m = this.route.snapshot.queryParamMap;
    const view = (m.get('view') as TasksViewType) || 'table';
    if (
      view === 'kanban' ||
      view === 'risk' ||
      view === 'table' ||
      view === 'calendar' ||
      view === 'department'
    ) {
      this.viewState.set({ type: view });
    }
    const page = Math.max(1, Number(m.get('page') || 1));
    const pageSize = Math.max(1, Math.min(200, Number(m.get('pageSize') || 20)));
    this.pagination.update((p) => ({ ...p, page, pageSize }));

    const filters: TaskFilters = {};
    const st = m.get('status') as TaskApiStatus | null;
    if (st) filters.status = st;
    const pr = m.get('priority');
    if (pr) filters.priority = Number(pr);
    const oid = m.get('ownerId');
    if (oid) filters.ownerId = oid;
    const did = m.get('departmentId');
    if (did) filters.departmentId = did;
    const cid = m.get('chapterId');
    if (cid) filters.chapterId = cid;
    const rl = m.get('riskLevel');
    if (rl) filters.riskLevel = rl as TaskFilters['riskLevel'];
    const search = m.get('search');
    if (search) filters.search = search;
    const category = m.get('category');
    if (category) filters.category = category as TaskFilters['category'];
    const taskKind = m.get('taskKind');
    if (taskKind) filters.taskKind = taskKind as TaskFilters['taskKind'];
    const dueFrom = m.get('dueFrom');
    if (dueFrom) filters.dueFrom = dueFrom;
    const dueTo = m.get('dueTo');
    if (dueTo) filters.dueTo = dueTo;
    const focus = m.get('focus');
    if (focus === 'true' || focus === '1') filters.focus = true;
    const quick = m.get('quick');
    if (
      quick === 'myTasks' ||
      quick === 'team' ||
      quick === 'overdue' ||
      quick === 'highPriority' ||
      quick === 'dueThisWeek' ||
      quick === 'highRisk' ||
      quick === 'focus' ||
      quick === 'criticalDeficiencies' ||
      quick === 'evidenceMissing' ||
      quick === 'verificationRejected' ||
      quick === 'coreStage' ||
      quick === 'commitmentStage'
    ) {
      filters.quick = quick as TaskQuickFilter;
    }
    if (view === 'calendar' && !filters.dueFrom && !filters.dueTo) {
      const now = new Date();
      const start = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1));
      const end = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + 1, 0));
      filters.dueFrom = start.toISOString().slice(0, 10);
      filters.dueTo = end.toISOString().slice(0, 10);
    }
    this.filters.set(filters);

    this.reloadData();
  }

  setView(type: TasksViewType): void {
    this.clearRowSelection();
    if (type === 'calendar') {
      const now = new Date();
      const start = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1));
      const end = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + 1, 0));
      this.patchQueryParams({
        view: type,
        dueFrom: start.toISOString().slice(0, 10),
        dueTo: end.toISOString().slice(0, 10),
        pageSize: '100',
      });
      return;
    }
    this.patchQueryParams({ view: type });
  }

  setFilters(patch: Partial<TaskFilters>): void {
    const prev = this.filters();
    const next: TaskFilters = { ...prev, ...patch };
    if (patch.status !== undefined && patch.status !== prev.status) delete next.quick;
    if (patch.priority !== undefined && patch.priority !== prev.priority) delete next.quick;
    if (patch.riskLevel !== undefined && patch.riskLevel !== prev.riskLevel) delete next.quick;
    if (patch.ownerId !== undefined && patch.ownerId !== prev.ownerId) delete next.quick;
    if (patch.departmentId !== undefined && patch.departmentId !== prev.departmentId) delete next.quick;
    if (patch.chapterId !== undefined && patch.chapterId !== prev.chapterId) delete next.quick;
    if (patch.focus === false) delete next.focus;

    this.filters.set(next);
    this.patchQueryParams({
      status: next.status ?? null,
      priority: next.priority != null ? String(next.priority) : null,
      ownerId: next.ownerId ?? null,
      departmentId: next.departmentId ?? null,
      chapterId: next.chapterId ?? null,
      riskLevel: next.riskLevel ?? null,
      search: next.search ?? null,
      category: next.category ?? null,
      taskKind: next.taskKind ?? null,
      dueFrom: next.dueFrom ?? null,
      dueTo: next.dueTo ?? null,
      quick: next.quick ?? null,
      focus: next.focus ? 'true' : null,
      page: '1',
    });
  }

  setQuickFilter(q: TaskQuickFilter | null): void {
    const next: TaskFilters = { ...this.filters() };
    if (q) {
      next.quick = q;
      if (q === 'highPriority') delete next.priority;
      if (q === 'myTasks' || q === 'team') delete next.ownerId;
      if (q === 'highRisk') delete next.riskLevel;
      if (q === 'focus') next.focus = true;
      else delete next.focus;
    } else {
      delete next.quick;
      delete next.focus;
    }
    this.filters.set(next);
    this.patchQueryParams({
      quick: q,
      focus: q === 'focus' ? 'true' : null,
      priority: next.priority != null ? String(next.priority) : null,
      ownerId: next.ownerId ?? null,
      riskLevel: next.riskLevel ?? null,
      page: '1',
    });
  }

  resetFilters(): void {
    this.filters.set({});
    this.pagination.update((p) => ({ ...p, page: 1 }));
    this.patchQueryParams({
      status: null,
      priority: null,
      ownerId: null,
      departmentId: null,
      chapterId: null,
      riskLevel: null,
      search: null,
      category: null,
      taskKind: null,
      dueFrom: null,
      dueTo: null,
      quick: null,
      focus: null,
      page: '1',
    });
  }

  setPage(page: number): void {
    this.patchQueryParams({ page: String(page) });
  }

  openDrawer(taskId: string, tab?: TaskDrawerTab): void {
    this.selectedTaskId.set(taskId);
    this.drawerInitialTab.set(tab ?? null);
    this.drawer.update((d) => ({ ...d, isOpen: true }));
    this.loadDrawerDetail(taskId);
  }

  closeDrawer(): void {
    this.drawer.update((d) => ({ ...d, isOpen: false }));
    this.selectedTaskId.set(null);
    this.drawerInitialTab.set(null);
    this.detail.set(null);
    this.evidences.set([]);
    this.comments.set([]);
    this.history.set([]);
  }

  setDrawerMode(mode: 'side' | 'fullscreen'): void {
    this.drawer.update((d) => ({ ...d, mode }));
  }

  toggleRowSelected(taskId: string): void {
    this.selectedTaskIds.update((s) => {
      const n = new Set(s);
      if (n.has(taskId)) n.delete(taskId);
      else n.add(taskId);
      return n;
    });
  }

  clearRowSelection(): void {
    this.selectedTaskIds.set(new Set());
  }

  loadCommentsTab(taskId: string): void {
    const ctx = this.frameworkContext();
    if (!ctx) return;
    this.loading.update((l) => ({ ...l, comments: true }));
    this.api
      .getComments(taskId, ctx.frameworkId, 1, 50)
      .pipe(
        finalize(() => this.loading.update((l) => ({ ...l, comments: false }))),
        catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 50 })),
      )
      .subscribe((r) => this.comments.set(r.items));
  }

  loadHistoryTab(taskId: string): void {
    const ctx = this.frameworkContext();
    if (!ctx) return;
    this.loading.update((l) => ({ ...l, history: true }));
    this.api
      .getHistory(taskId, ctx.frameworkId, 1, 50)
      .pipe(
        finalize(() => this.loading.update((l) => ({ ...l, history: false }))),
        catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 50 })),
      )
      .subscribe((r) => this.history.set(r.items));
  }

  completeSelected(): void {
    const id = this.selectedTaskId();
    const d = this.detail();
    const ctx = this.frameworkContext();
    if (!id || !d || !ctx) return;
    if (taskHasPendingEvidenceReviews(d)) {
      this.error.set(
        'This task has evidence pending review. Approve or reject each item before completing the task.',
      );
      return;
    }
    this.api.completeTask(id, ctx.frameworkId, d.rowVersion).subscribe({
      next: () => {
        this.loadDrawerDetail(id);
        this.reloadData();
      },
      error: (e) => this.error.set(e?.message ?? 'Complete failed'),
    });
  }

  /** Complete task from grid/kanban (uses rowVersion from cached list item). */
  completeTaskById(taskId: string): void {
    const ctx = this.frameworkContext();
    const row = this.findListItem(taskId);
    if (!ctx || !row || row.status === 'Completed') return;
    if (taskHasPendingEvidenceReviews(row)) {
      this.error.set(
        'This task has evidence pending review. Approve or reject each item before completing the task.',
      );
      return;
    }
    this.api.completeTask(taskId, ctx.frameworkId, row.rowVersion).subscribe({
      next: () => this.reloadData(),
      error: (e) => this.error.set(e?.message ?? 'Complete failed'),
    });
  }

  bulkCompleteSelection(): void {
    const ctx = this.frameworkContext();
    const ids = [...this.selectedTaskIds()];
    if (!ctx || ids.length === 0) return;
    const blocked = ids.filter((id) => {
      const row = this.findListItem(id);
      return row && taskHasPendingEvidenceReviews(row);
    });
    if (blocked.length > 0) {
      this.error.set(
        'One or more selected tasks have evidence pending review. Resolve evidence before completing.',
      );
      return;
    }
    this.loading.update((l) => ({ ...l, bulk: true }));
    const failed: string[] = [];
    from(ids)
      .pipe(
        concatMap((id) => {
          const row = this.findListItem(id);
          if (!row || row.status === 'Completed') return of(true);
          return this.api.completeTask(id, ctx.frameworkId, row.rowVersion).pipe(
            tap(() => true),
            catchError(() => {
              failed.push(id);
              return of(false);
            }),
          );
        }),
        finalize(() => {
          this.loading.update((l) => ({ ...l, bulk: false }));
          this.clearRowSelection();
          this.reloadData();
          if (failed.length) {
            this.error.set(`Could not complete ${failed.length} task(s). Refresh and try again.`);
          } else {
            this.error.set(null);
          }
        }),
      )
      .subscribe();
  }

  patchTaskFields(
    taskId: string,
    patch: { status?: TaskApiStatus; assignedToUserId?: string },
    optimisticSlice?: Partial<TaskListItem>,
  ): void {
    const ctx = this.frameworkContext();
    const row = this.findListItem(taskId);
    if (!ctx || !row) return;

    const body: Record<string, unknown> = {};
    try {
      if (patch.status != null) {
        body['status'] = patchStatusForApi(patch.status);
      }
    } catch {
      this.completeTaskById(taskId);
      return;
    }
    if (patch.assignedToUserId != null) {
      body['assignedToUserId'] = patch.assignedToUserId;
    }
    if (Object.keys(body).length === 0) return;

    if (optimisticSlice) {
      this.applyListPatch(taskId, optimisticSlice);
    }

    this.api.patchTask(taskId, ctx.frameworkId, body, row.rowVersion).subscribe({
      next: (d) => {
        this.mergeTaskFromDetail(d);
        if (this.selectedTaskId() === taskId) {
          this.detail.set(d);
        }
        this.error.set(null);
      },
      error: (e) => {
        this.reloadData();
        const msg = e?.status === 412 || e?.status === 428 ? 'Task was modified elsewhere. Refreshed.' : (e?.message ?? 'Update failed');
        this.error.set(msg);
      },
    });
  }

  uploadEvidence(file: File): void {
    const id = this.selectedTaskId();
    const ctx = this.frameworkContext();
    if (!id || !ctx) return;
    this.api.uploadEvidence(id, ctx.frameworkId, file).subscribe({
      next: () => this.loadEvidences(id),
      error: (e) => this.error.set(e?.message ?? 'Upload failed'),
    });
  }

  addComment(body: string): void {
    const id = this.selectedTaskId();
    const ctx = this.frameworkContext();
    if (!id || !ctx || !body.trim()) return;
    this.api.addComment(id, ctx.frameworkId, body.trim()).subscribe({
      next: () => this.loadCommentsTab(id),
      error: (e) => this.error.set(e?.message ?? 'Comment failed'),
    });
  }

  /** After kanban DnD: move card to column (Completed uses complete endpoint). */
  applyKanbanColumn(taskId: string, column: 'notStarted' | 'inProgress' | 'blocked' | 'completed'): void {
    if (column === 'completed') {
      const row = this.findListItem(taskId);
      if (row && taskHasPendingEvidenceReviews(row)) {
        this.error.set(
          'This task has evidence pending review. Approve or reject each item before completing the task.',
        );
        this.reloadData();
        return;
      }
      this.completeTaskById(taskId);
      return;
    }
    const status: TaskApiStatus =
      column === 'notStarted' ? 'NotStarted' : column === 'inProgress' ? 'InProgress' : 'Blocked';
    const optimistic = { status } as Partial<TaskListItem>;
    this.patchTaskFields(taskId, { status }, optimistic);
  }

  private patchQueryParams(patch: Record<string, string | null>): void {
    const current = { ...this.route.snapshot.queryParams };
    for (const [k, v] of Object.entries(patch)) {
      if (v === null || v === undefined || v === '') delete current[k];
      else current[k] = v;
    }
    void this.router.navigate([], { relativeTo: this.route, queryParams: current });
  }

  private normalizeItem(t: TaskListItem): TaskListItem {
    return {
      ...t,
      evidenceTotalCount: t.evidenceTotalCount ?? 0,
      evidenceApprovedCount: t.evidenceApprovedCount ?? 0,
      evidencePendingReviewCount: t.evidencePendingReviewCount ?? 0,
    };
  }

  /** For bulk UI: any selected row still has evidence in pending review. */
  selectionBlocksComplete(): boolean {
    for (const id of this.selectedTaskIds()) {
      const row = this.findListItem(id);
      if (row && taskHasPendingEvidenceReviews(row)) return true;
    }
    return false;
  }

  private reloadData(): void {
    const ctx = this.frameworkContext();
    if (!ctx) return;
    const view = this.viewState().type;
    const filters = this.filters();
    const { page, pageSize } = this.pagination();

    this.loading.update((l) => ({ ...l, summary: true }));
    this.api
      .getSummary(ctx.frameworkId, filters)
      .pipe(
        finalize(() => this.loading.update((l) => ({ ...l, summary: false }))),
        catchError(() => of(null)),
      )
      .subscribe((s) => this.summary.set(s));

    if (view === 'kanban') {
      this.loading.update((l) => ({ ...l, kanban: true }));
      this.api
        .getKanban(ctx.frameworkId, filters)
        .pipe(
          finalize(() => this.loading.update((l) => ({ ...l, kanban: false }))),
          catchError(() => of(null)),
        )
        .subscribe((k) => {
          if (k) {
            this.kanban.set({
              ...k,
              notStarted: k.notStarted.map((x) => this.normalizeItem(x)),
              inProgress: k.inProgress.map((x) => this.normalizeItem(x)),
              blocked: k.blocked.map((x) => this.normalizeItem(x)),
              completed: k.completed.map((x) => this.normalizeItem(x)),
            });
          } else {
            this.kanban.set(null);
          }
          this.tasks.set([]);
        });
      return;
    }

    this.loading.update((l) => ({ ...l, list: true }));
    this.kanban.set(null);
    const apiView =
      view === 'risk' ? 'risk' : view === 'calendar' ? 'calendar' : view === 'department' ? 'department' : 'table';
    this.api
      .getTasksPaged(ctx.frameworkId, filters, page, pageSize, apiView)
      .pipe(
        finalize(() => this.loading.update((l) => ({ ...l, list: false }))),
        catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize })),
      )
      .subscribe((r) => {
        this.tasks.set(r.items.map((t) => this.normalizeItem(t)));
        this.pagination.update((p) => ({ ...p, totalCount: r.totalCount, page: r.page, pageSize: r.pageSize }));
      });
  }

  private loadDrawerDetail(taskId: string): void {
    const ctx = this.frameworkContext();
    if (!ctx) return;
    this.loading.update((l) => ({ ...l, detail: true, evidences: true }));
    forkJoin({
      detail: this.api.getTaskDetail(taskId, ctx.frameworkId),
      ev: this.api.getEvidences(taskId, ctx.frameworkId, 1, 50),
    })
      .pipe(
        finalize(() => this.loading.update((l) => ({ ...l, detail: false, evidences: false }))),
        catchError(() => of(null)),
      )
      .subscribe((res) => {
        if (!res) return;
        const d = this.normalizeItem(res.detail) as TaskDetail;
        this.detail.set(d);
        this.evidences.set(res.ev.items);
        this.mergeTaskFromDetail(d);
      });
  }

  private loadEvidences(taskId: string): void {
    const ctx = this.frameworkContext();
    if (!ctx) return;
    this.api.getEvidences(taskId, ctx.frameworkId, 1, 50).subscribe((r) => this.evidences.set(r.items));
  }

  findListItem(taskId: string): TaskListItem | undefined {
    const t = this.tasks().find((x) => x.id === taskId);
    if (t) return t;
    const k = this.kanban();
    if (!k) return undefined;
    const all = [...k.notStarted, ...k.inProgress, ...k.blocked, ...k.completed];
    return all.find((x) => x.id === taskId);
  }

  private mergeTaskFromDetail(d: TaskDetail): void {
    const slice = this.normalizeItem(d) as TaskDetail;
    this.tasks.update((arr) => arr.map((t) => (t.id === d.id ? { ...t, ...slice } : t)));
    this.rebucketKanbanTask(slice);
  }

  private rebucketKanbanTask(updated: TaskListItem): void {
    const k = this.kanban();
    if (!k) return;
    const all = [...k.notStarted, ...k.inProgress, ...k.blocked, ...k.completed];
    const map = new Map<string, TaskListItem>();
    for (const t of all) {
      map.set(t.id, t);
    }
    const prev = map.get(updated.id);
    map.set(updated.id, prev ? { ...prev, ...updated } : updated);
    const list = [...map.values()];
    this.kanban.set({
      truncated: k.truncated,
      notStarted: list.filter((t) => t.status === 'NotStarted'),
      inProgress: list.filter((t) => t.status === 'InProgress'),
      blocked: list.filter((t) => t.status === 'Blocked'),
      completed: list.filter((t) => t.status === 'Completed'),
    });
  }

  private applyListPatch(taskId: string, patch: Partial<TaskListItem>): void {
    this.tasks.update((arr) => arr.map((t) => (t.id === taskId ? { ...t, ...patch } : t)));
    const k = this.kanban();
    if (!k) return;
    const all = [...k.notStarted, ...k.inProgress, ...k.blocked, ...k.completed];
    const hit = all.find((t) => t.id === taskId);
    if (!hit) return;
    this.rebucketKanbanTask({ ...hit, ...patch });
  }
}
