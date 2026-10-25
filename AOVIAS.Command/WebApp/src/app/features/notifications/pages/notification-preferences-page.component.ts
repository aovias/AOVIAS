import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { CommunicationApiService, PreferenceItem } from '../../communication/services/communication-api.service';

@Component({
  selector: 'app-notification-preferences-page',
  standalone: true,
  imports: [MatCardModule, SettingsPageHeaderComponent, NotificationPageStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-settings-page-header title="Preferences" description="User notification preferences for email, SMS, push, in-app, digest, quiet hours, and categories." />
    <mat-card>
      <mat-card-content>
        @if (loading()) {
          <app-notification-page-state state="loading" message="Loading preferences…" [showRetry]="false" />
        } @else if (error()) {
          <app-notification-page-state state="error" [message]="error()!" (retry)="reload()" />
        } @else if (!items().length) {
          <app-notification-page-state state="empty" icon="tune" message="No notification preferences configured yet." [showRetry]="false" />
        } @else {
          <ul class="list">
            @for (p of items(); track p.id) {
              <li>
                <div>
                  <strong>{{ p.channel }} · {{ p.frequency }}</strong>
                  <span>
                    {{ p.moduleCode || 'All modules' }}
                    @if (p.quietHoursStart && p.quietHoursEnd) {
                      · Quiet {{ p.quietHoursStart }}–{{ p.quietHoursEnd }}
                    }
                  </span>
                </div>
                <span class="chip" [class.chip--off]="!p.enabled">{{ p.enabled ? 'Enabled' : 'Disabled' }}</span>
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
      .list span {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class NotificationPreferencesPageComponent implements OnInit {
  private readonly api = inject(CommunicationApiService);
  readonly items = signal<PreferenceItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listPreferences().subscribe({
      next: (d) => {
        this.items.set(d ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.loading.set(false);
        this.error.set('Unable to load notification preferences.');
      },
    });
  }
}
