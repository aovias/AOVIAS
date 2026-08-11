import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AuditorApiService } from '../services/auditor-api.service';
import {
  type AuditorSessionSummary,
  type AuditType,
  type CreateAuditSessionRequest,
} from '../models/auditor-workspace.model';
import { injectAuditWorkspace } from '../../audit/workspace/services/audit-workspace-participation.service';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-auditor-sessions-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/auditor-workspace.layout.scss',
  template: `
    <div class="auditor-workspace">
      <header class="page-header">
        <div>
          <h1>Audit Sessions</h1>
          <p class="subtitle">Manage independent assessment sessions</p>
        </div>
        <button type="button" class="btn-primary" (click)="showCreate.set(true)">
          New Session
        </button>
      </header>

      @if (showCreate()) {
        <section class="panel create-panel">
          <h2>Create Audit Session</h2>
          <div class="form-grid">
            <label>
              Audit Type
              <select [(ngModel)]="createForm.auditType">
                @for (t of auditTypes(); track t.value) {
                  <option [value]="t.value">{{ t.label }}</option>
                }
              </select>
            </label>
            <label>
              Scope
              <input [(ngModel)]="createForm.scope" placeholder="Departments / chapters in scope" />
            </label>
            <label>
              Lead Auditor
              <input [(ngModel)]="createForm.leadAuditorName" />
            </label>
            <label class="full">
              Summary
              <textarea [(ngModel)]="createForm.summary" rows="2"></textarea>
            </label>
          </div>
          <div class="actions">
            <button type="button" class="btn-secondary" (click)="showCreate.set(false)">
              Cancel
            </button>
            <button
              type="button"
              class="btn-primary"
              (click)="createSession()"
              [disabled]="creating()"
            >
              Create
            </button>
          </div>
        </section>
      }

      @if (loading()) {
        <p class="muted">Loading sessions…</p>
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Session</th>
              <th>Type</th>
              <th>Status</th>
              <th>Lead Auditor</th>
              <th>Findings</th>
              <th>Observations</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (s of sessions(); track s.id) {
              <tr>
                <td>
                  <strong>{{ s.sessionNumber }}</strong>
                </td>
                <td>{{ auditTypeLabel(s.auditType) }}</td>
                <td>{{ statusLabel(s.status) }}</td>
                <td>{{ s.leadAuditorName || '—' }}</td>
                <td>{{ s.findingsCount }}</td>
                <td>{{ s.observationsCount }}</td>
                <td><a [routerLink]="['/app/audit/execution/sessions', s.id]">Workspace</a></td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
  styles: [
    `
      .page-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 16px;
      }
      .page-header h1 {
        margin: 0;
        font-size: var(--font-size-page-title);
      }
      .subtitle {
        margin: 4px 0 0;
        color: var(--text-secondary);
      }
      .panel {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 16px;
        margin-bottom: 16px;
      }
      .form-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        gap: 12px;
      }
      .form-grid label {
        display: flex;
        flex-direction: column;
        gap: 4px;
        font-size: 12px;
      }
      .form-grid .full {
        grid-column: 1 / -1;
      }
      .actions {
        display: flex;
        gap: 8px;
        margin-top: 12px;
      }
      .btn-primary,
      .btn-secondary {
        padding: 8px 14px;
        border-radius: 6px;
        border: none;
        cursor: pointer;
        font-size: 13px;
      }
      .btn-primary {
        background: var(--color-primary);
        color: #fff;
      }
      .btn-secondary {
        border: 1px solid var(--border-color, #e5e7eb);
        background: #fff;
      }
      .data-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px;
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        overflow: hidden;
      }
      .data-table th,
      .data-table td {
        text-align: left;
        padding: 10px 12px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }
      .muted {
        color: var(--text-secondary);
      }
    `,
  ],
})
export class AuditorSessionsPageComponent implements OnInit {
  private readonly api = inject(AuditorApiService);
  private readonly configuration = inject(PlatformConfigurationService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly showCreate = signal(false);
  readonly sessions = signal<AuditorSessionSummary[]>([]);

  /**
   * Audit types an auditor may start a session for — a different rule from the types a planner may
   * schedule, and now expressed as one rather than as a second hardcoded list.
   */
  readonly auditTypes = this.configuration.optionsWith(
    CONFIGURATION_CATEGORY.auditType,
    'selectableWhenStartingSession',
  );

  createForm: CreateAuditSessionRequest = {
    auditType: 'InternalAudit',
    scope: '',
    leadAuditorName: '',
    summary: '',
  };

  ngOnInit(): void {
    this.publishParticipation();
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api
      .getSessions()
      .pipe(
        catchError(() => of([])),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((list) => this.sessions.set(list));
  }

  createSession(): void {
    if (!this.createForm.scope.trim()) return;
    this.creating.set(true);
    this.api
      .createSession(this.createForm)
      .pipe(finalize(() => this.creating.set(false)))
      .subscribe(() => {
        this.showCreate.set(false);
        this.createForm = {
          auditType: 'InternalAudit',
          scope: '',
          leadAuditorName: '',
          summary: '',
        };
        this.reload();
      });
  }

  /**
   * The API returns these enums camelCased (`internalAudit`, `inProgress`) while the label maps are
   * keyed PascalCase, so a direct index returned `undefined` and rendered a blank cell.
   * {@link resolveEnumLabel} matches regardless of casing.
   */
  auditTypeLabel(type: AuditType): string {
    return this.configuration.auditTypeLabel(type);
  }

  statusLabel(status: AuditorSessionSummary['status']): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.auditSessionStatus, status);
  }

  /** Sessions list — the execution stage's entry point. */
  private publishParticipation(): void {
    this.workspace.set({
      stage: 'execution',
      subject: { kind: 'audit', title: 'Audit sessions' },
      actions: [
        {
          id: 'sessions.create',
          label: 'New session',
          variant: 'primary',
          run: () => this.showCreate.set(true),
        },
      ],
      workflowState: {
        stage: 'execution',
        statusLabel: `${this.sessions().length} sessions`,
        completionCriteria: 'Every session closed with findings raised',
      },
      relatedRecords: this.sessions()
        .slice(0, 8)
        .map((s) => ({
          id: s.id,
          label: s.scope || s.sessionNumber,
          kind: 'Audit session',
          status: String(s.status),
          route: ['/app/audit/execution/sessions', s.id],
        })),
      aiContext: { facts: { sessionCount: String(this.sessions().length) } },
    });
  }
}
