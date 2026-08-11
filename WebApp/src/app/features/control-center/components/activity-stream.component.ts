import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ActivityItem {
  id: string;
  type: 'task' | 'evidence' | 'audit' | 'finding';
  title: string;
  description: string;
  timestamp: Date;
  user: string;
  icon?: string;
}

@Component({
  selector: 'app-activity-stream',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="activity-stream">
      <div class="stream-header">
        <h2 class="section-title">Activity Stream</h2>

        <div class="filter-tabs">
          <button
            *ngFor="let filter of filters"
            class="filter-tab"
            [class.active]="activeFilter === filter.value"
            (click)="setFilter(filter.value)">
            {{ filter.label }}
          </button>
        </div>
      </div>

      <div class="stream-container">
        <div class="activity-list">
          <div *ngFor="let activity of filteredActivities; let last = last"
               class="activity-item"
               [class.last]="last">
            <div class="activity-timeline">
              <div class="timeline-dot" [class]="'dot-' + activity.type"></div>
              <div class="timeline-line" *ngIf="!last"></div>
            </div>

            <div class="activity-content">
              <div class="activity-header">
                <div class="activity-icon" [class]="'icon-' + activity.type">
                  <span>{{ getActivityIcon(activity.type) }}</span>
                </div>
                <div class="activity-meta">
                  <h3 class="activity-title">{{ activity.title }}</h3>
                  <p class="activity-description">{{ activity.description }}</p>
                </div>
              </div>

              <div class="activity-footer">
                <span class="activity-user">{{ activity.user }}</span>
                <span class="activity-timestamp">{{ formatTimestamp(activity.timestamp) }}</span>
              </div>
            </div>
          </div>

          <div *ngIf="filteredActivities.length === 0" class="empty-state">
            <div class="empty-icon">📭</div>
            <p class="empty-text">No activities found</p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .activity-stream {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 24px;
      box-shadow: var(--shadow-sm);
      height: 600px;
      display: flex;
      flex-direction: column;
    }

    .stream-header {
      margin-bottom: 20px;
    }

    .section-title {
      font-size: 20px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 16px 0;
    }

    .filter-tabs {
      display: flex;
      gap: 8px;
      background: var(--bg-tertiary);
      padding: 4px;
      border-radius: 8px;
    }

    .filter-tab {
      flex: 1;
      padding: 8px 16px;
      border: none;
      background: transparent;
      border-radius: 6px;
      font-size: 13px;
      font-weight: 600;
      color: var(--text-secondary);
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .filter-tab:hover {
      color: var(--text-primary);
    }

    .filter-tab.active {
      background: var(--card-bg);
      color: var(--primary-color);
      box-shadow: var(--shadow-sm);
    }

    .stream-container {
      flex: 1;
      overflow-y: auto;
      margin: 0 -8px;
      padding: 0 8px;
    }

    .stream-container::-webkit-scrollbar {
      width: 6px;
    }

    .stream-container::-webkit-scrollbar-track {
      background: var(--bg-tertiary);
      border-radius: 3px;
    }

    .stream-container::-webkit-scrollbar-thumb {
      background: var(--border-color);
      border-radius: 3px;
    }

    .stream-container::-webkit-scrollbar-thumb:hover {
      background: var(--text-secondary);
    }

    .activity-list {
      display: flex;
      flex-direction: column;
    }

    .activity-item {
      display: flex;
      gap: 16px;
      padding: 16px 0;
    }

    .activity-timeline {
      position: relative;
      display: flex;
      flex-direction: column;
      align-items: center;
      width: 32px;
      flex-shrink: 0;
    }

    .timeline-dot {
      width: 12px;
      height: 12px;
      border-radius: 50%;
      border: 3px solid var(--card-bg);
      box-shadow: 0 0 0 2px currentColor;
      flex-shrink: 0;
      z-index: 1;
    }

    .dot-task {
      color: var(--primary-color);
    }

    .dot-evidence {
      color: var(--success-color);
    }

    .dot-audit {
      color: var(--primary-color);
    }

    .dot-finding {
      color: var(--danger-color);
    }

    .timeline-line {
      width: 2px;
      flex: 1;
      background: var(--border-color);
      margin-top: 4px;
    }

    .activity-content {
      flex: 1;
      min-width: 0;
    }

    .activity-header {
      display: flex;
      gap: 12px;
      margin-bottom: 12px;
    }

    .activity-icon {
      width: 40px;
      height: 40px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 18px;
      flex-shrink: 0;
    }

    .icon-task {
      background: var(--primary-bg-light);
    }

    .icon-evidence {
      background: rgba(16, 185, 129, 0.1);
    }

    .icon-audit {
      background: var(--primary-bg-light);
    }

    .icon-finding {
      background: rgba(239, 68, 68, 0.1);
    }

    .activity-meta {
      flex: 1;
      min-width: 0;
    }

    .activity-title {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 4px 0;
    }

    .activity-description {
      font-size: 13px;
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.5;
    }

    .activity-footer {
      display: flex;
      align-items: center;
      gap: 12px;
      font-size: 12px;
      color: var(--text-secondary);
    }

    .activity-user {
      font-weight: 500;
    }

    .activity-timestamp {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .activity-timestamp::before {
      content: '•';
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 60px 20px;
    }

    .empty-icon {
      font-size: 48px;
      margin-bottom: 12px;
      opacity: 0.5;
    }

    .empty-text {
      font-size: 14px;
      color: var(--text-secondary);
      margin: 0;
    }
  `]
})
export class ActivityStreamComponent {
  @Input() activities: ActivityItem[] = [];

  activeFilter: string = 'all';

  filters = [
    { label: 'All', value: 'all' },
    { label: 'Tasks', value: 'task' },
    { label: 'Evidence', value: 'evidence' },
    { label: 'Audits', value: 'audit' }
  ];

  get filteredActivities(): ActivityItem[] {
    if (this.activeFilter === 'all') {
      return this.activities;
    }
    return this.activities.filter(a => a.type === this.activeFilter);
  }

  setFilter(filter: string): void {
    this.activeFilter = filter;
  }

  getActivityIcon(type: string): string {
    const icons: Record<string, string> = {
      'task': '✓',
      'evidence': '📄',
      'audit': '🔍',
      'finding': '⚠️'
    };
    return icons[type] || '•';
  }

  formatTimestamp(timestamp: Date): string {
    const now = new Date();
    const diff = now.getTime() - timestamp.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    if (days < 7) return `${days}d ago`;

    return timestamp.toLocaleDateString();
  }
}
