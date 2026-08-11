import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  GraphAnalytics,
  GraphDependencies,
  GraphEntitySummary,
  GraphExecutiveImpact,
  GraphImpact,
  GraphNavigation,
  GraphRelationships,
  GraphSearch,
  GraphTimeline,
  GraphTraceability,
} from '../models/compliance-graph.models';
import type { ComplianceGraph } from '../../documentation/models/intelligence.models';

@Injectable({ providedIn: 'root' })
export class GraphApiService extends BaseApiService {
  getEntity(id: string): Observable<GraphEntitySummary> {
    return this.get(`/graph/entity/${id}`);
  }

  getRelationships(id: string): Observable<GraphRelationships> {
    return this.get(`/graph/relationships/${id}`);
  }

  getTimeline(id: string): Observable<GraphTimeline> {
    return this.get(`/graph/timeline/${id}`);
  }

  getImpact(id: string): Observable<GraphImpact> {
    return this.get(`/graph/impact/${id}`);
  }

  getDependencies(id: string): Observable<GraphDependencies> {
    return this.get(`/graph/dependencies/${id}`);
  }

  search(q: string, entityType?: string): Observable<GraphSearch> {
    return this.get('/graph/search', {
      q,
      ...(entityType ? { entityType } : {}),
    });
  }

  getNavigation(id: string): Observable<GraphNavigation> {
    return this.get(`/graph/navigation/${id}`);
  }

  getTraceability(id: string): Observable<GraphTraceability> {
    return this.get(`/graph/traceability/${id}`);
  }

  getExecutive(): Observable<GraphExecutiveImpact> {
    return this.get('/graph/executive');
  }

  getAnalytics(): Observable<GraphAnalytics> {
    return this.get('/graph/analytics');
  }

  getVisualGraph(centerId?: string): Observable<ComplianceGraph> {
    return this.get('/graph/visual', centerId ? { centerId } : {});
  }
}
