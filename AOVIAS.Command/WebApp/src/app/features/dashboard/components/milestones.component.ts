import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MilestoneSnapshot } from '../models/dashboard-snapshot.model';

@Component({
  selector: 'app-milestones',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="milestones-container">
      <div class="milestones-header">
        <h3 class="milestones-title">{{ title() }}</h3>
        <span class="milestone-count">{{ milestones().length }}</span>
      </div>

      @if (milestones().length === 0) {
        <div class="empty-state">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2" stroke-width="2" />
            <path d="M16 2v4M8 2v4m-5 4h18" stroke-width="2" stroke-linecap="round" />
          </svg>
          <span>No upcoming milestones</span>
        </div>
      } @else {
        <div class="milestones-list praman-scrollbar praman-scrollbar--panel">
          @for (m of sorted(); track m.milestoneId) {
            <div class="milestone-row" [attr.data-urgency]="getUrgency(m.dueDate)">
              <div class="milestone-left">
                <div class="status-dot" [attr.data-status]="m.status.toLowerCase()"></div>
                <div class="milestone-info">
                  <span class="milestone-title">{{ m.title }}</span>
                  <div class="milestone-meta">
                    @if (m.frameworkCode) {
                      <span class="fw-tag">{{ m.frameworkCode }}</span>
                    }
                    @if (m.owner) {
                      <span class="owner-tag">{{ m.owner }}</span>
                    }
                  </div>
                </div>
              </div>
              <div class="milestone-right">
                <span class="due-badge" [attr.data-urgency]="getUrgency(m.dueDate)">
                  {{ formatDue(m.dueDate) }}
                </span>
                <span class="status-label" [attr.data-status]="m.status.toLowerCase()">
                  {{ m.status }}
                </span>
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

      .milestones-container {
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

      .milestones-container:hover {
        box-shadow: var(--shadow-md);
      }

      .milestones-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 12px;
      }

      .milestones-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .milestone-count {
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

      .milestones-list {
        display: flex;
        flex-direction: column;
        gap: 0;
        flex: 1;
        min-height: 0;
        max-height: 360px;
        overflow-y: auto;
      }

      .milestone-row {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 16px;
        border-bottom: 1px solid var(--border-color);
        transition: background 0.2s ease;
        border-radius: 8px;
        padding: 12px 8px;
        margin: 0 -8px;
      }

      .milestone-row:last-child {
        border-bottom: none;
      }

      .milestone-row:hover {
        background: var(--hover-bg);
      }

      .milestone-row[data-urgency='overdue'] {
        background: rgba(239, 68, 68, 0.03);
      }

      .milestone-row[data-urgency='overdue']:hover {
        background: rgba(239, 68, 68, 0.07);
      }

      .milestone-left {
        display: flex;
        align-items: flex-start;
        gap: 12px;
        flex: 1;
        min-width: 0;
      }

      .status-dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        flex-shrink: 0;
        margin-top: 5px;
        background: var(--text-secondary);
      }

      .status-dot[data-status='pending'] {
        background: var(--warning-color);
      }

      .status-dot[data-status='inprogress'],
      .status-dot[data-status='in progress'],
      .status-dot[data-status='inprogress'] {
        background: var(--primary-color);
      }

      .status-dot[data-status='completed'],
      .status-dot[data-status='done'] {
        background: var(--success-color);
      }

      .status-dot[data-status='overdue'] {
        background: var(--danger-color);
      }

      .milestone-info {
        display: flex;
        flex-direction: column;
        gap: 6px;
        min-width: 0;
      }

      .milestone-title {
        font-size: 14px;
        font-weight: 500;
        color: var(--text-primary);
        line-height: 1.4;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .milestone-meta {
        display: flex;
        gap: 6px;
        flex-wrap: wrap;
      }

      .fw-tag {
        display: inline-block;
        padding: 2px 8px;
        border-radius: 4px;
        font-size: 11px;
        font-weight: 600;
        background: rgba(47, 107, 255, 0.1);
        color: var(--primary-color);
        letter-spacing: 0.2px;
      }

      .owner-tag {
        display: inline-block;
        padding: 2px 8px;
        border-radius: 4px;
        font-size: 11px;
        font-weight: 500;
        background: var(--hover-bg);
        color: var(--text-secondary);
      }

      .milestone-right {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        gap: 6px;
        flex-shrink: 0;
      }

      .due-badge {
        font-size: 12px;
        font-weight: 600;
        padding: 3px 10px;
        border-radius: 6px;
        white-space: nowrap;
        background: var(--hover-bg);
        color: var(--text-secondary);
      }

      .due-badge[data-urgency='overdue'] {
        background: rgba(239, 68, 68, 0.1);
        color: var(--danger-color);
      }

      .due-badge[data-urgency='soon'] {
        background: rgba(245, 158, 11, 0.1);
        color: var(--warning-color);
      }

      .due-badge[data-urgency='normal'] {
        background: rgba(34, 197, 94, 0.08);
        color: var(--success-color);
      }

      .status-label {
        font-size: 11px;
        font-weight: 500;
        color: var(--text-secondary);
        text-transform: capitalize;
      }

      .status-label[data-status='overdue'] {
        color: var(--danger-color);
      }

      .status-label[data-status='completed'],
      .status-label[data-status='done'] {
        color: var(--success-color);
      }

      @media (max-width: 640px) {
        .milestone-row {
          flex-direction: column;
          gap: 10px;
        }

        .milestone-right {
          flex-direction: row;
          align-items: center;
        }
      }
    `,
  ],
})
export class MilestonesComponent {
  title = input<string>('Upcoming Milestones');
  milestones = input.required<MilestoneSnapshot[]>();

  sorted = computed(() =>
    [...this.milestones()].sort(
      (a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
    )
  );

  getUrgency(dueDate: string): 'overdue' | 'soon' | 'normal' {
    const now = new Date();
    const due = new Date(dueDate);
    const diffDays = Math.floor((due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    if (diffDays < 0) return 'overdue';
    if (diffDays <= 14) return 'soon';
    return 'normal';
  }

  formatDue(dueDate: string): string {
    const now = new Date();
    const due = new Date(dueDate);
    const diffMs = due.getTime() - now.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      const overdue = Math.abs(diffDays);
      return overdue === 1 ? '1 day overdue' : `${overdue} days overdue`;
    }
    if (diffDays === 0) return 'Due today';
    if (diffDays === 1) return 'Due tomorrow';
    if (diffDays <= 7) return `Due in ${diffDays} days`;
    if (diffDays <= 30) return `Due in ${Math.round(diffDays / 7)} weeks`;
    return due.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
