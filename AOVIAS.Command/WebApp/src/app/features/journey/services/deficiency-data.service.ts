import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  DeficiencyListApiDto,
  DeficiencySummaryApiDto,
  DeficiencyByStageApiDto,
} from '../models/deficiency-api.model';

/**
 * HTTP client for Phase 5 Deficiency Engine APIs.
 * Base path: /api/v1/deficiencies (apiBaseUrl already includes /api/v1)
 */
@Injectable({ providedIn: 'root' })
export class DeficiencyDataService extends BaseApiService {

  private deficiencyPath = '/deficiencies';

  /** GET /api/v1/deficiencies?hospitalId=&frameworkId= */
  getDeficiencies(
    hospitalId: string,
    frameworkId: string,
    page = 1,
    pageSize = 20,
  ): Observable<DeficiencyListApiDto> {
    return this.get(
      `${this.deficiencyPath}?hospitalId=${hospitalId}&frameworkId=${frameworkId}&page=${page}&pageSize=${pageSize}`,
    );
  }

  /** GET /api/v1/deficiencies/summary?hospitalId=&frameworkId= */
  getDeficiencySummary(
    hospitalId: string,
    frameworkId: string,
  ): Observable<DeficiencySummaryApiDto> {
    return this.get(
      `${this.deficiencyPath}/summary?hospitalId=${hospitalId}&frameworkId=${frameworkId}`,
    );
  }

  /** GET /api/v1/deficiencies/by-stage?hospitalId=&frameworkId= */
  getDeficienciesByStage(
    hospitalId: string,
    frameworkId: string,
  ): Observable<DeficiencyByStageApiDto[]> {
    return this.get(
      `${this.deficiencyPath}/by-stage?hospitalId=${hospitalId}&frameworkId=${frameworkId}`,
    );
  }

  /**
   * POST /api/v1/deficiencies/bootstrap-from-onboarding
   * Applies saved onboarding assessment to obligations (idempotent). Call once before reads.
   */
  bootstrapFromOnboarding(
    organizationId: string,
    hospitalId: string,
    frameworkId: string,
  ): Observable<unknown> {
    return this.post(
      `${this.deficiencyPath}/bootstrap-from-onboarding?organizationId=${organizationId}&hospitalId=${hospitalId}&frameworkId=${frameworkId}`,
      {},
    );
  }

  /** PATCH /api/v1/deficiencies/{id}/status */
  patchDeficiencyStatus(id: string, body: { status: string }): Observable<unknown> {
    return this.patch(`${this.deficiencyPath}/${id}/status`, body);
  }

  /** POST /api/v1/deficiencies/{id}/assign */
  assignDeficiency(
    id: string,
    body: { assignedTo: string; dueDate?: string },
  ): Observable<unknown> {
    return this.post(`${this.deficiencyPath}/${id}/assign`, body);
  }
}
