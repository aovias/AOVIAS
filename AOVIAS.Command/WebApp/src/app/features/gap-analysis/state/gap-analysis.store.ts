import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, finalize, throwError } from 'rxjs';
import { GapAnalysisReportDto } from '../models/gap-analysis-api.model';
import { GapAnalysisResultsVm } from '../models/gap-analysis-vm.model';
import { GapAnalysisApiService } from '../services/gap-analysis-api.service';
import { mapGapAnalysisToVm } from '../services/gap-analysis.mapper';

const GUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

@Injectable({
  providedIn: 'root',
})
export class GapAnalysisStore {
  private readonly api = inject(GapAnalysisApiService);

  readonly assessmentId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly report = signal<GapAnalysisReportDto | null>(null);
  readonly vm = computed<GapAnalysisResultsVm | null>(() => mapGapAnalysisToVm(this.report()));

  load(assessmentId: string): void {
    if (!GUID_PATTERN.test(assessmentId)) {
      this.assessmentId.set(assessmentId);
      this.report.set(null);
      this.error.set('Invalid assessment id.');
      return;
    }

    this.assessmentId.set(assessmentId);
    this.loading.set(true);
    this.error.set(null);

    this.api
      .getReport(assessmentId)
      .pipe(
        catchError((err: HttpErrorResponse) => {
          if (err.status === 404) {
            return this.api.generateReport(assessmentId);
          }
          return throwError(() => err);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (report) => {
          this.report.set(report);
          this.error.set(null);
        },
        error: (err: HttpErrorResponse) => {
          this.report.set(null);
          const msg =
            (err.error &&
              typeof err.error === 'object' &&
              'message' in err.error &&
              (err.error as { message?: string }).message) ||
            err.message ||
            'Could not load gap analysis.';
          this.error.set(typeof msg === 'string' ? msg : 'Could not load gap analysis.');
        },
      });
  }

  regenerate(): void {
    const id = this.assessmentId();
    if (!id || !GUID_PATTERN.test(id)) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.api
      .generateReport(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (report) => {
          this.report.set(report);
          this.error.set(null);
        },
        error: (err: HttpErrorResponse) => {
          const msg =
            (err.error &&
              typeof err.error === 'object' &&
              'message' in err.error &&
              (err.error as { message?: string }).message) ||
            err.message ||
            'Could not regenerate gap analysis.';
          this.error.set(typeof msg === 'string' ? msg : 'Could not regenerate gap analysis.');
        },
      });
  }

  reset(): void {
    this.assessmentId.set(null);
    this.loading.set(false);
    this.error.set(null);
    this.report.set(null);
  }
}
