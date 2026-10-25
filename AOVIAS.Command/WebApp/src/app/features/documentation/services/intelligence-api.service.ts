import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  AiComplianceReview,
  ComplianceGraph,
  CrossReferenceSuggestion,
  DocumentDependencies,
  DocumentExplorer,
  DocumentHealthDashboard,
  DocumentImpact,
  DocumentIntelligence,
  DocumentRelationships,
  DocumentSearchResult,
} from '../models/intelligence.models';

@Injectable({ providedIn: 'root' })
export class IntelligenceApiService extends BaseApiService {
  private readonly base = '/documentation';

  getGraph(): Observable<ComplianceGraph> {
    return this.get(`${this.base}/graph`);
  }

  getHealthDashboard(): Observable<DocumentHealthDashboard> {
    return this.get(`${this.base}/health-dashboard`);
  }

  getIntelligence(documentId: string): Observable<DocumentIntelligence> {
    return this.get(`${this.base}/intelligence/${documentId}`);
  }

  getImpact(documentId: string): Observable<DocumentImpact> {
    return this.get(`${this.base}/impact/${documentId}`);
  }

  getDependencies(documentId: string): Observable<DocumentDependencies> {
    return this.get(`${this.base}/dependencies/${documentId}`);
  }

  getRelationships(documentId: string): Observable<DocumentRelationships> {
    return this.get(`${this.base}/relationships/${documentId}`);
  }

  reviewAi(documentId: string): Observable<AiComplianceReview> {
    return this.post(`${this.base}/review-ai`, { documentId });
  }

  getExplorer(): Observable<DocumentExplorer> {
    return this.get(`${this.base}/explorer`);
  }

  search(query: string): Observable<DocumentSearchResult[]> {
    return this.get(`${this.base}/search`, { q: query });
  }

  getCrossReferenceSuggestions(documentId: string, query: string): Observable<CrossReferenceSuggestion[]> {
    return this.get(`${this.base}/cross-reference-suggestions`, { documentId, q: query });
  }
}
