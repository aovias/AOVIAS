import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CompliancePresentationDto } from '../../../features/control-center/services/ai-assistant-api.service';
import { ExecutiveSummaryCardComponent } from './executive-summary-card.component';
import { ComplianceInsightCardComponent } from './compliance-insight-card.component';
import { RecommendedActionCardComponent } from './recommended-action-card.component';
import { RiskBadgeComponent } from './risk-badge.component';
import { SupportingEvidencePanelComponent } from './supporting-evidence-panel.component';

interface SummaryCount {
  label: string;
  value: number;
}

/**
 * Executive Compliance Advisor response. Renders only the sections present in the
 * presentation model, with headers adapted to the question type.
 */
@Component({
  selector: 'app-compliance-response',
  standalone: true,
  imports: [
    CommonModule,
    ExecutiveSummaryCardComponent,
    ComplianceInsightCardComponent,
    RecommendedActionCardComponent,
    RiskBadgeComponent,
    SupportingEvidencePanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="compliance-response">
      @if (data().executiveSummary) {
        <app-executive-summary-card
          [summary]="data().executiveSummary"
          [counts]="summaryCounts()"
        />
      }

      @if (data().findings.length) {
        <section class="cr-section">
          <h4 class="cr-section-title">{{ findingsTitle() }}</h4>
          <div class="cr-stack">
            @for (finding of data().findings; track finding.title) {
              <app-compliance-insight-card [finding]="finding" />
            }
          </div>
        </section>
      }

      @if (data().risks.length) {
        <section class="cr-section">
          <h4 class="cr-section-title">Risk Assessment</h4>
          <div class="cr-stack">
            @for (risk of data().risks; track risk.title) {
              <article class="cr-risk">
                <div class="cr-risk-head">
                  <span class="cr-risk-title">{{ risk.title }}</span>
                  <app-risk-badge [severity]="risk.severity" />
                </div>
                @if (risk.affectedControls.length) {
                  <div class="cr-risk-controls">
                    <span class="cr-risk-controls-label">Affected controls:</span>
                    @for (control of risk.affectedControls; track control) {
                      <span class="cr-control-chip">{{ control }}</span>
                    }
                  </div>
                }
              </article>
            }
          </div>
        </section>
      }

      @if (data().recommendations.length) {
        <section class="cr-section">
          <h4 class="cr-section-title">Recommended Actions</h4>
          <div class="cr-stack">
            @for (action of data().recommendations; track action.priority) {
              <app-recommended-action-card [action]="action" />
            }
          </div>
        </section>
      }

      @if (data().expectedOutcome) {
        <section class="cr-outcome">
          <span class="cr-outcome-icon" aria-hidden="true">
            <svg
              width="15"
              height="15"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            >
              <path d="M13 2 3 14h7l-1 8 10-12h-7l1-8z" />
            </svg>
          </span>
          <div class="cr-outcome-body">
            <span class="cr-outcome-label">Expected Outcome</span>
            <p class="cr-outcome-text">{{ data().expectedOutcome }}</p>
          </div>
        </section>
      }

      <app-supporting-evidence-panel
        [evidence]="data().supportingEvidence"
        [technical]="data().technical"
      />
    </div>
  `,
  styles: [
    `
      .compliance-response {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .cr-section {
        display: flex;
        flex-direction: column;
        gap: 7px;
      }

      .cr-section-title {
        margin: 0;
        font-size: 12px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--text-secondary);
      }

      .cr-stack {
        display: flex;
        flex-direction: column;
        gap: 7px;
      }

      .cr-risk {
        border-radius: 10px;
        padding: 9px 10px;
        border: 1px solid var(--border-color);
        background: color-mix(in srgb, var(--bg-tertiary) 92%, transparent);
      }

      .cr-risk-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
      }

      .cr-risk-title {
        font-size: 12.5px;
        font-weight: 600;
        line-height: 1.3;
        color: var(--text-primary);
      }

      .cr-risk-controls {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 5px;
        margin-top: 7px;
      }

      .cr-risk-controls-label {
        font-size: 10px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--text-muted);
      }

      .cr-control-chip {
        font-size: 10.5px;
        font-weight: 600;
        padding: 2px 8px;
        border-radius: 999px;
        color: var(--text-secondary);
        background: color-mix(in srgb, var(--card-bg) 70%, transparent);
        border: 1px solid var(--border-color);
      }

      .cr-outcome {
        display: flex;
        align-items: flex-start;
        gap: 9px;
        border-radius: 10px;
        padding: 10px;
        border: 1px solid color-mix(in srgb, var(--color-status-success) 38%, transparent);
        background: color-mix(in srgb, var(--color-status-success) 12%, transparent);
      }

      .cr-outcome-icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 24px;
        height: 24px;
        border-radius: 7px;
        flex-shrink: 0;
        color: var(--color-status-success);
        background: color-mix(in srgb, var(--color-status-success) 18%, transparent);
      }

      .cr-outcome-label {
        display: block;
        font-size: 10px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--color-status-success);
        margin-bottom: 2px;
      }

      .cr-outcome-text {
        margin: 0;
        font-size: 12px;
        line-height: 1.4;
        color: var(--text-primary);
      }
    `,
  ],
})
export class ComplianceResponseComponent {
  readonly data = input.required<CompliancePresentationDto>();

  readonly summaryCounts = computed<SummaryCount[]>(() => {
    const model = this.data();
    const counts: SummaryCount[] = [];
    const findingCount = model.findings.length;
    const riskCount = model.risks.length;
    const actionCount = model.recommendations.length;
    if (findingCount > 0) {
      counts.push({ label: findingCount === 1 ? 'Finding' : 'Findings', value: findingCount });
    }
    if (riskCount > 0) {
      counts.push({ label: riskCount === 1 ? 'Risk' : 'Risks', value: riskCount });
    }
    if (actionCount > 0) {
      counts.push({ label: actionCount === 1 ? 'Action' : 'Actions', value: actionCount });
    }
    return counts;
  });

  readonly findingsTitle = computed(() => {
    switch (this.data().insightType) {
      case 'ComplianceScore':
        return 'Top Compliance Gaps';
      case 'Evidence':
        return 'Missing Evidence';
      case 'Standard':
        return 'Key Requirements';
      default:
        return 'Key Findings';
    }
  });
}
