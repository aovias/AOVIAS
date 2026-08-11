import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  Subject,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  filter,
  finalize,
  forkJoin,
  of,
  takeUntil,
} from 'rxjs';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { MembershipScopeService } from '../../../core/services/membership-scope.service';
import { injectAuditWorkspace } from '../workspace/services/audit-workspace-participation.service';
import { DeficiencyDataService } from '../../journey/services/deficiency-data.service';
import { ComplianceMaturityService } from '../../compliance-maturity/services/compliance-maturity.service';
import { DeficiencyCommandCenterService } from '../deficiency-command-center/services/deficiency-command-center.service';
import { DccDashboardComponent } from '../deficiency-command-center/components/dcc-dashboard.component';
import { DccGridComponent } from '../deficiency-command-center/components/dcc-grid.component';
import { DccDetailPanelComponent } from '../deficiency-command-center/components/dcc-detail-panel.component';
import { exportDeficienciesCsv } from '../deficiency-command-center/utils/deficiency-export.util';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';
import type {
  DeficiencyCommandCenterDashboard,
  DeficiencyWorkspaceDetail,
  DeficiencyWorkspaceItem,
  PatchDeficiencyRcaRequest,
  PatchDeficiencyRemediationRequest,
  PatchDeficiencyCorrectiveActionRequest,
  PatchDeficiencyPreventiveActionRequest,
  PatchDeficiencyEffectivenessReviewRequest,
  AssignmentRoutingSuggestion,
} from '../deficiency-command-center/models/deficiency-command-center.model';

