import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnInit,
  computed,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskStore } from './task.store';
import { TaskSummaryComponent } from './components/task-summary.component';
import { TaskFiltersComponent } from './components/task-filters.component';
import { TaskTableComponent } from './components/task-table.component';
import { TaskKanbanComponent } from './components/task-kanban.component';
import {
  TaskRiskViewComponent,
  type RiskSection,
} from './components/task-risk-view.component';
import { TaskDrawerComponent } from './components/task-drawer.component';
import { TaskQuickFiltersComponent } from './components/task-quick-filters.component';
import { TaskCalendarComponent } from './components/task-calendar.component';
import { TaskDepartmentViewComponent } from './components/task-department-view.component';
import type { TaskApiStatus } from './task-risk.util';
import { taskHasPendingEvidenceReviews } from './task-completion-gate.util';

@Component({
  selector: 'app-task-page',
  standalone: true,
  imports: [
    CommonModule,
    TaskSummaryComponent,
    TaskFiltersComponent,
    TaskTableComponent,
    TaskKanbanComponent,
    TaskRiskViewComponent,
    TaskCalendarComponent,
    TaskDepartmentViewComponent,
    TaskDrawerComponent,
    TaskQuickFiltersComponent,
  ],
  providers: [TaskStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="task-page">
      @if (store.error(); as err) {
        <div class="banner">{{ err }}</div>
      }
      @if (store.summary(); as sum) {
        @if (sum.overdue > 0) {
          <div class="overdue-banner" role="status">
            <div class="overdue-banner__icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path
                  d="M12 2L2 20h20L12 2zm0 3.99L18.51 18H5.49L12 5.99zM11 10v4h2v-4h-2zm0 6v2h2v-2h-2z"
                />
              </svg>
            </div>
            <div class="overdue-banner__copy">
              @if (sum.overdue === 1) {
                <strong>1 overdue task requires immediate attention</strong>
              } @else {
                <strong>{{ sum.overdue }} overdue tasks require immediate attention</strong>
              }
              <span class="overdue-banner__sub">Critical compliance deadlines may be at risk.</span>
            </div>
          </div>
        }
      }
      <header class="page-head">
        <h1>Tasks</h1>
        <p class="lede">Track objective elements, due dates, and evidence in one place.</p>
      </header>
      <app-task-summary [summary]="store.summary()" />
      <div class="toolbar">
        <div class="toolbar-start">
          <div class="view-toggle" role="tablist" aria-label="Task view">
            <button
              type="button"
              role="tab"
              [attr.aria-selected]="store.viewState().type === 'table'"
              [class.on]="store.viewState().type === 'table'"
              (click)="store.setView('table')"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <path d="M4 6h16M4 12h16M4 18h10" />
              </svg>
              Table
            </button>
            <button
              type="button"
              role="tab"
              [attr.aria-selected]="store.viewState().type === 'kanban'"
              [class.on]="store.viewState().type === 'kanban'"
              (click)="store.setView('kanban')"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <rect x="4" y="4" width="6" height="16" rx="1" />
                <rect x="14" y="4" width="6" height="10" rx="1" />
              </svg>
              Kanban
            </button>
            <button
              type="button"
              role="tab"
              [attr.aria-selected]="store.viewState().type === 'risk'"
              [class.on]="store.viewState().type === 'risk'"
              (click)="store.setView('risk')"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <path d="M12 3v18M3 12h18M5.6 5.6l12.8 12.8M18.4 5.6L5.6 18.4" />
              </svg>
              Risk
            </button>
            <button
              type="button"
              role="tab"
              [attr.aria-selected]="store.viewState().type === 'calendar'"
              [class.on]="store.viewState().type === 'calendar'"
              (click)="store.setView('calendar')"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <rect x="3" y="5" width="18" height="16" rx="2" />
                <path d="M3 10h18M8 3v4M16 3v4" />
              </svg>
              Calendar
            </button>
            <button
              type="button"
              role="tab"
              [attr.aria-selected]="store.viewState().type === 'department'"
              [class.on]="store.viewState().type === 'department'"
              (click)="store.setView('department')"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <path d="M4 20V8l8-4 8 4v12M9 20v-6h6v6" />
              </svg>
              Department
            </button>
          </div>
          <button
            type="button"
            class="btn-reset-filters"
            title="Reset filters"
            aria-label="Reset filters and show all tasks"
            (click)="store.resetFilters()"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
              <path
                d="M21 12a9 9 0 0 0-9-9 9.75 9.75 0 0 0-6.74 2.74L3 8M3 3v5h5M3 12a9 9 0 0 0 9 9 9.75 9.75 0 0 0 6.74-2.74L21 16M21 21v-5h-5"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </button>
        </div>
        <app-task-quick-filters
          [quick]="store.filters().quick"
          (quickChange)="store.setQuickFilter($event)"
        />
      </div>
      <app-task-filters
        [frameworkId]="store.frameworkContext()?.frameworkId ?? null"
        [model]="store.filters()"
        (filtersChange)="store.setFilters($event)"
      />
      @if (store.viewState().type === 'table') {
        <app-task-table
          [tasks]="store.tasks()"
          [loading]="store.loading().list"
          [assignees]="store.assigneeOptions()"
          [selectedIds]="store.selectedTaskIds()"
          [bulkCompleteDisabled]="store.selectionBlocksComplete()"
          (openTask)="store.openDrawer($event)"
          (openEvidence)="store.openDrawer($event, 'evidence')"
          (completeTask)="store.completeTaskById($event)"
          (toggleSelect)="store.toggleRowSelected($event)"
          (clearSelection)="store.clearRowSelection()"
          (bulkComplete)="store.bulkCompleteSelection()"
          (patchStatus)="onPatchStatus($event)"
          (patchOwner)="onPatchOwner($event)"
        />
        @if (store.pagination().totalCount > store.pagination().pageSize) {
          <div class="pager">
            <button
              type="button"
              [disabled]="store.pagination().page <= 1"
              (click)="store.setPage(store.pagination().page - 1)"
            >
              Prev
            </button>
            <span
              >Page {{ store.pagination().page }} /
              {{ ceil(store.pagination().totalCount / store.pagination().pageSize) }}</span
            >
            <button
              type="button"
              [disabled]="store.pagination().page * store.pagination().pageSize >= store.pagination().totalCount"
              (click)="store.setPage(store.pagination().page + 1)"
            >
              Next
            </button>
          </div>
        }
      }
      @if (store.viewState().type === 'kanban') {
        <app-task-kanban
          [kanban]="store.kanban()"
          [loading]="store.loading().kanban"
          (openTask)="store.openDrawer($event)"
          (openEvidence)="store.openDrawer($event, 'evidence')"
          (completeTask)="store.completeTaskById($event)"
          (columnChange)="store.applyKanbanColumn($event.taskId, $event.column)"
        />
      }
      @if (store.viewState().type === 'risk') {
        <app-task-risk-view
          [sections]="riskSections()"
          [loading]="store.loading().list"
          (open)="store.openDrawer($event)"
          (openEvidence)="store.openDrawer($event, 'evidence')"
        />
      }
      @if (store.viewState().type === 'calendar') {
        <app-task-calendar
          [tasks]="store.tasks()"
          [loading]="store.loading().list"
          (openTask)="store.openDrawer($event)"
        />
      }
      @if (store.viewState().type === 'department') {
        <app-task-department-view
          [tasks]="store.tasks()"
          [loading]="store.loading().list"
          (openTask)="store.openDrawer($event)"
        />
      }
      <app-task-drawer
        [open]="store.drawer().isOpen"
        [mode]="store.drawer().mode"
        [initialTab]="store.drawerInitialTab()"
        [detail]="store.detail()"
        [evidences]="store.evidences()"
        [comments]="store.comments()"
        [history]="store.history()"
        [historyLoading]="store.loading().history"
        (close)="store.closeDrawer()"
        (markComplete)="store.completeSelected()"
        (uploadFile)="store.uploadEvidence($event)"
        (submitComment)="onComment($event)"
        (loadComments)="onLoadComments()"
        (loadHistory)="onLoadHistory()"
        [completeDisabled]="drawerCompleteDisabled()"
      />
    </div>
  `,
  styles: [
    `
      .task-page {
        min-height: 100vh;
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        box-sizing: border-box;
        color: var(--color-text-primary);
      }
      .page-head {
        margin-bottom: 1rem;
      }
      .page-head h1 {
        margin: 0 0 0.25rem;
        font-size: var(--font-size-page-title);
        font-weight: 700;
        letter-spacing: -0.02em;
        color: var(--color-text-primary);
      }
      .lede {
        margin: 0;
        font-size: 0.9rem;
        color: var(--color-text-secondary);
        max-width: 42rem;
      }
      .overdue-banner {
        display: flex;
        align-items: flex-start;
        gap: 0.875rem;
        padding: 0.875rem 1rem;
        margin-bottom: 1.25rem;
        border-radius: 10px;
        border: 1px solid color-mix(in srgb, var(--color-status-danger) 35%, transparent);
        border-left-width: 4px;
        border-left-color: var(--color-status-danger);
        background: color-mix(in srgb, var(--color-status-danger) 12%, var(--color-bg-surface));
      }
      .overdue-banner__icon {
        flex-shrink: 0;
        width: 1.75rem;
        height: 1.75rem;
        color: var(--color-status-danger);
      }
      .overdue-banner__icon svg {
        width: 100%;
        height: 100%;
        display: block;
      }
      .overdue-banner__copy {
        display: flex;
        flex-direction: column;
        gap: 0.2rem;
        min-width: 0;
      }
      .overdue-banner__copy strong {
        font-size: 0.9rem;
        color: var(--color-text-primary);
      }
      .overdue-banner__sub {
        font-size: 0.8rem;
        color: var(--color-text-secondary);
      }
      .banner {
        background: color-mix(in srgb, var(--color-status-danger) 14%, var(--color-bg-surface));
        color: var(--color-status-danger);
        padding: 0.5rem 0.75rem;
        border-radius: 6px;
        margin-bottom: 1rem;
        border: 1px solid color-mix(in srgb, var(--color-status-danger) 28%, transparent);
      }
      .toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        flex-wrap: wrap;
        margin-bottom: 0.75rem;
      }
      .toolbar-start {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
      }
      .btn-reset-filters {
        display: inline-grid;
        place-items: center;
        width: 2.45rem;
        height: 2.45rem;
        flex-shrink: 0;
        padding: 0;
        border: 1px solid var(--color-border-subtle);
        border-radius: 8px;
        background: color-mix(in srgb, var(--color-bg-surface-alt) 65%, var(--color-bg-page));
        color: var(--color-text-secondary);
        cursor: pointer;
      }
      .btn-reset-filters:hover {
        color: var(--color-brand-primary);
        background: var(--color-bg-hover);
        border-color: color-mix(in srgb, var(--color-brand-primary) 35%, var(--color-border-subtle));
      }
      .btn-reset-filters:focus-visible {
        outline: none;
        box-shadow: var(--shadow-focus-ring-primary);
      }
      .btn-reset-filters svg {
        width: 1.15rem;
        height: 1.15rem;
      }
      .view-toggle {
        display: inline-flex;
        gap: 0.25rem;
        padding: 0.2rem;
        background: color-mix(in srgb, var(--color-bg-surface-alt) 65%, var(--color-bg-page));
        border: 1px solid var(--color-border-subtle);
        border-radius: 10px;
      }
      .view-toggle button {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        border: 1px solid transparent;
        background: transparent;
        color: var(--color-text-secondary);
        padding: 0.45rem 0.85rem;
        border-radius: 8px;
        cursor: pointer;
        font-size: 0.85rem;
        font-weight: 600;
      }
      .view-toggle button svg {
        width: 1rem;
        height: 1rem;
        opacity: 0.85;
      }
      .view-toggle button.on {
        background: color-mix(in srgb, var(--color-brand-primary) 18%, var(--color-bg-surface));
        color: var(--color-text-primary);
        border-color: color-mix(in srgb, var(--color-brand-primary) 35%, transparent);
      }
      .view-toggle button.on svg {
        color: var(--color-brand-primary);
        opacity: 1;
      }
      .view-toggle button:hover:not(.on) {
        color: var(--color-text-primary);
        background: var(--color-bg-hover);
      }
      .pager {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        margin-top: 1rem;
      }
      .pager button {
        padding: 0.4rem 0.9rem;
        border-radius: 8px;
        border: 1px solid var(--color-border-subtle);
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
        cursor: pointer;
        font-weight: 500;
      }
      .pager button:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }
      .pager span {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }
    `,
  ],
})
export class TaskPageComponent implements OnInit {
  readonly store = inject(TaskStore);

  readonly drawerCompleteDisabled = computed(() => {
    const d = this.store.detail();
    return !!d && taskHasPendingEvidenceReviews(d);
  });

  readonly riskSections = computed<RiskSection[]>(() =>
    this.store.riskViewSections().map((s) => ({
      level: s.level,
      label:
        s.level === 'Critical'
          ? 'Critical'
          : s.level === 'High'
            ? 'High'
            : s.level === 'Medium'
              ? 'Medium'
              : 'Low',
      tasks: s.tasks,
    })),
  );

  ngOnInit(): void {
    this.store.init();
    this.updateDrawerMode();
  }

  @HostListener('window:resize')
  onResize(): void {
    this.updateDrawerMode();
  }

  ceil(n: number): number {
    return Math.max(1, Math.ceil(n));
  }

  onComment(body: string): void {
    this.store.addComment(body);
  }

  onLoadComments(): void {
    const id = this.store.selectedTaskId();
    if (id) this.store.loadCommentsTab(id);
  }

  onLoadHistory(): void {
    const id = this.store.selectedTaskId();
    if (id) this.store.loadHistoryTab(id);
  }

  onPatchStatus(e: { taskId: string; status: TaskApiStatus }): void {
    if (e.status === 'Completed') {
      const row = this.store.findListItem(e.taskId);
      if (row && taskHasPendingEvidenceReviews(row)) {
        this.store.error.set(
          'This task has evidence pending review. Approve or reject each item before completing the task.',
        );
        return;
      }
      this.store.completeTaskById(e.taskId);
      return;
    }
    this.store.patchTaskFields(e.taskId, { status: e.status }, { status: e.status });
  }

  onPatchOwner(e: { taskId: string; assignedToUserId: string }): void {
    const opt = this.store.assigneeOptions().find((o) => o.userId === e.assignedToUserId);
    this.store.patchTaskFields(
      e.taskId,
      { assignedToUserId: e.assignedToUserId },
      {
        assignedToUserId: e.assignedToUserId,
        assignedToDisplayName: opt?.label ?? null,
      },
    );
  }

  private updateDrawerMode(): void {
    const full = typeof window !== 'undefined' && window.innerWidth < 1024;
    this.store.setDrawerMode(full ? 'fullscreen' : 'side');
  }
}
