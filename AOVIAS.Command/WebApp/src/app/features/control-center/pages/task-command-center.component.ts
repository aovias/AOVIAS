import { Component, ChangeDetectionStrategy, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface RiskCard {
  label: string;
  value: number;
  severity: 'critical' | 'major' | 'minor' | 'warning' | 'info';
  icon: string;
}

interface TaskItem {
  id: string;
  title: string;
  clause: string;
  clauseCode: string;
  chapter: string;
  chapterCode: string;
  owner: string;
  department: string;
  priority: 'critical' | 'high' | 'medium' | 'low';
  status: 'pending' | 'assigned' | 'in-progress' | 'evidence-submitted' | 'under-review' | 'completed';
  dueDate: string;
  overdue: boolean;
  riskScore: number;
  description: string;
  evidenceCount: number;
  commentCount: number;
}

interface ChartData {
  label: string;
  value: number;
  color: string;
}

interface ActivityEvent {
  id: string;
  type: 'task' | 'evidence' | 'finding' | 'status';
  title: string;
  user: string;
  timestamp: string;
  icon: string;
}

type ViewMode = 'table' | 'kanban' | 'risk-priority';

@Component({
  selector: 'app-task-command-center',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="task-command-center">
      @if (hasCriticalTasks()) {
        <div class="alert-banner">
          <div class="alert-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
              <path d="M12 9v4m0 4h.01" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
          </div>
          <div class="alert-content">
            <div class="alert-title">{{ criticalTaskCount() }} critical compliance tasks require immediate attention</div>
            <div class="alert-message">These tasks have high risk scores and may impact audit readiness</div>
          </div>
          <button class="alert-action" (click)="filterCriticalTasks()">View Critical Tasks</button>
        </div>
      }

      <div class="risk-radar-section">
        <h2 class="section-title">Risk Radar</h2>
        <div class="risk-cards">
          @for (card of riskCards(); track card.label) {
            <div class="risk-card" [attr.data-severity]="card.severity">
              <div class="risk-card-icon" [innerHTML]="card.icon"></div>
              <div class="risk-card-content">
                <div class="risk-card-value">{{ card.value }}</div>
                <div class="risk-card-label">{{ card.label }}</div>
              </div>
            </div>
          }
        </div>
      </div>

      <div class="intelligence-section">
        <h2 class="section-title">Task Intelligence</h2>
        <div class="intelligence-grid">
          <div class="chart-card">
            <h3 class="chart-title">Tasks by Department</h3>
            <div class="bar-chart">
              @for (item of departmentData(); track item.label) {
                <div class="bar-item">
                  <div class="bar-label">{{ item.label }}</div>
                  <div class="bar-wrapper">
                    <div class="bar-fill" [style.width.%]="(item.value / maxDepartmentValue()) * 100" [style.background]="item.color"></div>
                    <span class="bar-value">{{ item.value }}</span>
                  </div>
                </div>
              }
            </div>
          </div>

          <div class="chart-card">
            <h3 class="chart-title">Tasks by Status</h3>
            <div class="donut-chart">
              <div class="donut-legend">
                @for (item of statusData(); track item.label) {
                  <div class="legend-item">
                    <div class="legend-color" [style.background]="item.color"></div>
                    <div class="legend-label">{{ item.label }}</div>
                    <div class="legend-value">{{ item.value }}</div>
                  </div>
                }
              </div>
            </div>
          </div>

          <div class="chart-card">
            <h3 class="chart-title">Tasks by Chapter</h3>
            <div class="bar-chart">
              @for (item of chapterData(); track item.label) {
                <div class="bar-item">
                  <div class="bar-label">{{ item.label }}</div>
                  <div class="bar-wrapper">
                    <div class="bar-fill" [style.width.%]="(item.value / maxChapterValue()) * 100" [style.background]="item.color"></div>
                    <span class="bar-value">{{ item.value }}</span>
                  </div>
                </div>
              }
            </div>
          </div>

          <div class="chart-card">
            <h3 class="chart-title">Remediation Pipeline</h3>
            <div class="pipeline-chart">
              @for (stage of pipelineStages(); track stage.label) {
                <div class="pipeline-stage">
                  <div class="pipeline-value">{{ stage.value }}</div>
                  <div class="pipeline-label">{{ stage.label }}</div>
                  <div class="pipeline-bar" [style.width.%]="(stage.value / maxPipelineValue()) * 100"></div>
                </div>
              }
            </div>
          </div>
        </div>
      </div>

      <div class="workspace-section">
        <div class="workspace-header">
          <h2 class="section-title">Task Workspace</h2>

          <div class="workspace-controls">
            <div class="view-switcher">
              <button
                class="view-button"
                [class.active]="viewMode() === 'table'"
                (click)="setViewMode('table')"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <rect x="3" y="3" width="18" height="18" rx="2" stroke-width="2"/>
                  <path d="M3 9h18M3 15h18M9 3v18" stroke-width="2"/>
                </svg>
                Table
              </button>
              <button
                class="view-button"
                [class.active]="viewMode() === 'kanban'"
                (click)="setViewMode('kanban')"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <rect x="3" y="3" width="7" height="18" rx="1" stroke-width="2"/>
                  <rect x="14" y="3" width="7" height="18" rx="1" stroke-width="2"/>
                </svg>
                Kanban
              </button>
              <button
                class="view-button"
                [class.active]="viewMode() === 'risk-priority'"
                (click)="setViewMode('risk-priority')"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2"/>
                </svg>
                Risk Priority
              </button>
            </div>
          </div>
        </div>

        <div class="filters-section">
          <div class="filter-chips">
            @for (filter of quickFilters(); track filter.id) {
              <button
                class="filter-chip"
                [class.active]="activeFilter() === filter.id"
                (click)="setFilter(filter.id)"
              >
                {{ filter.label }}
              </button>
            }
          </div>
          <div class="search-box">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <circle cx="11" cy="11" r="8" stroke-width="2"/>
              <path d="M21 21l-4.35-4.35" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <input
              type="text"
              placeholder="Search tasks, clauses, or departments..."
              [(ngModel)]="searchQuery"
            />
          </div>
        </div>

        @if (viewMode() === 'table') {
          <div class="table-view">
            <table class="task-table">
              <thead>
                <tr>
                  <th class="checkbox-col">
                    <input type="checkbox" />
                  </th>
                  <th>Task</th>
                  <th>Clause</th>
                  <th>Chapter</th>
                  <th>Owner</th>
                  <th>Department</th>
                  <th>Priority</th>
                  <th>Status</th>
                  <th>Due Date</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (task of filteredTasks(); track task.id) {
                  <tr
                    class="task-row"
                    [class.overdue]="task.overdue"
                    (click)="selectTask(task)"
                  >
                    <td class="checkbox-col">
                      <input type="checkbox" (click)="$event.stopPropagation()" />
                    </td>
                    <td class="task-title-col">
                      <div class="task-title">{{ task.title }}</div>
                    </td>
                    <td>
                      <div class="clause-info">
                        <div class="clause-code">{{ task.clauseCode }}</div>
                        <div class="clause-text">{{ task.clause }}</div>
                      </div>
                    </td>
                    <td>
                      <span class="chapter-badge">{{ task.chapterCode }}</span>
                    </td>
                    <td>{{ task.owner }}</td>
                    <td>{{ task.department }}</td>
                    <td>
                      <span class="priority-badge" [attr.data-priority]="task.priority">
                        {{ task.priority }}
                      </span>
                    </td>
                    <td>
                      <span class="status-badge" [attr.data-status]="task.status">
                        {{ getStatusLabel(task.status) }}
                      </span>
                    </td>
                    <td>
                      <div class="date-col">
                        {{ formatDate(task.dueDate) }}
                        @if (task.overdue) {
                          <span class="overdue-badge">Overdue</span>
                        }
                      </div>
                    </td>
                    <td>
                      <button class="action-button" (click)="openTaskMenu($event, task)">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                          <circle cx="12" cy="12" r="1" fill="currentColor"/>
                          <circle cx="12" cy="5" r="1" fill="currentColor"/>
                          <circle cx="12" cy="19" r="1" fill="currentColor"/>
                        </svg>
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }

        @if (viewMode() === 'kanban') {
          <div class="kanban-view">
            @for (column of kanbanColumns(); track column.id) {
              <div class="kanban-column">
                <div class="kanban-header">
                  <h3 class="kanban-title">{{ column.label }}</h3>
                  <span class="kanban-count">{{ getColumnTasks(column.id).length }}</span>
                </div>
                <div class="kanban-cards">
                  @for (task of getColumnTasks(column.id); track task.id) {
                    <div class="kanban-card" (click)="selectTask(task)">
                      <div class="kanban-card-header">
                        <span class="priority-badge" [attr.data-priority]="task.priority">
                          {{ task.priority }}
                        </span>
                        @if (task.overdue) {
                          <span class="overdue-badge">Overdue</span>
                        }
                      </div>
                      <div class="kanban-card-title">{{ task.title }}</div>
                      <div class="kanban-card-meta">
                        <span class="clause-code">{{ task.clauseCode }}</span>
                        <span class="chapter-badge">{{ task.chapterCode }}</span>
                      </div>
                      <div class="kanban-card-footer">
                        <div class="assignee">{{ task.owner }}</div>
                        <div class="due-date">{{ formatDate(task.dueDate) }}</div>
                      </div>
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        }

        @if (viewMode() === 'risk-priority') {
          <div class="risk-priority-view">
            @for (task of sortedByRisk(); track task.id; let idx = $index) {
              <div class="risk-task-item" (click)="selectTask(task)">
                <div class="risk-rank">{{ idx + 1 }}</div>
                <div class="risk-task-content">
                  <div class="risk-task-header">
                    <h4 class="risk-task-title">{{ task.title }}</h4>
                    <span class="priority-badge" [attr.data-priority]="task.priority">
                      {{ task.priority }}
                    </span>
                  </div>
                  <div class="risk-task-meta">
                    <span class="clause-code">{{ task.clauseCode }}</span>
                    <span>•</span>
                    <span class="chapter-badge">{{ task.chapterCode }}</span>
                    <span>•</span>
                    <span>{{ task.owner }}</span>
                    <span>•</span>
                    <span>Due {{ formatDate(task.dueDate) }}</span>
                  </div>
                  <div class="risk-score-bar">
                    <div class="risk-score-fill" [style.width.%]="task.riskScore"></div>
                    <span class="risk-score-value">Risk Score: {{ task.riskScore }}/100</span>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </div>

      @if (selectedTask()) {
        <div class="task-detail-panel">
          <div class="panel-header">
            <h2 class="panel-title">Task Details</h2>
            <button class="close-button" (click)="closeTaskPanel()">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path d="M18 6L6 18M6 6l12 12" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <div class="panel-tabs">
            @for (tab of detailTabs(); track tab.id) {
              <button
                class="panel-tab"
                [class.active]="activeTab() === tab.id"
                (click)="setActiveTab(tab.id)"
              >
                {{ tab.label }}
              </button>
            }
          </div>

          <div class="panel-content">
            @if (activeTab() === 'details') {
              <div class="detail-section">
                <h3 class="detail-title">{{ selectedTask()?.title }}</h3>
                <p class="detail-description">{{ selectedTask()?.description }}</p>

                <div class="detail-grid">
                  <div class="detail-item">
                    <label>Clause</label>
                    <div class="detail-value">
                      <span class="clause-code">{{ selectedTask()?.clauseCode }}</span>
                      {{ selectedTask()?.clause }}
                    </div>
                  </div>
                  <div class="detail-item">
                    <label>Chapter</label>
                    <div class="detail-value">
                      <span class="chapter-badge">{{ selectedTask()?.chapterCode }}</span>
                      {{ selectedTask()?.chapter }}
                    </div>
                  </div>
                  <div class="detail-item">
                    <label>Owner</label>
                    <div class="detail-value">{{ selectedTask()?.owner }}</div>
                  </div>
                  <div class="detail-item">
                    <label>Department</label>
                    <div class="detail-value">{{ selectedTask()?.department }}</div>
                  </div>
                  <div class="detail-item">
                    <label>Priority</label>
                    <span class="priority-badge" [attr.data-priority]="selectedTask()?.priority">
                      {{ selectedTask()?.priority }}
                    </span>
                  </div>
                  <div class="detail-item">
                    <label>Status</label>
                    <span class="status-badge" [attr.data-status]="selectedTask()?.status">
                      {{ getStatusLabel(selectedTask()?.status) }}
                    </span>
                  </div>
                  <div class="detail-item">
                    <label>Due Date</label>
                    <div class="detail-value">{{ formatDate(selectedTask()?.dueDate) }}</div>
                  </div>
                  <div class="detail-item">
                    <label>Risk Score</label>
                    <div class="detail-value">{{ selectedTask()?.riskScore }}/100</div>
                  </div>
                </div>
              </div>
            }

            @if (activeTab() === 'evidence') {
              <div class="evidence-section">
                <button class="upload-button">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                  Upload Evidence
                </button>

                <div class="evidence-list">
                  <div class="evidence-item">
                    <div class="evidence-icon">
                      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/>
                        <path d="M13 2v7h7" stroke-width="2"/>
                      </svg>
                    </div>
                    <div class="evidence-info">
                      <div class="evidence-name">CCTV Installation Report.pdf</div>
                      <div class="evidence-meta">Uploaded by Rajesh Kumar • 2 days ago</div>
                    </div>
                    <span class="status-badge" data-status="evidence-submitted">Approved</span>
                  </div>
                </div>
              </div>
            }

            @if (activeTab() === 'comments') {
              <div class="comments-section">
                <div class="comment-input">
                  <textarea placeholder="Add a comment..." rows="3"></textarea>
                  <button class="post-button">Post Comment</button>
                </div>

                <div class="comments-list">
                  <div class="comment-item">
                    <div class="comment-avatar">RK</div>
                    <div class="comment-content">
                      <div class="comment-header">
                        <span class="comment-author">Rajesh Kumar</span>
                        <span class="comment-time">2 hours ago</span>
                      </div>
                      <p class="comment-text">Uploaded the CCTV coverage report. Please review.</p>
                    </div>
                  </div>
                </div>
              </div>
            }

            @if (activeTab() === 'history') {
              <div class="history-section">
                <div class="history-timeline">
                  <div class="history-item">
                    <div class="history-icon">
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <circle cx="12" cy="12" r="10" stroke-width="2"/>
                        <path d="M12 6v6l4 2" stroke-width="2" stroke-linecap="round"/>
                      </svg>
                    </div>
                    <div class="history-content">
                      <div class="history-title">Task Created</div>
                      <div class="history-meta">By System • 5 days ago</div>
                    </div>
                  </div>
                  <div class="history-item">
                    <div class="history-icon">
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" stroke-width="2"/>
                        <circle cx="12" cy="7" r="4" stroke-width="2"/>
                      </svg>
                    </div>
                    <div class="history-content">
                      <div class="history-title">Task Assigned to Rajesh Kumar</div>
                      <div class="history-meta">By Admin • 4 days ago</div>
                    </div>
                  </div>
                </div>
              </div>
            }
          </div>
        </div>
      }

      <div class="activity-section">
        <h2 class="section-title">Recent Activity</h2>
        <div class="activity-stream">
          @for (event of activityEvents(); track event.id) {
            <div class="activity-item">
              <div class="activity-icon" [innerHTML]="event.icon"></div>
              <div class="activity-content">
                <div class="activity-title">{{ event.title }}</div>
                <div class="activity-meta">{{ event.user }} • {{ event.timestamp }}</div>
              </div>
            </div>
          }
        </div>
      </div>

      <button class="fab" (click)="toggleQuickActions()">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <circle cx="12" cy="12" r="10" stroke-width="2"/>
          <path d="M12 8v8M8 12h8" stroke-width="2" stroke-linecap="round"/>
        </svg>
      </button>

      @if (showQuickActions()) {
        <div class="quick-actions-menu">
          <button class="quick-action-item">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M12 5v14M5 12h14" stroke-width="2" stroke-linecap="round"/>
            </svg>
            Create Task
          </button>
          <button class="quick-action-item">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" stroke-width="2"/>
              <circle cx="12" cy="7" r="4" stroke-width="2"/>
            </svg>
            Assign Owner
          </button>
          <button class="quick-action-item">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" stroke-width="2" stroke-linecap="round"/>
            </svg>
            Upload Evidence
          </button>
          <button class="quick-action-item">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2" stroke-linecap="round"/>
              <path d="M22 4L12 14.01l-3-3" stroke-width="2" stroke-linecap="round"/>
            </svg>
            Mark Completed
          </button>
          <button class="quick-action-item">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2"/>
            </svg>
            Escalate Issue
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .task-command-center {
      min-height: 100vh;
      background: var(--bg-primary);
      padding: 24px;
      color: var(--text-primary);
    }

    .alert-banner {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px 20px;
      background: linear-gradient(135deg, rgba(239, 68, 68, 0.1) 0%, rgba(220, 38, 38, 0.05) 100%);
      border: 1px solid rgba(239, 68, 68, 0.2);
      border-radius: 12px;
      margin-bottom: 24px;
      animation: slideIn 0.3s ease;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-8px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .alert-icon {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(239, 68, 68, 0.1);
      border-radius: 8px;
      color: #ef4444;
      flex-shrink: 0;
    }

    .alert-content {
      flex: 1;
    }

    .alert-title {
      font-size: 15px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .alert-message {
      font-size: 13px;
      color: var(--text-secondary);
    }

    .alert-action {
      padding: 10px 20px;
      background: #ef4444;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .alert-action:hover {
      background: #dc2626;
      transform: translateY(-1px);
    }

    .section-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 16px;
    }

    .risk-radar-section {
      margin-bottom: 32px;
    }

    .risk-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
    }

    .risk-card {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 20px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      transition: all 0.2s ease;
    }

    .risk-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
    }

    .risk-card[data-severity="critical"] {
      border-left: 3px solid #ef4444;
    }

    .risk-card[data-severity="major"] {
      border-left: 3px solid #f97316;
    }

    .risk-card[data-severity="warning"] {
      border-left: 3px solid #eab308;
    }

    .risk-card[data-severity="info"] {
      border-left: 3px solid #3b82f6;
    }

    .risk-card-icon {
      width: 48px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-tertiary);
      border-radius: 10px;
      color: var(--primary-color);
    }

    .risk-card[data-severity="critical"] .risk-card-icon {
      color: #ef4444;
      background: rgba(239, 68, 68, 0.1);
    }

    .risk-card[data-severity="major"] .risk-card-icon {
      color: #f97316;
      background: rgba(249, 115, 22, 0.1);
    }

    .risk-card-content {
      flex: 1;
    }

    .risk-card-value {
      font-size: 28px;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
      margin-bottom: 4px;
    }

    .risk-card-label {
      font-size: 13px;
      color: var(--text-secondary);
      font-weight: 500;
    }

    .intelligence-section {
      margin-bottom: 32px;
    }

    .intelligence-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 20px;
    }

    .chart-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
    }

    .chart-title {
      font-size: 15px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 16px;
    }

    .bar-chart {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .bar-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .bar-label {
      font-size: 13px;
      color: var(--text-secondary);
      font-weight: 500;
    }

    .bar-wrapper {
      position: relative;
      display: flex;
      align-items: center;
      gap: 8px;
      height: 32px;
      background: var(--bg-tertiary);
      border-radius: 6px;
      overflow: hidden;
    }

    .bar-fill {
      height: 100%;
      background: var(--primary-color);
      border-radius: 6px;
      transition: width 0.6s ease;
    }

    .bar-value {
      position: absolute;
      right: 12px;
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .donut-legend {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .legend-item {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .legend-color {
      width: 16px;
      height: 16px;
      border-radius: 4px;
      flex-shrink: 0;
    }

    .legend-label {
      flex: 1;
      font-size: 13px;
      color: var(--text-secondary);
    }

    .legend-value {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .pipeline-chart {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .pipeline-stage {
      position: relative;
    }

    .pipeline-value {
      font-size: 24px;
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .pipeline-label {
      font-size: 13px;
      color: var(--text-secondary);
      margin-bottom: 8px;
    }

    .pipeline-bar {
      height: 8px;
      background: var(--primary-color);
      border-radius: 4px;
      transition: width 0.6s ease;
    }

    .workspace-section {
      margin-bottom: 32px;
    }

    .workspace-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 20px;
    }

    .workspace-controls {
      display: flex;
      gap: 16px;
    }

    .view-switcher {
      display: flex;
      gap: 4px;
      background: var(--bg-secondary);
      padding: 4px;
      border-radius: 10px;
      border: 1px solid var(--border-color);
    }

    .view-button {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 16px;
      background: transparent;
      border: none;
      border-radius: 6px;
      color: var(--text-secondary);
      font-size: 13px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .view-button:hover {
      color: var(--text-primary);
      background: var(--hover-bg);
    }

    .view-button.active {
      background: var(--card-bg);
      color: var(--primary-color);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .filters-section {
      display: flex;
      gap: 16px;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }

    .filter-chips {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
      flex: 1;
    }

    .filter-chip {
      padding: 8px 16px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      color: var(--text-secondary);
      font-size: 13px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .filter-chip:hover {
      border-color: var(--primary-color);
      color: var(--text-primary);
    }

    .filter-chip.active {
      background: var(--primary-color);
      border-color: var(--primary-color);
      color: white;
    }

    .search-box {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 16px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      min-width: 300px;
    }

    .search-box svg {
      color: var(--text-secondary);
    }

    .search-box input {
      flex: 1;
      background: transparent;
      border: none;
      outline: none;
      color: var(--text-primary);
      font-size: 14px;
    }

    .search-box input::placeholder {
      color: var(--text-secondary);
    }

    .table-view {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      overflow: hidden;
    }

    .task-table {
      width: 100%;
      border-collapse: collapse;
    }

    .task-table thead {
      background: var(--bg-secondary);
    }

    .task-table th {
      padding: 14px 16px;
      text-align: left;
      font-size: 13px;
      font-weight: 600;
      color: var(--text-secondary);
      border-bottom: 1px solid var(--border-color);
    }

    .checkbox-col {
      width: 48px;
    }

    .task-row {
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .task-row:hover {
      background: var(--hover-bg);
    }

    .task-row.overdue {
      background: rgba(239, 68, 68, 0.05);
    }

    .task-table td {
      padding: 14px 16px;
      font-size: 14px;
      color: var(--text-primary);
      border-bottom: 1px solid var(--border-color);
    }

    .task-title {
      font-weight: 500;
    }

    .clause-info {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .clause-code {
      font-size: 12px;
      font-weight: 600;
      color: var(--primary-color);
      background: var(--primary-bg-light);
      padding: 2px 6px;
      border-radius: 4px;
      display: inline-block;
      width: fit-content;
    }

    .clause-text {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .chapter-badge {
      display: inline-block;
      padding: 4px 10px;
      background: var(--bg-tertiary);
      color: var(--text-primary);
      font-size: 12px;
      font-weight: 600;
      border-radius: 6px;
    }

    .priority-badge {
      display: inline-block;
      padding: 4px 10px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 600;
      text-transform: capitalize;
    }

    .priority-badge[data-priority="critical"] {
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
    }

    .priority-badge[data-priority="high"] {
      background: rgba(249, 115, 22, 0.1);
      color: #f97316;
    }

    .priority-badge[data-priority="medium"] {
      background: rgba(234, 179, 8, 0.1);
      color: #eab308;
    }

    .priority-badge[data-priority="low"] {
      background: rgba(34, 197, 94, 0.1);
      color: #22c55e;
    }

    .status-badge {
      display: inline-block;
      padding: 4px 10px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 600;
    }

    .status-badge[data-status="pending"] {
      background: rgba(148, 163, 184, 0.1);
      color: #94a3b8;
    }

    .status-badge[data-status="assigned"] {
      background: rgba(59, 130, 246, 0.1);
      color: #3b82f6;
    }

    .status-badge[data-status="in-progress"] {
      background: rgba(234, 179, 8, 0.1);
      color: #eab308;
    }

    .status-badge[data-status="evidence-submitted"] {
      background: var(--color-primary-background);
      color: #2f6bff;
    }

    .status-badge[data-status="under-review"] {
      background: rgba(59, 130, 246, 0.1);
      color: #3b82f6;
    }

    .status-badge[data-status="completed"] {
      background: rgba(34, 197, 94, 0.1);
      color: #22c55e;
    }

    .date-col {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .overdue-badge {
      padding: 2px 6px;
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
      font-size: 11px;
      font-weight: 600;
      border-radius: 4px;
    }

    .action-button {
      padding: 6px;
      background: transparent;
      border: none;
      color: var(--text-secondary);
      cursor: pointer;
      border-radius: 6px;
      transition: all 0.2s ease;
    }

    .action-button:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }

    .kanban-view {
      display: flex;
      gap: 16px;
      overflow-x: auto;
      padding-bottom: 16px;
    }

    .kanban-column {
      min-width: 300px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 16px;
    }

    .kanban-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 16px;
    }

    .kanban-title {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .kanban-count {
      padding: 4px 8px;
      background: var(--bg-tertiary);
      color: var(--text-secondary);
      font-size: 12px;
      font-weight: 600;
      border-radius: 6px;
    }

    .kanban-cards {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .kanban-card {
      padding: 16px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 10px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .kanban-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .kanban-card-header {
      display: flex;
      gap: 8px;
      margin-bottom: 12px;
    }

    .kanban-card-title {
      font-size: 14px;
      font-weight: 500;
      color: var(--text-primary);
      margin-bottom: 12px;
    }

    .kanban-card-meta {
      display: flex;
      gap: 8px;
      margin-bottom: 12px;
    }

    .kanban-card-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 12px;
      border-top: 1px solid var(--border-color);
    }

    .assignee {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .due-date {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .risk-priority-view {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
    }

    .risk-task-item {
      display: flex;
      gap: 20px;
      padding: 16px;
      border-bottom: 1px solid var(--border-color);
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .risk-task-item:hover {
      background: var(--hover-bg);
    }

    .risk-task-item:last-child {
      border-bottom: none;
    }

    .risk-rank {
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--primary-bg-light);
      color: var(--primary-color);
      font-size: 16px;
      font-weight: 700;
      border-radius: 8px;
      flex-shrink: 0;
    }

    .risk-task-content {
      flex: 1;
    }

    .risk-task-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 8px;
    }

    .risk-task-title {
      font-size: 15px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .risk-task-meta {
      display: flex;
      gap: 8px;
      align-items: center;
      font-size: 13px;
      color: var(--text-secondary);
      margin-bottom: 12px;
    }

    .risk-score-bar {
      position: relative;
      height: 6px;
      background: var(--bg-tertiary);
      border-radius: 3px;
      overflow: hidden;
    }

    .risk-score-fill {
      height: 100%;
      background: linear-gradient(90deg, #22c55e 0%, #eab308 50%, #ef4444 100%);
      border-radius: 3px;
      transition: width 0.6s ease;
    }

    .risk-score-value {
      position: absolute;
      right: 0;
      top: -20px;
      font-size: 11px;
      color: var(--text-secondary);
    }

    .task-detail-panel {
      position: fixed;
      right: 0;
      top: 64px;
      width: 480px;
      height: calc(100vh - 64px);
      background: var(--card-bg);
      border-left: 1px solid var(--border-color);
      box-shadow: -4px 0 20px rgba(0, 0, 0, 0.1);
      z-index: 50;
      overflow-y: auto;
      animation: slideInRight 0.3s ease;
    }

    @keyframes slideInRight {
      from {
        transform: translateX(100%);
      }
      to {
        transform: translateX(0);
      }
    }

    .panel-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px;
      border-bottom: 1px solid var(--border-color);
      position: sticky;
      top: 0;
      background: var(--card-bg);
      z-index: 10;
    }

    .panel-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .close-button {
      padding: 8px;
      background: transparent;
      border: none;
      color: var(--text-secondary);
      cursor: pointer;
      border-radius: 6px;
      transition: all 0.2s ease;
    }

    .close-button:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }

    .panel-tabs {
      display: flex;
      border-bottom: 1px solid var(--border-color);
      padding: 0 20px;
      position: sticky;
      top: 65px;
      background: var(--card-bg);
      z-index: 9;
    }

    .panel-tab {
      padding: 14px 20px;
      background: transparent;
      border: none;
      border-bottom: 2px solid transparent;
      color: var(--text-secondary);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .panel-tab:hover {
      color: var(--text-primary);
    }

    .panel-tab.active {
      color: var(--primary-color);
      border-bottom-color: var(--primary-color);
    }

    .panel-content {
      padding: 20px;
    }

    .detail-section {
    }

    .detail-title {
      font-size: 20px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 12px;
    }

    .detail-description {
      font-size: 14px;
      color: var(--text-secondary);
      line-height: 1.6;
      margin-bottom: 24px;
    }

    .detail-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 20px;
    }

    .detail-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .detail-item label {
      font-size: 12px;
      color: var(--text-secondary);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .detail-value {
      font-size: 14px;
      color: var(--text-primary);
      font-weight: 500;
    }

    .evidence-section {
    }

    .upload-button {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 20px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      margin-bottom: 20px;
      transition: all 0.2s ease;
    }

    .upload-button:hover {
      background: var(--primary-hover);
      transform: translateY(-1px);
    }

    .evidence-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .evidence-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 10px;
    }

    .evidence-icon {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-tertiary);
      border-radius: 8px;
      color: var(--primary-color);
    }

    .evidence-info {
      flex: 1;
    }

    .evidence-name {
      font-size: 14px;
      font-weight: 500;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .evidence-meta {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .comments-section {
    }

    .comment-input {
      margin-bottom: 20px;
    }

    .comment-input textarea {
      width: 100%;
      padding: 12px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      color: var(--text-primary);
      font-size: 14px;
      font-family: inherit;
      resize: vertical;
      margin-bottom: 12px;
    }

    .post-button {
      padding: 10px 20px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .post-button:hover {
      background: var(--primary-hover);
    }

    .comments-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .comment-item {
      display: flex;
      gap: 12px;
    }

    .comment-avatar {
      width: 36px;
      height: 36px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--primary-color);
      color: white;
      font-size: 13px;
      font-weight: 600;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .comment-content {
      flex: 1;
    }

    .comment-header {
      display: flex;
      gap: 8px;
      margin-bottom: 6px;
    }

    .comment-author {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .comment-time {
      font-size: 13px;
      color: var(--text-secondary);
    }

    .comment-text {
      font-size: 14px;
      color: var(--text-primary);
      line-height: 1.5;
    }

    .history-section {
    }

    .history-timeline {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .history-item {
      display: flex;
      gap: 12px;
    }

    .history-icon {
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-tertiary);
      border-radius: 8px;
      color: var(--primary-color);
      flex-shrink: 0;
    }

    .history-content {
    }

    .history-title {
      font-size: 14px;
      font-weight: 500;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .history-meta {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .activity-section {
      margin-bottom: 32px;
    }

    .activity-stream {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .activity-item {
      display: flex;
      gap: 12px;
      align-items: flex-start;
    }

    .activity-icon {
      width: 36px;
      height: 36px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-tertiary);
      border-radius: 8px;
      color: var(--primary-color);
      flex-shrink: 0;
    }

    .activity-content {
      flex: 1;
    }

    .activity-title {
      font-size: 14px;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .activity-meta {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .fab {
      position: fixed;
      bottom: 32px;
      right: 32px;
      width: 56px;
      height: 56px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 50%;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
      cursor: pointer;
      transition: all 0.2s ease;
      z-index: 40;
    }

    .fab:hover {
      transform: scale(1.1);
      box-shadow: 0 6px 16px rgba(0, 0, 0, 0.3);
    }

    .quick-actions-menu {
      position: fixed;
      bottom: 100px;
      right: 32px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
      padding: 8px;
      z-index: 40;
      animation: scaleIn 0.2s ease;
    }

    @keyframes scaleIn {
      from {
        opacity: 0;
        transform: scale(0.9);
      }
      to {
        opacity: 1;
        transform: scale(1);
      }
    }

    .quick-action-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      background: transparent;
      border: none;
      border-radius: 8px;
      color: var(--text-primary);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
      width: 100%;
      text-align: left;
    }

    .quick-action-item:hover {
      background: var(--hover-bg);
    }

    @media (max-width: 1200px) {
      .task-detail-panel {
        width: 400px;
      }
    }

    @media (max-width: 768px) {
      .task-command-center {
        padding: 16px;
      }

      .risk-cards {
        grid-template-columns: 1fr;
      }

      .intelligence-grid {
        grid-template-columns: 1fr;
      }

      .task-detail-panel {
        width: 100%;
        left: 0;
      }

      .kanban-view {
        flex-direction: column;
      }

      .kanban-column {
        min-width: 100%;
      }

      .search-box {
        min-width: 100%;
      }
    }
  `],
})
export class TaskCommandCenterComponent implements OnInit {
  viewMode = signal<ViewMode>('table');
  activeFilter = signal<string>('all');
  selectedTask = signal<TaskItem | null>(null);
  activeTab = signal<string>('details');
  showQuickActions = signal(false);
  searchQuery = '';

  readonly quickFilters = signal([
    { id: 'all', label: 'All Tasks' },
    { id: 'my-tasks', label: 'My Tasks' },
    { id: 'overdue', label: 'Overdue' },
    { id: 'high-priority', label: 'High Priority' },
    { id: 'due-this-week', label: 'Due This Week' },
    { id: 'critical', label: 'Critical Tasks' },
  ]);

  readonly detailTabs = signal([
    { id: 'details', label: 'Details' },
    { id: 'evidence', label: 'Evidence' },
    { id: 'comments', label: 'Comments' },
    { id: 'history', label: 'History' },
  ]);

  readonly kanbanColumns = signal([
    { id: 'pending', label: 'Pending' },
    { id: 'assigned', label: 'Assigned' },
    { id: 'in-progress', label: 'In Progress' },
    { id: 'evidence-submitted', label: 'Evidence Submitted' },
    { id: 'under-review', label: 'Under Review' },
    { id: 'completed', label: 'Completed' },
  ]);

  readonly riskCards = signal<RiskCard[]>([
    {
      label: 'Critical Tasks',
      value: 3,
      severity: 'critical',
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M12 8v4M12 16h.01" stroke-width="2" stroke-linecap="round"/></svg>',
    },
    {
      label: 'Major Issues',
      value: 8,
      severity: 'major',
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2"/></svg>',
    },
    {
      label: 'Overdue Tasks',
      value: 2,
      severity: 'critical',
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M12 6v6l4 2" stroke-width="2" stroke-linecap="round"/></svg>',
    },
    {
      label: 'Due This Week',
      value: 14,
      severity: 'warning',
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="4" width="18" height="18" rx="2" stroke-width="2"/><path d="M16 2v4M8 2v4M3 10h18" stroke-width="2"/></svg>',
    },
    {
      label: 'Tasks Without Evidence',
      value: 5,
      severity: 'info',
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/><path d="M13 2v7h7" stroke-width="2"/></svg>',
    },
  ]);

  readonly departmentData = signal<ChartData[]>([
    { label: 'Security', value: 12, color: '#3b82f6' },
    { label: 'Administration', value: 18, color: '#0d9488' },
    { label: 'Clinical', value: 9, color: '#10b981' },
    { label: 'Facility', value: 15, color: '#f59e0b' },
    { label: 'IT', value: 7, color: '#ef4444' },
  ]);

  readonly statusData = signal<ChartData[]>([
    { label: 'Pending', value: 8, color: '#94a3b8' },
    { label: 'In Progress', value: 15, color: '#eab308' },
    { label: 'Under Review', value: 12, color: '#3b82f6' },
    { label: 'Completed', value: 26, color: '#22c55e' },
  ]);

  readonly chapterData = signal<ChartData[]>([
    { label: 'ACC', value: 14, color: '#3b82f6' },
    { label: 'PCC', value: 10, color: '#0d9488' },
    { label: 'COP', value: 8, color: '#10b981' },
    { label: 'FMS', value: 12, color: '#f59e0b' },
    { label: 'MOC', value: 9, color: '#ef4444' },
    { label: 'QMS', value: 8, color: '#06b6d4' },
  ]);

  readonly pipelineStages = signal([
    { label: 'Findings Raised', value: 78 },
    { label: 'Tasks Assigned', value: 61 },
    { label: 'Evidence Submitted', value: 45 },
    { label: 'Tasks Completed', value: 26 },
  ]);

  readonly tasks = signal<TaskItem[]>([
    {
      id: '1',
      title: 'CCTV coverage gaps in emergency department',
      clause: 'Security and Safety Management',
      clauseCode: 'ACC.5.1',
      chapter: 'Access, Continuity and Care',
      chapterCode: 'ACC',
      owner: 'Rajesh Kumar',
      department: 'Security',
      priority: 'critical',
      status: 'in-progress',
      dueDate: '2026-03-20',
      overdue: false,
      riskScore: 92,
      description: 'Critical security gaps identified in emergency department CCTV coverage during internal audit',
      evidenceCount: 2,
      commentCount: 5,
    },
    {
      id: '2',
      title: 'Fire exit signage missing in OPD block',
      clause: 'Fire Safety Compliance',
      clauseCode: 'FMS.3.2',
      chapter: 'Facility Management',
      chapterCode: 'FMS',
      owner: 'Priya Sharma',
      department: 'Facility',
      priority: 'high',
      status: 'evidence-submitted',
      dueDate: '2026-03-18',
      overdue: false,
      riskScore: 85,
      description: 'Multiple fire exit signs missing or damaged in OPD block',
      evidenceCount: 4,
      commentCount: 3,
    },
    {
      id: '3',
      title: 'Visitor log compliance incomplete',
      clause: 'Visitor Management Protocol',
      clauseCode: 'ACC.2.4',
      chapter: 'Access, Continuity and Care',
      chapterCode: 'ACC',
      owner: 'Amit Patel',
      department: 'Administration',
      priority: 'medium',
      status: 'under-review',
      dueDate: '2026-03-25',
      overdue: false,
      riskScore: 68,
      description: 'Visitor logs not maintained according to standard protocol',
      evidenceCount: 1,
      commentCount: 2,
    },
    {
      id: '4',
      title: 'Hand hygiene compliance audit',
      clause: 'Infection Prevention and Control',
      clauseCode: 'PCC.7.1',
      chapter: 'Patient Care and Care',
      chapterCode: 'PCC',
      owner: 'Dr. Meera Singh',
      department: 'Clinical',
      priority: 'high',
      status: 'in-progress',
      dueDate: '2026-03-22',
      overdue: false,
      riskScore: 78,
      description: 'Quarterly hand hygiene compliance assessment required',
      evidenceCount: 3,
      commentCount: 7,
    },
    {
      id: '5',
      title: 'Emergency medication stock audit',
      clause: 'Medication Management',
      clauseCode: 'MOC.6.3',
      chapter: 'Management of Medications',
      chapterCode: 'MOC',
      owner: 'Pharmacist Ravi',
      department: 'Clinical',
      priority: 'critical',
      status: 'pending',
      dueDate: '2026-03-16',
      overdue: true,
      riskScore: 95,
      description: 'Critical medication stock levels below minimum threshold',
      evidenceCount: 0,
      commentCount: 1,
    },
  ]);

  readonly activityEvents = signal<ActivityEvent[]>([
    {
      id: '1',
      type: 'evidence',
      title: 'Rajesh Kumar uploaded CCTV installation report',
      user: 'Rajesh Kumar',
      timestamp: '2 hours ago',
      icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" stroke-width="2" stroke-linecap="round"/></svg>',
    },
    {
      id: '2',
      type: 'status',
      title: 'Priya Sharma updated fire exit signage task',
      user: 'Priya Sharma',
      timestamp: '4 hours ago',
      icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2" stroke-linecap="round"/><path d="M22 4L12 14.01l-3-3" stroke-width="2" stroke-linecap="round"/></svg>',
    },
    {
      id: '3',
      type: 'finding',
      title: 'Audit team raised major finding in ACC chapter',
      user: 'Audit Team',
      timestamp: '1 day ago',
      icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2"/></svg>',
    },
  ]);

  readonly maxDepartmentValue = computed(() =>
    Math.max(...this.departmentData().map(d => d.value))
  );

  readonly maxChapterValue = computed(() =>
    Math.max(...this.chapterData().map(d => d.value))
  );

  readonly maxPipelineValue = computed(() =>
    Math.max(...this.pipelineStages().map(s => s.value))
  );

  readonly filteredTasks = computed(() => {
    const filter = this.activeFilter();
    const allTasks = this.tasks();

    if (filter === 'all') return allTasks;
    if (filter === 'overdue') return allTasks.filter(t => t.overdue);
    if (filter === 'high-priority') return allTasks.filter(t => t.priority === 'high' || t.priority === 'critical');
    if (filter === 'critical') return allTasks.filter(t => t.priority === 'critical');

    return allTasks;
  });

  readonly sortedByRisk = computed(() => {
    return [...this.filteredTasks()].sort((a, b) => b.riskScore - a.riskScore);
  });

  readonly criticalTaskCount = computed(() =>
    this.tasks().filter(t => t.priority === 'critical').length
  );

  readonly hasCriticalTasks = computed(() =>
    this.criticalTaskCount() > 0
  );

  ngOnInit(): void {
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
  }

  setFilter(filterId: string): void {
    this.activeFilter.set(filterId);
  }

  filterCriticalTasks(): void {
    this.setFilter('critical');
  }

  selectTask(task: TaskItem): void {
    this.selectedTask.set(task);
    this.activeTab.set('details');
  }

  closeTaskPanel(): void {
    this.selectedTask.set(null);
  }

  setActiveTab(tabId: string): void {
    this.activeTab.set(tabId);
  }

  getColumnTasks(columnId: string): TaskItem[] {
    return this.filteredTasks().filter(task => task.status === columnId);
  }

  toggleQuickActions(): void {
    this.showQuickActions.update(v => !v);
  }

  openTaskMenu(event: Event, task: TaskItem): void {
    event.stopPropagation();
  }

  getStatusLabel(status: string | undefined): string {
    if (!status) return '';
    return status.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ');
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
