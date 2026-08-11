import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface ResponsibilityItem {
  id: string;
  responsibilityCode: string;
  responsibilityTypeValueId: string;
  governedResourceType: string;
  governedResourceId?: string;
  requiresPrimaryOwner: boolean;
}

export interface CommitteeItem {
  id: string;
  committeeCode: string;
  committeeName: string;
  status: string;
  chairpersonAssigneeId?: string;
  secretaryAssigneeId?: string;
}

export interface ApprovalChainItem {
  id: string;
  chainCode: string;
  chainName: string;
  status: string;
}

export interface TimelineItem {
  id: string;
  eventType: string;
  summary: string;
  occurredAt: string;
  actorUserId?: string;
  payloadJson?: string;
}

export interface GovernanceAnalytics {
  activeResponsibilities: number;
  activeCommittees: number;
  publishedApprovalChains: number;
  activeAssignments: number;
  scheduledMeetings?: number;
  completedMeetings?: number;
  meetingsPendingMinutesApproval?: number;
  openActionItems?: number;
  actionItemsWithLinkedTasks?: number;
  managementReviewCommittees?: number;
}

export interface GovernanceKpis {
  activeCommittees: number;
  openActionItems: number;
  meetingsThisMonth: number;
  pendingMinutesApprovals: number;
  documentsEffective: number;
  trainingOverdue: number;
  openIncidents: number;
  openRisks: number;
  sourceNotes?: string;
}

export interface PagedResponsibilities<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface LookupValue {
  id: string;
  code: string;
  name: string;
}

export interface AgendaTemplate {
  id: string;
  code: string;
  name: string;
  description?: string;
  sections: string[];
  isManagementReview: boolean;
}

export interface CommitteeMeeting {
  id: string;
  committeeId: string;
  scheduledAt: string;
  meetingTypeValueId: string;
  agenda?: string;
  agendaTemplateCode?: string;
  status: string;
  minutes?: string;
  attendanceJson?: string;
  minutesApproved: boolean;
  minutesApprovedAt?: string;
  completedAt?: string;
  decisions: CommitteeDecision[];
  actionItems: CommitteeActionItem[];
}

export interface CommitteeDecision {
  id: string;
  decisionTypeValueId: string;
  summary: string;
  deficiencyId?: string;
  riskId?: string;
  taskId?: string;
  ownerAssigneeId?: string;
  implementationStatus?: string;
  recordedAt: string;
}

export interface CommitteeActionItem {
  id: string;
  committeeMeetingId: string;
  title: string;
  description?: string;
  ownerAssigneeId?: string;
  dueDate?: string;
  status: string;
  taskId?: string;
  decisionId?: string;
  createdAt: string;
}

export interface ManagementReviewAgenda {
  committeeId?: string;
  templateCode: string;
  agendaSections: string[];
  incidents: { title: string; highlights: string[]; isPlaceholder: boolean };
  capas: { title: string; highlights: string[]; isPlaceholder: boolean };
  risks: { title: string; highlights: string[]; isPlaceholder: boolean };
  audits: { title: string; highlights: string[]; isPlaceholder: boolean };
  notes: string;
}

@Injectable({ providedIn: 'root' })
export class GovernanceApiService extends BaseApiService {
  listResponsibilities(page = 1, pageSize = 20): Observable<PagedResponsibilities<ResponsibilityItem>> {
    return this.get<PagedResponsibilities<ResponsibilityItem>>('/governance/responsibilities', { page, pageSize });
  }

  getResponsibility(id: string): Observable<ResponsibilityItem> {
    return this.get<ResponsibilityItem>(`/governance/responsibilities/${id}`);
  }

  createResponsibility(body: Record<string, unknown>): Observable<ResponsibilityItem> {
    return this.post<ResponsibilityItem>('/governance/responsibilities', body);
  }

  updateResponsibility(id: string, body: Record<string, unknown>): Observable<unknown> {
    return this.put(`/governance/responsibilities/${id}`, body);
  }

  deleteResponsibility(id: string): Observable<unknown> {
    return this.delete(`/governance/responsibilities/${id}`);
  }

  assignResponsibility(id: string, body: Record<string, unknown>): Observable<unknown> {
    return this.post(`/governance/responsibilities/${id}/assign`, body);
  }

  listCommittees(): Observable<CommitteeItem[]> {
    return this.get<CommitteeItem[]>('/committees');
  }

  getCommittee(id: string): Observable<CommitteeItem> {
    return this.get<CommitteeItem>(`/committees/${id}`);
  }

  createCommittee(body: Record<string, unknown>): Observable<CommitteeItem> {
    return this.post<CommitteeItem>('/committees', body);
  }

