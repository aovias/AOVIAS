import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideDownload,
  lucideFilterX,
  lucideInbox,
  lucideRefreshCw,
  lucideSearch,
  lucideTriangleAlert,
} from '@ng-icons/lucide';
import { Subject, Subscription, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { SessionService } from '../../../core/auth/session.service';
import { IdentityAdminApiService } from '../../settings/identity-admin/services/identity-admin-api.service';
import type { IdentityUserListItem } from '../../settings/identity-admin/models/identity-user-admin.model';
import {
  HospitalFrameworkOption,
  OeOwnershipApiService,
  OeOwnershipMatrixItem,
  UpsertOeOwnershipRequest,
} from '../services/oe-ownership-api.service';
import { OrganizationApiService } from '../services/organization-api.service';
import {
  PageBreadcrumbComponent,
  type PageBreadcrumbItem,
} from '../../../shared/ui/page-breadcrumb.component';
import { ConcurrentSaveQueue } from './oe-ownership-save-queue';
import { OeOwnershipMatrixRowComponent } from './oe-ownership-matrix-row.component';
import {
  FlatDepartment,
  OE_OWNERSHIP_COLUMNS,
  OeActivityState,
  OeDisplayStatus,
  ROW_HEIGHT_PX,
  SAVE_CONCURRENCY,
  SAVE_DEBOUNCE_MS,
  VIRTUAL_SCROLL_THRESHOLD,
  buildOwnershipCsv,
  computeDisplayStatus,
  computeKpis,
  flattenDepartments,
  formatDueDisplay,
  highlightMatch,
  isAssigned,
  loadRecentDepartmentIds,
  pushRecentDepartmentId,
  relativeSavedLabel,
  toDateInputValue,
} from './oe-ownership-matrix.utils';

interface RowDraft {
  departmentId: string;
  ownerUserId: string;
  dueDate: Date | null;
}

interface RowUi {
  activity: OeActivityState;
  dirty: boolean;
  savedAt?: number;
  errorMessage?: string;
  lastPayload?: UpsertOeOwnershipRequest;
  showAllUsers?: boolean;
  deptSearch?: string;
  ownerSearch?: string;
}

type BulkMode = null | 'department' | 'owner' | 'dueDate';

@Component({
  selector: 'app-oe-ownership-matrix',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    ScrollingModule,
    PageBreadcrumbComponent,
    NgIcon,
    MatAutocompleteModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    OeOwnershipMatrixRowComponent,
  ],
  viewProviders: [
    provideIcons({
      lucideDownload,
      lucideRefreshCw,
      lucideFilterX,
      lucideInbox,
      lucideSearch,
      lucideTriangleAlert,
    }),
  ],
  template: `
    <div class="aov-page oe-matrix-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header oe-matrix__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">OE Ownership Matrix</h1>
          <div class="oe-matrix__context">
            <div class="oe-matrix__context-item">
              <span class="aov-text-micro">Hospital</span>
              <span class="oe-matrix__context-value">{{ hospitalName() || '—' }}</span>
            </div>
            <div class="oe-matrix__context-item oe-matrix__fw">
              <label class="aov-text-micro" id="fw-label" for="fw-input">Framework</label>
              <input
                id="fw-input"
                class="aov-input"
                [ngModel]="frameworkSearch()"
                (ngModelChange)="onFrameworkSearch($event)"
                [matAutocomplete]="fwAuto"
                placeholder="Select framework"
                aria-labelledby="fw-label"
              />
              <mat-autocomplete
                #fwAuto="matAutocomplete"
                [displayWith]="displayFramework"
                (optionSelected)="selectFramework($event.option.value)"
              >
                @for (fw of filteredFrameworks(); track fw.frameworkId) {
                  <mat-option [value]="fw">{{ fw.frameworkName }}</mat-option>
                }
              </mat-autocomplete>
            </div>
          </div>
        </div>
        <div class="aov-page__actions">
          <button
            type="button"
            class="aov-btn aov-btn--secondary aov-btn--sm"
            (click)="exportCsv()"
            [disabled]="!filteredRows().length"
          >
            <ng-icon name="lucideDownload" aria-hidden="true" />
            Export
          </button>
          <button
            type="button"
            class="aov-btn aov-btn--secondary aov-btn--sm"
            (click)="refresh()"
            [disabled]="!selectedFrameworkId() || loading()"
            [class.aov-btn--loading]="loading()"
          >
            <ng-icon name="lucideRefreshCw" aria-hidden="true" />
            Refresh
          </button>
          <button type="button" class="aov-btn aov-btn--secondary aov-btn--sm" (click)="resetFilters()">
            <ng-icon name="lucideFilterX" aria-hidden="true" />
            Reset Filters
          </button>
        </div>
      </header>

      @if (error() && !selectedFrameworkId()) {
        <p class="aov-alert aov-alert--danger" role="alert">
          <ng-icon name="lucideTriangleAlert" aria-hidden="true" />
          {{ error() }}
        </p>
      }

      @if (!selectedFrameworkId()) {
        <div class="aov-empty">
          <ng-icon name="lucideInbox" class="aov-empty__icon" aria-hidden="true" />
          <p class="aov-empty__title">No Framework</p>
          <p class="aov-empty__message">Select a framework to begin assigning ownership.</p>
        </div>
      } @else {
        <div class="oe-matrix__sticky">
          <div class="oe-matrix__kpi-strip" aria-label="Assignment progress" aria-busy="{{ loading() }}">
            @if (loading()) {
              <span class="aov-text-caption">Loading matrix…</span>
            } @else {
              <span
                ><strong class="aov-num">{{ kpis().total }}</strong> OEs</span
              >
              <span
                ><strong class="aov-num">{{ kpis().assigned }}</strong> Assigned</span
              >
              <span
                ><strong class="aov-num">{{ kpis().pending }}</strong> Pending</span
              >
              <span
                ><strong class="aov-num">{{ kpis().overdue }}</strong> Overdue</span
              >
              <span
                ><strong class="aov-num">{{ kpis().completionPct }}%</strong></span
              >
              <div class="aov-progress oe-matrix__kpi-progress" role="progressbar" [attr.aria-valuenow]="kpis().completionPct">
                <div class="aov-progress__fill" [style.inline-size.%]="kpis().completionPct"></div>
              </div>
            }
          </div>

          <div class="oe-matrix__chips" role="group" aria-label="Quick filters">
            <button
              type="button"
              class="aov-chip aov-chip--selectable"
              [class.is-selected]="chipUnassigned()"
              [attr.aria-pressed]="chipUnassigned()"
              (click)="chipUnassigned.set(!chipUnassigned())"
            >
              Unassigned
            </button>
            <button
              type="button"
              class="aov-chip aov-chip--selectable"
              [class.is-selected]="chipOverdue()"
              [attr.aria-pressed]="chipOverdue()"
              (click)="chipOverdue.set(!chipOverdue())"
            >
              Overdue
            </button>
            <button
              type="button"
              class="aov-chip aov-chip--selectable"
              [class.is-selected]="chipAssignedToMe()"
              [attr.aria-pressed]="chipAssignedToMe()"
              (click)="chipAssignedToMe.set(!chipAssignedToMe())"
            >
              Assigned to me
            </button>
          </div>

          <div class="aov-toolbar oe-matrix__toolbar" role="search">
            <select
              class="aov-select oe-matrix__flex-2"
              [ngModel]="filterDepartmentId()"
              (ngModelChange)="filterDepartmentId.set($event)"
              aria-label="Assigned Department"
            >
              <option value="">All departments</option>
              @for (d of departments(); track d.id) {
                <option [value]="d.id">{{ d.name }}</option>
              }
            </select>
            <select
              class="aov-select oe-matrix__flex-2"
              [ngModel]="filterOwnerId()"
              (ngModelChange)="filterOwnerId.set($event)"
              aria-label="Owner"
            >
              <option value="">All owners</option>
              @for (u of filterOwnerOptions(); track u.userId) {
                <option [value]="u.userId">{{ u.fullName }}</option>
              }
            </select>
            <select
              class="aov-select oe-matrix__flex-1"
              [ngModel]="filterStatus()"
              (ngModelChange)="filterStatus.set($event)"
              aria-label="Status"
            >
              <option value="">All statuses</option>
              <option value="Unassigned">Unassigned</option>
              <option value="Assigned">Assigned</option>
              <option value="Due Soon">Due Soon</option>
              <option value="Overdue">Overdue</option>
            </select>
            <div class="aov-search oe-matrix__flex-3">
              <ng-icon name="lucideSearch" class="aov-search__icon" aria-hidden="true" />
              <input
                class="aov-input"
                type="search"
                [ngModel]="searchText()"
                (ngModelChange)="searchText.set($event)"
                placeholder="Search OE"
                aria-label="Search Objective Element"
              />
            </div>
          </div>
        </div>

        @if (selectedCount() > 0) {
          <div class="aov-toolbar aov-toolbar--plain oe-matrix__bulk" role="toolbar" aria-label="Bulk actions">
            <strong>{{ selectedCount() }} Objective Elements Selected</strong>
            <span class="aov-text-micro">Actions</span>
            <button type="button" class="aov-btn aov-btn--secondary aov-btn--sm" (click)="openBulk('department')">
              Department
            </button>
            <button type="button" class="aov-btn aov-btn--secondary aov-btn--sm" (click)="openBulk('owner')">
              Owner
            </button>
            <button type="button" class="aov-btn aov-btn--secondary aov-btn--sm" (click)="openBulk('dueDate')">
              Due Date
            </button>
            <button type="button" class="aov-btn aov-btn--danger aov-btn--sm" (click)="confirmClear()">
              Clear Assignment
            </button>
            <button type="button" class="aov-btn aov-btn--text aov-btn--sm" (click)="clearSelection()">
              Cancel Selection
            </button>
          </div>
        }

        @if (bulkMode()) {
          <div class="aov-toolbar aov-toolbar--plain oe-matrix__bulk-panel">
            @if (bulkMode() === 'department') {
              <input
                class="aov-input"
                [(ngModel)]="bulkDeptSearch"
                [matAutocomplete]="bulkDeptAuto"
                placeholder="Search department"
                aria-label="Assign Department"
              />
              <mat-autocomplete
                #bulkDeptAuto="matAutocomplete"
                [displayWith]="displayDept"
                (optionSelected)="bulkDept = $event.option.value"
              >
                <cdk-virtual-scroll-viewport class="oe-matrix__ac" [itemSize]="40" style="height: 240px">
                  <mat-option *cdkVirtualFor="let d of filteredBulkDepts(); trackBy: trackDept" [value]="d">
                    <span [innerHTML]="deptOptionHtml(d, bulkDeptSearch)"></span>
                  </mat-option>
                </cdk-virtual-scroll-viewport>
              </mat-autocomplete>
              <button type="button" class="aov-btn aov-btn--primary aov-btn--sm" (click)="applyBulkDepartment()">
                Apply
              </button>
            }
            @if (bulkMode() === 'owner') {
              <input
                class="aov-input"
                [(ngModel)]="bulkOwnerSearch"
                (ngModelChange)="onBulkOwnerSearch($event)"
                [matAutocomplete]="bulkOwnerAuto"
                placeholder="Name, EMP, or email"
                aria-label="Assign Owner"
              />
              <mat-autocomplete
                #bulkOwnerAuto="matAutocomplete"
                [displayWith]="displayOwner"
                (optionSelected)="bulkOwner = $event.option.value"
              >
                @for (u of bulkOwnerResults(); track u.userId) {
                  <mat-option [value]="u">
                    <div class="oe-matrix__person">
                      <span class="oe-matrix__person-name">{{ u.fullName }}</span>
                      <span class="aov-text-caption">{{ u.designation || u.role || '—' }}</span>
                      <span class="aov-text-caption">{{ personMeta(u) }}</span>
                    </div>
                  </mat-option>
                }
              </mat-autocomplete>
              <button type="button" class="aov-btn aov-btn--primary aov-btn--sm" (click)="applyBulkOwner()">
                Apply
              </button>
            }
            @if (bulkMode() === 'dueDate') {
              <input class="aov-input" [matDatepicker]="bulkPicker" [(ngModel)]="bulkDue" aria-label="Due Date" />
              <mat-datepicker-toggle [for]="bulkPicker" />
              <mat-datepicker #bulkPicker />
              <button type="button" class="aov-btn aov-btn--secondary aov-btn--sm" (click)="bulkDue = null; applyBulkDue()">
                No Due Date
              </button>
              <button type="button" class="aov-btn aov-btn--primary aov-btn--sm" (click)="applyBulkDue()">Apply</button>
            }
            <button type="button" class="aov-btn aov-btn--text aov-btn--sm" (click)="bulkMode.set(null)">Cancel</button>
          </div>
        }

        @if (error() && !loading()) {
          <p class="aov-alert aov-alert--danger" role="alert">
            <ng-icon name="lucideTriangleAlert" aria-hidden="true" />
            {{ error() }}
            <button type="button" class="aov-btn aov-btn--secondary aov-btn--sm" (click)="refresh()">Retry</button>
          </p>
        }

        @if (loading()) {
          <div class="aov-table-wrap oe-matrix__grid" aria-busy="true" aria-label="Loading objective elements">
            @for (i of skeletonSlots; track i) {
              <div class="aov-skeleton oe-matrix__skel"></div>
            }
          </div>
        } @else if (!rows().length && !error()) {
          <div class="aov-empty">
            <ng-icon name="lucideInbox" class="aov-empty__icon" aria-hidden="true" />
            <p class="aov-empty__title">No Objective Elements</p>
            <p class="aov-empty__message">This framework contains no objective elements.</p>
          </div>
        } @else if (!filteredRows().length && !error()) {
          <div class="aov-empty">
            <ng-icon name="lucideInbox" class="aov-empty__icon" aria-hidden="true" />
            <p class="aov-empty__title">No Search Results</p>
            <p class="aov-empty__message">No objective elements match your filters.</p>
          </div>
        } @else if (rows().length) {
          <div class="aov-table-wrap oe-matrix__grid">
            <div class="oe-matrix__head" role="row">
              <div class="oe-matrix__th oe-matrix__th--check">
                <mat-checkbox
                  [checked]="allFilteredSelected()"
                  [indeterminate]="someFilteredSelected() && !allFilteredSelected()"
                  (change)="toggleSelectAll($event.checked)"
                  aria-label="Select all visible objective elements"
                />
              </div>
              @for (col of visibleColumns(); track col.id) {
                @if (col.id !== 'select') {
                  <div class="oe-matrix__th" [attr.data-col]="col.id">{{ col.header }}</div>
                }
              }
            </div>

            @if (useVirtualScroll()) {
              <cdk-virtual-scroll-viewport class="oe-matrix__viewport" [itemSize]="rowHeight">
                <app-oe-ownership-matrix-row
                  *cdkVirtualFor="let row of filteredRows(); trackBy: trackRow"
                  [row]="row"
                  [selected]="isSelected(row.objectiveElementId)"
                  [dirty]="uiFor(row).dirty"
                  [activity]="uiFor(row).activity"
                  [status]="displayStatus(row)"
                  [deptLabel]="deptInputLabel(row)"
                  [ownerLabel]="ownerInputLabel(row)"
                  [dueDate]="draftDue(row)"
                  [duePlaceholder]="formatDue(row)"
                  [deptOptions]="deptOptionsFor(row)"
                  [ownerOptions]="ownerOptionsFor(row)"
                  [showAllUsers]="!!uiFor(row).showAllUsers"
                  [savedTooltip]="savedTooltip(row)"
                  [errorMessage]="uiFor(row).errorMessage"
                  [displayDeptFn]="displayDept"
                  [displayOwnerFn]="displayOwner"
                  [deptOptionHtml]="deptHtmlFn(row)"
                  [personMeta]="personMetaFn"
                  (selectChange)="toggleSelect(row.objectiveElementId, $event)"
                  (deptSearch)="onDeptSearch(row, $event)"
                  (deptSelected)="onDeptSelected(row, $event)"
                  (ownerSearch)="onOwnerSearch(row, $event)"
                  (ownerSelected)="onOwnerSelected(row, $event)"
                  (toggleShowAll)="toggleShowAllUsers(row)"
                  (dueChange)="onDueChanged(row, $event)"
                  (retry)="retrySave(row)"
                />
              </cdk-virtual-scroll-viewport>
            } @else {
              @for (row of filteredRows(); track row.objectiveElementId) {
                <app-oe-ownership-matrix-row
                  [row]="row"
                  [selected]="isSelected(row.objectiveElementId)"
                  [dirty]="uiFor(row).dirty"
                  [activity]="uiFor(row).activity"
                  [status]="displayStatus(row)"
                  [deptLabel]="deptInputLabel(row)"
                  [ownerLabel]="ownerInputLabel(row)"
                  [dueDate]="draftDue(row)"
                  [duePlaceholder]="formatDue(row)"
                  [deptOptions]="deptOptionsFor(row)"
                  [ownerOptions]="ownerOptionsFor(row)"
                  [showAllUsers]="!!uiFor(row).showAllUsers"
                  [savedTooltip]="savedTooltip(row)"
                  [errorMessage]="uiFor(row).errorMessage"
                  [displayDeptFn]="displayDept"
                  [displayOwnerFn]="displayOwner"
                  [deptOptionHtml]="deptHtmlFn(row)"
                  [personMeta]="personMetaFn"
                  (selectChange)="toggleSelect(row.objectiveElementId, $event)"
                  (deptSearch)="onDeptSearch(row, $event)"
                  (deptSelected)="onDeptSelected(row, $event)"
                  (ownerSearch)="onOwnerSearch(row, $event)"
                  (ownerSelected)="onOwnerSelected(row, $event)"
                  (toggleShowAll)="toggleShowAllUsers(row)"
                  (dueChange)="onDueChanged(row, $event)"
                  (retry)="retrySave(row)"
                />
              }
            }
          </div>
        }
      }
    </div>
  `,
  styles: [
    `
      .oe-matrix-page {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        padding-block-end: var(--space-3);
      }
      .oe-matrix__header {
        margin-block-end: 0;
      }
      .oe-matrix__context {
        display: flex;
        flex-wrap: wrap;
        align-items: end;
        gap: var(--space-4);
        margin-block-start: var(--space-2);
      }
      .oe-matrix__context-item {
        display: grid;
        gap: var(--space-1);
      }
      .oe-matrix__context-value {
        font-size: var(--font-size-body);
        font-weight: var(--font-weight-semibold);
        color: var(--text-primary);
      }
      .oe-matrix__fw {
        min-inline-size: min(18rem, 100%);
      }
      .oe-matrix__fw .aov-input {
        block-size: var(--control-height-sm);
        min-block-size: var(--control-height-sm);
      }
      .oe-matrix__sticky {
        position: sticky;
        inset-block-start: 0;
        z-index: calc(var(--z-base, 1) + 2);
        background: var(--color-bg-page, var(--bg-primary));
        padding-block: var(--space-2);
        display: grid;
        gap: var(--space-2);
        border-block-end: var(--border-width-hairline) solid var(--border-color);
      }
      .oe-matrix__kpi-strip {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--space-3);
        min-block-size: 2.75rem;
        max-block-size: 5.5rem;
        font-size: var(--font-size-body-sm);
        color: var(--text-secondary);
      }
      .oe-matrix__kpi-progress {
        flex: 1 1 8rem;
        max-inline-size: 14rem;
      }
      .oe-matrix__chips {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-2);
      }
      .oe-matrix__toolbar {
        display: flex;
        flex-wrap: nowrap;
        gap: var(--space-2);
        align-items: center;
      }
      .oe-matrix__flex-1 {
        flex: 1 1 0;
        min-inline-size: 0;
      }
      .oe-matrix__flex-2 {
        flex: 2 1 0;
        min-inline-size: 0;
      }
      .oe-matrix__flex-3 {
        flex: 3 1 0;
        min-inline-size: 0;
      }
      .oe-matrix__bulk,
      .oe-matrix__bulk-panel {
        margin-block-start: var(--space-2);
      }
      .oe-matrix__person {
        display: grid;
        line-height: var(--line-height-normal);
      }
      .oe-matrix__person-name {
        font-weight: var(--font-weight-semibold);
      }
      .oe-matrix__ac {
        inline-size: 100%;
      }
      .oe-matrix__skel {
        block-size: var(--control-height-md, 2.5rem);
        margin: var(--space-2);
      }
      .oe-matrix__grid {
        margin-block-start: var(--space-2);
        flex: 1 1 auto;
      }
      .oe-matrix__head {
        display: grid;
        grid-template-columns: 2.5rem 5.5rem minmax(10rem, 1.4fr) minmax(9rem, 1fr) minmax(10rem, 1.1fr) 8.25rem 6.875rem 3.5rem;
        align-items: center;
        position: sticky;
        inset-block-start: 0;
        z-index: var(--z-base, 1);
        background: var(--bg-tertiary);
        border-block-end: var(--border-width-hairline) solid var(--border-color);
        min-block-size: var(--table-header-min-height, 2.5rem);
      }
      .oe-matrix__th {
        padding: var(--space-2) var(--table-cell-padding-inline, var(--space-3));
        color: var(--text-secondary);
        font-size: var(--table-header-font-size, var(--font-size-micro));
        font-weight: var(--font-weight-semibold);
        letter-spacing: var(--letter-spacing-wide);
        text-transform: uppercase;
        white-space: nowrap;
        min-inline-size: 0;
      }
      .oe-matrix__viewport {
        block-size: min(70vh, 48rem);
        inline-size: 100%;
      }
      :host ::ng-deep .oe-hl {
        background: var(--color-status-warning-soft);
        color: inherit;
        padding: 0;
      }
      @media (max-width: 900px) {
        .oe-matrix__toolbar {
          flex-wrap: wrap;
        }
        .oe-matrix__flex-1,
        .oe-matrix__flex-2,
        .oe-matrix__flex-3 {
          flex: 1 1 100%;
        }
      }
    `,
  ],
})

