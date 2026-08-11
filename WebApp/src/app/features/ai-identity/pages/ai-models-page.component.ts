import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { AiIdentityApiService, AIExecutionItem } from '../services/ai-identity-api.service';

interface ModelRow {
  provider: string;
  modelName: string;
  status: string;
  executions: number;
  avgLatencyMs: number | null;
  lastSeenUtc: string | null;
  availability: string;
}

@Component({
  selector: 'app-ai-models-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    AiAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="Models"
        description="Observed model inventory from governed AI executions. Configuration and enablement actions arrive with the provider registry."
      />

      <mat-card class="ai-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-ai-admin-page-state state="loading" message="Loading models…" [showRetry]="false" />
          } @else if (error()) {
            <app-ai-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!models().length) {
            <app-ai-admin-page-state
              state="empty"
              icon="psychology"
              message="No model usage has been observed yet."
              [showRetry]="false"
            />
          } @else {
            <div class="ai-toolbar">
              <mat-form-field appearance="outline" class="ai-toolbar__search">
                <mat-label>Search models</mat-label>
                <input matInput [ngModel]="search()" (ngModelChange)="search.set($event)" />
                <mat-icon matSuffix aria-hidden="true">search</mat-icon>
              </mat-form-field>
            </div>

            <div class="ai-table-wrap">
              <table class="ai-table">
                <thead>
                  <tr>
                    <th>Provider</th>
                    <th>Model Name</th>
                    <th>Status</th>
                    <th>Availability</th>
                    <th>Avg Latency</th>
                    <th>Executions</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  @for (m of filteredModels(); track m.provider + m.modelName) {
                    <tr>
                      <td>{{ m.provider }}</td>
                      <td>
                        <strong>{{ m.modelName }}</strong>
                      </td>
                      <td><span class="ai-chip">{{ m.status }}</span></td>
                      <td>{{ m.availability }}</td>
                      <td>{{ m.avgLatencyMs == null ? '—' : m.avgLatencyMs + ' ms' }}</td>
                      <td>{{ m.executions }}</td>
                      <td>
                        <span class="ai-actions">
                          <span class="ai-actions__item" title="View details coming soon">View</span>
                          <span class="ai-actions__item ai-actions__item--muted" title="Coming soon">Configure</span>
                          <span class="ai-actions__item ai-actions__item--muted" title="Coming soon">Enable</span>
                          <span class="ai-actions__item ai-actions__item--muted" title="Coming soon">Disable</span>
                        </span>
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
      .ai-toolbar {
        margin-bottom: 12px;
      }
      .ai-toolbar__search {
        width: min(360px, 100%);
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
        font-weight: 600;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-chip {
        display: inline-block;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        background: #ecfdf5;
        color: #047857;
      }
      .ai-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .ai-actions__item {
        font-size: 12px;
        font-weight: 600;
        color: var(--color-primary);
      }
      .ai-actions__item--muted {
        color: #9ca3af;
        font-weight: 500;
      }
    `,
  ],
})
export class AiModelsPageComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);

  readonly models = signal<ModelRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly search = signal('');

  readonly filteredModels = computed(() => {
    const q = this.search().trim().toLowerCase();
    if (!q) return this.models();
    return this.models().filter(
      (m) =>
        m.provider.toLowerCase().includes(q) ||
        m.modelName.toLowerCase().includes(q) ||
        m.status.toLowerCase().includes(q),
    );
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listExecutions(1, 100).subscribe({
      next: (page) => {
        this.models.set(this.buildModels(page.items ?? []));
        this.loading.set(false);
      },
      error: () => {
        this.models.set([]);
        this.loading.set(false);
        this.error.set('Unable to load model inventory from execution history.');
      },
    });
  }

  private buildModels(items: AIExecutionItem[]): ModelRow[] {
    const map = new Map<string, { provider: string; modelName: string; items: AIExecutionItem[] }>();
    for (const item of items) {
      const provider = (item.modelProvider || 'Unknown').trim() || 'Unknown';
      const modelName = (item.modelName || item.useCase || 'Unknown').trim() || 'Unknown';
      const key = `${provider}::${modelName}`.toLowerCase();
      const bucket = map.get(key) ?? { provider, modelName, items: [] };
      bucket.items.push(item);
      map.set(key, bucket);
    }

    return Array.from(map.values())
      .map((bucket) => {
        const latencies = bucket.items
          .map((i) => i.latencyMs)
          .filter((v): v is number => typeof v === 'number' && Number.isFinite(v));
        const failed = bucket.items.filter((i) => /fail|error/i.test(i.outcome || '')).length;
        const last = bucket.items
          .map((i) => i.occurredAtUtc)
          .sort()
          .at(-1);
        return {
          provider: bucket.provider,
          modelName: bucket.modelName,
          status: failed === bucket.items.length ? 'Degraded' : 'Observed',
          executions: bucket.items.length,
          avgLatencyMs:
            latencies.length > 0
              ? Math.round(latencies.reduce((a, b) => a + b, 0) / latencies.length)
              : null,
          lastSeenUtc: last ?? null,
          availability: failed > 0 ? 'Intermittent' : 'Available',
        } satisfies ModelRow;
      })
      .sort((a, b) => b.executions - a.executions);
  }
}
