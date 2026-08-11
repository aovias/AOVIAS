import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { CommonModule } from '@angular/common';

import { RouterLink } from '@angular/router';

import type { DeficiencyCommandCenterDashboard } from '../models/deficiency-command-center.model';

import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';

import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-dcc-dashboard',

  standalone: true,

  imports: [CommonModule, RouterLink],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    @if (dashboard(); as d) {
      <div class="dcc-kpis">
        <div class="kpi">
          <span class="kpi__value">{{ d.totalDeficiencies }}</span>

          <span class="kpi__label">Total Deficiencies</span>
        </div>

        <div class="kpi kpi--critical">
          <span class="kpi__value">{{ d.criticalDeficiencies }}</span>

          <span class="kpi__label">Critical</span>
        </div>

        <div class="kpi kpi--major">
          <span class="kpi__value">{{ d.majorDeficiencies }}</span>

          <span class="kpi__label">Major</span>
        </div>

        <div class="kpi">
          <span class="kpi__value">{{ d.openDeficiencies }}</span>

          <span class="kpi__label">Open</span>
        </div>

        <div class="kpi kpi--resolved">
          <span class="kpi__value">{{ d.resolvedDeficiencies }}</span>

          <span class="kpi__label">Resolved</span>
        </div>

        <div class="kpi kpi--blocking">
          <span class="kpi__value">{{ d.blockingDeficiencies }}</span>

          <span class="kpi__label">Blocking Accreditation</span>
        </div>

        <div class="kpi kpi--sla-on">
          <span class="kpi__value">{{ d.slaOnTrack }}</span>

          <span class="kpi__label">SLA On Track</span>
        </div>

        <div class="kpi kpi--sla-warn">
          <span class="kpi__value">{{ d.slaWarning }}</span>

          <span class="kpi__label">SLA Warning</span>
        </div>

        <div class="kpi kpi--sla-risk">
          <span class="kpi__value">{{ d.slaAtRisk }}</span>

          <span class="kpi__label">At Risk</span>
        </div>

        <div class="kpi kpi--sla-breach">
          <span class="kpi__value">{{ d.slaBreached }}</span>

          <span class="kpi__label">Breached</span>
        </div>

        <div class="kpi kpi--escalated">
          <span class="kpi__value">{{ d.escalatedDeficiencies }}</span>

          <span class="kpi__label">Escalated</span>
        </div>

        <div class="kpi kpi--crit-esc">
          <span class="kpi__value">{{ d.criticalEscalations }}</span>

          <span class="kpi__label">Critical Escalations</span>
        </div>

        <div class="kpi kpi--capa-open">
          <span class="kpi__value">{{ d.openCapas ?? 0 }}</span>

          <span class="kpi__label">Open CAPAs</span>
        </div>

        <div class="kpi kpi--capa-overdue">
          <span class="kpi__value">{{ d.overdueCapas ?? 0 }}</span>

          <span class="kpi__label">Overdue CAPAs</span>
        </div>

        @if (d.agingBuckets) {
          <div class="kpi" title="Open CAPA aging by DetectedDate">
            <span class="kpi__value">{{ d.agingBuckets.within30 }}</span>
            <span class="kpi__label">CAPA ≤30d</span>
          </div>
          <div class="kpi">
            <span class="kpi__value">{{ d.agingBuckets.days31To60 }}</span>
            <span class="kpi__label">CAPA 31–60d</span>
          </div>
          <div class="kpi">
            <span class="kpi__value">{{ d.agingBuckets.days61To90 }}</span>
            <span class="kpi__label">CAPA 61–90d</span>
          </div>
          <div class="kpi kpi--capa-overdue">
            <span class="kpi__value">{{ d.agingBuckets.over90 }}</span>
            <span class="kpi__label">CAPA &gt;90d</span>
          </div>
        }

        @if (d.averageOpenCapaAgeDays != null) {
          <div class="kpi">
            <span class="kpi__value">{{ d.averageOpenCapaAgeDays | number: '1.0-0' }}</span>
            <span class="kpi__label">Avg Open CAPA Age (days)</span>
          </div>
        }

        <div class="kpi kpi--capa-effective">
          <span class="kpi__value">{{ d.effectiveCapas ?? 0 }}</span>

          <span class="kpi__label">Effective CAPAs</span>
        </div>

        <div class="kpi kpi--capa-ineffective">
          <span class="kpi__value">{{ d.ineffectiveCapas ?? 0 }}</span>

          <span class="kpi__label">Ineffective CAPAs</span>
        </div>

        <div class="kpi kpi--recurring">
          <span class="kpi__value">{{ d.recurringIssues ?? 0 }}</span>

          <span class="kpi__label">Recurring Issues</span>
        </div>

        <div class="kpi kpi--pending-review">
          <span class="kpi__value">{{ d.pendingEffectivenessReviews ?? 0 }}</span>

          <span class="kpi__label">Pending Reviews</span>
        </div>

        @if (d.departmentOverdueCapas?.[0]; as topOverdue) {
          <div class="kpi kpi--capa-overdue" title="Top departments with overdue CAPAs">
            <span class="kpi__value">{{ topOverdue.count }}</span>
            <span class="kpi__label">Overdue · {{ topOverdue.label }}</span>
          </div>
        }

        <div
          class="kpi kpi--advisory"
          title="Predicted gaps are advisory — not compliance deficiencies"
        >
          <span class="kpi__value">{{ predictedGaps() }}</span>

          <span class="kpi__label">Predicted Gaps</span>

          <a class="kpi__link" routerLink="/app/compliance-maturity">View in CM →</a>
        </div>

        <div
          class="kpi kpi--findings"
          title="Audit findings are separate from compliance deficiencies"
        >
          <span class="kpi__value">{{ d.auditFindings }}</span>

          <span class="kpi__label">Audit Findings</span>

          <a class="kpi__link" routerLink="/app/audit/findings">View Findings →</a>
        </div>

        <div class="kpi" title="Deficiencies promoted from audit findings">
          <span class="kpi__value">{{ d.auditOriginDeficiencies }}</span>

          <span class="kpi__label">Audit-Origin Deficiencies</span>
        </div>

        <div class="kpi" title="Deficiencies from assessment engine">
          <span class="kpi__value">{{ d.assessmentOriginDeficiencies }}</span>

          <span class="kpi__label">Assessment-Origin Deficiencies</span>
        </div>

        <div class="kpi" title="Manually created deficiencies">
          <span class="kpi__value">{{ d.manualOriginDeficiencies }}</span>

          <span class="kpi__label">Manual-Origin Deficiencies</span>
        </div>
      </div>

      <div class="dcc-kpis dcc-kpis--lifecycle">
        <div class="kpi">
          <span class="kpi__value">{{ d.awaitingAssignment }}</span>

          <span class="kpi__label">Awaiting Assignment</span>
        </div>

        <div class="kpi">
          <span class="kpi__value">{{ d.inProgressDeficiencies }}</span>

          <span class="kpi__label">In Progress</span>
        </div>

        <div class="kpi">
          <span class="kpi__value">{{ d.awaitingEvidence }}</span>

          <span class="kpi__label">Awaiting Evidence</span>
        </div>

        <div class="kpi">
          <span class="kpi__value">{{ d.verificationPending }}</span>

          <span class="kpi__label">Verification Pending</span>
        </div>

        <div class="kpi kpi--resolved">
          <span class="kpi__value">{{ d.verifiedDeficiencies }}</span>

          <span class="kpi__label">Verified</span>
        </div>

        <div class="kpi kpi--high-risk">
          <span class="kpi__value">{{ d.highRiskDeficiencies }}</span>

          <span class="kpi__label">High Risk</span>
        </div>

        <div class="kpi kpi--critical-risk">
          <span class="kpi__value">{{ d.criticalRiskDeficiencies }}</span>

          <span class="kpi__label">Critical Risk</span>
        </div>
      </div>

      @if (hasBreakdowns(d)) {
        <div class="breakdowns">
          @if (d.byLifecycleStatus.length) {
            <div class="breakdown">
              <h4>By Lifecycle</h4>

              <ul>
                @for (row of d.byLifecycleStatus; track row.label) {
                  <li>
                    <span>{{ formatLabel(statusCategory, row.label) }}</span
                    ><strong>{{ row.count }}</strong>
                  </li>
                }
              </ul>
            </div>
          }

          @if (d.byRiskLevel.length) {
            <div class="breakdown">
              <h4>By Risk Level</h4>

              <ul>
                @for (row of d.byRiskLevel; track row.label) {
                  <li>
                    <span>{{ formatLabel(riskCategory, row.label) }}</span
                    ><strong>{{ row.count }}</strong>
                  </li>
                }
              </ul>
            </div>
          }

          @if (d.byRootCauseCategory.length) {
            <div class="breakdown">
              <h4>By Root Cause</h4>

              <ul>
                @for (row of d.byRootCauseCategory; track row.label) {
                  <li>
                    <span>{{ formatLabel(rootCauseCategory, row.label) }}</span
                    ><strong>{{ row.count }}</strong>
                  </li>
                }
              </ul>
            </div>
          }

          @if (d.byDepartment.length) {
            <div class="breakdown">
              <h4>By Department</h4>

              <ul>
                @for (row of d.byDepartment; track row.label) {
                  <li>
                    <span>{{ row.label }}</span
                    ><strong>{{ row.count }}</strong>
                  </li>
                }
              </ul>
            </div>
          }

          @if (d.byStage.length) {
            <div class="breakdown">
              <h4>By Stage</h4>

              <ul>
                @for (row of d.byStage; track row.label) {
                  <li>
                    <span>{{ formatLabel(stageCategory, row.label) }}</span
                    ><strong>{{ row.count }}</strong>
                  </li>
                }
              </ul>
            </div>
          }
        </div>
      }
    }
  `,

  styles: [
    `
      .dcc-kpis {
        display: grid;

        grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));

        gap: 12px;

        margin-bottom: 12px;
      }

      .dcc-kpis--lifecycle {
        margin-bottom: 20px;
      }

      .kpi {
        background: var(--card-bg, #fff);

        border: 1px solid var(--border-color, #e5e7eb);

        border-radius: 8px;

        padding: 14px 16px;

        display: flex;

        flex-direction: column;

        gap: 4px;
      }

      .kpi__value {
        font-size: 24px;

        font-weight: 700;

        color: var(--text-primary, #111);

        line-height: 1.1;
      }

      .kpi__label {
        font-size: 11px;

        font-weight: 600;

        text-transform: uppercase;

        letter-spacing: 0.04em;

        color: var(--text-secondary, #6b7280);
      }

      .kpi__link {
        font-size: 11px;

        color: var(--primary-color, var(--color-primary));

        text-decoration: none;

        margin-top: 4px;
      }

      .kpi--critical .kpi__value {
        color: #dc2626;
      }

      .kpi--major .kpi__value {
        color: #ea580c;
      }

      .kpi--blocking .kpi__value {
        color: #b45309;
      }

      .kpi--resolved .kpi__value {
        color: #059669;
      }

      .kpi--high-risk .kpi__value {
        color: #ea580c;
      }

      .kpi--critical-risk .kpi__value {
        color: #dc2626;
      }

      .kpi--capa-overdue .kpi__value {
        color: #dc2626;
      }

      .kpi--capa-effective .kpi__value {
        color: #059669;
      }

      .kpi--capa-ineffective .kpi__value {
        color: #b45309;
      }

      .kpi--recurring .kpi__value {
        color: #2f6bff;
      }

      .kpi--advisory {
        border-style: dashed;
      }

      .breakdowns {
        display: grid;

        grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));

        gap: 12px;

        margin-bottom: 20px;
      }

      .breakdown {
        background: var(--card-bg, #fff);

        border: 1px solid var(--border-color, #e5e7eb);

        border-radius: 8px;

        padding: 12px 14px;
      }

      .breakdown h4 {
        margin: 0 0 8px;

        font-size: 11px;

        text-transform: uppercase;

        letter-spacing: 0.04em;

        color: var(--text-secondary);
      }

      .breakdown ul {
        list-style: none;

        margin: 0;

        padding: 0;

        font-size: 12px;
      }

      .breakdown li {
        display: flex;

        justify-content: space-between;

        gap: 8px;

        padding: 3px 0;
      }
    `,
  ],
})
export class DccDashboardComponent {
  private readonly configuration = inject(PlatformConfigurationService);

  readonly dashboard = input<DeficiencyCommandCenterDashboard | null>(null);

  readonly predictedGaps = input(0);

  protected readonly statusCategory = CONFIGURATION_CATEGORY.deficiencyStatus;
  protected readonly riskCategory = CONFIGURATION_CATEGORY.riskLevel;
  protected readonly rootCauseCategory = CONFIGURATION_CATEGORY.rootCauseCategory;
  protected readonly stageCategory = CONFIGURATION_CATEGORY.maturityStage;

  /**
   * Label for a breakdown bucket, resolved against the category it groups by.
   *
   * The API sends the bucket's code, which reached the user verbatim — a root-cause breakdown read
   * `PolicyMissing` while the picker offering the same value read "Policy Missing".
   */
  formatLabel(category: string, label: string): string {
    return this.configuration.label(category, label, label);
  }

  hasBreakdowns(d: DeficiencyCommandCenterDashboard): boolean {
    return (
      d.byLifecycleStatus.length > 0 ||
      d.byRiskLevel.length > 0 ||
      d.byRootCauseCategory.length > 0 ||
      d.byDepartment.length > 0 ||
      d.byStage.length > 0
    );
  }
}
