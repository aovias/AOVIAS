import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideTriangleAlert } from '@ng-icons/lucide';
import { RiskConfigurationService } from '../../../core/configuration/risk-configuration.service';
import { RiskApiService } from '../services/risk-api.service';
import type { RiskMatrixCell } from '../models/risk.models';

@Component({
  selector: 'app-risk-matrix-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NgIcon],
  viewProviders: [provideIcons({ lucideTriangleAlert })],
  template: `
    <div class="aov-page">
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Risk matrix</h1>
          <p class="aov-page__subtitle">Likelihood × Impact heatmap for active register risks.</p>
        </div>
        <div class="aov-page__actions">
          <label class="aov-check">
            <input type="checkbox" [(ngModel)]="useResidual" (ngModelChange)="load()" />
            Use residual scores
          </label>
        </div>
      </header>

      <div class="matrix-kpis">
        @for (kpi of kpis(); track kpi.label) {
          <div class="aov-card aov-card--compact matrix-kpi">
            <span class="aov-text-micro">{{ kpi.label }}</span>
            <strong class="matrix-kpi__value aov-num">{{ kpi.value }}</strong>
          </div>
        }
      </div>

      @if (error()) {
        <p class="aov-alert aov-alert--danger" role="alert">
          <ng-icon name="lucideTriangleAlert" aria-hidden="true" />
          {{ error() }}
        </p>
      }

      <section class="aov-card matrix-panel" aria-labelledby="matrix-heading">
        <h2 id="matrix-heading" class="aov-card__title">
          {{ useResidual ? 'Residual' : 'Inherent' }} risk heatmap
        </h2>

        <div class="matrix-scroll praman-scrollbar praman-scrollbar--timeline">
          <div class="matrix">
            <span class="matrix__axis matrix__axis--corner" aria-hidden="true"></span>
            @for (impact of impacts(); track impact) {
              <span class="matrix__axis">I{{ impact }}</span>
            }
            @for (likelihood of likelihoodsDesc(); track likelihood) {
              <span class="matrix__axis">L{{ likelihood }}</span>
              @for (impact of impacts(); track impact) {
                @let cell = cellAt(likelihood, impact);
                <button
                  type="button"
                  class="matrix__cell"
                  [attr.data-band]="cell?.band || 'Low'"
                  [attr.aria-pressed]="isSelected(likelihood, impact)"
                  [attr.aria-label]="cellLabel(likelihood, impact, cell)"
                  [title]="cellTitle(cell)"
                  (click)="select(cell)"
                >
                  <span class="matrix__count aov-num">{{ cell?.count || 0 }}</span>
                  <span class="matrix__score aov-num">
                    {{ cell?.score || scoreFor(likelihood, impact) }}
                  </span>
                </button>
              }
            }
          </div>
        </div>

        <p class="matrix__legend aov-text-caption">
          Rows are likelihood (L5 highest), columns are impact (I5 highest). Select a cell to list
          its risks.
        </p>
      </section>

      @if (selected(); as sel) {
        <section class="aov-card matrix-detail" aria-live="polite">
          <div class="aov-card__header">
            <h2 class="aov-card__title">
              L{{ sel.likelihood }} × I{{ sel.impact }} —
              <span class="aov-num">{{ sel.count }}</span> risk(s)
            </h2>
            <span class="aov-chip" [class]="bandChipClass(sel.band)">{{ sel.band }}</span>
          </div>
          <ul class="matrix-detail__list">
            @for (r of sel.risks; track r.id) {
              <li class="matrix-detail__item">
                <a class="matrix-detail__link" [routerLink]="['/app/risks', r.id, 'edit']">
                  {{ r.title }}
                </a>
                <span class="aov-text-caption">
                  {{ r.status }} · <span class="aov-num">{{ r.riskScore }}</span>
                </span>
              </li>
            } @empty {
              <li class="aov-text-muted">No risks in this cell.</li>
            }
          </ul>
        </section>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }

      .matrix-kpis {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
        gap: var(--grid-gap);
      }

      .matrix-kpi {
        gap: var(--space-1);
      }

      .matrix-kpi__value {
        font-size: var(--font-size-heading-md);
        font-weight: var(--font-weight-bold);
        line-height: var(--line-height-tight);
        color: var(--text-primary);
      }

      .matrix-panel,
      .matrix-detail {
        max-inline-size: 44rem;
      }

      /* UI-12 — the grid scrolls inside its own container, never the page. */
      .matrix-scroll {
        overflow-x: auto;
        min-inline-size: 0;
      }

      .matrix {
        display: grid;
        grid-template-columns: 2.5rem repeat(5, minmax(3.5rem, 1fr));
        gap: var(--space-2);
        min-inline-size: 22rem;
      }

      .matrix__axis {
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: var(--font-size-micro);
        font-weight: var(--font-weight-semibold);
        letter-spacing: var(--letter-spacing-wide);
        color: var(--text-secondary);
      }

      .matrix__cell {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: var(--space-1);
        aspect-ratio: 1;
        padding: var(--space-1);
        border: var(--border-width-hairline) solid transparent;
        border-radius: var(--radius-control);
        cursor: pointer;
        transition: var(--transition-control);
        /* Defaults are overridden per band below. */
        background: var(--color-status-neutral-soft);
        color: var(--color-status-neutral-fg);
      }

      .matrix__cell[data-band='Low'] {
        background: var(--color-status-success-soft);
        color: var(--color-status-success-fg);
        border-color: var(--color-status-success-border);
      }

      .matrix__cell[data-band='Medium'] {
        background: var(--color-status-warning-soft);
        color: var(--color-status-warning-fg);
        border-color: var(--color-status-warning-border);
      }

      /* High and Critical share the danger family; Critical reads stronger. */
      .matrix__cell[data-band='High'] {
        background: color-mix(in srgb, var(--color-status-danger) 12%, transparent);
        color: var(--color-status-danger-fg);
        border-color: var(--color-status-danger-border);
      }

      .matrix__cell[data-band='Critical'] {
        background: color-mix(in srgb, var(--color-status-danger) 26%, transparent);
        color: var(--color-status-danger-fg);
        border-color: var(--color-status-danger);
      }

      .matrix__cell:hover {
        box-shadow: var(--elevation-card-hover);
      }

      .matrix__cell:focus-visible {
        outline: var(--focus-ring-width) solid var(--state-focus-ring-color);
        outline-offset: var(--focus-ring-offset);
      }

      .matrix__cell[aria-pressed='true'] {
        border-color: var(--primary-color);
        box-shadow: var(--shadow-focus-ring-primary);
      }

      .matrix__count {
        font-size: var(--font-size-section-title);
        font-weight: var(--font-weight-bold);
        line-height: 1;
      }

      .matrix__score {
        font-size: var(--font-size-micro);
        opacity: 0.85;
      }

      .matrix__legend {
        margin: 0;
      }

      .matrix-detail__list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: grid;
        gap: var(--space-2);
      }

      .matrix-detail__item {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: var(--space-3);
        padding-block: var(--space-2);
        border-block-end: var(--border-width-hairline) solid var(--border-color);
      }

      .matrix-detail__item:last-child {
        border-block-end: 0;
      }

      .matrix-detail__link {
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-semibold);
        color: var(--primary-color);
        text-decoration: none;
        min-inline-size: 0;
      }

      .matrix-detail__link:hover {
        text-decoration: underline;
      }

      @media (prefers-reduced-motion: reduce) {
        .matrix__cell {
          transition-duration: 0.01ms;
        }
      }
    `,
  ],
})
export class RiskMatrixPageComponent implements OnInit {
  private readonly api = inject(RiskApiService);
  private readonly risk = inject(RiskConfigurationService);

