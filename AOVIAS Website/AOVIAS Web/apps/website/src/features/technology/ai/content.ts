/**
 * Artificial Intelligence section copy — Sprint T3.
 * Philosophy-driven. No models, vendors, or infrastructure.
 * Recurring theme: AI amplifies responsible human judgment; it does not replace it.
 */

export const AI_GUIDING_LINE =
  'Artificial Intelligence should amplify responsible human judgment, not replace it.';

export const aiHubContent = {
  hero: {
    eyebrow: 'Artificial Intelligence',
    title: 'Intelligence that assists people — and preserves accountability',
    description:
      'Across the AOVIAS Platform, Artificial Intelligence helps operators and leaders work with greater clarity. It proposes, summarizes, and surfaces patterns. People review, decide, and remain accountable.',
    primaryCta: { label: 'AI principles', href: '/technology/ai/principles' },
    secondaryCta: { label: 'Responsible AI', href: '/technology/ai/responsible' },
  },
  guidingLine: AI_GUIDING_LINE,
  whyAi: {
    eyebrow: 'Why AI?',
    title: 'Reduce burden without surrendering judgment',
    description:
      'Institutions face growing complexity. Used carefully, AI can reduce repetitive effort and illuminate options — while leaving critical decisions with the people who hold institutional responsibility.',
  },
  philosophy: {
    eyebrow: 'AI philosophy',
    title: 'Aligned with the AOVIAS Constitution',
    description:
      'Technology exists to strengthen institutions. Trust matters more than automation. Explainability matters more than novelty. Human oversight is mandatory for consequential decisions.',
  },
  acrossPlatform: {
    eyebrow: 'AI across the platform',
    title: 'A shared capability, not a standalone product',
    description:
      'Intelligence is orchestrated as a platform capability that products can compose — with the same security, privacy, and governance expectations as the rest of the foundation.',
  },
  benefits: [
    {
      title: 'Clearer decisions',
      description:
        'Recommendations and insights help people see relevant context faster — without removing their authority to decide.',
    },
    {
      title: 'Lower operational burden',
      description:
        'Assistance with routine steps frees attention for judgment, exception handling, and care.',
    },
    {
      title: 'Consistent standards',
      description:
        'Shared intelligence capabilities inherit platform controls for privacy, security, and auditability.',
    },
  ],
  partnership: {
    eyebrow: 'Human partnership',
    title: 'People remain essential',
    description:
      'AI does not replace accountability. Operators, clinicians, administrators, and leaders stay responsible for outcomes. The platform is designed so that collaboration between people and intelligence is explicit.',
  },
  future: {
    eyebrow: 'Future vision',
    title: 'Evolve capability without abandoning principle',
    description:
      'Capability may deepen over time — always in service of better human decisions, never as a substitute for institutional duty. See the AI Roadmap for themes, not speculative commitments.',
  },
} as const;

export const aiPrinciplesContent = {
  hero: {
    eyebrow: 'AI principles',
    title: 'Enduring commitments for institutional intelligence',
    description:
      'These principles guide how AOVIAS designs and applies AI across the platform. They are commitments of character — not a feature checklist.',
    primaryCta: {
      label: 'Human-in-the-loop',
      href: '/technology/ai/human-in-the-loop',
    },
    secondaryCta: { label: 'AI at AOVIAS', href: '/technology/ai' },
  },
  principles: [
    {
      title: 'Human-centered design',
      description:
        'Intelligence is shaped around the people who operate institutions — their workflows, limits, and duty of care.',
    },
    {
      title: 'Transparency',
      description:
        'Users should understand when assistance is present and what role it plays in a workflow.',
    },
    {
      title: 'Explainability',
      description:
        'Where AI contributes to a recommendation or insight, the path to understanding matters more than opaque novelty.',
    },
    {
      title: 'Accountability',
      description:
        'People and institutions remain accountable for decisions. Systems are designed to support that reality, not obscure it.',
    },
    {
      title: 'Privacy',
      description:
        'Sensitive institutional information is treated with restraint. Intelligence capabilities respect privacy expectations of the platform.',
    },
    {
      title: 'Security',
      description:
        'AI capabilities inherit secure-by-design expectations — access boundaries, integrity, and responsible operation.',
    },
    {
      title: 'Fairness',
      description:
        'We design with awareness that biased outcomes harm trust. Fairness is pursued as an ongoing engineering concern.',
    },
    {
      title: 'Reliability',
      description:
        'Assistance must be dependable enough for institutional use — calm, consistent, and suitable for operational contexts.',
    },
    {
      title: 'Continuous improvement',
      description:
        'Principles endure while practice improves. Feedback, review, and refinement are part of responsible stewardship.',
    },
  ],
} as const;

