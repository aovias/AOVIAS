import {
  Component, input, output, computed, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import type { AiDraft } from '../../models/ai-draft.model';
import { DraftType, DraftStatus, getDraftTypeLabel } from '../../models/ai-draft.model';

@Component({
  selector: 'app-draft-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './draft-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DraftCardComponent {
  readonly draft = input.required<AiDraft>();
  readonly isSelected = input<boolean>(false);

  readonly select = output<AiDraft>();
  readonly regenerate = output<AiDraft>();
  readonly viewVersions = output<AiDraft>();

  // ── Computed ────────────────────────────────────────────────────────────────

  readonly typeLabel = computed(() => getDraftTypeLabel(this.draft().draftTypeEnum));

  readonly statusClass = computed(() => {
    switch (this.draft().status) {
      case DraftStatus.Draft:    return 'status-draft';
      case DraftStatus.Reviewed: return 'status-reviewed';
      case DraftStatus.Approved: return 'status-approved';
      case DraftStatus.Archived: return 'status-archived';
      default:                   return 'status-draft';
    }
  });

  readonly typeClass = computed(() => {
    switch (this.draft().draftTypeEnum) {
      case DraftType.CapaDraft:                   return 'type-capa';
      case DraftType.ExecutiveComplianceBriefing:  return 'type-executive';
      case DraftType.ReadinessSummary:             return 'type-readiness';
      case DraftType.EvidenceChecklist:            return 'type-evidence';
      default:                                     return 'type-default';
    }
  });

  readonly confidencePercent = computed(() =>
    Math.round(this.draft().confidence * 100),
  );

  readonly formattedDate = computed(() =>
    new Date(this.draft().generatedAtUtc).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
    }),
  );

  readonly isArchived = computed(() => this.draft().status === DraftStatus.Archived);
  readonly isReadOnly = computed(() => this.isArchived());

  onSelect(): void    { this.select.emit(this.draft()); }
  onRegenerate(): void { this.regenerate.emit(this.draft()); }
  onVersions(): void  { this.viewVersions.emit(this.draft()); }
}
