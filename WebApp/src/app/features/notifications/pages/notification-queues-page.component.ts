import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { NotificationStatCardComponent } from '../components/notification-stat-card.component';
import { CommunicationApiService, QueueStatus } from '../../communication/services/communication-api.service';

@Component({
  selector: 'app-notification-queues-page',
  standalone: true,
  imports: [MatCardModule, SettingsPageHeaderComponent, NotificationPageStateComponent, NotificationStatCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-settings-page-header title="Queues" description="Administrative monitoring of notification delivery queues." />
    <mat-card>
      <mat-card-content>
        @if (loading()) {
          <app-notification-page-state state="loading" message="Loading queue status…" [showRetry]="false" />
        } @else if (error()) {
          <app-notification-page-state state="error" [message]="error()!" (retry)="reload()" />
        } @else if (status()) {
          <div class="stats">
            <app-notification-stat-card label="Pending" [value]="status()!.pending" />
            <app-notification-stat-card label="Processing" [value]="status()!.processing" />
            <app-notification-stat-card label="Failed" [value]="status()!.failed" />
            <app-notification-stat-card label="Retrying" [value]="status()!.retrying" />
            <app-notification-stat-card label="Completed" [value]="status()!.completed" />
          </div>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      mat-card {
        border-radius: 16px;
      }
      .stats {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 12px;
      }
    `,
  ],
})
export class NotificationQueuesPageComponent implements OnInit {
  private readonly api = inject(CommunicationApiService);
  readonly status = signal<QueueStatus | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getQueueStatus().subscribe({
      next: (d) => {
        this.status.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.status.set(null);
        this.loading.set(false);
        this.error.set('Unable to load queue status.');
      },
    });
  }
}
