import { Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { BaseApiService } from '../../../../shared/services/base-api.service';

import type {
  DeficiencyCommandCenterDashboard,
  DeficiencyWorkspaceDetail,
  DeficiencyWorkspaceList,
  DeficiencyWorkspaceQuery,
  PatchDeficiencyRcaRequest,
  PatchDeficiencyRemediationRequest,
  PatchDeficiencyCorrectiveActionRequest,
  PatchDeficiencyPreventiveActionRequest,
  PatchDeficiencyEffectivenessReviewRequest,
  DeficiencyCapaModel,
  AssignmentRoutingSuggestion,
  AssignmentRoutingApplyResult,
} from '../models/deficiency-command-center.model';

@Injectable({ providedIn: 'root' })
export class DeficiencyCommandCenterService extends BaseApiService {
  private readonly base = '/deficiencies/command-center';

  getDashboard(
    hospitalId: string,
    frameworkId: string,
  ): Observable<DeficiencyCommandCenterDashboard> {
    return this.get(`${this.base}/dashboard`, { hospitalId, frameworkId });
  }

  getWorkspace(
    hospitalId: string,

    frameworkId: string,

    query: DeficiencyWorkspaceQuery = {},
  ): Observable<DeficiencyWorkspaceList> {
    return this.get(`${this.base}/workspace`, {
      hospitalId,

      frameworkId,

      page: query.page ?? 1,

      pageSize: query.pageSize ?? 25,

      sortDesc: query.sortDesc ?? true,

      ...(query.status ? { status: query.status } : {}),

      ...(query.severity ? { severity: query.severity } : {}),

      ...(query.maturityLevel ? { maturityLevel: query.maturityLevel } : {}),

      ...(query.search ? { search: query.search } : {}),

      ...(query.objectiveElementId ? { objectiveElementId: query.objectiveElementId } : {}),

      ...(query.sortBy ? { sortBy: query.sortBy } : {}),
    });
  }

  getDetail(id: string): Observable<DeficiencyWorkspaceDetail> {
    return this.get(`${this.base}/workspace/${id}`);
  }

  patchRootCause(id: string, rootCauseCategory: string): Observable<unknown> {
    return this.patch(`/deficiencies/${id}/root-cause`, { rootCauseCategory });
  }

  patchRca(id: string, body: PatchDeficiencyRcaRequest): Observable<unknown> {
    return this.patch(`/deficiencies/${id}/rca`, body);
  }

  patchRemediation(id: string, body: PatchDeficiencyRemediationRequest): Observable<unknown> {
    return this.patch(`/deficiencies/${id}/remediation`, body);
  }

  patchCorrectiveAction(
    id: string,
    body: PatchDeficiencyCorrectiveActionRequest,
  ): Observable<unknown> {
    return this.patch(`/deficiencies/${id}/corrective-action`, body);
  }

  patchPreventiveAction(
    id: string,
    body: PatchDeficiencyPreventiveActionRequest,
  ): Observable<unknown> {
    return this.patch(`/deficiencies/${id}/preventive-action`, body);
  }

  patchEffectivenessReview(
    id: string,
    body: PatchDeficiencyEffectivenessReviewRequest,
  ): Observable<unknown> {
    return this.patch(`/deficiencies/${id}/effectiveness-review`, body);
  }

  getCapa(id: string): Observable<DeficiencyCapaModel> {
    return this.get(`/deficiencies/${id}/capa`);
  }

  getAssignmentSuggestion(deficiencyId: string): Observable<AssignmentRoutingSuggestion> {
    return this.get(`/assignment/deficiencies/${deficiencyId}/suggestion`);
  }

  autoAssign(deficiencyId: string, force = false): Observable<AssignmentRoutingApplyResult> {
    return this.post(`/assignment/deficiencies/${deficiencyId}/auto-assign?force=${force}`, {});
  }

  acknowledgeEscalation(deficiencyId: string, escalationId: string): Observable<unknown> {
    return this.post(`/sla/deficiencies/${deficiencyId}/acknowledge-escalation`, { escalationId });
  }

  acceptOwnership(id: string, acceptedBy?: string): Observable<unknown> {
    return this.post(`/deficiencies/${id}/accept`, acceptedBy ? { acceptedBy } : {});
  }

  rejectOwnership(id: string, reason?: string, rejectedBy?: string): Observable<unknown> {
    return this.post(`/deficiencies/${id}/reject-ownership`, {
      ...(reason ? { reason } : {}),
      ...(rejectedBy ? { rejectedBy } : {}),
    });
  }
}
