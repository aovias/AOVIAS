import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { TaskQuickFilter } from '../models';

@Component({
  selector: 'app-task-quick-filters',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="quick-filters" role="group" aria-label="Quick filters">
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'myTasks'"
        (click)="toggle('myTasks')"
      >
        My tasks
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'team'"
        (click)="toggle('team')"
      >
        Team
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'overdue'"
        (click)="toggle('overdue')"
      >
        Overdue
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'highPriority'"
        (click)="toggle('highPriority')"
      >
        High priority
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'dueThisWeek'"
        (click)="toggle('dueThisWeek')"
      >
        Due this week
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'highRisk'"
        (click)="toggle('highRisk')"
      >
        High risk
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'focus'"
        (click)="toggle('focus')"
      >
        Focus
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'criticalDeficiencies'"
        (click)="toggle('criticalDeficiencies')"
      >
        Critical deficiencies
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'evidenceMissing'"
        (click)="toggle('evidenceMissing')"
      >
        Evidence missing
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'verificationRejected'"
        (click)="toggle('verificationRejected')"
      >
        Verification rejected
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'coreStage'"
        (click)="toggle('coreStage')"
      >
        Core stage
      </button>
      <button
        type="button"
        class="pill"
        [class.on]="quick() === 'commitmentStage'"
        (click)="toggle('commitmentStage')"
      >
        Commitment stage
      </button>
    </div>
  `,
  styles: [
    `
      .quick-filters {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.45rem;
        justify-content: flex-end;
      }
      .pill {
        border: 1px solid var(--color-border-subtle);
        background: color-mix(in srgb, var(--color-bg-surface-alt) 55%, var(--color-bg-page));
        color: var(--color-text-secondary);
        padding: 0.4rem 0.95rem;
        border-radius: 999px;
        font-size: 0.8rem;
        font-weight: 600;
        cursor: pointer;
        white-space: nowrap;
      }
      .pill:hover {
        color: var(--color-text-primary);
        background: var(--color-bg-hover);
      }
      .pill.on {
        border-color: color-mix(in srgb, var(--color-brand-primary) 45%, var(--color-border-subtle));
        background: color-mix(in srgb, var(--color-brand-primary) 16%, var(--color-bg-surface));
        color: var(--color-brand-primary);
      }
    `,
  ],
})
export class TaskQuickFiltersComponent {
  readonly quick = input<TaskQuickFilter | undefined>(undefined);
  readonly quickChange = output<TaskQuickFilter | null>();

  toggle(key: TaskQuickFilter): void {
    const cur = this.quick();
    this.quickChange.emit(cur === key ? null : key);
  }
}
