import {
  Component, Input, Output, EventEmitter,
  ChangeDetectionStrategy, inject, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { RecommendationCardComponent } from '../recommendation-card/recommendation-card.component';
import { CopilotStore } from '../../state/copilot.store';
import type { CopilotRecommendation } from '../../models/copilot-recommendation.model';
import { RecommendationSeverity } from '../../models/copilot-recommendation.model';

export type RecommendationListView = 'top' | 'persona' | 'all' | 'urgent';

/**
 * Displays a list of Copilot recommendations with filtering and refresh support.
 * Uses signals from CopilotStore for reactive updates.
 *
 * Usage:
 *   <app-recommendation-list view="persona" />
 *   <app-recommendation-list view="top" [maxItems]="3" />
 */
@Component({
  selector: 'app-recommendation-list',
  standalone: true,
  imports: [CommonModule, RecommendationCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recommendation-list.component.html',
  styles: [
    `
      :host {
        display: block;
        color: var(--text-primary);
      }

      .rec-list-header {
        margin-bottom: 16px;
      }

      .rec-list-title-row {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 12px;
      }

      .rec-list-title {
        margin: 0;
        font-size: 18px;
        font-weight: 700;
        color: var(--text-primary);
        display: flex;
        align-items: center;
        gap: 10px;
      }

      .critical-badge {
        font-size: 11px;
        font-weight: 700;
        padding: 2px 8px;
        border-radius: 999px;
        background: color-mix(in srgb, var(--danger-color) 18%, transparent);
        color: var(--danger-color);
        border: 1px solid color-mix(in srgb, var(--danger-color) 35%, transparent);
      }

      .severity-summary {
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
      }

      .badge {
        font-size: 11px;
        font-weight: 600;
        padding: 4px 8px;
        border-radius: 6px;
      }

      .badge-critical {
        background: color-mix(in srgb, var(--danger-color) 16%, transparent);
        color: var(--danger-color);
      }

      .badge-high {
        background: color-mix(in srgb, var(--warning-color) 18%, transparent);
        color: var(--warning-color);
      }

      .badge-medium {
        background: color-mix(in srgb, var(--warning-color) 12%, transparent);
        color: var(--text-secondary);
      }

      .badge-low {
        background: color-mix(in srgb, var(--success-color) 14%, transparent);
        color: var(--success-color);
      }

      .btn-refresh,
      .btn-refresh-empty {
        margin-left: auto;
        padding: 8px 14px;
        border-radius: 8px;
        border: 1px solid var(--border-color);
        background: var(--card-bg);
        color: var(--text-primary);
        font-size: 13px;
        font-weight: 600;
        cursor: pointer;
      }

      .btn-refresh:hover:not(:disabled),
      .btn-refresh-empty:hover {
        background: var(--hover-bg);
      }

      .btn-refresh:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .rec-list-loading,
      .rec-list-empty,
      .rec-list-error {
        padding: 16px;
        border-radius: 10px;
        border: 1px dashed var(--border-color);
        background: var(--color-bg-surface-alt, var(--card-bg));
        color: var(--text-secondary);
        font-size: 14px;
      }

      .rec-list-items {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .rec-list-item {
        cursor: pointer;
      }
    `,
  ],
})
export class RecommendationListComponent implements OnInit {
  @Input() view: RecommendationListView = 'persona';
  @Input() maxItems = 5;
  @Input() showRefreshButton = true;
  @Input() frameworkId?: string;

  @Output() recommendationSelected = new EventEmitter<CopilotRecommendation>();

  protected readonly store = inject(CopilotStore);
  private readonly router = inject(Router);

  get displayedRecommendations(): CopilotRecommendation[] {
    switch (this.view) {
      case 'top':    return this.store.topRecommendations().slice(0, this.maxItems);
      case 'persona':return this.store.personaRecommendations().slice(0, this.maxItems);
      case 'urgent': return this.store.urgentRecommendations().slice(0, this.maxItems);
      default:       return this.store.proposedRecommendations().slice(0, this.maxItems);
    }
  }

  get counts() {
    return this.store.recommendationCounts();
  }

  get isRefreshing(): boolean {
    return this.store.loading().refresh;
  }

  get isLoading(): boolean {
    return this.store.loading().recommendations;
  }

  get hasCritical(): boolean {
    return this.counts.critical > 0;
  }

  ngOnInit(): void {
    if (this.store.recommendations().length === 0) {
      this.store.loadRecommendations();
    }
  }

  onRefresh(): void {
    this.store.refreshRecommendations(this.frameworkId);
  }

  onNavigate(deepLink: string): void {
    void this.router.navigateByUrl(deepLink);
  }

  onCardClick(rec: CopilotRecommendation): void {
    this.recommendationSelected.emit(rec);
  }

  trackById(_: number, rec: CopilotRecommendation): string {
    return rec.id;
  }
}
