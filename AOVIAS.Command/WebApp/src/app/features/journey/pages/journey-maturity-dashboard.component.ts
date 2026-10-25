import {
  ChangeDetectionStrategy,
  Component,
  inject,
  computed,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { combineLatest, of } from 'rxjs';
import { filter, switchMap, catchError } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { JourneyWorkspaceStore } from '../state/journey-workspace.store';
import { StageProgressStripComponent } from '../components/stage-progress-strip/stage-progress-strip.component';
import {
  maturityStageStatusLabel,
  type MaturityStageStateModel,
} from '../../../shared/models/maturity-stage.model';
import { DeficiencySummaryCardComponent } from '../components/deficiency-summary-card/deficiency-summary-card.component';
import { DeficiencyDataService } from '../services/deficiency-data.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import type { DeficiencySummaryApiDto } from '../models/deficiency-api.model';

/**
 * Compact execution launchpad — stage context and blocking deficiencies (Sprint 2A).
 * Governance analytics remain on Compliance Maturity.
 */
@Component({
  selector: 'app-journey-maturity-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, StageProgressStripComponent, DeficiencySummaryCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="journey-page">
      <div class="journey-page__main jmd">
      <header class="jmd__header">
        <h1 class="jmd__title">Execution Launchpad</h1>
        <p class="jmd__subtitle">
          Stage context and blocking deficiencies — launch daily compliance work from here.
        </p>
        <div class="jmd__header-actions">
          <a class="jmd__btn jmd__btn--primary" routerLink="/app/journey/execution">
            Open Execution Workspace
          </a>
          <a class="jmd__btn jmd__btn--secondary" routerLink="/app/compliance-maturity">
            Compliance Maturity
          </a>
          <a class="jmd__btn jmd__btn--secondary" routerLink="/app/tasks">Tasks</a>
          <a class="jmd__btn jmd__btn--secondary" routerLink="/app/evidence">Evidence</a>
          <a class="jmd__btn jmd__btn--secondary" routerLink="/app/audit/capa">
            Issues &amp; Risks
          </a>
        </div>
      </header>

      @if (store.loading()) {
        <section class="jmd__state-card" aria-busy="true">Loading journey workspace…</section>
      } @else if (store.loadError()) {
        <section class="jmd__state-card jmd__state-card--error">
          <p>{{ store.loadError() }}</p>
          <button type="button" class="jmd__btn jmd__btn--secondary" (click)="store.retryLoad()">Retry</button>
        </section>
      } @else {

      @if (maturityStages().length) {
        <section class="jmd__progress-strip" aria-label="Stage progress overview">
          <app-stage-progress-strip [stages]="maturityStages()" />
        </section>
      }

      <section class="jmd__overview">
        <div class="jmd__metric">
          <span class="jmd__metric-label">Compliance Readiness</span>
          <span class="jmd__metric-value">{{ complianceReadinessPercent() }}%</span>
        </div>
        <div class="jmd__metric">
          <span class="jmd__metric-label">Stage Readiness</span>
          <span class="jmd__metric-value">{{ stageReadinessPercent() | number:'1.0-0' }}%</span>
        </div>
        <div class="jmd__metric">
          <span class="jmd__metric-label">Current Stage</span>
          <span class="jmd__metric-value jmd__metric-value--stage">{{ activeStageLabel() }}</span>
        </div>
        <div class="jmd__metric">
          <span class="jmd__metric-label">Open Deficiencies</span>
          <span class="jmd__metric-value">{{ openDeficiencyCount() }}</span>
        </div>
      </section>

      @if (activeStageState(); as stage) {
        <section class="jmd__stage-context" aria-label="Current stage context">
          <h2 class="jmd__section-title">{{ stage.label }} — {{ stageBadgeLabel(stage.stage) }}</h2>
          @if (stage.blockingReasons?.length) {
            <ul class="jmd__blockers">
              @for (reason of stage.blockingReasons; track reason) {
                <li>{{ reason }}</li>
              }
            </ul>
          } @else {
            <p class="jmd__stage-hint">No blocking reasons for the current stage.</p>
          }
          @if (stage.status !== 'locked') {
            <a class="jmd__stage-cta"
               [routerLink]="['/app/journey/execution']"
               [queryParams]="{ stage: stage.stage }">
              Continue {{ stage.label }} work →
            </a>
          }
        </section>
      }

      @if (deficiencyLoaded() && blockingDeficiencySummary(); as summary) {
        <section class="jmd__def-section" aria-label="Blocking compliance deficiencies">
          <h2 class="jmd__section-title">Blocking Deficiencies</h2>
          <p class="jmd__section-subtitle">Active deficiencies requiring remediation before stage progression</p>
          <app-deficiency-summary-card [summary]="summary" stageLabel="All stages" />
          <a class="jmd__def-link" routerLink="/app/audit/capa">Open Issues &amp; Risks →</a>
        </section>
      } @else if (deficiencyLoading()) {
        <p class="jmd__def-loading">Loading deficiency data…</p>
      }

      }

      </div>
    </div>
  `,
  styles: [`
    .journey-page {
      position: relative;
      padding: var(--space-6);
    }

    .journey-page__main {
      display: grid;
      gap: var(--section-gap-y-loose);
      min-width: 0;
    }

    .jmd {
      display: flex;
      flex-direction: column;
      gap: var(--section-gap-y-loose);
    }

    .jmd__header {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .jmd__title {
      font-size: var(--font-size-heading-md);
      font-weight: var(--font-weight-bold);
      color: var(--color-text-primary);
      margin: 0;
    }

    .jmd__subtitle {
      font-size: var(--font-size-body);
      color: var(--color-text-secondary);
      margin: 0;
    }

    .jmd__header-actions {
      display: flex;
      gap: 12px;
      margin-top: 8px;
      flex-wrap: wrap;
    }

    .jmd__btn {
      padding: 8px 16px;
      border-radius: 6px;
      font-size: 13px;
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
      transition: opacity 0.15s;
    }

    .jmd__btn--secondary {
      border: var(--border-width-hairline) solid var(--color-border-subtle);
      color: var(--color-text-secondary);
      background: var(--color-bg-surface);
    }

    .jmd__btn--secondary:hover {
      opacity: 0.75;
    }

    .jmd__overview {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 16px;
    }

    .jmd__metric {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 10px;
      padding: 20px 24px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .jmd__metric-label {
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--text-muted);
    }

    .jmd__metric-value {
      font-size: 28px;
      font-weight: 700;
      color: var(--text-primary);
    }

    .jmd__metric-value--stage {
      font-size: 20px;
      text-transform: capitalize;
    }

    .jmd__section-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 16px;
    }

    .jmd__stage-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 20px;
    }

    .jmd__stage-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 12px;
      transition: box-shadow 0.2s, transform 0.2s;
    }

    .jmd__stage-card--active {
      border-color: var(--primary-color);
      box-shadow: 0 0 0 2px rgba(var(--primary-color-rgb, 37, 99, 235), 0.15);
    }

    .jmd__stage-card--complete {
      border-color: var(--success-color, #16a34a);
    }

    .jmd__stage-card--locked {
      opacity: 0.6;
    }

    .jmd__stage-card-header {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .jmd__stage-card-icon {
      font-size: 20px;
      line-height: 1;
    }

    .jmd__stage-card-title {
      font-size: 15px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0;
      flex: 1;
    }

    .jmd__stage-badge {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      padding: 2px 8px;
      border-radius: 20px;
      background: var(--hover-bg);
      color: var(--text-muted);
    }

    .jmd__stage-badge--active {
      background: color-mix(in srgb, var(--color-primary) 10%, transparent);
      color: var(--primary-color);
    }

    .jmd__stage-badge--complete {
      background: rgba(22, 163, 74, 0.1);
      color: var(--success-color, #16a34a);
    }

    .jmd__stage-badge--locked {
      background: var(--hover-bg);
      color: var(--text-muted);
    }

    .jmd__stage-card-desc {
      font-size: 12px;
      color: var(--text-muted);
      margin: 0;
      line-height: 1.5;
    }

    .jmd__stage-card-score {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .jmd__score-bar {
      flex: 1;
      height: 6px;
      background: var(--hover-bg);
      border-radius: 3px;
      overflow: hidden;
    }

    .jmd__score-fill {
      height: 100%;
      border-radius: 3px;
      transition: width 0.4s ease;
    }

    .jmd__score-text {
      font-size: 12px;
      font-weight: 700;
      color: var(--text-primary);
      min-width: 36px;
      text-align: right;
    }

    .jmd__stage-card-counts {
      font-size: 11px;
      color: var(--text-muted);
      margin: 0;
    }

    .jmd__stage-card-cta {
      font-size: 12px;
      font-weight: 600;
      color: var(--primary-color);
      text-decoration: none;
      margin-top: auto;
      transition: opacity 0.15s;

      &:hover { opacity: 0.75; text-decoration: underline; }
    }

    .jmd__stage-card-locked {
      font-size: 11px;
      color: var(--text-muted);
      font-style: italic;
      margin-top: auto;
    }

    @media (max-width: 1200px) {
      .jmd__stage-grid { grid-template-columns: repeat(2, 1fr); }
    }

    @media (max-width: 768px) {
      .journey-page {
        padding: var(--space-4);
      }

      .jmd__overview {
        grid-template-columns: 1fr;
      }

      .jmd__stage-grid {
        grid-template-columns: 1fr;
      }

      .jmd__obl-grid {
        grid-template-columns: 1fr 1fr;
      }
    }

    .jmd__obl-summary {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .jmd__section-subtitle {
      font-size: var(--font-size-body-sm);
      color: var(--color-text-secondary);
      margin: 0;
    }

    .jmd__obl-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;

      @media (max-width: 1024px) { grid-template-columns: repeat(2, 1fr); }
      @media (max-width: 640px)  { grid-template-columns: 1fr; }
    }

    .jmd__stage-obl-row {
      display: flex;
      flex-direction: column;
      gap: 4px;
      margin-top: 4px;
    }

    .jmd__stage-obl-label {
      font-size: 10px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--color-text-tertiary);
    }

    .jmd__stage-obl-counts {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    .jmd__obl-count {
      font-size: 11px;
      font-weight: 600;
    }

    .jmd__obl-count--verified {
      color: var(--color-success, #16a34a);
    }

    .jmd__obl-count--partial {
      color: var(--color-warning, #d97706);
    }

    .jmd__obl-count--deficient {
      color: var(--color-danger, #dc2626);
    }

    .jmd__btn--primary {
      background: var(--color-brand-primary);
      color: var(--color-text-on-brand, #fff);
      border-color: var(--color-brand-primary);
    }

    .jmd__stage-context {
      padding: 16px 20px;
      border-radius: 12px;
      border: 1px solid var(--color-border-subtle);
      background: var(--color-bg-surface);
    }

    .jmd__blockers {
      margin: 8px 0 12px;
      padding-left: 1.25rem;
      color: var(--color-text-secondary);
      font-size: 13px;
    }

    .jmd__stage-hint {
      margin: 0 0 12px;
      font-size: 13px;
      color: var(--color-text-secondary);
    }

    .jmd__stage-cta, .jmd__def-link {
      display: inline-block;
      font-size: 13px;
      font-weight: 600;
      color: var(--color-brand-primary);
      text-decoration: none;
    }

    .jmd__stage-cta:hover, .jmd__def-link:hover {
      text-decoration: underline;
    }

    .jmd__def-link {
      margin-top: 12px;
    }

    .jmd__def-section {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .jmd__def-overall {
      max-width: 480px;
    }

    .jmd__def-stage-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;

      @media (max-width: 1024px) { grid-template-columns: repeat(2, 1fr); }
      @media (max-width: 640px)  { grid-template-columns: 1fr; }
    }

    .jmd__def-loading {
      font-size: 12px;
      color: var(--color-text-tertiary, #94a3b8);
      margin: 0;
    }

    .jmd__state-card {
      padding: 24px;
      border-radius: 12px;
      background: var(--color-bg-surface);
      border: 1px solid var(--color-border-subtle);
      font-size: 14px;
      color: var(--color-text-secondary);
    }

    .jmd__state-card--error {
      border-color: var(--color-danger, #dc2626);
      color: var(--color-text-primary);
    }
  `],
})
export class JourneyMaturityDashboardComponent {
  readonly store = inject(JourneyWorkspaceStore);
  private readonly deficiencyService = inject(DeficiencyDataService);
  private readonly complianceContext = inject(ComplianceContextService);

  readonly maturityStages = computed(() => this.store.maturityStages());

  readonly deficiencySummary = signal<DeficiencySummaryApiDto | null>(null);
  readonly deficiencyLoading = signal(false);
  readonly deficiencyLoaded = signal(false);

  constructor() {
    combineLatest([
      this.complianceContext.hospitalId$,
      this.complianceContext.frameworkId$,
      this.complianceContext.organizationId$,
    ]).pipe(
      /*
       * Organization context is now a precondition, not a defaultable input.
       *
       * This previously read `getOrganizationId() ?? hospitalId`, so whenever organization context
       * had not hydrated (a reload, a deep link) it sent the **hospital id as the organization id**.
       * `bootstrap-from-onboarding` then wrote a full set of deficiencies stamped
       * `OrganizationId = TenantId = HospitalId` — the exact corruption behind the 88 duplicated
       * rows. Waiting for real organization context is correct: a bootstrap under the wrong tenant
       * is far worse than a bootstrap that runs a moment later.
       */
      filter(([h, f, o]) => !!h && !!f && !!o),
      switchMap(([hospitalId, frameworkId, organizationId]) => {
        this.deficiencyLoading.set(true);
        this.deficiencyLoaded.set(false);
        return this.deficiencyService
          .bootstrapFromOnboarding(organizationId!, hospitalId!, frameworkId!)
          .pipe(
            catchError(() => of(null)),
            switchMap(() =>
              this.deficiencyService.getDeficiencySummary(hospitalId!, frameworkId!).pipe(catchError(() => of(null))),
            ),
          );
      }),
      takeUntilDestroyed(),
    ).subscribe((summary) => {
      this.deficiencySummary.set(summary);
      this.deficiencyLoading.set(false);
      this.deficiencyLoaded.set(true);
    });
  }

  readonly complianceReadinessPercent = computed(() => {
    const snap = this.store.snapshot();
    const api = snap?.accreditationReadinessPercent;
    if (typeof api === 'number' && Number.isFinite(api)) {
      return Math.round(api);
    }
    const ov = this.store.overview();
    return ov ? Math.round(ov.coreCompliancePercent) : 0;
  });

  readonly stageReadinessPercent = computed(() => {
    const stage = this.activeStageState();
    return stage?.readinessPercent ?? 0;
  });

  readonly openDeficiencyCount = computed(() => this.deficiencySummary()?.openCount ?? this.deficiencySummary()?.total ?? 0);

  readonly blockingDeficiencySummary = computed(() => this.deficiencySummary());

  readonly activeStageLabel = computed(() => this.activeStageState()?.label ?? '—');

  readonly activeStageState = computed((): MaturityStageStateModel | null => {
    const current = this.store.currentMaturityStage();
    const stages = this.store.maturityStages();
    if (current) {
      return stages.find((s) => s.stage === current) ?? null;
    }
    return stages.find((s) => s.status === 'inProgress' || s.status === 'available') ?? null;
  });

  stageBadgeLabel(stageId: string): string {
    const s = this.store.maturityStages().find((x) => x.stage === stageId);
    if (!s) return 'Pending';
    return maturityStageStatusLabel(s.status);
  }
}
