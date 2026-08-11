import { CopilotPersona } from './copilot-persona.model';

/**
 * Runtime context for the Copilot session.
 * Mirrors backend CopilotContext domain model.
 * Separate from AIContextBundle — can compose it when generation logic arrives in Sprint 13B.
 */
export interface CopilotContext {
  organizationId: string;
  pageContext: string;
  focusedEntityId?: string | null;
  focusedEntityType?: string | null;
  conversationSummary?: string | null;
  recentEntityRefs?: string[];
  activePersona: CopilotPersona;
  personaOverride?: CopilotPersona | null;
  currentStage?: string | null;
  readinessScore?: number | null;
  analyticsSummary?: string | null;
}

/** Request payload sent to the persona resolution endpoint. */
export interface CopilotContextRequest {
  pageContext?: string;
  focusedEntityType?: string | null;
  personaOverride?: string | null;
}
