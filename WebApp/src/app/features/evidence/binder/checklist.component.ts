import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import type { EvidenceBinderChecklistItem } from './models';

@Component({
  selector: 'app-binder-checklist',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <mat-spinner diameter="28" />
    } @else if (!items() || items()!.length === 0) {
      <p class="empty">No checklist items configured for this objective.</p>
    } @else {
      <ul class="list">
        @for (it of items()!; track it.id) {
          <li [class.done]="it.isCompleted">{{ it.item }}</li>
        }
      </ul>
    }
  `,
  styles: [
    `
      .list {
        list-style: none;
        padding: 0;
        margin: 0;
      }
      li {
        padding: 10px 12px;
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        margin-bottom: 8px;
        font-size: 13px;
      }
      li.done {
        border-left: 3px solid #22c55e;
      }
      .empty {
        color: #6b7280;
        font-size: 13px;
      }
    `,
  ],
})
export class ChecklistComponent {
  readonly items = input<EvidenceBinderChecklistItem[] | null>(null);
  readonly loading = input(false);
}
