import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { FindingsDetailPanelComponent } from '../findings-workspace/components/findings-detail-panel.component';
import { FindingsWorkspaceService } from '../findings-workspace/services/findings-workspace.service';
import type {
  FindingPromotionStatus,
  FindingsWorkspaceDetail,
} from '../findings-workspace/models/findings-workspace.model';
import type { AssignmentRoutingSuggestion } from '../deficiency-command-center/models/deficiency-command-center.model';
import { toFindingWireEnum } from '../models/finding.models';
import { injectAuditWorkspace } from '../workspace/services/audit-workspace-participation.service';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';

/**
 * Full-page finding detail — same workspace panels as the Findings Workbench drawer.
 */
@Component({
  selector: 'app-audit-finding-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FindingsDetailPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <a routerLink="/app/audit/findings" class="back">← Findings Workbench</a>
          <h1>Finding detail</h1>
          <p class="subtitle">Lifecycle, evidence, comments, and promotion to CAPA</p>
        </div>
      </header>

      @if (error()) {
        <div class="banner error">
          {{ error() }}
          <button type="button" class="btn-secondary" (click)="load()">Retry</button>
        </div>
      }

      @if (loading()) {
        <p class="muted">Loading finding…</p>
      } @else if (detail(); as d) {
        <div class="detail-shell">
          <app-findings-detail-panel
            [detail]="d"
            [promotionStatus]="promotionStatus()"
            [routingSuggestion]="routingSuggestion()"
            [promotionLoading]="promotionLoading()"
            [promotionSaving]="promotionSaving()"
            [statusSaving]="statusSaving()"
            [mappingSaving]="mappingSaving()"
            (close)="goBack()"
            (statusChange)="onStatusChange($event)"
            (complianceContextMapped)="onComplianceContextMapped($event)"
            (promote)="promote()"
            (commentAdded)="onComment($event)"
            (attachmentAdded)="onAttachment($event)"
          />
        </div>
      }
    </div>
  `,
  styles: [
    `
      .page {
        padding: var(--page-padding-block-start) var(--page-padding-inline)
          var(--page-padding-block-end);
        color: var(--text-primary);
      }
      .page-header {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
        margin-bottom: 20px;
      }
      .back {
        color: var(--text-secondary);
        text-decoration: none;
        font-size: 13px;
      }
      h1 {
        margin: 8px 0 4px;
        font-size: var(--font-size-page-title);
      }
      .subtitle {
        margin: 0;
        color: var(--text-secondary);
        font-size: 14px;
      }
      .header-actions {
        display: flex;
        gap: 8px;
        align-items: flex-start;
      }
      .btn-secondary {
        padding: 8px 14px;
        border-radius: 6px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--card-bg, #fff);
        color: var(--text-primary);
        text-decoration: none;
        font-size: 13px;
        cursor: pointer;
      }
      .detail-shell {
        max-width: 720px;
      }
      .detail-shell ::ng-deep .panel {
        position: static;
        width: 100%;
        max-height: none;
        box-shadow: none;
      }
      .detail-shell ::ng-deep .close {
        display: none;
      }
      .banner.error {
        background: #fef2f2;
        color: #b91c1c;
        padding: 12px 16px;
        border-radius: 8px;
        margin-bottom: 16px;
        display: flex;
        gap: 12px;
        align-items: center;
      }
      .muted {
        color: var(--text-secondary);
      }
    `,
  ],
})
export class AuditFindingDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly workspace = inject(FindingsWorkspaceService);
  private readonly context = inject(ComplianceContextService);
  /** Withdrawn automatically on destroy. */
  private readonly auditWorkspace = injectAuditWorkspace();
  private readonly configuration = inject(PlatformConfigurationService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly detail = signal<FindingsWorkspaceDetail | null>(null);
  readonly promotionStatus = signal<FindingPromotionStatus | null>(null);
  readonly routingSuggestion = signal<AssignmentRoutingSuggestion | null>(null);
  readonly promotionLoading = signal(false);
  readonly promotionSaving = signal(false);
  readonly statusSaving = signal(false);
  readonly mappingSaving = signal(false);

  private findingId = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe((p) => {
      this.findingId = p.get('id') ?? '';
      this.load();
    });
  }

  load(): void {
    if (!this.findingId) {
      this.error.set('Finding id is missing.');
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.promotionLoading.set(true);
    forkJoin({
      detail: this.workspace.getDetail(this.findingId),
      promotion: this.workspace.getPromotionStatus(this.findingId).pipe(catchError(() => of(null))),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ detail, promotion }) => {
          this.detail.set(detail);
          this.promotionStatus.set(promotion);
          this.promotionLoading.set(false);
          this.loadRouting();
          this.publishParticipation();
        },
        error: () => {
          this.promotionLoading.set(false);
          this.error.set('Failed to load finding detail.');
        },
      });
  }

  /**
   * Finding detail is the one page always scoped to a single record, so its
   * declaration is the most specific in the module — subject, lifecycle status
   * and promotion state all come straight from the loaded finding.
   */
  private publishParticipation(): void {
    const d = this.detail();
    if (!d) return;

    this.auditWorkspace.set({
      stage: 'findings',
      subject: { kind: 'finding', id: d.id, title: d.title || d.findingNumber },
      actions: [
        {
          id: 'finding.promote',
          label: 'Create CAPA',
          variant: 'primary',
          disabled: d.isPromoted || this.promotionSaving(),
          disabledReason: d.isPromoted ? 'Already promoted to CAPA' : 'Promotion in progress',
          run: () => this.promote(),
        },
      ],
      workflowState: {
        stage: 'findings',
        // The status bar is user-facing, so it reads the configured label rather than the raw code
        // the API sends — `InProgress` in a status bar is a leaked wire value, not a status.
        statusLabel: this.configuration.label(
          CONFIGURATION_CATEGORY.findingStatus,
          String(d.status),
          String(d.status),
        ),
        completionCriteria: 'Finding promoted to CAPA or closed',
      },
      relatedRecords: [
        ...(d.promotedDeficiencyId
          ? [
              {
                id: d.promotedDeficiencyId,
                label: 'Promoted deficiency',
                kind: 'CAPA',
                route: ['/app/audit/capa'] as const,
              },
            ]
          : []),
        {
          id: `${d.id}-oe`,
          label: `${d.objectiveElementCode} — ${d.objectiveElementName}`,
          kind: 'Objective element',
        },
      ],
      documents: d.attachments.map((a) => ({
        id: a.id,
        title: a.fileName,
        kind: a.attachmentType || 'Attachment',
      })),
      timeline: d.history.map((h, i) => ({
        id: `${h.occurredAt}-${i}`,
        label: h.action,
        at: h.occurredAt,
        actor: h.actorType || undefined,
      })),
      aiContext: {
        department: d.department || null,
        facts: {
          findingNumber: d.findingNumber,
          severity: String(d.severity),
          status: String(d.status),
          chapter: d.chapterCode,
          promoted: String(d.isPromoted),
        },
      },
    });
  }

  goBack(): void {
    void this.router.navigate(['/app/audit/findings']);
  }

  onStatusChange(status: string): void {
    if (!this.findingId || this.statusSaving()) return;
    this.statusSaving.set(true);
    this.workspace.patchStatus(this.findingId, toFindingWireEnum(status)).subscribe({
      next: () => {
        this.statusSaving.set(false);
        this.load();
      },
      error: () => {
        this.statusSaving.set(false);
        this.error.set('Failed to update status.');
      },
    });
  }

  /** DEF-M3-001 — classifies the finding; reloading refreshes the promotion blockers. */
  onComplianceContextMapped(objectiveElementId: string): void {
    if (!this.findingId || this.mappingSaving()) return;
    this.mappingSaving.set(true);
    this.workspace.patchComplianceContext(this.findingId, objectiveElementId).subscribe({
      next: () => {
        this.mappingSaving.set(false);
        this.load();
      },
      error: () => {
        this.mappingSaving.set(false);
        this.error.set('Failed to update compliance mapping.');
      },
    });
  }

  promote(): void {
    if (!this.findingId || this.promotionSaving()) return;
    this.promotionSaving.set(true);
    this.workspace.promoteToDeficiency(this.findingId).subscribe({
      next: (result) => {
        this.promotionSaving.set(false);
        this.load();
        if (result?.deficiencyId) {
          void this.router.navigate(['/app/audit/capa'], {
            queryParams: { deficiencyId: result.deficiencyId },
          });
        }
      },
      error: () => {
        this.promotionSaving.set(false);
        this.error.set('Promotion failed.');
      },
    });
  }

  onComment(body: string): void {
    this.workspace.addComment(this.findingId, body).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Failed to add comment.'),
    });
  }

  onAttachment(payload: { fileName: string; fileUrl: string; attachmentType: string }): void {
    this.workspace.addAttachment(this.findingId, payload).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Failed to add attachment.'),
    });
  }

  private loadRouting(): void {
    const organizationId = this.context.getOrganizationId();
    const hospitalId = this.context.getHospitalId();
    if (!organizationId || !hospitalId) return;
    this.workspace
      .getAssignmentSuggestion(this.findingId, organizationId, hospitalId)
      .pipe(catchError(() => of(null)))
      .subscribe((s) => this.routingSuggestion.set(s));
  }
}
