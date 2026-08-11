import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

export interface HighRiskChapter {
  chapter: string;
  complianceScore: number;
  riskLevel: 'high' | 'medium' | 'low';
  openIssues: number;
}

export interface DepartmentRisk {
  department: string;
  riskScore: number;
  riskLevel: 'high' | 'medium' | 'low';
}

@Component({
  selector: 'app-risk-command-center',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="risk-command-center">
      <div class="section-heading">
        <div>
          <h2 class="section-title">Chapter &amp; Department Risk</h2>
          <p class="section-note">
            Compliance readiness risk by chapter/department — not the
            <a routerLink="/app/risks/register">Enterprise Risk Register</a>.
          </p>
        </div>
        <a class="register-link" routerLink="/app/risks/register">Open Risk Register →</a>
      </div>

      <div class="risk-grid">
        <div class="high-risk-chapters">
          <div class="card-header">
            <h3 class="card-title">High Risk Chapters</h3>
            <span class="badge badge-danger">{{ highRiskChapters.length }} Critical</span>
          </div>

          <div class="table-container">
            <table class="risk-table">
              <thead>
                <tr>
                  <th>Chapter</th>
                  <th>Compliance</th>
                  <th>Risk Level</th>
                  <th>Open Issues</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let chapter of highRiskChapters" class="table-row">
                  <td class="chapter-cell">{{ chapter.chapter }}</td>
                  <td class="score-cell">
                    <div class="score-wrapper">
                      <div class="score-bar">
                        <div class="score-fill"
                             [style.width.%]="chapter.complianceScore"
                             [class.score-low]="chapter.complianceScore < 60"
                             [class.score-medium]="chapter.complianceScore >= 60 && chapter.complianceScore < 80"
                             [class.score-high]="chapter.complianceScore >= 80">
                        </div>
                      </div>
                      <span class="score-value">{{ chapter.complianceScore }}%</span>
                    </div>
                  </td>
                  <td class="risk-cell">
                    <span class="risk-badge"
                          [class.risk-high]="chapter.riskLevel === 'high'"
                          [class.risk-medium]="chapter.riskLevel === 'medium'"
                          [class.risk-low]="chapter.riskLevel === 'low'">
                      {{ chapter.riskLevel }}
                    </span>
                  </td>
                  <td class="issues-cell">
                    <span class="issues-count">{{ chapter.openIssues }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="department-risk">
          <div class="card-header">
            <h3 class="card-title">Department Risk Analysis</h3>
          </div>

          <div class="department-list">
            <div *ngFor="let dept of departmentRisks" class="department-item">
              <div class="department-info">
                <span class="department-name">{{ dept.department }}</span>
                <span class="department-score">{{ dept.riskScore }}%</span>
              </div>
              <div class="risk-bar-container">
                <div class="risk-bar"
                     [style.width.%]="dept.riskScore"
                     [class.risk-high]="dept.riskLevel === 'high'"
                     [class.risk-medium]="dept.riskLevel === 'medium'"
                     [class.risk-low]="dept.riskLevel === 'low'">
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .risk-command-center {
      margin-bottom: 24px;
    }

    .section-heading {
      display: flex;
      justify-content: space-between;
      gap: 16px;
      align-items: flex-start;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }

    .section-title {
      font-size: 20px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 6px 0;
    }

    .section-note {
      margin: 0;
      font-size: 13px;
      color: var(--text-secondary, #6b7280);
      max-width: 520px;
    }

    .section-note a,
    .register-link {
      color: var(--color-brand-primary, var(--color-primary));
      text-decoration: none;
      font-weight: 600;
      font-size: 13px;
      white-space: nowrap;
    }

    .risk-grid {
      display: grid;
      grid-template-columns: 1.5fr 1fr;
      gap: 20px;
    }

    .high-risk-chapters,
    .department-risk {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 24px;
      box-shadow: var(--shadow-sm);
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .card-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .badge {
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
    }

    .badge-danger {
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color);
    }

    .table-container {
      overflow-x: auto;
    }

    .risk-table {
      width: 100%;
      border-collapse: collapse;
    }

    .risk-table thead th {
      text-align: left;
      font-size: 12px;
      font-weight: 600;
      color: var(--text-secondary);
      text-transform: uppercase;
      padding: 12px 8px;
      border-bottom: 2px solid var(--border-color);
      letter-spacing: 0.5px;
    }

    .table-row {
      transition: background 0.2s ease;
    }

    .table-row:hover {
      background: var(--hover-bg);
    }

    .risk-table td {
      padding: 16px 8px;
      border-bottom: 1px solid var(--border-color);
    }

    .chapter-cell {
      font-weight: 600;
      color: var(--text-primary);
      font-size: 14px;
    }

    .score-wrapper {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .score-bar {
      flex: 1;
      height: 6px;
      background: var(--bg-tertiary);
      border-radius: 3px;
      overflow: hidden;
    }

    .score-fill {
      height: 100%;
      border-radius: 3px;
      transition: width 0.5s ease;
    }

    .score-low {
      background: var(--danger-color);
    }

    .score-medium {
      background: var(--warning-color);
    }

    .score-high {
      background: var(--success-color);
    }

    .score-value {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
      min-width: 40px;
    }

    .risk-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
      text-transform: capitalize;
    }

    .risk-high {
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color);
    }

    .risk-medium {
      background: rgba(245, 158, 11, 0.1);
      color: var(--warning-color);
    }

    .risk-low {
      background: rgba(16, 185, 129, 0.1);
      color: var(--success-color);
    }

    .issues-count {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color);
      border-radius: 50%;
      font-size: 12px;
      font-weight: 700;
    }

    .department-list {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .department-item {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .department-info {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .department-name {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .department-score {
      font-size: 14px;
      font-weight: 700;
      color: var(--text-primary);
    }

    .risk-bar-container {
      height: 10px;
      background: var(--bg-tertiary);
      border-radius: 5px;
      overflow: hidden;
    }

    .risk-bar {
      height: 100%;
      border-radius: 5px;
      transition: width 0.5s ease;
    }

    @media (max-width: 1024px) {
      .risk-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class RiskCommandCenterComponent {
  @Input() highRiskChapters: HighRiskChapter[] = [];
  @Input() departmentRisks: DepartmentRisk[] = [];
}
