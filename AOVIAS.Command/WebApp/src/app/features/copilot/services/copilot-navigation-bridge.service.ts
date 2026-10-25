import { Injectable, inject, DestroyRef } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CopilotStore } from '../state/copilot.store';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { CopilotPersona } from '../models/copilot-persona.model';
import { PERSONA_OVERRIDE_SESSION_KEY } from '../models/persona-experience.model';

/** Maps shell routes to persona page-context when feature pages omit AiPageContext. */
function pageContextFromUrl(url: string): string {
  const path = url.split(/[?#]/)[0].replace(/\/+$/, '') || '/';
  if (path.includes('/compliance-maturity')) return 'ComplianceMaturity';
  if (path.includes('/analytics')) return 'AnalyticsHub';
  if (path.includes('/ccc') || path.includes('/control-center')) return 'Dcc';
  if (path.includes('/audit/findings')) return 'Findings';
  // `capa` is the canonical route; `corrective-actions` is the preserved legacy
  // alias, which a user can still land on from a bookmark before the redirect
  // resolves. Both must map to the same assistant context.
  if (path.includes('/audit/capa') || path.includes('/audit/corrective-actions')) {
    return 'IssuesAndRisks';
  }
  if (path.includes('/quality-intelligence')) return 'QualityIntelligence';
  if (path.includes('/compliance-graph')) return 'ComplianceGraph';
  if (path.includes('/patient-safety')) return 'PatientSafety';
  if (path.includes('/evidence')) return 'Evidence';
  if (path.includes('/journey')) return 'Journey';
  if (path.includes('/copilot')) return 'Copilot';
  if (path.includes('/dashboard')) return 'Dashboard';
  return 'General';
}

/**
 * Sprint 13E — wires navigation + session persona override to CopilotStore.
 * Provided in root; instantiated when Shell loads Copilot workspace or on first inject.
 */
@Injectable({ providedIn: 'root' })
export class CopilotNavigationBridgeService {
  private readonly router = inject(Router);
  private readonly store = inject(CopilotStore);
  private readonly pageContext = inject(AiPageContextService);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    this.restoreSessionOverride();
    this.resolveFromCurrentRoute();

    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.resolveFromCurrentRoute());
  }

  private restoreSessionOverride(): void {
    try {
      const saved = sessionStorage.getItem(PERSONA_OVERRIDE_SESSION_KEY);
      if (saved && Object.values(CopilotPersona).includes(saved as CopilotPersona)) {
        this.store.applySessionOverride(saved as CopilotPersona);
      }
    } catch {
      /* sessionStorage unavailable */
    }
  }

  private resolveFromCurrentRoute(): void {
    const page = this.pageContext.snapshot();
    const pageContext =
      page.pageContext !== 'General' ? page.pageContext : pageContextFromUrl(this.router.url);
    this.store.resolvePersona(
      pageContext,
      page.entityType ?? undefined,
      page.entityId ?? undefined,
    );
  }
}
