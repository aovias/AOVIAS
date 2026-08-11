import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import {
  AuditReport,
  ComplianceReport,
  CustomReportRequest,
  CustomReportResult,
  EvidenceReport,
  ReadinessReport,
  ReportExportFormat,
  ReportQueryParams,
  ReportType,
  TaskReport,
} from '../models/report.models';

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly context = inject(ComplianceContextService);
  private readonly baseUrl = environment.apiBaseUrl;

  getComplianceReport(params?: ReportQueryParams): Observable<ComplianceReport> {
    return this.http.get<ComplianceReport>(`${this.baseUrl}/reports/compliance`, {
      params: this.buildParams(params),
    });
  }

  getAuditReport(params?: ReportQueryParams): Observable<AuditReport> {
    return this.http.get<AuditReport>(`${this.baseUrl}/reports/audit`, {
      params: this.buildParams(params),
    });
  }

  getTaskReport(params?: ReportQueryParams): Observable<TaskReport> {
    return this.http.get<TaskReport>(`${this.baseUrl}/reports/tasks`, {
      params: this.buildParams(params),
    });
  }

  getEvidenceReport(params?: ReportQueryParams): Observable<EvidenceReport> {
    return this.http.get<EvidenceReport>(`${this.baseUrl}/reports/evidence`, {
      params: this.buildParams(params),
    });
  }

  getReadinessReport(params?: ReportQueryParams): Observable<ReadinessReport> {
    return this.http.get<ReadinessReport>(`${this.baseUrl}/reports/readiness`, {
      params: this.buildParams(params),
    });
  }

  generateCustomReport(request: CustomReportRequest): Observable<CustomReportResult> {
    let httpParams = this.buildParams();
    const hospitalId = this.context.getHospitalId();
    if (hospitalId) {
      httpParams = httpParams.set('hospitalId', hospitalId);
    }

    return this.http.post<CustomReportResult>(`${this.baseUrl}/reports/custom`, request, {
      params: httpParams,
    });
  }

  exportReport(
    reportType: ReportType,
    format: ReportExportFormat,
    params?: ReportQueryParams,
    customBody?: CustomReportRequest,
  ): Observable<Blob> {
    const hospitalId = this.context.getHospitalId();
    let httpParams = this.buildParams(params);
    httpParams = httpParams.set('format', format);
    if (hospitalId) {
      httpParams = httpParams.set('hospitalId', hospitalId);
    }

    const path =
      reportType === 'custom'
        ? `${this.baseUrl}/reports/custom/export`
        : `${this.baseUrl}/reports/${reportType}/export`;

    if (reportType === 'custom' && customBody) {
      return this.http.post(path, customBody, {
        params: httpParams,
        responseType: 'blob',
      });
    }

    return this.http.get(path, {
      params: httpParams,
      responseType: 'blob',
    });
  }

  downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  private buildParams(params?: ReportQueryParams): HttpParams {
    let httpParams = new HttpParams();
    const frameworkId = params?.frameworkId ?? this.context.getFrameworkId();
    const hospitalId = this.context.getHospitalId();

    if (hospitalId) {
      httpParams = httpParams.set('hospitalId', hospitalId);
    }

    if (frameworkId) {
      httpParams = httpParams.set('frameworkId', frameworkId);
    }

    if (params?.chapterId) {
      httpParams = httpParams.set('chapterId', params.chapterId);
    }

    if (params?.departmentId) {
      httpParams = httpParams.set('departmentId', params.departmentId);
    }

    if (params?.severity) {
      httpParams = httpParams.set('severity', params.severity);
    }

    if (params?.status) {
      httpParams = httpParams.set('status', params.status);
    }

    if (params?.fromDate) {
      httpParams = httpParams.set('fromDate', params.fromDate);
    }

    if (params?.toDate) {
      httpParams = httpParams.set('toDate', params.toDate);
    }

    if (params?.organizationId) {
      httpParams = httpParams.set('organizationId', params.organizationId);
    }

    return httpParams;
  }
}
