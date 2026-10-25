import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GapNextActionVm } from '../models/gap-analysis-vm.model';

@Component({
  selector: 'app-gap-next-actions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="gap-next" aria-labelledby="gap-next-title">
      <h2 id="gap-next-title" class="section-title">Next Actions</h2>
      <ol class="gap-next__list">
        @for (action of actions; track action.order) {
          <li class="gap-next__item">
            <span class="gap-next__order">{{ action.order }}</span>
            <a [routerLink]="action.route" class="gap-next__link">{{ action.label }}</a>
          </li>
        }
      </ol>
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

      .gap-next {
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        padding: 20px 24px;
      }

      .gap-next__list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .gap-next__item {
        display: flex;
        align-items: flex-start;
        gap: 12px;
      }

      .gap-next__order {
        flex-shrink: 0;
        width: 28px;
        height: 28px;
        border-radius: 50%;
        background: var(--primary-color, var(--color-primary));
        color: #fff;
        font-size: 13px;
        font-weight: 700;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .gap-next__link {
        font-size: 14px;
        font-weight: 500;
        color: var(--primary-color, var(--color-primary));
        text-decoration: none;
        line-height: 1.5;
        padding-top: 4px;
      }

      .gap-next__link:hover {
        text-decoration: underline;
      }
    `,
  ],
})
export class GapNextActionsComponent {
  @Input({ required: true }) actions: GapNextActionVm[] = [];
}