  updateCommittee(id: string, body: Record<string, unknown>): Observable<unknown> {
    return this.put(`/committees/${id}`, body);
  }

  addCommitteeMember(committeeId: string, body: Record<string, unknown>): Observable<unknown> {
    return this.post(`/committees/${committeeId}/members`, body);
  }

  listMeetings(committeeId: string): Observable<CommitteeMeeting[]> {
    return this.get<CommitteeMeeting[]>(`/committees/${committeeId}/meetings`);
  }

  getMeeting(committeeId: string, meetingId: string): Observable<CommitteeMeeting> {
    return this.get<CommitteeMeeting>(`/committees/${committeeId}/meetings/${meetingId}`);
  }

  scheduleMeeting(committeeId: string, body: Record<string, unknown>): Observable<CommitteeMeeting> {
    return this.post<CommitteeMeeting>(`/committees/${committeeId}/meetings`, body);
  }

  completeMeeting(committeeId: string, meetingId: string, body: Record<string, unknown>): Observable<unknown> {
    return this.post(`/committees/${committeeId}/meetings/${meetingId}/complete`, body);
  }

  recordAttendance(committeeId: string, meetingId: string, body: Record<string, unknown>): Observable<unknown> {
    return this.post(`/committees/${committeeId}/meetings/${meetingId}/attendance`, body);
  }

  approveMinutes(committeeId: string, meetingId: string): Observable<unknown> {
    return this.post(`/committees/${committeeId}/meetings/${meetingId}/approve-minutes`, {});
  }

  recordDecision(committeeId: string, meetingId: string, body: Record<string, unknown>): Observable<CommitteeDecision> {
    return this.post<CommitteeDecision>(`/committees/${committeeId}/meetings/${meetingId}/decisions`, body);
  }

  createActionItem(committeeId: string, meetingId: string, body: Record<string, unknown>): Observable<CommitteeActionItem> {
    return this.post<CommitteeActionItem>(`/committees/${committeeId}/meetings/${meetingId}/actions`, body);
  }

  linkActionTask(committeeId: string, meetingId: string, actionItemId: string, taskId: string): Observable<unknown> {
    return this.post(`/committees/${committeeId}/meetings/${meetingId}/actions/${actionItemId}/link-task`, { taskId });
  }

  updateActionStatus(committeeId: string, meetingId: string, actionItemId: string, status: string): Observable<unknown> {
    return this.patch(`/committees/${committeeId}/meetings/${meetingId}/actions/${actionItemId}/status`, { status });
  }

  listApprovalChains(): Observable<ApprovalChainItem[]> {
    return this.get<ApprovalChainItem[]>('/approval-chains');
  }

  createApprovalChain(body: Record<string, unknown>): Observable<ApprovalChainItem> {
    return this.post<ApprovalChainItem>('/approval-chains', body);
  }

  publishApprovalChain(id: string): Observable<unknown> {
    return this.post(`/approval-chains/${id}/publish`, {});
  }

  listEscalationChains(): Observable<ApprovalChainItem[]> {
    return this.get<ApprovalChainItem[]>('/escalation-chains');
  }

  createEscalationChain(body: Record<string, unknown>): Observable<ApprovalChainItem> {
    return this.post<ApprovalChainItem>('/escalation-chains', body);
  }

  publishEscalationChain(id: string): Observable<unknown> {
    return this.post(`/escalation-chains/${id}/publish`, {});
  }

  getTimeline(resourceType?: string, resourceId?: string): Observable<TimelineItem[]> {
    return this.get<TimelineItem[]>('/governance/timeline', { resourceType, resourceId });
  }

  getAnalytics(): Observable<GovernanceAnalytics> {
    return this.get<GovernanceAnalytics>('/governance/analytics/summary');
  }

  getKpis(): Observable<GovernanceKpis> {
    return this.get<GovernanceKpis>('/analytics/governance');
  }

  getLookups(category: string): Observable<LookupValue[]> {
    return this.get<LookupValue[]>(`/governance/lookups/${category}`);
  }

  listAgendaTemplates(): Observable<AgendaTemplate[]> {
    return this.get<AgendaTemplate[]>('/governance/agenda-templates');
  }

  getManagementReviewAgenda(committeeId?: string): Observable<ManagementReviewAgenda> {
    return this.get<ManagementReviewAgenda>('/governance/management-review/agenda', { committeeId });
  }

  evaluateApproval(body: Record<string, unknown>): Observable<{ completed: boolean; approved: boolean; reason: string; currentStep?: number }> {
    return this.post('/approval/evaluate', body);
  }

  resolveEscalation(body: Record<string, unknown>): Observable<{ reason?: string }> {
    return this.post<{ reason?: string }>('/escalation/resolve', body);
  }
}
