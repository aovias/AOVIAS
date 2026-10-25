/**
 * Integrations & Future section copy — Sprint T6.
 * Extensibility and evolution. No API docs, vendors, dates, or unreleased promises.
 */

export const ECOSYSTEM_GUIDING_LINE =
  'AOVIAS integrates with institutional ecosystems — it does not isolate them.';

export const FUTURE_VISION_NOTE =
  'This describes strategic direction. Detailed documentation and tooling will arrive through future developer resources — not as commitments on this page.';

export const integrationsContent = {
  hero: {
    eyebrow: 'Integrations',
    title: 'Built to connect with the systems institutions already trust',
    description:
      'AOVIAS is designed for interoperability. The platform fits into enterprise technology landscapes — identity, clinical and government systems, ERP, CRM, communications, and analytics — without requiring a closed world.',
    primaryCta: {
      label: 'Platform APIs',
      href: '/technology/integrations/apis',
    },
    secondaryCta: {
      label: 'Technology roadmap',
      href: '/technology/roadmap',
    },
  },
  guidingLine: ECOSYSTEM_GUIDING_LINE,
  why: {
    eyebrow: 'Why integrations matter',
    title: 'Institutions run on connected systems',
    description:
      'Critical work already spans many applications. A platform that cannot connect becomes a burden. Integration is how AOVIAS respects existing investment while extending capability.',
  },
  connectivity: {
    eyebrow: 'Enterprise connectivity',
    title: 'Conceptual classes of connection',
    description:
      'We describe categories of systems — not vendor names — unless an official support relationship is published elsewhere.',
  },
  categories: [
    {
      title: 'Identity providers',
      description: 'Connect institutional access models to platform identity capabilities.',
    },
    {
      title: 'Healthcare systems',
      description: 'Participate in clinical and operational landscapes with clear boundaries.',
    },
    {
      title: 'Government platforms',
      description: 'Align with public-sector systems and program contexts.',
    },
    {
      title: 'ERP',
      description: 'Relate institutional resource and finance systems to platform workflows.',
    },
    {
      title: 'CRM',
      description: 'Connect relationship and constituent systems where institutions require it.',
    },
    {
      title: 'Communication services',
      description: 'Support notifications and collaboration channels institutions already use.',
    },
    {
      title: 'Analytics platforms',
      description: 'Enable insight pipelines without trapping data in a closed silo.',
    },
  ],
  principles: [
    {
      title: 'Open by design',
      description: 'Interoperability is an architectural intent, not an afterthought.',
    },
    {
      title: 'Clear boundaries',
      description: 'Integrations respect security, privacy, and accountability expectations.',
    },
    {
      title: 'Sustainable coupling',
      description: 'Connections should remain maintainable as both sides evolve.',
    },
    {
      title: 'Institutional ownership',
      description: 'Customers govern what connects and why — the platform enables, it does not seize control.',
    },
  ],
  future: {
    eyebrow: 'Future ecosystem',
    title: 'Expand carefully as the platform matures',
    description:
      'Ecosystem growth follows architecture and trust. See the Technology Roadmap for capability themes — without dates or speculative product lists.',
  },
} as const;

export const apisContent = {
  hero: {
    eyebrow: 'Platform APIs · Future',
    title: 'API-first thinking for an extensible platform',
    description:
      'AOVIAS approaches the platform as a set of capabilities that can be composed securely. Detailed API documentation will be published in future developer resources — not on this page.',
    primaryCta: {
      label: 'Developer platform',
      href: '/technology/developer',
    },
    secondaryCta: {
      label: 'Integrations',
      href: '/technology/integrations',
    },
  },
  note: FUTURE_VISION_NOTE,
  topics: [
    {
      title: 'API-first thinking',
      description:
        'Capabilities are shaped so products, partners, and automation can compose them responsibly over time.',
    },
    {
      title: 'Secure APIs',
      description:
        'Programmatic access inherits platform expectations for identity, authorization, and stewardship.',
    },
    {
      title: 'Extensible architecture',
      description:
        'APIs support growth without requiring every need to become a bespoke product fork.',
    },
    {
      title: 'Platform services',
      description:
        'Shared services become accessible in a consistent way as the API surface matures.',
    },
    {
      title: 'Future automation',
      description:
        'Well-governed interfaces enable automation while keeping humans accountable for critical outcomes.',
    },
  ],
} as const;

