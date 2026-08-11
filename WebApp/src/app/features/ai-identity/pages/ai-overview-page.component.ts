import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { catchError, forkJoin, of } from 'rxjs';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { SettingsComingSoonBadgeComponent } from '../../settings/components/settings-coming-soon-badge.component';
import { AiStatCardComponent } from '../components/ai-stat-card.component';
import { AiAdminPageStateComponent } from '../components/ai-admin-page-state.component';
import {
  getAiAdminCapabilityCards,
  getAiAdminFuturePlaceholders,
} from '../models/ai-admin-nav.model';
import { AiIdentityApiService } from '../services/ai-identity-api.service';

interface OverviewStats {
  configuredProviders: string;
  availableModels: string;
  activeIdentities: string;
  promptTemplates: string;
  todaysExecutions: string;
  successRate: string;
  avgResponseTime: string;
  tokenConsumption: string;
  recentErrors: string;
}

@Component({
  selector: 'app-ai-overview-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    SettingsComingSoonBadgeComponent,
    AiStatCardComponent,
    AiAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-overview">
      <app-settings-page-header
        title="AI Dashboard"
        description="Enterprise overview of governed AI identities, models, providers, prompts, executions, and usage."
      />

      @if (loading()) {
        <app-ai-admin-page-state state="loading" message="Loading dashboard metrics…" [showRetry]="false" />
      } @else {
        <section class="ai-overview__stats" aria-label="AI platform statistics">
          <app-ai-stat-card
            label="Configured Providers"
            [value]="stats().configuredProviders"
            [placeholder]="stats().configuredProviders === '—'"
          />
          <app-ai-stat-card
            label="Available Models"
            [value]="stats().availableModels"
            [placeholder]="stats().availableModels === '—'"
          />
          <app-ai-stat-card
            label="Active AI Identities"
            [value]="stats().activeIdentities"
            [placeholder]="stats().activeIdentities === '—'"
          />
          <app-ai-stat-card
            label="Prompt Templates"
            [value]="stats().promptTemplates"
            [placeholder]="stats().promptTemplates === '—'"
          />
          <app-ai-stat-card
            label="Today's Executions"
            [value]="stats().todaysExecutions"
            [placeholder]="stats().todaysExecutions === '—'"
          />
          <app-ai-stat-card
            label="Success Rate"
            [value]="stats().successRate"
            [placeholder]="stats().successRate === '—'"
          />
          <app-ai-stat-card
            label="Average Response Time"
            [value]="stats().avgResponseTime"
            [placeholder]="stats().avgResponseTime === '—'"
          />
          <app-ai-stat-card
            label="Token Consumption"
            [value]="stats().tokenConsumption"
            [placeholder]="stats().tokenConsumption === '—'"
          />
          <app-ai-stat-card
            label="Recent Errors"
            [value]="stats().recentErrors"
            [placeholder]="stats().recentErrors === '—'"
          />
        </section>
      }

      <section class="ai-overview__cards" aria-label="AI administration areas">
        @for (card of capabilityCards; track card.label) {
          <a class="ai-capability-card" [routerLink]="card.route">
            <mat-icon aria-hidden="true">{{ card.icon }}</mat-icon>
            <div>
              <strong>{{ card.label }}</strong>
              <span>{{ card.description }}</span>
            </div>
          </a>
        }
      </section>

      <section class="ai-overview__future" aria-label="Future AI capabilities">
        <h2 class="ai-overview__future-title">Future Capabilities</h2>
        <div class="ai-overview__cards">
          @for (card of futureCards; track card.label) {
            <div class="ai-capability-card ai-capability-card--soon">
              <mat-icon aria-hidden="true">{{ card.icon }}</mat-icon>
              <div>
                <div class="ai-capability-card__title">
                  <strong>{{ card.label }}</strong>
                  <app-settings-coming-soon-badge />
                </div>
                <span>{{ card.description }}</span>
              </div>
            </div>
          }
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .ai-overview {
        display: flex;
        flex-direction: column;
        gap: 24px;
      }
      .ai-overview__stats {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
        gap: 12px;
      }
      .ai-overview__cards {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: 12px;
      }
      .ai-overview__future-title {
        margin: 0 0 12px;
        font-size: 16px;
        font-weight: 600;
        color: var(--color-text-primary, #111827);
      }
      .ai-capability-card {
        display: flex;
        gap: 12px;
        align-items: flex-start;
        padding: 16px;
        border-radius: 16px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
        text-decoration: none;
        color: inherit;
        transition: border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease;
      }
      .ai-capability-card:hover:not(.ai-capability-card--soon) {
        border-color: color-mix(in srgb, var(--color-primary) 40%, transparent);
        box-shadow: 0 4px 16px rgba(15, 23, 42, 0.06);
        transform: translateY(-1px);
      }
      .ai-capability-card mat-icon {
        color: var(--color-primary);
      }
      .ai-capability-card strong {
        display: block;
        font-size: 14px;
        margin-bottom: 4px;
      }
      .ai-capability-card span {
        display: block;
        font-size: 12px;
        line-height: 1.4;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-capability-card__title {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
        margin-bottom: 4px;
      }
      .ai-capability-card__title strong {
        margin-bottom: 0;
      }
      .ai-capability-card--soon {
        opacity: 0.92;
      }
    `,
  ],
})
export class AiOverviewPageComponent implements OnInit {
  private readonly api = inject(AiIdentityApiService);

  readonly capabilityCards = getAiAdminCapabilityCards();
  readonly futureCards = getAiAdminFuturePlaceholders();
  readonly loading = signal(true);
  readonly stats = signal<OverviewStats>(this.emptyStats());

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    forkJoin({
      profiles: this.api.listProfiles().pipe(catchError(() => of(null))),
      prompts: this.api.listPrompts().pipe(catchError(() => of(null))),
      executions: this.api.listExecutions(1, 100).pipe(catchError(() => of(null))),
      analytics: this.api.getAnalytics().pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ profiles, prompts, executions, analytics }) => {
        const activeIdentities = profiles?.filter((p) => p.status?.toLowerCase() === 'active').length;
        const items = executions?.items ?? [];
        const todaysExecutions = items.filter((e) => {
          const occurred = new Date(e.occurredAtUtc);
          return !Number.isNaN(occurred.getTime()) && occurred >= today;
        }).length;
        const providers = new Set(
          items.map((e) => (e.modelProvider || '').trim()).filter(Boolean),
        );
        const models = new Set(
          items
            .map((e) => `${(e.modelProvider || '').trim()}::${(e.modelName || e.useCase || '').trim()}`)
            .filter((v) => !v.startsWith('::') && v !== '::'),
        );
        const recentErrors = items.filter((e) => /fail|error/i.test(e.outcome || '')).length;
        const latencyValues = items
          .map((e) => e.latencyMs)
          .filter((v): v is number => typeof v === 'number' && Number.isFinite(v));
        const avgLatency =
          latencyValues.length > 0
            ? Math.round(latencyValues.reduce((a, b) => a + b, 0) / latencyValues.length)
            : analytics?.avgLatencyMs;

        this.stats.set({
          configuredProviders: executions ? String(providers.size) : '—',
          availableModels: executions ? String(models.size) : '—',
          activeIdentities: profiles ? String(activeIdentities ?? 0) : '—',
          promptTemplates: prompts ? String(prompts.length) : '—',
          todaysExecutions: executions ? String(todaysExecutions) : '—',
          successRate:
            analytics && typeof analytics.acceptanceRate === 'number'
              ? `${Math.round(analytics.acceptanceRate * 100)}%`
              : '—',
          avgResponseTime:
            typeof avgLatency === 'number' ? `${avgLatency} ms` : '—',
          tokenConsumption:
            analytics && typeof analytics.totalTokens === 'number'
              ? String(analytics.totalTokens)
              : '—',
          recentErrors: executions ? String(recentErrors) : '—',
        });
        this.loading.set(false);
      },
      error: () => {
        this.stats.set(this.emptyStats());
        this.loading.set(false);
      },
    });
  }

  private emptyStats(): OverviewStats {
    return {
      configuredProviders: '—',
      availableModels: '—',
      activeIdentities: '—',
      promptTemplates: '—',
      todaysExecutions: '—',
      successRate: '—',
      avgResponseTime: '—',
      tokenConsumption: '—',
      recentErrors: '—',
    };
  }
}
