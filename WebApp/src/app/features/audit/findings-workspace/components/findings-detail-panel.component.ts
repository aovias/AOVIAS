import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import {
  type FindingComplianceContextOption,
  type FindingPromotionStatus,
  type FindingsWorkspaceDetail,
} from '../models/findings-workspace.model';
import { FindingsWorkspaceService } from '../services/findings-workspace.service';
import type { AssignmentRoutingSuggestion } from '../../deficiency-command-center/models/deficiency-command-center.model';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-findings-detail-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (detail(); as d) {
      <aside class="panel">
        <header class="panel__head">
          <div>
            <span class="panel__num">{{ d.findingNumber }}</span>
            <h2>{{ d.title }}</h2>
            <span class="ready-badge" [attr.data-ready]="d.promotionReadiness">{{
              d.promotionReadiness
            }}</span>
          </div>
          <button type="button" class="close" (click)="close.emit()" aria-label="Close">×</button>
        </header>

        <section class="lifecycle">
          @for (step of d.lifecycleSteps; track step.key) {
            <div
              class="lc-step"
              [class.lc-step--done]="step.isComplete"
              [class.lc-step--active]="step.isCurrent"
            >
              <span class="lc-dot"></span>
              <span class="lc-label">{{ step.label }}</span>
              @if (step.occurredAt) {
                <span class="lc-time">{{ step.occurredAt | date: 'short' }}</span>
              }
            </div>
          }
        </section>

        @if (promotionStatus(); as ps) {
          <section class="section section--promotion">
            <h3>Promotion To Deficiency</h3>
            <p class="promotion-status">
              Status:
              <span class="ready-badge" [attr.data-ready]="ps.eligibilityStatus">{{
                ps.eligibilityStatus
              }}</span>
            </p>
            @if (ps.validationMessages.length) {
              <ul class="validation-list">
                @for (msg of ps.validationMessages; track msg) {
                  <li>{{ msg }}</li>
                }
              </ul>
            }
            @if (promotionHistory(d).length) {
              <div class="promotion-history">
                <h4>Promotion History</h4>
                @for (h of promotionHistory(d); track h.occurredAt + h.action) {
                  <div class="history-row">
                    <strong>{{ h.action }}</strong>
                    <span class="muted"
                      >{{ h.actorType }} · {{ h.occurredAt | date: 'medium' }}</span
                    >
                  </div>
                }
              </div>
            }
            @if (ps.allowPromotion) {
              @if (routingSuggestion(); as r) {
                <div class="routing-box">
                  <h4>Suggested Owner After Promotion</h4>
                  <p class="routing-reason">{{ r.routingReason }}</p>
                  <dl class="routing-meta">
                    <div>
                      <dt>Department</dt>
                      <dd>
                        {{ r.recommendedDepartmentName || r.normalizedDepartmentCode || '—' }}
                      </dd>
                    </div>
                    <div>
                      <dt>Owner</dt>
                      <dd>{{ r.recommendedAssigneeUserId || 'Configure department ownership' }}</dd>
                    </div>
                    <div>
                      <dt>Confidence</dt>
                      <dd>{{ r.confidence }}</dd>
                    </div>
                  </dl>
                </div>
              }
              <button
                type="button"
                class="btn btn--promote"
                (click)="promote.emit()"
                [disabled]="promotionSaving()"
              >
                {{ promotionSaving() ? 'Promoting…' : 'Promote To Deficiency' }}
              </button>
              <p class="hint">
                Creates a deficiency and primary remediation task — no remediation in Findings.
              </p>
            }
            @if (ps.isPromoted && ps.promotedDeficiencyId) {
              <div class="actions promotion-actions">
                <a
                  [routerLink]="['/app/audit/capa']"
                  [queryParams]="{ deficiencyId: ps.promotedDeficiencyId }"
                >
                  Open Deficiency
                </a>
                @if (ps.primaryRemediationTaskId) {
                  <a
                    [routerLink]="['/app/tasks']"
                    [queryParams]="{ taskId: ps.primaryRemediationTaskId }"
                  >
                    Open Remediation Task
                  </a>
                }
              </div>
            }
          </section>
        } @else if (promotionLoading()) {
          <section class="section">
            <p class="muted">Loading promotion status…</p>
          </section>
        }

        <section class="section">
          <h3>Lifecycle Status</h3>
          <select
            [ngModel]="d.status"
            (ngModelChange)="statusChange.emit($event)"
            [disabled]="statusSaving()"
          >
            @for (s of statusOptions(); track s.value) {
              <option [value]="s.value">{{ s.label }}</option>
            }
          </select>
          <p class="hint">Discovery lifecycle only — no remediation controls.</p>
        </section>

        <section class="section">
          <h3>Summary</h3>
          <p>{{ d.description || 'No description.' }}</p>
          <dl class="meta">
            <div>
              <dt>Type</dt>
              <dd>{{ formatType(d.classification) }}</dd>
            </div>
            <div>
              <dt>Source</dt>
              <dd>{{ formatSource(d.source) }}</dd>
            </div>
            <div>
              <dt>Severity</dt>
              <dd>{{ d.severity }}</dd>
            </div>
            <div>
              <dt>Department</dt>
              <dd>{{ d.department }}</dd>
            </div>
            <div>
              <dt>Owner</dt>
              <dd>{{ d.owner || '—' }}</dd>
            </div>
            <div>
              <dt>Status</dt>
              <dd>{{ formatStatus(d.status) }}</dd>
            </div>
            <div>
              <dt>Clause</dt>
              <dd>{{ d.clauseReference || '—' }}</dd>
            </div>
          </dl>
        </section>

        <section class="section">
          <h3>Compliance Context</h3>
          <dl class="meta">
            <div>
              <dt>Chapter</dt>
              <dd>{{ d.chapterCode ? d.chapterCode + ' — ' + d.chapterName : '—' }}</dd>
            </div>
            <div>
              <dt>Standard</dt>
              <dd>{{ d.standardCode ? d.standardCode + ' — ' + d.standardName : '—' }}</dd>
            </div>
            <div>
              <dt>Objective Element</dt>
              <dd>
                {{
                  d.objectiveElementCode
                    ? d.objectiveElementCode + ' — ' + d.objectiveElementName
                    : '—'
                }}
              </dd>
            </div>
          </dl>

          <!--
            DEF-M3-001 — classification happens at the review desk, not only at capture. An auditor
            on the ward often cannot cite the objective element; without this the finding could be
            accepted and then never promoted.
          -->
          @if (canMap()) {
            <div class="mapping">
              <label
                >Search Objective Element
                <input
                  placeholder="Search OE, standard, chapter…"
                  [(ngModel)]="oeSearch"
                  (ngModelChange)="onOeSearch($event)"
                />
              </label>
              @if (oeOptions().length) {
                <label
                  >{{ d.objectiveElementCode ? 'Re-map to' : 'Map to' }} Objective Element
                  <select [(ngModel)]="selectedOeId">
                    <option value="">— Select —</option>
                    @for (oe of oeOptions(); track oe.objectiveElementId) {
                      <option [value]="oe.objectiveElementId">
                        {{ oe.chapterCode }} / {{ oe.standardCode }} /
                        {{ oe.objectiveElementCode }} — {{ oe.objectiveElementName }}
                      </option>
                    }
                  </select>
                </label>
              }
              <button
                type="button"
                class="btn"
                [disabled]="!selectedOeId || mappingSaving()"
                (click)="submitMapping()"
              >
                {{ mappingSaving() ? 'Saving…' : 'Save Compliance Mapping' }}
              </button>
            </div>
          } @else {
            <p class="mapping__locked">
              {{
                d.promotedDeficiencyId
                  ? 'Mapping is fixed once the finding has been promoted to a CAPA.'
                  : 'Mapping is fixed once the finding is closed.'
              }}
            </p>
          }
        </section>

        @if (d.auditDate || d.auditorName || d.auditCycle) {
          <section class="section">
            <h3>Audit Context</h3>
            <dl class="meta">
              @if (d.auditDate) {
                <div>
                  <dt>Audit Date</dt>
                  <dd>{{ d.auditDate | date: 'mediumDate' }}</dd>
                </div>
              }
              @if (d.auditorName) {
                <div>
                  <dt>Auditor</dt>
                  <dd>{{ d.auditorName }}</dd>
                </div>
              }
              @if (d.auditCycle) {
                <div>
                  <dt>Audit Cycle</dt>
                  <dd>{{ d.auditCycle }}</dd>
                </div>
              }
              <div>
                <dt>Date Raised</dt>
                <dd>{{ d.dateRaised | date: 'mediumDate' }}</dd>
              </div>
            </dl>
          </section>
        }

        @if (d.observation || d.requirement || d.impact) {
          <section class="section">
            <h3>Observation</h3>
            @if (d.observation) {
              <p>{{ d.observation }}</p>
            }
            @if (d.requirement) {
              <p><strong>Requirement:</strong> {{ d.requirement }}</p>
            }
            @if (d.impact) {
              <p><strong>Impact:</strong> {{ d.impact }}</p>
            }
          </section>
        }

        @if (d.recommendation) {
          <section class="section">
            <h3>Recommendation</h3>
            <p>{{ d.recommendation }}</p>
          </section>
        }

        <section class="section">
          <h3>Attachments ({{ d.attachments.length }})</h3>
          @if (d.attachments.length) {
            <ul class="list">
              @for (a of d.attachments; track a.id) {
                <li>
                  <a [href]="a.fileUrl" target="_blank" rel="noopener">{{ a.fileName }}</a>
                  <span class="muted">{{ a.attachmentType }}</span>
                </li>
              }
            </ul>
          } @else {
            <p class="muted">No attachments yet.</p>
          }
          <div class="attach-form">
            <input placeholder="File name" [(ngModel)]="attachName" />
            <input placeholder="File URL" [(ngModel)]="attachUrl" />
            <select [(ngModel)]="attachType">
              @for (t of attachmentTypes(); track t.value) {
                <option [value]="t.value">{{ t.label }}</option>
              }
            </select>
            <button
              type="button"
              (click)="submitAttachment()"
              [disabled]="!attachName.trim() || !attachUrl.trim()"
            >
              Add Attachment
            </button>
          </div>
        </section>

        <section class="section">
          <h3>Comments ({{ d.comments.length }})</h3>
          @for (c of d.comments; track c.id) {
            <div class="comment">
              <p>{{ c.body }}</p>
              <span class="muted">{{ c.createdAt | date: 'medium' }}</span>
            </div>
          }
          <textarea
            rows="3"
            placeholder="Add a review comment…"
            [(ngModel)]="commentBody"
          ></textarea>
          <button
            type="button"
            class="btn"
            (click)="submitComment()"
            [disabled]="!commentBody.trim()"
          >
            Post Comment
          </button>
        </section>

        @if (d.history.length) {
          <section class="section">
            <h3>History</h3>
            @for (h of d.history; track h.occurredAt + h.action) {
              <div class="history-row">
                <strong>{{ h.action }}</strong>
                <span class="muted">{{ h.actorType }} · {{ h.occurredAt | date: 'medium' }}</span>
                @if (h.detail) {
                  <p class="history-detail">{{ h.detail }}</p>
                }
              </div>
            }
          </section>
        }

        <section class="section">
          <h3>Cross Navigation</h3>
          <div class="actions">
            @if (d.objectiveElementId) {
              <a [routerLink]="['/app/journey/execution']" [queryParams]="journeyParams(d)"
                >Journey</a
              >
            }
            @if (d.chapterCode) {
              <a [routerLink]="['/app/journey/execution']" [queryParams]="{ search: d.chapterCode }"
                >Standards &amp; Chapters</a
              >
            }
            <a routerLink="/app/compliance-maturity">Compliance Maturity</a>
          </div>
          <p class="hint">Read-only navigation — remediation lives in Deficiency Command Center.</p>
        </section>
      </aside>
    }
  `,
  styles: [
    `
      .panel {
        position: fixed;
        top: var(--app-shell-header-height, var(--header-height, 4rem));
        right: 0;
        width: min(460px, 100vw);
        height: calc(100dvh - var(--app-shell-header-height, var(--header-height, 4rem)));
        max-height: calc(100dvh - var(--app-shell-header-height, var(--header-height, 4rem)));
        background: var(--card-bg, #fff);
        border-left: 1px solid var(--border-color, #e5e7eb);
        box-shadow: -4px 0 24px rgba(0, 0, 0, 0.08);
        overflow-y: auto;
        z-index: var(--z-sticky, 100);
        padding-bottom: 24px;
      }
      .panel__head {
        display: flex;
        justify-content: space-between;
        padding: 20px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
        position: sticky;
        top: 0;
        background: inherit;
      }
      .panel__num {
        font-size: 11px;
        color: var(--text-secondary);
      }
      h2 {
        margin: 4px 0 8px;
        font-size: var(--font-size-section-title);
      }
      .close {
        border: none;
        background: none;
        font-size: 24px;
        cursor: pointer;
      }
      .ready-badge {
        display: inline-block;
        font-size: 11px;
        font-weight: 600;
        padding: 3px 8px;
        border-radius: 4px;
        background: #f3f4f6;
      }
      .ready-badge[data-ready='Eligible'] {
        background: var(--color-primary-100);
        color: var(--color-primary-hover);
      }
      .ready-badge[data-ready='Promoted'] {
        background: #d1fae5;
        color: #047857;
      }
      .ready-badge[data-ready='Awaiting Acceptance'] {
        background: #fef3c7;
        color: #b45309;
      }
      .lifecycle {
        display: flex;
        gap: 4px;
        padding: 16px 12px;
        overflow-x: auto;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
        flex-wrap: wrap;
      }
      .lc-step {
        flex: 1;
        min-width: 56px;
        text-align: center;
        opacity: 0.35;
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
      .lc-step--active .lc-dot,
      .lc-step--done .lc-dot {
        background: var(--primary-color, var(--color-primary));
      }
      .lc-label {
        font-size: 9px;
        display: block;
      }
      .lc-time {
        font-size: 8px;
        color: var(--text-secondary);
        display: block;
        margin-top: 2px;
      }
      .section {
        padding: 16px 20px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }
      h3 {
        margin: 0 0 10px;
        font-size: 12px;
        text-transform: uppercase;
        color: var(--text-secondary);
      }
      .meta {
        display: grid;
        gap: 8px;
        font-size: 13px;
      }
      .meta div {
        display: grid;
        grid-template-columns: 120px 1fr;
      }
      dt {
        color: var(--text-secondary);
      }
      dd {
        margin: 0;
      }
      .muted {
        color: var(--text-secondary);
        font-size: 12px;
      }
      .mapping {
        display: grid;
        gap: 8px;
        margin-top: 12px;
      }
      .mapping label {
        display: grid;
        gap: 4px;
        font-size: 12px;
        color: var(--text-secondary);
      }
      .mapping input,
      .mapping select {
        font-size: 13px;
        padding: 6px 8px;
      }
      .mapping__locked {
        margin: 12px 0 0;
        font-size: 12px;
        color: var(--text-secondary);
      }
      .hint {
        margin: 8px 0 0;
        font-size: 11px;
        color: var(--text-secondary);
      }
      .list {
        list-style: none;
        padding: 0;
        margin: 0 0 12px;
        font-size: 13px;
      }
      .attach-form,
      select,
      textarea,
      input {
        width: 100%;
      }
      .attach-form {
        display: grid;
        gap: 8px;
      }
      input,
      select,
      textarea {
        padding: 8px;
        border-radius: 6px;
        border: 1px solid var(--color-border-subtle, var(--border-color, #e5e7eb));
        background: var(--color-bg-surface, var(--card-bg, #fff));
        color: var(--color-text-primary, var(--text-primary));
        font-size: 13px;
        font-family: inherit;
      }
      select {
        cursor: pointer;
        appearance: none;
        padding-right: 32px;
        background-image: url("data:image/svg+xml,%3Csvg width='12' height='12' viewBox='0 0 12 12' fill='none' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M3 4.5L6 7.5L9 4.5' stroke='%2394a3b8' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
        background-repeat: no-repeat;
        background-position: right 12px center;
      }
      select option {
        background: var(--color-bg-surface, var(--card-bg, #fff));
        color: var(--color-text-primary, var(--text-primary));
      }
      :host-context([data-theme='dark']) select {
        color-scheme: dark;
      }
      :host-context([data-theme='light']) select {
        color-scheme: light;
      }
      .btn,
      .attach-form button {
        margin-top: 8px;
        padding: 8px 12px;
        border-radius: 6px;
        border: 1px solid var(--border-color);
        background: var(--card-bg);
        cursor: pointer;
      }
      .comment {
        margin-bottom: 12px;
        padding-bottom: 12px;
        border-bottom: 1px solid var(--border-color, #f3f4f6);
      }
      .history-row {
        font-size: 13px;
        margin-bottom: 10px;
      }
      .history-detail {
        font-size: 11px;
        color: var(--text-secondary);
        margin: 4px 0 0;
        word-break: break-word;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 12px;
      }
      .actions a {
        font-size: 13px;
        color: var(--primary-color, var(--color-primary));
        text-decoration: none;
      }
      .section--promotion {
        background: color-mix(in srgb, var(--primary-color, var(--color-primary)) 4%, transparent);
      }
      .promotion-status {
        font-size: 13px;
        margin: 0 0 10px;
      }
      .validation-list {
        margin: 0 0 12px;
        padding-left: 18px;
        font-size: 12px;
        color: var(--text-secondary);
      }
      .promotion-history h4 {
        margin: 12px 0 8px;
        font-size: 11px;
        text-transform: uppercase;
        color: var(--text-secondary);
      }
      .promotion-actions {
        margin-top: 12px;
      }
      .btn--promote {
        margin-top: 8px;
        padding: 10px 16px;
        border-radius: 6px;
        border: none;
        background: var(--primary-color, var(--color-primary));
        color: #fff;
        font-size: 13px;
        font-weight: 600;
        cursor: pointer;
        width: 100%;
      }
      .btn--promote:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .routing-box {
        margin: 12px 0;
        padding: 12px;
        border-radius: 8px;
        background: #f0f9ff;
        border: 1px solid #bae6fd;
      }
      .routing-box h4 {
        margin: 0 0 8px;
        font-size: 13px;
      }
      .routing-reason {
        margin: 0 0 8px;
        font-size: 12px;
        color: var(--text-secondary, #6b7280);
      }
      .routing-meta {
        display: grid;
        gap: 6px;
        margin: 0;
        font-size: 12px;
      }
      .routing-meta dt {
        font-weight: 600;
        display: inline;
      }
      .routing-meta dd {
        display: inline;
        margin: 0 0 0 4px;
      }
    `,
  ],
})
export class FindingsDetailPanelComponent {
  readonly detail = input<FindingsWorkspaceDetail | null>(null);
  readonly promotionStatus = input<FindingPromotionStatus | null>(null);
  readonly routingSuggestion = input<AssignmentRoutingSuggestion | null>(null);
  readonly promotionLoading = input(false);
  readonly promotionSaving = input(false);
  readonly statusSaving = input(false);
  readonly mappingSaving = input(false);
  readonly close = output<void>();
  readonly statusChange = output<string>();
  /** DEF-M3-001 — emits the chosen objective element id; the container performs the write. */
  readonly complianceContextMapped = output<string>();
  readonly promote = output<void>();
  readonly commentAdded = output<string>();
  readonly attachmentAdded = output<{
    fileName: string;
    fileUrl: string;
    attachmentType: string;
  }>();

  private readonly configuration = inject(PlatformConfigurationService);
  private readonly workspace = inject(FindingsWorkspaceService);

  /** DEF-M3-001 — objective element lookup, the same search the capture panel uses. */
  private readonly oeSearch$ = new Subject<string>();
  readonly oeOptions = signal<FindingComplianceContextOption[]>([]);
  oeSearch = '';
  selectedOeId = '';

  constructor() {
    this.oeSearch$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) =>
          term.trim().length >= 2
            ? this.workspace.searchComplianceContext(term.trim())
            : of([] as FindingComplianceContextOption[]),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((options) => this.oeOptions.set(options));
  }

  /** A settled record stays settled: closed findings and promoted findings keep their mapping. */
  canMap(): boolean {
    const d = this.detail();
    return !!d && d.status !== 'Closed' && !d.promotedDeficiencyId;
  }

  onOeSearch(value: string): void {
    this.oeSearch$.next(value);
  }

  submitMapping(): void {
    if (!this.selectedOeId) return;
    this.complianceContextMapped.emit(this.selectedOeId);
  }

  /** Statuses a reviewer may move a finding to. */
  readonly statusOptions = this.configuration.optionsWith(
    CONFIGURATION_CATEGORY.findingStatus,
    'selectableWhenTransitioning',
  );
  readonly attachmentTypes = this.configuration.options(
    CONFIGURATION_CATEGORY.findingAttachmentType,
  );
  commentBody = '';
  attachName = '';
  attachUrl = '';
  attachType = 'Document';

  formatType(v: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.findingClassification, v, v);
  }

  formatSource(v: string): string {
    return this.configuration.labelByOrdinalOrCode(CONFIGURATION_CATEGORY.findingSource, v);
  }

  formatStatus(v: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.findingStatus, v, v);
  }

  journeyParams(d: FindingsWorkspaceDetail): Record<string, string> {
    const q: Record<string, string> = {};
    if (d.objectiveElementId) q['objectiveElementId'] = d.objectiveElementId;
    if (d.objectiveElementCode) q['search'] = d.objectiveElementCode;
    return q;
  }

  promotionHistory(d: FindingsWorkspaceDetail) {
    const keys = new Set(['FindingPromoted', 'DeficiencyCreated', 'PrimaryRemediationTaskCreated']);
    return d.history.filter((h) => keys.has(h.action));
  }

  submitComment(): void {
    const body = this.commentBody.trim();
    if (!body) return;
    this.commentAdded.emit(body);
    this.commentBody = '';
  }

  submitAttachment(): void {
    const fileName = this.attachName.trim();
    const fileUrl = this.attachUrl.trim();
    if (!fileName || !fileUrl) return;
    this.attachmentAdded.emit({ fileName, fileUrl, attachmentType: this.attachType });
    this.attachName = '';
    this.attachUrl = '';
  }
}
