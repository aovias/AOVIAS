import { DomainScene, PlatformOrchestration } from '@/components/brand';
import type { CommerceEditorialContent } from '@/features/commerce/editorial/types';

/**
 * Pricing / commercial engagement editorial content (Phase V9).
 * Atmosphere: Commercial Horizon (enterprise-abstract).
 */
export const pricingCommerceContent = {
  id: 'pricing',
  atmosphere: 'enterprise-abstract',
  hero: {
    eyebrow: 'Commercial',
    title: 'A partnership built for institutional adoption.',
    description:
      'AOVIAS commercial engagement is designed for procurement teams, CIOs, and technical evaluators — transparent licensing, scalable adoption, and long-term operational partnership.',
    primaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Talk with enterprise',
      href: '/company/contact#form-enterprise',
    },
    illustration: (
      <PlatformOrchestration
        title="Enterprise adoption partnership"
        className="xl:min-h-[20rem]"
      />
    ),
  },
  philosophy: {
    title: 'Pricing philosophy before plans.',
    description:
      'We explain how AOVIAS engages commercially before presenting list prices — so buyers evaluate partnership fit, not promotional packaging.',
    principles: [
      {
        id: 'transparent',
        title: 'Transparent licensing',
        description:
          'Published packaging for evaluation, with enterprise terms available when institutional constraints require them.',
      },
      {
        id: 'scalable',
        title: 'Scalable adoption',
        description:
          'Start with a focused team, then expand across products and sites without rewriting commercial foundations.',
      },
      {
        id: 'flexible',
        title: 'Enterprise flexibility',
        description:
          'Deployment, identity, and governance options that respect procurement and security review.',
      },
      {
        id: 'partnership',
        title: 'Long-term partnership',
        description:
          'Success is measured in operational continuity — not short-term conversion tactics.',
      },
    ],
  },
  plans: {
    title: 'Evaluate. Professional. Enterprise.',
    description:
      'Three engagement models. Differences reflect organizational needs — not artificial upgrade pressure.',
    summaries: [
      {
        id: 'evaluate',
        name: 'Evaluate',
        audience: 'Teams validating Studio for institutional delivery practice.',
        deployment: 'Standard cloud tenancy for evaluation and early adoption.',
        support: 'Standard support with documentation-led onboarding.',
        governance: 'Core controls and limited audit retention.',
        engagement: 'Self-serve checkout for published list pricing.',
      },
      {
        id: 'professional',
        name: 'Professional',
        audience: 'Organizations scaling Command or Studio across multiple teams.',
        deployment: 'Institution-scoped environments with SSO options.',
        support: 'Priority support aligned to operational calendars.',
        governance: 'Expanded auditability and collaboration controls.',
        engagement: 'Self-serve or guided commercial conversation.',
      },
      {
        id: 'enterprise',
        name: 'Enterprise',
        audience: 'Institutions with procurement, security, and multi-site requirements.',
        deployment: 'Enterprise deployment options and governed rollout.',
        support: 'Dedicated success and implementation pathways.',
        governance: 'Full governance, identity, and assurance posture.',
        engagement: 'Sales-assisted licensing — not consumer checkout.',
      },
    ],
  },
  comparison: {
    title: 'Procurement-friendly comparison',
    description:
      'Capability dimensions evaluators typically assess — without marketing fluff.',
    columns: [
      { id: 'evaluate', name: 'Evaluate' },
      { id: 'professional', name: 'Professional' },
      { id: 'enterprise', name: 'Enterprise' },
    ],
    rows: [
      {
        id: 'deployment',
        label: 'Deployment',
        values: {
          evaluate: 'Standard cloud tenancy',
          professional: 'Institution-scoped environments',
          enterprise: 'Enterprise deployment options',
        },
      },
      {
        id: 'security',
        label: 'Security',
        values: {
          evaluate: 'Platform baseline controls',
          professional: 'Baseline + expanded audit',
          enterprise: 'Full institutional security posture',
        },
      },
      {
        id: 'identity',
        label: 'Identity',
        values: {
          evaluate: 'Standard authentication',
          professional: 'SSO options',
          enterprise: 'Enterprise identity integration',
        },
      },
      {
        id: 'ai',
        label: 'AI',
        values: {
          evaluate: 'Governed assistance where available',
          professional: 'Governed assistance with oversight',
          enterprise: 'Policy-bound intelligence under governance',
        },
      },
      {
        id: 'governance',
        label: 'Governance',
        values: {
          evaluate: 'Core ownership trails',
          professional: 'Expanded auditability',
          enterprise: 'Assurance-ready governance',
        },
      },
      {
        id: 'support',
        label: 'Support',
        values: {
          evaluate: 'Standard support',
          professional: 'Priority support',
          enterprise: 'Dedicated success pathways',
        },
      },
      {
        id: 'integrations',
        label: 'Integrations',
        values: {
          evaluate: 'Documented platform APIs',
          professional: 'Governed integrations',
          enterprise: 'Enterprise integration engagement',
        },
      },
      {
        id: 'scalability',
        label: 'Scalability',
        values: {
          evaluate: 'Team-scale adoption',
          professional: 'Multi-team scale',
          enterprise: 'Multi-site institutional scale',
        },
      },
    ],
  },
  illustrations: {
    title: 'Illustrations that reinforce trust',
    description:
      'Visual anchors for how institutions evaluate, deploy, and expand AOVIAS — not decorative graphics.',
    items: [
      {
        id: 'procurement',
        title: 'Procurement workflow',
        description:
          'From evaluation packaging through security review and commercial agreement.',
        illustration: (
          <DomainScene
            title="Procurement workflow"
            caption="Evaluation → review → agreement"
            variant="operations"
          />
        ),
      },
      {
        id: 'deployment',
        title: 'Enterprise deployment journey',
        description:
          'Governed rollout across teams and sites with institutional controls.',
        illustration: (
          <DomainScene
            title="Enterprise deployment"
            caption="Pilot → rollout → scale"
            variant="deployment"
          />
        ),
      },
      {
        id: 'success',
        title: 'Customer success lifecycle',
        description:
          'Ongoing partnership after go-live — adoption, support, and expansion.',
        illustration: (
          <DomainScene
            title="Customer success"
            caption="Onboard → operate → expand"
            variant="enterprise"
          />
        ),
      },
      {
        id: 'roadmap',
        title: 'Platform adoption roadmap',
        description:
          'Studio, Command, and Platform adoption sequenced to organizational readiness.',
        illustration: (
          <DomainScene
            title="Adoption roadmap"
            caption="Capability → integration → scale"
            variant="architecture"
          />
        ),
      },
    ],
  },
  journey: {
    title: 'Discovery → Evaluation → Partnership',
    description:
      'The commercial relationship from first conversation through long-term expansion.',
    steps: [
      'Discovery',
      'Technical evaluation',
      'Solution workshop',
      'Pilot',
      'Deployment',
      'Expansion',
      'Long-term partnership',
    ],
  },
  closing: {
    title: 'Begin with consultation — not pressure.',
    description:
      'Request a demonstration, speak with enterprise, or review published packaging with your procurement stakeholders.',
    primaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Contact enterprise',
      href: '/company/contact#form-enterprise',
    },
  },
} as const satisfies CommerceEditorialContent;
