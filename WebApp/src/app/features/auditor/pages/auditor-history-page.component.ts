import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AuditorApiService } from '../services/auditor-api.service';
import { type AuditorSessionSummary } from '../models/auditor-workspace.model';
import { injectAuditWorkspace } from '../../audit/workspace/services/audit-workspace-participation.service';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

const HISTORY_STATUSES = new Set(['Submitted', 'Closed', 'DraftReport']);

@Component({
  selector: 'app-auditor-history-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/auditor-workspace.layout.scss',
  template: `
    <div class="auditor-workspace">
      <header class="page-header">
        <div>
          <h1>Audit History</h1>
          <p class="subtitle">Closed and submitted assessment sessions</p>
        </div>
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="muted">Loading history…</p>
      } @else if (!sessions().length) {
        <p class="muted">No closed or submitted sessions yet.</p>
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Session</th>
              <th>Type</th>
              <th>Status</th>
              <th>Result</th>
              <th>Findings</th>
              <th>Observations</th>
              <th>Created</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (s of sessions(); track s.id) {
              <tr>
                <td>
                  <strong>{{ s.sessionNumber }}</strong>
                  <br /><span class="muted">{{ s.scope }}</span>
                </td>
                <td>{{ auditTypeLabel(s.auditType) }}</td>
                <td>{{ statusLabel(s.status) }}</td>
                <td>{{ s.overallResult || '—' }}</td>
                <td>{{ s.findingsCount }}</td>
                <td>{{ s.observationsCount }}</td>
                <td>{{ s.createdAt | date: 'mediumDate' }}</td>
                <td><a [routerLink]="['/app/audit/execution/sessions', s.id]">Open</a></td>
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
        gap: 12px;
      }
      .page-header h1 {
        margin: 0;
        font-size: var(--font-size-page-title);
      }
      .subtitle {
        margin: 4px 0 0;
        color: var(--text-secondary);
        font-size: var(--font-size-subtitle);
      }
      .btn-secondary {
        padding: 8px 14px;
        border-radius: 6px;
        text-decoration: none;
        font-size: 13px;
        border: 1px solid var(--border-color, #e5e7eb);
        color: inherit;
      }
      .data-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px;
      }
      .data-table th,
      .data-table td {
        text-align: left;
        padding: 10px 8px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }
      .banner.error {
        background: #fef2f2;
        color: #b91c1c;
        padding: 10px 12px;
        border-radius: 6px;
        margin-bottom: 12px;
      }
      .muted {
        color: var(--text-secondary);
        font-size: 12px;
      }
    `,
  ],
})
export class AuditorHistoryPageComponent implements OnInit {
  private readonly api = inject(AuditorApiService);
  private readonly configuration = inject(PlatformConfigurationService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly sessions = signal<AuditorSessionSummary[]>([]);

  ngOnInit(): void {
    this.publishParticipation();
    this.api
      .getSessions()
      .pipe(
        catchError(() => {
          this.error.set('Unable to load audit history.');
          return of([] as AuditorSessionSummary[]);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((all) => {
        const history = (all ?? [])
          .filter((s) => HISTORY_STATUSES.has(s.status))
          .sort((a, b) => (b.createdAt || '').localeCompare(a.createdAt || ''));
        this.sessions.set(history);
      });
  }

  auditTypeLabel(type: string): string {
    return this.configuration.auditTypeLabel(type, type);
  }

  statusLabel(status: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.auditSessionStatus, status, status);
  }

  /** History is the reporting stage's retrospective view. */
  private publishParticipation(): void {
    this.workspace.set({
      stage: 'reports',
      subject: { kind: 'none', title: 'Audit history' },
      relatedRecords: this.sessions()
        .slice(0, 8)
        .map((s) => ({
          id: s.id,
          label: s.scope || s.sessionNumber,
          kind: 'Past session',
          status: String(s.status),
        })),
      aiContext: { facts: { historicSessions: String(this.sessions().length) } },
    });
  }
}
