import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
  computed,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import {
  AuditManagementApiService,
  type ScheduledAuditDto,
} from '../services/audit-management-api.service';
import { injectAuditWorkspace } from '../workspace/services/audit-workspace-participation.service';
import { AuditorApiService } from '../../auditor/services/auditor-api.service';
import type { AuditorSessionSummary } from '../../auditor/models/auditor-workspace.model';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

/**
 * Surveillance MVP — schedule + sessions filtered to Surveillance audit type.
 */
@Component({
  selector: 'app-audit-surveillance',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <h1>Surveillance</h1>
          <p class="subtitle">
            Surveillance-typed scheduled audits and sessions — continuous compliance monitoring
            cycle.
          </p>
        </div>
      </header>

      @if (loading()) {
        <p class="muted">Loading surveillance…</p>
      }
      @if (error()) {
        <p class="error">
          {{ error() }} <button type="button" class="btn-secondary" (click)="load()">Retry</button>
        </p>
      }

      <div class="kpis">
        <div class="kpi">
          <div class="kpi-value">{{ scheduled().length }}</div>
          <div class="kpi-label">Scheduled</div>
        </div>
        <div class="kpi">
          <div class="kpi-value">{{ openSessions().length }}</div>
          <div class="kpi-label">Open sessions</div>
        </div>
        <div class="kpi">
          <div class="kpi-value">{{ closedSessions().length }}</div>
          <div class="kpi-label">Closed</div>
        </div>
        <div class="kpi">
          <div class="kpi-value">{{ overdue().length }}</div>
          <div class="kpi-label">Overdue schedule</div>
        </div>
      </div>

      <section class="panel">
        <h2>Scheduled surveillance audits</h2>
        <table>
          <thead>
            <tr>
              <th>Date</th>
              <th>Title</th>
              <th>Program</th>
              <th>Status</th>
              <th>Auditor</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (a of scheduled(); track a.id) {
              <tr>
                <td>{{ a.scheduledDate | date: 'mediumDate' }}</td>
                <td>{{ a.title }}</td>
                <td>{{ a.programName || a.programYear + '/' + a.programMonth }}</td>
                <td>{{ a.status }}</td>
                <td>{{ a.auditorName || '—' }}</td>
                <td>
                  <a [routerLink]="['/app/audit/planning']" [queryParams]="{ focus: a.id }"
                    >Planning</a
                  >
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6" class="muted">
                  No surveillance schedules yet. Create one from Planning with type Surveillance.
                </td>
              </tr>
            }
          </tbody>
        </table>
      </section>

      <section class="panel">
        <h2>Surveillance sessions</h2>
        <table>
          <thead>
            <tr>
              <th>Session</th>
              <th>Status</th>
              <th>Findings</th>
              <th>Lead</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (s of sessions(); track s.id) {
              <tr>
                <td>{{ s.sessionNumber }}</td>
                <td>{{ statusLabel(s.status) }}</td>
                <td>{{ s.findingsCount }}</td>
                <td>{{ s.leadAuditorName || '—' }}</td>
                <td><a [routerLink]="['/app/audit/execution/sessions', s.id]">Open</a></td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5" class="muted">No surveillance sessions yet.</td>
              </tr>
            }
          </tbody>
        </table>
      </section>
    </div>
  `,
  styles: [
    `
      .page {
        padding: var(--page-padding-block-start) var(--page-padding-inline)
          var(--page-padding-block-end);
      }
      .page-header {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
        margin-bottom: 20px;
      }
      h1 {
        margin: 0 0 6px;
        font-size: var(--font-size-page-title);
      }
      .subtitle,
      .muted {
        color: var(--text-secondary);
        font-size: 14px;
        margin: 0;
      }
      .header-actions {
        display: flex;
        gap: 8px;
      }
      .btn-primary,
      .btn-secondary {
        padding: 8px 14px;
        border-radius: 6px;
        font-size: 13px;
        text-decoration: none;
        border: none;
        cursor: pointer;
      }
      .btn-primary {
        background: var(--color-primary);
        color: #fff;
      }
      .btn-secondary {
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--card-bg, #fff);
        color: var(--text-primary);
      }
      .kpis {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 12px;
        margin-bottom: 20px;
      }
      .kpi {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 14px;
      }
      .kpi-value {
        font-size: 24px;
        font-weight: 600;
      }
      .kpi-label {
        font-size: 12px;
        color: var(--text-secondary);
        margin-top: 4px;
      }
      .panel {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 16px;
        margin-bottom: 16px;
      }
      h2 {
        margin: 0 0 12px;
        font-size: 16px;
      }
      table {
        width: 100%;
        border-collapse: collapse;
        font-size: 14px;
      }
      th,
      td {
        text-align: left;
        padding: 10px 8px;
        border-bottom: 1px solid var(--border-color, #f0f0f0);
      }
      a {
        color: var(--color-primary);
        text-decoration: none;
      }
      .error {
        color: #b91c1c;
      }
    `,
  ],
})
export class AuditSurveillanceComponent implements OnInit {
  private readonly scheduleApi = inject(AuditManagementApiService);
  private readonly auditorApi = inject(AuditorApiService);
  private readonly configuration = inject(PlatformConfigurationService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly scheduled = signal<ScheduledAuditDto[]>([]);
  readonly sessions = signal<AuditorSessionSummary[]>([]);

  /**
   * Which states mean the work is finished — configuration, not a private tuple.
   *
   * Case is resolved by the configuration service, which matters because the API emits camelCase
   * (`closed`) while the catalog is keyed PascalCase; the PascalCase equality this replaces never
   * matched, so the Closed counter sat at zero regardless of the data.
   */
  private isTerminalSessionStatus(status: string | null | undefined): boolean {
    return this.configuration.attribute(
      CONFIGURATION_CATEGORY.auditSessionStatus,
      status,
      'terminal',
      false,
    );
  }

  private isTerminalScheduleStatus(status: string | null | undefined): boolean {
    return this.configuration.attribute(
      CONFIGURATION_CATEGORY.scheduledAuditStatus,
      status,
      'terminal',
      false,
    );
  }

  readonly openSessions = computed(() =>
    this.sessions().filter((s) => !this.isTerminalSessionStatus(s.status)),
  );
  readonly closedSessions = computed(() =>
    this.sessions().filter((s) => this.isTerminalSessionStatus(s.status)),
  );
  readonly overdue = computed(() => {
    const now = Date.now();
    return this.scheduled().filter((a) => {
      const t = new Date(a.scheduledDate).getTime();
      return t < now && !this.isTerminalScheduleStatus(a.status);
    });
  });

  ngOnInit(): void {
    this.publishParticipation();
    this.load();
  }

  /**
   * Surveillance watches the execution stage across the programme rather than
   * driving one audit, so it contributes context and related sessions but no
   * operations of its own.
   */
  private publishParticipation(): void {
    this.workspace.set({
      stage: 'execution',
      subject: { kind: 'audit', title: 'Surveillance' },
      workflowState: {
        stage: 'execution',
        statusLabel: `${this.sessions().length} active sessions`,
        completionCriteria: 'Every scheduled surveillance audit has a session',
      },
      relatedRecords: this.sessions()
        .slice(0, 8)
        .map((sn) => ({
          id: sn.id,
          label: sn.scope || sn.sessionNumber,
          kind: 'Audit session',
          status: String(sn.status),
          route: ['/app/audit/execution/sessions', sn.id],
        })),
      aiContext: {
        facts: {
          scheduledAudits: String(this.scheduled().length),
          activeSessions: String(this.sessions().length),
        },
      },
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const year = new Date().getFullYear();
    forkJoin({
      schedule: this.scheduleApi
        .getCalendar({ year })
        .pipe(catchError(() => of([] as ScheduledAuditDto[]))),
      sessions: this.auditorApi
        .getSessions()
        .pipe(catchError(() => of([] as AuditorSessionSummary[]))),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ schedule, sessions }) => {
          // Membership of the surveillance cycle is an attribute of the audit type, not a substring
          // of its name. The previous test matched any type whose *label* happened to contain
          // "surveillance", so renaming a type could silently add or drop it from this screen.
          const isSurveillance = (type: string | null | undefined) =>
            this.configuration.isSurveillanceAuditType(type);

          this.scheduled.set(schedule.filter((a) => isSurveillance(a.auditType)));
          this.sessions.set(sessions.filter((s) => isSurveillance(String(s.auditType))));
        },
        error: () => this.error.set('Unable to load surveillance data.'),
      });
  }

  statusLabel(status: AuditorSessionSummary['status']): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.auditSessionStatus, status, status);
  }
}
