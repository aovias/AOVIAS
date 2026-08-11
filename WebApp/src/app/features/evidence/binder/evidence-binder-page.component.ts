import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  inject,
  OnInit,
  DestroyRef,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { EvidenceBinderStore } from './evidence-binder.store';
import { SummaryCardsComponent } from './summary-cards.component';
import { ChapterSidebarComponent } from './chapter-sidebar.component';
import { ObjectiveListComponent } from './objective-list.component';
import { EvidenceDrawerComponent } from './evidence-drawer.component';
import type { BinderTab } from './models';

@Component({
  selector: 'app-evidence-binder-page',
  standalone: true,
  imports: [CommonModule, SummaryCardsComponent, ChapterSidebarComponent, ObjectiveListComponent, EvidenceDrawerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="header">
        <div>
          <h1>Evidence Binder</h1>
          <p class="desc">Compliance readiness by framework chapter and objective element.</p>
        </div>
      </header>

      @if (!store.frameworkId()) {
        <div class="banner">Select a hospital framework in context to load the binder.</div>
      }

      <app-binder-summary-cards [cards]="store.readinessCards()" [loading]="store.summaryLoading()" />

      <div class="workspace" [class.no-framework]="!store.frameworkId()">
        <app-binder-chapter-sidebar
          [chapters]="store.chapters()"
          [selectedChapterId]="store.selectedChapterId()"
          (select)="store.selectChapter($event)"
        />
        <app-binder-objective-list
          [chapter]="store.selectedChapter()"
          [objectives]="store.objectives()"
          [selectedObjectiveId]="store.selectedObjectiveElementId()"
          [loading]="store.objectivesLoading()"
          (pick)="store.selectObjective($event)"
        />
      </div>

      <app-binder-evidence-drawer />
    </div>
  `,
  styles: [
    `
      .page {
        min-height: 100vh;
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        background: var(--bg-primary, #f3f4f6);
        color: var(--text-primary, #111827);
      }
      .header h1 {
        margin: 0 0 8px;
        font-size: var(--font-size-page-title);
      }
      .desc {
        margin: 0;
        color: var(--text-secondary, #6b7280);
        font-size: 14px;
      }
      .banner {
        padding: 12px 16px;
        background: #fef3c7;
        color: #92400e;
        border-radius: 8px;
        margin-bottom: 16px;
      }
      .workspace {
        display: grid;
        grid-template-columns: 300px 1fr;
        gap: 20px;
        align-items: start;
      }
      .workspace.no-framework {
        opacity: 0.5;
        pointer-events: none;
      }
      @media (max-width: 960px) {
        .workspace {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class EvidenceBinderPageComponent implements OnInit {
  readonly store = inject(EvidenceBinderStore);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.store.initFromContext();
    this.store.loadSummaryAndChapters();

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((q) => {
      this.store.applyRouteParams({
        chapterId: q.get('chapterId'),
        objectiveElementId: q.get('objectiveElementId'),
        tab: q.get('tab') as BinderTab | null,
      });
    });
  }

  @HostListener('window:resize')
  onResize(): void {
    this.store.refreshDrawerMode();
  }
}
