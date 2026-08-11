import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { AuditorApiService } from '../services/auditor-api.service';
import { injectAuditWorkspace } from '../../audit/workspace/services/audit-workspace-participation.service';
import { AuditWorkspaceStateService } from '../../audit/workspace/services/audit-workspace-state.service';
import { type AuditSessionDetail, type ObservationType } from '../models/auditor-workspace.model';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-auditor-session-workspace-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/auditor-workspace.layout.scss',
  template: `
    <div class="auditor-workspace">
      @if (loading()) {
        <p class="muted">Loading session…</p>
      } @else if (session(); as s) {
        <header class="page-header">
          <div>
            <a routerLink="/app/audit/execution/sessions" class="back">← Sessions</a>
            <h1>{{ s.sessionNumber }}</h1>
            <p class="subtitle">{{ s.scope }} · {{ statusLabel(s.status) }}</p>
          </div>
          <div class="header-actions">
            <button type="button" class="btn-secondary" (click)="loadReport()">
              Generate Report
            </button>
            <a [routerLink]="['/app/audit/assessment']" class="btn-primary">Assessment Workspace</a>
            <a [routerLink]="['/app/evidence']" class="btn-secondary">Evidence library</a>
            @if (s.status !== 'Closed' && s.status !== 'Submitted') {
              <button
                type="button"
                class="btn-danger"
                (click)="closeSession()"
                [disabled]="closing()"
              >
                {{ closing() ? 'Closing…' : 'Close session' }}
              </button>
            }
          </div>
        </header>
        @if (closeError()) {
          <div class="banner error">{{ closeError() }}</div>
        }
        @if (closeInfo()) {
          <div class="banner ok">{{ closeInfo() }}</div>
        }

        <div class="tabs">
          <button type="button" [class.active]="tab() === 'overview'" (click)="tab.set('overview')">
            Overview
          </button>
          <button
            type="button"
            [class.active]="tab() === 'observations'"
            (click)="tab.set('observations')"
          >
            Observations
          </button>
          <button type="button" [class.active]="tab() === 'findings'" (click)="tab.set('findings')">
            Findings
          </button>
          <button type="button" [class.active]="tab() === 'comments'" (click)="tab.set('comments')">
            Comments
          </button>
          <button type="button" [class.active]="tab() === 'report'" (click)="tab.set('report')">
            Report
          </button>
        </div>

        @if (tab() === 'overview') {
          <section class="panel">
            <p>{{ s.summary || 'No summary provided.' }}</p>
            <dl class="meta">
              <div>
                <dt>Lead Auditor</dt>
                <dd>{{ s.leadAuditorName || '—' }}</dd>
              </div>
              <div>
                <dt>Findings</dt>
                <dd>{{ s.findingsCount }}</dd>
              </div>
              <div>
                <dt>Observations</dt>
                <dd>{{ s.observationsCount }}</dd>
              </div>
            </dl>
          </section>
        }

        @if (tab() === 'observations') {
          <section class="panel">
            <h2>Capture Observation</h2>
            <div class="form-inline">
              <select [(ngModel)]="obsForm.type">
                @for (t of observationTypes; track t) {
                  <option [value]="t">{{ observationLabel(t) }}</option>
                }
              </select>
              <input [(ngModel)]="obsForm.title" placeholder="Title" />
              <input [(ngModel)]="obsForm.description" placeholder="Description" class="wide" />
              <button type="button" class="btn-primary" (click)="addObservation()">Add</button>
            </div>
            @for (o of s.observations; track o.id) {
              <article class="item">
                <strong>{{ o.observationNumber }}</strong> · {{ observationLabel(o.type) }}
                <h3>{{ o.title }}</h3>
                <p>{{ o.description }}</p>
              </article>
            }
          </section>
        }

        @if (tab() === 'findings') {
          <section class="panel">
            <p class="muted">
              Findings use the Findings Workbench — open detail for evidence, comments, and CAPA
              promotion.
            </p>
            @for (f of s.findings; track f.id) {
              <article class="item">
                <strong>{{ f.findingNumber }}</strong> · {{ f.severity }}
                <h3>
                  <a [routerLink]="['/app/audit/findings', f.id]">{{ f.title }}</a>
                </h3>
                <p>
                  Status: {{ f.status }}
                  @if (f.promotedDeficiencyId) {
                    · Promoted
                  }
                </p>
              </article>
            }
            @if (!s.findings.length) {
              <p class="muted">No findings linked to this session yet.</p>
            }
          </section>
        }

        @if (tab() === 'comments') {
          <section class="panel">
            <div class="form-inline">
              <input
                [(ngModel)]="commentText"
                placeholder="Add comment or clarification request…"
                class="wide"
              />
              <button type="button" class="btn-primary" (click)="addComment()">Post</button>
            </div>
            @for (c of s.comments; track c.id) {
              <article class="item" [class.auditor]="c.isAuditorAuthor">
                <strong>{{ c.authorName || 'User' }}</strong>
                @if (c.isAuditorAuthor) {
                  <span class="tag">Auditor</span>
                }
                <p>{{ c.content }}</p>
              </article>
            }
          </section>
        }

        @if (tab() === 'report' && reportMarkdown()) {
          <section class="panel report-preview">
            <pre>{{ reportMarkdown() }}</pre>
          </section>
        }
      }
    </div>
  `,
  styles: [
    `
      .back {
        font-size: 13px;
        color: var(--text-secondary);
        text-decoration: none;
      }
      .page-header {
        display: flex;
        justify-content: space-between;
        margin-bottom: 16px;
        flex-wrap: wrap;
        gap: 12px;
      }
      .page-header h1 {
        margin: 8px 0 0;
      }
      .subtitle {
        color: var(--text-secondary);
        margin: 4px 0 0;
      }
      .header-actions {
        display: flex;
        gap: 8px;
        align-items: center;
      }
      .tabs {
        display: flex;
        gap: 4px;
        margin-bottom: 16px;
        flex-wrap: wrap;
      }
      .tabs button {
        padding: 8px 12px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: #fff;
        border-radius: 6px;
        cursor: pointer;
      }
      .tabs button.active {
        background: var(--color-primary);
        color: #fff;
        border-color: var(--color-primary);
      }
      .panel {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 16px;
      }
      .meta {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
        gap: 12px;
        margin-top: 12px;
      }
      .meta dt {
        font-size: 11px;
        text-transform: uppercase;
        color: var(--text-secondary);
      }
      .form-inline {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
        margin-bottom: 16px;
      }
      .form-inline .wide {
        flex: 1;
        min-width: 200px;
      }
      .item {
        border-top: 1px solid var(--border-color, #e5e7eb);
        padding: 12px 0;
      }
      .item.auditor {
        border-left: 3px solid var(--color-primary);
        padding-left: 10px;
      }
      .tag {
        font-size: 10px;
        background: var(--color-primary-100);
        color: var(--color-primary-hover);
        padding: 2px 6px;
        border-radius: 4px;
        margin-left: 6px;
      }
      .report-preview pre {
        white-space: pre-wrap;
        font-size: 12px;
        line-height: 1.5;
      }
      .btn-primary,
      .btn-secondary {
        padding: 8px 14px;
        border-radius: 6px;
        text-decoration: none;
        font-size: 13px;
        border: none;
        cursor: pointer;
      }
      .btn-primary {
        background: var(--color-primary);
        color: #fff;
      }
      .btn-secondary {
        border: 1px solid var(--border-color, #e5e7eb);
        background: #fff;
        color: var(--text-primary);
      }
      .btn-danger {
        padding: 8px 14px;
        border-radius: 6px;
        border: none;
        cursor: pointer;
        font-size: 13px;
        background: #b91c1c;
        color: #fff;
      }
      .btn-danger:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .banner {
        padding: 10px 14px;
        border-radius: 8px;
        margin-bottom: 12px;
        font-size: 13px;
      }
      .banner.error {
        background: #fef2f2;
        color: #b91c1c;
      }
      .banner.ok {
        background: #ecfdf5;
        color: #047857;
      }
      .muted {
        color: var(--text-secondary);
      }
    `,
  ],
})
export class AuditorSessionWorkspacePageComponent implements OnInit {
  private readonly api = inject(AuditorApiService);
  private readonly configuration = inject(PlatformConfigurationService);
  private readonly route = inject(ActivatedRoute);
  private readonly workspaceState = inject(AuditWorkspaceStateService);
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  readonly loading = signal(true);
  readonly closing = signal(false);
  readonly closeError = signal<string | null>(null);
  readonly closeInfo = signal<string | null>(null);
  readonly tab = signal<'overview' | 'observations' | 'findings' | 'comments' | 'report'>(
    'overview',
  );
  readonly session = signal<AuditSessionDetail | null>(null);
  readonly reportMarkdown = signal<string | null>(null);

