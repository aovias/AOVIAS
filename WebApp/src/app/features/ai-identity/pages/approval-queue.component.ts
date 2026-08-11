import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { NotificationService } from '../../../core/ui/services/notification.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { AiIdentityApiService, AIApprovalItem } from '../services/ai-identity-api.service';

@Component({
  selector: 'app-approval-queue',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    SettingsPageHeaderComponent,
    AiAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="Approval Queue"
        description="Review and grant pending automation approvals for governed AI executions."
      />
      <mat-card class="ai-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-ai-admin-page-state state="loading" message="Loading approvals…" [showRetry]="false" />
          } @else if (error()) {
            <app-ai-admin-page-state state="error" [message]="error()!" (retry)="load()" />
          } @else if (!approvals().length) {
            <app-ai-admin-page-state
              state="empty"
              icon="task_alt"
              message="No pending approvals."
              [showRetry]="false"
            />
          } @else {
            <ul class="ai-list">
              @for (a of approvals(); track a.id) {
                <li>
                  <div>
                    <strong>{{ a.status }}</strong>
                    <span>Audit {{ a.executionAuditId }}</span>
                  </div>
                  <button mat-flat-button color="primary" type="button" (click)="grant(a)">Grant</button>
                </li>
              }
            </ul>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .ai-page__card {
        border-radius: 16px;
      }
      .ai-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      .ai-list li {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        align-items: center;
        padding: 14px 4px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .ai-list li:last-child {
        border-bottom: 0;
      }
      .ai-list strong {
        display: block;
        font-size: 14px;
        text-transform: capitalize;
      }
      .ai-list span {
        display: block;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
        margin-top: 2px;
        word-break: break-all;
      }
    `,
  ],
})
export class ApprovalQueueComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);
  private readonly notify = inject(NotificationService);
  readonly approvals = signal<AIApprovalItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listApprovals().subscribe({
      next: (d) => {
        this.approvals.set(d ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.approvals.set([]);
        this.loading.set(false);
        this.error.set('Unable to load approval queue.');
      },
    });
  }

  grant(a: AIApprovalItem): void {
    this.api.grantApproval(a.id).subscribe({
      next: () => {
        this.notify.showSuccess('Approval granted');
        this.load();
      },
      error: () => this.notify.showError('Failed to grant approval'),
    });
  }
}
