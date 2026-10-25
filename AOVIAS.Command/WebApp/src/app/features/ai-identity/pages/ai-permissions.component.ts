import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { AiIdentityApiService, AIProfileItem } from '../services/ai-identity-api.service';

@Component({
  selector: 'app-ai-permissions',
  standalone: true,
  imports: [MatCardModule, SettingsPageHeaderComponent, AiAdminPageStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="AI Permissions"
        description="Profile permission grants map to Access Governance codes for query, assist, and execute actions."
      />
      <mat-card class="ai-page__card">
        <mat-card-content>
          <p class="ai-page__hint">
            Permissions such as AI.Query.Read and AI.Action.Execute are managed through Access Governance role assignments.
          </p>
          @if (loading()) {
            <app-ai-admin-page-state state="loading" message="Loading profiles…" [showRetry]="false" />
          } @else if (error()) {
            <app-ai-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!profiles().length) {
            <app-ai-admin-page-state
              state="empty"
              icon="lock"
              message="No profiles available for permission mapping."
              [showRetry]="false"
            />
          } @else {
            <ul class="ai-list">
              @for (p of profiles(); track p.id) {
                <li>
                  <div>
                    <strong>{{ p.name }}</strong>
                    <span>Managed via Access Governance role assignments</span>
                  </div>
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
      .ai-page__hint {
        margin: 0 0 16px;
        font-size: 13px;
        line-height: 1.5;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      .ai-list li {
        padding: 14px 4px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .ai-list li:last-child {
        border-bottom: 0;
      }
      .ai-list strong {
        display: block;
        font-size: 14px;
      }
      .ai-list span {
        display: block;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
        margin-top: 2px;
      }
    `,
  ],
})
export class AiPermissionsComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);
  readonly profiles = signal<AIProfileItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listProfiles().subscribe({
      next: (d) => {
        this.profiles.set(d ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.profiles.set([]);
        this.loading.set(false);
        this.error.set('Unable to load AI permissions.');
      },
    });
  }
}