export class OeOwnershipMatrixComponent implements OnDestroy {
  private readonly api = inject(OeOwnershipApiService);
  private readonly orgApi = inject(OrganizationApiService);
  private readonly identityApi = inject(IdentityAdminApiService);
  private readonly session = inject(SessionService);
  private readonly dialog = inject(MatDialog);

  readonly crumbs: PageBreadcrumbItem[] = [
    { label: 'Organization', route: '/app/organization/overview' },
    { label: 'OE Ownership' },
  ];

  readonly rowHeight = ROW_HEIGHT_PX;
  readonly skeletonSlots = [1, 2, 3, 4, 5, 6, 7, 8];
  readonly visibleColumns = computed(() => OE_OWNERSHIP_COLUMNS.filter((c) => c.enabled));

  readonly hospitalId = signal(this.session.getHospitalId() || '');
  readonly hospitalName = signal('');
  readonly frameworks = signal<HospitalFrameworkOption[]>([]);
  readonly frameworkSearch = signal('');
  readonly selectedFrameworkId = signal('');
  readonly selectedFrameworkName = signal('');

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<OeOwnershipMatrixItem[]>([]);
  readonly departments = signal<FlatDepartment[]>([]);
  readonly userCache = signal<Record<string, IdentityUserListItem>>({});

