import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  ComplianceMaturityDashboardModel,
  MaturityStage,
} from '../../models/compliance-maturity.model';

interface GovernanceNavLink {
  label: string;
  description: string;
  route: string | string[];
  queryParams?: Record<string, string>;
}

@Component({
  selector: 'app-journey-actions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="journey-actions-card" aria-labelledby="journey-actions-heading">
      <h2 id="journey-actions-heading" class="journey-actions-card__title">Governance Navigation</h2>
      <p class="journey-actions-card__subtitle">Open execution modules — remediation happens outside this dashboard</p>
      <div class="journey-actions-card__list">
        @for (link of navLinks(); track link.label) {
          <a
            class="journey-cta journey-cta--secondary"
            [routerLink]="link.route"
            [queryParams]="link.queryParams ?? null"
          >
            <span class="journey-cta__label">{{ link.label }}</span>
            <span class="journey-cta__desc">{{ link.description }}</span>
          </a>
        }
      </div>
    </section>
  `,
  styles: [`
    .journey-actions-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
    }

    .journey-actions-card__title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 4px;
    }

    .journey-actions-card__subtitle {
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
      margin: 0 0 12px;
    }

    .journey-actions-card__list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .journey-cta {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 2px;
      text-align: left;
      width: 100%;
      padding: 10px 12px;
      border-radius: 8px;
      border: 1px solid var(--border-color);
      text-decoration: none;
      transition: background 0.15s ease, border-color 0.15s ease;
    }

    .journey-cta--secondary {
      background: var(--bg-secondary);
      color: var(--text-primary);
    }

    .journey-cta--secondary:hover {
      background: var(--hover-bg);
    }

    .journey-cta__label {
      font-size: 13px;
      font-weight: 600;
    }

    .journey-cta__desc {
      font-size: 11px;
      opacity: 0.85;
      line-height: 1.35;
      color: var(--text-secondary);
    }
  `],
})
export class JourneyActionsComponent {
  dashboard = input.required<ComplianceMaturityDashboardModel>();

  navLinks = computed((): GovernanceNavLink[] => {
    const stage: MaturityStage = this.dashboard().currentStage;
    return [
      {
        label: 'Open Journey',
        description: 'Compliance execution workspace for objective element work.',
        route: '/app/journey/execution',
        queryParams: { stage },
      },
      {
        label: 'Open Tasks',
        description: 'View and manage remediation tasks.',
        route: '/app/tasks',
      },
      {
        label: 'Open Evidence',
        description: 'Evidence register and verification pipeline.',
        route: '/app/evidence',
      },
      {
        label: 'Open Issues & Risks',
        description: 'Compliance deficiency command center.',
        route: '/app/audit/capa',
      },
    ];
  });
}
