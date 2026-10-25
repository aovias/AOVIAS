import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FrameworkSnapshot } from '../models/dashboard-snapshot.model';

interface ActivityItem {
  activityId: string;
  type: string;
  actor: string;
  createdAt: string;
  summary: string;
  frameworkCode: string;
}

@Component({
  selector: 'app-activity-stream',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="activity-container">
      <div class="activity-header">
        <h3 class="activity-title">{{ title() }}</h3>
        @if (activities().length > 0) {
          <span class="activity-count">{{ activities().length }}</span>
        }
      </div>

      @if (activities().length === 0) {
        <div class="empty-state">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M22 12h-4l-3 9L9 3l-3 9H2" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span>No recent activity</span>
        </div>
      } @else {
        <div class="activity-feed praman-scrollbar praman-scrollbar--panel">
          @for (item of activities(); track item.activityId) {
            <div class="activity-item">
              <div class="activity-icon" [attr.data-type]="normalizeType(item.type)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path [attr.d]="getIconPath(item.type)" />
                </svg>
              </div>
              <div class="activity-content">
                <p class="activity-summary">{{ item.summary }}</p>
                <div class="activity-meta">
                  <span class="activity-actor">{{ item.actor }}</span>
                  <span class="activity-sep">·</span>
                  <span class="fw-chip">{{ item.frameworkCode }}</span>
                  <span class="activity-sep">·</span>
                  <span class="activity-time">{{ formatTime(item.createdAt) }}</span>
                </div>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
      }

      .activity-container {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
        transition: box-shadow 0.3s ease;
        height: 100%;
        box-sizing: border-box;
        display: flex;
        flex-direction: column;
      }

      .activity-container:hover {
        box-shadow: var(--shadow-md);
      }

      .activity-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 12px;
      }

      .activity-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .activity-count {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        min-width: 24px;
        height: 24px;
        padding: 0 8px;
        background: var(--hover-bg);
        border-radius: 12px;
        font-size: 12px;
        font-weight: 700;
        color: var(--text-secondary);
      }

      .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 12px;
        padding: 20px 16px;
        color: var(--text-secondary);
        font-size: 14px;
      }

      .empty-state svg {
        opacity: 0.4;
      }

      .activity-feed {
        display: flex;
        flex-direction: column;
        gap: 0;
        flex: 1;
        min-height: 0;
        max-height: 360px;
        overflow-y: auto;
      }

      .activity-item {
        display: flex;
        gap: 12px;
        padding: 12px 8px;
        margin: 0 -8px;
        border-radius: 8px;
        transition: background 0.2s ease;
      }

      .activity-item:hover {
        background: var(--hover-bg);
      }

      .activity-item + .activity-item {
        border-top: 1px solid var(--border-color);
      }

      .activity-icon {
        width: 28px;
        height: 28px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        margin-top: 2px;
        background: var(--hover-bg);
        color: var(--text-secondary);
      }

      .activity-icon[data-type='upload'],
      .activity-icon[data-type='evidence'] {
        background: var(--color-primary-bg);
        color: var(--color-chart-chapter-trend);
      }

      .activity-icon[data-type='audit'],
      .activity-icon[data-type='schedule'] {
        background: rgba(239, 68, 68, 0.1);
        color: var(--danger-color);
      }

      .activity-icon[data-type='assessment'],
      .activity-icon[data-type='score'] {
        background: rgba(34, 197, 94, 0.1);
        color: var(--success-color);
      }

      .activity-icon[data-type='task'],
      .activity-icon[data-type='update'] {
        background: rgba(59, 130, 246, 0.1);
        color: var(--primary-color);
      }

      .activity-icon[data-type='expiry'],
      .activity-icon[data-type='warning'] {
        background: rgba(245, 158, 11, 0.1);
        color: var(--warning-color);
      }

      .activity-content {
        flex: 1;
        min-width: 0;
      }

      .activity-summary {
        font-size: 13px;
        font-weight: 500;
        color: var(--text-primary);
        line-height: 1.5;
        margin: 0 0 6px;
      }

      .activity-meta {
        display: flex;
        align-items: center;
        gap: 6px;
        flex-wrap: wrap;
      }

      .activity-actor {
        font-size: 12px;
        font-weight: 500;
        color: var(--text-secondary);
      }

      .activity-sep {
        font-size: 12px;
        color: var(--border-color);
      }

      .fw-chip {
        display: inline-block;
        padding: 1px 6px;
        border-radius: 4px;
        font-size: 11px;
        font-weight: 600;
        background: rgba(47, 107, 255, 0.1);
        color: var(--primary-color);
      }

      .activity-time {
        font-size: 11px;
        color: var(--text-secondary);
      }

      @media (max-width: 640px) {
        .activity-meta {
          gap: 4px;
        }
      }
    `,
  ],
})
export class ActivityStreamComponent {
  title = input<string>('Recent Activity');
  frameworks = input.required<FrameworkSnapshot[]>();

  activities = computed<ActivityItem[]>(() => {
    const result: ActivityItem[] = [];
    for (const fw of this.frameworks()) {
      for (const a of fw.recentActivity ?? []) {
        result.push({ ...a, frameworkCode: fw.frameworkCode });
      }
    }
    return result
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 20);
  });

  normalizeType(type: string): string {
    const t = type.toLowerCase();
    if (t.includes('upload') || t.includes('evidence')) return 'upload';
    if (t.includes('audit') || t.includes('schedule')) return 'audit';
    if (t.includes('score') || t.includes('assessment')) return 'assessment';
    if (t.includes('expiry') || t.includes('warning')) return 'expiry';
    if (t.includes('task')) return 'task';
    return 'update';
  }

  getIconPath(type: string): string {
    const normalized = this.normalizeType(type);
    switch (normalized) {
      case 'upload':
        return 'M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z M14 2v6h6 M12 18v-6 M9 15l3-3 3 3';
      case 'audit':
        return 'M9 11l3 3L22 4 M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11';
      case 'assessment':
        return 'M22 11.08V12a10 10 0 11-5.93-9.14 M22 4L12 14.01l-3-3';
      case 'expiry':
        return 'M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z M12 9v4 M12 17h.01';
      case 'task':
        return 'M9 11l3 3L22 4 M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11';
      default:
        return 'M12 20h9 M16.5 3.5a2.121 2.121 0 013 3L7 19l-4 1 1-4L16.5 3.5z';
    }
  }

  formatTime(dateStr: string): string {
    const now = new Date();
    const d = new Date(dateStr);
    const diffMs = now.getTime() - d.getTime();
    const diffMin = Math.floor(diffMs / 60000);
    if (diffMin < 1) return 'just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    const diffHrs = Math.floor(diffMin / 60);
    if (diffHrs < 24) return `${diffHrs}h ago`;
    const diffDays = Math.floor(diffHrs / 24);
    if (diffDays === 1) return 'yesterday';
    if (diffDays < 7) return `${diffDays}d ago`;
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
