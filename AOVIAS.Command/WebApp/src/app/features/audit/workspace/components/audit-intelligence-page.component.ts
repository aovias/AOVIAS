import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { AnalyticsApiService } from '../../../analytics/services/analytics-api.service';
import type { CapaAnalytics, ExecutiveAnalytics } from '../../../analytics/models/analytics.model';
import {
  WorkspaceHeaderComponent,
  WorkspacePageComponent,
  WorkspaceSectionComponent,
} from '../../../../shared/ui/workspace/workspace-layout';
import {
  WorkspaceEmptyStateComponent,
  WorkspaceErrorStateComponent,
  WorkspaceSkeletonComponent,
} from '../../../../shared/ui/workspace/workspace-states';
import { injectAuditWorkspace } from '../services/audit-workspace-participation.service';

interface ExecutiveKpi {
  readonly label: string;
  readonly value: string;
  readonly tone: 'neutral' | 'good' | 'warn' | 'bad';
  readonly hint?: string;
}

/**
 * Executive Intelligence Center — Stage 4, Phase 1.
 *
 * Replaces the capability placeholder that had listed five unbuilt features
 * since Phase 1. Everything here composes `/analytics`, which already served
 * executive readiness, department performance, CAPA effectiveness, SLA health
 * and risk exposure — and which the Audit module had never called.
 *
 * No new backend and no new analytics: presentation over an existing
 * capability, which is what Stage 4 asks for — build intelligence on top of the
 * established platform.
 *
 * Scope note: forecasting and the generated executive narrative are absent.
 * Both need history these endpoints do not expose, and inventing a trend line
 * from a single snapshot would be fabrication rather than intelligence.
 */
