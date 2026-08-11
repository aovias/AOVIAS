import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ComplianceMaturityDashboardModel, MaturityStage } from '../../models/compliance-maturity.model';

@Component({
  selector: 'app-cmi-header',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="page-header">
      <div class="page-header__content">
        <div class="page-header__meta">
          <span class="org-badge">{{ dashboard().organizationName }}</span>
          <span class="framework-badge">{{ dashboard().frameworkName }}</span>
        </div>
        <h1 class="page-title">Accreditation Governance Workspace</h1>
        <p class="page-description">
          Understand stage blockers, unlock progress, accreditation forecast, and predicted future gaps.
          Execution work is performed in the Journey workspace.
        </p>
        @if (dashboard().assessmentDate) {
          <p class="page-meta">
            Last assessment: {{ dashboard().assessmentDate | date: 'dd MMM yyyy' }}
          </p>
        }
      </div>
      <div class="page-header__stage">
        <span class="stage-label">Current Stage</span>
        <span class="stage-value" [attr.data-stage]="stageClass()">
          {{ stageLabel() }}
        </span>
      </div>
    </header>
  `,
  styles: [`
    .page-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 24px;
      flex-wrap: wrap;
    }

    .page-header__meta {
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
      margin-bottom: 8px;
    }

    .org-badge {
      font-size: 12px;
      font-weight: 600;
      color: var(--text-secondary);
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 999px;
      padding: 3px 12px;
    }

    .framework-badge {
      font-size: 12px;
      font-weight: 600;
      color: var(--primary-color);
      background: var(--primary-bg-light);
      border-radius: 999px;
      padding: 3px 12px;
    }

    .page-title {
      font-size: var(--font-size-page-title);
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 8px;
      letter-spacing: -0.02em;
    }

    .page-description {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 0;
      max-width: 640px;
      line-height: 1.5;
    }

    .page-meta {
      font-size: 13px;
      color: var(--text-muted);
      margin: 8px 0 0;
    }

    .page-header__stage {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 4px;
      padding: 16px 20px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      box-shadow: var(--shadow-sm);
      min-width: 160px;
    }

    .stage-label {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--text-muted);
    }

    .stage-value {
      font-size: 20px;
      font-weight: 700;
      color: var(--primary-color);
    }

    .stage-value[data-stage='commitment'] { color: var(--warning-color); }
    .stage-value[data-stage='achievement'] { color: var(--success-color); }
    .stage-value[data-stage='excellence'] { color: var(--primary-color); }

    @media (max-width: 768px) {
      .page-title { font-size: var(--font-size-page-title-compact); }
      .page-header__stage { align-items: flex-start; width: 100%; }
    }
  `],
})
export class CmiHeaderComponent {
  dashboard = input.required<ComplianceMaturityDashboardModel>();

  stageClass(): string {
    return this.dashboard().currentStage;
  }

  stageLabel(): string {
    const stage: MaturityStage = this.dashboard().currentStage;
    return `${stage.charAt(0).toUpperCase()}${stage.slice(1)}`;
  }
}