  readonly observationTypes: ObservationType[] = [
    'PositivePractice',
    'Suggestion',
    'MinorObservation',
    'Strength',
    'Opportunity',
    'GeneralComment',
  ];

  obsForm = { type: 'GeneralComment' as ObservationType, title: '', description: '' };
  commentText = '';

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((p) => {
          const id = p.get('id')!;
          this.loading.set(true);
          return this.api.getSession(id).pipe(
            catchError(() => of(null)),
            finalize(() => this.loading.set(false)),
          );
        }),
      )
      .subscribe((s) => {
        if (s) {
          this.session.set(s);
          // The session workspace is where an audit is actually executed, so it
          // also binds the workspace's active audit — downstream capabilities
          // read that rather than re-deriving it from the URL.
          this.workspaceState.setActiveAudit(s.id, s.scheduledAuditId ?? null);
          this.publishParticipation();
        }
      });
  }

  addObservation(): void {
    const s = this.session();
    if (!s || !this.obsForm.title.trim()) return;
    this.api
      .createObservation({
        auditSessionId: s.id,
        type: this.obsForm.type,
        title: this.obsForm.title,
        description: this.obsForm.description,
      })
      .subscribe((obs) => {
        this.session.update((cur) =>
          cur
            ? {
                ...cur,
                observations: [obs, ...cur.observations],
                observationsCount: cur.observationsCount + 1,
              }
            : cur,
        );
        this.obsForm = { type: 'GeneralComment', title: '', description: '' };
      });
  }

  addComment(): void {
    const s = this.session();
    if (!s || !this.commentText.trim()) return;
    this.api
      .createComment({ auditSessionId: s.id, type: 'GeneralComment', content: this.commentText })
      .subscribe((c) => {
        this.session.update((cur) => (cur ? { ...cur, comments: [...cur.comments, c] } : cur));
        this.commentText = '';
      });
  }

  loadReport(): void {
    const s = this.session();
    if (!s) return;
    this.api.getReport(s.id).subscribe((r) => {
      this.reportMarkdown.set(r.markdownContent);
      this.tab.set('report');
    });
  }

  closeSession(): void {
    const s = this.session();
    if (!s || this.closing()) return;
    this.closing.set(true);
    this.closeError.set(null);
    this.closeInfo.set(null);
    this.api
      .closeSession(s.id, { summary: s.summary || undefined })
      .pipe(finalize(() => this.closing.set(false)))
      .subscribe({
        next: (updated) => {
          this.session.set(updated);
          this.closeInfo.set('Session closed successfully.');
        },
        error: (err) => {
          const msg =
            err?.error?.message ||
            err?.error?.title ||
            err?.message ||
            'Unable to close session. Resolve or promote open findings first (close gate).';
          this.closeError.set(
            typeof msg === 'string'
              ? msg
              : 'Unable to close session. Open findings may block closure.',
          );
        },
      });
  }

  statusLabel(status: AuditSessionDetail['status']): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.auditSessionStatus, status, status);
  }

  observationLabel(type: ObservationType): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.observationType, type, type);
  }

  /**
   * The execution centre's declaration — the richest in the module, because
   * this is the one page that holds a whole audit in memory: its observations,
   * findings and team are all already loaded here.
   */
  private publishParticipation(): void {
    const s = this.session();
    if (!s) return;

    this.workspace.set({
      stage: 'execution',
      subject: { kind: 'audit', id: s.id, title: s.scope || s.sessionNumber },
      actions: [
        {
          id: 'session.close',
          label: 'Close audit',
          variant: 'primary',
          disabled: this.closing() || String(s.status) === 'Closed',
          disabledReason:
            String(s.status) === 'Closed' ? 'Audit already closed' : 'Closing in progress',
          run: () => this.closeSession(),
        },
      ],
      workflowState: {
        stage: 'execution',
        statusLabel: String(s.status),
        completionCriteria: 'All observations recorded and findings raised',
      },
      relatedRecords: [
        ...s.observations.slice(0, 6).map((o) => ({
          id: o.id,
          label: o.title,
          kind: 'Observation',
          status: this.observationLabel(o.type),
        })),
        ...s.findings.slice(0, 6).map((f) => ({
          id: f.id,
          label: f.title,
          kind: 'Finding',
          status: String(f.status),
          route: ['/app/audit/findings'] as const,
        })),
      ],
      timeline: s.comments.slice(0, 8).map((c) => ({
        id: c.id,
        label: c.content,
        at: c.createdAt,
        actor: c.authorName ?? undefined,
      })),
      aiContext: {
        framework: s.frameworkId ?? null,
        facts: {
          sessionNumber: s.sessionNumber,
          status: String(s.status),
          observations: String(s.observationsCount),
          findings: String(s.findingsCount),
        },
      },
      /**
       * The session detail is the one payload carrying the whole audit, so this
       * is where real lifecycle progress can be derived rather than inferred
       * from the open tab (Stage 1.4, Step 12).
       *
       * Evidence and CAPA counts are deliberately omitted: this payload does not
       * carry them, and reporting a guess would mark those stages complete on no
       * evidence at all.
       */
      lifecycleFacts: {
        hasScheduledAudit: s.scheduledAuditId != null,
        hasSession: true,
        sessionClosed: String(s.status) === 'Closed',
        observationCount: s.observationsCount,
        findingCount: s.findingsCount,
        openFindingCount: s.findings.filter((f) => String(f.status) !== 'Closed').length,
      },
    });
  }
}
