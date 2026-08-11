import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { NotificationStatusChipComponent } from '../../../shared/notifications/components/notification-ui.components';
import { CommunicationApiService, TemplateItem } from '../../communication/services/communication-api.service';

@Component({
  selector: 'app-notification-templates-page',
  standalone: true,
  imports: [MatCardModule, SettingsPageHeaderComponent, NotificationPageStateComponent, NotificationStatusChipComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-settings-page-header title="Templates" description="Reusable templates with subject, body, variables, version, and status by module category." />
    <mat-card>
      <mat-card-content>
        @if (loading()) {
          <app-notification-page-state state="loading" message="Loading templates…" [showRetry]="false" />
        } @else if (error()) {
          <app-notification-page-state state="error" [message]="error()!" (retry)="reload()" />
        } @else if (!items().length) {
          <app-notification-page-state state="empty" icon="description" message="No notification templates configured yet." [showRetry]="false" />
        } @else {
          <ul class="list">
            @for (t of items(); track t.id) {
              <li>
                <div>
                  <strong>{{ t.name }}</strong>
                  <span>{{ t.code }} · {{ t.channel }} · {{ t.moduleCode }} · v{{ t.currentVersion }}</span>
                </div>
                <app-notification-status-chip [status]="t.status" />
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
      .list li:last-child {
        border-bottom: 0;
      }
      .list strong {
        display: block;
      }
      .list span {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class NotificationTemplatesPageComponent implements OnInit {
  private readonly api = inject(CommunicationApiService);
  readonly items = signal<TemplateItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listTemplates().subscribe({
      next: (d) => {
        this.items.set(d ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.loading.set(false);
        this.error.set('Unable to load notification templates.');
      },
    });
  }
}
