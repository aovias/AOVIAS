import { Injectable, inject, signal } from '@angular/core';
import { Observable, of, shareReplay, tap, throwError } from 'rxjs';
import { HttpContext } from '@angular/common/http';
import { BaseApiService } from '../../shared/services/base-api.service';
import {
  DashboardSnapshot,
  SnapshotQueryParams,
} from '../../features/dashboard/models/dashboard-snapshot.model';
import {
  BACKGROUND_REQUEST,
  DASHBOARD_HTTP_CATEGORY,
  SKIP_LOADING_OVERLAY,
} from '../performance/http-context.tokens';

export interface SnapshotCacheKey {
  hospitalId: string | null;
  frameworkId: string | null;
}

interface CacheEntry {
  data: DashboardSnapshot;
  fetchedAt: number;
}

const DEFAULT_TTL_MS = 3 * 60 * 1000;

function cacheKey(params?: SnapshotQueryParams): string {
  const hospitalId = params?.hospitalId ?? '';
  const frameworkId = params?.frameworkId ?? '';
  return `${hospitalId}|${frameworkId}`;
}

/**
 * Shared program-overview snapshot cache. Deduplicates in-flight requests and serves
 * stale data while revalidating (used by dashboard, sidebar ring, and AI assistant).
 */
@Injectable({ providedIn: 'root' })
export class ProgramOverviewSnapshotStore extends BaseApiService {
  private readonly cache = new Map<string, CacheEntry>();
  private readonly inFlight = new Map<string, Observable<DashboardSnapshot>>();
  private readonly lastUpdatedAt = signal<number | null>(null);

  readonly snapshotLastUpdatedAt = this.lastUpdatedAt.asReadonly();

  getCached(params?: SnapshotQueryParams): DashboardSnapshot | null {
    const key = cacheKey(params);
    const entry = this.cache.get(key);
    return entry?.data ?? null;
  }

  isFresh(params?: SnapshotQueryParams, ttlMs: number = DEFAULT_TTL_MS): boolean {
    const key = cacheKey(params);
    const entry = this.cache.get(key);
    if (!entry) {
      return false;
    }
    return Date.now() - entry.fetchedAt < ttlMs;
  }

  invalidate(params?: SnapshotQueryParams): void {
    if (params) {
      this.cache.delete(cacheKey(params));
      this.inFlight.delete(cacheKey(params));
      return;
    }
    this.cache.clear();
    this.inFlight.clear();
  }

  getProgramOverviewSnapshot(
    params?: SnapshotQueryParams,
    options?: { background?: boolean; forceRefresh?: boolean },
  ): Observable<DashboardSnapshot> {
    const key = cacheKey(params);
    const background = options?.background ?? false;
    const forceRefresh = options?.forceRefresh ?? false;

    if (!forceRefresh) {
      const cached = this.cache.get(key);
      if (cached && this.isFresh(params)) {
        return of(cached.data);
      }
    }

    const existing = this.inFlight.get(key);
    if (existing && !forceRefresh) {
      return existing;
    }

    const hospitalId = params?.hospitalId;
    const frameworkId = params?.frameworkId;
    const endpoint = hospitalId
      ? `/dashboard/${hospitalId}/program-overview-snapshot`
      : '/dashboard/program-overview-snapshot';

    const queryParams: Record<string, string> = {};
    if (frameworkId) {
      queryParams['frameworkId'] = frameworkId;
    }

    const context = new HttpContext()
      .set(DASHBOARD_HTTP_CATEGORY, 'snapshot')
      .set(BACKGROUND_REQUEST, background)
      .set(SKIP_LOADING_OVERLAY, background);

    const request$ = this.get<DashboardSnapshot>(
      endpoint,
      Object.keys(queryParams).length ? queryParams : undefined,
      { context },
    ).pipe(
      tap((data) => {
        this.cache.set(key, { data, fetchedAt: Date.now() });
        this.lastUpdatedAt.set(Date.now());
        this.inFlight.delete(key);
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    this.inFlight.set(key, request$);
    return request$.pipe(
      tap({
        error: () => this.inFlight.delete(key),
      }),
    );
  }
}
