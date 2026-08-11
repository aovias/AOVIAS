import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ColumnMetadata, CustomReportRequest, CustomReportResult } from '../models/report.models';
import { ReportsApiService } from '../services/reports-api.service';
import { exportReportFile } from '../services/report-page.helper';

@Component({
  selector: 'app-custom-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="custom-reports-page">
      <div class="page-header">
        <h1 class="page-title">Custom Reports</h1>
        <p class="page-description">Build custom reports with flexible data dimensions, filters, and export options.</p>
      </div>

      <div class="report-builder">
        <div class="builder-section">
          <h3 class="builder-title">Select Data Dimensions</h3>
          <div class="dimension-grid">
            @for (dim of dimensions; track dim.id) {
              <label class="dimension-checkbox">
                <input type="checkbox" [(ngModel)]="dim.selected"/>
                <span class="checkbox-label">{{ dim.label }}</span>
              </label>
            }
          </div>
        </div>

        <div class="builder-section">
          <h3 class="builder-title">Apply Filters</h3>
          <div class="filters-grid">
            <select [(ngModel)]="selectedFramework" class="filter-select">
              <option value="">Framework</option>
              <option value="nabh">NABH</option>
              <option value="jci">JCI</option>
            </select>
            <select [(ngModel)]="selectedChapter" class="filter-select">
              <option value="">Chapter</option>
              <option value="acc">ACC</option>
              <option value="cop">COP</option>
            </select>
            <select [(ngModel)]="selectedDepartment" class="filter-select">
              <option value="">Department</option>
              <option value="security">Security</option>
              <option value="clinical">Clinical</option>
            </select>
            <select [(ngModel)]="selectedSeverity" class="filter-select">
              <option value="">Severity</option>
              <option value="critical">Critical</option>
              <option value="major">Major</option>
            </select>
          </div>
        </div>

        <div class="builder-actions">
          <button class="btn-generate" type="button" (click)="generateReport()" [disabled]="loading()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" stroke-width="2"/>
            </svg>
            Generate Report
          </button>
          <button class="btn-clear" (click)="clearFilters()">Clear All</button>
        </div>
      </div>

      @if (loading()) { <p class="page-description">Generating report…</p> }
      @if (error()) { <p class="page-description" style="color:#ef4444">{{ error() }}</p> }

      @if (reportGenerated()) {
        <div class="report-results">
          <div class="results-header">
            <h3 class="results-title">Generated Report</h3>
            <div class="results-actions">
              <button type="button" class="btn-export" (click)="onExport()">Export PDF</button>
            </div>
          </div>
          <div class="results-table">
            <table class="data-table">
              <thead>
                <tr>
                  @for (col of columns; track col.key) {
                    <th>{{ col.label }}</th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (row of tableRows; track $index) {
                  <tr>
                    @for (col of columns; track col.key) {
                      <td>{{ cellValue(row, col.key) }}</td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .custom-reports-page {
      padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      background: var(--bg-primary);
      min-height: calc(100vh - 64px);
    }

    .page-title {
      font-size: var(--font-size-page-title);
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 8px 0;
    }

    .page-description {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 0 0 32px 0;
    }

    .report-builder {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 24px;
      margin-bottom: 32px;
    }

    .builder-section {
      margin-bottom: 32px;
    }

    .builder-section:last-child {
      margin-bottom: 0;
    }

    .builder-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 16px 0;
    }

    .dimension-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 12px;
    }

    .dimension-checkbox {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .dimension-checkbox:hover {
      background: var(--hover-bg);
    }

    .dimension-checkbox input[type="checkbox"] {
      width: 18px;
      height: 18px;
      cursor: pointer;
    }

    .checkbox-label {
      font-size: 14px;
      color: var(--text-primary);
      font-weight: 500;
    }

    .filters-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 12px;
    }

    .filter-select {
      padding: 12px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      color: var(--text-primary);
      font-size: 14px;
      cursor: pointer;
    }

    .builder-actions {
      display: flex;
      gap: 12px;
      padding-top: 24px;
      border-top: 1px solid var(--border-color);
    }

    .btn-generate {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 24px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-generate:hover {
      background: var(--primary-hover);
    }

    .btn-clear {
      padding: 12px 24px;
      background: var(--bg-primary);
      color: var(--text-primary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
    }

    .report-results {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      overflow: hidden;
    }

    .results-header {
      padding: 20px;
      border-bottom: 1px solid var(--border-color);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .results-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .results-actions {
      display: flex;
      gap: 12px;
    }

    .btn-export {
      padding: 8px 16px;
      background: var(--primary-bg-light);
      color: var(--primary-color);
      border: 1px solid var(--primary-color);
      border-radius: 6px;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
    }

    .results-table {
      overflow-x: auto;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
    }

    .data-table thead {
      background: var(--bg-primary);
    }

    .data-table th {
      padding: var(--space-2) var(--table-cell-padding-inline);
      text-align: left;
      font-size: 12px;
      font-weight: 600;
      color: var(--text-secondary);
      text-transform: uppercase;
    }

    .data-table td {
      padding: var(--table-cell-padding-block) var(--table-cell-padding-inline);
      border-bottom: 1px solid var(--border-color);
      font-size: 14px;
      color: var(--text-primary);
    }

    .data-table tr:hover {
      background: var(--hover-bg);
    }

    .status-badge {
      padding: 4px 10px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 600;
      text-transform: capitalize;
    }

    .status-badge.compliant {
      background: rgba(16, 185, 129, 0.15);
      color: #10b981;
    }

    .status-badge.partial {
      background: rgba(245, 158, 11, 0.15);
      color: #f59e0b;
    }
  `]
})
export class CustomReportsComponent {
  private readonly api = inject(ReportsApiService);

  reportGenerated = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);
  selectedFramework = '';
  selectedChapter = '';
  selectedDepartment = '';
  selectedSeverity = '';

  columns: ColumnMetadata[] = [];
  tableRows: Record<string, unknown>[] = [];

  dimensions = [
    { id: 'Clause', label: 'Clause', selected: true },
    { id: 'Chapter', label: 'Chapter', selected: true },
    { id: 'Department', label: 'Department', selected: true },
    { id: 'Tasks', label: 'Tasks', selected: false },
    { id: 'Evidence', label: 'Evidence', selected: false },
    { id: 'AuditFindings', label: 'Audit Findings', selected: false },
  ];

  generateReport(): void {
    this.loading.set(true);
    this.error.set(null);
    const request: CustomReportRequest = {
      dimensions: this.dimensions.filter((d) => d.selected).map((d) => d.id),
      frameworkId: this.selectedFramework || undefined,
      chapterId: this.selectedChapter || undefined,
      departmentId: this.selectedDepartment || undefined,
      severity: this.selectedSeverity || undefined,
    };
    this.api.generateCustomReport(request).subscribe({
      next: (result) => this.applyResult(result),
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.message ?? 'Failed to generate report.');
      },
    });
  }

  onExport(): void {
    const request: CustomReportRequest = {
      dimensions: this.dimensions.filter((d) => d.selected).map((d) => d.id),
      severity: this.selectedSeverity || undefined,
    };
    exportReportFile(this.api, 'custom', 'pdf', undefined, request);
  }

  clearFilters(): void {
    this.selectedFramework = '';
    this.selectedChapter = '';
    this.selectedDepartment = '';
    this.selectedSeverity = '';
  }

  cellValue(row: Record<string, unknown>, key: string): string {
    const v = row[key];
    return v == null ? '' : String(v);
  }

  private applyResult(result: CustomReportResult): void {
    this.columns = result.columns ?? [];
    this.tableRows = result.rows ?? [];
    this.reportGenerated.set(true);
    this.loading.set(false);
  }
}
