import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { TaskListItem } from '../models';

@Component({
  selector: 'app-task-department-view',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="dept" role="region" aria-label="Tasks by department">
      @if (loading()) {
        <p class="muted">Loading departments…</p>
      } @else if (groups().length === 0) {
        <p class="muted">No tasks to group by department.</p>
      } @else {
        <div class="cols">
          @for (g of groups(); track g.key) {
            <section class="col">
              <header>
                <h3>{{ g.label }}</h3>
                <span class="count">{{ g.tasks.length }}</span>
              </header>
              <ul>
                @for (t of g.tasks; track t.id) {
                  <li>
                    <button type="button" class="row" (click)="openTask.emit(t.id)">
                      <span class="title">{{ t.title || t.objectiveElementCode }}</span>
                      <span class="meta">
                        {{ t.status }}
                        @if (t.category) {
                          · {{ t.category }}
                        }
                        @if (t.isOverdue) {
                          · Overdue
                        }
                      </span>
                    </button>
                  </li>
                }
              </ul>
            </section>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      .dept {
        margin-top: 0.5rem;
      }
      .muted {
        color: var(--color-text-secondary);
        font-size: 0.9rem;
      }
      .cols {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
        gap: 0.75rem;
      }
      .col {
        border: 1px solid var(--color-border-subtle);
        border-radius: 10px;
        background: var(--color-bg-surface);
        padding: 0.75rem;
      }
      .col header {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 0.5rem;
        margin-bottom: 0.65rem;
      }
      .col h3 {
        margin: 0;
        font-size: 0.95rem;
        font-weight: 700;
      }
      .count {
        font-size: 0.75rem;
        color: var(--color-text-muted);
      }
      ul {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
      }
      .row {
        width: 100%;
        text-align: left;
        border: 1px solid var(--color-border-subtle);
        background: color-mix(in srgb, var(--color-bg-surface-alt) 55%, var(--color-bg-page));
        border-radius: 8px;
        padding: 0.5rem 0.6rem;
        cursor: pointer;
        display: flex;
        flex-direction: column;
        gap: 0.2rem;
      }
      .row:hover {
        border-color: color-mix(in srgb, var(--color-brand-primary) 40%, var(--color-border-subtle));
      }
      .title {
        font-size: 0.82rem;
        font-weight: 600;
        color: var(--color-text-primary);
      }
      .meta {
        font-size: 0.72rem;
        color: var(--color-text-secondary);
      }
    `,
  ],
})
export class TaskDepartmentViewComponent {
  readonly tasks = input<TaskListItem[]>([]);
  readonly loading = input(false);
  readonly openTask = output<string>();

  readonly groups = computed(() => {
    const map = new Map<string, TaskListItem[]>();
    for (const t of this.tasks()) {
      const key = t.departmentId?.trim() || '__none__';
      const list = map.get(key) ?? [];
      list.push(t);
      map.set(key, list);
    }
    return [...map.entries()]
      .map(([key, tasks]) => ({
        key,
        label: key === '__none__' ? 'Unassigned department' : tasks[0]?.departmentName || key,
        tasks: [...tasks].sort((a, b) => (a.isOverdue === b.isOverdue ? 0 : a.isOverdue ? -1 : 1)),
      }))
      .sort((a, b) => a.label.localeCompare(b.label, undefined, { sensitivity: 'base' }));
  });
}
