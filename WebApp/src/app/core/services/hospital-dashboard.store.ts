import { Injectable, inject } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable, of, shareReplay, tap } from 'rxjs';
import { BaseApiService } from '../../shared/services/base-api.service';
import { HospitalDashboardDto } from '../../features/dashboard/models/hospital-dashboard.model';
import {
  BACKGROUND_REQUEST,
  DASHBOARD_HTTP_CATEGORY,
  SKIP_LOADING_OVERLAY,
} from '../performance/http-context.tokens';
import { environment } from '../../../environments/environment';

interface CacheEntry {
  data: HospitalDashboardDto;
  fetchedAt: number;
}

@Injectable({ providedIn: 'root' })
export class HospitalDashboardStore extends BaseApiService {
  private readonly cache = new Map<string, CacheEntry>();
  private readonly inFlight = new Map<string, Observable<HospitalDashboardDto>>();

  getCached(hospitalId: string): HospitalDashboardDto | null {
    return this.cache.get(hospitalId)?.data ?? null;
  }

  isFresh(hospitalId: string, ttlMs: number = environment.dashboardSnapshotCacheTtlMs): boolean {
    const entry = this.cache.get(hospitalId);
    if (!entry) {
      return false;
    }
    return Date.now() - entry.fetchedAt < ttlMs;
  }

  getHospitalDashboard(
    hospitalId: string,
    options?: { background?: boolean; forceRefresh?: boolean },
  ): Observable<HospitalDashboardDto> {
    const background = options?.background ?? true;
    const forceRefresh = options?.forceRefresh ?? false;

    if (!forceRefresh) {
      const cached = this.cache.get(hospitalId);
      if (cached && this.isFresh(hospitalId)) {
        return of(cached.data);
      }
    }

    const existing = this.inFlight.get(hospitalId);
    if (existing && !forceRefresh) {
      return existing;
    }

    const context = new HttpContext()
      .set(DASHBOARD_HTTP_CATEGORY, 'hospital-dashboard')
      .set(BACKGROUND_REQUEST, background)
      .set(SKIP_LOADING_OVERLAY, true);

    const request$ = this.get<HospitalDashboardDto>(
      '/dashboard/hospital',
      { hospitalId },
      { context },
    ).pipe(
      tap((data) => {
        this.cache.set(hospitalId, { data, fetchedAt: Date.now() });
        this.inFlight.delete(hospitalId);
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    this.inFlight.set(hospitalId, request$);
    return request$.pipe(tap({ error: () => this.inFlight.delete(hospitalId) }));
  }
}
