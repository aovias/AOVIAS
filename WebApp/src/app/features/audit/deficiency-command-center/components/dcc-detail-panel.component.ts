import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';

import { CommonModule } from '@angular/common';

import { FormsModule } from '@angular/forms';

import { RouterLink } from '@angular/router';

import { EvidenceStatusBadgeComponent } from '../../../../shared/ui/evidence-status-badge/evidence-status-badge.component';

import { ComplianceClassificationBadgeComponent } from '../../../../shared/ui/compliance-classification-badge/compliance-classification-badge.component';

import {
  AiAssistantApiService,
  CapaSuggestionItemDto,
} from '../../../control-center/services/ai-assistant-api.service';

import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';

import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';

import { RiskConfigurationService } from '../../../../core/configuration/risk-configuration.service';

import {
  type DeficiencyWorkspaceDetail,
  type PatchDeficiencyRcaRequest,
  type PatchDeficiencyRemediationRequest,
  type PatchDeficiencyCorrectiveActionRequest,
  type PatchDeficiencyPreventiveActionRequest,
  type PatchDeficiencyEffectivenessReviewRequest,
  type AssignmentRoutingSuggestion,
} from '../models/deficiency-command-center.model';

@Component({
  selector: 'app-dcc-detail-panel',

  standalone: true,

  imports: [
    CommonModule,

    FormsModule,

    RouterLink,

    EvidenceStatusBadgeComponent,

    ComplianceClassificationBadgeComponent,
  ],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    @if (detail(); as d) {
      <aside class="panel">
        <header class="panel__head">
          <div>
            <span class="panel__code">{{ d.deficiencyCode }}</span>
            @if (d.capaNumber) {
              <span class="panel__capa">{{ d.capaNumber }}</span>
            }

            <h2>{{ d.title }}</h2>
          </div>

          <button type="button" class="close" (click)="close.emit()" aria-label="Close">×</button>
        </header>

        @if (canAcceptOwnership(d)) {
          <section class="section section--ownership">
            <h3>CAPA Ownership</h3>
            <p class="muted">Assigned — awaiting owner acceptance.</p>
            <div class="actions">
              <button
                type="button"
                class="btn-accept"
                [disabled]="saving()"
                (click)="acceptOwnership.emit()"
              >
                Accept
              </button>
              <button
                type="button"
                class="btn-reject"
                [disabled]="saving()"
                (click)="rejectOwnership.emit()"
              >
                Reject
              </button>
            </div>
          </section>
        } @else if (d.ownerAcceptedAt) {
          <section class="section section--ownership">
            <h3>CAPA Ownership</h3>
            <p class="muted">
              Accepted by {{ d.ownerAcceptedBy || 'owner' }} on
              {{ d.ownerAcceptedAt | date: 'mediumDate' }}
            </p>
          </section>
        }

        <section class="lifecycle">
          @for (step of lifecycleSteps(); track step.id; let i = $index) {
            <div
              class="lc-step"
              [class.lc-step--done]="lifecycleIndex(d.status) > i"
              [class.lc-step--active]="lifecycleIndex(d.status) === i"
            >
              <span class="lc-dot"></span>

              <span class="lc-label">{{ step.label }}</span>
            </div>
          }
        </section>

        @if (d.capaLifecycleSteps?.length) {
          <section class="section section--capa-lifecycle">
            <h3>CAPA Lifecycle</h3>

            <div class="lifecycle capa-lifecycle">
              @for (step of d.capaLifecycleSteps; track step.key) {
                <div
                  class="lc-step"
                  [class.lc-step--done]="step.isComplete"
                  [class.lc-step--active]="step.isCurrent"
                >
                  <span class="lc-dot"></span>

                  <span class="lc-label">{{ step.label }}</span>
                </div>
              }
            </div>
          </section>
        }

        @if (routingSuggestion(); as r) {
          <section class="section section--routing">
            <h3>Recommended Assignment</h3>

            <dl class="meta">
              <div>
                <dt>Rule</dt>
                <dd>{{ r.ruleAppliedLabel }}</dd>
              </div>

              <div>
                <dt>Department</dt>
                <dd>{{ r.recommendedDepartmentName || r.normalizedDepartmentCode || '—' }}</dd>
              </div>

              <div>
                <dt>Recommended Owner</dt>
                <dd>{{ r.recommendedAssigneeUserId || 'Not configured' }}</dd>
              </div>

              <div>
                <dt>Confidence</dt>
                <dd>
                  <span [class]="confidenceClass(r.confidence)">{{
                    formatConfidence(r.confidence)
                  }}</span>
                </dd>
              </div>
            </dl>

            <p class="routing-reason">{{ r.routingReason }}</p>

            @if (r.recommendedAssigneeUserId) {
              <button
                type="button"
                class="btn-accept"
                (click)="acceptRouting.emit()"
                [disabled]="routingLoading() || saving()"
              >
                {{
                  routingLoading()
                    ? 'Applying…'
                    : r.canAutoAssign
                      ? 'Accept Assignment'
                      : 'Apply Suggested Owner'
                }}
              </button>
            }
          </section>
        }

        <section class="section section--sla">
          <h3>SLA Accountability</h3>

          <dl class="meta">
            <div>
              <dt>Policy</dt>
              <dd>{{ d.slaPolicyName || '—' }}</dd>
            </div>

            <div>
              <dt>Status</dt>
              <dd>
                <span [class]="slaStatusClass(d.slaStatus)">{{
                  formatSlaStatus(d.slaStatus)
                }}</span>
              </dd>
            </div>

            <div>
              <dt>Due</dt>
              <dd>{{ d.slaDueDate || d.targetResolutionDate | date: 'medium' }}</dd>
            </div>

            <div>
              <dt>Remaining</dt>
              <dd>
                {{
                  d.remainingHours != null
                    ? d.remainingHours + 'h'
                    : d.daysRemaining != null
                      ? d.daysRemaining + ' days'
                      : '—'
                }}
              </dd>
            </div>

            <div>
              <dt>Consumed</dt>
              <dd>{{ d.slaPercentConsumed != null ? d.slaPercentConsumed + '%' : '—' }}</dd>
            </div>

            <div>
              <dt>Escalation</dt>
              <dd>
                {{
                  d.escalationLevel
                    ? 'Level ' + d.escalationLevel + ' · ' + (d.escalationState || '')
                    : 'None'
                }}
              </dd>
            </div>

            @if (d.escalationTargetLabel) {
              <div>
                <dt>Escalation Target</dt>
                <dd>{{ d.escalationTargetLabel }}</dd>
              </div>
            }
          </dl>

          @if (d.escalationHistory?.length) {
            <div class="escalation-history">
              <h4>Escalation History</h4>

              @for (h of d.escalationHistory; track h.id) {
                <div class="history-row">
                  <strong>Level {{ h.level }} · {{ h.state }}</strong>

                  <span class="muted"
                    >{{ h.targetLabel }} · {{ h.triggeredAt | date: 'medium' }}</span
                  >

                  <p class="hint">{{ h.reason }}</p>

                  @if (h.state === 'Escalated' || h.state === 'Pending') {
                    <button
                      type="button"
                      class="btn-accept"
                      (click)="acknowledgeEscalation.emit(h.id)"
                      [disabled]="saving()"
                    >
                      Acknowledge Escalation
                    </button>
                  }
                </div>
              }
            </div>
          }
        </section>

        <section class="section">
          <h3>Summary</h3>

          <p>{{ d.description || 'No description provided.' }}</p>

          <dl class="meta">
            <div>
              <dt>Severity</dt>
              <dd>
                <span [class]="severityClass(d.severity)">{{ formatSeverity(d.severity) }}</span>
              </dd>
            </div>

            <div>
              <dt>Lifecycle</dt>
              <dd>{{ d.lifecycleStatus || formatStatus(d.status) }}</dd>
            </div>

            <div>
              <dt>Stage</dt>
              <dd>{{ formatStage(d.maturityLevel) }}</dd>
            </div>

            <div>
              <dt>Blocking</dt>
              <dd>{{ d.blockingStageLabel }}</dd>
            </div>

            @if (d.assessedRiskLevel) {
              <div>
                <dt>Risk</dt>
                <dd>
                  <span class="risk" [class]="riskClass(d.assessedRiskLevel)"
                    >{{ formatRiskLevel(d.assessedRiskLevel) }} ({{ d.riskScore ?? '—' }})</span
                  >
                </dd>
              </div>
            }
          </dl>
        </section>

        <section class="section">
          <h3>Ownership</h3>

          <dl class="meta">
            <div>
              <dt>Owner</dt>
              <dd>{{ d.assignedOwner || '—' }}</dd>
            </div>

            <div>
              <dt>Department</dt>
              <dd>{{ department || '—' }}</dd>
            </div>

            <div>
              <dt>Assigned</dt>
              <dd>{{ d.assignedDate ? (d.assignedDate | date: 'mediumDate') : '—' }}</dd>
            </div>

            <div>
              <dt>Target Resolution</dt>
              <dd>
                {{ d.targetResolutionDate ? (d.targetResolutionDate | date: 'mediumDate') : '—' }}
              </dd>
            </div>

            <div>
              <dt>Actual Resolution</dt>
              <dd>
                {{ d.actualResolutionDate ? (d.actualResolutionDate | date: 'mediumDate') : '—' }}
              </dd>
            </div>
          </dl>

          <label class="field">
            <span>Department</span>

            <input type="text" [(ngModel)]="department" [disabled]="saving()" />
          </label>

          <label class="field">
            <span>Target Resolution Date</span>

            <input type="date" [(ngModel)]="targetResolutionDate" [disabled]="saving()" />
          </label>
        </section>

        <section class="section">
          <h3>Root Cause Analysis</h3>

          <label class="field">
            <span>Category</span>

            <select [(ngModel)]="rootCauseCategory" [disabled]="saving()">
              <option value="">Not classified</option>

              @for (opt of rootCauseOptions(); track opt.value) {
                <option [value]="opt.value">{{ opt.label }}</option>
              }
            </select>
          </label>

          <label class="field"
            ><span>Problem Statement</span
            ><textarea rows="2" [(ngModel)]="problemStatement" [disabled]="saving()"></textarea>
          </label>

          <label class="field"
            ><span>Root Cause Summary</span
            ><textarea rows="2" [(ngModel)]="rootCauseSummary" [disabled]="saving()"></textarea>
          </label>

          <label class="field"
            ><span>Impact Description</span
            ><textarea rows="2" [(ngModel)]="impactDescription" [disabled]="saving()"></textarea>
          </label>

          <button type="button" class="btn-save" (click)="emitRcaSave()" [disabled]="saving()">
            Save RCA
          </button>
        </section>

        <section class="section section--ai-capa">
          <h3>AI CAPA Suggestions</h3>
          <p class="muted">
            Advisory proposals only — never auto-applied. Human approval required.
          </p>
          <button
            type="button"
            class="btn-save"
            [disabled]="saving() || capaSuggestLoading()"
            (click)="requestCapaSuggestions(d)"
          >
            {{ capaSuggestLoading() ? 'Suggesting…' : 'Suggest CAPA actions' }}
          </button>
          @if (capaSuggestError()) {
            <p class="overdue-flag">{{ capaSuggestError() }}</p>
          }
          @for (s of capaSuggestions(); track s.title) {
            <article class="ai-capa-card">
              <div class="ai-capa-kind">
                {{ s.kind }} · {{ s.confidenceScore * 100 | number: '1.0-0' }}%
              </div>
              <div class="ai-capa-title">{{ s.title }}</div>
              <p class="ai-capa-proposal">{{ s.proposal }}</p>
              <p class="muted">Expected: {{ s.expectedOutcome }}</p>
            </article>
          }
        </section>

        <section class="section">
          <h3>Corrective Action</h3>

          <label class="field"
            ><span>Summary</span
            ><textarea
              rows="2"
              [(ngModel)]="correctiveActionSummary"
              [disabled]="saving()"
            ></textarea>
          </label>

          <label class="field"
            ><span>Owner</span
            ><input type="text" [(ngModel)]="correctiveActionOwner" [disabled]="saving()"
          /></label>

          <div class="risk-row">
            <label class="field"
              ><span>Start Date</span
              ><input type="date" [(ngModel)]="correctiveActionStartDate" [disabled]="saving()"
            /></label>

            <label class="field"
              ><span>Target Date</span
              ><input type="date" [(ngModel)]="correctiveActionTargetDate" [disabled]="saving()"
            /></label>
          </div>

          <label class="field"
            ><span>Completed Date</span
            ><input type="date" [(ngModel)]="correctiveActionCompletedDate" [disabled]="saving()"
          /></label>

          <label class="field">
            <span>Status</span>

            <select [(ngModel)]="correctiveActionStatus" [disabled]="saving()">
              @for (opt of capaStatusOptions(); track opt.value) {
                <option [value]="opt.value">{{ opt.label }}</option>
              }
            </select>
          </label>

          @if (d.correctiveActionOverdue) {
            <p class="overdue-flag">Corrective action is overdue</p>
          }

          <button
            type="button"
            class="btn-save"
            (click)="emitCorrectiveSave()"
            [disabled]="saving()"
          >
            Save Corrective Action
          </button>
        </section>

        <section class="section">
          <h3>Preventive Action</h3>

          <label class="field"
            ><span>Summary</span
            ><textarea
              rows="2"
              [(ngModel)]="preventiveActionSummary"
              [disabled]="saving()"
            ></textarea>
          </label>

          <label class="field"
            ><span>Owner</span
            ><input type="text" [(ngModel)]="preventiveActionOwner" [disabled]="saving()"
          /></label>

          <label class="field"
            ><span>Target Date</span
            ><input type="date" [(ngModel)]="preventiveActionTargetDate" [disabled]="saving()"
          /></label>

          <label class="field"
            ><span>Completed Date</span
            ><input type="date" [(ngModel)]="preventiveActionCompletedDate" [disabled]="saving()"
          /></label>

          <label class="field"
            ><span>Success Criteria</span
            ><textarea
              rows="2"
              [(ngModel)]="preventiveActionSuccessCriteria"
              [disabled]="saving()"
            ></textarea>
          </label>

          <label class="field">
            <span>Status</span>

            <select [(ngModel)]="preventiveActionStatus" [disabled]="saving()">
              @for (opt of capaStatusOptions(); track opt.value) {
                <option [value]="opt.value">{{ opt.label }}</option>
              }
            </select>
          </label>

          @if (d.preventiveActionOverdue) {
            <p class="overdue-flag">Preventive action is overdue</p>
          }

          <button
            type="button"
            class="btn-save"
            (click)="emitPreventiveSave()"
            [disabled]="saving()"
          >
            Save Preventive Action
          </button>
        </section>

        <section class="section">
          <h3>Effectiveness Review</h3>

          <label class="field">
            <span>Status</span>

            <select [(ngModel)]="effectivenessReviewStatus" [disabled]="saving()">
              @for (opt of effectivenessOptions(); track opt.value) {
                <option [value]="opt.value">{{ opt.label }}</option>
              }
            </select>
          </label>

          <div class="schedule-row">
            @for (opt of reviewScheduleOptions(); track opt.value) {
              <button
                type="button"
                class="btn-schedule"
                (click)="scheduleReview(reviewDays(opt.value))"
                [disabled]="saving()"
              >
                {{ opt.label }}
              </button>
            }
          </div>

          <label class="field"
            ><span>Scheduled Date</span
            ><input
              type="date"
              [(ngModel)]="effectivenessReviewScheduledDate"
              [disabled]="saving()"
          /></label>

          <label class="field"
            ><span>Review Date</span
            ><input type="date" [(ngModel)]="effectivenessReviewDate" [disabled]="saving()"
          /></label>

          <label class="field"
            ><span>Reviewer</span
            ><input type="text" [(ngModel)]="effectivenessReviewer" [disabled]="saving()"
          /></label>

          <label class="field"
            ><span>Outcome</span
            ><input type="text" [(ngModel)]="effectivenessReviewOutcome" [disabled]="saving()"
          /></label>

          <label class="field"
            ><span>Notes</span
            ><textarea
              rows="2"
              [(ngModel)]="effectivenessReviewNotes"
              [disabled]="saving()"
            ></textarea>
          </label>

          <button
            type="button"
            class="btn-save"
            (click)="emitEffectivenessSave()"
            [disabled]="saving()"
          >
            Save Effectiveness Review
          </button>
        </section>

        @if (d.isRecurringIssue || d.recurrenceReason) {
          <section class="section section--recurrence">
            <h3>Recurrence Analysis</h3>

            <p class="recurrence-flag">Recurring Issue</p>

            @if (d.recurrenceReason) {
              <p class="recurrence-reason">{{ d.recurrenceReason }}</p>
            }

            @if (d.recurrenceDetectedAt) {
              <p class="muted">Detected {{ d.recurrenceDetectedAt | date: 'medium' }}</p>
            }
          </section>
        }

        <section class="section">
          <h3>5 Why Analysis</h3>

          @for (n of whyNumbers; track n) {
            <label class="field"
              ><span>Why {{ n }}</span
              ><textarea rows="2" [(ngModel)]="whyValues[n - 1]" [disabled]="saving()"></textarea>
            </label>
          }

          <label class="field"
            ><span>Final Root Cause</span
            ><textarea rows="2" [(ngModel)]="finalRootCause" [disabled]="saving()"></textarea>
          </label>
        </section>

        <section class="section">
          <h3>Risk Assessment</h3>

          <div class="risk-row">
            <label class="field"
              ><span>Likelihood (1–5)</span
              ><input
                type="number"
                min="1"
                max="5"
                [(ngModel)]="riskLikelihood"
                [disabled]="saving()"
            /></label>

            <label class="field"
              ><span>Impact (1–5)</span
              ><input type="number" min="1" max="5" [(ngModel)]="riskImpact" [disabled]="saving()"
            /></label>
          </div>

          @if (computedRiskScore()) {
            <p class="risk-preview">
              Score: {{ computedRiskScore() }} · Level: {{ computedRiskLevel() }}
            </p>
          }
        </section>

        <section class="section">
          <h3>Remediation Plan</h3>

          <label class="field"
            ><span>Action Plan</span
            ><textarea rows="2" [(ngModel)]="actionPlan" [disabled]="saving()"></textarea>
          </label>

          <label class="field"
            ><span>Expected Outcome</span
            ><textarea rows="2" [(ngModel)]="expectedOutcome" [disabled]="saving()"></textarea>
          </label>

          <label class="field"
            ><span>Target Completion</span
            ><input type="date" [(ngModel)]="remediationTargetCompletion" [disabled]="saving()"
          /></label>

          <label class="field"
            ><span>Success Criteria</span
            ><textarea rows="2" [(ngModel)]="successCriteria" [disabled]="saving()"></textarea>
          </label>

          <button
            type="button"
            class="btn-save"
            (click)="emitRemediationSave()"
            [disabled]="saving()"
          >
            Save Remediation
          </button>
        </section>

        @if (d.originType === 'AuditFinding') {
          <section class="section">
            <h3>Origin</h3>

            <dl class="meta">
              <div>
                <dt>Source</dt>
                <dd>Audit Finding</dd>
              </div>

              @if (d.originFindingNumber) {
                <div>
                  <dt>Finding Number</dt>
                  <dd>{{ d.originFindingNumber }}</dd>
                </div>
              }

              @if (d.promotedAt) {
                <div>
                  <dt>Promoted Date</dt>
                  <dd>{{ d.promotedAt | date: 'medium' }}</dd>
                </div>
              }
            </dl>

            @if (d.originFindingId) {
              <div class="actions">
                <a
                  [routerLink]="['/app/audit/findings']"
                  [queryParams]="{ findingId: d.originFindingId }"
                  >Open Finding</a
                >
              </div>
            }
          </section>
        }

        <section class="section">
          <h3>Primary Remediation Task</h3>

          @if (d.primaryRemediationTaskId) {
            <p class="task-title">{{ d.primaryTaskTitle }}</p>

            <dl class="meta">
              <div>
                <dt>Status</dt>
                <dd>{{ d.primaryTaskStatus }}</dd>
              </div>

              <div>
                <dt>Due</dt>
                <dd>{{ d.dueDate ? (d.dueDate | date: 'mediumDate') : '—' }}</dd>
              </div>
            </dl>

            <div class="actions">
              <a [routerLink]="['/app/tasks']" [queryParams]="{ search: d.objectiveElementCode }"
                >Open Task</a
              >
            </div>
          } @else {
            <p class="muted">No primary remediation task linked.</p>
          }
        </section>

        <section class="section">
          <h3>Evidence &amp; Verification</h3>

          <dl class="meta">
            <div>
              <dt>Evidence Count</dt>
              <dd>{{ d.evidenceCount }}</dd>
            </div>

            <div>
              <dt>Evidence Status</dt>
              <dd><app-evidence-status-badge [status]="d.evidenceStatus" /></dd>
            </div>

            <div>
              <dt>Verification</dt>
              <dd><app-evidence-status-badge [status]="d.verificationStatus" /></dd>
            </div>
          </dl>

          <div class="actions">
            <a [routerLink]="['/app/evidence']" [queryParams]="evidenceParams(d)">Open Evidence</a>

            <a [routerLink]="['/app/evidence/review-queue']" [queryParams]="reviewQueueParams(d)"
              >Review Queue</a
            >
          </div>
        </section>

        @if (d.timeline.length) {
          <section class="section">
            <h3>Timeline</h3>

            <ul class="timeline">
              @for (entry of d.timeline; track entry.occurredAt + entry.action) {
                <li>
                  <span class="tl-action">{{ formatTimelineAction(entry.action) }}</span>

                  <span class="tl-meta"
                    >{{ entry.occurredAt | date: 'medium' }} · {{ entry.actorType }}</span
                  >
                </li>
              }
            </ul>
          </section>
        }

        <footer class="panel__foot">
          <app-compliance-classification-badge classification="complianceDeficiency" />
        </footer>
      </aside>
    }
  `,

  styles: [
    `
      .panel {
        position: fixed;

        top: var(--app-shell-header-height, var(--header-height, 4rem));

        right: 0;

        width: min(440px, 100vw);

        height: calc(100dvh - var(--app-shell-header-height, var(--header-height, 4rem)));

        background: var(--card-bg, #fff);

        border-left: 1px solid var(--border-color, #e5e7eb);

        box-shadow: -4px 0 24px rgba(0, 0, 0, 0.08);

        overflow-y: auto;

        z-index: var(--z-sticky, 100);

        padding: 0 0 24px;
      }

      .panel__head {
        display: flex;

        justify-content: space-between;

        align-items: flex-start;

        padding: 20px 20px 12px;

        border-bottom: 1px solid var(--border-color, #e5e7eb);

        position: sticky;

        top: 0;

        background: inherit;

        z-index: 1;
      }

      .panel__code {
        font-size: 11px;
        color: var(--text-secondary);
      }

      .panel__capa {
        display: inline-block;
        margin-left: 8px;
        font-size: 11px;
        font-weight: 600;
        color: var(--color-primary-hover);
        background: var(--color-primary-100);
        padding: 2px 8px;
        border-radius: 4px;
      }

      .section--ownership .actions {
        display: flex;
        gap: 8px;
      }

      .btn-reject {
        padding: 8px 14px;
        border-radius: 6px;
        border: 1px solid #fecaca;
        background: #fff;
        color: #b91c1c;
        font-size: 13px;
        cursor: pointer;
      }

      .btn-reject:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      h2 {
        margin: 4px 0 0;
        font-size: var(--font-size-section-title);
      }

      .close {
        border: none;
        background: none;
        font-size: 24px;
        cursor: pointer;
        color: var(--text-secondary);
      }

      .lifecycle {
        display: flex;
        gap: 4px;
        padding: 16px 12px;
        overflow-x: auto;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }

      .lc-step {
        flex: 1;
        min-width: 48px;
        text-align: center;
        opacity: 0.4;
      }

      .lc-step--done,
      .lc-step--active {
        opacity: 1;
      }

      .lc-dot {
        display: block;
        width: 10px;
        height: 10px;
        border-radius: 50%;
        background: var(--border-color, #d1d5db);
        margin: 0 auto 4px;
      }

      .lc-step--done .lc-dot,
      .lc-step--active .lc-dot {
        background: var(--primary-color, var(--color-primary));
      }

      .lc-label {
        font-size: 9px;
        line-height: 1.2;
        display: block;
      }

      .section {
        padding: 16px 20px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }

      .section--routing {
        background: #f0f9ff;
      }

      .routing-reason {
        font-size: 13px;
        color: var(--text-secondary, #6b7280);
        margin: 0 0 12px;
      }

      .conf {
        font-size: 12px;
        font-weight: 600;
        padding: 2px 8px;
        border-radius: 4px;
      }

      .conf--high {
        background: #d1fae5;
        color: #047857;
      }

      .conf--medium {
        background: #fef3c7;
        color: #b45309;
      }

      .conf--low {
        background: #f3f4f6;
        color: #6b7280;
      }

      .conf--none {
        background: #f3f4f6;
        color: #9ca3af;
      }

      .btn-accept {
        padding: 8px 14px;

        border-radius: 6px;

        border: none;

        background: var(--color-primary);

        color: #fff;

        font-size: 13px;

        cursor: pointer;
      }

      .btn-accept:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .section--sla {
        background: #fefce8;
      }

      .sla {
        font-size: 12px;
        font-weight: 600;
        padding: 2px 8px;
        border-radius: 4px;
      }

      .sla--ontrack {
        background: #d1fae5;
        color: #047857;
      }

      .sla--warning {
        background: #fef3c7;
        color: #b45309;
      }

      .sla--atrisk {
        background: #ffedd5;
        color: #c2410c;
      }

      .sla--breached {
        background: #fee2e2;
        color: #b91c1c;
      }

      .sla--closed {
        background: #f3f4f6;
        color: #6b7280;
      }

      .escalation-history {
        margin-top: 12px;
      }

      .history-row {
        margin-bottom: 12px;
        padding-bottom: 12px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }

      .hint {
        font-size: 12px;
        color: var(--text-secondary);
        margin: 4px 0 8px;
      }

      h3 {
        margin: 0 0 10px;
        font-size: 12px;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--text-secondary);
      }

      .meta {
        display: grid;
        gap: 8px;
        margin: 0 0 12px;
      }

      .meta div {
        display: grid;
        grid-template-columns: 120px 1fr;
        gap: 8px;
        font-size: 13px;
      }

      dt {
        color: var(--text-secondary);
      }

      dd {
        margin: 0;
      }

      .field {
        display: block;
        margin-bottom: 10px;
        font-size: 12px;
      }

      .field span {
        display: block;
        margin-bottom: 4px;
        color: var(--text-secondary);
      }

      .field input,
      .field select,
      .field textarea {
        width: 100%;
        box-sizing: border-box;
        padding: 8px;
        border-radius: 6px;

        border: 1px solid var(--border-color, #e5e7eb);
        font: inherit;
      }

      .risk-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 10px;
      }

      .risk-preview {
        font-size: 13px;
        margin: 0 0 8px;
        font-weight: 500;
      }

      .risk {
        font-size: 12px;
        font-weight: 600;
        padding: 2px 6px;
        border-radius: 4px;
      }

      .risk--low {
        background: #ecfdf5;
        color: #047857;
      }

      .risk--medium {
        background: #fef9c3;
        color: #a16207;
      }

      .risk--high {
        background: #ffedd5;
        color: #c2410c;
      }

      .risk--critical {
        background: #fef2f2;
        color: #b91c1c;
      }

      .sev--critical {
        color: #dc2626;
        font-weight: 600;
      }

      .btn-save {
        margin-top: 8px;
        padding: 8px 14px;
        border-radius: 6px;
        border: none;

        background: var(--primary-color, var(--color-primary));
        color: #fff;
        cursor: pointer;
        font-size: 13px;
      }

      .btn-save:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .actions {
        display: flex;
        gap: 12px;
        margin-top: 12px;
        flex-wrap: wrap;
      }

      .actions a {
        font-size: 13px;
        color: var(--primary-color, var(--color-primary));
        text-decoration: none;
      }

      .timeline {
        list-style: none;
        margin: 0;
        padding: 0;
      }

      .timeline li {
        padding: 8px 0;
        border-bottom: 1px solid var(--border-color, #f3f4f6);
        font-size: 12px;
      }

      .tl-action {
        display: block;
        font-weight: 500;
      }

      .tl-meta {
        color: var(--text-secondary);
        font-size: 11px;
      }

      .muted {
        color: var(--text-secondary);
        font-size: 13px;
      }

      .task-title {
        font-weight: 500;
        margin: 0 0 8px;
      }

      .panel__foot {
        padding: 16px 20px;
      }

      .capa-lifecycle {
        flex-wrap: wrap;
      }

      .overdue-flag {
        color: #dc2626;
        font-size: 12px;
        font-weight: 600;
        margin: 0 0 8px;
      }

      .section--ai-capa .ai-capa-card {
        margin-top: 10px;
        padding: 10px;
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        background: #fafafa;
      }

      .ai-capa-kind {
        font-size: 11px;
        font-weight: 600;
        color: #6b7280;
        text-transform: uppercase;
      }

      .ai-capa-title {
        font-size: 13px;
        font-weight: 600;
        margin-top: 4px;
      }

      .ai-capa-proposal {
        font-size: 12px;
        margin: 6px 0;
        line-height: 1.4;
      }

      .schedule-row {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin-bottom: 12px;
      }

      .btn-schedule {
        padding: 6px 10px;

        border-radius: 6px;

        border: 1px solid var(--border-color, #e5e7eb);

        background: var(--card-bg, #fff);

        font-size: 12px;

        cursor: pointer;
      }

      .recurrence-flag {
        color: #2f6bff;
        font-weight: 600;
        margin: 0 0 6px;
      }

      .recurrence-reason {
        font-size: 13px;
        margin: 0 0 6px;
      }
    `,
  ],
})
export class DccDetailPanelComponent {
  private readonly aiApi = inject(AiAssistantApiService);

  readonly detail = input<DeficiencyWorkspaceDetail | null>(null);

  readonly routingSuggestion = input<AssignmentRoutingSuggestion | null>(null);

  readonly routingLoading = input(false);

  readonly saving = input(false);

  readonly capaSuggestLoading = signal(false);

  readonly capaSuggestError = signal<string | null>(null);

  readonly capaSuggestions = signal<CapaSuggestionItemDto[]>([]);

  readonly close = output<void>();

  readonly rcaSave = output<PatchDeficiencyRcaRequest>();

  readonly remediationSave = output<PatchDeficiencyRemediationRequest>();

  readonly correctiveSave = output<PatchDeficiencyCorrectiveActionRequest>();

  readonly preventiveSave = output<PatchDeficiencyPreventiveActionRequest>();

  readonly effectivenessSave = output<PatchDeficiencyEffectivenessReviewRequest>();

  readonly acceptRouting = output<void>();

  readonly acknowledgeEscalation = output<string>();

  readonly acceptOwnership = output<void>();

  readonly rejectOwnership = output<void>();

  private readonly configuration = inject(PlatformConfigurationService);

  private readonly risk = inject(RiskConfigurationService);

  /** The lifecycle rail — statuses flagged as steps, not every status the API can emit. */
  readonly lifecycleSteps = this.configuration.valuesWith(
    CONFIGURATION_CATEGORY.deficiencyStatus,
    'lifecycleStep',
  );

  readonly rootCauseOptions = this.configuration.options(CONFIGURATION_CATEGORY.rootCauseCategory);

  readonly effectivenessOptions = this.configuration.options(
    CONFIGURATION_CATEGORY.effectivenessReviewStatus,
  );

  readonly capaStatusOptions = this.configuration.options(CONFIGURATION_CATEGORY.capaActionStatus);

  readonly reviewScheduleOptions = this.configuration.options(
    CONFIGURATION_CATEGORY.effectivenessReviewSchedule,
  );

  readonly whyNumbers = [1, 2, 3, 4, 5];

  rootCauseCategory = '';

  problemStatement = '';

  rootCauseSummary = '';

  impactDescription = '';

  correctiveActionSummary = '';

  preventiveActionSummary = '';

  correctiveActionOwner = '';

  correctiveActionStartDate = '';

  correctiveActionTargetDate = '';

  correctiveActionCompletedDate = '';

  correctiveActionStatus = 'NotStarted';

  preventiveActionOwner = '';

  preventiveActionTargetDate = '';

  preventiveActionCompletedDate = '';

  preventiveActionSuccessCriteria = '';

  preventiveActionStatus = 'NotStarted';

  effectivenessReviewScheduledDate = '';

  effectivenessReviewDate = '';

  effectivenessReviewer = '';

  effectivenessReviewNotes = '';

  effectivenessReviewOutcome = '';

  whyValues = ['', '', '', '', ''];

  finalRootCause = '';

  department = '';

  targetResolutionDate = '';

  riskLikelihood: number | null = null;

  riskImpact: number | null = null;

  actionPlan = '';

  expectedOutcome = '';

  remediationTargetCompletion = '';

  successCriteria = '';

  effectivenessReviewStatus = 'NotRequired';

  requestCapaSuggestions(detail: DeficiencyWorkspaceDetail): void {
    this.capaSuggestError.set(null);
    this.capaSuggestLoading.set(true);
    this.capaSuggestions.set([]);
    this.aiApi
      .suggestCapa({
        deficiencyId: detail.id,
        deficiencySummary: detail.title || detail.deficiencyCode,
        rootCauseHint: this.rootCauseSummary || detail.rootCauseSummary || null,
        useLlmAssist: false,
      })
      .subscribe({
        next: (result) => {
          this.capaSuggestions.set(result.suggestions ?? []);
          this.capaSuggestLoading.set(false);
        },
        error: () => {
          this.capaSuggestError.set('Unable to load CAPA suggestions right now.');
          this.capaSuggestLoading.set(false);
        },
      });
  }

  lifecycleIndex(status: string): number {
    return this.configuration.lifecycleIndex(CONFIGURATION_CATEGORY.deficiencyStatus, status);
  }

  /**
   * Routing-confidence badge class.
   *
   * An unrecognised confidence resolves to the category's designated fallback rather than to the
   * last arm of an `if` ladder — the ladder made "no suggestion" and "a level we do not know" look
   * identical.
   */
  confidenceClass(confidence: string): string {
    return this.configuration.attributeOrFallback(
      CONFIGURATION_CATEGORY.assignmentConfidence,
      confidence,
      'badgeClass',
    );
  }

  formatConfidence(confidence: string): string {
    return this.configuration.label(
      CONFIGURATION_CATEGORY.assignmentConfidence,
      confidence,
      confidence,
    );
  }

  slaStatusClass(status?: string | null): string {
    return this.configuration.attributeOrFallback(
      CONFIGURATION_CATEGORY.slaStatus,
      status,
      'badgeClass',
    );
  }

  formatSlaStatus(status?: string | null): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.slaStatus, status);
  }

  formatStatus(status: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.deficiencyStatus, status, status);
  }

  formatSeverity(severity: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.deficiencySeverity, severity, severity);
  }

  severityClass(severity: string): string {
    const token = this.configuration.attribute(
      CONFIGURATION_CATEGORY.deficiencySeverity,
      severity,
      'badgeToken',
      '',
    );
    return token ? `sev sev--${token}` : 'sev';
  }

  formatStage(stage: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.maturityStage, stage, stage);
  }

  formatRiskLevel(level: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.riskLevel, level, level);
  }

  /** The day offset a review-schedule option submits, carried as configuration, not in its name. */
  reviewDays(option: string): number {
    return this.configuration.attribute(
      CONFIGURATION_CATEGORY.effectivenessReviewSchedule,
      option,
      'days',
      0,
    );
  }

  formatTimelineAction(action: string): string {
    return action.replace(/([A-Z])/g, ' $1').trim();
  }

  riskClass(level: string): string {
    return this.configuration.attribute(CONFIGURATION_CATEGORY.riskLevel, level, 'badgeClass', '');
  }

  computedRiskScore(): number | null {
    return this.risk.score(this.riskLikelihood, this.riskImpact);
  }

  /**
   * Risk band for the computed score.
   *
   * Delegated to {@link RiskConfigurationService} so this panel, the risk matrix and the risk form
   * band a score identically. The `if` ladder this replaced (DEF-CFG-003) made the CAPA panel the
   * third place with its own opinion about where High begins.
   */
  computedRiskLevel(): string {
    return this.risk.bandLabel(this.computedRiskScore());
  }

  evidenceParams(d: DeficiencyWorkspaceDetail): Record<string, string> {
    return {
      source: 'deficiency-command-center',

      objectiveElementId: d.objectiveElementId,

      objectiveElementCode: d.objectiveElementCode,
    };
  }

  reviewQueueParams(d: DeficiencyWorkspaceDetail): Record<string, string> {
    return { objectiveElementId: d.objectiveElementId, search: d.objectiveElementCode };
  }

  syncFromDetail(d: DeficiencyWorkspaceDetail): void {
    this.rootCauseCategory = d.rootCauseCategory ?? '';

    this.problemStatement = d.problemStatement ?? '';

    this.rootCauseSummary = d.rootCauseSummary ?? '';

    this.impactDescription = d.impactDescription ?? '';

    this.correctiveActionSummary = d.correctiveActionSummary ?? '';

    this.preventiveActionSummary = d.preventiveActionSummary ?? '';

    this.correctiveActionOwner = d.correctiveActionOwner ?? d.assignedOwner ?? '';

    this.correctiveActionStartDate = toDateInput(d.correctiveActionStartDate);

    this.correctiveActionTargetDate = toDateInput(d.correctiveActionTargetDate);

    this.correctiveActionCompletedDate = toDateInput(d.correctiveActionCompletedDate);

    this.correctiveActionStatus = d.correctiveActionStatus ?? 'NotStarted';

    this.preventiveActionOwner = d.preventiveActionOwner ?? '';

    this.preventiveActionTargetDate = toDateInput(d.preventiveActionTargetDate);

    this.preventiveActionCompletedDate = toDateInput(d.preventiveActionCompletedDate);

    this.preventiveActionSuccessCriteria =
      d.preventiveActionSuccessCriteria ?? d.successCriteria ?? '';

    this.preventiveActionStatus = d.preventiveActionStatus ?? 'NotStarted';

    this.whyValues = [d.why1 ?? '', d.why2 ?? '', d.why3 ?? '', d.why4 ?? '', d.why5 ?? ''];

    this.finalRootCause = d.finalRootCause ?? '';

    this.department = d.department ?? '';

    this.targetResolutionDate = toDateInput(d.targetResolutionDate);

    this.riskLikelihood = d.riskLikelihood ?? null;

    this.riskImpact = d.riskImpact ?? null;

    this.actionPlan = d.actionPlan ?? '';

    this.expectedOutcome = d.expectedOutcome ?? '';

    this.remediationTargetCompletion = toDateInput(d.remediationTargetCompletion);

    this.successCriteria = d.successCriteria ?? '';

    this.effectivenessReviewStatus = d.effectivenessReviewStatus ?? 'NotRequired';

    this.effectivenessReviewScheduledDate = toDateInput(d.effectivenessReviewScheduledDate);

    this.effectivenessReviewDate = toDateInput(d.effectivenessReviewDate);

    this.effectivenessReviewer = d.effectivenessReviewer ?? '';

    this.effectivenessReviewNotes = d.effectivenessReviewNotes ?? '';

    this.effectivenessReviewOutcome = d.effectivenessReviewOutcome ?? '';
  }

  emitRcaSave(): void {
    this.rcaSave.emit({
      rootCauseCategory: this.rootCauseCategory || undefined,

      problemStatement: this.problemStatement || undefined,

      rootCauseSummary: this.rootCauseSummary || undefined,

      impactDescription: this.impactDescription || undefined,

      why1: this.whyValues[0] || undefined,

      why2: this.whyValues[1] || undefined,

      why3: this.whyValues[2] || undefined,

      why4: this.whyValues[3] || undefined,

      why5: this.whyValues[4] || undefined,

      finalRootCause: this.finalRootCause || undefined,
    });
  }

  emitRemediationSave(): void {
    this.remediationSave.emit({
      riskLikelihood: this.riskLikelihood ?? undefined,

      riskImpact: this.riskImpact ?? undefined,

      department: this.department || undefined,

      targetResolutionDate: this.targetResolutionDate || undefined,

      actionPlan: this.actionPlan || undefined,

      expectedOutcome: this.expectedOutcome || undefined,

      remediationTargetCompletion: this.remediationTargetCompletion || undefined,

      successCriteria: this.successCriteria || undefined,
    });
  }

  emitCorrectiveSave(): void {
    this.correctiveSave.emit({
      summary: this.correctiveActionSummary || undefined,
      owner: this.correctiveActionOwner || undefined,
      startDate: this.correctiveActionStartDate || undefined,
      targetDate: this.correctiveActionTargetDate || undefined,
      completedDate: this.correctiveActionCompletedDate || undefined,
      status: this.correctiveActionStatus || undefined,
    });
  }

  emitPreventiveSave(): void {
    this.preventiveSave.emit({
      summary: this.preventiveActionSummary || undefined,
      owner: this.preventiveActionOwner || undefined,
      targetDate: this.preventiveActionTargetDate || undefined,
      completedDate: this.preventiveActionCompletedDate || undefined,
      successCriteria: this.preventiveActionSuccessCriteria || undefined,
      status: this.preventiveActionStatus || undefined,
    });
  }

  emitEffectivenessSave(): void {
    this.effectivenessSave.emit({
      status: this.effectivenessReviewStatus || undefined,
      scheduledDate: this.effectivenessReviewScheduledDate || undefined,
      reviewDate: this.effectivenessReviewDate || undefined,
      reviewer: this.effectivenessReviewer || undefined,
      notes: this.effectivenessReviewNotes || undefined,
      outcome: this.effectivenessReviewOutcome || undefined,
    });
  }

  scheduleReview(days: number): void {
    this.effectivenessSave.emit({
      reviewDaysOffset: days,
      status: 'Scheduled',
    });
  }

  /**
   * Whether the owner may still accept or reject this assignment.
   *
   * The state that permits it is an attribute of the status, not the literal `'Assigned'`
   * (DEF-CFG-002). Both `status` and `lifecycleStatus` are checked because the API emits the code on
   * one and the display form on the other, and either may be the one populated.
   */
  canAcceptOwnership(d: DeficiencyWorkspaceDetail): boolean {
    const awaiting = (status: string | null | undefined) =>
      this.configuration.attribute(
        CONFIGURATION_CATEGORY.deficiencyStatus,
        status,
        'awaitingOwnerAcceptance',
        false,
      );

    return (
      (awaiting(d.status) || awaiting(d.lifecycleStatus)) &&
      !d.ownerAcceptedAt &&
      !d.ownerAcceptanceRejected
    );
  }
}

function toDateInput(value?: string | null): string {
  if (!value) return '';

  return value.slice(0, 10);
}
