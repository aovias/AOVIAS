import {
  Component, input, output, computed, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import type { AiDraft } from '../../models/ai-draft.model';
import { DraftStatus, getDraftTypeLabel } from '../../models/ai-draft.model';

@Component({
  selector: 'app-draft-version-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './draft-version-history.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DraftVersionHistoryComponent {
  readonly versions = input.required<AiDraft[]>();
  readonly activeVersionId = input<string | null>(null);

  readonly selectVersion = output<AiDraft>();
  readonly close = output<void>();

  // ── Computed ────────────────────────────────────────────────────────────────

  readonly sortedVersions = computed(() =>
    [...this.versions()].sort((a, b) => b.version - a.version),
  );

  readonly draftTitle = computed(() =>
    this.versions()[0]?.title ?? 'Draft',
  );

  readonly typeLabel = computed(() => {
    const first = this.versions()[0];
    return first ? getDraftTypeLabel(first.draftTypeEnum) : 'Draft';
  });

  readonly latestVersion = computed(() =>
    Math.max(...this.versions().map(v => v.version), 0),
  );

  isActive(version: AiDraft): boolean {
    return version.id === this.activeVersionId();
  }

  isLatest(version: AiDraft): boolean {
    return version.version === this.latestVersion();
  }

  formatDate(utc: string): string {
    return new Date(utc).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  formatConfidence(confidence: number): number {
    return Math.round(confidence * 100);
  }

  statusClass(status: DraftStatus): string {
    switch (status) {
      case DraftStatus.Draft:    return 'status-draft';
      case DraftStatus.Reviewed: return 'status-reviewed';
      case DraftStatus.Approved: return 'status-approved';
      case DraftStatus.Archived: return 'status-archived';
      default:                   return 'status-draft';
    }
  }

  onSelect(version: AiDraft): void { this.selectVersion.emit(version); }
  onClose(): void                  { this.close.emit(); }
}
