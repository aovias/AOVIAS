import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CopilotStore } from '../../state/copilot.store';
import { RecommendationCategory, RecommendationSeverity, RecommendationStatus } from '../../models/copilot-recommendation.model';
import type { PersonaWorkspace } from '../../models/persona-experience.model';

@Component({
  selector: 'app-persona-header',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (store.loading().workspace && !store.personaWorkspace()) {
      <div class="persona-banner persona-banner--loading">
        <p>Loading persona context…</p>
      </div>
    } @else {
      @let ws = store.displayWorkspace();
      <header class="persona-banner" [attr.data-theme]="ws.definition.theme">
        <div class="banner-layout">
          <div class="persona-identity">
            <span class="persona-avatar" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path d="M12 12a4 4 0 100-8 4 4 0 000 8z" stroke-width="2"/>
                <path d="M4 20a8 8 0 0116 0" stroke-width="2" stroke-linecap="round"/>
                <path d="M16 7l2-2M18 9l2 1" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </span>
            <div>
              <h2 class="persona-name">{{ ws.definition.displayName }}</h2>
              <p class="persona-focus">{{ focusLine(ws) }}</p>
            </div>
          </div>

          <div class="kpi-grid">
          <article class="kpi-card kpi-card--primary">
            <span class="kpi-label">Overall Readiness</span>
            <div class="kpi-row">
              <span class="kpi-value">{{ readinessDisplay() }}</span>
              @if (readinessTrend()) {
                <span class="kpi-trend">{{ readinessTrend() }}</span>
              }
            </div>
            <div class="kpi-spark" aria-hidden="true">
              <span class="spark-bar" style="height: 40%"></span>
              <span class="spark-bar" style="height: 55%"></span>
              <span class="spark-bar" style="height: 48%"></span>
              <span class="spark-bar" style="height: 70%"></span>
              <span class="spark-bar spark-bar--active" style="height: 85%"></span>
            </div>
          </article>

          <article class="kpi-card">
            <span class="kpi-label">Critical Deficiencies</span>
            <span class="kpi-value">{{ criticalCount() }}</span>
            <span class="kpi-sub">{{ criticalSubtext() }}</span>
          </article>

          <article class="kpi-card">
            <span class="kpi-label">CAPAs Overdue</span>
            <span class="kpi-value">{{ capaCount() }}</span>
            <span class="kpi-sub">{{ capaSubtext() }}</span>
          </article>

          <article class="kpi-card">
            <span class="kpi-label">SLA Breaches Risk</span>
            <span class="kpi-value">{{ slaCount() }}</span>
            <span class="kpi-sub">{{ slaSubtext() }}</span>
          </article>
        </div>
        </div>
      </header>
    }
  `,
  styles: [
    `
      .persona-banner {
        padding: 20px 24px;
        border-radius: 14px;
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--color-primary) 22%, var(--color-bg-surface)) 0%,
          color-mix(in srgb, var(--color-primary) 10%, var(--color-bg-surface)) 55%,
          var(--color-bg-surface) 100%
        );
        border: 1px solid var(--color-primary-border);
        box-shadow: var(--shadow-md);
        display: flex;
        flex-direction: column;
        gap: 20px;
        color: var(--color-text-primary);
      }
      .persona-banner--loading {
        min-height: 120px;
        align-items: center;
        justify-content: center;
        color: var(--color-text-secondary);
        font-size: 14px;
      }
      .banner-layout {
        display: grid;
        grid-template-columns: minmax(240px, 320px) 1fr;
        gap: 16px;
        align-items: stretch;
      }
      .persona-identity {
        display: flex;
        gap: 14px;
        align-items: flex-start;
      }
      .persona-avatar {
        width: 44px;
        height: 44px;
        border-radius: 12px;
        background: var(--color-primary-background);
        border: 1px solid var(--color-primary-border);
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--color-primary-text);
        flex-shrink: 0;
      }
      .persona-avatar svg {
        width: 26px;
        height: 26px;
      }
      .persona-name {
        margin: 0;
        font-size: 22px;
        font-weight: 700;
        color: var(--color-text-primary);
      }
      .persona-focus {
        margin: 6px 0 0;
        font-size: 13px;
        line-height: 1.45;
        color: var(--color-text-secondary);
        max-width: 640px;
      }
      .kpi-grid {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 12px;
      }
      .kpi-card {
        padding: 14px 16px;
        border-radius: 10px;
        background: color-mix(in srgb, var(--color-bg-surface) 60%, transparent);
        border: 1px solid var(--color-border-subtle);
        display: flex;
        flex-direction: column;
        gap: 4px;
        min-height: 88px;
      }
      .kpi-card--primary {
        background: var(--color-primary-background);
        border-color: var(--color-primary-border);
      }
      .kpi-label {
        font-size: 11px;
        font-weight: 600;
        letter-spacing: 0.03em;
        text-transform: uppercase;
        color: var(--color-text-secondary);
      }
      .kpi-row {
        display: flex;
        align-items: baseline;
        gap: 8px;
        flex-wrap: wrap;
      }
      .kpi-value {
        font-size: 28px;
        font-weight: 700;
        line-height: 1.1;
        color: var(--color-text-primary);
      }
      .kpi-trend {
        font-size: 12px;
        font-weight: 600;
        color: var(--color-status-success);
      }
      .kpi-sub {
        font-size: 12px;
        color: var(--color-text-secondary);
      }
      .kpi-spark {
        display: flex;
        align-items: flex-end;
        gap: 3px;
        height: 28px;
        margin-top: 4px;
      }
      .spark-bar {
        flex: 1;
        border-radius: 2px;
        background: color-mix(in srgb, var(--color-primary) 35%, transparent);
        min-height: 4px;
      }
      .spark-bar--active {
        background: var(--color-primary);
      }
      @media (max-width: 900px) {
        .banner-layout {
          grid-template-columns: 1fr;
        }
        .kpi-grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }
      @media (max-width: 520px) {
        .kpi-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class PersonaHeaderComponent {
  protected readonly store = inject(CopilotStore);

  focusLine(ws: PersonaWorkspace): string {
    const objectives = ws.definition.primaryObjectives;
    if (objectives.length > 0) {
      return `Focus: ${objectives.join(', ').replace(/,([^,]*)$/, ' and$1')}.`;
    }
    return ws.landing.subheadline ?? ws.landing.headline;
  }

  readonly readinessDisplay = computed(() => {
    const score = this.store.displayWorkspace().landing.readinessScore;
    if (score != null) return `${Math.round(score)}%`;
    const total = this.store.recommendationCounts().total;
    if (total === 0) return '—';
    const accepted = this.store.recommendations().filter(
      r => r.status === RecommendationStatus.Accepted,
    ).length;
    return `${Math.round((accepted / total) * 100)}%`;
  });

  readonly readinessTrend = computed(() => {
    const snapshots = this.store.analyticsTrends()?.snapshots ?? [];
    if (snapshots.length >= 2) {
      const last = snapshots[snapshots.length - 1];
      const prev = snapshots[snapshots.length - 2];
      const delta = last.recommendationsAccepted - prev.recommendationsAccepted;
      if (delta !== 0) {
        const sign = delta > 0 ? '+' : '';
        return `${sign}${delta}% vs last snapshot`;
      }
    }
    return null;
  });

  readonly criticalCount = computed(() => this.store.recommendationCounts().critical);

  readonly criticalSubtext = computed(() => {
    const c = this.criticalCount();
    return c > 0 ? `${Math.min(c, 3)} require attention` : 'None open';
  });

  readonly capaCount = computed(() =>
    this.store.proposedRecommendations().filter(r => r.category === RecommendationCategory.CAPA).length,
  );

  readonly capaSubtext = computed(() => {
    const high = this.store.proposedRecommendations().filter(
      r => r.category === RecommendationCategory.CAPA && r.severity === RecommendationSeverity.High,
    ).length;
    return high > 0 ? `${high} high priority` : 'On track';
  });

  readonly slaCount = computed(() =>
    this.store.proposedRecommendations().filter(
      r => r.category === RecommendationCategory.SLA || r.category === RecommendationCategory.Escalation,
    ).length,
  );

  readonly slaSubtext = computed(() => {
    const critical = this.store.proposedRecommendations().filter(
      r => (r.category === RecommendationCategory.SLA || r.category === RecommendationCategory.Escalation)
        && r.severity === RecommendationSeverity.Critical,
    ).length;
    return critical > 0 ? `${critical} critical` : 'Monitoring';
  });
}
