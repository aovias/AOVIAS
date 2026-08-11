import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ProgramOverviewSnapshotService } from '../../../core/services/program-overview-snapshot.service';
import { DashboardSnapshot, SnapshotQueryParams } from '../models/dashboard-snapshot.model';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private readonly snapshot = inject(ProgramOverviewSnapshotService);

  getProgramOverviewSnapshot(
    params?: SnapshotQueryParams,
    options?: { background?: boolean; forceRefresh?: boolean },
  ): Observable<DashboardSnapshot> {
    return this.snapshot.getProgramOverviewSnapshot(params, options);
  }

  getCached(params?: SnapshotQueryParams): DashboardSnapshot | null {
    return this.snapshot.getCached(params);
  }

  isFresh(params?: SnapshotQueryParams, ttlMs?: number): boolean {
    return this.snapshot.isFresh(params, ttlMs);
  }
}
