/**
 * AOVIAS Platform section copy — Sprint T2.
 * High-level, timeless, enterprise audience. No implementation internals.
 */

export const platformHubContent = {
  hero: {
    eyebrow: 'AOVIAS Platform',
    title: 'One enterprise platform. Many institutional products.',
    description:
      'AOVIAS is built as a shared technology foundation — common services, engineering standards, and security posture that every product inherits. Products differ by mission; the platform keeps them coherent.',
    primaryCta: {
      label: 'Explore Platform Architecture',
      href: '/technology/architecture',
    },
    secondaryCta: {
      label: 'Platform overview',
      href: '/technology/platform/overview',
    },
  },
  whyPlatform: {
    eyebrow: 'Why a platform?',
    title: 'Coherence at institutional scale',
    description:
      'Institutions do not need another collection of disconnected tools. A platform approach delivers shared capabilities once, then specializes through products — improving consistency, reducing duplication, and making long-term evolution tractable.',
  },
  vision: {
    eyebrow: 'Platform vision',
    title: 'Technology as an instrument for stronger institutions',
    description:
      'The platform exists to support organizations society relies on — with clarity, responsible intelligence, and durable architecture. Capability growth should strengthen operations and governance, not create novelty for its own sake.',
  },
  principles: [
    {
      title: 'One enterprise platform',
      description:
        'A single foundation underpins the portfolio so products share language, quality bars, and operational patterns.',
    },
    {
      title: 'Multiple products',
      description:
        'Command, Studio, and future products specialize for distinct missions while remaining part of one system.',
    },
    {
      title: 'Shared platform services',
      description:
        'Identity, runtime, publishing, intelligence orchestration, and related services are provided once and reused.',
    },
    {
      title: 'Common engineering standards',
      description:
        'Delivery, assurance, and design standards apply across the platform so institutions experience predictability.',
    },
    {
      title: 'Security by design',
      description:
        'Trust controls and privacy expectations are treated as platform requirements, not product afterthoughts.',
    },
    {
      title: 'AI as a shared capability',
      description:
        'Intelligence is a platform capability applied where it serves institutional work — with oversight and clear purpose.',
    },
  ],
  benefits: [
    {
      title: 'Operational consistency',
      description:
        'Teams encounter familiar patterns across products, reducing training friction and integration risk.',
    },
    {
      title: 'Enterprise scalability',
      description:
        'Architecture choices favor growth across organizations, workloads, and regulatory contexts.',
    },
    {
      title: 'Long-term evolution',
      description:
        'Roadmaps can extend the foundation without rewriting every product from scratch.',
    },
  ],
  capabilities: {
    eyebrow: 'Shared capabilities',
    title: 'What the platform provides to every product',
    description:
      'From identity and deployment to search, analytics, and responsible intelligence orchestration — products compose platform capabilities rather than reinventing them.',
  },
  future: {
    eyebrow: 'Future platform strategy',
    title: 'Extend the foundation deliberately',
    description:
      'APIs, deeper developer surfaces, and additional shared services will arrive as the portfolio and institutional needs mature — registered in Technology navigation as the architecture grows.',
  },
} as const;

export const platformOverviewContent = {
  hero: {
    eyebrow: 'Platform overview',
    title: 'Platform-first by design',
    description:
      'AOVIAS products are expressions of one platform. This overview explains the philosophy, the shared capability model, and how consistency and extensibility are preserved as the portfolio expands.',
    primaryCta: {
      label: 'View platform services',
      href: '/technology/platform/services',
    },
    secondaryCta: {
      label: 'Back to platform',
      href: '/technology/platform',
    },
  },
  topics: [
    {
      title: 'Platform-first philosophy',
      description:
        'Capabilities are designed for reuse before they are specialized into product experiences. That discipline keeps the portfolio coherent as new products appear.',
    },
    {
      title: 'Shared capabilities',
      description:
        'Cross-cutting concerns — identity, runtime, publishing, intelligence, notifications, search, analytics, and integrations — live at the platform layer.',
    },
    {
      title: 'Products on one platform',
      description:
        'Command and Studio (and future products) consume the same foundation, then add mission-specific workflows and interfaces.',
    },
    {
      title: 'Operational consistency',
      description:
        'Shared patterns for delivery, assurance, and user experience reduce the cost of running multiple products inside one institution.',
    },
    {
      title: 'Enterprise scalability',
      description:
        'The platform anticipates multi-organization deployment, varied workloads, and the need for clear operational boundaries.',
    },
    {
      title: 'Future extensibility',
      description:
        'New industries, products, and services can attach to the foundation without abandoning prior investments.',
    },
  ],
} as const;

