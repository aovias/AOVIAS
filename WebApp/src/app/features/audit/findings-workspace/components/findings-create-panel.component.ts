import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, switchMap, of } from 'rxjs';
import { toFindingWireEnum } from '../../models/finding.models';
import {
  type CreateFindingAttachmentInput,
  type CreateStructuredFindingRequest,
  type FindingComplianceContextOption,
} from '../models/findings-workspace.model';
import { FindingsWorkspaceService } from '../services/findings-workspace.service';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';
import { AuditWorkspaceStateService } from '../../workspace/services/audit-workspace-state.service';

@Component({
  selector: 'app-findings-create-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="modal-backdrop" (click)="cancel.emit()">
      <div class="modal" (click)="$event.stopPropagation()">
        <header class="modal__head">
          <div>
            <h2>Capture Audit Finding</h2>
            <p class="subtitle">Structured discovery capture — no remediation workflows</p>
          </div>
          <button type="button" class="close" (click)="cancel.emit()" aria-label="Close">×</button>
        </header>

        <div class="modal__body">
          <section class="form-section">
            <h3>1. Finding Information</h3>
            <label>Title <input [(ngModel)]="title" required /></label>
            <label
              >Classification
              <select [(ngModel)]="classification">
                @for (c of classificationOptions(); track c.value) {
                  <option [value]="c.value">{{ c.label }}</option>
                }
              </select>
            </label>
            <label
              >Source
              <select [(ngModel)]="source">
                @for (s of sourceOptions(); track s.value) {
                  <option [value]="s.value">{{ s.label }}</option>
                }
              </select>
            </label>
            <div class="row">
              <label
                >Severity
                <select [(ngModel)]="severity">
                  @for (s of severityOptions(); track s.value) {
                    <option [value]="s.value">{{ s.label }}</option>
                  }
                </select>
              </label>
              <label
                >Department
                <select [(ngModel)]="department">
                  @for (d of departmentOptions(); track d.value) {
                    <option [value]="d.value">{{ d.label }}</option>
                  }
                </select>
              </label>
            </div>
          </section>

          <section class="form-section">
            <h3>2. Compliance Context</h3>
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
                >Mapped Objective Element
                <select [(ngModel)]="selectedOeId" (ngModelChange)="onOeSelected($event)">
                  <option value="">— Select —</option>
                  @for (oe of oeOptions(); track oe.objectiveElementId) {
                    <option [value]="oe.objectiveElementId">
                      {{ oe.chapterCode }} / {{ oe.standardCode }} / {{ oe.objectiveElementCode }} —
                      {{ oe.objectiveElementName }}
                    </option>
                  }
                </select>
              </label>
            }
            <dl class="context-preview">
              <div>
                <dt>Chapter</dt>
                <dd>{{ selectedContext()?.chapterCode || '—' }}</dd>
              </div>
              <div>
                <dt>Standard</dt>
                <dd>{{ selectedContext()?.standardCode || '—' }}</dd>
              </div>
              <div>
                <dt>Objective Element</dt>
                <dd>{{ selectedContext()?.objectiveElementCode || '—' }}</dd>
              </div>
            </dl>
          </section>

          <section class="form-section">
            <h3>3. Audit Context</h3>
            <div class="row">
              <label>Audit Date <input type="date" [(ngModel)]="auditDate" /></label>
              <label>Auditor <input [(ngModel)]="auditorName" placeholder="Auditor name" /></label>
            </div>
            <label
              >Audit Cycle
              <input [(ngModel)]="auditCycle" placeholder="e.g. Q2 2026 Internal Audit"
            /></label>
          </section>

          <section class="form-section">
            <h3>4. Observation Details</h3>
            <label>Observation <textarea rows="2" [(ngModel)]="observation"></textarea></label>
            <label>Requirement <textarea rows="2" [(ngModel)]="requirement"></textarea></label>
            <label>Impact <textarea rows="2" [(ngModel)]="impact"></textarea></label>
            <label
              >Recommendation <textarea rows="2" [(ngModel)]="recommendation"></textarea>
            </label>
          </section>

          <section class="form-section">
            <h3>5. Reference</h3>
            <label>Clause / Reference <input [(ngModel)]="clauseReference" required /></label>
            <label
              >Summary Description <textarea rows="2" [(ngModel)]="description"></textarea>
            </label>
          </section>

          <section class="form-section">
            <h3>6. Attachments</h3>
            @for (a of pendingAttachments(); track $index; let i = $index) {
              <div class="attach-row">
                <span>{{ a.fileName }} ({{ a.attachmentType }})</span>
                <button type="button" (click)="removeAttachment(i)">Remove</button>
              </div>
            }
            <div class="attach-form">
              <input placeholder="File name" [(ngModel)]="attachName" />
              <input placeholder="File URL" [(ngModel)]="attachUrl" />
              <select [(ngModel)]="attachType">
                @for (t of attachmentTypes(); track t.value) {
                  <option [value]="t.value">{{ t.label }}</option>
                }
              </select>
              <button type="button" (click)="addPendingAttachment()">Add Attachment</button>
            </div>
          </section>
        </div>

        <footer class="modal__foot">
          <label class="status-pick"
            >Initial Status
            <select [(ngModel)]="initialStatus">
              @for (s of initialStatusOptions(); track s.value) {
                <option [value]="s.value">{{ s.label }}</option>
              }
            </select>
          </label>
          <div class="actions">
            <button type="button" class="btn-secondary" (click)="cancel.emit()">Cancel</button>
            <button
              type="button"
              class="btn-primary"
              [disabled]="saving() || !canSubmit()"
              (click)="submit()"
            >
              {{ saving() ? 'Saving…' : 'Create Finding' }}
            </button>
          </div>
        </footer>
      </div>
    </div>
  `,
  styles: [
    `
      .modal-backdrop {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: flex-start;
        justify-content: center;
        z-index: 200;
        padding: 24px;
        overflow-y: auto;
      }
      .modal {
        background: var(--card-bg, #fff);
        border-radius: 12px;
        width: min(640px, 100%);
        max-height: calc(100vh - 48px);
        display: flex;
        flex-direction: column;
        box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15);
      }
      .modal__head {
        display: flex;
        justify-content: space-between;
        padding: 20px 24px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }
      h2 {
        margin: 0 0 4px;
        font-size: var(--font-size-section-title);
        color: var(--color-text-primary, var(--text-primary));
      }
      .subtitle {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--text-secondary);
      }
      .close {
        border: none;
        background: none;
        font-size: 24px;
        cursor: pointer;
        color: var(--color-text-secondary, var(--text-secondary));
      }
      .modal__body {
        overflow-y: auto;
        padding: 0 24px 16px;
      }
      .form-section {
        padding: 16px 0;
        border-bottom: 1px solid var(--border-color, #f3f4f6);
      }
      h3 {
        margin: 0 0 12px;
        font-size: 12px;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--text-secondary);
      }
      label {
        display: grid;
        gap: 4px;
        font-size: 13px;
        margin-bottom: 10px;
        color: var(--color-text-primary, var(--text-primary));
      }
      input,
      select,
      textarea {
        padding: 8px 12px;
        border-radius: 6px;
        border: 1px solid var(--color-border-subtle, var(--border-color, #e5e7eb));
        background: var(--color-bg-surface, var(--card-bg, #fff));
        color: var(--color-text-primary, var(--text-primary));
        font-size: 13px;
        font-family: inherit;
      }
      input::placeholder,
      textarea::placeholder {
        color: var(--color-text-muted, var(--text-secondary));
        opacity: 0.75;
      }
      input:focus,
      select:focus,
      textarea:focus {
        outline: none;
        border-color: color-mix(
          in srgb,
          var(--color-brand-primary, var(--primary-color, var(--color-primary))) 55%,
          var(--color-border-subtle, var(--border-color))
        );
        box-shadow: var(
          --shadow-focus-ring-primary,
          0 0 0 2px color-mix(in srgb, var(--primary-color, var(--color-primary)) 22%, transparent)
        );
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
      .row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;
      }
      .context-preview {
        display: grid;
        gap: 6px;
        font-size: 12px;
        margin: 8px 0 0;
      }
      .context-preview div {
        display: grid;
        grid-template-columns: 120px 1fr;
      }
      dt {
        color: var(--text-secondary);
      }
      dd {
        margin: 0;
      }
      .attach-row {
        display: flex;
        justify-content: space-between;
        font-size: 12px;
        padding: 6px 0;
        color: var(--color-text-primary, var(--text-primary));
      }
      .attach-row button {
        background: transparent;
        border: 1px solid var(--color-border-subtle, var(--border-color));
        color: var(--color-text-secondary, var(--text-secondary));
        border-radius: 4px;
        cursor: pointer;
        font-size: 11px;
        padding: 2px 8px;
      }
      .attach-form {
        display: grid;
        gap: 8px;
        margin-top: 8px;
      }
      .modal__foot {
        padding: 16px 24px;
        border-top: 1px solid var(--border-color, #e5e7eb);
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 12px;
      }
      .status-pick {
        margin: 0;
      }
      .actions {
        display: flex;
        gap: 8px;
      }
      .btn-primary,
      .btn-secondary {
        padding: 8px 16px;
        border-radius: 6px;
        font-size: 13px;
        cursor: pointer;
      }
      .btn-primary {
        background: var(--color-brand-primary, var(--primary-color, var(--color-primary)));
        color: var(--color-text-inverse, #fff);
        border: none;
      }
      .btn-primary:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .btn-secondary {
        background: var(--color-bg-surface, var(--card-bg, #fff));
        color: var(--color-text-primary, var(--text-primary));
        border: 1px solid var(--color-border-subtle, var(--border-color, #e5e7eb));
      }
      .attach-form button {
        background: var(--color-bg-surface-alt, var(--bg-tertiary, #f3f4f6));
        color: var(--color-text-primary, var(--text-primary));
        border: 1px solid var(--color-border-subtle, var(--border-color));
        border-radius: 6px;
        padding: 8px 12px;
        cursor: pointer;
        font-size: 13px;
      }
    `,
  ],
})
export class FindingsCreatePanelComponent {
  private readonly workspace = inject(FindingsWorkspaceService);
  private readonly auditWorkspace = inject(AuditWorkspaceStateService);
  private readonly oeSearch$ = new Subject<string>();

  readonly cancel = output<void>();
  readonly created = output<void>();

  private readonly configuration = inject(PlatformConfigurationService);

  readonly classificationOptions = this.configuration.options(
    CONFIGURATION_CATEGORY.findingClassification,
  );
  readonly sourceOptions = this.configuration.options(CONFIGURATION_CATEGORY.findingSource);
  readonly severityOptions = this.configuration.options(CONFIGURATION_CATEGORY.findingSeverity);
  readonly departmentOptions = this.configuration.options(CONFIGURATION_CATEGORY.department);
  readonly attachmentTypes = this.configuration.options(
    CONFIGURATION_CATEGORY.findingAttachmentType,
  );

  /** Statuses a finding may be created in — a business rule, not the whole lifecycle. */
  readonly initialStatusOptions = this.configuration.optionsWith(
    CONFIGURATION_CATEGORY.findingStatus,
    'selectableWhenCreating',
  );

  readonly saving = signal(false);
  readonly oeOptions = signal<FindingComplianceContextOption[]>([]);
  readonly pendingAttachments = signal<CreateFindingAttachmentInput[]>([]);

  title = '';
  description = '';
  clauseReference = '';
  classification = 'AuditFinding';
  source = 'InternalAudit';
  severity = 'Minor';
  department = 'clinical';
  initialStatus = 'Open';
  auditDate = new Date().toISOString().slice(0, 10);
  auditorName = '';
  auditCycle = '';
  observation = '';
  requirement = '';
  impact = '';
  recommendation = '';
  oeSearch = '';
  selectedOeId = '';
  attachName = '';
  attachUrl = '';
  attachType = 'Document';

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
      )
      .subscribe((options) => this.oeOptions.set(options));
  }

  onOeSearch(value: string): void {
    this.oeSearch$.next(value);
  }

  onOeSelected(id: string): void {
    this.selectedOeId = id;
  }

  selectedContext(): FindingComplianceContextOption | undefined {
    return this.oeOptions().find((o) => o.objectiveElementId === this.selectedOeId);
  }

  canSubmit(): boolean {
    return !!this.title.trim() && !!this.clauseReference.trim();
  }

  addPendingAttachment(): void {
    const fileName = this.attachName.trim();
    const fileUrl = this.attachUrl.trim();
    if (!fileName || !fileUrl) return;
    this.pendingAttachments.update((list) => [
      ...list,
      { fileName, fileUrl, attachmentType: this.attachType },
    ]);
    this.attachName = '';
    this.attachUrl = '';
  }

  removeAttachment(index: number): void {
    this.pendingAttachments.update((list) => list.filter((_, i) => i !== index));
  }

  submit(): void {
    if (!this.canSubmit() || this.saving()) return;
    this.saving.set(true);

    const payload: CreateStructuredFindingRequest = {
      title: this.title.trim(),
      description: this.description.trim() || this.observation.trim() || undefined,
      clauseReference: this.clauseReference.trim(),
      severity: toFindingWireEnum(this.severity),
      department: this.department,
      raisedBy: toFindingWireEnum(this.source),
      classification: toFindingWireEnum(this.classification),
      status: toFindingWireEnum(this.initialStatus),
      auditDate: this.auditDate ? new Date(this.auditDate).toISOString() : undefined,
      auditorName: this.auditorName.trim() || undefined,
      auditCycle: this.auditCycle.trim() || undefined,
      observation: this.observation.trim() || undefined,
      requirement: this.requirement.trim() || undefined,
      impact: this.impact.trim() || undefined,
      recommendation: this.recommendation.trim() || undefined,
      objectiveElementId: this.selectedOeId || undefined,
      // DEF-M6-001 — a finding raised while an audit is open belongs to that audit's session.
      // The server has always accepted and validated this id; nothing ever sent it, so every
      // session reported "no findings linked". Left undefined for ad-hoc findings raised outside
      // an audit, which the domain permits.
      auditSessionId: this.auditWorkspace.sessionId() ?? undefined,
      attachments: this.pendingAttachments().length ? this.pendingAttachments() : undefined,
    };

    this.workspace.createFinding(payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.created.emit();
      },
      error: () => this.saving.set(false),
    });
  }
}
