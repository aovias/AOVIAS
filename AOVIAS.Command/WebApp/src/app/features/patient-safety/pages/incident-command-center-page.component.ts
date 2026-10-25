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
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, forkJoin, of, takeUntil } from 'rxjs';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import { IncidentDashboardComponent } from '../components/incident-dashboard.component';
import { IncidentGridComponent } from '../components/incident-grid.component';
import { IncidentDetailPanelComponent } from '../components/incident-detail-panel.component';
import {
  INCIDENT_CATEGORIES,
  INCIDENT_QUICK_FILTERS,
  type IncidentDashboard,
  type IncidentDetail,
  type IncidentListItem,
} from '../models/incident.models';

@Component({
  selector: 'app-incident-command-center-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IncidentDashboardComponent,
    IncidentGridComponent,
    IncidentDetailPanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-workbench">
      <header class="page-header">
        <div>
          <h1>Incident Command Center</h1>
          <p class="subtitle">Enterprise patient safety incident registry</p>
        </div>
        <div class="header-actions">
          <button type="button" class="btn-secondary" (click)="exportCsv()" [disabled]="!items().length">Export CSV</button>
          <button type="button" class="btn-primary" (click)="showCreate.set(true)">Report Incident</button>
        </div>
      </header>

      @if (error()) { <div class="banner error">{{ error() }}</div> }
      @if (loading()) { <p class="muted">Loading incident command center…</p> }
      @else {
        <app-incident-dashboard [dashboard]="dashboard()" />
        <div class="chips">
          @for (chip of quickFilters; track chip.key) {
            <button type="button" class="chip" [class.chip--active]="filterQuick === chip.key" (click)="toggleQuickFilter(chip.key)">{{ chip.label }}</button>
          }
        </div>
        <div class="filters">
          <input type="search" placeholder="Search incidents…" [(ngModel)]="searchInput" (ngModelChange)="onSearchChange($event)" />
          <select [(ngModel)]="filterStatus" (ngModelChange)="reload()">
            <option value="">All Statuses</option>
            <option value="Reported">Reported</option>
            <option value="UnderInvestigation">Under Investigation</option>
            <option value="RcaInProgress">RCA In Progress</option>
            <option value="Closed">Closed</option>
          </select>
          <select [(ngModel)]="filterSeverity" (ngModelChange)="reload()">
            <option value="">All Severities</option>
            <option value="Low">Low</option>
            <option value="Moderate">Moderate</option>
            <option value="Major">Major</option>
            <option value="Critical">Critical</option>
            <option value="Sentinel">Sentinel</option>
          </select>
          <select [(ngModel)]="filterCategory" (ngModelChange)="reload()">
            <option value="">All Categories</option>
            @for (c of categories; track c.value) {
              <option [value]="c.value">{{ c.label }}</option>
            }
          </select>
        </div>
        <app-incident-grid [items]="items()" [selectedId]="selectedId()" (select)="openDetail($event)" (sort)="onSort($event)" />
        @if (detail()) {
          <app-incident-detail-panel
            [detail]="detail()"
            (close)="closeDetail()"
            (promoteFinding)="onPromoteFinding($event)"
            (promoteDeficiency)="onPromoteDeficiency($event)"
          />
        }
      }

      @if (showCreate()) {
        <div class="modal-backdrop" (click)="showCreate.set(false)" role="dialog" aria-modal="true" aria-labelledby="report-incident-title">
          <div class="modal" (click)="$event.stopPropagation()">
            <header class="modal__head">
              <div>
                <h2 id="report-incident-title">Report Incident</h2>
                <p class="modal__subtitle">Capture a patient safety event for investigation</p>
              </div>
              <button type="button" class="modal__close" (click)="showCreate.set(false)" aria-label="Close">×</button>
            </header>
            <div class="modal__body">
              <label class="field">
                <span class="field__label">Title</span>
                <input [(ngModel)]="createTitle" placeholder="Brief incident title" />
              </label>
              <label class="field">
                <span class="field__label">Description</span>
                <textarea [(ngModel)]="createDescription" rows="4" placeholder="What happened? Include relevant context."></textarea>
              </label>
              <div class="field-row">
                <label class="field">
                  <span class="field__label">Category</span>
                  <select [(ngModel)]="createCategory">
                    @for (c of categories; track c.value) { <option [value]="c.value">{{ c.label }}</option> }
                  </select>
                </label>
                <label class="field">
                  <span class="field__label">Severity</span>
                  <select [(ngModel)]="createSeverity">
                    <option value="Moderate">Moderate</option>
                    <option value="Major">Major</option>
                    <option value="Critical">Critical</option>
                  </select>
                </label>
              </div>
              <div class="checkbox-row">
                <label class="checkbox">
                  <input type="checkbox" [(ngModel)]="createAnonymous" />
                  <span>Anonymous report</span>
                </label>
                <label class="checkbox">
                  <input type="checkbox" [(ngModel)]="createPatientInvolved" />
                  <span>Patient involved</span>
                </label>
              </div>
            </div>
            <footer class="modal__foot">
              <button type="button" class="btn-secondary" (click)="showCreate.set(false)">Cancel</button>
              <button type="button" class="btn-primary" (click)="submitCreate()" [disabled]="!createTitle.trim()">Submit</button>
            </footer>
          </div>
        </div>
      }
    </div>
  `,
})
export class IncidentCommandCenterPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(IncidentApiService);
  private readonly aiContext = inject(AiPageContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  readonly dashboard = signal<IncidentDashboard | null>(null);
  readonly items = signal<IncidentListItem[]>([]);
  readonly detail = signal<IncidentDetail | null>(null);
  readonly selectedId = signal<string | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly showCreate = signal(false);

  quickFilters = INCIDENT_QUICK_FILTERS;
  categories = INCIDENT_CATEGORIES;
  filterQuick = '';
  filterStatus = '';
  filterSeverity = '';
  filterCategory = '';
  searchInput = '';
  createTitle = '';
  createDescription = '';
  createCategory = 'Other';
  createSeverity = 'Moderate';
  createAnonymous = false;
  createPatientInvolved = false;

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety' });
    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe((p) => {
      if (p['quickFilter']) this.filterQuick = p['quickFilter'];
      this.reload();
    });
    this.search$.pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$)).subscribe(() => this.reload());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      dashboard: this.api.getDashboard().pipe(catchError(() => of(null))),
      list: this.api.getIncidents({
        quickFilter: this.filterQuick || undefined,
        status: this.filterStatus || undefined,
        severity: this.filterSeverity || undefined,
        category: this.filterCategory || undefined,
        search: this.searchInput || undefined,
      }).pipe(catchError((e) => { this.error.set(e?.message ?? 'Failed to load incidents'); return of({ items: [], totalCount: 0, page: 1, pageSize: 25 }); })),
    }).pipe(finalize(() => this.loading.set(false))).subscribe(({ dashboard, list }) => {
      this.dashboard.set(dashboard);
      this.items.set(list.items);
    });
  }

  onSearchChange(value: string): void { this.search$.next(value); }
  toggleQuickFilter(key: string): void { this.filterQuick = this.filterQuick === key ? '' : key; this.reload(); }
  onSort(_col: string): void { this.reload(); }

  openDetail(id: string): void {
    this.selectedId.set(id);
    this.api.getById(id).subscribe((d) => this.detail.set(d));
  }

  closeDetail(): void { this.selectedId.set(null); this.detail.set(null); }

  onPromoteFinding(id: string): void {
    this.api.promoteFinding({ incidentId: id }).subscribe(() => { this.openDetail(id); this.reload(); });
  }

  onPromoteDeficiency(id: string): void {
    this.api.promoteDeficiency({ incidentId: id }).subscribe(() => { this.openDetail(id); this.reload(); });
  }

  submitCreate(): void {
    this.api.create({
      title: this.createTitle,
      description: this.createDescription,
      category: this.createCategory,
      severity: this.createSeverity,
      isAnonymous: this.createAnonymous,
      patientInvolved: this.createPatientInvolved,
    }).subscribe(() => {
      this.showCreate.set(false);
      this.createTitle = '';
      this.createDescription = '';
      this.reload();
    });
  }

  exportCsv(): void {
    const rows = this.items();
    const header = ['IncidentNumber', 'Title', 'Severity', 'Category', 'Department', 'Status', 'Date'];
    const lines = [header.join(','), ...rows.map((r) =>
      [r.incidentNumber, `"${r.title.replace(/"/g, '""')}"`, r.severity, r.category, r.department, r.status, r.incidentDateTime].join(','),
    )];
    const blob = new Blob([lines.join('\n')], { type: 'text/csv' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = 'incidents.csv';
    a.click();
  }
}
