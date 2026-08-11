import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { AiIdentityApiService, AIExecutionItem } from '../services/ai-identity-api.service';

@Component({
  selector: 'app-execution-history',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    SettingsPageHeaderComponent,
    AiAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="Executions"
        description="Audit trail of AI executions including prompt/use case, provider, model, duration, status, and correlation identifiers."
      />
      <mat-card class="ai-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-ai-admin-page-state state="loading" message="Loading executions…" [showRetry]="false" />
          } @else if (error()) {
            <app-ai-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!executions().length) {
            <app-ai-admin-page-state
              state="empty"
              icon="history"
              message="No execution history is available yet."
              [showRetry]="false"
            />
          } @else {
            <div class="ai-toolbar">
              <mat-form-field appearance="outline" class="ai-toolbar__search">
                <mat-label>Search</mat-label>
                <input matInput [ngModel]="search()" (ngModelChange)="search.set($event)" />
                <mat-icon matSuffix aria-hidden="true">search</mat-icon>
              </mat-form-field>
              <button mat-stroked-button type="button" (click)="prevPage()" [disabled]="page() <= 1">Previous</button>
              <button mat-stroked-button type="button" (click)="nextPage()" [disabled]="!hasMore()">Next</button>
              <span class="ai-muted">Page {{ page() }} · {{ totalCount() }} total</span>
            </div>

            <div class="ai-table-wrap">
              <table class="ai-table">
                <thead>
                  <tr>
                    <th>Prompt / Use Case</th>
                    <th>Provider</th>
                    <th>Model</th>
                    <th>Status</th>
                    <th>Duration</th>
                    <th>Tokens</th>
                    <th>Cost</th>
                    <th>Correlation Id</th>
                    <th>Occurred</th>
                  </tr>
                </thead>
                <tbody>
                  @for (e of filtered(); track e.id) {
                    <tr>
                      <td><strong>{{ e.useCase }}</strong></td>
                      <td>{{ e.modelProvider || '—' }}</td>
                      <td>{{ e.modelName || '—' }}</td>
                      <td><span class="ai-chip">{{ e.outcome }}</span></td>
                      <td>{{ e.latencyMs == null ? '—' : e.latencyMs + ' ms' }}</td>
                      <td>—</td>
                      <td class="ai-muted">Future</td>
                      <td class="ai-trace">{{ e.traceId }}</td>
                      <td>{{ e.occurredAtUtc | date: 'medium' }}</td>
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
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        align-items: center;
        margin-bottom: 12px;
      }
      .ai-toolbar__search {
        width: min(320px, 100%);
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
        white-space: nowrap;
      }
      .ai-chip {
        display: inline-block;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        background: #f3f4f6;
        color: #374151;
      }
      .ai-muted {
        color: var(--color-text-secondary, #6b7280);
        font-size: 12px;
      }
      .ai-trace {
        font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
        font-size: 11px;
        word-break: break-all;
        max-width: 180px;
      }
    `,
  ],
})
export class ExecutionHistoryComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);
  readonly executions = signal<AIExecutionItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly totalCount = signal(0);

  readonly filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    if (!q) return this.executions();
    return this.executions().filter(
      (e) =>
        e.useCase?.toLowerCase().includes(q) ||
        e.modelProvider?.toLowerCase().includes(q) ||
        e.modelName?.toLowerCase().includes(q) ||
        e.outcome?.toLowerCase().includes(q) ||
        e.traceId?.toLowerCase().includes(q),
    );
  });

  readonly hasMore = computed(() => this.page() * this.pageSize < this.totalCount());

  ngOnInit(): void {
    this.reload();
  }

  prevPage(): void {
    if (this.page() <= 1) return;
    this.page.update((p) => p - 1);
    this.reload();
  }

  nextPage(): void {
    if (!this.hasMore()) return;
    this.page.update((p) => p + 1);
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listExecutions(this.page(), this.pageSize).subscribe({
      next: (d) => {
        this.executions.set(d.items ?? []);
        this.totalCount.set(d.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => {
        this.executions.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
        this.error.set('Unable to load execution history.');
      },
    });
  }
}
