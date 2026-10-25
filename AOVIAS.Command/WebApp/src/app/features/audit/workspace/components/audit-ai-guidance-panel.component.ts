import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, finalize, of } from 'rxjs';
import { CopilotApiService } from '../../../copilot/services/copilot-api.service';
import type { CopilotRecommendation } from '../../../copilot/models/copilot-recommendation.model';
import { AuditWorkspaceParticipationService } from '../services/audit-workspace-participation.service';

/**
 * AI guidance — Phase 3, Step 9.
 *
 * Lives inside the context panel rather than on its own page, which is the
 * whole requirement: guidance about the current record should not cost a
 * navigation away from that record.
 *
 * Reuses the existing {@link CopilotApiService} and its recommendation
 * contract — Phase 3 adds no AI backend, it surfaces the one already there in
 * the place the user is working (Step 16: integrate existing assets).
 *
 * Recommendations are re-fetched when the panel's subject changes, so guidance
 * always describes what is on screen. A failed fetch degrades to a quiet
 * message: AI guidance is advisory, and an error banner in a side panel would
 * imply the user's actual audit work had failed.
 */
@Component({
  selector: 'app-audit-ai-guidance-panel',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="ai-section">
      <header class="ai-header">
        <h3>AI guidance</h3>
        <button type="button" class="refresh" (click)="reload()" [disabled]="loading()">
          {{ loading() ? 'Thinking…' : 'Refresh' }}
        </button>
      </header>

      @if (scopeLabel(); as scope) {
        <p class="ai-scope">{{ scope }}</p>
      }

      @if (loading()) {
        <p class="muted">Reading the current context…</p>
      } @else if (failed()) {
        <p class="muted">Guidance is unavailable right now.</p>
      } @else if (recommendations().length === 0) {
        <p class="muted">No suggestions for this context.</p>
      } @else {
        <ul class="ai-list">
          @for (rec of recommendations(); track rec.id) {
            <li class="ai-item" [class]="'sev-' + (rec.severity || '').toLowerCase()">
              <p class="ai-title">{{ rec.title }}</p>
              @if (rec.description) {
                <p class="ai-summary">{{ rec.description }}</p>
              }
              <p class="ai-meta">{{ rec.category }} · {{ rec.severity }}</p>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: [
    `
      .ai-section {
        padding: var(--space-3);
        border-block-start: var(--border-width-hairline) solid var(--border-color);
      }

      .ai-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-2);
        margin-block-end: var(--space-2);
      }

      h3 {
        margin: 0;
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-semibold);
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--text-secondary);
      }

      .refresh {
        border: none;
        background: none;
        padding: 0;
        color: var(--primary-color);
        font-size: var(--font-size-micro);
        font-weight: var(--font-weight-medium);
        cursor: pointer;
      }

      .refresh:disabled {
        color: var(--text-muted);
        cursor: default;
      }

      .muted {
        margin: 0;
        color: var(--text-muted);
        font-size: var(--font-size-caption);
      }

      .ai-scope {
        margin: 0 0 var(--space-2);
        color: var(--text-muted);
        font-size: var(--font-size-micro);
      }

      .ai-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .ai-item {
        padding: var(--space-2);
        border-radius: var(--radius-control);
        background: var(--bg-tertiary);
        border-inline-start: 3px solid var(--border-color);
      }

      .ai-item.sev-high,
      .ai-item.sev-critical {
        border-inline-start-color: var(--color-status-danger);
      }

      .ai-item.sev-medium {
        border-inline-start-color: var(--color-status-warning);
      }

      .ai-title {
        margin: 0;
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-semibold);
        color: var(--text-primary);
      }

      .ai-summary {
        margin: var(--space-1) 0 0;
        font-size: var(--font-size-micro);
        color: var(--text-secondary);
      }

      .ai-meta {
        margin: var(--space-1) 0 0;
        font-size: var(--font-size-micro);
        color: var(--text-muted);
      }
    `,
  ],
})
export class AuditAiGuidancePanelComponent {
  private readonly copilot = inject(CopilotApiService);
  private readonly participation = inject(AuditWorkspaceParticipationService);

  protected readonly recommendations = signal<readonly CopilotRecommendation[]>([]);
  protected readonly loading = signal(false);
  protected readonly failed = signal(false);

  constructor() {
    // Re-read whenever the declared context changes so guidance tracks the
    // record on screen rather than whatever was open when the panel mounted.
    effect(() => {
      const ctx = this.participation.aiContext();
      this.fetch(ctx.pageContext);
    });
  }

  protected reload(): void {
    this.fetch(this.participation.aiContext().pageContext);
  }

  /**
   * Scope line under the header — shows the user which context the guidance was
   * produced against, so generic-looking output is attributable rather than
   * mysterious.
   */
  protected readonly scopeLabel = computed(() => {
    const ctx = this.participation.aiContext();
    const parts = [ctx.department, ctx.framework, ctx.statusLabel].filter((p): p is string => !!p);
    return parts.length ? parts.join(' · ') : null;
  });

  private fetch(pageContext: string): void {
    this.loading.set(true);
    this.failed.set(false);
    this.copilot
      .getRecommendationsByPersona(pageContext, 4)
      .pipe(
        catchError(() => {
          this.failed.set(true);
          return of([] as CopilotRecommendation[]);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((recs) => this.recommendations.set(recs ?? []));
  }
}