export const developerContent = {
  hero: {
    eyebrow: 'Developer Platform · Future',
    title: 'A strategic direction for institutional builders',
    description:
      'Over time, AOVIAS intends to enable developers and technical teams to extend the platform responsibly. This is a vision of direction — not a currently available product catalogue.',
    primaryCta: {
      label: 'Platform APIs',
      href: '/technology/integrations/apis',
    },
    secondaryCta: {
      label: 'Technology roadmap',
      href: '/technology/roadmap',
    },
  },
  note: FUTURE_VISION_NOTE,
  topics: [
    {
      title: 'SDK philosophy',
      description:
        'Client libraries, when introduced, should make correct and secure use easier than incorrect use.',
    },
    {
      title: 'Extensibility',
      description:
        'Builders should compose platform capabilities within clear governance boundaries.',
    },
    {
      title: 'Automation',
      description:
        'Repetitive integration and operational tasks can be automated without removing accountability.',
    },
    {
      title: 'Integrations',
      description:
        'Developer surfaces reinforce the same interoperability principles as institutional integrations.',
    },
    {
      title: 'Developer experience',
      description:
        'Clarity, consistency, and documentation quality matter as much as raw capability.',
    },
    {
      title: 'Future tooling',
      description:
        'Tooling will evolve with the platform. We do not list speculative features or release dates here.',
    },
  ],
} as const;

export const roadmapContent = {
  hero: {
    eyebrow: 'Technology roadmap',
    title: 'Long-term evolution, not feature marketing',
    description:
      'The roadmap shows how capability themes build on one another. It is a model of progression — without dates, release commitments, or speculative promises.',
    primaryCta: {
      label: 'Why AOVIAS Technology',
      href: '/technology/why',
    },
    secondaryCta: {
      label: 'Integrations',
      href: '/technology/integrations',
    },
  },
  note: 'This is not a delivery schedule. Themes may deepen or reorder as institutional needs and platform maturity require.',
  stages: [
    {
      title: 'Foundation',
      description: 'Shared platform principles, architecture, and engineering discipline.',
    },
    {
      title: 'Platform Services',
      description: 'Reusable capabilities that products compose instead of reinventing.',
    },
    {
      title: 'Artificial Intelligence',
      description: 'Responsible assistance that amplifies human judgment.',
    },
    {
      title: 'Enterprise Integrations',
      description: 'Interoperability with institutional ecosystems.',
    },
    {
      title: 'Developer Platform',
      description: 'Enablement for builders extending the platform responsibly.',
    },
    {
      title: 'Automation',
      description: 'Governed automation of repetitive work with accountability preserved.',
    },
    {
      title: 'Ecosystem Expansion',
      description: 'Broader connection and partnership on a stable foundation.',
    },
    {
      title: 'Continuous Innovation',
      description: 'Ongoing improvement without abandoning stewardship.',
    },
  ],
} as const;

export const whyTechnologyContent = {
  hero: {
    eyebrow: 'Why AOVIAS Technology',
    title: 'Why institutions choose AOVIAS as a technology partner',
    description:
      'Across the Technology section, one story emerges: a platform-first foundation, responsible intelligence, engineering discipline, security and trust, open integration, and long-term vision — built for organizations that must endure.',
    primaryCta: {
      label: 'Explore products',
      href: '/products',
    },
    secondaryCta: {
      label: 'Contact AOVIAS',
      href: '/company/contact',
    },
  },
  themes: [
    {
      title: 'Platform-first architecture',
      description: 'Shared capabilities power products with consistency and longevity.',
      href: '/technology/platform',
    },
    {
      title: 'Responsible AI',
      description: 'Intelligence that assists people and preserves accountability.',
      href: '/technology/ai',
    },
    {
      title: 'Engineering discipline',
      description: 'Quality, reliability, and maintainability as designed outcomes.',
      href: '/technology/engineering',
    },
    {
      title: 'Security & trust',
      description: 'Security, privacy, identity, and stewardship as platform principles.',
      href: '/technology/trust',
    },
    {
      title: 'Open integration',
      description: 'Interoperability with the systems institutions already run.',
      href: '/technology/integrations',
    },
    {
      title: 'Long-term technology vision',
      description: 'Evolution through capability themes — not short-term hype.',
      href: '/technology/roadmap',
    },
  ],
} as const;
