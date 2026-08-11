import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of, switchMap } from 'rxjs';
import { AuditorApiService } from '../services/auditor-api.service';
import {
  type AuditObservationItem,
  type AuditorSessionSummary,
} from '../models/auditor-workspace.model';
import { injectAuditWorkspace } from '../../audit/workspace/services/audit-workspace-participation.service';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

interface ObservationRow extends AuditObservationItem {
  sessionId: string;
  sessionNumber: string;
}

@Component({
  selector: 'app-auditor-observations-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/auditor-workspace.layout.scss',
  template: `
    <div class="auditor-workspace">
      <header class="page-header">
        <div>
          <h1>Audit Observations</h1>
          <p class="subtitle">Independent observations across assessment sessions</p>
        </div>
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="muted">Loading observations…</p>
      } @else if (!rows().length) {
        <p class="muted">No observations recorded yet.</p>
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Observation</th>
              <th>Type</th>
              <th>Session</th>
              <th>Department</th>
              <th>Created</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (o of rows(); track o.id) {
              <tr>
                <td>
                  <strong>{{ o.observationNumber || o.title }}</strong>
                  <br /><span class="muted">{{ o.title }}</span>
                </td>
                <td>{{ typeLabel(o.type) }}</td>
                <td>{{ o.sessionNumber }}</td>
                <td>{{ o.department || '—' }}</td>
                <td>{{ o.createdAt | date: 'mediumDate' }}</td>
                <td>
                  <a [routerLink]="['/app/audit/execution/sessions', o.sessionId]">Open session</a>
                </td>
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
export class AuditorObservationsPageComponent implements OnInit {
  private readonly api = inject(AuditorApiService);
  private readonly configuration = inject(PlatformConfigurationService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly rows = signal<ObservationRow[]>([]);

  ngOnInit(): void {
    this.publishParticipation();
    this.api
      .getSessions()
      .pipe(
        switchMap((sessions) => {
          if (!sessions?.length) return of([] as ObservationRow[]);
          return forkJoin(
            sessions.map((s: AuditorSessionSummary) =>
              this.api.getSession(s.id).pipe(
                catchError(() => of(null)),
                switchMap((detail) =>
                  of(
                    (detail?.observations ?? []).map(
                      (o) =>
                        ({
                          ...o,
                          sessionId: s.id,
                          sessionNumber: s.sessionNumber,
                        }) satisfies ObservationRow,
                    ),
                  ),
                ),
              ),
            ),
          ).pipe(switchMap((chunks) => of(chunks.flat())));
        }),
        catchError(() => {
          this.error.set('Unable to load observations.');
          return of([] as ObservationRow[]);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((rows) => this.rows.set(rows));
  }

  typeLabel(type: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.observationType, type, type);
  }

  /** Observations are the raw material findings are raised from. */
  private publishParticipation(): void {
    this.workspace.set({
      stage: 'execution',
      subject: { kind: 'audit', title: 'Observations' },
      workflowState: {
        stage: 'execution',
        statusLabel: `${this.rows().length} observations`,
        completionCriteria: 'Every observation triaged or promoted to a finding',
      },
      aiContext: { facts: { observationCount: String(this.rows().length) } },
    });
  }
}
