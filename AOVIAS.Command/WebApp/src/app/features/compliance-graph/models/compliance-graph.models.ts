import type { ComplianceGraph } from '../../documentation/models/intelligence.models';

export interface GraphEntitySummary {
  entityId: string;
  entityType: string;
  nodeId: string;
  title: string;
  subtitle?: string;
  status: string;
  owner?: string;
  department?: string;
  route?: string;
  health?: string;
  risk?: string;
  relatedObjectCount: number;
  lastUpdated?: string;
}

export interface GraphRelationship {
  id: string;
  sourceEntityId: string;
  sourceEntityType: string;
  sourceNodeId: string;
  sourceLabel: string;
  targetEntityId: string;
  targetEntityType: string;
  targetNodeId: string;
  targetLabel: string;
  relationshipType: string;
  direction: string;
  targetRoute?: string;
  isBlocking: boolean;
}

export interface GraphRelationships {
  entity: GraphEntitySummary;
  incoming: GraphRelationship[];
  outgoing: GraphRelationship[];
  visualGraph: ComplianceGraph;
  impactSummary: string;
}

export interface GraphTimelineEntry {
  occurredAt: string;
  layer: string;
  action: string;
  label: string;
  detail?: string;
  actorName?: string;
  entityId?: string;
  entityType?: string;
  route?: string;
}

export interface GraphTimeline {
  entity: GraphEntitySummary;
  entries: GraphTimelineEntry[];
}

export interface GraphImpactItem {
  category: string;
  count: number;
  examples: GraphEntitySummary[];
}

export interface GraphImpact {
  entity: GraphEntitySummary;
  summary: string;
  affectedAreas: GraphImpactItem[];
}

export interface GraphDependencyItem {
  category: string;
  relationshipType: string;
  count: number;
  isBlocking: boolean;
  examples: string[];
}

export interface GraphDependencies {
  entity: GraphEntitySummary;
  canArchive: boolean;
  canSupersede: boolean;
  canDelete: boolean;
  canClose: boolean;
  warningMessage: string;
  blocking: GraphDependencyItem[];
  downstream: GraphDependencyItem[];
  upstream: GraphDependencyItem[];
}

export interface GraphSearchResultItem {
  entityId: string;
  entityType: string;
  title: string;
  subtitle?: string;
  status: string;
  route: string;
  score: number;
}

export interface GraphSearchGroup {
  entityType: string;
  label: string;
  items: GraphSearchResultItem[];
}

export interface GraphSearch {
  query: string;
  totalResults: number;
  groups: GraphSearchGroup[];
}

export interface GraphNavigationStep {
  order: number;
  label: string;
  entityType: string;
  entityId?: string;
  route: string;
  relationshipType: string;
}

export interface GraphNavigation {
  entity: GraphEntitySummary;
  forwardPath: GraphNavigationStep[];
  backwardPath: GraphNavigationStep[];
  relatedShortcuts: GraphEntitySummary[];
}

export interface GraphTraceabilityStep {
  order: number;
  layer: string;
  label: string;
  status: string;
  entityId?: string;
  entityType?: string;
  route?: string;
  detail?: string;
}

export interface GraphTraceability {
  root: GraphEntitySummary;
  chain: GraphTraceabilityStep[];
  summary: string;
  readinessScore?: number;
}

export interface GraphExecutiveImpact {
  topConnectedRisks: GraphEntitySummary[];
  mostReferencedDocuments: GraphEntitySummary[];
  mostInfluentialCapas: GraphEntitySummary[];
  highDependencyAreas: string[];
  departmentsWithHighestImpact: string[];
  complianceBottlenecks: string[];
  crossDomainHeatMap: GraphImpactItem[];
}

export interface GraphAnalytics {
  totalNodes: number;
  totalRelationships: number;
  relationshipDensity: number;
  mostConnectedAssets: number;
  crossDomainCoverage: number;
  dependencyRisk: number;
  traceabilityCompleteness: number;
  domainCoverage: GraphImpactItem[];
}

export function formatGraphLabel(value?: string): string {
  if (!value) return '—';
  return value.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/_/g, ' ');
}

export function graphEntityRoute(route?: string): string {
  if (!route) return '/app/compliance-graph';
  return route.split('?')[0];
}

export function graphEntityQuery(route?: string): Record<string, string> {
  if (!route?.includes('?')) return {};
  const qs = route.split('?')[1];
  const params: Record<string, string> = {};
  qs.split('&').forEach((p) => { const [k, v] = p.split('='); if (k) params[k] = v ?? ''; });
  return params;
}
