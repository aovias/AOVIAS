import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { AiIdentityApiService, AIAutomationPolicyItem } from '../services/ai-identity-api.service';

@Component({
  selector: 'app-automation-policies',
  standalone: true,
  imports: [MatCardModule, SettingsPageHeaderComponent, AiAdminPageStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="AI Policies"
        description="Approval mode, human review, confidence thresholds, and governed execution rules for AI automation."
      />
      <mat-card class="ai-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-ai-admin-page-state state="loading" message="Loading policies…" [showRetry]="false" />
          } @else if (error()) {
            <app-ai-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!policies().length) {
            <app-ai-admin-page-state
              state="empty"
              icon="policy"
              message="No governance policies configured yet."
              [showRetry]="false"
            />
          } @else {
            <div class="ai-table-wrap">
              <table class="ai-table">
                <thead>
                  <tr>
                    <th>Policy</th>
                    <th>Approval Mode</th>
                    <th>Human Review</th>
                    <th>Confidence</th>
                    <th>Risk</th>
                    <th>Execution Rules</th>
                  </tr>
                </thead>
                <tbody>
                  @for (p of policies(); track p.id) {
                    <tr>
                      <td>
                        <strong>{{ p.name }}</strong>
                      </td>
                      <td>{{ p.mode }}</td>
                      <td>{{ requiresHumanReview(p.mode) }}</td>
                      <td>{{ p.confidenceThreshold }}</td>
                      <td>{{ p.riskLevel || '—' }}</td>
                      <td class="ai-muted">
                        Allowed models / providers and rate limits remain enforced by AI Orchestrator policy configuration.
                      </td>
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
        vertical-align: top;
      }
      .ai-table th {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-muted {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
        max-width: 280px;
      }
    `,
  ],
})
export class AutomationPoliciesComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);
  readonly policies = signal<AIAutomationPolicyItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  requiresHumanReview(mode: string): string {
    const value = (mode || '').toLowerCase();
    if (value.includes('auto')) return 'Optional';
    if (value.includes('suggest')) return 'Always';
    return 'Required';
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listPolicies().subscribe({
      next: (d) => {
        this.policies.set(d ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.policies.set([]);
        this.loading.set(false);
        this.error.set('Unable to load governance policies.');
      },
    });
  }
}
