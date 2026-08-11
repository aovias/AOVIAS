import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { JourneyRecommendationModel } from '../../models/compliance-maturity.model';

@Component({
  selector: 'app-next-actions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="actions-card" aria-labelledby="recommended-actions-heading">
      <h2 id="recommended-actions-heading" class="actions-card__title">Governance Recommendations</h2>
      <p class="actions-card__subtitle">Prioritized accreditation actions — execution in Journey</p>

      <div class="actions-list">
        @for (action of actions(); track action.title; let i = $index) {
          <article
            class="action-item"
            [class.action-item--disabled]="!action.isEnabled"
            [attr.aria-disabled]="!action.isEnabled"
          >
            <div class="action-item__rank" aria-hidden="true">{{ i + 1 }}</div>
            <div class="action-item__body">
              <h3 class="action-item__title">{{ action.title }}</h3>
              @if (action.description) {
                <p class="action-item__desc">{{ action.description }}</p>
              }
            </div>
            <div class="action-item__meta">
              <span class="action-item__priority" [attr.data-priority]="priorityClass(action.priority)">
                {{ priorityLabel(action.priority) }}
              </span>
              @if (action.isEnabled) {
                <a
                  class="action-item__goto"
                  [routerLink]="['/app/journey/execution']"
                  [queryParams]="{ stage: 'core' }"
                  aria-label="Remediate in execution workspace"
                >Remediate →</a>
              }
            </div>
          </article>
        }

        @if (actions().length === 0) {
          <p class="actions-card__empty" role="status">No recommendations available at this time.</p>
        }
      </div>

      <div class="actions-card__footer">
        <a
          class="actions-card__view-all"
          [routerLink]="['/app/journey/execution']"
          [queryParams]="{ stage: 'core' }"
        >View All Core Gaps in Execution Workspace →</a>
      </div>
    </section>
  `,
  styles: [`
    .actions-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
    }

    .actions-card__title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 4px;
    }

    .actions-card__subtitle {
      font-size: var(--font-size-body-sm);
      color: var(--text-secondary);
      margin: 0 0 16px;
    }

    .actions-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .action-item {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 10px;
      transition: background 0.15s ease;
    }

    .action-item:not(.action-item--disabled):hover {
      background: var(--hover-bg);
    }

    .action-item--disabled {
      opacity: 0.55;
    }

    .action-item__rank {
      width: 28px;
      height: 28px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      font-size: 12px;
      font-weight: 700;
      background: var(--primary-bg-light);
      color: var(--primary-color);
    }

    .action-item__body {
      flex: 1;
      min-width: 0;
    }

    .action-item__title {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 2px;
    }

    .action-item__desc {
      font-size: 12px;
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.4;
    }

    .action-item__priority {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      padding: 3px 8px;
      border-radius: 999px;
      white-space: nowrap;
      flex-shrink: 0;
    }

    .action-item__priority[data-priority='high'] {
      background: color-mix(in srgb, var(--danger-color) 12%, transparent);
      color: var(--danger-color);
    }

    .action-item__priority[data-priority='medium'] {
      background: color-mix(in srgb, var(--warning-color) 12%, transparent);
      color: var(--warning-color);
    }

    .action-item__priority[data-priority='low'] {
      background: color-mix(in srgb, var(--success-color) 12%, transparent);
      color: var(--success-color);
    }

    .actions-card__empty {
      font-size: 13px;
      color: var(--text-secondary);
      margin: 0;
      padding: 12px 0;
    }

    .action-item__meta {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 6px;
      flex-shrink: 0;
    }

    .action-item__goto {
      font-size: 11px;
      font-weight: 600;
      color: var(--primary-color);
      text-decoration: none;
      white-space: nowrap;
      transition: opacity 0.15s;

      &:hover { opacity: 0.75; text-decoration: underline; }
    }

    .actions-card__footer {
      margin-top: 12px;
      padding-top: 12px;
      border-top: 1px solid var(--border-color);
      text-align: center;
    }

    .actions-card__view-all {
      font-size: 12px;
      font-weight: 600;
      color: var(--primary-color);
      text-decoration: none;
      transition: opacity 0.15s;

      &:hover { opacity: 0.75; text-decoration: underline; }
    }
  `],
})
export class NextActionsComponent {
  actions = input.required<JourneyRecommendationModel[]>();

  priorityClass(priority: number): string {
    if (priority <= 2) return 'high';
    if (priority <= 4) return 'medium';
    return 'low';
  }

  priorityLabel(priority: number): string {
    if (priority <= 2) return 'High';
    if (priority <= 4) return 'Medium';
    return 'Low';
  }
}
