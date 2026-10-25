/**
 * Engineering section copy — Sprint T4.
 * Philosophy-driven. No languages, frameworks, or proprietary implementation.
 * Theme: engineering for longevity, reliability, and institutional stewardship.
 */

export const ENGINEERING_GUIDING_LINE =
  'Engineering excellence is earned through discipline, not speed.';

export const engineeringHubContent = {
  hero: {
    eyebrow: 'Engineering',
    title: 'Discipline that institutions can depend on',
    description:
      'AOVIAS engineers enterprise software with long-term thinking: quality by design, reliability as a feature, and architecture that evolves without sacrificing stability.',
    primaryCta: {
      label: 'Architecture',
      href: '/technology/engineering/architecture',
    },
    secondaryCta: {
      label: 'Design principles',
      href: '/technology/engineering/design-principles',
    },
  },
  guidingLine: ENGINEERING_GUIDING_LINE,
  whyMatters: {
    eyebrow: 'Why engineering matters',
    title: 'Software that organizations entrust with critical work',
    description:
      'Institutions need systems that remain clear, secure, and operable over years. Engineering is how AOVIAS earns that trust — calmly, deliberately, and with accountability.',
  },
  values: [
    {
      title: 'Responsibility',
      description:
        'Every design choice considers the people and institutions who will live with the outcome.',
    },
    {
      title: 'Discipline',
      description:
        'Excellence comes from consistent practice — review, restraint, and standards — not haste.',
    },
    {
      title: 'Longevity',
      description:
        'We engineer for years of stewardship, not the next short-term trend cycle.',
    },
  ],
  quality: {
    eyebrow: 'Quality by design',
    title: 'Quality is designed, not inspected',
    description:
      'Quality is built into architecture, workflows, and review — not added at the end. Defects are prevented by clarity of design as much as by testing.',
  },
  longTerm: {
    eyebrow: 'Long-term thinking',
    title: 'Systems that evolve without sacrificing stability',
    description:
      'Architecture enables change. We prefer decisions that keep future options open while protecting operational calm today.',
  },
  continuous: {
    eyebrow: 'Continuous improvement',
    title: 'A permanent engineering practice',
    description:
      'Feedback from operations, users, and review loops back into design. Improvement is ongoing stewardship — not a temporary program.',
  },
  institutions: {
    eyebrow: 'Building for institutions',
    title: 'Engineering serves institutions',
    description:
      'Technology should become simpler as organizations grow. Shared platforms, clear boundaries, and maintainable systems reduce burden instead of compounding it.',
  },
} as const;

export const engineeringArchitectureContent = {
  hero: {
    eyebrow: 'Architecture',
    title: 'Architecture that enables long-term evolution',
    description:
      'AOVIAS architecture is platform-first and modular: shared services, clear separation of concerns, and room to extend without rewriting the foundation.',
    primaryCta: {
      label: 'Design principles',
      href: '/technology/engineering/design-principles',
    },
    secondaryCta: {
      label: 'Platform architecture',
      href: '/technology/architecture',
    },
  },
  topics: [
    {
      title: 'Platform-first architecture',
      description:
        'Products compose shared capabilities instead of rebuilding the same foundations repeatedly.',
    },
    {
      title: 'Modular design',
      description:
        'Boundaries keep change local. Teams can evolve modules without destabilizing the whole.',
    },
    {
      title: 'Service-oriented systems',
      description:
        'Capabilities are organized so responsibilities stay clear and composable across products.',
    },
    {
      title: 'Separation of concerns',
      description:
        'Presentation, domain logic, and platform services remain distinct so complexity stays manageable.',
    },
    {
      title: 'Shared platform services',
      description:
        'Cross-cutting needs — identity, data stewardship, orchestration — live once and serve many.',
    },
    {
      title: 'Extensibility',
      description:
        'New products and institutional needs can attach to the platform without fracturing architecture.',
    },
    {
      title: 'Future evolution',
      description:
        'We design so the system can grow and adapt while protecting stability for those who depend on it.',
    },
  ],
} as const;

export const engineeringDesignPrinciplesContent = {
  hero: {
    eyebrow: 'Design principles',
    title: 'Principles that guide product and engineering',
    description:
      'These principles shape how we design interfaces, services, and platform capabilities — so systems stay clear, safe, and maintainable.',
    primaryCta: {
      label: 'Reliability',
      href: '/technology/engineering/reliability',
    },
    secondaryCta: {
      label: 'Engineering philosophy',
      href: '/technology/engineering',
    },
  },
  principles: [
    {
      title: 'Simplicity',
      description:
        'Prefer the clearest solution that meets institutional need. Complexity must earn its keep.',
    },
    {
      title: 'Consistency',
      description:
        'Familiar patterns reduce training burden and operational surprise across products.',
    },
    {
      title: 'Composability',
      description:
        'Capabilities combine into larger solutions without brittle coupling.',
    },
    {
      title: 'Reusability',
      description:
        'Shared building blocks accelerate delivery while preserving quality standards.',
    },
    {
      title: 'Accessibility',
      description:
        'Inclusive design is part of engineering excellence — usable by the people institutions serve.',
    },
    {
      title: 'Security by design',
      description:
        'Security is foundational. Controls are considered as systems are shaped, not bolted on later.',
    },
    {
      title: 'Performance by default',
      description:
        'Responsiveness and efficiency are expected qualities of institutional software.',
    },
    {
      title: 'Maintainability',
      description:
        'Code and architecture must remain understandable for the teams who will steward them for years.',
    },
    {
      title: 'Human-centered design',
      description:
        'Workflows respect how people work under real operational pressure.',
    },
  ],
} as const;

