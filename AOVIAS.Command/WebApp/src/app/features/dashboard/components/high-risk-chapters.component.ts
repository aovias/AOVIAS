import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface FrameworkOverviewRow {
  chapter: string;
  score: number;
  riskLevel: 'High' | 'Medium' | 'Low';
  openTasks?: number;
  overdueTasks?: number;
}

@Component({
  selector: 'app-high-risk-chapters',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="chapters-container">
      <h3 class="chapters-title">{{ title() }}</h3>
      <div class="table-wrapper">
        <table class="chapters-table">
          <thead>
            <tr>
              <th>Framework</th>
              <th>Score</th>
              <th>Status</th>
              @if (showTaskColumns()) {
                <th class="col-tasks">Open</th>
                <th class="col-tasks">Overdue</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (chapter of chapters(); track chapter.chapter) {
              <tr [attr.data-risk]="chapter.riskLevel">
                <td class="chapter-name">{{ chapter.chapter }}</td>
                <td class="chapter-score">
                  <div class="score-wrapper">
                    <div class="score-bar">
                      <div class="score-fill" [style.width.%]="chapter.score"></div>
                    </div>
                    <span class="score-text">{{ chapter.score }}%</span>
                  </div>
                </td>
                <td class="chapter-risk">
                  <span class="risk-badge" [attr.data-level]="chapter.riskLevel">
                    {{ chapter.riskLevel }}
                  </span>
                </td>
                @if (showTaskColumns()) {
                  <td class="col-tasks">
                    <span class="task-count">{{ chapter.openTasks ?? 0 }}</span>
                  </td>
                  <td class="col-tasks">
                    <span
                      class="task-count"
                      [class.overdue-highlight]="(chapter.overdueTasks ?? 0) > 0"
                    >
                      {{ chapter.overdueTasks ?? 0 }}
                    </span>
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
      }

      .chapters-container {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        box-shadow: var(--shadow-sm);
        transition: all 0.3s ease;
        height: 100%;
        box-sizing: border-box;
        display: flex;
        flex-direction: column;
      }

      .table-wrapper {
        flex: 1;
      }

      .chapters-container:hover {
        box-shadow: var(--shadow-md);
      }

      .chapters-title {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
        margin-bottom: 12px;
      }

      .table-wrapper {
        overflow-x: auto;
      }

      .chapters-table {
        width: 100%;
        border-collapse: collapse;
      }

      .chapters-table thead tr {
        border-bottom: 2px solid var(--border-color);
      }

      .chapters-table th {
        padding: 8px 12px;
        text-align: left;
        font-size: 13px;
        font-weight: 600;
        color: var(--text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }

      .chapters-table th.col-tasks {
        text-align: center;
        min-width: 70px;
      }

      .chapters-table tbody tr {
        border-bottom: 1px solid var(--border-color);
        transition: background 0.2s ease;
      }

      .chapters-table tbody tr:hover {
        background: var(--hover-bg);
      }

      .chapters-table tbody tr[data-risk='High'] {
        background: rgba(239, 68, 68, 0.03);
      }

      .chapters-table tbody tr[data-risk='High']:hover {
        background: rgba(239, 68, 68, 0.08);
      }

      .chapters-table td {
        padding: 10px 12px;
        font-size: 14px;
      }

      .chapters-table td.col-tasks {
        text-align: center;
      }

      .chapter-name {
        font-weight: 600;
        color: var(--text-primary);
      }

      .score-wrapper {
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .score-bar {
        flex: 1;
        height: 6px;
        background: var(--border-color);
        border-radius: 4px;
        overflow: hidden;
      }

      .score-fill {
        height: 100%;
        background: linear-gradient(90deg, #ef4444, #f59e0b, #22c55e);
        border-radius: 4px;
        transition: width 0.6s ease;
      }

      .score-text {
        font-weight: 600;
        color: var(--text-primary);
        min-width: 45px;
      }

      .risk-badge {
        display: inline-flex;
        align-items: center;
        padding: 4px 10px;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.3px;
      }

      .risk-badge[data-level='High'] {
        background: rgba(239, 68, 68, 0.1);
        color: #dc2626;
      }

      .risk-badge[data-level='Medium'] {
        background: rgba(245, 158, 11, 0.1);
        color: #d97706;
      }

      .risk-badge[data-level='Low'] {
        background: rgba(34, 197, 94, 0.1);
        color: #16a34a;
      }

      .task-count {
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .overdue-highlight {
        color: var(--danger-color);
        background: rgba(239, 68, 68, 0.08);
        padding: 4px 10px;
        border-radius: 6px;
      }

      @media (max-width: 640px) {
        .chapters-table th,
        .chapters-table td {
          padding: 12px 8px;
          font-size: 13px;
        }

        .score-wrapper {
          flex-direction: column;
          align-items: flex-start;
          gap: 8px;
        }

        .score-bar {
          width: 100%;
        }
      }
    `,
  ],
})
export class HighRiskChaptersComponent {
  title = input<string>('Framework Overview');
  chapters = input.required<FrameworkOverviewRow[]>();
  showTaskColumns = input<boolean>(false);
}
