import { Injectable, inject } from '@angular/core';
import { HttpContext, HttpParams } from '@angular/common/http';
import { JOURNEY_LONG_READ_CONTEXT } from '../../../core/http/journey-http.tokens';
import { HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { environment } from '../../../../environments/environment';
import { EvidenceMultipartApiClient } from './evidence-multipart-api.client';
import { JourneyCacheService } from './journey-cache.service';
import { JourneyWorkspaceSnapshot } from '../models/journey-tree.model';
import {
  mapApiToWorkspaceSnapshot,
  mapComplianceStandardDto,
  mapObjectiveElementApiToDomain,
  mapEvidenceItemApiToDomain,
  mapHistoryEventApiToDomain,
} from '../mappers/journey-workspace.mapper';
import {
  JourneyWorkspaceApiDto,
  ObjectiveElementApiDto,
  OeEvidenceItemApiDto,
  OeHistoryEventApiDto,
  PatchObjectiveElementBody,
  PatchObjectiveElementResponseDto,
  JourneySessionDto,
  JourneyDashboardDto,
  JourneyChaptersIndexDto,
  ComplianceStandardApiDto,
  PatchOperationalModeRequestDto,
  RegisterOeEvidenceRequest,
  RegisterOeEvidenceResponseDto,
  BulkEvidenceUploadResponseDto,
  DeleteOeEvidenceResponseDto,
  EvidenceDetailDto,
  VerifyOeEvidenceRequest,
  ObjectiveJourneyStateDto,
  SaveObjectiveJourneyDraftRequest,
  CompleteObjectiveJourneyRequest,
  ObligationStateApiDto,
  ObligationMaturityBreakdownApiDto,
} from '../models/journey-api.model';
import { ComplianceStandard } from '../models/standard.model';
import { ObjectiveElement, OeEvidenceItem, OeHistoryEvent } from '../models/objective-element.model';
import { buildSingleEvidenceFormData } from '../utils/oe-evidence-upload.payload';
import { parseHttpContentTypeHeader } from '../utils/oe-evidence-display.util';

/** Optional mode scoping for GETs (must not send conflicting pair per API spec). */
export interface JourneyModeQuery {
  operationalMode?: number;
  auditLifecycleMode?: string;
  /**
   * Narrow results to a specific NABH maturity classification band.
   * Accepted values: core | commitment | achievement | excellence
   * When provided, chapters/standards/OEs are filtered to only those
   * belonging to the requested stage.
   */
  classificationFilter?: string;
}

export interface ChapterStandardsQuery extends JourneyModeQuery {
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root',
})
export class JourneyDataService extends BaseApiService {
  private readonly evidenceMultipartApi = inject(EvidenceMultipartApiClient);
  private readonly journeyCache = inject(JourneyCacheService);

  private readonly journeyLongReadContext: HttpContext = new HttpContext().set(
    JOURNEY_LONG_READ_CONTEXT,
    true,
  );

  /**
   * Path prefix `/journeys/{journeyId}`.
   * `environment.apiBaseUrl` is usually `.../api/v1` — confirm deployed Gateway uses same segment (`v1` vs `v1.0`).
   */
  private journeyBase(journeyId: string): string {
    return `/journeys/${encodeURIComponent(journeyId)}`;
  }

  private toHttpParams(
    q: JourneyModeQuery | ChapterStandardsQuery | undefined,
  ): Record<string, string | number> | undefined {
    if (!q) return undefined;
    const out: Record<string, string | number> = {};
    if (q.operationalMode !== undefined && q.operationalMode !== null) {
      out['operationalMode'] = q.operationalMode;
    }
    if (q.auditLifecycleMode) {
      out['auditLifecycleMode'] = q.auditLifecycleMode;
    }
    if (q.classificationFilter) {
      out['classificationFilter'] = q.classificationFilter;
    }
    const cq = q as ChapterStandardsQuery;
    if (typeof cq.page === 'number') out['page'] = cq.page;
    if (typeof cq.pageSize === 'number') out['pageSize'] = cq.pageSize;
    return Object.keys(out).length ? out : undefined;
  }

  getSession(journeyId: string, query?: JourneyModeQuery): Observable<JourneySessionDto> {
    return this.get<JourneySessionDto>(`${this.journeyBase(journeyId)}/session`, this.toHttpParams(query), {
      context: this.journeyLongReadContext,
    });
  }

