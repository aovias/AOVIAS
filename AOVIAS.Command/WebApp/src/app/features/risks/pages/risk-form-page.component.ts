import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideTriangleAlert } from '@ng-icons/lucide';
import { RiskConfigurationService } from '../../../core/configuration/risk-configuration.service';
import { RiskApiService } from '../services/risk-api.service';
import type { CreateRiskRequest, RiskStatus } from '../models/risk.models';

@Component({
  selector: 'app-risk-form-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NgIcon],
  viewProviders: [provideIcons({ lucideArrowLeft, lucideTriangleAlert })],
  template: `
    <div class="aov-page risk-form-page">
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <a class="risk-form__back" routerLink="/app/risks/register">
            <ng-icon name="lucideArrowLeft" aria-hidden="true" />
            Register
          </a>
          <h1 class="aov-page__title">{{ isEdit() ? 'Edit risk' : 'Create risk' }}</h1>
        </div>
      </header>

      @if (error()) {
        <p class="aov-alert aov-alert--danger" role="alert">
          <ng-icon name="lucideTriangleAlert" aria-hidden="true" />
          {{ error() }}
        </p>
      }

      <form class="aov-card risk-form" (ngSubmit)="save()">
        <div class="aov-field">
          <label class="aov-label aov-label--required" for="risk-title">Title</label>
          <input
            id="risk-title"
            class="aov-input"
            [(ngModel)]="model.title"
            name="title"
            required
            maxlength="250"
          />
        </div>

        <div class="aov-field">
          <label class="aov-label" for="risk-category">Category</label>
          <input
            id="risk-category"
            class="aov-input"
            [(ngModel)]="model.category"
            name="category"
            placeholder="Clinical, Operational…"
          />
        </div>

        <div class="aov-field">
          <label class="aov-label" for="risk-description">Description</label>
          <textarea
            id="risk-description"
            class="aov-textarea"
            [(ngModel)]="model.description"
            name="description"
            rows="3"
          ></textarea>
        </div>

        <fieldset class="risk-form__group">
          <legend class="aov-text-micro">Inherent risk</legend>
          <div class="risk-form__row">
            <div class="aov-field">
              <label class="aov-label" for="risk-likelihood">Likelihood ({{ scaleHint() }})</label>
              <input
                id="risk-likelihood"
                class="aov-input"
                type="number"
                [min]="minLevel()"
                [max]="maxLevel()"
                [(ngModel)]="model.likelihood"
                name="likelihood"
              />
            </div>
            <div class="aov-field">
              <label class="aov-label" for="risk-impact">Impact ({{ scaleHint() }})</label>
              <input
                id="risk-impact"
                class="aov-input"
                type="number"
                [min]="minLevel()"
                [max]="maxLevel()"
                [(ngModel)]="model.impact"
                name="impact"
              />
            </div>
            <p class="risk-form__score">
              <span class="aov-text-micro">Score</span>
              <strong class="aov-num">{{ score }}</strong>
            </p>
          </div>
        </fieldset>

        <fieldset class="risk-form__group">
          <legend class="aov-text-micro">Residual risk</legend>
          <div class="risk-form__row">
            <div class="aov-field">
              <label class="aov-label" for="risk-residual-likelihood">Residual likelihood</label>
              <input
                id="risk-residual-likelihood"
                class="aov-input"
                type="number"
                [min]="minLevel()"
                [max]="maxLevel()"
                [(ngModel)]="model.residualLikelihood"
                name="rl"
              />
            </div>
            <div class="aov-field">
              <label class="aov-label" for="risk-residual-impact">Residual impact</label>
              <input
                id="risk-residual-impact"
                class="aov-input"
                type="number"
                [min]="minLevel()"
                [max]="maxLevel()"
                [(ngModel)]="model.residualImpact"
                name="ri"
              />
            </div>
            <p class="risk-form__score">
              <span class="aov-text-micro">Residual</span>
              <strong class="aov-num">{{ residualScore ?? '—' }}</strong>
            </p>
          </div>
        </fieldset>

        <div class="aov-field">
          <label class="aov-label" for="risk-owner">Owner name</label>
          <input id="risk-owner" class="aov-input" [(ngModel)]="model.ownerName" name="ownerName" />
        </div>

        <div class="aov-field">
          <label class="aov-label" for="risk-mitigation">Mitigation plan</label>
          <textarea
            id="risk-mitigation"
            class="aov-textarea"
            [(ngModel)]="model.mitigationPlan"
            name="mitigationPlan"
            rows="4"
          ></textarea>
        </div>

        <div class="aov-field">
          <label class="aov-label" for="risk-review-due">Review due date</label>
          <input
            id="risk-review-due"
            class="aov-input risk-form__date"
            type="date"
            [(ngModel)]="reviewDueLocal"
            name="reviewDue"
          />
        </div>

        @if (isEdit()) {
          <div class="aov-field">
            <label class="aov-label" for="risk-status">Status</label>
            <select id="risk-status" class="aov-select" [(ngModel)]="model.status" name="status">
              <option value="identified">Identified</option>
              <option value="assessed">Assessed</option>
              <option value="mitigating">Mitigating</option>
              <option value="underReview">Under review</option>
              <option value="closed">Closed</option>
            </select>
          </div>
        }

        <div class="risk-form__actions">
          <a class="aov-btn aov-btn--secondary" routerLink="/app/risks/register">Cancel</a>
          <button
            class="aov-btn aov-btn--primary"
            [class.aov-btn--loading]="saving()"
            type="submit"
            [disabled]="saving()"
          >
            {{ saving() ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }

      /* Forms read best on a measured column, not the full page width. */
      .risk-form-page {
        max-inline-size: 48rem;
        margin-inline: 0;
      }

      .risk-form__back {
        display: inline-flex;
        align-items: center;
        gap: var(--space-1);
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-medium);
        color: var(--primary-color);
        text-decoration: none;
      }

      .risk-form__back:hover {
        text-decoration: underline;
      }

      .risk-form__back ng-icon {
        font-size: var(--icon-size-md);
      }

      .risk-form {
        gap: var(--form-row-gap);
      }

      .risk-form__group {
        display: flex;
        flex-direction: column;
        gap: var(--space-3);
        padding: var(--space-3) 0 0;
        margin: 0;
        border: 0;
        border-block-start: var(--border-width-hairline) solid var(--border-color);
      }

      .risk-form__group legend {
        padding: 0;
      }

      .risk-form__row {
        display: grid;
        grid-template-columns: 1fr 1fr auto;
        gap: var(--form-row-gap);
        align-items: end;
      }

      .risk-form__score {
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
        margin: 0;
        min-inline-size: 5rem;
        padding-block-end: var(--space-2);
      }

      .risk-form__score strong {
        font-size: var(--font-size-section-title);
        color: var(--text-primary);
      }

      /* Native date controls render their own picker glyph in the theme colour. */
      .risk-form__date {
        color-scheme: light dark;
        max-inline-size: 14rem;
      }

      .risk-form__actions {
        display: flex;
        gap: var(--button-gap);
        justify-content: flex-end;
        padding-block-start: var(--space-2);
        border-block-start: var(--border-width-hairline) solid var(--border-color);
      }

      @media (max-width: 767px) {
        .risk-form__row {
          grid-template-columns: 1fr;
          align-items: stretch;
        }

        .risk-form__actions {
          flex-direction: column-reverse;
        }

        .risk-form__actions .aov-btn {
          inline-size: 100%;
        }
      }
    `,
  ],
})
export class RiskFormPageComponent implements OnInit {
  private readonly api = inject(RiskApiService);
  private readonly risk = inject(RiskConfigurationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  private riskId: string | null = null;
  reviewDueLocal = '';

  model: CreateRiskRequest & { status?: RiskStatus } = {
    title: '',
    likelihood: 3,
    impact: 3,
    severity: 'major' as any,
  };

  /**
   * Scale bounds come from configuration, so an organization on a 3×3 matrix cannot enter a 5.
   * The inputs previously carried `min="1" max="5"` in four places.
   */
  protected readonly minLevel = this.risk.minimumLevel;
  protected readonly maxLevel = this.risk.maximumLikelihood;

  /** "1–5" for the platform default; whatever the configured scale actually spans otherwise. */
  protected readonly scaleHint = computed(() => `${this.minLevel()}–${this.maxLevel()}`);

  get score(): number {
    return this.risk.score(Number(this.model.likelihood), Number(this.model.impact)) ?? 0;
  }

  get residualScore(): number | null {
    return this.risk.score(this.model.residualLikelihood, this.model.residualImpact);
  }

  /** Band label for the inherent score — the user sees "High", not just a number. */
  get scoreBand(): string {
    return this.risk.bandLabel(this.score);
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.riskId = id;
      this.isEdit.set(true);
      this.api.getById(id).subscribe({
        next: (risk) => {
          if (risk.status === 'Closed') {
            this.error.set('Closed risks are immutable.');
            return;
          }
          this.model = {
            title: risk.title,
            description: risk.description ?? undefined,
            category: risk.category ?? undefined,
            severity: (risk.severity?.charAt(0).toLowerCase() + risk.severity?.slice(1)) as any,
            likelihood: risk.likelihood,
            impact: risk.impact,
            residualLikelihood: risk.residualLikelihood ?? undefined,
            residualImpact: risk.residualImpact ?? undefined,
            ownerName: risk.ownerName ?? undefined,
            mitigationPlan: risk.mitigationPlan ?? undefined,
            status: this.toApiStatus(risk.status),
          };
          this.reviewDueLocal = risk.reviewDueDate ? risk.reviewDueDate.slice(0, 10) : '';
        },
        error: () => this.error.set('Failed to load risk.'),
      });
    }
  }

  save(): void {
    if (!this.model.title?.trim()) {
      this.error.set('Title is required.');
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    const payload: CreateRiskRequest & { status?: RiskStatus } = {
      ...this.model,
      reviewDueDate: this.reviewDueLocal ? new Date(this.reviewDueLocal).toISOString() : undefined,
    };

    const req$ =
      this.isEdit() && this.riskId
        ? this.api.update(this.riskId, payload)
        : this.api.create(payload);

    req$.subscribe({
      next: () => this.router.navigateByUrl('/app/risks/register'),
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.error?.message || err?.message || 'Save failed.');
      },
    });
  }

  private toApiStatus(status: string): RiskStatus {
    const map: Record<string, RiskStatus> = {
      Identified: 'identified' as RiskStatus,
      Assessed: 'assessed' as RiskStatus,
      Mitigating: 'mitigating' as RiskStatus,
      UnderReview: 'underReview' as RiskStatus,
      Closed: 'closed' as RiskStatus,
    };
    return map[status] ?? ('identified' as RiskStatus);
  }
}
