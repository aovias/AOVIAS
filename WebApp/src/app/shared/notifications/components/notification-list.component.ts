import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { NotificationItem } from '../../../features/communication/services/communication-api.service';

export type NotificationPriority = 'critical' | 'warning' | 'info';

export interface NotificationListEntry {
  id: string;
  title: string;
  message: string;
  time: string;
  type: NotificationPriority;
  isRead?: boolean;
}

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="notification-list">
      @for (item of items; track item.id) {
        <li
          class="notification-list__item"
          [class.notification-list__item--unread]="!item.isRead"
          [attr.data-type]="item.type"
          (click)="select.emit(item)"
        >
          <div class="notification-list__icon" [attr.data-type]="item.type" aria-hidden="true">●</div>
          <div class="notification-list__content">
            <strong>{{ item.title }}</strong>
            <p>{{ item.message }}</p>
            <small>{{ item.time }}</small>
          </div>
        </li>
      }
    </ul>
  `,
  styles: [
    `
      .notification-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      .notification-list__item {
        display: flex;
        gap: 12px;
        padding: 12px 16px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
        cursor: pointer;
      }
      .notification-list__item:hover {
        background: color-mix(in srgb, var(--color-bg-muted, #f3f4f6) 60%, transparent);
      }
      .notification-list__item--unread strong {
        font-weight: 700;
      }
      .notification-list__icon[data-type='critical'] {
        color: #dc2626;
      }
      .notification-list__icon[data-type='warning'] {
        color: #d97706;
      }
      .notification-list__icon[data-type='info'] {
        color: #2f6bff;
      }
      .notification-list__content strong {
        display: block;
        font-size: 14px;
        margin-bottom: 4px;
      }
      .notification-list__content p {
        margin: 0 0 4px;
        font-size: 13px;
        color: var(--color-text-secondary, #6b7280);
        line-height: 1.4;
      }
      .notification-list__content small {
        font-size: 11px;
        color: var(--color-text-secondary, #9ca3af);
      }
    `,
  ],
})
export class NotificationListComponent {
  @Input({ required: true }) items: NotificationListEntry[] = [];
  @Output() select = new EventEmitter<NotificationListEntry>();
}

export function mapNotificationItem(n: NotificationItem): NotificationListEntry {
  const type: NotificationPriority =
    n.moduleCode === 'Compliance' ? 'critical' : n.moduleCode === 'Governance' ? 'warning' : 'info';
  return {
    id: n.id,
    type,
    title: n.title,
    message: n.body,
    time: new Date(n.createdAt).toLocaleString(),
    isRead: n.isRead,
  };
}