  getDashboard(journeyId: string, query?: JourneyModeQuery): Observable<JourneyDashboardDto> {
    return this.get<JourneyDashboardDto>(`${this.journeyBase(journeyId)}/dashboard`, this.toHttpParams(query), {
      context: this.journeyLongReadContext,
    });
  }

  getChaptersIndex(journeyId: string, query?: JourneyModeQuery): Observable<JourneyChaptersIndexDto> {
    return this.get<JourneyChaptersIndexDto>(`${this.journeyBase(journeyId)}/chapters`, this.toHttpParams(query), {
      context: this.journeyLongReadContext,
    });
  }

  getChapterStandards(
    journeyId: string,
    chapterId: string,
    query?: ChapterStandardsQuery,
  ): Observable<ComplianceStandard[]> {
    const params = {
      ...this.toHttpParams(query),
      page: query?.page ?? 1,
      pageSize: query?.pageSize ?? 200,
    };
    return this.get<ComplianceStandardApiDto[]>(
      `${this.journeyBase(journeyId)}/chapters/${encodeURIComponent(chapterId)}/standards`,
      params,
      { context: this.journeyLongReadContext },
    ).pipe(
      map((rows) =>
        (Array.isArray(rows) ? rows : []).map((row) => mapComplianceStandardDto(row ?? {}, chapterId)),
      ),
    );
  }

  getObjectiveElement(
    journeyId: string,
    objectiveElementId: string,
    query?: JourneyModeQuery,
  ): Observable<ObjectiveElement> {
    return this.get<ObjectiveElementApiDto>(
      `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}`,
      this.toHttpParams(query),
      { context: this.journeyLongReadContext },
    ).pipe(map((dto) => mapObjectiveElementApiToDomain(dto ?? {})));
  }

  patchObjectiveElement(
    journeyId: string,
    objectiveElementId: string,
    body: PatchObjectiveElementBody,
    query?: JourneyModeQuery,
  ): Observable<PatchObjectiveElementResponseDto> {
    return this.patch<PatchObjectiveElementResponseDto>(
      `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}`,
      body,
      undefined,
      this.toHttpParams(query),
    );
  }

  patchOperationalMode(
    journeyId: string,
    body: PatchOperationalModeRequestDto,
  ): Observable<JourneySessionDto> {
    return this.patch<JourneySessionDto>(`${this.journeyBase(journeyId)}/session/operational-mode`, body);
  }

  /**
   * `POST …/evidence` with **JSON** body (`application/json`) — URL reference registration, not file bytes.
   * For multipart file upload use {@link uploadSingleEvidence} / {@link postEvidenceMultipart}.
   */
  postEvidenceJson(
    journeyId: string,
    objectiveElementId: string,
    body: RegisterOeEvidenceRequest,
  ): Observable<RegisterOeEvidenceResponseDto> {
    return this.post<RegisterOeEvidenceResponseDto>(
      `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence`,
      body,
    );
  }

  /**
   * Multipart single-file evidence upload. Prefer {@link uploadSingleEvidence} when FormData is pre-built.
   */
  postEvidenceMultipart(
    journeyId: string,
    objectiveElementId: string,
    file: File,
    evidenceType: string,
    title?: string,
  ): Observable<RegisterOeEvidenceResponseDto> {
    const fd = buildSingleEvidenceFormData({ file, evidenceType, title });
    return this.uploadSingleEvidence(journeyId, objectiveElementId, fd);
  }

  /**
   * Single-file multipart upload (Gateway §5.3 — ASP.NET model binding).
   *
   * **Required form parts (camelCase):** `file` (binary), `evidenceType` (non-empty string).
   * Optional: `name`, `title`, `metadata` (JSON string). Builders set `name` from title or filename.
   *
   * Multipart upload is delegated to `EvidenceMultipartApiClient` (native `XMLHttpRequest` + `FormData`).
   */
  uploadSingleEvidence(
    journeyId: string,
    objectiveElementId: string,
    formData: FormData,
  ): Observable<RegisterOeEvidenceResponseDto> {
    const url = this.objectiveElementEvidenceMultipartUploadUrl(journeyId, objectiveElementId);
    return this.postMultipartFormData<RegisterOeEvidenceResponseDto>(url, formData);
  }

