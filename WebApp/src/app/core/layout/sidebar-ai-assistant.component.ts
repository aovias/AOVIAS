import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  SidebarAiAssistantStateService,
  SidebarAiSnapshotCard,
  SidebarAiSuggestion,
} from './sidebar-ai-assistant-state.service';
import { ComplianceResponseComponent } from './ai-response/compliance-response.component';

@Component({
  selector: 'app-sidebar-ai-assistant',
  standalone: true,
  imports: [CommonModule, ComplianceResponseComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="assistant-panel" aria-label="AI Assistant panel">
      <div class="assistant-content" #contentScroll (scroll)="onScroll()">
        @if (!state.hasConversation() && state.uiState() === 'initial') {
          <section class="welcome-card">
            <h4 class="welcome-title">Hello! I'm PRAMAN AI</h4>
            <p class="welcome-description">
              Your AI companion for seamless NABH, JCI, and NABL compliance with audit readiness,
              standards interpretation, risk analysis, and evidence management.
            </p>
            <div class="chip-row">
              @for (chip of state.welcomeChips(); track chip.id) {
                <button
                  type="button"
                  class="chip"
                  [class.chip--info]="chip.tone === 'info'"
                  [class.chip--warning]="chip.tone === 'warning'"
                  [class.chip--success]="chip.tone === 'success'"
                  [class.chip--brand]="chip.tone === 'brand'"
                  [disabled]="state.isBusy()"
                  (click)="onQuickPrompt(chip)"
                >
                  {{ chip.text }}
                </button>
              }
            </div>
          </section>

          <section class="panel-section">
            <h4 class="section-title">Compliance Snapshot</h4>
            @if (state.loadingSnapshot()) {
              <div class="snapshot-grid snapshot-grid--loading" aria-hidden="true">
                <div class="snapshot-card shimmer"></div>
                <div class="snapshot-card shimmer"></div>
                <div class="snapshot-card shimmer"></div>
                <div class="snapshot-card shimmer"></div>
              </div>
            } @else {
              <div class="snapshot-grid">
                @for (card of state.snapshotCards(); track card.id) {
                  <button
                    type="button"
                    class="snapshot-card"
                    [class.snapshot-card--danger]="card.tone === 'danger'"
                    [class.snapshot-card--warning]="card.tone === 'warning'"
                    [class.snapshot-card--info]="card.tone === 'info'"
                    [class.snapshot-card--success]="card.tone === 'success'"
                    (click)="onSnapshotPrompt(card)"
                    [disabled]="state.isBusy()"
                  >
                    <span class="snapshot-value">{{ card.value }}</span>
                    <span class="snapshot-label">{{ card.title }}</span>
                  </button>
                }
              </div>
            }
          </section>

          <section class="panel-section">
            <h4 class="section-title">Suggested for You</h4>
            <div class="suggestions-list">
              @for (item of state.suggestedActions(); track item.id) {
                <button
                  type="button"
                  class="suggestion-item"
                  [class.suggestion-item--info]="item.tone === 'info'"
                  [class.suggestion-item--warning]="item.tone === 'warning'"
                  [class.suggestion-item--success]="item.tone === 'success'"
                  [class.suggestion-item--brand]="item.tone === 'brand'"
                  [disabled]="state.isBusy()"
                  (click)="onQuickPrompt(item)"
                >
                  {{ item.text }}
                </button>
              }
            </div>
          </section>

          @if (state.knowledgeSourceChips().length) {
            <section class="panel-section">
              <h4 class="section-title">Knowledge Sources</h4>
              <div class="chip-row">
                @for (chip of state.knowledgeSourceChips(); track chip.id) {
                  <button
                    type="button"
                    class="chip chip--info"
                    [disabled]="state.isBusy()"
                    (click)="onQuickPrompt(chip)"
                  >
                    {{ chip.text }}
                  </button>
                }
              </div>
            </section>
          }
        } @else {
          <section class="chat-thread" aria-live="polite">
            @if (!state.hasConversation()) {
              <p class="chat-helper">{{ state.helperText }}</p>
            }
            @for (turn of state.turns(); track turn.id) {
              <article class="chat-turn">
                <div class="bubble bubble--user">{{ turn.prompt }}</div>
                @if (turn.contextLabel) {
                  <div class="context-label">{{ turn.contextLabel }}</div>
                }
                @if (turn.status === 'thinking') {
                  <div class="bubble bubble--ai bubble--typing">
                    <span class="typing-dot"></span>
                    <span class="typing-dot"></span>
                    <span class="typing-dot"></span>
                    <span class="typing-text">Analyzing...</span>
                  </div>
                } @else if (turn.status === 'error') {
                  <div class="bubble bubble--error">
                    <span>{{
                      turn.errorMessage || "I couldn't process this. Please try again."
                    }}</span>
                    <button type="button" class="retry-btn" (click)="state.retryLastFailed()">
                      Retry
                    </button>
                  </div>
                } @else if (turn.presentation) {
                  <app-compliance-response [data]="turn.presentation" />
                  @if (turn.confidence != null) {
                    <div class="confidence-row">Confidence: {{ turn.confidence }}%</div>
                  }
                  @if (turn.citations?.length || turn.sources?.length) {
                    <div class="references-panel">
                      <div class="references-title">References / Citations</div>
                      <ul class="references-list">
                        @for (
                          cite of turn.citations?.length ? turn.citations : [];
                          track cite.label
                        ) {
                          <li>
                            <span class="ref-label">{{ cite.label }}</span>
                            @if (cite.excerpt) {
                              <span class="ref-excerpt">{{ cite.excerpt }}</span>
                            }
                          </li>
                        }
                        @if (!turn.citations?.length) {
                          @for (src of turn.sources; track src) {
                            <li>
                              <span class="ref-label">{{ src }}</span>
                            </li>
                          }
                        }
                      </ul>
                    </div>
                  }
                  @if (turn.followUps?.length) {
                    <div class="followups">
                      @for (followUp of turn.followUps; track followUp) {
                        <button
                          type="button"
                          class="followup-chip"
                          (click)="state.sendPrompt(followUp)"
                        >
                          {{ followUp }}
                        </button>
                      }
                    </div>
                  }
                } @else {
                  <div class="bubble bubble--ai">
                    @if (turn.answerModeLabel) {
                      <span class="answer-mode-badge">{{ turn.answerModeLabel }}</span>
                    }
                    @if (turn.confidence != null) {
                      <span class="confidence-badge">Confidence {{ turn.confidence }}%</span>
                    }
                    <p class="response-text">{{ turn.responseText }}</p>
                    @if (turn.reasoning?.length) {
                      <ul class="reasoning-list">
                        @for (why of turn.reasoning; track why) {
                          <li>{{ why }}</li>
                        }
                      </ul>
                    }
                  </div>
                  @if (turn.citations?.length || turn.sources?.length) {
                    <div class="references-panel">
                      <div class="references-title">References / Citations</div>
                      <ul class="references-list">
                        @for (
                          cite of turn.citations?.length ? turn.citations : [];
                          track cite.label
                        ) {
                          <li>
                            <span class="ref-label">{{ cite.label }}</span>
                            @if (cite.excerpt) {
                              <span class="ref-excerpt">{{ cite.excerpt }}</span>
                            }
                          </li>
                        }
                        @if (!turn.citations?.length) {
                          @for (src of turn.sources; track src) {
                            <li>
                              <span class="ref-label">{{ src }}</span>
                            </li>
                          }
                        }
                      </ul>
                    </div>
                  }
                  @if (turn.followUps?.length) {
                    <div class="followups">
                      @for (followUp of turn.followUps; track followUp) {
                        <button
                          type="button"
                          class="followup-chip"
                          (click)="state.sendPrompt(followUp)"
                        >
                          {{ followUp }}
                        </button>
                      }
                    </div>
                  }
                }
              </article>
            }
            @if (state.hasConversation()) {
              <div class="clear-conversation-row">
                <a
                  href="#"
                  role="button"
                  class="clear-conversation-link"
                  (click)="onClearConversation($event)"
                >
                  Clear conversation
                </a>
              </div>
            }
          </section>
        }
      </div>

      <footer class="assistant-input-wrap">
        @if (state.uiState() === 'error') {
          <div class="inline-error">
            {{ state.lastError() }}
            <button type="button" class="retry-inline" (click)="state.retryLastFailed()">
              Retry
            </button>
          </div>
        }
        <div class="assistant-input-row">
          <textarea
            class="assistant-input"
            [value]="state.activeInput()"
            rows="1"
            (input)="onInput($event)"
            (keydown)="onInputKeydown($event)"
            [attr.aria-label]="'Assistant message input'"
            [placeholder]="'Ask about compliance, risks, or evidence...'"
          ></textarea>
          <div class="assistant-actions-column">
            <button
              type="button"
              class="send-btn"
              [disabled]="!state.canSend()"
              (click)="state.sendTypedMessage()"
              aria-label="Send message"
              title="Send message"
            >
              ↑
            </button>
            <button
              type="button"
              class="assistant-option-btn"
              aria-label="Start audio recording"
              title="Start audio recording"
            >
              🎤
            </button>
            <button
              type="button"
              class="assistant-option-btn"
              aria-label="Attach file"
              title="Attach file"
            >
              📎
            </button>
          </div>
        </div>
      </footer>
    </section>

    <button
      type="button"
      class="assistant-collapsed-pill"
      aria-label="AI Assistant"
      title="AI Assistant"
    >
      AI
    </button>
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        flex: 1;
        min-height: 0;
        padding: 0;
      }

      .assistant-panel {
        display: flex;
        flex-direction: column;
        width: 100%;
        max-width: 100%;
        height: 100%;
        flex: 1;
        min-height: 0;
        max-height: 100%;
        border: none;
        border-radius: 0;
        background: var(--card-bg);
        overflow: hidden;
      }

      .assistant-content {
        flex: 1;
        min-height: 0;
        overflow-y: auto;
        padding: 10px 10px 12px;
      }

      .welcome-card {
        border-radius: 12px;
        padding: 12px;
        border: 1px solid color-mix(in srgb, var(--primary-color) 40%, transparent);
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--primary-color) 18%, var(--card-bg)) 0%,
          color-mix(in srgb, var(--primary-color) 8%, var(--card-bg)) 100%
        );
        box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--primary-color) 16%, transparent);
      }

      .welcome-title {
        margin: 0 0 6px;
        color: var(--text-primary);
        font-size: 16px;
        font-weight: 700;
      }

      .welcome-description {
        margin: 0;
        color: var(--text-secondary);
        font-size: 12px;
        line-height: 1.35;
      }

      .chip-row {
        margin-top: 10px;
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
      }

      .chip {
        border: 1px solid transparent;
        border-radius: 999px;
        font-size: 11px;
        font-weight: 600;
        color: var(--color-text-inverse);
        padding: 4px 10px;
        cursor: pointer;
      }

      .chip--info {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--color-status-info) 80%, transparent),
          color-mix(in srgb, var(--color-status-info) 60%, transparent)
        );
      }
      .chip--warning {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--warning-color) 80%, transparent),
          color-mix(in srgb, var(--warning-color) 60%, transparent)
        );
      }
      .chip--success {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--success-color) 80%, transparent),
          color-mix(in srgb, var(--success-color) 60%, transparent)
        );
      }
      .chip--brand {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--color-primary) 80%, transparent),
          color-mix(in srgb, var(--color-primary) 60%, transparent)
        );
      }

      .panel-section {
        margin-top: 14px;
      }

      .section-title {
        margin: 0 0 8px;
        font-size: 14px;
        font-weight: 700;
        line-height: 1;
        color: var(--text-primary);
      }

      .snapshot-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 8px;
      }

      .snapshot-grid--loading .snapshot-card {
        min-height: 56px;
      }

      .snapshot-card {
        text-align: left;
        border: 1px solid var(--border-color);
        border-radius: 10px;
        padding: 8px;
        display: flex;
        flex-direction: column;
        gap: 2px;
        cursor: pointer;
      }

      .snapshot-card--danger {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--danger-color) 22%, transparent),
          color-mix(in srgb, var(--danger-color) 14%, transparent)
        );
      }
      .snapshot-card--warning {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--warning-color) 22%, transparent),
          color-mix(in srgb, var(--warning-color) 14%, transparent)
        );
      }
      .snapshot-card--info {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--color-status-info) 22%, transparent),
          color-mix(in srgb, var(--color-status-info) 14%, transparent)
        );
      }
      .snapshot-card--success {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--success-color) 22%, transparent),
          color-mix(in srgb, var(--success-color) 14%, transparent)
        );
      }

      .snapshot-value {
        color: var(--color-text-inverse);
        font-size: 20px;
        font-weight: 700;
        line-height: 1;
      }

      .snapshot-label {
        color: var(--text-secondary);
        font-size: 11px;
        font-weight: 500;
      }

      .suggestions-list {
        display: flex;
        flex-direction: column;
        gap: 8px;
      }

      .suggestion-item {
        text-align: left;
        border-radius: 10px;
        border: 1px solid color-mix(in srgb, var(--border-color) 85%, transparent);
        color: var(--text-secondary);
        font-size: 11px;
        font-weight: 500;
        line-height: 1.25;
        padding: 10px;
        cursor: pointer;
      }

      .suggestion-item--info {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--color-status-info) 70%, transparent),
          color-mix(in srgb, var(--color-status-info) 55%, transparent)
        );
      }
      .suggestion-item--warning {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--warning-color) 70%, transparent),
          color-mix(in srgb, var(--warning-color) 55%, transparent)
        );
      }
      .suggestion-item--success {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--success-color) 70%, transparent),
          color-mix(in srgb, var(--success-color) 55%, transparent)
        );
      }
      .suggestion-item--brand {
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--primary-color) 70%, transparent),
          color-mix(in srgb, var(--primary-color) 55%, transparent)
        );
      }

      .chat-thread {
        display: flex;
        flex-direction: column;
        gap: 10px;
      }

      .chat-helper {
        margin: 0;
        color: var(--text-secondary);
        font-size: 12px;
      }

      .clear-conversation-row {
        display: flex;
        justify-content: center;
        margin-top: 4px;
        padding-top: 4px;
      }

      .clear-conversation-link {
        color: var(--text-muted);
        font-size: 11px;
        font-weight: 500;
        text-decoration: underline;
        text-underline-offset: 2px;
        cursor: pointer;
      }

      .clear-conversation-link:hover {
        color: var(--danger-color);
      }

      .clear-conversation-link:focus-visible {
        outline: 2px solid color-mix(in srgb, var(--primary-color) 60%, transparent);
        outline-offset: 2px;
        border-radius: 3px;
      }

      .chat-turn {
        display: flex;
        flex-direction: column;
        gap: 6px;
      }

      .bubble {
        max-width: 92%;
        border-radius: 12px;
        padding: 8px 10px;
        font-size: 12px;
        line-height: 1.35;
        color: var(--color-text-inverse);
      }

      .bubble--user {
        align-self: flex-end;
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--primary-color) 88%, transparent),
          color-mix(in srgb, var(--primary-hover) 80%, transparent)
        );
      }

      .bubble--ai {
        align-self: flex-start;
        background: color-mix(in srgb, var(--bg-tertiary) 92%, transparent);
        border: 1px solid var(--border-color);
      }

      .bubble--error {
        align-self: flex-start;
        display: inline-flex;
        align-items: center;
        gap: 8px;
        background: color-mix(in srgb, var(--danger-color) 26%, transparent);
        border: 1px solid color-mix(in srgb, var(--danger-color) 50%, transparent);
      }

      .response-text {
        margin: 0;
      }

      .reasoning-list {
        margin: 6px 0 0;
        padding-left: 16px;
        color: var(--text-secondary);
      }

      .meta-row {
        margin-top: 6px;
        font-size: 11px;
        color: var(--text-muted);
      }

      .answer-mode-badge {
        display: inline-block;
        margin-bottom: 6px;
        padding: 2px 8px;
        border-radius: 10px;
        font-size: 10px;
        font-weight: 600;
        letter-spacing: 0.02em;
        text-transform: uppercase;
        color: var(--text-muted);
        background: rgba(125, 125, 125, 0.14);
      }

      .confidence-badge {
        display: inline-block;
        margin: 0 0 6px 6px;
        padding: 2px 8px;
        border-radius: 10px;
        font-size: 10px;
        font-weight: 600;
        color: #065f46;
        background: rgba(16, 185, 129, 0.16);
      }

      .confidence-row {
        font-size: 11px;
        color: var(--text-muted);
        margin: 4px 0 6px;
      }

      .references-panel {
        margin-top: 6px;
        padding: 8px 10px;
        border: 1px solid color-mix(in srgb, var(--border-color) 85%, transparent);
        border-radius: 8px;
        background: color-mix(in srgb, var(--bg-tertiary) 70%, transparent);
      }

      .references-title {
        font-size: 11px;
        font-weight: 650;
        color: var(--text-secondary);
        margin-bottom: 6px;
      }

      .references-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 6px;
      }

      .references-list li {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }

      .ref-label {
        font-size: 11px;
        color: var(--text-primary);
      }

      .ref-excerpt {
        font-size: 10px;
        color: var(--text-muted);
        line-height: 1.35;
      }

      .context-label {
        align-self: flex-start;
        color: var(--text-muted);
        font-size: 10px;
      }

      .bubble--typing {
        display: inline-flex;
        align-items: center;
        gap: 4px;
      }

      .typing-dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: color-mix(in srgb, var(--primary-color) 70%, transparent);
        animation: typing 1.2s infinite ease-in-out;
      }

      .typing-dot:nth-child(2) {
        animation-delay: 0.15s;
      }

      .typing-dot:nth-child(3) {
        animation-delay: 0.3s;
      }

      .typing-text {
        margin-left: 6px;
        color: var(--text-secondary);
        font-size: 11px;
      }

      .retry-btn,
      .retry-inline {
        border: 1px solid color-mix(in srgb, var(--border-color) 80%, transparent);
        border-radius: 7px;
        background: transparent;
        color: var(--text-primary);
        padding: 3px 8px;
        font-size: 11px;
        cursor: pointer;
      }

      .followups {
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
      }

      .followup-chip {
        border: 1px solid color-mix(in srgb, var(--primary-color) 40%, transparent);
        border-radius: 999px;
        background: color-mix(in srgb, var(--bg-tertiary) 90%, transparent);
        color: var(--primary-color);
        font-size: 11px;
        padding: 4px 9px;
        cursor: pointer;
      }

      .assistant-input-wrap {
        border-top: 1px solid var(--border-color);
        background: var(--card-bg);
        padding: 8px;
      }

      .inline-error {
        color: var(--danger-color);
        font-size: 11px;
        margin-bottom: 6px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
      }

      .assistant-input-row {
        display: flex;
        align-items: flex-start;
        gap: 8px;
      }

      .assistant-input {
        flex: 1;
        resize: none;
        min-height: 105px;
        max-height: 105px;
        border-radius: 10px;
        border: 1px solid var(--border-color);
        background: color-mix(in srgb, var(--bg-tertiary) 95%, transparent);
        color: var(--text-primary);
        padding: 3px 5px;
        font-size: 12px;
        line-height: 1.3;
      }

      .assistant-actions-column {
        display: inline-flex;
        flex-direction: column;
        align-items: center;
        gap: 6px;
      }

      .send-btn {
        width: 34px;
        height: 34px;
        border: none;
        border-radius: 9px;
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--primary-color) 90%, transparent),
          color-mix(in srgb, var(--primary-hover) 82%, transparent)
        );
        color: var(--color-text-inverse);
        cursor: pointer;
      }

      .assistant-option-btn {
        width: 30px;
        height: 30px;
        border: 1px solid var(--border-color);
        border-radius: 8px;
        background: color-mix(in srgb, var(--bg-tertiary) 88%, transparent);
        color: var(--text-secondary);
        cursor: pointer;
        font-size: 13px;
        line-height: 1;
      }

      .send-btn:disabled,
      .assistant-option-btn:disabled,
      .chip:disabled,
      .snapshot-card:disabled,
      .suggestion-item:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }

      .shimmer {
        background: linear-gradient(
          90deg,
          color-mix(in srgb, var(--card-bg) 10%, transparent) 25%,
          color-mix(in srgb, var(--card-bg) 18%, transparent) 37%,
          color-mix(in srgb, var(--card-bg) 10%, transparent) 63%
        );
        background-size: 300% 100%;
        animation: shimmer 1.4s infinite;
      }

      .assistant-collapsed-pill {
        display: none;
      }

      @keyframes shimmer {
        0% {
          background-position: 100% 0;
        }
        100% {
          background-position: -100% 0;
        }
      }

      @keyframes typing {
        0%,
        80%,
        100% {
          transform: scale(0.8);
          opacity: 0.7;
        }
        40% {
          transform: scale(1);
          opacity: 1;
        }
      }

      :host-context(.sidebar-host--collapsed) {
        padding: 0;
        flex: 0 0 auto;
      }

      :host-context(.sidebar-host--collapsed) .assistant-panel {
        display: none;
      }

      :host-context(.sidebar-host--collapsed) .assistant-collapsed-pill {
        display: inline-flex;
        width: 100%;
        height: 34px;
        border: 1px solid color-mix(in srgb, var(--primary-color) 40%, transparent);
        border-radius: 8px;
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--primary-color) 72%, transparent),
          color-mix(in srgb, var(--primary-color) 58%, transparent)
        );
        color: var(--color-text-inverse);
        justify-content: center;
        align-items: center;
        font-size: 12px;
        font-weight: 600;
      }
    `,
  ],
})
export class SidebarAiAssistantComponent {
  readonly state = inject(SidebarAiAssistantStateService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly contentScroll = viewChild<ElementRef<HTMLElement>>('contentScroll');
  private stickToBottom = true;

  constructor() {
    effect(
      () => {
        this.state.turns();
        this.state.uiState();
        queueMicrotask(() => this.tryAutoScroll());
      },
      { allowSignalWrites: false },
    );
    this.destroyRef.onDestroy(() => {
      this.stickToBottom = true;
    });
  }

  onClearConversation(event: Event): void {
    event.preventDefault();
    this.state.clearConversation();
  }

  onQuickPrompt(item: SidebarAiSuggestion): void {
    this.state.sendPrompt(item.prompt);
  }

  onSnapshotPrompt(card: SidebarAiSnapshotCard): void {
    this.state.sendPrompt(card.prompt);
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.state.setInput(value);
  }

  onInputKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' || event.shiftKey) {
      return;
    }
    event.preventDefault();
    this.state.sendTypedMessage();
  }

  onScroll(): void {
    const host = this.contentScroll()?.nativeElement;
    if (!host) {
      return;
    }
    this.stickToBottom = this.isNearBottom(host);
  }

  private tryAutoScroll(): void {
    const host = this.contentScroll()?.nativeElement;
    if (!host || !this.stickToBottom) {
      return;
    }
    host.scrollTop = host.scrollHeight;
  }

  private isNearBottom(el: HTMLElement): boolean {
    return el.scrollHeight - el.scrollTop - el.clientHeight < 40;
  }
}
