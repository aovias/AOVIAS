import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommunicationApiService, NotificationItem } from '../services/communication-api.service';
import { NotificationRealtimeService } from '../../../shared/notifications/services/notification-realtime.service';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header>
        <h1>Notification Center</h1>
        <nav>
          <a routerLink="preferences">Preferences</a> |
          <a routerLink="history">History</a> |
          <a routerLink="templates">Templates</a> |
          <a routerLink="reminders">Reminders</a> |
          <a routerLink="campaigns">Campaigns</a> |
          <a routerLink="delivery">Delivery</a> |
          <a routerLink="availability">Availability</a>
        </nav>
      </header>
      @if (loading()) {
        <p>Loading notifications…</p>
      } @else if (items().length === 0) {
        <p>No notifications yet.</p>
      } @else {
        <ul>
          @for (n of items(); track n.id) {
            <li [class.unread]="!n.isRead">
              <strong>{{ n.title }}</strong> — {{ n.body }}
              <small>{{ n.createdAt | date: 'medium' }} · {{ n.moduleCode }}</small>
              @if (!n.isRead) {
                <button type="button" (click)="markRead(n)">Mark read</button>
              }
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class NotificationCenterComponent implements OnInit, OnDestroy {
  private api = inject(CommunicationApiService);
  private realtime = inject(NotificationRealtimeService);
  items = signal<NotificationItem[]>([]);
  loading = signal(true);

  private readonly onRealtime = (): void => this.reload();

  ngOnInit(): void {
    this.reload();
    void this.realtime.connect(this.onRealtime);
  }

  ngOnDestroy(): void {
    void this.realtime.disconnect(this.onRealtime);
  }

  reload(): void {
    this.loading.set(true);
    this.api.listNotifications().subscribe({
      next: (p) => {
        this.items.set(p.items ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  markRead(n: NotificationItem): void {
    this.api.markRead(n.id).subscribe({
      next: () => this.items.update((list) => list.map((x) => (x.id === n.id ? { ...x, isRead: true } : x))),
    });
  }
}