@Component({
  selector: 'app-audit-intelligence-page',
  standalone: true,
  imports: [
    CommonModule,
    WorkspacePageComponent,
    WorkspaceHeaderComponent,
    WorkspaceSectionComponent,
    WorkspaceSkeletonComponent,
    WorkspaceEmptyStateComponent,
    WorkspaceErrorStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-workspace-page [wide]="true">
      <app-workspace-header
        title="Executive Intelligence"
        description="Organisational compliance health, department performance and risk exposure."
      >
        <button headerActions type="button" class="refresh-btn" (click)="load()">Refresh</button>
      </app-workspace-header>

      @if (error()) {
        <app-workspace-error-state
          message="Executive analytics could not be loaded."
          hint="Check that a framework is selected for this hospital."
          (retry)="load()"
        />
      }

      @if (loading()) {
        <app-workspace-skeleton variant="cards" [count]="8" label="Loading executive analytics" />
        <app-workspace-skeleton
          variant="table"
          [count]="5"
          label="Loading department performance"
        />
      } @else if (executive(); as exec) {
        <app-workspace-section heading="Executive KPIs" [plain]="true">
          <div class="kpis">
            @for (kpi of kpis(); track kpi.label) {
              <div class="kpi" [class]="'tone-' + kpi.tone">
                <span class="kpi__value">{{ kpi.value }}</span>
                <span class="kpi__label">{{ kpi.label }}</span>
                @if (kpi.hint) {
                  <span class="kpi__hint">{{ kpi.hint }}</span>
                }
              </div>
            }
          </div>
        </app-workspace-section>

        <app-workspace-section heading="Department performance">
          @if (exec.departmentPerformance.length) {
            <ul class="bars">
              @for (d of exec.departmentPerformance; track d.label) {
                <li class="bar">
                  <span class="bar__label">{{ d.label }}</span>
                  <span class="bar__track">
                    <span class="bar__fill" [style.inline-size.%]="barWidth(d.count)"></span>
                  </span>
                  <span class="bar__value">{{ d.count }}</span>
                </li>
              }
            </ul>
          } @else {
            <app-workspace-empty-state
              icon="▦"
              title="No department data"
              description="Department performance appears once audits have been recorded against departments."
            />
          }
        </app-workspace-section>

        @if (exec.slaHealth.departmentsAtRisk.length) {
          <app-workspace-section heading="Departments at risk">
            <ul class="chips">
              @for (d of exec.slaHealth.departmentsAtRisk; track d) {
                <li class="chip">{{ d }}</li>
              }
            </ul>
          </app-workspace-section>
        }
      } @else if (!error()) {
        <app-workspace-empty-state
          icon="◔"
          title="No analytics available yet"
          description="Executive intelligence appears once audits, findings and CAPAs have been recorded."
          actionLabel="Reload"
          (action)="load()"
        />
      }
    </app-workspace-page>
  `,
  styleUrl: './audit-intelligence-page.component.scss',
})
export class AuditIntelligencePageComponent {
  private readonly analytics = inject(AnalyticsApiService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  protected readonly executive = signal<ExecutiveAnalytics | null>(null);
  protected readonly capa = signal<CapaAnalytics | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly kpis = signal<readonly ExecutiveKpi[]>([]);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(false);

    forkJoin({
      exec: this.analytics.getExecutive().pipe(catchError(() => of(null))),
      capa: this.analytics.getCapa().pipe(catchError(() => of(null))),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ exec, capa }) => {
        // Only the executive payload is essential; CAPA detail enriches it.
        if (!exec) {
          this.error.set(true);
          return;
        }
        this.executive.set(exec);
        this.capa.set(capa);
        this.kpis.set(this.buildKpis(exec, capa));
        this.publishParticipation(exec);
      });
  }

  /**
   * Percentage metrics are toned against fixed thresholds. Raw counts are toned
   * only when a non-zero value genuinely warrants attention — colouring a plain
   * count green because it happens to be small would assert a judgement the
   * data does not support.
   */
  private buildKpis(exec: ExecutiveAnalytics, capa: CapaAnalytics | null): ExecutiveKpi[] {
    const pct = (n: number) => `${Math.round(n)}%`;
    const scoreTone = (n: number): ExecutiveKpi['tone'] =>
      n >= 85 ? 'good' : n >= 60 ? 'warn' : 'bad';
    const countTone = (n: number): ExecutiveKpi['tone'] => (n > 0 ? 'bad' : 'neutral');

    const kpis: ExecutiveKpi[] = [
      {
        label: 'Compliance readiness',
        value: pct(exec.complianceReadiness.score),
        tone: scoreTone(exec.complianceReadiness.score),
        hint: 'Weighted score across all chapters in the active framework',
      },
      {
        label: 'Accreditation readiness',
        value: pct(exec.accreditationReadiness.score),
        tone: scoreTone(exec.accreditationReadiness.score),
        hint: "This hospital's progress through its current accreditation journey",
      },
      {
        label: 'CAPA effectiveness',
        value: pct(exec.capaPerformance.capaEffectivenessRate),
        tone: scoreTone(exec.capaPerformance.capaEffectivenessRate),
        hint: 'Closed CAPAs verified as effective at follow-up review',
      },
      {
        label: 'CAPA completion',
        value: pct(exec.capaPerformance.capaCompletionRate),
        tone: scoreTone(exec.capaPerformance.capaCompletionRate),
        hint: 'Share of raised CAPAs that have been closed',
      },
      {
        label: 'SLA health',
        value: pct(exec.slaHealth.slaHealthScore),
        tone: scoreTone(exec.slaHealth.slaHealthScore),
        hint: 'Open deficiencies still within their agreed resolution window',
      },
      {
        label: 'Critical open deficiencies',
        value: String(exec.riskExposure.criticalOpenDeficiencies),
        tone: countTone(exec.riskExposure.criticalOpenDeficiencies),
        hint: 'Critical-severity gaps blocking accreditation',
      },
      {
        label: 'High-risk deficiencies',
        value: String(exec.riskExposure.highRiskDeficiencies),
        tone: countTone(exec.riskExposure.highRiskDeficiencies),
        hint: 'Open gaps scored high on the risk matrix',
      },
      {
        label: 'Overdue deficiencies',
        value: String(exec.slaHealth.overdueDeficiencies),
        tone: countTone(exec.slaHealth.overdueDeficiencies),
        hint: 'Past their agreed resolution date',
      },
      {
        label: 'Recurring issues',
        value: String(exec.capaPerformance.recurringIssues),
        tone: countTone(exec.capaPerformance.recurringIssues),
        hint: 'Gaps that reopened after a CAPA was closed',
      },
      {
        label: 'Pending reviews',
        value: String(exec.capaPerformance.pendingReviews),
        tone: 'neutral',
        hint: 'Closed CAPAs awaiting an effectiveness review',
      },
    ];

    if (capa) {
      kpis.push({
        label: 'Average closure time',
        value: `${Math.round(capa.averageClosureDays)} d`,
        tone: 'neutral',
        hint: 'Mean days to close a CAPA',
      });
    }

    return kpis;
  }

  /** Widths are relative to the largest department, so the chart self-scales. */
  protected barWidth(count: number): number {
    const max = Math.max(
      ...(this.executive()?.departmentPerformance.map((d) => d.count) ?? [1]),
      1,
    );
    return Math.round((count / max) * 100);
  }

  private publishParticipation(exec: ExecutiveAnalytics): void {
    this.workspace.set({
      stage: null,
      subject: { kind: 'none', title: 'Executive intelligence' },
      actions: [{ id: 'intel.refresh', label: 'Refresh analytics', run: () => this.load() }],
      relatedRecords: exec.departmentPerformance.slice(0, 8).map((d) => ({
        id: d.label,
        label: d.label,
        kind: 'Department',
        status: String(d.count),
      })),
      aiContext: {
        facts: {
          complianceReadiness: String(Math.round(exec.complianceReadiness.score)),
          capaEffectiveness: String(Math.round(exec.capaPerformance.capaEffectivenessRate)),
          criticalOpenDeficiencies: String(exec.riskExposure.criticalOpenDeficiencies),
          departmentsAtRisk: String(exec.slaHealth.departmentsAtRisk.length),
        },
      },
    });
  }
}