export const aiCapabilitiesContent = {
  hero: {
    eyebrow: 'AI capabilities',
    title: 'Outcomes that assist institutional work',
    description:
      'Capabilities describe the kinds of help the platform can provide. They are outcome-oriented — not promises of autonomous decision-making.',
    primaryCta: {
      label: 'Human-in-the-loop',
      href: '/technology/ai/human-in-the-loop',
    },
    secondaryCta: {
      label: 'Responsible AI',
      href: '/technology/ai/responsible',
    },
  },
  note: 'AOVIAS does not position AI as a replacement for professional or institutional judgment.',
  capabilities: [
    {
      title: 'Intelligent recommendations',
      description:
        'Surface relevant options so people can choose with better context.',
    },
    {
      title: 'Workflow assistance',
      description:
        'Help complete routine steps and keep processes moving without removing approval points.',
    },
    {
      title: 'Knowledge discovery',
      description:
        'Make policies, guidance, and institutional knowledge easier to find and apply.',
    },
    {
      title: 'Compliance guidance',
      description:
        'Point people toward applicable rules and expectations — humans interpret and act.',
    },
    {
      title: 'Operational insights',
      description:
        'Highlight patterns in operations that deserve human attention.',
    },
    {
      title: 'Pattern recognition',
      description:
        'Detect recurring signals that may indicate risk, delay, or opportunity.',
    },
    {
      title: 'Predictive alerts',
      description:
        'Notify operators of emerging conditions that warrant review — not automatic resolution.',
    },
    {
      title: 'Natural language assistance',
      description:
        'Help people draft, search, and clarify in plain language while preserving review and control.',
    },
  ],
} as const;

export const aiHitlContent = {
  hero: {
    eyebrow: 'Human-in-the-loop',
    title: 'AI proposes. People decide.',
    description:
      'Institutional responsibility always remains with people. The collaboration model is explicit: intelligence assists; humans review, approve, and stay accountable.',
    primaryCta: {
      label: 'Responsible AI',
      href: '/technology/ai/responsible',
    },
    secondaryCta: {
      label: 'AI capabilities',
      href: '/technology/ai/capabilities',
    },
  },
  guidingLine: AI_GUIDING_LINE,
  topics: [
    {
      title: 'AI proposes',
      description:
        'The platform may suggest actions, summaries, or insights based on available context.',
    },
    {
      title: 'Humans review',
      description:
        'Operators and leaders examine recommendations against policy, ethics, and situational judgment.',
    },
    {
      title: 'Humans approve',
      description:
        'Consequential steps require human confirmation. Automation does not silently conclude critical outcomes.',
    },
    {
      title: 'Humans remain accountable',
      description:
        'Duty of care, legal responsibility, and institutional authority stay with people and organizations.',
    },
  ],
} as const;

export const aiResponsibleContent = {
  hero: {
    eyebrow: 'Responsible AI',
    title: 'Governance as an engineering principle',
    description:
      'Responsible AI is how AOVIAS builds trust into intelligence capabilities — by design. We discuss principles and controls, not certifications we have not earned.',
    primaryCta: { label: 'AI principles', href: '/technology/ai/principles' },
    secondaryCta: { label: 'AI roadmap', href: '/technology/ai/roadmap' },
  },
  guidingLine: AI_GUIDING_LINE,
  topics: [
    {
      title: 'Responsible AI by design',
      description:
        'Controls and oversight are considered when capabilities are shaped — not bolted on after launch.',
    },
    {
      title: 'Privacy protection',
      description:
        'Sensitive data is handled with platform privacy expectations; intelligence does not create a separate, weaker standard.',
    },
    {
      title: 'Secure processing',
      description:
        'Access, integrity, and secure operation apply to intelligence services as they do to the rest of the platform.',
    },
    {
      title: 'Auditability',
      description:
        'Institutions need the ability to understand what assistance was offered and how workflows proceeded.',
    },
    {
      title: 'Traceability',
      description:
        'Important contributions from AI should be attributable within operational context for later review.',
    },
    {
      title: 'Bias awareness',
      description:
        'We acknowledge that unfair outcomes erode trust. Awareness and mitigation are ongoing responsibilities.',
    },
    {
      title: 'Governance',
      description:
        'Clear ownership of how intelligence is used inside products and institutions is part of responsible deployment.',
    },
    {
      title: 'Enterprise controls',
      description:
        'Organizational policy, access boundaries, and human approval points remain first-class concerns.',
    },
  ],
} as const;

export const aiRoadmapContent = {
  hero: {
    eyebrow: 'AI roadmap',
    title: 'Capability themes — not speculative commitments',
    description:
      'The roadmap describes directions of maturity. It does not publish dates, vendors, or unproven features. Principle remains constant as capability evolves.',
    primaryCta: { label: 'AI at AOVIAS', href: '/technology/ai' },
    secondaryCta: {
      label: 'AOVIAS Platform',
      href: '/technology/platform',
    },
  },
  guidingLine: AI_GUIDING_LINE,
  themes: [
    {
      title: 'Assist',
      description: 'Help people complete work with less friction.',
    },
    {
      title: 'Recommend',
      description: 'Offer options grounded in context for human selection.',
    },
    {
      title: 'Understand',
      description: 'Improve clarity over policies, records, and operational signals.',
    },
    {
      title: 'Coordinate',
      description: 'Support handoffs and shared awareness across teams.',
    },
    {
      title: 'Automate repetitive tasks',
      description:
        'Reduce rote effort where risk is low and human review remains available when needed.',
    },
    {
      title: 'Enable better decisions',
      description:
        'Strengthen judgment with timely insight — never by removing accountability.',
    },
  ],
} as const;
