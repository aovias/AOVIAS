import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface SmartAlert {
  id: string;
  severity: 'critical' | 'warning' | 'info';
  title: string;
  message: string;
  timestamp: Date;
  actions: AlertAction[];
}

export interface AlertAction {
  label: string;
  type: 'primary' | 'secondary';
  action: string;
}

@Component({
  selector: 'app-smart-alerts',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="smart-alerts">
      <div class="alerts-header">
        <h2 class="section-title">Smart Alerts</h2>
        <span class="alert-count" *ngIf="criticalCount > 0">
          {{ criticalCount }} critical
        </span>
      </div>

      <div class="alerts-container">
        <div *ngFor="let alert of sortedAlerts"
             class="alert-card"
             [class.alert-critical]="alert.severity === 'critical'"
             [class.alert-warning]="alert.severity === 'warning'"
             [class.alert-info]="alert.severity === 'info'">
          <div class="alert-indicator"></div>

          <div class="alert-content">
            <div class="alert-header">
              <div class="alert-severity">
                <span class="severity-badge"
                      [class.badge-critical]="alert.severity === 'critical'"
                      [class.badge-warning]="alert.severity === 'warning'"
                      [class.badge-info]="alert.severity === 'info'">
                  {{ getSeverityLabel(alert.severity) }}
                </span>
                <span class="alert-time">{{ formatTime(alert.timestamp) }}</span>
              </div>
              <button class="dismiss-button" (click)="dismissAlert(alert.id)">
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                </svg>
              </button>
            </div>

            <h3 class="alert-title">{{ alert.title }}</h3>
            <p class="alert-message">{{ alert.message }}</p>

            <div class="alert-actions" *ngIf="alert.actions.length > 0">
              <button *ngFor="let action of alert.actions"
                      class="action-button"
                      [class.btn-primary]="action.type === 'primary'"
                      [class.btn-secondary]="action.type === 'secondary'"
                      (click)="handleAction(alert, action)">
                {{ action.label }}
              </button>
            </div>
          </div>
        </div>

        <div *ngIf="alerts.length === 0" class="empty-state">
          <div class="empty-icon">🎉</div>
          <h3 class="empty-title">All Clear!</h3>
          <p class="empty-text">No alerts at this time</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .smart-alerts {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 24px;
      box-shadow: var(--shadow-sm);
      height: 600px;
      display: flex;
      flex-direction: column;
    }

    .alerts-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .section-title {
      font-size: 20px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0;
    }

    .alert-count {
      background: rgba(239, 68, 68, 0.1);
      color: var(--danger-color);
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .alerts-container {
      flex: 1;
      overflow-y: auto;
      margin: 0 -8px;
      padding: 0 8px;
    }

    .alerts-container::-webkit-scrollbar {
      width: 6px;
    }

    .alerts-container::-webkit-scrollbar-track {
      background: var(--bg-tertiary);
      border-radius: 3px;
    }

    .alerts-container::-webkit-scrollbar-thumb {
      background: var(--border-color);
      border-radius: 3px;
    }

    .alerts-container::-webkit-scrollbar-thumb:hover {
      background: var(--text-secondary);
    }

    .alert-card {
      position: relative;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      margin-bottom: 16px;
      overflow: hidden;
      transition: all 0.3s ease;
    }

    .alert-card:hover {
      box-shadow: var(--shadow-md);
      transform: translateY(-2px);
    }

    .alert-indicator {
      position: absolute;
      left: 0;
      top: 0;
      bottom: 0;
      width: 4px;
    }

    .alert-critical .alert-indicator {
      background: var(--danger-color);
    }

    .alert-warning .alert-indicator {
      background: var(--warning-color);
    }

    .alert-info .alert-indicator {
      background: var(--primary-color);
    }

    .alert-content {
      padding: 20px 20px 20px 24px;
    }

    .alert-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }

    .alert-severity {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .severity-badge {
      padding: 4px 10px;
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

    .badge-warning {
      background: rgba(245, 158, 11, 0.1);
      color: var(--warning-color);
    }

    .badge-info {
      background: var(--primary-bg-light);
      color: var(--primary-color);
    }

    .alert-time {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .dismiss-button {
      width: 24px;
      height: 24px;
      border: none;
      background: transparent;
      color: var(--text-secondary);
      cursor: pointer;
      border-radius: 6px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
    }

    .dismiss-button:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }

    .dismiss-button svg {
      width: 16px;
      height: 16px;
    }

    .alert-title {
      font-size: 16px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 8px 0;
      line-height: 1.4;
    }

    .alert-message {
      font-size: 14px;
      color: var(--text-secondary);
      margin: 0 0 16px 0;
      line-height: 1.6;
    }

    .alert-actions {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    .action-button {
      padding: 8px 16px;
      border: none;
      border-radius: 8px;
      font-size: 13px;
      font-weight: 600;
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
      box-shadow: var(--shadow-md);
    }

    .btn-secondary {
      background: var(--bg-tertiary);
      color: var(--text-primary);
    }

    .btn-secondary:hover {
      background: var(--hover-bg);
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
      margin-bottom: 16px;
    }

    .empty-title {
      font-size: 18px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 8px 0;
    }

    .empty-text {
      font-size: 14px;
      color: var(--text-secondary);
      margin: 0;
    }
  `]
})
export class SmartAlertsComponent {
  @Input() alerts: SmartAlert[] = [];

  get sortedAlerts(): SmartAlert[] {
    const severityOrder = { critical: 0, warning: 1, info: 2 };
    return [...this.alerts].sort((a, b) => {
      const severityDiff = severityOrder[a.severity] - severityOrder[b.severity];
      if (severityDiff !== 0) return severityDiff;
      return b.timestamp.getTime() - a.timestamp.getTime();
    });
  }

  get criticalCount(): number {
    return this.alerts.filter(a => a.severity === 'critical').length;
  }

  getSeverityLabel(severity: string): string {
    const labels: Record<string, string> = {
      'critical': 'Critical',
      'warning': 'Warning',
      'info': 'Info'
    };
    return labels[severity] || severity;
  }

  formatTime(timestamp: Date): string {
    const now = new Date();
    const diff = now.getTime() - timestamp.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);

    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;

    return timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  dismissAlert(id: string): void {
    console.log('Dismissing alert:', id);
  }

  handleAction(alert: SmartAlert, action: AlertAction): void {
    console.log('Alert action:', action.action, alert);
  }
}
