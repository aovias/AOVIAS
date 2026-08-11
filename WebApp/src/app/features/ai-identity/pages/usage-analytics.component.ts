import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { catchError, forkJoin, of } from 'rxjs';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import { AiStatCardComponent } from '../components/ai-stat-card.component';
import { categorizePromptLayer } from '../models/ai-admin-nav.model';
import { AiIdentityApiService, AIAnalytics, AIExecutionItem, AIPromptItem } from '../services/ai-identity-api.service';

interface DistRow {
  label: string;
  count: number;
  pct: number;
}

@Component({
  selector: 'app-usage-analytics',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    SettingsPageHeaderComponent,
    AiAdminPageStateComponent,
    AiStatCardComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="Usage & Analytics"
        description="Daily usage, provider and model distribution, prompt categories, success rate, and latency from existing AI telemetry."
      />

      @if (loading()) {
        <app-ai-admin-page-state state="loading" message="Loading analytics…" [showRetry]="false" />
      } @else if (error()) {
        <app-ai-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
      } @else {
        <section class="ai-stats" aria-label="Usage metrics">
          <app-ai-stat-card label="Total executions" [value]="stats()?.totalExecutions ?? '—'" />
          <app-ai-stat-card label="Token consumption" [value]="stats()?.totalTokens ?? '—'" />
          <app-ai-stat-card label="Avg latency" [value]="avgLatencyLabel()" />
          <app-ai-stat-card label="Success rate" [value]="successRateLabel()" />
          <app-ai-stat-card label="Failure rate" [value]="failureRateLabel()" />
          <app-ai-stat-card label="Cost estimate" [value]="stats()?.estimatedCost ?? '—'" />
        </section>

        <div class="ai-grid">
          <mat-card class="ai-page__card">
            <mat-card-header>
              <mat-card-title>Daily Usage</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (!dailyUsage().length) {
                <p class="ai-muted">No daily usage samples in the recent execution window.</p>
              } @else {
                <ul class="ai-bars">
                  @for (row of dailyUsage(); track row.label) {
                    <li>
                      <span>{{ row.label }}</span>
                      <div class="ai-bars__track">
                        <div class="ai-bars__fill" [style.width.%]="row.pct"></div>
                      </div>
                      <strong>{{ row.count }}</strong>
                    </li>
                  }
                </ul>
              }
            </mat-card-content>
          </mat-card>

          <mat-card class="ai-page__card">
            <mat-card-header>
              <mat-card-title>Provider Distribution</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (!providerDist().length) {
                <p class="ai-muted">No provider distribution available.</p>
              } @else {
                <ul class="ai-bars">
                  @for (row of providerDist(); track row.label) {
                    <li>
                      <span>{{ row.label }}</span>
                      <div class="ai-bars__track">
                        <div class="ai-bars__fill" [style.width.%]="row.pct"></div>
                      </div>
                      <strong>{{ row.count }}</strong>
                    </li>
                  }
                </ul>
              }
            </mat-card-content>
          </mat-card>

          <mat-card class="ai-page__card">
            <mat-card-header>
              <mat-card-title>Model Distribution</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (!modelDist().length) {
                <p class="ai-muted">No model distribution available.</p>
              } @else {
                <ul class="ai-bars">
                  @for (row of modelDist(); track row.label) {
                    <li>
                      <span>{{ row.label }}</span>
                      <div class="ai-bars__track">
                        <div class="ai-bars__fill" [style.width.%]="row.pct"></div>
                      </div>
                      <strong>{{ row.count }}</strong>
                    </li>
                  }
                </ul>
              }
            </mat-card-content>
          </mat-card>

          <mat-card class="ai-page__card">
            <mat-card-header>
              <mat-card-title>Prompt Categories</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (!promptDist().length) {
                <p class="ai-muted">No prompt categories available.</p>
              } @else {
                <ul class="ai-bars">
                  @for (row of promptDist(); track row.label) {
                    <li>
                      <span>{{ row.label }}</span>
                      <div class="ai-bars__track">
                        <div class="ai-bars__fill" [style.width.%]="row.pct"></div>
                      </div>
                      <strong>{{ row.count }}</strong>
                    </li>
                  }
                </ul>
              }
            </mat-card-content>
          </mat-card>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .ai-page {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .ai-stats {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 12px;
      }
      .ai-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
        gap: 12px;
      }
      .ai-page__card {
        border-radius: 16px;
      }
      .ai-muted {
        color: var(--color-text-secondary, #6b7280);
        font-size: 13px;
      }
      .ai-bars {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 10px;
      }
      .ai-bars li {
        display: grid;
        grid-template-columns: minmax(80px, 34%) 1fr auto;
        gap: 8px;
        align-items: center;
        font-size: 12px;
      }
      .ai-bars__track {
        height: 8px;
        border-radius: 999px;
        background: #f3f4f6;
        overflow: hidden;
      }
      .ai-bars__fill {
        height: 100%;
        background: var(--color-primary);
        border-radius: inherit;
      }
    `,
  ],
})
export class UsageAnalyticsComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);
  readonly stats = signal<AIAnalytics | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly dailyUsage = signal<DistRow[]>([]);
  readonly providerDist = signal<DistRow[]>([]);
  readonly modelDist = signal<DistRow[]>([]);
  readonly promptDist = signal<DistRow[]>([]);
  readonly successRate = signal<number | null>(null);
  readonly failureRate = signal<number | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  avgLatencyLabel(): string {
    const ms = this.stats()?.avgLatencyMs;
    return typeof ms === 'number' ? `${ms} ms` : '—';
  }

  successRateLabel(): string {
    const rate = this.successRate();
    if (rate == null && this.stats()) {
      return `${Math.round((this.stats()!.acceptanceRate || 0) * 100)}%`;
    }
    return rate == null ? '—' : `${Math.round(rate * 100)}%`;
  }

  failureRateLabel(): string {
    const rate = this.failureRate();
    return rate == null ? '—' : `${Math.round(rate * 100)}%`;
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      analytics: this.api.getAnalytics().pipe(catchError(() => of(null))),
      executions: this.api.listExecutions(1, 100).pipe(catchError(() => of(null))),
      prompts: this.api.listPrompts().pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ analytics, executions, prompts }) => {
        this.stats.set(analytics);
        const items = executions?.items ?? [];
        this.dailyUsage.set(this.buildDaily(items));
        this.providerDist.set(this.buildDist(items.map((i) => i.modelProvider || 'Unknown')));
        this.modelDist.set(
          this.buildDist(items.map((i) => i.modelName || i.useCase || 'Unknown')),
        );
        this.promptDist.set(
          this.buildDist((prompts ?? []).map((p: AIPromptItem) => categorizePromptLayer(p.layerType))),
        );
        if (items.length) {
          const failed = items.filter((i) => /fail|error/i.test(i.outcome || '')).length;
          this.failureRate.set(failed / items.length);
          this.successRate.set(1 - failed / items.length);
        } else {
          this.failureRate.set(null);
          this.successRate.set(analytics ? analytics.acceptanceRate : null);
        }
        this.loading.set(false);
      },
      error: () => {
        this.stats.set(null);
        this.loading.set(false);
        this.error.set('Unable to load usage analytics.');
      },
    });
  }

  private buildDaily(items: AIExecutionItem[]): DistRow[] {
    const map = new Map<string, number>();
    for (const item of items) {
      const d = new Date(item.occurredAtUtc);
      if (Number.isNaN(d.getTime())) continue;
      const key = d.toISOString().slice(0, 10);
      map.set(key, (map.get(key) ?? 0) + 1);
    }
    const rows = Array.from(map.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .slice(-7)
      .map(([label, count]) => ({ label, count, pct: 0 }));
    const max = Math.max(...rows.map((r) => r.count), 1);
    return rows.map((r) => ({ ...r, pct: Math.round((r.count / max) * 100) }));
  }

  private buildDist(labels: string[]): DistRow[] {
    const map = new Map<string, number>();
    for (const label of labels) {
      const key = (label || 'Unknown').trim() || 'Unknown';
      map.set(key, (map.get(key) ?? 0) + 1);
    }
    const total = Array.from(map.values()).reduce((a, b) => a + b, 0) || 1;
    return Array.from(map.entries())
      .map(([label, count]) => ({
        label,
        count,
        pct: Math.round((count / total) * 100),
      }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 8);
  }
}