@Component({
  selector: 'app-corrective-actions',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DccDashboardComponent,
    DccGridComponent,
    DccDetailPanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="dcc-page">
      <header class="page-header">
        <div>
          <h1>Issues &amp; Risks</h1>
          <p class="subtitle">Deficiency Command Center — what is preventing accreditation?</p>
        </div>
        <button type="button" class="btn-export" (click)="exportCsv()" [disabled]="!items().length">
          Export CSV
        </button>
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="muted">Loading deficiency command center…</p>
      } @else {
        <app-dcc-dashboard [dashboard]="dashboard()" [predictedGaps]="predictedGaps()" />

        <div class="filters">
          <input
            type="search"
            placeholder="Search deficiencies, OE codes, chapters…"
            [(ngModel)]="searchInput"
            (ngModelChange)="onSearchChange($event)"
          />
          <select [(ngModel)]="filterStatus" (ngModelChange)="reload()">
            <option value="">All Statuses</option>
            @for (s of statusOptions(); track s.value) {
              <option [value]="s.value">{{ s.label }}</option>
            }
          </select>
          <select [(ngModel)]="filterSeverity" (ngModelChange)="reload()">
            <option value="">All Severities</option>
            @for (s of severityOptions(); track s.value) {
              <option [value]="s.value">{{ s.label }}</option>
            }
          </select>
          <select [(ngModel)]="filterStage" (ngModelChange)="reload()">
            <option value="">All Stages</option>
            @for (s of stageOptions(); track s.value) {
              <option [value]="s.value">{{ s.label }}</option>
            }
          </select>
          <select [(ngModel)]="groupBy" (ngModelChange)="applyGrouping()">
            <option value="">No Grouping</option>
            @for (g of groupingOptions(); track g.value) {
              <option [value]="g.value">{{ g.label }}</option>
            }
          </select>
        </div>

        @if (groupBy) {
          @for (group of groupedItems(); track group.key) {
            <h3 class="group-heading">{{ group.label }} ({{ group.items.length }})</h3>
            <app-dcc-grid
              [items]="group.items"
              [selectedId]="selectedId()"
              (select)="openDetail($event)"
              (sort)="onSort($event)"
            />
          }
        } @else {
          <app-dcc-grid
            [items]="items()"
            [selectedId]="selectedId()"
            (select)="openDetail($event)"
            (sort)="onSort($event)"
          />
        }

        <div class="pagination">
          <button type="button" [disabled]="page() <= 1 || loading()" (click)="setPage(page() - 1)">
            Previous
          </button>
          <span>Page {{ page() }} · {{ totalCount() }} deficiencies</span>
          <button
            type="button"
            [disabled]="page() * pageSize() >= totalCount() || loading()"
            (click)="setPage(page() + 1)"
          >
            Next
          </button>
        </div>
      }

      @if (selectedDetail()) {
        <app-dcc-detail-panel
          [detail]="selectedDetail()"
          [routingSuggestion]="routingSuggestion()"
          [routingLoading]="routingLoading()"
          [saving]="savingDetail()"
          (close)="closeDetail()"
          (rcaSave)="saveRca($event)"
          (remediationSave)="saveRemediation($event)"
          (correctiveSave)="saveCorrectiveAction($event)"
          (preventiveSave)="savePreventiveAction($event)"
          (effectivenessSave)="saveEffectivenessReview($event)"
          (acceptRouting)="acceptRouting()"
          (acknowledgeEscalation)="acknowledgeEscalation($event)"
          (acceptOwnership)="acceptOwnership()"
          (rejectOwnership)="rejectOwnership()"
        />
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
      }
      .dcc-page {
        width: 100%;
        max-width: 100%;
        box-sizing: border-box;
        padding: var(--page-padding-block-start) var(--page-padding-inline)
          var(--page-padding-block-end);
        min-height: calc(100vh - 64px);
        background: var(--bg-primary, #f9fafb);
      }
      .page-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 20px;
        gap: 16px;
      }
      h1 {
        margin: 0 0 6px;
        font-size: var(--font-size-page-title);
        font-weight: 600;
      }
      .subtitle {
        margin: 0;
        color: var(--text-secondary, #6b7280);
        font-size: var(--font-size-subtitle);
      }
      .btn-export {
        padding: 8px 16px;
        border-radius: 6px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--card-bg, #fff);
        cursor: pointer;
        font-size: 13px;
      }
      .filters {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        margin-bottom: 16px;
      }
      .filters input,
      .filters select {
        padding: 8px 12px;
        border-radius: 6px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--card-bg, #fff);
        font-size: 13px;
      }
      .filters input {
        flex: 1;
        min-width: 220px;
      }
      .pagination {
        display: flex;
        align-items: center;
        gap: 16px;
        margin-top: 16px;
        font-size: 13px;
      }
      .pagination button {
        padding: 6px 12px;
        border-radius: 6px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--card-bg, #fff);
        cursor: pointer;
      }
      .banner.error {
        background: #fef2f2;
        color: #b91c1c;
        padding: 12px 16px;
        border-radius: 8px;
        margin-bottom: 16px;
      }
      .muted {
        color: var(--text-secondary);
      }
      .group-heading {
        margin: 20px 0 8px;
        font-size: 14px;
        font-weight: 600;
        color: var(--text-secondary);
      }
    `,
  ],
})
export class CorrectiveActionsComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly context = inject(ComplianceContextService);
  private readonly dcc = inject(DeficiencyCommandCenterService);
  private readonly deficiencyBootstrap = inject(DeficiencyDataService);
  private readonly cmService = inject(ComplianceMaturityService);
  private readonly membershipScope = inject(MembershipScopeService);
  /** Feeds panel, toolbar and assistant; withdrawn on destroy. */
  private readonly workspace = injectAuditWorkspace();
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();
  private deptScopeName: string | null = null;

  @ViewChild(DccDetailPanelComponent) detailPanel?: DccDetailPanelComponent;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly dashboard = signal<DeficiencyCommandCenterDashboard | null>(null);
  readonly predictedGaps = signal(0);
  readonly items = signal<DeficiencyWorkspaceItem[]>([]);
  readonly groupedItems = signal<
    { key: string; label: string; items: DeficiencyWorkspaceItem[] }[]
  >([]);
  readonly selectedId = signal<string | null>(null);
  readonly selectedDetail = signal<DeficiencyWorkspaceDetail | null>(null);
  readonly routingSuggestion = signal<AssignmentRoutingSuggestion | null>(null);
  readonly routingLoading = signal(false);
  readonly savingDetail = signal(false);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);

  private readonly configuration = inject(PlatformConfigurationService);

  readonly statusOptions = this.configuration.optionsWith(
    CONFIGURATION_CATEGORY.deficiencyStatus,
    'selectableWhenFiltering',
  );
  readonly severityOptions = this.configuration.options(CONFIGURATION_CATEGORY.deficiencySeverity);
  readonly stageOptions = this.configuration.options(CONFIGURATION_CATEGORY.maturityStage);
  readonly groupingOptions = this.configuration.options(CONFIGURATION_CATEGORY.deficiencyGrouping);

  searchInput = '';
  filterStatus = '';
  filterSeverity = '';
  filterStage = '';
  groupBy = '';
  sortBy = 'severity';
  sortDesc = true;

  ngOnInit(): void {
    // Declares the workspace subject and the assistant context together —
    // replaces the direct AiPageContextService call that lived here.
    this.publishParticipation();

    const initialSearch = this.route.snapshot.queryParamMap.get('search');
    if (initialSearch) {
      this.searchInput = initialSearch;
    }

    this.search$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.page.set(1);
        this.reload();
      });

    this.route.queryParamMap.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      const s = params.get('search');
      if (s != null && s !== this.searchInput) {
        this.searchInput = s;
        this.page.set(1);
        this.reload();
      }
      const deficiencyId = params.get('deficiencyId');
      if (deficiencyId) {
        this.openDetail(deficiencyId);
      }
    });

    this.membershipScope
      .resolveDepartmentScope$()
      .pipe(
        catchError(() => of({ departmentId: null, departmentName: null, isDepartmentHead: false })),
        takeUntil(this.destroy$),
      )
      .subscribe((scope) => {
        this.deptScopeName = scope.isDepartmentHead ? scope.departmentName : null;
        if (scope.isDepartmentHead && scope.departmentName && !this.searchInput) {
          // Prefer name match when DCC workspace lacks departmentId filter.
          this.searchInput = scope.departmentName;
        }
      });

    this.bootstrapAndLoad();
  }

  ngOnDestroy(): void {
    // Workspace participation (and with it the assistant context) is withdrawn
    // automatically via DestroyRef.
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * CAPA's declaration. Re-published on load and selection so the panel follows
   * the opened deficiency rather than the queue as a whole.
   */
  private publishParticipation(): void {
    const selected = this.selectedDetail();
    const dash = this.dashboard();

    this.workspace.set({
      stage: 'capa',
      subject: selected
        ? { kind: 'capa', id: selected.id, title: selected.title ?? selected.deficiencyCode }
        : { kind: 'capa', title: 'Deficiency Command Center' },
      actions: [
        {
          id: 'capa.accept-routing',
          label: 'Accept suggested owner',
          disabled: !this.routingSuggestion(),
          disabledReason: 'No routing suggestion for the selected deficiency',
          run: () => this.acceptRouting(),
        },
        {
          id: 'capa.accept-ownership',
          label: 'Accept ownership',
          disabled: !selected || !!selected.ownerAcceptedAt,
          disabledReason: selected?.ownerAcceptedAt
            ? 'Ownership already accepted'
            : 'Select a deficiency first',
          run: () => this.acceptOwnership(),
        },
        {
          id: 'capa.export',
          label: 'Export CSV',
          run: () => this.exportCsv(),
        },
      ],
      workflowState: {
        stage: 'capa',
        statusLabel: selected?.lifecycleStatus
          ? String(selected.lifecycleStatus)
          : `${dash?.openDeficiencies ?? this.totalCount()} open`,
        completionCriteria: 'Every corrective action verified and closed',
      },
      relatedRecords: this.items()
        .slice(0, 8)
        .map((d) => ({
          id: d.id,
          label: d.title ?? d.deficiencyCode,
          kind: 'Deficiency',
          status: String(d.status),
        })),
      aiContext: {
        facts: {
          openDeficiencies: String(dash?.openDeficiencies ?? 0),
          criticalDeficiencies: String(dash?.criticalDeficiencies ?? 0),
          blockingDeficiencies: String(dash?.blockingDeficiencies ?? 0),
          selectedDeficiency: selected?.deficiencyCode ?? 'none',
        },
      },
    });
  }

  onSearchChange(value: string): void {
    this.search$.next(value);
  }

  onSort(column: string): void {
    if (this.sortBy === column) {
      this.sortDesc = !this.sortDesc;
    } else {
      this.sortBy = column;
      this.sortDesc = true;
    }
    this.reload();
  }

  setPage(p: number): void {
    this.page.set(p);
    this.reload();
  }

  reload(): void {
    const hospitalId = this.context.getHospitalId();
    const frameworkId = this.context.getFrameworkId();
    if (!hospitalId || !frameworkId) {
      this.error.set('Select a hospital and framework to view deficiencies.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      dashboard: this.dcc.getDashboard(hospitalId, frameworkId),
      workspace: this.dcc.getWorkspace(hospitalId, frameworkId, {
        status: this.filterStatus || undefined,
        severity: this.filterSeverity || undefined,
        maturityLevel: this.filterStage || undefined,
        search: this.searchInput.trim() || undefined,
        sortBy: this.sortBy,
        sortDesc: this.sortDesc,
        page: this.page(),
        pageSize: this.pageSize(),
      }),
      cm: this.cmService.getComplianceMaturityDashboard().pipe(catchError(() => of(null))),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ dashboard, workspace, cm }) => {
          this.dashboard.set(dashboard);
          let rows = workspace.items ?? [];
          if (this.deptScopeName) {
            const needle = this.deptScopeName.toLowerCase();
            rows = rows.filter((i) => (i.department ?? '').toLowerCase().includes(needle));
          }
          this.items.set(rows);
          this.totalCount.set(this.deptScopeName ? rows.length : workspace.totalCount);
          this.applyGrouping();

          if (cm) {
            const predicted =
              (cm.commitmentSummary?.predictedGapCount ?? 0) +
              (cm.achievementSummary?.predictedGapCount ?? 0) +
              (cm.excellenceSummary?.predictedGapCount ?? 0);
            this.predictedGaps.set(predicted);
          }
          this.publishParticipation();
        },
        error: () => this.error.set('Failed to load deficiency command center data.'),
      });
  }

  openDetail(id: string): void {
    this.selectedId.set(id);
    this.routingSuggestion.set(null);
    this.dcc.getDetail(id).subscribe({
      next: (detail) => {
        this.selectedDetail.set(detail);
        queueMicrotask(() => this.detailPanel?.syncFromDetail(detail));
        this.loadRoutingSuggestion(id);
        this.publishParticipation();
      },
      error: () => this.error.set('Failed to load deficiency detail.'),
    });
  }

  closeDetail(): void {
    this.selectedId.set(null);
    this.selectedDetail.set(null);
    this.routingSuggestion.set(null);
  }

  acceptRouting(): void {
    const id = this.selectedId();
    if (!id || this.routingLoading()) return;

    this.routingLoading.set(true);
    const force = !this.routingSuggestion()?.canAutoAssign;
    this.dcc.autoAssign(id, force).subscribe({
      next: () => {
        this.dcc.getDetail(id).subscribe({
          next: (detail) => {
            this.selectedDetail.set(detail);
            queueMicrotask(() => this.detailPanel?.syncFromDetail(detail));
            this.loadRoutingSuggestion(id);
            this.routingLoading.set(false);
            this.reload();
          },
          error: () => this.routingLoading.set(false),
        });
      },
      error: () => {
        this.routingLoading.set(false);
        this.error.set('Failed to apply suggested assignment.');
      },
    });
  }

  private loadRoutingSuggestion(deficiencyId: string): void {
    this.dcc.getAssignmentSuggestion(deficiencyId).subscribe({
      next: (suggestion) => this.routingSuggestion.set(suggestion),
      error: () => this.routingSuggestion.set(null),
    });
  }

  acknowledgeEscalation(escalationId: string): void {
    const id = this.selectedId();
    if (!id) return;

    this.savingDetail.set(true);
    this.dcc.acknowledgeEscalation(id, escalationId).subscribe({
      next: () => {
        this.dcc.getDetail(id).subscribe({
          next: (detail) => {
            this.selectedDetail.set(detail);
            queueMicrotask(() => this.detailPanel?.syncFromDetail(detail));
            this.savingDetail.set(false);
            this.reload();
          },
          error: () => this.savingDetail.set(false),
        });
      },
      error: () => {
        this.savingDetail.set(false);
        this.error.set('Failed to acknowledge escalation.');
      },
    });
  }

  acceptOwnership(): void {
    this.saveCapaPatch((id) => this.dcc.acceptOwnership(id), 'Failed to accept CAPA ownership.');
  }

  rejectOwnership(): void {
    const reason = window.prompt('Reason for rejecting ownership (optional):') ?? undefined;
    this.saveCapaPatch(
      (id) => this.dcc.rejectOwnership(id, reason || undefined),
      'Failed to reject CAPA ownership.',
    );
  }

  saveRca(body: PatchDeficiencyRcaRequest): void {
    const id = this.selectedId();
    if (!id) return;

    this.savingDetail.set(true);
    this.dcc.patchRca(id, body).subscribe({
      next: () => {
        this.dcc.getDetail(id).subscribe({
          next: (detail) => {
            this.selectedDetail.set(detail);
            this.items.update((rows) =>
              rows.map((r) =>
                r.id === id ? { ...r, rootCauseCategory: detail.rootCauseCategory } : r,
              ),
            );
            queueMicrotask(() => this.detailPanel?.syncFromDetail(detail));
            this.savingDetail.set(false);
            this.reload();
          },
          error: () => this.savingDetail.set(false),
        });
      },
      error: () => {
        this.savingDetail.set(false);
        this.error.set('Failed to save RCA.');
      },
    });
  }

  saveRemediation(body: PatchDeficiencyRemediationRequest): void {
    this.saveCapaPatch(
      (id) => this.dcc.patchRemediation(id, body),
      'Failed to save remediation plan.',
    );
  }

  saveCorrectiveAction(body: PatchDeficiencyCorrectiveActionRequest): void {
    this.saveCapaPatch(
      (id) => this.dcc.patchCorrectiveAction(id, body),
      'Failed to save corrective action.',
    );
  }

  savePreventiveAction(body: PatchDeficiencyPreventiveActionRequest): void {
    this.saveCapaPatch(
      (id) => this.dcc.patchPreventiveAction(id, body),
      'Failed to save preventive action.',
    );
  }

  saveEffectivenessReview(body: PatchDeficiencyEffectivenessReviewRequest): void {
    this.saveCapaPatch(
      (id) => this.dcc.patchEffectivenessReview(id, body),
      'Failed to save effectiveness review.',
    );
  }

  private saveCapaPatch(
    request: (id: string) => ReturnType<DeficiencyCommandCenterService['patchRemediation']>,
    errorMessage: string,
  ): void {
    const id = this.selectedId();
    if (!id) return;

    this.savingDetail.set(true);
    request(id).subscribe({
      next: () => {
        this.dcc.getDetail(id).subscribe({
          next: (detail) => {
            this.selectedDetail.set(detail);
            queueMicrotask(() => this.detailPanel?.syncFromDetail(detail));
            this.savingDetail.set(false);
            this.reload();
          },
          error: () => this.savingDetail.set(false),
        });
      },
      error: () => {
        this.savingDetail.set(false);
        this.error.set(errorMessage);
      },
    });
  }

  exportCsv(): void {
    exportDeficienciesCsv(this.items());
  }

  applyGrouping(): void {
    const rows = this.items();
    if (!this.groupBy) {
      this.groupedItems.set([]);
      return;
    }

    // Which property to group on and which category names the resulting headings both come from
    // the grouping's own configuration, so adding a grouping is a configuration change rather than
    // another arm of a conditional here.
    const grouping = CONFIGURATION_CATEGORY.deficiencyGrouping;
    const field = this.configuration.attribute(grouping, this.groupBy, 'field', '');
    const labelCategory = this.configuration.attribute(
      grouping,
      this.groupBy,
      'labelFromCategory',
      '',
    );
    if (!field) {
      this.groupedItems.set([]);
      return;
    }

    const map = new Map<string, DeficiencyWorkspaceItem[]>();
    for (const row of rows) {
      const key = String((row as unknown as Record<string, unknown>)[field] ?? '');
      const list = map.get(key) ?? [];
      list.push(row);
      map.set(key, list);
    }

    this.groupedItems.set(
      [...map.entries()].map(([key, groupItems]) => ({
        key,
        label: this.configuration.label(labelCategory, key, key),
        items: groupItems,
      })),
    );
  }

  private bootstrapAndLoad(): void {
    // Wait for real organization context — never fall back to hospitalId (that stamped
    // deficiencies with OrganizationId = HospitalId and corrupted the CAPA set).
    combineLatest([
      this.context.hospitalId$,
      this.context.frameworkId$,
      this.context.organizationId$,
    ])
      .pipe(
        filter(([h, f, o]) => !!h && !!f && !!o),
        takeUntil(this.destroy$),
      )
      .subscribe(([hospitalId, frameworkId, organizationId]) => {
        this.deficiencyBootstrap
          .bootstrapFromOnboarding(organizationId!, hospitalId!, frameworkId!)
          .pipe(
            catchError(() => of(null)),
            finalize(() => this.reload()),
          )
          .subscribe();
      });
  }
}
