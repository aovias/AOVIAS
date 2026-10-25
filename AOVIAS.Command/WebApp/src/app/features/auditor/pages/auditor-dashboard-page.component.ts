import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AuditorApiService } from '../services/auditor-api.service';
import {
  RoleExperienceQuickAction,
  RoleExperienceService,
} from '../../../core/services/role-experience.service';
import { type AuditorDashboard } from '../models/auditor-workspace.model';
import { injectAuditWorkspace } from '../../audit/workspace/services/audit-workspace-participation.service';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-auditor-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/auditor-workspace.layout.scss',
  template: `
    <div class="auditor-workspace">
      <header class="page-header">
        <div>
          <h1>External Auditor Workspace</h1>
          <p class="subtitle">Independent assessment — read-only operational truth</p>
        </div>
        <div class="header-actions">
          @for (a of quickActions(); track a.label) {
            <a
              [routerLink]="routePath(a.href)"
              [queryParams]="routeQuery(a.href)"
              [class.btn-primary]="$first"
              [class.btn-secondary]="!$first"
            >
              {{ a.label }}
            </a>
          }
        </div>
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="muted">Loading auditor dashboard…</p>
      } @else if (dashboard(); as d) {
        <div class="kpi-grid">
          <div class="kpi">
            <span class="kpi__v">{{ d.activeSessions }}</span
            ><span class="kpi__l">Active Sessions</span>
          </div>
          <div class="kpi">
            <span class="kpi__v">{{ d.areasReviewed }}</span
            ><span class="kpi__l">Areas Reviewed</span>
          </div>
          <div class="kpi">
            <span class="kpi__v">{{ d.pendingReviews }}</span
            ><span class="kpi__l">Pending Reviews</span>
          </div>
          <div class="kpi">
            <span class="kpi__v">{{ d.findingsRaised }}</span
            ><span class="kpi__l">Findings Raised</span>
          </div>
          <div class="kpi">
            <span class="kpi__v">{{ d.observations }}</span
            ><span class="kpi__l">Observations</span>
          </div>
          <div class="kpi kpi--critical">
            <span class="kpi__v">{{ d.criticalRisks }}</span
            ><span class="kpi__l">Critical Risks</span>
          </div>
          <div class="kpi">
            <span class="kpi__v">{{ d.coveragePercent | number: '1.0-1' }}%</span
            ><span class="kpi__l">Coverage</span>
          </div>
          <div class="kpi">
            <span class="kpi__v">{{ d.evidenceReviewed }}</span
            ><span class="kpi__l">Evidence Reviewed</span>
          </div>
          <div class="kpi kpi--ready">
            <span class="kpi__v">{{ d.readinessSnapshot | number: '1.0-1' }}%</span
            ><span class="kpi__l">Readiness Snapshot</span>
          </div>
        </div>

        <section class="panel">
          <h2>Recent Audit Sessions</h2>
          @if (!d.recentSessions.length) {
            <p class="muted">No audit sessions yet.</p>
          } @else {
            <table class="data-table">
              <thead>
                <tr>
                  <th>Session</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>Findings</th>
                  <th>Observations</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                @for (s of d.recentSessions; track s.id) {
                  <tr>
                    <td>
                      <strong>{{ s.sessionNumber }}</strong
                      ><br /><span class="muted">{{ s.scope }}</span>
                    </td>
                    <td>{{ auditTypeLabel(s.auditType) }}</td>
                    <td>
                      <span class="badge">{{ statusLabel(s.status) }}</span>
                    </td>
                    <td>{{ s.findingsCount }}</td>
                    <td>{{ s.observationsCount }}</td>
                    <td><a [routerLink]="['/app/audit/execution/sessions', s.id]">Open</a></td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </section>
      }
    </div>
  `,
  styles: [
    `
      .page-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 20px;
        gap: 16px;
        flex-wrap: wrap;
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
      .header-actions {
        display: flex;
        gap: 8px;
      }
      .btn-primary,
      .btn-secondary {
        padding: 8px 14px;
        border-radius: 6px;
        text-decoration: none;
        font-size: 13px;
      }
      .btn-primary {
        background: var(--color-primary);
        color: #fff;
      }
      .btn-secondary {
        border: 1px solid var(--border-color, #e5e7eb);
        color: inherit;
      }
      .kpi-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
        gap: 10px;
        margin-bottom: 20px;
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
      .panel {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 16px;
      }
      .panel h2 {
        margin: 0 0 12px;
        font-size: var(--font-size-section-title);
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
      .badge {
        background: #f3f4f6;
        padding: 2px 8px;
        border-radius: 999px;
        font-size: 11px;
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
        font-size: 13px;
      }
    `,
  ],
})
export class AuditorDashboardPageComponent implements OnInit {
  private readonly api = inject(AuditorApiService);
  private readonly configuration = inject(PlatformConfigurationService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();
  private readonly roleExperience = inject(RoleExperienceService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly dashboard = signal<AuditorDashboard | null>(null);
  /**
   * Starts empty by design (Phase 2, Steps 9 + 12).
   *
   * The previous defaults — Sessions, Evidence review, Observations — were
   * header buttons that only navigated, duplicating the workspace tabs that now
   * own that navigation. Server-supplied quick actions are still honoured and
   * rendered from `roleExperience.getHome()` below.
   */
  readonly quickActions = signal<RoleExperienceQuickAction[]>([]);

  ngOnInit(): void {
    this.publishParticipation();
    this.roleExperience.getHome().subscribe((h) => {
      if (h.quickActions?.length) {
        this.quickActions.set(h.quickActions);
      }
    });

    this.api
      .getDashboard()
      .pipe(
        catchError(() => {
          this.error.set('Unable to load auditor dashboard.');
          return of(null);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((d) => {
        if (d) this.dashboard.set(d);
      });
  }

  routePath(href: string): string {
    return href.split('?')[0] || href;
  }

  routeQuery(href: string): Record<string, string> | undefined {
    const q = href.includes('?') ? href.split('?')[1] : '';
    if (!q) return undefined;
    const out: Record<string, string> = {};
    for (const part of q.split('&')) {
      const [k, v] = part.split('=');
      if (k) out[decodeURIComponent(k)] = decodeURIComponent(v ?? '');
    }
    return out;
  }

  auditTypeLabel(type: string): string {
    return this.configuration.auditTypeLabel(type, type);
  }

  statusLabel(status: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.auditSessionStatus, status, status);
  }

  /** Auditor home sits above the lifecycle, so it declares no stage. */
  private publishParticipation(): void {
    const d = this.dashboard();
    this.workspace.set({
      stage: null,
      subject: { kind: 'none', title: 'Auditor home' },
      aiContext: {
        facts: {
          activeSessions: String(d?.activeSessions ?? 0),
          findingsRaised: String(d?.findingsRaised ?? 0),
          pendingReviews: String(d?.pendingReviews ?? 0),
        },
      },
    });
  }
}
