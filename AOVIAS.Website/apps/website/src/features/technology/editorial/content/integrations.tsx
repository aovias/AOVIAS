import {
  ApiFirstPlatform,
  IntegrationEcosystem,
} from '@/components/brand';
import type { TechnologyEditorialContent } from '@/features/technology/editorial/types';

/**
 * Integrations Technology Editorial — architecture over logo walls.
 */
export const integrationsEditorialContent = {
  id: 'integrations',
  atmosphere: 'infrastructure-mesh',
  hero: {
    eyebrow: 'Integrations',
    title: 'Connect without creating complexity.',
    description:
      'AOVIAS integrations are architectural pathways — adapters, pipelines, events, and governed federation — designed so institutions connect systems without inventing a second source of truth.',
    primaryCta: {
      label: 'Platform APIs',
      href: '/technology/integrations/apis',
    },
    secondaryCta: {
      label: 'Developer platform',
      href: '/technology/developer',
    },
    illustration: (
      <IntegrationEcosystem className="xl:min-h-[20rem] 2xl:min-h-[24rem]" />
    ),
  },
  principle: {
    eyebrow: 'Core principle',
    title: 'Interoperability with boundaries.',
    description:
      'Connectivity should extend institutional capability. It should not multiply ownership gaps, silent data copies, or unmanaged vendor sprawl.',
  },
  architecture: {
    eyebrow: 'Architecture',
    title: 'Pipelines, adapters, and governed exchange.',
    description:
      'Illustrate the integration fabric — not a wall of logos. Services connect through deliberate contracts.',
    caption: 'Connect · Transform · Route · Observe · Govern',
    diagram: (
      <ApiFirstPlatform
        title="Integration architecture"
        className="mx-auto"
      />
    ),
  },
  story: {
    eyebrow: 'Technical story',
    title: 'How connections become operable.',
    description:
      'From connection to governance — one coherent integration narrative.',
    steps: ['Connect', 'Transform', 'Route', 'Observe', 'Govern', 'Evolve'],
  },
  practices: {
    eyebrow: 'Integration practice',
    title: 'Principles for durable connectivity.',
    description:
      'Extensibility without chaos. Ecosystem without badge collecting.',
    items: [
      {
        id: 'contracts',
        title: 'Contracts',
        description:
          'Explicit interfaces so change remains reviewable.',
      },
      {
        id: 'boundaries',
        title: 'Boundaries',
        description:
          'Purpose-bound data exchange with clear ownership.',
      },
      {
        id: 'events',
        title: 'Event streams',
        description:
          'Observable flows that support operational judgment.',
      },
      {
        id: 'adapters',
        title: 'Adapters',
        description:
          'Isolation layers that protect the platform from system drift.',
      },
      {
        id: 'federation',
        title: 'Identity federation',
        description:
          'Access that travels with institutional identity — not shadow accounts.',
      },
      {
        id: 'governance',
        title: 'Governance',
        description:
          'Every connection remains attributable and policy-aware.',
      },
    ],
  },
  workflow: {
    eyebrow: 'Workflow',
    title: 'Connect → Transform → Route → Observe → Govern',
    description:
      'The integration operating rhythm for institutional systems.',
    steps: ['Connect', 'Transform', 'Route', 'Observe', 'Govern'],
  },
  highlights: {
    eyebrow: 'Capabilities',
    title: 'What the integration fabric provides.',
    description:
      'Architecture first. Vendors second — if at all.',
    items: [
      {
        id: 'apis',
        title: 'Platform APIs',
        description:
          'Governed interfaces for Command, Studio, and institutional systems.',
      },
      {
        id: 'pipelines',
        title: 'Pipelines',
        description:
          'Reliable transform and route patterns for operational data.',
      },
      {
        id: 'events',
        title: 'Event streams',
        description:
          'Signals that keep connected systems inspectable.',
      },
      {
        id: 'adapters',
        title: 'Adapters',
        description:
          'Stable edges between platform services and external systems.',
      },
      {
        id: 'federation',
        title: 'Identity federation',
        description:
          'Enterprise identity continuity across connected environments.',
      },
      {
        id: 'observe',
        title: 'Observability',
        description:
          'Connection health and flow evidence for operators and architects.',
      },
    ],
  },
  closing: {
    title: 'Integrations as architecture — not afterthought connectors.',
    description:
      'Continue into platform APIs and developer enablement for evaluation depth.',
    primaryCta: {
      label: 'Explore Platform APIs',
      href: '/technology/integrations/apis',
    },
  },
} as const satisfies TechnologyEditorialContent;
