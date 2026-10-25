/**
 * AVLF typed catalogs — Phase V1 data only.
 * Do not import from page features in V1.
 * Source of truth for prose: docs/design-language/registries/*
 */

export const avlfAtmosphereIds = [
  'enterprise',
  'aurora',
  'blueprint',
  'workflow',
  'architecture',
  'knowledge',
  'security',
  'ai',
  'platform',
  'corporate',
  'minimal',
] as const;

export type AvlfAtmosphereId = (typeof avlfAtmosphereIds)[number];

/** Compatibility map → live EnterpriseAtmosphere keys */
export const avlfAtmosphereCompatibility = {
  enterprise: 'enterprise-abstract',
  aurora: 'aurora',
  blueprint: 'blueprint-grid',
  workflow: 'workflow',
  architecture: 'architecture',
  knowledge: 'knowledge-pattern',
  security: 'security-geometry',
  ai: 'ai-neural-network',
  platform: 'platform-grid',
  corporate: 'corporate-gradient',
  minimal: 'minimal-neutral',
} as const satisfies Record<AvlfAtmosphereId, string>;

export const avlfHeroIds = [
  'hero-home',
  'hero-product',
  'hero-technology',
  'hero-platform',
  'hero-engineering',
  'hero-ai',
  'hero-security',
  'hero-company',
  'hero-industry',
  'hero-knowledge',
  'hero-pricing',
  'hero-contact',
  'hero-careers',
] as const;

export type AvlfHeroId = (typeof avlfHeroIds)[number];

export const avlfIllustrationIds = [
  'ill-platform-core',
  'ill-command-terminal',
  'ill-studio-workstation',
  'ill-ai-reasoning',
  'ill-security-envelope',
  'ill-workflow-token',
  'ill-deployment-rack',
  'ill-architecture-layers',
  'ill-identity-flow',
  'ill-knowledge-graph',
  'ill-trust-rings',
  'ill-company-institution',
] as const;

export type AvlfIllustrationId = (typeof avlfIllustrationIds)[number];

export const avlfDiagramIds = [
  'diag-editorial-hierarchy',
  'diag-isometric-ecosystem',
  'diag-executive-overview',
  'diag-workflow-lanes',
  'diag-architecture-layers',
  'diag-governance-board',
  'diag-ecosystem-mesh',
  'diag-pipeline',
  'diag-deployment',
  'diag-ai-orchestration',
  'diag-identity-flow',
] as const;

export type AvlfDiagramId = (typeof avlfDiagramIds)[number];

export const avlfHeroCatalog: Record<
  AvlfHeroId,
  { atmosphere: AvlfAtmosphereId; label: string }
> = {
  'hero-home': { atmosphere: 'enterprise', label: 'Homepage Hero' },
  'hero-product': { atmosphere: 'workflow', label: 'Product Hero' },
  'hero-technology': { atmosphere: 'platform', label: 'Technology Hero' },
  'hero-platform': { atmosphere: 'platform', label: 'Platform Hero' },
  'hero-engineering': { atmosphere: 'blueprint', label: 'Engineering Hero' },
  'hero-ai': { atmosphere: 'ai', label: 'AI Hero' },
  'hero-security': { atmosphere: 'security', label: 'Security Hero' },
  'hero-company': { atmosphere: 'corporate', label: 'Company Hero' },
  'hero-industry': { atmosphere: 'corporate', label: 'Industry Hero' },
  'hero-knowledge': { atmosphere: 'knowledge', label: 'Knowledge Hero' },
  'hero-pricing': { atmosphere: 'minimal', label: 'Pricing Hero' },
  'hero-contact': { atmosphere: 'aurora', label: 'Contact Hero' },
  'hero-careers': { atmosphere: 'enterprise', label: 'Careers Hero' },
};
