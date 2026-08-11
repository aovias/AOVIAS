/**
 * Mirrors backend CopilotPersona enum (Domain/Copilot/CopilotPersona.cs).
 * Single Copilot, multiple persona modes.
 */
export enum CopilotPersona {
  AccreditationCoach      = 'AccreditationCoach',
  QualityManagerAssistant = 'QualityManagerAssistant',
  EvidenceAdvisor         = 'EvidenceAdvisor',
  CapaAdvisor             = 'CapaAdvisor',
  ExecutiveAdvisor        = 'ExecutiveAdvisor',
  DepartmentHeadAssistant = 'DepartmentHeadAssistant',
  AuditorAssistant        = 'AuditorAssistant',
}

export interface PersonaInfo {
  id: CopilotPersona;
  displayName: string;
  description: string;
  pageContexts: string[];
  isActive: boolean;
}

/** Display-friendly labels for UI rendering. */
export const PERSONA_DISPLAY_NAMES: Record<CopilotPersona, string> = {
  [CopilotPersona.AccreditationCoach]:      'Accreditation Coach',
  [CopilotPersona.QualityManagerAssistant]: 'Quality Manager Assistant',
  [CopilotPersona.EvidenceAdvisor]:         'Evidence Advisor',
  [CopilotPersona.CapaAdvisor]:             'CAPA Advisor',
  [CopilotPersona.ExecutiveAdvisor]:        'Executive Advisor',
  [CopilotPersona.DepartmentHeadAssistant]: 'Department Head Assistant',
  [CopilotPersona.AuditorAssistant]:        'Auditor Assistant',
};
