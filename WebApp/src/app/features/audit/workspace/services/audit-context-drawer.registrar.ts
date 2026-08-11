import { Injectable, effect, inject } from '@angular/core';
import { ContextDrawerService } from '../../../../shared/ui/context-drawer/context-drawer.service';
import type { ContextDrawerSection } from '../../../../shared/ui/context-drawer/context-drawer.model';
import { AuditWorkspaceParticipationService } from './audit-workspace-participation.service';

/**
 * Publishes Audit's drawer sections — Stage 1.2.
 *
 * The bridge between Audit's participation contract and the platform drawer.
 * Keeping it separate is what lets the drawer stay module-agnostic: Audit
 * decides which sections exist and what they are called, the drawer only
 * renders them.
 *
 * Sections are declared with dynamic imports, so opening the workspace does not
 * pull in any section's code. A user who never opens Documents never downloads
 * it (Step 10).
 *
 * Sections whose data is empty are omitted rather than shown empty — Step 5 is
 * explicit that no empty placeholders should appear. AI guidance and Summary
 * are always offered because both are meaningful with no records: guidance
 * still has the page context, and Summary carries lifecycle status.
 */
@Injectable({ providedIn: 'root' })
export class AuditContextDrawerRegistrar {
  private readonly participation = inject(AuditWorkspaceParticipationService);
  private readonly drawer = inject(ContextDrawerService);

  constructor() {
    effect(() => {
      const records = this.participation.relatedRecords();
      const documents = this.participation.documents();
      const timeline = this.participation.timeline();

      const sections: ContextDrawerSection[] = [
        {
          id: 'ai',
          label: 'AI guidance',
          glyph: '✦',
          load: () =>
            import('../components/audit-ai-guidance-panel.component').then(
              (m) => m.AuditAiGuidancePanelComponent,
            ),
        },
        {
          id: 'summary',
          label: 'Context',
          glyph: '◈',
          load: () =>
            import('../components/sections/audit-context-sections').then(
              (m) => m.AuditContextSummarySectionComponent,
            ),
        },
      ];

      if (records.length) {
        sections.push({
          id: 'related',
          label: 'Related',
          glyph: '⧉',
          badge: records.length,
          load: () =>
            import('../components/sections/audit-context-sections').then(
              (m) => m.AuditRelatedRecordsSectionComponent,
            ),
        });
      }

      if (documents.length) {
        sections.push({
          id: 'documents',
          label: 'Documents',
          glyph: '▤',
          badge: documents.length,
          load: () =>
            import('../components/sections/audit-context-sections').then(
              (m) => m.AuditDocumentsSectionComponent,
            ),
        });
      }

      if (timeline.length) {
        sections.push({
          id: 'timeline',
          label: 'Activity',
          glyph: '◷',
          badge: timeline.length,
          load: () =>
            import('../components/sections/audit-context-sections').then(
              (m) => m.AuditTimelineSectionComponent,
            ),
        });
      }

      this.drawer.setSections(sections);
    });
  }
}
