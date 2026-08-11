import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ChapterNavigationComponent } from '../../components/chapter-navigation/chapter-navigation.component';
import { StandardsPanelComponent } from '../../components/standards-panel/standards-panel.component';
import {
  ExecutionChapterItemVm,
  ExecutionOeRowVm,
  ExecutionSortMode,
} from '../../models/journey-execution-vm.model';
import {
  StandardModel,
  standardModelFromExecutionStandardVm,
} from '../../components/standard-card/standard-card.model';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { ObjectiveElement } from '../../models/objective-element.model';

@Component({
  selector: 'app-journey-execution',
  standalone: true,
  imports: [CommonModule, RouterModule, ChapterNavigationComponent, StandardsPanelComponent],
  providers: [JourneyWorkspaceStore],
  templateUrl: './journey-execution.component.html',
  styleUrl: './journey-execution.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
/**
 * @deprecated Legacy execution container kept only for rollback safety.
 * The canonical route `/app/journey/execution` is served by `ExecutionScreenComponent`.
 */
export class JourneyExecutionComponent {
  readonly searchTerm = signal('');
  readonly sortMode = signal<ExecutionSortMode>('code');
  readonly expandedIds = signal<ReadonlySet<string>>(new Set());
  readonly selectedOeId = signal<string | null>(null);
  readonly store = inject(JourneyWorkspaceStore);

  readonly chapterItems = computed<ExecutionChapterItemVm[]>(() => {
    const chapters = this.store.snapshot()?.chapters ?? [];
    const filtered = chapters.filter((chapter) => {
      const query = this.searchTerm().trim().toLowerCase();
      if (!query) return true;
      return (
        chapter.code.toLowerCase().includes(query) || chapter.name.toLowerCase().includes(query)
      );
    });
    return [...filtered]
      .sort((left, right) => {
        if (this.sortMode() === 'code') return left.code.localeCompare(right.code);
        const ranking = { high: 0, medium: 1, low: 2 };
        return ranking[left.risk] - ranking[right.risk];
      })
      .map((chapter) => ({
        id: chapter.id,
        code: chapter.code,
        title: chapter.name,
        risk: chapter.risk,
        objectiveCount: chapter.standards.reduce(
          (count, standard) => count + standard.objectiveElements.length,
          0,
        ),
        progressPercent: chapter.complianceScorePercent,
      }));
  });

  readonly standardItems = computed<StandardModel[]>(() => {
    return this.store.standardsForSelectedChapter().map((standard) =>
      standardModelFromExecutionStandardVm({
        id: standard.id,
        code: standard.code,
        title: standard.title,
        description: standard.description,
        compliancePercent: standard.completionPercent,
        classification: standard.riskBadge,
        openIssues: standard.objectiveElements.filter((oe) => oe.auditStatus === 'non_compliant')
          .length,
        objectiveElements: standard.objectiveElements.map((oe) => this.toRow(oe)),
      }),
    );
  });

  readonly openIssues = computed(() =>
    this.standardItems().reduce((count, standard) => count + (standard.openIssuesCount ?? 0), 0),
  );

  constructor() {
    this.store.initFromRoute();
  }

  onChapterSelected(chapterId: string): void {
    this.store.selectChapter(chapterId);
    this.expandedIds.set(new Set());
    this.selectedOeId.set(null);
  }

  onStandardToggle(standardId: string): void {
    this.expandedIds.update((current) => {
      const next = new Set(current);
      if (next.has(standardId)) next.delete(standardId);
      else next.add(standardId);
      return next;
    });
  }

  onOeView(oeId: string): void {
    this.selectedOeId.set(oeId);
  }

  private toRow(oe: ObjectiveElement): ExecutionOeRowVm {
    const statusMap = {
      compliant: 'compliant',
      non_compliant: 'non-compliant',
      not_applicable: 'partially-compliant',
    } as const;
    return {
      id: oe.id,
      code: oe.code,
      title: oe.description,
      classification: oe.classification,
      scoreLabel: oe.scoreLabel,
      status: statusMap[oe.auditStatus],
      evidenceCount: oe.evidence.length,
    };
  }
}
