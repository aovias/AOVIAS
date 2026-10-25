import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { catchError, forkJoin, of } from 'rxjs';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { NotificationStatCardComponent } from '../components/notification-stat-card.component';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { NOTIFICATION_ADMIN_BASE } from '../models/notification-admin-nav.model';
import { CommunicationApiService } from '../../communication/services/communication-api.service';

@Component({
  selector: 'app-notification-overview-page',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    NotificationStatCardComponent,
    NotificationPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="overview">
      <app-settings-page-header
        title="Enterprise Notifications"
        description="Platform-wide notification framework for templates, channels, delivery, preferences, and subscriptions."
      />
      @if (loading()) {
        <app-notification-page-state state="loading" message="Loading overview…" [showRetry]="false" />
      } @else {
        <section class="overview__stats">
          <app-notification-stat-card label="Pending deliveries" [value]="stats().pending" [placeholder]="stats().pending === '—'" />
          <app-notification-stat-card label="Delivered today" [value]="stats().delivered" [placeholder]="stats().delivered === '—'" />
          <app-notification-stat-card label="Failed today" [value]="stats().failed" [placeholder]="stats().failed === '—'" />
          <app-notification-stat-card label="Unread in-app" [value]="stats().unread" [placeholder]="stats().unread === '—'" />
          <app-notification-stat-card label="Queue pending" [value]="stats().queuePending" [placeholder]="stats().queuePending === '—'" />
          <app-notification-stat-card label="Active templates" [value]="stats().templates" [placeholder]="stats().templates === '—'" />
          <app-notification-stat-card label="Subscriptions" [value]="stats().subscriptions" [placeholder]="stats().subscriptions === '—'" />
          <app-notification-stat-card label="Channels enabled" [value]="stats().channels" [placeholder]="stats().channels === '—'" />
        </section>
        <section class="overview__cards">
          @for (card of cards; track card.route) {
            <a class="overview-card" [routerLink]="card.route">
              <mat-icon aria-hidden="true">{{ card.icon }}</mat-icon>
              <div>
                <strong>{{ card.label }}</strong>
                <span>{{ card.description }}</span>
              </div>
            </a>
          }
        </section>
      }
    </div>
  `,
  styles: [
    `
      .overview__stats {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
        gap: 12px;
        margin-bottom: 24px;
      }
      .overview__cards {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: 12px;
      }
      .overview-card {
        display: flex;
        gap: 12px;
        padding: 16px;
        border-radius: 16px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        text-decoration: none;
        color: inherit;
      }
      .overview-card mat-icon {
        color: var(--color-primary);
      }
      .overview-card strong {
        display: block;
        margin-bottom: 4px;
      }
      .overview-card span {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class NotificationOverviewPageComponent implements OnInit {
  private readonly api = inject(CommunicationApiService);
  readonly loading = signal(true);
  readonly stats = signal({
    pending: '—',
    delivered: '—',
    failed: '—',
    unread: '—',
    queuePending: '—',
    templates: '—',
    subscriptions: '—',
    channels: '—',
  });

  readonly cards = [
    { label: 'Templates', description: 'Reusable notification templates with variables and versions.', route: `${NOTIFICATION_ADMIN_BASE}/templates`, icon: 'description' },
    { label: 'Channels', description: 'In-app, email, SMS, push, Teams, Slack, and future channels.', route: `${NOTIFICATION_ADMIN_BASE}/channels`, icon: 'hub' },
    { label: 'Delivery Providers', description: 'Pluggable SMTP, Azure, Twilio, Firebase, and more.', route: `${NOTIFICATION_ADMIN_BASE}/providers`, icon: 'settings_input_component' },
    { label: 'Queues', description: 'Monitor pending, processing, failed, and completed deliveries.', route: `${NOTIFICATION_ADMIN_BASE}/queues`, icon: 'queue' },
    { label: 'Delivery History', description: 'Recipient, channel, status, retries, and correlation IDs.', route: `${NOTIFICATION_ADMIN_BASE}/history`, icon: 'history' },
    { label: 'Preferences', description: 'User channel preferences, digest, and quiet hours.', route: `${NOTIFICATION_ADMIN_BASE}/preferences`, icon: 'tune' },
    { label: 'Subscriptions', description: 'Event-driven notification subscriptions across modules.', route: `${NOTIFICATION_ADMIN_BASE}/subscriptions`, icon: 'subscriptions' },
  ];

  ngOnInit(): void {
    forkJoin({
      dashboard: this.api.getDashboard().pipe(catchError(() => of(null))),
      queues: this.api.getQueueStatus().pipe(catchError(() => of(null))),
      templates: this.api.listTemplates().pipe(catchError(() => of(null))),
      subscriptions: this.api.listSubscriptions().pipe(catchError(() => of(null))),
      channels: this.api.listChannels().pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ dashboard, queues, templates, subscriptions, channels }) => {
        this.stats.set({
          pending: dashboard ? String(dashboard.pendingMessages) : '—',
          delivered: dashboard ? String(dashboard.deliveredToday) : '—',
          failed: dashboard ? String(dashboard.failedToday) : '—',
          unread: dashboard ? String(dashboard.unreadInApp) : '—',
          queuePending: queues ? String(queues.pending) : '—',
          templates: templates ? String(templates.length) : '—',
          subscriptions: subscriptions ? String(subscriptions.length) : '—',
          channels: channels ? String(channels.filter((c) => c.enabled).length) : '—',
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
