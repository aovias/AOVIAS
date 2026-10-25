import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { EvidenceBinderObjectiveElement } from './models';

@Component({
  selector: 'app-binder-objective-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="card"
      [class.selected]="selected()"
      (click)="pick.emit(objective().objectiveElementId)"
    >
      <div class="head">
        <div>
          <span class="code">{{ objective().code }}</span>
          <div class="title">{{ objective().title }}</div>
        </div>
        <span class="icon" [attr.data-status]="objective().status">{{
          objective().status === 'approved' ? '✔' : objective().status === 'partial' ? '⚠' : '✕'
        }}</span>
      </div>
      <p class="desc">{{ objective().description }}</p>
      <div class="foot">
        <span>{{ objective().evidenceCount }} evidence</span>
        <span>{{ objective().taskCount }} tasks</span>
      </div>
    </button>
  `,
  styles: [
    `
      .card {
        width: 100%;
        text-align: left;
        padding: 14px;
        border-radius: 10px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--bg-secondary, #f9fafb);
        cursor: pointer;
        margin-bottom: 10px;
      }
      .card.selected {
        border-color: var(--primary-color, var(--color-primary));
        background: var(--primary-bg-light, var(--color-primary-50));
      }
      .head {
        display: flex;
        justify-content: space-between;
        gap: 8px;
      }
      .code {
        font-size: 11px;
        font-weight: 700;
        color: var(--primary-color, var(--color-primary));
      }
      .title {
        font-size: 14px;
        font-weight: 600;
        margin-top: 4px;
      }
      .icon {
        font-size: 18px;
        flex-shrink: 0;
      }
      .icon[data-status='approved'] {
        color: #22c55e;
      }
      .icon[data-status='partial'] {
        color: #eab308;
      }
      .icon[data-status='missing'] {
        color: #ef4444;
      }
      .desc {
        font-size: 12px;
        color: #6b7280;
        margin: 8px 0;
        line-height: 1.4;
      }
      .foot {
        display: flex;
        gap: 16px;
        font-size: 11px;
        color: #6b7280;
      }
    `,
  ],
})
export class ObjectiveCardComponent {
  readonly objective = input.required<EvidenceBinderObjectiveElement>();
  readonly selected = input(false);
  readonly pick = output<string>();
}
