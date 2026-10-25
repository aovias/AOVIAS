import { Injectable, inject } from '@angular/core';
import { HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../shared/services/base-api.service';
import { EvidenceMultipartApiClient } from '../journey/services/evidence-multipart-api.client';
import { environment } from '../../../environments/environment';
import { SessionService } from '../../core/auth/session.service';
import type {
  PagedResult,
  TaskComment,
  TaskDetail,
  TaskEvidenceItem,
  TaskFilters,
  TaskHistoryEntry,
  TaskKanbanResponse,
  TaskListItem,
  TaskSummary,
} from './models';

@Injectable({ providedIn: 'root' })
export class TaskApiService extends BaseApiService {
  private readonly multipart = inject(EvidenceMultipartApiClient);
  private readonly session = inject(SessionService);

  getSummary(frameworkId: string, filters: TaskFilters): Observable<TaskSummary> {
    return this.get<TaskSummary>('/tasks/summary', this.filterParams(frameworkId, filters));
  }

  getTasksPaged(
    frameworkId: string,
    filters: TaskFilters,
    page: number,
    pageSize: number,
    view: 'table' | 'risk' | 'calendar' | 'department' = 'table',
  ): Observable<PagedResult<TaskListItem>> {
    const params: Record<string, string | number> = {
      ...this.filterParams(frameworkId, filters),
      page,
      pageSize,
      view,
    };
    return this.get<PagedResult<TaskListItem>>('/tasks', params);
  }

  getKanban(frameworkId: string, filters: TaskFilters): Observable<TaskKanbanResponse> {
    const params: Record<string, string | number> = {
      ...this.filterParams(frameworkId, filters),
      view: 'kanban',
    };
    return this.get<TaskKanbanResponse>('/tasks', params);
  }

  getTaskDetail(taskId: string, frameworkId: string): Observable<TaskDetail> {
    return this.get<TaskDetail>(`/tasks/${encodeURIComponent(taskId)}`, { frameworkId });
  }

  getEvidences(taskId: string, frameworkId: string, page: number, pageSize: number): Observable<PagedResult<TaskEvidenceItem>> {
    return this.get<PagedResult<TaskEvidenceItem>>(`/tasks/${encodeURIComponent(taskId)}/evidences`, {
      frameworkId,
      page,
      pageSize,
    });
  }

  getComments(taskId: string, frameworkId: string, page: number, pageSize: number): Observable<PagedResult<TaskComment>> {
    return this.get<PagedResult<TaskComment>>(`/tasks/${encodeURIComponent(taskId)}/comments`, {
      frameworkId,
      page,
      pageSize,
    });
  }

  getHistory(taskId: string, frameworkId: string, page: number, pageSize: number): Observable<PagedResult<TaskHistoryEntry>> {
    return this.get<PagedResult<TaskHistoryEntry>>(`/tasks/${encodeURIComponent(taskId)}/history`, {
      frameworkId,
      page,
      pageSize,
    });
  }

  patchTask(
    taskId: string,
    frameworkId: string,
    body: Record<string, unknown>,
    ifMatchRowVersion: string,
  ): Observable<TaskDetail> {
    const headers = new HttpHeaders({ 'If-Match': ifMatchRowVersion });
    return this.patch<TaskDetail>(`/tasks/${encodeURIComponent(taskId)}`, body, headers, { frameworkId });
  }

  completeTask(taskId: string, frameworkId: string, ifMatchRowVersion: string): Observable<{ taskId: string }> {
    const url = `${this.apiBaseUrl}/tasks/${encodeURIComponent(taskId)}/complete`;
    let params = new HttpParams().set('frameworkId', frameworkId);
    return this.http.put<{ taskId: string }>(url, null, {
      params,
      headers: new HttpHeaders({ 'If-Match': ifMatchRowVersion }),
    });
  }

  addComment(taskId: string, frameworkId: string, body: string): Observable<TaskComment> {
    return this.post<TaskComment>(
      `/tasks/${encodeURIComponent(taskId)}/comments?frameworkId=${encodeURIComponent(frameworkId)}`,
      { body },
    );
  }

  uploadEvidence(taskId: string, frameworkId: string, file: File): Observable<{ evidenceId: string }> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    fd.append('evidenceType', 'document');
    const url = `${environment.apiBaseUrl}/tasks/${encodeURIComponent(taskId)}/evidences?frameworkId=${encodeURIComponent(frameworkId)}`;
    return this.multipart.postFormData<{ evidenceId: string }>(url, fd);
  }

  private filterParams(frameworkId: string, f: TaskFilters): Record<string, string | number | boolean> {
    const p: Record<string, string | number | boolean> = { frameworkId };
    if (f.status) p['status'] = f.status;
    if (f.departmentId) p['departmentId'] = f.departmentId;
    if (f.chapterId) p['chapterId'] = f.chapterId;
    if (f.riskLevel) p['riskLevel'] = f.riskLevel;
    if (f.search) p['search'] = f.search;

    const q = f.quick;
    if (q === 'myTasks') {
      const uid = this.session.getUserId();
      if (uid) p['ownerId'] = uid;
    } else if (f.ownerId) {
      p['ownerId'] = f.ownerId;
    }

    if (q === 'highPriority') {
      p['priority'] = 2;
    } else if (f.priority != null) {
      p['priority'] = f.priority;
    }

    if (q === 'overdue') p['overdueOnly'] = true;
    if (q === 'team') p['teamOnly'] = true;
    if (q === 'dueThisWeek') p['dueThisWeek'] = true;
    if (q === 'highRisk') p['riskLevel'] = 'High';
    if (q === 'focus') p['focus'] = true;
    if (q === 'criticalDeficiencies') {
      p['deficiencySeverity'] = 'Critical';
      p['deficiencyLinkedOnly'] = true;
    }
    if (q === 'evidenceMissing') p['obligationEvidenceStatus'] = 'Missing';
    if (q === 'verificationRejected') p['obligationVerificationStatus'] = 'Rejected';
    if (q === 'coreStage') p['maturityStage'] = 'Core';
    if (q === 'commitmentStage') p['maturityStage'] = 'Commitment';

    if (f.focus) p['focus'] = true;

    if (f.deficiencySeverity) p['deficiencySeverity'] = f.deficiencySeverity;
    if (f.deficiencyType) p['deficiencyType'] = f.deficiencyType;
    if (f.maturityStage) p['maturityStage'] = f.maturityStage;
    if (f.obligationEvidenceStatus) p['obligationEvidenceStatus'] = f.obligationEvidenceStatus;
    if (f.obligationVerificationStatus) {
      p['obligationVerificationStatus'] = f.obligationVerificationStatus;
    }
    if (f.deficiencyLinkedOnly) p['deficiencyLinkedOnly'] = true;
    if (f.category) p['category'] = f.category;
    if (f.taskKind) p['taskKind'] = f.taskKind;
    if (f.dueFrom) p['dueFrom'] = f.dueFrom;
    if (f.dueTo) p['dueTo'] = f.dueTo;

    return p;
  }

  reassignTask(
    taskId: string,
    frameworkId: string,
    assignedToUserId: string,
    ifMatchRowVersion: string,
    reason?: string,
  ): Observable<TaskDetail> {
    const headers = new HttpHeaders({ 'If-Match': ifMatchRowVersion });
    return this.post<TaskDetail>(
      `/tasks/${encodeURIComponent(taskId)}/reassign?frameworkId=${encodeURIComponent(frameworkId)}`,
      { assignedToUserId, reason },
      headers,
    );
  }
}
