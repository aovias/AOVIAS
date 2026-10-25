import { Component, ChangeDetectionStrategy, inject, Output, EventEmitter, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CopilotStore } from '../../state/copilot.store';
import type { CopilotRecommendation } from '../../models/copilot-recommendation.model';
import { RecommendationCategory, RecommendationSeverity } from '../../models/copilot-recommendation.model';
import type { AiDraft } from '../../models/ai-draft.model';
import { getDraftTypeLabel } from '../../models/ai-draft.model';
import type { CopilotActionProposal } from '../../models/proposal.model';

@Component({
  selector: 'app-copilot-workspace-cards',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
  @if (store.loading().workspace && !store.personaWorkspace()) {
    <div class="overview-loading">
      <div class="skeleton skeleton--banner"></div>
      <div class="skeleton skeleton--row"></div>
      <div class="skeleton skeleton--grid"></div>
    </div>
  } @else {
    @let ws = store.displayWorkspace();
    <div class="overview">
      <section class="section">
        <div class="section-head">
          <h3 class="section-title">At a Glance</h3>
          <span class="section-hint">Key signals from your persona context</span>
        </div>
        <div class="glance-grid">
          @for (card of glanceCards(); track card.id) {
            <article class="glance-card" [class]="'glance-card--' + card.id">
              <div class="glance-card-top">
                <span class="glance-icon" [class]="'glance-icon--' + card.id" aria-hidden="true">
                  @switch (card.id) {
                    @case ('priority') {
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M12 9v4m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                    }
                    @case ('evidence') {
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                    }
                    @case ('verification') {
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                    }
                    @default {
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 10V3L4 14h7v7l9-11h-7z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                    }
                  }
                </span>
                <h4>{{ card.title }}</h4>
              </div>
              <p class="glance-body">{{ card.body }}</p>
              @if (card.actionLabel) {
                <button type="button" class="glance-link" (click)="onGlanceAction(card)">
                  {{ card.actionLabel }}
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M5 12h14M13 6l6 6-6 6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                </button>
              }
            </article>
          }
        </div>
      </section>

      <section class="section section--card">
        <div class="section-head section-head--split">
          <div class="section-head-left">
            <h3 class="section-title">Suggested Questions</h3>
            <span class="section-hint">Ask the copilot in one click</span>
          </div>
          <button type="button" class="view-all-link" (click)="store.setWorkspaceTab('recommendations')">
            View all
          </button>
        </div>
        <div class="question-chips">
          @for (q of ws.suggestedQuestions; track q) {
            <button type="button" class="question-chip" (click)="questionSelected.emit(q)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
              {{ q }}
            </button>
          }
        </div>
      </section>

      <div class="lists-row">
        <section class="section list-panel">
          <div class="section-head">
            <h3 class="section-title">Recent Recommendations</h3>
            <span class="section-badge">{{ recentRecommendations().length }}</span>
          </div>
          @if (recentRecommendations().length === 0) {
            <p class="empty">No recommendations yet. Refresh from the Recommendations tab.</p>
          } @else {
            <ul class="rec-list">
              @for (rec of recentRecommendations(); track rec.id) {
                <li class="rec-item" (click)="onRecClick(rec)">
                  <span class="severity-rail" [class]="severityClass(rec.severity)"></span>
                  <span class="severity" [class]="severityClass(rec.severity)">{{ rec.severity }}</span>
                  <span class="rec-title">{{ rec.title }}</span>
                  <span class="rec-time">{{ relativeTime(rec.generatedAtUtc) }}</span>
                </li>
              }
            </ul>
          }
        </section>

        <section class="section list-panel">
          <div class="section-head">
            <h3 class="section-title">Recent Drafts</h3>
            <span class="section-badge">{{ recentDrafts().length }}</span>
          </div>
          @if (recentDrafts().length === 0) {
            <p class="empty">No drafts yet. Generate from a recommendation or Drafts tab.</p>
          } @else {
            <ul class="draft-list">
              @for (draft of recentDrafts(); track draft.id) {
                <li class="draft-item" (click)="draftSelected.emit(draft)">
                  <span class="draft-icon" aria-hidden="true">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                  </span>
                  <div class="draft-meta">
                    <span class="draft-title">{{ draft.title }}</span>
                    <span class="draft-type">{{ getDraftTypeLabel(draft.draftTypeEnum) }}</span>
                  </div>
                  <span class="draft-time">{{ relativeTime(draft.generatedAtUtc) }}</span>
                </li>
              }
            </ul>
          }
        </section>
      </div>
    </div>
  }
  `,
  styles: [
    `
      .overview {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .section-head {
        display: flex;
        align-items: center;
        gap: 10px;
        margin-bottom: 12px;
        flex-wrap: wrap;
      }
      .section-head--split {
        justify-content: space-between;
        align-items: flex-start;
      }
      .section-head-left {
        display: flex;
        align-items: center;
        gap: 10px;
        flex-wrap: wrap;
      }
      .view-all-link {
        border: none;
        background: none;
        color: var(--copilot-accent, var(--color-primary));
        font-size: 13px;
        font-weight: 600;
        cursor: pointer;
        padding: 0;
      }
      .view-all-link:hover {
        text-decoration: underline;
      }
      .section-title {
        margin: 0;
        font-size: 15px;
        font-weight: 700;
        color: var(--copilot-text, var(--color-text-primary));
      }
      .section-hint {
        font-size: 12px;
        color: var(--copilot-muted, var(--color-text-secondary));
      }
      .section-badge {
        font-size: 11px;
        font-weight: 700;
        padding: 2px 8px;
        border-radius: 999px;
        background: rgba(255, 255, 255, 0.06);
        color: var(--copilot-muted);
        border: 1px solid var(--copilot-border, rgba(255, 255, 255, 0.09));
      }
      .section--card {
        background: var(--copilot-surface-elevated, var(--color-bg-surface));
        border: 1px solid var(--copilot-border, rgba(255, 255, 255, 0.09));
        border-radius: 12px;
        padding: 16px;
      }
      .glance-grid {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 12px;
      }
      .glance-card {
        padding: 16px;
        border-radius: 12px;
        background: var(--copilot-surface-elevated, var(--color-bg-surface));
        border: 1px solid var(--copilot-border, rgba(255, 255, 255, 0.09));
        display: flex;
        flex-direction: column;
        gap: 10px;
        min-height: 132px;
        transition: border-color 0.15s, box-shadow 0.15s;
      }
      .glance-card:hover {
        border-color: color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 45%, transparent);
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.25);
      }
      .glance-card-top {
        display: flex;
        align-items: center;
        gap: 10px;
      }
      .glance-icon {
        width: 36px;
        height: 36px;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
      }
      .glance-icon svg {
        width: 18px;
        height: 18px;
      }
      .glance-icon--priority {
        background: rgba(239, 68, 68, 0.15);
        color: #f87171;
        border: 1px solid rgba(248, 113, 113, 0.35);
      }
      .glance-icon--evidence {
        background: rgba(34, 197, 94, 0.12);
        color: #4ade80;
        border: 1px solid rgba(74, 222, 128, 0.3);
      }
      .glance-icon--verification {
        background: rgba(45, 212, 191, 0.12);
        color: #2dd4bf;
        border: 1px solid rgba(45, 212, 191, 0.3);
      }
      .glance-icon--action {
        background: color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 15%, transparent);
        color: var(--copilot-accent, var(--color-primary-text));
        border: 1px solid color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 35%, transparent);
      }
      .glance-card h4 {
        margin: 0;
        font-size: 13px;
        font-weight: 700;
        color: var(--copilot-text, var(--color-text-primary));
      }
      .glance-body {
        margin: 0;
        font-size: 13px;
        line-height: 1.5;
        color: var(--copilot-muted);
        flex: 1;
      }
      .glance-link {
        align-self: flex-start;
        display: inline-flex;
        align-items: center;
        gap: 4px;
        border: none;
        background: none;
        padding: 0;
        font-size: 13px;
        font-weight: 600;
        color: var(--copilot-accent, var(--color-primary));
        cursor: pointer;
      }
      .glance-link svg {
        width: 14px;
        height: 14px;
      }
      .glance-link:hover {
        text-decoration: underline;
      }
      .question-chips {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .question-chip {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 10px 14px;
        border-radius: 999px;
        border: 1px solid var(--copilot-border);
        background: rgba(255, 255, 255, 0.04);
        font-size: 13px;
        font-weight: 500;
        color: var(--copilot-text);
        cursor: pointer;
        transition: background 0.15s, border-color 0.15s;
      }
      .question-chip svg {
        width: 16px;
        height: 16px;
        color: var(--copilot-accent);
        flex-shrink: 0;
      }
      .question-chip:hover {
        border-color: color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 50%, transparent);
        background: color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 12%, transparent);
      }
      .lists-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px;
      }
      .list-panel {
        padding: 16px;
        border-radius: 12px;
        background: var(--copilot-surface-elevated, var(--color-bg-surface));
        border: 1px solid var(--copilot-border);
        min-height: 220px;
      }
      .rec-list, .draft-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      .rec-item, .draft-item {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 10px 8px;
        border-radius: 8px;
        cursor: pointer;
        transition: background 0.12s;
      }
      .rec-item + .rec-item,
      .draft-item + .draft-item {
        margin-top: 2px;
      }
      .rec-item:hover, .draft-item:hover {
        background: rgba(255, 255, 255, 0.05);
      }
      .severity-rail {
        width: 3px;
        align-self: stretch;
        border-radius: 2px;
        flex-shrink: 0;
        min-height: 28px;
      }
      .severity-rail.severity-critical { background: #f87171; }
      .severity-rail.severity-high { background: #fb923c; }
      .severity-rail.severity-medium { background: #facc15; }
      .severity-rail.severity-low { background: #4ade80; }
      .severity {
        font-size: 10px;
        font-weight: 700;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        padding: 4px 8px;
        border-radius: 6px;
        flex-shrink: 0;
      }
      .severity-critical { background: rgba(239, 68, 68, 0.18); color: #fca5a5; border: 1px solid rgba(248, 113, 113, 0.35); }
      .severity-high { background: rgba(249, 115, 22, 0.18); color: #fdba74; border: 1px solid rgba(251, 146, 60, 0.35); }
      .severity-medium { background: rgba(234, 179, 8, 0.18); color: #fde047; border: 1px solid rgba(250, 204, 21, 0.35); }
      .severity-low { background: rgba(34, 197, 94, 0.15); color: #86efac; border: 1px solid rgba(74, 222, 128, 0.35); }
      .rec-title {
        flex: 1;
        font-size: 13px;
        font-weight: 500;
        color: var(--copilot-text);
        line-height: 1.4;
      }
      .rec-time, .draft-time {
        font-size: 11px;
        font-weight: 500;
        color: var(--copilot-muted);
        flex-shrink: 0;
      }
      .draft-icon {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        background: color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 12%, transparent);
        border: 1px solid color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 25%, transparent);
        color: var(--copilot-accent);
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
      }
      .draft-icon svg {
        width: 16px;
        height: 16px;
      }
      .draft-meta {
        flex: 1;
        min-width: 0;
      }
      .draft-title {
        display: block;
        font-size: 13px;
        font-weight: 600;
        color: var(--copilot-text);
      }
      .draft-type {
        font-size: 11px;
        color: var(--copilot-muted);
        margin-top: 2px;
      }
      .empty {
        font-size: 13px;
        color: var(--copilot-muted);
        padding: 12px;
        background: rgba(255, 255, 255, 0.03);
        border-radius: 8px;
        border: 1px dashed var(--copilot-border);
      }
      .overview-loading {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }
      .skeleton {
        border-radius: 12px;
        background: linear-gradient(90deg, rgba(255,255,255,0.04) 0%, rgba(255,255,255,0.08) 50%, rgba(255,255,255,0.04) 100%);
        animation: shimmer 1.2s infinite;
      }
      .skeleton--banner { height: 140px; }
      .skeleton--row { height: 48px; }
      .skeleton--grid { height: 200px; }
      @keyframes shimmer {
        0% { opacity: 0.65; }
        50% { opacity: 1; }
        100% { opacity: 0.65; }
      }
      @media (max-width: 1024px) {
        .glance-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
        .lists-row { grid-template-columns: 1fr; }
      }
      @media (max-width: 520px) {
        .glance-grid { grid-template-columns: 1fr; }
      }
    `,
  ],
})
export class CopilotWorkspaceCardsComponent {
  protected readonly store = inject(CopilotStore);
  private readonly router = inject(Router);

  readonly getDraftTypeLabel = getDraftTypeLabel;

  @Output() draftSelected = new EventEmitter<AiDraft>();
  @Output() proposalSelected = new EventEmitter<CopilotActionProposal>();
  @Output() questionSelected = new EventEmitter<string>();
  @Output() recommendationSelected = new EventEmitter<CopilotRecommendation>();

  readonly recentRecommendations = computed(() =>
    this.store.displayWorkspace().prioritizedRecommendations?.slice(0, 5)
      ?? this.store.personaRecommendations().slice(0, 5),
  );

  readonly recentDrafts = computed(() => {
    const highlighted = this.store.displayWorkspace().highlightedDrafts ?? [];
    if (highlighted.length > 0) return highlighted.slice(0, 5);
    return this.store.latestDraftsByGroup().slice(0, 5);
  });

  readonly glanceCards = computed(() => {
    const ws = this.store.displayWorkspace();
    const recs = this.store.proposedRecommendations();
    const evidenceMissing = recs.filter(r => r.category === RecommendationCategory.Evidence).length;
    const verificationPending = recs.filter(r => r.category === RecommendationCategory.Verification).length;
    const topRec = ws.prioritizedRecommendations[0];
    const recommendedAction = ws.definition.recommendedActions[0] ?? 'Review top recommendations';

    return [
      {
        id: 'priority',
        title: 'Top Priority',
        body: ws.landing.headline,
        actionLabel: topRec?.deepLink ? 'View details' : 'View recommendations',
        route: topRec?.deepLink ?? null,
        tab: 'recommendations' as const,
      },
      {
        id: 'evidence',
        title: 'Evidence Gap',
        body: evidenceMissing > 0
          ? `${evidenceMissing} obligations need evidence attention.`
          : ws.landing.subheadline.includes('evidence')
            ? ws.landing.subheadline
            : 'Evidence coverage is stable.',
        actionLabel: 'View obligations',
        route: '/app/evidence',
        tab: null,
      },
      {
        id: 'verification',
        title: 'Verification Queue',
        body: verificationPending > 0
          ? `${verificationPending} items pending verification.`
          : 'No verification backlog flagged.',
        actionLabel: 'Review queue',
        route: '/app/evidence',
        tab: null,
      },
      {
        id: 'action',
        title: 'Recommended Action',
        body: recommendedAction,
        actionLabel: 'Create draft',
        route: null,
        tab: 'drafts' as const,
      },
    ];
  });

  severityClass(severity: RecommendationSeverity): string {
    return `severity-${severity.toLowerCase()}`;
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }

  onGlanceAction(card: {
    route: string | null;
    tab: 'recommendations' | 'drafts' | null;
  }): void {
    if (card.route) {
      void this.router.navigateByUrl(card.route);
      return;
    }
    if (card.tab) {
      this.store.setWorkspaceTab(card.tab);
    }
  }

  onRecClick(rec: CopilotRecommendation): void {
    this.recommendationSelected.emit(rec);
    this.store.recordActivity('RecommendationViewed', {
      entityId: rec.id,
      entityType: 'Recommendation',
    });
    if (rec.deepLink) {
      void this.router.navigateByUrl(rec.deepLink);
    } else {
      this.store.setWorkspaceTab('recommendations');
    }
  }
}
