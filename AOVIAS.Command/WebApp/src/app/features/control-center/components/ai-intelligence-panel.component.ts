import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface AIInsight {
  type: 'warning' | 'success' | 'critical' | 'info';
  message: string;
  metric?: string;
}

export interface RecommendedAction {
  title: string;
  impact: string;
  priority: 'high' | 'medium' | 'low';
}

@Component({
  selector: 'app-ai-intelligence-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="ai-intelligence-panel">
      <div class="panel-header">
        <div class="ai-icon-wrapper">
          <svg class="ai-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M12 2L2 7l10 5 10-5-10-5z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M2 17l10 5 10-5M2 12l10 5 10-5" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </div>
        <div>
          <h2 class="panel-title">AI Compliance Intelligence</h2>
          <p class="panel-subtitle">Real-time insights powered by machine learning</p>
        </div>
      </div>

      <div class="insights-grid">
        <div *ngFor="let insight of insights"
             class="insight-card"
             [class.warning]="insight.type === 'warning'"
             [class.success]="insight.type === 'success'"
             [class.critical]="insight.type === 'critical'"
             [class.info]="insight.type === 'info'">
          <div class="insight-icon">
            <span *ngIf="insight.type === 'critical'">⚠️</span>
            <span *ngIf="insight.type === 'warning'">⚡</span>
            <span *ngIf="insight.type === 'success'">✓</span>
            <span *ngIf="insight.type === 'info'">ℹ️</span>
          </div>
          <div class="insight-content">
            <p class="insight-message">{{ insight.message }}</p>
            <span *ngIf="insight.metric" class="insight-metric">{{ insight.metric }}</span>
          </div>
        </div>
      </div>

      <div class="recommendations-section">
        <h3 class="recommendations-title">Recommended Actions</h3>
        <div class="recommendations-list">
          <div *ngFor="let action of recommendedActions"
               class="recommendation-card"
               [class.priority-high]="action.priority === 'high'"
               [class.priority-medium]="action.priority === 'medium'"
               [class.priority-low]="action.priority === 'low'">
            <div class="priority-indicator"></div>
            <div class="recommendation-content">
              <h4 class="recommendation-title">{{ action.title }}</h4>
              <p class="recommendation-impact">{{ action.impact }}</p>
            </div>
            <button class="action-button">Take Action</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .ai-intelligence-panel {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 24px;
      box-shadow: var(--shadow-sm);
    }

    .panel-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 20px;
    }

    .ai-icon-wrapper {
      width: 48px;
      height: 48px;
      background: var(--primary-bg-light);
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .ai-icon {
      width: 24px;
      height: 24px;
      color: var(--primary-color);
    }

    .panel-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .panel-subtitle {
      font-size: var(--font-size-body-sm);
      margin: 4px 0 0 0;
      color: var(--text-secondary);
    }

    .insights-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 12px;
      margin-bottom: 20px;
    }

    .insight-card {
      background: var(--bg-tertiary);
      border-radius: 8px;
      padding: 14px;
      display: flex;
      gap: 10px;
      transition: all 0.2s ease;
      border: 1px solid var(--border-color);
    }

    .insight-card:hover {
      background: var(--hover-bg);
      transform: translateY(-1px);
    }

    .insight-card.critical {
      border-left: 3px solid var(--danger-color);
    }

    .insight-card.warning {
      border-left: 3px solid var(--warning-color);
    }

    .insight-card.success {
      border-left: 3px solid var(--success-color);
    }

    .insight-card.info {
      border-left: 3px solid var(--primary-color);
    }

    .insight-icon {
      font-size: 20px;
      flex-shrink: 0;
    }

    .insight-content {
      flex: 1;
    }

    .insight-message {
      font-size: 13px;
      margin: 0 0 4px 0;
      line-height: 1.4;
      color: var(--text-primary);
    }

    .insight-metric {
      font-size: 18px;
      font-weight: 700;
      display: block;
      color: var(--text-primary);
    }

    .recommendations-section {
      margin-top: 20px;
      padding-top: 20px;
      border-top: 1px solid var(--border-color);
    }

    .recommendations-title {
      font-size: 14px;
      font-weight: 600;
      margin: 0 0 12px 0;
      color: var(--text-primary);
    }

    .recommendations-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .recommendation-card {
      background: var(--bg-tertiary);
      border-radius: 8px;
      padding: 14px;
      display: flex;
      align-items: center;
      gap: 12px;
      transition: all 0.2s ease;
      border: 1px solid var(--border-color);
    }

    .recommendation-card:hover {
      background: var(--hover-bg);
      transform: translateX(4px);
    }

    .priority-indicator {
      width: 3px;
      height: 36px;
      border-radius: 2px;
      flex-shrink: 0;
    }

    .priority-high .priority-indicator {
      background: var(--danger-color);
    }

    .priority-medium .priority-indicator {
      background: var(--warning-color);
    }

    .priority-low .priority-indicator {
      background: var(--success-color);
    }

    .recommendation-content {
      flex: 1;
    }

    .recommendation-title {
      font-size: 13px;
      font-weight: 600;
      margin: 0 0 4px 0;
      color: var(--text-primary);
    }

    .recommendation-impact {
      font-size: 12px;
      margin: 0;
      color: var(--text-secondary);
    }

    .action-button {
      background: var(--primary-color);
      color: white;
      border: none;
      padding: 7px 16px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      white-space: nowrap;
    }

    .action-button:hover {
      background: var(--primary-hover);
      transform: translateY(-1px);
    }
  `]
})
export class AIIntelligencePanelComponent {
  @Input() insights: AIInsight[] = [];
  @Input() recommendedActions: RecommendedAction[] = [];
}
