import {
  ArchitectureDiagram,
  PlatformOrchestration,
} from '@/components/brand';
import type { TechnologyEditorialContent } from '@/features/technology/editorial/types';

/**
 * Platform Technology Editorial — foundational architecture guide.
 */
export const platformEditorialContent = {
  id: 'platform',
  atmosphere: 'blueprint-grid',
  hero: {
    eyebrow: 'Platform',
    title: 'One foundation powering every capability.',
    description:
      'AOVIAS Platform is the shared technology base beneath Command, Studio, and every institutional product. Built once. Inherited everywhere.',
    primaryCta: {
      label: 'Explore products',
      href: '/products',
    },
    secondaryCta: {
      label: 'Platform services',
      href: '/technology/platform/services',
    },
    illustration: (
      <PlatformOrchestration className="xl:min-h-[20rem] 2xl:min-h-[24rem]" />
    ),
  },
  principle: {
    eyebrow: 'Core principle',
    title: 'Coherence before accumulation.',
    description:
      'Institutions do not need another stack of disconnected tools. A platform delivers shared capability once — identity, policy, runtime, publishing, observation — then specializes through products.',
  },
  architecture: {
    eyebrow: 'Architecture',
    title: 'Layered foundation, clear product planes.',
    description:
      'Platform services form the structural core. Products consume them without reinventing identity, security, or deployment.',
    caption: 'Shared foundation · product planes · institutional outcomes',
    diagram: (
      <ArchitectureDiagram
        title="AOVIAS Platform architecture"
        className="mx-auto"
      />
    ),
  },
  story: {
    eyebrow: 'Technical story',
    title: 'How the foundation operates.',
    description:
      'A single institutional pathway — from who you are to how the system improves.',
    steps: [
      'Identity',
      'Policy',
      'Services',
      'Deployment',
      'Observation',
      'Continuous improvement',
    ],
  },
  practices: {
    eyebrow: 'Platform practice',
    title: 'Principles that keep the foundation durable.',
    description:
      'Engineering decisions favor longevity, isolation, and governance over short-cycle novelty.',
    items: [
      {
        id: 'versioning',
        title: 'Versioning',
        description:
          'Compatible evolution so institutions adopt change without forced rewrites.',
      },
      {
        id: 'deployment',
        title: 'Deployment',
        description:
          'Governed pathways into institution-scoped environments.',
      },
      {
        id: 'isolation',
        title: 'Isolation',
        description:
          'Clear tenancy and service boundaries that protect shared strength.',
      },
      {
        id: 'governance',
        title: 'Governance',
        description:
          'Policy and ownership treated as platform requirements.',
      },
      {
        id: 'observability',
        title: 'Observability',
        description:
          'Signals designed for judgment — not noise for dashboards alone.',
      },
      {
        id: 'reliability',
        title: 'Reliability',
        description:
          'Continuity patterns institutions can plan around.',
      },
    ],
  },
  workflow: {
    eyebrow: 'Workflow',
    title: 'Request → Deploy → Observe → Improve',
    description:
      'The operating rhythm of the platform — deliberate, inspectable, continuous.',
    steps: ['Request', 'Deploy', 'Observe', 'Improve'],
  },
  highlights: {
    eyebrow: 'Capabilities',
    title: 'What every product inherits.',
    description:
      'Premium foundation services — not optional add-ons.',
    items: [
      {
        id: 'identity',
        title: 'Identity',
        description:
          'Enterprise identity and role-aware access across the portfolio.',
      },
      {
        id: 'deployment',
        title: 'Deployment',
        description:
          'Institution-scoped environments with governed release discipline.',
      },
      {
        id: 'runtime',
        title: 'Runtime',
        description:
          'Shared execution patterns products rely on without divergence.',
      },
      {
        id: 'publishing',
        title: 'Publishing',
        description:
          'Controlled pathways from authored work to operational use.',
      },
      {
        id: 'observability',
        title: 'Observability',
        description:
          'Consistent signals for operators, architects, and auditors.',
      },
      {
        id: 'intelligence',
        title: 'Intelligence orchestration',
        description:
          'Assisted capability under shared governance — applied where it serves.',
      },
    ],
  },
  closing: {
    title: 'The foundation everything else stands on.',
    description:
      'Evaluate how Platform strengthens Command and Studio — or continue into architecture depth.',
    primaryCta: {
      label: 'Explore Platform architecture',
      href: '/technology/architecture',
    },
  },
} as const satisfies TechnologyEditorialContent;
