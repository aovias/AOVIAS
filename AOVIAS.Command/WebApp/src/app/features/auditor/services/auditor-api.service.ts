import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  AuditSessionDetail,
  AuditorAssessmentContext,
  AuditorDashboard,
  AuditorSessionReport,
  AuditorSessionSummary,
  AuditObservationItem,
  AuditCommentItem,
  CreateAuditCommentRequest,
  CreateAuditObservationRequest,
  CreateAuditSessionRequest,
  EvidenceTraceability,
} from '../models/auditor-workspace.model';

@Injectable({ providedIn: 'root' })
export class AuditorApiService extends BaseApiService {
  private readonly base = '/auditor';

  getDashboard(): Observable<AuditorDashboard> {
    return this.get(`${this.base}/dashboard`);
  }

  getSessions(): Observable<AuditorSessionSummary[]> {
    return this.get(`${this.base}/sessions`);
  }

  getSession(id: string): Observable<AuditSessionDetail> {
    return this.get(`${this.base}/session/${id}`);
  }

  createSession(payload: CreateAuditSessionRequest): Observable<AuditSessionDetail> {
    return this.post(`${this.base}/sessions`, payload);
  }

  createObservation(payload: CreateAuditObservationRequest): Observable<AuditObservationItem> {
    return this.post(`${this.base}/observations`, payload);
  }

  createComment(payload: CreateAuditCommentRequest): Observable<AuditCommentItem> {
    return this.post(`${this.base}/comments`, payload);
  }

  getTraceability(objectiveElementId: string): Observable<EvidenceTraceability> {
    return this.get(`${this.base}/traceability/${objectiveElementId}`);
  }

  getReport(sessionId: string): Observable<AuditorSessionReport> {
    return this.get(`${this.base}/report/${sessionId}`);
  }

  getAssessmentContext(frameworkId?: string): Observable<AuditorAssessmentContext> {
    return this.get(`${this.base}/assessment-context`, frameworkId ? { frameworkId } : {});
  }

  closeSession(
    id: string,
    payload?: { overallResult?: string; summary?: string },
  ): Observable<AuditSessionDetail> {
    return this.post(`${this.base}/sessions/${id}/close`, payload ?? {});
  }
}
