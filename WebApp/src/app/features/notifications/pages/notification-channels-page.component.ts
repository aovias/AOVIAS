import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { SettingsComingSoonBadgeComponent } from '../../settings/components/settings-coming-soon-badge.component';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { CommunicationApiService, ChannelItem } from '../../communication/services/communication-api.service';

@Component({
  selector: 'app-notification-channels-page',
  standalone: true,
  imports: [MatCardModule, SettingsPageHeaderComponent, SettingsComingSoonBadgeComponent, NotificationPageStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-settings-page-header title="Channels" description="Supported delivery channels across the AOVIAS platform notification framework." />
    <mat-card>
      <mat-card-content>
        @if (loading()) {
          <app-notification-page-state state="loading" message="Loading channels…" [showRetry]="false" />
        } @else if (error()) {
          <app-notification-page-state state="error" [message]="error()!" (retry)="reload()" />
        } @else {
          <ul class="list">
            @for (c of items(); track c.channel) {
              <li>
                <div>
                  <strong>{{ c.channel }}</strong>
                  <span>{{ c.description }}</span>
                </div>
                <div class="list__meta">
                  @if (!c.implemented) {
                    <app-settings-coming-soon-badge />
                  } @else {
                    <span class="chip" [class.chip--off]="!c.enabled">{{ c.enabled ? 'Enabled' : 'Disabled' }}</span>
                  }
                </div>
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
      .list strong {
        display: block;
      }
      .list span {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .list__meta {
        display: flex;
        align-items: center;
        gap: 8px;
      }
      .chip {
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        background: #dcfce7;
        color: #166534;
      }
      .chip--off {
        background: #f3f4f6;
        color: #6b7280;
      }
    `,
  ],
})
export class NotificationChannelsPageComponent implements OnInit {
  private readonly api = inject(CommunicationApiService);
  readonly items = signal<ChannelItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listChannels().subscribe({
      next: (d) => {
        this.items.set(d ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.loading.set(false);
        this.error.set('Unable to load notification channels.');
      },
    });
  }
}
