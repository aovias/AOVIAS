import { WritableSignal } from '@angular/core';
import { Observable } from 'rxjs';
import { ReportsApiService } from './reports-api.service';
import { ReportExportFormat, ReportQueryParams, ReportType } from '../models/report.models';

export function periodToDateRange(period: string): { fromDate?: string; toDate?: string } {
  const to = new Date();
  const from = new Date();
  switch (period) {
    case 'quarter':
      from.setMonth(from.getMonth() - 3);
      break;
    case 'year':
      from.setFullYear(from.getFullYear() - 1);
      break;
    /**
     * The audit programme year: 1 Jan – 31 Dec of the current year.
     *
     * Audits are planned, scheduled and reported against a programme year (`programYear` /
     * `programMonth` on the schedule), so a rolling window can land between audit cycles and
     * report nothing. Whole-year bounds match how the business reasons about audit reporting.
     */
    case 'programYear':
      from.setMonth(0, 1);
      from.setHours(0, 0, 0, 0);
      to.setMonth(11, 31);
      to.setHours(23, 59, 59, 999);
      break;
    default:
      from.setMonth(from.getMonth() - 1);
  }
  return { fromDate: from.toISOString(), toDate: to.toISOString() };
}

export function loadReport<T>(
  request: () => Observable<T>,
  onSuccess: (data: T) => void,
  loading: WritableSignal<boolean>,
  error: WritableSignal<string | null>,
): void {
  loading.set(true);
  error.set(null);
  request().subscribe({
    next: (data) => {
      onSuccess(data);
      loading.set(false);
    },
    error: (err) => {
      loading.set(false);
      error.set(err?.message ?? 'Failed to load report. Please retry.');
    },
  });
}

export function exportReportFile(
  api: ReportsApiService,
  type: ReportType,
  format: ReportExportFormat,
  params?: ReportQueryParams,
  customBody?: import('../models/report.models').CustomReportRequest,
): void {
  api.exportReport(type, format, params, customBody).subscribe({
    next: (blob) => {
      const ext = format === 'pdf' ? 'pdf' : format === 'excel' ? 'xlsx' : 'csv';
      api.downloadBlob(blob, `${type}-report.${ext}`);
    },
    error: () => {
      // caller may set error signal
    },
  });
}
