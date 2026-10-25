import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AIIntelligencePanelComponent } from '../components/ai-intelligence-panel.component';
import { ExecutiveKPIBarComponent } from '../components/executive-kpi-bar.component';
import { RiskCommandCenterComponent } from '../components/risk-command-center.component';
import { ActionCenterComponent } from '../components/action-center.component';
import { ComplianceAnalyticsComponent } from '../components/compliance-analytics.component';
import { AuditReadinessComponent } from '../components/audit-readiness.component';
import { ActivityStreamComponent } from '../components/activity-stream.component';
import { SmartAlertsComponent } from '../components/smart-alerts.component';
import { AICopilotComponent } from '../components/ai-copilot.component';
import { ControlCenterFacade } from '../facades/control-center.facade';
import { PerformanceTimelineService } from '../../../core/performance/performance-timeline.service';

@Component({
  selector: 'app-compliance-command-center',
  standalone: true,
  imports: [
    CommonModule,
    AIIntelligencePanelComponent,
    ExecutiveKPIBarComponent,
    RiskCommandCenterComponent,
    ActionCenterComponent,
    ComplianceAnalyticsComponent,
    AuditReadinessComponent,
    ActivityStreamComponent,
    SmartAlertsComponent,
    AICopilotComponent
  ],
  template: `
    <div class="command-center">
      <div class="page-header">
        <div class="header-content">
          <h1 class="page-title">Compliance Command Center</h1>
          <p class="page-subtitle">AI-powered real-time compliance operations and risk intelligence</p>
        </div>
        <div class="header-actions">
          <div class="status-indicator" [class.offline]="!facade.isLive()">
            <span class="status-dot"></span>
            <span class="status-text">{{ facade.isLive() ? 'Live' : 'Offline' }}</span>
            @if (facade.lastUpdated(); as updated) {
              <span class="status-time">· {{ updated | date: 'HH:mm:ss' }}</span>
            }
          </div>
          <button class="refresh-button" (click)="refreshData()" [disabled]="facade.isRefreshing()">
            <svg viewBox="0 0 20 20" fill="currentColor" [class.spinning]="facade.isRefreshing()">
              <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
            </svg>
            {{ facade.isRefreshing() ? 'Refreshing…' : 'Refresh' }}
          </button>
        </div>
      </div>

      @if (facade.isLoading()) {
        <div class="state-panel">
          <span class="spinner"></span>
          <span>Loading compliance command center…</span>
        </div>
      } @else if (facade.error()) {
        <div class="state-panel error">
          <span>{{ facade.error() }}</span>
          <button class="retry-button" (click)="refreshData()">Retry</button>
        </div>
      } @else if (facade.isEmpty()) {
        <div class="state-panel">
          <span>No compliance data is available for the current scope yet.</span>
        </div>
      }

      <div class="dashboard-layout" [class.is-refreshing]="facade.isRefreshing()" [hidden]="facade.isLoading() || !!facade.error()">
        <section class="ai-intelligence-section">
          <app-ai-intelligence-panel
            [insights]="facade.aiInsights()"
            [recommendedActions]="facade.recommendedActions()">
          </app-ai-intelligence-panel>
        </section>

        <section class="kpi-section">
          <app-executive-kpi-bar [kpis]="facade.kpiCards()"></app-executive-kpi-bar>
        </section>

        <section class="risk-section">
          <app-risk-command-center
            [highRiskChapters]="facade.highRiskChapters()"
            [departmentRisks]="facade.departmentRisks()">
          </app-risk-command-center>
        </section>

        <section class="action-section">
          <app-action-center [blockingTasks]="facade.blockingTasks()"></app-action-center>
        </section>

        <section class="analytics-section">
          <app-compliance-analytics
            [trendData]="facade.trendData()"
            [chapterData]="facade.chapterData()"
            [evidenceData]="facade.evidenceData()">
          </app-compliance-analytics>
        </section>

        <section class="readiness-section">
          <app-audit-readiness [data]="facade.auditReadinessData()"></app-audit-readiness>
        </section>

        <div class="bottom-grid">
          <section class="activity-section">
            <app-activity-stream [activities]="facade.activities()"></app-activity-stream>
          </section>

          <section class="alerts-section">
            <app-smart-alerts [alerts]="facade.smartAlerts()"></app-smart-alerts>
          </section>
        </div>
      </div>

      <app-ai-copilot></app-ai-copilot>
    </div>
  `,
  styles: [`
    .command-center {
      min-height: 100vh;
      background: var(--bg-primary);
      padding: 24px;
      animation: fadeIn 0.5s ease;
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }

    .header-content {
      flex: 1;
    }

    .page-title {
      font-size: var(--font-size-page-title);
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 6px 0;
    }

    .page-subtitle {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 0;
    }

    .header-actions {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .status-indicator {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 8px 14px;
      background: var(--card-bg);
      border-radius: 6px;
      border: 1px solid var(--border-color);
      font-size: 13px;
      color: var(--text-secondary);
    }

    .status-dot {
      width: 6px;
      height: 6px;
      background: var(--success-color);
      border-radius: 50%;
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.5;
      }
    }

    .status-text {
      font-weight: 600;
    }

    .refresh-button {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 18px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .refresh-button:hover {
      background: var(--primary-hover);
      transform: translateY(-1px);
    }

    .refresh-button svg {
      width: 16px;
      height: 16px;
    }

    .refresh-button:disabled {
      opacity: 0.7;
      cursor: default;
    }

    .refresh-button svg.spinning {
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .status-indicator.offline .status-dot {
      background: var(--text-secondary);
      animation: none;
    }

    .status-time {
      font-variant-numeric: tabular-nums;
      opacity: 0.75;
    }

    .state-panel {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px 18px;
      margin-bottom: 16px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      font-size: 14px;
      color: var(--text-secondary);
    }

    .state-panel.error {
      border-color: var(--danger-color, #ef4444);
      color: var(--danger-color, #ef4444);
      justify-content: space-between;
    }

    .retry-button {
      padding: 6px 14px;
      background: var(--primary-color);
      color: #fff;
      border: none;
      border-radius: 6px;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
    }

    .spinner {
      width: 16px;
      height: 16px;
      border: 2px solid var(--border-color);
      border-top-color: var(--primary-color);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    .dashboard-layout.is-refreshing {
      opacity: 0.85;
      transition: opacity 0.2s ease;
    }

    .dashboard-layout {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .ai-intelligence-section,
    .kpi-section,
    .risk-section,
    .action-section,
    .analytics-section,
    .readiness-section {
      animation: slideUp 0.6s ease;
    }

    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(30px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .ai-intelligence-section {
      animation-delay: 0.1s;
    }

    .kpi-section {
      animation-delay: 0.2s;
    }

    .risk-section {
      animation-delay: 0.3s;
    }

    .action-section {
      animation-delay: 0.4s;
    }

    .analytics-section {
      animation-delay: 0.5s;
    }

    .readiness-section {
      animation-delay: 0.6s;
    }

    .bottom-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 24px;
      animation: slideUp 0.6s ease 0.7s both;
    }

    @media (max-width: 1400px) {
      .bottom-grid {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 768px) {
      .command-center {
        padding: 16px;
      }

      .page-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 16px;
      }

      .page-title {
        font-size: var(--font-size-page-title-compact);
      }

      .page-subtitle {
        font-size: var(--font-size-subtitle);
      }

      .header-actions {
        width: 100%;
        justify-content: space-between;
      }
    }
  `]
})
export class ComplianceCommandCenterComponent implements OnInit, OnDestroy {
  readonly facade = inject(ControlCenterFacade);
  private readonly router = inject(Router);
  private readonly perf = inject(PerformanceTimelineService);

  ngOnInit(): void {
    this.perf.mark('control_center_route_activate');
    this.facade.load();
    this.facade.startAutoRefresh(30_000);
  }

  ngOnDestroy(): void {
    this.facade.stopAutoRefresh();
  }

  refreshData(): void {
    this.facade.refresh();
  }
}
