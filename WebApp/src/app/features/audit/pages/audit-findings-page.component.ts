import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  finalize,
  forkJoin,
  of,
  takeUntil,
} from 'rxjs';
import { injectAuditWorkspace } from '../workspace/services/audit-workspace-participation.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { toFindingWireEnum } from '../models/finding.models';
import { FindingsWorkspaceService } from '../findings-workspace/services/findings-workspace.service';
import { FindingsDashboardComponent } from '../findings-workspace/components/findings-dashboard.component';
import { FindingsGridComponent } from '../findings-workspace/components/findings-grid.component';
import { FindingsDetailPanelComponent } from '../findings-workspace/components/findings-detail-panel.component';
import { FindingsCreatePanelComponent } from '../findings-workspace/components/findings-create-panel.component';
import { exportFindingsCsv } from '../findings-workspace/utils/findings-export.util';
import {
  type FindingsWorkspaceDashboard,
  type FindingsWorkspaceDetail,
  type FindingsWorkspaceItem,
  type FindingPromotionStatus,
} from '../findings-workspace/models/findings-workspace.model';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';
import type { AssignmentRoutingSuggestion } from '../deficiency-command-center/models/deficiency-command-center.model';

@Component({
  selector: 'app-audit-findings-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    FindingsDashboardComponent,
    FindingsGridComponent,
    FindingsDetailPanelComponent,
    FindingsCreatePanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="findings-workbench">
      <header class="page-header">
        <div>
          <h1>Audit Findings</h1>
          <p class="subtitle">Findings Workbench — discovery and audit documentation</p>
        </div>
        <div class="header-actions">
          <button
            type="button"
            class="btn-secondary"
            (click)="exportCsv()"
            [disabled]="!items().length"
          >
            Export CSV
          </button>
          <button type="button" class="btn-primary" (click)="openAdd.set(true)">
            Capture Finding
          </button>
        </div>
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      @if (loading()) {
        <p class="muted">Loading findings workbench…</p>
      } @else {
        <app-findings-dashboard [dashboard]="dashboard()" />

        <div class="chips">
          @for (chip of quickFilters(); track chip.value) {
            <button
              type="button"
              class="chip"
              [class.chip--active]="filterQuick === chip.value"
              (click)="toggleQuickFilter(chip.value)"
            >
              {{ chip.label }}
            </button>
          }
        </div>

        <div class="filters">
          <input
            type="search"
            placeholder="Search findings, OE, chapters…"
            [(ngModel)]="searchInput"
            (ngModelChange)="onSearchChange($event)"
          />
          <select [(ngModel)]="filterStatus" (ngModelChange)="reload()">
            <option value="">All Statuses</option>
            @for (s of statusOptions(); track s.value) {
              <option [value]="s.value">{{ s.label }}</option>
            }
          </select>
          <select [(ngModel)]="filterPromotionReadiness" (ngModelChange)="reload()">
            <option value="">All Promotion Readiness</option>
            @for (p of promotionReadinessOptions(); track p.value) {
              <option [value]="p.value">{{ p.label }}</option>
            }
          </select>
          <select [(ngModel)]="filterClassification" (ngModelChange)="reload()">
            <option value="">All Types</option>
            @for (c of classificationOptions(); track c.value) {
              <option [value]="c.value">{{ c.label }}</option>
            }
          </select>
          <select [(ngModel)]="filterSource" (ngModelChange)="reload()">
            <option value="">All Sources</option>
            @for (s of sourceOptions(); track s.value) {
              <option [value]="s.value">{{ s.label }}</option>
            }
          </select>
          <select [(ngModel)]="filterSeverity" (ngModelChange)="reload()">
            <option value="">All Severities</option>
            @for (s of severityOptions(); track s.value) {
              <option [value]="s.value">{{ s.label }}</option>
            }
          </select>
          <select [(ngModel)]="filterDepartment" (ngModelChange)="reload()">
            <option value="">All Departments</option>
            @for (d of departmentOptions(); track d.value) {
              <option [value]="d.value">{{ d.label }}</option>
            }
          </select>
          <input
            placeholder="Chapter code"
            [(ngModel)]="filterChapterCode"
            (ngModelChange)="onFilterTextChange()"
          />
          <input
            placeholder="Standard code"
            [(ngModel)]="filterStandardCode"
            (ngModelChange)="onFilterTextChange()"
          />
        </div>

        @if (groupBy) {
          @for (group of groupedItems(); track group.key) {
            <h3 class="group-heading">{{ group.label }} ({{ group.items.length }})</h3>
            <app-findings-grid
              [items]="group.items"
              [selectedId]="selectedId()"
              (select)="openDetail($event)"
              (sort)="onSort($event)"
            />
          }
        } @else {
          <app-findings-grid
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
          <span>Page {{ page() }} · {{ totalCount() }} findings</span>
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
        <app-findings-detail-panel
          [detail]="selectedDetail()"
          [promotionStatus]="promotionStatus()"
          [routingSuggestion]="routingSuggestion()"
          [promotionLoading]="promotionLoading()"
          [promotionSaving]="promotionSaving()"
          [statusSaving]="statusSaving()"
          [mappingSaving]="mappingSaving()"
          (close)="closeDetail()"
          (statusChange)="changeStatus($event)"
          (complianceContextMapped)="mapComplianceContext($event)"
          (promote)="promoteFinding()"
          (commentAdded)="addComment($event)"
          (attachmentAdded)="addAttachment($event)"
        />
      }

      @if (openAdd()) {
        <app-findings-create-panel (cancel)="openAdd.set(false)" (created)="onFindingCreated()" />
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
      }
      .findings-workbench {
        width: 100%;
        max-width: 100%;
        box-sizing: border-box;
        padding: 24px;
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
        color: var(--text-secondary);
        font-size: var(--font-size-subtitle);
      }
      .header-actions {
        display: flex;
        gap: 10px;
      }
      .btn-primary,
      .btn-secondary {
        padding: 8px 16px;
        border-radius: 6px;
        font-size: 13px;
        cursor: pointer;
      }
      .btn-primary {
        background: var(--color-primary);
        color: #fff;
        border: none;
      }
      .btn-secondary {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
      }
      .chips {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin-bottom: 12px;
      }
      .chip {
        padding: 6px 12px;
        border-radius: 999px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--card-bg, #fff);
        font-size: 12px;
        cursor: pointer;
      }
      .chip--active {
        background: var(--color-primary);
        color: #fff;
        border-color: var(--color-primary);
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
        border: 1px solid var(--color-border-subtle, var(--border-color, #e5e7eb));
        background: var(--color-bg-surface, var(--card-bg, #fff));
        color: var(--color-text-primary, var(--text-primary));
        font-size: 13px;
        font-family: inherit;
      }
      .filters select {
        cursor: pointer;
        appearance: none;
        padding-right: 32px;
        background-image: url("data:image/svg+xml,%3Csvg width='12' height='12' viewBox='0 0 12 12' fill='none' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M3 4.5L6 7.5L9 4.5' stroke='%2394a3b8' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
        background-repeat: no-repeat;
        background-position: right 12px center;
      }
      .filters select option {
        background: var(--color-bg-surface, var(--card-bg, #fff));
        color: var(--color-text-primary, var(--text-primary));
      }
      :host-context([data-theme='dark']) .filters select {
        color-scheme: dark;
      }
      :host-context([data-theme='light']) .filters select {
        color-scheme: light;
      }
      .filters input[type='search'] {
        flex: 1;
        min-width: 200px;
      }
      .pagination {
        display: flex;
        align-items: center;
        gap: 16px;
        margin-top: 16px;
        font-size: 13px;
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
        margin: 16px 0 8px;
        font-size: 14px;
        color: var(--text-secondary);
      }
    `,
  ],
})
export class AuditFindingsPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly workspace = inject(FindingsWorkspaceService);
  private readonly context = inject(ComplianceContextService);
  /** Feeds the panel, toolbar and app-wide assistant; withdrawn on destroy. */
  private readonly auditWorkspace = injectAuditWorkspace();
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();
  private readonly filterText$ = new Subject<void>();

  private readonly configuration = inject(PlatformConfigurationService);

  /**
   * Every picker on this screen is Administration configuration.
   *
   * The status filter reads `selectableWhenFiltering` rather than listing statuses, so the two
   * states that were previously unfilterable — `InProgress` and `Verified`, present in the label map
   * but missing from the hardcoded `<option>` block — become a configuration toggle rather than a
   * code change.
   */
  readonly statusOptions = this.configuration.optionsWith(
    CONFIGURATION_CATEGORY.findingStatus,
    'selectableWhenFiltering',
  );
  readonly classificationOptions = this.configuration.options(
    CONFIGURATION_CATEGORY.findingClassification,
  );
  readonly sourceOptions = this.configuration.options(CONFIGURATION_CATEGORY.findingSource);
  readonly severityOptions = this.configuration.options(CONFIGURATION_CATEGORY.findingSeverity);
  readonly departmentOptions = this.configuration.options(CONFIGURATION_CATEGORY.department);
  readonly promotionReadinessOptions = this.configuration.options(
    CONFIGURATION_CATEGORY.promotionReadiness,
  );
  readonly quickFilters = this.configuration.options(CONFIGURATION_CATEGORY.findingQuickFilter);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly dashboard = signal<FindingsWorkspaceDashboard | null>(null);
  readonly items = signal<FindingsWorkspaceItem[]>([]);
  readonly groupedItems = signal<{ key: string; label: string; items: FindingsWorkspaceItem[] }[]>(
    [],
  );
  readonly selectedId = signal<string | null>(null);
  readonly selectedDetail = signal<FindingsWorkspaceDetail | null>(null);
  readonly promotionStatus = signal<FindingPromotionStatus | null>(null);
  readonly routingSuggestion = signal<AssignmentRoutingSuggestion | null>(null);
  readonly promotionLoading = signal(false);
  readonly promotionSaving = signal(false);
  readonly statusSaving = signal(false);
  readonly mappingSaving = signal(false);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly openAdd = signal(false);

  searchInput = '';
  filterStatus = '';
  filterSeverity = '';
  filterClassification = '';
  filterSource = '';
  filterDepartment = '';
  filterPromotionReadiness = '';
  filterChapterCode = '';
  filterStandardCode = '';
  filterQuick = '';
  groupBy = '';
  sortBy = 'updated';
  sortDesc = true;

  ngOnInit(): void {
    // Declares the workspace subject *and* the app-wide assistant context in one
    // call — this replaces the direct AiPageContextService use that used to live
    // here (Stage 1.1, Step 12: no duplicate AI initialization).
    this.publishParticipation();

    const params = this.route.snapshot.queryParamMap;
    if (params.get('search')) this.searchInput = params.get('search')!;
    const findingId = params.get('findingId');
    if (findingId) this.openDetail(findingId);

    this.search$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.page.set(1);
        this.reload();
      });

    this.filterText$.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => {
      this.page.set(1);
      this.reload();
    });

    this.route.queryParamMap.pipe(takeUntil(this.destroy$)).subscribe((qp) => {
      const s = qp.get('search');
      if (s != null && s !== this.searchInput) {
        this.searchInput = s;
        this.page.set(1);
        this.reload();
      }
      const fid = qp.get('findingId');
      if (fid) this.openDetail(fid);
    });

    this.reload();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Findings' declaration.
   *
   * Re-published on every reload and selection change so the panel tracks the
   * selected finding rather than the list as a whole. `Create CAPA` is offered
   * only with a finding selected — promoting nothing is not a meaningful action.
   */
  private publishParticipation(): void {
    const selected = this.selectedDetail();
    const dash = this.dashboard();

    this.auditWorkspace.set({
      stage: 'findings',
      subject: selected
        ? { kind: 'finding', id: selected.id, title: selected.title ?? selected.findingNumber }
        : { kind: 'finding', title: 'Findings workspace' },
      actions: [
        {
          id: 'findings.create',
          label: 'Create finding',
          variant: 'primary',
          run: () => this.openAdd.set(true),
        },
        {
          id: 'findings.export',
          label: 'Export CSV',
          run: () => this.exportCsv(),
        },
        {
          id: 'findings.create-capa',
          label: 'Create CAPA',
          disabled: !selected || selected.isPromoted,
          disabledReason: selected?.isPromoted
            ? 'Already promoted to CAPA'
            : 'Select a finding to promote it to CAPA',
          run: () => this.promoteFinding(),
        },
      ],
      workflowState: {
        stage: 'findings',
        statusLabel: selected?.status ?? `${this.totalCount()} findings`,
        completionCriteria: 'Every open finding is promoted to CAPA or closed',
      },
      relatedRecords: this.items()
        .slice(0, 8)
        .map((f) => ({
          id: f.id,
          label: f.title ?? f.findingNumber,
          kind: 'Finding',
          status: f.status,
        })),
      aiContext: {
        department: this.filterDepartment || null,
        facts: {
          totalFindings: String(dash?.totalFindings ?? this.totalCount()),
          openFindings: String(dash?.openFindings ?? 0),
          criticalFindings: String(dash?.criticalFindings ?? 0),
          selectedFinding: selected?.findingNumber ?? 'none',
        },
      },
    });
  }

  onSearchChange(value: string): void {
    this.search$.next(value);
  }

  onFilterTextChange(): void {
    this.filterText$.next();
  }

  toggleQuickFilter(key: string): void {
    this.filterQuick = this.filterQuick === key ? '' : key;
    this.page.set(1);
    this.reload();
  }

  onSort(column: string): void {
    if (this.sortBy === column) this.sortDesc = !this.sortDesc;
    else {
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
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      dashboard: this.workspace.getDashboard(),
      workspace: this.workspace.getWorkspace({
        status: this.filterStatus || undefined,
        severity: this.filterSeverity || undefined,
        classification: this.filterClassification || undefined,
        source: this.filterSource || undefined,
        department: this.filterDepartment || undefined,
        promotionReadiness: this.filterPromotionReadiness || undefined,
        chapterCode: this.filterChapterCode.trim() || undefined,
        standardCode: this.filterStandardCode.trim() || undefined,
        quickFilter: this.filterQuick || undefined,
        search: this.searchInput.trim() || undefined,
        sortBy: this.sortBy,
        sortDesc: this.sortDesc,
        page: this.page(),
        pageSize: this.pageSize(),
      }),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ dashboard, workspace }) => {
          this.dashboard.set(dashboard);
          this.items.set(workspace.items);
          this.totalCount.set(workspace.totalCount);
          this.applyGrouping();
          this.publishParticipation();
        },
        error: () => this.error.set('Failed to load findings workbench.'),
      });
  }

  openDetail(id: string): void {
    this.selectedId.set(id);
    this.promotionStatus.set(null);
    this.routingSuggestion.set(null);
    this.promotionLoading.set(true);
    forkJoin({
      detail: this.workspace.getDetail(id),
      promotion: this.workspace.getPromotionStatus(id),
    }).subscribe({
      next: ({ detail, promotion }) => {
        this.selectedDetail.set(detail);
        this.promotionStatus.set(promotion);
        this.promotionLoading.set(false);
        this.loadRoutingSuggestion(id);
        // Panel and toolbar now describe the opened finding, not the list.
        this.publishParticipation();
      },
      error: () => {
        this.promotionLoading.set(false);
        this.error.set('Failed to load finding detail.');
      },
    });
  }

  closeDetail(): void {
    this.selectedId.set(null);
    this.selectedDetail.set(null);
    this.promotionStatus.set(null);
    this.routingSuggestion.set(null);
    this.publishParticipation();
  }

  private loadRoutingSuggestion(findingId: string): void {
    const organizationId = this.context.getOrganizationId();
    const hospitalId = this.context.getHospitalId();
    if (!organizationId || !hospitalId) {
      return;
    }

    this.workspace
      .getAssignmentSuggestion(findingId, organizationId, hospitalId)
      .pipe(catchError(() => of(null)))
      .subscribe((suggestion) => this.routingSuggestion.set(suggestion));
  }

  promoteFinding(): void {
    const id = this.selectedId();
    if (!id || this.promotionSaving()) return;
    this.promotionSaving.set(true);
    this.error.set(null);
    this.workspace.promoteToDeficiency(id).subscribe({
      next: () => {
        this.promotionSaving.set(false);
        this.openDetail(id);
        this.reload();
      },
      error: (err) => {
        this.promotionSaving.set(false);
        this.error.set(
          err?.error?.errors?.[0]?.message ?? err?.error?.message ?? 'Failed to promote finding.',
        );
        this.workspace.getPromotionStatus(id).subscribe({
          next: (status) => this.promotionStatus.set(status),
        });
      },
    });
  }

  changeStatus(status: string): void {
    const id = this.selectedId();
    if (!id) return;
    this.statusSaving.set(true);
    this.workspace.patchStatus(id, toFindingWireEnum(status)).subscribe({
      next: () => {
        this.statusSaving.set(false);
        this.openDetail(id);
        this.reload();
      },
      error: () => {
        this.statusSaving.set(false);
        this.error.set('Failed to update finding status.');
      },
    });
  }

  /**
   * DEF-M3-001 — classifies the open finding. Re-opening the detail refreshes the promotion
   * blockers, so the reviewer sees the mapping prerequisites clear as soon as they are satisfied.
   */
  mapComplianceContext(objectiveElementId: string): void {
    const id = this.selectedId();
    if (!id) return;
    this.mappingSaving.set(true);
    this.workspace.patchComplianceContext(id, objectiveElementId).subscribe({
      next: () => {
        this.mappingSaving.set(false);
        this.openDetail(id);
        this.reload();
      },
      error: () => {
        this.mappingSaving.set(false);
        this.error.set('Failed to update compliance mapping.');
      },
    });
  }

  addComment(body: string): void {
    const id = this.selectedId();
    if (!id) return;
    this.workspace.addComment(id, body).subscribe({
      next: () => this.openDetail(id),
      error: () => this.error.set('Failed to add comment.'),
    });
  }

  addAttachment(payload: { fileName: string; fileUrl: string; attachmentType: string }): void {
    const id = this.selectedId();
    if (!id) return;
    this.workspace.addAttachment(id, payload).subscribe({
      next: () => this.openDetail(id),
      error: () => this.error.set('Failed to add attachment.'),
    });
  }

  onFindingCreated(): void {
    this.openAdd.set(false);
    this.reload();
  }

  exportCsv(): void {
    exportFindingsCsv(this.items());
  }

  applyGrouping(): void {
    const rows = this.items();
    if (!this.groupBy) {
      this.groupedItems.set([]);
      return;
    }
    // The grouping key is the row's raw code; its heading is the configured label for whichever
    // category that code belongs to. Headings previously showed the code itself, so a group of
    // findings read "InProgress" while the same value read "In Progress" everywhere else.
    const category =
      this.groupBy === 'source'
        ? CONFIGURATION_CATEGORY.findingSource
        : this.groupBy === 'severity'
          ? CONFIGURATION_CATEGORY.findingSeverity
          : CONFIGURATION_CATEGORY.findingStatus;

    const map = new Map<string, FindingsWorkspaceItem[]>();
    for (const row of rows) {
      const key =
        this.groupBy === 'source'
          ? row.source
          : this.groupBy === 'severity'
            ? row.severity
            : row.status;
      const list = map.get(key) ?? [];
      list.push(row);
      map.set(key, list);
    }
    this.groupedItems.set(
      [...map.entries()].map(([key, groupItems]) => ({
        key,
        label: this.configuration.label(category, key, key),
        items: groupItems,
      })),
    );
  }
}
