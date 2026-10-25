import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface AIProfileItem {
  id: string;
  code: string;
  name: string;
  description: string;
  moduleCode: string;
  status: string;
  version: number;
  copilotPersonaKey?: string;
  platformId?: string;
}

export interface AIAutomationPolicyItem {
  id: string;
  profileId: string;
  name: string;
  mode: string;
  confidenceThreshold: number;
  riskLevel: string;
}

export interface AIPromptItem {
  id: string;
  code: string;
  name: string;
  layerType: string;
  status: string;
  currentVersion: number;
}

export interface AIExecutionItem {
  id: string;
  profileId?: string;
  useCase: string;
  modelProvider: string;
  modelName?: string;
  latencyMs?: number;
  confidence?: number;
  outcome: string;
  traceId: string;
  occurredAtUtc: string;
}

export interface AIAnalytics {
  totalExecutions: number;
  totalTokens: number;
  estimatedCost: number;
  avgLatencyMs: number;
  acceptanceRate: number;
  automationRate: number;
}

export interface AIApprovalItem {
  id: string;
  executionAuditId: string;
  status: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AiIdentityApiService extends BaseApiService {
  listProfiles(): Observable<AIProfileItem[]> {
    return this.get<AIProfileItem[]>('/ai/profiles');
  }

  listPolicies(profileId?: string): Observable<AIAutomationPolicyItem[]> {
    return this.get<AIAutomationPolicyItem[]>('/ai/policies', profileId ? { profileId } : undefined);
  }

  listPrompts(): Observable<AIPromptItem[]> {
    return this.get<AIPromptItem[]>('/ai/prompts');
  }

  listExecutions(page = 1, pageSize = 20): Observable<{ items: AIExecutionItem[]; totalCount: number }> {
    return this.get<{ items: AIExecutionItem[]; totalCount: number }>('/ai/executions', { page, pageSize });
  }

  getAnalytics(): Observable<AIAnalytics> {
    return this.get<AIAnalytics>('/ai/analytics');
  }

  listApprovals(): Observable<AIApprovalItem[]> {
    return this.get<AIApprovalItem[]>('/ai/approvals');
  }

  grantApproval(id: string): Observable<unknown> {
    return this.post<unknown>(`/ai/approvals/${id}/grant`, {});
  }
}
