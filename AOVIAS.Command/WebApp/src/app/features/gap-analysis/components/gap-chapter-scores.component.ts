import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccreditationProgressCardComponent } from '../../../shared/ui/accreditation-progress-card/accreditation-progress-card.component';
import { GapChapterScoreVm } from '../models/gap-analysis-vm.model';

@Component({
  selector: 'app-gap-chapter-scores',
  standalone: true,
  imports: [CommonModule, AccreditationProgressCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="gap-chapters" aria-labelledby="gap-chapters-title">
      <h2 id="gap-chapters-title" class="section-title">Chapter Scores</h2>
      @if (chapters.length === 0) {
        <p class="gap-chapters__empty">No chapter-level scores available for this assessment.</p>
      } @else {
        <div class="gap-chapters__grid">
          @for (chapter of chapters; track chapter.chapterId) {
            <app-accreditation-progress-card
              [title]="chapter.chapterName"
              [subtitle]="'Average OE score'"
              [progress]="chapter.percent" />
          }
        </div>
      }
    </section>
  `,
  styles: [
    `
      .section-title {
        font-size: 18px;
        font-weight: 600;
        color: var(--text-primary);
        margin: 0 0 16px;
      }

      .gap-chapters__grid {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 16px;
      }

      .gap-chapters__empty {
        font-size: 14px;
        color: var(--text-secondary);
        margin: 0;
        padding: 16px;
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
      }

      @media (max-width: 1199px) {
        .gap-chapters__grid {
          grid-template-columns: repeat(2, 1fr);
        }
      }

      @media (max-width: 768px) {
        .gap-chapters__grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class GapChapterScoresComponent {
  @Input({ required: true }) chapters: GapChapterScoreVm[] = [];
}
