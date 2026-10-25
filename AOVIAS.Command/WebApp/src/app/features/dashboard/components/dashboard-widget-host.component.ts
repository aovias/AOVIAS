import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardWidgetDescriptor } from '../models/dashboard-experience.model';
import { DashboardWidgetSkeletonComponent } from './dashboard-widget-skeleton.component';

@Component({
  selector: 'app-dashboard-widget-host',
  standalone: true,
  imports: [CommonModule, DashboardWidgetSkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (visible()) {
      <section
        class="widget-host"
        [attr.aria-label]="descriptor()?.title ?? 'Dashboard widget'"
        [attr.data-widget-id]="descriptor()?.id"
      >
        @if (loading()) {
          <app-dashboard-widget-skeleton
            [minHeight]="skeletonMinHeight()"
            [ariaLabel]="'Loading ' + (descriptor()?.title ?? 'widget')"
          />
        } @else if (error()) {
          <div class="widget-host__error" role="alert">
            <p class="widget-host__error-title">{{ descriptor()?.title ?? 'Widget' }} unavailable</p>
            <p class="widget-host__error-message">{{ error() }}</p>
            <button type="button" class="widget-host__retry" (click)="retry.emit(descriptor()?.id ?? '')">
              Retry
            </button>
          </div>
        } @else if (dataReady()) {
          <div class="widget-host__content widget-host__content--ready">
            <ng-content />
          </div>
        } @else {
          <app-dashboard-widget-skeleton
            [minHeight]="skeletonMinHeight()"
            [ariaLabel]="'Waiting for ' + (descriptor()?.title ?? 'widget')"
          />
        }
      </section>
    }
  `,
  styles: [
    `
      .widget-host {
        display: block;
        min-width: 0;
      }

      .widget-host__content {
        opacity: 0;
        transition: opacity 0.2s ease;
      }

      .widget-host__content--ready {
        opacity: 1;
      }

      .widget-host__error {
        display: flex;
        flex-direction: column;
        gap: 8px;
        padding: 16px;
        min-height: 120px;
        border-radius: 12px;
        border: 1px solid rgba(239, 68, 68, 0.25);
        background: rgba(239, 68, 68, 0.06);
      }

      .widget-host__error-title {
        margin: 0;
        font-size: 14px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .widget-host__error-message {
        margin: 0;
        font-size: 13px;
        color: var(--text-secondary);
      }

      .widget-host__retry {
        align-self: flex-start;
        padding: 8px 14px;
        border: none;
        border-radius: 8px;
        background: var(--primary-color);
        color: #fff;
        font-size: 13px;
        font-weight: 600;
        cursor: pointer;
      }

      .widget-host__retry:focus-visible {
        outline: 2px solid var(--primary-color);
        outline-offset: 2px;
      }
    `,
  ],
})
export class DashboardWidgetHostComponent {
  readonly descriptor = input<DashboardWidgetDescriptor | null>(null);
  readonly visible = input(true);
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly dataReady = input(false);
  readonly skeletonMinHeight = input(180);

  readonly retry = output<string>();
}
