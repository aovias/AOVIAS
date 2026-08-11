import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { TaskSummary } from '../models';

@Component({
  selector: 'app-task-summary',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (summary(); as s) {
      <div class="summary-grid" role="list">
        <article class="stat stat--total" role="listitem">
          <div class="icon-tile" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
              <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
          </div>
          <div class="copy">
            <span class="label">Total tasks</span>
            <span class="val">{{ s.total }}</span>
          </div>
        </article>
        <article class="stat stat--pending" role="listitem">
          <div class="icon-tile" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
              <circle cx="12" cy="12" r="9" />
              <path d="M12 7v5l3 2" />
            </svg>
          </div>
          <div class="copy">
            <span class="label">Not started</span>
            <span class="val">{{ s.notStarted }}</span>
          </div>
        </article>
        <article class="stat stat--progress" role="listitem">
          <div class="icon-tile" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
              <path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z" />
            </svg>
          </div>
          <div class="copy">
            <span class="label">In progress</span>
            <span class="val">{{ s.inProgress }}</span>
          </div>
        </article>
        <article class="stat stat--blocked" role="listitem">
          <div class="icon-tile" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
              <circle cx="12" cy="12" r="9" />
              <path d="M8 12h8" stroke-linecap="round" />
            </svg>
          </div>
          <div class="copy">
            <span class="label">Blocked</span>
            <span class="val">{{ s.blocked }}</span>
          </div>
        </article>
        <article class="stat stat--done" role="listitem">
          <div class="icon-tile" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
              <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div class="copy">
            <span class="label">Completed</span>
            <span class="val">{{ s.completed }}</span>
          </div>
        </article>
        <article class="stat stat--overdue" role="listitem">
          <div class="icon-tile" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
              <path d="M12 9v4m0 4h.01M4.93 4.93l14.14 14.14M12 2a10 10 0 100 20 10 10 0 000-20z" />
            </svg>
          </div>
          <div class="copy">
            <span class="label">Overdue</span>
            <span class="val">{{ s.overdue }}</span>
          </div>
        </article>
      </div>
    } @else {
      <div class="empty">No summary</div>
    }
  `,
  styles: [
    `
      .summary-grid {
        display: grid;
        grid-template-columns: repeat(6, minmax(0, 1fr));
        gap: 0.875rem;
        margin-bottom: 1.25rem;
        width: 100%;
      }
      @media (max-width: 1200px) {
        .summary-grid {
          grid-template-columns: repeat(3, minmax(0, 1fr));
        }
      }
      @media (max-width: 640px) {
        .summary-grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }
      .stat {
        display: flex;
        align-items: center;
        gap: 0.875rem;
        padding: 1rem 1.125rem;
        background: var(--color-bg-surface);
        border: 1px solid var(--color-border-subtle);
        border-radius: 12px;
        box-shadow: var(--shadow-xs);
      }
      .icon-tile {
        width: 2.75rem;
        height: 2.75rem;
        border-radius: 10px;
        display: grid;
        place-items: center;
        flex-shrink: 0;
      }
      .icon-tile svg {
        width: 1.35rem;
        height: 1.35rem;
      }
      .copy {
        display: flex;
        flex-direction: column;
        gap: 0.2rem;
        min-width: 0;
      }
      .label {
        font-size: 0.65rem;
        font-weight: 600;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-secondary);
      }
      .val {
        font-size: 1.5rem;
        font-weight: 700;
        font-variant-numeric: tabular-nums;
        line-height: 1.1;
      }
      .stat--total .icon-tile {
        background: color-mix(in srgb, var(--color-brand-primary) 20%, transparent);
        color: var(--color-brand-primary);
      }
      .stat--total .val {
        color: var(--color-brand-primary);
      }
      .stat--pending .icon-tile {
        background: color-mix(in srgb, var(--color-status-warning) 22%, transparent);
        color: var(--color-status-warning);
      }
      .stat--pending .val {
        color: var(--color-status-warning);
      }
      .stat--progress .icon-tile {
        background: color-mix(in srgb, var(--color-status-info) 22%, transparent);
        color: var(--color-status-info);
      }
      .stat--progress .val {
        color: var(--color-status-info);
      }
      .stat--blocked .icon-tile {
        background: color-mix(in srgb, var(--color-chart-neutral) 28%, transparent);
        color: var(--color-text-secondary);
      }
      .stat--blocked .val {
        color: var(--color-text-primary);
      }
      .stat--done .icon-tile {
        background: color-mix(in srgb, var(--color-status-success) 22%, transparent);
        color: var(--color-status-success);
      }
      .stat--done .val {
        color: var(--color-status-success);
      }
      .stat--overdue {
        border-color: color-mix(in srgb, var(--color-status-danger) 45%, var(--color-border-subtle));
        box-shadow: 0 0 0 1px color-mix(in srgb, var(--color-status-danger) 25%, transparent);
      }
      .stat--overdue .icon-tile {
        background: color-mix(in srgb, var(--color-status-danger) 22%, transparent);
        color: var(--color-status-danger);
      }
      .stat--overdue .val {
        color: var(--color-status-danger);
      }
      .empty {
        color: var(--color-text-muted);
        margin-bottom: 1rem;
      }
    `,
  ],
})
export class TaskSummaryComponent {
  readonly summary = input<TaskSummary | null>(null);
}
