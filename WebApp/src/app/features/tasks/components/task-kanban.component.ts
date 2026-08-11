import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CdkDrag,
  CdkDragDrop,
  CdkDropList,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import type { TaskKanbanResponse, TaskListItem } from '../models';
import { evidenceStateClass, taskRiskBadgeClass } from '../task-display.util';
import { taskHasPendingEvidenceReviews } from '../task-completion-gate.util';

type ColumnKey = 'notStarted' | 'inProgress' | 'blocked' | 'completed';

@Component({
  selector: 'app-task-kanban',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ['../tasks-tags.css'],
  template: `
    @if (loading()) {
      <p class="muted">Loading kanban…</p>
    }
    @if (kanban(); as k) {
      @if (k.truncated) {
        <p class="warn">Results truncated (server cap). Refine filters.</p>
      }
      <div class="board-scroll praman-scrollbar praman-scrollbar--panel">
        <div class="board" cdkDropListGroup>
          @for (col of columns; track col.key) {
            <div class="column">
              <header class="col-head">
                <h3>{{ col.label }}</h3>
                <div class="col-stats">
                  <span>{{ columnTasks(k, col.key).length }} tasks</span>
                  @if (overdueCount(k, col.key) > 0) {
                    <span class="col-od">{{ overdueCount(k, col.key) }} overdue</span>
                  }
                </div>
              </header>
              <div
                class="drop-list"
                cdkDropList
                [id]="col.key"
                [cdkDropListData]="columnTasks(k, col.key)"
                [cdkDropListConnectedTo]="connectedTo"
                [cdkDropListEnterPredicate]="dropEnterPredicate(col.key)"
                (cdkDropListDropped)="drop($event, col.key, k)"
              >
                @for (t of columnTasks(k, col.key); track t.id) {
                  <div class="card" cdkDrag [cdkDragData]="t" (click)="cardClick($event, t.id)">
                    <div *cdkDragPlaceholder class="card-drag-placeholder"></div>
                    <div class="card-title">{{ t.title || 'Task' }}</div>
                    <div class="card-row">
                      <span class="tag tag-oe">{{ t.objectiveElementCode }}</span>
                      <span class="tag" [ngClass]="taskRiskBadgeClass(t.riskLevel)">{{ t.riskLevel }}</span>
                    </div>
                    <div class="card-meta">
                      @if (t.isOverdue) {
                        <span class="pill-od">Overdue</span>
                      }
                      <span class="due">{{ t.dueDate | date: 'mediumDate' }}</span>
                    </div>
                    <button
                      type="button"
                      class="ev-chip"
                      [class]="evidenceStateClass(t)"
                      (click)="$event.stopPropagation(); openEvidence.emit(t.id)"
                    >
                      📎 {{ t.evidenceApprovedCount }}/{{ t.evidenceTotalCount }}
                    </button>
                    <div class="card-footer">
                      <span class="owner" [title]="t.assignedToDisplayName || t.assignedToUserId">{{
                        initials(t)
                      }}</span>
                      <div class="card-actions" (click)="$event.stopPropagation()">
                        @if (t.status !== 'Completed') {
                          <button
                            type="button"
                            class="mini"
                            [disabled]="taskHasPendingEvidenceReviews(t)"
                            [attr.title]="
                              taskHasPendingEvidenceReviews(t)
                                ? 'Evidence pending review'
                                : 'Mark complete'
                            "
                            (click)="completeTask.emit(t.id)"
                            aria-label="Complete"
                          >
                            Done
                          </button>
                        }
                        <button type="button" class="mini" (click)="openTask.emit(t.id)">Open</button>
                      </div>
                    </div>
                  </div>
                }
                @if (columnTasks(k, col.key).length === 0) {
                  <p class="empty-col">No tasks</p>
                }
              </div>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [
    `
      .board-scroll {
        overflow-x: auto;
        padding-bottom: 0.5rem;
      }
      .board {
        display: flex;
        flex-direction: row;
        gap: 0.75rem;
        align-items: flex-start;
        min-width: min(100%, 960px);
      }
      .column {
        flex: 0 0 min(240px, 85vw);
        background: var(--color-bg-surface-alt);
        border: 1px solid var(--color-border-subtle);
        border-radius: 10px;
        padding: 0.5rem;
        min-height: 160px;
      }
      .col-head h3 {
        margin: 0 0 0.35rem;
        font-size: 0.82rem;
        font-weight: 700;
        color: var(--color-text-primary);
      }
      .col-stats {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem 0.65rem;
        font-size: 0.72rem;
        color: var(--color-text-secondary);
        margin-bottom: 0.5rem;
      }
      .col-od {
        color: var(--color-status-danger);
        font-weight: 600;
      }
      .drop-list {
        min-height: 80px;
      }
      .cdk-drop-list-dragging .card:not(.cdk-drag-placeholder) {
        transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
      }
      .card {
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
        border: 1px solid var(--color-border-subtle);
        border-radius: 8px;
        padding: 0.55rem;
        margin-bottom: 0.45rem;
        cursor: grab;
        box-shadow: 0 1px 2px color-mix(in srgb, var(--color-text-primary) 6%, transparent);
      }
      .card:active {
        cursor: grabbing;
      }
      .cdk-drag-preview {
        box-shadow:
          0 8px 24px color-mix(in srgb, var(--color-text-primary) 18%, transparent),
          0 2px 6px color-mix(in srgb, var(--color-text-primary) 12%, transparent);
        border-radius: 8px;
        opacity: 0.95;
      }
      .card-drag-placeholder {
        min-height: 72px;
        border: 2px dashed color-mix(in srgb, var(--color-brand-primary) 45%, transparent);
        border-radius: 8px;
        margin-bottom: 0.45rem;
        background: color-mix(in srgb, var(--color-brand-primary) 6%, transparent);
      }
      .drop-list.cdk-drop-list-receiving {
        background: color-mix(in srgb, var(--color-brand-primary) 8%, transparent);
        border-radius: 8px;
      }
      .card-title {
        font-weight: 700;
        font-size: 0.88rem;
        line-height: 1.25;
        margin-bottom: 0.35rem;
      }
      .card-row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
        margin-bottom: 0.35rem;
      }
      .card-meta {
        display: flex;
        align-items: center;
        gap: 0.45rem;
        font-size: 0.75rem;
        color: var(--color-text-secondary);
        margin-bottom: 0.35rem;
      }
      .pill-od {
        font-size: 0.65rem;
        font-weight: 700;
        text-transform: uppercase;
        color: var(--color-status-danger);
      }
      .ev-chip {
        border: 1px solid var(--color-border-subtle);
        border-radius: 6px;
        padding: 0.15rem 0.4rem;
        font-size: 0.72rem;
        font-weight: 600;
        background: var(--color-bg-hover);
        cursor: pointer;
        margin-bottom: 0.35rem;
      }
      .ev-chip.ev-none {
        border-color: color-mix(in srgb, var(--color-status-warning) 40%, transparent);
        color: var(--color-status-warning);
      }
      .ev-chip.ev-complete {
        border-color: color-mix(in srgb, var(--color-status-success) 40%, transparent);
        color: var(--color-status-success);
      }
      .card-footer {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.35rem;
      }
      .owner {
        font-size: 0.72rem;
        font-weight: 700;
        color: var(--color-text-secondary);
        width: 1.75rem;
        height: 1.75rem;
        border-radius: 999px;
        display: inline-grid;
        place-items: center;
        background: color-mix(in srgb, var(--color-brand-primary) 14%, transparent);
        color: var(--color-brand-primary);
      }
      .card-actions {
        display: flex;
        gap: 0.25rem;
      }
      .mini {
        font-size: 0.68rem;
        font-weight: 600;
        padding: 0.15rem 0.4rem;
        border-radius: 6px;
        border: 1px solid var(--color-border-subtle);
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
        cursor: pointer;
      }
      .mini:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      .empty-col {
        margin: 0.5rem 0 0;
        font-size: 0.8rem;
        color: var(--color-text-muted);
        text-align: center;
      }
      .warn {
        color: var(--color-status-warning);
        font-size: 0.85rem;
      }
      .muted {
        color: var(--color-text-muted);
      }
    `,
  ],
})
export class TaskKanbanComponent {
  readonly kanban = input<TaskKanbanResponse | null>(null);
  readonly loading = input(false);
  readonly openTask = output<string>();
  readonly openEvidence = output<string>();
  readonly completeTask = output<string>();
  readonly columnChange = output<{ taskId: string; column: ColumnKey }>();

  readonly columns = [
    { key: 'notStarted' as const, label: 'Not started' },
    { key: 'inProgress' as const, label: 'In progress' },
    { key: 'blocked' as const, label: 'Blocked' },
    { key: 'completed' as const, label: 'Completed' },
  ];

  readonly connectedTo = this.columns.map((c) => c.key);

  readonly taskRiskBadgeClass = taskRiskBadgeClass;
  readonly evidenceStateClass = evidenceStateClass;
  readonly taskHasPendingEvidenceReviews = taskHasPendingEvidenceReviews;

  dropEnterPredicate(targetKey: ColumnKey) {
    return (drag: CdkDrag<TaskListItem>, _drop: CdkDropList<TaskListItem[]>) => {
      if (targetKey !== 'completed') return true;
      const t = drag.data;
      return !taskHasPendingEvidenceReviews(t);
    };
  }

  columnTasks(k: TaskKanbanResponse, key: ColumnKey): TaskListItem[] {
    return k[key] as TaskListItem[];
  }

  overdueCount(k: TaskKanbanResponse, key: ColumnKey): number {
    return this.columnTasks(k, key).filter((t) => t.isOverdue).length;
  }

  initials(t: TaskListItem): string {
    const n = (t.assignedToDisplayName || '').trim();
    if (!n) return (t.assignedToUserId || '?').slice(0, 2).toUpperCase();
    const parts = n.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return n.slice(0, 2).toUpperCase();
  }

  drop(ev: CdkDragDrop<TaskListItem[]>, targetKey: ColumnKey, _k: TaskKanbanResponse): void {
    if (ev.previousContainer === ev.container) {
      moveItemInArray(ev.container.data, ev.previousIndex, ev.currentIndex);
      return;
    }
    transferArrayItem(
      ev.previousContainer.data,
      ev.container.data,
      ev.previousIndex,
      ev.currentIndex,
    );
    const task = ev.container.data[ev.currentIndex];
    this.columnChange.emit({ taskId: task.id, column: targetKey });
  }

  cardClick(ev: MouseEvent, id: string): void {
    const t = ev.target as HTMLElement | null;
    if (t?.closest('button,.card-actions')) return;
    this.openTask.emit(id);
  }
}
