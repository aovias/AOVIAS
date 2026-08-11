import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, forkJoin, takeUntil } from 'rxjs';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import { IndicatorDashboardComponent } from '../components/indicator-dashboard.component';
import { IndicatorGridComponent } from '../components/indicator-grid.component';
import { IndicatorDetailPanelComponent } from '../components/indicator-detail-panel.component';
import type { ClinicalIndicatorDetail, ClinicalIndicatorListItem, QualityDashboard } from '../models/quality-indicator.models';

@Component({
  selector: 'app-clinical-indicators-page',
  standalone: true,
  imports: [CommonModule, FormsModule, IndicatorDashboardComponent, IndicatorGridComponent, IndicatorDetailPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-workbench">
      <header class="page-header">
        <div><h1>Clinical Indicators</h1><p class="subtitle">Enterprise indicator registry with threshold status and trends</p></div>
        <div class="header-actions"><button type="button" class="btn-secondary" (click)="exportCsv()" [disabled]="!items().length">Export CSV</button></div>
      </header>
      <app-indicator-dashboard [dashboard]="dashboard()" [showShortcuts]="false" />
      <div class="filters">
        <input type="search" placeholder="Search indicators…" [(ngModel)]="searchInput" (ngModelChange)="onSearchChange($event)" />
        <select [(ngModel)]="filterThreshold" (ngModelChange)="reload()">
          <option value="">All Threshold Status</option>
          <option value="Green">Green</option>
          <option value="Yellow">Yellow</option>
          <option value="Red">Red</option>
          <option value="Critical">Critical</option>
        </select>
        <select [(ngModel)]="filterTrend" (ngModelChange)="reload()">
          <option value="">All Trends</option>
          <option value="Improving">Improving</option>
          <option value="Stable">Stable</option>
          <option value="Declining">Declining</option>
          <option value="Volatile">Volatile</option>
        </select>
      </div>
      <app-indicator-grid [items]="items()" [selectedId]="selectedId()" (select)="openDetail($event)" (sort)="onSort($event)" />
      @if (detail()) { <app-indicator-detail-panel [detail]="detail()" (close)="closeDetail()" /> }
    </div>
  `,
})
export class ClinicalIndicatorsPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  readonly dashboard = signal<QualityDashboard | null>(null);
  readonly items = signal<ClinicalIndicatorListItem[]>([]);
  readonly detail = signal<ClinicalIndicatorDetail | null>(null);
  readonly selectedId = signal<string | null>(null);

  searchInput = '';
  filterThreshold = '';
  filterTrend = '';
  sortBy = 'name';
  sortDesc = true;

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', entityType: 'IndicatorRegistry' });
    this.search$.pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$)).subscribe(() => this.reload());
    this.reload();
    const selected = this.route.snapshot.queryParamMap.get('selected');
    if (selected) this.openDetail(selected);
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  onSearchChange(v: string): void { this.search$.next(v); }
  onSort(field: string): void {
    if (this.sortBy === field) this.sortDesc = !this.sortDesc;
    else { this.sortBy = field; this.sortDesc = true; }
    this.reload();
  }

  reload(): void {
    forkJoin({
      dashboard: this.api.getDashboard(),
      list: this.api.getIndicators({
        search: this.searchInput || undefined,
        thresholdStatus: this.filterThreshold || undefined,
        trend: this.filterTrend || undefined,
        sortBy: this.sortBy,
        sortDesc: this.sortDesc,
      }),
    }).subscribe(({ dashboard, list }) => {
      this.dashboard.set(dashboard);
      this.items.set(list.items);
    });
  }

  openDetail(id: string): void {
    this.selectedId.set(id);
    this.api.getById(id).subscribe((d) => {
      this.detail.set(d);
      this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', entityType: 'ClinicalIndicator', entityId: id, label: d.name });
    });
  }

  closeDetail(): void {
    this.detail.set(null);
    this.selectedId.set(null);
    this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', entityType: 'IndicatorRegistry' });
  }

  exportCsv(): void {
    const rows = this.items();
    const header = ['Code', 'Name', 'Category', 'Department', 'Current', 'Target', 'Status', 'Trend'];
    const lines = rows.map((r) => [r.indicatorCode, r.name, r.category, r.department, r.currentValue ?? '', r.targetValue ?? '', r.thresholdStatus, r.trend].join(','));
    const blob = new Blob([[header.join(','), ...lines].join('\n')], { type: 'text/csv' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = 'clinical-indicators.csv';
    a.click();
  }
}
