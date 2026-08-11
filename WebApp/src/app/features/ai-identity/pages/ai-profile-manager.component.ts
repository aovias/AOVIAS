import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { AiIdentityApiService, AIProfileItem } from '../services/ai-identity-api.service';

@Component({
  selector: 'app-ai-profile-manager',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    SettingsPageHeaderComponent,
    AiAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="AI Identity"
        description="Governed AI identity profiles bound to platform modules and lifecycle status."
      />
      <mat-card class="ai-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-ai-admin-page-state state="loading" message="Loading AI identities…" [showRetry]="false" />
          } @else if (error()) {
            <app-ai-admin-page-state
              state="error"
              [message]="error()!"
              (retry)="reload()"
            />
          } @else if (!profiles().length) {
            <app-ai-admin-page-state
              state="empty"
              icon="badge"
              message="No AI identities configured yet."
              [showRetry]="false"
            />
          } @else {
            <div class="ai-table-wrap">
              <table class="ai-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Code</th>
                    <th>Module</th>
                    <th>Version</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  @for (p of profiles(); track p.id) {
                    <tr>
                      <td><strong>{{ p.name }}</strong></td>
                      <td>{{ p.code }}</td>
                      <td>{{ p.moduleCode }}</td>
                      <td>v{{ p.version }}</td>
                      <td><span class="ai-chip">{{ p.status }}</span></td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
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
      .ai-table-wrap {
        overflow-x: auto;
      }
      .ai-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px;
      }
      .ai-table th,
      .ai-table td {
        text-align: left;
        padding: 12px 10px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .ai-table th {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-chip {
        display: inline-block;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        background: var(--color-primary-50, #eef3ff);
        color: var(--color-primary-700, #1e4bc7);
      }
    `,
  ],
})
export class AiProfileManagerComponent implements OnInit {
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
        this.error.set('Unable to load AI profiles.');
      },
    });
  }
}
