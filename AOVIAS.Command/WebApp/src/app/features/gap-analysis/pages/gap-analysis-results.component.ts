import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { GapAnalysisStore } from '../state/gap-analysis.store';
import { GapOverallScoreComponent } from '../components/gap-overall-score.component';
import { GapCategoryKpiStripComponent } from '../components/gap-category-kpi-strip.component';
import { GapChapterScoresComponent } from '../components/gap-chapter-scores.component';
import { GapItemsListComponent } from '../components/gap-items-list.component';
import { GapRecommendationsPanelComponent } from '../components/gap-recommendations-panel.component';
import { GapNextActionsComponent } from '../components/gap-next-actions.component';

@Component({
  selector: 'app-gap-analysis-results',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    GapOverallScoreComponent,
    GapCategoryKpiStripComponent,
    GapChapterScoresComponent,
    GapItemsListComponent,
    GapRecommendationsPanelComponent,
    GapNextActionsComponent,
  ],
  templateUrl: './gap-analysis-results.component.html',
  styleUrl: './gap-analysis-results.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GapAnalysisResultsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  readonly store = inject(GapAnalysisStore);

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const assessmentId = params.get('assessmentId');
      if (assessmentId) {
        this.store.load(assessmentId);
      } else {
        this.store.reset();
        this.store.error.set('Assessment id is required.');
      }
    });
  }

  onRegenerate(): void {
    this.store.regenerate();
  }
}
