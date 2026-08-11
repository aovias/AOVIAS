import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  AdminPageStateComponent,
  AdminPageStateKind,
} from '../../ui/administration/admin-page-state.component';

export type NotificationPageStateKind = AdminPageStateKind;

@Component({
  selector: 'app-notification-empty-state',
  standalone: true,
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="notification-state" role="status">
      <mat-icon aria-hidden="true">{{ icon }}</mat-icon>
      <p>{{ message }}</p>
    </div>
  `,
  styles: [
    `
      .notification-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 12px;
        padding: 40px 16px;
        color: var(--color-text-secondary, #6b7280);
        text-align: center;
      }
      .notification-state p {
        margin: 0;
        font-size: 14px;
      }
      .notification-state mat-icon {
        font-size: 32px;
        width: 32px;
        height: 32px;
        opacity: 0.7;
      }
    `,
  ],
})
export class NotificationEmptyStateComponent {
  @Input() message = 'Nothing here yet.';
  @Input() icon = 'inbox';
}

@Component({
  selector: 'app-notification-loading-state',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="notification-state" role="status" aria-live="polite">
      <mat-spinner diameter="36" />
      <span>{{ message }}</span>
    </div>
  `,
  styles: [
    `
      .notification-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 12px;
        padding: 40px 16px;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class NotificationLoadingStateComponent {
  @Input() message = 'Loading…';
}

@Component({
  selector: 'app-notification-page-state',
  standalone: true,
  imports: [AdminPageStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-admin-page-state
      [state]="state"
      [message]="message"
      [icon]="icon"
      [showRetry]="showRetry"
      (retry)="retry.emit()"
    />
  `,
})
export class NotificationPageStateComponent {
  @Input({ required: true }) state!: NotificationPageStateKind;
  @Input() message = '';
  @Input() icon = '';
  @Input() showRetry = true;
  @Output() retry = new EventEmitter<void>();
}

@Component({
  selector: 'app-notification-status-chip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="chip" [attr.data-status]="status">{{ status }}</span>`,
  styles: [
    `
      .chip {
        display: inline-flex;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        text-transform: capitalize;
        background: var(--color-primary-50, #eef3ff);
        color: var(--color-primary-700, #1e4bc7);
      }
      .chip[data-status='Failed'],
      .chip[data-status='failed'] {
        background: #fee2e2;
        color: #991b1b;
      }
      .chip[data-status='Delivered'],
      .chip[data-status='delivered'],
      .chip[data-status='Completed'],
      .chip[data-status='completed'] {
        background: #dcfce7;
        color: #166534;
      }
      .chip[data-status='Pending'],
      .chip[data-status='pending'],
      .chip[data-status='Queued'],
      .chip[data-status='queued'] {
        background: #fef3c7;
        color: #92400e;
      }
    `,
  ],
})
export class NotificationStatusChipComponent {
  @Input({ required: true }) status!: string;
}

@Component({
  selector: 'app-notification-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (count > 0) {
      <span class="badge" [attr.aria-label]="count + ' unread notifications'">{{ displayCount }}</span>
    }
  `,
  styles: [
    `
      .badge {
        position: absolute;
        top: 2px;
        right: 2px;
        min-width: 18px;
        height: 18px;
        padding: 0 5px;
        border-radius: 999px;
        background: #dc2626;
        color: #fff;
        font-size: 11px;
        font-weight: 700;
        line-height: 18px;
        text-align: center;
      }
    `,
  ],
})
export class NotificationBadgeComponent {
  @Input() count = 0;

  get displayCount(): string {
    return this.count > 99 ? '99+' : String(this.count);
  }
}
