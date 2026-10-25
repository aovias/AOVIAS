import {
  avlfIllustrationIds,
  type AvlfIllustrationId,
} from './catalog';

/**
 * Component keys mapped in the Illustration Registry.
 * Actual React components live under `@/components/brand`.
 */
export const illustrationComponentKeys = [
  'PlatformOrchestration',
  'PlatformEcosystem',
  'ProductEcosystemFlow',
  'CommandOperations',
  'CommandMissionControl',
  'StudioDelivery',
  'StudioAuthoringWorkspace',
  'PlatformIntelligence',
  'PlatformServicesConsole',
  'HumanAiCollaboration',
  'SecurePlatform',
  'TrustEcosystem',
  'WorkflowPanel',
  'ArchitectureDiagram',
  'IdentityPlatformVisual',
  'KnowledgeCover',
  'InstitutionalScene',
  'DomainScene',
  'IndustryPlatformComposition',
] as const;

export type IllustrationComponentKey = (typeof illustrationComponentKeys)[number];

export type IllustrationRegistryEntry = {
  id: AvlfIllustrationId;
  category: string;
  purpose: string;
  /** Primary brand component for this registry ID. */
  componentKey: IllustrationComponentKey;
  /** Alternate components allowed for the same ID. */
  alternateKeys?: readonly IllustrationComponentKey[];
  themes: readonly ('light' | 'dark')[];
};

export const illustrationCatalog: Record<
  AvlfIllustrationId,
  IllustrationRegistryEntry
> = {
  'ill-platform-core': {
    id: 'ill-platform-core',
    category: 'Platform',
    purpose: 'Shared foundation metaphor',
    componentKey: 'PlatformOrchestration',
    alternateKeys: ['PlatformEcosystem', 'ProductEcosystemFlow'],
    themes: ['light', 'dark'],
  },
  'ill-command-terminal': {
    id: 'ill-command-terminal',
    category: 'Command',
    purpose: 'Operations control',
    componentKey: 'CommandOperations',
    alternateKeys: ['CommandMissionControl'],
    themes: ['light', 'dark'],
  },
  'ill-studio-workstation': {
    id: 'ill-studio-workstation',
    category: 'Studio',
    purpose: 'Delivery workspace',
    componentKey: 'StudioDelivery',
    alternateKeys: ['StudioAuthoringWorkspace'],
    themes: ['light', 'dark'],
  },
  'ill-ai-reasoning': {
    id: 'ill-ai-reasoning',
    category: 'AI',
    purpose: 'Assistive intelligence',
    componentKey: 'PlatformIntelligence',
    alternateKeys: ['HumanAiCollaboration', 'PlatformServicesConsole'],
    themes: ['light', 'dark'],
  },
  'ill-security-envelope': {
    id: 'ill-security-envelope',
    category: 'Security',
    purpose: 'Protection layers',
    componentKey: 'SecurePlatform',
    alternateKeys: ['TrustEcosystem'],
    themes: ['light', 'dark'],
  },
  'ill-workflow-token': {
    id: 'ill-workflow-token',
    category: 'Workflow',
    purpose: 'Orchestration path',
    componentKey: 'WorkflowPanel',
    themes: ['light', 'dark'],
  },
  'ill-deployment-rack': {
    id: 'ill-deployment-rack',
    category: 'Deployment',
    purpose: 'Infrastructure presence',
    componentKey: 'ArchitectureDiagram',
    themes: ['light', 'dark'],
  },
  'ill-architecture-layers': {
    id: 'ill-architecture-layers',
    category: 'Architecture',
    purpose: 'Layer boundaries',
    componentKey: 'ArchitectureDiagram',
    themes: ['light', 'dark'],
  },
  'ill-identity-flow': {
    id: 'ill-identity-flow',
    category: 'Identity',
    purpose: 'AuthZ/AuthN path',
    componentKey: 'IdentityPlatformVisual',
    themes: ['light', 'dark'],
  },
  'ill-knowledge-graph': {
    id: 'ill-knowledge-graph',
    category: 'Knowledge',
    purpose: 'Relationships / findability',
    componentKey: 'KnowledgeCover',
    themes: ['light', 'dark'],
  },
  'ill-trust-rings': {
    id: 'ill-trust-rings',
    category: 'Trust',
    purpose: 'Concentric assurance',
    componentKey: 'TrustEcosystem',
    themes: ['light', 'dark'],
  },
  'ill-company-institution': {
    id: 'ill-company-institution',
    category: 'Company',
    purpose: 'Institutional presence',
    componentKey: 'InstitutionalScene',
    alternateKeys: ['DomainScene', 'IndustryPlatformComposition'],
    themes: ['light', 'dark'],
  },
};

export function getIllustrationEntry(
  id: AvlfIllustrationId,
): IllustrationRegistryEntry | undefined {
  return illustrationCatalog[id];
}

export function listIllustrationIds(): readonly AvlfIllustrationId[] {
  return avlfIllustrationIds;
}

export function isAvlfIllustrationId(value: string): value is AvlfIllustrationId {
  return (avlfIllustrationIds as readonly string[]).includes(value);
}
