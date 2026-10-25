import { Injectable, signal } from '@angular/core';

export type AiPageContext =
  | 'General'
  | 'Dcc'
  | 'ComplianceMaturity'
  | 'AnalyticsHub'
  | 'Journey'
  | 'Evidence'
  | 'ReviewQueue'
  | 'Findings'
  | 'IssuesAndRisks'
  | 'PatientSafety'
  | 'QualityIntelligence'
  | 'ComplianceGraph'
  | 'Copilot';

export interface AiPageContextState {
  pageContext: AiPageContext;
  entityType?: string | null;
  entityId?: string | null;
  label?: string | null;
}

/** Sprint 12C — page-aware and entity-scoped context for the AI assistant. */
@Injectable({ providedIn: 'root' })
export class AiPageContextService {
  private readonly state = signal<AiPageContextState>({ pageContext: 'General' });
  private readonly recentEntityRefs = signal<string[]>([]);

  readonly snapshot = this.state.asReadonly();

  setPageContext(ctx: AiPageContextState): void {
    this.state.set({
      pageContext: ctx.pageContext,
      entityType: ctx.entityType ?? null,
      entityId: ctx.entityId ?? null,
      label: ctx.label ?? null,
    });

    if (ctx.entityType && ctx.entityId) {
      this.trackEntityRef(`${ctx.entityType}:${ctx.entityId}`);
    }
  }

  clearPageContext(): void {
    this.state.set({ pageContext: 'General' });
  }

  trackEntityRef(ref: string): void {
    const clean = ref.trim();
    if (!clean) return;
    this.recentEntityRefs.update((items) =>
      [clean, ...items.filter((x) => x !== clean)].slice(0, 8),
    );
  }

  getRecentEntityRefs(): string[] {
    return this.recentEntityRefs();
  }

  buildConversationSummary(turns: { prompt: string; responseText?: string }[]): string {
    const recent = turns.slice(-3);
    if (recent.length === 0) return '';
    return recent
      .map((t) => {
        const answer = (t.responseText ?? '').trim().slice(0, 120);
        return `Q: ${t.prompt.trim().slice(0, 80)}${answer ? ` → A: ${answer}` : ''}`;
      })
      .join(' | ');
  }
}
