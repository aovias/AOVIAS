import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChevronLeft,
  lucideChevronRight,
  lucideInbox,
  lucidePencil,
  lucidePlus,
  lucideRefreshCw,
  lucideSearch,
  lucideSparkles,
  lucideTriangleAlert,
} from '@ng-icons/lucide';
import {
  Phase5AiApiService,
  type PredictiveOverviewDto,
  type RiskClusterDto,
} from '../../aovias/services/phase5-ai-api.service';
import { RiskApiService } from '../services/risk-api.service';
import type { RiskSummary } from '../models/risk.models';

@Component({
  selector: 'app-risk-register-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NgIcon],
  viewProviders: [
    provideIcons({
      lucideSearch,
      lucidePlus,
      lucidePencil,
      lucideRefreshCw,
      lucideSparkles,
      lucideTriangleAlert,
      lucideInbox,
      lucideChevronLeft,
      lucideChevronRight,
    }),
  ],
  template: `
    <div class="aov-page">
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Enterprise Risk Register</h1>
          <p class="aov-page__subtitle">
            Hospital-level risks with owners, mitigation, and residual scoring. Distinct from
            Control Center <em>chapter compliance risk</em>.
          </p>
        </div>
        <div class="aov-page__actions">
          <a class="aov-btn aov-btn--secondary" routerLink="/app/risks/matrix">Matrix</a>
          <a class="aov-btn aov-btn--primary" routerLink="/app/risks/new">
            <ng-icon name="lucidePlus" aria-hidden="true" />
            New risk
          </a>
        </div>
      </header>

      <section class="aov-card risk-ai" aria-labelledby="risk-ai-heading">
        <div class="aov-card__header">
          <div class="risk-ai__heading">
            <ng-icon name="lucideSparkles" class="risk-ai__heading-icon" aria-hidden="true" />
            <h2 id="risk-ai-heading" class="aov-card__title">AI risk clusters &amp; readiness</h2>
          </div>
          <button
            type="button"
            class="aov-btn aov-btn--secondary aov-btn--sm"
            [class.aov-btn--loading]="aiLoading()"
            (click)="loadAi()"
            [disabled]="aiLoading()"
          >
            <ng-icon name="lucideRefreshCw" aria-hidden="true" />
            {{ aiLoading() ? 'Refreshing…' : 'Refresh AI' }}
          </button>
        </div>

        @if (readiness(); as r) {
          <p class="risk-ai__forecast">
            Readiness
            <strong class="aov-num">{{ r.currentReadinessPercent }}%</strong>
            →
            <strong class="aov-num">{{ r.forecastReadinessPercent }}%</strong>
            <span class="aov-text-caption">({{ r.horizon }}, {{ r.trend }})</span>
          </p>
        }

        @if (aiError()) {
          <p class="aov-alert aov-alert--warning" role="status">
            <ng-icon name="lucideTriangleAlert" aria-hidden="true" />
            {{ aiError() }}
          </p>
        }

        @if (aiClusters().length) {
          <div class="risk-ai__clusters">
            @for (c of aiClusters(); track c.theme) {
              <article class="risk-ai__cluster" [attr.data-severity]="c.severity">
                <div class="risk-ai__cluster-head">
                  <strong class="risk-ai__cluster-title">{{ c.theme }}</strong>
                  <span class="aov-chip" [class]="severityChipClass(c.severity)">
                    {{ c.severity }}
                  </span>
                </div>
                <p class="risk-ai__cluster-meta aov-text-caption">
                  <span class="aov-num">{{ c.signalCount }}</span> signals ·
                  <span class="aov-num">{{ c.confidence | number: '1.0-2' }}</span> confidence
                </p>
                <p class="risk-ai__cluster-summary">{{ c.summary }}</p>
              </article>
            }
          </div>
        } @else if (!aiLoading() && !aiError()) {
          <p class="aov-text-caption">No AI clusters yet. Refresh to analyze.</p>
        }
      </section>

      <section class="aov-page__section" aria-label="Risk register">
        <div class="aov-toolbar" role="search">
          <div class="aov-search">
            <ng-icon name="lucideSearch" class="aov-search__icon" aria-hidden="true" />
            <input
              id="risk-search"
              class="aov-input"
              type="search"
              [(ngModel)]="search"
              (ngModelChange)="reload()"
              placeholder="Search title, category, owner…"
              aria-label="Search risks"
            />
          </div>

          <select
            class="aov-select risk-filter__select"
            [(ngModel)]="status"
            (ngModelChange)="reload()"
            aria-label="Filter by status"
          >
            <option value="">All statuses</option>
            <option>Identified</option>
            <option>Assessed</option>
            <option>Mitigating</option>
            <option>UnderReview</option>
            <option>Closed</option>
          </select>

          <label class="aov-check">
            <input type="checkbox" [(ngModel)]="reviewDueOnly" (ngModelChange)="reload()" />
            Review due
          </label>

          <span class="aov-toolbar__spacer"></span>

          <span class="aov-text-caption">
            <strong class="aov-num">{{ total() }}</strong> total
          </span>
        </div>

        @if (error()) {
          <p class="aov-alert aov-alert--danger" role="alert">
            <ng-icon name="lucideTriangleAlert" aria-hidden="true" />
            {{ error() }}
          </p>
        }

        <div class="aov-table-wrap praman-scrollbar praman-scrollbar--dense">
          <table class="aov-table">
            <caption class="sr-only">
              Enterprise risks with score, owner, and review date
            </caption>
            <thead>
              <tr>
                <th scope="col">Title</th>
                <th scope="col">Category</th>
                <th scope="col">Status</th>
                <th scope="col" data-align="end">Score</th>
                <th scope="col" data-align="end">Residual</th>
                <th scope="col">Owner</th>
                <th scope="col">Review due</th>
                <th scope="col"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (risk of items(); track risk.id) {
                <tr>
                  <td>
                    <div class="risk-row__title">{{ risk.title }}</div>
                    <div class="risk-row__meta aov-text-caption">
                      {{ risk.severity }} · L{{ risk.likelihood }} × I{{ risk.impact }}
                    </div>
                  </td>
                  <td>{{ risk.category || '—' }}</td>
                  <td>
                    <span class="aov-chip" [class]="statusChipClass(risk.status)">
                      {{ risk.status }}
                    </span>
                  </td>
                  <td data-align="end">
                    <strong class="aov-num">{{ risk.riskScore }}</strong>
                  </td>
                  <td data-align="end" class="aov-num">{{ risk.residualRiskScore ?? '—' }}</td>
                  <td>{{ risk.ownerName || '—' }}</td>
                  <td class="aov-date">
                    {{ risk.reviewDueDate ? (risk.reviewDueDate | date: 'mediumDate') : '—' }}
                  </td>
                  <td>
                    @if (risk.status !== 'Closed') {
                      <a
                        class="aov-btn aov-btn--text aov-btn--sm"
                        [routerLink]="['/app/risks', risk.id, 'edit']"
                        [attr.aria-label]="'Edit ' + risk.title"
                      >
                        <ng-icon name="lucidePencil" aria-hidden="true" />
                        Edit
                      </a>
                    } @else {
                      <span class="aov-text-muted">Closed</span>
                    }
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="8">
                    <div class="aov-empty">
                      <ng-icon name="lucideInbox" class="aov-empty__icon" aria-hidden="true" />
                      <p class="aov-empty__title">No risks yet</p>
                      <p class="aov-empty__message">
                        Create the first register entry to start tracking hospital-level risk.
                      </p>
                      <div class="aov-empty__actions">
                        <a class="aov-btn aov-btn--primary aov-btn--sm" routerLink="/app/risks/new">
                          <ng-icon name="lucidePlus" aria-hidden="true" />
                          New risk
                        </a>
                      </div>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <nav class="risk-pager" aria-label="Risk register pages">
          <button
            type="button"
            class="aov-btn aov-btn--secondary aov-btn--sm"
            [disabled]="page <= 1"
            (click)="prev()"
          >
            <ng-icon name="lucideChevronLeft" aria-hidden="true" />
            Prev
          </button>
          <span class="aov-text-caption" aria-live="polite">
            Page <span class="aov-num">{{ page }}</span> of
            <span class="aov-num">{{ pageCount() }}</span>
          </span>
          <button
            type="button"
            class="aov-btn aov-btn--secondary aov-btn--sm"
            [disabled]="page * pageSize >= total()"
            (click)="next()"
          >
            Next
            <ng-icon name="lucideChevronRight" aria-hidden="true" />
          </button>
        </nav>
      </section>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }

      .sr-only {
        position: absolute;
        inline-size: 1px;
        block-size: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
        border: 0;
      }

      /* --- AI intelligence panel --- */

      .risk-ai__heading {
        display: flex;
        align-items: center;
        gap: var(--icon-gap);
        min-inline-size: 0;
      }

      .risk-ai__heading-icon {
        color: var(--primary-color);
        font-size: var(--icon-size-lg);
        flex-shrink: 0;
      }

      .risk-ai__forecast {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: var(--space-2);
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--text-secondary);
      }

      .risk-ai__forecast strong {
        color: var(--text-primary);
      }

      .risk-ai__clusters {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr));
        gap: var(--grid-gap);
        min-inline-size: 0;
      }

      .risk-ai__cluster {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        padding: var(--card-padding-compact);
        background: var(--bg-tertiary);
        border: var(--border-width-hairline) solid var(--border-color);
        border-radius: var(--radius-control);
        min-inline-size: 0;
      }

      /* Severity is carried by the border so the chip stays the only fill. */
      .risk-ai__cluster[data-severity='High'],
      .risk-ai__cluster[data-severity='Critical'] {
        border-color: var(--color-status-danger-border);
      }

      .risk-ai__cluster[data-severity='Medium'] {
        border-color: var(--color-status-warning-border);
      }

      .risk-ai__cluster-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-2);
      }

      .risk-ai__cluster-title {
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-semibold);
        color: var(--text-primary);
        min-inline-size: 0;
        overflow-wrap: anywhere;
      }

      .risk-ai__cluster-meta {
        margin: 0;
      }

      .risk-ai__cluster-summary {
        margin: 0;
        font-size: var(--font-size-caption);
        line-height: var(--line-height-normal);
        color: var(--text-secondary);
      }

      /* --- Filters --- */

      .risk-filter__select {
        inline-size: auto;
        min-inline-size: 11rem;
        block-size: var(--control-height-lg);
      }

      /* --- Table rows --- */

      .risk-row__title {
        font-weight: var(--font-weight-semibold);
        color: var(--text-primary);
      }

      .risk-row__meta {
        margin-block-start: var(--space-1);
      }

      /* --- Pagination --- */

      .risk-pager {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: var(--space-3);
      }

      /* --- Responsive (UI-12) --- */

      @media (max-width: 767px) {
        .risk-filter__select,
        .risk-pager {
          inline-size: 100%;
        }

        .risk-pager {
          justify-content: space-between;
        }
      }
    `,
  ],
})
export class RiskRegisterPageComponent implements OnInit {
  private readonly api = inject(RiskApiService);
  private readonly phase5Ai = inject(Phase5AiApiService);

