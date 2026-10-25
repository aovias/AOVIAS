import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../../shared/services/base-api.service';
import type {
  CreateStructuredFindingRequest,
  FindingComplianceContextOption,
  FindingPromotionEligibility,
  FindingPromotionStatus,
  FindingsWorkspaceDashboard,
  FindingsWorkspaceDetail,
  FindingsWorkspaceList,
  FindingsWorkspaceQuery,
  PromoteFindingResult,
} from '../models/findings-workspace.model';
import type { AssignmentRoutingSuggestion } from '../../deficiency-command-center/models/deficiency-command-center.model';

@Injectable({ providedIn: 'root' })
export class FindingsWorkspaceService extends BaseApiService {
  private readonly base = '/findings/workspace';

  getDashboard(): Observable<FindingsWorkspaceDashboard> {
    return this.get(`${this.base}/dashboard`);
  }

  getWorkspace(query: FindingsWorkspaceQuery = {}): Observable<FindingsWorkspaceList> {
    return this.get(this.base, {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      sortDesc: query.sortDesc ?? true,
      ...(query.status ? { status: query.status } : {}),
      ...(query.severity ? { severity: query.severity } : {}),
      ...(query.classification ? { classification: query.classification } : {}),
      ...(query.source ? { source: query.source } : {}),
      ...(query.department ? { department: query.department } : {}),
      ...(query.promotionReadiness ? { promotionReadiness: query.promotionReadiness } : {}),
      ...(query.chapterCode ? { chapterCode: query.chapterCode } : {}),
      ...(query.standardCode ? { standardCode: query.standardCode } : {}),
      ...(query.objectiveElementId ? { objectiveElementId: query.objectiveElementId } : {}),
      ...(query.quickFilter ? { quickFilter: query.quickFilter } : {}),
      ...(query.search ? { search: query.search } : {}),
      ...(query.sortBy ? { sortBy: query.sortBy } : {}),
    });
  }

  getDetail(id: string): Observable<FindingsWorkspaceDetail> {
    return this.get(`${this.base}/${id}`);
  }

  searchComplianceContext(
    search?: string,
    chapterCode?: string,
    standardCode?: string,
    limit = 20,
  ): Observable<FindingComplianceContextOption[]> {
    return this.get(`${this.base}/compliance-context`, {
      limit,
      ...(search ? { search } : {}),
      ...(chapterCode ? { chapterCode } : {}),
      ...(standardCode ? { standardCode } : {}),
    });
  }

  createFinding(payload: CreateStructuredFindingRequest): Observable<unknown> {
    return this.post('/findings', payload);
  }

  patchStatus(id: string, status: string): Observable<unknown> {
    return this.patch(`/findings/${id}/status`, { status });
  }

  /**
   * DEF-M3-001 — classifies an existing finding. Only the objective element travels; the server
   * resolves standard and chapter from its hierarchy, exactly as the promotion rules do.
   */
  patchComplianceContext(id: string, objectiveElementId: string): Observable<unknown> {
    return this.patch(`/findings/${id}/compliance-context`, { objectiveElementId });
  }

  addComment(id: string, body: string): Observable<unknown> {
    return this.post(`/findings/${id}/comments`, { body });
  }

  addAttachment(
    id: string,
    payload: {
      fileName: string;
      fileUrl: string;
      contentType?: string;
      attachmentType: string;
      fileSizeBytes?: number;
    },
  ): Observable<unknown> {
    return this.post(`/findings/${id}/attachments`, payload);
  }

  getPromotionStatus(id: string): Observable<FindingPromotionStatus> {
    return this.get(`/findings/${id}/promotion-status`);
  }

  getEligibility(id: string): Observable<FindingPromotionEligibility> {
    return this.get(`/findings/${id}/eligibility`);
  }

  promoteToDeficiency(id: string): Observable<PromoteFindingResult> {
    return this.post(`/findings/${id}/promote`, {});
  }

  getAssignmentSuggestion(
    findingId: string,
    organizationId: string,
    hospitalId: string,
  ): Observable<AssignmentRoutingSuggestion> {
    return this.get(`/assignment/findings/${findingId}/suggestion`, { organizationId, hospitalId });
  }
}