export const engineeringReliabilityContent = {
  hero: {
    eyebrow: 'Reliability',
    title: 'Reliability is a feature',
    description:
      'Dependable software is an engineering intent: predictable behavior, resilience under stress, and calm operations — discussed as philosophy, not as published SLAs.',
    primaryCta: {
      label: 'Observability',
      href: '/technology/engineering/observability',
    },
    secondaryCta: {
      label: 'Scalability',
      href: '/technology/engineering/scalability',
    },
  },
  note: 'We do not publish uptime guarantees or SLA figures on this site. This page describes engineering intent.',
  topics: [
    {
      title: 'Availability',
      description:
        'Systems are designed so critical institutional work can proceed with confidence during normal and degraded conditions.',
    },
    {
      title: 'Fault tolerance',
      description:
        'Failures are expected. Design aims to contain impact and support recovery without cascading surprise.',
    },
    {
      title: 'Predictable behavior',
      description:
        'Operators and leaders should trust that the system behaves consistently under known conditions.',
    },
    {
      title: 'Operational stability',
      description:
        'Calm operations matter. Change is managed so production remains understandable.',
    },
    {
      title: 'Resilience',
      description:
        'The platform should absorb disruption and return to a trustworthy state.',
    },
    {
      title: 'Continuous monitoring',
      description:
        'Visibility into health supports early response — reliability and observability reinforce each other.',
    },
  ],
} as const;

export const engineeringScalabilityContent = {
  hero: {
    eyebrow: 'Scalability',
    title: 'Designed to grow with institutions',
    description:
      'Scalability means supporting organizational growth, multi-product strategy, and modular expansion — through sustainable architecture, not infrastructure slogans.',
    primaryCta: {
      label: 'Observability',
      href: '/technology/engineering/observability',
    },
    secondaryCta: {
      label: 'Architecture',
      href: '/technology/engineering/architecture',
    },
  },
  topics: [
    {
      title: 'Organizational growth',
      description:
        'As teams and usage expand, the platform should remain coherent rather than fragmenting into one-off systems.',
    },
    {
      title: 'Platform evolution',
      description:
        'Shared capabilities deepen over time so products inherit progress instead of reinventing it.',
    },
    {
      title: 'Modular expansion',
      description:
        'New modules and products attach to clear boundaries, keeping change manageable.',
    },
    {
      title: 'Enterprise readiness',
      description:
        'Patterns for access, governance, and operations scale with institutional expectation.',
    },
    {
      title: 'Multi-product strategy',
      description:
        'One foundation supports a portfolio — Command, Studio, and future products — without architectural drift.',
    },
    {
      title: 'Sustainable architecture',
      description:
        'Growth should simplify stewardship: shared services, clear ownership, and long-lived design choices.',
    },
  ],
} as const;

export const engineeringObservabilityContent = {
  hero: {
    eyebrow: 'Observability',
    title: 'Visibility that enables reliable operations',
    description:
      'Observability is how teams understand system behavior: monitoring, diagnostics, metrics, logs, and health signals that feed continuous improvement.',
    primaryCta: {
      label: 'Reliability',
      href: '/technology/engineering/reliability',
    },
    secondaryCta: {
      label: 'Engineering philosophy',
      href: '/technology/engineering',
    },
  },
  topics: [
    {
      title: 'Monitoring',
      description:
        'Ongoing awareness of system condition so issues surface before they become crises.',
    },
    {
      title: 'Diagnostics',
      description:
        'The ability to investigate behavior with enough context to act responsibly.',
    },
    {
      title: 'Logging',
      description:
        'Structured operational records that support review, auditability, and learning.',
    },
    {
      title: 'Metrics',
      description:
        'Quantitative signals that reveal trends, capacity pressure, and service health.',
    },
    {
      title: 'Health monitoring',
      description:
        'Clear views of whether critical capabilities are fit for institutional use.',
    },
    {
      title: 'Operational insights',
      description:
        'Patterns from operations inform engineering priorities and product clarity.',
    },
    {
      title: 'Continuous feedback',
      description:
        'What is observed returns to design and reliability practice — closing the loop.',
    },
  ],
} as const;
