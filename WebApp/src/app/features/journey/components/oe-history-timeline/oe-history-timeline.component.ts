import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OeHistoryEvent } from '../../models/objective-element.model';

@Component({
  selector: 'app-oe-history-timeline',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!sorted().length) {
      <p class="empty">No history yet.</p>
    } @else {
    <ol class="tl">
      @for (ev of sorted(); track ev.id) {
        <li>
          <div class="dot" [attr.data-type]="ev.type"></div>
          <div class="body">
            <div class="title">{{ ev.title }}</div>
            <div class="desc">{{ ev.description }}</div>
            <div class="meta">{{ ev.timestamp }} · {{ ev.actor }}</div>
          </div>
        </li>
      }
    </ol>
    }
  `,
  styles: `
    .tl {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
    }
    li {
      display: grid;
      grid-template-columns: var(--space-3) 1fr;
      gap: var(--space-3);
    }
    .dot {
      width: 0.625rem;
      height: 0.625rem;
      border-radius: 50%;
      margin-top: var(--space-1);
      background: var(--primary-color);
    }
    .title {
      font-weight: var(--font-weight-bold);
      font-size: var(--font-size-caption);
      color: var(--text-primary);
    }
    .desc {
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
    }
    .meta {
      font-size: 0.6875rem;
      color: var(--text-muted);
      margin-top: var(--space-1);
    }
    .empty {
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
      margin: 0;
    }
  `,
})
export class OeHistoryTimelineComponent {
  readonly events = input<OeHistoryEvent[]>([]);

  sorted(): OeHistoryEvent[] {
    return [...this.events()].sort((a, b) => b.timestamp.localeCompare(a.timestamp));
  }
}