  /**
   * Multipart file upload URL (may differ from JSON `postEvidenceJson` when `environment.evidenceMultipartPathSuffix` is set).
   */
  private objectiveElementEvidenceMultipartUploadUrl(journeyId: string, objectiveElementId: string): string {
    const suffix = environment.evidenceMultipartPathSuffix ?? '';
    return `${environment.apiBaseUrl}${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence${suffix}`;
  }

  /**
   * Bulk multipart — **different URL** ending in `/evidence/bulk`.
   * Required parts: repeated `files` + `evidenceType`. Do not send `file` here.
   */
  uploadBulkEvidence(
    journeyId: string,
    objectiveElementId: string,
    formData: FormData,
  ): Observable<BulkEvidenceUploadResponseDto> {
    const url = `${environment.apiBaseUrl}${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence/bulk`;
    return this.postMultipartFormData<BulkEvidenceUploadResponseDto>(url, formData);
  }

  deleteEvidence(
    journeyId: string,
    objectiveElementId: string,
    evidenceId: string,
  ): Observable<DeleteOeEvidenceResponseDto> {
    return this.delete<DeleteOeEvidenceResponseDto>(
      `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence/${encodeURIComponent(evidenceId)}`,
    );
  }

  getObjectiveElementEvidence(
    journeyId: string,
    objectiveElementId: string,
  ): Observable<OeEvidenceItem[]> {
    return this.get<OeEvidenceItemApiDto[]>(
      `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence`,
    ).pipe(
      map((rows) =>
        (Array.isArray(rows) ? rows : []).map((r) => mapEvidenceItemApiToDomain(r ?? {})),
      ),
    );
  }

  /** Gateway §5.6 — single evidence aggregate + versions. */
  getObjectiveElementEvidenceDetail(
    journeyId: string,
    objectiveElementId: string,
    evidenceId: string,
  ): Observable<EvidenceDetailDto> {
    return this.get<EvidenceDetailDto>(
      `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence/${encodeURIComponent(evidenceId)}`,
    );
  }

  /**
   * Gateway §5.7 — stream current file bytes. Uses `responseType: 'blob'`.
   * `apiResponseInterceptor` only unwraps JSON bodies that include `success`; **Blob** bodies pass through unchanged.
   * Returns **Content-Type** from response headers (often reliable when `Blob.type` is empty).
   */
  downloadObjectiveElementEvidenceFile(
    journeyId: string,
    objectiveElementId: string,
    evidenceId: string,
  ): Observable<{ blob: Blob; contentType: string | null }> {
    const url = `${environment.apiBaseUrl}${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence/${encodeURIComponent(evidenceId)}/download`;
    return this.http
      .get(url, { observe: 'response', responseType: 'blob' })
      .pipe(
        map((res: HttpResponse<Blob>) => ({
          blob: res.body ?? new Blob(),
          contentType: parseHttpContentTypeHeader(res.headers.get('content-type')),
        })),
      );
  }

  /** Gateway §5.8 */
  verifyObjectiveElementEvidence(
    journeyId: string,
    objectiveElementId: string,
    evidenceId: string,
    body: VerifyOeEvidenceRequest,
  ): Observable<unknown> {
    return this.post<unknown>(
      `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence/${encodeURIComponent(evidenceId)}/verify`,
      body,
    );
  }

  /** Gateway §5.9 — new file version for existing evidence id. */
  uploadObjectiveElementEvidenceVersion(
    journeyId: string,
    objectiveElementId: string,
    evidenceId: string,
    formData: FormData,
  ): Observable<RegisterOeEvidenceResponseDto> {
    const url = `${environment.apiBaseUrl}${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/evidence/${encodeURIComponent(evidenceId)}/versions`;
    return this.postMultipartFormData<RegisterOeEvidenceResponseDto>(url, formData);
  }

  private postMultipartFormData<T>(url: string, formData: FormData): Observable<T> {
    return this.evidenceMultipartApi.postFormData<T>(url, formData);
  }

  getObjectiveElementHistory(
    journeyId: string,
    objectiveElementId: string,
  ): Observable<OeHistoryEvent[]> {
    return this.get<OeHistoryEventApiDto[]>(
      `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/history`,
    ).pipe(
      map((rows) =>
        (Array.isArray(rows) ? rows : []).map((r) => mapHistoryEventApiToDomain(r ?? {})),
      ),
    );
  }

