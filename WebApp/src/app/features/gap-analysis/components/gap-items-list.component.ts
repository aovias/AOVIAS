import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GapCategory } from '../models/gap-analysis-api.model';
import { GapItemVm } from '../models/gap-analysis-vm.model';

@Component({
  selector: 'app-gap-items-list',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="gap-list" [attr.aria-labelledby]="headingId">
      <h2 [id]="headingId" class="section-title">{{ title }}</h2>
      @if (items.length === 0) {
        <p class="gap-list__empty">No {{ category | lowercase }} gaps identified.</p>
      } @else {
        <ul class="gap-list__items">
          @for (item of items; track item.nodeId) {
            <li class="gap-item">
              <div class="gap-item__header">
                <span class="gap-item__badge" [attr.data-category]="category">{{ category }}</span>
                <span class="gap-item__code">{{ item.nodeCode }}</span>
                <span class="gap-item__score">Score {{ item.score }}/5</span>
              </div>
              <div class="gap-item__title">{{ item.nodeTitle }}</div>
              @if (item.classification) {
                <span class="gap-item__classification">{{ item.classification }}</span>
              }
            </li>
          }
        </ul>
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

      .gap-list__empty {
        font-size: 14px;
        color: var(--text-secondary);
        margin: 0;
        padding: 16px;
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
      }

      .gap-list__items {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .gap-item {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 16px;
        border-left: 4px solid var(--gap-accent, var(--border-color));
      }

      .gap-item__header {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
        margin-bottom: 8px;
      }

      .gap-item__badge {
        font-size: 11px;
        font-weight: 700;
        text-transform: uppercase;
        padding: 2px 8px;
        border-radius: 4px;
      }

      .gap-item__badge[data-category='Critical'] {
        background: rgba(239, 68, 68, 0.15);
        color: #dc2626;
      }

      .gap-item__badge[data-category='Major'] {
        background: rgba(245, 158, 11, 0.15);
        color: #d97706;
      }

      .gap-item {
        --gap-accent: var(--border-color);
      }

      .gap-item:has(.gap-item__badge[data-category='Critical']) {
        --gap-accent: #dc2626;
      }

      .gap-item:has(.gap-item__badge[data-category='Major']) {
        --gap-accent: #d97706;
      }

      .gap-item__code {
        font-family: monospace;
        font-size: 13px;
        color: var(--text-secondary);
      }

      .gap-item__score {
        margin-left: auto;
        font-size: 12px;
        font-weight: 600;
        color: var(--text-secondary);
      }

      .gap-item__title {
        font-size: 14px;
        font-weight: 500;
        color: var(--text-primary);
      }

      .gap-item__classification {
        display: inline-block;
        margin-top: 8px;
        font-size: 11px;
        padding: 2px 8px;
        border-radius: 4px;
        background: var(--hover-bg, rgba(0, 0, 0, 0.04));
        color: var(--text-secondary);
      }

      @media (max-width: 768px) {
        .gap-item__score {
          margin-left: 0;
          width: 100%;
        }
      }
    `,
  ],
})
export class GapItemsListComponent {
  @Input({ required: true }) title = '';
  @Input({ required: true }) category: GapCategory = 'Critical';
  @Input({ required: true }) items: GapItemVm[] = [];
  @Input() headingId = 'gap-items-list';
}
