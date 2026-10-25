import type { ControlledDocumentListItem } from './documentation.model';
import type { DocumentHealthScore } from './authoring.models';

export interface ComplianceGraphNode {
  id: string;
  label: string;
  nodeType: string;
  route?: string | null;
  connectionCount: number;
}

export interface ComplianceGraphEdge {
  sourceId: string;
  targetId: string;
  relationshipType: string;
  bidirectional: boolean;
}

export interface ComplianceGraph {
  nodes: ComplianceGraphNode[];
  edges: ComplianceGraphEdge[];
  totalDocuments: number;
  totalRelationships: number;
}

export interface DocumentImpactSummary {
  affectedChapters: number;
  affectedStandards: number;
  affectedObjectiveElements: number;
  affectedEvidence: number;
  affectedFindings: number;
  affectedDeficiencies: number;
  affectedCapas: number;
  affectedTasks: number;
  affectedAuditSessions: number;
  affectedDepartments: number;
  publishingRisk: string;
  impactSummary: string;
}

export interface DocumentImpact {
  documentId: string;
  title: string;
  summary: DocumentImpactSummary;
  items: { category: string; id: string; label: string; route?: string | null }[];
}

export interface LinkedAsset {
  assetType: string;
  id: string;
  label: string;
  route?: string | null;
}

export interface DocumentIntelligence {
  documentId: string;
  documentNumber: string;
  title: string;
  health: DocumentHealthScore;
  complianceMappingCoveragePercent: number;
  acknowledgementPercent: number;
  linkedAssetsCount: number;
  dependenciesCount: number;
  aiConfidencePercent: number;
  reviewStatus: string;
  impact: DocumentImpactSummary;
  linkedAssets: LinkedAsset[];
  strengths: string[];
  risks: string[];
}

export interface DocumentDependencies {
  documentId: string;
  title: string;
  canArchive: boolean;
  canSupersede: boolean;
  canDelete: boolean;
  blockReason: string;
  usedBy: { category: string; count: number; examples?: string[] }[];
}

export interface DocumentRelationships {
  documentId: string;
  title: string;
  graph: ComplianceGraph;
}

export interface AiComplianceReview {
  overallComplianceReviewScore: number;
  strengths: string[];
  risks: string[];
  suggestions: { category: string; message: string; severity: string }[];
  isAdvisoryOnly: boolean;
}

export interface DocumentExplorerNode {
  id: string;
  label: string;
  nodeType: string;
  documentId?: string | null;
  route?: string | null;
  children: DocumentExplorerNode[];
}

export interface DocumentExplorer {
  tree: DocumentExplorerNode[];
  recent: ControlledDocumentListItem[];
  pinned: ControlledDocumentListItem[];
}

export interface DocumentSearchResult {
  documentId: string;
  documentNumber: string;
  title: string;
  matchContext: string;
  matchLocation: string;
  score: number;
  route?: string | null;
}

export interface DocumentHealthDashboard {
  healthyDocuments: number;
  needsReview: number;
  weakMapping: number;
  lowAcknowledgement: number;
  missingSections: number;
  aiReviewPending: number;
  highImpactDocuments: number;
  recentlyModified: number;
  topReferencedDocuments: number;
  affectingAccreditation: number;
  insights: { insightType: string; title: string; description: string; documentId?: string | null }[];
  topReferenced: ControlledDocumentListItem[];
}

export interface CrossReferenceSuggestion {
  referenceType: string;
  targetId: string;
  label: string;
  insertText: string;
  route?: string | null;
}
