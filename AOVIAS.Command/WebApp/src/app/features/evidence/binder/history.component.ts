import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import type { OeHistoryEvent } from './models';

@Component({
  selector: 'app-binder-history',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <mat-spinner diameter="28" />
    } @else if (!events() || events()!.length === 0) {
      <p class="empty">No history events.</p>
    } @else {
      <div class="timeline">
        @for (e of events()!; track e.id) {
          <div class="row">
            <div class="dot"></div>
            <div>
              <div class="title">{{ e.title }}</div>
              <div class="meta">{{ e.actor }} · {{ e.timestamp | date: 'medium' }}</div>
            </div>
          </div>
        }
      </div>
    }
  `,
  styles: [
    `
      .timeline {
        display: flex;
        flex-direction: column;
        gap: 14px;
      }
      .row {
        display: flex;
        gap: 12px;
      }
      .dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        background: var(--primary-color, var(--color-primary));
        margin-top: 4px;
        flex-shrink: 0;
      }
      .title {
        font-size: 13px;
        font-weight: 500;
      }
      .meta {
        font-size: 11px;
        color: #6b7280;
      }
      .empty {
        color: #6b7280;
        font-size: 13px;
      }
    `,
  ],
})
export class HistoryComponent {
  readonly events = input<OeHistoryEvent[] | null>(null);
  readonly loading = input(false);
}
