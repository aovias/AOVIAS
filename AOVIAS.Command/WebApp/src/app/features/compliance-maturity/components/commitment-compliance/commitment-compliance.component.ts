import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PredictedGapSummaryModel } from '../../models/compliance-maturity.model';

@Component({
  selector: 'app-commitment-compliance',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="section-card" [class.section-card--locked]="summary().isLocked">
      <div class="section-card__header">
        <div class="section-card__title-group">
          <div class="section-card__title-row">
            <h2 class="section-card__title">Commitment Compliance</h2>
            @if (summary().isLocked) {
              <span class="lock-badge">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                  <path d="M7 11V7a5 5 0 0110 0v4"/>
                </svg>
                Locked
              </span>
            } @else {
              <span class="predicted-badge">Predicted Based On Current Maturity</span>
            }
          </div>
          <span class="section-card__subtitle">Next maturity level requirements</span>
        </div>
        <div class="section-card__score-badge">
          <span class="section-card__score-value section-card__score-value--muted">{{ summary().predictedGapCount }}</span>
          <span class="section-card__score-label">Predicted Gaps</span>
        </div>
      </div>

      @if (summary().isLocked) {
        <div class="unlock-message">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <circle cx="12" cy="12" r="10"/>
            <path d="M12 8v4m0 4h.01" stroke-linecap="round"/>
          </svg>
          <span>{{ summary().unlockCriteria }}</span>
        </div>
      }

      @if (summary().items.length > 0) {
        <div class="predicted-list">
          @for (item of summary().items; track item.title) {
            <div class="predicted-item">
              <div class="predicted-item__icon">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                  <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
              <div class="predicted-item__content">
                <span class="predicted-item__title">{{ item.title }}</span>
                <div class="predicted-item__meta">
                  <span class="predicted-item__chapter">{{ item.chapter }}</span>
                  @if (item.riskCategory) {
                    <span class="predicted-item__risk">{{ item.riskCategory }}</span>
                  }
                </div>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .section-card {
      background: var(--bg-primary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-lg, 12px);
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .section-card--locked {
      opacity: 0.72;
    }

    .section-card__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .section-card__title-group { display: flex; flex-direction: column; gap: 0.375rem; }

    .section-card__title-row {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .section-card__title {
      font-size: 1.125rem;
      font-weight: 700;
      color: var(--color-text-primary);
      margin: 0;
    }

    .section-card__subtitle {
      font-size: var(--font-size-body-sm);
      color: var(--color-text-secondary);
    }

    .lock-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      background: var(--bg-secondary);
      color: var(--color-text-tertiary);
      border-radius: 999px;
      padding: 0.1875rem 0.625rem;
      font-size: 0.75rem;
      font-weight: 600;
    }

    .predicted-badge {
      background: var(--color-warning-subtle, #fffbeb);
      color: var(--color-warning, #d97706);
      border-radius: 999px;
      padding: 0.1875rem 0.625rem;
      font-size: 0.6875rem;
      font-weight: 600;
      letter-spacing: 0.02em;
    }

    .section-card__score-badge {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
    }

    .section-card__score-value {
      font-size: 2rem;
      font-weight: 700;
      color: var(--color-text-primary);
      line-height: 1;
    }

    .section-card__score-value--muted { color: var(--color-text-secondary); }

    .section-card__score-label {
      font-size: 0.75rem;
      color: var(--color-text-secondary);
    }

    .unlock-message {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      background: var(--color-info-subtle, var(--color-primary-50));
      color: var(--color-info, var(--color-primary));
      border-radius: var(--radius-md, 8px);
      padding: 0.75rem 1rem;
      font-size: 0.875rem;
    }

    .predicted-list {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .predicted-item {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 0.75rem;
      background: var(--bg-secondary);
      border-radius: var(--radius-md, 8px);
      border: 1px solid var(--border-subtle);
    }

    .predicted-item__icon {
      width: 28px;
      height: 28px;
      border-radius: 50%;
      background: var(--color-warning-subtle, #fffbeb);
      color: var(--color-warning, #d97706);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .predicted-item__content {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .predicted-item__title {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary);
    }

    .predicted-item__meta {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .predicted-item__chapter {
      font-size: 0.75rem;
      color: var(--color-text-secondary);
    }

    .predicted-item__risk {
      font-size: 0.75rem;
      color: var(--color-text-tertiary);
      background: var(--bg-primary);
      border: 1px solid var(--border-subtle);
      border-radius: 999px;
      padding: 0 0.375rem;
    }
  `],
})
export class CommitmentComplianceComponent {
  summary = input.required<PredictedGapSummaryModel>();
}
