import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard-widget-skeleton',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="widget-skeleton"
      [style.min-height.px]="minHeight()"
      role="status"
      aria-busy="true"
      [attr.aria-label]="ariaLabel()"
    >
      <div class="widget-skeleton__title"></div>
      <div class="widget-skeleton__body"></div>
    </div>
  `,
  styles: [
    `
      .widget-skeleton {
        display: flex;
        flex-direction: column;
        gap: 12px;
        padding: 16px;
        border-radius: 12px;
        border: 1px solid var(--border-color);
        background: var(--card-bg);
        overflow: hidden;
      }

      .widget-skeleton__title,
      .widget-skeleton__body {
        border-radius: 8px;
        background: var(--hover-bg);
        position: relative;
        overflow: hidden;
      }

      .widget-skeleton__title {
        width: 40%;
        height: 16px;
      }

      .widget-skeleton__body {
        flex: 1;
        min-height: 120px;
      }

      .widget-skeleton__title::after,
      .widget-skeleton__body::after {
        content: '';
        position: absolute;
        inset: 0;
        transform: translateX(-100%);
        background: linear-gradient(
          90deg,
          transparent,
          rgba(255, 255, 255, 0.08),
          transparent
        );
        animation: widget-shimmer 1.5s infinite;
      }

      @keyframes widget-shimmer {
        100% {
          transform: translateX(100%);
        }
      }
    `,
  ],
})
export class DashboardWidgetSkeletonComponent {
  readonly minHeight = input(180);
  readonly ariaLabel = input('Loading widget');
}
