import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ComplianceMaturityDashboardModel } from '../../models/compliance-maturity.model';

interface KpiCard {
  title: string;
  value: string | number;
  label: string;
  icon: string;
  color: 'primary' | 'success' | 'warning' | 'danger';
  actionLink?: string[];
  actionQueryParams?: Record<string, string>;
  actionLabel?: string;
}

@Component({
  selector: 'app-cm-executive-kpi',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="kpi-grid" role="region" aria-label="Compliance maturity key indicators">
      @for (card of cards(); track card.title) {
        <article class="kpi-card" [attr.data-color]="card.color">
          <div class="kpi-card__icon" [attr.data-color]="card.color">
            <span [innerHTML]="card.icon" aria-hidden="true"></span>
          </div>
          <div class="kpi-card__content">
            <h3 class="kpi-card__title">{{ card.title }}</h3>
            <p class="kpi-card__value" [attr.data-color]="card.color">{{ card.value }}</p>
            <p class="kpi-card__label">{{ card.label }}</p>
            @if (card.actionLink) {
              <a
                class="kpi-card__action"
                [routerLink]="card.actionLink"
                [queryParams]="card.actionQueryParams"
                aria-label="{{ card.actionLabel }}"
              >{{ card.actionLabel }} →</a>
            }
          </div>
        </article>
      }
    </div>
  `,
  styles: [`
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: var(--cm-gap, 16px);
    }

    .kpi-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 16px;
      display: flex;
      gap: 14px;
      align-items: flex-start;
      box-shadow: var(--shadow-sm);
      position: relative;
      overflow: hidden;
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }

    .kpi-card::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 3px;
      background: transparent;
    }

    .kpi-card[data-color='primary']::before { background: var(--primary-color); }
    .kpi-card[data-color='success']::before { background: var(--success-color); }
    .kpi-card[data-color='warning']::before { background: var(--warning-color); }
    .kpi-card[data-color='danger']::before { background: var(--danger-color); }

    .kpi-card:first-child {
      box-shadow: var(--shadow-md);
    }

    .kpi-card:first-child::before {
      height: 4px;
    }

    .kpi-card:hover {
      transform: translateY(-2px);
      box-shadow: var(--shadow-md);
    }

    .kpi-card__icon {
      width: 44px;
      height: 44px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .kpi-card__icon[data-color='primary'] {
      background: var(--primary-bg-light);
      color: var(--primary-color);
    }

    .kpi-card__icon[data-color='success'] {
      background: color-mix(in srgb, var(--success-color) 12%, transparent);
      color: var(--success-color);
    }

    .kpi-card__icon[data-color='warning'] {
      background: color-mix(in srgb, var(--warning-color) 12%, transparent);
      color: var(--warning-color);
    }

    .kpi-card__icon[data-color='danger'] {
      background: color-mix(in srgb, var(--danger-color) 12%, transparent);
      color: var(--danger-color);
    }

    .kpi-card__content {
      min-width: 0;
      flex: 1;
    }

    .kpi-card__title {
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--text-secondary);
      margin: 0 0 4px;
    }

    .kpi-card__value {
      font-size: 30px;
      font-weight: 700;
      line-height: 1.1;
      color: var(--text-primary);
      margin: 0;
    }

    .kpi-card:first-child .kpi-card__value {
      font-size: 34px;
    }

    .kpi-card__value[data-color='success'] { color: var(--success-color); }
    .kpi-card__value[data-color='warning'] { color: var(--warning-color); }
    .kpi-card__value[data-color='danger'] { color: var(--danger-color); }

    .kpi-card__label {
      font-size: 13px;
      color: var(--text-muted);
      margin: 4px 0 0;
    }

    .kpi-card__action {
      display: inline-block;
      margin-top: 8px;
      font-size: 11px;
      font-weight: 600;
      text-decoration: none;
      color: var(--primary-color);
      letter-spacing: 0.02em;
      transition: opacity 0.15s;

      &:hover { opacity: 0.75; text-decoration: underline; }
    }

    @media (max-width: 1280px) {
      .kpi-grid { grid-template-columns: repeat(2, 1fr); }
    }

    @media (max-width: 640px) {
      .kpi-grid { grid-template-columns: 1fr; }
    }
  `],
})
export class CmExecutiveKpiComponent {
  dashboard = input.required<ComplianceMaturityDashboardModel>();

  cards = computed<KpiCard[]>(() => {
    const d = this.dashboard();
    const cmi = d.complianceMaturityIndex;
    const core = d.coreSummary;
    const cmiColor: KpiCard['color'] =
      cmi.value >= 80 ? 'success' : cmi.value >= 50 ? 'warning' : 'primary';
    const coreColor: KpiCard['color'] =
      core.readinessScore >= 80 ? 'success' : core.readinessScore >= 50 ? 'warning' : 'danger';

    return [
      {
        title: 'Compliance Maturity Index',
        value: `${Math.round(cmi.value)}`,
        label: 'Weighted composite across all stages',
        icon: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: cmiColor,
      },
      {
        title: 'Core Readiness',
        value: `${Math.round(core.readinessScore)}%`,
        label: 'Actual assessment readiness score',
        icon: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M22 4L12 14.01l-3-3" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: coreColor,
        actionLink: ['/app/journey/execution'],
        actionQueryParams: { stage: 'core' },
        actionLabel: 'View Core Deficiencies',
      },
      {
        title: 'Open Deficiencies',
        value: core.openGapCount,
        label: 'Compliance deficiencies requiring remediation',
        icon: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M12 8v4m0 4h.01" stroke-width="2" stroke-linecap="round"/></svg>',
        color: core.openGapCount > 0 ? 'warning' : 'success',
        actionLink: ['/app/journey/execution'],
        actionQueryParams: { stage: 'core' },
        actionLabel: 'Remediate Deficiencies',
      },
      {
        title: 'Critical Deficiencies',
        value: core.criticalCount,
        label: 'Highest severity compliance blockers',
        icon: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M12 9v4m0 4h.01" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: core.criticalCount > 0 ? 'danger' : 'success',
        actionLink: ['/app/journey/execution'],
        actionQueryParams: { stage: 'core' },
        actionLabel: 'Fix Critical Issues',
      },
      ...(d.auditOriginDeficiencyCount && d.auditOriginDeficiencyCount > 0
        ? [
            {
              title: 'Audit-Origin Deficiencies',
              value: d.auditOriginDeficiencyCount,
              label: 'Open deficiencies promoted from audit findings',
              icon: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/><path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" stroke-width="2" stroke-linecap="round"/></svg>',
              color: 'warning' as const,
              actionLink: ['/app/audit/capa'],
              actionLabel: 'View Deficiencies',
            },
          ]
        : []),
      ...(d.capaGovernance
        ? [
            {
              title: 'CAPA Effectiveness',
              value: `${Math.round(d.capaGovernance.capaEffectivenessRate)}%`,
              label: 'Closed CAPAs judged effective',
              icon: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 12l2 2 4-4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><circle cx="12" cy="12" r="10" stroke-width="2"/></svg>',
              color: (d.capaGovernance.capaEffectivenessRate >= 80 ? 'success' : 'warning') as KpiCard['color'],
              actionLink: ['/app/audit/capa'],
              actionLabel: 'View CAPAs',
            },
            {
              title: 'Recurring Issues',
              value: d.capaGovernance.recurringIssues,
              label: 'Deficiencies flagged as recurring',
              icon: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
              color: (d.capaGovernance.recurringIssues > 0 ? 'danger' : 'success') as KpiCard['color'],
              actionLink: ['/app/audit/capa'],
              actionLabel: 'Review Recurrence',
            },
            {
              title: 'Pending CAPA Reviews',
              value: d.capaGovernance.pendingReviews,
              label: 'Effectiveness reviews awaiting completion',
              icon: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="4" width="18" height="18" rx="2" stroke-width="2"/><path d="M16 2v4M8 2v4M3 10h18" stroke-width="2" stroke-linecap="round"/></svg>',
              color: (d.capaGovernance.pendingReviews > 0 ? 'warning' : 'success') as KpiCard['color'],
              actionLink: ['/app/audit/capa'],
              actionLabel: 'Schedule Reviews',
            },
          ]
        : []),
    ];
  });
}
