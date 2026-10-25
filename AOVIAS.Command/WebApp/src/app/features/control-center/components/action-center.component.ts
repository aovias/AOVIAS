import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface BlockingTask {
  id: string;
  title: string;
  complianceImpact: string;
  owner: string;
  department: string;
  dueDate: Date;
  priority: 'critical' | 'high' | 'medium';
  status: 'overdue' | 'due-soon' | 'on-track';
}

@Component({
  selector: 'app-action-center',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="action-center">
      <div class="section-header">
        <div>
          <h2 class="section-title">Action Center</h2>
          <p class="section-subtitle">Blocking compliance tasks requiring immediate attention</p>
        </div>
        <span class="task-count">{{ blockingTasks.length }} tasks</span>
      </div>

      <div class="tasks-grid">
        <div *ngFor="let task of blockingTasks"
             class="task-card"
             [class.critical]="task.priority === 'critical'"
             [class.high]="task.priority === 'high'"
             [class.overdue]="task.status === 'overdue'">
          <div class="task-header">
            <div class="priority-badge"
                 [class.badge-critical]="task.priority === 'critical'"
                 [class.badge-high]="task.priority === 'high'"
                 [class.badge-medium]="task.priority === 'medium'">
              {{ task.priority }}
            </div>
            <div class="status-badge"
                 [class.status-overdue]="task.status === 'overdue'"
                 [class.status-due-soon]="task.status === 'due-soon'"
                 [class.status-on-track]="task.status === 'on-track'">
              {{ getStatusLabel(task.status) }}
            </div>
          </div>

          <h3 class="task-title">{{ task.title }}</h3>

          <div class="task-impact">
            <svg class="impact-icon" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
            <span class="impact-text">Impacts {{ task.complianceImpact }}</span>
          </div>

          <div class="task-meta">
            <div class="meta-item">
              <svg class="meta-icon" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
              </svg>
              <span>{{ task.owner }}</span>
            </div>
            <div class="meta-item">
              <svg class="meta-icon" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h8a2 2 0 012 2v12a1 1 0 110 2h-3a1 1 0 01-1-1v-2a1 1 0 00-1-1H9a1 1 0 00-1 1v2a1 1 0 01-1 1H4a1 1 0 110-2V4zm3 1h2v2H7V5zm2 4H7v2h2V9zm2-4h2v2h-2V5zm2 4h-2v2h2V9z" clip-rule="evenodd" />
              </svg>
              <span>{{ task.department }}</span>
            </div>
            <div class="meta-item">
              <svg class="meta-icon" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M6 2a1 1 0 00-1 1v1H4a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V6a2 2 0 00-2-2h-1V3a1 1 0 10-2 0v1H7V3a1 1 0 00-1-1zm0 5a1 1 0 000 2h8a1 1 0 100-2H6z" clip-rule="evenodd" />
              </svg>
              <span>{{ formatDate(task.dueDate) }}</span>
            </div>
          </div>

          <div class="task-actions">
            <button class="btn btn-primary" (click)="onTaskAction(task, 'view')">
              View Details
            </button>
            <button class="btn btn-secondary" (click)="onTaskAction(task, 'assign')">
              Reassign
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .action-center {
      margin-bottom: 24px;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 20px;
    }

    .section-title {
      font-size: 20px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 4px 0;
    }

    .section-subtitle {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 0;
    }

    .task-count {
      background: var(--bg-tertiary);
      color: var(--text-primary);
      padding: 6px 16px;
      border-radius: 20px;
      font-size: 14px;
      font-weight: 600;
    }

    .tasks-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
      gap: 20px;
    }

    .task-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
      border-left: 4px solid var(--border-color);
      transition: all 0.3s ease;
    }

    .task-card.critical {
      border-left-color: var(--danger-color);
    }

    .task-card.high {
      border-left-color: var(--warning-color);
    }

    .task-card.overdue {
      background: rgba(239, 68, 68, 0.05);
    }

    .task-card:hover {
      transform: translateY(-4px);
      box-shadow: var(--shadow-md);
    }

    .task-header {
      display: flex;
      gap: 8px;
      margin-bottom: 16px;
    }

    .priority-badge {
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .badge-critical {
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color);
    }

    .badge-high {
      background: rgba(245, 158, 11, 0.1);
      color: var(--warning-color);
    }

    .badge-medium {
      background: var(--primary-bg-light);
      color: var(--primary-color);
    }

    .status-badge {
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 600;
    }

    .status-overdue {
      background: rgba(239, 68, 68, 0.15);
      color: var(--danger-color);
    }

    .status-due-soon {
      background: rgba(245, 158, 11, 0.15);
      color: var(--warning-color);
    }

    .status-on-track {
      background: rgba(16, 185, 129, 0.1);
      color: var(--success-color);
    }

    .task-title {
      font-size: 16px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 12px 0;
      line-height: 1.4;
    }

    .task-impact {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 12px;
      background: var(--primary-bg-light);
      border-radius: 8px;
      margin-bottom: 16px;
    }

    .impact-icon {
      width: 18px;
      height: 18px;
      color: var(--primary-color);
      flex-shrink: 0;
    }

    .impact-text {
      font-size: 13px;
      font-weight: 600;
      color: var(--primary-color);
    }

    .task-meta {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-bottom: 16px;
    }

    .meta-item {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 13px;
      color: var(--text-secondary);
    }

    .meta-icon {
      width: 16px;
      height: 16px;
      flex-shrink: 0;
    }

    .task-actions {
      display: flex;
      gap: 8px;
      padding-top: 16px;
      border-top: 1px solid var(--border-color);
    }

    .btn {
      flex: 1;
      padding: 8px 16px;
      border-radius: 8px;
      font-size: 13px;
      font-weight: 600;
      border: none;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-primary {
      background: var(--primary-color);
      color: white;
    }

    .btn-primary:hover {
      background: var(--primary-hover);
      transform: translateY(-1px);
    }

    .btn-secondary {
      background: var(--bg-tertiary);
      color: var(--text-primary);
    }

    .btn-secondary:hover {
      background: var(--hover-bg);
    }

    @media (max-width: 768px) {
      .tasks-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class ActionCenterComponent {
  @Input() blockingTasks: BlockingTask[] = [];

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      'overdue': 'Overdue',
      'due-soon': 'Due Soon',
      'on-track': 'On Track'
    };
    return labels[status] || status;
  }

  formatDate(date: Date): string {
    const now = new Date();
    const diff = date.getTime() - now.getTime();
    const days = Math.ceil(diff / (1000 * 60 * 60 * 24));

    if (days < 0) return `${Math.abs(days)} days overdue`;
    if (days === 0) return 'Due today';
    if (days === 1) return 'Due tomorrow';
    return `Due in ${days} days`;
  }

  onTaskAction(task: BlockingTask, action: string): void {
    console.log('Task action:', action, task);
  }
}