  readonly searchText = signal('');
  readonly filterDepartmentId = signal('');
  readonly filterOwnerId = signal('');
  readonly filterStatus = signal('');
  readonly chipUnassigned = signal(false);
  readonly chipOverdue = signal(false);
  readonly chipAssignedToMe = signal(false);

  readonly selection = signal<Set<string>>(new Set());
  readonly bulkMode = signal<BulkMode>(null);
  readonly bulkOwnerResults = signal<IdentityUserListItem[]>([]);

  bulkDeptSearch = '';
  bulkDept: FlatDepartment | null = null;
  bulkOwnerSearch = '';
  bulkOwner: IdentityUserListItem | null = null;
  bulkDue: Date | null = null;

  private readonly drafts = new Map<string, RowDraft>();
  private readonly draftRevision = signal(0);
  private readonly ui = signal<Record<string, RowUi>>({});
  private readonly ownerResults = signal<Record<string, IdentityUserListItem[]>>({});
  private readonly saveQueue = new ConcurrentSaveQueue(SAVE_CONCURRENCY);
  private readonly debounceTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private readonly ownerSearch$ = new Subject<{ oeId: string; term: string; departmentId?: string; showAll?: boolean }>();
  private readonly bulkOwnerSearch$ = new Subject<string>();
  private subs = new Subscription();
  private recentDeptIds: string[] = [];