  readonly items = signal<RiskSummary[]>([]);
  readonly total = signal(0);
  readonly error = signal<string | null>(null);
  readonly aiClusters = signal<RiskClusterDto[]>([]);
  readonly readiness = signal<PredictiveOverviewDto['readiness'] | null>(null);
  readonly aiLoading = signal(false);
  readonly aiError = signal<string | null>(null);

  search = '';
  status = '';
  reviewDueOnly = false;
  page = 1;
  pageSize = 25;

  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  ngOnInit(): void {
    this.reload();
    this.loadAi();
  }

  loadAi(): void {
    this.aiLoading.set(true);
    this.aiError.set(null);
    this.phase5Ai.analyzeRiskIntelligence({ maxClusters: 6 }).subscribe({
      next: (res) => {
        this.aiClusters.set(res.clusters ?? []);
        this.aiLoading.set(false);
      },
      error: () => {
        this.aiError.set('AI risk intelligence unavailable.');
        this.aiLoading.set(false);
      },
    });
    this.phase5Ai.predictOverview('monthly').subscribe({
      next: (res) => this.readiness.set(res.readiness ?? null),
      error: () => {},
    });
  }

  reload(): void {
    this.error.set(null);
    this.api
      .list({
        search: this.search || undefined,
        status: this.status || undefined,
        reviewDueOnly: this.reviewDueOnly || undefined,
        page: this.page,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.items.set(res.items ?? []);
          this.total.set(res.totalCount ?? 0);
        },
        error: () => this.error.set('Failed to load risk register.'),
      });
  }

  prev(): void {
    if (this.page > 1) {
      this.page -= 1;
      this.reload();
    }
  }

  next(): void {
    if (this.page * this.pageSize < this.total()) {
      this.page += 1;
      this.reload();
    }
  }

  /** Maps register status to the shared status-chip vocabulary (UI-10). */
  protected statusChipClass(status: string): string {
    switch (status) {
      case 'Closed':
        return '';
      case 'UnderReview':
        return 'aov-chip--warning';
      case 'Mitigating':
        return 'aov-chip--info';
      case 'Assessed':
        return 'aov-chip--primary';
      default:
        return 'aov-chip--primary';
    }
  }

  protected severityChipClass(severity: string): string {
    switch (severity) {
      case 'Critical':
      case 'High':
        return 'aov-chip--danger';
      case 'Medium':
        return 'aov-chip--warning';
      default:
        return 'aov-chip--success';
    }
  }
}
