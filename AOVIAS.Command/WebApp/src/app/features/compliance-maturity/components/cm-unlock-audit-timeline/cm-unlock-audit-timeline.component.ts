import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { UnlockAuditTimelineEntryModel } from '../../models/compliance-maturity.model';

@Component({
  selector: 'app-cm-unlock-audit-timeline',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="audit-timeline" aria-labelledby="cm-audit-timeline-heading">
      <h2 id="cm-audit-timeline-heading" class="audit-timeline__title">Unlock Audit History</h2>
      <p class="audit-timeline__subtitle">Governance timeline of stage unlock decisions</p>

      @if (entries().length === 0) {
        <p class="audit-timeline__empty" role="status">No unlock audit entries recorded yet.</p>
      } @else {
        <ol class="audit-timeline__list">
          @for (entry of entries(); track entry.occurredAt + entry.title) {
            <li class="audit-timeline__item" [attr.data-type]="entry.eventType">
              <div class="audit-timeline__dot" aria-hidden="true"></div>
              <div class="audit-timeline__body">
                <h3 class="audit-timeline__event">{{ entry.title }}</h3>
                <p class="audit-timeline__desc">{{ entry.description }}</p>
                <time class="audit-timeline__time" [dateTime]="entry.occurredAt">{{ entry.occurredAt | date: 'medium' }}</time>
              </div>
            </li>
          }
        </ol>
      }
    </section>
  `,
  styles: [`
    .audit-timeline {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
    }
    .audit-timeline__title { margin: 0 0 4px; font-size: 16px; font-weight: 600; color: var(--text-primary); }
    .audit-timeline__subtitle { margin: 0 0 14px; font-size: var(--font-size-body-sm); color: var(--text-secondary); }
    .audit-timeline__empty { margin: 0; font-size: 13px; color: var(--text-secondary); }
    .audit-timeline__list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .audit-timeline__item {
      display: grid;
      grid-template-columns: 12px 1fr;
      gap: 10px;
    }
    .audit-timeline__dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      margin-top: 5px;
      background: var(--primary-color);
    }
    .audit-timeline__item[data-type='StageBlocked'] .audit-timeline__dot {
      background: var(--danger-color);
    }
    .audit-timeline__item[data-type='StageCompleted'] .audit-timeline__dot,
    .audit-timeline__item[data-type='StageUnlocked'] .audit-timeline__dot {
      background: var(--success-color);
    }
    .audit-timeline__event {
      margin: 0;
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }
    .audit-timeline__desc {
      margin: 2px 0 0;
      font-size: 12px;
      color: var(--text-secondary);
      line-height: 1.4;
    }
    .audit-timeline__time {
      display: block;
      margin-top: 4px;
      font-size: 11px;
      color: var(--text-muted);
    }
  `],
})
export class CmUnlockAuditTimelineComponent {
  entries = input.required<UnlockAuditTimelineEntryModel[]>();
}
