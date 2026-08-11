import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface Connector {
  id: string;
  organizationId: string;
  name: string;
  providerType: string;
  lifecycleState: string;
  healthStatus: string;
  version: number;
  configurationJson?: string;
  createdAtUtc?: string;
}

export interface ConnectorHealthResult {
  status: string;
  latencyMs: number;
  message?: string;
}

export interface UpdateConnectorRequest {
  name: string;
  configurationJson: string;
  lifecycleState: string;
}

export interface SynchronizationJob {
  id: string;
  name: string;
  sourceConnectorId: string;
  mappingProfileId: string;
  direction: string;
  syncType: string;
  isEnabled: boolean;
}

export interface SynchronizationRun {
  id: string;
  jobId: string;
  status: string;
  recordsProcessed: number;
  recordsFailed: number;
  conflictsDetected: number;
  durationMs: number;
  traceId?: string;
}

export interface MappingProfile {
  id: string;
  name: string;
  sourceEntity: string;
  targetEntity: string;
  fields: { sourceField: string; targetField: string }[];
}

export interface WebhookSubscription {
  id: string;
  name: string;
  direction: string;
  targetUrl?: string;
  isEnabled: boolean;
}

export interface WebhookDelivery {
  id: string;
  eventType: string;
  status: string;
  attemptCount: number;
}

export interface ConnectorHealth {
  connectorId: string;
  connectorName: string;
  status: string;
  latencyMs: number;
  successRate: number;
}

export interface IntegrationAuditEntry {
  id: string;
  operation: string;
  result: string;
  recordsProcessed: number;
  durationMs: number;
  traceId?: string;
  occurredAtUtc: string;
}

export interface ImportJob {
  id: string;
  status: string;
  format: string;
  previewSummaryJson?: string;
}

export interface ExportJob {
  id: string;
  status: string;
  format: string;
  outputContent?: string;
}

export interface CreateSyncJobRequest {
  organizationId: string;
  name: string;
  sourceConnectorId: string;
  mappingProfileId: string;
  direction?: string;
  syncType?: string;
  cronExpression?: string;
}

@Injectable({ providedIn: 'root' })
export class IntegrationApiService extends BaseApiService {
  private base = '/api/v1/integration';

  listConnectors(organizationId: string): Observable<Connector[]> {
    return this.get<Connector[]>(`${this.base}/connectors?organizationId=${organizationId}`);
  }

  listProviders(): Observable<string[]> {
    return this.get<string[]>(`${this.base}/connectors/providers`);
  }

  getConnector(id: string): Observable<Connector> {
    return this.get<Connector>(`${this.base}/connectors/${id}`);
  }

  createConnector(body: unknown): Observable<Connector> {
    return this.post<Connector>(`${this.base}/connectors`, body);
  }

  updateConnector(id: string, body: UpdateConnectorRequest): Observable<Connector> {
    return this.put<Connector>(`${this.base}/connectors/${id}`, body);
  }

  validateConnector(id: string): Observable<ConnectorHealthResult> {
    return this.post<ConnectorHealthResult>(`${this.base}/connectors/${id}/validate`, {});
  }

  listSyncJobs(organizationId: string): Observable<SynchronizationJob[]> {
    return this.get<SynchronizationJob[]>(`${this.base}/synchronizations?organizationId=${organizationId}`);
  }

  createSyncJob(body: CreateSyncJobRequest): Observable<SynchronizationJob> {
    return this.post<SynchronizationJob>(`${this.base}/synchronizations`, body);
  }

  runSync(jobId: string): Observable<SynchronizationRun> {
    return this.post<SynchronizationRun>(`${this.base}/synchronizations/run?jobId=${jobId}`, {});
  }

  listMappings(organizationId: string): Observable<MappingProfile[]> {
    return this.get<MappingProfile[]>(`${this.base}/mappings?organizationId=${organizationId}`);
  }

  listWebhooks(organizationId: string): Observable<WebhookSubscription[]> {
    return this.get<WebhookSubscription[]>(`${this.base}/webhooks?organizationId=${organizationId}`);
  }

  listWebhookDeliveries(subscriptionId: string): Observable<WebhookDelivery[]> {
    return this.get<WebhookDelivery[]>(`${this.base}/webhooks/${subscriptionId}/deliveries`);
  }

  getConnectorHealth(organizationId: string): Observable<ConnectorHealth[]> {
    return this.get<ConnectorHealth[]>(`${this.base}/connector-health?organizationId=${organizationId}`);
  }

  listAudit(organizationId: string): Observable<IntegrationAuditEntry[]> {
    return this.get<IntegrationAuditEntry[]>(`${this.base}/audit?organizationId=${organizationId}`);
  }

  createImport(body: unknown): Observable<ImportJob> {
    return this.post<ImportJob>(`${this.base}/imports`, body);
  }

  previewImport(id: string): Observable<ImportJob> {
    return this.post<ImportJob>(`${this.base}/imports/${id}/preview`, {});
  }

  executeImport(id: string): Observable<ImportJob> {
    return this.post<ImportJob>(`${this.base}/imports/${id}/execute`, {});
  }

  rollbackImport(id: string): Observable<ImportJob> {
    return this.post<ImportJob>(`${this.base}/imports/${id}/rollback`, {});
  }

  createExport(body: unknown): Observable<ExportJob> {
    return this.post<ExportJob>(`${this.base}/exports`, body);
  }

  executeExport(id: string): Observable<ExportJob> {
    return this.post<ExportJob>(`${this.base}/exports/${id}/execute`, {});
  }
}
