import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { catchError, finalize, map, tap, timeout } from 'rxjs/operators';
import { BaseApiService } from './base-api.service';
import { LoggerService } from '../../core/services/logger.service';
import { DepartmentLookup, FrameworkLookup } from '../models/lookup.models';
import { sortFrameworkLookupsByCode } from '../utils/framework-code-sort';

@Injectable({
  providedIn: 'root',
})
export class CommonLookupService extends BaseApiService {
  private departmentsCache: DepartmentLookup[] | null = null;
  private inFlightDepartmentsRequest$: Observable<DepartmentLookup[]> | null = null;
  private frameworksCache: FrameworkLookup[] | null = null;
  private inFlightFrameworksRequest$: Observable<FrameworkLookup[]> | null = null;

  constructor(private readonly logger: LoggerService) {
    super();
  }

  /** Clears in-memory lookup caches (call on logout / session end). */
  clearCaches(): void {
    this.departmentsCache = null;
    this.frameworksCache = null;
    this.inFlightDepartmentsRequest$ = null;
    this.inFlightFrameworksRequest$ = null;
  }

  loadDepartments(forceReload = false): Observable<DepartmentLookup[]> {
    if (!forceReload && this.departmentsCache) {
      return of(this.departmentsCache);
    }

    if (!forceReload && this.inFlightDepartmentsRequest$) {
      return this.inFlightDepartmentsRequest$;
    }

    this.logger.debug('Loading master departments', { endpoint: '/common/master-departments' });
    console.log('[CommonLookupService] Requesting departments from /common/master-departments');

    this.inFlightDepartmentsRequest$ = this.get<unknown>('/common/master-departments').pipe(
      timeout(15000),
      map((res): DepartmentLookup[] => {
        console.log('[CommonLookupService] Raw response received:', res);
        // Normally the ApiResponseInterceptor unwraps the envelope to `data: Department[]`.
        // This normalization makes the integration resilient even if interceptors are bypassed/misconfigured.
        const maybeArray = Array.isArray(res) ? res : (res as any)?.data;
        console.log('[CommonLookupService] Candidate array for mapping:', maybeArray);
        if (!Array.isArray(maybeArray)) {
          console.log('[CommonLookupService] Candidate is not an array; returning []');
          return [];
        }

        const normalized = maybeArray
          .map((item) => this.normalizeDepartment(item))
          .filter((item): item is DepartmentLookup => !!item);
        console.log('[CommonLookupService] Normalized departments:', normalized);
        return normalized;
      }),
      map((departments) => {
        // If backend provides displayOrder, keep UX stable by sorting on it.
        return [...departments].sort((a, b) => {
          const ao = a.displayOrder ?? Number.POSITIVE_INFINITY;
          const bo = b.displayOrder ?? Number.POSITIVE_INFINITY;
          return ao - bo;
        });
      }),
      tap((departments) => {
        this.logger.debug('Master departments mapped for UI', { count: departments.length });
        console.log('[CommonLookupService] Final departments count:', departments.length);
        this.departmentsCache = departments ?? [];
      }),
      catchError((error) => {
        if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
          this.logger.debug('Departments request not authorized', { status: error.status });
        } else {
          this.logger.error('Failed to load departments', error);
        }
        console.error('[CommonLookupService] Departments fetch failed:', error);
        return throwError(() => error);
      }),
      finalize(() => {
        this.inFlightDepartmentsRequest$ = null;
      }),
    );

    return this.inFlightDepartmentsRequest$;
  }

  refreshDepartments(): Observable<DepartmentLookup[]> {
    this.departmentsCache = null;
    this.inFlightDepartmentsRequest$ = null;
    return this.loadDepartments(true);
  }

  loadFrameworks(forceReload = false): Observable<FrameworkLookup[]> {
    if (!forceReload && this.frameworksCache) {
      return of(this.frameworksCache);
    }

    if (!forceReload && this.inFlightFrameworksRequest$) {
      return this.inFlightFrameworksRequest$;
    }

    this.logger.debug('Loading compliance frameworks', { endpoint: '/frameworks/all-frameworks' });

    this.inFlightFrameworksRequest$ = this.get<unknown>('/frameworks/all-frameworks').pipe(
      timeout(15000),
      map((res): FrameworkLookup[] => {
        const maybeArray = Array.isArray(res) ? res : (res as { data?: unknown })?.data;
        if (!Array.isArray(maybeArray)) {
          return [];
        }
        const normalized = maybeArray
          .map((item) => this.normalizeFramework(item))
          .filter((item): item is FrameworkLookup => !!item);
        return sortFrameworkLookupsByCode(normalized);
      }),
      tap((frameworks) => {
        this.logger.debug('Frameworks mapped for UI', { count: frameworks.length });
        this.frameworksCache = frameworks ?? [];
      }),
      catchError((error) => {
        if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
          this.logger.debug('Frameworks request not authorized', { status: error.status });
        } else {
          this.logger.error('Failed to load frameworks', error);
        }
        return throwError(() => error);
      }),
      finalize(() => {
        this.inFlightFrameworksRequest$ = null;
      }),
    );

    return this.inFlightFrameworksRequest$;
  }

  refreshFrameworks(): Observable<FrameworkLookup[]> {
    this.frameworksCache = null;
    this.inFlightFrameworksRequest$ = null;
    return this.loadFrameworks(true);
  }

  private normalizeFramework(raw: unknown): FrameworkLookup | null {
    if (!raw || typeof raw !== 'object') {
      return null;
    }
    const item = raw as Record<string, unknown>;
    const platformId = (item['platformId'] ??
      item['PlatformId'] ??
      item['frameworkId'] ??
      item['FrameworkId']) as string | undefined;
    const name = (item['name'] ?? item['Name']) as string | undefined;
    const code = (item['code'] ?? item['Code']) as string | undefined;
    const authorityFromApi = (item['authority'] ?? item['Authority']) as string | undefined;
    const iconKey = (item['iconKey'] ?? item['IconKey'] ?? item['icon'] ?? item['Icon']) as
      | string
      | undefined;
    const isActive = (item['isActive'] ?? item['IsActive']) as boolean | undefined;
    if (!platformId || !name) {
      return null;
    }
    const codeTrim = (code ?? '').trim();
    if (!codeTrim) {
      return null;
    }
    if (isActive === false) {
      return null;
    }
    const authorityTrim = authorityFromApi?.trim() ?? '';
    const authority = authorityTrim || codeTrim;
    return {
      platformId,
      name,
      authority,
      code: codeTrim,
      iconKey: iconKey?.trim() || undefined,
      isActive,
    };
  }

  private normalizeDepartment(raw: unknown): DepartmentLookup | null {
    if (!raw || typeof raw !== 'object') {
      console.log('[CommonLookupService] Skipping non-object department row:', raw);
      return null;
    }

    const item = raw as Record<string, unknown>;
    const id = (item['id'] ?? item['Id']) as string | undefined;
    const name = (item['name'] ?? item['Name']) as string | undefined;

    if (!id || !name) {
      console.log('[CommonLookupService] Skipping invalid department row (missing id/name):', item);
      return null;
    }

    return {
      id,
      name,
      code: (item['code'] ?? item['Code']) as string | undefined,
      category: (item['category'] ?? item['Category']) as string | undefined,
      displayOrder: (item['displayOrder'] ?? item['DisplayOrder']) as number | null | undefined,
    };
  }
}

