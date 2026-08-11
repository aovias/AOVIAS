import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { CommonModule } from '@angular/common';

import type { FindingsWorkspaceDashboard } from '../models/findings-workspace.model';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-findings-dashboard',

  standalone: true,

  imports: [CommonModule],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    @if (dashboard(); as d) {
      <div class="fw-kpis">
        <div class="kpi">
          <span class="kpi__v">{{ d.totalFindings }}</span
          ><span class="kpi__l">Total</span>
        </div>

        <div class="kpi">
          <span class="kpi__v">{{ d.openFindings }}</span
          ><span class="kpi__l">Open</span>
        </div>

        <div class="kpi">
          <span class="kpi__v">{{ d.acceptedFindings }}</span
          ><span class="kpi__l">Accepted</span>
        </div>

        <div class="kpi">
          <span class="kpi__v">{{ d.awaitingReview }}</span
          ><span class="kpi__l">Awaiting Review</span>
        </div>

        <div class="kpi kpi--ready">
          <span class="kpi__v">{{ d.eligibleForPromotion }}</span
          ><span class="kpi__l">Eligible For Promotion</span>
        </div>

        <div class="kpi">
          <span class="kpi__v">{{ d.findingsAwaitingPromotion }}</span
          ><span class="kpi__l">Awaiting Promotion</span>
        </div>

        <div class="kpi">
          <span class="kpi__v">{{ d.promotedFindings }}</span
          ><span class="kpi__l">Promoted</span>
        </div>

        <div class="kpi">
          <span class="kpi__v">{{ d.promotionRate | number: '1.0-1' }}%</span
          ><span class="kpi__l">Promotion Rate</span>
        </div>

        <div class="kpi kpi--critical">
          <span class="kpi__v">{{ d.criticalFindings }}</span
          ><span class="kpi__l">Critical</span>
        </div>
      </div>

      <div class="fw-breakdown">
        @if (d.byClassification.length) {
          <div class="breakdown">
            <h4>By Classification</h4>

            @for (row of d.byClassification; track row.label) {
              <div class="row">
                <span>{{ formatLabel(classificationCategory, row.label) }}</span
                ><span>{{ row.count }}</span>
              </div>
            }
          </div>
        }

        @if (d.bySource.length) {
          <div class="breakdown">
            <h4>By Source</h4>

            @for (row of d.bySource.slice(0, 6); track row.label) {
              <div class="row">
                <span>{{ formatLabel(sourceCategory, row.label) }}</span
                ><span>{{ row.count }}</span>
              </div>
            }
          </div>
        }

        @if (d.byChapter.length) {
          <div class="breakdown">
            <h4>By Chapter</h4>

            @for (row of d.byChapter.slice(0, 5); track row.label) {
              <div class="row">
                <span>{{ row.label }}</span
                ><span>{{ row.count }}</span>
              </div>
            }
          </div>
        }
      </div>
    }
  `,

  styles: [
    `
      :host {
        display: block;
        width: 100%;
      }

      .fw-kpis {
        display: grid;

        grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));

        gap: 10px;

        margin-bottom: 16px;
      }

      .kpi {
        background: var(--card-bg, #fff);

        border: 1px solid var(--border-color, #e5e7eb);

        border-radius: 8px;

        padding: 12px;
      }

      .kpi__v {
        display: block;
        font-size: 22px;
        font-weight: 700;
      }

      .kpi__l {
        font-size: 10px;

        text-transform: uppercase;

        letter-spacing: 0.04em;

        color: var(--text-secondary);
      }

      .kpi--critical .kpi__v {
        color: #dc2626;
      }

      .kpi--ready .kpi__v {
        color: var(--color-primary);
      }

      .fw-breakdown {
        display: grid;

        grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));

        gap: 12px;

        margin-bottom: 16px;
      }

      .breakdown {
        background: var(--card-bg, #fff);

        border: 1px solid var(--border-color, #e5e7eb);

        border-radius: 8px;

        padding: 12px;

        font-size: 12px;
      }

      h4 {
        margin: 0 0 8px;

        font-size: 11px;

        text-transform: uppercase;

        color: var(--text-secondary);
      }

      .row {
        display: flex;
        justify-content: space-between;
        padding: 4px 0;
      }
    `,
  ],
})
export class FindingsDashboardComponent {
  private readonly configuration = inject(PlatformConfigurationService);

  readonly dashboard = input<FindingsWorkspaceDashboard | null>(null);

  protected readonly classificationCategory = CONFIGURATION_CATEGORY.findingClassification;
  protected readonly sourceCategory = CONFIGURATION_CATEGORY.findingSource;

  /**
   * Label for a breakdown bucket, resolved against the category it groups by.
   *
   * The API sends the bucket's *code*; a single regex that spaced capitals used to serve every
   * breakdown, so a dashboard row and the filter offering the same value read differently
   * ("Non Conformity" against "Non-Conformity"). Unrecognised codes still humanise, so a value the
   * platform does not yet know is readable rather than blank.
   */
  formatLabel(category: string, label: string): string {
    return this.configuration.label(category, label, label);
  }
}