  /** @deprecated Prefer split GETs (`getSession`, `getDashboard`, `getChaptersIndex`). */
  getJourneyWorkspace(journeyId: string): Observable<JourneyWorkspaceSnapshot> {
    return this.get<JourneyWorkspaceApiDto>('/Journeys/workspace', { journeyId }).pipe(
      map((dto) => mapApiToWorkspaceSnapshot(dto ?? {})),
    );
  }

  /** No-op placeholder — split calls manage their own caching in the store. */
  clearWorkspaceCache(): void {
    this.journeyCache.clearAll();
  }

  private oeExecutionWizardPath(journeyId: string, objectiveElementId: string): string {
    return `${this.journeyBase(journeyId)}/objective-elements/${encodeURIComponent(objectiveElementId)}/oe-execution-wizard`;
  }

  getOeExecutionWizard(journeyId: string, objectiveElementId: string): Observable<ObjectiveJourneyStateDto> {
    return this.get<ObjectiveJourneyStateDto>(this.oeExecutionWizardPath(journeyId, objectiveElementId));
  }

  putOeExecutionWizardDraft(
    journeyId: string,
    objectiveElementId: string,
    body: SaveObjectiveJourneyDraftRequest,
  ): Observable<ObjectiveJourneyStateDto> {
    const rowVersion =
      body.rowVersion != null && String(body.rowVersion).trim() !== '' ? body.rowVersion : undefined;
    const normalized: SaveObjectiveJourneyDraftRequest =
      rowVersion !== undefined ? { payload: body.payload, rowVersion } : { payload: body.payload };
    return this.put<ObjectiveJourneyStateDto>(
      this.oeExecutionWizardPath(journeyId, objectiveElementId),
      normalized,
    );
  }

  postOeExecutionWizardComplete(
    journeyId: string,
    objectiveElementId: string,
    body: CompleteObjectiveJourneyRequest,
    query?: JourneyModeQuery,
  ): Observable<ObjectiveJourneyStateDto> {
    const rowVersion =
      body.rowVersion != null && String(body.rowVersion).trim() !== '' ? body.rowVersion : undefined;
    const normalized: CompleteObjectiveJourneyRequest = {
      payload: body.payload,
      ...(rowVersion !== undefined ? { rowVersion } : {}),
      ...(body.complianceTaskId ? { complianceTaskId: body.complianceTaskId } : {}),
    };
    let path = `${this.oeExecutionWizardPath(journeyId, objectiveElementId)}/complete`;
    const q = this.toHttpParams(query);
    if (q && Object.keys(q).length > 0) {
      let params = new HttpParams();
      for (const [k, v] of Object.entries(q)) {
        params = params.set(k, String(v));
      }
      path += `?${params.toString()}`;
    }
    return this.post<ObjectiveJourneyStateDto>(path, normalized);
  }

  // ── Phase 11: Journey Obligation endpoints ──────────────────────────────

  private journeyObligationBase(): string {
    return `${environment.apiBaseUrl}/api/v1/journey`;
  }

  /** GET /api/v1/journey/overview?hospitalId=&frameworkId= */
  getJourneyObligationOverview(
    hospitalId: string,
    frameworkId: string,
  ): Observable<{ summary: unknown; journeyReadinessScore: number; totalObligations: number }> {
    return this.get(`${this.journeyObligationBase()}/overview?hospitalId=${hospitalId}&frameworkId=${frameworkId}`);
  }

  /** GET /api/v1/journey/obligations?hospitalId=&frameworkId= */
  getJourneyObligations(
    hospitalId: string,
    frameworkId: string,
  ): Observable<{ items: ObligationStateApiDto[]; totalCount: number }> {
    return this.get(`${this.journeyObligationBase()}/obligations?hospitalId=${hospitalId}&frameworkId=${frameworkId}`);
  }

  /** GET /api/v1/journey/readiness?hospitalId=&frameworkId= */
  getJourneyReadiness(
    hospitalId: string,
    frameworkId: string,
  ): Observable<{ journeyReadinessScore: number; byStage: unknown[]; byChapter: unknown[] }> {
    return this.get(`${this.journeyObligationBase()}/readiness?hospitalId=${hospitalId}&frameworkId=${frameworkId}`);
  }
}
