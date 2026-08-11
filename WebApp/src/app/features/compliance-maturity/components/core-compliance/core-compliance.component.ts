import { Component, ChangeDetectionStrategy, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GapSummaryModel, GapItemModel, GapItemStatus } from '../../models/compliance-maturity.model';
import { ComplianceClassificationBadgeComponent } from '../../../../shared/ui/compliance-classification-badge/compliance-classification-badge.component';

@Component({
  selector: 'app-core-compliance',
  standalone: true,
  imports: [CommonModule, RouterLink, ComplianceClassificationBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel" aria-labelledby="actual-gaps-heading">
      <header class="panel__header">
        <div class="panel__title-group">
          <div class="panel__eyebrow">
            <app-compliance-classification-badge classification="complianceDeficiency" />
          </div>
          <h2 id="actual-gaps-heading" class="panel__title">Compliance Deficiencies</h2>
          <p class="panel__subtitle">Governance view of open deficiencies — remediate in Journey or linked modules</p>
        </div>
        <div class="panel__kpi">
          <span class="panel__kpi-value">{{ summary().readinessScore | number: '1.0-0' }}%</span>
          <span class="panel__kpi-label">Core Readiness</span>
        </div>
      </header>

      <div class="panel__stats" role="list" aria-label="Compliance deficiency severity breakdown">
        <div class="stat-pill" role="listitem">
          <span class="stat-pill__value">{{ summary().openGapCount }}</span>
          <span class="stat-pill__label">Open</span>
        </div>
        <div class="stat-pill stat-pill--critical" role="listitem">
          <span class="stat-pill__value">{{ summary().criticalCount }}</span>
          <span class="stat-pill__label">Critical</span>
        </div>
        <div class="stat-pill stat-pill--major" role="listitem">
          <span class="stat-pill__value">{{ summary().majorCount }}</span>
          <span class="stat-pill__label">Major</span>
        </div>
        <div class="stat-pill stat-pill--minor" role="listitem">
          <span class="stat-pill__value">{{ summary().minorCount }}</span>
          <span class="stat-pill__label">Minor</span>
        </div>
      </div>

      @if (summary().items.length === 0) {
        <div class="panel__empty" role="status">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true">
            <path d="M9 11l3 3L22 4" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <p>No open compliance deficiencies for this stage.</p>
        </div>
      } @else {
        <div class="gap-grid">
          @for (gap of visibleGaps(); track gap.id) {
            <article class="gap-card" [attr.data-severity]="gap.severity">
              <div class="gap-card__top">
                <span class="severity-badge" [attr.data-severity]="gap.severity">
                  {{ gap.severity | titlecase }}
                </span>
                <span class="status-badge" [attr.data-status]="gapStatusClass(gap.status)">
                  {{ gapStatusLabel(gap.status) }}
                </span>
              </div>
              <h3 class="gap-card__title">{{ gap.title }}</h3>
              @if (gap.standard) {
                <p class="gap-card__standard">{{ gap.standard }}</p>
              }
              <div class="gap-card__meta">
                <span class="gap-card__chapter">{{ gap.chapter }}</span>
              </div>
              <div class="gap-card__actions">
                <a class="gap-action gap-action--link" routerLink="/app/journey/execution" [queryParams]="{ stage: 'core' }">Open Journey</a>
                <a class="gap-action gap-action--link" routerLink="/app/tasks">Open Tasks</a>
                <a class="gap-action gap-action--link" routerLink="/app/evidence">Open Evidence</a>
                <a class="gap-action gap-action--link" routerLink="/app/audit/capa">Issues &amp; Risks</a>
              </div>
            </article>
          }
        </div>

        @if (summary().items.length > visibleCount()) {
          <button type="button" class="show-more" (click)="showMore()">
            Show {{ summary().items.length - visibleCount() }} more gaps
          </button>
        }
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

    .panel__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;
    }

    .panel__eyebrow { margin-bottom: 6px; }

    .panel__type-badge {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      padding: 3px 10px;
      border-radius: 999px;
    }

    .panel__type-badge--actual {
      background: color-mix(in srgb, var(--danger-color) 12%, transparent);
      color: var(--danger-color);
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

    .panel__kpi {
      text-align: right;
    }

    .panel__kpi-value {
      display: block;
      font-size: 32px;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
    }

    .panel__kpi-label {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .panel__stats {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
    }

    .stat-pill {
      display: flex;
      flex-direction: column;
      align-items: center;
      min-width: 72px;
      padding: 8px 14px;
      border-radius: 8px;
      background: var(--bg-secondary);
    }

    .stat-pill--critical {
      background: color-mix(in srgb, var(--danger-color) 10%, var(--bg-secondary));
    }

    .stat-pill--major {
      background: color-mix(in srgb, var(--warning-color) 10%, var(--bg-secondary));
    }

    .stat-pill--minor {
      background: color-mix(in srgb, var(--primary-color) 10%, var(--bg-secondary));
    }

    .stat-pill__value {
      font-size: 20px;
      font-weight: 700;
      color: var(--text-primary);
    }

    .stat-pill--critical .stat-pill__value { color: var(--danger-color); }
    .stat-pill--major .stat-pill__value { color: var(--warning-color); }

    .stat-pill__label {
      font-size: 10px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--text-muted);
    }

    .panel__empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
      padding: 32px;
      color: var(--text-secondary);
      text-align: center;
    }

    .panel__empty svg { opacity: 0.35; }
    .panel__empty p { margin: 0; font-size: 14px; }

    .gap-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 12px;
    }

    .gap-card {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 10px;
      padding: 14px;
      display: flex;
      flex-direction: column;
      gap: 8px;
      transition: box-shadow 0.2s ease, border-color 0.2s ease;
    }

    .gap-card:hover {
      box-shadow: var(--shadow-sm);
      border-color: color-mix(in srgb, var(--border-color) 60%, var(--primary-color));
    }

    .gap-card[data-severity='critical'] {
      border-left: 3px solid var(--danger-color);
    }

    .gap-card[data-severity='major'] {
      border-left: 3px solid var(--warning-color);
    }

    .gap-card[data-severity='minor'] {
      border-left: 3px solid var(--primary-color);
    }

    .gap-card__top {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 8px;
    }

    .severity-badge {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      padding: 2px 8px;
      border-radius: 999px;
    }

    .severity-badge[data-severity='critical'] {
      background: color-mix(in srgb, var(--danger-color) 12%, transparent);
      color: var(--danger-color);
    }

    .severity-badge[data-severity='major'] {
      background: color-mix(in srgb, var(--warning-color) 12%, transparent);
      color: var(--warning-color);
    }

    .severity-badge[data-severity='minor'] {
      background: color-mix(in srgb, var(--primary-color) 12%, transparent);
      color: var(--primary-color);
    }

    .status-badge {
      font-size: 10px;
      font-weight: 600;
      padding: 2px 8px;
      border-radius: 999px;
    }

    .status-badge[data-status='open'] {
      background: color-mix(in srgb, var(--danger-color) 10%, transparent);
      color: var(--danger-color);
    }

    .status-badge[data-status='in-remediation'] {
      background: color-mix(in srgb, var(--warning-color) 10%, transparent);
      color: var(--warning-color);
    }

    .status-badge[data-status='resolved'] {
      background: color-mix(in srgb, var(--success-color) 10%, transparent);
      color: var(--success-color);
    }

    .gap-card__title {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
      line-height: 1.35;
    }

    .gap-card__standard {
      font-size: 12px;
      color: var(--text-muted);
      margin: 0;
    }

    .gap-card__meta {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    .gap-card__chapter {
      font-size: 12px;
      color: var(--text-secondary);
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 999px;
      padding: 2px 10px;
    }

    .gap-card__actions {
      display: flex;
      gap: 6px;
      flex-wrap: wrap;
      margin-top: 4px;
      padding-top: 10px;
      border-top: 1px solid var(--border-color);
    }

    .gap-action {
      font-size: 12px;
      font-weight: 500;
      color: var(--primary-color);
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      padding: 4px 10px;
      text-decoration: none;
    }

    .gap-action--link:hover {
      background: var(--hover-bg);
      text-decoration: underline;
    }

    .show-more {
      align-self: center;
      font-size: 14px;
      font-weight: 500;
      color: var(--primary-color);
      background: transparent;
      border: none;
      cursor: pointer;
      padding: 8px 16px;
      border-radius: 8px;
      transition: background 0.15s;
    }

    .show-more:hover {
      background: var(--hover-bg);
    }
  `],
})
export class CoreComplianceComponent {
  summary = input.required<GapSummaryModel>();

  private readonly PAGE_SIZE = 6;
  private _visibleCount = signal(this.PAGE_SIZE);

  visibleCount() {
    return this._visibleCount();
  }

  visibleGaps(): GapItemModel[] {
    return this.summary().items.slice(0, this._visibleCount());
  }

  showMore(): void {
    this._visibleCount.update((c) => c + this.PAGE_SIZE);
  }

  gapStatusClass(status: GapItemStatus): string {
    switch (status) {
      case 'open': return 'open';
      case 'inRemediation': return 'in-remediation';
      case 'resolved': return 'resolved';
      default: return 'open';
    }
  }

  gapStatusLabel(status: GapItemStatus): string {
    switch (status) {
      case 'open': return 'Open';
      case 'inRemediation': return 'In Remediation';
      case 'resolved': return 'Resolved';
      default: return status;
    }
  }
}
