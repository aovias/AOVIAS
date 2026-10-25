import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { TaskListItem } from '../models';
import type { TaskRiskLevelApi } from '../task-risk.util';
import { evidenceStateClass, taskStatusBadgeClass, taskStatusLabel } from '../task-display.util';

export interface RiskSection {
  level: TaskRiskLevelApi;
  label: string;
  tasks: TaskListItem[];
}

@Component({
  selector: 'app-task-risk-view',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ['../tasks-tags.css'],
  template: `
    @if (loading()) {
      <p class="muted">Loading…</p>
    }
    @if (!loading() && totalTasks() === 0) {
      <p class="muted">No tasks.</p>
    }
    <div class="risk-board">
      @for (sec of sections(); track sec.level) {
        <section class="band" [attr.data-band]="sec.level">
          <header class="band-head">
            <h2>{{ sec.label }}</h2>
            <span class="band-count">{{ sec.tasks.length }} tasks</span>
          </header>
          @if (sec.tasks.length === 0) {
            <p class="empty">No tasks in this band.</p>
          }
          <ul class="task-list">
            @for (t of sec.tasks; track t.id) {
              <li (click)="open.emit(t.id)">
                <div class="score-col">{{ t.riskScore }}</div>
                <div class="main-col">
                  <div class="title-row">
                    <span class="title">{{ t.title || 'Task' }}</span>
                    <span class="tag tag-oe">{{ t.objectiveElementCode }}</span>
                  </div>
                  <div class="meta-row">
                    <span class="tag" [ngClass]="taskStatusBadgeClass(t.status)">{{
                      taskStatusLabel(t.status)
                    }}</span>
                    <span class="due" [class.overdue]="t.isOverdue">{{ t.dueDate | date: 'mediumDate' }}</span>
                    <button
                      type="button"
                      class="ev"
                      [class]="evidenceStateClass(t)"
                      (click)="$event.stopPropagation(); openEvidence.emit(t.id)"
                    >
                      📎 {{ t.evidenceApprovedCount }}/{{ t.evidenceTotalCount }}
                    </button>
                  </div>
                </div>
              </li>
            }
          </ul>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .risk-board {
        display: flex;
        flex-direction: column;
        gap: 1.25rem;
      }
      .band {
        border: 1px solid var(--color-border-subtle);
        border-radius: 12px;
        padding: 0.75rem 1rem 1rem;
        background: color-mix(in srgb, var(--color-bg-surface) 92%, var(--color-bg-page));
      }
      .band[data-band='Critical'] {
        border-left: 4px solid var(--color-status-danger);
      }
      .band[data-band='High'] {
        border-left: 4px solid color-mix(in srgb, var(--color-status-danger) 70%, var(--color-status-warning));
      }
      .band[data-band='Medium'] {
        border-left: 4px solid var(--color-status-warning);
      }
      .band[data-band='Low'] {
        border-left: 4px solid var(--color-status-success);
      }
      .band-head {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 0.65rem;
      }
      .band-head h2 {
        margin: 0;
        font-size: var(--font-size-section-title);
        font-weight: 800;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--color-text-primary);
      }
      .band-count {
        font-size: 0.8rem;
        font-weight: 600;
        color: var(--color-text-secondary);
      }
      .empty {
        margin: 0;
        font-size: 0.85rem;
        color: var(--color-text-muted);
      }
      .task-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      .task-list li {
        display: grid;
        grid-template-columns: 52px 1fr;
        gap: 0.65rem 1rem;
        padding: 0.65rem 0.35rem;
        border-bottom: 1px solid var(--color-border-subtle);
        cursor: pointer;
        align-items: start;
      }
      .task-list li:last-child {
        border-bottom: none;
      }
      .score-col {
        font-size: 1.35rem;
        font-weight: 800;
        line-height: 1.1;
        color: var(--color-status-danger);
        font-variant-numeric: tabular-nums;
      }
      .band[data-band='Low'] .score-col {
        color: var(--color-status-success);
      }
      .band[data-band='Medium'] .score-col {
        color: var(--color-status-warning);
      }
      .title-row {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.35rem 0.65rem;
        margin-bottom: 0.35rem;
      }
      .title {
        font-weight: 700;
        color: var(--color-text-primary);
      }
      .meta-row {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.45rem;
        font-size: 0.8rem;
      }
      .due {
        color: var(--color-text-secondary);
        font-variant-numeric: tabular-nums;
      }
      .due.overdue {
        color: var(--color-status-danger);
        font-weight: 700;
      }
      .ev {
        border: 1px solid var(--color-border-subtle);
        border-radius: 6px;
        padding: 0.1rem 0.4rem;
        font-size: 0.72rem;
        font-weight: 600;
        background: var(--color-bg-hover);
        cursor: pointer;
      }
      .ev.ev-none {
        border-color: color-mix(in srgb, var(--color-status-warning) 40%, transparent);
        color: var(--color-status-warning);
      }
      .ev.ev-complete {
        border-color: color-mix(in srgb, var(--color-status-success) 40%, transparent);
        color: var(--color-status-success);
      }
      .muted {
        color: var(--color-text-muted);
      }
    `,
  ],
})
export class TaskRiskViewComponent {
  readonly sections = input<RiskSection[]>([]);
  readonly loading = input(false);

  readonly open = output<string>();
  readonly openEvidence = output<string>();

  readonly taskStatusLabel = taskStatusLabel;
  readonly taskStatusBadgeClass = taskStatusBadgeClass;
  readonly evidenceStateClass = evidenceStateClass;

  totalTasks(): number {
    return this.sections().reduce((n, s) => n + s.tasks.length, 0);
  }
}
