import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  DashboardSnapshot,
  SnapshotQueryParams,
} from '../../features/dashboard/models/dashboard-snapshot.model';
import { ProgramOverviewSnapshotStore } from './program-overview-snapshot.store';

/**
 * Program overview snapshot API (shared by header + dashboard).
 * Delegates to {@link ProgramOverviewSnapshotStore} for deduplicated caching.
 */
@Injectable({
  providedIn: 'root',
})
export class ProgramOverviewSnapshotService {
  private readonly store = inject(ProgramOverviewSnapshotStore);

  getProgramOverviewSnapshot(
    params?: SnapshotQueryParams,
    options?: { background?: boolean; forceRefresh?: boolean },
  ): Observable<DashboardSnapshot> {
    return this.store.getProgramOverviewSnapshot(params, options);
  }

  getCached(params?: SnapshotQueryParams): DashboardSnapshot | null {
    return this.store.getCached(params);
  }

  isFresh(params?: SnapshotQueryParams, ttlMs?: number): boolean {
    return this.store.isFresh(params, ttlMs);
  }

  invalidate(params?: SnapshotQueryParams): void {
    this.store.invalidate(params);
  }
}
