import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  AcknowledgeDocumentRequest,
  ApproveDocumentRequest,
  CompleteDocumentReviewRequest,
  ControlledDocumentDetail,
  ControlledDocumentListItem,
  CreateControlledDocumentRequest,
  CreateDocumentVersionRequest,
  DocumentationAnalytics,
  DocumentationDashboard,
  DocumentAcknowledgementListItem,
  DocumentTemplate,
  DocumentsQuery,
  PublishDocumentRequest,
  ReviewCalendarItem,
  TransitionDocumentStatusRequest,
} from '../models/documentation.model';

@Injectable({ providedIn: 'root' })
export class DocumentationApiService extends BaseApiService {
  private readonly base = '/documentation';

  getDashboard(): Observable<DocumentationDashboard> {
    return this.get(`${this.base}/dashboard`);
  }

  getDocuments(query: DocumentsQuery = {}): Observable<ControlledDocumentListItem[]> {
    return this.get(`${this.base}/documents`, query as Record<string, string>);
  }

  getDocument(id: string): Observable<ControlledDocumentDetail> {
    return this.get(`${this.base}/documents/${id}`);
  }

  createDocument(payload: CreateControlledDocumentRequest): Observable<ControlledDocumentDetail> {
    return this.post(this.base, payload);
  }

  createVersion(documentId: string, payload: CreateDocumentVersionRequest): Observable<ControlledDocumentDetail> {
    return this.post(`${this.base}/${documentId}/versions`, payload);
  }

  transition(documentId: string, payload: TransitionDocumentStatusRequest): Observable<ControlledDocumentDetail> {
    return this.post(`${this.base}/${documentId}/transition`, payload);
  }

  approve(payload: ApproveDocumentRequest): Observable<ControlledDocumentDetail> {
    return this.post(`${this.base}/approve`, payload);
  }

  publish(documentId: string, payload: PublishDocumentRequest = {}): Observable<ControlledDocumentDetail> {
    return this.post(`${this.base}/publish?documentId=${encodeURIComponent(documentId)}`, payload);
  }

  acknowledge(payload: AcknowledgeDocumentRequest): Observable<DocumentAcknowledgementListItem> {
    return this.post(`${this.base}/acknowledge`, payload);
  }

  completeReview(documentId: string, payload: CompleteDocumentReviewRequest = {}): Observable<ControlledDocumentDetail> {
    return this.post(`${this.base}/${documentId}/reviews/complete`, payload);
  }

  getAcknowledgements(): Observable<DocumentAcknowledgementListItem[]> {
    return this.get(`${this.base}/acknowledgements`);
  }

  getReviewCalendar(): Observable<ReviewCalendarItem[]> {
    return this.get(`${this.base}/review-calendar`);
  }

  getTemplates(): Observable<DocumentTemplate[]> {
    return this.get(`${this.base}/templates`);
  }

  getAnalytics(): Observable<DocumentationAnalytics> {
    return this.get(`${this.base}/analytics`);
  }
}
