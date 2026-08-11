import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { TaskListItem } from '../models';

@Component({
  selector: 'app-task-calendar',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="cal" role="region" aria-label="Task calendar">
      @if (loading()) {
        <p class="muted">Loading calendar…</p>
      } @else if (days().length === 0) {
        <p class="muted">No tasks with due dates in this window.</p>
      } @else {
        <div class="grid">
          @for (day of days(); track day.key) {
            <section class="day" [class.overdue]="day.hasOverdue">
              <header>
                <strong>{{ day.label }}</strong>
                <span class="count">{{ day.tasks.length }}</span>
              </header>
              <ul>
                @for (t of day.tasks; track t.id) {
                  <li>
                    <button type="button" class="chip" (click)="openTask.emit(t.id)">
                      <span class="title">{{ t.title || t.objectiveElementCode }}</span>
                      @if (t.category) {
                        <span class="meta">{{ t.category }}</span>
                      }
                      @if (t.isOverdue) {
                        <span class="od">Overdue</span>
                      }
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
      .cal {
        margin-top: 0.5rem;
      }
      .muted {
        color: var(--color-text-secondary);
        font-size: 0.9rem;
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: 0.75rem;
      }
      .day {
        border: 1px solid var(--color-border-subtle);
        border-radius: 10px;
        background: var(--color-bg-surface);
        padding: 0.65rem 0.75rem;
        min-height: 8rem;
      }
      .day.overdue {
        border-color: color-mix(in srgb, var(--color-status-danger) 40%, var(--color-border-subtle));
      }
      .day header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.5rem;
        gap: 0.5rem;
      }
      .day header strong {
        font-size: 0.85rem;
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
      .chip {
        width: 100%;
        text-align: left;
        border: 1px solid var(--color-border-subtle);
        background: color-mix(in srgb, var(--color-bg-surface-alt) 55%, var(--color-bg-page));
        border-radius: 8px;
        padding: 0.4rem 0.55rem;
        cursor: pointer;
        display: flex;
        flex-direction: column;
        gap: 0.15rem;
      }
      .chip:hover {
        border-color: color-mix(in srgb, var(--color-brand-primary) 40%, var(--color-border-subtle));
      }
      .title {
        font-size: 0.8rem;
        font-weight: 600;
        color: var(--color-text-primary);
      }
      .meta {
        font-size: 0.7rem;
        color: var(--color-text-secondary);
      }
      .od {
        font-size: 0.68rem;
        font-weight: 700;
        color: var(--color-status-danger);
      }
    `,
  ],
})
export class TaskCalendarComponent {
  readonly tasks = input<TaskListItem[]>([]);
  readonly loading = input(false);
  readonly openTask = output<string>();

  readonly days = computed(() => {
    const map = new Map<string, TaskListItem[]>();
    for (const t of this.tasks()) {
      if (!t.dueDate) continue;
      const key = t.dueDate.slice(0, 10);
      const list = map.get(key) ?? [];
      list.push(t);
      map.set(key, list);
    }
    return [...map.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([key, tasks]) => ({
        key,
        label: key,
        tasks,
        hasOverdue: tasks.some((x) => x.isOverdue),
      }));
  });
}
