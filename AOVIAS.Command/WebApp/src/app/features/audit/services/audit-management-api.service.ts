import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';

import type { AuditTypeCode } from '../../../core/configuration/configuration-catalog';

/** The wire models name the configured set rather than restating it. */
export type AuditType = AuditTypeCode;

export interface ScheduledAuditDto {
  id: string;
  organizationId: string;
  hospitalId: string;
  frameworkId: string;
  complianceJourneyId?: string | null;
  title: string;
  scheduledDate: string;
  status: string;
  auditorName?: string | null;
  result?: string | null;
  programYear?: number | null;
  programMonth?: number | null;
  auditType?: AuditType | null;
  auditTypeLabel?: string | null;
  programName?: string | null;
}

export interface CreateScheduledAuditRequest {
  hospitalId?: string;
  frameworkId: string;
  title: string;
  scheduledDate: string;
  auditorName?: string;
  programYear?: number;
  programMonth?: number;
  auditType?: AuditType;
  programName?: string;
}

export interface AuditSimulationResultDto {
  majorNCCount: number;
  minorNCCount: number;
  observationCount: number;
  ofiCount: number;
  totalNonConformances: number;
  score: number;
  totalClauses: number;
  compliantClauses: number;
  openTasks: number;
  highRiskChapters: Array<{
    chapterId: string;
    chapterCode: string;
    chapterName: string;
    completionPercentage: number;
    totalObjectiveElements: number;
    completedObjectiveElements: number;
  }>;
}

@Injectable({ providedIn: 'root' })
export class AuditManagementApiService extends BaseApiService {
  private readonly context = inject(ComplianceContextService);
  private readonly scheduleBase = '/audit-schedule';
  private readonly auditorBase = '/auditor';

  listScheduled(params?: {
    year?: number;
    month?: number;
    hospitalId?: string;
    frameworkId?: string;
    from?: string;
    to?: string;
  }): Observable<ScheduledAuditDto[]> {
    return this.get(this.scheduleBase, this.withContext(params));
  }

  getCalendar(params?: {
    year?: number;
    month?: number;
    hospitalId?: string;
    frameworkId?: string;
    from?: string;
    to?: string;
  }): Observable<ScheduledAuditDto[]> {
    return this.get(`${this.scheduleBase}/calendar`, this.withContext(params));
  }

  createScheduled(payload: CreateScheduledAuditRequest): Observable<ScheduledAuditDto> {
    const hospitalId = payload.hospitalId || this.context.getHospitalId() || undefined;
    const frameworkId = payload.frameworkId || this.context.getFrameworkId() || '';
    return this.post(this.scheduleBase, { ...payload, hospitalId, frameworkId });
  }

  runSimulation(payload?: {
    hospitalId?: string;
    frameworkId?: string;
  }): Observable<AuditSimulationResultDto> {
    return this.post(`${this.auditorBase}/simulation`, {
      hospitalId: payload?.hospitalId || this.context.getHospitalId() || undefined,
      frameworkId: payload?.frameworkId || this.context.getFrameworkId() || undefined,
    });
  }

  private withContext(
    params?: Record<string, string | number | undefined>,
  ): Record<string, string | number> {
    const hospitalId = params?.['hospitalId'] || this.context.getHospitalId() || undefined;
    const frameworkId = params?.['frameworkId'] || this.context.getFrameworkId() || undefined;
    const out: Record<string, string | number> = {};
    for (const [k, v] of Object.entries(params ?? {})) {
      if (v !== undefined && v !== null && v !== '') out[k] = v;
    }
    if (hospitalId) out['hospitalId'] = hospitalId;
    if (frameworkId) out['frameworkId'] = frameworkId;
    return out;
  }
}
