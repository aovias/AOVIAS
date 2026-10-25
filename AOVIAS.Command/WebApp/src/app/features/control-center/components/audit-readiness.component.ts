import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface AuditReadinessData {
  confidenceScore: number;
  criticalFindings: number;
  correctiveActionsPending: number;
  clausesMissingEvidence: number;
  auditPassProbability: number;
}

@Component({
  selector: 'app-audit-readiness',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="audit-readiness">
      <h2 class="section-title">Audit Readiness</h2>

      <div class="readiness-card">
        <div class="confidence-section">
          <h3 class="subsection-title">Audit Confidence Score</h3>

          <div class="circular-indicator">
            <svg viewBox="0 0 200 200" class="confidence-circle">
              <circle
                cx="100"
                cy="100"
                r="85"
                fill="none"
                stroke="#e5e7eb"
                stroke-width="12"
              />

              <circle
                cx="100"
                cy="100"
                r="85"
                fill="none"
                [attr.stroke]="getScoreColor(data.confidenceScore)"
                stroke-width="12"
                [attr.stroke-dasharray]="getCircleDashArray(data.confidenceScore)"
                stroke-dashoffset="0"
                stroke-linecap="round"
                transform="rotate(-90 100 100)"
                class="progress-circle"
              />

              <text x="100" y="90" text-anchor="middle" font-size="48" font-weight="700" fill="#111827">
                {{ data.confidenceScore }}
              </text>
              <text x="100" y="115" text-anchor="middle" font-size="16" fill="#6b7280">
                Confidence
              </text>
            </svg>

            <div class="score-label" [class]="getScoreLabelClass(data.confidenceScore)">
              {{ getScoreLabel(data.confidenceScore) }}
            </div>
          </div>
        </div>

        <div class="metrics-section">
          <div class="metric-card critical">
            <div class="metric-icon">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="metric-content">
              <div class="metric-value">{{ data.criticalFindings }}</div>
              <div class="metric-label">Critical Findings</div>
            </div>
          </div>

          <div class="metric-card warning">
            <div class="metric-icon">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M6 2a1 1 0 00-1 1v1H4a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V6a2 2 0 00-2-2h-1V3a1 1 0 10-2 0v1H7V3a1 1 0 00-1-1zm0 5a1 1 0 000 2h8a1 1 0 100-2H6z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="metric-content">
              <div class="metric-value">{{ data.correctiveActionsPending }}</div>
              <div class="metric-label">Corrective Actions</div>
            </div>
          </div>

          <div class="metric-card info">
            <div class="metric-icon">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4zm2 6a1 1 0 011-1h6a1 1 0 110 2H7a1 1 0 01-1-1zm1 3a1 1 0 100 2h6a1 1 0 100-2H7z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="metric-content">
              <div class="metric-value">{{ data.clausesMissingEvidence }}</div>
              <div class="metric-label">Clauses Missing Evidence</div>
            </div>
          </div>
        </div>

        <div class="probability-section">
          <div class="probability-header">
            <h3 class="subsection-title">Audit Pass Probability</h3>
            <span class="probability-value">{{ data.auditPassProbability }}%</span>
          </div>

          <div class="probability-bar-wrapper">
            <div class="probability-bar">
              <div class="probability-fill"
                   [style.width.%]="data.auditPassProbability"
                   [class]="getProbabilityClass(data.auditPassProbability)">
                <div class="probability-shimmer"></div>
              </div>
            </div>
            <div class="probability-markers">
              <span class="marker" style="left: 60%;">Pass Threshold</span>
            </div>
          </div>

          <div class="probability-insights">
            <div class="insight-item" *ngIf="data.auditPassProbability >= 80">
              <span class="insight-icon success">✓</span>
              <span class="insight-text">High likelihood of passing audit</span>
            </div>
            <div class="insight-item" *ngIf="data.auditPassProbability < 80 && data.auditPassProbability >= 60">
              <span class="insight-icon warning">⚠</span>
              <span class="insight-text">Additional preparation recommended</span>
            </div>
            <div class="insight-item" *ngIf="data.auditPassProbability < 60">
              <span class="insight-icon critical">✕</span>
              <span class="insight-text">Significant gaps require immediate attention</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .audit-readiness {
      margin-bottom: 24px;
    }

    .section-title {
      font-size: 20px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 20px 0;
    }

    .readiness-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 28px;
      box-shadow: var(--shadow-sm);
    }

    .confidence-section {
      text-align: center;
      margin-bottom: 32px;
    }

    .subsection-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 24px 0;
    }

    .circular-indicator {
      display: inline-block;
      position: relative;
    }

    .confidence-circle {
      width: 200px;
      height: 200px;
    }

    .progress-circle {
      transition: stroke-dasharray 1s ease;
    }

    .score-label {
      margin-top: 12px;
      padding: 6px 20px;
      border-radius: 20px;
      font-size: 14px;
      font-weight: 600;
      display: inline-block;
    }

    .score-label.excellent {
      background: rgba(16, 185, 129, 0.1);
      color: var(--success-color);
    }

    .score-label.good {
      background: var(--primary-bg-light);
      color: var(--primary-color);
    }

    .score-label.fair {
      background: rgba(245, 158, 11, 0.1);
      color: var(--warning-color);
    }

    .score-label.poor {
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color);
    }

    .metrics-section {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 32px;
    }

    .metric-card {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 20px;
      border-radius: 12px;
      transition: transform 0.2s ease;
    }

    .metric-card:hover {
      transform: translateY(-2px);
    }

    .metric-card.critical {
      background: rgba(239, 68, 68, 0.1);
    }

    .metric-card.warning {
      background: rgba(245, 158, 11, 0.1);
    }

    .metric-card.info {
      background: var(--primary-bg-light);
    }

    .metric-icon {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--card-bg);
      flex-shrink: 0;
    }

    .metric-card.critical .metric-icon {
      color: var(--danger-color);
    }

    .metric-card.warning .metric-icon {
      color: var(--warning-color);
    }

    .metric-card.info .metric-icon {
      color: var(--primary-color);
    }

    .metric-icon svg {
      width: 24px;
      height: 24px;
    }

    .metric-content {
      flex: 1;
    }

    .metric-value {
      font-size: 28px;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
      margin-bottom: 4px;
    }

    .metric-label {
      font-size: 13px;
      font-weight: 500;
      color: var(--text-secondary);
    }

    .probability-section {
      padding-top: 24px;
      border-top: 1px solid var(--border-color);
    }

    .probability-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .probability-value {
      font-size: 24px;
      font-weight: 700;
      color: var(--text-primary);
    }

    .probability-bar-wrapper {
      position: relative;
      margin-bottom: 16px;
    }

    .probability-bar {
      height: 16px;
      background: var(--bg-tertiary);
      border-radius: 8px;
      overflow: hidden;
      position: relative;
    }

    .probability-fill {
      height: 100%;
      border-radius: 8px;
      position: relative;
      overflow: hidden;
      transition: width 1s ease;
    }

    .probability-fill.high {
      background: var(--success-color);
    }

    .probability-fill.medium {
      background: var(--warning-color);
    }

    .probability-fill.low {
      background: var(--danger-color);
    }

    .probability-shimmer {
      position: absolute;
      top: 0;
      left: -100%;
      width: 100%;
      height: 100%;
      background: linear-gradient(90deg, transparent, rgba(255,255,255,0.3), transparent);
      animation: shimmer 2s infinite;
    }

    @keyframes shimmer {
      to {
        left: 100%;
      }
    }

    .probability-markers {
      position: relative;
      height: 20px;
      margin-top: 8px;
    }

    .marker {
      position: absolute;
      font-size: 11px;
      color: var(--text-secondary);
      transform: translateX(-50%);
    }

    .marker::before {
      content: '';
      position: absolute;
      top: -24px;
      left: 50%;
      width: 2px;
      height: 8px;
      background: var(--border-color);
    }

    .probability-insights {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-top: 16px;
    }

    .insight-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px;
      background: var(--bg-tertiary);
      border-radius: 8px;
    }

    .insight-icon {
      width: 24px;
      height: 24px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 14px;
      flex-shrink: 0;
    }

    .insight-icon.success {
      background: rgba(16, 185, 129, 0.1);
      color: var(--success-color);
    }

    .insight-icon.warning {
      background: rgba(245, 158, 11, 0.1);
      color: var(--warning-color);
    }

    .insight-icon.critical {
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color);
    }

    .insight-text {
      font-size: 14px;
      color: var(--text-primary);
    }

    @media (max-width: 768px) {
      .metrics-section {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AuditReadinessComponent {
  @Input() data: AuditReadinessData = {
    confidenceScore: 0,
    criticalFindings: 0,
    correctiveActionsPending: 0,
    clausesMissingEvidence: 0,
    auditPassProbability: 0
  };

  getCircleDashArray(score: number): string {
    const circumference = 2 * Math.PI * 85;
    const progress = (score / 100) * circumference;
    return `${progress} ${circumference}`;
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#10b981';
    if (score >= 60) return '#3b82f6';
    if (score >= 40) return '#f59e0b';
    return '#ef4444';
  }

  getScoreLabel(score: number): string {
    if (score >= 80) return 'Excellent';
    if (score >= 60) return 'Good';
    if (score >= 40) return 'Fair';
    return 'Needs Improvement';
  }

  getScoreLabelClass(score: number): string {
    if (score >= 80) return 'excellent';
    if (score >= 60) return 'good';
    if (score >= 40) return 'fair';
    return 'poor';
  }

  getProbabilityClass(probability: number): string {
    if (probability >= 80) return 'high';
    if (probability >= 60) return 'medium';
    return 'low';
  }
}
