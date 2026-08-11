import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE } from '../../journey/utils/oe-evidence-upload.constants';
import type {
  EvidenceBinderAiMappingItem,
  EvidenceBinderChapter,
  EvidenceBinderChecklistItem,
  EvidenceBinderObjectiveElement,
  EvidenceBinderOverview,
  EvidenceBinderSummary,
  OeHistoryEvent,
  PagedEvidenceBinderEvidences,
} from './models';

@Injectable({ providedIn: 'root' })
export class EvidenceBinderApiService extends BaseApiService {
  private readonly complianceContext = inject(ComplianceContextService);

  private requireFrameworkId(explicit?: string | null): string {
    const id = explicit ?? this.complianceContext.getFrameworkId();
    if (!id) {
      throw new Error('frameworkId is required for Evidence Binder API calls.');
    }
    return id;
  }

  getSummary(frameworkId?: string | null): Observable<EvidenceBinderSummary> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceBinderSummary>('/evidence-binder/summary', { frameworkId: fid });
  }

  getChapters(frameworkId?: string | null): Observable<EvidenceBinderChapter[]> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceBinderChapter[]>('/evidence-binder/chapters', { frameworkId: fid });
  }

  getObjectivesForChapter(chapterId: string, frameworkId?: string | null): Observable<EvidenceBinderObjectiveElement[]> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceBinderObjectiveElement[]>(`/evidence-binder/chapters/${encodeURIComponent(chapterId)}/objectives`, {
      frameworkId: fid,
    });
  }

  getOverview(objectiveElementId: string, frameworkId?: string | null): Observable<EvidenceBinderOverview> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceBinderOverview>(
      `/evidence-binder/objective-elements/${encodeURIComponent(objectiveElementId)}/overview`,
      { frameworkId: fid },
    );
  }

  getEvidences(
    objectiveElementId: string,
    page: number,
    pageSize: number,
    frameworkId?: string | null,
  ): Observable<PagedEvidenceBinderEvidences> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<PagedEvidenceBinderEvidences>(
      `/evidence-binder/objective-elements/${encodeURIComponent(objectiveElementId)}/evidences`,
      { frameworkId: fid, page, pageSize },
    );
  }

  getChecklist(objectiveElementId: string, frameworkId?: string | null): Observable<EvidenceBinderChecklistItem[]> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceBinderChecklistItem[]>(
      `/evidence-binder/objective-elements/${encodeURIComponent(objectiveElementId)}/checklist`,
      { frameworkId: fid },
    );
  }

  getAiMapping(objectiveElementId: string, frameworkId?: string | null): Observable<EvidenceBinderAiMappingItem[]> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceBinderAiMappingItem[]>(
      `/evidence-binder/objective-elements/${encodeURIComponent(objectiveElementId)}/ai-mapping`,
      { frameworkId: fid },
    );
  }

  getHistory(objectiveElementId: string, frameworkId?: string | null): Observable<OeHistoryEvent[]> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<OeHistoryEvent[]>(
      `/evidence-binder/objective-elements/${encodeURIComponent(objectiveElementId)}/history`,
      { frameworkId: fid },
    );
  }

  /** Multipart upload; journey resolved server-side. */
  postEvidence(objectiveElementId: string, file: File, frameworkId?: string | null): Observable<{ evidenceId: string }> {
    const fid = this.requireFrameworkId(frameworkId);
    const fd = new FormData();
    fd.append('file', file, file.name);
    fd.append('evidenceType', OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE);
    const url = `${this.apiBaseUrl}/evidence-binder/objective-elements/${encodeURIComponent(objectiveElementId)}/evidences?frameworkId=${encodeURIComponent(fid)}`;
    return this.http.post<{ evidenceId: string }>(url, fd);
  }
}
