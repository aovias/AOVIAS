import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import type { TaskListItem } from '../models';
import type { TaskApiStatus } from '../task-risk.util';
import {
  evidenceStateClass,
  isHighRiskRow,
  taskPriorityBadgeClass,
  taskRiskBadgeClass,
  taskStatusBadgeClass,
  taskStatusLabel,
} from '../task-display.util';
import { taskHasPendingEvidenceReviews } from '../task-completion-gate.util';

@Component({
  selector: 'app-task-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ['../tasks-tags.css'],
  template: `
    @if (tasks().length === 0 && !loading()) {
      <p class="muted">No tasks match filters.</p>
    }
    @if (loading()) {
      <p class="muted">Loading…</p>
    }
    @if (selectedIds().size > 0) {
      <div class="bulk-bar" role="toolbar" aria-label="Bulk actions">
        <span class="bulk-bar__count">{{ selectedIds().size }} selected</span>
        <button
          type="button"
          class="btn-bulk"
          [disabled]="bulkCompleteDisabled()"
          [attr.title]="
            bulkCompleteDisabled()
              ? 'One or more selected tasks have evidence pending review.'
              : null
          "
          (click)="bulkComplete.emit()"
        >
          Complete selected
        </button>
        <button type="button" class="btn-bulk btn-bulk--ghost" (click)="clearSelection.emit()">
          Clear selection
        </button>
      </div>
    }
    @if (tasks().length) {
      <div class="table-scroll praman-scrollbar praman-scrollbar--panel">
        <table>
          <thead>
            <tr>
              <th class="th-check"></th>
              <th>OE</th>
              <th>Title</th>
              <th>Status</th>
              <th>Priority</th>
              <th>Risk</th>
              <th>Evidence</th>
              <th>Due</th>
              <th>Owner</th>
              <th class="th-actions">Actions</th>
            </tr>
          </thead>
          <tbody>
            @for (t of tasks(); track t.id) {
              <tr
                class="data-row"
                [class.row-overdue]="t.isOverdue"
                [class.row-high-risk]="isHighRiskRow(t)"
                [class.row-selected]="selectedIds().has(t.id)"
                (click)="onRowClick($event, t.id)"
              >
                <td class="td-check" (click)="$event.stopPropagation()">
                  <label class="row-check">
                    <input
                      type="checkbox"
                      class="row-check__input"
                      [checked]="selectedIds().has(t.id)"
                      (change)="toggleSelect.emit(t.id)"
                      [attr.aria-label]="'Select task ' + (t.title || t.objectiveElementCode)"
                    />
                    <span class="row-check__box" aria-hidden="true"></span>
                  </label>
                </td>
                <td>
                  <span class="tag tag-oe">{{ t.objectiveElementCode }}</span>
                </td>
                <td class="cell-title">{{ t.title || '—' }}</td>
                <td (click)="$event.stopPropagation()">
                  <select
                    class="inline-select"
                    [ngModel]="t.status"
                    (ngModelChange)="patchStatus.emit({ taskId: t.id, status: $event })"
                  >
                    <option value="NotStarted">Not started</option>
                    <option value="InProgress">In progress</option>
                    <option value="Blocked">Blocked</option>
                    <option value="Completed" [disabled]="taskHasPendingEvidenceReviews(t)">Completed</option>
                  </select>
                </td>
                <td>
                  <span class="tag" [ngClass]="taskPriorityBadgeClass(t.priority)">{{ t.priority | uppercase }}</span>
                </td>
                <td>
                  <span class="tag" [ngClass]="taskRiskBadgeClass(t.riskLevel)">
                    {{ t.riskLevel }}
                    <span class="risk-num">({{ t.riskScore }})</span>
                  </span>
                </td>
                <td (click)="$event.stopPropagation()">
                  <button
                    type="button"
                    class="evidence-btn"
                    [class]="evidenceStateClass(t)"
                    (click)="openEvidence.emit(t.id)"
                    [attr.aria-label]="'Evidence ' + t.evidenceApprovedCount + ' of ' + t.evidenceTotalCount"
                  >
                    <span class="ev-ic" aria-hidden="true">📎</span>
                    <span class="ev-count">{{ t.evidenceApprovedCount }}/{{ t.evidenceTotalCount }}</span>
                  </button>
                </td>
                <td>
                  @if (t.isOverdue && t.dueDate) {
                    <span class="due-wrap overdue">
                      <svg class="due-ic" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                        <path
                          d="M12 2L2 20h20L12 2zm0 3.99L18.51 18H5.49L12 5.99zM11 10v4h2v-4h-2zm0 6v2h2v-2h-2z"
                        />
                      </svg>
                      {{ t.dueDate | date: 'mediumDate' }}
                    </span>
                  } @else {
                    <span class="due-wrap">{{ t.dueDate | date: 'mediumDate' }}</span>
                  }
                </td>
                <td (click)="$event.stopPropagation()">
                  <select
                    class="inline-select inline-select--wide"
                    [ngModel]="t.assignedToUserId"
                    (ngModelChange)="patchOwner.emit({ taskId: t.id, assignedToUserId: $event })"
                  >
                    @if (t.assignedToUserId && !hasAssigneeOption(t.assignedToUserId)) {
                      <option [ngValue]="t.assignedToUserId">{{
                        t.assignedToDisplayName || t.assignedToUserId
                      }}</option>
                    }
                    @for (o of assignees(); track o.userId) {
                      <option [ngValue]="o.userId">{{ o.label }}</option>
                    }
                  </select>
                </td>
                <td class="td-actions" (click)="$event.stopPropagation()">
                  <div class="row-actions">
                    <button
                      type="button"
                      class="btn-icon"
                      (click)="openTask.emit(t.id)"
                      title="View"
                      aria-label="View details"
                    >
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                        <path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7-11-7-11-7z" />
                        <circle cx="12" cy="12" r="3" />
                      </svg>
                    </button>
                    @if (t.status !== 'Completed') {
                      <button
                        type="button"
                        class="btn-icon"
                        [disabled]="taskHasPendingEvidenceReviews(t)"
                        [attr.title]="
                          taskHasPendingEvidenceReviews(t)
                            ? 'Evidence pending review — complete after approval'
                            : 'Complete'
                        "
                        (click)="completeTask.emit(t.id)"
                        aria-label="Mark complete"
                      >
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                          <path d="M20 6L9 17l-5-5" />
                        </svg>
                      </button>
                    }
                    <button
                      type="button"
                      class="btn-icon"
                      (click)="openEvidence.emit(t.id)"
                      title="Evidence"
                      aria-label="Open evidence"
                    >
                      <span class="clip" aria-hidden="true">📎</span>
                    </button>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
  styles: [
    `
      .bulk-bar {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        flex-wrap: wrap;
        padding: 0.65rem 0.85rem;
        margin-bottom: 0.65rem;
        position: sticky;
        top: 0;
        z-index: 3;
        border-radius: 10px;
        border: 1px solid var(--color-border-subtle);
        background: color-mix(in srgb, var(--color-brand-primary) 10%, var(--color-bg-surface));
      }
      .bulk-bar__count {
        font-size: 0.85rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin-right: 0.25rem;
      }
      .btn-bulk {
        padding: 0.4rem 0.85rem;
        border-radius: 8px;
        border: 1px solid var(--color-border-subtle);
        background: var(--color-brand-primary);
        color: var(--color-text-inverse);
        font-weight: 600;
        font-size: 0.8rem;
        cursor: pointer;
      }
      .btn-bulk--ghost {
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
      }
      .btn-bulk:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      .btn-icon:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }
      .table-scroll {
        overflow: auto;
        max-height: min(70vh, 720px);
        color: var(--color-text-primary);
        border: 1px solid var(--color-border-subtle);
        border-radius: 12px;
        background: color-mix(in srgb, var(--color-bg-surface) 88%, var(--color-bg-page));
      }
      table {
        width: 100%;
        border-collapse: separate;
        border-spacing: 0;
        font-size: 0.875rem;
      }
      thead th {
        position: sticky;
        top: 0;
        z-index: 2;
        color: var(--color-text-secondary);
        font-weight: 700;
        font-size: 0.65rem;
        letter-spacing: 0.07em;
        text-transform: uppercase;
        text-align: center;
        padding: var(--space-2) var(--table-cell-padding-inline);
        border-bottom: 1px solid var(--color-border-subtle);
        background: color-mix(in srgb, var(--color-bg-surface-alt) 92%, var(--color-bg-page));
        box-shadow: 0 1px 0 var(--color-border-subtle);
      }
      tbody td {
        text-align: center;
        padding: var(--table-cell-padding-block) var(--table-cell-padding-inline);
        border-bottom: 1px solid var(--color-border-subtle);
        vertical-align: middle;
      }
      tbody tr.data-row {
        cursor: pointer;
      }
      tbody tr:nth-child(even) td {
        background: color-mix(in srgb, var(--color-bg-surface-alt) 18%, transparent);
      }
      tbody tr.row-selected td {
        background: color-mix(in srgb, var(--color-brand-primary) 8%, var(--color-bg-surface)) !important;
      }
      tbody tr.row-overdue td {
        box-shadow: inset 3px 0 0 var(--color-status-danger);
      }
      tbody tr.row-high-risk td {
        background: color-mix(in srgb, var(--color-status-warning) 7%, var(--color-bg-surface)) !important;
      }
      tbody tr.row-high-risk.row-overdue td {
        box-shadow: inset 3px 0 0 var(--color-status-danger);
      }
      .cell-title {
        font-weight: 700;
        color: var(--color-text-primary);
        max-width: 240px;
        margin-left: auto;
        margin-right: auto;
        text-align: center;
      }
      .th-check,
      .td-check {
        width: 2.5rem;
        text-align: center;
      }
      .row-check {
        position: relative;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        vertical-align: middle;
        cursor: pointer;
      }
      .row-check__input {
        position: absolute;
        opacity: 0;
        width: 1.35rem;
        height: 1.35rem;
        margin: 0;
        cursor: pointer;
        z-index: 1;
      }
      .row-check__input:focus-visible + .row-check__box {
        box-shadow: 0 0 0 2px var(--color-bg-surface), 0 0 0 4px var(--color-brand-primary);
      }
      .row-check__box {
        display: inline-block;
        width: 1.05rem;
        height: 1.05rem;
        border-radius: 5px;
        border: 2px solid color-mix(in srgb, var(--color-text-muted) 55%, var(--color-border-subtle));
        background: linear-gradient(180deg, var(--color-bg-surface) 0%, color-mix(in srgb, var(--color-bg-surface-alt) 35%, var(--color-bg-surface)) 100%);
        box-shadow: inset 0 1px 2px color-mix(in srgb, var(--color-text-primary) 6%, transparent);
        transition: border-color 0.12s ease, background 0.12s ease, box-shadow 0.12s ease;
      }
      .row-check__input:checked + .row-check__box {
        border-color: var(--color-brand-primary);
        background: var(--color-brand-primary);
        box-shadow: inset 0 -1px 0 color-mix(in srgb, var(--color-text-primary) 15%, transparent);
      }
      .row-check__input:checked + .row-check__box::after {
        content: '';
        display: block;
        width: 0.28rem;
        height: 0.55rem;
        margin: 0.1rem auto 0;
        border: solid var(--color-text-inverse);
        border-width: 0 2px 2px 0;
        transform: rotate(45deg);
      }
      .row-check__input:hover + .row-check__box {
        border-color: color-mix(in srgb, var(--color-brand-primary) 45%, var(--color-border-subtle));
      }
      .th-actions,
      .td-actions {
        text-align: center;
        width: 7.5rem;
      }
      .row-actions {
        display: inline-flex;
        gap: 0.2rem;
        justify-content: center;
        align-items: center;
      }
      .inline-select {
        max-width: 8.5rem;
        font-size: 0.8rem;
        padding: 0.25rem 0.35rem;
        border-radius: 6px;
        border: 1px solid var(--color-border-subtle);
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
      }
      .inline-select--wide {
        max-width: 10rem;
      }
      .evidence-btn {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        border: 1px solid var(--color-border-subtle);
        border-radius: 8px;
        padding: 0.25rem 0.5rem;
        background: var(--color-bg-surface);
        cursor: pointer;
        font-size: 0.78rem;
        font-weight: 600;
      }
      .evidence-btn.ev-none {
        border-color: color-mix(in srgb, var(--color-status-warning) 45%, transparent);
        color: var(--color-status-warning);
      }
      .evidence-btn.ev-partial {
        color: var(--color-text-secondary);
      }
      .evidence-btn.ev-complete {
        border-color: color-mix(in srgb, var(--color-status-success) 45%, transparent);
        color: var(--color-status-success);
      }
      .ev-ic {
        font-size: 0.85rem;
        line-height: 1;
      }
      .ev-count {
        font-variant-numeric: tabular-nums;
      }
      .due-wrap {
        color: var(--color-text-secondary);
        font-variant-numeric: tabular-nums;
      }
      .due-wrap.overdue {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        color: var(--color-status-danger);
        font-weight: 600;
      }
      .due-ic {
        width: 1rem;
        height: 1rem;
        flex-shrink: 0;
        opacity: 0.95;
      }
      .btn-icon {
        display: inline-grid;
        place-items: center;
        width: 2.1rem;
        height: 2.1rem;
        padding: 0;
        border: 1px solid var(--color-border-subtle);
        border-radius: 8px;
        background: color-mix(in srgb, var(--color-bg-hover) 80%, transparent);
        color: var(--color-brand-primary);
        cursor: pointer;
      }
      .btn-icon:hover {
        background: color-mix(in srgb, var(--color-brand-primary) 14%, transparent);
        border-color: color-mix(in srgb, var(--color-brand-primary) 35%, transparent);
      }
      .btn-icon svg {
        width: 1.05rem;
        height: 1.05rem;
      }
      .clip {
        font-size: 1rem;
        line-height: 1;
      }
      .muted {
        color: var(--color-text-muted);
      }
    `,
  ],
})
export class TaskTableComponent {
  readonly tasks = input<TaskListItem[]>([]);
  readonly loading = input(false);
  readonly assignees = input<{ userId: string; label: string }[]>([]);
  readonly selectedIds = input<Set<string>>(new Set());
  readonly bulkCompleteDisabled = input(false);

  readonly openTask = output<string>();
  readonly openEvidence = output<string>();
  readonly completeTask = output<string>();
  readonly toggleSelect = output<string>();
  readonly clearSelection = output<void>();
  readonly bulkComplete = output<void>();
  readonly patchStatus = output<{ taskId: string; status: TaskApiStatus }>();
  readonly patchOwner = output<{ taskId: string; assignedToUserId: string }>();

  readonly taskStatusLabel = taskStatusLabel;
  readonly taskStatusBadgeClass = taskStatusBadgeClass;
  readonly taskPriorityBadgeClass = taskPriorityBadgeClass;
  readonly taskRiskBadgeClass = taskRiskBadgeClass;
  readonly evidenceStateClass = evidenceStateClass;
  readonly isHighRiskRow = isHighRiskRow;
  readonly taskHasPendingEvidenceReviews = taskHasPendingEvidenceReviews;

  onRowClick(ev: MouseEvent, taskId: string): void {
    const t = ev.target as HTMLElement | null;
    if (t?.closest('button,a,select,input,option')) return;
    this.openTask.emit(taskId);
  }

  hasAssigneeOption(userId: string): boolean {
    return this.assignees().some((o) => o.userId === userId);
  }
}
