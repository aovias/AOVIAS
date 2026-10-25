import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { NotificationService } from '../../../core/ui/services/notification.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { SettingsComingSoonBadgeComponent } from '../../settings/components/settings-coming-soon-badge.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { AI_PROVIDER_CATALOG } from '../models/ai-admin-nav.model';
import { AiIdentityApiService, AIExecutionItem } from '../services/ai-identity-api.service';

interface ProviderRow {
  key: string;
  name: string;
  status: string;
  health: string;
  isDefault: boolean;
  executions: number;
  future: boolean;
}

@Component({
  selector: 'app-ai-providers-page',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    SettingsComingSoonBadgeComponent,
    AiAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="Providers"
        description="Provider catalog for AOVIAS Command. Status and health are inferred from existing execution telemetry — configuration remains in AI Orchestrator infrastructure."
      />

      <mat-card class="ai-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-ai-admin-page-state state="loading" message="Loading providers…" [showRetry]="false" />
          } @else if (error()) {
            <app-ai-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else {
            <div class="ai-provider-grid">
              @for (p of providers(); track p.key) {
                <article class="ai-provider" [class.ai-provider--future]="p.future">
                  <header class="ai-provider__header">
                    <div>
                      <h3>{{ p.name }}</h3>
                      @if (p.isDefault) {
                        <span class="ai-chip ai-chip--default">Default</span>
                      }
                      @if (p.future) {
                        <app-settings-coming-soon-badge />
                      }
                    </div>
                    <mat-icon aria-hidden="true">cloud</mat-icon>
                  </header>
                  <dl class="ai-provider__meta">
                    <div>
                      <dt>Status</dt>
                      <dd>{{ p.status }}</dd>
                    </div>
                    <div>
                      <dt>Health</dt>
                      <dd>{{ p.health }}</dd>
                    </div>
                    <div>
                      <dt>Executions</dt>
                      <dd>{{ p.executions }}</dd>
                    </div>
                  </dl>
                  <div class="ai-provider__actions">
                    <button mat-stroked-button type="button" (click)="testConnection(p)" [disabled]="p.future">
                      Test Connection
                    </button>
                    <button mat-button type="button" disabled title="Configuration remains in orchestrator settings">
                      Configure
                    </button>
                  </div>
                </article>
              }
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
      .ai-provider-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
        gap: 14px;
      }
      .ai-provider {
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        border-radius: 14px;
        padding: 16px;
        background: #fff;
      }
      .ai-provider--future {
        opacity: 0.85;
      }
      .ai-provider__header {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        margin-bottom: 12px;
      }
      .ai-provider__header h3 {
        margin: 0 0 6px;
        font-size: 15px;
      }
      .ai-provider__header mat-icon {
        color: var(--color-primary);
      }
      .ai-chip {
        display: inline-block;
        margin-right: 6px;
        padding: 2px 8px;
        border-radius: 999px;
        font-size: 11px;
        font-weight: 600;
      }
      .ai-chip--default {
        background: var(--color-primary-50, #eef3ff);
        color: var(--color-primary-700, #1e4bc7);
      }
      .ai-provider__meta {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 8px;
        margin: 0 0 14px;
      }
      .ai-provider__meta dt {
        font-size: 11px;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-provider__meta dd {
        margin: 2px 0 0;
        font-size: 13px;
        font-weight: 600;
      }
      .ai-provider__actions {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
    `,
  ],
})
export class AiProvidersPageComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);
  private readonly notify = inject(NotificationService);

  readonly providers = signal<ProviderRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listExecutions(1, 100).subscribe({
      next: (page) => {
        this.providers.set(this.buildProviders(page.items ?? []));
        this.loading.set(false);
      },
      error: () => {
        // Soft-fail: still show the known provider catalog without telemetry.
        this.providers.set(this.buildProviders([]));
        this.loading.set(false);
        this.error.set(null);
      },
    });
  }

  testConnection(provider: ProviderRow): void {
    if (provider.future) {
      this.notify.showError('Provider is not available yet.');
      return;
    }
    if (provider.executions > 0 && !/degraded|unavailable/i.test(provider.health)) {
      this.notify.showSuccess(`${provider.name}: recent executions look healthy.`);
    } else if (provider.executions > 0) {
      this.notify.showError(`${provider.name}: recent failures were observed.`);
    } else {
      this.notify.showError(`${provider.name}: no recent executions to evaluate.`);
    }
  }

  private buildProviders(items: AIExecutionItem[]): ProviderRow[] {
    const usage = new Map<string, AIExecutionItem[]>();
    for (const item of items) {
      const raw = (item.modelProvider || '').trim().toLowerCase();
      if (!raw) continue;
      const catalog = AI_PROVIDER_CATALOG.find((p) => p.aliases.some((a) => raw.includes(a)));
      const key = catalog?.key ?? raw;
      const bucket = usage.get(key) ?? [];
      bucket.push(item);
      usage.set(key, bucket);
    }

    const ranked = Array.from(usage.entries()).sort((a, b) => b[1].length - a[1].length);
    const defaultKey = ranked[0]?.[0] ?? 'azureopenai';

    return AI_PROVIDER_CATALOG.map((catalog) => {
      const bucket = usage.get(catalog.key) ?? [];
      const failed = bucket.filter((i) => /fail|error/i.test(i.outcome || '')).length;
      const future = catalog.key === 'ollama' || catalog.key === 'anthropic' || catalog.key === 'google'
        ? bucket.length === 0
        : false;
      let health = 'Unknown';
      let status = future ? 'Future' : 'Configured';
      if (bucket.length > 0) {
        status = 'Active';
        health = failed === 0 ? 'Healthy' : failed === bucket.length ? 'Unavailable' : 'Degraded';
      } else if (!future) {
        health = 'No recent usage';
      }

      return {
        key: catalog.key,
        name: catalog.name,
        status,
        health,
        isDefault: catalog.key === defaultKey && bucket.length > 0,
        executions: bucket.length,
        future,
      } satisfies ProviderRow;
    });
  }
}