  readonly filteredFrameworks = computed(() => {
    const q = this.frameworkSearch().trim().toLowerCase();
    const list = this.frameworks();
    if (!q) return list;
    return list.filter((f) => f.frameworkName.toLowerCase().includes(q));
  });

  readonly kpis = computed(() => {
    if (this.loading()) {
      return { total: 0, assigned: 0, pending: 0, overdue: 0, completionPct: 0 };
    }
    return computeKpis(this.rows());
  });

  readonly filteredRows = computed(() => {
    this.draftRevision();
    const q = this.searchText().trim().toLowerCase();
    const dept = this.filterDepartmentId();
    const owner = this.filterOwnerId();
    const status = this.filterStatus();
    const onlyUnassigned = this.chipUnassigned();
    const onlyOverdue = this.chipOverdue();
    const onlyMe = this.chipAssignedToMe();
    const me = this.session.getUserId();

    return this.rows().filter((row) => {
      const draft = this.effectiveRow(row);
      const ds = computeDisplayStatus(draft);
      if (q) {
        const hay = `${row.objectiveElementCode} ${row.objectiveElementTitle}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      if (dept && draft.departmentId !== dept) return false;
      if (owner && draft.primaryOwnerUserId !== owner) return false;
      if (status && ds !== status) return false;
      if (onlyUnassigned && ds !== 'Unassigned') return false;
      if (onlyOverdue && ds !== 'Overdue') return false;
      if (onlyMe && draft.primaryOwnerUserId !== me) return false;
      return true;
    });
  });

  readonly useVirtualScroll = computed(() => this.filteredRows().length >= VIRTUAL_SCROLL_THRESHOLD);
  readonly selectedCount = computed(() => this.selection().size);

  readonly filterOwnerOptions = computed(() => {
    const cache = this.userCache();
    const ids = new Set(
      this.rows()
        .map((r) => r.primaryOwnerUserId)
        .filter((x): x is string => !!x),
    );
    return [...ids].map((id) => cache[id]).filter(Boolean) as IdentityUserListItem[];
  });

  constructor() {
    this.bootstrap();
    this.subs.add(
      this.ownerSearch$
        .pipe(
          debounceTime(250),
          distinctUntilChanged((a, b) => a.oeId === b.oeId && a.term === b.term && a.departmentId === b.departmentId && a.showAll === b.showAll),
          switchMap(({ oeId, term, departmentId, showAll }) =>
            this.identityApi
              .listUsers({
                search: term || undefined,
                departmentId: showAll ? undefined : departmentId || undefined,
                page: 1,
                pageSize: 25,
              })
              .pipe(
                switchMap((page) => {
                  this.mergeUsers(page.items);
                  this.ownerResults.update((m) => ({ ...m, [oeId]: page.items }));
                  return of(null);
                }),
              ),
          ),
        )
        .subscribe(),
    );
    this.subs.add(
      this.bulkOwnerSearch$
        .pipe(
          debounceTime(250),
          switchMap((term) =>
            this.identityApi.listUsers({ search: term || undefined, page: 1, pageSize: 25 }).pipe(
              switchMap((page) => {
                this.mergeUsers(page.items);
                this.bulkOwnerResults.set(page.items);
                return of(null);
              }),
            ),
          ),
        )
        .subscribe(),
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.saveQueue.destroy();
    for (const t of this.debounceTimers.values()) clearTimeout(t);
  }

  displayFramework = (fw: HospitalFrameworkOption | string | null): string => {
    if (!fw) return '';
    if (typeof fw === 'string') return fw;
    return fw.frameworkName || '';
  };

  displayDept = (d: FlatDepartment | string | null): string => {
    if (!d) return '';
    if (typeof d === 'string') {
      const found = this.departments().find((x) => x.id === d);
      return found ? this.deptLabel(found) : '';
    }
    return this.deptLabel(d);
  };

  displayOwner = (u: IdentityUserListItem | string | null): string => {
    if (!u) return '';
    if (typeof u === 'string') return this.userCache()[u]?.fullName || '';
    return u.fullName || '';
  };

  trackRow = (_: number, row: OeOwnershipMatrixItem) => row.objectiveElementId;
  trackDept = (_: number, d: FlatDepartment) => d.id;

  readonly personMetaFn = (u: IdentityUserListItem) => this.personMeta(u);

  deptHtmlFn(row: OeOwnershipMatrixItem): (d: FlatDepartment) => string {
    return (d) => this.deptOptionHtml(d, this.uiFor(row).deptSearch || '');
  }

  onFrameworkSearch(value: string | HospitalFrameworkOption): void {
    if (typeof value === 'string') {
      this.frameworkSearch.set(value);
    } else if (value?.frameworkName) {
      this.frameworkSearch.set(value.frameworkName);
    }
  }

  selectFramework(fw: HospitalFrameworkOption): void {
    if (!fw?.frameworkId) return;
    this.selectedFrameworkId.set(fw.frameworkId);
    this.selectedFrameworkName.set(fw.frameworkName);
    this.frameworkSearch.set(fw.frameworkName);
    this.clearSelection();
    this.bulkMode.set(null);
    this.loadMatrix();
  }

  refresh(): void {
    if (this.selectedFrameworkId()) this.loadMatrix();
  }

  resetFilters(): void {
    this.searchText.set('');
    this.filterDepartmentId.set('');
    this.filterOwnerId.set('');
    this.filterStatus.set('');
    this.chipUnassigned.set(false);
    this.chipOverdue.set(false);
    this.chipAssignedToMe.set(false);
  }

  exportCsv(): void {
    const cache = this.userCache();
    const depts = new Map(this.departments().map((d) => [d.id, d]));
    const lines = this.filteredRows().map((row) => {
      const eff = this.effectiveRow(row);
      const deptName =
        (eff.departmentId && depts.get(eff.departmentId)?.name) || row.departmentName || '';
      const ownerName =
        (eff.primaryOwnerUserId && cache[eff.primaryOwnerUserId]?.fullName) || '';
      return {
        code: row.objectiveElementCode,
        title: row.objectiveElementTitle,
        departmentName: deptName,
        ownerName,
        dueDate: formatDueDisplay(eff.dueDate),
        status: computeDisplayStatus(eff),
      };
    });
    const csv = buildOwnershipCsv(lines);
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `oe-ownership-${this.selectedFrameworkName() || 'export'}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  uiFor(row: OeOwnershipMatrixItem): RowUi {
    return this.ui()[row.objectiveElementId] || { activity: 'idle', dirty: false };
  }

  displayStatus(row: OeOwnershipMatrixItem): OeDisplayStatus {
    return computeDisplayStatus(this.effectiveRow(row));
  }

  formatDue(row: OeOwnershipMatrixItem): string {
    return formatDueDisplay(this.effectiveRow(row).dueDate);
  }

  draftDue(row: OeOwnershipMatrixItem): Date | null {
    const d = this.drafts.get(row.objectiveElementId);
    if (d) return d.dueDate;
    return toDateInputValue(row.dueDate);
  }

  deptInputLabel(row: OeOwnershipMatrixItem): string {
    const id = this.drafts.get(row.objectiveElementId)?.departmentId ?? row.departmentId ?? '';
    const found = this.departments().find((d) => d.id === id);
    return found ? this.deptLabel(found) : row.departmentName || '';
  }

  ownerInputLabel(row: OeOwnershipMatrixItem): string {
    const id = this.drafts.get(row.objectiveElementId)?.ownerUserId ?? row.primaryOwnerUserId ?? '';
    return this.userCache()[id]?.fullName || '';
  }

  deptOptionsFor(row: OeOwnershipMatrixItem): FlatDepartment[] {
    const q = (this.uiFor(row).deptSearch || '').trim().toLowerCase();
    let list = this.departments();
    if (q) {
      list = list.filter(
        (d) => d.name.toLowerCase().includes(q) || d.code.toLowerCase().includes(q),
      );
    }
    return this.pinRecent(list);
  }

  filteredBulkDepts(): FlatDepartment[] {
    const q = this.bulkDeptSearch.trim().toLowerCase();
    let list = this.departments();
    if (q) {
      list = list.filter(
        (d) => d.name.toLowerCase().includes(q) || d.code.toLowerCase().includes(q),
      );
    }
    return this.pinRecent(list);
  }

  deptOptionHtml(d: FlatDepartment, q: string): string {
    const label = this.deptLabel(d);
    return highlightMatch(label, q);
  }

  ownerOptionsFor(row: OeOwnershipMatrixItem): IdentityUserListItem[] {
    return this.ownerResults()[row.objectiveElementId] || [];
  }

  personMeta(u: IdentityUserListItem): string {
    return u.employeeNumber ? `EMP-${u.employeeNumber.replace(/^EMP-/i, '')}` : u.email || '';
  }

  onDeptSearch(row: OeOwnershipMatrixItem, value: string): void {
    this.patchUi(row.objectiveElementId, { deptSearch: value, dirty: true, activity: 'dirty' });
  }

  onDeptSelected(row: OeOwnershipMatrixItem, dept: FlatDepartment): void {
    if (!dept?.id) return;
    this.ensureDraft(row);
    const draft = this.drafts.get(row.objectiveElementId)!;
    draft.departmentId = dept.id;
    this.draftRevision.update((n) => n + 1);
    pushRecentDepartmentId(this.hospitalId(), dept.id);
    this.recentDeptIds = loadRecentDepartmentIds(this.hospitalId());
    this.patchUi(row.objectiveElementId, { dirty: true, activity: 'dirty', deptSearch: '' });
    this.scheduleSave(row);
  }

  onOwnerSearch(row: OeOwnershipMatrixItem, value: string): void {
    this.patchUi(row.objectiveElementId, { ownerSearch: value, dirty: true, activity: 'dirty' });
    const deptId = this.drafts.get(row.objectiveElementId)?.departmentId || row.departmentId;
    this.ownerSearch$.next({
      oeId: row.objectiveElementId,
      term: value,
      departmentId: deptId,
      showAll: this.uiFor(row).showAllUsers,
    });
  }

  onOwnerSelected(row: OeOwnershipMatrixItem, user: IdentityUserListItem): void {
    if (!user?.userId) return;
    this.ensureDraft(row);
    this.drafts.get(row.objectiveElementId)!.ownerUserId = user.userId;
    this.draftRevision.update((n) => n + 1);
    this.mergeUsers([user]);
    this.patchUi(row.objectiveElementId, { dirty: true, activity: 'dirty' });
    this.scheduleSave(row);
  }

  toggleShowAllUsers(row: OeOwnershipMatrixItem): void {
    const next = !this.uiFor(row).showAllUsers;
    this.patchUi(row.objectiveElementId, { showAllUsers: next });
    const term = this.uiFor(row).ownerSearch || '';
    const deptId = this.drafts.get(row.objectiveElementId)?.departmentId || row.departmentId;
    this.ownerSearch$.next({
      oeId: row.objectiveElementId,
      term,
      departmentId: deptId,
      showAll: next,
    });
  }

  onDueChanged(row: OeOwnershipMatrixItem, value: Date | null): void {
    this.ensureDraft(row);
    this.drafts.get(row.objectiveElementId)!.dueDate = value;
    this.draftRevision.update((n) => n + 1);
    this.patchUi(row.objectiveElementId, { dirty: true, activity: 'dirty' });
    this.scheduleSave(row);
  }

  savedTooltip(row: OeOwnershipMatrixItem): string {
    return relativeSavedLabel(this.uiFor(row).savedAt);
  }

  retrySave(row: OeOwnershipMatrixItem): void {
    const payload = this.uiFor(row).lastPayload;
    if (!payload) {
      this.scheduleSave(row, true);
      return;
    }
    this.enqueueUpsert(row, payload);
  }

  isSelected(id: string): boolean {
    return this.selection().has(id);
  }

  toggleSelect(id: string, checked: boolean): void {
    this.selection.update((set) => {
      const next = new Set(set);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  allFilteredSelected(): boolean {
    const rows = this.filteredRows();
    if (!rows.length) return false;
    const sel = this.selection();
    return rows.every((r) => sel.has(r.objectiveElementId));
  }

  someFilteredSelected(): boolean {
    const sel = this.selection();
    return this.filteredRows().some((r) => sel.has(r.objectiveElementId));
  }

  toggleSelectAll(checked: boolean): void {
    this.selection.update((set) => {
      const next = new Set(set);
      for (const r of this.filteredRows()) {
        if (checked) next.add(r.objectiveElementId);
        else next.delete(r.objectiveElementId);
      }
      return next;
    });
  }

  clearSelection(): void {
    this.selection.set(new Set());
    this.bulkMode.set(null);
  }

  openBulk(mode: BulkMode): void {
    this.bulkMode.set(mode);
    this.bulkDept = null;
    this.bulkOwner = null;
    this.bulkDue = null;
    this.bulkDeptSearch = '';
    this.bulkOwnerSearch = '';
  }

  onBulkOwnerSearch(term: string): void {
    this.bulkOwnerSearch = term;
    this.bulkOwnerSearch$.next(term);
  }

  applyBulkDepartment(): void {
    if (!this.bulkDept?.id) return;
    const dept = this.bulkDept;
    pushRecentDepartmentId(this.hospitalId(), dept.id);
    this.recentDeptIds = loadRecentDepartmentIds(this.hospitalId());
    for (const id of this.selection()) {
      const row = this.rows().find((r) => r.objectiveElementId === id);
      if (!row) continue;
      this.ensureDraft(row);
      this.drafts.get(id)!.departmentId = dept.id;
      this.draftRevision.update((n) => n + 1);
      this.patchUi(id, { dirty: true, activity: 'dirty' });
      this.scheduleSave(row);
    }
    this.bulkMode.set(null);
  }

  applyBulkOwner(): void {
    if (!this.bulkOwner?.userId) return;
    const owner = this.bulkOwner;
    this.mergeUsers([owner]);
    for (const id of this.selection()) {
      const row = this.rows().find((r) => r.objectiveElementId === id);
      if (!row) continue;
      this.ensureDraft(row);
      this.drafts.get(id)!.ownerUserId = owner.userId;
      this.draftRevision.update((n) => n + 1);
      this.patchUi(id, { dirty: true, activity: 'dirty' });
      this.scheduleSave(row);
    }
    this.bulkMode.set(null);
  }

  applyBulkDue(): void {
    for (const id of this.selection()) {
      const row = this.rows().find((r) => r.objectiveElementId === id);
      if (!row) continue;
      this.ensureDraft(row);
      this.drafts.get(id)!.dueDate = this.bulkDue;
      this.draftRevision.update((n) => n + 1);
      this.patchUi(id, { dirty: true, activity: 'dirty' });
      this.scheduleSave(row);
    }
    this.bulkMode.set(null);
  }

  confirmClear(): void {
    const n = this.selectedCount();
    const ref = this.dialog.open(OeOwnershipClearConfirmDialog, {
      width: '420px',
      data: { count: n },
    });
    ref.afterClosed().subscribe((ok) => {
      if (ok) this.applyBulkClear();
    });
  }

  private applyBulkClear(): void {
    const hospitalId = this.hospitalId();
    const ids = [...this.selection()];
    for (const id of ids) {
      const row = this.rows().find((r) => r.objectiveElementId === id);
      if (!row) continue;
      this.patchUi(id, { activity: 'saving', dirty: true });
      this.saveQueue.enqueue({
        key: `clear:${id}`,
        run: () => this.api.clear(hospitalId, id),
        onSuccess: () => {
          this.rows.update((list) =>
            list.map((r) =>
              r.objectiveElementId === id
                ? {
                    ...r,
                    ownershipId: undefined,
                    departmentId: undefined,
                    departmentName: undefined,
                    primaryOwnerUserId: undefined,
                    dueDate: undefined,
                    status: 'Unassigned',
                  }
                : r,
            ),
          );
          this.drafts.delete(id);
          this.draftRevision.update((n) => n + 1);
          this.patchUi(id, { activity: 'saved', dirty: false, savedAt: Date.now() });
        },
        onError: (err) => {
          this.patchUi(id, {
            activity: 'error',
            dirty: true,
            errorMessage: this.errMsg(err, 'Failed to clear'),
          });
        },
      });
    }
    this.clearSelection();
  }

  private bootstrap(): void {
    const orgId = this.session.getOrganizationId();
    const hid = this.hospitalId();
    if (!orgId) {
      this.error.set('Organization context is required.');
      return;
    }
    this.orgApi.listHospitals(orgId).subscribe({
      next: (hospitals) => {
        let id = hid;
        if (!id && hospitals?.length) id = hospitals[0].id;
        const match = hospitals?.find((h) => h.id === id) || hospitals?.[0];
        if (match) {
          this.hospitalId.set(match.id);
          this.hospitalName.set(match.name);
          this.recentDeptIds = loadRecentDepartmentIds(match.id);
          this.loadFrameworksAndDeps(match.id, orgId);
        } else {
          this.error.set('No hospital found for this organization.');
        }
      },
      error: (err) => this.error.set(this.errMsg(err, 'Failed to load hospital')),
    });
  }

  private loadFrameworksAndDeps(hospitalId: string, orgId: string): void {
    this.api.listHospitalFrameworks(hospitalId).subscribe({
      next: (list) => {
        this.frameworks.set(list);
        if (list.length === 1) {
          this.selectFramework(list[0]);
        }
      },
      error: (err) => this.error.set(this.errMsg(err, 'Failed to load frameworks')),
    });
    this.orgApi.getDepartmentTree(orgId, hospitalId).subscribe({
      next: (tree) => this.departments.set(flattenDepartments(tree)),
      error: () => this.departments.set([]),
    });
  }

  private loadMatrix(): void {
    const hospitalId = this.hospitalId();
    const frameworkId = this.selectedFrameworkId();
    if (!hospitalId || !frameworkId) return;
    this.loading.set(true);
    this.error.set(null);
    this.rows.set([]);
    this.drafts.clear();
    this.draftRevision.update((n) => n + 1);
    this.api.listMatrix(hospitalId, frameworkId).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
        this.prefetchOwners(rows);
      },
      error: (err) => {
        this.loading.set(false);
        this.rows.set([]);
        this.error.set(this.errMsg(err, 'Unable to load Objective Elements'));
      },
    });
  }

  private prefetchOwners(rows: OeOwnershipMatrixItem[]): void {
    const missing = [
      ...new Set(rows.map((r) => r.primaryOwnerUserId).filter((x): x is string => !!x)),
    ].filter((id) => !this.userCache()[id]);
    if (!missing.length) return;
    this.identityApi.listUsers({ page: 1, pageSize: 200 }).subscribe({
      next: (page) => this.mergeUsers(page.items),
    });
  }

  private mergeUsers(users: IdentityUserListItem[]): void {
    if (!users?.length) return;
    this.userCache.update((cache) => {
      const next = { ...cache };
      for (const u of users) next[u.userId] = u;
      return next;
    });
  }

  private ensureDraft(row: OeOwnershipMatrixItem): void {
    if (this.drafts.has(row.objectiveElementId)) return;
    this.drafts.set(row.objectiveElementId, {
      departmentId: row.departmentId || '',
      ownerUserId: row.primaryOwnerUserId || '',
      dueDate: toDateInputValue(row.dueDate),
    });
    this.draftRevision.update((n) => n + 1);
  }

  private effectiveRow(row: OeOwnershipMatrixItem): OeOwnershipMatrixItem {
    const d = this.drafts.get(row.objectiveElementId);
    if (!d) return row;
    return {
      ...row,
      departmentId: d.departmentId || undefined,
      primaryOwnerUserId: d.ownerUserId || undefined,
      dueDate: d.dueDate ? d.dueDate.toISOString() : undefined,
    };
  }

  private scheduleSave(row: OeOwnershipMatrixItem, immediate = false): void {
    const id = row.objectiveElementId;
    const existing = this.debounceTimers.get(id);
    if (existing) clearTimeout(existing);
    if (immediate) {
      this.trySave(row);
      return;
    }
    this.debounceTimers.set(
      id,
      setTimeout(() => {
        this.debounceTimers.delete(id);
        this.trySave(row);
      }, SAVE_DEBOUNCE_MS),
    );
  }

  private trySave(row: OeOwnershipMatrixItem): void {
    const organizationId = this.session.getOrganizationId();
    if (!organizationId) {
      this.error.set('Session organization is required');
      return;
    }
    this.ensureDraft(row);
    const draft = this.drafts.get(row.objectiveElementId)!;
    const departmentId = draft.departmentId.trim();
    const primaryOwnerUserId = draft.ownerUserId.trim();
    const alreadyAssigned = isAssigned(row);

    if (!departmentId || !primaryOwnerUserId) {
      if (!alreadyAssigned) {
        this.patchUi(row.objectiveElementId, { dirty: true, activity: 'dirty' });
        return;
      }
      return;
    }

    const payload: UpsertOeOwnershipRequest = {
      organizationId,
      frameworkId: row.frameworkId || this.selectedFrameworkId(),
      objectiveElementId: row.objectiveElementId,
      departmentId,
      primaryOwnerUserId,
      dueDate: draft.dueDate ? new Date(draft.dueDate).toISOString() : null,
    };
    this.enqueueUpsert(row, payload);
  }

  private enqueueUpsert(row: OeOwnershipMatrixItem, payload: UpsertOeOwnershipRequest): void {
    const id = row.objectiveElementId;
    this.patchUi(id, { lastPayload: payload });
    this.saveQueue.enqueue({
      key: id,
      run: () => this.api.upsert(this.hospitalId(), payload),
      onStart: () => this.patchUi(id, { activity: 'saving' }),
      onSuccess: () => {
        const dept = this.departments().find((d) => d.id === payload.departmentId);
        this.rows.update((list) =>
          list.map((r) =>
            r.objectiveElementId === id
              ? {
                  ...r,
                  departmentId: payload.departmentId,
                  departmentName: dept?.name || r.departmentName,
                  primaryOwnerUserId: payload.primaryOwnerUserId,
                  dueDate: payload.dueDate || undefined,
                  status: 'Active',
                }
              : r,
          ),
        );
        this.drafts.delete(id);
        this.draftRevision.update((n) => n + 1);
        this.patchUi(id, { activity: 'saved', dirty: false, savedAt: Date.now(), errorMessage: undefined });
      },
      onError: (err) => {
        this.patchUi(id, {
          activity: 'error',
          dirty: true,
          errorMessage: this.errMsg(err, 'Failed to save — Retry'),
        });
      },
    });
  }

  private patchUi(id: string, patch: Partial<RowUi>): void {
    this.ui.update((m) => {
      const prev: RowUi = m[id] ?? { activity: 'idle', dirty: false };
      return { ...m, [id]: { ...prev, ...patch } };
    });
  }

  private pinRecent(list: FlatDepartment[]): FlatDepartment[] {
    if (!this.recentDeptIds.length) return list;
    const map = new Map(list.map((d) => [d.id, d]));
    const recent: FlatDepartment[] = [];
    for (const id of this.recentDeptIds) {
      const d = map.get(id);
      if (d) {
        recent.push(d);
        map.delete(id);
      }
    }
    return [...recent, ...map.values()];
  }

  private deptLabel(d: FlatDepartment): string {
    return d.code ? `${d.name} (${d.code})` : d.name;
  }

  private errMsg(err: unknown, fallback: string): string {
    const e = err as { error?: { error?: { message?: string }; message?: string }; message?: string };
    return e?.error?.error?.message || e?.error?.message || e?.message || fallback;
  }
}

@Component({
  selector: 'app-oe-ownership-clear-confirm',
  standalone: true,
  imports: [CommonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Clear Assignment</h2>
    <mat-dialog-content>
      Remove ownership from {{ data.count }} objective element{{ data.count === 1 ? '' : 's' }}?
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button type="button" class="aov-btn aov-btn--text" mat-dialog-close>Cancel</button>
      <button type="button" class="aov-btn aov-btn--danger" [mat-dialog-close]="true">Clear</button>
    </mat-dialog-actions>
  `,
})
export class OeOwnershipClearConfirmDialog {
  readonly data = inject<{ count: number }>(MAT_DIALOG_DATA);
}
