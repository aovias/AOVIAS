import {
  IdentityPlatformVisual,
  SecurePlatform,
} from '@/components/brand';
import type { TechnologyEditorialContent } from '@/features/technology/editorial/types';

/**
 * Security Technology Editorial — trust designed in, not advertised.
 * Route remains /technology/trust (Security hub).
 */
export const securityEditorialContent = {
  id: 'security',
  atmosphere: 'security-geometry',
  hero: {
    eyebrow: 'Security',
    title: 'Trust is designed into every layer.',
    description:
      'AOVIAS treats security as platform architecture — identity, policy, encryption, audit, and resilience — calm confidence for institutions that cannot improvise trust.',
    primaryCta: {
      label: 'Identity',
      href: '/technology/trust/identity',
    },
    secondaryCta: {
      label: 'Data protection',
      href: '/technology/trust/data-protection',
    },
    illustration: (
      <SecurePlatform className="xl:min-h-[20rem] 2xl:min-h-[24rem]" />
    ),
  },
  principle: {
    eyebrow: 'Core principle',
    title: 'Designed in — not bolted on.',
    description:
      'Security is a structural property of the platform. Controls, boundaries, and evidence pathways exist because institutional duty requires them — not because a checklist demanded a badge.',
  },
  architecture: {
    eyebrow: 'Architecture',
    title: 'Identity, policy, protection, and stewardship.',
    description:
      'Visualize control as layered practice: who you are, what you may do, how data is protected, and how actions remain attributable.',
    caption: 'Identity · Policy · Encryption · Audit · Stewardship',
    diagram: (
      <IdentityPlatformVisual
        title="Security as platform architecture"
        className="mx-auto"
      />
    ),
  },
  story: {
    eyebrow: 'Technical story',
    title: 'How trust travels through the system.',
    description:
      'A continuous path from authentication to recovery — without cyber cliché.',
    steps: [
      'Authenticate',
      'Authorize',
      'Protect',
      'Audit',
      'Recover',
      'Improve',
    ],
  },
  practices: {
    eyebrow: 'Security practice',
    title: 'Governance expressed as engineering.',
    description:
      'Calm confidence: protection, accountability, and operational resilience.',
    items: [
      {
        id: 'identity',
        title: 'Identity',
        description:
          'Enterprise identity as the root of access and attribution.',
      },
      {
        id: 'policy',
        title: 'Policy',
        description:
          'Authorization patterns shared across products and services.',
      },
      {
        id: 'encryption',
        title: 'Encryption',
        description:
          'Protection of data in motion and at rest as a platform baseline.',
      },
      {
        id: 'audit',
        title: 'Audit',
        description:
          'Evidence-friendly records of consequential actions.',
      },
      {
        id: 'stewardship',
        title: 'Stewardship',
        description:
          'Clear ownership of data boundaries and institutional duty.',
      },
      {
        id: 'resilience',
        title: 'Operational resilience',
        description:
          'Recovery and continuity designed alongside protection.',
      },
    ],
  },
  workflow: {
    eyebrow: 'Workflow',
    title: 'Authenticate → Authorize → Protect → Audit → Recover',
    description:
      'The security operating rhythm institutions can evaluate.',
    steps: ['Authenticate', 'Authorize', 'Protect', 'Audit', 'Recover'],
  },
  highlights: {
    eyebrow: 'Capabilities',
    title: 'Controls as product fundamentals.',
    description:
      'What technical evaluators should expect from a mature platform.',
    items: [
      {
        id: 'encryption',
        title: 'Encryption',
        description:
          'Baseline cryptographic protection across platform services.',
      },
      {
        id: 'audit',
        title: 'Audit',
        description:
          'Attributable actions that support oversight and assurance.',
      },
      {
        id: 'policy',
        title: 'Policy',
        description:
          'Consistent authorization without product-by-product reinvention.',
      },
      {
        id: 'compliance',
        title: 'Compliance readiness',
        description:
          'Evidence pathways designed for institutional evaluation.',
      },
      {
        id: 'identity',
        title: 'Identity',
        description:
          'Role-aware access rooted in enterprise identity providers.',
      },
      {
        id: 'privacy',
        title: 'Privacy posture',
        description:
          'Purpose-bound data use with clear institutional ownership.',
      },
    ],
  },
  closing: {
    title: 'Security that feels designed — not advertised.',
    description:
      'Continue into identity, privacy, compliance, and data protection depth.',
    primaryCta: {
      label: 'Explore identity foundation',
      href: '/technology/trust/identity',
    },
  },
} as const satisfies TechnologyEditorialContent;
