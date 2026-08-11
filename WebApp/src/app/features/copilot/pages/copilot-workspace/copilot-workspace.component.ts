import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { AiPageContextService } from '../../../../core/services/ai-page-context.service';
import { SidebarAiAssistantStateService } from '../../../../core/layout/sidebar-ai-assistant-state.service';
import { CopilotStore } from '../../state/copilot.store';
import { PersonaSwitcherComponent } from '../../components/persona-switcher/persona-switcher.component';
import { PersonaHeaderComponent } from '../../components/persona-header/persona-header.component';
import { CopilotWorkspaceCardsComponent } from '../../components/copilot-workspace-cards/copilot-workspace-cards.component';
import { CopilotActivityFeedComponent } from '../../components/copilot-activity-feed/copilot-activity-feed.component';
import { RecommendationListComponent } from '../../components/recommendation-list/recommendation-list.component';
import { DraftCardComponent } from '../../components/draft-card/draft-card.component';
import { ProposalListComponent } from '../../components/proposal-list/proposal-list.component';
import { DraftViewerComponent } from '../../components/draft-viewer/draft-viewer.component';
import { CopilotAnalyticsComponent } from '../../components/copilot-analytics/copilot-analytics.component';
import type { CopilotWorkspaceTab } from '../../models/persona-experience.model';
import type { AiDraft } from '../../models/ai-draft.model';
import type { CopilotActionProposal } from '../../models/proposal.model';

