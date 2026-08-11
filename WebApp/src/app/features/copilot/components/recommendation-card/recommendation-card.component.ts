import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { CopilotRecommendation } from '../../models/copilot-recommendation.model';
import { RecommendationSeverity, RecommendationCategory, parseRationale } from '../../models/copilot-recommendation.model';

/**
 * Displays a single Copilot recommendation with:
 *   - Severity badge (colour-coded)
 *   - Category chip
 *   - Title + description
 *   - Priority indicator
 *   - Deep link for one-click navigation
 *   - Expandable rationale panel (machine-readable evidence)
 *
 * No approval cards in Sprint 13B — read-only display only.
 */
@Component({
  selector: 'app-recommendation-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recommendation-card.component.html',
  styles: [
    `
      :host {
        display: block;
      }

      .recommendation-card {
        padding: 16px;
        border-radius: 12px;
        border: 1px solid var(--border-color);
        background: var(--card-bg);
        color: var(--text-primary);
        transition: border-color 0.15s, box-shadow 0.15s;
      }

      .recommendation-card:hover {
        border-color: color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
        box-shadow: var(--shadow-sm);
      }

      .rec-header {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
        margin-bottom: 10px;
      }

      .severity-badge,
      .category-chip,
      .priority-badge,
      .confidence-badge {
        font-size: 11px;
        font-weight: 600;
        padding: 4px 8px;
        border-radius: 6px;
        border: 1px solid var(--border-color);
        background: var(--color-bg-surface-alt, var(--card-bg));
        color: var(--text-secondary);
      }

      .severity-critical {
        background: color-mix(in srgb, var(--danger-color) 16%, transparent);
        color: var(--danger-color);
        border-color: color-mix(in srgb, var(--danger-color) 30%, transparent);
      }

      .severity-high {
        background: color-mix(in srgb, var(--warning-color) 18%, transparent);
        color: var(--warning-color);
        border-color: color-mix(in srgb, var(--warning-color) 30%, transparent);
      }

      .severity-medium {
        background: color-mix(in srgb, var(--warning-color) 12%, transparent);
        color: var(--text-secondary);
      }

      .severity-low {
        background: color-mix(in srgb, var(--success-color) 14%, transparent);
        color: var(--success-color);
        border-color: color-mix(in srgb, var(--success-color) 30%, transparent);
      }

      .rec-title {
        margin: 0 0 8px;
        font-size: 15px;
        font-weight: 700;
        color: var(--text-primary);
        line-height: 1.35;
      }

      .rec-description,
      .rec-outcome,
      .rec-risk {
        margin: 0 0 8px;
        font-size: 13px;
        line-height: 1.5;
        color: var(--text-secondary);
      }

      .rec-footer {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin-top: 12px;
      }

      .btn-deeplink,
      .btn-rationale-toggle {
        padding: 8px 12px;
        border-radius: 8px;
        border: 1px solid var(--border-color);
        background: transparent;
        color: var(--primary-color);
        font-size: 13px;
        font-weight: 600;
        cursor: pointer;
      }

      .btn-deeplink:hover,
      .btn-rationale-toggle:hover {
        background: var(--hover-bg);
      }

      .rec-rationale-panel {
        margin-top: 12px;
        padding: 12px;
        border-radius: 8px;
        border: 1px solid var(--border-color);
        background: var(--color-bg-surface-alt, var(--card-bg));
        font-size: 13px;
        color: var(--text-secondary);
      }

      .rationale-title {
        margin: 0 0 8px;
        font-size: 13px;
        font-weight: 700;
        color: var(--text-primary);
      }

      .rationale-metrics dl,
      .rationale-thresholds dl {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 4px 12px;
        margin: 0;
      }

      dt {
        font-weight: 600;
        color: var(--text-muted);
      }

      dd {
        margin: 0;
      }
    `,
  ],
})
export class RecommendationCardComponent {
  @Input({ required: true }) recommendation!: CopilotRecommendation;
  @Input() showRationale = false;

  @Output() navigateToDeepLink = new EventEmitter<string>();

  expanded = false;

  readonly RecommendationSeverity = RecommendationSeverity;

  get severityClass(): string {
    const s = this.recommendation.severity;
    return {
      [RecommendationSeverity.Critical]: 'severity-critical',
      [RecommendationSeverity.High]:     'severity-high',
      [RecommendationSeverity.Medium]:   'severity-medium',
      [RecommendationSeverity.Low]:      'severity-low',
    }[s] ?? 'severity-medium';
  }

  get severityIcon(): string {
    return {
      [RecommendationSeverity.Critical]: '🔴',
      [RecommendationSeverity.High]:     '🟠',
      [RecommendationSeverity.Medium]:   '🟡',
      [RecommendationSeverity.Low]:      '🟢',
    }[this.recommendation.severity] ?? '⚪';
  }

  get categoryLabel(): string {
    return this.recommendation.category;
  }

  get confidencePercent(): number {
    return Math.round(this.recommendation.confidence * 100);
  }

  get parsedRationale() {
    return parseRationale(this.recommendation);
  }

  onDeepLinkClick(): void {
    if (this.recommendation.deepLink) {
      this.navigateToDeepLink.emit(this.recommendation.deepLink);
    }
  }

  toggleExpand(): void {
    this.expanded = !this.expanded;
  }
}
