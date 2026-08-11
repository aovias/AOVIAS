import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PredictedGapSummaryModel } from '../../models/compliance-maturity.model';
import { ComplianceClassificationBadgeComponent } from '../../../../shared/ui/compliance-classification-badge/compliance-classification-badge.component';

@Component({
  selector: 'app-predicted-opportunities',
  standalone: true,
  imports: [CommonModule, ComplianceClassificationBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section
      class="panel"
      [class.panel--locked]="summary().isLocked"
      [attr.aria-labelledby]="headingId()"
    >
      <header class="panel__header">
        <div class="panel__title-group">
          <div class="panel__eyebrow">
            <app-compliance-classification-badge classification="predictedGap" />
            @if (summary().isLocked) {
              <span class="lock-chip" aria-label="Stage locked">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
                  <rect x="3" y="11" width="18" height="11" rx="2"/>
                  <path d="M7 11V7a5 5 0 0110 0v4"/>
                </svg>
                Locked
              </span>
            }
          </div>
          <h2 [id]="headingId()" class="panel__title">{{ title() }}</h2>
          <p class="panel__subtitle">{{ subtitle() }}</p>
        </div>
        <div class="panel__kpi">
          <span class="panel__kpi-value panel__kpi-value--muted">{{ summary().predictedGapCount }}</span>
          <span class="panel__kpi-label">Predicted Gaps</span>
        </div>
      </header>

      @if (summary().isLocked) {
        <div class="unlock-banner" role="status">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <circle cx="12" cy="12" r="10"/>
            <path d="M12 16v-4m0-4h.01" stroke-linecap="round"/>
          </svg>
          <span>{{ summary().unlockCriteria }}</span>
        </div>
      }

      @if (summary().items.length > 0) {
        <div class="opportunity-grid">
          @for (item of summary().items; track item.title) {
            <article class="opportunity-card" [class.opportunity-card--locked]="summary().isLocked">
              <div class="opportunity-card__icon" aria-hidden="true">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
              <div class="opportunity-card__body">
                <h3 class="opportunity-card__title">{{ item.title }}</h3>
                <div class="opportunity-card__meta">
                  <span class="opportunity-card__chapter">{{ item.chapter }}</span>
                  @if (item.riskCategory) {
                    <span class="opportunity-card__risk">{{ item.riskCategory }}</span>
                  }
                </div>
              </div>
            </article>
          }
        </div>
      }
    </section>
  `,
  styles: [`
    .panel {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      box-shadow: var(--shadow-sm);
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .panel--locked {
      opacity: 0.88;
    }

    .panel__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;
    }

    .panel__eyebrow {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 6px;
    }

    .panel__type-badge {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      padding: 3px 10px;
      border-radius: 999px;
    }

    .panel__type-badge--predicted {
      background: color-mix(in srgb, var(--warning-color) 12%, transparent);
      color: var(--warning-color);
    }

    .lock-chip {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-size: 11px;
      font-weight: 600;
      color: var(--text-muted);
      background: var(--bg-secondary);
      padding: 2px 8px;
      border-radius: 999px;
    }

    .panel__title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .panel__subtitle {
      font-size: var(--font-size-body-sm);
      color: var(--text-secondary);
      margin: 4px 0 0;
    }

    .panel__kpi { text-align: right; }

    .panel__kpi-value {
      display: block;
      font-size: 32px;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
    }

    .panel__kpi-value--muted { color: var(--text-secondary); }

    .panel__kpi-label {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .unlock-banner {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      padding: 12px 14px;
      border-radius: 8px;
      background: color-mix(in srgb, var(--primary-color) 8%, var(--bg-secondary));
      color: var(--text-secondary);
      font-size: 13px;
      line-height: 1.45;
    }

    .unlock-banner svg {
      flex-shrink: 0;
      color: var(--primary-color);
      margin-top: 1px;
    }

    .opportunity-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
      gap: 10px;
    }

    .opportunity-card {
      display: flex;
      gap: 12px;
      padding: 12px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 10px;
      transition: box-shadow 0.2s ease;
    }

    .opportunity-card:not(.opportunity-card--locked):hover {
      box-shadow: var(--shadow-sm);
    }

    .opportunity-card--locked {
      opacity: 0.75;
    }

    .opportunity-card__icon {
      width: 32px;
      height: 32px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      background: color-mix(in srgb, var(--warning-color) 12%, transparent);
      color: var(--warning-color);
    }

    .opportunity-card__body {
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .opportunity-card__title {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
      line-height: 1.35;
    }

    .opportunity-card__meta {
      display: flex;
      gap: 6px;
      flex-wrap: wrap;
    }

    .opportunity-card__chapter {
      font-size: 11px;
      color: var(--text-secondary);
    }

    .opportunity-card__risk {
      font-size: 11px;
      color: var(--text-muted);
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 999px;
      padding: 0 8px;
    }
  `],
})
export class PredictedOpportunitiesComponent {
  title = input.required<string>();
  subtitle = input.required<string>();
  summary = input.required<PredictedGapSummaryModel>();

  headingId(): string {
    return `predicted-${this.title().replace(/\s+/g, '-').toLowerCase()}`;
  }
}
