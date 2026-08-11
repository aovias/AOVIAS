import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { TaskReport } from '../models/report.models';
import { ReportsApiService } from '../services/reports-api.service';
import { exportReportFile, loadReport } from '../services/report-page.helper';

@Component({
  selector: 'app-task-reports',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="task-reports-page">
      <div class="page-header">
        <h1 class="page-title">Task Reports</h1>
        <p class="page-description">Track operational task metrics including completion rates, overdue items, and department workloads.</p>
      </div>

      <div style="margin-bottom:12px">
        <button type="button" (click)="load()">Refresh</button>
        <button type="button" (click)="onExport()">Export</button>
      </div>
      @if (loading()) { <p class="page-description">Loading…</p> }
      @if (error()) { <p class="page-description" style="color:#ef4444">{{ error() }} <button type="button" (click)="load()">Retry</button></p> }
      <div class="metrics-grid">
        <div class="metric-card" style="border-left-color: #3b82f6">
          <div class="metric-value">{{ completionRate }}%</div>
          <div class="metric-label">Completion Rate</div>
        </div>
        <div class="metric-card" style="border-left-color: #ef4444">
          <div class="metric-value">{{ overdueTasks }}</div>
          <div class="metric-label">Overdue Tasks</div>
        </div>
        <div class="metric-card" style="border-left-color: #10b981">
          <div class="metric-value">{{ completedTasks }}</div>
          <div class="metric-label">Completed</div>
        </div>
        <div class="metric-card" style="border-left-color: #f59e0b">
          <div class="metric-value">{{ inProgressTasks }}</div>
          <div class="metric-label">In Progress</div>
        </div>
      </div>

      <div class="charts-section">
        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">Tasks by Department</h3>
          </div>
          <div class="chart-body">
            @for (dept of departmentTasks; track dept.name) {
              <div class="dept-row">
                <div class="dept-name">{{ dept.name }}</div>
                <div class="dept-bar-container">
                  <div class="dept-bar" [style.width.%]="maxDeptCount ? (dept.count / maxDeptCount) * 100 : 0"></div>
                </div>
                <div class="dept-count">{{ dept.count }} tasks</div>
              </div>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .task-reports-page {
      padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      background: var(--bg-primary);
      min-height: calc(100vh - 64px);
    }

    .page-title {
      font-size: var(--font-size-page-title);
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 8px 0;
    }

    .page-description {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 0 0 24px 0;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 32px;
    }

    .metric-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-left: 4px solid;
      border-radius: 8px;
      padding: 20px;
    }

    .metric-value {
      font-size: 32px;
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: 8px;
    }

    .metric-label {
      font-size: 13px;
      font-weight: 500;
      color: var(--text-secondary);
    }

    .charts-section {
      display: grid;
      gap: 24px;
    }

    .chart-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
    }

    .chart-header {
      padding: 20px;
      border-bottom: 1px solid var(--border-color);
    }

    .chart-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .chart-body {
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .dept-row {
      display: grid;
      grid-template-columns: 150px 1fr 100px;
      gap: 16px;
      align-items: center;
    }

    .dept-name {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .dept-bar-container {
      height: 32px;
      background: var(--bg-primary);
      border-radius: 6px;
      overflow: hidden;
    }

    .dept-bar {
      height: 100%;
      background: linear-gradient(90deg, var(--color-status-info), var(--color-chart-chapter-trend));
      border-radius: 6px;
    }

    .dept-count {
      font-size: 13px;
      color: var(--text-secondary);
      text-align: right;
    }
  `]
})
export class TaskReportsComponent implements OnInit {
  private readonly api = inject(ReportsApiService);
  private readonly context = inject(ComplianceContextService);
  private readonly destroyRef = inject(DestroyRef);

  loading = signal(false);
  error = signal<string | null>(null);
  completionRate = 0;
  overdueTasks = 0;
  completedTasks = 0;
  inProgressTasks = 0;
  departmentTasks: { name: string; count: number }[] = [];
  maxDeptCount = 1;

  ngOnInit(): void {
    this.context.hospitalId$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    this.load();
  }

  load(): void {
    loadReport(() => this.api.getTaskReport(), (r) => this.apply(r), this.loading, this.error);
  }

  onExport(): void {
    exportReportFile(this.api, 'tasks', 'pdf');
  }

  private apply(r: TaskReport): void {
    this.completionRate = r.completionRate;
    this.overdueTasks = r.overdueTasks;
    this.completedTasks = r.completedTasks;
    this.inProgressTasks = r.inProgressTasks;
    this.departmentTasks = (r.departmentTasks ?? []).map((d) => ({
      name: d.departmentName,
      count: d.count,
    }));
    this.maxDeptCount = Math.max(1, ...this.departmentTasks.map((d) => d.count));
  }
}
