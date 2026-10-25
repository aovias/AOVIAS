import { Injectable, signal } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable, of, shareReplay, tap, throwError } from 'rxjs';
import { BaseApiService } from '../../shared/services/base-api.service';
import { DASHBOARD_HTTP_CATEGORY, SKIP_LOADING_OVERLAY } from '../performance/http-context.tokens';
import {
  DashboardExperienceQueryParams,
  DashboardExperienceResult,
} from '../../features/dashboard/models/dashboard-experience.model';

interface CacheEntry {
  data: DashboardExperienceResult;
  fetchedAt: number;
}

const DEFAULT_TTL_MS = 60_000;

function cacheKey(params?: DashboardExperienceQueryParams): string {
  return `${params?.hospitalId ?? ''}|${params?.frameworkId ?? ''}`;
}

@Injectable({ providedIn: 'root' })
export class DashboardExperienceStore extends BaseApiService {
  private readonly cache = new Map<string, CacheEntry>();
  private readonly inFlight = new Map<string, Observable<DashboardExperienceResult>>();

  getCached(params?: DashboardExperienceQueryParams): DashboardExperienceResult | null {
    return this.cache.get(cacheKey(params))?.data ?? null;
  }

  isFresh(params?: DashboardExperienceQueryParams, ttlMs: number = DEFAULT_TTL_MS): boolean {
    const entry = this.cache.get(cacheKey(params));
    if (!entry) {
      return false;
    }
    return Date.now() - entry.fetchedAt < ttlMs;
  }

  invalidate(params?: DashboardExperienceQueryParams): void {
    if (params) {
      const key = cacheKey(params);
      this.cache.delete(key);
      this.inFlight.delete(key);
      return;
    }
    this.cache.clear();
    this.inFlight.clear();
  }

  getExperience(
    params?: DashboardExperienceQueryParams,
    options?: { forceRefresh?: boolean },
  ): Observable<DashboardExperienceResult> {
    const key = cacheKey(params);
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

    const query: Record<string, string> = {};
    if (params?.hospitalId) {
      query['hospitalId'] = params.hospitalId;
    }
    if (params?.frameworkId) {
      query['frameworkId'] = params.frameworkId;
    }

    const context = new HttpContext()
      .set(SKIP_LOADING_OVERLAY, true)
      .set(DASHBOARD_HTTP_CATEGORY, 'experience');

    const request$ = this.get<DashboardExperienceResult>('/dashboard/experience', query, {
      context,
    }).pipe(
      tap((data) => {
        this.cache.set(key, { data, fetchedAt: Date.now() });
        this.inFlight.delete(key);
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    this.inFlight.set(key, request$);
    return request$.pipe(
      tap({
        error: () => this.inFlight.delete(key),
      }),
    );
  }
}
