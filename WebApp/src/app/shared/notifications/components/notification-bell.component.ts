import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommunicationApiService } from '../../../features/communication/services/communication-api.service';
import { NotificationBadgeComponent } from './notification-ui.components';
import { NotificationListComponent, NotificationListEntry, mapNotificationItem } from './notification-list.component';
import { NotificationLoadingStateComponent, NotificationEmptyStateComponent } from './notification-ui.components';
import { NotificationRealtimeService } from '../services/notification-realtime.service';

@Component({
  selector: 'app-notification-drawer',
  standalone: true,
  imports: [
    RouterLink,
    NotificationBadgeComponent,
    NotificationListComponent,
    NotificationLoadingStateComponent,
    NotificationEmptyStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="notification-drawer">
      <button
        type="button"
        class="notification-drawer__bell"
        (click)="toggle.emit()"
        title="Notifications"
        aria-label="Notifications"
        [attr.aria-expanded]="open"
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true">
          <path
            d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        </svg>
        <app-notification-badge [count]="unreadCount" />
      </button>

      @if (open) {
        <div class="notification-drawer__panel" role="dialog" aria-label="Recent notifications">
          <div class="notification-drawer__header">
            <h4>Notifications</h4>
            <button type="button" class="notification-drawer__mark-all" (click)="markAllRead.emit()" [disabled]="unreadCount === 0">
              Mark all read
            </button>
          </div>
          <div class="notification-drawer__body">
            @if (loading) {
              <app-notification-loading-state message="Loading notifications…" />
            } @else if (!items.length) {
              <app-notification-empty-state message="No notifications yet." icon="notifications_none" />
            } @else {
              <app-notification-list [items]="items" (select)="itemSelect.emit($event)" />
            }
          </div>
          <div class="notification-drawer__footer">
            <a [routerLink]="viewAllRoute" class="notification-drawer__view-all">View all</a>
          </div>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .notification-drawer {
        position: relative;
      }
      .notification-drawer__bell {
        position: relative;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        border: 0;
        border-radius: 10px;
        background: transparent;
        color: inherit;
        cursor: pointer;
      }
      .notification-drawer__bell:hover {
        background: color-mix(in srgb, var(--color-bg-muted, #f3f4f6) 80%, transparent);
      }
      .notification-drawer__panel {
        position: absolute;
        top: calc(100% + 8px);
        right: 0;
        width: min(380px, calc(100vw - 48px));
        background: var(--color-bg-surface, #fff);
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        border-radius: 16px;
        box-shadow: 0 12px 40px rgba(15, 23, 42, 0.12);
        z-index: var(--z-modal, 1000);
        overflow: hidden;
      }
      .notification-drawer__header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        padding: 14px 16px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .notification-drawer__header h4 {
        margin: 0;
        font-size: 15px;
        font-weight: 600;
      }
      .notification-drawer__mark-all {
        border: 0;
        background: transparent;
        color: var(--color-primary);
        font-size: 13px;
        font-weight: 500;
        cursor: pointer;
      }
      .notification-drawer__mark-all:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }
      .notification-drawer__body {
        max-height: 360px;
        overflow: auto;
      }
      .notification-drawer__footer {
        padding: 12px 16px;
        border-top: 1px solid var(--color-border-subtle, #e5e7eb);
        text-align: center;
      }
      .notification-drawer__view-all {
        color: var(--color-primary);
        font-size: 13px;
        font-weight: 600;
        text-decoration: none;
      }
    `,
  ],
})
export class NotificationDrawerComponent {
  @Input() open = false;
  @Input() unreadCount = 0;
  @Input() items: NotificationListEntry[] = [];
  @Input() loading = false;
  @Input() viewAllRoute = '/app/settings/notifications';
  @Output() toggle = new EventEmitter<void>();
  @Output() markAllRead = new EventEmitter<void>();
  @Output() itemSelect = new EventEmitter<NotificationListEntry>();
}

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [NotificationDrawerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-notification-drawer
      [open]="menuOpen()"
      [unreadCount]="unreadCount()"
      [items]="items()"
      [loading]="loading()"
      [viewAllRoute]="viewAllRoute"
      (toggle)="onToggle()"
      (markAllRead)="onMarkAllRead()"
      (itemSelect)="onItemSelect($event)"
    />
  `,
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  private readonly api = inject(CommunicationApiService);
  private readonly realtime = inject(NotificationRealtimeService);

  readonly viewAllRoute = '/app/settings/notifications';
  readonly menuOpen = signal(false);
  readonly unreadCount = signal(0);
  readonly items = signal<NotificationListEntry[]>([]);
  readonly loading = signal(false);

  private readonly onRealtime = (): void => {
    this.refreshBadge();
    if (this.menuOpen()) this.loadRecent();
  };

  ngOnInit(): void {
    if (typeof requestIdleCallback === 'function') {
      requestIdleCallback(() => this.refreshBadge());
    } else {
      setTimeout(() => this.refreshBadge(), 0);
    }
    void this.realtime.connect(this.onRealtime);
  }

  ngOnDestroy(): void {
    void this.realtime.disconnect(this.onRealtime);
  }

  refreshBadge(): void {
    this.api.getUnreadCount().subscribe({
      next: (count) => this.unreadCount.set(count ?? 0),
      error: () => this.unreadCount.set(0),
    });
  }

  onToggle(): void {
    const opening = !this.menuOpen();
    this.menuOpen.set(opening);
    if (opening) this.loadRecent();
  }

  close(): void {
    this.menuOpen.set(false);
  }

  onMarkAllRead(): void {
    this.api.markAllRead().subscribe({
      next: () => {
        this.unreadCount.set(0);
        this.items.update((list) => list.map((x) => ({ ...x, isRead: true })));
      },
    });
  }

  onItemSelect(item: NotificationListEntry): void {
    if (!item.isRead) {
      this.api.markRead(item.id).subscribe({
        next: () => {
          this.items.update((list) => list.map((x) => (x.id === item.id ? { ...x, isRead: true } : x)));
          this.unreadCount.update((c) => Math.max(0, c - 1));
        },
      });
    }
  }

  private loadRecent(): void {
    this.loading.set(true);
    this.api.listNotifications(1, 8).subscribe({
      next: (page) => {
        this.items.set((page.items ?? []).map(mapNotificationItem));
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.loading.set(false);
      },
    });
  }
}
