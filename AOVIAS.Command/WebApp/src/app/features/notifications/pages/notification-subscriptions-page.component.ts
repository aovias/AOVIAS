import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { NotificationStatusChipComponent } from '../../../shared/notifications/components/notification-ui.components';
import { CommunicationApiService, SubscriptionItem } from '../../communication/services/communication-api.service';

@Component({
  selector: 'app-notification-subscriptions-page',
  standalone: true,
  imports: [MatCardModule, SettingsPageHeaderComponent, NotificationPageStateComponent, NotificationStatusChipComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-settings-page-header title="Subscriptions" description="Event-driven notification subscriptions registered by platform modules." />
    <mat-card>
      <mat-card-content>
        @if (loading()) {
          <app-notification-page-state state="loading" message="Loading subscriptions…" [showRetry]="false" />
        } @else if (error()) {
          <app-notification-page-state state="error" [message]="error()!" (retry)="reload()" />
        } @else if (!items().length) {
          <app-notification-page-state state="empty" icon="subscriptions" message="No event subscriptions configured yet." [showRetry]="false" />
        } @else {
          <ul class="list">
            @for (s of items(); track s.id) {
              <li>
                <div>
                  <strong>{{ s.sourceEventType }}</strong>
                  <span>{{ s.moduleCode }} · {{ s.templateCode }} · {{ s.channels }}</span>
                </div>
                <app-notification-status-chip [status]="s.enabled ? 'Enabled' : 'Disabled'" />
              </li>
            }
          </ul>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      mat-card {
        border-radius: 16px;
      }
      .list {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      .list li {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        align-items: center;
        padding: 14px 4px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .list span {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class NotificationSubscriptionsPageComponent implements OnInit {
  private readonly api = inject(CommunicationApiService);
  readonly items = signal<SubscriptionItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listSubscriptions().subscribe({
      next: (d) => {
        this.items.set(d ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.loading.set(false);
        this.error.set('Unable to load event subscriptions.');
      },
    });
  }
}
