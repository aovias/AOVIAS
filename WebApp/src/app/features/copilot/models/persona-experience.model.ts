import type { CopilotRecommendation } from './copilot-recommendation.model';
import type { AiDraft } from './ai-draft.model';
import type { CopilotActionProposal } from './proposal.model';

export interface PersonaExperienceDefinition {
  persona: string;
  displayName: string;
  description: string;
  icon: string;
  theme: string;
  primaryObjectives: string[];
  recommendedActions: string[];
  defaultQuestions: string[];
  priorityCategories: string[];
  supportedDraftTypes: number[];
  supportedProposalTypes: number[];
}

export interface PersonaLandingState {
  persona: string;
  headline: string;
  subheadline: string;
  focusArea?: string | null;
  readinessScore?: number | null;
  topRecommendationTitle?: string | null;
  keyKpiLabel?: string | null;
  keyKpiValue?: string | null;
}

export interface PersonaWorkspace {
  definition: PersonaExperienceDefinition;
  landing: PersonaLandingState;
  prioritizedRecommendations: CopilotRecommendation[];
  highlightedDrafts: AiDraft[];
  highlightedProposals: CopilotActionProposal[];
  suggestedQuestions: string[];
}

export interface CopilotActivityEvent {
  id: string;
  eventType: string;
  persona?: string | null;
  actorId?: string | null;
  actorEmail?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  notes?: string | null;
  occurredAtUtc: string;
}

export interface CopilotUsageMetrics {
  personaUsageCounts: Record<string, number>;
  draftsGenerated: number;
  proposalsApproved: number;
  proposalsExecuted: number;
  recommendationsEngaged: number;
  workspaceViews: number;
  personaChanges: number;
  metricsAsOfUtc?: string | null;
}

export interface RecordCopilotActivityRequest {
  eventType: string;
  persona?: string;
  entityType?: string;
  entityId?: string;
  notes?: string;
}

export type CopilotWorkspaceTab = 'overview' | 'recommendations' | 'drafts' | 'approvals' | 'history' | 'analytics';

export const COPILOT_ACTIVITY = {
  RecommendationGenerated: 'RecommendationGenerated',
  DraftCreated: 'DraftCreated',
  ProposalApproved: 'ProposalApproved',
  ProposalExecuted: 'ProposalExecuted',
  PersonaChanged: 'PersonaChanged',
  WorkspaceViewed: 'WorkspaceViewed',
  RecommendationViewed: 'RecommendationViewed',
} as const;

export const PERSONA_OVERRIDE_SESSION_KEY = 'copilot.personaOverride';
