import {
  Component, input, output, computed, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import type { AiDraft } from '../../models/ai-draft.model';
import {
  DraftStatus, getDraftTypeLabel, parseDraftSourceIntelligence,
} from '../../models/ai-draft.model';

@Component({
  selector: 'app-draft-viewer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './draft-viewer.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DraftViewerComponent {
  readonly draft = input.required<AiDraft>();
  readonly isGenerating = input<boolean>(false);

  readonly close = output<void>();
  readonly regenerate = output<AiDraft>();
  readonly viewVersions = output<AiDraft>();

  // ── Computed ────────────────────────────────────────────────────────────────

  readonly typeLabel = computed(() => getDraftTypeLabel(this.draft().draftTypeEnum));

  readonly isArchived   = computed(() => this.draft().status === DraftStatus.Archived);
  readonly isApproved   = computed(() => this.draft().status === DraftStatus.Approved);

  readonly formattedDate = computed(() =>
    new Date(this.draft().generatedAtUtc).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    }) + ' UTC',
  );

  readonly confidencePercent = computed(() =>
    Math.round(this.draft().confidence * 100),
  );

  /** Parsed source intelligence — reveals provenance metadata. */
  readonly sourceIntelligence = computed(() =>
    parseDraftSourceIntelligence(this.draft()),
  );

  /** Sections parsed from markdown content (splits on ## headings). */
  readonly sections = computed(() => {
    const content = this.draft().content;
    if (!content) return [];

    return content
      .split(/^## /m)
      .filter(s => s.trim())
      .map(s => {
        const lineEnd = s.indexOf('\n');
        const heading  = lineEnd > -1 ? s.substring(0, lineEnd).trim() : s.trim();
        const body     = lineEnd > -1 ? s.substring(lineEnd + 1).trim() : '';
        return { heading, body };
      })
      .filter(s => !s.heading.startsWith('<!-- SOURCE_INTELLIGENCE'));
  });

  onClose():      void { this.close.emit(); }
  onRegenerate(): void { this.regenerate.emit(this.draft()); }
  onVersions():   void { this.viewVersions.emit(this.draft()); }
}
