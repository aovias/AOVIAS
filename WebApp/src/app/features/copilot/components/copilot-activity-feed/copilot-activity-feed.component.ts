import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CopilotStore } from '../../state/copilot.store';

@Component({
  selector: 'app-copilot-activity-feed',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="activity-feed">
      <h3 class="feed-title">Activity</h3>
      @if (store.loading().activity) {
        <p class="feed-empty">Loading activity…</p>
      } @else if (store.activityFeed().length === 0) {
        <p class="feed-empty">No copilot activity recorded yet.</p>
      } @else {
        <ul class="feed-list">
          @for (event of store.activityFeed(); track event.id) {
            <li class="feed-item">
              <span class="event-type">{{ formatEventType(event.eventType) }}</span>
              @if (event.persona) {
                <span class="event-persona">{{ event.persona }}</span>
              }
              <time class="event-time">{{ event.occurredAtUtc | date: 'medium' }}</time>
              @if (event.notes) {
                <p class="event-notes">{{ event.notes }}</p>
              }
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: [
    `
      .activity-feed {
        border: 1px solid var(--border-color);
        border-radius: 10px;
        padding: 16px;
        background: var(--card-bg);
      }
      .feed-title {
        margin: 0 0 12px;
        font-size: 15px;
        font-weight: 600;
      }
      .feed-empty {
        font-size: 13px;
        color: var(--text-secondary);
      }
      .feed-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      .feed-item {
        padding: 10px 0;
        border-bottom: 1px solid var(--border-color);
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        align-items: center;
      }
      .feed-item:last-child {
        border-bottom: none;
      }
      .event-type {
        font-weight: 600;
        font-size: 13px;
      }
      .event-persona {
        font-size: 12px;
        padding: 2px 8px;
        border-radius: 999px;
        background: var(--hover-bg);
        color: var(--text-secondary);
      }
      .event-time {
        font-size: 12px;
        color: var(--text-secondary);
      }
      .event-notes {
        margin: 4px 0 0;
        font-size: 12px;
        color: var(--text-secondary);
        width: 100%;
      }
    `,
  ],
})
export class CopilotActivityFeedComponent implements OnInit {
  protected readonly store = inject(CopilotStore);

  ngOnInit(): void {
    if (this.store.activityFeed().length === 0) {
      this.store.loadActivityFeed();
    }
  }

  formatEventType(type: string): string {
    return type.replace(/([A-Z])/g, ' $1').trim();
  }
}