  readonly cells = signal<RiskMatrixCell[]>([]);
  readonly selected = signal<RiskMatrixCell | null>(null);
  readonly totalActive = signal(0);
  readonly highScore = signal(0);
  readonly reviewDue = signal(0);
  readonly error = signal<string | null>(null);

  useResidual = false;
  /**
   * The scale is configuration, not a literal. Removing two likelihood values makes this a 3×5
   * matrix with no code change; the axes, the cell scores and the bands all follow.
   */
  readonly impacts = this.risk.impacts;
  readonly likelihoodsDesc = this.risk.likelihoodsDescending;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error.set(null);
    this.api.matrix(this.useResidual).subscribe({
      next: (m) => {
        this.cells.set(m.cells ?? []);
        this.totalActive.set(m.totalActiveRisks ?? 0);
        this.highScore.set(m.highScoreCount ?? 0);
        this.reviewDue.set(m.reviewDueCount ?? 0);
      },
      error: () => this.error.set('Failed to load risk matrix.'),
    });
  }

  cellAt(likelihood: number, impact: number): RiskMatrixCell | undefined {
    return this.cells().find((c) => c.likelihood === likelihood && c.impact === impact);
  }

  cellTitle(cell?: RiskMatrixCell): string {
    if (!cell) return '';
    return `${cell.count} risk(s) · score ${cell.score} · ${cell.band}`;
  }

  select(cell?: RiskMatrixCell): void {
    if (cell) this.selected.set(cell);
  }

  protected kpis(): { label: string; value: number }[] {
    return [
      { label: 'Active', value: this.totalActive() },
      { label: 'High / Critical', value: this.highScore() },
      { label: 'Review due', value: this.reviewDue() },
    ];
  }

  protected isSelected(likelihood: number, impact: number): boolean {
    const sel = this.selected();
    return !!sel && sel.likelihood === likelihood && sel.impact === impact;
  }

  /** Accessible name for a heatmap cell — screen readers get counts, not colour. */
  protected cellLabel(likelihood: number, impact: number, cell?: RiskMatrixCell): string {
    const count = cell?.count ?? 0;
    const band = cell?.band ?? 'Low';
    return `Likelihood ${likelihood}, impact ${impact}: ${count} risk(s), ${band} band`;
  }

  /** Score for a cell the API did not return — an empty cell still shows its score. */
  protected scoreFor(likelihood: number, impact: number): number {
    return this.risk.score(likelihood, impact) ?? 0;
  }

  /**
   * Chip styling for a band.
   *
   * The tone comes from the band's configuration rather than a `switch` over band names, so a
   * tenant that renames Critical to Severe, or adds a fifth band, still gets the right colour.
   */
  protected bandChipClass(band: string): string {
    return `aov-chip--${this.risk.bandToneByLabel(band)}`;
  }
}