export const platformServicesContent = {
  hero: {
    eyebrow: 'Platform services',
    title: 'Shared services with clear responsibilities',
    description:
      'Platform services describe what the foundation provides — not how it is hosted. Each service owns a responsibility that products should not re-implement.',
    primaryCta: {
      label: 'See the ecosystem',
      href: '/technology/platform/ecosystem',
    },
    secondaryCta: {
      label: 'Architecture overview',
      href: '/technology/architecture',
    },
  },
  services: [
    {
      id: 'identity',
      title: 'Identity',
      description:
        'Establishes who is acting, within which organization, and under which access boundaries across products.',
    },
    {
      id: 'runtime',
      title: 'Runtime',
      description:
        'Provides the execution environment and operational context products rely on to run with consistency.',
    },
    {
      id: 'publishing',
      title: 'Publishing',
      description:
        'Supports controlled release of content, configurations, and product artifacts into institutional environments.',
    },
    {
      id: 'deployment',
      title: 'Deployment',
      description:
        'Coordinates how platform and product capabilities are delivered into target environments with predictability.',
    },
    {
      id: 'ai-orchestrator',
      title: 'AI Orchestrator',
      description:
        'Coordinates responsible intelligence capabilities as a shared service — with purpose, oversight, and product boundaries.',
    },
    {
      id: 'notifications',
      title: 'Notifications',
      description:
        'Delivers timely institutional signals across channels without each product inventing its own messaging stack.',
    },
    {
      id: 'search',
      title: 'Search',
      description:
        'Makes knowledge and operational information discoverable with shared indexing and retrieval patterns.',
    },
    {
      id: 'analytics',
      title: 'Analytics',
      description:
        'Provides measurement and insight surfaces that respect institutional context and product separation.',
    },
    {
      id: 'commerce',
      title: 'Commerce',
      description:
        'Future commercial and entitlement services for packaging platform value — planned, not yet a primary surface.',
      future: true,
    },
    {
      id: 'integrations',
      title: 'Integrations',
      description:
        'Connects institutional systems through governed integration patterns rather than one-off product bridges.',
    },
  ],
} as const;

export const platformEcosystemContent = {
  hero: {
    eyebrow: 'Platform ecosystem',
    title: 'How the platform connects to institutions and society',
    description:
      'The ecosystem view shows relationships — from shared services and products to organizations, people, and societal outcomes. Technology is an instrument; stronger institutions are the aim.',
    primaryCta: {
      label: 'Explore architecture',
      href: '/technology/architecture',
    },
    secondaryCta: {
      label: 'Platform services',
      href: '/technology/platform/services',
    },
  },
  layers: [
    {
      title: 'AOVIAS Platform',
      description: 'Shared foundation, standards, and security posture.',
    },
    {
      title: 'Shared services',
      description: 'Reusable capabilities every product can compose.',
    },
    {
      title: 'Products',
      description: 'Command, Studio, and future products for specific missions.',
    },
    {
      title: 'Organizations',
      description: 'Hospitals, agencies, campuses, and enterprises that operate the products.',
    },
    {
      title: 'People',
      description: 'Operators, leaders, and communities who depend on those organizations.',
    },
    {
      title: 'Society',
      description: 'Public outcomes that improve when institutions perform with clarity and trust.',
    },
  ],
} as const;

export const platformArchitectureContent = {
  hero: {
    eyebrow: 'Platform architecture',
    title: 'Architectural philosophy for institutional software',
    description:
      'AOVIAS architecture is modular, service-oriented, and secure by design. Diagrams stay conceptual — they communicate structure and intent, not internal infrastructure.',
    primaryCta: {
      label: 'Return to platform',
      href: '/technology/platform',
    },
    secondaryCta: {
      label: 'Technology home',
      href: '/technology',
    },
  },
  principles: [
    {
      title: 'Modular architecture',
      description:
        'Capabilities are separated into modules with clear boundaries so products can compose what they need.',
    },
    {
      title: 'Service-oriented design',
      description:
        'Shared services expose responsibilities that products consume without owning the underlying concern.',
    },
    {
      title: 'Shared platform services',
      description:
        'Cross-cutting services remain at the foundation to prevent drift and duplicated risk.',
    },
    {
      title: 'Registry-driven design',
      description:
        'Products, industries, and technology topics are registered as structured catalogs — navigation, routes, and content stay aligned as the portfolio grows.',
    },
    {
      title: 'Secure-by-design',
      description:
        'Security and privacy controls are first-class architectural concerns, not bolt-ons.',
    },
    {
      title: 'AI-ready architecture',
      description:
        'Intelligence integrates as an orchestrated capability with governance hooks — not as an unbounded layer.',
    },
    {
      title: 'Scalability',
      description:
        'Structures anticipate growth across organizations, workloads, and product surfaces.',
    },
    {
      title: 'Reliability',
      description:
        'Operational expectations favor durable behavior suitable for institutional dependence.',
    },
    {
      title: 'Observability',
      description:
        'The architecture assumes systems must be understandable in operation — measurable, diagnosable, accountable.',
    },
  ],
} as const;