@Component({
  selector: 'app-copilot-workspace',
  standalone: true,
  imports: [
    CommonModule,
    PersonaSwitcherComponent,
    PersonaHeaderComponent,
    CopilotWorkspaceCardsComponent,
    CopilotActivityFeedComponent,
    RecommendationListComponent,
    DraftCardComponent,
    ProposalListComponent,
    DraftViewerComponent,
    CopilotAnalyticsComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="copilot-shell">
      <div class="copilot-container">
        <header class="workspace-header">
          <div class="workspace-intro">
            <div class="title-row">
              <span class="title-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M12 3l1.5 4.5L18 9l-4.5 1.5L12 15l-1.5-4.5L6 9l4.5-1.5L12 3z" stroke-width="1.5" stroke-linejoin="round"/>
                  <path d="M5 19h14" stroke-width="1.5" stroke-linecap="round"/>
                </svg>
              </span>
              <h1>Compliance Copilot</h1>
            </div>
            <p class="subtitle">One copilot — persona-driven recommendations, drafts, and approvals.</p>
          </div>
          <app-persona-switcher />
        </header>

        <app-persona-header />

        <nav class="tab-nav" aria-label="Copilot workspace sections">
          @for (tab of tabs; track tab.id) {
            <button
              type="button"
              class="tab-btn"
              [class.active]="activeTab() === tab.id"
              (click)="selectTab(tab.id)"
            >
              {{ tab.label }}
            </button>
          }
        </nav>

        <div class="tab-body">
          @switch (activeTab()) {
            @case ('overview') {
              <app-copilot-workspace-cards
                (questionSelected)="onSuggestedQuestion($event)"
                (draftSelected)="onDraftSelected($event)"
                (proposalSelected)="onProposalSelected($event)"
              />
            }
            @case ('recommendations') {
              <div class="tab-panel tab-panel--card">
                <app-recommendation-list view="persona" [maxItems]="20" />
              </div>
            }
            @case ('drafts') {
              <div class="tab-panel drafts-layout">
                <div class="draft-list">
                  @if (store.personaWorkspaceDrafts().length === 0 && store.latestDraftsByGroup().length === 0) {
                    <p class="panel-empty">No drafts yet. Generate from a recommendation or use Generate Draft.</p>
                  }
                  @for (draft of draftListItems(); track draft.id) {
                    <app-draft-card
                      [draft]="draft"
                      [isSelected]="selectedDraft()?.id === draft.id"
                      (select)="onDraftSelected($event)"
                    />
                  }
                </div>
                @if (selectedDraft(); as d) {
                  <div class="draft-viewer-wrap">
                    <app-draft-viewer [draft]="d" />
                  </div>
                }
              </div>
            }
            @case ('approvals') {
              <div class="tab-panel tab-panel--card tab-panel--approvals">
                <app-proposal-list />
              </div>
            }
            @case ('history') {
              <div class="tab-panel tab-panel--card">
                <app-copilot-activity-feed />
              </div>
            }
            @case ('analytics') {
              <div class="tab-panel tab-panel--card">
                <app-copilot-analytics />
              </div>
            }
          }
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100%;
        /* Align with shell / dashboard tokens; AOVIAS Primary for Copilot chrome */
        --copilot-bg: transparent;
        --copilot-surface: var(--card-bg);
        --copilot-surface-elevated: var(--color-bg-surface, var(--card-bg));
        --copilot-border: var(--border-color);
        --copilot-text: var(--text-primary);
        --copilot-muted: var(--text-secondary);
        --copilot-accent: var(--color-primary);
        --copilot-gap: 16px;
        --copilot-radius: 12px;
      }

      .copilot-shell {
        background: var(--copilot-bg);
        min-height: 100%;
        color: var(--copilot-text);
      }

      .copilot-container {
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        margin: 0 auto;
        display: flex;
        flex-direction: column;
        gap: var(--copilot-gap);
      }

      .workspace-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 20px;
        flex-wrap: wrap;
      }

      .title-row {
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .title-icon {
        width: 40px;
        height: 40px;
        border-radius: 10px;
        background: color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 18%, transparent);
        border: 1px solid color-mix(in srgb, var(--copilot-accent, var(--color-primary)) 35%, transparent);
        color: var(--copilot-accent);
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
      }

      .title-icon svg {
        width: 22px;
        height: 22px;
      }

      h1 {
        margin: 0;
        font-size: var(--font-size-page-title);
        font-weight: 700;
        color: var(--text-primary);
        line-height: 1.2;
      }

      .subtitle {
        margin: 8px 0 0;
        color: var(--copilot-muted);
        font-size: var(--font-size-subtitle);
        line-height: 1.45;
        max-width: 560px;
      }

      .tab-nav {
        display: flex;
        gap: 4px;
        flex-wrap: wrap;
        border-bottom: 1px solid var(--copilot-border);
      }

      .tab-btn {
        padding: 10px 16px;
        border: none;
        background: transparent;
        color: var(--copilot-muted);
        font-size: 14px;
        font-weight: 600;
        cursor: pointer;
        border-radius: 8px 8px 0 0;
        border-bottom: 2px solid transparent;
        margin-bottom: -1px;
        transition: color 0.15s, border-color 0.15s, background 0.15s;
      }

      .tab-btn:hover {
        color: var(--copilot-text);
        background: rgba(255, 255, 255, 0.04);
      }

      .tab-btn.active {
        color: var(--text-primary);
        border-bottom-color: var(--copilot-accent);
        background: color-mix(in srgb, var(--copilot-accent) 14%, transparent);
      }

      .tab-body {
        min-height: 360px;
        padding-top: 4px;
      }

      .tab-panel--card {
        background: var(--copilot-surface-elevated);
        border: 1px solid var(--copilot-border);
        border-radius: var(--copilot-radius);
        padding: 16px;
      }

      .panel-empty {
        font-size: 14px;
        color: var(--copilot-muted);
        padding: 20px;
        border: 1px dashed var(--copilot-border);
        border-radius: 10px;
        background: rgba(255, 255, 255, 0.03);
        text-align: center;
      }

      .drafts-layout {
        display: grid;
        grid-template-columns: minmax(280px, 360px) 1fr;
        gap: var(--copilot-gap);
        align-items: start;
      }

      .draft-list {
        display: flex;
        flex-direction: column;
        gap: 10px;
      }

      .draft-viewer-wrap {
        border: 1px solid var(--copilot-border);
        border-radius: var(--copilot-radius);
        background: var(--copilot-surface-elevated);
        padding: 16px;
      }

      @media (max-width: 900px) {
        .drafts-layout {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 768px) {
        .copilot-container {
          padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        }
      }
    `,
  ],
})
export class CopilotWorkspaceComponent implements OnInit {
  private readonly aiPageContext = inject(AiPageContextService);
  private readonly sidebarAi = inject(SidebarAiAssistantStateService);

  protected readonly store = inject(CopilotStore);
  protected readonly selectedDraft = signal<AiDraft | null>(null);

  readonly tabs: { id: CopilotWorkspaceTab; label: string }[] = [
    { id: 'overview', label: 'Overview' },
    { id: 'recommendations', label: 'Recommendations' },
    { id: 'drafts', label: 'Drafts' },
    { id: 'approvals', label: 'Approvals' },
    { id: 'history', label: 'History' },
    { id: 'analytics', label: 'Analytics' },
  ];

  readonly activeTab = this.store.activeWorkspaceTab;

  draftListItems(): AiDraft[] {
    const persona = this.store.personaWorkspaceDrafts();
    if (persona.length > 0) return persona;
    return this.store.latestDraftsByGroup();
  }

  ngOnInit(): void {
    this.aiPageContext.setPageContext({
      pageContext: 'Copilot',
      label: 'Compliance Copilot Workspace',
    });

    this.store.loadExperienceCatalog();
    this.store.loadRecommendations();
    this.store.loadDrafts();
    this.store.loadProposals();
    this.store.loadActivityFeed();
    this.store.loadUsageMetrics();
    this.store.loadAnalyticsTrends(14);
    this.store.resolvePersona('Copilot');
    this.store.recordActivity('WorkspaceViewed');
  }

  selectTab(tab: CopilotWorkspaceTab): void {
    this.store.setWorkspaceTab(tab);
    if (tab === 'history') {
      this.store.loadActivityFeed();
    }
  }

  onSuggestedQuestion(question: string): void {
    this.sidebarAi.sendPrompt(question);
  }

  onDraftSelected(draft: AiDraft): void {
    this.selectedDraft.set(draft);
    this.store.selectDraft(draft);
    this.store.setWorkspaceTab('drafts');
  }

  onProposalSelected(proposal: CopilotActionProposal): void {
    this.store.selectProposal(proposal);
    this.store.setWorkspaceTab('approvals');
  }
}
