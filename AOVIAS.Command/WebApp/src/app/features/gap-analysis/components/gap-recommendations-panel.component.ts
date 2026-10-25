import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GapRecommendationVm } from '../models/gap-analysis-vm.model';

@Component({
  selector: 'app-gap-recommendations-panel',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="gap-rec" aria-labelledby="gap-rec-title">
      <h2 id="gap-rec-title" class="section-title">Recommendations</h2>
      @if (recommendations.length === 0) {
        <p class="gap-rec__empty">No remediation recommendations for this assessment.</p>
      } @else {
        <div class="gap-rec__list">
          @for (rec of recommendations; track rec.nodeId) {
            <article class="gap-rec__item">
              <header class="gap-rec__header">
                <span class="gap-rec__badge" [attr.data-category]="rec.category">{{ rec.category }}</span>
                <span class="gap-rec__code">{{ rec.nodeCode }}</span>
              </header>
              <h3 class="gap-rec__title">{{ rec.nodeTitle }}</h3>
              <p class="gap-rec__text">{{ rec.recommendation }}</p>
            </article>
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

      .gap-rec__empty {
        font-size: 14px;
        color: var(--text-secondary);
        margin: 0;
        padding: 16px;
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
      }

      .gap-rec__list {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .gap-rec__item {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
      }

      .gap-rec__header {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 8px;
      }

      .gap-rec__badge {
        font-size: 11px;
        font-weight: 700;
        text-transform: uppercase;
        padding: 2px 8px;
        border-radius: 4px;
      }

      .gap-rec__badge[data-category='Critical'] {
        background: rgba(239, 68, 68, 0.15);
        color: #dc2626;
      }

      .gap-rec__badge[data-category='Major'] {
        background: rgba(245, 158, 11, 0.15);
        color: #d97706;
      }

      .gap-rec__badge[data-category='Minor'] {
        background: color-mix(in srgb, var(--color-primary) 12%, transparent);
        color: var(--color-primary);
      }

      .gap-rec__code {
        font-family: monospace;
        font-size: 12px;
        color: var(--text-secondary);
      }

      .gap-rec__title {
        font-size: 15px;
        font-weight: 600;
        margin: 0 0 8px;
        color: var(--text-primary);
      }

      .gap-rec__text {
        font-size: 14px;
        line-height: 1.5;
        margin: 0;
        color: var(--text-secondary);
      }
    `,
  ],
})
export class GapRecommendationsPanelComponent {
  @Input({ required: true }) recommendations: GapRecommendationVm[] = [];
}
