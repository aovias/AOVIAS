import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AuditorApiService } from '../services/auditor-api.service';
import { type AuditorSessionSummary } from '../models/auditor-workspace.model';
import { injectAuditWorkspace } from '../../audit/workspace/services/audit-workspace-participation.service';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-auditor-reports-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/auditor-workspace.layout.scss',
  template: `
    <div class="auditor-workspace">
      <header class="page-header">
        <div>
          <h1>Audit Reports</h1>
          <p class="subtitle">Generate exportable audit summaries from session data</p>
        </div>
      </header>

      @if (loading()) {
        <p class="muted">Loading sessions…</p>
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Session</th>
              <th>Status</th>
              <th>Findings</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (s of sessions(); track s.id) {
              <tr>
                <td>
                  <strong>{{ s.sessionNumber }}</strong>
                </td>
                <td>{{ statusLabel(s.status) }}</td>
                <td>{{ s.findingsCount }}</td>
                <td>
                  <button type="button" class="link-btn" (click)="previewReport(s.id)">
                    Preview Report
                  </button>
                  ·
                  <a [routerLink]="['/app/audit/execution/sessions', s.id]">Workspace</a>
                </td>
              </tr>
            }
          </tbody>
        </table>

        @if (reportPreview()) {
          <section class="panel report-preview">
            <div class="panel-header">
              <h2>Report Preview</h2>
              <button type="button" class="link-btn" (click)="downloadReport()">
                Download Markdown
              </button>
            </div>
            <pre>{{ reportPreview() }}</pre>
          </section>
        }
      }
    </div>
  `,
  styles: [
    `
      .page-header h1 {
        margin: 0;
        font-size: var(--font-size-page-title);
      }
      .subtitle {
        color: var(--text-secondary);
      }
      .data-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px;
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        overflow: hidden;
        margin-bottom: 16px;
      }
      .data-table th,
      .data-table td {
        text-align: left;
        padding: 10px 12px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }
      .panel {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 16px;
      }
      .panel-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 8px;
      }
      .report-preview pre {
        white-space: pre-wrap;
        font-size: 12px;
        line-height: 1.5;
        max-height: 60vh;
        overflow: auto;
      }
      .link-btn {
        background: none;
        border: none;
        color: var(--color-primary);
        cursor: pointer;
        padding: 0;
        font-size: inherit;
      }
      .muted {
        color: var(--text-secondary);
      }
    `,
  ],
})
export class AuditorReportsPageComponent implements OnInit {
  private readonly api = inject(AuditorApiService);
  private readonly configuration = inject(PlatformConfigurationService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly loading = signal(true);
  readonly sessions = signal<AuditorSessionSummary[]>([]);
  readonly reportPreview = signal<string | null>(null);
  private activeSessionId: string | null = null;

  ngOnInit(): void {
    this.publishParticipation();
    this.api
      .getSessions()
      .pipe(
        catchError(() => of([])),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((list) => this.sessions.set(list));
  }

  previewReport(sessionId: string): void {
    this.activeSessionId = sessionId;
    this.api.getReport(sessionId).subscribe((r) => this.reportPreview.set(r.markdownContent));
  }

  downloadReport(): void {
    const content = this.reportPreview();
    if (!content) return;
    const blob = new Blob([content], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `audit-report-${this.activeSessionId ?? 'session'}.md`;
    a.click();
    URL.revokeObjectURL(url);
  }

  statusLabel(status: AuditorSessionSummary['status']): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.auditSessionStatus, status, status);
  }

  /** Session reports belong to the reporting stage. */
  private publishParticipation(): void {
    this.workspace.set({
      stage: 'reports',
      subject: { kind: 'none', title: 'Session reports' },
      workflowState: {
        stage: 'reports',
        statusLabel: `${this.sessions().length} sessions`,
        completionCriteria: 'Report issued for every closed session',
      },
      relatedRecords: this.sessions()
        .slice(0, 8)
        .map((s) => ({
          id: s.id,
          label: s.scope || s.sessionNumber,
          kind: 'Audit session',
          status: String(s.status),
        })),
    });
  }
}
