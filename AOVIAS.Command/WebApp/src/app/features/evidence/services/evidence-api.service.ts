import { Injectable, inject } from '@angular/core';
import { HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
/** Library GET /evidence/{id}/detail — matches Compliance EvidenceDetailDto (camelCase). */
export interface LibraryEvidenceVerification {
  status: string;
  isApproved?: boolean | null;
  verifiedBy?: string | null;
  verifiedAt?: string | null;
  reviewComment?: string | null;
  remarks?: string | null;
  rowVersion?: string | null;
}

export interface LibraryEvidenceVersionItem {
  versionNumber: number;
  fileUrl: string;
  createdAt: string;
  notes?: string | null;
}

export interface LibraryEvidenceDetail {
  id: string;
  title: string;
  evidenceType: string;
  fileUrl: string;
  uploadedBy: string;
  uploadedOn: string;
  metadata?: string;
  verification: LibraryEvidenceVerification;
  versions: LibraryEvidenceVersionItem[];
}

export interface EvidenceUploadContext {
  journeyId: string | null;
}

export interface EvidenceSummary {
  total: number;
  approved: number;
  pendingReview: number;
  rejected: number;
  missing: number;
}

export interface EvidenceListItem {
  id: string;
  evidenceId: string;
  objectiveElementId?: string;
  fileName: string;
  fileSizeBytes: number;
  fileType: string;
  objectiveElementCode: string;
  objectiveElementName: string;
  chapterCode: string;
  chapterName: string;
  /** Task department id when linked to a task; use with master departments when `department` is empty. */
  departmentId?: string | null;
  department: string;
  uploadedBy: string;
  uploadDate: string;
  status: 'approved' | 'pendingReview' | 'rejected';
  isApproved?: boolean | null;
  reviewComment?: string | null;
  rowVersion?: string | null;
  verifiedBy?: string | null;
  verifiedAt?: string | null;
  linkedTask: string | null;
  description: string | null;
  version: number;
}

export interface PagedEvidenceList {
  items: EvidenceListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EvidenceChapterAggregate {
  chapterId?: string | null;
  name: string;
  count: number;
}

export interface EvidenceDepartmentAggregate {
  departmentId?: string | null;
  name: string;
  count: number;
}

export interface EvidenceStandardAggregate {
  standardId?: string | null;
  chapterId?: string | null;
  name: string;
  count: number;
}

export interface EvidenceApprovalStatus {
  approved: number;
  pendingReview: number;
  rejected: number;
}

export interface EvidenceMissingItem {
  objectiveElementId: string;
  code: string;
  name: string;
  chapter: string;
}

export interface EvidenceAnalytics {
  byChapter: EvidenceChapterAggregate[];
  /** Present when API returns analytics with standard breakdown (Evidence Library). */
  byStandard?: EvidenceStandardAggregate[];
  byDepartment: EvidenceDepartmentAggregate[];
  approvalStatus: EvidenceApprovalStatus;
  missingEvidence: EvidenceMissingItem[];
}

export interface EvidenceActivityItem {
  user: string;
  action: string;
  entity: string;
  timestamp: string;
}

/** Evidence-centric lifecycle row for the grouped timeline view (Upload -> Updated -> Review). */
export interface EvidenceTimelineItem {
  evidenceId: string;
  objectiveElementId: string;
  objectiveElementCode: string;
  objectiveElementName: string;
  chapterCode: string;

  fileName: string;
  fileType: string;
  fileUrl?: string | null;

  uploadedBy: string;
  uploadedAt: string;

  updatedBy: string;
  updatedAt: string;

  status: 'approved' | 'rejected' | 'pendingReview' | string;
  verifiedBy?: string | null;
  verifiedAt?: string | null;

  reviewComment?: string | null;
  hasUpdate: boolean;
  mostRecentAt: string;
}

export interface EvidenceDashboard {
  summary: EvidenceSummary;
  analytics: EvidenceAnalytics;
  recentActivity: EvidenceActivityItem[];
  /** New grouped evidence-lifecycle timeline (up to 10 rows). Optional for backward compat. */
  recentTimeline?: EvidenceTimelineItem[];
}

export interface EvidenceListFilters {
  frameworkId: string;
  objectiveElementId?: string | null;
  chapterId?: string | null;
  standardId?: string | null;
  status?: string | null;
  departmentId?: string | null;
  fileType?: string | null;
  search?: string | null;
  page?: number;
  pageSize?: number;
  sortBy?: string | null;
  sortOrder?: string | null;
}

export interface EvidenceApprovalPayload {
  action: 'approve' | 'reject' | 'requestMoreEvidence';
  remark?: string | null;
  recommendation?: string | null;
  reviewerRole?: string | null;
  rowVersion?: string | null;
}

export type EvidenceVerificationStatus =
  | 'approved'
  | 'pendingReview'
  | 'rejected'
  | 'needsMoreEvidence'
  | 'draft'
  | 'submitted';

export interface EvidenceApprovalResult {
  evidenceId: string;
  status: EvidenceVerificationStatus;
  isApproved?: boolean | null;
  reviewComment?: string | null;
  verifiedBy?: string | null;
  verifiedAt?: string | null;
  rowVersion?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class EvidenceApiService extends BaseApiService {
  private readonly complianceContext = inject(ComplianceContextService);

  private requireFrameworkId(explicit?: string | null): string {
    const id = explicit ?? this.complianceContext.getFrameworkId();
    if (!id) {
      throw new Error('frameworkId is required for Evidence API calls.');
    }
    return id;
  }

  getSummary(frameworkId?: string | null): Observable<EvidenceSummary> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceSummary>('/evidence/summary', { frameworkId: fid });
  }

  getEvidenceList(filters: EvidenceListFilters): Observable<PagedEvidenceList> {
    const frameworkId = this.requireFrameworkId(filters.frameworkId);
    const params: Record<string, string | number> = {
      frameworkId,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
    };
    if (filters.objectiveElementId) params['objectiveElementId'] = filters.objectiveElementId;
    if (filters.chapterId) params['chapterId'] = filters.chapterId;
    if (filters.standardId) params['standardId'] = filters.standardId;
    if (filters.status) params['status'] = filters.status;
    if (filters.departmentId) params['departmentId'] = filters.departmentId;
    if (filters.fileType) params['fileType'] = filters.fileType;
    if (filters.search) params['search'] = filters.search;
    if (filters.sortBy) params['sortBy'] = filters.sortBy;
    if (filters.sortOrder) params['sortOrder'] = filters.sortOrder;
    return this.get<PagedEvidenceList>('/evidence/list', params);
  }

  getAnalytics(frameworkId?: string | null): Observable<EvidenceAnalytics> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceAnalytics>('/evidence/analytics', { frameworkId: fid });
  }

  getRecentActivity(take = 20, frameworkId?: string | null): Observable<EvidenceActivityItem[]> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceActivityItem[]>('/evidence/recent-activity', {
      frameworkId: fid,
      take: Math.min(20, Math.max(1, take)),
    });
  }

  getDashboard(frameworkId?: string | null): Observable<EvidenceDashboard> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceDashboard>('/evidence/dashboard', { frameworkId: fid });
  }

  /**
   * Library-scoped evidence detail (versions + verification).
   */
  getLibraryEvidenceDetail(evidenceId: string, frameworkId?: string | null): Observable<LibraryEvidenceDetail> {
    if (!evidenceId?.trim()) {
      throw new Error('evidenceId is required.');
    }
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<LibraryEvidenceDetail>(`/evidence/${encodeURIComponent(evidenceId)}/detail`, {
      frameworkId: fid,
    });
  }

  getUploadContext(frameworkId?: string | null): Observable<EvidenceUploadContext> {
    const fid = this.requireFrameworkId(frameworkId);
    return this.get<EvidenceUploadContext>('/evidence/upload-context', { frameworkId: fid });
  }

  /**
   * Streams the current evidence file bytes (Gateway proxies without buffering JSON).
   */
  downloadLibraryEvidenceFile(
    evidenceId: string,
    frameworkId?: string | null,
  ): Observable<{ blob: Blob; contentType: string | null }> {
    if (!evidenceId?.trim()) {
      throw new Error('evidenceId is required.');
    }
    const fid = this.requireFrameworkId(frameworkId);
    const url = `${this.apiBaseUrl}/evidence/${encodeURIComponent(evidenceId)}/download?frameworkId=${encodeURIComponent(fid)}`;
    return this.http.get(url, { observe: 'response', responseType: 'blob' }).pipe(
      map((res: HttpResponse<Blob>) => ({
        blob: res.body ?? new Blob(),
        contentType: res.headers.get('content-type'),
      })),
    );
  }

  approveOrReject(evidenceId: string, payload: EvidenceApprovalPayload): Observable<EvidenceApprovalResult> {
    if (!evidenceId?.trim()) {
      throw new Error('evidenceId is required for approval.');
    }

    return this.post<EvidenceApprovalResult>(`/evidence/${evidenceId}/approval`, payload);
  }

  getReviewQueue(filters: EvidenceReviewQueueFilters): Observable<PagedEvidenceReviewQueue> {
    const frameworkId = this.requireFrameworkId(filters.frameworkId);
    const params: Record<string, string | number> = {
      frameworkId,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
    };
    if (filters.status) params['status'] = filters.status;
    if (filters.stage) params['stage'] = filters.stage;
    if (filters.departmentId) params['departmentId'] = filters.departmentId;
    if (filters.ownerId) params['ownerId'] = filters.ownerId;
    if (filters.evidenceType) params['evidenceType'] = filters.evidenceType;
    if (filters.search) params['search'] = filters.search;
    return this.get<PagedEvidenceReviewQueue>('/evidence/review-queue', params);
  }
}

export interface EvidenceReviewQueueItem {
  linkId: string;
  evidenceId: string;
  objectiveElementId: string;
  fileName: string;
  fileType: string;
  objectiveElementCode: string;
  objectiveElementName: string;
  chapterCode: string;
  chapterName: string;
  maturityStage?: string | null;
  departmentId?: string | null;
  department: string;
  uploadedBy: string;
  uploadDate: string;
  status: string;
  evidenceType?: string | null;
  taskId?: string | null;
  linkedTask?: string | null;
  rowVersion?: string | null;
  version: number;
}

export interface PagedEvidenceReviewQueue {
  items: EvidenceReviewQueueItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EvidenceReviewQueueFilters {
  frameworkId?: string | null;
  status?: string;
  stage?: string;
  departmentId?: string;
  ownerId?: string;
  evidenceType?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
