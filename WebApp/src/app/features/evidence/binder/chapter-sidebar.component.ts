import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { EvidenceBinderChapter } from './models';

@Component({
  selector: 'app-binder-chapter-sidebar',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="sidebar">
      <div class="sidebar-header"><h2>Framework Chapters</h2></div>
      <div class="chapter-list">
        @for (ch of chapters(); track ch.chapterId) {
          <button
            type="button"
            class="chapter-card"
            [class.selected]="selectedChapterId() === ch.chapterId"
            (click)="select.emit(ch.chapterId)"
          >
            <div class="row">
              <span class="code">{{ ch.code }}</span>
              <span class="dot" [attr.data-risk]="ch.riskLevel"></span>
            </div>
            <div class="name">{{ ch.name }}</div>
            <div class="progress">
              <div class="bar"><div class="fill" [style.width.%]="ch.coveragePercent"></div></div>
              <span class="pct">{{ ch.coveragePercent }}% coverage</span>
            </div>
            <div class="meta">{{ ch.objectivesWithEvidence }}/{{ ch.objectiveCount }} objectives</div>
          </button>
        } @empty {
          <p class="empty">No chapters</p>
        }
      </div>
    </div>
  `,
  styles: [
    `
      .sidebar {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 12px;
        display: flex;
        flex-direction: column;
      }
      .sidebar-header {
        padding: 16px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }
      .sidebar-header h2 {
        margin: 0;
        font-size: var(--font-size-section-title);
      }
      .chapter-list {
        padding: 12px;
      }
      .chapter-card {
        width: 100%;
        text-align: left;
        padding: 14px;
        margin-bottom: 10px;
        border-radius: 10px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--bg-secondary, #f9fafb);
        cursor: pointer;
      }
      .chapter-card.selected {
        border-color: var(--primary-color, var(--color-primary));
        background: var(--primary-bg-light, var(--color-primary-50));
      }
      .row {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
      .code {
        font-weight: 700;
        font-size: 13px;
        color: var(--primary-color, var(--color-primary));
      }
      .dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
      }
      .dot[data-risk='low'] {
        background: #22c55e;
      }
      .dot[data-risk='medium'] {
        background: #eab308;
      }
      .dot[data-risk='high'] {
        background: #ef4444;
      }
      .name {
        font-size: 12px;
        margin: 6px 0 10px;
      }
      .progress .bar {
        height: 6px;
        background: #e5e7eb;
        border-radius: 3px;
        overflow: hidden;
      }
      .fill {
        height: 100%;
        background: var(--primary-color, var(--color-primary));
      }
      .pct {
        font-size: 11px;
        color: #6b7280;
      }
      .meta {
        font-size: 11px;
        color: #6b7280;
        margin-top: 8px;
      }
      .empty {
        padding: 16px;
        color: #6b7280;
      }
    `,
  ],
})
export class ChapterSidebarComponent {
  readonly chapters = input<EvidenceBinderChapter[]>([]);
  readonly selectedChapterId = input<string | null>(null);
  readonly select = output<string>();
}
