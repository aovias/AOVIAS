import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  AuthoringAiDraftRequest,
  AuthoringAiDraftResponse,
  AuthoringComment,
  AuthoringSession,
  CreateAuthoringRequest,
  CrossReference,
  PatchAuthoringRequest,
  StructuredCompareResult,
} from '../models/authoring.models';
import type { DocumentTemplate } from '../models/documentation.model';

@Injectable({ providedIn: 'root' })
export class AuthoringApiService extends BaseApiService {
  private readonly base = '/documentation';

  createAuthoring(payload: CreateAuthoringRequest): Observable<AuthoringSession> {
    return this.post(`${this.base}/authoring`, payload);
  }

  getAuthoring(documentId: string): Observable<AuthoringSession> {
    return this.get(`${this.base}/authoring/${documentId}`);
  }

  patchAuthoring(documentId: string, payload: PatchAuthoringRequest): Observable<AuthoringSession> {
    return this.patch(`${this.base}/authoring/${documentId}`, payload);
  }

  compareStructured(documentId: string, versionId?: string): Observable<StructuredCompareResult> {
    return this.post(`${this.base}/compare`, { documentId, versionId });
  }

  draftAi(payload: AuthoringAiDraftRequest): Observable<AuthoringAiDraftResponse> {
    return this.post(`${this.base}/draft-ai`, payload);
  }

  addComment(payload: {
    documentId: string;
    sectionId?: string;
    blockId?: string;
    content: string;
    commentType?: string;
    mentionedUserName?: string;
  }): Observable<AuthoringComment> {
    return this.post(`${this.base}/authoring/comments`, payload);
  }

  addCrossReference(payload: {
    documentId: string;
    sectionId?: string;
    referenceType: string;
    targetId: string;
    targetLabel: string;
    targetRoute?: string;
  }): Observable<CrossReference> {
    return this.post(`${this.base}/authoring/cross-references`, payload);
  }

  exportDocument(documentId: string, format: 'pdf' | 'docx' | 'html' | 'markdown'): Observable<{
    fileName: string;
    contentType: string;
    base64Content: string;
  }> {
    return this.post(`${this.base}/export`, { documentId, format });
  }

  getTemplates(): Observable<DocumentTemplate[]> {
    return this.get(`${this.base}/templates`);
  }
}
