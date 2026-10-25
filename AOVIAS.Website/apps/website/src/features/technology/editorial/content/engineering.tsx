import {
  ArchitectureDiagram,
  EngineeringLifecycle,
} from '@/components/brand';
import type { TechnologyEditorialContent } from '@/features/technology/editorial/types';

/**
 * Engineering Technology Editorial — precision, craftsmanship, reliability.
 */
export const engineeringEditorialContent = {
  id: 'engineering',
  atmosphere: 'architecture',
  hero: {
    eyebrow: 'Engineering',
    title: 'Reliability through disciplined engineering.',
    description:
      'AOVIAS engineering is a handbook of deliberate practice — architecture, quality, automation, and operational excellence for software institutions depend on.',
    primaryCta: {
      label: 'Architecture',
      href: '/technology/engineering/architecture',
    },
    secondaryCta: {
      label: 'Reliability',
      href: '/technology/engineering/reliability',
    },
    illustration: (
      <EngineeringLifecycle className="xl:min-h-[20rem] 2xl:min-h-[24rem]" />
    ),
  },
  principle: {
    eyebrow: 'Core principle',
    title: 'Discipline compounds trust.',
    description:
      'Institutional software earns confidence through versioning, testing, isolation, and observable delivery — not through speed theater or novelty cycles.',
  },
  architecture: {
    eyebrow: 'Architecture',
    title: 'Modular systems with durable boundaries.',
    description:
      'Architecture exists to make change safe: clear modules, shared standards, and interfaces institutions can evaluate.',
    caption: 'Design for longevity · change without chaos',
    diagram: (
      <ArchitectureDiagram
        title="AOVIAS engineering architecture"
        className="mx-auto"
      />
    ),
  },
  story: {
    eyebrow: 'Technical story',
    title: 'How work becomes operable software.',
    description:
      'From design intent to continuous operation — one coherent engineering narrative.',
    steps: ['Design', 'Build', 'Validate', 'Release', 'Operate', 'Improve'],
  },
  practices: {
    eyebrow: 'Engineering practice',
    title: 'Principles from a modern engineering handbook.',
    description:
      'Each practice exists to protect institutional duty under change.',
    items: [
      {
        id: 'versioning',
        title: 'Versioning',
        description:
          'Compatible evolution with clear ownership of breaking change.',
      },
      {
        id: 'deployment',
        title: 'Deployment',
        description:
          'Release pathways that are reviewable, reversible, and scoped.',
      },
      {
        id: 'reliability',
        title: 'Reliability',
        description:
          'Failure modes anticipated; recovery treated as a first-class design.',
      },
      {
        id: 'scalability',
        title: 'Scalability',
        description:
          'Growth across tenants, workloads, and regulatory contexts.',
      },
      {
        id: 'isolation',
        title: 'Isolation',
        description:
          'Fault and data boundaries that limit blast radius.',
      },
      {
        id: 'observability',
        title: 'Observability',
        description:
          'Evidence for operators and evaluators — not vanity metrics.',
      },
    ],
  },
  workflow: {
    eyebrow: 'Workflow',
    title: 'Design → Build → Validate → Release → Operate',
    description:
      'The delivery rhythm expected of institutional-grade software.',
    steps: ['Design', 'Build', 'Validate', 'Release', 'Operate'],
  },
  highlights: {
    eyebrow: 'Capabilities',
    title: 'What engineering excellence looks like in practice.',
    description:
      'Craftsmanship expressed as durable systems — not slogans.',
    items: [
      {
        id: 'architecture',
        title: 'Architecture',
        description:
          'Modular design with explicit boundaries and shared standards.',
      },
      {
        id: 'testing',
        title: 'Testing',
        description:
          'Validation that protects intent across change and release.',
      },
      {
        id: 'automation',
        title: 'Automation',
        description:
          'Repeatable delivery that reduces human error without hiding ownership.',
      },
      {
        id: 'quality',
        title: 'Quality',
        description:
          'Quality bars applied across the platform, not product by product.',
      },
      {
        id: 'release',
        title: 'Release',
        description:
          'Governed shipping with rollback and review discipline.',
      },
      {
        id: 'operate',
        title: 'Operational excellence',
        description:
          'Runbooks, signals, and continuity patterns for long-lived duty.',
      },
    ],
  },
  closing: {
    title: 'Engineering you can evaluate with rigor.',
    description:
      'Continue into architecture depth, reliability models, and observability practice.',
    primaryCta: {
      label: 'Explore engineering architecture',
      href: '/technology/engineering/architecture',
    },
  },
} as const satisfies TechnologyEditorialContent;
